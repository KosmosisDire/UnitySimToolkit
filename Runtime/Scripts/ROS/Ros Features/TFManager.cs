using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RosMessageTypes.Geometry;
using SimToolkit;
using UnityEngine;

public class TFManager : RosFeatureSingleton<TFManager>
{
    private TFSystem tfSystem;
    List<string> transformNames = new List<string>();
    string worldFrameName = "world";

    public static List<string> TFTopics 
    {
        get
        {
            return ROSManager.Instance.TFTopics;
        }
        set
        {
            ROSManager.Instance.TFTopics = value;
            ROS.TFTopics = value.ToArray();
        }
    }
    
    public static TFStream WorldFrame
    {
        get
        {
            return GetTransformStream(Instance.worldFrameName);
        }
    }

    public static TFStream GetTransformStream(string frame)
    {
        var topicsCopy = TFTopics;
        foreach (var topic in topicsCopy)
        {
            if (Instance.tfSystem.GetTransformNames(topic).Contains(frame))
            {
                return Instance.tfSystem.GetTransformStream(frame, topic);
            }
        }

        return null;
    }

    public static string WorldFrameName
    {
        get
        {
            return Instance.worldFrameName;
        }
        set
        {
            Instance.worldFrameName = value;
        }
    }
    

    protected override async Task Init()
    {
        Instance.tfSystem = null;
        Debug.Log(TFTopics);
        TFTopics = TFTopics;
        ROS.listenForTFMessages = true;
        Instance.tfSystem = TFSystem.GetOrCreateInstance();
        Instance.worldFrameName = ROSManager.Instance.worldFrame;

        while (!Instance.tfSystem.GetTransformNames(TFTopics[0]).Any())
        {
            await Awaitable.WaitForSecondsAsync(0.33f);
            Debug.Log("Waiting for TF topics to be published...");
        }

        if (string.IsNullOrEmpty(Instance.worldFrameName))
        {
            var frameName = AutoDetectWorldFrame();
            if (!string.IsNullOrEmpty(frameName))
            {
                Instance.worldFrameName = frameName;
            }
        }
    }

    public async override Task PostInit()
    {
        await base.PostInit();
        UpdateTFViewLoop();
    }

    public static string AutoDetectWorldFrame()
    {
        var topicsCopy = TFTopics;

        foreach (var topic in topicsCopy)
        {
            var transforms = Instance.tfSystem.GetTransformNames(topic);
            foreach (var transform in transforms)
            {
                if (transform == "world" || transform == "map")
                {
                    return transform;
                }
            }

            foreach (var transform in transforms)
            {
                if (transform.Contains("world") || transform.Contains("map"))
                {
                    return transform;
                }
            }

            foreach (var transform in transforms)
            {
                if (transform == "odom" || transform == "base_link")
                {
                    return transform;
                }
            }

            foreach (var transform in transforms)
            {
                if (transform.Contains("odom") || transform.Contains("base_link"))
                {
                    return transform;
                }
            }
        }

        return Instance.tfSystem.GetTransformNames(topicsCopy[0]).FirstOrDefault();
    }

    public static async void UpdateTFViewLoop()
    {
        while (Application.isPlaying)
        {
            foreach (var topic in TFTopics)
            {
                var transforms = Instance.tfSystem.GetTransformNames(topic);
                foreach (var transform in transforms)
                {
                    if (!Instance.transformNames.Contains(transform))
                    {
                        Instance.transformNames.Add(transform);
                    }
                }
            }

            await Awaitable.WaitForSecondsAsync(1f);
        }
    }

    public static Vector3 TransformUnityWorldToROSWorld(Vector3 point)
    {
        return WorldFrame.GetWorldTF().TransformPoint(point);
    }

    public static Vector3 TransformROSWorldToUnityWorld(Vector3 point)
    {
        return WorldFrame.GetWorldTF().InverseTransformPoint(point);
    }

    public static PoseMsg TransformFrom(PoseMsg msg, string fromFrame)
    {
        return new PoseMsg
        {
            position = TransformFrom(msg.position, fromFrame),
            orientation = TransformFrom(msg.orientation, fromFrame)
        };
    }
   
    public static PointMsg TransformFrom(PointMsg msg, string fromFrame)
    {
        var tf = GetTransformStream(fromFrame);
        if (tf == null)
        {
            return msg;
        }

        var worldTF = WorldFrame.GetWorldTF();
        return worldTF.TransformPoint(msg.ToVector3().Ros2Unity()).Unity2Ros().ToPointMsg();
    }

    public static QuaternionMsg TransformFrom(QuaternionMsg msg, string fromFrame)
    {
        var tf = GetTransformStream(fromFrame);
        if (tf == null)
        {
            return msg;
        }

        var worldTF = WorldFrame.GetWorldTF();
        return (msg.ToQuaternion().Ros2Unity() * worldTF.rotation).Unity2Ros().ToQuaternionMsg();
    }

    public static PoseMsg TransformTo(PoseMsg msg, string toFrame)
    {
        return new PoseMsg
        {
            position = TransformTo(msg.position, toFrame),
            orientation = TransformTo(msg.orientation, toFrame)
        };
    }

    public static PointMsg TransformTo(PointMsg msg, string toFrame)
    {
        var tf = GetTransformStream(toFrame);
        if (tf == null)
        {
            return msg;
        }

        var worldTF = WorldFrame.GetWorldTF();
        return worldTF.InverseTransformPoint(msg.ToVector3().Ros2Unity()).Unity2Ros().ToPointMsg();
    }

    public static QuaternionMsg TransformTo(QuaternionMsg msg, string toFrame)
    {
        var tf = GetTransformStream(toFrame);
        if (tf == null)
        {
            return msg;
        }

        var worldTF = WorldFrame.GetWorldTF();
        return (msg.ToQuaternion().Ros2Unity() * Quaternion.Inverse(worldTF.rotation)).Unity2Ros().ToQuaternionMsg();
    }
}
