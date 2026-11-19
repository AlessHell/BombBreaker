using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    public class PlayerCampaignPersistence : MonoBehaviour
    {
        public static PlayerCampaignPersistence Instance { get; private set; }

        public float SavedHealth = -1f;
        public int SavedActiveWeaponIndex = 0;
        public string LastPlayedSceneName = "MainScene";
        public string LastPlayedLevelTitle = "E1M1: Citadel of the Void";

        // Global Options
        public float MasterVolume = 1.0f;
        public float LookSensitivity = 1.0f;
        public bool UseBrightWorkLighting = false;

        [System.NonSerialized]
        public HashSet<string> CollectedKeys = new HashSet<string>();

        public static PlayerCampaignPersistence EnsureInstance()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<PlayerCampaignPersistence>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var go = new GameObject("PlayerCampaignPersistence");
                    Instance = go.AddComponent<PlayerCampaignPersistence>();
                }
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveState(GameObject player)
        {
            if (player == null) return;

            var health = player.GetComponent<Health>();
            if (health != null)
            {
                SavedHealth = health.CurrentHealth;
            }

            var wm = player.GetComponent<PlayerWeaponsManager>();
            if (wm != null)
            {
                SavedActiveWeaponIndex = wm.ActiveWeaponIndex;
            }
        }

        public void RestoreState(GameObject player)
        {
            if (player == null) return;

            if (SavedHealth > 0f)
            {
                var health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.CurrentHealth = SavedHealth;
                }
            }

            var wm = player.GetComponent<PlayerWeaponsManager>();
            if (wm != null && SavedActiveWeaponIndex >= 0)
            {
                wm.SwitchToWeaponIndex(SavedActiveWeaponIndex, true);
            }

            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null && LookSensitivity > 0.05f)
            {
                input.LookSensitivity = LookSensitivity;
            }
        }

        public void AddKey(string keyId)
        {
            if (!CollectedKeys.Contains(keyId))
            {
                CollectedKeys.Add(keyId);
            }
        }

        public bool HasKey(string keyId)
        {
            return CollectedKeys.Contains(keyId);
        }

        public void ClearKeys()
        {
            CollectedKeys.Clear();
        }

        public void ResetCampaign()
        {
            SavedHealth = -1f;
            SavedActiveWeaponIndex = 0;
            CollectedKeys.Clear();
        }
    }
}
