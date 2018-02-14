using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbWetLab : PartModule
    {
        [KSPField()]
        public string labResource = "ElectricCharge";

        [KSPField()]
        public int crewCapacity = 2;

        private float saveLabStorage = 0.0f;
        

        public void Start()
        {
            ModuleScienceLab scienceLab = this.GetComponent<ModuleScienceLab>();
            if (scienceLab != null)
            {
                saveLabStorage = scienceLab.dataStorage;
            }
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            UpdateLab();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            UpdateLab();
        }

        private void UpdateLab()
        {
            ModuleScienceLab scienceLab = this.GetComponent<ModuleScienceLab>();
            if (part.Resources.Contains(labResource))
            {
                if (scienceLab != null)
                {
                    // Enable the lab
                    scienceLab.dataStorage = saveLabStorage;
                }
                SetCrewCapacity(crewCapacity);
            }
            else
            {
                if (scienceLab != null)
                {
                    // Disable the lab
                    scienceLab.dataStorage = 0.0f;
                }
                SetCrewCapacity(0);
            }
        }

        private void SetCrewCapacity(int capacity)
        {
            if (HighLogic.LoadedSceneIsEditor) // We can't disable the switch if crew are loaded, just prevent crew in the editor
                capacity = 0;

            part.CrewCapacity = capacity;
            if (capacity > 0)
            {
                part.crewTransferAvailable = true;
                if (HighLogic.LoadedSceneIsFlight)
                    part.SpawnIVA();
            }
            else
            {
                part.crewTransferAvailable = false;
                if (HighLogic.LoadedSceneIsFlight)
                    part.DespawnIVA();
            }
            part.CheckTransferDialog();
        }
    }
}
