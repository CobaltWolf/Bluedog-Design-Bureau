using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbAnimationMass : PartModule, IPartMassModifier
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

        [KSPField(guiActive = true, isPersistant = false, guiName = "Info0")]
        public string infoDisplay0 = "";
        [KSPField(guiActive = true, isPersistant = false, guiName = "Info1")]
        public string infoDisplay1 = "";
        [KSPField(guiActive = true, isPersistant = false, guiName = "Info2")]
        public string infoDisplay2 = "";
        [KSPField(guiActive = true, isPersistant = false, guiName = "Info3")]
        public string infoDisplay3 = "";
        [KSPField(guiActive = true, isPersistant = false, guiName = "Info4")]
        public string infoDisplay4 = "";

        GameObject lineObj;
        LineRenderer line;

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
            Debug.LogFormat("ModuleBdbAnimationMass: OnMoving position [{0}]", anim.GetScalar);
            Debug.LogFormat("ModuleBdbAnimationMass: OnMoving CoM [{0}]", part.CoMOffset.ToString());
        }

        private void OnStop(float pos)
        {
            listen = false;
            Debug.LogFormat("ModuleBdbAnimationMass: OnStop position [{0}]", anim.GetScalar);
            Debug.LogFormat("ModuleBdbAnimationMass: OnStop CoM [{0}]", part.CoMOffset.ToString());
        }

        public override void OnUpdate() //void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || anim == null)
                return;

            Vector3 vesselCoM = vessel.localCoM;
            Vector3 partPos = Part.PartToVesselSpacePos(Vector3.zero, part, vessel, PartSpaceMode.Pristine);
            partPos -= vesselCoM;
            Vector3 vesselAngVel = vessel.angularVelocity;
            Vector3 partAngSpeed;
            partAngSpeed.x = new Vector2(partPos.y, partPos.z).magnitude * vesselAngVel.x;
            partAngSpeed.y = new Vector2(partPos.x, partPos.z).magnitude * vesselAngVel.y;
            partAngSpeed.z = new Vector2(partPos.x, partPos.y).magnitude * vesselAngVel.z;
            Vector3 partAccel;
            partAccel.x = (float)Math.Pow(partAngSpeed.x, 2) / new Vector2(partPos.y, partPos.z).magnitude;
            partAccel.y = (float)Math.Pow(partAngSpeed.y, 2) / new Vector2(partPos.x, partPos.z).magnitude;
            partAccel.z = (float)Math.Pow(partAngSpeed.z, 2) / new Vector2(partPos.x, partPos.y).magnitude;
            infoDisplay0 = "CoM: " + vesselCoM.ToString();
            infoDisplay1 = "Pos: " + partPos.ToString();
            infoDisplay2 = "AngV (rad/s): " + vesselAngVel.ToString();
            infoDisplay3 = "AngS (m/s): " + partAngSpeed.ToString();
            infoDisplay4 = "Accel (m/s): " + partAccel.ToString() + " (" + partAccel.magnitude + ")";

            if (line == null)
                InitLine();
            //Vector3 lineVector = Part.VesselToPartSpacePos(partAccel, part, vessel, PartSpaceMode.Pristine);
            //line.transform.rotation = Quaternion.LookRotation(lineVector.normalized);
            //Vector3 lookDir = new Vector3(partPos.x, 0, 0);
            //Vector3 upDir = new Vector3(partPos.x, 0, partPos.z);
            //line.transform.rotation = Quaternion.LookRotation(vessel.up, upDir);
            Vector3 lineVector = partAccel;
            if (partPos.x < 0)
                lineVector.x *= -1;
            if (partPos.y < 0)
                lineVector.y *= -1;
            if (partPos.z < 0)
                lineVector.z *= -1;
            lineVector += partPos;
            lineVector = Part.VesselToPartSpacePos(lineVector, part, vessel, PartSpaceMode.Pristine);
            line.SetPosition(1, lineVector);

            if (listen || wasListening) // null ref in editor and flight init
            {
                wasListening = listen;
                float animPos = anim.GetScalar;
                //Debug.LogFormat("ModuleBdbAnimationMass: Moving position [{0}]", animPos);
                part.CoMOffset.Set(_origComOffset.x + animPos * extentV.x, _origComOffset.y + animPos * extentV.y, _origComOffset.z + animPos * extentV.z);
                //GameEvents.onVesselWasModified.Fire(vessel);
                //Debug.LogFormat("ModuleBdbAnimationMass: CoM [{0}]", part.CoMOffset.ToString());
            }
        }

        public void InitLine()
        {
            // First of all, create a GameObject to which LineRenderer will be attached.
            lineObj = new GameObject("Line");

            // Then create renderer itself...
            line = lineObj.AddComponent<LineRenderer>();
            line.transform.parent = part.transform; // ...child to our part...
            line.useWorldSpace = false; // ...and moving along with it (rather 
                                        // than staying in fixed world coordinates)
            line.transform.localPosition = Vector3.zero;
            line.transform.localEulerAngles = Vector3.zero;
            line.transform.rotation = Quaternion.LookRotation(vessel.transform.forward, vessel.transform.up);

            // Make it render a red to yellow triangle, 1 meter wide and 2 meters long
            line.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            //line.SetColors(Color.red, Color.yellow);
            line.startColor = Color.red;
            line.endColor = Color.yellow;
            //line.SetWidth(1, 0);
            line.startWidth = 0.05f;
            line.endWidth = 0.01f;
            //line.SetVertexCount(2);
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.forward * 2);
            line.enabled = true;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return 0.0f;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
    }

}
