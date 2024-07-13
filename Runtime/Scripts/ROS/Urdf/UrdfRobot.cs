

using System.Collections.Generic;
using System.Linq;
using SimToolkit.ROS.Urdf;
using UnityEngine;

public class UrdfRobot : MonoBehaviour
{
    public List<UrdfJoint> joints = new List<UrdfJoint>();
    public List<UrdfLink> links = new List<UrdfLink>();
    public UrdfLink RootLink => links[0];
    public UrdfRobot planningRobot;
    public bool isPlanningRobot = false;

    public UrdfJoint GetJoint(string name)
    {
        return joints.Find(j => j.jointName == name);
    }

    public UrdfLink GetLink(string name)
    {
        return links.Find(l => l.LinkName == name);
    }

    public void DisableColliders()
    {
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    public void EnableColliders()
    {
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }

    public void DisableVisuals()
    {
        var visuals = GetComponentsInChildren<MeshRenderer>();
        foreach (var visual in visuals)
        {
            visual.enabled = false;
        }
    }

    public void EnableVisuals()
    {
        var visuals = GetComponentsInChildren<MeshRenderer>();
        foreach (var visual in visuals)
        {
            visual.enabled = true;
        }
    }

    [ContextMenu("Populate Joints")]
    public void PopulateLinks()
    {
        links = new List<UrdfLink>();
        foreach (var link in GetComponentsInChildren<UrdfLink>())
        {
            links.Add(link);
        }
    }

    [ContextMenu("Populate Links")]
    public void PopulateJoints()
    {
        joints = new List<UrdfJoint>();
        foreach (var joint in GetComponentsInChildren<UrdfJoint>())
        {
            joints.Add(joint);
        }
    }
}