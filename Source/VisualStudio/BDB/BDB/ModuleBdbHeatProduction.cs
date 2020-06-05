using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    public class ModuleBdbHeatProduction : PartModule
    {
        [KSPField(guiActive = true, guiName = "Internal Heat", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public string heatFluxDisplay = "";

        private bool useThermal = true;

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);

            useThermal = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().useThermal;

            if (!useThermal)
            {
                foreach (BaseField f in Fields)
                {
                    f.guiActive = false;
                    f.guiActiveEditor = false;
                    f.isPersistant = false;
                }

                return;
            }
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (!useThermal)
                return;

        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || !useThermal)
                return;

            double heatFlux = 0.0;

            heatFlux += part.protoModuleCrew.Count * 0.1; // 100 W/occupant

            ModuleCommand mc = part.FindModuleImplementing<ModuleCommand>();
            if (mc != null)
            {
                double ec = 0.0;
                if (mc.ModuleState != ModuleCommand.ModuleControlState.NotEnoughResources)
                {
                    /*
                    ModuleResourceHandler mcr = mc.resHandler;
                    if (mcr != null && mcr.inputResources != null)
                    {
                        for (int i = 0; i < mcr.inputResources.Count; i++)
                        {
                            if (mcr.inputResources[i].name == "ElectricCharge")
                            {
                                ec = mcr.inputResources[i].rate;
                            }
                        }
                    }
                    */
                    ec = part.mass * 0.2; // 200 W/ton
                    ec += part.CrewCapacity * 0.15; // 150 W/seat
                    if (mc.IsHibernating)
                    {
                        ec *= mc.hibernationMultiplier;
                    }
                }
                heatFlux += ec;
            }

            heatFluxDisplay = (heatFlux * 1000).ToString("0.0 W");

            part.AddThermalFlux(heatFlux);
        }
    }

    class ModuleBdbEnvironmentControl : PartModule
    {
        [KSPField(guiActive = true, guiName = "Cabin Temp", guiFormat = "N3", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double temperatureDisplay = 0.0f;

        [KSPField(guiActive = true, guiName = "Skin Temp", guiFormat = "N3", guiUnits = " F", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public double skinTemperatureDisplay = 0.0f;

        [KSPField(guiActive = true, guiName = "Thermostat", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_FloatRange(minValue = 0.0f, maxValue = 1000.0f, stepIncrement = 1.0f, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.Editor)]
        public float thermostatDisplay = 0.0f;

        [KSPField(isPersistant = true)]
        public double thermostat = -1;

        [KSPField(guiActive = true, guiName = "Outside Temp", groupDisplayName = "Environment", groupName = "bdbEnvironment")]
        public string outsideTemperatureDisplay = "";

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Vent", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_Toggle(disabledText = "Locked", enabledText = "Auto", scene = UI_Scene.All)]
        public bool ventEnabled = false;

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Evaporator", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_Toggle(disabledText = "Disabled", enabledText = "Auto", scene = UI_Scene.All)]
        public bool coolingEnabled = false;

        [KSPField(guiActive = true, isPersistant = true, guiName = "Evaporator Rate", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_FloatRange(minValue = 0.0f, maxValue = 8.0f, stepIncrement = 0.5f, scene = UI_Scene.Flight)]
        public float coolingRate = 4.0f;

        [KSPField]
        public double coolingCapacity = 0.1;

        [KSPField]
        public string coolingResource = "Water";

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Heater", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_Toggle(disabledText = "Disabled", enabledText = "Auto", scene = UI_Scene.All)]
        public bool heaterEnabled = false;

        [KSPField(guiActive = true, isPersistant = true, guiName = "Heat Level", groupDisplayName = "Environment", groupName = "bdbEnvironment"),
            UI_FloatRange(minValue = 0.0f, maxValue = 8.0f, stepIncrement = 0.5f, scene = UI_Scene.Flight)]
        public float heaterRate = 1.0f;

        [KSPField]
        public double heaterPower = -1;

        private bool useThermal = true;
        private double saveConduction;
        private bool isPreLaunch = false;

        private bool canCool = false;
        private bool cooling = false;
        private bool canHeat = true;
        private bool heating = false;
        private bool canVent = true;
        private bool venting = false;

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);

            useThermal = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().useThermal;

            if (!useThermal)
            {
                foreach (BaseField f in Fields)
                {
                    f.guiActive = false;
                    f.guiActiveEditor = false;
                    f.isPersistant = false;
                }

                return;
            }

            saveConduction = part.skinInternalConductionMult;

            if (thermostat < 1.0)
                thermostat = part.maxTemp * Math.Min(part.radiatorMax, 1.0);
            else
                part.radiatorMax = thermostat / part.maxTemp;

            UI_FloatRange control;
            if (state == StartState.Editor)
                control = (UI_FloatRange)Fields[nameof(thermostatDisplay)].uiControlEditor;
            else
                control = (UI_FloatRange)Fields[nameof(thermostatDisplay)].uiControlFlight;

            control.minValue = (float)BdbTemperature.KelvinToDisplay(Math.Min(thermostat, BdbTemperature.FtoK(50)));
            control.maxValue = (float)BdbTemperature.KelvinToDisplay(Math.Max(thermostat, BdbTemperature.FtoK(90)));
            thermostatDisplay = (float)BdbTemperature.KelvinToDisplay(thermostat);
            control.onFieldChanged = OnThermostatChanged;

            UpdatePreLaunch();
            UpdateUI();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (!useThermal)
                return;

            UpdatePreLaunch();
            UpdateUI();
        }

        private void UpdatePreLaunch()
        {
            if (HighLogic.LoadedSceneIsFlight)
                isPreLaunch = vessel.FindPartModuleImplementing<LaunchClamp>() != null;
        }

        private void UpdateUI()
        {
            canCool = part.Resources[coolingResource] != null;
            Fields[nameof(coolingEnabled)].guiActive = canCool;
            Fields[nameof(coolingEnabled)].guiActiveEditor = canCool;
            Fields[nameof(coolingRate)].guiActive = canCool && coolingEnabled;

            Fields[nameof(heaterEnabled)].guiActive = canHeat;
            Fields[nameof(heaterEnabled)].guiActiveEditor = canHeat;
            Fields[nameof(heaterRate)].guiActive = canHeat && heaterEnabled;

            if (part.CrewCapacity > 0)
            {
                Fields[nameof(temperatureDisplay)].guiName = "Cabin Temp";
                Fields[nameof(ventEnabled)].guiName = "Vent";
            }
            else
            {
                Fields[nameof(temperatureDisplay)].guiName = "Compartment Temp";
                Fields[nameof(ventEnabled)].guiName = "Louvers";
            }

            Fields[nameof(temperatureDisplay)].guiUnits = BdbTemperature.TempDisplayUnits();
            Fields[nameof(thermostatDisplay)].guiUnits = BdbTemperature.TempDisplayUnits();
            Fields[nameof(thermostatDisplay)].guiName = "Thermostat (" + BdbTemperature.TempDisplayUnits().Trim() + ")";
            Fields[nameof(skinTemperatureDisplay)].guiUnits = BdbTemperature.TempDisplayUnits();
        }

        private void OnThermostatChanged(BaseField field, object oldFieldValueObj)
        {
            thermostat = BdbTemperature.DisplayToKelvin(thermostatDisplay);
            part.radiatorMax = thermostat / part.maxTemp;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || !useThermal)
                return;

            if (isPreLaunch && part.radiatorMax < 1)
            {
                part.temperature = thermostat;
                return;
            }

            double heatFlux = 0.0;

            if (canCool)
            {
                if (!coolingEnabled)
                    cooling = false;
                else if (part.temperature > thermostat + 0.5)
                    cooling = true;
                else if (cooling && part.temperature < thermostat)
                    cooling = false;

                Fields[nameof(coolingRate)].guiActive = coolingEnabled;

                if (cooling)
                {
                    PartResource item = part.Resources[coolingResource];
                    double partCoolingFlux = coolingCapacity * coolingRate;
                    double massLost = partCoolingFlux / 2496900.0; // item.heatOfVaporization;
                    double evaporativeFlux = 274.8 * massLost * item.info.specificHeatCapacity;

                    massLost *= TimeWarp.fixedDeltaTime;
                    double unitsLost = massLost / item.info.density;
                    item.amount = Math.Max(item.amount - unitsLost, 0);

                    heatFlux -= partCoolingFlux + evaporativeFlux;

                    double unitsPerHour = unitsLost * (1 / TimeWarp.fixedDeltaTime) * 3600;
                    ((UI_Toggle)(Fields[nameof(coolingEnabled)].uiControlFlight)).enabledText = "Active (" + (unitsPerHour).ToString("0.000") + "/hr)";
                }
                else
                {
                    ((UI_Toggle)(Fields[nameof(coolingEnabled)].uiControlFlight)).enabledText = "Auto";
                }
            }

            
            if (canHeat)
            {
                if (!heaterEnabled)
                    heating = false;
                else if (part.temperature < thermostat - 0.5)
                    heating = true;
                else if (heating && part.temperature > thermostat)
                    heating = false;

                Fields[nameof(heaterRate)].guiActive = heaterEnabled;

                if (heating)
                {
                    double partHeaterFlux = heaterPower;
                    if (partHeaterFlux < 0)
                        partHeaterFlux = Math.Pow(part.mass, 0.5) * 1.0;

                    partHeaterFlux *= heaterRate;

                    double partHeaterEC = partHeaterFlux / 500; // Give 500 kW/EC, because there's just not enough EC around.
                    partHeaterEC = part.RequestResource("ElectricCharge", partHeaterEC * TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) / TimeWarp.fixedDeltaTime;
                    partHeaterFlux = partHeaterEC * 500;

                    heatFlux += partHeaterFlux;
                    ((UI_Toggle)(Fields[nameof(heaterEnabled)].uiControlFlight)).enabledText = "Active (" + (partHeaterFlux * 1000).ToString("0.0") + " W)";
                }
                else
                {
                    ((UI_Toggle)(Fields[nameof(heaterEnabled)].uiControlFlight)).enabledText = "Auto";
                }
            }

            
            if (canVent)
            {
                if (!ventEnabled)
                    venting = false;
                else if (part.temperature > thermostat + 0.5)
                    venting = true;
                else if (venting && part.temperature < thermostat)
                    venting = false;

                if (venting && part.skinTemperature > part.temperature)
                {
                    venting = false;
                    ((UI_Toggle)(Fields[nameof(ventEnabled)].uiControlFlight)).enabledText = "Closed (Skin Hot)";
                }
                else if (venting)
                {
                    ((UI_Toggle)(Fields[nameof(ventEnabled)].uiControlFlight)).enabledText = "Open";
                }
                else
                {
                    ((UI_Toggle)(Fields[nameof(ventEnabled)].uiControlFlight)).enabledText = "Auto";
                }
            }

            if (canVent && venting)
                part.skinInternalConductionMult = 2000;
            else
                part.skinInternalConductionMult = saveConduction;


            part.AddThermalFlux(heatFlux);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !useThermal)
                return;

            temperatureDisplay = BdbTemperature.KelvinToDisplay(part.temperature);
            //part.radiatorMax = thermostat / part.maxTemp;
            //thermostatDisplay = KelvinToDisplay(thermostat);

            if (vessel.staticPressurekPa > 0.0)
                outsideTemperatureDisplay = BdbTemperature.KelvinToDisplay(vessel.atmosphericTemperature).ToString("N1") + BdbTemperature.TempDisplayUnits(); // KtoF(vessel.externalTemperature);
            else
                outsideTemperatureDisplay = "-.-" + BdbTemperature.TempDisplayUnits();

            skinTemperatureDisplay = BdbTemperature.KelvinToDisplay(part.skinTemperature);
        }

    }

    public static class BdbTemperature
    {
        public static string TempDisplayUnits()
        {
            return " F";
        }

        public static double KelvinToDisplay(double k)
        {
            return KtoF(k);
        }

        public static double DisplayToKelvin(double t)
        {
            return FtoK(t);
        }

        public static double KtoC(double k)
        {
            return k - 273.15;
        }

        public static double CtoK(double c)
        {
            return c + 273.15;
        }

        public static double KtoF(double k)
        {
            return k * (9.0 / 5.0) - 459.67;
        }

        public static double FtoK(double f)
        {
            return (f + 459.67) / (9.0 / 5.0);
        }
    }
}
