using Holowerkz_v1_App_ShareCrafter;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Holowerkz_v1_Lib 
{
    public class OperationManager : MonoBehaviour
    {
        private Operations_APIUser _opsAPIUser = null;
        public Operations_APIUser APIUser => _opsAPIUser;

        private Operations_APIData _opsAPIData = null;
        public Operations_APIData APIData => _opsAPIData;

        private List<OperationState> operationsRunning = new List<OperationState>();

        [SerializeField] private bool loadTestData = false;
        public bool LoadTestData => loadTestData;

        private void Awake()
        {
            OperationHandle.SetOpMgr(this);
            Initialize();
        }

        //private void Update()
        //{
        //    Debug.Log($"OperationManager Running Ops {operationsRunning.Count}");
        //}

        public void Initialize()
        {
            _opsAPIUser = GetComponent<Operations_APIUser>();
            _opsAPIData = GetComponent<Operations_APIData>();

            _opsAPIUser.Initialize();
            _opsAPIData.Initialize();        
        }

        public OperationState CreateOperation()
        {
            OperationState opState = new OperationState();
            opState.Queue();
            operationsRunning.Add(opState);
            return opState;
        }

        public void StartOperation(OperationState opState)
        {
            opState.Run();
            StartCoroutine(opState.Coroutine);
        }

        public OperationState StartOperation(IEnumerator co)
        {
            OperationState opState = CreateOperation();
            opState.SetCoroutine(co);
            OperationHandle.OpMgr.StartOperation(opState);
            return opState;
        }

        public void CompleteOperation(OperationState removeOp)
        {
            foreach(OperationState op in operationsRunning) 
            {
                if(op == removeOp) 
                {
                    removeOp = op;
                } 
            }
            
            if(removeOp!=null) 
            {
                operationsRunning.Remove(removeOp);
            }
            else 
            {
                Debug.Log("OperationManager CompleteOperation coroutine not found");
            }
        }

        public void CompleteOperation(IEnumerator co)
        {
            OperationState removeOp = null;
            foreach(OperationState op in operationsRunning) 
            {
                if(op.Coroutine == co) 
                {
                    removeOp = op;
                    break;
                } 
            }

            if(removeOp!=null) 
            {
                operationsRunning.Remove(removeOp);
            }
            else 
            {
                Debug.Log("OperationManager CompleteOperation coroutine not found");
            }
        }
    }

    public class Operation : MonoBehaviour
    {
        private OperationState _operationalState = new OperationState();

        public OperationStatus OpState => _operationalState.OpState;

        public delegate void OperationCompletionHandler(bool aborted);
        public event OperationCompletionHandler _OnCompleted;

        public void Create(IEnumerator coroutine)
        {
            _operationalState.SetCoroutine(coroutine);
            _operationalState._OnCompleted += TaskCompleted;
        }

        public void Begin()
        {
            _operationalState?.Run();
        }

        public void End()
        {
            _operationalState?.Complete();
        }

        public void Pause()
        {
             _operationalState?.Pause();
        }

        public void Resume()
        {
             _operationalState?.Resume();
        }

        private void TaskCompleted(bool aborted)
        {
            _OnCompleted?.Invoke(aborted);
        }
    }

       public class OperationState
    {
        public OperationStatus OpState = OperationStatus.Inactive;

        public delegate void OperationCompletionHandler(bool aborted);
        public event OperationCompletionHandler _OnCompleted;
        private IEnumerator _coroutine;
        public IEnumerator Coroutine => _coroutine;

        public void SetCoroutine(IEnumerator coroutine)
        {
            _coroutine = coroutine;
        }

        public void Queue() { OpState = OperationStatus.Queued; }
        public void Run() { OpState = OperationStatus.Running; }
        public void Pause() { OpState = OperationStatus.Paused; }
        public void Resume() { OpState = OperationStatus.Resuming; }
        public void Complete() { OpState = OperationStatus.Completed; OperationHandle.OpMgr.CompleteOperation(this);}
        public void Abort() { OpState = OperationStatus.Aborted; OperationHandle.OpMgr.CompleteOperation(this);}
        public void Dequeue() { OpState = OperationStatus.Inactive; }

        public bool IsOperationalState(OperationStatus checkState) => (OpState == checkState);
        public bool IsOperationalReady() => (OperationStatus.Queued == OpState || OperationStatus.Running == OpState);
    }

    public enum OperationStatus 
    {
        Inactive = 0,
        Queued,
        Running,
        Paused,
        Resuming,
        Completed,
        Aborted,
    }
}
