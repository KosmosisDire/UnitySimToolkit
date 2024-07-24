using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using System.Threading.Tasks;
using RosMessageTypes.Actionlib;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.Std;
using SimToolkit;
using System;
using SimToolkit.ROS.Urdf;
using System.Linq;
using RosMessageTypes.Sensor;
using System.Collections.Generic;
using SimToolkit.ROS.Srdf;
using Toolkit;
using System.Collections;
namespace SimToolkit.ROS.Moveit
{
[Serializable]
public class MoveGroupController
{
    public TransformGizmo gizmo;
    public MoveGroup moveGroup;
    public UrdfRobot planningRobot;
    public MoveGroupControllerSettings settings = new MoveGroupControllerSettings();
    public MoveGroupState state;

    public string Name => moveGroup.name;

    public URDFJointMirror jointMirror;
    public UrdfLink planningBaseLink;
    public UrdfLink planningEndEffector;
    
    // events
    [HideInInspector] public event Action<Vector3, Quaternion, MoveGroupController> OnTransform;
    [HideInInspector] public event Action<TransformGizmo.Axis, TransformGizmo.ControlType, MoveGroupController> OnGrab;
    [HideInInspector] public event Action<TransformGizmo.Axis, TransformGizmo.ControlType, MoveGroupController> OnRelease;

    public MoveGroupController(MoveGroup group, MoveGroupControllerSettings settings)        
    {
        this.moveGroup = group;
        this.settings = settings;
        this.state = new MoveGroupState();

        // Create parent for cloned robots
        planningRobot = moveGroup.robot.planningRobot;
        if (planningRobot == null)
        {
            planningRobot = GameObject.Instantiate(moveGroup.robot.gameObject).GetComponent<UrdfRobot>();
            planningRobot.isPlanningRobot = true;
            planningRobot.enabled = true;
            var allComponents = planningRobot.gameObject.GetComponents<Component>();

            // delete all components other than UrdfJointMirror and UrdfRobot
            for (int i = 0; i < 3; i++) // three times in case there are component dependencies
            {
                foreach (var component in allComponents)
                {
                    if (component is URDFJointMirror || component is UrdfRobot || component is Transform) continue;

                    try
                    {
                        GameObject.Destroy(component);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }

            moveGroup.robot.planningRobot = planningRobot;
        }

        jointMirror = planningRobot.GetComponent<URDFJointMirror>();
        jointMirror.enabled = true;
        jointMirror.localControl = true;
        jointMirror.instantanious = true;

        gizmo = TransformGizmo.Create();
        gizmo.RotationAxes = settings.AngularAxes;
        gizmo.PositionAxes = settings.LinearAxes;
        gizmo.OnMove += (lastPosition, newPosition) => OnTransform?.Invoke(newPosition, gizmo.Rotation, this);
        gizmo.OnRotate += (lastPosition, newPosition) => OnTransform?.Invoke(gizmo.Position, gizmo.Rotation, this);
        gizmo.OnGrab += (axis, type) => OnGrab?.Invoke(axis, type, this);
        gizmo.OnRelease += (axis, type) => OnRelease?.Invoke(axis, type, this);
        
        if (planningRobot)
        {
            var endEffector = SrdfManager.Description.GetEndEffectorForGroup(group.name);
            Debug.Log(endEffector);
            if (endEffector != null)
                planningEndEffector = planningRobot.GetLink(endEffector.parentLink);
            if (planningEndEffector == null)
                planningEndEffector = planningRobot.GetJoint(SrdfManager.Description.GetTipJointForGroup(group.name)).childLink;
            if (planningEndEffector == null)
                planningEndEffector = planningRobot.links[planningRobot.links.Count - 1];

            var baseJoint = SrdfManager.Description.GetBaseJointForGroup(group.name);
            if (baseJoint != null)
                planningBaseLink = planningRobot.GetJoint(baseJoint).parentLink;
            if (planningBaseLink == null)
                planningBaseLink = planningRobot.links[0];
        }
    }

    public void SetPlanningMaterial(Material mat)
    {
        var renderers = planningBaseLink.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select((object m) => mat).ToArray();
        }
    }

    public void SetTrueMaterial(Material mat)
    {
        var renderers = moveGroup.robot.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select((object m) => mat).ToArray();
        }
    }

