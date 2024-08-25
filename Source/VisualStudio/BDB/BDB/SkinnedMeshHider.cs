using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Taken from Restock https://github.com/PorktoberRevolution/ReStocked/blob/master/Source/Restock/ModuleRestockDeployableMeshHider.cs by Chris Adderley, LGPL license

namespace BDB
{
    public class ModuleBDBDeployableMeshHider : PartModule
    {
        private ModuleDeployablePart deployable;
        public List<GameObject> disableableGameObjects;
        private ModuleDeployablePart.DeployState savedState;

        [SerializeField]
        private string serializedNode;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (serializedNode == null)
                serializedNode = node.ToString();
        }


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {

                /*if (string.IsNullOrEmpty(serializedNode))
                {
                    this.LogError("Serialized node is null or empty!");
                    return;
                }*/

                ConfigNode node = ConfigNode.Parse(serializedNode).nodes[0];
                LoadTransforms(node);


                deployable = this.GetComponent<ModuleDeployablePart>();
                if (deployable == null)
                {
                    Debug.LogError("No ModuleDeployablePart found on part");
                    return;
                }

                savedState = deployable.deployState;
                SetVisibility(savedState != ModuleDeployablePart.DeployState.BROKEN);
            }
        }
        public void LoadTransforms(ConfigNode node)
        {

            disableableGameObjects = new List<GameObject>();

            foreach (string transformName in node.GetValues("transformName"))
            {

                Transform[] transforms = part.FindModelTransforms(transformName);
                /*if (transforms.Length == 0)
                {
                    this.LogError($"No transforms named '{transformName}' found in model");
                    continue;
                }*/

                foreach (Transform xform in transforms)
                {
                    disableableGameObjects.Add(xform.gameObject);
                }
            }
        }

        private void SetVisibility(bool visible)
        {
            for (int i = 0; i < disableableGameObjects.Count; i++)
            {
                disableableGameObjects[i].SetActive(visible);
            }
        }
        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && deployable && disableableGameObjects.Count > 0)
            {

                if (deployable.deployState != savedState)
                {
                    SetVisibility(deployable.deployState != ModuleDeployablePart.DeployState.BROKEN);
                    savedState = deployable.deployState;
                }
            }
        }
        void OnDestroy()
        {

            if (HighLogic.LoadedSceneIsFlight && disableableGameObjects.Count > 0)
                SetVisibility(false);
        }
    }
}
