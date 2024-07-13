using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RosMessageTypes.Sensor;
using SimToolkit.ROS.Urdf;
using UnityEngine;
using Toolkit;

public class URDFJointMirror : RosBehaviour
{

    [Header("General Settings")]
    public string jointStateTopic = "/joint_states";
    public bool localControl = false;
    public bool interpolate = true;

    [Header("Joint Properties")]
    public bool instantanious = false;
    public bool setStiffness = true;
    public float stiffness = 100000;
    public bool setDamping = true;
    public float damping = 1000;

    private readonly Dictionary<string, ArticulationBody> jointMap = new Dictionary<string, ArticulationBody>();
    private float timeSinceLastMessage = 0;
    private float updateRateHz = 1 / 40f;

    private JointStateMsg lastJointStatesLocal;
    private JointStateMsg _jointStatesLocal = null;
    public JointStateMsg JointStatesLocal 
    { 
        get 
        {
            return _jointStatesLocal ?? JointStatesRemote; 
        } 
        set 
        {
            // if the joint states are the same as the last message, ignore them
            if (_jointStatesLocal != null && Enumerable.SequenceEqual(_jointStatesLocal.position, value.position))
                return;
            
            if (timeSinceLastMessage > 0.001f && timeSinceLastMessage < 2) 
                updateRateHz = (1 / timeSinceLastMessage) * 0.1f + updateRateHz * 0.9f;

            timeSinceLastMessage = 0;
            
            lastJointStatesLocal = _jointStatesLocal;
            _jointStatesLocal = value;
        }
    }

    private JointStateMsg lastJointStatesRemote;
    private JointStateMsg _jointStatesRemote = null;
    public JointStateMsg JointStatesRemote
    {
        get
        {
            return _jointStatesRemote;
        }
        set
        {
            lastJointStatesRemote = _jointStatesRemote;
            _jointStatesRemote = value;
            if (!localControl)
            {
                lastJointStatesLocal = JointStatesLocal;
                _jointStatesLocal = value;
            }
        }
    }

    public List<ArticulationReducedSpace> startingPositions = new();

    public async override Task BeforeInit()
    {
        // make sure the joints have an initial spring and damping
        var joints = GetComponentsInChildren<ArticulationBody>().ToList();
        foreach (var joint in joints)
        {
            var jointX = joint.xDrive;
            jointX.stiffness = stiffness;
            jointX.damping = damping;
            jointX.target = joint.jointType == ArticulationJointType.RevoluteJoint ? joint.jointPosition[0] * Mathf.Rad2Deg : 0;
            jointX.driveType = ArticulationDriveType.Target;
            joint.xDrive = jointX;
        }

        startingPositions = joints.Select((joint) => joint.jointPosition).ToList();
    }


    public async override Task Init()
    {
        ROS.Subscribe<JointStateMsg>(jointStateTopic, JointStateCallback);

        // wait for the first joint states message
        while (JointStatesLocal == null || JointStatesLocal.name.Length == 0)
        {
            await Awaitable.NextFrameAsync();
        }
        
        // get the joint names and create a map of joint names to joints
        Debug.Log($"Found {JointStatesLocal.name.Length} joints for {gameObject.name}");
        for (int i = 0; i < JointStatesLocal.name.Length; i++)
        {
            var name = JointStatesLocal.name[i];
            var child = gameObject.FindRecursive<UrdfJoint>((t) =>
            {
                return t.jointName.StartsWith(name);
            });
            if (!child) continue;

            var childJoint = child.GetComponent<ArticulationBody>();
            jointMap[name] = childJoint;
            Debug.Log($"Found joint {name}");
        }
    }

    void JointStateCallback(JointStateMsg msg)
    {
        if (msg == null) return;
        JointStatesRemote = msg;
    }

    bool firstUpdate = true;

    void Update()
    {
        if (!IsReady)
            return;
        
        timeSinceLastMessage += Time.deltaTime;
       
        if (firstUpdate)
        {
            firstUpdate = false;
            var oldInstantanious = instantanious;
            instantanious = true;
            _ = Task.Delay(100).ContinueWith((_) => instantanious = oldInstantanious);
        }

        for (int i = 0; i < JointStatesLocal.name.Length; i++)
        {
            var name = JointStatesLocal.name[i];
            var position = JointStatesLocal.position[i];

            // get the child joint with the same name
            jointMap.TryGetValue(name, out var childJoint);

            if (!childJoint) continue;

            // set the position, interpolating if necessary
            var jointX = childJoint.xDrive;
            jointX.stiffness = setStiffness ? stiffness : jointX.stiffness;
            jointX.damping = setDamping ? damping : jointX.damping;
            if (instantanious) 
            {
                jointX.driveType = ArticulationDriveType.Target;
                jointX.forceLimit = 1000000;
            }
            else jointX.driveType = ArticulationDriveType.Force;

            if (interpolate && !localControl && JointStatesRemote != null && lastJointStatesRemote.position.Length > i)
            {
                var oldPosition = lastJointStatesRemote.position[i];
                jointX.target = Mathf.Lerp((float)oldPosition, (float)position, timeSinceLastMessage * updateRateHz);
            }
            else
            {
                jointX.target = (float)position;
            }

            if (childJoint.jointType == ArticulationJointType.RevoluteJoint)
            {
                jointX.target *= Mathf.Rad2Deg;
            }

            childJoint.xDrive = jointX;
            
        }
    }
}
