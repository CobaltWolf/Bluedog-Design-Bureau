using UnityEngine;

namespace BDB
{
    public class ModuleBdbDefAGHelper : PartModule
    {
        [KSPField(isPersistant = true)]
        public string actionModuleName;

        [KSPField(isPersistant = true)]
        public int actionModuleIndex;

        [KSPField(isPersistant = true)]
        public string actionName;

        [KSPField(isPersistant = true)]
        public KSPActionGroup actionDefaultActionGroup;

        [KSPField(isPersistant = true)]
        public bool saveFlag = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsEditor && !saveFlag)
            {
                bool found = false;
                int saveIndex = actionModuleIndex;
                foreach (PartModule p in this.part.Modules )
                {
                    if (p.moduleName == actionModuleName)
                    {
                        if (actionModuleIndex > 0)
                        {
                            actionModuleIndex--;
                        }
                        else
                        {
                            found = true;
                            BaseAction a = p.Actions[actionName];
                            if (a != null)
                            {
                                a.actionGroup = actionDefaultActionGroup;
                            }
                            else
                            {
                                Debug.LogErrorFormat("[{0}] : An Action named {1} was not found on PartModule {2}", moduleName, actionName, actionModuleName);
                            }
                            break;
                        }
                    }
                }
                if (!found)
                {
                    Debug.LogErrorFormat("[{0}] : A PartModule named {1} was not found at index {2}", moduleName, actionModuleName, saveIndex);
                }
                saveFlag = true;
           }
        }
    }
}
