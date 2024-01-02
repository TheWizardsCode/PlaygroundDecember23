using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Playground
{
    /// <summary>
    /// The Scanner Controller provides a scanner to enemies that enables them to detect the player up to a given range and precision, even without a line of sight.
    /// This allows them to report approximate location to the AI Director and other enemies.
    /// When the Scanner can see the player it will be able to notify of the exact last known location of the player.
    /// </summary>
    public class ScannerController :MonoBehaviour
    {
        [SerializeField, Tooltip("If the scanner can't see the player this is the accuracy of the scan. 0 is perfect accuracy, anything above 0 is a random radius offset from the players position.")]
        float m_ScanPrecision = 50f;
        [SerializeField, Tooltip("How often the scanner will scan for the player. 0 is every frame, anything above 0 is the number of seconds between scan pulses.")]
        float m_ScanFrequency = 10f;
        [SerializeField, Tooltip("Wether or not the enemy should report the suspected or location of the player to the AI Director.")]
        bool m_ReportSuspectedLocation = true;

        float m_TimeOfNextScan = 0f;
        private BasicEnemyController m_controller;
        private AIDirector m_director;
        
        private void Awake()
        {
            m_controller = GetComponentInParent<BasicEnemyController>();
            // OPTIMIZATION: don't use FindObjectOfType
            m_director = FindObjectOfType<AIDirector>();
            if (m_director == null)
            {
                Debug.LogWarning("ScannerController: No AIDirector found in scene. Scanner functionality removed.");
                Destroy(this);
            }
        }

        protected void Update()
        {
            if (m_controller.Target == null || m_TimeOfNextScan > Time.timeSinceLevelLoad)
            {
                return;
            }

            if (m_controller.CanSeeTarget)
            {
                m_director.ReportPlayerLocation(m_controller.Target.position, 0f);
            }
            else
            {
                m_TimeOfNextScan = Time.timeSinceLevelLoad + m_ScanFrequency;

                m_controller.goalDestination = m_controller.Target.position;
                if (m_ScanPrecision > 0f)
                {
                    m_controller.goalDestination += UnityEngine.Random.insideUnitSphere * m_ScanPrecision;
                }

                if (m_ReportSuspectedLocation)
                {
                    m_director.ReportPlayerLocation(m_controller.goalDestination, m_ScanPrecision);
                }
            }
        }
    }
}
