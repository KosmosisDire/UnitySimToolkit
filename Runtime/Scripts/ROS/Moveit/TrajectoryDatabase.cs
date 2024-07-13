using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolkit;
using UnityEngine;

namespace SimToolkit.ROS.Moveit
{
public class TrajectoryDatabase : MonoBehaviour
{
    public string folderName = "Paths";
    private string savePath => Path.Combine(Application.persistentDataPath, folderName + Path.DirectorySeparatorChar);
    public static TrajectoryDatabase instance;
    public static TrajectoryInverseKinematic currentTrajectory;
    public static List<TrajectoryInverseKinematic> allTrajectories = new List<TrajectoryInverseKinematic>();

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

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
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
        currentTrajectory = new TrajectoryInverseKinematic(name);
    }

    public static void SaveTrajectory()
    {
        var json = JsonUtility.ToJson(currentTrajectory);
        var name = currentTrajectory.name;
        if (string.IsNullOrEmpty(name))
        {
            name = "Unnamed";
        }
        var path = Path.Combine(instance.savePath, name + ".json");
        NotificationManager.Notice("Saving trajectory as: " + name);
        File.WriteAllText(path, json);

        if (!allTrajectories.Contains(currentTrajectory))
        {
            allTrajectories.Add(currentTrajectory);
        }
        else
        {
            var index = allTrajectories.FindIndex(x => x.name == name);
            allTrajectories[index] = currentTrajectory;
        }
        
    }

    public static void LoadTrajectory(string id)
    {
        Debug.Log("Loading trajectory");
        var json = File.ReadAllText(Path.Combine(instance.savePath, id + ".json"));
        currentTrajectory = JsonUtility.FromJson<TrajectoryInverseKinematic>(json);
    }

    public static void LoadAllTrajectories()
    {
        Debug.Log("Loading all trajectories");
        allTrajectories.Clear();
        var files = Directory.GetFiles(instance.savePath);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var trajectory = JsonUtility.FromJson<TrajectoryInverseKinematic>(json);
            allTrajectories.Add(trajectory);
            NotificationManager.Notice("Loaded Trajectory: " + trajectory.name);
        }

        currentTrajectory = allTrajectories.FirstOrDefault();
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

        currentTrajectory.positions.Add(group.gizmo.transform.position);
        currentTrajectory.rotations.Add(group.gizmo.Rotation);

        NotificationManager.Notice("Added pose to path: " + currentTrajectory.name);
    }

    void OnDrawGizmos()
    {
        if (currentTrajectory.positions == null) return;
        for (int i = 0; i < currentTrajectory.positions.Count; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentTrajectory.positions[i], 0.01f);
            
            if (i > 0)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(currentTrajectory.positions[i - 1], currentTrajectory.positions[i]);
            }
        }
    }

    public static TrajectoryInverseKinematic? GetTrajectory(string name)
    {
        return allTrajectories.Find(t => t.name == name);
    }
}
}