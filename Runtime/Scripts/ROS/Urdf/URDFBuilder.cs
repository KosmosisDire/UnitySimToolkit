using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimToolkit.ROS.Urdf;
using SimToolkit.ROS.Urdf.Importer;
using UrdfToolkit.Urdf;
using UnityEngine;
using Toolkit;
using SimToolkit;

public class URDFBuilder : MonoBehaviour
{
    public static async Task<UrdfRobot> Build(string urdfPath)
    {
        if (!File.Exists(urdfPath))
        {
            Debug.LogError($"URDF file not found at {urdfPath}");
            return null;
        }

        var urdfText = File.ReadAllText(urdfPath);

        return await BuildRuntime(urdfText, null, urdfPath);
    }

    public static async Task<UrdfRobot> BuildRuntime(string robotDescription, GameObject buildOnObject = null, string urdfPath = null)
    {
        var pathUndefined = string.IsNullOrEmpty(urdfPath);
        if (pathUndefined && FileServer.Initialized)
        {
            urdfPath = Path.Combine(Application.persistentDataPath, "temp_urdf_meshes");
            Debug.Log(urdfPath);
            Debug.Log(Application.persistentDataPath);
            if (!Directory.Exists(urdfPath))
            {
                Directory.CreateDirectory(urdfPath);
            }
        }

        var urdfDescription = new UrdfDescription(robotDescription, urdfPath);

        // download meshes if no path was given in which to find them
        if (pathUndefined)
        {
            for (var i = 0; i < urdfDescription.meshes.Count; i++)
            {
                var mesh = urdfDescription.meshes[i];
                var fileStat = await FileServer.GetFileStat(mesh.filename);
                if (!fileStat.exists) 
                {
                    Debug.LogError($"Mesh file {mesh.filename} not found on server");
                    continue;
                }

                // if the file is already downloaded and up to date, skip it
                var savePath = Path.Combine(urdfPath, mesh.filename.Replace("package://", ""));
                var fileInfo = new FileInfo(savePath);
                var localModTime = fileInfo.LastWriteTimeUtc.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
                if (fileInfo.Exists && localModTime >= fileStat.modifiedTimeMs)
                {
                    Debug.Log($"Mesh file {mesh.filename} already up to date");
                    continue;
                }


                var fileData = await FileServer.GetFile(mesh.filename);
                if (fileData != null)
                {
                    // create directory if it doesn't exist
                    var directory = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllBytes(savePath, fileData);
                }
            }
        }

        if (buildOnObject == null)
        {
            buildOnObject = new GameObject(urdfDescription.name);
        }
        var robot = buildOnObject.GetComponent<UrdfRobot>() ?? buildOnObject.AddComponent<UrdfRobot>();
        robot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var tree = BuildRecursive(urdfDescription.links.Values.First(), urdfDescription, null);
        tree.transform.SetParent(robot.transform);
        
        // add joint mirror
        if (robot.GetComponent<URDFJointMirror>() == null)
            robot.gameObject.AddComponent<URDFJointMirror>();

        robot.PopulateJoints();
        robot.PopulateLinks();

        // make root articulation bodies immovable
        foreach (var body in robot.GetComponentsInChildren<ArticulationBody>())
        {
            if (body.transform.parent.GetComponent<ArticulationBody>() == null)
            {
                body.immovable = true;
            }
        }

        return robot;
    }

    public static UrdfLink BuildRecursive(UrdfLinkDef linkData, UrdfDescription urdf, UrdfLink? parent)
    {
        var link = BuildLink(linkData, urdf, parent);
        parent = link;

        var children = urdf.GetChildrenForLink(linkData.name);
        foreach (var child in children)
        {
            BuildRecursive(child, urdf, parent);
        }

        return link;
    }

    private static UrdfLink BuildLink(UrdfLinkDef linkData, UrdfDescription? urdf, UrdfLink? parent)
    {
        var link = new GameObject(linkData.name).AddComponent<UrdfLink>();
        
        link.transform.SetParent(parent?.transform ?? null);
        
        if (linkData.visual.HasValue)
        {
            var visuals = new GameObject("visual").transform;
            visuals.SetParent(link.transform);
            visuals.localPosition = linkData.visual?.origin?.xyzRUF.ToUnity() ?? Vector3.zero;
            visuals.localRotation = linkData.visual?.origin?.rotationRUF.ToUnity() ?? Quaternion.identity;
            visuals.localScale = linkData.visual?.geometry.box?.size.ToUnity() ?? Vector3.one;

            var geomData = linkData.visual.Value.geometry;
            UrdfVisualMeshImporter.Create(visuals, geomData);
        }
        
        if (linkData.collision.HasValue)
        {
            var colliders = new GameObject("collision").transform;
            colliders.SetParent(link.transform);
            colliders.localPosition = linkData.collision?.origin?.xyzRUF.ToUnity() ?? Vector3.zero;
            colliders.localRotation = linkData.collision?.origin?.rotationRUF.ToUnity() ?? Quaternion.identity;
            colliders.localScale = linkData.collision?.geometry.box?.size.ToUnity() ?? Vector3.one;

            var geomData = linkData.collision.Value.geometry;
            UrdfCollisionMeshImporter.Create(colliders, geomData);
        }
        
        var jointData = urdf?.GetJointForLink(linkData.name);
        if (jointData.HasValue)
        {   
            // the root link should have an articulation body even if it has no joint
            if (parent.joint == null)
            {
                parent.gameObject.AddComponent<ArticulationBody>();
            }

            link.transform.SetLocalPositionAndRotation
            (
                jointData?.origin?.xyzRUF.ToUnity() ?? Vector3.zero,
                jointData?.origin?.rotationRUF.ToUnity() ?? Quaternion.identity
            );

            var joint = UrdfJoint.Create(link.gameObject, jointData.Value);
            link.joint = joint;
            joint.parentLink = parent;
            joint.childLink = link;
        }

        if (parent != null) parent.childLinks.Add(link);

        return link;
    }

}
