using System.Xml;
using UrdfToolkit.Extensions;

namespace UrdfToolkit.Urdf
{

public struct UrdfLinkDef
{
    public string name;
    public UrdfInertialDef? inertial;
    public UrdfVisualDef? visual;
    public UrdfCollisionDef? collision;

    public UrdfLinkDef(XmlNode source)
    {
        name = source.Attributes?["name"]?.Value ?? "";
        var inertialNode = source.SelectSingleNode("inertial");
        var visualNode = source.SelectSingleNode("visual");
        var collisionNode = source.SelectSingleNode("collision");

        inertial = null;
        if (inertialNode != null) 
        {
            inertial = new UrdfInertialDef(inertialNode);
        }

        visual = null;
        if (visualNode != null) 
        {
            visual = new UrdfVisualDef(visualNode);
        }

        collision = null;
        if (collisionNode != null) 
        {
            collision = new UrdfCollisionDef(collisionNode);
        }
    }

    public readonly string Stringify(int indentation)
    {
        var str = $"link: {name}";

        var strIn = "";
        if (inertial.HasValue) strIn += $"\ninertial:\n{inertial?.Stringify(indentation)}";
        if (visual.HasValue) strIn += $"\nvisual:\n{visual?.Stringify(indentation)}";
        if (collision.HasValue) strIn += $"\ncollision:\n{collision?.Stringify(indentation)}";
        strIn = strIn.Indent(indentation);

        return (str + strIn).Indent(indentation);
    }
}

}