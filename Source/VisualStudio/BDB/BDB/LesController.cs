using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

// Portions copied with permission from PEBKAC Industries: Launch Escape System by Kurld
// https://github.com/Jurld/PebkacLaunchEscape
// Modified to work as a single part.

namespace BDB
{
    public class ModuleBdbLesController : PartModule
    {
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

        public override void OnStart(StartState state)
        {
            List<ModuleEngines> engines = part.FindModulesImplementing<ModuleEngines>();
            foreach (ModuleEngines e in engines)
            {
                if (e.engineID == escapeEngineID)
                {
                    escapeEngine = e;
                } else if (e.engineID == pitchEngineID)
                {
                    pitchEngine = e;
                } else if (e.engineID == jettisonEngineID)
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

            part.Resources["SolidFuel"].isTweakable = false;
            part.Resources["SolidFuel"].isVisible = false;
        }

        [KSPAction("Abort!", actionGroup = KSPActionGroup.Abort)]
        public void activateAbortAction(KSPActionParam param)
        {
            if (vessel.altitude < 3000)
            {
                activateAbortMode1aAction(param);
            } else
            {
                activateAbortMode1bAction(param);
            }
        }

        // https://en.wikipedia.org/wiki/Apollo_abort_modes
        // https://www.hq.nasa.gov/alsj/CSM15_Launch_Escape_Subsystem_pp137-146.pdf

        [KSPAction("Abort Mode 1A")]
        public void activateAbortMode1aAction(KSPActionParam param)
        {
            Debug.Log("Abort Mode 1A Activated");
            _aborted = true;
            activateEscapeEngine();
            activatePitchEngine();
        }

        [KSPAction("Abort Mode 1B")]
        public void activateAbortMode1bAction(KSPActionParam param)
        {
            Debug.Log("Abort Mode 1B Activated");
            _aborted = true;
            activateEscapeEngine();
        }

        [KSPAction("Jettison Tower")]
        public void activateJettisonAction(KSPActionParam param)
        {
            doJettison();
        }

        public override void OnActive()
        {
            doJettison();
        }

        [KSPEvent(guiName = "Jettison Tower", guiActive = true)]
        public void doJettison()
        {
            Debug.Log("Jettison Tower Activated");
            _hasJettisoned = true;
            activatePitchEngine();
            activateJettisonEngine();
            if (part.Modules.Contains("ModuleDecouple"))
            {
                part.FindModulesImplementing<ModuleDecouple>().FirstOrDefault().Decouple();
            }
        }
        
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics)
            {
                return;
            }

            if (escapeEngine != null && escapeEngine.EngineIgnited)
            {
                if(Planetarium.GetUniversalTime() - escapeEngineStartTime >= escapeEngineRunTime)
                {
                    escapeEngine.Shutdown();
                }
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
            if (_aborted && checkCanAutoJettison())
            {
                doJettison();
            }
        }
        
        private void activateEscapeEngine()
        {
            if (escapeEngineStartTime <= 0)
            {
                escapeEngineStartTime = Planetarium.GetUniversalTime();
                escapeEngine.Activate();
            }
        }
        
        private void activatePitchEngine()
        {
            if (pitchEngineStartTime <= 0)
            {
                pitchEngineStartTime = Planetarium.GetUniversalTime();
                pitchEngine.Activate();
            }
        }
        
        private void activateJettisonEngine()
        {
            if (jettisonEngineStartTime <= 0)
            {
                jettisonEngineStartTime = Planetarium.GetUniversalTime();
                jettisonEngine.Activate();
            }
        }

        private bool checkCanAutoJettison()
        {
            if (!_aborted || _hasJettisoned || Planetarium.GetUniversalTime() - escapeEngineStartTime > 20)
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
    }
}
