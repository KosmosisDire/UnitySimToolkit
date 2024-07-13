
using System.Collections.Generic;
using System.Threading.Tasks;
using UrdfToolkit.Srdf;

namespace SimToolkit.ROS.Srdf
{

public class SrdfManager : RosFeatureSingleton<SrdfManager>
{
    public SrdfRobotDescription srdfDescription;

    protected override async Task Init()
    {
        var srdfString = await ParamServer.GetParam("robot_description_semantic");
        srdfDescription = new SrdfRobotDescription(srdfString);
    }

    public static SrdfGroup GetGroup(string name)
    {
        return Instance.srdfDescription.groups.Find(g => g.name == name);
    }

    public static SrdfRobotDescription Description => Instance.srdfDescription;
    public static List<SrdfGroup> Groups => Instance.srdfDescription.groups;
    
}

}
