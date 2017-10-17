using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbBoiloff : PartModule
    {
        [KSPField(isPersistant = true)]
        public double lastUpdateTime = -1.0;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Boiloff")]
        public string boiloffDisplay = "";

        private List<CryoResourceItem> cryoResources;
        private bool boiloffEnabled;
        private double boiloffMultiplier;
        private bool hasCryoResource = false;
        private bool isPreLaunch = false;

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
            if (HighLogic.LoadedSceneIsFlight)
            {
                boiloffEnabled = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffEnabled;
                boiloffMultiplier = HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffMultiplier;
                
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

            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            UpdatePreLaunch();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            UpdatePreLaunch();
        }

        private void UpdatePreLaunch()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                var lc = vessel.FindPartModuleImplementing<LaunchClamp>();
                isPreLaunch = (lc != null);
            }
        }

        public void Update()
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
                            double resourceAmount = part.Resources[item.name].amount;
                            if (halfLife > 0 && resourceAmount > 0)
                            {
                                double amt0 = resourceAmount;
                                double amtT = amt0 * Math.Pow(0.5, deltaTime / halfLife);
                                double deltaAmount = amt0 - amtT;

                                double resourceConsumed = Math.Max(item.lastAmount - resourceAmount, 0); // Amount being drawn from tank, i.e. engine running.
                                deltaAmount = Math.Max(deltaAmount - resourceConsumed, 0);

                                if (deltaAmount > 0)
                                {
                                    //part.RequestResource(item.name, deltaAmount, ResourceFlowMode.NO_FLOW);
                                    part.Resources[item.name].amount = Math.Max(resourceAmount - deltaAmount, 0);
                                }
                                if (s != "")
                                {
                                    s += ", ";
                                }
                                s += item.name + " " + (deltaAmount * (1 / deltaTime) * 60 * 60).ToString("0.0") + "/hr";
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
            }
            else
            {
                boiloffDisplay = "Disabled";
                lastUpdateTime = -1;
            }
        }
    }

    class CryoResourceItem
    {
        public static string itemName = "CRYOGENICRESOURCE";
        public string name = "";
        public double boiloffRate = -1.0;
        public double lastAmount = -1.0;

        public CryoResourceItem()
        {
        }
        public CryoResourceItem(CryoResourceItem source)
        {
            name = source.name;
            boiloffRate = source.boiloffRate;
        }
        public CryoResourceItem(ConfigNode node)
        {
            name = GetStringValue(node, "name", name);
            boiloffRate = GetDoubleValue(node, "boiloffRate", boiloffRate);
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
}
