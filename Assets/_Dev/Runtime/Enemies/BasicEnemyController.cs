using NaughtyAttributes;
using NeoFPS;
using NeoFPS.SinglePlayer;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Playground
{
    public class BasicEnemyController : MonoBehaviour
    {
        [SerializeField, Tooltip("The name of this enemy as displayed in the UI.")]
        protected string displayName = "TBD";
        [SerializeField, TextArea, Tooltip("The description of this enemy as displayed in the UI.")]
        protected string description = "TBD";
        [SerializeField, Tooltip("The level of this enemy. Higher level enemies will be more difficult to defeat.")]
        internal int challengeRating = 1;

        [SerializeField, Tooltip("The Enemy behaviour definition defines how this enemy will behave. The best way to start is to drag in an existing configuration and then save a copy using the button below. Then edit for your needs."), Expandable]
        [Required("A configuration must be provided. This forms the base definition of the enemy. Higher level enemies will be generated from this base definition.")]
        internal EnemyBehaviourDefinition config = null;

        [SerializeField, Tooltip("The source of the sensor array for this enemy. Note this must be inside the enemies collider."), Foldout("References")]
        Transform sensor;

        [SerializeField, Tooltip("The event to trigger when this enemy dies."), Foldout("Events")]
        public UnityEvent<BasicEnemyController> onDeath;
        [SerializeField, Tooltip("The event to trigger when this enemy is destroyed."), Foldout("Events")]
        public UnityEvent onDestroyed;

        [SerializeField, Tooltip("Enable debuggging for this enemy."), Foldout("Debug")]
        bool isDebug;

        Transform _target;
        internal Transform Target
        {
            get
            {
                if (_target == null && FpsSoloCharacter.localPlayerCharacter != null)
                    _target = FpsSoloCharacter.localPlayerCharacter.transform;
                return _target;
            }
        }
        internal virtual bool shouldAttack
        {
            get
            { 
                if (config.requireLineOfSight && CanSeeTarget == false)
                {
                    return false;
                }

                return true;
            }
        }

        internal bool CanSeeTarget
        {
            get
            {
                if (Target == null)
                    return false;

                if (Vector3.Distance(Target.position, transform.position) <= config.viewDistance)
                {
                    Vector3 rayTargetPosition = Target.position;
                    rayTargetPosition.y = Target.position.y + 0.8f; // TODO: Should use the seek targets

                    Vector3 targetVector = rayTargetPosition - sensor.position;

                    Ray ray = new Ray(sensor.position, targetVector);
#if UNITY_EDITOR
                    if (isDebug)
                    {
                        Debug.DrawRay(sensor.position, targetVector, Color.red);
                    }
#endif
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, config.viewDistance, config.sensorMask))
                    {
                        if (hit.transform == Target)
                        {
                            goalDestination = GetDestination(Target.position);
                            timeOfNextWanderPositionChange = Time.timeSinceLevelLoad + config.seekDuration;
                            return true;
                        }
#if UNITY_EDITOR
                        else if (isDebug)
                        {
                            Debug.Log($"{name} couldn't see the player, sightline blocked by {hit.collider} on {hit.transform.root} at {hit.point}.");
                        }
#endif
                    }
#if UNITY_EDITOR
                    else if (isDebug)
                    {
                        Debug.Log($"{name} couldn't see the player, the raycast hit nothing.");
                    }
#endif
                }
#if UNITY_EDITOR
                else if (isDebug)
                {
                    Debug.Log($"{name} cannot see the player as they are further than {config.viewDistance}m away.");
                }
#endif

                return false;
            }
        }

        private void OnDestroy()
        {
            onDestroyed?.Invoke();

            onDestroyed.RemoveAllListeners();
        }

        /// <summary>
        /// Enemies should not go to exactly where the player is but rather somewhere that places them at an
        /// optimal position. This method will return such a position.
        /// </summary>
        /// <param name="targetPosition">The current position of the target.</param>
        /// <returns>A position near the player that places the enemy at an optimal position to attack from.</returns>
        public Vector3 GetDestination(Vector3 targetPosition)
        {
            Vector3 newPosition = new Vector3();
            for (int i = 0; i < 4; i++)
            {
                newPosition = UnityEngine.Random.onUnitSphere * config.optimalDistanceFromPlayer;
                newPosition += targetPosition;
                if (newPosition.y >= config.minimumHeight)
                {
                    return newPosition;
                }
            }
            newPosition.y = Mathf.Max(newPosition.y, config.minimumHeight);
            return newPosition;
        }

        Vector3 spawnPosition = Vector3.zero;
        internal Vector3 goalDestination = Vector3.zero;
        Vector3 wanderDestination = Vector3.zero;
        float timeOfNextWanderPositionChange = 0;

        private void Awake()
        {
#if ! UNITY_EDITOR
            isDebug = false;
#endif
        }

        private void Start()
        {
            spawnPosition = transform.position;
        }

        protected virtual void Update()
        {
            if (config.isMobile == false)
            {
                return;
            }

            if (Target == null)
            {
                if (config.shouldWander)
                {
                    Wander();
                }
                return;
            }

            if (config.requireLineOfSight == false)
            {
                goalDestination = GetDestination(Target.position);
            }

            MoveAwayIfTooClose();

            if (config.requireLineOfSight && CanSeeTarget == false)
            {
                if (config.shouldWander && Time.timeSinceLevelLoad > timeOfNextWanderPositionChange)
                {
                    Wander();
                }
                else
                {
                    MoveTowards(goalDestination);
                }

                return;
            }
            else
            {
                MoveTowards(goalDestination);
            }
        }

        protected void MoveAwayIfTooClose()
        {
            float currentDistance = Vector3.Distance(transform.position, goalDestination);
            if (currentDistance < config.optimalDistanceFromPlayer)
            {
                float distanceToMoveAway = config.optimalDistanceFromPlayer - currentDistance;
                goalDestination = transform.position - (transform.forward * distanceToMoveAway);
            }
        }

        internal virtual void MoveTowards(Vector3 destination, float speedMultiplier = 1)
        {
            Vector3 directionToDestination = (destination - transform.position).normalized;
            
            if (destination.y < config.minimumHeight)
            {
                destination.y = config.minimumHeight;
            } else if (destination.y > config.maximumHeight)
            {
                destination.y = config.maximumHeight;
            }

            float turnAngle = Vector3.Angle(transform.forward, directionToDestination);

            float distanceToObstacle = 0;
            bool rotateRight = true;

            Ray ray = new Ray(sensor.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit forwardHit, config.obstacleAvoidanceDistance, config.sensorMask))
            {
                if (forwardHit.collider.transform.root != Target)
                {
                    distanceToObstacle = forwardHit.distance;
                }
            }

            ray.direction = Quaternion.AngleAxis(turnAngle, transform.transform.up) * transform.forward;
            if (Physics.Raycast(ray, out RaycastHit rightForwardHit, config.obstacleAvoidanceDistance, config.sensorMask))
            {
                if (rightForwardHit.collider.transform.root != Target)
                {
                    distanceToObstacle = rightForwardHit.distance;
                    rotateRight = false;
                }
            }

            ray.direction = Quaternion.AngleAxis(-turnAngle, transform.transform.up) * transform.forward;
            if (Physics.Raycast(ray, out RaycastHit leftForwardHit, config.obstacleAvoidanceDistance, config.sensorMask))
            {
                if (leftForwardHit.collider.transform.root != Target)
                {
                    distanceToObstacle = leftForwardHit.distance;
                }
            }

            Quaternion targetRotation = Quaternion.identity;
            if (distanceToObstacle > 0) // turn to avoid obstacle
            {
                if (rotateRight)
                {
                    targetRotation = transform.rotation * Quaternion.Euler(0, turnAngle * (distanceToObstacle / config.obstacleAvoidanceDistance), 0);
                }
                else
                {
                    targetRotation = transform.rotation * Quaternion.Euler(0, -turnAngle * (distanceToObstacle / config.obstacleAvoidanceDistance), 0);
                }
                //Debug.Log($"Rotating to avoid obstacle F {forwardHit.collider}, L {leftForwardHit.collider}, R {rightForwardHit.collider}");
            }
            else // no obstacle so rotate towards target
            {
                Vector3 directionToTarget = destination - transform.position;
                directionToTarget.y = 0;
                if (directionToTarget.magnitude > 0.5f)
                {
                    targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, config.rotationSpeed * Time.deltaTime);
            transform.position += transform.forward * config.speed * speedMultiplier * Time.deltaTime;

            AdjustHeight(destination, speedMultiplier);
        }

        private void AdjustHeight(Vector3 destination, float speedMultiplier)
        {
            float heightDifference = transform.position.y - destination.y;
            if ( heightDifference > 0.2f || heightDifference < -0.2f)
            {
                float rate = config.speed * speedMultiplier * Time.deltaTime;
                if (destination.y > transform.position.y)
                {
                    transform.position += Vector3.up * rate;
                }
                else
                {
                    transform.position -= Vector3.up * rate;
                }

            }
        }

        protected void Wander()
        {
            if (Time.timeSinceLevelLoad > timeOfNextWanderPositionChange)
            {
                timeOfNextWanderPositionChange = Time.timeSinceLevelLoad + Random.Range(10f, config.maxWanderRange);
                wanderDestination = spawnPosition + Random.insideUnitSphere * config.maxWanderRange;
                wanderDestination.y = Mathf.Clamp(wanderDestination.y, 1, config.maxWanderRange);
                goalDestination = wanderDestination;

#if UNITY_EDITOR
                if (isDebug)
                    Debug.LogWarning($"{name} updating wander position to {goalDestination}");
#endif

            }

            MoveTowards(wanderDestination);
        }

        public void OnAliveIsChanged(bool isAlive)
        {
            if (!isAlive)
                Die();
        }

        private void Die()
        {
            Renderer parentRenderer = GetComponentInChildren<Renderer>();

            // TODO use pool for particles
            if (config.deathParticlePrefab != null)
            {
                ParticleSystem deathParticle = Instantiate(config.deathParticlePrefab, transform.position, Quaternion.identity);
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

            // Drop resources
            if (UnityEngine.Random.value <= config.resourcesDropChance)
            {
                Vector3 pos = transform.position;
                pos.y = 0;
                ResourcesPickup resources = Instantiate(config.resourcesPrefab, pos, Quaternion.identity);
                if (parentRenderer != null)
                {
                    var resourcesRenderer = resources.GetComponentInChildren<Renderer>();
                    if (resourcesRenderer != null)
                    {
                        resourcesRenderer.material = parentRenderer.material;
                    }
                }
            }

            onDeath?.Invoke(this);

            Destroy(gameObject);
        }

        /// <summary>
        /// The Enemy is requested to move to and attack the location provided. 
        /// The enemy will move to a point near the location and attack if it sees a target on the way.
        /// </summary>
        /// <param name="position"></param>
        internal void RequestAttack(Vector3 position)
        {
            goalDestination = GetDestination(position);
            timeOfNextWanderPositionChange = Time.timeSinceLevelLoad + config.seekDuration;
            //Debug.Log($"{name} has been requested to attack {position}.");
        }

#if UNITY_EDITOR
        #region Inspector
        [Button]
        private void SaveCopyOfConfig()
        {
            string defaultPath = AssetDatabase.GetAssetPath(config);
            string directoryPath = Path.GetDirectoryName(defaultPath);

            string path = EditorUtility.SaveFilePanel(
                "Save Enemy Behaviour Definition",
                directoryPath,
                $"{transform.root.name} Enemy Behaviour Definition",
                "asset"
            );

            if (path.Length != 0)
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

                EnemyBehaviourDefinition newConfig = ScriptableObject.CreateInstance<EnemyBehaviourDefinition>();

                FieldInfo[] fields = newConfig.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    if (field.IsPublic && !Attribute.IsDefined(field, typeof(System.NonSerializedAttribute)) ||
                        Attribute.IsDefined(field, typeof(SerializeField)))
                    {
                        field.SetValue(newConfig, field.GetValue(config));
                    }
                }

                AssetDatabase.CreateAsset(newConfig, relativePath);
                config = newConfig;
                AssetDatabase.SaveAssets();
            }
        }
        #region Validatoin
        #endregion

        #endregion
#endif
    }
}