    public async Task<TrajectoryForwardKinematic?> GetFKTrajectory(TrajectoryInverseKinematic trajectory)
    {
        var plannedTrajectory = new TrajectoryForwardKinematic(new JointStateMsg[trajectory.positions.Count]);

        var startingState = moveGroup.FilterJoints(trajectory.startingState);
        for (int i = 0; i < trajectory.positions.Count; i++)
        {
            var states = await SolveIK(startingState, trajectory.positions[i], trajectory.rotations[i]);
            if (states == null)
            {
                return null;
            }

            plannedTrajectory.jointStates[i] = states;
            startingState = states;
        }

        return plannedTrajectory;
    }

    public void Execute(TrajectoryForwardKinematic trajectory)
    {
        if (!state.IsReady(out var feedback))
        {
            NotificationManager.Notice($"Cannot execute {moveGroup.name}: {feedback}");
            return;
        }

        var jointStates = new List<JointStateMsg>();
        for (int i = 0; i < trajectory.jointStates.Length; i++)
        {
            if (trajectory.jointStates[i] == null)
            {
                NotificationManager.Notice("Invalid trajectory, joint state was null");
                return;
            }

            if (trajectory.jointStates[i].name.Length == 0)
            {
                NotificationManager.Notice("Invalid trajectory, joint state was empty");
                return;
            }

            trajectory.jointStates[i] = moveGroup.FilterJoints(trajectory.jointStates[i]);

            if (trajectory.jointStates[i].name.Length == 0)
            {
                NotificationManager.Notice("Invalid trajectory, joint states do not apply to this move group");
                return;
            }

            jointStates.Add(trajectory.jointStates[i]);
        }

        if (jointStates.Count == 0)
        {
            NotificationManager.Notice("Invalid trajectory, no joint states supplied");
            return;
        }

        if (jointStates.Count > 2)
        {
            NotificationManager.Notice("Invalid trajectory, only a start and end state can be supplied to this function");
            return;
        }

        state.isComplete = false;
        state.status = TrajectoryStatus.None;
        state.currentTrajectory = trajectory;

        var motionPlanRequest = new MotionPlanRequestMsg()
        {
            group_name = moveGroup.name,
            pipeline_id = "ompl",
            planner_id = settings.planningOptions.planner.ToString(),
            allowed_planning_time = settings.planningOptions.planningTime,
            num_planning_attempts = settings.planningOptions.attempts,
            max_velocity_scaling_factor = settings.planningOptions.maxVelocityScaling,
            max_acceleration_scaling_factor = settings.planningOptions.maxAccelerationScaling,
            
            workspace_parameters = new WorkspaceParametersMsg()
            {
                header = new HeaderMsg()
                {
                    stamp = new TimeMsg()
                    {
                        sec = 0,
                        nanosec = 0
                    },
                    frame_id = settings.planningOptions.worldFrame
                },
                min_corner = new Vector3Msg()
                {
                    x = -2,
                    y = -2,
                    z = -2
                },
                max_corner = new Vector3Msg()
                {
                    x = 2,
                    y = 2,
                    z = 2
                }
            },

            start_state = new RobotStateMsg()
            {
                joint_state = jointStates[0]
            },

            goal_constraints = new ConstraintsMsg[]
            {
                new ConstraintsMsg()
                {
                    joint_constraints = jointStates[jointStates.Count - 1].name.Select((name, i) => new JointConstraintMsg()
                    {
                        joint_name = name,
                        position = jointStates[jointStates.Count - 1].position[i],
                        weight = 1
                    }).ToArray()
                }
            },
        };

        var goal = new MoveGroupActionGoalMsg()
        {
            goal_id = new GoalIDMsg()
            {
                id = state.currentTrajectory.id,
                stamp = new TimeMsg()
                {
                    sec = 0,
                    nanosec = 0
                }
            },
            goal = new MoveGroupGoalMsg()
            {
                request = motionPlanRequest,
                planning_options = new PlanningOptionsMsg()
                {
                    replan = settings.planningOptions.replan,
                    replan_attempts = settings.planningOptions.replanAttempts,
                    replan_delay = settings.planningOptions.replanDelay
                }
            },
        };

        var ros = ROSConnection.GetOrCreateInstance();
        ros.Publish(settings.planningOptions.executeRequestTopic, goal);
    }

