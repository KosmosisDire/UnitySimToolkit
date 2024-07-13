using System.Collections.Generic;
using System.Threading.Tasks;
using SimToolkit.ROS.Srdf;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace SimToolkit
{

public class ROSManager : MonoBehaviour
{
    [Header("General")]
    public bool keepOnLoad = true;

    [Header("Enable Features")]
    public bool useParamServer = true;
    public bool useFileServer = true;
    public bool usePlanningScene = true;
    public bool useTFManager = true;
    public bool useSrdfManager = true;

    [Space(10)]

    [Header("Transform Options")]
    public List<string> TFTopics = new List<string> { "/tf" };
    
    [Tooltip("The name of the world frame to use for TF operations. Leave blank to auto-detect.")]
    [SerializeField] public string worldFrame = "";




    public static ROSManager Instance { get; private set; }
    public static ROSConnection ros;
    private static bool initialized = false;
    private static bool loaded = false;
    public static bool Loaded => loaded;

    public static List<RosFeatureManager> Features { get; private set; } = new List<RosFeatureManager>();
    public static bool IsReady => loaded;


    async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        ros = await ROSConnector.AwaitConnection();
        await AddFeature<ROSNotifier>();
        
        if (useParamServer) await AddFeature<ParamServer>();
        if (useFileServer) await AddFeature<FileServer>();
        if (useTFManager) await AddFeature<TFManager>();
        if (usePlanningScene) await AddFeature<PlanningSceneManager>();
        if (useSrdfManager) await AddFeature<SrdfManager>();

        initialized = true;

        await AwaitROS();

        foreach (var feature in Features)
        {
            _ = feature.PostInit();
        }
    }

    public static async Task<ROSConnection> AwaitROS()
    {
        if (loaded) return ros;

        while (!initialized)
        {
            await Awaitable.NextFrameAsync();
        }
        
        await ROSConnector.AwaitConnection();

        foreach (var feature in Features)
        {
            await feature.Await();
        }

        loaded = true;
        return ros;
    }
    

    void Update()
    {
        if (!IsReady) return;
        worldFrame = TFManager.WorldFrameName;
    }

    public async Task AddFeature<T>() where T : RosFeatureSingleton<T>, new()
    {
        await ROSConnector.AwaitConnection();
        Features.Add(await new T().RunInit());
    }

    void OnApplicationQuit()
    {
        if (ROSConnector.rosConnection) ROSConnector.rosConnection.Disconnect();
        Features = new List<RosFeatureManager>();
        loaded = false;
        initialized = false;
    }
}

}