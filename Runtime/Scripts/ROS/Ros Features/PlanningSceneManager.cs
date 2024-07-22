using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.Std;
using SimToolkit;
using SimToolkit.ROS.Moveit;
using SimToolkit.ROS.Urdf;
using Toolkit;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEditor;
using UnityEngine;

public class PlanningSceneManager : RosFeatureSingleton<PlanningSceneManager>
{
    public bool notifyChanges = true;
    public string applyPlanningSceneService = "/apply_planning_scene";
    public string getPlanningSceneService = "/get_planning_scene";
    public float refreshRate = 0.25f;

    public static PlanningSceneObjects SceneObjects { get; private set; } = new PlanningSceneObjects();
    public static PlanningSceneMsg PlanningScene { get; private set; }
    private static PlanningSceneMsg planningSceneDiff;

    protected override async Task Init()
    {
        ROS.RegisterRosService<ApplyPlanningSceneRequest, ApplyPlanningSceneResponse>(applyPlanningSceneService);
        ROS.RegisterRosService<GetPlanningSceneRequest, GetPlanningSceneResponse>(getPlanningSceneService);

        SceneObjects = new PlanningSceneObjects();
        PlanningScene = new PlanningSceneMsg();
        planningSceneDiff = new PlanningSceneMsg();
    }

    public override async Task PostInit()
    {
        await ClearPlanningScene();
        LocalUpdateLoop();
        RemoteUpdateLoop();
    }

    public static async Task ClearPlanningScene()
    {
        var oldObjects = ReadLocalObjects();
        Debug.Log("Clearing " + string.Join(", ", oldObjects));

        var removeObjectMsgs = new List<CollisionObjectMsg>();
        foreach (var obj in oldObjects)
        {
            var msg = new CollisionObjectMsg
            {
                id = obj,
                operation = CollisionObjectMsg.REMOVE
            };
            removeObjectMsgs.Add(msg);
        }

        var clearedRequest = new ApplyPlanningSceneRequest();
        clearedRequest.scene.world.collision_objects = removeObjectMsgs.ToArray();
        clearedRequest.scene.is_diff = true;

        var response = await ROS.SendServiceMessage<ApplyPlanningSceneResponse>(Instance.applyPlanningSceneService, clearedRequest);
        NotificationManager.Notice("Planning scene cleared " + (response.success ? "successfully" : "unsuccessfully"));
    }

    public static void AddObject(CollisionObject obj)
    {
        if (obj == null) return;
        if (SceneObjects.HasObject(obj)) return;
        
        if (obj.RemoteAuthority)
        {
            Debug.LogWarning("Object with ID " + obj.ID + " has remote authority and cannot be added to the planning scene");
            return;
        }

        SceneObjects.EnqueueAddition(obj);
    }

    public static void UpdateObject(CollisionObject obj)
    {
        if (obj == null) return;
        if (!SceneObjects.HasObject(obj)) return;
        

        obj.transform.GetPositionAndRotation(out var position, out var rotation);

        UpdateObjectTransformToRemote(obj.ID, position, rotation);
        
        obj.lastPosition = obj.transform.position;
        obj.lastRotation = obj.transform.rotation;
        obj.lastUpdateTime = DateTime.Now;
    }
    
    public static void RemoveObject(CollisionObject obj, bool onlyLocal = false, bool destroy = false)
    {
        if (obj == null) return;
        if (!SceneObjects.HasObject(obj)) return;

        if (!onlyLocal) RemoveObjectFromRemote(obj.ID);
        
        SceneObjects.UnregisterObject(obj);
        WriteLocalObjects();

        if (destroy)
        {
            GameObject.Destroy(obj.gameObject);
        }
    }

    const string localObjectsFile = "/local_collision_objects.txt";
    private static void WriteLocalObjects()
    {
        // write the objects to a file so we know which objects to delete upon restart
        var localObjects = SceneObjects.Local.Select(o => o.ID).ToArray();
        var localObjectsString = string.Join("\n", localObjects);
        System.IO.File.WriteAllText(Application.persistentDataPath + localObjectsFile, localObjectsString);
    }

