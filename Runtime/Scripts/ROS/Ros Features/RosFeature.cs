using System.Threading.Tasks;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public abstract class RosFeatureManager
{
    public bool initialized = false;
    public bool enabled = false;
    private static ROSConnection ros;
    public static ROSConnection ROS
    {
        get
        {
            if (ros == null)
            {
                throw new System.Exception("Do not use RosFeature before it is initialized");
            }
            return ros;
        }
        protected set
        {
            ros = value;
        }
    }

    /// <summary>
    /// Initialize the object. Do not override or call this method directly.
    /// <br/>
    /// This is called automatically when the feature is added to the ROSManager
    internal abstract Task<RosFeatureManager> RunInit();

    /// <summary>
    /// Override this method to add initialization code. This is called after ros is connected when the feature is created.
    /// </summary>
    protected abstract Task Init();

    /// <summary>
    /// Override this method to add start code. This is called after all features are initialized.
    /// </summary>
    public abstract Task PostInit();

    /// <summary>
    /// Wait for the feature to be ready.
    /// </summary>
    /// <returns></returns>
    public async Task Await() 
    {
        if (!enabled) return;
        while (!initialized)
        {
            await Awaitable.NextFrameAsync();
        }
    }
}

public class RosFeatureSingleton<T> : RosFeatureManager where T : RosFeatureSingleton<T>, new()
{
    public static bool Initialized => instance?.initialized ?? false;
    private static T instance;
    public static T Instance 
    {
        get
        {
            if (!instance.enabled) instance = new T();

            if (instance == null)
            {
                throw new System.Exception("Do not use RosFeature before it is initialized");
            }
            return instance;
        }
        private set
        {
            instance = value;
        }
    }

    internal override async Task<RosFeatureManager> RunInit()
    {
        if (initialized) return this;
        enabled = true;
        ROS = await ROSConnector.AwaitConnection();
        Instance = this as T;
        await Init();
        initialized = true;
        Debug.Log("Initialized " + this.GetType().Name);
        return this;
    }

    public new static async Task<RosFeatureManager> Await()
    {
        if (!instance.enabled) return null;
        while (!instance.initialized)
        {
            await Awaitable.NextFrameAsync();
        }
        return instance;
    }

    protected override async Task Init() => await Task.CompletedTask;

    public override async Task PostInit() => await Task.CompletedTask;
}