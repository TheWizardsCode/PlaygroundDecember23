using NeoFPS;
using NeoFPS.SinglePlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Playground
{
    public class NanobotManager : MonoBehaviour
    {
        [Header("Building")]
        [SerializeField, Tooltip("Cooldown between recipes.")]
        private float cooldown = 5;
        [SerializeField, Tooltip("How many resources are needed for a reward. This will be multiplied by the current level squared. Meaning the higher the level the more resources are required for a reward crate.")]
        private int _resourcesRewardMultiplier = 200;
        [SerializeField, Tooltip("The prefab to use when generating level up rewards.")]
        private RecipeSelectorUI rewardsPrefab;

        [Header("Default Feedback")]
        [SerializeField, Tooltip("The sound to play when the build is started. Note that this can be overridden in the recipe.")]
        private AudioClip[] buildStartedClips;
        [SerializeField, Tooltip("The sound to play when the build is complete. Note that this can be overridden in the recipe.")]
        private AudioClip[] buildCompleteClips;
        [SerializeField, Tooltip("The default particle system to play when a pickup is spawned. Note that this can be overridden in the recipe."), FormerlySerializedAs("pickupSpawnParticlePrefab")]
        ParticleSystem defaultPickupParticlePrefab;

        private List<HealthPickupRecipe> healthRecipes = new List<HealthPickupRecipe>();
        private List<ShieldPickupRecipe> shieldRecipes = new List<ShieldPickupRecipe>();
        private List<WeaponPickupRecipe> weaponRecipes = new List<WeaponPickupRecipe>();
        private List<AmmoPickupRecipe> ammoRecipes = new List<AmmoPickupRecipe>();
        private List<ToolPickupRecipe> toolRecipes = new List<ToolPickupRecipe>();
        private List<GenericItemPickupRecipe> itemRecipes = new List<GenericItemPickupRecipe>();

        internal int nextRewardsLevel = 200;

        public delegate void OnResourcesChanged(float from, float to);
        public event OnResourcesChanged onResourcesChanged;

        private bool isBuilding = false;
        private float timeOfNextBuiild = 0;

        private void Start()
        {
            nextRewardsLevel = GetRequiredResourcesForNextLevel();

            foreach (var healthRecipe in healthRecipes)
            {
                healthRecipe.Reset();
            }
            foreach (var weaponRecipe in weaponRecipes)
            {
                weaponRecipe.Reset();
            }
            foreach (var ammoRecipe in ammoRecipes)
            {
                ammoRecipe.Reset();
            }
            foreach (var toolRecipe in toolRecipes)
            {
                toolRecipe.Reset();
            }
            foreach (var itemRecipe in itemRecipes)
            {
                itemRecipe.Reset();
            }
        }

        private void Update()
        {
            if (isBuilding || Time.timeSinceLevelLoad < timeOfNextBuiild)
            {
                return;
            }

            // Offer a new recipe if possible
            if (RogueLiteManager.persistentData.currentResources > nextRewardsLevel)
            {
                Transform player = FpsSoloCharacter.localPlayerCharacter.transform;
                Vector3 position = player.position + player.forward * 5 + player.right * 1.5f;
                RecipeSelectorUI rewards = Instantiate(rewardsPrefab, position, Quaternion.identity);

                nextRewardsLevel = GetRequiredResourcesForNextLevel();
            }

            // Prioritize building ammo if the player is low on ammo
            if (TryAmmoRecipes(0.1f))
            {
                return;
            }

            // Health is the next priority, got to stay alive
            if (TryHealthRecipes())
            {
                return;
            }

            // Prioritize building ammo if the player is low on ammo
            if (TryShieldRecipes())
            {
                return;
            }

            // If we can afford a powerup, build it
            if (TryPowerUpRecipes()) {
                return;
            }

            // If we can't afford a powerup, build ammo up to near mazimum (not maximum because there will often be half used clips lying around)
            if (TryAmmoRecipes(0.9f))
            {
                return;
            }

            // If we are in good shape then see if there is a generic item we can build
            if (TryItemRecipes())
            {
                return;
            }
        }



        private int GetRequiredResourcesForNextLevel()
        {
            int level = RogueLiteManager.runData.currentLevel + 1;
            return RogueLiteManager.persistentData.currentResources + (level * level * _resourcesRewardMultiplier);
        }

        // TODO: we can probably generalize these Try* methods now that we have refactored the recipes to use interfaces/Abstract classes
        private bool TryHealthRecipes()
        {
            HealthPickupRecipe chosenRecipe = null;
            float chosenOverage = int.MaxValue;
            for (int i = 0; i < healthRecipes.Count; i++)
            {
                if (RogueLiteManager.persistentData.currentResources >= healthRecipes[i].Cost && healthRecipes[i].ShouldBuild)
                {
                    float overage = healthRecipes[i].Overage;
                    if (chosenOverage > overage)
                    {
                        chosenRecipe = healthRecipes[i];
                        chosenOverage = overage;
                    }
                }
            }

            if (chosenRecipe != null)
            {
                StartCoroutine(BuildRecipe(chosenRecipe));
                return true;
            }

            return false;
        }

        private bool TryShieldRecipes()
        {
            for (int i = 0; i < shieldRecipes.Count; i++)
            {
                if (TryRecipe(shieldRecipes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPowerUpRecipes()
        {
            for (int i = 0; i < weaponRecipes.Count; i++)
            {
                if (TryRecipe(weaponRecipes[i]))
                {
                    weaponRecipes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        float earliestTimeOfNextItemSpawn = 0;
        private bool TryItemRecipes()
        {
            if (earliestTimeOfNextItemSpawn > Time.timeSinceLevelLoad)
            {
                return false;
            }

            float approximateFrequency = 1000;
            for (int i = 0; i < itemRecipes.Count; i++)
            {
                // TODO: make a decision on whether to make a generic item in a more intelligent way
                // TODO: can we make tests that are dependent on the pickup, e.g. when the pickup is triggered it will only be picked up if needed 
                if (RogueLiteManager.persistentData.currentResources < itemRecipes[i].Cost)
                {
                    continue;
                }

                if (TryRecipe(itemRecipes[i]))
                {
                    earliestTimeOfNextItemSpawn = Time.timeSinceLevelLoad + (approximateFrequency * Random.Range(0.7f, 1.3f));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If the ammo available for the currently equipped weapon is below the minimum level, try to build ammo.
        /// </summary>
        /// <param name="minimumAmmoAmount">The % (0-1) of ammo that is the minimum required</param>
        /// <returns></returns>
        private bool TryAmmoRecipes(float minimumAmmoAmount)
        {
            for (int i = 0; i < ammoRecipes.Count; i++)
            {
                if (!ammoRecipes[i].HasAmount(minimumAmmoAmount))
                {
                    return TryRecipe(ammoRecipes[i]);
                }
            }

            return false;
        }

        private bool TryRecipe(IRecipe recipe)
        {
            if (RogueLiteManager.persistentData.currentResources >= recipe.Cost && recipe.ShouldBuild)
            {
                StartCoroutine(BuildRecipe(recipe));
                return true;
            }

            return false;
        }

        internal IEnumerator BuildRecipe(IRecipe recipe)
        {
            Debug.Log($"Building {recipe.DisplayName}");
            isBuilding = true;
            resources -= recipe.Cost;

            if (recipe.BuildStartedClip != null)
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(recipe.BuildStartedClip, transform.position, 1);
            } else
            {
                NeoFpsAudioManager.PlayEffectAudioAtPosition(buildStartedClips[Random.Range(0, buildStartedClips.Length)], transform.position, 1);
            }

            yield return new WaitForSeconds(recipe.TimeToBuild);


            IItemRecipe itemRecipe = recipe as IItemRecipe;
            if (itemRecipe != null)
            {
                // TODO Use the pool manager to create the item
                GameObject go = Instantiate(itemRecipe.Item.gameObject);
                go.transform.position = transform.position + (transform.forward * 5) + (transform.up * 0.5f);

                if (recipe.BuildCompleteClip != null)
                {
                    NeoFpsAudioManager.PlayEffectAudioAtPosition(recipe.BuildCompleteClip, go.transform.position, 1);
                }
                else
                {
                    NeoFpsAudioManager.PlayEffectAudioAtPosition(buildCompleteClips[Random.Range(0, buildCompleteClips.Length)], go.transform.position, 1);
                }

                // TODO: Use the pool manager to create the particle system
                if (recipe.PickupParticles != null)
                {
                    ParticleSystem ps = Instantiate(recipe.PickupParticles, go.transform);
                    ps.Play();
                } else if (defaultPickupParticlePrefab != null)
                {
                    ParticleSystem ps = Instantiate(defaultPickupParticlePrefab, go.transform);
                    ps.Play();
                }
            }
            else
            {
                Debug.LogError("TODO: handle building recipes of type: " + recipe.GetType().Name);
            }

            recipe.BuildFinished();

            isBuilding = false;
            timeOfNextBuiild = Time.timeSinceLevelLoad + cooldown;
        }

        /// <summary>
        /// Adds the recipe to the list of starting recipes.
        /// </summary>
        /// <param name="recipe">The recipe to add.</param>
        /// <param name="isPermanent">If set to <c>true</c> the recipe will be added to the list of permanent recipes, otherwise it will be added only to the current runs recipes..</param>
        internal void Add(IRecipe recipe, bool isPermanent = false)
        {
            if (recipe == null)
            {
                Debug.LogError("Attempting to add a null recipe to the NanobotManager.");
                return;
            }

            RogueLiteManager.runData.Add(recipe);
            if (isPermanent)
            {
                RogueLiteManager.persistentData.Add(recipe);
            }

            AmmoPickupRecipe ammo = recipe as AmmoPickupRecipe;
            if (ammo != null)
            {
                if (!ammoRecipes.Contains(ammo))
                {
                    ammoRecipes.Add(recipe as AmmoPickupRecipe);
                }
                return;
            }

            HealthPickupRecipe health = recipe as HealthPickupRecipe;
            if (health != null)
            {
                if (!healthRecipes.Contains(health))
                {
                    healthRecipes.Add(recipe as HealthPickupRecipe);
                }
                return;
            }

            WeaponPickupRecipe weapon = recipe as WeaponPickupRecipe;
            if (weapon != null)
            {
                if (!weaponRecipes.Contains(weapon))
                {
                    weaponRecipes.Add(recipe as WeaponPickupRecipe);
                    ShuffleWeaponRecipes();
                }
                return;
            }

            ToolPickupRecipe tool = recipe as ToolPickupRecipe;
            if (tool != null)
            {
                if (!toolRecipes.Contains(tool))
                {
                    toolRecipes.Add(recipe as ToolPickupRecipe);
                    ShuffleToolRecipes();
                }
                return;
            }

            ShieldPickupRecipe shield = recipe as ShieldPickupRecipe;
            if (shield != null)
            {
                if (!shieldRecipes.Contains(shield))
                {
                    shieldRecipes.Add(shield);
                    ShuffleItemRecipes();
                }
                return;
            }

            GenericItemPickupRecipe item = recipe as GenericItemPickupRecipe;
            if (item != null)
            {
                if (!itemRecipes.Contains(item))
                {
                    itemRecipes.Add(item);
                    ShuffleItemRecipes();
                }
                return;
            }
            StatRecipe statRecipe = recipe as StatRecipe;
            if (statRecipe != null)
            {
                statRecipe.Apply();
                return;
            }

            Debug.LogError("Unknown recipe type: " + recipe.GetType().Name);
        }

        /// <summary>
        /// The amount of resources the player currently has.
        /// </summary>
        public int resources
        {
            get { return RogueLiteManager.persistentData.currentResources; }
            set
            {
                if (RogueLiteManager.persistentData.currentResources == value)
                    return;

                if (onResourcesChanged != null)
                    onResourcesChanged(RogueLiteManager.persistentData.currentResources, value);

                RogueLiteManager.persistentData.currentResources = value;
            }
        }

        private void ShuffleWeaponRecipes()
        {
            int n = weaponRecipes.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                WeaponPickupRecipe value = weaponRecipes[k];
                weaponRecipes[k] = weaponRecipes[n];
                weaponRecipes[n] = value;
            }
        }

        private void ShuffleToolRecipes()
        {
            int n = toolRecipes.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                ToolPickupRecipe value = toolRecipes[k];
                toolRecipes[k] = toolRecipes[n];
                toolRecipes[n] = value;
            }
        }

        private void ShuffleItemRecipes()
        {
            int n = itemRecipes.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                GenericItemPickupRecipe value = itemRecipes[k];
                itemRecipes[k] = itemRecipes[n];
                itemRecipes[n] = value;
            }
        }
    }
}