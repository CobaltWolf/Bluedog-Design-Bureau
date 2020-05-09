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

        private Transform jettison;

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
            OnMovingEvent = new EventData<float, float>("ModuleBdbDecouplerAnimation.OnMovingEvent");
            OnStopEvent = new EventData<float>("ModuleBdbDecouplerAnimation.OnStopEvent");
            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            jettison = part.FindModelTransform(jettisonName);
            if (jettison == null)
                isJettisoned = true;
            else
                jettison.gameObject.SetActive(!isJettisoned);

            Fields[nameof(isJettisoned)].uiControlEditor.onFieldChanged = OnEditorToggleJettisoned;
            Fields[nameof(isJettisoned)].guiName = toggleJettisonEditorGuiName;

            Events[nameof(Jettison)].active = !isJettisoned;
            Events[nameof(Jettison)].guiName = jettisonGuiName;

            Actions[nameof(JettisonAction)].active = !isJettisoned;
            Actions[nameof(JettisonAction)].guiName = jettisonGuiName;

            Debug.Log("[ModuleBdbJettison] jettisonDirection: " + jettisonDirection.ToString());
        }

        private void OnEditorToggleJettisoned(BaseField field, object oldValue)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            if (jettison != null)
            {
                jettison.gameObject.SetActive(!isJettisoned);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Jettison")]
        public void Jettison()
        {
            if (isJettisoned)
                return;

            if (jettison == null)
                return;

            OnMoving.Fire(0, 1);

            Rigidbody rb = physicalObject.ConvertToPhysicalObject(part, jettison.gameObject).rb;
            rb.useGravity = true;
            rb.mass = jettisonedObjectMass;
            rb.maxAngularVelocity = PhysicsGlobals.MaxAngularVelocity;
            rb.angularVelocity = part.Rigidbody.angularVelocity;
            rb.velocity = part.Rigidbody.velocity + Vector3.Cross(part.Rigidbody.worldCenterOfMass - vessel.CurrentCoM, vessel.angularVelocity);
            //rb.AddForce(part.transform.TransformDirection(jettisonDirection) * (jettisonForce * 0.5f), ForceMode.Force);
            rb.AddForceAtPosition(part.transform.TransformDirection(jettisonDirection) * (jettisonForce * 0.5f), part.transform.position, ForceMode.Force);
            part.Rigidbody.AddForce(part.transform.TransformDirection(jettisonDirection) * (-jettisonForce * 0.5f), ForceMode.Force);

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
            if (jettison == null)
                return;

            if (name == "Jettisoned")
                jettison.gameObject.SetActive(false);
            else
                jettison.gameObject.SetActive(true);
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
