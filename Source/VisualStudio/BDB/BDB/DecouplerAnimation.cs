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
        public bool waitDuringAbort = false;

        [KSPField(isPersistant = false)]
        public int layer = 1;

        [KSPField(isPersistant = false)]
        public bool isOneShot = true;

        [KSPField(isPersistant = true)]
        public float animPosition = 0f;

        [KSPField(isPersistant = true)]
        public float animSpeed = 0f;

        private bool decoupled = false;

        private bool playing = false;

        private ModuleDecouplerBase decoupler;
        private ModuleDecouplerBase payloadDecoupler;

        [KSPField(isPersistant = false)]
        public string decouplerNodeID = "";

        [KSPField(isPersistant = false)]
        public string payloadDecouplerNodeID = "";

        private AnimationState[] animationStates;

        [KSPField(isPersistant = false)]
        public string editorGUIName = "Toggle Animation";

        [KSPField(isPersistant = false)]
        public string flightGUIName = "Decouple";

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Animation")]
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
                x = animSpeed * -1;
            }
            PlayAnimation(x);
        }

        [KSPEvent(guiName = "Decouple", guiActive = true)]
        public void Decouple()
        {
            Events["Decouple"].active = false;
            if (animPosition == 0)
            {
                PlayAnimation(1);
            }
        }

        [KSPAction("Decouple")]
        public void DecoupleAction(KSPActionParam param)
        {
            if (!waitDuringAbort && param.group == KSPActionGroup.Abort)
            {
                if (decoupler != null)
                    decoupler.Decouple();
            }
            else
            {
                Decouple();
            }
        }

        public override void OnActive()
        {
            Decouple();
        }

        public void Start()
        {
            animationStates = SetUpAnimation(animationName, this.part);
            //if (part.stagingIcon == "")
            //    part.stagingIcon = "DECOUPLER_HOR";

            List<ModuleDecouplerBase> decouplers = new List<ModuleDecouplerBase>();
            decouplers = this.GetComponents<ModuleDecouplerBase>().ToList();
            foreach (ModuleDecouplerBase d in decouplers)
            {
                if (d.explosiveNodeID == decouplerNodeID)
                {
                    decoupler = d;
                    decoupler.Actions["DecoupleAction"].active = false;
                    decoupler.Events["Decouple"].active = false;
                    decoupler.Events["ToggleStaging"].active = false;
                    if (HighLogic.LoadedSceneIsFlight && decoupler.ExplosiveNode.attachedPart != null)
                    {
                        animPosition = 0;
                        animSpeed = 0;
                    }
                }
                else if (payloadDecouplerNodeID != "" && d.explosiveNodeID == payloadDecouplerNodeID)
                {
                    payloadDecoupler = d;
                    payloadDecoupler.Events["Decouple"].guiName = "Decouple Payload";
                    payloadDecoupler.Actions["DecoupleAction"].guiName = "Decouple Payload";
                    payloadDecoupler.Fields["ejectionForcePercent"].guiName = "Force Percent (Payload)";
                    if (HighLogic.LoadedSceneIsFlight)
                        payloadDecoupler.isEnabled = animPosition >= waitForAnimation;
                }
            }
            if (decoupler == null)
                Debug.LogErrorFormat("[{0}] A '{1}' node decoupler was not found.", moduleID, decouplerNodeID);
            if (payloadDecouplerNodeID != "" && payloadDecoupler == null)
                Debug.LogErrorFormat("[{0}] A '{1}' node decoupler was not found.", moduleID, payloadDecouplerNodeID);

            SetAnimation(animPosition, 0);
            if (animSpeed != 0)
                PlayAnimation(animSpeed);

            Events["Decouple"].active = animPosition == 0;
            Events["Decouple"].guiName = flightGUIName;
            Actions["DecoupleAction"].guiName = flightGUIName;
            Events["ToggleAnimationEditor"].guiName = editorGUIName;
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

            if (HighLogic.LoadedSceneIsFlight && animPosition >= waitForAnimation && playing)
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
                DoStopAnimation(animPosition);
            }
        }

        public void SetAnimation(float position, float speed = 0.0f)
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

            if (speed != 0)
                DoStartAnimation(animPosition, moveTo);

            foreach (var anim in animationStates)
            {
                anim.speed = speed;
            }

            playing = speed != 0f;
        }

        private void DoStartAnimation(float pos, float moveto)
        {
            OnMoving.Fire(animPosition, moveto);
            part.Effect("deploy");
        }

        private void DoStopAnimation(float pos)
        {
            OnStop.Fire(animPosition);
            part.Effect("deploy", 0f);
            part.Effect("deployed");
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
                return false;
            }
        }

        #endregion
    }
}

