using NaughtyAttributes;
using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Playground
{
    public abstract class AbstractRecipe : ScriptableObject, IRecipe
    {
        [Header("Metadata")]
        [SerializeField, Tooltip("The name of this recipe. Used in the UI for the player.")]
        string displayName = "TBD";
        [SerializeField, TextArea(1, 4), Tooltip("A short description of this recipe helping the player understand what it is.")]
        string description = "TBD";
        [SerializeField, Tooltip("An image to use as the hero image for this recipe."), ShowAssetPreview]
        Texture2D heroImage;
        [SerializeField, Tooltip("Powerups are recipes that can be offered between levels and, if purchased, become permanent.")]
        bool isPowerUp = false; 
        [SerializeField, Tooltip("The resources required to build this ammo type.")]
        int cost = 10;
        [SerializeField, Tooltip("The time it takes to build this recipe.")]
        float timeToBuild = 5;

        [Header("Feedback")]
        [SerializeField, Tooltip("The sound to play when the build is started. If there are no sounds here then a default will be used.")]
        AudioClip[] buildStartedClips = new AudioClip[0];
        [SerializeField, Tooltip("The sound to play when the build is complete. If there are no sounds here then a default will be used.")]
        AudioClip[] buildCompleteClips = new AudioClip[0];
        [SerializeField, Tooltip("The particle system to play when a pickup is spawned.")]
        ParticleSystem pickupParticles;

        public string UniqueID => uniqueID;

        public string DisplayName => displayName;

        public string Description => description;

        public Texture2D HeroImage => heroImage;

        public bool IsPowerUp => isPowerUp;

        public int Cost => cost;

        public float TimeToBuild => timeToBuild;

        public AudioClip BuildStartedClip
        {
            get
            {
                if (buildStartedClips.Length == 0)
                {
                    return null;
                } else
                {
                    return buildStartedClips[Random.Range(0, buildStartedClips.Length)];
                }
            }
        }

        public AudioClip BuildCompleteClip
        {
            get
            {
                if (buildCompleteClips.Length == 0)
                {
                    return null;
                }
                else
                {
                    return buildStartedClips[Random.Range(0, buildCompleteClips.Length)];
                }
            }
        }

        public ParticleSystem PickupParticles => pickupParticles;

        public virtual void Reset()
        {
        }

        public virtual bool ShouldBuild
        {
            get
            {
                return true;
            }
        }
        public virtual void BuildFinished()
        {
        }


        [SerializeField, Tooltip("DO NOT CHANGE THIS. TODO: Create a custom editor that hides this in case of accidental change."), BoxGroup("Internal"), ReadOnly]
        internal string uniqueID;

#if UNITY_EDITOR
        [Button("ERROR: Need a valid ID."), HideIf("IsValidID")]
        protected void GenerateID()
        {
            uniqueID = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private bool IsValidID
        {
            get { return string.IsNullOrEmpty(uniqueID) == false; }
        }
#endif
    }
}