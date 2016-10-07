using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private double dayLength;
        private bool hasCryoResource = false;

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
                Debug.Log("[ModuleBdbBoiloff] Found " + cryoResourceNodes.Count() + " " + CryoResourceItem.itemName + " nodes");
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
                    Debug.Log("[ModuleBdbBoiloff] Loading " + prefab.cryoResources.Count() + " resources from prefab part.");
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
                CelestialBody homeBody = FlightGlobals.GetHomeBody();
                double period = homeBody.orbit.period;
                dayLength = homeBody.solarDayLength; // can this be zero?
                double periodDays = period / dayLength;
                Debug.LogFormat("Home body {0} year length: {1}, {2}", homeBody.bodyName, periodDays.ToString("0.0"), dayLength.ToString("0.00"));

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

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && hasCryoResource && HighLogic.CurrentGame.Parameters.CustomParams<BdbCustomParams>().boiloffEnabled)
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
                        double halfLife = item.boiloffRate * dayLength;

                        if (halfLife > 0 && part.Resources.Contains(item.name) && part.Resources[item.name].amount > 0)
                        {
                            double amt0 = part.Resources[item.name].amount;
                            double amtT = amt0 * Math.Pow(0.5, deltaTime / halfLife);
                            double deltaAmount = amt0 - amtT;
                            part.RequestResource(item.name, deltaAmount, ResourceFlowMode.NO_FLOW);
                            if (s != "")
                                s += ", ";
                            s += item.name + " " + (deltaAmount * (1 / deltaTime) * 60 * 60).ToString("0") + "/hr";
                        }
                    }
                    boiloffDisplay = s;
                    lastUpdateTime = currentTime;
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
        public double boiloffRate = -1.0f;

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
