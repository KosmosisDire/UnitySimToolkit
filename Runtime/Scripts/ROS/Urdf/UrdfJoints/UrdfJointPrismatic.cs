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

                public override UrdfJointType JointType => UrdfJointType.Prismatic;

                public static UrdfJoint Create(GameObject linkObject)
                {
                        UrdfJointPrismatic urdfJoint = linkObject.AddComponent<UrdfJointPrismatic>();
                        urdfJoint.unityJoint = linkObject.GetComponent<ArticulationBody>();
                        urdfJoint.unityJoint.jointType = ArticulationJointType.PrismaticJoint;

                        return urdfJoint;
                }

                #region Runtime

                /// <summary>
                /// Returns the current position of the joint in meters
                /// </summary>
                /// <returns>floating point number for joint position in meters</returns>
                public override float GetPosition()
                {
                        return unityJoint.jointPosition[xAxis];
                }

                /// <summary>
                /// Returns the current velocity of joint in meters per second
                /// </summary>
                /// <returns>floating point for joint velocity in meters per second</returns>
                public override float GetVelocity()
                {
                        return unityJoint.jointVelocity[xAxis];
                }

                /// <summary>
                /// Returns current joint torque in N
                /// </summary>
                /// <returns>floating point in N</returns>
                public override float GetEffort()
                {
                        return unityJoint.jointForce[xAxis];
                }

                /// <summary>
                /// Rotates the joint by deltaState m
                /// </summary>
                /// <param name="deltaState">amount in m by which joint needs to be rotated</param>
                protected override void OnUpdateJointState(float deltaState)
                {
                        ArticulationDrive drive = unityJoint.xDrive;
                        drive.target += deltaState;
                        unityJoint.xDrive = drive;
                }

                #endregion

                #region Import

                protected override void ImportJointData(UrdfJointDef joint)
                {
                        AdjustMovement(joint);
                        if (joint.dynamics != null) SetDynamics(joint.dynamics.Value);
                }

                /// <summary>
                /// Reads axis joint information and rotation to the articulation body to produce the required motion
                /// </summary>
                /// <param name="joint">Structure containing joint information</param>
                protected override void AdjustMovement(UrdfJointDef joint) // Test this function
                {
                        var effort = joint.limit?.effort ?? 0f;
                        var velocity = joint.limit?.velocity ?? 0f;
                        var lower = joint.limit?.lower ?? 0f;
                        var upper = joint.limit?.upper ?? 0f;

                        axisofMotion = joint.axis?.xyzRUF.ToUnity() ?? Vector3.right;
                        unityJoint.linearLockX = (joint.limit != null) ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
                        unityJoint.linearLockY = ArticulationDofLock.LockedMotion;
                        unityJoint.linearLockZ = ArticulationDofLock.LockedMotion;

                        Quaternion Motion = new Quaternion();
                        Motion.SetFromToRotation(new Vector3(1, 0, 0), axisofMotion);
                        unityJoint.anchorRotation = Motion;

                        if (joint.limit != null)
                        {
                                ArticulationDrive drive = unityJoint.xDrive;
                                drive.upperLimit = upper;
                                drive.lowerLimit = lower;
                                drive.forceLimit = effort;
                                unityJoint.maxLinearVelocity = velocity;
                                unityJoint.xDrive = drive;
                        }
                }

                #endregion


                #region Export

                public override bool AreLimitsCorrect()
                {
                        ArticulationBody joint = GetComponent<ArticulationBody>();
                        return joint.linearLockX == ArticulationDofLock.LimitedMotion && joint.xDrive.lowerLimit < joint.xDrive.upperLimit;
                }

                #endregion
        }
}
