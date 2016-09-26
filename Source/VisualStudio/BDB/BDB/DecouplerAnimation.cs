using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDB
{
    public class ModuleBdbDecouplerAnimation : PartModule , IScalarModule, IMultipleDragCube
    {
        [KSPField]
        public string moduleID = "bdbDecouplerAnimation";

        [KSPField(isPersistant = false)]
        public string animationName;

        [KSPField(isPersistant = false)]
        public float waitForAnimation = 0.1f;

        [KSPField(isPersistant = false)]
        public int layer = 1;

        [KSPField(isPersistant = false)]
        public bool isOneShot = true;

        private float animPosition = 0f;

        private float animSpeed = 0f;

        private bool decoupled = false;

        private bool playing = false;

        private ModuleDecouple decoupler;
        private ModuleDecouple payloadDecoupler;

        [KSPField(isPersistant = false)]
        public string decouplerNodeID = "top";

        [KSPField(isPersistant = false)]
        public string payloadDecouplerNodeID = "";

        private AnimationState[] animationStates;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Payload Bay Doors")]
        public void ToggleAnimationEditor()
        {
            float x = 0;
            if (animPosition >= 1)
            {
                x = -1;
            } else if (animPosition <= 0)
            {
                x = 1;
            } else
            {
                x = x * -1;
            }
            PlayAnimation(x);
        }


        public void Start()
        {

            animationStates = SetUpAnimation(animationName, this.part);
            //if (part.stagingIcon == "")
            //    part.stagingIcon = "DECOUPLER_HOR";

            List<ModuleDecouple> decouplers = new List<ModuleDecouple>();
            decouplers = this.GetComponents<ModuleDecouple>().ToList();
            foreach (ModuleDecouple d in decouplers)
            {
                if (d.explosiveNodeID == decouplerNodeID)
                    decoupler = d;
                else if (payloadDecouplerNodeID != "" && d.explosiveNodeID == payloadDecouplerNodeID)
                    payloadDecoupler = d;
            }
            if (decoupler == null)
                Debug.LogErrorFormat("[{0}] A '{1}' node decoupler was not found.", moduleID, decouplerNodeID);
            if (payloadDecouplerNodeID != "" && payloadDecoupler == null)
                Debug.LogErrorFormat("[{0}] A '{1}' node decoupler was not found.", moduleID, payloadDecouplerNodeID);

            if (HighLogic.LoadedSceneIsFlight)
            {
                decoupled = decoupler != null && decoupler.isDecoupled;
                if (payloadDecoupler != null)
                    payloadDecoupler.isEnabled = decoupled;
            }


            foreach (AnimationState anim in animationStates)
            {
                if (decoupled)
                {
                    anim.normalizedTime = 1f;
                }
                else
                {
                    anim.normalizedTime = 0f;
                }
            }

        }



        public void FixedUpdate() // Clean this up and use OnVesselWasModified to catch decouple by other means
        {
            foreach (var anim in animationStates)
            {
                if (anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }

                if (HighLogic.LoadedSceneIsFlight && !decoupled && decoupler != null && decoupler.isDecoupled)
                {
                    decoupled = true;
                    PlayAnimation(1);
                }

                if (anim.normalizedTime < 0)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 0;
                }
                animPosition = anim.normalizedTime;
                animSpeed = anim.speed;
            }

            if (HighLogic.LoadedSceneIsFlight && animPosition >= waitForAnimation && animSpeed > 0)
            {
                if (decoupler != null && !decoupler.isDecoupled)
                {
                    decoupler.Decouple();
                }
                decoupled = true;
                if (payloadDecoupler != null)
                    payloadDecoupler.isEnabled = true;
            }

            if (playing && animSpeed == 0f)
            {
                playing = false;
                OnStop.Fire(animPosition);
            }
        }

        public override void OnActive()
        {
            PlayAnimation(1);
        }

        public void SetAninmation(float position, float speed = 0.0f)
        {
            animPosition = position;
            animSpeed = speed;
            foreach (var anim in animationStates)
            {
                anim.normalizedTime = position;
                anim.speed = speed;
            }
        }

        public void PlayAnimation(float speed)
        {
            float moveTo = animPosition;

            if (speed < 0)
                moveTo = 0f;
            else if (speed > 0)
                moveTo = 1f;

            OnMoving.Fire(animPosition, moveTo);

            foreach (var anim in animationStates)
            {
                anim.speed = speed;
            }

            playing = speed != 0f;
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
            return animSpeed != 0f;
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
                return animPosition;
            }
        }

        public bool CanMove
        {
            get
            {
                return !isOneShot || animPosition < 1;
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

        #region IMultipleDragCube Interface

        public string[] GetDragCubeNames()
        {
            throw new NotImplementedException();
        }

        public void AssumeDragCubePosition(string name)
        {
            throw new NotImplementedException();
        }

        public bool UsesProceduralDragCubes()
        {
            return true;
        }

        public bool IsMultipleCubesActive
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

