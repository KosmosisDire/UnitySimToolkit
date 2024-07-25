using RosMessageTypes.Moveit;
namespace SimToolkit.ROS.Moveit
{
public class TrajectoryViz : RosVisualizer<DisplayTrajectoryMsg>
{
    public string displayPathTopic = "move_group/display_planned_path";

    // Start is called before the first frame update
    async void Start()
    {
        var ros = await ROSManager.AwaitROS();
        ros.Subscribe<DisplayTrajectoryMsg>(displayPathTopic, DisplayPath);
    }

    private void DisplayPath(DisplayTrajectoryMsg msg)
    {
        throw new System.NotImplementedException();
    }
}
}