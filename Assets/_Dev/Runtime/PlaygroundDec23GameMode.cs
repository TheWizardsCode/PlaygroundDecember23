using UnityEngine;
using NeoFPS.SinglePlayer;
using System;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using Playground;
using NeoFPS;
using UnityEngine.Events;

namespace Playground
{
    public class PlaygroundDecember23GameMode : FpsSoloGameCustomisable, ISpawnZoneSelector, ILoadoutBuilder
    {
        [Header("Character")]
        [SerializeField, NeoPrefabField(required = true), Tooltip("The player prefab to instantiate if none exists.")]
        private FpsSoloPlayerController m_PlayerPrefab = null;
        [SerializeField, NeoPrefabField(required = true), Tooltip("The character prefab to use.")]
        private FpsSoloCharacter m_CharacterPrefab = null;

        [Header("Level Generation")]
        [SerializeField, Tooltip("Should a level be auto generated on start?")]
        bool generateLevelOnSpawn = true;

        LevelGenerator levelGenerator;
        private int spawnersRemaining = int.MaxValue;

        #region Unity Life-cycle
        protected override void Awake()
        {
            levelGenerator = GetComponentInChildren<LevelGenerator>();
            base.Awake();
        }

        private void Update()
        {
            if (spawnersRemaining == 0)
            {
                PreSpawnStep();
            }
        }
        #endregion

        #region Game Events
        internal void SpawnerDestroyed()
        {
            spawnersRemaining--;
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

        [SerializeField, Tooltip("The loadouts that are available to use.")]
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

        protected override void OnCharacterSpawned(ICharacter character)
        {
            // Apply inventory loadout
            var loadout = m_LoadoutBuilder.GetLoadout();
            if (loadout != null)
                character.GetComponent<IInventory>()?.ApplyLoadout(loadout);
        }

        #endregion

        protected override bool PreSpawnStep()
        {
            if (generateLevelOnSpawn)
            {
                spawnersRemaining = levelGenerator.Generate(this);
            }
            return base.PreSpawnStep();
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
