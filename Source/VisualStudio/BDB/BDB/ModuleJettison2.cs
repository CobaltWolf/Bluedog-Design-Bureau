using System;
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

    class ModuleBdbJettison : PartModule, IScalarModule, IPartMassModifier, IMultipleDragCube
    {
        

        [KSPField()]
        public string jettisonName = "jettison";

        [KSPField()]
        public Vector3 jettisonDirection = Vector3.up;

        [KSPField()]
        public float jettisonForce = 5.0f;

        [KSPField()]
        public float jettisonedObjectMass = 0.01f;

        private Transform[] jettisons;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Fairing"), UI_Toggle(affectSymCounterparts = UI_Scene.Editor, disabledText = "Installed", enabledText = "Removed")]
        public bool isJettisoned = false;

        [KSPField]
        public string toggleJettisonEditorGuiName = "Fairing";

        [KSPField]
        public string jettisonGuiName = "Jettison";

        [KSPField]
        public bool enableDisabledModules = false;

        [KSPField]
        public string fxGroupName = "jettison";

        private const string DRAG_CUBE_JETTISONED = "Jettisoned";
        private const string DRAG_CUBE_COVERED = "Covered";

        [UI_Toggle(scene = UI_Scene.All, disabledText = "No", enabledText = "Yes")]
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Auto-Deploy Fairing")]
        public bool autoDeploy = true;

        [UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f, scene = UI_Scene.All)]
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Autodeploy Altitude (km)")]
        public float deployAltitude = float.NaN;


        public override void OnAwake()
        {
            OnMovingEvent = new EventData<float, float>("OnMovingEvent");
            OnStopEvent = new EventData<float>("OnStopEvent");

            jettisons = part.FindModelTransforms(jettisonName);

            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            if (jettisons.Length == 0)
                isJettisoned = true;

            SetJettisoned(isJettisoned);

            Fields[nameof(isJettisoned)].uiControlEditor.onFieldChanged = OnEditorToggleJettisoned;
            Fields[nameof(isJettisoned)].guiName = toggleJettisonEditorGuiName;

            Events[nameof(Jettison)].active = !isJettisoned;
            Events[nameof(Jettison)].guiName = jettisonGuiName;

            Actions[nameof(JettisonAction)].active = !isJettisoned;
            Actions[nameof(JettisonAction)].guiName = jettisonGuiName;

            Fields[nameof(autoDeploy)].uiControlEditor.onFieldChanged = OnToggleAutodeploy;
            Fields[nameof(autoDeploy)].uiControlFlight.onFieldChanged = OnToggleAutodeploy;

            CalculateAutodeployAltitude();
            UpdateDeployAltitudeVisibility();
        }

        private void OnEditorToggleJettisoned(BaseField field, object oldValue)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            OnMoving.Fire(isJettisoned ? 0 : 1, isJettisoned ? 1 : 0);

            if (jettisons.Length > 0)
            {
                SetJettisoned(isJettisoned);
            }

            OnStop.Fire(isJettisoned ? 1 : 0);

            part.UpdateStageability(true, true);
        }

        private void OnToggleAutodeploy(BaseField field, object oldValue)
        {
            UpdateDeployAltitudeVisibility();
        }

        public virtual void FixedUpdate()
        {
            if (isJettisoned || !autoDeploy || HighLogic.LoadedSceneIsEditor || !part.started) return;

            if (deployAltitude * 1000f < vessel.altitude) Jettison();
        }

        [KSPEvent(guiActive = true, guiName = "Jettison")]
        public void Jettison()
        {
            if (isJettisoned)
                return;

            if (jettisons.Length == 0)
                return;

            OnMoving.Fire(0, 1);

            for (int i = 0; i < jettisons.Length; i++)
            {
                Rigidbody rb = physicalObject.ConvertToPhysicalObject(part, jettisons[i].gameObject).rb;
                rb.useGravity = true;
                rb.mass = jettisonedObjectMass / jettisons.Length;
                rb.maxAngularVelocity = PhysicsGlobals.MaxAngularVelocity;
                rb.angularVelocity = part.Rigidbody.angularVelocity;
                rb.velocity = part.Rigidbody.velocity + Vector3.Cross(part.Rigidbody.worldCenterOfMass - vessel.CurrentCoM, vessel.angularVelocity);

                Vector3 d = jettisonDirection;
                if (d == Vector3.zero)
                    d = Vector3.Normalize(rb.transform.position - part.transform.position);
                else
                    d = part.transform.TransformDirection(d);

                //rb.AddForce(part.transform.TransformDirection(jettisonDirection) * (jettisonForce * 0.5f), ForceMode.Force);
                rb.AddForceAtPosition(d * (jettisonForce * 0.5f), part.transform.position, ForceMode.Force);
                part.Rigidbody.AddForce(d * (-jettisonForce * 0.5f), ForceMode.Force);
            }

            jettisons = new Transform[0];

            if (part.temperature < part.skinMaxTemp)
                part.skinTemperature = part.temperature;

            isJettisoned = true;

            SetJettisoned(isJettisoned);

            OnStop.Fire(1);

            EnableOtherModules();

            FXGroup effect = part.findFxGroup(fxGroupName);
            if (effect != null)
            {
                effect.Burst();
            }

            GameEvents.onVesselWasModified.Fire(vessel);
        }

        [KSPAction("Deploy")]
        public void JettisonAction(KSPActionParam param)
        {
            Jettison();
        }

        public override void OnActive()
        {
            Jettison();
        }

        private void EnableOtherModules()
        {
            if (enableDisabledModules)
            {
                foreach (PartModule pm in part.Modules)
                {
                    if (!pm.moduleIsEnabled)
                        pm.moduleIsEnabled = true;
                }
            }
        }

        private void SetJettisoned(bool b)
        {
            SetDragCube(b);
            JettisonsSetActive(!b);

            Events[nameof(Jettison)].active = !b;
            Actions[nameof(JettisonAction)].active = !b;
            Fields[nameof(autoDeploy)].guiActive = !b;
            Fields[nameof(autoDeploy)].guiActiveEditor = !b;
            UpdateDeployAltitudeVisibility();
        }

        private void UpdateDeployAltitudeVisibility()
        {
            Fields[nameof(deployAltitude)].guiActive = !isJettisoned && autoDeploy;
            Fields[nameof(deployAltitude)].guiActiveEditor = !isJettisoned && autoDeploy;
        }

        public override bool IsStageable()
        {
            return !isJettisoned;
        }

        private void SetDragCube(bool deployed)
        {
            if (deployed)
            {
                part.DragCubes.SetCubeWeight(DRAG_CUBE_JETTISONED, 1);
                part.DragCubes.SetCubeWeight(DRAG_CUBE_COVERED, 0);
            }
            else
            {
                part.DragCubes.SetCubeWeight(DRAG_CUBE_JETTISONED, 0);
                part.DragCubes.SetCubeWeight(DRAG_CUBE_COVERED, 1);
            }
        }

        private void JettisonsSetActive(bool b)
        {
            for (int i = 0; i < jettisons.Length; i++)
                jettisons[i].gameObject.SetActive(b);
        }

        private void CalculateAutodeployAltitude()
        {
            UI_FloatRange deployAltitudeControl;
            if (HighLogic.LoadedSceneIsEditor)
                deployAltitudeControl = (UI_FloatRange)Fields[nameof(deployAltitude)].uiControlEditor;
            else
                deployAltitudeControl = (UI_FloatRange)Fields[nameof(deployAltitude)].uiControlFlight;

            float newDeployAltitude;

            CelestialBody home = Planetarium.fetch.Home;
            if (home != null)
            {
                newDeployAltitude = Mathf.Round((float)home.atmosphereDepth * 0.70f / 1000f);// / 5f) * 5f;
                deployAltitudeControl.maxValue = (float)home.atmosphereDepth / 1000f;
            }
            else
            {
                Debug.LogError($"[{part.name} {GetType().Name}] Cannot find home celestial body to set altitude from");
                autoDeploy = false;
                newDeployAltitude = 100f;
                deployAltitudeControl.maxValue = 200f;
            }

            if (float.IsNaN(deployAltitude)) deployAltitude = newDeployAltitude;
        }

        #region IPartMassModifier
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            float mass = 0;
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
            return mass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }
        #endregion

        #region IScalarModule
        public void SetScalar(float t) {}
        public void SetUIRead(bool state) {}
        public void SetUIWrite(bool state) {}

        public bool IsMoving()
        {
            return false;
        }

       [KSPField()]
        public string ModuleID;

        public string ScalarModuleID
        {
            get
            {
                return ModuleID;
            }
        }
        
        public float GetScalar
        {
            get
            {
                if (isJettisoned)
                    return 1;
                else
                    return 0;
            }
        }

        public bool CanMove
        {
            get
            {
                return false;
            }
        }

        private EventData<float, float> OnMovingEvent;
        private EventData<float> OnStopEvent;

        public EventData<float, float> OnMoving
        {
            get
            {
                return OnMovingEvent;
            }
        }

        public EventData<float> OnStop
        {
            get
            {
                return OnStopEvent;
            }
        }
        #endregion

        #region IMultipleDragCube
        public string[] GetDragCubeNames()
        {
            return new string[2] { DRAG_CUBE_JETTISONED, DRAG_CUBE_COVERED };
        }

        public void AssumeDragCubePosition(string name)
        {
            Debug.Log("AssumeDragCubePosition: " + name);
            if (jettisons.Length == 0)
                return;

            if (name == DRAG_CUBE_JETTISONED)
                JettisonsSetActive(false);
            else
                JettisonsSetActive(true);
        }

        public bool UsesProceduralDragCubes()
        {
            return false;
        }

        public bool IsMultipleCubesActive
        {
            get
            {
                return true;
            }
        }
        #endregion
    }
}
