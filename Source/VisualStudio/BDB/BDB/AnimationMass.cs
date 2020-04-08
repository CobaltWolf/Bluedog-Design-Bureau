using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbYoyoDespin : PartModule, IPartMassModifier
    {
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

        [KSPField(isPersistant = false)]
        public bool showEditor = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spring K"), UI_FloatRange(minValue = 0, maxValue = 50, stepIncrement = 0.1f, affectSymCounterparts = UI_Scene.All)]
        public float springK = 0.1f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Line Length"), UI_FloatRange(minValue = 0, maxValue = 50, stepIncrement = 0.1f, affectSymCounterparts = UI_Scene.All)]
        public float lineLength = 10;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Jettison Mass"), UI_FloatRange(minValue = 0, maxValue = 0.1f, stepIncrement = 0.001f, affectSymCounterparts = UI_Scene.All)]
        public float jettisonMass = 0.01f;

        [KSPField(isPersistant = true)]
        public bool deployed = false;

        
        GameObject lineObj;
        LineRenderer line;
        Vector3 lineEnd;
        Vector3 lineVelocity;
        private Vector3 _origComOffset;
        private bool jettisoned = false;
        private float maxLineMagnitude = 0f;

        [KSPEvent(guiName = "Reset", guiActive = true)]
        public void Reset()
        {
            if (!deployed)
                return;
            deployed = false;
            jettisoned = false;
            if (line != null)
            {
                line.SetPosition(1, Vector3.zero);
                line.enabled = true;
            }
            lineEnd = Vector3.zero;
            line.SetPosition(1, lineEnd);
            lineVelocity = Vector3.zero;
            maxLineMagnitude = 0;
            Events["Deploy"].guiActive = true;
            Actions["DeployAction"].active = true;
            foreach (Part p in part.symmetryCounterparts)
            {
                p.Modules.OfType<ModuleBdbYoyoDespin>().FirstOrDefault().Reset();
            }
        }

        [KSPEvent(guiName = "Deploy", guiActive = true)]
        public void Deploy()
        {
            if (deployed)
                return;
            lineEnd = Vector3.zero;  //Part.PartToVesselSpacePos(Vector3.zero, part, vessel, PartSpaceMode.Pristine);
            lineVelocity = Vector3.zero;
            deployed = true;
            Events["Deploy"].guiActive = false;
            Actions["DeployAction"].active = false;
            foreach (Part p in part.symmetryCounterparts)
            {
                p.Modules.OfType<ModuleBdbYoyoDespin>().FirstOrDefault().Deploy();
            }
        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam param)
        {
            Deploy();
        }

        public override void OnActive()
        {
            Deploy();
        }

        public override bool IsStageable()
        {
            return !deployed;
        }

        public override void OnStart(StartState state)
        {
            if (part.stagingIcon == "")
                part.stagingIcon = "REACTION_WHEEL";

            jettisoned = deployed;
            _origComOffset = part.CoMOffset;

            Fields["springK"].guiActive = showEditor;
            Fields["springK"].guiActiveEditor = showEditor;
            Fields["springDamping"].guiActive = showEditor;
            Fields["springDamping"].guiActiveEditor = showEditor;
            Fields["deployedAnchor"].guiActive = showEditor;
            Fields["deployedAnchor"].guiActiveEditor = showEditor;
            Fields["stowedAnchor"].guiActive = showEditor;
            Fields["stowedAnchor"].guiActiveEditor = showEditor;
            Events["Deploy"].guiActive = !deployed;
            Actions["DeployAction"].active = !deployed;
            Events["Reset"].guiActive = showEditor;
        }

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics || !deployed || jettisoned)
                return;

            Vector3 vesselCoM = vessel.localCoM;

            // position of Part on Vessel
            Vector3 partAbsPos = Part.PartToVesselSpacePos(Vector3.zero, part, vessel, PartSpaceMode.Pristine);

            // position of Part relative to CoM
            Vector3 partPos = partAbsPos - vesselCoM;

            // radians/second around CoM
            Vector3 vesselAngVel = vessel.angularVelocity;

            // Angular velocity (m/s) at Part position relative to CoM (center of rotation)
            // radius = Vector2().magnitude
            // velocity = radius * (radians/sec)
            Vector3 partAngSpeed;
            partAngSpeed.x = new Vector2(partPos.y, partPos.z).magnitude * vesselAngVel.x;
            partAngSpeed.y = new Vector2(partPos.x, partPos.z).magnitude * vesselAngVel.y;
            partAngSpeed.z = new Vector2(partPos.x, partPos.y).magnitude * vesselAngVel.z;

            // centripetal acceleration around each axis (v^2 / radius)
            Vector3 partAccel;
            partAccel.x = (float)Math.Pow(partAngSpeed.x, 2) / new Vector2(partPos.y, partPos.z).magnitude;
            partAccel.y = (float)Math.Pow(partAngSpeed.y, 2) / new Vector2(partPos.x, partPos.z).magnitude;
            partAccel.z = (float)Math.Pow(partAngSpeed.z, 2) / new Vector2(partPos.x, partPos.y).magnitude;

            infoDisplay0 = "CoM: " + vesselCoM.ToString();
            infoDisplay1 = "Pos: " + partPos.ToString();
            infoDisplay2 = "AngV (rad/s): " + vesselAngVel.ToString();
            infoDisplay3 = "AngS (m/s): " + partAngSpeed.ToString();
            infoDisplay4 = "Accel (m/s): " + partAccel.ToString() + " (" + partAccel.magnitude + ")";

            // vector pointing away from each axis * the magnitude of the acceleration
            Vector3 xpart = new Vector3(0, partPos.y, partPos.z).normalized * partAccel.x;
            Vector3 ypart = new Vector3(partPos.x, 0, partPos.z).normalized * partAccel.y;
            Vector3 zpart = new Vector3(partPos.x, partPos.y, 0).normalized * partAccel.z;

            // add the axis vectors together for total Part inverse centripetal acceleration (in Vessel space)
            Vector3 centripetalAcceleration = xpart + ypart + zpart;

            // Inverse of acceleration of the Vessel (in Vessel space)
            Vector3 accelVessel = vessel.acceleration_immediate;
            Vector3 accelG = vessel.graviticAcceleration;
            Vector3 accelVesselFelt = (accelVessel - accelG) * -1;
            accelVesselFelt = Quaternion.FromToRotation(vessel.transform.up, Vector3.up) * accelVesselFelt;
            // Total acceleration at the Part
            Vector3 totalAcceleration = centripetalAcceleration + accelVesselFelt;


            Vector3 accelSpring = Vector3.zero;
            if (lineEnd.magnitude > lineLength)
            {
                Vector3 springForce = lineEnd.normalized;
                springForce = springForce * (-springK * (lineEnd.magnitude - lineLength));
                //float dampingForce = springDamping * lineVelocity.magnitude;
                accelSpring = springForce;// + totalAcceleration;// - dampingForce;
            }

            lineVelocity = lineVelocity + ((totalAcceleration + accelSpring) * TimeWarp.fixedDeltaTime);
            lineEnd = lineEnd + lineVelocity * TimeWarp.fixedDeltaTime;

            infoDisplay2 = "lineEnd: " + lineEnd.ToString() + " (" + lineEnd.magnitude + ")";
            infoDisplay3 = "lineVelocity (m/s): " + lineVelocity.ToString() + " (" + lineVelocity.magnitude + ")";

            if (line == null)
                InitLine();
            line.SetPosition(1, lineEnd);

            Vector3 lineCoM = lineEnd * (jettisonMass / part.mass);
            part.CoMOffset = _origComOffset + Part.VesselToPartSpacePos(lineCoM, part, vessel, PartSpaceMode.Pristine);

            if (lineEnd.magnitude > maxLineMagnitude)
                maxLineMagnitude = lineEnd.magnitude;
            else if (lineEnd.magnitude - lineLength < (maxLineMagnitude - lineLength) * 0.9)
            {
                jettisoned = true;
                part.CoMOffset = _origComOffset;
                if (!showEditor)
                    line.enabled = false;
            }
        }

        public void InitLine()
        {
            // First of all, create a GameObject to which LineRenderer will be attached.
            lineObj = new GameObject("Line");

            // Then create renderer itself...
            line = lineObj.AddComponent<LineRenderer>();
            line.transform.parent = part.transform;
            line.useWorldSpace = false;

            line.transform.localPosition = Vector3.zero;
            line.transform.localEulerAngles = Vector3.zero;
            line.transform.rotation = Quaternion.LookRotation(vessel.transform.forward, vessel.transform.up);

            line.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            line.startColor = Color.gray;
            line.endColor = Color.gray;
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.zero);
            line.enabled = true;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!jettisoned)
                return jettisonMass;
            else
                return 0.0f;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
    }

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

        public override void OnStart(StartState state)
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

            if (listen || wasListening)
            {
                wasListening = listen;
                float animPos = anim.GetScalar;
                //Debug.LogFormat("ModuleBdbAnimationMass: Moving position [{0}]", animPos);
                part.CoMOffset.Set(_origComOffset.x + animPos * extentV.x, _origComOffset.y + animPos * extentV.y, _origComOffset.z + animPos * extentV.z);
                //GameEvents.onVesselWasModified.Fire(vessel);
                //Debug.LogFormat("ModuleBdbAnimationMass: CoM [{0}]", part.CoMOffset.ToString());
            }
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
