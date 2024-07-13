using RosMessageTypes.Geometry;
using SimToolkit;
using Toolkit.Visualization;
using UnityEngine;

public class PoseViz : RosVisualizer<PoseStampedMsg>
{
    public float size = 0.1f;
    public Color color = Color.red;
    public bool showAxes = true;

    public override PoseStampedMsg Transform(PoseStampedMsg msg)
    {
        msg.pose = TFManager.TransformFrom(msg.pose, msg.header.frame_id);
        return msg;
    }

    public override void VisualizeImmediate(PoseStampedMsg msg)
    {
        var position = msg.pose.position.ToVector3().Ros2Unity();
        var rotation = msg.pose.orientation.ToQuaternion().Ros2Unity();
        DrawAxes(position, rotation);
    }

    private void DrawAxes(Vector3 position, Quaternion rotation)
    {
        Draw.LinePixel(position, position + rotation * Vector3.forward * size, Color.blue);
        Draw.LinePixel(position, position + rotation * Vector3.up * size, Color.green);
        Draw.LinePixel(position, position + rotation * Vector3.right * size, Color.red);
    }
}
