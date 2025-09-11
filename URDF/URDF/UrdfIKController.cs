using UnityEngine;

[RequireComponent(typeof(UrdfJointController))]
[RequireComponent(typeof(UrdfIKSolver))]
[ExecuteAlways]
public class UrdfIKController : MonoBehaviour
{
    private UrdfJointController jointController;
    private UrdfIKSolver ikSolver;

    public Transform target;
    public string endEffectorLinkName = "tool_link";
    [SerializeField] private bool solveForRotation = true;

    void OnEnable()
    {
        jointController = GetComponent<UrdfJointController>();
        ikSolver = GetComponent<UrdfIKSolver>();
    }

    void Update()
    {
        if (target != null)
        {
            SolveIK();
        }
    }

    void SetTarget(Vector3 position, Quaternion rotation)
    {
        if (target == null)
        {
            target = new GameObject("IK Target").transform;
        }
        target.position = position;
        target.rotation = rotation;
    }

    void SolveIK()
    {
        UrdfIKSolver.IKResult result;

        if (solveForRotation)
        {
            result = ikSolver.SolveIK(endEffectorLinkName, target.position, target.rotation);
        }
        else
        {
            result = ikSolver.SolveIK(endEffectorLinkName, target.position);
        }

        if (result.success)
        {
            jointController.joints = result.jointPositions;
        }
        else
        {
            // You can still apply the best solution found
            if (result.positionError < 0.01f) // Within 1cm
            {
                jointController.joints = result.jointPositions;
            }
        }
    }
}