    public async void ExecuteMulti(TrajectoryForwardKinematic trajectory)
    {
        if (!Enumerable.SequenceEqual(trajectory.jointStates[0].position, jointMirror.JointStatesRemote.position))
        {
            trajectory.jointStates = new[] { jointMirror.JointStatesRemote }.Concat(trajectory.jointStates).ToArray();
        }

        for (int i = 1; i < trajectory.jointStates.Length; i++)
        {
            Execute(new TrajectoryForwardKinematic(new[] { trajectory.jointStates[i - 1], trajectory.jointStates[i] }));

            while (!state.IsReady(out _))
            {
                await Awaitable.NextFrameAsync();
            }
        }
    }

    public async void Execute(TrajectoryInverseKinematic trajectory)
    {
        var plannedTrajectory = await GetFKTrajectory(trajectory);
        if (!plannedTrajectory.HasValue)
        {
            NotificationManager.Notice("Failed to solve IK for trajectory");
            return;
        }
        ExecuteMulti(plannedTrajectory.Value);
    }

    public async Task<JointStateMsg> SolveIK(JointStateMsg startingState, Vector3 gizmoPosition, Quaternion gizmoOrientation)
    {
        if (state.isComputingIK) return null;
        state.isComputingIK = true;

        if (startingState == null)
        {
            Debug.LogError("Start pose not set when solving IK");
            state.isComputingIK = false;
            return null;
        }

        var gizmoPositionFLU = gizmoPosition.To<FLU>();
        var gizmoOrientationFLU = gizmoOrientation.To<FLU>();

        var request = new GetPositionIKRequest
        {
            ik_request = new PositionIKRequestMsg()
            {
                group_name = moveGroup.name,
                avoid_collisions = settings.ikOptions.avoidCollisions,
                timeout = new DurationMsg((int)MathF.Floor(settings.ikOptions.planningTime), (int)((settings.ikOptions.planningTime - MathF.Floor(settings.ikOptions.planningTime)) * 1e9f)),
                attempts = settings.ikOptions.attempts,
                ik_link_name = planningEndEffector.LinkName,
                pose_stamped = new PoseStampedMsg()
                {
                    header = new HeaderMsg()
                    {
                        stamp = new TimeMsg()
                        {
                            sec = 0,
                            nanosec = 0
                        },
                        frame_id = TFManager.WorldFrameName ?? "world"
                    },
                    pose = new PoseMsg()
                    {
                        position = new PointMsg()
                        {
                            x = gizmoPositionFLU.x,
                            y = gizmoPositionFLU.y,
                            z = gizmoPositionFLU.z
                        },
                        orientation = new QuaternionMsg()
                        {
                            x = gizmoOrientationFLU.x,
                            y = gizmoOrientationFLU.y,
                            z = gizmoOrientationFLU.z,
                            w = gizmoOrientationFLU.w
                        }
                    }
                },
                robot_state = new RobotStateMsg()
                {
                    joint_state = startingState
                }
            }
        };

        var ros = ROSConnection.GetOrCreateInstance();
        var response = await ros.SendServiceMessage<GetPositionIKResponse>(settings.ikOptions.computeIkService, request);
        var result = response?.solution?.joint_state;
        if (response != null)
        {
            state.hasValidPose = false;
            if (gizmo) gizmo.BlendColor = gizmo.BlendColor.WithAlpha(0);

            if (response.error_code.val == 1) // valid solution found
            {
                state.hasValidPose = true;
                result = response?.solution?.joint_state;
            }
            else if (response.error_code.val == -31) // no solution found
            {
                if (gizmo) gizmo.BlendColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
                request.ik_request.avoid_collisions = false;
                response = await ros.SendServiceMessage<GetPositionIKResponse>(settings.ikOptions.computeIkService, request);

                if (response.error_code.val == 1) // solution with colision found
                {
                    result = response?.solution?.joint_state;
                }
                else if (response.error_code.val == -31) // no solution at all found
                {
                    if (gizmo) gizmo.BlendColor = new Color(0.81f, 0.22f, 0.17f, 0.9f);
                }
            }

            if (gizmo) gizmo.RefreshColors();
        }

        if (result.name.Length == 0 || result.position.Length == 0) result = null;

        state.isComputingIK = false;
        return result;
    }
}
}