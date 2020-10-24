using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDB
{
    class ModuleBdbAtlasBoosterSkirt : PartModule
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Auto Jettison", groupDisplayName = "Auto Jettison", groupName = "bdbAutoJettison"), UI_Toggle(affectSymCounterparts = UI_Scene.All)]
        public bool autoJettison = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "G Force", groupDisplayName = "Auto Jettison", groupName = "bdbAutoJettison"), UI_FloatRange(minValue = 1.5f, maxValue = 10.0f, stepIncrement = 0.1f, affectSymCounterparts = UI_Scene.All)]
        public float geeForce = 4.0f;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "G Force", guiFormat = "0.0", groupDisplayName = "Auto Jettison", groupName = "bdbAutoJettison")]
        public double geeForceDisplay = 0.0;

        ModuleDecouple decoupler;

        double gTime = -1;

        public override void OnStart(StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            decoupler = part.FindModuleImplementing<ModuleDecouple>();
        }

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !autoJettison)
                return;

            if (decoupler != null && !decoupler.isDecoupled)
            {
                double g = part.vessel.geeForce;
                geeForceDisplay = g;
                if (g < geeForce)
                    gTime = Planetarium.GetUniversalTime();
                else if (gTime + 0.25 < Planetarium.GetUniversalTime())
                {
                    decoupler.Decouple();
                    ModuleEngines e = part.FindModuleImplementing<ModuleEngines>();
                    if (e != null)
                        e.Activate();
                }
            }
        }
    }

}