    private static string[] ReadLocalObjects()
    {
        if (!System.IO.File.Exists(Application.persistentDataPath + localObjectsFile))
        {
            return new string[0];
        }

        var localObjectsString = System.IO.File.ReadAllText(Application.persistentDataPath + localObjectsFile);
        return localObjectsString.Split('\n');
    }

    private static async void LocalUpdateLoop()
    {
        await Awaitable.WaitForSecondsAsync(1f);

        while (Application.isPlaying)
        {
            planningSceneDiff = new PlanningSceneMsg();
            planningSceneDiff.is_diff = true;

            ProcessAdditionQueue();
            UpdateLocal();

            ApplyPlanningScene();

            await Awaitable.WaitForSecondsAsync(Instance.refreshRate);
        }
    }

    public static void UpdateLocal()
    {
        if (!Initialized) return;

        foreach (var obj in SceneObjects.Local)
        {
            if (obj.isFrozen || obj.IsAttached) continue;

            if (Vector3.Distance(obj.lastPosition, obj.transform.position) > obj.refreshDistance ||
                Quaternion.Angle(obj.lastRotation, obj.transform.rotation) > obj.refreshAngle)
            {
                UpdateObject(obj);
            }
            
            if (!Application.isPlaying) return;
        }

        WriteLocalObjects();
    }

    public static void ProcessAdditionQueue()
    {
        while (SceneObjects.GetNextAddition(out var obj))
        {
            if (obj.isFrozen) continue;

            AddObjectToRemote(obj);
            
            if (Instance.notifyChanges) 
                NotificationManager.Notice("Added object to planning scene: " + obj.name);
        }
    }

    private static void ApplyPlanningScene()
    {
        if (ROS.HasConnectionError) return;

        var request = new ApplyPlanningSceneRequest();
        request.scene = planningSceneDiff;
        request.scene.is_diff = true;

        ROS.SendServiceMessage<ApplyPlanningSceneResponse>(Instance.applyPlanningSceneService, request);
    }

    private static CollisionObject CreateCollisionObjectFromRemoteMsg(CollisionObjectMsg objMsg)
    {
        var obj = new GameObject(objMsg.id).AddComponent<CollisionObject>();
        obj.FromMessage(objMsg);
        return obj;
    }

    public static void DisableCollisions(IPlanningSceneObject obj1, IPlanningSceneObject obj2)
    {
        if (ROS.HasConnectionError || !Initialized) return;
        throw new NotImplementedException();
    }

    private static async void RemoteUpdateLoop()
    {
        await Awaitable.WaitForSecondsAsync(5f);

        while (Application.isPlaying)
        {
            await UpdateFromRemote();
            await Awaitable.WaitForSecondsAsync(Instance.refreshRate);
        }
    }

    public static void AttachObjects(CollisionObject obj, UrdfLink link, bool updateRemote = true)
    {
        if (ROS.HasConnectionError) return;

        obj.SetCollidersEnabled(false);
        obj.attachedTo = link;
        obj.transform.SetParent(link.transform);

        if (updateRemote)
        {
            var attachedObject = new AttachedCollisionObjectMsg()
            {
                @object = obj.ToMessage(),
                link_name = link.name,
                touch_links = new string[0],
                weight = 1
            };

            planningSceneDiff.robot_state.attached_collision_objects
                = planningSceneDiff.robot_state.attached_collision_objects.Append(attachedObject).ToArray();
        }
    }

    public static void DetachObjects(CollisionObject obj, UrdfLink link, bool updateRemote = true)
    {
        if (ROS.HasConnectionError) return;

        obj.SetCollidersEnabled(true);
        obj.attachedTo = null;
        obj.transform.SetParent(null);

        if (updateRemote)
        {
            planningSceneDiff.robot_state.attached_collision_objects 
                = planningSceneDiff.robot_state.attached_collision_objects
                    .Where(a => !(a.@object.id == obj.ID && a.link_name == link.ID)).ToArray();
        }
    }
    
