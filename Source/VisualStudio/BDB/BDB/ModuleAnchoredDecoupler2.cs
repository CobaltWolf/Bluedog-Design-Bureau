using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleAnchoredDecouplerBdb : ModuleAnchoredDecoupler
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Stage Delay"), UI_FloatRange(minValue = 0.0f, maxValue = 2.0f, stepIncrement = 0.1f,affectSymCounterparts = UI_Scene.None)]
        public float stageDelay = 0.0f;

        private double stageTime = double.NaN;

        public override void OnActive()
        {
            if (stageDelay <= 0.0f)
                base.OnActive();
            else
                stageTime = Planetarium.GetUniversalTime() + stageDelay;
        }

        public override void OnUpdate()
        {
            if (isDecoupled)
                return;
            if (stageTime <= Planetarium.GetUniversalTime())
                Decouple();
        }
    }
}
