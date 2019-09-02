using UnityEngine;
using UnityEngine.Serialization;

// Taken from ModuleDepthMask v1.1.0
// and added by CineboxAndrew
// https://github.com/drewcassidy/KSP-DepthMask

namespace BDB
{
    public class ModuleBdbDepthMask : PartModule
    {
        // The name of the transform that has your mask mesh. The only strictly required property
        [KSPField] public string maskTransform = "";

        [KSPField] public string bodyTransform = "";

        // The name of the depth mask shader
        [KSPField] public string shaderName = "DepthMask";

        // The render queue value for the mesh, should be less than maskRenderQueue
        [KSPField] public int meshRenderQueue = 1000;

        // the render queue value for the mask, should be less than 2000
        [KSPField] public int maskRenderQueue = 1999;


        // depth mask object transforms
        public Transform[] maskTransformObjects;

        // body object transform
        public Transform bodyTransformObject;

        // depth mask shader object
        public Shader depthShader;


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            UpdateAllMaterials();

            // the part variant system is implemented extremely stupidly
            // so we have to make this whole module more complicated as a result
            GameEvents.onVariantApplied.Add(OnVariantApplied);
        }


        private void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(OnVariantApplied);
        }


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) return;

            this.maskTransformObjects = base.part.FindModelTransforms(maskTransform);
            if (this.maskTransformObjects.Length == 0 || this.maskTransformObjects == null)
            {
                this.LogError($"Can't find transform {maskTransform}");
                return;
            }

            if (bodyTransform.Length == 0)
            {
                this.bodyTransformObject = base.part.partTransform;
            }
            else
            {
                this.bodyTransformObject = base.part.FindModelTransform(bodyTransform);
                if (this.bodyTransformObject == null)
                {
                    this.LogError($"Can't find transform {bodyTransform}");
                    this.bodyTransformObject = base.part.partTransform;
                }
            }

            this.depthShader = Shader.Find(shaderName);
            if (this.depthShader == null)
            {
                this.LogError($"Can't find shader {shaderName}");
                return;
            }
        }


        public void OnVariantApplied(Part appliedPart, PartVariant variant)
        {
            // I dont know why changing part variants resets all the materials to their as-loaded state, but it does
            if (appliedPart == this.part) UpdateAllMaterials();
        }


        private void UpdateAllMaterials()
        {
            var renderers = bodyTransformObject.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                var queue = renderer.material.renderQueue;
                if (queue <= maskRenderQueue) continue;
                queue = meshRenderQueue + ((queue - 2000) / 2);
                renderer.material.renderQueue = queue;
            }

            foreach (var maskObject in maskTransformObjects)
            {
                var renderer = maskObject.GetComponent<Renderer>();
                renderer.material.shader = depthShader;
                renderer.material.renderQueue = maskRenderQueue;
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[{part.partInfo?.name ?? part.name} {this.GetType()}] {message}");
        }
    }
}