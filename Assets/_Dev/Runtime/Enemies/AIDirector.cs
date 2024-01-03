using NaughtyAttributes;
using NeoFPS.SinglePlayer;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Playground
{
    /// <summary>
    /// The AI director is responsible for coordinating AI attacks.
    /// It manages the cycle of the game, ensuring the player gets some moments of respite, as well as some moments of intense action.
    /// </summary>
    public class AIDirector : MonoBehaviour
    {
        [Required, SerializeField, Tooltip("The level generator which will be used to create levels for the game.")]
        LevelGenerator levelGenerator;
        [SerializeField, Tooltip("The maximum time that the AI director will wait between reports of the players location. If no reports are made within this time the AI director will instruct a spawner to create a scannet.")]
        float maximumTimeBetweenReports = 30;
        [Required, SerializeField, Tooltip("The Scanner prefab that will be spawned when the AI director detects that the player has not been reported for a while.")]
        ScannerController scannerPrefab;
        [SerializeField, Tooltip("The length of a time slice. The AI director will evaluate the current state of play every timeSlice seconds. Based on this evluation the AI will issue orders to the Enemies.")]
        float timeSlice = 25f;
        [SerializeField, Tooltip("The target kill rate. When the AI director detects that the current kill rate is below this value it will send more enemies to the player in order to pressure them.")]
        float targetKillRate = 10f;

        List<Spawner> spawners = new List<Spawner>();
        List<BasicEnemyController> enemies = new List<BasicEnemyController>();
        List<Vector3> reportedLocations = new List<Vector3>();
        List<KillReport> killReports = new List<KillReport>();

        float timeOfLastPlayerLocationReport = 0;
        float timeOfNextTimeSlice = 0;
        float currentKillRate = 0; // the current value of the total challenge rating of enemies killed in the last timeSlice

        /// <summary>
        /// Returns the suspected location of the player based on the reports made by the enemies.
        /// </summary>
        public Vector3 suspectedTargetLocation
        {
            get
            {
                if (reportedLocations.Count == 0)
                {
                    return Vector3.zero;
                }

                float x = 0f;
                float y = 0f;
                float z = 0f;

                foreach (Vector3 location in reportedLocations)
                {
                    x += location.x;
                    y += location.y;
                    z += location.z;
                }

                return new Vector3(x / reportedLocations.Count, y / reportedLocations.Count, z / reportedLocations.Count);
            }
        }

        protected void Awake()
        {
            levelGenerator.onSpawnerCreated.AddListener(OnSpawnerCreated);
        }

        private void Update()
        {
            if (FpsSoloCharacter.localPlayerCharacter != null && Time.timeSinceLevelLoad - timeOfLastPlayerLocationReport > maximumTimeBetweenReports)
            {
                Spawner spawner = spawners[Random.Range(0, spawners.Count)];
                spawner.RequestSpawn(scannerPrefab.transform.root.GetComponent<BasicEnemyController>());
            }

            if (Time.timeSinceLevelLoad > timeOfNextTimeSlice)
            {
                timeOfNextTimeSlice = Time.timeSinceLevelLoad + timeSlice;
                float totalChallengeRating = 0.0f;
                for (int i = killReports.Count - 1; i >= 0; i--)
                {
                    if (killReports[i].time < Time.timeSinceLevelLoad - timeSlice)
                    {
                        break;
                    }
                    totalChallengeRating += killReports[i].challengeRating;
                }
                currentKillRate = totalChallengeRating / timeSlice;

                if (enemies.Count > 0 && currentKillRate < targetKillRate)
                {
                    int orderedEnemies = 0;
                    while (totalChallengeRating < targetKillRate * 3 || orderedEnemies == enemies.Count)
                    {
                        orderedEnemies++;
                        BasicEnemyController randomEnemy = enemies[Random.Range(0, enemies.Count)];
                        totalChallengeRating += randomEnemy.challengeRating;
                        randomEnemy.RequestAttack(suspectedTargetLocation);
                    }

                    Debug.Log($"AIDirector: Sent {orderedEnemies} enemies to the player as the current kill rate is {currentKillRate} (target {targetKillRate}).");
                }
            }
        }

        private void OnSpawnerCreated(Spawner spawner)
        {
            spawners.Add(spawner);
            spawner.onDestroyed.AddListener(OnSpawnerDestroyed);
            spawner.onEnemySpawned.AddListener(OnEnemySpawned);
        }

        private void OnSpawnerDestroyed(Spawner spawner)
        {
            spawners.Remove(spawner);
            killReports.Add(new KillReport() { time = Time.timeSinceLevelLoad, challengeRating = spawner.challengeRating, enemyName = spawner.name, location = spawner.transform.position });
        }

        private void OnEnemySpawned(BasicEnemyController enemy)
        {
            enemies.Add(enemy);
            enemy.onDeath.AddListener(OnEnemyDeath);
        }

        private void OnEnemyDeath(BasicEnemyController enemy)
        {
            killReports.Add(new KillReport() { time = Time.timeSinceLevelLoad, challengeRating = enemy.challengeRating, enemyName = enemy.name, location = enemy.transform.position });
            enemies.Remove(enemy);
        }

        internal void ReportPlayerLocation(Vector3 position, float precision)
        {
            timeOfLastPlayerLocationReport = Time.timeSinceLevelLoad;

            if (precision == 0)
            {
                reportedLocations.Clear();
                reportedLocations.Add(position);
            }
            else
            {
                if (reportedLocations.Count >= 3)
                {
                    reportedLocations.RemoveAt(0);
                }
                reportedLocations.Add(position);
            }
        }
    }

    internal struct KillReport
    {
        public float time;
        public float challengeRating;
        public Vector3 location;
        public string enemyName;
    }
}