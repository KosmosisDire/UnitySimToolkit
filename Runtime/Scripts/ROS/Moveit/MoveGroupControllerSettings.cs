using System;
using UnityEngine;
using static Toolkit.TransformGizmo;

namespace SimToolkit.ROS.Moveit
{
[Serializable]
public struct MoveGroupControllerSettings
{
    public string group;
    public bool hide;
    [SerializeField] private Axis linearAxes;
    [SerializeField] private Axis angularAxes;
    [HideInInspector] public MoveitPlannerOptions planningOptions;
    [HideInInspector] public MoveitIKOptions ikOptions;

    public readonly Axis LinearAxes => !string.IsNullOrWhiteSpace(group) ? linearAxes : Axis.X | Axis.Y | Axis.Z | Axis.XY | Axis.XZ | Axis.YZ;
    public readonly Axis AngularAxes => !string.IsNullOrWhiteSpace(group) ? angularAxes : Axis.X | Axis.Y | Axis.Z;
}
}