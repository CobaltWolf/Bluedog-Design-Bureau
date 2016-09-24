using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Portions copied with permission from PEBKAC Industries: Launch Escape System by Kurld
// https://github.com/Jurld/PebkacLaunchEscape
// Modified to work as a single part.

namespace BDB
{
    public class ModuleBdbPebkacLiftingSurface : ModuleLiftingSurface
    {
        [KSPField]
        public string transformName;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!string.IsNullOrEmpty(transformName))
            {
                Transform testTransform = part.FindModelTransform(transformName);
                if (testTransform != null)
                    baseTransform = testTransform;
                else
                    Debug.LogError($"[{moduleName}] could not find transform named '{transformName}'");
            }
        }
    }

    public class ModuleBdbLesController : PartModule
    {

        private static string _myModTag = "[BDB Apollo]";

        [KSPField(isPersistant = false, guiActive = false)]
        public string escapeEngineID = "LES_Escape";

        [KSPField(isPersistant = false, guiActive = false)]
        public float escapeEngineRunTime = 3.5f;

        private double escapeEngineStartTime = 0;

        [KSPField(isPersistant = false, guiActive = false)]
        public string pitchEngineID = "LES_PitchControl";

        // GUI enabled for tuning purposes
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Pitch Motor Run Time")]
        [UI_FloatRange(minValue = 0, stepIncrement = 0.1f, maxValue = 10)]
        public float pitchEngineRunTime = 1.0f;
        
        private double pitchEngineStartTime = 0;

        [KSPField(isPersistant = false, guiActive = false)]
        public string jettisonEngineID = "LES_Jettison";

        [KSPField(isPersistant = false, guiActive = false)]
        public float jettisonEngineRunTime = 1.0f;

        private double jettisonEngineStartTime = 0;

        private ModuleEngines escapeEngine;
        private ModuleEngines pitchEngine;
        private ModuleEngines jettisonEngine;

        private bool _aborted = false;
        private bool _hasJettisoned = false;

        private Vector3 _progradev;
        private Vector3 _yawComponent;
        private Vector3 _pitchComponent;
        private double _yaw;
        private double _pitch;

        #region Fun With Canards!

        private double _maxFuel;
        private Vector3 _origComOffset;
        private Vector3 _origCopOffset;
        private Vector3 _origColOffset;

        // GUI enabled for tuning purposes
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CoM multiplier")]
        [UI_FloatRange(minValue = 1, stepIncrement = 1.0f, maxValue = 100)]
        public float comMult = 90.0f;

        // GUI enabled for tuning purposes
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CoP multiplier")]
        [UI_FloatRange(minValue = 1, stepIncrement = 1.0f, maxValue = 100)]
        public float copMult = 90.0f;

        // time when the canards will deploy
        private double _canardDeployTime;
        private bool _deployed;

        //the animation for the canards
        private ModuleAnimateGeneric _deployAnimation;

        // the lifting surface for the deployed canards
        private ModuleLiftingSurface _liftingSurface;

        private ModuleAnimateGeneric GetDeployAnimation()
        {
            Debug.Log(string.Format("{0} GetDeployAnimation", _myModTag));
            ModuleAnimateGeneric myAnimation = null;

            try
            {
                myAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myAnimation)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, "Didn't find ModuleAnimateGeneric on LES!"));
            }

            return myAnimation;
        }

        private ModuleLiftingSurface GetLiftingSurface()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesPitchControl.GetLiftingSurface", _myModTag));
            ModuleLiftingSurface myLiftingSurface = null;

            try
            {
                myLiftingSurface = part.FindModulesImplementing<ModuleLiftingSurface>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myLiftingSurface)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError("ERROR: Didn't find ModuleLiftingSurface on LES nosecone!");
            }

            return myLiftingSurface;
        }

        #endregion

        public override void OnStart(StartState state)
        {

            // hook up to the part attach callback
            /*
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
            }
            */

            List<ModuleEngines> engines = part.FindModulesImplementing<ModuleEngines>();
            foreach (ModuleEngines e in engines)
            {
                if (e.engineID == escapeEngineID)
                {
                    escapeEngine = e;
                }
                else if (e.engineID == pitchEngineID)
                {
                    pitchEngine = e;
                }
                else if (e.engineID == jettisonEngineID)
                {
                    jettisonEngine = e;
                }
                foreach (BaseAction a in e.Actions)
                {
                    a.active = false;
                }
                foreach (BaseField f in e.Fields)
                {
                    f.guiActive = false;
                    f.guiActiveEditor = false;
                }
                foreach (BaseEvent ev in e.Events)
                {
                    ev.guiActive = false;
                    ev.guiActiveEditor = false;
                }
            }

            // check for nulls in engines

            // set up the variables used to shift aerodynamics
            _maxFuel = part.Resources["SolidFuel"].maxAmount;
            _origComOffset = part.CoMOffset;
            _origCopOffset = part.CoPOffset;
            _origColOffset = part.CoLOffset;

            // set up the variables used by code for simming the canards
            _deployAnimation = GetDeployAnimation();
            _liftingSurface = GetLiftingSurface();

        }
       
        /* 
        private void OnEditorAttach()
        {
            Debug.Log(string.Format("{0} LES OnEditorAttach", _myModTag));
            part.transform.Rotate(0, -90, 0);
        }
        */

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics)
            {
                return;
            }

            if (escapeEngine != null && escapeEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - escapeEngineStartTime >= escapeEngineRunTime)
                {
                    escapeEngine.Shutdown();
                }

                // in real life, the LES had a huge chunk of depleted uranium ballast in its nosecone
                // in the game, when we model the LES using a single part the center of mass shifts 
                // too far aft as solid fuel is burned, compared to real life. This makes it 
                // difficult to maintain stable flight

                // while the escape engine is running, adjust the center of mass offset
                // until 1.2 hits, we also have to adjust the center of pressure offset since CoP 
                // is currently incorrectly tied to the final center of mass, rather than the part origin
                var comFactor = (_maxFuel - part.Resources["SolidFuel"].amount) / comMult;
                var copFactor = (_maxFuel - part.Resources["SolidFuel"].amount) / copMult;

                var newComY = _origComOffset.y + (float)comFactor;
                var newCopY = _origCopOffset.y + (float)copFactor;

                part.CoMOffset.Set(_origComOffset.x, newComY, _origComOffset.z);
                part.CoPOffset.Set(_origCopOffset.x, -newCopY, _origCopOffset.z);

                //Debug.Log(string.Format("CoMOffset updated to {0}", newY.ToString()));

            }

            if (pitchEngine != null && pitchEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - pitchEngineStartTime >= pitchEngineRunTime)
                {
                    pitchEngine.Shutdown();
                }
            }

            if (jettisonEngine != null && jettisonEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - jettisonEngineStartTime >= jettisonEngineRunTime)
                {
                    jettisonEngine.Shutdown();
                }
            }

            if (_aborted && !_deployed && (Planetarium.GetUniversalTime() >= _canardDeployTime))
            {
                // pop the canards
                DeployCanards();
            }

            if (_aborted && CheckCanAutoJettison())
            {
                DoJettison();
            }
        }
        
        #region Abort!

        [KSPAction("Abort!", actionGroup = KSPActionGroup.Abort)]
        public void ActivateAbortAction(KSPActionParam param)
        {
            if (_aborted == false)
            {
                if (vessel.altitude < 3000)
                {
                    ActivateAbortMode1aAction(param);
                }
                else
                {
                    ActivateAbortMode1bAction(param);
                }

                // set the game time when the canards are to pop:
                _canardDeployTime = escapeEngineStartTime + 11;
            }
        }

        // https://en.wikipedia.org/wiki/Apollo_abort_modes
        // https://www.hq.nasa.gov/alsj/CSM15_Launch_Escape_Subsystem_pp137-146.pdf

        [KSPAction("Abort Mode 1A")]
        public void ActivateAbortMode1aAction(KSPActionParam param)
        {
            Debug.Log("Abort Mode 1A Activated");
            _aborted = true;
            ActivateEscapeEngine();
            ActivatePitchEngine();
        }

        [KSPAction("Abort Mode 1B")]
        public void ActivateAbortMode1bAction(KSPActionParam param)
        {
            Debug.Log("Abort Mode 1B Activated");
            _aborted = true;
            ActivateEscapeEngine();
        }

        private void ActivateEscapeEngine()
        {
            if (escapeEngineStartTime <= 0)
            {
                escapeEngineStartTime = Planetarium.GetUniversalTime();
                escapeEngine.Activate();
            }
        }

        private void ActivatePitchEngine()
        {
            if (pitchEngineStartTime <= 0)
            {
                pitchEngineStartTime = Planetarium.GetUniversalTime();
                pitchEngine.Activate();
            }
        }

        private void DeployCanards()
        {
            // start the animation and adjust aero for deployed canards
            Debug.Log(string.Format("{0} Deploying canards", _myModTag));
             if (_deployAnimation != null)
            {
                _deployAnimation.Toggle();
            }
           
            Debug.Log(string.Format("{0} setting CoMOffset in Y axis from {1} to {2}", _myModTag, part.CoMOffset.y.ToString(), _origComOffset.y.ToString()));
            Debug.Log(string.Format("{0} setting CoPOffset in Y axis from {1} to {2}", _myModTag, part.CoPOffset.y.ToString(), _origCopOffset.y.ToString()));

            part.CoMOffset.Set(_origComOffset.x, _origComOffset.y, _origComOffset.z);
            part.CoPOffset.Set(_origCopOffset.x, _origCopOffset.y, _origCopOffset.z);
            part.CoLOffset.Set(_origColOffset.x, _origColOffset.y + 1.45f, _origColOffset.z);
            
            if (_liftingSurface != null && part.Modules.Contains<ModuleLiftingSurface>())
            {
                _liftingSurface.useInternalDragModel = true;
                _liftingSurface.deflectionLiftCoeff = 0.35f;
            }
            
            _deployed = true;
            
        }

        #endregion

        #region Jettison

        // fires when AG is triggered
        [KSPAction("Jettison Tower")]
        public void ActivateJettisonAction(KSPActionParam param)
        {
            DoJettison();
        }

        // fires when part is staged
        public override void OnActive()
        {
            DoJettison();
        }

        [KSPEvent(guiName = "Jettison Tower", guiActive = true)]
        public void DoJettison()
        {
            Debug.Log("Jettison Tower Activated");
            _hasJettisoned = true;
            ActivateJettisonEngine();
            if (part.Modules.Contains("ModuleDecouple"))
            {
                part.FindModulesImplementing<ModuleDecouple>().FirstOrDefault().Decouple();
            }
        }

        private bool CheckCanAutoJettison()
        {
            if (!_aborted || _hasJettisoned) // || Planetarium.GetUniversalTime() - escapeEngineStartTime > 20)
            {
                return false;
            }
            else
            {
                // is the vessel pointed retrograde?
                _progradev = vessel.GetSrfVelocity();
                _yawComponent = Vector3d.Exclude(vessel.GetTransform().forward, _progradev);
                _pitchComponent = Vector3d.Exclude(vessel.GetTransform().right, _progradev);
                _yaw = Vector3d.Angle(_yawComponent, vessel.GetTransform().up);
                _pitch = Vector3d.Angle(_pitchComponent, vessel.GetTransform().up);

                return _yaw > 175d && _pitch > 175d;
            }
        }

        private void ActivateJettisonEngine()
        {
            if (jettisonEngineStartTime <= 0)
            {
                jettisonEngineStartTime = Planetarium.GetUniversalTime();
                jettisonEngine.Activate();
            }
        }
        
        #endregion

    }
}