using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FullBodyTracking.Cosmetics
{
    /// <summary>
    /// OBSOLETE, use <code>CharacterSkinSelector</code>
    /// </summary>
    [Obsolete]
    public class HandSkinSelector : MonoBehaviour
    {
        public enum Gender { M, F }

        [SerializeField] internal Gender gender = Gender.M;
        public Mesh maleHand, femaleHand;
        public int selectedSkin = 0;
        public Material[] maleSkins, femaleSkins;
        public float thickness = 1;

        public SkinnedMeshRenderer[] skinnedMeshRenderers;

        public void SelectSkin(Gender gender, int skinIndex)
        {
            this.gender = gender;
            this.selectedSkin = skinIndex;
            ApplySkin();
        }

        internal void ApplySkin()
        { 
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                skinnedMeshRenderer.sharedMesh = gender == Gender.M ? maleHand : femaleHand;

                var skins = gender == Gender.M ? maleSkins : femaleSkins;

                if (selectedSkin >= 0 && selectedSkin < skins.Length)
                {
                    skinnedMeshRenderer.enabled = true;
                    skinnedMeshRenderer.sharedMaterial = skins[(selectedSkin < 0 || selectedSkin >= skins.Length) ? 0 : selectedSkin];
                }
                else
                {
                    skinnedMeshRenderer.enabled = false;
                }
            }
        }

        private void SetupSkinMaterial()
        {
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                skinnedMeshRenderer.material.SetFloat("_RampPower", 0);
                skinnedMeshRenderer.material.SetColor("_TransluencyColor", new Color(.25f, .25f, .25f, 0f));
            }
        }

        public void Start()
        {
            ApplySkin();
            SetupSkinMaterial();
        }

        public void OnValidate()
        {
            ApplySkin();
        }
    }
}