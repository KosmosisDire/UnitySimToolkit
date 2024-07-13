using System.Threading.Tasks;
using RosMessageTypes.Std;
using Toolkit;

public class ROSNotifier : RosFeatureSingleton<ROSNotifier>
{
    public string topicName = "/unity_notify";

    protected override Task Init()
    {
        ROS.Subscribe<StringMsg>(topicName, (msg) => 
        {
            NotificationManager.Notice(msg.data);
        });
        return base.Init();
    }
}