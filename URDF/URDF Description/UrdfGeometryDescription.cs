using System.Xml;
using UrdfToolkit.Extensions;
using UnityEngine;

namespace UrdfToolkit.Urdf
{

public enum GeometryTypes
{
    Box,
    Cylinder,
    Sphere,
    Mesh
}


public struct UrdfGeometryDef
{
    public GeometryTypes type;
    public UrdfBox? box;
    public UrdfCylinder? cylinder;
    public UrdfSphere? sphere;
    public UrdfMesh mesh;
    public UrdfIGeometryType geometry;

    public static GeometryTypes GetGeometryType(string type)
    {
        switch (type)
        {
            case "box":
                return GeometryTypes.Box;
            case "cylinder":
                return GeometryTypes.Cylinder;
            case "sphere":
                return GeometryTypes.Sphere;
            case "mesh":
                return GeometryTypes.Mesh;
            default:
                return GeometryTypes.Box;
        }
    }

    public UrdfGeometryDef(XmlNode source)
    {
        var firstNode = source.FirstChild;
        type = GetGeometryType(firstNode?.Name ?? "");

        box = null;
        cylinder = null;
        sphere = null;
        mesh = null;
        geometry = null;

        switch (type)
        {
            case GeometryTypes.Box:
                box = new UrdfBox(firstNode);
                geometry = box.Value;
                break;
            case GeometryTypes.Cylinder:
                cylinder = new UrdfCylinder(firstNode);
                geometry = cylinder.Value;
                break;
            case GeometryTypes.Sphere:
                sphere = new UrdfSphere(firstNode);
                geometry = sphere.Value;
                break;
            case GeometryTypes.Mesh:
                mesh = new UrdfMesh(firstNode);
                geometry = mesh;
                break;
        }
    }

    public readonly string Stringify(int indentation)
    {
        var str = $"type: {type}";
        if (box.HasValue) str += $"\n{box?.Stringify(indentation)}";
        if (cylinder.HasValue) str += $"\n{cylinder?.Stringify(indentation)}";
        if (sphere.HasValue) str += $"\n{sphere?.Stringify(indentation)}";
        if (mesh != null) str += $"\n{mesh?.Stringify(indentation)}";
        return str.Indent(indentation);
    }
}

public interface UrdfIGeometryType
{
    public Vector3 UnityScale { get; }
}

public struct UrdfBox : UrdfIGeometryType
{
    public Vector3 size;
    public Vector3 UnityScale => size.Ros2UnityScale();

    public UrdfBox(XmlNode source)
    {
        size = source.GetVector3("size", Vector3.one);
    }

    public readonly string Stringify(int indentation)
    {
        return $"size: {size}".Indent(indentation);
    }
}

public struct UrdfCylinder : UrdfIGeometryType
{
    public float radius;
    public float length;
    public Vector3 UnityScale => new Vector3((float)radius, (float)length, (float)radius).Ros2UnityScale();

    public UrdfCylinder(XmlNode source)
    {
        radius = source.GetFloat("radius");
        length = source.GetFloat("length");
    }

    public readonly string Stringify(int indentation)
    {
        return $"radius: {radius}  length: {length}".Indent(indentation);
    }
}

public struct UrdfSphere : UrdfIGeometryType
{
    public float radius;
    public Vector3 UnityScale => new Vector3((float)radius, (float)radius, (float)radius).Ros2UnityScale();

    public UrdfSphere(XmlNode source)
    {
        radius = source.GetFloat("radius");
    }

    public readonly string Stringify(int indentation)
    {
        return $"radius: {radius}".Indent(indentation);
    }
}

public class UrdfMesh : UrdfIGeometryType
{
    public string filename;
    public Vector3 scale;
    public Vector3 UnityScale => scale.Ros2UnityScale();
    public string meshPath;

    public UrdfMesh(XmlNode source)
    {
        filename = source.GetString("filename");
        scale = source.GetVector3("scale", Vector3.one);
        meshPath = null;
    }

    public string Stringify(int indentation)
    {
        var str = $"filename: {filename}";
        str += $"\nlikelyPath: {meshPath}";
        return str.Indent(indentation);
    }
}

}