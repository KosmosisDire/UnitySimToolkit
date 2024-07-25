using System.Collections.Generic;
using System.Threading.Tasks;
using SimToolkit.ROS.Moveit;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PathEvent
{
    public string pathName;
    public string groupName;
    public UnityEvent onBegin;
    public UnityEvent onComplete;
}

public class TrajectorySequencer : MonoBehaviour
{
    public List<PathEvent> pathEvents;
    public MoveitRobot robot;

    public void Play()
    {
        _ = PlayAsync();
    }

    public async Task PlayAsync()
    {
        foreach (var pathEvent in pathEvents)
        {
            var group = robot.GetGroup(pathEvent.groupName);
            if (group == null)
            {
                Debug.LogError($"Group {pathEvent.groupName} not found");
                continue;
            }

            pathEvent.onBegin.Invoke();
            await group.ExecuteNamedTrajectory(pathEvent.pathName);
        }
    }

}
