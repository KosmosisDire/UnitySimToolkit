using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Moveit;
using SimToolkit;
using UnityEngine;
namespace SimToolkit.ROS.Moveit
{
public class TrajectoryViewer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public string displayPathTopic = "move_group/display_planned_path";

    // Start is called before the first frame update
    async void Start()
    {
        var ros = await ROSManager.AwaitROS();
        ros.Subscribe<DisplayTrajectoryMsg>(displayPathTopic, DisplayPath);
    }

    private void DisplayPath(DisplayTrajectoryMsg msg)
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
        }

        lineRenderer.transform.position = Vector3.zero;

        var points = new List<Vector3>();
        foreach (var trajectory in msg.trajectory)
        {
            foreach (var point in trajectory.joint_trajectory.points)
            {
                points.Add(new Vector3((float)point.positions[0], (float)point.positions[1], (float)point.positions[2]));
            }
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
}