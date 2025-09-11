
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UrdfToolkit.Extensions;

namespace UrdfToolkit.Urdf
{
public struct UrdfDescription
{
    public XmlDocument document;
    public string source;
    public string sourcePath;
    public string name;
    public Dictionary<string, UrdfLinkDef> links;
    public Dictionary<string, UrdfJointDef> joints;
    public List<UrdfMesh> meshes;

    public UrdfDescription(string source, string sourcePath = null)
    {
        this.source = source;
        this.sourcePath = sourcePath;
        this.name = "";
        this.links = new Dictionary<string, UrdfLinkDef>();
        this.joints = new Dictionary<string, UrdfJointDef>();
        this.meshes = new List<UrdfMesh>();
        document = new XmlDocument();

        Parse();

        // find likely paths for mesh files
        foreach (var link in links)
        {
            var visual = link.Value.visual?.geometry.mesh;
            if (visual != null)
            {
                meshes.Add(visual);
            }

            var collision = link.Value.collision?.geometry.mesh;
            if (collision != null)
            {
                meshes.Add(collision);
            }
        }

        foreach (var mesh in meshes)
        {
            var meshRelativePath = mesh.filename.Replace("package://", "");

            var sourceCopy = sourcePath;
            var path = Path.Combine(sourceCopy, meshRelativePath);

            var depth = 5;
            while (!File.Exists(path))
            {
                sourceCopy = Path.GetDirectoryName(sourceCopy);
                path = Path.Combine(sourceCopy, meshRelativePath);
                depth--;
                if (path.ToLower() == Directory.GetCurrentDirectory() || depth == 0)
                {
                    path = null;
                    break;
                }
            }

            if (path != null)
            {
                mesh.meshPath = path.Replace("\\", "/");
            }
        }
    }


    private void Parse()
    {
        document.LoadXml(source);
        XmlNode robotNode = document.SelectSingleNode("robot")!;
        if (robotNode == null) return;

        name = robotNode.GetString("name");

        var linkNodes = robotNode?.SelectNodes("link");
        var jointNodes = robotNode?.SelectNodes("joint");

        for (int i = 0; i < linkNodes?.Count; i++)
        {
            var linkNode = linkNodes[i];
            if (linkNode == null) continue;
            var link = new UrdfLinkDef(linkNode);
            links.Add(link.name ?? "", link);
        }

        for (int i = 0; i < jointNodes?.Count; i++)
        {
            var jointNode = jointNodes[i];
            if (jointNode == null) continue;
            var joint = new UrdfJointDef(jointNode);
            joints.Add(joint.name ?? "", joint);
        }
    }

    public UrdfJointDef? GetJointForLink(string linkName)
    {
        foreach (var joint in joints)
        {
            if (joint.Value.child == linkName)
            {
                return joint.Value;
            }
        }
        
        return null;
    }

    public List<UrdfLinkDef> GetChildrenForLink(string linkName)
    {
        var children = new List<UrdfLinkDef>();
        foreach (var joint in joints)
        {
            if (joint.Value.parent == linkName)
            {
                children.Add(links[joint.Value.child]);
            }
        }
        return children;
    }

    public readonly string Stringify(int indentation)
    {
        var str = $"robot: {name}";
        str += StringifyLinks(indentation);
        str += StringifyJoints(indentation);
        return str;
    }

    public readonly string StringifyLinks(int indentation)
    {
        var str = "";
        foreach (var link in links)
        {
            str += $"\n{link.Value.Stringify(indentation)}";
        }
        return str;
    }

    public readonly string StringifyJoints(int indentation)
    {
        var str = "";
        foreach (var joint in joints)
        {
            str += $"\n{joint.Value.Stringify(indentation)}";
        }
        return str;
    }

    public override string ToString()
    {
        return Stringify(2);
    }
}

}