    public static async Task UpdateFromRemote()
    {
        if (!Initialized) return;

        var res = await ROS.SendServiceMessage<GetPlanningSceneResponse>(Instance.getPlanningSceneService, new GetPlanningSceneRequest());
        PlanningScene = res.scene;
        var sceneObjects = new List<CollisionObjectMsg>(PlanningScene.world.collision_objects);
        var attachedObjects = PlanningScene.robot_state.attached_collision_objects.ToList();
        sceneObjects.AddRange(attachedObjects.Select(a => a.@object));
        var allCollisionObjects = SceneObjects.AllCollisionObjects;
        
        var newObjects = sceneObjects.Where(objMsg => !allCollisionObjects.Exists(obj => obj.ID == objMsg.id)).ToArray();
        foreach (var objMsg in newObjects)
        {
            CreateCollisionObjectFromRemoteMsg(objMsg);
        }

        foreach (var obj in SceneObjects.Attached)
        {
            if (obj.isFrozen || !obj.RemoteAuthority) continue;

            var attachedObj = attachedObjects.Find(a => a.@object.id == obj.ID);
            if (attachedObj == null)
            {
                DetachObjects(obj, obj.attachedTo, false);
            }
        }

        foreach (var obj in attachedObjects)
        {
            var existingObj = allCollisionObjects.Find(o => o.ID == obj.@object.id);
            if (existingObj == null || existingObj.IsAttached || !existingObj.RemoteAuthority) continue;

            var link = SceneObjects.GetLink(obj.link_name);
            if (link == null) continue;

            AttachObjects(existingObj, link, false);
        }

        var existingObjects = allCollisionObjects.Where(o => sceneObjects.Exists(r => r.id == o.ID)).ToArray();
        foreach (var obj in existingObjects)
        {
            if (obj.isFrozen || !obj.RemoteAuthority) continue;

            var collisionObj = obj;
            var objMsg = sceneObjects.Find(o => o.id == collisionObj.ID);

            // detect if the object primitives or shapes have changed
            if (!objMsg.IsSame(collisionObj.MessageRepresentation))
            {
                collisionObj.FromMessage(objMsg);
            }

            var pos = TFManager.TransformROSWorldToUnityWorld(objMsg.pose.position.ToVector3()).Ros2Unity();
            var rot = objMsg.pose.orientation.ToQuaternion().Ros2Unity();

            var beforeMove = collisionObj.transform.position;

            if (collisionObj.IsAttached)
            {
                collisionObj.transform.SetLocalPositionAndRotation(pos, rot);
            }
            else
            {
                collisionObj.transform.SetPositionAndRotation(pos, rot);
            }

            var afterMove = collisionObj.transform.position;
            collisionObj.velocity = (afterMove - beforeMove) / (float) (DateTime.Now - collisionObj.lastUpdateTime).TotalSeconds;
            collisionObj.lastPosition = collisionObj.transform.position;
            collisionObj.lastRotation = collisionObj.transform.rotation;
            collisionObj.lastUpdateTime = DateTime.Now;
        }

        var deletedObjects = allCollisionObjects.Where(o => !sceneObjects.Exists(r => r.id == o.ID)).ToArray();
        foreach (var obj in deletedObjects)
        {
            if (obj.isFrozen || !obj.RemoteAuthority) continue;
            RemoveObject(obj, true, true);
        }
    }

    private static void AddObjectToRemote(CollisionObject obj)
    {
        SceneObjects.RegisterObject(obj);

        if (ROS.HasConnectionError) return;
  
        var msg = obj.ToMessage();
        msg.operation = CollisionObjectMsg.ADD;
        planningSceneDiff.world.collision_objects = planningSceneDiff.world.collision_objects.Append(msg).ToArray();
    }

