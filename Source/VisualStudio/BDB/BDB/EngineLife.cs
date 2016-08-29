using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace BDB
{
    public class ModuleBdbEngineLife: PartModule
    {
        [KSPField(isPersistant = false)]
        public string engineID;
        private Boolean haveEngine = false;

        [KSPField(isPersistant = false)]
        public float runTime = -1.0f;

        [KSPField(isPersistant = false)]
        public float runTimeEnd = -1.0f;

        [KSPField(isPersistant = true)]
        public float failTime = -1.0f;

        [KSPField(isPersistant = true)]
        //[KSPField(guiActive = true, isPersistant = true, guiName = "Failure Severity")]
        public float failSeverity = -1.0f;

        [KSPField(isPersistant = false)]
        public float failSeverityMin = 1.0f;

        [KSPField(isPersistant = false)]
        public float failSeverityMax = 1.1f;

        [KSPField(isPersistant = false)]
        public float maxHeatProduction = -1.0f;

        [KSPField(isPersistant = true)]
        public float timeActive = 0.0f;

        [KSPField(guiActive = true, isPersistant = false, guiName = "Engine Time")]
        public string runTimeDisplay = "";

        [KSPField(guiActive = true, isPersistant = false, guiName = "Engine Status")]
        public string engineStatusDisplay = "Ok";

        private ModuleEngines engine;
        private double lastUpdateTime = -1.0f;
        private float baseHeatProduction = 0.0f;

        public override void OnStart(PartModule.StartState state)
        {
            //Debug.Log("[ModuleEngineLife]: OnStart()");
            List<ModuleEngines> engines = new List<ModuleEngines>();
            engines = this.GetComponents<ModuleEngines>().ToList();
            foreach (ModuleEngines e in engines)
            {
                if (e.engineID == engineID || engineID == "")
                {
                    engine = e;
                    haveEngine = true;
                    break;
                }
                    
            }
            if (!haveEngine)
            {
                foreach (ModuleEngines e in engines)
                {
                    if (e.thrustVectorTransformName == engineID)
                    {
                        engine = e;
                        haveEngine = true;
                        break;
                    }

                }
            }
            if (haveEngine)
            {
                baseHeatProduction = engine.heatProduction;
                if (maxHeatProduction < baseHeatProduction)
                {
                    maxHeatProduction = baseHeatProduction * 30.0f;
                }
                failSeverityMin = Math.Max(0.01f, failSeverityMin);
                failSeverityMax = Math.Max(failSeverityMin + 0.01f, failSeverityMax);
            } else {
                Debug.Log("[ModuleEngineLife]: EngineID '" + engineID + "' not found on part " + part.name);
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || Time.timeSinceLevelLoad < 1.0f || !haveEngine)
            {
                return;
            }
            //Debug.Log("[ModuleEngineLife]: FixedUpdate()");
            double currentTime = Planetarium.GetUniversalTime();
            if (lastUpdateTime > 0)
            {

                float deltaTime = (float)(currentTime - lastUpdateTime);
                if (engine.EngineIgnited)
                {
                    timeActive += deltaTime * (engine.GetCurrentThrust() / engine.maxThrust);
                    if (failTime < 0)
                    {
                        InitEngineFailure();
                    }
                }
                runTimeDisplay = timeActive.ToString("0.0") + "s";// + " (" + failTime.ToString("0.0") + "s)";
                if (runTime > 0 && timeActive > runTime)
                {
                    runTimeDisplay += "!";
                }
                if (failTime > 0.0f && timeActive > failTime)
                {
                    engineStatusDisplay = "Internal Failure";
                    engine.heatProduction = Math.Min(maxHeatProduction, baseHeatProduction * Math.Max(1, (timeActive - runTime) * failSeverity));
                }
            }
            lastUpdateTime = currentTime;
        }

        public override string GetInfo()
        {
            String info = "Rated run time: " + runTime.ToString("0");
            if (runTimeEnd > runTime)
                info += "-" + runTimeEnd.ToString("0");
            info += "s";
            return info;
        }

        private float InitEngineFailure()
        {
            if (failTime < 0)
            {
                if (runTimeEnd <= runTime)
                {
                    failTime = runTime;
                }
                else
                {
                    failTime = UnityEngine.Random.Range(runTime, runTimeEnd);
                }
                failSeverity = UnityEngine.Random.Range(failSeverityMin, failSeverityMax);
            }
            return failTime;
        }
    }
}
