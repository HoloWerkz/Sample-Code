using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Holomeeting.HandSharing;
using Photon.Pun;
using Microsoft.MixedReality.Toolkit.Input;

namespace Holomeeting.HandSharing
{
    public class HandInit : MonoBehaviour
    {
        private HandJoints handJoints = null;
        Dictionary<TrackedHandJoint, DefaultAngleData> rotations = new Dictionary<TrackedHandJoint, DefaultAngleData>();
        public DefaultAngleData[] jointClamps = new DefaultAngleData[25];

        void OnEnable()
        {
            handJoints = GetComponent<HandJoints>();
            RefreshData();
        }

        void Update()
        {
#if UNITY_EDITOR
            RefreshData();
#endif
        }

        /// <summary>
        /// Refresh the data for editor control
        /// </summary>
        private void RefreshData()
        {
            rotations.Clear();

            //editor tool find the HandJoint on this gameobject populate it
            if (handJoints != null) {
                //add the wrist skipping the palm
                rotations.Add(TrackedHandJoint.Wrist, jointClamps[0]);
                int jointID = (int)TrackedHandJoint.Palm;
                for (int n = 1; n < jointClamps.Length; ++n) {
                    //thumb
                    rotations.Add((TrackedHandJoint)(++jointID), jointClamps[n]);
                }

                Debug.Assert(rotations.Count == jointClamps.Length);

                handJoints.InitializeData(rotations);
            }
        }
    }
}


