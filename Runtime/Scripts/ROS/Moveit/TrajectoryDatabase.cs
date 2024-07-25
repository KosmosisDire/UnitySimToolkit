using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toolkit;
using Toolkit.Visualization;
using UnityEngine;

namespace SimToolkit.ROS.Moveit
{
public class TrajectoryDatabase : MonoBehaviour
{
    public string folderName = "Paths";
    private string SavePath => Path.Combine(Application.persistentDataPath, folderName + Path.DirectorySeparatorChar);
    public TrajectoryInverseKinematic currentTrajectory;
    public List<TrajectoryInverseKinematic> allTrajectories = new();
    public static TrajectoryDatabase instance;
    private bool loadedFromDisk = false;

    public static async Task AwaitLoad()
    {
        while (instance == null || !instance.loadedFromDisk)
        {
            await Awaitable.NextFrameAsync();
        }
    }

    public static TrajectoryInverseKinematic CurrentTrajectory
    {
        get => instance.currentTrajectory;
        set => instance.currentTrajectory = value;
    }

    public static List<TrajectoryInverseKinematic> AllTrajectories => instance.allTrajectories;

    void Start()
    {
        currentTrajectory = new TrajectoryInverseKinematic();
        allTrajectories.Clear();
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple Trajectory Databases detected. Destroying this one.");
            Destroy(this);
        }

        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }

        LoadAllTrajectories();
    }

    void OnApplicationQuit()
    {
        currentTrajectory = new TrajectoryInverseKinematic();
        allTrajectories.Clear();
    }

    public static void BeginTrajectory(string name)
    {
        Debug.Log("Begin trajectory");
        instance.currentTrajectory = new TrajectoryInverseKinematic(name);
    }

    public static void SaveTrajectory()
    {
        var json = JsonUtility.ToJson(instance.currentTrajectory);
        var name = instance.currentTrajectory.name;
        if (string.IsNullOrEmpty(name))
        {
            name = "Unnamed";
        }
        var path = Path.Combine(instance.SavePath, name + ".json");
        NotificationManager.Notice("Saving trajectory as: " + name);
        Debug.Log("Saving trajectory to: " + path);
        File.WriteAllText(path, json);

        if (!instance.allTrajectories.Contains(instance.currentTrajectory))
        {
            instance.allTrajectories.Add(instance.currentTrajectory);
        }
        else
        {
            var index = instance.allTrajectories.FindIndex(x => x.name == name);
            instance.allTrajectories[index] = instance.currentTrajectory;
        }
        
    }

    public static void DeleteTrajectory(string name)
    {
        var path = Path.Combine(instance.SavePath, name + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
            instance.allTrajectories.RemoveAll(t => t.name == name);
            if (instance.currentTrajectory.name == name)
            {
                instance.currentTrajectory = instance.allTrajectories.FirstOrDefault();
            }
            NotificationManager.Notice("Deleted trajectory: " + name);
        }
    }

    private static void LoadAllTrajectories()
    {
        Debug.Log("Loading all trajectories");
        instance.allTrajectories.Clear();
        var files = Directory.GetFiles(instance.SavePath);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var trajectory = JsonUtility.FromJson<TrajectoryInverseKinematic>(json);
            instance.allTrajectories.Add(trajectory);
            NotificationManager.Notice("Loaded Trajectory: " + trajectory.name);
        }

        instance.currentTrajectory = instance.allTrajectories.FirstOrDefault();
        instance.loadedFromDisk = true;
    }

    public static void SavePose(MoveGroupController group)
    {
        if (!group.state.isComplete) return;
        if (!group.state.hasValidPose)
        {
            Debug.LogError("Invalid pose for " + group.Name);
            NotificationManager.Notice("Invalid pose for " + group.Name);
            return;
        }

        var jointMirror = group.planningBaseLink.gameObject.FindInParents<URDFJointMirror>();
        if (jointMirror == null)
        {
            Debug.LogError("No URDFJointMirror found for " + group.Name);
            return;
        }

        if (!jointMirror.localControl)
        {
            Debug.LogError("URDFJointMirror for " + group.Name + " is not set to local control");
            return;
        }

        if (jointMirror.JointStatesLocal == null || jointMirror.JointStatesLocal.name.Length == 0)
        {
            Debug.LogError("No joint states found for " + group.Name);
            return;
        }

        instance.currentTrajectory.positions.Add(group.gizmo.transform.position);
        instance.currentTrajectory.rotations.Add(group.gizmo.Rotation);

        NotificationManager.Notice("Added pose to path: " + instance.currentTrajectory.name);
        SaveTrajectory();
    }

    void Update()
    {
        if (currentTrajectory.positions == null) return;
        for (int i = 0; i < currentTrajectory.positions.Count; i++)
        {
            Draw.Sphere(currentTrajectory.positions[i], 0.01f, Color.green, true);
            
            if (i > 0)
            {
                Draw.LinePixel(currentTrajectory.positions[i - 1], currentTrajectory.positions[i], Color.blue);
            }
        }
    }

    public static TrajectoryInverseKinematic? GetTrajectory(string name)
    {
        return instance.allTrajectories.Find(t => t.name == name);
    }
}
}