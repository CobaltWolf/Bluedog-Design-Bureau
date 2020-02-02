using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDB
{
    class ModuleBdbLiquifier : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Liquifier"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool isActive = false;

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Load")]
        public string loadDisplay = "";

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Power")]
        public string powerDisplay = "";

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Output")]
        public string outputDisplay = "";

        [KSPField(isPersistant = true)]
        public double lastUpdateTime = -1.0;

        private double liquidDensity;
        private double liquidVSP = 448500; // heat of vapourization (KJ/tonne as units)

        private int pauseCtr;
        private bool firstPass = true;

        public void Start()
        {
            liquidDensity = PartResourceLibrary.Instance.GetDefinition("LqdHydrogen").density;
            pauseCtr = 3;
        }

        public void FixedUpdate()
        {
            loadDisplay = "Off";
            powerDisplay = "-.-/sec";
            outputDisplay = "-.-/sec";
            if (pauseCtr > 0)
                pauseCtr--;

            if (isActive && HighLogic.LoadedSceneIsFlight && pauseCtr == 0)
            {
                double currentTime = Planetarium.GetUniversalTime();
                if (lastUpdateTime < 0)
                {
                    lastUpdateTime = currentTime;
                }
                double deltaTime = currentTime - lastUpdateTime;
                if (deltaTime > 0)
                {
                    double efficiency = 1;
                    double scale = efficiency * deltaTime;
                    double gasParts = 788.0978865;
                    double ecParts = 60;
                    double liquidParts = 1;

                    if (scale > 0)
                    {
                        scale = Math.Min(scale, scale * (TestResource("Hydrogen", gasParts * scale) / (gasParts * scale)));
                        if (scale == 0)
                            loadDisplay = "No gas supply";
                    }
                        
                    if (!firstPass && scale > 0)
                    {
                        scale = Math.Min(scale, scale * (TestResource("ElectricCharge", ecParts * scale) / (ecParts * scale)));
                        if (scale == 0)
                            loadDisplay = "No EC supply";
                    }
                        
                    if (scale > 0)
                    {
                        scale = Math.Min(scale, scale * (TestResource("LqdHydrogen", -liquidParts * scale) / (-liquidParts * scale)));
                        if (scale == 0)
                            loadDisplay = "No liquid destination";
                    }
                        

                    if (scale > 0)
                    {
                        gasParts = gasParts * scale;
                        ecParts = ecParts * scale;
                        liquidParts = liquidParts * scale;

                        double gasAmt = FetchResource("Hydrogen", gasParts, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                        double partsFraction = gasAmt / gasParts;
                        //loadDisplay = ((scale * partsFraction) / deltaTime).ToString("P2");
                        loadDisplay = deltaTime.ToString("F6") + " scale " + scale.ToString("F6");

                        if (!firstPass) // free ec on the first time through.
                        {
                            ecParts = ecParts * partsFraction;
                            double ecAmt = FetchResource("ElectricCharge", ecParts, ResourceFlowMode.ALL_VESSEL);
                            powerDisplay = (ecAmt / deltaTime).ToString("F1") + "/sec";
                            if (ecAmt < ecParts)
                            {
                                // fail?
                                Debug.Log("[ModuleBdbLiquifier] ecAmt " + ecAmt.ToString("F5") + " < ecparts " + ecParts.ToString("F5"));
                            }
                        }

                        liquidParts = liquidParts * partsFraction;
                        double liquidAmt = -FetchResource("LqdHydrogen", -liquidParts, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                        outputDisplay = (liquidAmt / deltaTime).ToString("F1") + "/sec";
                        if (liquidAmt != liquidParts)
                        {
                            Debug.Log("[ModuleBdbLiquifier] liquidAmt " + liquidAmt.ToString("F5") + " != liquidParts " + liquidParts.ToString("F5"));
                        }

                        // gas/liquid phase change
                        // heatRelease in kJ = mass * vsp (448500)
                        // kW = kJ / deltaTime
                        // part.AddThermalFlux(kW)
                        // ModuleCoreHeat.AddEnergyToCore(kW)
                        double heatRelease = (liquidAmt * liquidDensity * liquidVSP) / deltaTime;
                        part.AddThermalFlux(heatRelease);
                    }
                    lastUpdateTime = currentTime;
                    firstPass = false;
                }

            }
            else
            {
                lastUpdateTime = -1;
            }

        }

        private double TestResource(string resourceName, double need)
        {
            double amount;
            double maxAmount;
            vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition(resourceName).id, out amount, out maxAmount);
            if (need > 0)
            {
                if (amount > need)
                    return need;
                else
                    return amount;
            }
            else
            {
                double space = -(maxAmount - amount);
                if (space < need)
                    return need;
                else
                    return space;
            }
            
        }

        private double FetchResource(string resourceName, double demand, ResourceFlowMode flowMode)
        {
            double todo = demand;
            double done = 0;
            while (Math.Abs(todo) > 0)
            {
                done = part.RequestResource(resourceName, todo, flowMode);
                if (done == 0)
                    break;

                todo = todo - done;
            }
            return demand - todo;
        }
    }
}
