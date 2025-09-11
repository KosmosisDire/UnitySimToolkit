using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SimToolkit.ROS.Urdf
{

public class UrdfLink : MonoBehaviour
{
    public bool isBaseLink;

    public List<UrdfLink> childLinks = new List<UrdfLink>();
    public UrdfJoint joint;
    public UrdfRobot robot;

    void Awake()
    {
        robot = GetComponentInParent<UrdfRobot>();
    }

    void OnDestroy()
    {
    }

}


}

