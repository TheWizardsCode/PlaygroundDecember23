using UnityEngine;
using NeoFPS.SinglePlayer;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using NeoFPS;
using UnityEngine.Events;
using NeoSaveGames.SceneManagement;
using System.Collections;
using NeoFPS.Constants;
using NaughtyAttributes;

namespace Playground
{
    public class PlaygroundDecember23GameMode : FpsSoloGameCustomisable, ISpawnZoneSelector, ILoadoutBuilder
    {
        [Header("Victory")]
        [SerializeField, Tooltip("The amount of time to wait after victory before heading to the hub")]
        float m_VictoryDuration = 5f;

        [Header("Character")]
        [SerializeField, NeoPrefabField(required = true), Tooltip("The player prefab to instantiate if none exists.")]
        private FpsSoloPlayerController m_PlayerPrefab = null;
        [SerializeField, NeoPrefabField(required = true), Tooltip("The character prefab to use.")]
        private FpsSoloCharacter m_CharacterPrefab = null;
        [SerializeField, Tooltip("The recipes that will be available to the player at the start of each run, regardless of resources.")]
        private AbstractRecipe[] _startingRecipes;

        [Header("Level Management")]
        [SerializeField, Tooltip("The level definitions which define the enemies, geometry and more for each level.")]
        LevelDefinition[] levels;

        [SerializeField, Tooltip("Turn on debug mode for this Game Mode"), Foldout("Debug")]
        private bool _isDebug = false;

        private int spawnersRemaining = int.MaxValue;

        public static PlaygroundDecember23GameMode Instance { get; private set; }

        LevelGenerator _levelGenerator;
        internal LevelGenerator levelGenerator
        {
            get
            {
                if (_levelGenerator == null)
                    _levelGenerator = GetComponentInChildren<LevelGenerator>();
                return _levelGenerator;
            }
            private set
            {
                if (_levelGenerator != null && _levelGenerator != value)
                {
                    _levelGenerator.onSpawnerCreated.RemoveListener(OnSpawnerCreated);
                }
                _levelGenerator = value;
            }
        }

        public LevelDefinition currentLevelDefinition
        {
            get { 
                if (levels.Length <= RogueLiteManager.runData.currentLevel)
                    return levels[levels.Length - 1]; 
                else
                    return levels[RogueLiteManager.runData.currentLevel];
            }
        }

        #region Unity Life-cycle
        protected override void Awake()
        {
            levelGenerator = GetComponentInChildren<LevelGenerator>();
            levelGenerator.onSpawnerCreated.AddListener(OnSpawnerCreated);

            base.Awake();
        }

        protected override void OnDestroy()
        {
            levelGenerator.onSpawnerCreated.RemoveListener(OnSpawnerCreated);

            base.OnDestroy();
        }

        private void Update()
        {
            if (FpsSoloCharacter.localPlayerCharacter == null) 
            {
                return;
            }
        }
        #endregion

        #region Game Events

        private Coroutine m_VictoryCoroutine = null;
        private float m_VictoryTimer = 0f;

        public static event UnityAction onVictory;

        internal void OnSpawnerCreated(Spawner spawner)
        {
            spawnersRemaining++;
            spawner.onDestroyed.AddListener(OnSpawnerDestroyed);
        }

        internal void OnSpawnerDestroyed(Spawner spawner)
        {
            spawnersRemaining--;

            if (!_isDebug && spawnersRemaining == 0 && m_VictoryCoroutine == null)
            {
                m_VictoryCoroutine = StartCoroutine(DelayedVictoryCoroutine(m_VictoryDuration));
            }
        }

        protected override void DelayedDeathAction()
        {
            RogueLiteManager.ResetRunData();

            NeoSceneManager.LoadScene(RogueLiteManager.hubScene);
        }

        void DelayedVictoryAction()
        {
            RogueLiteManager.runData.currentLevel++;

            NeoSceneManager.LoadScene(RogueLiteManager.hubScene);
        }

        private IEnumerator DelayedVictoryCoroutine(float delay)
        {
            onVictory?.Invoke();

            // Temporary magnet buff to pull in victory rewards
            MagnetController magnet = FpsSoloCharacter.localPlayerCharacter.GetComponent<MagnetController>();
            float originalRange = 0;
            float originalSpeed = 0;
            if (magnet != null) {
                originalRange = magnet.range;
                originalSpeed = magnet.speed;
                magnet.range = 100;
                magnet.speed = 25;
            }

            yield return null;

            // Delay timer
            m_VictoryTimer = delay;
            while (m_VictoryTimer > 0f && !SkipDelayedDeathReaction())
            {
                m_VictoryTimer -= Time.deltaTime;
                yield return null;
            }

            // Reset magnet
            if (magnet != null)
            {
                magnet.range = originalRange;
                magnet.speed = originalSpeed;
            }

            if (inGame)
                DelayedVictoryAction();
        }

        #endregion

        #region ISpawnZoneSelector IMPLEMENTATION

        [Header("Spawning")]

        [SerializeField, Tooltip("The spawn zones (groups of spawn points) available on this map.")]
        private SpawnZoneSelectorData m_SpawnZones = new SpawnZoneSelectorData();

        private static readonly NeoSerializationKey k_SpawnZoneIndexKey = new NeoSerializationKey("spawnIndex");

        public Sprite mapSprite
        {
            get { return m_SpawnZones.mapSprite; }
        }

        public int numSpawnZones
        {
            get { return m_SpawnZones.spawnZones.Length; }
        }

        public int currentSpawnZoneIndex
        {
            get { return m_SpawnZones.currentIndex; }
            set { m_SpawnZones.currentIndex = value; }
        }

