/*
© Siemens AG, 2018-2019
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
    public class UrdfJointRevolute : UrdfJoint
    {
        // public override UrdfJointType JointType => UrdfJointType.Revolute;

        public static UrdfJoint Create(GameObject linkObject)
        {
            UrdfJointRevolute urdfJoint = linkObject.AddComponent<UrdfJointRevolute>();
            return urdfJoint;
        }

        #region Runtime

        /// <summary>
        /// Returns the current position of the joint in degrees
        /// </summary>
        /// <returns>floating point number for joint position in degrees</returns>
        public override float GetPosition()
        {
            return Vector3.SignedAngle(Vector3.right, transform.localRotation * Vector3.right, LocalAxis);
        }

        /// <summary>
        /// Sets the target position of the joint in degrees
        /// </summary>
        /// <param name="position">Target position in degrees</param>
        public override void SetPosition(float position)
        {
            transform.localRotation = Quaternion.AngleAxis(position, LocalAxis);
        }

        /// <summary>
        /// Get the transform matrix for this revolute joint
        /// </summary>
        public override Matrix4x4 GetJointTransform()
        {
            float angle = GetPosition();
            return Matrix4x4.Rotate(Quaternion.AngleAxis(angle, LocalAxis));
        }

        #endregion

        protected override void ImportJointData(UrdfJointDef joint)
        {
            base.ImportJointData(joint);
            var effort = joint.limit?.effort ?? 1e3f;
            var velocity = joint.limit?.velocity ?? 1e3f;
            var lower = joint.limit?.lower ?? float.NegativeInfinity;
            var upper = joint.limit?.upper ?? float.PositiveInfinity;

            // Store limits for IK
            LowerLimit = lower;
            UpperLimit = upper;

            axisofMotion = -joint.axis?.xyzRUF ?? -Vector3.right;
        }
    }
}