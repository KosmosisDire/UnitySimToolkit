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
    public class UrdfJointPrismatic : UrdfJoint
    {
        private ArticulationDrive drive;
#if UNITY_2020_1
        private float maxLinearVelocity;
#endif

        // public override UrdfJointType JointType => UrdfJointType.Prismatic;

        public static UrdfJoint Create(GameObject linkObject)
        {
            UrdfJointPrismatic urdfJoint = linkObject.AddComponent<UrdfJointPrismatic>();


            return urdfJoint;
        }

        #region Runtime

        /// <summary>
        /// Returns the current position of the joint in meters
        /// </summary>
        /// <returns>floating point number for joint position in meters</returns>
        public override float GetPosition()
        {
            return Vector3.Dot(transform.localPosition, LocalAxis);
        }

        /// <summary>
        /// Sets the target position of the joint in meters
        /// </summary>
        /// <param name="position">Target position in meters</param>
        public override void SetPosition(float position)
        {
            transform.localPosition = LocalAxis * position;
        }

        /// <summary>
        /// Get the transform matrix for this prismatic joint
        /// </summary>
        public override Matrix4x4 GetJointTransform()
        {
            float position = GetPosition();
            Vector3 translation = LocalAxis * position;
            return Matrix4x4.Translate(translation);
        }

        #endregion

        #region Import

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

            axisofMotion = joint.axis?.xyzRUF ?? Vector3.right;
        }

        #endregion

    }
}