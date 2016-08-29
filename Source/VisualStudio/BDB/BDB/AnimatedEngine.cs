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
		public float WaitForAnimation = 1.0f;

        [KSPField(isPersistant = false)]
        public int Layer = 1;

        [KSPField(isPersistant = false)]
        public bool isOneShot = false;

        [KSPField(isPersistant = true)]
        public bool deployed = false;

        private bool engineIsOn = false;
        private bool hasMultiEngine = false;

		private List<ModuleEngines> engines = new List<ModuleEngines>();
        private MultiModeEngine multiController;
        private string activeEngineName = "";
		private AnimationState[]  animationStates;

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Animation")]
		public void ToggleAnimationEditor()
		{
			engineIsOn = !engineIsOn;
		}
		

		public void Start()
		{
			
			animationStates = SetUpAnimation(animationName, this.part);
		
			if(HighLogic.LoadedSceneIsFlight)
			{
                engines = this.GetComponents<ModuleEngines>().ToList();
                engineIsOn = QueryEngineOn() || (isOneShot && deployed);

                multiController = this.GetComponent<MultiModeEngine>();
                if (multiController != null)
                    hasMultiEngine = true;
			}

            
			foreach(AnimationState anim in animationStates)
			{
				if (engineIsOn)
				{
						anim.normalizedTime = 1f;
				}
				else
				{
						anim.normalizedTime = 0f;
				}
			}
			
		}
		
		
		
		public void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
                engineIsOn = QueryEngineOn();
                deployed = deployed || engineIsOn;
                if (hasMultiEngine)
                    activeEngineName = multiController.mode;
			}

			foreach(var anim in animationStates)
			{
                if (engineIsOn && anim.normalizedTime < WaitForAnimation)
				{
					anim.speed = 1;
					if(HighLogic.LoadedSceneIsFlight)
					{
                        if (hasMultiEngine)
                        {
                            foreach (ModuleEngines fx in engines)
                            {
                                if (fx.engineID == activeEngineName)
                                    fx.Shutdown();
                            }
                        }
                        else
                        {
                            foreach (ModuleEngines fx in engines)
                            {
                                fx.Shutdown();
                            }
                        }
					}
				}
				
				
				if(HighLogic.LoadedSceneIsFlight &&  anim.normalizedTime >= WaitForAnimation && anim.speed > 0)
				{
                    if (hasMultiEngine)
                    {
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
				
				if(anim.normalizedTime>=1)
				{
					anim.speed = 0;
					anim.normalizedTime = 1;
				}
				
				if(anim.normalizedTime >=1 && !engineIsOn && !(HighLogic.LoadedSceneIsFlight && isOneShot))
				{
					anim.speed = -1;
					
				}
				
				if(anim.normalizedTime <0)
				{
					anim.speed = 0;
					anim.normalizedTime = 0;
				}
				
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
				
            }
            return states.ToArray();
        }
	}
}
