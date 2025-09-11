/*
© Siemens AG, 2017-2019
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)
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

using SimToolkit.ROS.Urdf.Importer;
using UrdfToolkit.Urdf;
using UnityEngine;
using Toolkit;

namespace SimToolkit.ROS.Urdf
{
    [ExecuteAlways]
    // [RequireComponent(typeof(ArticulationBody))]
    public abstract class UrdfJoint : MonoBehaviour
    {
        public UrdfLink parentLink;
        public UrdfLink childLink;
        public Vector3 axisofMotion;

        // Joint limits for IK
        public double LowerLimit = double.NegativeInfinity;
        public double UpperLimit = double.PositiveInfinity;
        public bool HasLimits => !double.IsInfinity(LowerLimit) && !double.IsInfinity(UpperLimit);
        public bool IsFixed => this is UrdfJointFixed;

        // Properties for IK
        public Vector3 WorldAxis => transform.TransformDirection(axisofMotion);
        public Vector3 LocalAxis => axisofMotion;

        public static UrdfJoint Create(GameObject linkObject, UrdfJointDef joint)
        {
            UrdfJoint urdfJoint = AddCorrectJointType(linkObject, joint.type);
            urdfJoint.ImportJointData(joint);
            return urdfJoint;
        }

        private static UrdfJoint AddCorrectJointType(GameObject linkObject, UrdfJointType jointType)
        {
            UrdfJoint urdfJoint = null;

            switch (jointType)
            {
                case UrdfJointType.Revolute:
                    urdfJoint = UrdfJointRevolute.Create(linkObject);
                    break;
                case UrdfJointType.Prismatic:
                    urdfJoint = UrdfJointPrismatic.Create(linkObject);
                    break;
                case UrdfJointType.Fixed:
                    urdfJoint = UrdfJointFixed.Create(linkObject);
                    break;
            }

            return urdfJoint;
        }

        /// <summary>
        /// Changes the type of the joint
        /// </summary>
        /// <param name="linkObject">Joint whose type is to be changed</param>
        /// <param name="newJointType">Type of the new joint</param>
        public static void ChangeJointType(GameObject linkObject, UrdfJointType newJointType)
        {
            linkObject.DestroyImmediateIfExists<UrdfJoint>();
            linkObject.DestroyImmediateIfExists<PrismaticJointLimitsManager>();
            linkObject.DestroyImmediateIfExists<ArticulationBody>();
            AddCorrectJointType(linkObject, newJointType);
        }

        #region Runtime

        public abstract float GetPosition();
        public abstract void SetPosition(float position);
        public virtual void SetPositionClamped(float position)
        {
            if (HasLimits)
            {
                position = Mathf.Clamp(position, (float)LowerLimit, (float)UpperLimit);
            }
            SetPosition(position);
        }

        // Get the joint's contribution to transform hierarchy
        public virtual Matrix4x4 GetJointTransform()
        {
            return transform.localToWorldMatrix;
        }

        #endregion

        #region Import Helpers

        protected virtual void ImportJointData(UrdfJointDef joint)
        {
            // Import limits if they exist
            if (joint.limit != null)
            {
                LowerLimit = joint.limit.Value.lower;
                UpperLimit = joint.limit.Value.upper;
            }
        }

        protected virtual void AdjustMovement(UrdfJointDef joint) { }

        #endregion
    }
}