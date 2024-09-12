using Holomeeting.Utility;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holomeeting.HandSharing
{
    [Serializable]
    public class DefaultAngleData
    {
        //to see a name in the editor
        public string idName = string.Empty; 
        public Vector3 defaultValue = Vector3.zero;
        //clamping not implemented
        //public Vector3 minAngle = Vector3.zero;
        //public Vector3 maxAngle = Vector3.zero;

        public DefaultAngleData()
        {
        }

        public DefaultAngleData(Vector3 def, Vector3 min, Vector3 max)
        {
            defaultValue = def;
            //minAngle = min;
            //maxAngle = max;
        }
    }

    public class HandJoints : MonoBehaviourPunCallbacks
    {
        //enums for MS TrackedHandJoint
        //None = 0,     NA
        //Wrist,        
        //Palm,         NA
        //ThumbMetacarpalJoint,
        //ThumbProximalJoint,
        //ThumbDistalJoint,
        //ThumbTip,
        //IndexMetacarpal,
        //IndexKnuckle,
        //IndexMiddleJoint,
        //IndexDistalJoint,
        //IndexTip,
        //MiddleMetacarpal,
        //MiddleKnuckle,
        //MiddleMiddleJoint,
        //MiddleDistalJoint,
        //MiddleTip,
        //RingMetacarpal,
        //RingKnuckle,
        //RingMiddleJoint,
        //RingDistalJoint,
        //RingTip,
        //PinkyMetacarpal,
        //PinkyKnuckle,
        //PinkyMiddleJoint,
        //PinkyDistalJoint,
        //PinkyTip
        [SerializeField]
        public GameObject[] jointObjects = null;

        //list of the default rotations and clamps
        private Dictionary<TrackedHandJoint, DefaultAngleData> defaultRotationData = new Dictionary<TrackedHandJoint, DefaultAngleData>();

        public float InterpolatePointLerpSpeed = 25.0f;

		[SerializeField]
		private bool _isOwnerHandVisible = false;
        // Visible state of the hand
        [SerializeField]
        private HandStateFading _handStateFading = null;
        // Orientation of the Hand
        [SerializeField]
        private Handedness Hand = Handedness.Left;
		
        [Space]

        //the ids of the valid joints
        private TrackedHandJoint[] _jointNameListArray = new TrackedHandJoint[0];

        private IMixedRealityHand _mixedRealityHand = null;

        private Vector3 _rootPos;
        private Vector3 _rootRot;
        private short[] _rotArrayShorts = new short[4 * ((int)TrackedHandJoint.PinkyTip + 1)];

        private bool _isShow = false;
        private bool _isForceReset = true;
        private int RPCRuntimeUPDShortcut = 0;

        public bool isShow { get; private set; }

        //set the axis for our rotation
        public Vector3 axisRot = new Vector3(0,90,180);

        //counting frames
        private float systemFPS = 60.0f;
        public int localTargetFPS = 30;
        public int clientTargetFPS = 30;
        public int lerpTargetFPS = 30;

        //counting frames
        private int localCounterFrames = 0;
        //local update frame rate
        private int localUpdateFrameRate = 2;
        //send to client update rate
        private int clientSendFrameRate = 12;

        //client side frame counter
        private int clientCounterFrames = 0;
        private int clientUpdateFrameRate = 2;

        //float converters
        private const int FRACTIONAL_BITS = 5;

        // Start is called before the first frame update
        void Awake()
        {
            RPCRuntimeUPDShortcut = UtilityExtentions.GetRPCShortcut("RPCRuntimeReceiveData");

            if (RootTransform.root != null)
                transform.SetParent(RootTransform.root.transform);

            var tmp = (TrackedHandJoint[])System.Enum.GetValues(typeof(TrackedHandJoint));
            _jointNameListArray = new TrackedHandJoint[tmp.Length - 2];
            for (int i = 0, j = 0; i < tmp.Length; i++) {
                if ((i != (int)TrackedHandJoint.None) && (i != (int)TrackedHandJoint.Palm))
                    _jointNameListArray[j++] = tmp[i];
            }

            _mixedRealityHand = HandJointUtils.FindHand(Hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // init the fade state
            setMeshEnabled(false);
            // force to hide the mesh at the start
            _handStateFading.SetState(HandStateFading.FadeState.Hide);

            Debug.Assert(localTargetFPS != 0);
            Debug.Assert(clientTargetFPS != 0);

            localUpdateFrameRate = (int)(systemFPS / (float)localTargetFPS);
            clientSendFrameRate = (int)(systemFPS / (float)clientTargetFPS);
            clientUpdateFrameRate = (int)(systemFPS / (float)lerpTargetFPS);

            if(localUpdateFrameRate==0||clientSendFrameRate==0||clientUpdateFrameRate==0) {
                //prevent divide by zero on the modulus divs below
                Debug.Log("FATAL Div by Zero! Local: " + localUpdateFrameRate.ToString() + " Send: " + clientSendFrameRate.ToString() + " Client: " + clientUpdateFrameRate.ToString());
            }
        }

        void Update()
        {
            if (!photonView.IsMine) {
                if (_isShow) {
                    if (((clientCounterFrames%clientUpdateFrameRate) == 0)) {
                        //now the wrist. lerp motion and rotation
                        GameObject wrist = GetJointObject(TrackedHandJoint.Wrist);
                        float lerpTime = Mathf.Clamp(InterpolatePointLerpSpeed * Time.smoothDeltaTime, 0.33f, 1.0f);
                        wrist.transform.localPosition = Vector3.Lerp(wrist.transform.localPosition, _rootPos, lerpTime);
                        Vector3 rotV = wrist.transform.localEulerAngles;
                        wrist.transform.localEulerAngles = Vector3.Lerp(rotV, _rootRot.Normalise(rotV - new Vector3(180, 180, 180), rotV + new Vector3(180, 180, 180)), lerpTime);

                        GameObject objectJoint;
                        float x, y, z, w;
                        for (int key = 0; key < _jointNameListArray.Length; key++) {
                            int index = (int)_jointNameListArray[key];
                            if (index >= 0 && index < jointObjects.Length) {
                                if (index == (int)TrackedHandJoint.Wrist)
                                    continue;
                                objectJoint = jointObjects[index];
                                if (objectJoint != null) {
                                    int rIdx = (int)index * 4;
                                    x = (float)ShortToFloat(_rotArrayShorts[rIdx + 0]);
                                    y = (float)ShortToFloat(_rotArrayShorts[rIdx + 1]);
                                    z = (float)ShortToFloat(_rotArrayShorts[rIdx + 2]);
                                    w = (float)ShortToFloat(_rotArrayShorts[rIdx + 3]);
                                    //lerp
                                    if(x==0&&y==0&&z==0&&w==0) {
                                        //zero quaternions are invalid
                                        //Debug.Log("Zero Quat: x==0&&y==0&&z==0&&w==0");
                                        continue;
                                    }

                                    Quaternion rotQ = objectJoint.transform.localRotation;
                                    objectJoint.transform.localRotation = Quaternion.Lerp(rotQ, new Quaternion(x, y, z, w).normalized, lerpTime);
                                }
                            }
                        }
                        clientCounterFrames = 0;
                    }
                    ++clientCounterFrames;
                }
            } else {
                bool isHandExist = HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Hand, out MixedRealityPose pose);
                if (isHandExist) {
                    if (_mixedRealityHand == null)
                        _mixedRealityHand = HandJointUtils.FindHand(Hand);                    
                    if(((localCounterFrames%localUpdateFrameRate)==0)) {
                        SetJoint(TrackedHandJoint.Wrist, true);
                        // Enable the mesh if its disabled
                        if (!_isShow) photonView.RPC("setMeshEnabled", RpcTarget.All, true);
                        // Set each of the joints
                        int count = _jointNameListArray.Length;
                        for (int i = 0; i < count; i++) {
                            if (_jointNameListArray[i] != TrackedHandJoint.Wrist)
                                    SetJoint(_jointNameListArray[i], false);
                            }
                        }
                        if((localCounterFrames%clientSendFrameRate)==0) {
                            // Send the client local joints
                            GameObject wrist = GetJointObject(TrackedHandJoint.Wrist);
                            photonView.SendToAllUnReliableRPC(RPCRuntimeUPDShortcut, ReceiverGroup.Others, wrist.transform.localPosition, wrist.transform.localRotation, _rotArrayShorts);
                            localCounterFrames = 0;
                        }
                }
                else if (!isHandExist)  {
                    // Disable the mesh if the Hand Wrist doesn't exist
                    _mixedRealityHand = null;
                    if (_isShow) photonView.RPC("setMeshEnabled", RpcTarget.All, false);
                }
                //always run the counter so its ready to send on first frame when the hand exists
                ++localCounterFrames;
            }
        }

         /// <summary>
        /// Converts a float value to a short
        /// </summary>
        /// <param name="input">Float value to convert</param>
        /// <returns>Short value for the float</returns>
        private short FloatToShort(float input)
        {
            return (short)(input * (1 << FRACTIONAL_BITS));
        }

        /// <summary>
        /// Converts a short value to float
        /// </summary>
        /// <param name="input">Short value to convert</param>
        /// <returns>Float value for the short</returns>
        private float ShortToFloat(short input)
        {
            return ((float)input / (float)(1 << FRACTIONAL_BITS));
        }

        /// <summary>
        /// Set the initial rotations. Data driven from inspector
        /// </summary>
        /// <param name="jointClamps">Dictionary of TrackedHandJoints to default data object</param>
        /// <returns>The rotation</returns>
        public void InitializeData(Dictionary<TrackedHandJoint, DefaultAngleData> jointClamps)
        {
            //data we dont touch
            defaultRotationData.Clear();
            foreach(KeyValuePair<TrackedHandJoint, DefaultAngleData> kvp  in jointClamps) {
                defaultRotationData.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Get the game object that represents the joint transforms
        /// <param name="joint">Joint to fetch</param>
        /// </summary>
        public GameObject GetJointObject(TrackedHandJoint joint)
        {
            GameObject retVal = null;
            int index = (int)joint;
            if((index>=0 && index<jointObjects.Length)) {
                retVal = jointObjects[index];
            }

            if(retVal == null) {
                Debug.Log("Failed to get Joint Object" + " " + Hand.ToString() + " " + joint.ToString() );
            }

            return retVal;
        }
        
        /// <summary>
        /// Get the default rotation data
        /// </summary>
        /// <param name="joint">Joint type</param>
        /// <returns>The rotation</returns>
        public Vector3 GetJointData(TrackedHandJoint joint)
        {
            if(defaultRotationData.ContainsKey(joint)) {
                return defaultRotationData[joint].defaultValue;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Set a specific joint rotation for the working set
        /// </summary>
        /// <param name="joint">Joint type</param>
        /// <param name="rot">Set the rotation</param>
        /// <returns></returns>
        public void SetJointData(TrackedHandJoint joint, Vector3 rot)
        {
            if(defaultRotationData.ContainsKey(joint)) {
                defaultRotationData[joint].defaultValue = rot;
            }
        }

        /// <summary>
        /// Set the joint rotation or position (optional) based on the MRTK joint position
        /// </summary>
        /// <param name="sensorJoint">Joint type</param>
        /// <param name="movePosition">Set the position</param>
        /// <returns></returns>
        bool SetJoint(TrackedHandJoint sensorJoint, bool movePosition = false)
        {
            TrackedHandJoint prev = sensorJoint - 1;
            if ((sensorJoint == TrackedHandJoint.IndexMetacarpal) || (sensorJoint == TrackedHandJoint.MiddleMetacarpal) || (sensorJoint == TrackedHandJoint.PinkyMetacarpal) || (sensorJoint == TrackedHandJoint.RingMetacarpal) || (sensorJoint == TrackedHandJoint.ThumbMetacarpalJoint))
                prev = TrackedHandJoint.Wrist;

            if (_mixedRealityHand.TryGetJoint(sensorJoint, out MixedRealityPose pose) && 
                defaultRotationData.ContainsKey(sensorJoint) && (_mixedRealityHand.TryGetJoint(prev, out MixedRealityPose poseParent)||sensorJoint == TrackedHandJoint.Wrist)) {

                int index = (int)sensorJoint;
                if (index >= 0 && index < jointObjects.Length) {
                    GameObject objectJoint = jointObjects[index];
                    Vector3 rot = Vector3.zero;
                    //no parent for the wrist
                    if(poseParent!=null) {
                        rot = (Quaternion.Inverse(poseParent.Rotation*Quaternion.Euler(axisRot)) * pose.Rotation*Quaternion.Euler(axisRot)).eulerAngles;
                    }
                   
                    if (movePosition && sensorJoint == TrackedHandJoint.Wrist) {
                        //process the wrist as world for stability. note its default is its world as true if no parents
                        objectJoint.transform.position = pose.Position;
                        objectJoint.transform.rotation = pose.Rotation * Quaternion.Euler(defaultRotationData[sensorJoint].defaultValue);
                    } else {
                        //these thumb joints are rotated
                        if (sensorJoint == TrackedHandJoint.ThumbProximalJoint || sensorJoint == TrackedHandJoint.ThumbDistalJoint) {
                            objectJoint.transform.localRotation = Quaternion.Euler(rot.x, -rot.z, -rot.y) * Quaternion.Euler(defaultRotationData[sensorJoint].defaultValue);
                        } else {
                            //this tracks the other joints but the wrist
                             objectJoint.transform.localRotation = Quaternion.Euler(rot) * Quaternion.Euler(defaultRotationData[sensorJoint].defaultValue);
                        }
                    }

                    DefaultAngleData data = defaultRotationData[sensorJoint];
                    int rIdx = (int)sensorJoint * 4;
                    _rotArrayShorts[rIdx + 0] = FloatToShort(objectJoint.transform.localRotation.x);
                    _rotArrayShorts[rIdx + 1] = FloatToShort(objectJoint.transform.localRotation.y);
                    _rotArrayShorts[rIdx + 2] = FloatToShort(objectJoint.transform.localRotation.z);
                    _rotArrayShorts[rIdx + 3] = FloatToShort(objectJoint.transform.localRotation.w);
                    
                    if (movePosition && sensorJoint != TrackedHandJoint.Wrist) {
                        objectJoint.transform.position = pose.Position;
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Servcer callback
        /// </summary>
        /// <param name="newPlayer">Player object</param>
        /// <returns></returns>
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            photonView.RPC("setMeshEnabled", newPlayer, _isShow);
        }

        /// <summary>
        /// Remote call to populate data
        /// </summary>
        /// <param name="rootPos">Root pos</param>
        /// <param name="rootRot">Root rotation</param>
        /// <param name="rotations">Floating rotations</param>
        /// <returns></returns>
        [PunRPC]
        void RPCRuntimeReceiveData(Vector3 rootPos, Quaternion rootRot, short[] rotations)
        {
            _rootPos = rootPos;
            _rootRot = rootRot.eulerAngles;
            _rotArrayShorts = rotations;

            if (_isForceReset)
            {
                _isForceReset = false;
                _handStateFading.isVisible = true;
              
                GameObject objectJoint;
                float x, y, z, w;
                for (int key = 0; key < _jointNameListArray.Length; key++) {
                    objectJoint = jointObjects[key];
                    if (objectJoint != null) {
                        int rIdx = (int)key * 4;
                        x = (float)ShortToFloat(_rotArrayShorts[rIdx + 0]);
                        y = (float)ShortToFloat(_rotArrayShorts[rIdx + 1]);
                        z = (float)ShortToFloat(_rotArrayShorts[rIdx + 2]);
                        w = (float)ShortToFloat(_rotArrayShorts[rIdx + 3]);
                        objectJoint.transform.localRotation = new Quaternion(x, y, z, w);
                    }
                }
            }
        }

        /// <summary>
        /// Enable or Diable the mesh based on the information received by Photon
        /// </summary>
        /// <param name="enable">Enable = true, Disable = false</param>
        [PunRPC]
        void setMeshEnabled(bool enable)
        {
            _isShow = enable;
            _isForceReset = _isShow;
            if (photonView.IsMine)
                _handStateFading.isVisible = _isOwnerHandVisible;
            else
                _handStateFading.isVisible = enable;
        }

        /// <summary>
        /// Get the name list
        /// </summary>
        public TrackedHandJoint[] GetNameList()
        {
            return _jointNameListArray;
        }

        /// <summary>
        /// Get a name in the list from the index
        /// </summary>
        /// <param name="index">0 to length of name list</param>
        public TrackedHandJoint GetJoint(int index)
        {
            if(index>=0&&index<_jointNameListArray.Length) {
                return _jointNameListArray[index];
            }
            return TrackedHandJoint.None;
        }
        /// <summary>
        /// Get the handedness
        /// </summary>
        public Handedness GetHandedness()
        {
            return Hand;
        }
    }
}