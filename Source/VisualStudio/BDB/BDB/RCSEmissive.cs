// Lifted in full from Nertea's Near Future Propulsion.
// https://github.com/ChrisAdderley/NearFuturePropulsion/blob/master/Source/RCSEmissive.cs

/// NFPAnimateHeat
/// ---------------------------
/// Plays a second animation. Replaces ModuleAnimateHeat, because it can't handle 2 animations on one part
/// Kinda a unity hack.
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDB
{
    public class ModuleBdbRCSEmissive : PartModule
    {
        [KSPField(isPersistant = false)]
        public FloatCurve alphaCurve = new FloatCurve();

        ModuleRCSFX rcs;
        List<Material> thrustMaterials;

        public void Start()
        {
            rcs = part.GetComponent<ModuleRCSFX>();
            
            thrustMaterials = new List<Material>();
            foreach (Transform t in rcs.thrusterTransforms)
            {
                thrustMaterials.Add(t.GetComponentInChildren<MeshRenderer>().material);
            }
        }

        public void FixedUpdate()
        {

            if (HighLogic.LoadedSceneIsFlight)
            {
                for (int i= 0; i < thrustMaterials.Count; i++)
                {
                    Color c;
                    c = new Color(1f,1f,1f,alphaCurve.Evaluate(rcs.thrustForces[i]));
                    thrustMaterials[i].SetColor("_EmissiveColor",c);

                }
            }
        }
    }
}