using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Taken from Nertea's ModuleDeployableEngines
// https://github.com/ChrisAdderley/DeployableEngines

// The concepts behind this code are credited to BahamutoD's AnimatedEngine module
// can be found https://github.com/BahamutoD/BDAnimationModules/blob/master/BDAnimationModules/AnimatedEngine.cs

namespace BDB
{
    public class ModuleBdbAnimatedEngine : PartModule
	{
		[KSPField(isPersistant = false)]
		public string animationName;
		
		[KSPField(isPersistant = false)]
		public float WaitForAnimation = 1.0f; // >0.0 to 1.0

        [KSPField(isPersistant = false)]
        public int Layer = 1;

        [KSPField(isPersistant = false)]
        public bool isOneShot = true;

        [KSPField(isPersistant = true)]
        public bool deployed = false;

        private bool wantEngineOn = false;
        private bool hasMultiEngine = false;

        private float animPosition = 0f;
        private float animSpeed = 0f;
        private bool playing = false;

        private List<ModuleEngines> engines = new List<ModuleEngines>();
        private MultiModeEngine multiController;
        private string activeEngineName = "";
		private AnimationState[]  animationStates;

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Nozzle")]
		public void ToggleAnimationEditor()
		{
            float x = 0;
            if (animPosition >= 1)
            {
                x = -1;
            }
            else if (animPosition <= 0)
            {
                x = 1;
            }
            else
            {
                x = animSpeed * -1;
            }
            PlayAnimation(x);
        }
		

		public void Start()
		{
			
			animationStates = SetUpAnimation(animationName, this.part);
            Events["ToggleAnimationEditor"].guiActive = !isOneShot;
		
			if(HighLogic.LoadedSceneIsFlight)
			{
                engines = this.GetComponents<ModuleEngines>().ToList();
                multiController = this.GetComponent<MultiModeEngine>();
                if (multiController != null)
                    hasMultiEngine = true;

                wantEngineOn = QueryEngineOn();
                if (wantEngineOn)
                    deployed = true;
			}

            if (deployed)
            {
                SetAnimation(1, 0);
            }
            else
            {
                SetAnimation(0, 0);
            }
		}
		
		
		
		public void FixedUpdate()
		{
            float oldSpeed = animSpeed;

            if (HighLogic.LoadedSceneIsFlight)
			{
                if (!playing)
                {
                    wantEngineOn = QueryEngineOn();
                }
			}

			foreach(var anim in animationStates)
			{
                if(anim.normalizedTime>=1)
				{
					anim.speed = 0;
					anim.normalizedTime = 1;
				}
				
				if(anim.normalizedTime <0)
				{
					anim.speed = 0;
					anim.normalizedTime = 0;
				}

                animPosition = anim.normalizedTime;
                animSpeed = anim.speed;
            }
            deployed = animPosition > 0 || animSpeed != 0;
            
            if (playing && animSpeed == 0)
            {
                playing = false;
                //OnStop.Fire(animPosition);
            }

            if (wantEngineOn && animPosition < WaitForAnimation) // engine on, nozzle not extended enough
            {
                SetEngineOff();
                if (!playing) 
                {
                    PlayAnimation(1); // Engine ignited while stowed. Extend nozzle, engine will ignite when extended enough
                }
                else
                {
                    if (animSpeed < 0) // retracting
                    {
                        wantEngineOn = false; // shutdown due to retracting. Toggle this or it will reignite.
                    }
                }
            }
            if (wantEngineOn && animPosition >= WaitForAnimation && oldSpeed > 0) // extending, and nozzle extended enough, ok to start engine
            {
                SetEngineOn();
            }

            if (!playing)
            {
                if (animPosition == 0)
                    Events["ToggleAnimationEditor"].guiName = "Extend Nozzle";
                else
                    Events["ToggleAnimationEditor"].guiName = "Retract Nozzle";
            }
            else
            {
                if (animSpeed < 0)
                    Events["ToggleAnimationEditor"].guiName = "Extend Nozzle";
                else
                    Events["ToggleAnimationEditor"].guiName = "Retract Nozzle";
            }
        }

        private bool QueryEngineOn()
        {
            foreach (ModuleEngines e in engines)
            {
                if (e.EngineIgnited)
                    return true;
            }
            return false;
        }

        private void SetEngineOn()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (hasMultiEngine)
                {
                    activeEngineName = multiController.mode;
                    foreach (ModuleEngines fx in engines)
                    {
                        if (fx.engineID == activeEngineName)
                            fx.Activate();
                    }
                }
                else
                {
                    foreach (ModuleEngines fx in engines)
                    {
                        fx.Activate();
                    }
                }
            }
        }

        private void SetEngineOff()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (hasMultiEngine)
                {
                    activeEngineName = multiController.mode;
                    foreach (ModuleEngines fx in engines)
                    {
                        if (fx.engineID == activeEngineName && fx.EngineIgnited)
                            fx.Shutdown();
                    }
                }
                else
                {
                    foreach (ModuleEngines fx in engines)
                    {
                        if (fx.EngineIgnited)
                            fx.Shutdown();
                    }
                }
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
            //float moveTo = animPosition;

            //if (speed < 0)
            //    moveTo = 0f;
            //else if (speed > 0)
            //    moveTo = 1f;

            //OnMoving.Fire(animPosition, moveTo);

            foreach (var anim in animationStates)
            {
                anim.speed = speed;
            }

            playing = speed != 0f;
        }

        public AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.layer = Layer;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
                break;
            }
            return states.ToArray();
        }
	}
}
