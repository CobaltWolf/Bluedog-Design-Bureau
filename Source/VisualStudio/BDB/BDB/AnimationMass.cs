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

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Spring K"), UI_FloatRange(minValue = 1, maxValue = 100, stepIncrement = 1.0f, affectSymCounterparts = UI_Scene.All)]
        public float springK = 50;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 1, maxValue = 10, stepIncrement = 0.1f)]
        public float springDamping = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Line Length"), UI_FloatRange(minValue = 1, maxValue = 50, stepIncrement = 0.1f, affectSymCounterparts = UI_Scene.All)]
        public float lineLength = 10;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Jettison Mass"), UI_FloatRange(minValue = 0.0001f, maxValue = 0.01f, stepIncrement = 0.0001f, affectSymCounterparts = UI_Scene.All)]
        public float jettisonMass = 0.001f;

        [KSPField(isPersistant = true)]
        public bool deployed = false;

        
        GameObject lineObj;
        LineRenderer line;
        GameObject lineObj2;
        LineRenderer line2;
        Vector3 lineEnd;
        float lineDeployed = 0.0f;
        Vector3 lineVelocity;
        Vector3 lineAngularVel;
        Vector3 partParentPos = Vector3.zero;
        private Vector3 _origComOffset;
        private bool jettisoned = false;
        
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
                if (line2 != null)
                {
                    line2.SetPosition(1, Vector3.zero);
                    line2.enabled = true;
                }
            }
            lineEnd = Vector3.zero;
            lineDeployed = 0.0f;
            line.endColor = line.startColor;
            line.SetPosition(1, lineEnd);
            lineVelocity = Vector3.zero;
            lineAngularVel = Vector3.zero;
            partParentPos = Vector3.zero;
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
            lineAngularVel = Vector3.zero;
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
            //Fields["springK"].guiActiveEditor = showEditor;
            Fields["springDamping"].guiActive = showEditor;
            //Fields["springDamping"].guiActiveEditor = showEditor;
            Fields["lineLength"].guiActive = showEditor;
            //Fields["lineLength"].guiActiveEditor = showEditor;
            Fields["jettisonMass"].guiActive = showEditor;
            //Fields["jettisonMass"].guiActiveEditor = showEditor;
            Events["Deploy"].guiActive = !deployed;
            Actions["DeployAction"].active = !deployed;
            Events["Reset"].guiActive = showEditor;
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics)
                return;

            if (!deployed || jettisoned)
                return;

            if (line == null)
                InitLine();

            Vector3 vesselCoM = vessel.localCoM;

            // position of Part on Vessel
            Vector3 partAbsPos = Part.PartToVesselSpacePos(Vector3.zero, part, vessel, PartSpaceMode.Pristine);
            if (partParentPos == Vector3.zero)
            {
                partParentPos = Part.VesselToPartSpacePos(partAbsPos, part.parent, vessel, PartSpaceMode.Pristine);
            }
                

            // position of Part relative to CoM
            Vector3 partPos = partAbsPos - vesselCoM;

            // radians/second around CoM. Positive = clockwise looking down
            Vector3 vesselAngVel = vessel.angularVelocity;
            if (lineAngularVel == Vector3.zero)
                lineAngularVel = vesselAngVel;

            // Angular velocity (m/s) at Part position relative to CoM (center of rotation)
            // radius = Vector2().magnitude
            // velocity = radius * (radians/sec)
            Vector3 partAngSpeed;
            partAngSpeed.x = new Vector2(partPos.y, partPos.z).magnitude * vesselAngVel.x;
            partAngSpeed.y = new Vector2(partPos.x, partPos.z).magnitude * vesselAngVel.y;
            partAngSpeed.z = new Vector2(partPos.x, partPos.y).magnitude * vesselAngVel.z;

            


            // vector pointing clockwise around each axis * the speed (m/s in vessel space)
            Vector3 xpart = Quaternion.AngleAxis(90, Vector3.right) * new Vector3(0, partPos.y, partPos.z).normalized * partAngSpeed.x;
            Vector3 ypart = Quaternion.AngleAxis(90, Vector3.up) * new Vector3(partPos.x, 0, partPos.z).normalized * partAngSpeed.y;
            Vector3 zpart = Quaternion.AngleAxis(90, Vector3.forward) * new Vector3(partPos.x, partPos.y, 0).normalized * partAngSpeed.z;

            // add the axis vectors together for Part direction of travel around the Vessel CoM
            Vector3 partRotationalVelocity = xpart + ypart + zpart;

            if (lineEnd == Vector3.zero)
                lineEnd = partRotationalVelocity.normalized * -1 * TimeWarp.fixedDeltaTime;

            // position of the end of the line relative to the CoM
            Vector3 lineEndPos = partPos + lineEnd;

            Vector3 lineEndAngSpeed;
            //lineEndAngSpeed.x = new Vector2(lineEndPos.y, lineEndPos.z).magnitude * vesselAngVel.x;
            //lineEndAngSpeed.y = new Vector2(lineEndPos.x, lineEndPos.z).magnitude * vesselAngVel.y;
            //lineEndAngSpeed.z = new Vector2(lineEndPos.x, lineEndPos.y).magnitude * vesselAngVel.z;
            lineEndAngSpeed.x = new Vector2(lineEndPos.y, lineEndPos.z).magnitude * lineAngularVel.x;
            lineEndAngSpeed.y = new Vector2(lineEndPos.x, lineEndPos.z).magnitude * lineAngularVel.y;
            lineEndAngSpeed.z = new Vector2(lineEndPos.x, lineEndPos.y).magnitude * lineAngularVel.z;

            Vector3 lineEndAngSpeedRelative;
            lineEndAngSpeedRelative.x = new Vector2(lineEndPos.y, lineEndPos.z).magnitude * lineAngularVel.x - vesselAngVel.x;
            lineEndAngSpeedRelative.y = new Vector2(lineEndPos.x, lineEndPos.z).magnitude * lineAngularVel.y - vesselAngVel.y;
            lineEndAngSpeedRelative.z = new Vector2(lineEndPos.x, lineEndPos.y).magnitude * lineAngularVel.z - vesselAngVel.z;

            // vector pointing clockwise around each axis * the speed (m/s in vessel space)
            xpart = Quaternion.AngleAxis(90, Vector3.right) * new Vector3(0, lineEnd.y, lineEnd.z).normalized * lineEndAngSpeedRelative.x;
            ypart = Quaternion.AngleAxis(90, Vector3.up) * new Vector3(lineEnd.x, 0, lineEnd.z).normalized * lineEndAngSpeedRelative.y;
            zpart = Quaternion.AngleAxis(90, Vector3.forward) * new Vector3(lineEnd.x, lineEnd.y, 0).normalized * lineEndAngSpeedRelative.z;

            // add the axis vectors together for line end direction of travel around the part
            Vector3 lineEndRotationalVelocityRelative = xpart + ypart + zpart;

            // centripetal acceleration around each axis at the line end (v^2 / radius)
            Vector3 lineEndAccel;
            lineEndAccel.x = (float)Math.Pow(lineEndAngSpeed.x, 2) / new Vector2(lineEndPos.y, lineEndPos.z).magnitude;
            lineEndAccel.y = (float)Math.Pow(lineEndAngSpeed.y, 2) / new Vector2(lineEndPos.x, lineEndPos.z).magnitude;
            lineEndAccel.z = (float)Math.Pow(lineEndAngSpeed.z, 2) / new Vector2(lineEndPos.x, lineEndPos.y).magnitude;

            // vector pointing away from each axis * the magnitude of the acceleration
            xpart = new Vector3(0, lineEndPos.y, lineEndPos.z).normalized * lineEndAccel.x;
            ypart = new Vector3(lineEndPos.x, 0, lineEndPos.z).normalized * lineEndAccel.y;
            zpart = new Vector3(lineEndPos.x, lineEndPos.y, 0).normalized * lineEndAccel.z;

            // add the axis vectors together for total line end inverse centripetal acceleration (in Vessel space)
            Vector3 lineEndCentripetalAcceleration = xpart + ypart + zpart;

            // Inverse of acceleration of the Vessel (in Vessel space)
            Vector3 accelVessel = vessel.acceleration_immediate;
            Vector3 accelG = vessel.graviticAcceleration;
            Vector3 accelVesselFelt = (accelVessel - accelG) * -1;
            accelVesselFelt = Quaternion.FromToRotation(vessel.transform.up, Vector3.up) * accelVesselFelt;

            float lineRemaining = Math.Max(0, lineLength - lineDeployed);
            //float lineUnwoundThisTick = Math.Min(lineRemaining, partRotationalVelocity.magnitude * TimeWarp.fixedDeltaTime);
            float lineUnwoundThisTick = Math.Min(lineRemaining, lineEndCentripetalAcceleration.magnitude * TimeWarp.fixedDeltaTime);
            lineDeployed = Math.Min(lineLength, lineDeployed + lineUnwoundThisTick);
            Vector3 lineSlack = lineEnd.normalized * lineUnwoundThisTick;

            float springLength = lineEnd.magnitude;
            Vector3 accelSpring = Vector3.zero;
            if (lineEnd.magnitude > lineDeployed)
            {
                float dampingForce = springDamping * lineVelocity.magnitude;
                accelSpring = lineEnd.normalized * ((-springK * lineEnd.magnitude) - dampingForce);
            }
                

            lineVelocity = lineVelocity + (lineEnd.normalized * lineEndCentripetalAcceleration.magnitude + accelSpring) * TimeWarp.fixedDeltaTime;
            Vector3 lineMoveThisTick = lineVelocity * TimeWarp.fixedDeltaTime;
            if (lineRemaining == 0)
            {
                lineMoveThisTick = lineMoveThisTick + lineEndRotationalVelocityRelative * TimeWarp.fixedDeltaTime;
                Debug.Log("ModuleBdbYoyoDespin adding rotation: " + lineEndRotationalVelocityRelative + " " + lineEndRotationalVelocityRelative.magnitude);
            }
            lineEnd = lineEnd + Vector3.ProjectOnPlane(lineMoveThisTick, Vector3.up);


            // Total acceleration at the Part ???
            //Vector3 totalAcceleration;
            //if (lineEnd.magnitude > lineLength)
            //    totalAcceleration = lineEndCentripetalAcceleration * -1 + accelVesselFelt;
            //else
            //    totalAcceleration = partRotationalVelocity.normalized * lineEndCentripetalAcceleration.magnitude * -1 + accelVesselFelt;

            if (true)
            {
                float forceMagnitude = (lineEndCentripetalAcceleration - lineSlack).magnitude * TimeWarp.fixedDeltaTime;
                Vector3 force = Quaternion.LookRotation(vessel.transform.forward, vessel.transform.up) * lineEnd;
                //Vector3 force = Part.VesselToPartSpaceDir(lineEnd.normalized, part, vessel, PartSpaceMode.Pristine);
                //Vector3 force = Part.VesselToPartSpace(lineEnd.normalized * -1, part, vessel, PartSpaceMode.Pristine);
                //Vector3 force = Vector3.ProjectOnPlane(lineEnd, Vector3.up).normalized;
                //force = Quaternion.Inverse(line.transform.rotation) * force;
                force = force.normalized * (jettisonMass * 1000 * forceMagnitude);
                if (line2 != null)
                {
                    //line2.SetPosition(0, lineEnd);
                    line2.SetPosition(1, lineEnd.normalized * force.magnitude);
                }
                

                if (force.magnitude > part.breakingForce)
                {
                    jettisoned = true;
                    Debug.Log("ModuleBdbYoyoDespin Line snapped with force: " + force + " " + force.magnitude);
                }
                else
                    //part.parent.AddForceAtPosition(force, partParentPos);
                    part.AddForce(force);
                
                Debug.Log("ModuleBdbYoyoDespin AddForce: " + force + " " + force.magnitude + " lineEnd: " + lineEnd + " " + lineEnd.magnitude);
                
            }
            

            //Vector3 accelSpring = Vector3.zero;
            //if (lineEnd.magnitude > lineLength)
            //{
            //    Vector3 springForce = lineEnd.normalized;
            //    springForce = springForce * (-springK * (lineEnd.magnitude - lineLength));
            //    //float dampingForce = springDamping * lineVelocity.magnitude;
            //    accelSpring = springForce;// + totalAcceleration;// - dampingForce;
            //}

            //lineVelocity = lineVelocity + ((totalAcceleration + accelSpring) * TimeWarp.fixedDeltaTime);
            //lineEnd = lineEnd + lineVelocity * TimeWarp.fixedDeltaTime;

            infoDisplay0 = "CoM: " + vesselCoM.ToString();
            //infoDisplay1 = "Pos: " + partPos.ToString();
            infoDisplay1 = "spring: " + accelSpring.ToString() + " (" + accelSpring.magnitude + ")";
            infoDisplay2 = "lineEnd: " + lineEnd.ToString() + " (" + lineEnd.magnitude + ")";
            infoDisplay3 = "lineVelocity (m/s): " + lineVelocity.ToString() + " (" + lineVelocity.magnitude + ")";
            infoDisplay4 = "Accel (m/s): " + lineEndAccel.ToString() + " (" + lineEndAccel.magnitude + ")";



            

            Vector3 lineCoM = lineEnd * (jettisonMass / part.mass);
            part.CoMOffset = _origComOffset + Part.VesselToPartSpacePos(lineCoM, part, vessel, PartSpaceMode.Pristine);

            if (jettisoned || Vector3.Angle(lineEnd, new Vector3(partPos.x, 0, partPos.z).normalized) < 10)
            {
                jettisoned = true;
                part.CoMOffset = _origComOffset;
                if (!showEditor)
                    line.enabled = false;
                line.endColor = Color.yellow;
            }
        }

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics)
                return;

            if (!deployed || jettisoned)
                return;

            if (line == null)
                InitLine();

            line.SetPosition(1, lineEnd);
        }

        public void InitLine()
        {
            lineObj = new GameObject("Line");
            line = lineObj.AddComponent<LineRenderer>();
            InitLine(line, Color.gray);
            if (showEditor)
            {
                lineObj2 = new GameObject("Line2");
                line2 = lineObj2.AddComponent<LineRenderer>();
                InitLine(line2, Color.red, true);
                line2.startWidth *= 5;
                line2.endWidth *= 2;
            }
            
        }

        public void InitLine(LineRenderer l, Color color, bool rotate = true)
        {
            l.transform.parent = part.transform;
            l.useWorldSpace = false;

            l.transform.localPosition = Vector3.zero;
            l.transform.localEulerAngles = Vector3.zero;
            if (rotate)
                l.transform.rotation = Quaternion.LookRotation(vessel.transform.forward, vessel.transform.up);

            l.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            l.startColor = color;
            l.endColor = color;
            l.startWidth = 0.01f;
            l.endWidth = 0.01f;
            l.positionCount = 2;
            l.SetPosition(0, Vector3.zero);
            l.SetPosition(1, Vector3.zero);
            l.enabled = true;
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
