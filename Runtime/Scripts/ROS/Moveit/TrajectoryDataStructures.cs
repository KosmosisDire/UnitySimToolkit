using System.Collections.Generic;
using System.Threading.Tasks;
using RosMessageTypes.Sensor;
using UnityEngine;
namespace SimToolkit.ROS.Moveit
{
public struct TrajectoryInverseKinematic
{
    public string name;
    public List<Vector3> positions;
    public List<Quaternion> rotations;
    public JointStateMsg startingState;
    public float[] velAcc;

    public TrajectoryInverseKinematic(string name)
    {
        this.name = name;
        positions = new List<Vector3>();
        rotations = new List<Quaternion>();
        startingState = new JointStateMsg();
        velAcc = null;
    }

    public TrajectoryInverseKinematic(JointStateMsg startingState, IEnumerable<Vector3> positions, IEnumerable<Quaternion> rotations)
    {
        name = "Unnamed";
        this.startingState = startingState;
        this.positions = new List<Vector3>(positions);
        this.rotations = new List<Quaternion>(rotations);
        velAcc = null;
    }

    public readonly async Task<TrajectoryForwardKinematic?> ToForwardKinematic(MoveGroupController forGroup)
    {
        return await forGroup.GetFKTrajectory(this);
    }
}

public struct TrajectoryForwardKinematic
{
    public string id;
    public JointStateMsg[] jointStates;

    public TrajectoryForwardKinematic(JointStateMsg[] trajectory)
    {
        this.id = System.Guid.NewGuid().ToString() + "-Unity";
        this.jointStates = trajectory;
    }

    public static async Task<TrajectoryForwardKinematic?> FromInverseKinematic(TrajectoryInverseKinematic trajectory, MoveGroupController forGroup)
    {
        return await trajectory.ToForwardKinematic(forGroup);
    }
}
}