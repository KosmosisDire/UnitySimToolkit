using System;
using UrdfToolkit.Urdf;
using UnityEngine;

namespace SimToolkit.ROS.Urdf.Importer
{

public class UrdfVisualMeshImporter : BaseMeshImporter
{
    public static GameObject Create(Transform parent, UrdfGeometryDef geometry)
    {
        GameObject geometryGameObject = null;

        switch (geometry.type)
        {
            case GeometryTypes.Box:
                geometryGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var boxCollider = geometryGameObject.GetComponent<BoxCollider>();
                if (boxCollider) GameObject.DestroyImmediate(boxCollider);
                break;
            case GeometryTypes.Cylinder:
                geometryGameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var cylinderCollider = geometryGameObject.GetComponent<CapsuleCollider>();
                if (cylinderCollider) GameObject.DestroyImmediate(cylinderCollider);
                break;
            case GeometryTypes.Sphere:
                geometryGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var sphereCollider = geometryGameObject.GetComponent<SphereCollider>();
                if (sphereCollider) GameObject.DestroyImmediate(sphereCollider);
                break;
            case GeometryTypes.Mesh:
                geometryGameObject = CreateMeshVisual(geometry.mesh, parent);
                break;
        }

        if (geometryGameObject != null)
        {
            geometryGameObject.transform.SetParent(parent, false);
            geometryGameObject.transform.localScale = Vector3.one;
            SetScale(parent, geometry);
        }

        return geometryGameObject;
    }

    private static GameObject CreateMeshVisual(UrdfMesh mesh, Transform parent = null)
    {
        return CreateMeshVisualRuntime(mesh, parent);
    }

    private static GameObject CreateMeshVisualRuntime(UrdfMesh mesh, Transform parent = null)
    {
        GameObject meshObject = null;
        if (!string.IsNullOrEmpty(mesh.filename))
        {
            try 
            {
                string meshPath = mesh.meshPath;
                if (meshPath.ToLower().EndsWith(".stl"))
                {
                    meshObject = CreateStlGameObjectRuntime(meshPath, parent);
                }
                else if (meshPath.ToLower().EndsWith(".dae"))
                {
                    float globalScale = ColladaAssetPostProcessor.ReadGlobalScale(meshPath);
                    meshObject = MeshImporter.Load(meshPath, globalScale, globalScale, globalScale);
                    if (meshObject != null) 
                    {
                        ColladaAssetPostProcessor.ApplyColladaOrientation(meshObject, meshPath);
                    }
                }
                else if (meshPath.ToLower().EndsWith(".obj"))
                {
                    meshObject = MeshImporter.Load(meshPath);
                }
            }
            catch (Exception ex) 
            {
                Debug.LogAssertion(ex);
            }
            
            if (meshObject == null) 
            {
                Debug.LogError("Unable to load visual mesh: " + mesh.meshPath);
            }
        }
        return meshObject;
    }
}

}