/*
© Siemens AG, 2018
Author: Suzannah Smith (suzannah.smith@siemens.com)
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UrdfToolkit.Urdf;

using UnityEngine;

namespace SimToolkit.ROS.Urdf
{
    public class UrdfJointPlanar : UrdfJoint
    {
        public override UrdfJointType JointType => UrdfJointType.Planar;

        public static UrdfJoint Create(GameObject linkObject)
        {
            UrdfJointPlanar urdfJoint = linkObject.AddComponent<UrdfJointPlanar>();
            urdfJoint.unityJoint = linkObject.GetComponent<ArticulationBody>();
            urdfJoint.unityJoint.jointType = ArticulationJointType.PrismaticJoint;
            return urdfJoint;
        }

        public override float GetPosition()
        {
            Vector3 distanceFromAnchor = unityJoint.transform.localPosition;
            return distanceFromAnchor.magnitude;
        }

        protected override void ImportJointData(UrdfJointDef joint)
        {
            AdjustMovement(joint);
            if (joint.dynamics != null) SetDynamics(joint.dynamics.Value);
        }

        private static JointDrive GetJointDrive(UrdfDynamicsDef dynamics)
        {
            return new JointDrive
            {
                maximumForce = float.MaxValue,
                positionDamper = (float)dynamics.damping,
                positionSpring = (float)dynamics.friction
            };
        }

        private static JointSpring GetJointSpring(UrdfDynamicsDef dynamics)
        {
            return new JointSpring
            {
                damper = (float)dynamics.damping,
                spring = (float)dynamics.friction,
                targetPosition = 0
            };
        }

        private static SoftJointLimit GetLinearLimit(UrdfLimitDef limit)
        {
            return new SoftJointLimit { limit = (float)limit.upper };
        }

        #region Export

        protected override void AdjustMovement(UrdfJointDef joint)
        {
            var effort = joint.limit?.effort ?? 0f;
            var velocity = joint.limit?.velocity ?? 0f;
            var lower = joint.limit?.lower ?? 0f;
            var upper = joint.limit?.upper ?? 0f;

            unityJoint.linearLockX = ArticulationDofLock.LockedMotion;
            if (joint.limit != null)
            {
                unityJoint.linearLockY = ArticulationDofLock.LimitedMotion;
                unityJoint.linearLockZ = ArticulationDofLock.LimitedMotion;
                var drive = new ArticulationDrive()
                {
                    stiffness = unityJoint.xDrive.stiffness,
                    damping = unityJoint.xDrive.damping,
                    forceLimit = effort,
                    lowerLimit = lower,
                    upperLimit = upper,
                };
                unityJoint.xDrive = drive;
                unityJoint.zDrive = drive;
                unityJoint.yDrive = drive;
                unityJoint.maxLinearVelocity = velocity;
            }
            else
            {
                unityJoint.linearLockZ = ArticulationDofLock.FreeMotion;
                unityJoint.linearLockY = ArticulationDofLock.FreeMotion;
            }

            Quaternion motion = unityJoint.anchorRotation;
            motion.eulerAngles = joint.axis?.xyzRUF.ToUnity() ?? Vector3.up;
            unityJoint.anchorRotation = motion;
        }

        #endregion
    }
}
