using UnityEngine;
using System;
using System.Collections.Generic;

namespace Holowerkz_MainBoot
{
    public class CSMachine : CSIMachine
    {
        public string parentName = "";
        public string machineName = "";

        public string machineResources { get { return "MachinePath"; } }

        private Dictionary<string, CSIState> machineStates = new Dictionary<string, CSIState>();

        private CSIState _currentState = null;
        private CSIState _previousState = null;
        private CSIState _nextState = null;

        //get and set variables
        float _enteredStateTime = 0.0f;

        private float enteredStateTime
        {
            get { return _enteredStateTime; }
            set { _enteredStateTime = value; }
        }

        public void EnterController(CSIMachine machine)
        {
            //enter with the latest state machine
            if (_currentState != null) {
                _currentState.Enter(machine);
            }
        }

        public void InputController()
        {
            if (_currentState != null) {
                _currentState.Input();
            }
        }

        public void TickController()
        {
            if (_currentState != null) {
                _currentState.Tick();
            }
        }

        public bool EvaluateController()
        {
            bool retVal = true;
            if (_currentState != null) {
                _currentState.Evaluate();

                if ((_currentState != _nextState) && (_nextState != null)) {
                    SetNextState(_nextState);
                }

                //AppI_Debug.ShowMsg("Evaluate _currentState Parent: " + parentName + " Machine: " + machineName);
            } else {
                AppI_Debug.ShowMsg(string.Format("NULL _currentState evaluateController! Parent: {0}  Machine: {1} State: {2}", parentName, machineName, GetCurrentStateName()));
                retVal = false;
            }

            //AppI_Debug.ShowMsg("Evaluate _currentState State : " + getCurrentStateName() + " Last : " + getPreviousStateName());
            return retVal;
        }

        public void ExitController()
        {
            if (_currentState != null) {
                _currentState.Exit();
            }
        }

        //custom machine calls
        public void AddState(string name, CSIState state)
        {
            machineStates[name] = state;
        }

        public string GetCurrentStateName()
        {
            return GetStateName(_currentState);
        }

        public CSIState GetCurrentState()
        {
            return _currentState;
        }

        public string GetPreviousStateName()
        {
            return GetStateName(_previousState);
        }

        public CSIState GetPreviousState()
        {
            return _previousState;
        }

        public CSIState GetState(string name)
        {
            //handlers when the states returned here trying to be accessed are null
            if (machineStates.ContainsKey(name)) {
                CSIState retState = machineStates[name] as CSIState;
                if (retState == null) {
                    //error condition
                    AppI_Debug.ShowMsg("getState: " + name + " ERROR: NULL!");
                    return null;
                }
                return retState;
            }

            //error condition
            AppI_Debug.ShowMsg("getState: " + name + " ERROR:NOT FOUND!");
            return null;
        }

        public string GetStateName(CSIState state)
        {
            foreach (KeyValuePair<string, CSIState> dictEntry in machineStates)
                if (dictEntry.Value == state) {
                    return dictEntry.Key;
                }

            return null;
        }

        public Boolean SetNextStateNamed(string name)
        {
            CSIState newState = GetState(name);
            return SetNextState(newState);
        }

        private Boolean SetNextState(CSIState newState)
        {
            bool success = false;

            if (newState != null) {
                CSIState _previousState = _currentState;

                // Old state gets notified it is changing out.
                if (_previousState != null) {
                    //AppI_Debug.ShowMsg("setNextState Old:" + _previousState.ToString());
                    _previousState.Exit();
                }

                //AppI_Debug.ShowMsg("setNextState Current: " + newState.ToString());

                _currentState = newState;
                _nextState = null;

                if(_currentState != null) {
                    //Note the time at which we entered this state.             
                    _enteredStateTime = TimeHelp.GetCurrentMS();
                    _currentState.Enter(this);
                    success = true;
                }
            } else {
                AppI_Debug.ShowMsg("setNextState: ERROR:NULL STATE!");
                success = false;
            }
            return success;
        }
    }
}
