using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.Shape;
using RosMessageTypes.Std;
using SimToolkit.ROS.Urdf;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using Toolkit;

namespace SimToolkit.ROS.Moveit
{
public class CollisionObject : RosBehaviour, IPlanningSceneObject, IMessageRepresentation<CollisionObjectMsg, CollisionObject>
{
    private string staticID = null;
    public string ID
    {
        get
        {
            if (string.IsNullOrEmpty(staticID))
            {
                if (gameObject == null)
                {
                    return "null";
                }
                
                return (isPickable ? "pickable_" : "") + gameObject.name + "_"  + gameObject.GetInstanceID().ToString();
            }

            return staticID;
        }
        set
        {
            staticID = value;
        }
    }

    [Header("Behavior")]
    [SerializeField] private bool isPickable = false;
    public bool IsPickable
    {
        get => isPickable;
        set
        {
            isPickable = value;
            if (isPickable && !gameObject.GetComponent<PickableObjectTransform>())
            {
                gameObject.AddComponent<PickableObjectTransform>();
            }
            else if (!isPickable && gameObject.GetComponent<PickableObjectTransform>())
            {
                var pickable = gameObject.GetComponent<PickableObjectTransform>();
                if (pickable)
                {
                    Destroy(pickable);
                }
            }
        }
    }

    public bool isFrozen = false;

    [Tooltip("Is this object controlled externally?")]
    public bool RemoteAuthority = false;

    [Header("Update Rates")]

    [Tooltip("How far the object can move in meters before a new message is sent to ROS")]
    public float refreshDistance = 0.02f;

    [Tooltip("How far the object can rotate in degrees before a new message is sent to ROS")]
    public float refreshAngle = 1f;

    [Header("Collision Generation")]
    public bool includeMeshColliders = true;
    public bool includePrimitiveColliders = true;
    public bool includeChildren = true;
    public bool includeTriggers = false;

    [HideInInspector] public Vector3 lastPosition;
    [HideInInspector] public Quaternion lastRotation;
    [HideInInspector] public DateTime lastUpdateTime;

    [HideInInspector] public Vector3 velocity;

    public List<GameObject> childObjects = new List<GameObject>();
    public UrdfLink attachedTo = null;
    public bool IsAttached => attachedTo != null;

    private bool initialized = false;

    public void AttachTo(UrdfLink link)
    {
        if (IsAttached)
        {
            Detach();
        }

        PlanningSceneManager.AttachObjects(this, link);
    }

    public void Detach()
    {
        PlanningSceneManager.DetachObjects(this, attachedTo);
    }