        public ISpawnZoneInfo GetSpawnZoneInfo(int index)
        {
            return m_SpawnZones.spawnZones[index];
        }

        protected override void OnStart()
        {
            base.OnStart();

            if (m_SpawnZones.currentIndex == -1 && m_SpawnZones.spawnZones.Length > 0)
                m_SpawnZones.currentIndex = 0;
        }

        #endregion

        #region ILoadoutBuilder IMPLEMENTATION

        [Header("Loadout Builder")]

        [SerializeField, HideInInspector]
        private LoadoutBuilderData m_LoadoutBuilder = new LoadoutBuilderData();

        public int numLoadoutBuilderSlots
        {
            get { return m_LoadoutBuilder.slots.Length; }
        }

        public ILoadoutBuilderSlot GetLoadoutBuilderSlotInfo(int index)
        {
            return m_LoadoutBuilder.slots[index];
        }

        public FpsInventoryLoadout GetLoadout()
        {
            return m_LoadoutBuilder.GetLoadout();
        }

        public void AddToLoadout(FpsInventoryItemBase item)
        {
            RogueLiteManager.runData.AddToLoadout(item);
        }

        protected override void OnCharacterSpawned(ICharacter character)
        {
            var loadout = m_LoadoutBuilder.GetLoadout();
            if (loadout != null)
                character.GetComponent<IInventory>()?.ApplyLoadout(loadout);

            // Add nanobot recipes
            NanobotManager manager = character.GetComponent<NanobotManager>();
            for (int i = 0; i < RogueLiteManager.runData.Recipes.Count; i++)
            {
                manager.Add(RogueLiteManager.runData.Recipes[i]);
            }

            for (int i = 0; i < RogueLiteManager.persistentData.RecipeIds.Count; i++)
            {
                if (RecipeManager.TryGetRecipeFor(RogueLiteManager.persistentData.RecipeIds[i], out IRecipe recipe))
                {
                    manager.Add(recipe);

                    WeaponPickupRecipe weaponRecipe = recipe as WeaponPickupRecipe;
                    if (weaponRecipe != null)
                    {
                        if (weaponRecipe.ammoRecipe != null)
                        {
                            manager.Add(weaponRecipe.ammoRecipe);
                        }
                    }
                }
            }
        }

        #endregion

        protected override bool PreSpawnStep()
        {
            RogueLiteManager.persistentData.runNumber++;

            if (currentLevelDefinition.generateLevelOnSpawn)
            {
                spawnersRemaining = levelGenerator.Generate(this);
            }

            for (int i = 0; i < _startingRecipes.Length; i++)
            {
                ConfigureRecipe(_startingRecipes[i]);
            }

            for (int i = 0; i < RogueLiteManager.persistentData.RecipeIds.Count; i++)
            {
                ConfigureRecipe(RogueLiteManager.persistentData.RecipeIds[i]);
            }
            
            ConfigureLoadout();

            return base.PreSpawnStep();
        }

        private void ConfigureRecipe(IRecipe recipe)
        {
            RogueLiteManager.persistentData.Add(recipe);
            RogueLiteManager.runData.Add(recipe);

            WeaponPickupRecipe weaponRecipe = recipe as WeaponPickupRecipe;
            if (weaponRecipe != null)
            {
                if (weaponRecipe.pickup == null)
                {
                    Debug.LogError("WeaponPickupRecipe " + weaponRecipe.name + " has no pickup assigned. Not adding this weapon recipe to the loadout.");
                    return;
                }
                RogueLiteManager.runData.AddToLoadout(weaponRecipe.pickup.GetItemPrefab());
            }

            ToolPickupRecipe toolRecipe = recipe as ToolPickupRecipe;
            if (toolRecipe != null)
            {
                if (toolRecipe.pickup == null)
                {
                    Debug.LogError("ToolPickupRecipe " + toolRecipe.name + " has no pickup assigned. Not adding this tool recipe to the loadout.");
                    return;
                }
                RogueLiteManager.runData.AddToLoadout(toolRecipe.pickup.GetItemPrefab());
            }
        }

        private void ConfigureLoadout()
        {
            for (int i = 0; i < RogueLiteManager.runData.Loadout.Count; i++)
            {
                FpsInventoryItemBase item = RogueLiteManager.runData.Loadout[i];
                FpsSwappableCategory category = FpsSwappableCategory.Firearm;

                FpsInventoryQuickUseSwappableItem quickUse = item as FpsInventoryQuickUseSwappableItem;
                if (quickUse != null)
                {
                    category = quickUse.category;
                }
                else
                {
                    FpsInventoryWieldableSwappable wieldable = item as FpsInventoryWieldableSwappable;
                    if (wieldable != null)
                    {
                        category = wieldable.category;
                    }
                }
                m_LoadoutBuilder.slots[category].AddOption(item);
            }
        }

        private void ConfigureRecipe(string recipeId)
        {
            if (RecipeManager.TryGetRecipeFor(recipeId, out IRecipe recipe) == false)
            {
                Debug.LogError($"Attempt to configure a recipe with ID {recipeId} but no such recipe can be found. Ignoring this recipe.");
                return;
            }

            ConfigureRecipe(recipe);
        }

        protected override IController GetPlayerControllerPrototype()
        {
            return m_PlayerPrefab;
        }

        protected override ICharacter GetPlayerCharacterPrototype(IController player)
        {
            return m_CharacterPrefab;
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_SpawnZoneIndexKey, m_SpawnZones.currentIndex);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            int index;
            if (reader.TryReadValue(k_SpawnZoneIndexKey, out index, m_SpawnZones.currentIndex))
                m_SpawnZones.currentIndex = index;
        }
    }
}
