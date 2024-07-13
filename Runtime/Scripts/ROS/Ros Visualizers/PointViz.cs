using RosMessageTypes.Geometry;
using SimToolkit;
using Toolkit.Visualization;
using UnityEngine;

public class PointViz : RosVisualizer<PointStampedMsg>
{
    public float size = 0.1f;
    public Color color = Color.red;

    public override PointStampedMsg Transform(PointStampedMsg msg)
    {
        msg.point = TFManager.TransformFrom(msg.point, msg.header.frame_id);
        return msg;
    }

    public override void VisualizeImmediate(PointStampedMsg msg)
    {
        var position = msg.point.ToVector3().Ros2Unity();
        Draw.Sphere(position, size / 2, color, true);
    }
}
