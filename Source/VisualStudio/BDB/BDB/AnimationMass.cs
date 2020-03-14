using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbAnimationMass : PartModule
    {
        [KSPField(isPersistant = false)]
        public string actionModuleName = "";

        [KSPField(isPersistant = false)]
        public int actionModuleIndex = 0;

        [KSPField(isPersistant = false)]
        public bool debug = false;

        [KSPField(isPersistant = false)]
        public string extent = "";

        private IScalarModule anim;
        private bool listen = false;
        private bool wasListening = true;

        private Vector3 extentV;
        private Vector3 _origComOffset;
        private Vector3 _origCopOffset;
        private Vector3 _origColOffset;

        public void Start()
        {
            int moduleCount = actionModuleIndex;
            bool found = false;
            foreach (PartModule p in this.part.Modules)
            {
                if (p.moduleName == actionModuleName)
                {
                    if (moduleCount > 0)
                    {
                        moduleCount--;
                    }
                    else
                    {
                        found = true;
                        anim = p as IScalarModule;
                        if (anim != null)
                        {
                            anim.OnMoving.Add(OnMoving);
                            anim.OnStop.Add(OnStop);
                        }
                        else
                        {
                            Debug.LogErrorFormat("ModuleBdbAnimationMass: Module [{0}] index [{1}] does not impliment IScalarModule", actionModuleName, actionModuleIndex);
                        }

                        break;
                    }
                }
            }
            if (!found)
                Debug.LogErrorFormat("ModuleBdbAnimationMass: Module [{0}] index [{1}] not found", actionModuleName, actionModuleIndex);

            _origComOffset = part.CoMOffset;
            _origCopOffset = part.CoPOffset;
            _origColOffset = part.CoLOffset;

            string[] sArray = extent.Split(',');
            if (sArray.Length > 0)
                extentV.x = float.Parse(sArray[0]);
            if (sArray.Length > 1)
                extentV.y = float.Parse(sArray[1]);
            if (sArray.Length > 2)
                extentV.z = float.Parse(sArray[2]);
        }

        private void OnDestroy()
        {
            if (anim != null)
            {
                anim.OnMoving.Remove(OnMoving);
                anim.OnStop.Remove(OnStop);
            }
        }

        private void OnMoving(float from, float to)
        {
            listen = true;
        }

        private void OnStop(float pos)
        {
            listen = false;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || anim == null)
                return;

            if (listen || wasListening) // null ref in editor and flight init
            {
                wasListening = listen;
                float animPos = anim.GetScalar;
                Debug.LogFormat("ModuleBdbAnimationMass: Moving position [{0}]", animPos);
                part.CoMOffset.Set(_origComOffset.x + animPos * extentV.x, _origComOffset.y + animPos * extentV.y, _origComOffset.z + animPos * extentV.z);
                GameEvents.onVesselWasModified.Fire(vessel);
                Debug.LogFormat("ModuleBdbAnimationMass: CoM [{0}]", part.CoMOffset.ToString());
            }
        }
    }

}
