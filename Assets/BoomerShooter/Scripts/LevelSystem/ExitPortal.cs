using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    [RequireComponent(typeof(Collider))]
    public class ExitPortal : Objective
    {
        [Tooltip("If true, portal requires all enemies to be eliminated before activating")]
        public bool RequireAllEnemiesDefeated = false;

        [Tooltip("The vortex visual object that rotates")]
        public Transform VortexVisual;

        [Tooltip("Rotation speed")]
        public float SpinSpeed = 120f;

        [Tooltip("Portal entry audio")]
        public AudioClip TeleportSfx;

        bool m_Triggered;
        EnemyManager m_EnemyManager;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (string.IsNullOrEmpty(Title))
            {
                Title = "Reach the Cosmic Exit Portal";
            }
            if (string.IsNullOrEmpty(Description))
            {
                Description = "Find the Rune Key, unlock the gate, and enter the Exit Portal";
            }
        }

        protected override void Start()
        {
            base.Start();
            m_EnemyManager = FindAnyObjectByType<EnemyManager>();
        }

        void Update()
        {
            if (VortexVisual != null)
            {
                VortexVisual.Rotate(Vector3.forward, SpinSpeed * Time.deltaTime, Space.Self);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Triggered || IsCompleted) return;

            var player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                if (RequireAllEnemiesDefeated && m_EnemyManager != null && m_EnemyManager.NumberOfEnemiesRemaining > 0)
                {
                    DisplayMessageEvent msg = Events.DisplayMessageEvent;
                    msg.Message = $"Portal Locked! Eliminate remaining {m_EnemyManager.NumberOfEnemiesRemaining} enemies!";
                    msg.DelayBeforeDisplay = 0f;
                    EventManager.Broadcast(msg);
                    return;
                }

                m_Triggered = true;

                if (TeleportSfx != null)
                {
                    AudioSource.PlayClipAtPoint(TeleportSfx, transform.position, 1f);
                }

                CompleteObjective(string.Empty, string.Empty, "Exit Portal Reached!");

                if (BoomerShooterLevelManager.Instance != null)
                {
                    BoomerShooterLevelManager.Instance.CompleteLevel();
                }
            }
        }
    }
}
