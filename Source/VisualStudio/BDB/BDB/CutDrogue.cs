using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbCutDrogue : PartModule
    {
        [KSPField]
        public bool isDrogueChute = false;

        [UI_Toggle(scene = UI_Scene.All, disabledText = "No", enabledText = "Yes")]
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Auto-Cut Drogue Chute")]
        public bool autoCutDrogue = true;

        [KSPField(isPersistant = true)]
        public bool triggered = false;

        private ModuleParachute chute = null;

        public override void OnStart(StartState state)
        {
            chute = part.FindModulesImplementing<ModuleParachute>().FirstOrDefault();
            if (chute == null)
                Debug.LogError("[ModuleBdbCutDrogue] ModuleParachute not found on part " + part.partInfo.title);

            Fields[nameof(autoCutDrogue)].guiActive = !isDrogueChute;
            Fields[nameof(autoCutDrogue)].guiActiveEditor = !isDrogueChute;
        }

        public override void OnUpdate()
        {
            if (isDrogueChute)
                return;

            if (chute == null)
                return;

            if(chute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED || chute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
            {
                if (!triggered)
                {
                    List<ModuleBdbCutDrogue> drogues = vessel.FindPartModulesImplementing<ModuleBdbCutDrogue>().ToList();
                    foreach (ModuleBdbCutDrogue d in drogues)
                    {
                        if (d.isDrogueChute && d.chute != null)
                        {
                            if (d.chute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED || d.chute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                                d.chute.CutParachute();
                        }
                    }
                    triggered = true;
                }
                
            } else if (chute.deploymentState == ModuleParachute.deploymentStates.STOWED)
            {
                triggered = false;
            }
        }
    }
}
