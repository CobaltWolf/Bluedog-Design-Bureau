using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleJettison2 : ModuleJettison, IPartMassModifier
    {
        public override void OnStart(StartState state)
        {
            if (part.stagingIcon == "")
                part.stagingIcon = "FUEL_TANK";
            base.OnStart(state);
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            float mass = 0;
            if (!useCalculatedMass)
            {
                switch (sit)
                {
                    case ModifierStagingSituation.CURRENT:
                        if (!isJettisoned)
                            mass = jettisonedObjectMass;
                        break;
                    case ModifierStagingSituation.UNSTAGED:
                        mass = jettisonedObjectMass;
                        break;
                }
            }
            return mass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }
    }
}
