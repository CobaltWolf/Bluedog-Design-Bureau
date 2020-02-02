using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace BDB
{
    public class ModuleBdbSymmetricalPart : PartModule
    {
        [KSPField(isPersistant = false)]
        public string transformNameA;
        [KSPField(isPersistant = false)]
        public string transformNameB;

        protected bool updateSolar = false;

        [KSPField(isPersistant = false)]
        public string raycastTransformNameA;
        [KSPField(isPersistant = false)]
        public string raycastTransformNameB;
        [KSPField(isPersistant = false)]
        public string pivotNameA;
        [KSPField(isPersistant = false)]
        public string pivotNameB;

        public Transform[] transformsA;
        public Transform[] transformsB;
        public Transform raycastTransformA;
        public Transform raycastTransformB;
        public Transform pivotA;
        public Transform pivotB;
        public ModuleDeployableSolarPanel solarPanel = null;

        [KSPField(isPersistant = true)]
        public bool isSideA = true;

        [KSPField(isPersistant = false)]
        public string toggleSideEventGUINameA = "Side A";
        [KSPField(isPersistant = false)]
        public string toggleSideEventGUINameB = "Side B";

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            transformsA = part.FindModelTransforms(transformNameA);
            transformsB = part.FindModelTransforms(transformNameB);
            raycastTransformA = part.FindModelTransforms(raycastTransformNameA).FirstOrDefault();
            raycastTransformB = part.FindModelTransforms(raycastTransformNameB).FirstOrDefault();
            pivotA = part.FindModelTransforms(pivotNameA).FirstOrDefault();
            pivotB = part.FindModelTransforms(pivotNameB).FirstOrDefault();
            if (raycastTransformA == null)
                Debug.Log("[ModuleBdbSymmetricalPart]: OnStart() raycastTransformA is null");
            if (raycastTransformB == null)
                Debug.Log("[ModuleBdbSymmetricalPart]: OnStart() raycastTransformB is null");
            if (pivotA == null)
                Debug.Log("[ModuleBdbSymmetricalPart]: OnStart() pivotA is null");
            if (pivotB == null)
                Debug.Log("[ModuleBdbSymmetricalPart]: OnStart() pivotB is null");
            if (raycastTransformA != null && raycastTransformB != null && pivotA != null && pivotB != null)
            {
                solarPanel = this.GetComponents<ModuleDeployableSolarPanel>().FirstOrDefault<ModuleDeployableSolarPanel>();
                updateSolar = solarPanel != null;
                if (solarPanel == null)
                    Debug.Log("[ModuleBdbSymmetricalPart]: OnStart() solarPanel is null");
            }

            UpdateTransforms();
            if (state == StartState.Editor)
                this.part.OnEditorAttach += OnEditorAttach;
        }

        public void OnEditorAttach()
        {
            // In 2x symmetry only one will be Side A, the other Side B
            if (this.part.symmetryCounterparts.Count == 1)
            {
                ModuleBdbSymmetricalPart counterpartModule = this.part.symmetryCounterparts[0].Modules.OfType<ModuleBdbSymmetricalPart>().FirstOrDefault();
                if (counterpartModule.isSideA == isSideA)
                    counterpartModule.ToggleSide();
            }
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle Side")]
        public void ToggleSideEvent()
        {
            ToggleSide();

            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part counterpart in this.part.symmetryCounterparts)
                    counterpart.Modules.OfType<ModuleBdbSymmetricalPart>().FirstOrDefault().ToggleSide();
            }
        }

        public void ToggleSide()
        {
            isSideA = !isSideA;
            UpdateTransforms();
        }

        public void UpdateTransforms()
        {
            foreach(Transform transform in transformsA)
                transform.gameObject.SetActive(isSideA);
            foreach (Transform transform in transformsB)
                transform.gameObject.SetActive(!isSideA);
            
            UpdateSolarTransforms();
            UpdateUI();
        }

        public void UpdateSolarTransforms()
        {
            if (updateSolar)
            {
                if (isSideA)
                {
                    solarPanel.panelRotationTransform = pivotA; 
                    solarPanel.trackingDotTransform = raycastTransformA;   // which side gets the sun
                }
                else
                {
                    solarPanel.panelRotationTransform = pivotB;
                    solarPanel.trackingDotTransform = raycastTransformB;
                }
            }
        }

        public void UpdateUI()
        {
            if (isSideA)
                Events["ToggleSideEvent"].guiName = toggleSideEventGUINameA;
            else
                Events["ToggleSideEvent"].guiName = toggleSideEventGUINameB;
        }
    }
}
