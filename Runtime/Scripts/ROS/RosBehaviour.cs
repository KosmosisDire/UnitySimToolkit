using System.Threading.Tasks;
using SimToolkit;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public abstract class RosBehaviour : MonoBehaviour
{
    public ROSConnection ROS { get; private set; }
    public bool IsReady => ROSManager.IsReady && IsInitialized;
    public bool IsInitialized { get; private set; } = false;

    protected virtual async void Start()
    {
        Debug.Log($"Starting {GetType().Name}");
        await BeforeInit();
        ROS = await ROSManager.AwaitROS();
        await Init();
        IsInitialized = true;
        await OnReady();
    }

    public virtual Task BeforeInit() => Task.CompletedTask;
    public virtual Task Init() => Task.CompletedTask;
    public virtual Task OnReady() => Task.CompletedTask;

    public async Task<RosBehaviour> AwaitReady()
    {
        if (IsReady) return this;
        while (!IsReady)
        {
            await Awaitable.NextFrameAsync();
        }
        
        return this;
    }


}