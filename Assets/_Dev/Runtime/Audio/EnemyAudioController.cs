using NaughtyAttributes;
using NeoFPS;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Playground
{
    internal class EnemyAudioController : MonoBehaviour
    {
        [SerializeField, Tooltip("The Enemy Audio Definition defines the sounds to use. The best way to start is to drag in an existing configuration and then save a copy using the button below. Then edit for your needs."), Expandable]
        [Required("An Audio configuration must be provided.")]
        EnemyAudioDefinition config = null;

        [Header("Audio Sources")]
        [SerializeField, Tooltip("The audio source for this enemies drone sound.")]
        AudioSource droneSource = null;

        BasicEnemyController m_EnemyController = null;

        private void Awake()
        {
            m_EnemyController = GetComponent<BasicEnemyController>();
        }

        private void OnEnable()
        {
            m_EnemyController.onDeath.AddListener(OnDeath);
        }

        private void Start()
        {
            StartDrone();
        }

        private void OnDisable()
        {
            m_EnemyController.onDeath.RemoveListener(OnDeath);
            StopDrone();
        }

        protected void OnDeath(BasicEnemyController enemy)
        {
            droneSource.Stop();

            if (config.deathClips.Length > 0)
            {
                PlayOneShot(config.deathClips[UnityEngine.Random.Range(0, config.deathClips.Length)], transform.position);
            } else
            {
                PlayOneShot(config.GetDeathClip(), transform.position);
            }
        }

        private void StartDrone()
        {
            if (config.droneClip != null)
            {
                droneSource.clip = config.droneClip;
            }
            else
            {
                droneSource.clip = config.droneClip;
            }
            droneSource.loop = true;
            droneSource.Play();
        }

        private void StopDrone()
        {
            droneSource.Stop();
        }

        static void PlayOneShot(AudioClip clip, Vector3 position)
        {
            // OPTIMIZATION Play only a limited number of death sounds within a certain time frame. Perhaps adding chorus or similar on subsequent calls
            NeoFpsAudioManager.PlayEffectAudioAtPosition(clip, position);
        }

#if UNITY_EDITOR
        #region Inspector
        [Button]
        private void SaveCopyOfConfig()
        {
            string defaultPath = AssetDatabase.GetAssetPath(config);
            string directoryPath = Path.GetDirectoryName(defaultPath);

            string path = EditorUtility.SaveFilePanel(
                "Save Enemy Definition",
                directoryPath,
                $"{transform.root.name} Enemy Audio Definition",
                "asset"
            );

            if (path.Length != 0)
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

                EnemyAudioDefinition newConfig = ScriptableObject.CreateInstance<EnemyAudioDefinition>();

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
