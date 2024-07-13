using System.IO;
using Toolkit;
using UnityEngine;
using UrdfToolkit.Urdf;

namespace SimToolkit.ROS.Urdf.Importer
{

public class BaseMeshImporter
{
    public static void SetScale(Transform transform, UrdfGeometryDef geometry)
    {
        var localScale = geometry.geometry.UnityScale.ToUnity();

        if (geometry.type == GeometryTypes.Mesh)
        {
            localScale = Vector3.Scale(transform.localScale, localScale);
        }

        transform.localScale = localScale;
    }

    public static GameObject CreateStlGameObjectRuntime(string stlFile, Transform parent = null)
    {
        Mesh[] meshes = StlImporter.ImportMesh(stlFile);
        if (meshes == null)
        {
            return null;
        }
        
        if (parent == null) parent = new GameObject(Path.GetFileNameWithoutExtension(stlFile)).transform;

        Material material = MaterialExtensions.CreateBasicMaterial();
        
        for (int i = 0; i < meshes.Length; i++)
        {
            GameObject gameObject = new GameObject(Path.GetFileNameWithoutExtension(stlFile));
            gameObject.AddComponent<MeshFilter>().sharedMesh = meshes[i];
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            gameObject.transform.SetParent(parent, false);
        }
        return parent.gameObject;
    }

}

}