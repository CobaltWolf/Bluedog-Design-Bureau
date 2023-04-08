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

            staged = true;
        }

        public override void OnUpdate()
        {
            if (isDecoupled)
                return;
            if (stageTime <= Planetarium.GetUniversalTime())
                Decouple();
        }

        public override void OnDecouple()
        {
            Part attachedTo = explosiveNode.attachedPart;
            
            base.OnDecouple();

            if (attachedTo != null)
            {
                List<Collider> attachedColliders = attachedTo.FindModelComponents<Collider>();
                List<Collider> colliders = part.FindModelComponents<Collider>();
                Debug.Log("[ModuleAnchoredDecouplerBdb] IgnoreCollision x: " + attachedColliders.Count.ToString() + " y: " + colliders.Count.ToString());
                for (int x = 0; x < attachedColliders.Count; x++)
                {
                    for (int y = 0; y < colliders.Count; y++)
                    {
                        Physics.IgnoreCollision(attachedColliders[x], colliders[y]);
                    }
                }
            }
        }
    }

    class ModuleBdbDecoupleAfterBurn : ModuleDecouple
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Auto Jettison"), UI_Toggle()]
        public bool autoDecouple = true;

        private ModuleEngines engine;
        private bool wasRunning = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = part.FindModulesImplementing<ModuleEngines>().FirstOrDefault();
        }

        public override void OnActive()
        {
            //base.OnActive();
            staged = true;
        }

        public override void OnUpdate()
        {
            if (isDecoupled)
                return;
            if (engine == null)
                return;
            if (autoDecouple)
            {
                if (!wasRunning)
                {
                    wasRunning = engine.GetCurrentThrust() > 0;
                }
                else
                {
                    if (engine.GetCurrentThrust() <= 0)
                        Decouple();
                }
            }
        }
    }

    class ModuleBdbAnchoredDecoupleAfterBurn : ModuleAnchoredDecoupler
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Auto Jettison"), UI_Toggle()]
        public bool autoDecouple = true;

        private ModuleEngines engine;
        private bool wasRunning = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = part.FindModulesImplementing<ModuleEngines>().FirstOrDefault();
        }

        public override void OnActive()
        {
            //base.OnActive();
            staged = true;
        }

        public override void OnUpdate()
        {
            if (isDecoupled)
                return;
            if (engine == null)
                return;
            if (autoDecouple)
            {
                if (!wasRunning)
                {
                    wasRunning = engine.GetCurrentThrust() > 0;
                }
                else
                {
                    if (engine.GetCurrentThrust() <= 0)
                        Decouple();
                }
            }
        }
    }
}
