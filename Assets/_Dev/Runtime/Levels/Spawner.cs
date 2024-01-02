using NeoFPS;
using NeoFPS.SinglePlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Playground
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The level of this spawner.")]
        internal int challengeRating = 3;

        [Header("Spawn Behaviours")]
        [SerializeField, Tooltip("Distance to player for this spawner to be activated. If this is set to 0 then it will always be active, if >0 then the spawner will only be active when the player is within this many units. If the player moves further away then the spawner will pause.")]
        float activeRange = 0;
        [SerializeField, Tooltip("The radius around the spawner to spawn enemies.")]
        internal float spawnRadius = 5f;
        [SerializeField, Tooltip("If true, all enemies spawned by this spawner will be destroyed when this spawner is destroyed.")]
        internal bool destroySpawnsOnDeath = true;
        [SerializeField, Tooltip("The time to wait betweeen waves, in seconds.")]
        private float timeBetweenWaves = 10f;
        [SerializeField, Tooltip("The waves this spawner should spawn. If this is set to null then it is assumed that the waves will be set by a level manager.")]
        private WaveDefinition[] waves;
        [SerializeField, Tooltip("If no more wave definitions are available should this spawner generate new waves of increasing difficulty? This value may be overriden by a level manager.")]
        private bool generateWaves = false;

        [Header("Juice")]
        [SerializeField, Tooltip("The particle system to play when the spawner is destroyed.")]
        internal ParticleSystem deathParticlePrefab;

        [Header("Shield")]
        [SerializeField, Tooltip("Should this spawner generate a shield to protect itself?")]
        bool hasShield = true;
        [SerializeField, Tooltip("Shield generators are models that will orbit the spawner and create a shield. The player must destroy the generators to destroy the shield and thus get t othe spawner.")]
        internal BasicHealthManager shieldGenerator;
        [SerializeField, Tooltip("The particle system to play when a shield generator is destroyed.")]
        internal ParticleSystem shieldGenParticlePrefab;
        [SerializeField, Tooltip("How many generators this shield should have.")]
        internal int numShieldGenerators = 3;
        [SerializeField, Tooltip("The speed of the shield generators, in revolutions per minute.")]
        internal float shieldGeneratorRPM = 45f;
        [SerializeField, Tooltip("The model and collider that will represent the shield.")]
        internal Collider shieldCollider;

        [Header("Events")]
        [SerializeField, Tooltip("The event to trigger when this spawner is destroyed.")]
        public UnityEvent<Spawner> onDestroyed;
        [SerializeField, Tooltip("The event to trigger when an enemy is spawned.")]
        public UnityEvent<BasicEnemyController> onEnemySpawned;
        [SerializeField, Tooltip("The event to trigger when all waves are complete.")]
        public UnityEvent onAllWavesComplete;

        List<BasicEnemyController> spawnedEnemies = new List<BasicEnemyController>();
        List<ShieldGenerator> shieldGenerators = new List<ShieldGenerator>();

        private class ShieldGenerator
        {
            public BasicHealthManager healthManager;
            public GameObject gameObject;
            public Transform transform;

            public ShieldGenerator (BasicHealthManager h)
            {
                healthManager = h;
                gameObject = h.gameObject;
                transform = h.transform;
                h.onIsAliveChanged += OnIsAliveChanged;
            }

            public void OnIsAliveChanged(bool alive)
            {
                gameObject.SetActive(alive);
            }

            public void Respawn(float health)
            {
                healthManager.healthMax = health;
                healthManager.health = health;
                // TODO: Shader
            }
        }

        private int currentWaveIndex = -1;
        private WaveDefinition currentWave;


        private int livingShieldGenerators = 0;
        private float activeRangeSqr;

        private void Update()
        {
            if (hasShield == false)
            {
                return;
            }

            var rotation = new Vector3(0f, shieldGeneratorRPM * Time.deltaTime, 0f);
            for (int i = 0; i < numShieldGenerators; i++)
                shieldGenerators[i].transform.Rotate(rotation, Space.Self);
        }

        private void Start()
        {
            transform.localScale = new Vector3(spawnRadius * 2, spawnRadius * 2, spawnRadius * 2);

            StartCoroutine(SpawnWaves());

            if (hasShield && shieldGenerator != null)
            {
                RegisterShieldGenerator(shieldGenerator);
                for (int i = 1; i < numShieldGenerators; ++i)
                {
                    var duplicate = Instantiate(shieldGenerator, shieldGenerator.transform.parent);
                    duplicate.transform.localRotation = Quaternion.Euler(0f, 360f * i / numShieldGenerators, 0f);
                    RegisterShieldGenerator(duplicate);

                    //var lineRenderer = duplicate.GetComponentInChildren<LineRenderer>();
                    //if (lineRenderer != null)
                    //    lineRenderer.widthMultiplier = spawnRadius * 2;
                }

                livingShieldGenerators = numShieldGenerators;
            }

            activeRangeSqr = activeRange * activeRange;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            onDestroyed?.Invoke(this);
        }

        void RegisterShieldGenerator(BasicHealthManager h)
        {
            var sg = new ShieldGenerator(h);
            shieldGenerators.Add(sg);
            h.onIsAliveChanged += OnShieldGeneratorIsAliveChanged;
        }

        private void OnShieldGeneratorIsAliveChanged(bool alive)
        {
            // TODO: Need to detect which shield gen and disable it, spawn particles, etc

            int oldLivingShieldGenerators = livingShieldGenerators;

            if (alive)
                ++livingShieldGenerators;
            else
                --livingShieldGenerators;

            if (shieldCollider != null)
            {
                if (livingShieldGenerators == 0 && oldLivingShieldGenerators != 0)
                {
                    // Disable the shield
                    shieldCollider.enabled = false;
                    shieldCollider.gameObject.SetActive(false); // TODO: Shader
                }
                else
                {
                    if (livingShieldGenerators != 0 && oldLivingShieldGenerators == 0)
                    {
                        // Enable the shield
                        shieldCollider.enabled = true;
                        shieldCollider.gameObject.SetActive(true); // TODO: Shader
                    }
                }
            }
        }

        private void NextWave()
        {
            currentWaveIndex++;
            if (currentWaveIndex >= waves.Length)
            {
                onAllWavesComplete?.Invoke();
                if (!generateWaves)
                {
                    Debug.LogWarning("No more waves to spawn.");
                    currentWave = null;
                    StopCoroutine(SpawnWaves());
                    return;
                }
                currentWave = GenerateNewWave();
                return;
            }
            //Debug.Log($"Starting wave {currentWaveIndex + 1} of {waves.Length}...");
            currentWave = waves[currentWaveIndex];
            currentWave.Reset();
        }

        private WaveDefinition GenerateNewWave()
        {
            WaveDefinition newWave = ScriptableObject.CreateInstance<WaveDefinition>();
            // populate newWave with random values based loosely on the previous wave
            var lastWave = currentWave;
            if (lastWave == null)
            {
                lastWave = waves[waves.Length - 1];
            }
            // collect all enemy prefabs from all waves and then randomly select from them
            List<BasicEnemyController> enemyPrefabs = new List<BasicEnemyController>();
            for (int i = 0; i < waves.Length; i++)
            {
                enemyPrefabs.AddRange(waves[i].EnemyPrefabs);
            }
            newWave.Init(
                enemyPrefabs.ToArray(),
                Mathf.Max(lastWave.SpawnRate - 0.1f, 0.1f), // faster!
                lastWave.WaveDuration + Random.Range(1f, 5f), // longer!
                WaveDefinition.SpawnOrder.Random
            );
            return newWave;
        }

        private IEnumerator SpawnWaves()
        {
            while (FpsSoloCharacter.localPlayerCharacter == null)
            {
                yield return new WaitForSeconds(0.5f);
            }


            NextWave();
            while (currentWave != null)
            {
                float waveStart = Time.time;
                while (Time.time - waveStart < currentWave.WaveDuration)
                {
                    yield return new WaitForSeconds(currentWave.SpawnRate);

                    if (activeRange == 0 || Vector3.SqrMagnitude(FpsSoloCharacter.localPlayerCharacter.transform.position - transform.position) <= activeRangeSqr)
                    {
                        // TODO: rather than spawn all of them at once spread this out over the SpawnRate
                        for (int i = 0; i < currentWave.SpawnAmount; i++)
                            SpawnEnemy();
                    }
                }

                yield return new WaitForSeconds(timeBetweenWaves);
                NextWave();
            }
        }

        private void SpawnEnemy()
        {
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            var prefab = currentWave.GetNextEnemy();
            if (prefab == null)
            {
                Debug.LogError("No enemy prefab found in wave definition.");
                return;
            }
            BasicEnemyController enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(enemy);

            onEnemySpawned?.Invoke(enemy);
        }

        public void OnAliveIsChanged(bool isAlive)
        {
            if (isAlive)
            {
                StartCoroutine(SpawnWaves());
            }
            else
            {
                Renderer parentRenderer = GetComponentInChildren<Renderer>();

                // TODO use pool for particles
                if (deathParticlePrefab != null)
                {
                    ParticleSystem deathParticle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
                    if (parentRenderer != null)
                    {
                        var particleSystemRenderer = deathParticle.GetComponent<ParticleSystemRenderer>();
                        if (particleSystemRenderer != null)
                        {
                            particleSystemRenderer.material = parentRenderer.material;
                        }
                    }
                    deathParticle.Play();
                }

                if (destroySpawnsOnDeath)
                {
                    for (int i = 0; i < spawnedEnemies.Count; i++)
                    {
                        if (spawnedEnemies[i] != null)
                        {
                            spawnedEnemies[i].OnAliveIsChanged(false);
                        }
                    }
                }

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Configure this spawner accoring to a level definition.
        /// </summary>
        /// <param name="currentLevelDefinition"></param>
        internal void Initialize(LevelDefinition currentLevelDefinition)
        {
            waves = currentLevelDefinition.Waves;
            timeBetweenWaves  = currentLevelDefinition.WaveWait;
            generateWaves = currentLevelDefinition.GenerateNewWaves;
        }

        /// <summary>
        /// The spawner will attempt to spawn the given scanner prefab.
        /// </summary>
        /// <param name="scannerPrefab"></param>
        internal void RequestSpawn(BasicEnemyController enemyPrefab)
        {
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            BasicEnemyController enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(enemy);

            onEnemySpawned?.Invoke(enemy);
        }
    }
}