    public static void RemoveObjectFromRemote(string id)
    {
        if (ROS.HasConnectionError) return;

        var msg = new CollisionObjectMsg()
        {
            header = new HeaderMsg()
            {
                frame_id = TFManager.WorldFrameName,
                stamp = new TimeMsg()
            },
            id = id,
            operation = CollisionObjectMsg.REMOVE
        };

        planningSceneDiff.world.collision_objects = planningSceneDiff.world.collision_objects.Append(msg).ToArray();
    }

    public static void UpdateObjectTransformToRemote(string id, Vector3 position, Quaternion orientation)
    {
        if (ROS.HasConnectionError) return;

        var positionROS = position.To<FLU>();
        var orientationROS = orientation.To<FLU>();
        
        var msg = new CollisionObjectMsg()
        {
            header = new HeaderMsg()
            {
                frame_id = TFManager.WorldFrameName,
                stamp = new TimeMsg()
            },
            id = id,
            operation = CollisionObjectMsg.MOVE,
            pose = new PoseMsg()
            {
                position = new PointMsg(positionROS.x,positionROS.y, positionROS.z),
                orientation = new QuaternionMsg(orientationROS.x, orientationROS.y, orientationROS.z, orientationROS.w)
            }
        };

        planningSceneDiff.world.collision_objects = planningSceneDiff.world.collision_objects.Append(msg).ToArray();
    }

}

public class PlanningSceneObjects
{
    readonly Dictionary<string, IPlanningSceneObject> allObjects = new();
    readonly Dictionary<string, CollisionObject> collisionObjects = new();
    public List<CollisionObject> AllCollisionObjects => collisionObjects.Values.ToList();
    public List<CollisionObject> Local => collisionObjects.Values.Where(o => !o.RemoteAuthority).ToList();
    public List<CollisionObject> Remote => collisionObjects.Values.Where(o => o.RemoteAuthority).ToList();
    public List<CollisionObject> Attached => collisionObjects.Values.Where(o => o.IsAttached).ToList();

    readonly ConcurrentQueue<CollisionObject> addObjectsQueue = new ConcurrentQueue<CollisionObject>();
    public void EnqueueAddition(CollisionObject obj) => addObjectsQueue.Enqueue(obj);
    public bool GetNextAddition(out CollisionObject obj) => addObjectsQueue.TryDequeue(out obj);

    /// <summary>
    /// Just adds the object to the appropriate dictionaries
    /// </summary>
    /// <param name="obj"></param>
    public void RegisterObject(IPlanningSceneObject obj)
    {
        if (obj == null) return;
        
        allObjects[obj.ID] = obj;

        if (obj is CollisionObject collisionObject)
        {
            collisionObjects[collisionObject.ID] = collisionObject;
        }
    }

    public void UnregisterObject(IPlanningSceneObject obj)
    {
        allObjects.Remove(obj.ID);

        if (obj is CollisionObject collisionObject)
        {
            collisionObjects.Remove(collisionObject.ID);
        }
    }

    public bool HasObject(string id)
    {
        return allObjects.ContainsKey(id) || addObjectsQueue.Any(o => (o != null && o.ID == id));
    }

    public bool HasObject(IPlanningSceneObject obj)
    {
        if (obj == null) return false;
        return HasObject(obj.ID);
    }

    public IPlanningSceneObject GetObject(string id)
    {
        if (!allObjects.ContainsKey(id)) return null;
        return allObjects[id];
    }

    public CollisionObject GetCollisionObject(string id)
    {
        if (!collisionObjects.ContainsKey(id)) return null;
        return collisionObjects[id];
    }

    public UrdfLink GetLink(string id)
    {
        if (!allObjects.ContainsKey(id)) return null;
        return allObjects[id] as UrdfLink;
    }

    public IPlanningSceneObject FindObject(Predicate<IPlanningSceneObject> predicate)
    {
        return allObjects.Values.ToList().Find(predicate);
    }

    public CollisionObject FindCollisionObject(Predicate<CollisionObject> predicate)
    {
        return collisionObjects.Values.ToList().Find(predicate);
    }
}
