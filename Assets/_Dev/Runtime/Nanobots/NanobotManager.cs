using NaughtyAttributes;
using NeoFPS;
using NeoFPS.SinglePlayer;
using System;
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
        [SerializeField, Tooltip("Cooldown between recipe builds.")]
        private float buildingCooldown = 4;
        [SerializeField, Tooltip("How many resources are needed for a level up recipe offer. This will be multiplied by the current level squared. Meaning the higher the level the more resources are required for a reward crate.")]
        private int resourcesRewardMultiplier = 100;
        [SerializeField, Tooltip("The time between recipe offers from the home planet. Once a player has levelled up they will recieve an updated offer until they accept one. This is the freqency at which the offer will be changed.")]
        private float timeBetweenRecipeOffers = 60;
        [SerializeField, Tooltip("The sound to play to indicate a new recipe is available from home planet. This will be played before the name of the recipe to tell the player that they can call it in if they want.")]
        private AudioClip[] recipeRequestPrefix;
        [SerializeField, Tooltip("The sound to play to indicate a recipe has been requested.")]
        private AudioClip[] recipeRequested;
        [SerializeField, Tooltip("The sound to play to indicate a new recipe has been recieved. This will be played before the name of the recipe to tell the player that the recipe has been recieved.")]
        private AudioClip[] recipeRecievedPrefix;

        [Header("Default Feedbacks")]
        [SerializeField, Tooltip("The sound to play when the build is started. Note that this can be overridden in the recipe.")]
        private AudioClip[] buildStartedClips;
        [SerializeField, Tooltip("The sound to play when the build is complete. Note that this can be overridden in the recipe.")]
        private AudioClip[] buildCompleteClips;
        [SerializeField, Tooltip("The default particle system to play when a pickup is spawned. Note that this can be overridden in the recipe."), FormerlySerializedAs("pickupSpawnParticlePrefab")]
        ParticleSystem defaultPickupParticlePrefab;
        [SerializeField, Tooltip("The default audio clip to play when a recipe name is needed, but the recipe does not have a name clip. This should never be used in practice.")]
        AudioClip defaultRecipeName;

        [SerializeField, Tooltip("Turn on debug features for the Nanobot Manager"), Foldout("Debug")]
        bool isDebug = false;

        private List<HealthPickupRecipe> healthRecipes = new List<HealthPickupRecipe>();
        private List<ShieldPickupRecipe> shieldRecipes = new List<ShieldPickupRecipe>();
        private List<WeaponPickupRecipe> weaponRecipes = new List<WeaponPickupRecipe>();
        private List<AmmoPickupRecipe> ammoRecipes = new List<AmmoPickupRecipe>();
        private List<ToolPickupRecipe> toolRecipes = new List<ToolPickupRecipe>();
        private List<GenericItemPickupRecipe> itemRecipes = new List<GenericItemPickupRecipe>();

        internal int nextRewardsLevel = 200;

        public delegate void OnResourcesChanged(float from, float to);
        public event OnResourcesChanged onResourcesChanged;

        private bool isRequesting = false;
        private float timeOfLastRewardOffer = 0;

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
                if (isRequesting == false && timeOfLastRewardOffer + timeBetweenRecipeOffers < Time.timeSinceLevelLoad)
                {
                    if (rewardCoroutine != null)
                    {
                        StopCoroutine(rewardCoroutine);
                    }
                    rewardCoroutine = StartCoroutine(OfferInGameRewardRecipe());
                }
            }

            if (isRequesting) {                 
                return;
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

        IEnumerator OfferInGameRewardRecipe()
        {
            timeOfLastRewardOffer = Time.timeSinceLevelLoad;

            IRecipe offer = RecipeManager.GetOffers(1)[0];
            yield return null;

            // Announce a recipe is available
            AudioClip clip = recipeRequestPrefix[Random.Range(0, recipeRequestPrefix.Length)];
            AudioClip recipeName = offer.NameClip;
            if (recipeName == null)
            {
                recipeName = defaultRecipeName;
                Debug.LogError($"Recipe {offer.DisplayName} (offer) does not have an audio clip for its name. Used default of `Unkown`.");
            }
            yield return Announce(clip, recipeName);

            while (true) // this coroutine will run until the player accepts or a new coroutine is started with a new offer
            {
                // TODO: add this key to the NeoFPS input manager
                if (Input.GetKeyDown(KeyCode.B))
                {
                    nextRewardsLevel = GetRequiredResourcesForNextLevel();

                    isRequesting = true;
                    timeOfNextBuiild = Time.timeSinceLevelLoad + offer.TimeToBuild + 5f;

                    // Announce request made
                    clip = recipeRequested[Random.Range(0, recipeRequested.Length)];
                    if (Time.timeSinceLevelLoad - timeOfLastRewardOffer > 5)
                    {
                        yield return Announce(clip);
                    }
                    else
                    {
                        yield return Announce(clip, recipeName);
                    }

                    yield return new WaitForSeconds(offer.TimeToBuild);

                    // Announce request recieved
                    clip = recipeRecievedPrefix[Random.Range(0, recipeRecievedPrefix.Length)];
                    Announce(clip);
                    yield return new WaitForSeconds(clip.length + 0.1f);
                    if (offer.TimeToBuild > 5)
                    {
                        yield return Announce(clip);
                    }
                    else
                    {
                        yield return Announce(clip, recipeName);
                    }

                    Add(offer, false);

                    isRequesting = false;

                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Make an announcement to the player. This will play the clip at the position of the nanobot manager.
        /// </summary>
        /// <param name="mainClip">The main clip to play</param>
        private IEnumerator Announce(AudioClip mainClip)
        {
            yield return Announce(mainClip, null);
        }

        /// <summary>
        /// Make an announcement to the player. This will play the clip at the position of the nanobot manager.
        /// </summary>
        /// <param name="mainClip">The main clip to play</param>
        /// <param name="recipeName">OPTIONAL: if not null then this recipe name clip will be announced after the main clip</param>
        private IEnumerator Announce(AudioClip mainClip, AudioClip recipeName)
        {
            NeoFpsAudioManager.PlayEffectAudioAtPosition(mainClip, transform.position, 1);
            yield return new WaitForSeconds(mainClip.length);

            if (recipeName == null)
            {
                yield break;
            }

            NeoFpsAudioManager.PlayEffectAudioAtPosition(recipeName, transform.position, 1);

            yield return new WaitForSeconds(recipeName.length);
        }

        private int GetRequiredResourcesForNextLevel()
        {
            int level = RogueLiteManager.runData.currentLevel + 1;
            return RogueLiteManager.persistentData.currentResources + (level * level * resourcesRewardMultiplier);
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
        private Coroutine rewardCoroutine;

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
#if UNITY_EDITOR
            if (isDebug)
            {
                Debug.Log($"Building {recipe.DisplayName}");
            }
#endif
            isBuilding = true;
            resources -= recipe.Cost;

            if (recipe.BuildStartedClip != null)
            {
                yield return Announce(recipe.BuildStartedClip, null);
            } else
            {
                AudioClip recipeName = recipe.NameClip;
                if (recipeName == null)
                {
                    recipeName = defaultRecipeName;
                    Debug.LogError($"Recipe {recipe.DisplayName} ({recipe}) does not have an audio clip for its name. Used default of `Unkown`.");
                }

                yield return Announce(buildStartedClips[Random.Range(0, buildStartedClips.Length)], recipeName);
            }

            yield return new WaitForSeconds(recipe.TimeToBuild);


            IItemRecipe itemRecipe = recipe as IItemRecipe;
            if (itemRecipe != null)
            {
                // TODO Use the pool manager to create the item
                GameObject go = Instantiate(itemRecipe.Item.gameObject);
                go.transform.position = transform.position + (transform.forward * 5) + (transform.up * 1f);

                // TODO: Use the pool manager to create the particle system
                if (recipe.PickupParticles != null)
                {
                    ParticleSystem ps = Instantiate(recipe.PickupParticles, go.transform);
                    ps.Play();
                }
                else if (defaultPickupParticlePrefab != null)
                {
                    ParticleSystem ps = Instantiate(defaultPickupParticlePrefab, go.transform);
                    ps.Play();
                }

                if (recipe.BuildCompleteClip != null)
                {
                    yield return Announce(recipe.BuildCompleteClip, null);
                }
                else
                {
                    //AudioClip recipeName = recipe.NameClip;
                    //if (recipeName == null)
                    //{
                    //    recipeName = defaultRecipeName;
                    //    Debug.LogError($"Recipe {recipe.DisplayName} ({recipe}) does not have an audio clip for its name. Used default of `Unkown`.");
                    //}

                    yield return Announce(buildCompleteClips[Random.Range(0, buildCompleteClips.Length)]);
                }
            }
            else
            {
                Debug.LogError("TODO: handle building recipes of type: " + recipe.GetType().Name);
            }

            recipe.BuildFinished();

            isBuilding = false;
            timeOfNextBuiild = Time.timeSinceLevelLoad + buildingCooldown;
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