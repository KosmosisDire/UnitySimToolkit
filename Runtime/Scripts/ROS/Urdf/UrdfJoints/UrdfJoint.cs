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

    [RequireComponent(typeof(ArticulationBody))]
    public abstract class UrdfJoint : MonoBehaviour
    {
        public UrdfLink parentLink;
        public UrdfLink childLink;

        public int xAxis = 0;
        protected ArticulationBody unityJoint;
        protected Vector3 axisofMotion;
        public string jointName;
        new public string name => jointName;

        public abstract UrdfJointType JointType { get; } // Clear out syntax
        public bool IsRevoluteOrContinuous => JointType == UrdfJointType.Revolute || JointType == UrdfJointType.Revolute;
        public double EffortLimit = 1e3;
        public double VelocityLimit = 1e3;
        public ArticulationBody ArticulationBody => unityJoint;

        protected int defaultDamping = 0;
        protected int defaultFriction = 0;

        public static UrdfJoint Create(GameObject linkObject, UrdfJointDef joint)
        {
            UrdfJoint urdfJoint = AddCorrectJointType(linkObject, joint.type);
            urdfJoint.jointName = joint.name;
            urdfJoint.ImportJointData(joint);
            return urdfJoint;
        }

        private static UrdfJoint AddCorrectJointType(GameObject linkObject, UrdfJointType jointType)
        {
            UrdfJoint urdfJoint = null;

            switch (jointType)
            {
                case UrdfJointType.Fixed:
                    urdfJoint = UrdfJointFixed.Create(linkObject);
                    break;
                case UrdfJointType.Continuous:
                    urdfJoint = UrdfJointContinuous.Create(linkObject);
                    break;
                case UrdfJointType.Revolute:
                    urdfJoint = UrdfJointRevolute.Create(linkObject);
                    break;
                case UrdfJointType.Floating:
                    urdfJoint = UrdfJointFloating.Create(linkObject);
                    break;
                case UrdfJointType.Prismatic:
                    urdfJoint = UrdfJointPrismatic.Create(linkObject);
                    break;
                case UrdfJointType.Planar:
                    urdfJoint = UrdfJointPlanar.Create(linkObject);
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

        public void Awake()
        {
            unityJoint = GetComponent<ArticulationBody>();
        }

        public virtual float GetPosition()
        {
            return 0;
        }

        public virtual float GetVelocity()
        {
            return 0;
        }

        public virtual float GetEffort()
        {
            return 0;
        }

        public void UpdateJointState(float deltaState)
        {
            OnUpdateJointState(deltaState);
        }
        protected virtual void OnUpdateJointState(float deltaState) { }

        #endregion

        #region Import Helpers

        public static UrdfJointType GetJointType(string jointType)
        {
            switch (jointType)
            {
                case "fixed":
                    return UrdfJointType.Fixed;
                case "continuous":
                    return UrdfJointType.Continuous;
                case "revolute":
                    return UrdfJointType.Revolute;
                case "floating":
                    return UrdfJointType.Floating;
                case "prismatic":
                    return UrdfJointType.Prismatic;
                case "planar":
                    return UrdfJointType.Planar;
                default:
                    return UrdfJointType.Fixed;
            }
        }

        protected virtual void ImportJointData(UrdfJointDef joint) { }

        protected virtual void AdjustMovement(UrdfJointDef joint) { }
        
        public virtual bool AreLimitsCorrect()
        {
            return true; // limits aren't needed
        }

        protected void SetDynamics(UrdfDynamicsDef dynamics)
        {
            if (unityJoint == null)
            {
                unityJoint = GetComponent<ArticulationBody>();
            }

            float damping = double.IsNaN(dynamics.damping) ? defaultDamping : (float)dynamics.damping;
            unityJoint.linearDamping = damping;
            unityJoint.angularDamping = damping;
            unityJoint.jointFriction = double.IsNaN(dynamics.friction) ? defaultFriction : (float)dynamics.friction;

        }

        #endregion
    }
}

