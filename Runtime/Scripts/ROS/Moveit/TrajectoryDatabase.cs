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

    public float pointRadius = 0.01f;
    public Color lineColor = Color.blue;
    public Color pointColor = Color.green;

    private bool pointSelected = false;
    private TransformGizmo gizmo;


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
        instance.currentTrajectory = new TrajectoryInverseKinematic(name)
        {
            velAcc = new float[] { 1, 1 } // velocity scale, acceleration scale
        };
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
            if (trajectory.velAcc is not float[])
            {
                trajectory.velAcc = new float[] { 1, 1 };
            }
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

        if (pointSelected && gizmo && !gizmo.IsHovered && Input.GetMouseButtonUp(0))
        {
            pointSelected = false;
            Destroy(gizmo.gameObject);
        }

        for (int i = 0; i < currentTrajectory.positions.Count; i++)
        {
            Draw.LinePixel(currentTrajectory.positions[i], currentTrajectory.positions[i] + currentTrajectory.rotations[i] * Vector3.right * pointRadius * 5, Color.red);
            Draw.LinePixel(currentTrajectory.positions[i], currentTrajectory.positions[i] + currentTrajectory.rotations[i] * Vector3.up * pointRadius * 5, Color.green);
            Draw.LinePixel(currentTrajectory.positions[i], currentTrajectory.positions[i] + currentTrajectory.rotations[i] * Vector3.forward * pointRadius * 5, Color.blue);
            
            if (i > 0)
            {
                Draw.LinePixel(currentTrajectory.positions[i - 1], currentTrajectory.positions[i], lineColor);
            }

            var drawPointColor = pointColor;

            var ray = Math3d.MouseRay();
            if (Math3d.RayIntersectsSphere(ray.origin, ray.direction, currentTrajectory.positions[i], pointRadius, out _))
            {
                if (gizmo && gizmo.IsHovered) continue;
                
                if (Input.GetMouseButtonUp(2))
                {
                    currentTrajectory.positions.RemoveAt(i);
                    currentTrajectory.rotations.RemoveAt(i);
                    SaveTrajectory();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    pointSelected = true;

                    gizmo = TransformGizmo.Create(null);
                    gizmo.SetPosition(currentTrajectory.positions[i]);
                    gizmo.SetRotation(currentTrajectory.rotations[i]);

                    var localIndex = i;
                    gizmo.OnMove += (pos, rot) =>
                    {
                        currentTrajectory.positions[localIndex] = pos;
                        SaveTrajectory();
                    };

                    gizmo.OnRotate += (pos, rot) =>
                    {
                        currentTrajectory.rotations[localIndex] = rot;
                        SaveTrajectory();
                    };

                    break;
                }

                drawPointColor = Color.yellow;
                
            }

            Draw.Sphere(currentTrajectory.positions[i], pointRadius, drawPointColor, true);
        }

        
    }

    public static TrajectoryInverseKinematic? GetTrajectory(string name)
    {
        return instance.allTrajectories.Find(t => t.name == name);
    }
}
}