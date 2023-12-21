using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static NeoFPS.BasicHealthManager;

namespace Playground
{
    public class NanobotManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The health recipes available to the Nanobots in order of preference.")]
        private List<HealthPickupRecipe> healthRecipes = new List<HealthPickupRecipe>();
        [SerializeField, Tooltip("The ammo recipes available to the Nanobots in order of preference.")]
        private List<AmmoPickupRecipe> ammoRecipes = new List<AmmoPickupRecipe>();
        [SerializeField, Tooltip("Cooldown between recipes.")]
        private float cooldown = 3;

        [Header("Feedback")]
        [SerializeField, Tooltip("The sound to play when the build is started. Note that this can be overridden in the recipe.")]
        private AudioClip buildStartedClip;
        [SerializeField, Tooltip("The sound to play when the build is complete. Note that this can be overridden in the recipe.")]
        private AudioClip buildCompleteClip;
        [SerializeField, Tooltip("The particle system to play when a pickup is spawned.")]
        ParticleSystem pickupSpawnParticlePrefab;

        public delegate void OnResourcesChanged(float from, float to);
        public event OnResourcesChanged onResourcesChanged;

        private int currentResources = 0;
        private FpsInventorySwappable inventory;
        private bool isBuilding = false;
        private float timeOfNextBuiild = 0;

        private void Start()
        {
            inventory = FpsSoloCharacter.localPlayerCharacter.inventory as FpsInventorySwappable;
        }

        private void Update()
        {
            if (isBuilding || Time.timeSinceLevelLoad < timeOfNextBuiild) return;

            for (int i = 0; i < healthRecipes.Count; i++)
            {
                if (TryRecipe(healthRecipes[i]))
                {
                    return;
                }
            }

            for (int i = 0; i < ammoRecipes.Count; i++)
            {
                if (TryRecipe(ammoRecipes[i]))
                {
                    return;
                }
            }
        }

        private bool TryRecipe(IRecipe recipe)
        {
            if (currentResources >= recipe.Cost && recipe.ShouldBuild)
            {
                StartCoroutine(BuildRecipe(recipe));
                return true;
            }

            return false;
        }

        private IEnumerator BuildRecipe(IRecipe recipe)
        {
            isBuilding = true;
            resources -= recipe.Cost;

            if (recipe.BuildStartedClip != null)
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(recipe.BuildStartedClip, transform.position, 1);
            } else
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(buildStartedClip, transform.position, 1);
            }

            yield return new WaitForSeconds(recipe.TimeToBuild);

            // TODO Use the pool manager to create the item
            GameObject go = Instantiate(recipe.Item.gameObject);
            go.transform.position = transform.position + (transform.forward * 5);
            if (recipe.BuildCompleteClip != null)
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(recipe.BuildCompleteClip, go.transform.position, 1);
            } else
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(buildCompleteClip, go.transform.position, 1);
            }

            // TODO: Use the pool manager to create the particle system
            if (pickupSpawnParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(pickupSpawnParticlePrefab);
                ps.transform.position = go.transform.position;
                ps.Play();
            }
            
            isBuilding = false;
            timeOfNextBuiild = Time.timeSinceLevelLoad + cooldown;
        }

        /// <summary>
        /// The amount of resources the player currently has.
        /// </summary>
        public int resources
        {
            get { return currentResources; }
            set
            {
                if (currentResources == value)
                    return;

                if (onResourcesChanged != null)
                    onResourcesChanged(currentResources, value);

                currentResources = value;
            }
        }
    }
}