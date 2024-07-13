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


using UnityEngine;
using UrdfToolkit.Urdf;

namespace SimToolkit.ROS.Urdf
{
    public class UrdfJointFloating : UrdfJoint
    {
        public override UrdfJointType JointType => UrdfJointType.Floating;

        public static UrdfJoint Create(GameObject linkObject)
        {
            UrdfJointFloating urdfJoint = linkObject.AddComponent<UrdfJointFloating>();
            urdfJoint.unityJoint = linkObject.AddComponent<ArticulationBody>();
            return urdfJoint;
        }

#region Runtime

        public override float GetPosition()
        {
            Vector3 distanceFromAnchor = ((ArticulationBody)unityJoint).transform.localPosition ;
            return distanceFromAnchor.magnitude;
        }

#endregion

    }
}

