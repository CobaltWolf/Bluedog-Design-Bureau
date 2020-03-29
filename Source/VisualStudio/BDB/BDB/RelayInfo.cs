using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbRelayInfo : PartModule, IModuleInfo
    {
        double homeworldRadius = 0;
        double homeworldTopOfAtmo = 0;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Show Relay Info")]
        public void ShowInfo()
        {
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), GetModuleTitle(), GetModuleDisplayName(), GetNetworkInfo(), "Close", false, HighLogic.UISkin);
        }

        public string GetModuleTitle()
        {
            return "Relay Info";
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return string.Empty;
        }

        public override string GetModuleDisplayName()
        {
            return "Network Info";
        }

        public override string GetInfo()
        {
            return string.Empty;
        }

        public string GetNetworkInfo()
        {
            if (!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor))
                return "Empty";

            string info = FlightGlobals.GetHomeBody().displayName + " Relay Constellation\n";
            homeworldRadius = FlightGlobals.GetHomeBody().Radius;
            homeworldTopOfAtmo = FlightGlobals.GetHomeBody().atmosphereDepth;
            double rp = FlightGlobals.GetHomeBody().rotationPeriod;
            double GM = FlightGlobals.GetHomeBody().gravParameter;
            double geoAlt = Math.Pow(Math.Pow(rp / (2 * Math.PI), 2) * GM, 1.0 / 3.0) - homeworldRadius;
            double commPower = 0;

            //info += "Radius: " + (homeworldRadius / 1000).ToString("n0") + "km" + "\n";
            //info += "Atmosphere Height: " + (homeworldTopOfAtmo / 1000).ToString("n0") + "km" + "\n";

            // CommNet Math https://wiki.kerbalspaceprogram.com/wiki/CommNet
            double combStrongestPower = 0;
            double combSumOfPowers = 0;
            double combWeightedSumOfPowers = 0;
            double strongestPower = 0;
            ICommAntenna strongestAntenna = null;
            bool thisPartCombinable = false;
            bool combinableNotStrongest = false;

            List<ICommAntenna> antennas;
            if (HighLogic.LoadedSceneIsFlight)
            {
                antennas = vessel.FindPartModulesImplementing<ICommAntenna>();
            }
            else
            {
                antennas = new List<ICommAntenna>();
                foreach (Part p in EditorLogic.fetch.ship.parts)
                {
                    antennas.AddRange(p.FindModulesImplementing<ICommAntenna>());
                }
            }

            foreach (ICommAntenna antenna in antennas)
            {
                if (antenna.CommType == AntennaType.RELAY)
                {
                    if (antenna.CommPower > strongestPower)
                    {
                        strongestAntenna = antenna;
                        strongestPower = antenna.CommPower;
                    }

                    if (antenna.CommCombinable)
                    {
                        if (antenna.CommPower > combStrongestPower)
                            combStrongestPower = antenna.CommPower;
                        combSumOfPowers += antenna.CommPower;
                        combWeightedSumOfPowers += antenna.CommPower * antenna.CommCombinableExponent;
                    }
                }
            }
            if (combStrongestPower > 0)
            {
                double combAvgCombExponent = combWeightedSumOfPowers / combSumOfPowers;
                commPower = combStrongestPower * Math.Pow(combSumOfPowers / combStrongestPower, combAvgCombExponent);
            }
            
            if (commPower < strongestPower)
            {
                combinableNotStrongest = true;
                commPower = strongestPower;
            }
            
            if (commPower > 0)
            {
                // Right triangle
                // a = 1/2 power
                // b = homeworld core to midpoint between sats
                // c = homeworldRadius + sat altitude
                // 
                double a = commPower / 2 * 0.8042; // 10% signal strength
                double b = homeworldRadius;
                double c = Math.Sqrt(Math.Pow(b, 2) + Math.Pow(a, 2));
                if (c < homeworldRadius + homeworldTopOfAtmo)
                {
                    c = homeworldRadius + homeworldTopOfAtmo;
                    b = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(c, 2));
                }

                double angleA = Math.Asin(a / c);
                int minSats = (int)Math.Ceiling(Math.PI / angleA);
                double basicPower = 500000 * (homeworldRadius / 600000); // Scaled Communotron 16

                info += "\n<b>Min Sats:</b> " + minSats.ToString("n0") + "\n";
                info += "<b>Best* Alt:</b> " + FormatAltitudeKM(c - homeworldRadius) + "km\n";
                info += "<b>Signal loss to " + FormatAltitudeKM(basicPower) + "K ant:</b> " + FormatAltitudeKM(Math.Sqrt(basicPower * commPower)) + "km\n";
                info += "<b>Geosync:</b> " + Math.Round(geoAlt / 1000, 3).ToString("n3") + "km\n\n";

                c = homeworldRadius + commPower * 0.8042;
                angleA = Math.Asin(a / c);
                int maxSats = (int)Math.Ceiling(Math.PI / angleA);

                info += "<color=green># Sats: Min / Max Alt / Signal to " + FormatAltitudeKM(basicPower) + "K antenna</color>\n";
                for (int i = minSats; i <= maxSats + 1; i++)
                {
                    //info += "\n";
                    info += GetNetworkInfoLine(i, commPower, basicPower) + "\n";
                }
                info += "\n<i>*Best alt is the altitude where a constellation member can connect to any other member that is over the horizon.</i>";
            }
            else
            {
                info += "No relay antennas found!\n";
                info += "commPower: " + commPower.ToString("n") + "\n";
                info += "antennas: " + antennas.Count.ToString("n") + "\n";
            }

            return info;
        }

        private string GetNetworkInfoLine(int numSats, double commPower, double surfPower)
        {
            string info = numSats.ToString() + ": ";
            double angleA = Math.PI / numSats;

            double b = homeworldRadius;
            double a = b * Math.Tan(angleA);
            double c = b / Math.Cos(angleA);
            info += FormatAltitudeKM(Math.Max(homeworldTopOfAtmo, c - homeworldRadius)) + " / ";

            a = commPower / 2 * 0.8042;
            c = a / Math.Sin(angleA);
            info += FormatAltitudeKM(Math.Min(commPower, c - homeworldRadius)) + "km";

            // CommNet Math https://wiki.kerbalspaceprogram.com/wiki/CommNet
            double signalRange = Math.Sqrt(commPower * surfPower);
            double signalPercent = Math.Max(0, 1 - (c - homeworldRadius) / signalRange);
            double signalStrength = (3 - 2 * signalPercent) * Math.Pow(signalPercent, 2);
            info += " / " + signalStrength.ToString("P1");
            return info;
        }

        private string FormatAltitudeKM(double altitude)
        {
            return Math.Floor(altitude / 1000).ToString("n0");
        }

        private double Deg(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        private double Rad(double degrees)
        {
            return (Math.PI / 180) * degrees;
        }

        //public static bool In<T>(this T item, params T[] list)
        //{
        //    return list.Contains(item);
        //}

    }

}
