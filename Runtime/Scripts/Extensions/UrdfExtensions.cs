
using System.Collections.Generic;
using SimToolkit.ROS.Urdf;
using UrdfToolkit.Srdf;

namespace SimToolkit
{

    public static class UrdfExtensions
    {
        public static List<UrdfJoint> GetJointsOnRobot(this SrdfGroup group, UrdfRobot robot)
        {
            var urdfJoints = new List<UrdfJoint>();
            foreach (var joint in group.FullJointList)
            {
                urdfJoints.Add(robot.GetJoint(joint));
            }
            return urdfJoints;
        }
    }

}