using System.Threading.Tasks;
using RosMessageTypes.Geometry;
using RosMessageTypes.MasterControl;
using RosMessageTypes.Std;
using UnityEngine;

namespace SimToolkit.ROS.Moveit
{
[RequireComponent(typeof(MoveitRobot))]
public class MoveitRobotPickPlace : RosBehaviour
{
    public string moveGroup = "arm";
    public string pickTopic = "/pick";
    public string placeTopic = "/place";
    public string runTopic = "/run_pick_place";
    public CollisionObject targetObject;
    public Transform placePositionOverride;
    private MoveitRobot robot;

    public override async Task Init()
    {
        ROS.RegisterPublisher<PickMsg>(pickTopic);
        ROS.RegisterPublisher<PlaceMsg>(placeTopic);
        ROS.RegisterPublisher<EmptyMsg>(runTopic);
        ROS.RegisterPublisher<PoseStampedMsg>("/target_pose");
        robot = GetComponent<MoveitRobot>();
    }

    [ContextMenu("Pick")]
    public void Pick()
    {
        Pick(targetObject);
    }

    public void Pick(CollisionObject targetObject, float approach_height = 0.1f)
    {
        if (!IsReady) return;

        this.targetObject = targetObject;

        if (targetObject == null)
        {
            Debug.LogWarning("No target object specified");
            return;
        }

        var pick = new PickMsg()
        {
            object_name = targetObject.ID,
            group_name = moveGroup,
            approach_height = approach_height,
        };

        ROS.Publish(pickTopic, pick);
    }

    public void Pick(IPlanningSceneObject targetObject, float approach_height = 0.1f)
    {
        if (targetObject is CollisionObject collisionObject)
        {
            Pick(collisionObject, approach_height);
        }
        else
        {
            Debug.LogWarning("Target object is not a CollisionObject");
        }


    }

    public void Place(Vector3 position, Quaternion rotation, float approach_height = 0.1f)
    {
        if (!IsReady) return;

        var place = new PlaceMsg()
        {
            object_name = targetObject.ID,
            group_name = moveGroup,
            place_pose = new PoseMsg()
            {
                position = position.Unity2Ros().ToPointMsg(),
                orientation = rotation.Unity2Ros().ToQuaternionMsg()
            }.Stamped(),
            approach_height = approach_height,
        };

        ROS.Publish(placeTopic, place);
    }

    public async void PickAndPlace(CollisionObject targetObject, Vector3 position, Quaternion rotation, float approach_height = 0.1f)
    {
        Pick(targetObject, approach_height);
        await Awaitable.WaitForSecondsAsync(0.5f);
        Place(position, rotation, approach_height);
        await Awaitable.WaitForSecondsAsync(0.5f);
        ExecuteQueue();
    }

    public async void PickAndPlace(IPlanningSceneObject targetObject, Vector3 position, Quaternion rotation, float approach_height = 0.1f)
    {
        if (targetObject is CollisionObject collisionObject)
        {
            PickAndPlace(collisionObject, position, rotation, approach_height);
        }
        else
        {
            Debug.LogError("Target object is not a CollisionObject");
        }
    }

    public void ExecuteQueue()
    {
        ROS.Publish(runTopic, new EmptyMsg());
    }

}

}