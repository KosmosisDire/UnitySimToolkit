using System;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Sensor;
using SimToolkit.ROS.Srdf;
using SimToolkit.ROS.Urdf;
using UnityEngine;
using UrdfToolkit.Srdf;
namespace SimToolkit.ROS.Moveit
{
public class MoveGroupState
{
    public TrajectoryForwardKinematic currentTrajectory;
    public TrajectoryStatus status;
    public bool isComplete;
    public bool isComputingIK;
    public bool hasValidPose;

    public bool IsReady(out string feedback)
    {
        if (!hasValidPose)
        {
            feedback = "Invalid pose";
            return false;
        }

        if (!isComplete)
        {
            feedback = "Busy executing trajectory";
            return false;
        }

        if (isComputingIK)
        {
            feedback = "Busy computing IK";
            return false;
        }

        feedback = "Ready";
        return true;
    }

    public MoveGroupState()
    {
        this.currentTrajectory = new TrajectoryForwardKinematic();
        status = TrajectoryStatus.None;
        isComplete = true;
        isComputingIK = false;
        hasValidPose = false;
    }
}


public class MoveGroup
{
    public string name = "";
    public UrdfRobot robot;
    public UrdfJoint[] joints;
    public string[] jointNames;
    public SrdfGroup srdfGroup;

    public MoveGroup(string groupName, UrdfRobot robot)
    {
        name = groupName;
        this.robot = robot;
        srdfGroup = SrdfManager.GetGroup(name);
        joints = srdfGroup.GetJointsOnRobot(robot).ToArray();
        jointNames = joints.Select(j => j?.jointName ?? "unknown").ToArray();
    }

    public JointStateMsg FilterJoints(JointStateMsg jointState)
    {
        if (jointState == null) return null;
        var keep = new List<(string, double)>();
        for (int i = 0; i < jointState.name.Length; i++)
        {
            if (jointNames.Contains(jointState.name[i]))
            {
                keep.Add((jointState.name[i], jointState.position[i]));
            }
        }
        jointState = new JointStateMsg()
        {
            name = keep.Select(k => k.Item1).ToArray(),
            position = keep.Select(k => k.Item2).ToArray()
        };
        return jointState;
    }

    public MoveGroupController CreateController(MoveGroupControllerSettings settings)
    {
        return new MoveGroupController(this, settings);
    }

}

}