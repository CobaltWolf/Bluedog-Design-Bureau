using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDB
{
    class ModuleBdbAccelAnimation : PartModule
    {
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

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spring Anchor"), UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f)]
        public float springAnchor = 0.5f;

        double animPosition = 0.5;
        double animSpeed = 0;

        private AnimationState[] animationStates;


        public void Start()
        {
            animationStates = SetUpAnimation(animationName, this.part);
            animPosition = springAnchor;
            SetAnimation((float)animPosition);
            Fields["springK"].guiActive = showEditor;
            Fields["springK"].guiActiveEditor = showEditor;
            Fields["springDamping"].guiActive = showEditor;
            Fields["springDamping"].guiActiveEditor = showEditor;
            Fields["springAnchor"].guiActive = showEditor;
            Fields["springAnchor"].guiActiveEditor = showEditor;
        }

        public void FixedUpdate()
        {
            // Adapted from EngineIgnitor, ModuleEngineIgnitor.CheckUllageState()

            Vector3d accelTotal = this.part.vessel.acceleration_immediate;
            Vector3d accelG = this.part.vessel.graviticAcceleration;

            Vector3d accelFelt = accelTotal - accelG;

            double a = Vector3.Angle(this.part.transform.up, accelFelt);
            //double a = Vector3.Angle(vessel.transform.up, accelFelt);
            double feltForward = Math.Cos(a * Math.PI / 180) * accelFelt.magnitude;
            
            double springForce = -springK * (animPosition - springAnchor);
            double dampingForce = springDamping * animSpeed;
            double acceleration = springForce + feltForward - dampingForce;

            animSpeed += acceleration * TimeWarp.fixedDeltaTime;

            double newPosition = animPosition + animSpeed * TimeWarp.fixedDeltaTime;

            if (newPosition <= 0 || newPosition >= 1)
                animSpeed = -animSpeed; // 0;
            SetAnimation((float)newPosition);
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
    }
}
