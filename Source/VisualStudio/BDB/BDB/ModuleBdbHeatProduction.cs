using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbHeatProduction : PartModule
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Heat Production", guiFormat = "N1", guiUnits = " kW", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_FloatRange(minValue = 0.0f, maxValue = 10f, stepIncrement = 0.1f, affectSymCounterparts = UI_Scene.All)]
        public float heatProduction = 10.0f;

        [KSPField(guiActive = true, guiName = "Cabin Temperature", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double temperature = 0.0f;

        [KSPField(guiActive = true, guiName = "Cooling Thermostat", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double thermostat = 0.0f;

        [KSPField(guiActive = true, guiName = "Outside Temperature", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public string outsideTemperature = "";

        [KSPField(guiActive = true, guiName = "Skin Temperature", guiFormat = "N1", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double skinTemperature = 0.0f;

        [KSPField(guiActive = true, isPersistant = true, guiName = "Cabin Vent", groupDisplayName = "Environment", groupName = "bdbEnvironment"), UI_Toggle(disabledText = "Closed", enabledText = "Open")]
        public bool vented = false;

        private double saveConduction;

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            saveConduction = part.skinInternalConductionMult;
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            Fields["thermostat"].guiActive = vessel.FindPartModuleImplementing<ModuleActiveRadiator>() != null;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            part.AddThermalFlux(heatProduction);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            temperature = KtoF(part.temperature);
            thermostat = KtoF(part.maxTemp * part.radiatorMax);

            if (vessel.staticPressurekPa > 0.0)
                outsideTemperature = KtoF(vessel.atmosphericTemperature).ToString("N1") + " F"; // KtoF(vessel.externalTemperature);
            else
                outsideTemperature = "-.- F";

            skinTemperature = KtoF(part.skinTemperature);

            if (vented)
                part.skinInternalConductionMult = 1.0;
            else
                part.skinInternalConductionMult = saveConduction;
        }

        private double KtoC(double k)
        {
            return k - 273.15;
        }

        private double KtoF(double k)
        {
            return k * (9.0 / 5.0) - 459.67;
        }
    }
}
