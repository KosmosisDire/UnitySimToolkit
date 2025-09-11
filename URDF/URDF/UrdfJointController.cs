using System;
using UnityEngine;

[RequireComponent(typeof(UrdfRobot))]
[ExecuteAlways]
public class UrdfJointController : MonoBehaviour
{
    public float[] joints = new float[0];
    private UrdfRobot robot;

    void OnEnable()
    {
        robot = GetComponent<UrdfRobot>();
    }

    void Update()
    {
        if (robot == null)
        {
            robot = GetComponent<UrdfRobot>();
        }

        if (robot != null)
        {
            // we have to subtract the starting joints from the joints because the articulation bodies use the starting joints as 0,0,0,0,0,0,0
            Array.Resize(ref joints, robot.joints.Count);
            robot.SetJointPositions(joints);

            // if in edit mode then disable all child colliders and then reenable (if they were previously enabled). This forces them to recalculate their bounds
            if (!Application.isPlaying)
            {
                // disable and then reenable all children if they are enabled
                foreach (Transform child in transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
