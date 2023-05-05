using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FullBodyTracking.Cosmetics
{
    public class CharacterSkinSelector : MonoBehaviour
    {
        [Serializable]
        public class VRCharacterSkin
        {
            public Texture icon;
            [FormerlySerializedAs("skinRenderers")] public Renderer[] renderers; 
        }

        public VRCharacterSkin[] skins;

        [SerializeField] int selectedSkin = 0;
        [SerializeField] int availableSkins = 0;

        public int SkinCount => UserConfig.DevMode ? skins.Length : Mathf.Min(availableSkins, skins.Length);
        public int SelectedSkin { get => selectedSkin; set => SelectSkin(value); }
 
        public void OnValidate()
        {
            if (availableSkins == 0) availableSkins = skins.Length;
            ApplySkin();
        }

        public Texture GetSkinIcon(int index)
        {
            return skins[index].icon;
        }

        public void SelectSkin(int index)
        {
            selectedSkin = index;
            this.ApplySkin();
        }

        private void ApplySkin()
        {
            for (int i = 0; i < skins.Length; i++)
            {
                var skin = skins[i];

                foreach (var meshRenderer in skin.renderers)
                {
                    meshRenderer.enabled = false;
                }
            }

            if (selectedSkin >= 0 && selectedSkin < skins.Length)
            {
                var skin = skins[selectedSkin];

                foreach (var meshRenderer in skin.renderers)
                {
                    meshRenderer.enabled = true;
                } 
            }
        }

        public void Start()
        {
            ApplySkin();
        } 
    }
}