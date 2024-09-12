using UnityEngine;
using UnityEngine.SceneManagement;
using Holowerkz_MainBoot;

//TODOMJB fix the audio source music is from unite demo
namespace Holowerkz_App_HumorIsland
{
    public class Procs_BaseActController
    {
        public static string _Boot = "_Boot";
        public static string _Main = "_Main";
        public static string _Diorama = "_Diorama";
        public static string _Credits = "_Credits";
        public static string _Exit = "_Exit";
    }

    public class BaseActController : BasicAct
    {
        [SerializeField]
        private string currentSceneName = string.Empty;

        [SerializeField]
        private GameObject planeObj = null;
        [SerializeField]
        private GameObject terrainObj = null;
        [SerializeField]
        private GameObject sunObj = null;

        void Awake()
        {
            MainRoot.SetTerrainLot(FindObjectOfType<TerrainLot>());
            MainRoot.GetUIMgr().HudQuads = FindObjectOfType<HUDQuadDiag>();
            MainRoot.GetUIMgr().HudQuads.gameObject.SetActive(false);
            MainRoot.GetUIMgr().HUDSpares = FindObjectOfType<HUDSpareDiag>();
            MainRoot.GetUIMgr().HUDSpares.gameObject.SetActive(false);
            MainRoot.GetUIMgr().HudCamPegs = FindObjectOfType<HUDCamPeg>();
            MainRoot.GetUIMgr().HudCamPegs.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            BuildControllerProcess();
            MainRoot.GetController().MainBus().StartListening("StartGame", StartGame);
        }

        void OnDisable()
        {
            MainRoot.GetController().MainBus().StopListening("StartGame", StartGame);
        }

        void Update()
        {
            if (libController != null) {
                libController.Tick();
            }
        }

        #region Controller
        public LibController GetLibController()
        {
            return libController;
        }

        public void ControllerStart()
        {
            libController.SetProc(Procs_BaseActController._Boot);
        }

        public void ControllerRestart()
        {
            libController.SetProc(Procs_BaseActController._Main);
        }

        public void ControllerInterrupt()
        {
            libController.SetProc(Procs_BaseActController._Credits);
        }

        public void ControllerStop()
        {
            libController.SetProc(Procs_BaseActController._Exit);
        }
        #endregion

        #region Interface
        public void StartGame(string state)
        {
           ControllerStart();
        }
        #endregion

        #region Process
        public void BuildControllerProcess()
        {
            libController = new LibController(this.gameObject.name, true);
            libController.basiCTimer.transform.SetParent(this.gameObject.transform);

            //build the man main controllers that control games apps and what ever
            libController.addProc(Procs_BaseActController._Boot, new proc_Boot(this));
            libController.addProc(Procs_BaseActController._Main, new proc_Main(this));
            libController.addProc(Procs_BaseActController._Diorama, new proc_Diorama(this));
            libController.addProc(Procs_BaseActController._Credits, new proc_Credits(this));
            libController.addProc(Procs_BaseActController._Exit, new proc_Exit(this));
        }
        #endregion

        #region proc_Boot
        public class proc_Boot : CSProc
        {
            public proc_Boot(BasicAct act)
            {
                Create(act);

                enterCall = thisEnter;
                updateCall = thisUpdate;
                evalCall = thisEvaluate;
                exitCall = thisExit;
            }

            protected void thisEnter()
            {
                AppI_Debug.ShowMsg("BaseAct Controller proc_Boot thisEnter");

                SceneManager.sceneUnloaded += OnPlanetSceneUnloaded;

                ((BaseActController)actBase).planeObj.SetActive(true);
                ((BaseActController)actBase).terrainObj.SetActive(true);
                ((BaseActController)actBase).sunObj.SetActive(false);

                SceneManager.UnloadSceneAsync(MainRoot.GetAppConf().homeToLoad);
            }

            protected void thisUpdate()
            {
            }

            protected void thisEvaluate()
            {
            }

            protected void thisExit()
            {
                 SceneManager.LoadSceneAsync(((BaseActController)actBase).currentSceneName, LoadSceneMode.Additive);
            }

            void OnPlanetSceneUnloaded(Scene scene)
            {
                SceneManager.sceneUnloaded -= OnPlanetSceneUnloaded;
                SceneManager.sceneLoaded += OnPlanetSceneLoaded;
                //GetParentalController().SetProc(Procs_BaseActController._Main);
                SceneManager.LoadSceneAsync(MainRoot.GetAppConf().worldToLoad, LoadSceneMode.Additive);
            }

            void OnPlanetSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnPlanetSceneLoaded;
                SceneManager.sceneLoaded += OnActSceneLoaded;
                GetParentalController().SetProc(Procs_BaseActController._Main);
            }

            void OnActSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnActSceneLoaded;
                MainRoot.GetAppConf().LoadAppCamera();
            }
        }
        #endregion

        #region proc_Main
        public class proc_Main : CSProc
        {
            public proc_Main(BasicAct act)
            {
                Create(act);

                enterCall = thisEnter;
                updateCall = thisUpdate;
                evalCall = thisEvaluate;
                exitCall = thisExit;
            }

            protected void thisEnter()
            {
                AppI_Debug.ShowMsg("BaseAct Controller proc_Main thisEnter");

                MainRoot.GetUIMgr().HudCamPegs.gameObject.SetActive(true);
                MainRoot.GetUIMgr().HudQuads.gameObject.SetActive(true);
                MainRoot.GetUIMgr().HUDSpares.gameObject.SetActive(true);
            }

            protected void thisUpdate()
            {
            }

            protected void thisEvaluate()
            {
            }

            protected void thisExit()
            {
            }
        }
        #endregion

        #region proc_Diorama
        public class proc_Diorama : CSProc
        {
            public proc_Diorama(BasicAct act)
            {
                Create(act);

                enterCall = thisEnter;
                updateCall = thisUpdate;
                evalCall = thisEvaluate;
                exitCall = thisExit;
            }

            protected void thisEnter()
            {
                AppI_Debug.ShowMsg("BaseAct Controller proc_Diorama thisEnter");
            }

            protected void thisUpdate()
            {
            }

            protected void thisEvaluate()
            {
            }

            protected void thisExit()
            {
            }
        }
        #endregion

        #region proc_Credits
        public class proc_Credits : CSProc
        {
            public proc_Credits(BasicAct act)
            {
                Create(act);

                enterCall = thisEnter;
                updateCall = thisUpdate;
                evalCall = thisEvaluate;
                exitCall = thisExit;
            }

            protected void thisEnter()
            {
                AppI_Debug.ShowMsg("BaseAct Controller proc_Credits thisEnter");
            }

            protected void thisUpdate()
            {
            }

            protected void thisEvaluate()
            {
            }

            protected void thisExit()
            {
            }
        }
        #endregion

        #region proc_Exit
        public class proc_Exit : CSProc
        {
            public proc_Exit(BasicAct act)
            {
                Create(act);

                enterCall = thisEnter;
                updateCall = thisUpdate;
                evalCall = thisEvaluate;
                exitCall = thisExit;
            }

            protected void thisEnter()
            {
                AppI_Debug.ShowMsg("BaseAct Controller proc_Exit thisEnter");
            }

            protected void thisUpdate()
            {
            }

            protected void thisEvaluate()
            {
            }

            protected void thisExit()
            {
            }
        }
        #endregion
    }
}
