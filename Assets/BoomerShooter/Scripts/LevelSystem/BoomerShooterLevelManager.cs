using System.Collections;
using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BoomerShooter.LevelSystem
{
    public class BoomerShooterLevelManager : MonoBehaviour
    {
        public static BoomerShooterLevelManager Instance { get; private set; }

        [Header("Level Info")]
        public int LevelNumber = 1;
        public string LevelName = "Citadel of the Void";
        public string NextSceneName = "SecondaryScene";

        [Header("Lighting Mode")]
        [Tooltip("Optional flat studio lighting mode; false uses the atmospheric Bomb Breaker cover lighting")]
        public bool UseBrightWorkLighting = false;

        [Header("Statistics")]
        public int TotalSecretsInLevel = 1;
        public int SecretsFound { get; private set; }

        [Header("Level Completion Flow")]
        [Tooltip("Delay before loading next scene if auto-advancing")]
        public float DelayBeforeNextScene = 4f;

        [Tooltip("Audio played on level complete")]
        public AudioClip LevelVictorySfx;

        public bool IsLevelComplete { get; private set; }
        public float LevelStartTime { get; private set; }
        public float LevelElapsedTime { get; private set; }

        int m_InitialEnemiesCount;
        EnemyManager m_EnemyManager;
        GameFlowManager m_GameFlowManager;
        GameObject m_Player;
        GUIStyle m_HudBoxStyle;
        GUIStyle m_HudLabelStyle;
        GUIStyle m_KeyLabelStyle;
        GUIStyle m_PauseBtnStyle;
        GUIStyle m_PauseHeaderStyle;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            LevelStartTime = Time.time;
            EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);

            var persist = PlayerCampaignPersistence.EnsureInstance();
            persist.LastPlayedSceneName = SceneManager.GetActiveScene().name;
            persist.LastPlayedLevelTitle = $"E1M{LevelNumber}: {LevelName}";

            ApplyLightingMode(persist.UseBrightWorkLighting);
        }

        public void ApplyLightingMode(bool flatWorkLighting)
        {
            LightmapSettings.lightmaps = new LightmapData[0];

            if (flatWorkLighting)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.92f, 0.92f, 0.96f, 1f);
                RenderSettings.ambientIntensity = 1.5f;
                RenderSettings.fog = false;

                var dirLight = GameObject.Find("Directional Light");
                if (dirLight != null)
                {
                    var l = dirLight.GetComponent<Light>();
                    if (l != null)
                    {
                        l.intensity = 1.8f;
                        l.color = Color.white;
                        l.shadows = LightShadows.None;
                    }
                }
            }
            else
            {
                // Atmospheric "Bomb Breaker" Stormy Highland lighting matching FondoMenu.jpg
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.56f, 0.72f, 0.71f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.36f, 0.47f, 0.46f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.20f, 0.27f, 0.18f, 1f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.27f, 0.39f, 0.39f, 1f);
                RenderSettings.fogDensity = (LevelNumber == 3) ? 0.0062f : 0.0085f;

                var dirLight = GameObject.Find("Directional Light");
                if (dirLight != null)
                {
                    dirLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                    var l = dirLight.GetComponent<Light>();
                    if (l != null)
                    {
                        l.intensity = 1.45f;
                        l.color = new Color(0.86f, 0.94f, 0.93f, 1f);
                        l.shadows = LightShadows.Soft;
                        l.shadowStrength = 0.55f;
                    }
                }
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
        }

        void Start()
        {
            m_EnemyManager = FindAnyObjectByType<EnemyManager>();
            if (m_EnemyManager != null)
            {
                m_InitialEnemiesCount = m_EnemyManager.NumberOfEnemiesTotal;
            }

            m_GameFlowManager = FindAnyObjectByType<GameFlowManager>();
            if (m_GameFlowManager != null && !string.IsNullOrEmpty(NextSceneName))
            {
                m_GameFlowManager.WinSceneName = NextSceneName;
            }

            var playerController = FindAnyObjectByType<PlayerCharacterController>();
            if (playerController != null)
            {
                m_Player = playerController.gameObject;

                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ClearKeys();
                persist.RestoreState(m_Player);
            }
        }

        void OnAllObjectivesCompleted(AllObjectivesCompletedEvent evt)
        {
            if (!IsLevelComplete)
            {
                CompleteLevel();
            }
        }

        public void RegisterSecretFound()
        {
            SecretsFound++;
        }

        public void CompleteLevel()
        {
            if (IsLevelComplete) return;
            IsLevelComplete = true;
            LevelElapsedTime = Time.time - LevelStartTime;

            if (m_Player != null && PlayerCampaignPersistence.Instance != null)
            {
                PlayerCampaignPersistence.Instance.SaveState(m_Player);
            }

            Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
            if (LevelVictorySfx != null)
            {
                AudioSource.PlayClipAtPoint(LevelVictorySfx, camTransform.position, 1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(CreateVictoryClip(), camTransform.position, 1f);
            }

            var intermission = FindAnyObjectByType<LevelIntermissionUI>();
            if (intermission != null)
            {
                int totalEnemies = m_EnemyManager != null ? m_EnemyManager.NumberOfEnemiesTotal : m_InitialEnemiesCount;
                int remainingEnemies = m_EnemyManager != null ? m_EnemyManager.NumberOfEnemiesRemaining : 0;
                int kills = Mathf.Max(0, totalEnemies - remainingEnemies);

                intermission.Show($"E1M{LevelNumber}: {LevelName}", LevelElapsedTime, kills, totalEnemies, SecretsFound, TotalSecretsInLevel);
            }

            StartCoroutine(LoadNextLevelRoutine());
        }

        IEnumerator LoadNextLevelRoutine()
        {
            float elapsed = 0f;
            while (elapsed < DelayBeforeNextScene)
            {
                elapsed += Time.deltaTime;
                if (elapsed > 0.75f)
                {
                    bool skipPressed = false;
                    if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                        skipPressed = true;
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                        skipPressed = true;

                    if (skipPressed)
                        break;
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(NextSceneName))
            {
                SceneManager.LoadScene(NextSceneName);
            }
        }

        void OnGUI()
        {
            if (IsLevelComplete) return;

            if (m_HudLabelStyle == null)
            {
                m_HudBoxStyle = new GUIStyle(GUI.skin.box);
                m_HudLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.9f, 1f) }
                };
                m_KeyLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.85f, 0.25f) }
                };
                m_PauseBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                m_PauseHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.85f, 0.3f) }
                };
            }

            float width = 260f;
            float height = 90f;
            float x = Screen.width - width - 16f;
            float y = 16f;

            GUI.color = new Color(0.05f, 0.04f, 0.12f, 0.75f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, m_HudBoxStyle);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 12f, y + 8f, width - 24f, height - 16f));
            GUILayout.Label($"E1M{LevelNumber}: {LevelName.ToUpper()}", m_HudLabelStyle);

            int totalEnemies = m_EnemyManager != null ? m_EnemyManager.NumberOfEnemiesTotal : m_InitialEnemiesCount;
            int remaining = m_EnemyManager != null ? m_EnemyManager.NumberOfEnemiesRemaining : 0;
            int kills = Mathf.Max(0, totalEnemies - remaining);
            GUILayout.Label($"KILLS: {kills}/{totalEnemies}   SECRETS: {SecretsFound}/{TotalSecretsInLevel}", m_HudLabelStyle);

            string keysText = "KEYS: NONE";
            if (PlayerCampaignPersistence.Instance != null && PlayerCampaignPersistence.Instance.CollectedKeys.Count > 0)
            {
                keysText = "KEYS: " + string.Join(", ", PlayerCampaignPersistence.Instance.CollectedKeys).ToUpper();
            }
            GUILayout.Label(keysText, m_KeyLabelStyle);
            GUILayout.EndArea();

            // When Pause Menu is open (Time.timeScale == 0f), display quick campaign actions bar at bottom
            if (Time.timeScale == 0f)
            {
                float barW = Mathf.Min(760f, Screen.width - 40f);
                float barH = 92f;
                float barX = (Screen.width - barW) * 0.5f;
                float barY = Screen.height - barH - 24f;

                GUI.color = new Color(0.06f, 0.05f, 0.15f, 0.95f);
                GUI.Box(new Rect(barX, barY, barW, barH), GUIContent.none, m_HudBoxStyle);
                GUI.color = Color.white;

                GUILayout.BeginArea(new Rect(barX + 16f, barY + 10f, barW - 32f, barH - 20f));
                GUILayout.Label("NAVEGACION RAPIDA DE CAMPANA", m_PauseHeaderStyle);
                GUILayout.Space(6f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("REINICIAR NIVEL", m_PauseBtnStyle, GUILayout.Height(36f)))
                {
                    LoadSceneFromPause(SceneManager.GetActiveScene().name, false);
                }
                if (GUILayout.Button("E1M1: TUTORIAL", m_PauseBtnStyle, GUILayout.Height(36f)))
                {
                    LoadSceneFromPause("MainScene", false);
                }
                if (GUILayout.Button("E1M2: CATACUMBAS", m_PauseBtnStyle, GUILayout.Height(36f)))
                {
                    LoadSceneFromPause("SecondaryScene", false);
                }
                if (GUILayout.Button("E1M3: COLISEO", m_PauseBtnStyle, GUILayout.Height(36f)))
                {
                    LoadSceneFromPause("ThirdScene", false);
                }
                if (GUILayout.Button("MENU PRINCIPAL", m_PauseBtnStyle, GUILayout.Height(36f)))
                {
                    LoadSceneFromPause("IntroMenu", false);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        void LoadSceneFromPause(string sceneName, bool resetCampaign)
        {
            Time.timeScale = 1f;
            AudioUtility.SetMasterVolume(1f);
            if (resetCampaign && PlayerCampaignPersistence.Instance != null)
            {
                PlayerCampaignPersistence.Instance.ResetCampaign();
            }
            SceneManager.LoadScene(sceneName);
        }

        AudioClip CreateVictoryClip()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] freqs = { 261.63f, 329.63f, 392.00f, 523.25f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 2.5f);
                float wave = 0f;
                foreach (var f in freqs)
                {
                    wave += Mathf.Sin(2 * Mathf.PI * f * t);
                }
                samples[i] = (wave / freqs.Length) * env * 0.8f;
            }

            AudioClip clip = AudioClip.Create("LevelVictoryChime", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