    public async override Task Init()
    {
        if (!gameObject) return;
        if (!Application.isPlaying)
            return;

        IsPickable = isPickable;

        // if there are no colliders, add box colliders to all renderers
        if (GetComponentsInChildren<Collider>().Length == 0)
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.GetComponent<Collider>() == null)
                {
                    renderer.gameObject.AddComponent<MeshCollider>();
                }
            }
        }
        
        if (!RemoteAuthority) PlanningSceneManager.AddObject(this);

        initialized = true;
    }

    public void OnDestroy()
    {
        NotificationManager.Notice("Removed object " + ID);
        if (!Application.isPlaying || !initialized || !ROSManager.IsReady)
            return;
        
        PlanningSceneManager.RemoveObject(this);
    }

    public CollisionObjectMsg MessageRepresentation {get; private set;}

    public CollisionObjectMsg ToMessage()
    {
        var primitives = new List<SolidPrimitiveMsg>();
        var primitivePoses = new List<PoseMsg>();
        var meshes = new List<MeshMsg>();
        var meshPoses = new List<PoseMsg>();

        Collider[] colliders;
        if (includeChildren)
            colliders = GetComponentsInChildren<Collider>();
        else
            colliders = GetComponents<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i];
            var localPosition = transform.InverseTransformPoint(collider.transform.position).To<FLU>();
            Debug.Log("Local Position: " + transform.InverseTransformPoint(collider.transform.position));

            if (collider.isTrigger && !includeTriggers) continue;
            
            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
            {
                if (!includePrimitiveColliders) continue;
                var primitive = new SolidPrimitiveMsg();

                var collCenter = collider.transform.InverseTransformPoint(collider.bounds.center);
                var finalLocalOffset = localPosition + collCenter.To<FLU>();
                Debug.Log("Final Local Offset: " + finalLocalOffset);
                var scale = collider.transform.lossyScale.To<FLU>();
                var rotation = (Quaternion.Inverse(transform.rotation) * collider.transform.rotation).To<FLU>();

                if (collider.gameObject == gameObject)
                {
                    rotation = Quaternion.identity.To<FLU>();
                }

                var primitivePose = new PoseMsg
                {
                    position = new PointMsg(finalLocalOffset.x, finalLocalOffset.y, finalLocalOffset.z),
                    orientation = new QuaternionMsg(rotation.x, rotation.y, rotation.z, rotation.w)
                };

                switch (collider)
                {
                    case BoxCollider box:
                        var size = box.size.To<FLU>();
                        primitive.type = SolidPrimitiveMsg.BOX;
                        primitive.dimensions = new double[] {Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), Mathf.Abs(size.z * scale.z)};
                        break;
                    case SphereCollider sphere:
                        primitive.type = SolidPrimitiveMsg.SPHERE;
                        primitive.dimensions = new double[] {Mathf.Abs(sphere.radius * scale.x)};
                        break;
                    case CapsuleCollider capsule:
                        primitive.type = SolidPrimitiveMsg.CYLINDER; 
                        primitive.dimensions = new double[] {Mathf.Abs(capsule.radius * scale.x), Mathf.Abs(capsule.height * scale.y)};
                        break;
                }

                primitives.Add(primitive);
                primitivePoses.Add(primitivePose);
            }
            else if (collider is MeshCollider meshCollider)
            {
                if (!includeMeshColliders) continue;
                var mesh = new MeshMsg();
                var pose = new PoseMsg();
                IEnumerable<IGrouping<int, TSource>> GroupBy<TSource>(IEnumerable<TSource> source, int itemsPerGroup)
                {
                    return source.Zip(Enumerable.Range(0, source.Count()),
                                    (s, r) => new { Group = r / itemsPerGroup, Item = s })
                                .GroupBy(i => i.Group, g => g.Item)
                                .ToList();
                }

                var readableMesh = meshCollider.sharedMesh.GetReadableMesh();

                var triangles = readableMesh.triangles.Select(t => (uint)t);
                mesh.triangles = GroupBy(triangles, 3).Select(g => new MeshTriangleMsg(g.Reverse().ToArray())).ToArray();

                var vertices = readableMesh.vertices.Select(v => Vector3.Scale(v, meshCollider.transform.lossyScale).To<FLU>() + localPosition);
                mesh.vertices = vertices.Select(v => new PointMsg(v.x, v.y, v.z)).ToArray();

                meshes.Add(mesh);
                meshPoses.Add(pose);
            }

        }

        var objPosition = transform.position.To<FLU>();
        var objRotation = transform.rotation.To<FLU>();
        
        var msg = new CollisionObjectMsg()
        {
            header = new HeaderMsg()
            {
                frame_id = TFManager.WorldFrameName,
                stamp = new TimeMsg()
            },
            id = ID,
            operation = CollisionObjectMsg.ADD,
            primitives = primitives.ToArray(),
            primitive_poses = primitivePoses.ToArray(),
            meshes = meshes.ToArray(),
            mesh_poses = meshPoses.ToArray(),
            pose = new PoseMsg()
            {
                position = new PointMsg(objPosition.x, objPosition.y, objPosition.z),
                orientation = new QuaternionMsg(objRotation.x, objRotation.y, objRotation.z, objRotation.w)
            }
        };

        MessageRepresentation = msg;
        return msg;
    }

    public CollisionObject FromMessage(CollisionObjectMsg objMsg)
    {
        // delete all existing child objects
        var previousMaterial = GetComponentInChildren<MeshRenderer>()?.material ?? null;
        foreach (var child in childObjects)
        {
            if (child == null) continue;
            if (child == this)
            {
                var renderers = GetComponents<MeshRenderer>();
                var colliders = GetComponents<Collider>();

                foreach (var renderer in renderers)
                {
                    Destroy(renderer);
                }

                foreach (var collider in colliders)
                {
                    Destroy(collider);
                }
            }

            Destroy(child);
        }
        childObjects.Clear();

        IsPickable = objMsg.id.Contains("pickable");
        RemoteAuthority = true;
        ID = objMsg.id;
        MessageRepresentation = objMsg;
        
        // create the mesh / primitive
        if (objMsg.primitives.Length > 0)
        {
            for (int i = 0; i < objMsg.primitives.Length; i++)
            {
                var primitive = objMsg.primitives[i];
                var primitiveType = primitive.ToPrimitiveType();
                var primitiveObject = GameObject.CreatePrimitive(primitiveType);
                childObjects.Add(primitiveObject);
                primitiveObject.transform.SetParent(transform);
                primitiveObject.transform.localPosition = objMsg.primitive_poses[i].position.ToVector3().Ros2Unity();
                primitiveObject.transform.localRotation = objMsg.primitive_poses[i].orientation.ToQuaternion().Ros2Unity();

                switch (primitiveType)
                {
                    case PrimitiveType.Cube:
                        var x = (float)primitive.dimensions[SolidPrimitiveMsg.BOX_X];
                        var y = (float)primitive.dimensions[SolidPrimitiveMsg.BOX_Y];
                        var z = (float)primitive.dimensions[SolidPrimitiveMsg.BOX_Z];
                        primitiveObject.transform.localScale = new Vector3(x, y, z).Ros2UnityScale();
                        break;
                    case PrimitiveType.Sphere:
                        var radius = (float)primitive.dimensions[SolidPrimitiveMsg.SPHERE_RADIUS];
                        primitiveObject.transform.localScale = new Vector3(radius, radius, radius).Ros2UnityScale();
                        break;
                    case PrimitiveType.Cylinder:
                        var radiusCylinder = (float)primitive.dimensions[SolidPrimitiveMsg.CYLINDER_RADIUS];
                        var length = (float)primitive.dimensions[SolidPrimitiveMsg.CYLINDER_HEIGHT];
                        primitiveObject.transform.localScale = new Vector3(radiusCylinder, length, radiusCylinder).Ros2UnityScale();
                        break;
                }

                primitiveObject.name = objMsg.id + "_primitive_" + i;
                var renderer = primitiveObject.GetComponentInChildren<Renderer>();

                if (previousMaterial != null)
                    renderer.material= previousMaterial;
            }
        }
        if (objMsg.meshes.Length > 0)
        {
            for (int i = 0; i < objMsg.meshes.Length; i++)
            {
                var mesh = objMsg.meshes[i];
                var meshObject = new GameObject(objMsg.id + "_mesh_" + i);
                childObjects.Add(meshObject);
                meshObject.transform.SetParent(transform);
                meshObject.transform.localPosition = objMsg.mesh_poses[i].position.ToVector3().Ros2Unity();
                meshObject.transform.localRotation = objMsg.mesh_poses[i].orientation.ToQuaternion().Ros2Unity();
                meshObject.AddComponent<MeshFilter>().mesh = mesh.ToMesh();
                var renderer = meshObject.AddComponent<MeshRenderer>();

                if (previousMaterial != null)
                    renderer.material= previousMaterial;
            }
        }

        PlanningSceneManager.SceneObjects.RegisterObject(this);

        return this;
    }

    public void SetCollidersEnabled(bool enabled)
    {
        foreach (var child in childObjects)
        {
            if (!child) continue;
            var colliders = child.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (!collider) continue;
                collider.enabled = enabled;
            }
        }
    }
    

}
}