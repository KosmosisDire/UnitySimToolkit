using System.Collections.Generic;
using System.Threading.Tasks;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

public abstract class RosVisualizer<T> : RosBehaviour where T : Message
{
    public List<string> topicNames;
    public Dictionary<string, List<T>> LastValues { get; private set; } = new();
    public int historyLength = 1;
    protected bool useGizmos = false;

    public override Task Init()
    {
        topicNames.ForEach(topic =>
        {
            ROS.Subscribe<T>(topic, (msg) => MsgCallback(msg, topic));
        });

        return Task.CompletedTask;
    }

    private void MsgCallback(T msg, string topicName)
    {
        msg = Transform(msg);

        if (!LastValues.ContainsKey(topicName))
        {
            LastValues.Add(topicName, new List<T>());
        }

        LastValues[topicName].Add(msg);

        var overflow = LastValues[topicName].Count - historyLength;
        if (overflow > 0)
        {
            LastValues[topicName].RemoveRange(0, overflow);
        }
        
        VisualizeOnce(msg);
    }

    public void ForLastValues(System.Action<T, string> action)
    {
        foreach (var topic in LastValues.Keys)
        {
            foreach (var msg in LastValues[topic])
            {
                action(msg, topic);
            }
        }
    }

    void Update()
    {
        if (useGizmos) return;
        ForLastValues((msg, topic) =>
        {
            VisualizeImmediate(msg);
        });
    }   

    void OnDrawGizmos()
    {
        if (!useGizmos) return;
        ForLastValues((msg, topic) =>
        {
            VisualizeImmediate(msg);
        });
    }

    public virtual void VisualizeOnce(T msg) { }
    public virtual void VisualizeImmediate(T msg) { }
    public virtual T Transform(T msg) {  return msg; }
}


public abstract class RosVisualizerGizmos<T> : RosVisualizer<T> where T : Message
{
    public override Task Init()
    {
        useGizmos = true;
        return base.Init();
    }
}