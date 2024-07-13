using System;
using System.Drawing;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.Shape;
using RosMessageTypes.Std;
using UnityEngine;

namespace SimToolkit
{

public static class RosExtensions
{
    public static Vector3 Ros2Unity(this Vector3 vector3)
    {
        return new Vector3(-vector3.y, vector3.z, vector3.x);
    }

    public static Vector3 Unity2Ros(this Vector3 vector3)
    {
        return new Vector3(vector3.z, -vector3.x, vector3.y);
    }

    public static Quaternion Ros2Unity(this Quaternion quaternion)
    {
        return new Quaternion(quaternion.y, -quaternion.z, -quaternion.x, quaternion.w);
    }

    public static Quaternion Unity2Ros(this Quaternion quaternion)
    {
        return new Quaternion(-quaternion.z, quaternion.x, -quaternion.y, quaternion.w);
    }

    public static Vector3 Ros2UnityScale(this Vector3 vector3)
    {
        return new Vector3(vector3.y, vector3.z, vector3.x);
    }

    public static Vector3 Unity2RosScale(this Vector3 vector3)
    {
        return new Vector3(vector3.z, vector3.x, vector3.y);
    }

    public static Vector3 ToVector3(this PointMsg point)
    {
        return new Vector3((float)point.x, (float)point.y, (float)point.z);
    }

    public static PointMsg ToPointMsg(this Vector3 vector3)
    {
        return new PointMsg(vector3.x, vector3.y, vector3.z);
    }

    public static Vector3Msg ToVector3Msg(this Vector3 vector3)
    {
        return new Vector3Msg(vector3.x, vector3.y, vector3.z);
    }

    public static Quaternion ToQuaternion(this QuaternionMsg quaternion)
    {
        return new Quaternion((float)quaternion.x, (float)quaternion.y, (float)quaternion.z, (float)quaternion.w);
    }

    public static QuaternionMsg ToQuaternionMsg(this Quaternion quaternion)
    {
        return new QuaternionMsg(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }

    public static Vector3StampedMsg Stamp(this Vector3Msg vector3, string frameId = null)
    {
        if (frameId == null)
        {
            frameId = TFManager.WorldFrameName;
        }

        return new Vector3StampedMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg()
            },
            vector = vector3
        };
    }

    public static PoseStampedMsg Stamped(this PoseMsg pose, string frameId = null)
    {
        if (frameId == null)
        {
            frameId = TFManager.WorldFrameName;
        }

        return new PoseStampedMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg()
            },
            pose = pose
        };
    }

    public static PointStampedMsg Stamp(this PointMsg point, string frameId = null)
    {
        if (frameId == null)
        {
            frameId = TFManager.WorldFrameName;
        }

        return new PointStampedMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg()
            },
            point = point
        };
    }

    public static QuaternionStampedMsg Stamp(this QuaternionMsg quaternion, string frameId = null)
    {
        if (frameId == null)
        {
            frameId = TFManager.WorldFrameName;
        }
        
        return new QuaternionStampedMsg
        {
            header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg()
            },
            quaternion = quaternion
        };
    }

    public static PrimitiveType ToPrimitiveType(this SolidPrimitiveMsg prim)
    {
        switch (prim.type)
        {
            case 1:
                return PrimitiveType.Cube;
            case 2:
                return PrimitiveType.Sphere;
            case 3:
                return PrimitiveType.Cylinder;
            default:
                return PrimitiveType.Cube;
        }
    }

    public static Mesh ToMesh(this MeshMsg mesh)
    {
        var unityMesh = new Mesh
        {
            vertices = mesh.vertices.Select(v => v.ToVector3().Ros2Unity()).ToArray(),
            triangles = mesh.triangles.SelectMany(t => t.vertex_indices).Cast<int>().ToArray(),
            normals = new Vector3[mesh.vertices.Length],
            uv = new Vector2[mesh.vertices.Length]
        };
        unityMesh.RecalculateBounds();
        unityMesh.RecalculateNormals();
        unityMesh.RecalculateTangents();
        unityMesh.RecalculateUVDistributionMetrics();

        return unityMesh;
    }


    /// <summary>
    /// Compares two CollisionObjectMsgs to see if they are identical, excluding the transform which changes frequently
    /// </summary>
    /// <param name="objMsg"></param>
    /// <returns></returns>
    public static bool IsSame(this CollisionObjectMsg objMsg, CollisionObjectMsg other)
    {
        var result = true;

        if (objMsg.primitives.Length != other.primitives.Length ||
            objMsg.meshes.Length != other.meshes.Length)
        {
            return false;
        }

        for (int i = 0; i < objMsg.primitives.Length; i++)
        {
            var primitive = objMsg.primitives[i];
            var otherPrimitive = other.primitives[i];
            if (primitive.type != otherPrimitive.type)
            {
                result = false;
                break;
            }

            if (!Enumerable.SequenceEqual(primitive.dimensions, otherPrimitive.dimensions))
            {
                result = false;
                break;
            }
        }

        for (int i = 0; i < objMsg.meshes.Length; i++)
        {
            var mesh = objMsg.meshes[i];
            var otherMesh = other.meshes[i];
            if (mesh.vertices.Length != otherMesh.vertices.Length)
            {
                result = false;
                break;
            }

            if (mesh.triangles.Length != otherMesh.triangles.Length)
            {
                result = false;
                break;
            }
        }

        return result;
    }
    

}

}