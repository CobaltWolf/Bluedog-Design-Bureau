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




        public override void OnAwake()
        {
            OnMovingEvent = new EventData<float, float>("OnMovingEvent");
            OnStopEvent = new EventData<float>("OnStopEvent");
            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            jettisons = part.FindModelTransforms(jettisonName);
            if (jettisons.Length == 0)
                isJettisoned = true;
            else
                JettisonsSetActive(!isJettisoned);

            Fields[nameof(isJettisoned)].uiControlEditor.onFieldChanged = OnEditorToggleJettisoned;
            Fields[nameof(isJettisoned)].guiName = toggleJettisonEditorGuiName;

            Events[nameof(Jettison)].active = !isJettisoned;
            Events[nameof(Jettison)].guiName = jettisonGuiName;

            Actions[nameof(JettisonAction)].active = !isJettisoned;
            Actions[nameof(JettisonAction)].guiName = jettisonGuiName;
        }

        private void OnEditorToggleJettisoned(BaseField field, object oldValue)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            if (jettisons.Length > 0)
            {
                JettisonsSetActive(!isJettisoned);
            }
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

            isJettisoned = true;

            OnStop.Fire(1);

            Events[nameof(Jettison)].active = false;
            Actions[nameof(JettisonAction)].active = false;

            EnableOtherModules();

            FXGroup effect = part.findFxGroup(fxGroupName);
            if (effect != null)
            {
                effect.Burst();
            }
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

        private void JettisonsSetActive(bool b)
        {
            for (int i = 0; i < jettisons.Length; i++)
                jettisons[i].gameObject.SetActive(b);
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
            return new string[] { "Jettisoned", "Covered" };
        }

        public void AssumeDragCubePosition(string name)
        {
            if (jettisons.Length == 0)
                return;

            if (name == "Jettisoned")
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
