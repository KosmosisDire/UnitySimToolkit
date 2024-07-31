using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Moveit;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Actionlib;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toolkit;
using SimToolkit.ROS.Srdf;
using static Toolkit.TransformGizmo;

namespace SimToolkit.ROS.Moveit
{
[RequireComponent(typeof(URDFJointMirror))]
[RequireComponent(typeof(UrdfRobot))]
public class MoveitRobot : MonoBehaviour
{
    [Tooltip("Optional settings to modify the behaviour of specific move groups.")]
    public List<MoveGroupControllerSettings> moveGroupSettings = new();
    public List<MoveGroupController> moveGroupControllers = new();
    private Dictionary<string, Toolbar<MoveGroupController>> toolbars = new();
    private Dictionary<string, WorldUIElement> toolbarPositions = new();

    public MoveitIKOptions ikPreviewOptions = new();
    public MoveitPlannerOptions motionPlanningOptions = new();

    [Header("Visuals")]
    public Material planningMaterial;
    public Material trueMaterial;

    [Header("Toolbar Options")]
    public Vector2 toolbarOffset = new Vector2(0, -100);
    public List<ToolbarItem<MoveGroupController>> toolbarButtons;


    public UrdfRobot robot;
    private ROSConnection ros;
    private bool isExecuting = false;
    public bool IsExecuting => isExecuting;

    // Start is called before the first frame update
    async void Start()
    {
        robot = GetComponent<UrdfRobot>();

        // setup ros
        ros = await ROSManager.AwaitROS();
        ros.RegisterRosService<GetPositionIKRequest, GetPositionIKResponse>(ikPreviewOptions.computeIkService);
        ros.RegisterPublisher<MotionPlanRequestMsg>(motionPlanningOptions.planRequestTopic);
        ros.RegisterPublisher<MoveGroupActionGoalMsg>(motionPlanningOptions.executeRequestTopic);
        ros.Subscribe<GoalStatusArrayMsg>(motionPlanningOptions.statusTopic, CheckTrajectoryStates);

        var groups = SrdfManager.Groups;
        Debug.Log("Found " + groups.Count + " move groups in SRDF");
        foreach (var group in groups)
        {
            var groupSettings = moveGroupSettings.FirstOrDefault((g) => g.group == group.name);
            if (groupSettings.hide) continue;

            groupSettings.planningOptions = motionPlanningOptions;
            groupSettings.ikOptions = ikPreviewOptions;

            var moveGroup = new MoveGroup(group.name, robot);
            var MoveGroupPlanner = moveGroup.CreateController(groupSettings);
            moveGroupControllers.Add(MoveGroupPlanner);
        }
        
        foreach (var groupController in moveGroupControllers)
        {
            if (groupController.Name.Trim() == "")
            {
                Debug.LogError("Planning group name not set for move group!");
                NotificationManager.Notice("Please set the planning group name for all move groups in the Trajectory Planner component!", 15);
                continue;
            }

            groupController.SetPlanningMaterial(planningMaterial);
            groupController.SetTrueMaterial(trueMaterial);

            // add toolbar UI
            if (toolbarButtons.Count > 0)
            {
                var toolbar = new Toolbar<MoveGroupController>();
                // assign events to toolbar buttons
                foreach (var button in toolbarButtons)
                {
                    toolbar.AddItem(button.Clone(), groupController);
                }

                toolbar.Create(UIManager.rootElement);
                var toolbarPosition = toolbar.Follow(groupController.gizmo.transform);
                toolbarPositions.Add(groupController.Name, toolbarPosition);
                toolbars.Add(groupController.Name, toolbar);
            }

            // Assign move group events
            groupController.OnTransform += TargetUpdated;
            groupController.OnGrab += TargetGrabbed;
            groupController.OnRelease += TargetReleased;
        }
    }

    public MoveGroupController GetGroup(string name)
    {
        return moveGroupControllers.Find(g => g.Name == name);
    }

