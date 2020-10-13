using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDB
{
    class ModuleBdbSequentialFire : PartModule
    {
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Sequential Fire"), UI_Toggle(affectSymCounterparts = UI_Scene.All)]
        public bool sequentialFire = false;

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, guiName = "Overlap", guiFormat = "P1"), UI_FloatRange(minValue = 0.0f, maxValue = 1.0f, affectSymCounterparts = UI_Scene.All)]
        public float overlap = 0.0f;

        private bool saveSequentialFire = false;
        public int sequence = 0;
        public bool running = false;
        public ModuleEngines engine;
        public ModuleJettison jettison;

        private void UpdateSequences()
        {
            sequence = 1;

            int symCount = part.symmetryCounterparts.Count;
            int mid = (int)Math.Floor(symCount / 2d);
            int i;
            int s = 3;
            for (i = 0; i < mid; i++)
            {
                part.symmetryCounterparts[i].Modules.OfType<ModuleBdbSequentialFire>().FirstOrDefault().sequence = s;
                s += 2;
            }
            s = 2;
            for (i = mid; i < symCount; i++)
            {
                part.symmetryCounterparts[i].Modules.OfType<ModuleBdbSequentialFire>().FirstOrDefault().sequence = s;
                s += 2;
            }
        }

        private void UpdateSequentialFire()
        {
            saveSequentialFire = sequentialFire;
            engine = part.FindModulesImplementing<ModuleEngines>().FirstOrDefault();
            if (engine != null)
            {
                //stagingEnabled = engine.stagingEnabled && sequentialFire;
                engine.stagingEnabled = !sequentialFire;
            }
            jettison = part.FindModulesImplementing<ModuleJettison>().FirstOrDefault();
            if (jettison != null)
            {
                jettison.stagingEnabled = !sequentialFire;
            }
        }

        public override void OnStart(StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            UpdateSequentialFire();
        }

        public override void OnActive()
        {
            Fields["sequentialFire"].guiActive = false;
            Fields["overlap"].guiActive = false;
            if (!sequentialFire)
                return;

            if (engine != null && part.Resources["SolidFuel"] != null)
            running = true;
            if (sequence == 0)
            {
                UpdateSequences();
            }
        }

        public override void OnUpdate()
        {
            if (sequentialFire != saveSequentialFire)
                UpdateSequentialFire();

            if (!sequentialFire)
                return;

            if (running && sequence == 1)
            {
                if (!engine.EngineIgnited)
                    engine.Activate();
                if (jettison != null && !jettison.isJettisoned)
                    jettison.Jettison();

                double r = part.Resources["SolidFuel"].amount / part.Resources["SolidFuel"].maxAmount;
                if (r <= overlap)
                {
                    sequence--;
                    foreach (Part counterpart in this.part.symmetryCounterparts)
                    {
                        counterpart.Modules.OfType<ModuleBdbSequentialFire>().FirstOrDefault().sequence--;
                    }
                }
            }
        }
    }

    class ModuleBdbMercuryLES : PartModule
    {
        public bool aborting = false;
        private ModuleEngines escapeEngine;
        private ModuleEngines jettisonEngine;

        [KSPField(isPersistant = false, guiActive = false)]
        public string escapeEngineID = "LES";

        [KSPField(isPersistant = false, guiActive = false)]
        public string jettisonEngineID = "jettison";

        public override void OnStart(StartState state)
        {

            // hook up to the part attach callback
            /*
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
            }
            */

            List<ModuleEngines> engines = part.FindModulesImplementing<ModuleEngines>();
            foreach (ModuleEngines e in engines)
            {
                if (e.engineID == escapeEngineID)
                {
                    escapeEngine = e;
                }
                else if (e.engineID == jettisonEngineID)
                {
                    jettisonEngine = e;
                }
                foreach (BaseAction a in e.Actions)
                {
                    a.active = false;
                }
                foreach (BaseField f in e.Fields)
                {
                    f.guiActive = false;
                    f.guiActiveEditor = false;
                }
                foreach (BaseEvent ev in e.Events)
                {
                    ev.guiActive = false;
                    ev.guiActiveEditor = false;
                }
            }

            // check for nulls in engines

        }

        [KSPAction("Abort!")]
        public void AbortAction(KSPActionParam param)
        {
            aborting = true;
            escapeEngine.Activate();
        }

        [KSPAction("Jettison Tower")]
        public void ActivateJettisonAction(KSPActionParam param)
        {
            DoJettison();
        }

        // fires when part is staged
        public override void OnActive()
        {
            DoJettison();
        }

        [KSPEvent(guiName = "Jettison Tower", guiActive = true)]
        public void DoJettison()
        {
            if (!escapeEngine.flameout)
                escapeEngine.Activate();
            else
                jettisonEngine.Activate();

            if (part.Modules.Contains("ModuleDecouple"))
            {
                part.FindModulesImplementing<ModuleDecouple>().FirstOrDefault().Decouple();
            }
        }

        public void FixedUpdate()
        {
            if (aborting)
            {
                if (part.Resources["SolidFuel"].amount / part.Resources["SolidFuel"].maxAmount < 0.1)
                {
                    escapeEngine.Shutdown();
                    DoJettison();
                    aborting = false;
                }
            }
        }
    }

    class ModuleBdbMercuryAbortChuteController : PartModule
    {
        public bool aborting = false;
        public ModuleParachute drogueChute;
        public ModuleParachute mainChute;

        [KSPField(isPersistant = false)]
        public string topNodeName = "top";

        [KSPField(isPersistant = false)]
        public string chuteNodeName = "parachute";

        [KSPAction("Abort!")]
        public void AbortAction(KSPActionParam param)
        {
            aborting = true;

            AttachNode aNode = part.FindAttachNode(topNodeName);
            if (aNode == null)
                aNode = part.FindAttachNode("node_stack_" + topNodeName);
            if (aNode != null && aNode.attachedPart != null)
                drogueChute = aNode.attachedPart.FindModulesImplementing<ModuleParachute>().FirstOrDefault();

            aNode = part.FindAttachNode(chuteNodeName);
            if (aNode == null)
                aNode = part.FindAttachNode("node_stack_" + chuteNodeName);
            if (aNode != null && aNode.attachedPart != null)
                mainChute = aNode.attachedPart.FindModulesImplementing<ModuleParachute>().FirstOrDefault();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!aborting)
                return;
            if (vessel.verticalSpeed >= 0)
                return;
            if (vessel.altitude > drogueChute.deployAltitude)
            {
                aborting = false;
                return;
            }

            if (vessel.FindPartModuleImplementing<ModuleBdbMercuryLES>() == null)
            {
                if (vessel.radarAltitude < mainChute.deployAltitude)
                {
                    part.FindModulesImplementing<ModuleDecouple>().FirstOrDefault().Decouple();
                    mainChute.Deploy();
                    ModuleBdbMercuryLandingBag bag = vessel.FindPartModuleImplementing<ModuleBdbMercuryLandingBag>();
                    if (bag != null)
                        bag.Arm();
                    aborting = false;
                }
                else if (vessel.radarAltitude < drogueChute.deployAltitude)
                {
                    drogueChute.Deploy();
                }
            }
        }
    }

    class ModuleBdbMercuryLandingBag : PartModule, IScalarModule, IMultipleDragCube
    {
        [KSPField]
        public string moduleID = "bdbMercuryLandingBag";

        [KSPField(guiActive = true, isPersistant = false, guiActiveEditor = false, guiName = "Landing bag")]
        public string statusDisplay = "";

        [KSPField(isPersistant = false)]
        public string animationName = "";

        [KSPField(isPersistant = false)]
        public int layer = 1;

        [KSPField(isPersistant = false)]
        public string restrictedNodeName = "";

        public AttachNode restrictedNode = null;
        public bool playing = false;

        [KSPField(isPersistant = true)]
        public float animPosition = 0f;

        [KSPField(isPersistant = true)]
        public float animSpeed = 0f;

        private AnimationState[] animationStates;

        [KSPField(isPersistant = true)]
        public bool armed = false;

        [KSPField(isPersistant = true)]
        public bool deployed = false;

        [KSPField(isPersistant = false)]
        public int deployAltitude = 500;

        public bool wasFlying = false;

        public void Start()
        {
            restrictedNode = part.FindAttachNode(restrictedNodeName);
            if (restrictedNode == null)
                restrictedNode = part.FindAttachNode("node_stack_" + restrictedNodeName);

            animationStates = SetUpAnimation(animationName, this.part);
            SetAnimation(animPosition, 0);
            if (animSpeed != 0)
                PlayAnimation(animSpeed);
            if (part.stackIcon == null)
                Debug.LogFormat("{0}: stackIcon is null", moduleID);
        }

        public override void OnActive()
        {
            Arm();
        }

        [KSPEvent(guiName = "Arm Landing Bag", guiActive = true)]
        public void Arm()
        {
            Events["Arm"].active = false;
            armed = true;
            //part.stackIcon.SetBackgroundColor(Color.yellow);
        }

        [KSPAction("Arm Landing Bag")]
        public void ArmAction(KSPActionParam param)
        {
            Arm();
        }

        [KSPEvent(guiName = "Deploy Landing Bag", guiActive = true)]
        public void Deploy()
        {
            if (!CanMove)
                return;

            Arm();

            Events["Deploy"].active = false;
            deployed = true;
            part.stackIcon.SetIconColor(Color.green);
            if (!(vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED))
            {
                wasFlying = true;
                PlayAnimation(1);
            }
            
        }

        [KSPAction("Deploy Landing Bag")]
        public void DeployAction(KSPActionParam param)
        {
            Deploy();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            statusDisplay = vessel.situation.ToString();
            UpdateAnimation();

            if (deployed)
            {
                if (animPosition > 0 && animSpeed >= 0 && wasFlying && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED))
                    PlayAnimation(-0.2f);
                else if (animPosition == 0 && vessel.situation == Vessel.Situations.SPLASHED)
                    PlayAnimation(0.1f);
                wasFlying = !(vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED);
            }
            if (armed && !deployed)
            {
                if (CanMove)
                    part.stackIcon.SetIconColor(Color.yellow);
                else
                    part.stackIcon.SetIconColor(Color.red);

                if (vessel.radarAltitude < deployAltitude)
                    Deploy();
            }

            
        }

        public void UpdateAnimation()
        {
            foreach (var anim in animationStates)
            {
                if (anim.normalizedTime >= 1)
                {
                    anim.speed = Math.Min(0, anim.speed);
                    anim.normalizedTime = 1;
                }

                if (anim.normalizedTime <= 0)
                {
                    anim.speed = Math.Max(0, anim.speed);
                    anim.normalizedTime = 0;
                }
                animPosition = anim.normalizedTime;
                animSpeed = anim.speed;
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
            if (!CanMove)
                return;

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

            animSpeed = speed;
            playing = speed != 0f;
        }

        private void DoStartAnimation(float pos, float moveto)
        {
            OnMoving.Fire(animPosition, moveto);
            //part.Effect("deploy");
        }

        private void DoStopAnimation(float pos)
        {
            OnStop.Fire(animPosition);
            //part.Effect("deploy", 0f);
            //part.Effect("deployed");
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
                return restrictedNode == null || restrictedNode.attachedPart == null;
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

