using System;
using System.Threading.Tasks;
using SimToolkit;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using Toolkit;

public class ROSConnector : MonoBehaviour
{
    public VisualTreeAsset menuAsset;

    [Header("Element Names")]
    public string rootName = "ros-container";
    public string ipInputName = "ipInput";
    public string portInputName = "portInput";
    public string connectButtonName = "connectButton";
    public string feedbackLabelName = "ipError";
    public string loadingIconName = "loadingIcon";

    // UI elements
    public static VisualElement rosRoot;
    public static TextField ipInput;
    public static IntegerField portInput;
    public static Button connectButton;
    public static Label feedbackLabel;
    public static VisualElement loadingIcon;

    [Space(30)]
    public UnityEvent onSuccessfulConnection;
    [Space(30)]
    public UnityEvent onDisconnected;

    private static bool _isConnected;
    public static bool IsConnected => _isConnected && !rosConnection.HasConnectionError;
    public static ROSConnection rosConnection;
    public static ROSConnector Instance { get; private set; }

    public static async Task<ROSConnection> AwaitConnection()
    {
        await Awaitable.NextFrameAsync();

        while (Instance == null || (!IsConnected && Application.isPlaying))
        {
            await Awaitable.NextFrameAsync();
        }

        await Awaitable.NextFrameAsync();

        return ROSConnection.GetOrCreateInstance();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _isConnected = false;
        rosConnection = null;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple ROSIPConnector instances found. Destroying this one.");
            Destroy(gameObject);
        }

        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.transform.SetParent(transform.parent);
        rosConnection.ShowHud = false;
        rosConnection.listenForTFMessages = false;

        rosRoot = menuAsset.CloneTreeEl(UIManager.rootElement);

        ipInput = rosRoot.Q<TextField>(ipInputName);
        portInput = rosRoot.Q<IntegerField>(portInputName);
        feedbackLabel = rosRoot.Q<Label>(feedbackLabelName);
        connectButton = rosRoot.Q<Button>(connectButtonName);
        loadingIcon = rosRoot.Q(loadingIconName);

        if (ipInput == null) Debug.LogError("ipInput not found");
        if (portInput == null) Debug.LogError("portInput not found");
        if (connectButton == null) Debug.LogError("connectButton not found");

        if (loadingIcon != null) loadingIcon.style.display = DisplayStyle.None;

        var savedIP = PlayerPrefs.GetString("ros_ip");
        var savedPort = PlayerPrefs.GetString("ros_port");

        if (!string.IsNullOrEmpty(savedIP))
        {
            ipInput.value = savedIP;
        }

        if (!string.IsNullOrEmpty(savedPort))
        {
            portInput.value = int.Parse(savedPort);
        }

        if (feedbackLabel != null)
        {
            ipInput?.RegisterValueChangedCallback(async (evt) => 
            {
                feedbackLabel.text = "";
                var ip = evt.newValue;
                if (ROSConnection.IPFormatIsCorrect(ip))
                {
                    await Ping(ip, 1000, (success) => 
                    {
                        feedbackLabel.text = success ? "" : "Device with this IP not found";
                    });

                    if (ip == "localhost" || ip == "127.0.0.1")
                    {
                        feedbackLabel.text = "";
                        return;
                    }
                }
                else
                {
                    feedbackLabel.text = "Invalid IP Address";
                }
            });
        
            portInput?.RegisterValueChangedCallback(evt => 
            {
                feedbackLabel.text = "";
                var port = evt.newValue;
                if (port < 0 || port > 65535)
                {
                    feedbackLabel.text = "Invalid Port";
                }
            });
        }

        connectButton.clicked += () =>
        {
            var ip = ipInput.value;
            var port = portInput.value;

            if (port < 0 || port > 65535)
            {
                if (feedbackLabel != null) feedbackLabel.text = "Invalid Port";
                return;
            }


            ROSConnect(ip, port, (success) => 
            {
                if (feedbackLabel != null) feedbackLabel.text = success ? "" : "Connection Error";
                if (success)
                {
                    if (feedbackLabel != null) feedbackLabel.text = "Connected Successfully!";
                    if (feedbackLabel != null) feedbackLabel.style.color = new StyleColor(new Color(0.31f, 0.79f, 0.69f));
                    PlayerPrefs.SetString("ros_ip", ip);
                    PlayerPrefs.SetString("ros_port", port.ToString());
                    onSuccessfulConnection.Invoke();
                    SearchForConnectionError();
                }
            });
           
        };
    }

    async void SearchForConnectionError()
    {
        while (!rosConnection.HasConnectionError)
        {
            await Awaitable.WaitForSecondsAsync(1f);
        }

        onDisconnected.Invoke();
        _isConnected = false;
        rosConnection.Disconnect();
        UnitySceneManager.LoadScene(UnitySceneManager.GetActiveScene().buildIndex);
        Show();
        feedbackLabel.text = "ROS Disconnected";
        feedbackLabel.style.color = new StyleColor(new Color(0.79f, 0.31f, 0.31f));
    }

    void OnApplicationQuit()
    {
        rosConnection = null;
        Instance = null;
        _isConnected = false;
        rosRoot = null;
        ipInput = null;
        portInput = null;
        connectButton = null;
        feedbackLabel = null;
        loadingIcon = null;
    }

    async Task Ping(string ip, int timeoutMs, Action<bool> callback)
    {
        var timeout = timeoutMs;
        var now = DateTime.Now;

        var ping = new Ping(ip);
        while (!ping.isDone && (DateTime.Now - now).TotalMilliseconds < timeout)
        {
            await Awaitable.NextFrameAsync();
        }

        callback(ping.isDone);
    }

    async void ROSConnect(string ip, int port, Action<bool> callback, int timeoutMs = 500)
    {
        if (loadingIcon != null) loadingIcon.style.display = DisplayStyle.Flex;
        rosConnection.Connect(ip, port);
        var timeout = timeoutMs;
        var now = DateTime.Now;

        int angle = 0;
        while (((DateTime.Now - now).TotalMilliseconds < timeout) || (IsConnected && !ROSManager.IsReady))
        {
            await Awaitable.NextFrameAsync();
            if (loadingIcon != null) loadingIcon.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(angle)));
            angle += 2;
            _isConnected = !rosConnection.HasConnectionError;
        }

        if (loadingIcon != null) loadingIcon.style.display = DisplayStyle.None;
        callback(!rosConnection.HasConnectionError);
        if (rosConnection.HasConnectionError)
        {
            rosConnection.Disconnect();
        }
    }

    public async void Hide()
    {
        rosRoot.style.opacity = 0;
        await Awaitable.WaitForSecondsAsync(1f);
        rosRoot.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        rosRoot.style.display = DisplayStyle.Flex;
        rosRoot.style.opacity = 1;
    }

}
