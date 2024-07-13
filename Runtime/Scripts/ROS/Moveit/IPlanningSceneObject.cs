
using UnityEngine;
namespace SimToolkit.ROS.Moveit
{
public interface IPlanningSceneObject
{
    public string ID { get; }
    public GameObject gameObject { get; }
    public Transform transform { get; }
}
}