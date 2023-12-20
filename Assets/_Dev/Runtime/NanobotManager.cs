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

        public delegate void OnResourcesChanged(float from, float to);
        public event OnResourcesChanged onResourcesChanged;

        private int currentResources = 0;
        private FpsInventorySwappable inventory;
        private bool isBuilding = false;

        private void Start()
        {
            inventory = FpsSoloCharacter.localPlayerCharacter.inventory as FpsInventorySwappable;
        }

        private void Update()
        {
            if (isBuilding) return;

            for (int i = 0; i < healthRecipes.Count; i++)
            {
                if (TryRecipe(healthRecipes[i]))
                {
                    break;
                }
            }

            for (int i = 0; i < ammoRecipes.Count; i++)
            {
                if (TryRecipe(ammoRecipes[i]))
                {
                    break;
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

            yield return new WaitForSeconds(recipe.TimeToBuild);

            // TODO Use the pool manager to create the item
            GameObject go = Instantiate(recipe.Item.gameObject);
            go.transform.position = transform.position + recipe.SpawnOffset;
            if (recipe.BuildCompleteClip != null)
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(recipe.BuildCompleteClip, go.transform.position, 1);
            }
            isBuilding = false;
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