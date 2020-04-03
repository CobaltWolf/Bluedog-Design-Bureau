using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbAccelAnimation : PartModule, IScalarModule  //, IMultipleDragCube
    {
        [KSPField]
        public string moduleID = "bdbAccelAnimation";

        [KSPField(isPersistant = false)]
        public string animationName = "";

        [KSPField(isPersistant = false)]
        public int layer = 1;

        [KSPField(isPersistant = false)]
        public bool showEditor = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spring K"), UI_FloatRange(minValue = 0, maxValue = 200, stepIncrement = 1)]
        public float springK = 75;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 10, stepIncrement = 0.1f)]
        public float springDamping = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Deployed Anchor"), UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f)]
        public float deployedAnchor = 0.5f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Stowed Anchor"), UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f)]
        public float stowedAnchor = 0.0f;

        [KSPField(isPersistant = true)]
        public bool deployed = true;

        [KSPField(isPersistant = true)]
        public bool bounce = true;

        float animPosition = 0.5f;
        float animSpeed = 0.0f;

        private AnimationState[] animationStates;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Animation")]
        public void ToggleAnimationEditor()
        {
            SetDeployed(!deployed);
        }

        [KSPEvent(guiName = "Deploy", guiActive = true)]
        public void Deploy()
        {
            SetDeployed(true);
        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam param)
        {
            Deploy();
        }

        public override void OnStart(StartState state)
        {
            animationStates = SetUpAnimation(animationName, this.part);
            animPosition = SpringAnchor();
            SetAnimation(animPosition);
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
        }

        public void SetDeployed(bool newState)
        {
            if (deployed != newState)
            {
                if (newState)
                    OnMoving.Fire(stowedAnchor, deployedAnchor);
                else
                    OnMoving.Fire(deployedAnchor, stowedAnchor);
                OnStop.Fire(SpringAnchor());
            }
            deployed = newState;
            Events["Deploy"].guiActive = !deployed;
            Actions["DeployAction"].active = !deployed;
        }

        public bool CanDeploy()
        {
            return true;
        }

        public float SpringAnchor()
        {
            if (deployed)
                return deployedAnchor;
            else
                return stowedAnchor;
        }

        public void FixedUpdate()
        {
            // Adapted from EngineIgnitor, ModuleEngineIgnitor.CheckUllageState()

            float feltForward = 9.80665f;
            if (HighLogic.LoadedSceneIsFlight)
            {
                Vector3d accelTotal = this.part.vessel.acceleration_immediate;
                Vector3d accelG = this.part.vessel.graviticAcceleration;

                Vector3d accelFelt = accelTotal - accelG;

                float a = Vector3.Angle(this.part.transform.up, accelFelt);
                //double a = Vector3.Angle(vessel.transform.up, accelFelt);
                feltForward = (float)(Math.Cos(a * Math.PI / 180) * accelFelt.magnitude);
            }

            float springForce = -springK * (animPosition - SpringAnchor());
            float dampingForce = springDamping * animSpeed;
            float acceleration = springForce + feltForward - dampingForce;

            animSpeed += acceleration * TimeWarp.fixedDeltaTime;
            float newPosition = animPosition + animSpeed * TimeWarp.fixedDeltaTime;

            if (newPosition <= 0 || newPosition >= 1)
            {
                if (bounce)
                    animSpeed = -animSpeed;
                else
                    animSpeed = 0;
            }
            SetAnimation(newPosition);
        }

        public void SetAnimation(float position, float speed = 0.0f)
        {
            animPosition = Math.Min(1, Math.Max(0, position));
            //animSpeed = speed;
            foreach (var anim in animationStates)
            {
                anim.normalizedTime = position;
                anim.speed = speed;
            }
        }

        public AnimationState[] SetUpAnimation(string animationName, Part part)
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.layer = layer;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);

            }
            return states.ToArray();
        }

        #region IScalarModule Interface

        // From Starwaster's Animated Decoupler

        public override void OnAwake()
        {
            OnMovingEvent = new EventData<float, float>("ModuleBdbDecouplerAnimation.OnMovingEvent");
            OnStopEvent = new EventData<float>("ModuleBdbDecouplerAnimation.OnStopEvent");
            base.OnAwake();
        }

        private EventData<float, float> OnMovingEvent;

        private EventData<float> OnStopEvent;



        public bool IsMoving()
        {
            return false;
        }

        public void SetScalar(float t)
        {
            //throw new NotImplementedException();
        }

        public void SetUIRead(bool state)
        {
            //throw new NotImplementedException();
        }

        public void SetUIWrite(bool state)
        {
            //throw new NotImplementedException();
        }


        public string ScalarModuleID
        {
            get
            {
                return moduleID;
            }
        }

        public float GetScalar
        {
            get
            {
                return SpringAnchor();
            }
        }

        public bool CanMove
        {
            get
            {
                return CanDeploy();
            }
        }

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
    }
}
