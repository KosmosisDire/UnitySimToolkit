using System.Collections.Generic;
using System.Threading.Tasks;
using SimToolkit.ROS.Moveit;

namespace SimToolkit.ROS.Urdf
{

public class UrdfLink : RosBehaviour, IPlanningSceneObject
{
    public bool isBaseLink;
    private string _linkName;
    public string LinkName
    {
        get
        {
            return _linkName;
        }
        set
        {
            _linkName = value;
            gameObject.name = value;
        }
    }
    new public string name => LinkName;

    public string ID => LinkName;

    public List<UrdfLink> childLinks = new List<UrdfLink>();
    public UrdfJoint joint;
    public UrdfRobot robot;

    void Awake()
    {
        if(LinkName == null || LinkName == "")
        {
            LinkName = gameObject.name;
        }
    }

    public override Task Init()
    {
        robot = GetComponentInParent<UrdfRobot>();
        if (!robot.isPlanningRobot)
            PlanningSceneManager.SceneObjects.RegisterObject(this);

        return base.Init();
    }

    void OnDestroy()
    {
        if (!robot.isPlanningRobot)
            PlanningSceneManager.SceneObjects.UnregisterObject(this);
    }

}


}

