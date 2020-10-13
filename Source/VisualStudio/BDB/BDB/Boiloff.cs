using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbBoiloff : PartModule
    {
        [KSPField(isPersistant = true)]
        public double lastUpdateTime = -1.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Status", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public string boiloffDisplay = "";

        [KSPField(guiActive = false, isPersistant = false, guiActiveEditor = false, guiName = "Heat Leakage", guiFormat = "0.000", guiUnits = " kW", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public double heatLeakageDisplay = 0.0;

        [KSPField(guiActive = false, isPersistant = false, guiActiveEditor = false, guiName = "Flux", guiFormat = "0.000", guiUnits = " kW", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public double exposureDisplay = 0.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Solar Flux", guiFormat = "0.000", guiUnits = " kW/m^2", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public double sunFluxDisplay = 0.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Body Flux", guiFormat = "0.000", guiUnits = " kW/m^2", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public double bodyFluxDisplay = 0.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "X", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public string xDisplay = "";

        [KSPField(isPersistant = false)]
        public bool debug = false;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = true, guiName = "Insulation", guiFormat = "P0", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public string insulationDisplay = "";

        [KSPField(isPersistant = false)]
        public float insulation = 0.0f;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = true, guiName = "Reflectivity", guiFormat = "P0", groupDisplayName = "Boiloff", groupName = "bdbBoiloff")]
        public float reflectivity = 0.0f;

        [KSPField(isPersistant = false)]
        public int insulationDeployModuleIndex = -1;

        [KSPField(isPersistant = false)]
        public float internalInsulation = -1f;

        private IScalarModule insulationScalarModule = null;

        private List<CryoResourceItem> cryoResources;
        private bool boiloffEnabled;
        private double boiloffDifficulty;
        private double boiloffFactor = 0.34;
        private bool hasCryoResource = false;
        private bool isPreLaunch = false;
        private bool useThermal = true;

        [KSPField(isPersistant = true)]
        public bool temperatureInitialized = false;

        private double saveHeatConductivity;
        private double saveSkinInternalConductionMult;
        private double saveEmissiveConstant;
        private double saveAbsorptiveConstant;
        private double insulationConductionFactor = 0.006;
        private double skinInsulationConductionFactor = 190;
        private double internalTankConductionFactor = 40;
        private double dayLength = 6 * 3600;

        public override void OnAwake()
        {
            if (cryoResources == null)
            {
                cryoResources = new List<CryoResourceItem>();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            cryoResources.Clear();
            if (part.partInfo == null || part.partInfo.partPrefab == null)
            {
                ConfigNode[] cryoResourceNodes = node.GetNodes(CryoResourceItem.itemName);
                //Debug.Log("[ModuleBdbBoiloff] Found " + cryoResourceNodes.Count() + " " + CryoResourceItem.itemName + " nodes");
                foreach (ConfigNode cryoResourceNode in cryoResourceNodes)
                {
                    CryoResourceItem newItem = new CryoResourceItem(cryoResourceNode);
                    //Debug.Log("[ModuleBdbBoiloff] Adding " + newItem.name + ", " + newItem.boiloffRate);
                    if (PartResourceLibrary.Instance.GetDefinition(newItem.name) != null)
                    {
                        cryoResources.Add(newItem);
                    }
                    else
                    {
                        Debug.LogError("[ModuleBdbBoiloff] Resource " + newItem.name + " not found");
                    }

                }
            }
            else
            {
                if (part.partInfo.partPrefab.Modules.Contains("ModuleBdbBoiloff"))
                {
                    ModuleBdbBoiloff prefab = (ModuleBdbBoiloff)part.partInfo.partPrefab.Modules["ModuleBdbBoiloff"];
                    //Debug.Log("[ModuleBdbBoiloff] Loading " + prefab.cryoResources.Count() + " resources from prefab part.");
                    foreach (CryoResourceItem item in prefab.cryoResources)
                    {
                        CryoResourceItem newItem = new CryoResourceItem(item);
                        cryoResources.Add(newItem);
                    }
                }
                else
                {
                    Debug.LogError("[ModuleBdbBoiloff] ModuleCryoResource not found on prefab part.");
                }

            }
        }

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);

            saveHeatConductivity = part.heatConductivity;
            saveSkinInternalConductionMult = part.skinInternalConductionMult;
            saveEmissiveConstant = part.emissiveConstant;
            saveAbsorptiveConstant = part.absorptiveConstant;

            CelestialBody homeWorld = FlightGlobals.GetHomeBody();
            if (homeWorld != null)
                dayLength = homeWorld.solarDayLength;

            if (insulationDeployModuleIndex >= 0 && part.Modules.Count > insulationDeployModuleIndex)
            {
                if (part.Modules[insulationDeployModuleIndex] is IScalarModule)
                {
                    insulationScalarModule = part.Modules[insulationDeployModuleIndex] as IScalarModule;
                    insulationScalarModule.OnStop.Add(OnInsulationChanged);
                }
            }

            UpdateResources();
            UpdatePreLaunch();
            UpdateUI();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

            if (insulationScalarModule != null)
                insulationScalarModule.OnStop.Remove(OnInsulationChanged);
        }

        private void OnVesselWasModified(Vessel v)
        {
            UpdateResources();
            UpdatePreLaunch();
            UpdateUI();
        }

        private void UpdateUI()
        {
            Fields[nameof(boiloffDisplay)].guiActive = hasCryoResource;
            Fields[nameof(heatLeakageDisplay)].guiActive = hasCryoResource && useThermal;
            Fields[nameof(insulationDisplay)].guiActive = hasCryoResource;
            Fields[nameof(reflectivity)].guiActive = hasCryoResource && !useThermal;
            Fields[nameof(reflectivity)].guiActiveEditor = !useThermal;

            Fields[nameof(exposureDisplay)].guiActive = hasCryoResource && debug && !useThermal;
            Fields[nameof(sunFluxDisplay)].guiActive = hasCryoResource && debug && !useThermal;
            Fields[nameof(bodyFluxDisplay)].guiActive = hasCryoResource && debug && !useThermal;
            Fields[nameof(xDisplay)].guiActive = hasCryoResource && debug;

            insulationDisplay = "Int: " + internalInsulation.ToString("P0");
            if (SkinInsulationActive())
                insulationDisplay += " Shell: " + insulation.ToString("P0");
        }

        private void UpdateResources()
        {
            RefreshSettings();
            if (HighLogic.LoadedSceneIsFlight)
            {
                hasCryoResource = false;
                foreach (CryoResourceItem item in cryoResources)
                {
                    //item.loadDatabaseValues();
                    if (part.Resources.Contains(item.name))
                    {
                        hasCryoResource = true;
                    }
                }
                

                if (useThermal)
                {
                    if (hasCryoResource)
                    {
                        if (insulation >= 0)
                        {
                            if (internalInsulation < 0.0f)
                                internalInsulation = insulation;

                            if (SkinInsulationActive())
                            {
                                part.heatConductivity = saveHeatConductivity * Math.Pow(1 - internalInsulation, 2) * insulationConductionFactor;
                                part.skinInternalConductionMult = saveSkinInternalConductionMult * Math.Pow(1 - insulation, 2) * skinInsulationConductionFactor;
                            }
                            else
                            {
                                part.heatConductivity = saveHeatConductivity * Math.Pow(1 - internalInsulation, 2) * insulationConductionFactor;
                                part.skinInternalConductionMult = 1 / part.heatConductivity;
                            }
                        }

                        //if (reflectivity > 0)
                        //    part.absorptiveConstant = 1 - reflectivity;
                    }
                    else
                    {
                        part.heatConductivity = saveHeatConductivity;
                        part.skinInternalConductionMult = saveSkinInternalConductionMult;
                        part.emissiveConstant = saveEmissiveConstant;
                        part.absorptiveConstant = saveAbsorptiveConstant;
                    }
                }
            }
        }

        private void OnInsulationChanged(float at)
        {
            UpdateResources();
            UpdateUI();
        }

        private bool SkinInsulationActive()
        {
            if (insulationScalarModule != null)
                return insulationScalarModule.GetScalar == 0;
            else
                return true;
        }

        private void UpdatePreLaunch()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                var lc = vessel.FindPartModuleImplementing<LaunchClamp>();
                isPreLaunch = (lc != null);
            }
        }

        private void RefreshSettings()
        {
            bool saveUseThermal = useThermal;

            useThermal = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().useThermal;
            boiloffEnabled = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffEnabled;
            boiloffDifficulty = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffMultiplier;

            if (saveUseThermal != useThermal || !useThermal || !boiloffEnabled)
                temperatureInitialized = false;
        }

        public override string GetModuleDisplayName()
        {
            return "Cryogenic Fuel";
        }

        public override string GetInfo()
        {
            string info = "Insulation effectivness: " + insulation.ToString("P0") + "\n";
            info += "Reflectivity: " + reflectivity.ToString("P0") + "\n";

            foreach (CryoResourceItem item in cryoResources)
            {
                double halfLife = item.boiloffRate / boiloffDifficulty;
                double pctLoss = 1 - Math.Pow(0.5, 1 / halfLife);
                info += "\n<B>" + item.name + ":</B>\n";
                //info += "    per hour: " + pctLoss.ToString("P1") + "\n";
                //info += "    half life: " + halfLife.ToString("F1") + " hrs\n";
            }

            if (!boiloffEnabled)
                info += "\n<B>Boiloff is disabled</B>\n";

            return info;
        }

        public string GetInfoEditor()
        {
            RefreshSettings();
            return GetInfo();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && hasCryoResource && boiloffEnabled && !vessel.HoldPhysics)
            {
                if (!isPreLaunch)
                {
                    double currentTime = Planetarium.GetUniversalTime();
                    if (lastUpdateTime < 0)
                    {
                        lastUpdateTime = currentTime;
                    }
                    double deltaTime = currentTime - lastUpdateTime;

                    if (useThermal && deltaTime > 0)
                    {
                        if (!temperatureInitialized)
                            InitializeTemperature();

                        string s = "";
                        if (part.ShieldedFromAirstream)
                            s = "Shielded from airstream";

                        double partTemp = part.temperature;
                        xDisplay = "Part Temp: " + partTemp.ToString("0.000 K");
                        heatLeakageDisplay = 0.0;

                        foreach (CryoResourceItem item in cryoResources)
                        {
                            double resourceAmount = part.Resources[item.name].amount;
                            double resourceMaxAmount = part.Resources[item.name].maxAmount;
                            double resourceTemp = item.tempBoil;
                            
                            xDisplay += ", " + item.abbreviation + resourceTemp.ToString(" 0 K");

                            double deltaTemp = partTemp - resourceTemp;
                            if (deltaTemp > 0)
                            {
                                double surfaceArea = 4 * Math.PI * Math.Pow((resourceMaxAmount / 1000 / item.volume / (4 / 3 / Math.PI)), 2.0 / 3.0); // Sphere
                                surfaceArea *= resourceAmount / resourceMaxAmount;
                                double conduction = part.heatConductivity * PhysicsGlobals.ConductionFactor * internalTankConductionFactor;
                                double internalFlux = conduction * surfaceArea * deltaTemp * (boiloffDifficulty / 0.5);
                                internalFlux /= 1000; // kW
                                xDisplay = "Part Temp: " + partTemp.ToString("0.000 K") + ", " + internalFlux.ToString("0.000 kW");
                                double massLost = internalFlux / item.heatOfVaporization;
                                double evaporativeFlux = resourceTemp * massLost * part.Resources[item.name].info.specificHeatCapacity;

                                massLost *= deltaTime;
                                double unitsLost = massLost / item.density;

                                heatLeakageDisplay += internalFlux;
                                part.AddThermalFlux(-(internalFlux + evaporativeFlux)); // Is it both, or just one?
                                part.Resources[item.name].amount = Math.Max(resourceAmount - unitsLost, 0);

                                if (item.hasOutput)
                                {
                                    double outputAmount = -(unitsLost * item.outputRatio * item.outputRate);
                                    part.RequestResource(item.outputResource, outputAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                                }

                                if (s != "")
                                {
                                    s += ", ";
                                }
                                s += item.abbreviation + " " + ((unitsLost * (1 / deltaTime) * dayLength) / resourceMaxAmount).ToString("P1") + "/day";
                            }
                            item.lastAmount = part.Resources[item.name].amount;
                        }
                        boiloffDisplay = s;
                        lastUpdateTime = currentTime;

                    }
                    else if (deltaTime > 0)
                    {
                        string s = "";
                        if (part.ShieldedFromAirstream)
                            s = "Shielded from airstream";

                        double Q = radExposure() * boiloffFactor * boiloffDifficulty * (1 - insulation) * (1 - reflectivity / 2);

                        foreach (CryoResourceItem item in cryoResources)
                        {
                            double resourceAmount = part.Resources[item.name].amount; // will nullref if a resource is missing
                            double resourceMass = resourceAmount * item.density;
                            
                            double lossRate = 0.0;
                            
                            if (resourceMass > 0)
                                lossRate = Q / (item.heatOfVaporization * resourceMass); // T per second
                            
                            if (lossRate > 0)
                            {
                                if (item.lastLossRate < 0)
                                    item.lastLossRate = lossRate;

                                double smoothTime = 5; // number of seconds to it takes to move halfway from old loss rate to new loss rate
                                double smoothDelta = lossRate - item.lastLossRate;
                                double smoothChange = (1 - Math.Pow(0.5, deltaTime / smoothTime)) * smoothDelta;
                                lossRate = item.lastLossRate + smoothChange;

                                double deltaAmount = lossRate / item.density * deltaTime; // Resource units boiled this tick

                                double resourceConsumed = item.lastAmount - resourceAmount; // Amount being drawn from tank, i.e. engine running.
                                if (resourceConsumed > 0)
                                {
                                    deltaAmount = Math.Max(deltaAmount - resourceConsumed, 0);
                                    deltaAmount = Math.Min(deltaAmount, resourceAmount);
                                }


                                if (deltaAmount > 0)
                                {
                                    //part.RequestResource(item.name, deltaAmount, ResourceFlowMode.NO_FLOW);
                                    part.Resources[item.name].amount = Math.Max(resourceAmount - deltaAmount, 0);

                                    if (item.hasOutput)
                                    {
                                        double outputAmount = -(deltaAmount * item.outputRatio * item.outputRate);
                                        part.RequestResource(item.outputResource, outputAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                                    }
                                }
                                if (s != "")
                                {
                                    s += ", ";
                                }
                                s += item.abbreviation + " " + ((deltaAmount * (1 / deltaTime) * 60 * 60) / part.Resources[item.name].maxAmount).ToString("P1") + "/hr";
                            }
                            item.lastAmount = part.Resources[item.name].amount;
                            item.lastLossRate = lossRate;
                        }
                        boiloffDisplay = s;
                        lastUpdateTime = currentTime;
                    }
                }
                else
                {
                    boiloffDisplay = "Pre-Launch";
                    lastUpdateTime = -1;
                    if (useThermal)
                        InitializeTemperature();
                }
            }
            else
            {
                boiloffDisplay = "Disabled";
                lastUpdateTime = -1;
            }
        }

        private void InitializeTemperature()
        {
            temperatureInitialized = true;

            double maxTemp = 0.0;

            foreach (CryoResourceItem item in cryoResources)
            {
                if (item.tempBoil > maxTemp)
                    maxTemp = item.tempBoil;
            }

            if (maxTemp <= 0.0 || maxTemp > vessel.atmosphericTemperature)
                return;

            part.temperature = (maxTemp + (vessel.atmosphericTemperature - maxTemp) * 0.1);
            part.skinTemperature = part.temperature;
        }

        private double radExposure()
        {
            double exposure = 0;
            xDisplay = "PTD";
            if (part.ptd != null)
            {
                sunFluxDisplay = part.ptd.sunFlux * part.ptd.sunAreaMultiplier;
                bodyFluxDisplay = part.ptd.bodyFlux * part.ptd.bodyAreaMultiplier;

                exposure = part.ptd.sunFlux * part.radiativeArea * part.ptd.sunAreaMultiplier / 2;
                exposure += part.ptd.bodyFlux * part.radiativeArea * part.ptd.bodyAreaMultiplier / 2;

                xDisplay = "convectionFlux: " + part.ptd.convectionFlux.ToString("0.00");
            }
            else xDisplay = "PTD is null";
            exposureDisplay = exposure;
            return exposure;
        }
    }

    class CryoResourceItem
    {
        public static string itemName = "CRYOGENICRESOURCE";
        public string name = "";
        public int id = 0;
        public string abbreviation = "";
        public double boiloffRate = -1.0;
        public double lastAmount = -1.0;
        public double lastLossRate = -1.0;
        public string outputResource = "";
        public double outputRate = 1.0; // leakage. multiplier for output 0..1
        public double outputRatio = 0.0; // X Parts Gas to 1 Part Liquid = liquid density / output gas density
        public bool hasOutput = false;
        public double density = 0.0;
        public double volume = 0.0;
        public double heatOfVaporization = 0.0; // heat of vapourization
        public double specificHeatCapacity = 0.0; // specific heat capacity
        public double tempBoil = 0.0;

        public CryoResourceItem()
        {
        }
        public CryoResourceItem(CryoResourceItem source)
        {
            name = source.name;
            id = source.id;
            abbreviation = source.abbreviation;
            boiloffRate = source.boiloffRate;
            outputResource = source.outputResource;
            outputRate = source.outputRate;
            outputRatio = source.outputRatio;
            hasOutput = source.hasOutput;
            density = source.density;
            volume = source.volume;
            heatOfVaporization = source.heatOfVaporization;
            specificHeatCapacity = source.specificHeatCapacity;
            tempBoil = source.tempBoil;
        }
        public CryoResourceItem(ConfigNode node)
        {
            name = GetStringValue(node, "name", name);
            boiloffRate = GetDoubleValue(node, "boiloffRate", boiloffRate);
            outputResource = GetStringValue(node, "outputResource", outputResource);
            outputRate = GetDoubleValue(node, "outputRate", outputRate);

            tempBoil = 20; // LqdHydrogen

            loadDatabaseValues();
        }

        public void loadDatabaseValues()
        {
            PartResourceDefinition res = PartResourceLibrary.Instance.GetDefinition(name);
            id = res.id;
            abbreviation = res.abbreviation;
            density = res.density;
            volume = res.volume;
            specificHeatCapacity = res.specificHeatCapacity;

            foreach (ConfigNode resDef in GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION"))
            {
                if (resDef.GetValue("name") == name)
                {
                    heatOfVaporization = GetDoubleValue(resDef, "vsp", heatOfVaporization);
                    break;
                }
            }

            hasOutput = outputResource != "";
            if (hasOutput)
            {
                double outputDensity = PartResourceLibrary.Instance.GetDefinition(outputResource).density;
                outputRatio = density / outputDensity;
                outputRate = Math.Min(Math.Max(outputRate, 0), 1.0); // Clamp
            }
        }

        public static string GetStringValue(ConfigNode node, string name, string defaultValue = "")
        {
            if (node.HasValue(name))
            {
                return node.GetValue(name);
            }
            else
            {
                return defaultValue;
            }
        }

        public static double GetDoubleValue(ConfigNode node, string name, double defaultValue = 0.0)
        {
            double returnValue = 0.0;
            if (node.HasValue(name) && double.TryParse(node.GetValue(name), out returnValue))
            {
                return returnValue;
            }
            else
            {
                return defaultValue;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class BdbBoiloffPartInfo : MonoBehaviour
    {
        public void Start()
        {
            IEnumerable<AvailablePart> pl = PartLoader.LoadedPartsList.Where(part => part.partPrefab.Modules != null && part.partPrefab.Modules.Contains<ModuleBdbBoiloff>());
            
            foreach (AvailablePart p in pl)
            {
                for (int i = 0; i < p.moduleInfos.Count; i++)
                {
                    AvailablePart.ModuleInfo mi = p.moduleInfos[i];
                    if (mi.moduleName == "Bdb Boiloff")
                    {
                        string s = "";
                        ModuleBdbBoiloff pm = p.partPrefab.GetComponent<ModuleBdbBoiloff>();
                        if (pm != null)
                            s = pm.GetInfoEditor();

                        if (s != "")
                            mi.info = s;
                        else
                            p.moduleInfos.Remove(mi);

                        break;
                    }
                }
            }
        }
    }
}
