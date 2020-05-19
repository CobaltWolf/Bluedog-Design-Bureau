using System;
using UnityEngine;

namespace BDB
{
    class ModuleBdbCoronaHS : PartModule, IAirstreamShield
    {
        [KSPField]
        public string shieldedNodeID = "bottom";

        private Callback<IAirstreamShield> shieldCallBack;
        private Part shieldedPart;
        private AttachNode shieldedNode;

        

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            shieldedNode = part.FindAttachNode(shieldedNodeID);
            if (shieldedNode == null)
                Debug.Log("[ModuleBdbCoronaHS] shieldedNodeID " + "'" + shieldedNodeID + "' not found.");
            CheckShieldedPart();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }

        private void OnVesselWasModified(Vessel v)
        {
            CheckShieldedPart();
        }

        private void CheckShieldedPart()
        {
            if (shieldedNode != null)
            {
                if (shieldedNode.attachedPart != null)
                {
                    shieldedPart = shieldedNode.attachedPart;
                    shieldCallBack = shieldedPart.AddShield(this);
                    ShieldModified();
                    Debug.Log("[ModuleBdbCoronaHS] Added shielding to " + shieldedPart.partInfo.name);
                }
                else
                {
                    if (shieldedPart != null)
                    {
                        Debug.Log("[ModuleBdbCoronaHS] Remove shielding from " + shieldedPart.partInfo.name);
                        shieldedPart.RemoveShield(this);
                        shieldedPart = null;
                        ShieldModified();
                        shieldCallBack = null;
                    }
                }
            }
        }

        private void ShieldModified()
        {
            shieldCallBack?.Invoke(this);
        }

        #region IAirstreamShield
        public bool ClosedAndLocked()
        {
            return shieldedPart != null;
        }

        public Part GetPart()
        {
            return part;
        }

        public Vessel GetVessel()
        {
            return vessel;
        }
        #endregion
    }

}
