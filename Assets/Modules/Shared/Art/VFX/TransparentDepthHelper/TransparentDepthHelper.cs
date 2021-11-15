using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    [System.Serializable]
    public class MaterialPair
    {
        public Material transparentMaterial;
        public Material opaqueMaterial;
        public List<MeshRenderer> meshRenderers;
        public List<SkinnedMeshRenderer> skinnedMeshRenderers;
    }

    public class TransparentDepthHelper : MonoBehaviour
    {
        public List<MaterialPair> materialPairs = default;
        private const int DefaultLayer = 0;
        private const int TransparentLayer = 15;

        void Awake()
        {
            //SetTransparent(true);
        }

        public void SetTransparent(bool transparent)
        {
            SetMaterial(transparent);
        }

        private void SetMaterial(bool transparent)
        {
            foreach (MaterialPair materialPair in materialPairs)
            {
                Material material = (!transparent) ?
                    materialPair.opaqueMaterial :
                    materialPair.transparentMaterial;

                foreach (MeshRenderer meshRenderer in materialPair.meshRenderers)
                {
                    meshRenderer.gameObject.layer = (!transparent) ? DefaultLayer : TransparentLayer;
                    meshRenderer.material = material;
                }

                foreach (SkinnedMeshRenderer skinnedMeshRenderer in materialPair.skinnedMeshRenderers)
                {
                    skinnedMeshRenderer.gameObject.layer = (!transparent) ? DefaultLayer : TransparentLayer;
                    skinnedMeshRenderer.material = material;
                }
            }
        }
    }
}