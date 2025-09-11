using System;
using System.Xml;
using UrdfToolkit.Extensions;

namespace UrdfToolkit.Urdf
{

public struct UrdfLimitDef
{
    public float lower;
    public float upper;
    public float effort;
    public float velocity;

    public UrdfLimitDef(XmlNode source)
    {
        lower = source.Attributes?["lower"]?.Value.ParseFloatOrDefault(0) * 57.296f ?? 0; // 57.296 is rad to degrees
        upper = source.Attributes?["upper"]?.Value.ParseFloatOrDefault(0) * 57.296f ?? 0;
        effort = source.Attributes?["effort"]?.Value.ParseFloatOrDefault(0) ?? 0;
        velocity = source.Attributes?["velocity"]?.Value.ParseFloatOrDefault(0) ?? 0;
    }

    public readonly string Stringify(int indentation)
    {
        return $"lower: {lower}  upper: {upper}  effort: {effort}  velocity: {velocity}".Indent(indentation);
    }
}

}