    private void CheckTrajectoryStates(GoalStatusArrayMsg msg)
    {
        var states = msg.status_list;
        foreach (var state in states)
        {
            var id = state.goal_id.id;
            if (!id.EndsWith("-Unity")) continue; // ignore non-unity trajectories

            var groupController = moveGroupControllers.FirstOrDefault(g => g.state.currentTrajectory.id == id);
            if (groupController == null)
            {
                continue;
            }

            var status = (TrajectoryStatus)state.status;
            if (groupController.state.status == status) continue;

            var statusText = state.text;
            if (statusText == "TIMED_OUT") statusText = "Solution could not be exectuted";
            if (statusText == "PREEMPTED") statusText = "Trajectory was canceled";
            
            Debug.Log(status + ": " + statusText);
            NotificationManager.Notice(statusText);
            groupController.state.status = status;

            switch (status)
            {
                case TrajectoryStatus.Pending:
                case TrajectoryStatus.Active:
                case TrajectoryStatus.Preempting:
                case TrajectoryStatus.Recalling:
                    groupController.state.isComplete = false;
                    break;
                default:
                    groupController.state.isComplete = true;
                    break;
            }
        }

        isExecuting = moveGroupControllers.Any(g => !g.state.isComplete);
    }

    async void TargetUpdated(Vector3 newPosition, Quaternion newRotation, MoveGroupController controller)
    {
        var jointStates = await controller.SolveIK(controller.jointMirror.JointStatesLocal, newPosition, newRotation);
        if (jointStates == null) return;

        var joints = controller.moveGroup.jointNames;
        var jointStatesCopy = controller.jointMirror.JointStatesLocal;

        // only change joint states matching the group's joints
        for (int i = 0; i < jointStatesCopy.name.Length; i++)
        {
            var name = jointStatesCopy.name[i];
            if (joints.Contains(name))
            {
                jointStatesCopy.position[i] = jointStates.position[jointStates.name.ToList().IndexOf(name)];
            }
        }

        controller.jointMirror.JointStatesLocal = jointStatesCopy;
    }

    void TargetGrabbed(Axis axis, ControlType type, MoveGroupController controller)
    {
        toolbars[controller.Name]?.Hide();
    }

    void TargetReleased(Axis axis, ControlType type, MoveGroupController controller)
    {
        toolbars[controller.Name]?.Show();
    }

    public void GoToPlanningPose(MoveGroupController group)
    {
        _ = GoToPlanningPoseAsync(group);
    }

    public async Task GoToPlanningPoseAsync(MoveGroupController controller)
    {
        isExecuting = true;

        controller.settings.planningOptions.worldFrame = TFManager.WorldFrameName;
        var startState = controller.jointMirror.JointStatesRemote;
        var endState = controller.jointMirror.JointStatesLocal;
        var trajectory = new TrajectoryForwardKinematic(new[]{startState, endState});
        await controller.ExecuteMultiPointTrajectoryAsync(trajectory);
    }

    public void SetTarget(string groupName, Vector3 position, Quaternion orientation)
    {
        var group = moveGroupControllers.Find(controller => controller.Name == groupName);
        if (group == null)
        {
            Debug.LogError("Move group with name " + groupName + " not found!");
            return;
        }

        group.gizmo.SetPosition(position);
        group.gizmo.SetRotation(orientation);
        TargetUpdated(position, orientation, group);
    }

    public void ExecuteCurrentDatabaseTrajectory(MoveGroupController group)
    {
        _ = group.ExecuteNamedTrajectory(TrajectoryDatabase.CurrentTrajectory.name);
    }

    void Update()
    {
        if (!ROSManager.IsReady) return;
        
        foreach (var group in moveGroupControllers)
        {
            if (group.gizmo && !group.gizmo.IsGrabbed) // while a target is not grabbed
            {
                // position the target at the end effector if it is not already there
                if (group.gizmo.transform.position != group.planningEndEffector.transform.position)
                {
                    group.gizmo.SetPosition(group.planningEndEffector.transform.position, false);
                    
                    // if (planningJoints.JointStatesLocal != null) 
                    //     _ = group.SolveIK(planningJoints.JointStatesLocal, group.target.Position, group.target.Rotation);
                }
                
                // rotate the target to match the end effector
                if (group.gizmo.Rotation != group.planningEndEffector.transform.rotation)
                {
                    group.gizmo.SetRotation(group.planningEndEffector.transform.rotation, false);
                    
                    // if (planningJoints.JointStatesLocal != null) 
                    //     _ = group.SolveIK(planningJoints.JointStatesLocal, group.target.Position, group.target.Rotation);
                }
            }

            var toolbar = toolbarPositions.TryGetValue(group.Name, out var toolbarPosition) ? toolbarPosition : null;
            if (toolbar != null) toolbar.screenOffset = toolbarOffset;
        }
    }

}
}
