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

        [KSPField(isPersistant = true)]
        public double lastDeltaAmount = 0.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Boiloff")]
        public string boiloffDisplay = "";

        [KSPField(guiActive = false, isPersistant = false, guiActiveEditor = false, guiName = "Exposure")]
        public string exposureDisplay = "";

        [KSPField(isPersistant = false)]
        public bool debug = false;

        private List<CryoResourceItem> cryoResources;
        private bool boiloffEnabled;
        private double boiloffMultiplier;
        private bool hasCryoResource = false;
        private bool isPreLaunch = false;
        private double homeAltAboveSun = 13599840256; // Stock Kerbin

        public override void OnAwake()
        {
            if (cryoResources == null)
            {
                cryoResources = new List<CryoResourceItem>();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (part.partInfo == null || part.partInfo.partPrefab == null)
            {
                cryoResources.Clear();
                ConfigNode[] cryoResourceNodes = node.GetNodes(CryoResourceItem.itemName);
                //Debug.Log("[ModuleBdbBoiloff] Found " + cryoResourceNodes.Count() + " " + CryoResourceItem.itemName + " nodes");
                foreach (ConfigNode cryoResourceNode in cryoResourceNodes)
                {
                    CryoResourceItem newItem = new CryoResourceItem(cryoResourceNode);
                    Debug.Log("[ModuleBdbBoiloff] Adding " + newItem.name + ", " + newItem.boiloffRate);
                    if (PartResourceLibrary.Instance.GetDefinition(newItem.name) != null)
                    {
                        cryoResources.Add(newItem);
                    }
                    else
                    {
                        Debug.Log("[ModuleBdbBoiloff] Resource " + newItem.name + " not found");
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
                    Debug.Log("[ModuleBdbBoiloff] ModuleCryoResource not found on prefab part.");
                }

            }
        }

        public void Start()
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            homeAltAboveSun = FlightGlobals.getAltitudeAtPos(FlightGlobals.GetHomeBody().position, FlightGlobals.Bodies[0]);
            Fields["exposureDisplay"].guiActive = debug;
            UpdateResources();
            UpdatePreLaunch();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            UpdateResources();
            UpdatePreLaunch();
        }

        private void UpdateResources()
        {
            RefreshSettings();
            if (HighLogic.LoadedSceneIsFlight)
            {
                hasCryoResource = false;
                foreach (CryoResourceItem item in cryoResources)
                {
                    if (part.Resources.Contains(item.name))
                    {
                        hasCryoResource = true;
                        break;
                    }
                }
                Fields["boiloffDisplay"].guiActive = hasCryoResource;
            }
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
            boiloffEnabled = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffEnabled;
            boiloffMultiplier = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffMultiplier;
        }

        public override string GetModuleDisplayName()
        {
            return "Cryogenic Fuel";
        }

        public override string GetInfo()
        {
            string info = "Maximum boiloff rate in sunlight\n";

            foreach (CryoResourceItem item in cryoResources)
            {
                double halfLife = item.boiloffRate / boiloffMultiplier;
                double pctLoss = 1 - Math.Pow(0.5, 1 / halfLife);
                info += "\n<B>" + item.name + ":</B>\n";
                info += "    per hour: " + pctLoss.ToString("P1") + "\n";
                info += "    half life: " + halfLife.ToString("F1") + " hrs\n";
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
            if (HighLogic.LoadedSceneIsFlight && hasCryoResource && boiloffEnabled)
            {
                if (!isPreLaunch)
                {
                    double currentTime = Planetarium.GetUniversalTime();
                    if (lastUpdateTime < 0)
                    {
                        lastUpdateTime = currentTime;
                    }
                    double deltaTime = currentTime - lastUpdateTime;
                    if (deltaTime > 0)
                    {
                        string s = "";
                        foreach (CryoResourceItem item in cryoResources)
                        {
                            double halfLife = item.boiloffRate / boiloffMultiplier * 60 * 60;
                            if (part.ShieldedFromAirstream)
                            {
                                halfLife *= 10; // We'll pretend shielding acts as insulation.
                            }

                            halfLife = halfLife / Math.Max(0.01, sunExposure());

                            double resourceAmount = part.Resources[item.name].amount; // will nullref if a resource is missing
                            if (halfLife > 0 && resourceAmount > 0)
                            {
                                double amt0 = resourceAmount;
                                double amtT = amt0 * Math.Pow(0.5, deltaTime / halfLife);
                                double deltaAmountTgt = amt0 - amtT;

                                deltaAmountTgt = deltaAmountTgt / deltaTime; // per sec for smoothing calc
                                double maxRateChange = 1.0 / 60 / 60; // X/hr/sec
                                double deltaAmount = lastDeltaAmount + Math.Min(maxRateChange, Math.Max(-maxRateChange, (deltaAmountTgt - lastDeltaAmount))) * deltaTime; // smooth the rate change
                                deltaAmount = deltaAmount * deltaTime; // back from per sec

                                double resourceConsumed = item.lastAmount - resourceAmount; 
                                if (resourceConsumed > 0)
                                {
                                    // Amount being drawn from tank, i.e. engine running.
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
                                s += item.name + " " + (deltaAmount * (1 / deltaTime) * 60 * 60).ToString("0.0") + "/hr";
                                lastDeltaAmount = deltaAmount / deltaTime; // per sec
                            }
                            item.lastAmount = part.Resources[item.name].amount;
                        }
                        boiloffDisplay = s;
                        lastUpdateTime = currentTime;
                    }
                }
                else
                {
                    boiloffDisplay = "Pre-Launch";
                    lastUpdateTime = -1;
                }
                exposureDisplay = (sunExposure()).ToString(); //(part.ptd.bodyFlux * part.ptd.bodyAreaMultiplier).ToString();
                //sunFluxDisplay = (part.ptd.sunAreaMultiplier).ToString(); //(part.ptd.sunFlux * part.ptd.sunAreaMultiplier).ToString();
            }
            else
            {
                boiloffDisplay = "Disabled";
                lastUpdateTime = -1;
            }
        }

        private double sunExposure()
        {
            double altAboveSun = FlightGlobals.getAltitudeAtPos(vessel.GetWorldPos3D(), FlightGlobals.Bodies[0]);
            double solarPower = (homeAltAboveSun * homeAltAboveSun) / (altAboveSun * altAboveSun);

            double solarExposure = 0;
            if (part.ptd != null && part.ptd.sunFlux > 0)
                solarExposure = 1;//part.ptd.sunAreaMultiplier;

            return solarExposure * solarPower;
        }
    }

    class CryoResourceItem
    {
        public static string itemName = "CRYOGENICRESOURCE";
        public string name = "";
        public double boiloffRate = -1.0;
        public double lastAmount = -1.0;
        public string outputResource = "";
        public double outputRate = 1.0; // leakage. multiplier for output 0..1
        public double outputRatio = 0.0; // X Parts Gas to 1 Part Liquid = liquid density / output gas density
        public bool hasOutput = false;

        public CryoResourceItem()
        {
        }
        public CryoResourceItem(CryoResourceItem source)
        {
            name = source.name;
            boiloffRate = source.boiloffRate;
            outputResource = source.outputResource;
            outputRate = source.outputRate;
            setupOutput();
        }
        public CryoResourceItem(ConfigNode node)
        {
            name = GetStringValue(node, "name", name);
            boiloffRate = GetDoubleValue(node, "boiloffRate", boiloffRate);
            outputResource = GetStringValue(node, "outputResource", outputResource);
            outputRate = GetDoubleValue(node, "outputRate", outputRate);
            setupOutput();
        }

        private void setupOutput()
        {
            hasOutput = outputResource != "";
            if (hasOutput)
            {
                double density = PartResourceLibrary.Instance.GetDefinition(name).density;
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
