using System;
using System.Collections;
using System.Collections.Generic;

namespace Holowerkz_MainBoot
{
    public class BasicController
    {
        private Dictionary<string, CSIProc> controllerProcs = new Dictionary<string, CSIProc>();

        public string defaultProc = null;
        private float _enteredProcTime = 0.0f;

        private CSIProc _currentProc = null;
        private CSIProc _previousProc = null;
        private CSIProc _nextProc = null;

        public BasicTimer basiCTimer = null;

        protected bool updateTick = true; //always on now
        protected bool alwaysTick = false;
        public bool isPluggedIn = true;

        public float enteredProcTime()
        {
            return _enteredProcTime;
        }

        public virtual void enterController(CSIMachine machine)
        {
            //enter with the latest Proc machine
            if ((_currentProc != null) && (isPluggedIn == true))
                _currentProc.EnterProcess();
        }

        public virtual void updateController()
        {
            if ((_currentProc != null) && (isPluggedIn == true))
                _currentProc.UpdateProcess();
        }

        public virtual void evaluateController()
        {
            if ((_currentProc != null) && (isPluggedIn == true)) {
                _currentProc.EvaluateProcess();

                if ((_currentProc != _nextProc) && (_nextProc != null)) {
                    setNextProcess(_nextProc);
                }
            }
        }

        public virtual void exitController()
        {
            if ((_currentProc != null) && (isPluggedIn == true))
                _currentProc.ExitProcess();
        }

        public void SetPlug(bool isOn)
        {
            isPluggedIn = isOn;
        }

        public CSIProc getCurrentProc()
        {
            return _currentProc;
        }

        public string getCurrentProcessName()
        {
            return getProcessName(getCurrentProc());
        }

        public CSIProc getPreviousProc()
        {
            return _previousProc;
        }

        public void addProc(string key, CSIProc proc)
        {
            controllerProcs[key] = proc;
        }

        public void removeProc(string key)
        {
            if (controllerProcs.ContainsKey(key) == true) {
                controllerProcs.Remove(key);
            }
        }

        public void clearAllProcs()
        {
            controllerProcs.Clear();
        }

        public CSIProc getProc(string name)
        {
            //handlers when the Procs returned here trying to be accessed are null
            if (controllerProcs.ContainsKey(name)) {
                return controllerProcs[name] as CSIProc;
            }

            AppI_Debug.ShowMsg("ERROR! " + getCurrentProcessName() + " NO Controller for Proc: " + name);
            //bad proc
            return null;
        }

        public string getProcessName(CSIProc proc)
        {
            if (proc == null) {
                //AppI_Debug.ShowMsg("getProcessName: ERROR: PROC NULL!");
                return "NULL";
            }

            foreach (KeyValuePair<string, CSIProc> dictEntry in controllerProcs)
                if (controllerProcs[dictEntry.Key] == proc)
                    return dictEntry.Key;

            AppI_Debug.ShowMsg("getProcessName: ERROR: NOT FOUND! " + proc.GetProcessStateName());
            return "NoControllerProc";
        }

        //forced to by pass the queue
        public Boolean setNextProcNamed(string name)
        {
            Boolean retVal = false;

            CSIProc newProc = getProc(name);
            retVal = setNextProcess(newProc);
            if (newProc != null) {
                _currentProc.StopProcess();
                newProc.StartProcess();
                _currentProc = newProc;
                retVal = true;
            }

            if (retVal == false) {
                AppI_Debug.ShowMsg("Process Not Found Warning: " + name);
            }

            return retVal;
        }

        private Boolean setNextProcess(CSIProc newProc)
        {
            if (newProc == null) {
                //AppI_Debug.ShowMsg("setNextProcess: ERROR: NULL!");
                return false;
            }

            CSIProc oldProc = _currentProc;

            _previousProc = _currentProc;
            _currentProc = newProc;
            _nextProc = null;

            // Old Proc gets notified it is changing out.
            if (oldProc != null)
                oldProc.ExitProcess();

            // New Proc finds out it is coming in. 
            if (_currentProc != null)
                _currentProc.EnterProcess();

            //Note the time at which we entered this Proc.             
            _enteredProcTime = TimeHelp.GetCurrentMS();

            return true;
        }

        //SetSpeed(0);//off no ms tick
        //SetSpeed(1);//full on ms tick
        //SetSpeed(1000);//one sec delay ms
        public void SetSpeed(float tickMS)
        {
            if (tickMS == 0) {
                //off
                updateTick = false;
                alwaysTick = false;
            } else if (tickMS == 1) {
                // full on always on now bypass timer
                updateTick = true;
                alwaysTick = true;
            } else {
                // some interval
                updateTick = true;
                alwaysTick = false;
            }

            if (basiCTimer != null) {
                basiCTimer.SetSpeed(tickMS);

                if (tickMS == 0) {
                    //off
                    basiCTimer.gameObject.SetActive(false);
                } else if (tickMS == 1) {
                    //off
                    basiCTimer.gameObject.SetActive(false);
                } else {
                    //back on
                    basiCTimer.gameObject.SetActive(true);
                }
            }
        }
    }
}
