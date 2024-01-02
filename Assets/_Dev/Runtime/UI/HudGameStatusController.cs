using NeoFPS;
using System;
using UnityEngine;
using UnityEngine.UI;
using static NeoFPS.HealthDelegates;

namespace Playground
{
	public class HudGameStatusController : PlayerCharacterHudBase
    {
		[SerializeField, Tooltip("The text readout for the current characters resources.")]
		private Text m_ResourcesText = null;
        [SerializeField, Tooltip("The text readout for the number of remaining spawners.")]
        private Text m_SpawnersText = null;
        [SerializeField, Tooltip("The text readout for the number of remaining enemies.")]
        private Text m_EnemiesText = null;

        private PlaygroundDecember23GameMode gameMode = null;
        private NanobotManager nanobotManager = null;

        int spawnersCount = 0;
        private int enemiesCount;

        protected override void Awake()
        {
            base.Awake();

            gameMode = FindObjectOfType<PlaygroundDecember23GameMode>();
            if (gameMode != null)
            {
                gameMode.levelGenerator.onSpawnerCreated.AddListener(OnSpawnerCreated);
            }
        }

        private void OnSpawnerCreated(Spawner spawner)
        {
            spawnersCount++;
            if (m_SpawnersText != null)
            {
                m_SpawnersText.text = spawnersCount.ToString();
            }

            spawner.onDestroyed.AddListener(OnSpawnerDestroyed);
            spawner.onEnemySpawned.AddListener(OnEnemySpawned);
        }

        private void OnSpawnerDestroyed(Spawner spawner)
        {
            spawnersCount--;
            if (m_SpawnersText != null)
            {
                m_SpawnersText.text = spawnersCount.ToString();
            }
        }

        private void OnEnemySpawned(BasicEnemyController enemy)
        {
            enemiesCount++;
            if (m_EnemiesText != null)
            {
                m_EnemiesText.text = enemiesCount.ToString();
            }

            enemy.onDestroyed.AddListener(OnEnemyDestroyed);
        }

        private void OnEnemyDestroyed()
        {
            enemiesCount--;
            if (m_EnemiesText != null)
            {
                m_EnemiesText.text = enemiesCount.ToString();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (nanobotManager != null)
                nanobotManager.onResourcesChanged -= OnResourcesChanged;
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            if (nanobotManager != null)
            {
                nanobotManager.onResourcesChanged -= OnResourcesChanged;
            }

            if (character as Component != null)
            {
                nanobotManager = character.GetComponent<NanobotManager>();
            }
            else
            {
                nanobotManager = null;
            }

            if (nanobotManager != null)
            {
                nanobotManager.onResourcesChanged += OnResourcesChanged;
                OnResourcesChanged(0f, nanobotManager.resources);
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
		}

		protected virtual void OnResourcesChanged (float from, float to)
        {
            m_ResourcesText.text = ((int)to).ToString ();
        }
    }
}