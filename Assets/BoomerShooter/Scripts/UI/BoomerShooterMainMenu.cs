using BoomerShooter.LevelSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerShooter.UI
{
    public enum BoomerMenuMode
    {
        MainMenu,
        DefeatScreen,
        VictoryScreen
    }

    public class BoomerShooterMainMenu : MonoBehaviour
    {
        public BoomerMenuMode Mode = BoomerMenuMode.MainMenu;
        public string GameTitle = "BOMB BREAKER";
        public Texture2D BackgroundTexture;

        enum SubPage
        {
            Home,
            LevelSelect,
            ArsenalAndControls,
            Options
        }

        SubPage m_CurrentPage = SubPage.Home;
        AudioSource m_AudioSource;
        AudioClip m_ClickClip;

        GUIStyle m_PanelStyle;
        GUIStyle m_BigTitleStyle;
        GUIStyle m_SectionTitleStyle;
        GUIStyle m_SubtitleStyle;
        GUIStyle m_ButtonStyle;
        GUIStyle m_PrimaryButtonStyle;
        GUIStyle m_CardTitleStyle;
        GUIStyle m_CardBodyStyle;
        GUIStyle m_LabelStyle;
        Texture2D m_FallbackBgTex;
        Texture2D m_DarkPanelTex;
        Texture2D m_CardPanelTex;
        Texture2D m_BtnNormalTex;
        Texture2D m_BtnHoverTex;
        Texture2D m_PrimaryBtnNormalTex;
        Texture2D m_PrimaryBtnHoverTex;

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlayerCampaignPersistence.EnsureInstance();

            m_AudioSource = gameObject.GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
            m_ClickClip = CreateClickSound();
        }

        void InitStyles()
        {
            if (m_PanelStyle != null) return;

            if (BackgroundTexture == null && m_FallbackBgTex == null)
            {
                m_FallbackBgTex = CreateStaticMenuBackdrop(512, 288);
            }

            m_DarkPanelTex = MakeTex(2, 2, new Color(0.05f, 0.05f, 0.11f, 0.90f));
            m_CardPanelTex = MakeTex(2, 2, new Color(0.10f, 0.09f, 0.20f, 0.94f));
            m_BtnNormalTex = MakeTex(2, 2, new Color(0.14f, 0.13f, 0.26f, 0.95f));
            m_BtnHoverTex  = MakeTex(2, 2, new Color(0.26f, 0.22f, 0.48f, 0.98f));
            m_PrimaryBtnNormalTex = MakeTex(2, 2, new Color(0.62f, 0.16f, 0.20f, 0.95f));
            m_PrimaryBtnHoverTex  = MakeTex(2, 2, new Color(0.84f, 0.24f, 0.28f, 0.98f));

            m_PanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_DarkPanelTex },
                padding = new RectOffset(24, 24, 20, 20)
            };

            m_BigTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 56,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.84f, 0.25f) }
            };

            m_SectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.84f, 0.28f) }
            };

            m_SubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.82f, 0.98f) }
            };

            m_ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = m_BtnNormalTex, textColor = Color.white },
                hover = { background = m_BtnHoverTex, textColor = new Color(1f, 0.9f, 0.4f) },
                active = { background = m_BtnHoverTex, textColor = Color.cyan }
            };

            m_PrimaryButtonStyle = new GUIStyle(m_ButtonStyle)
            {
                fontSize = 18,
                normal = { background = m_PrimaryBtnNormalTex, textColor = new Color(1f, 0.95f, 0.75f) },
                hover = { background = m_PrimaryBtnHoverTex, textColor = Color.white },
                active = { background = m_PrimaryBtnHoverTex, textColor = Color.yellow }
            };

            m_CardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.92f, 1.0f) }
            };

            m_CardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.90f, 0.96f) }
            };

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        Texture2D CreateStaticMenuBackdrop(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = nx - 0.5f;
                    float dy = ny - 0.55f;
                    float radial = Mathf.Sqrt(dx * dx + dy * dy);

                    // Deep retro industrial-cosmic gradient with subtle grid & central blast glow
                    Color baseCol = Color.Lerp(new Color(0.06f, 0.05f, 0.12f), new Color(0.16f, 0.08f, 0.22f), ny);
                    float glow = Mathf.Exp(-radial * 4.2f) * 0.42f;
                    Color blastCol = new Color(0.95f, 0.35f, 0.18f) * glow;

                    bool gridLine = (x % 32 == 0) || (y % 32 == 0);
                    float gridFactor = gridLine ? 0.04f : 0f;

                    float vignette = Mathf.Clamp01(1.15f - radial * 1.1f);
                    Color finalCol = (baseCol + blastCol + new Color(gridFactor, gridFactor, gridFactor * 1.5f)) * vignette;
                    finalCol.a = 1f;
                    pixels[y * w + x] = finalCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void OnGUI()
        {
            InitStyles();

            // Draw full-screen static background image
            Texture2D bg = BackgroundTexture != null ? BackgroundTexture : m_FallbackBgTex;
            if (bg != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), bg, ScaleMode.ScaleAndCrop);
            }

            float panelW = Mathf.Min(780f, Screen.width - 40f);
            float panelH = Mathf.Min(560f, Screen.height - 40f);
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.5f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none, m_PanelStyle);

            GUILayout.BeginArea(new Rect(panelX + 28f, panelY + 22f, panelW - 56f, panelH - 44f));

            if (Mode == BoomerMenuMode.DefeatScreen && m_CurrentPage == SubPage.Home)
            {
                DrawDefeatHome();
            }
            else if (Mode == BoomerMenuMode.VictoryScreen && m_CurrentPage == SubPage.Home)
            {
                DrawVictoryHome();
            }
            else
            {
                switch (m_CurrentPage)
                {
                    case SubPage.Home:
                        DrawMainHome();
                        break;
                    case SubPage.LevelSelect:
                        DrawLevelSelect();
                        break;
                    case SubPage.ArsenalAndControls:
                        DrawArsenalAndControls();
                        break;
                    case SubPage.Options:
                        DrawOptions();
                        break;
                }
            }

            GUILayout.EndArea();
        }

        void DrawMainHome()
        {
            GUILayout.Space(14f);
            // Large title without any subtitle/description
            GUILayout.Label(GameTitle.ToUpper(), m_BigTitleStyle, GUILayout.Height(74f));
            GUILayout.Space(28f);

            if (GUILayout.Button("INICIAR CAMPANA (E1M1: TUTORIAL)", m_PrimaryButtonStyle, GUILayout.Height(52f)))
            {
                PlayClick();
                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ResetCampaign();
                SceneManager.LoadScene("MainScene");
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("SELECCIONAR NIVEL (E1M1 / E1M2 / E1M3)", m_ButtonStyle, GUILayout.Height(46f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("ARSENAL Y CONTROLES", m_ButtonStyle, GUILayout.Height(46f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.ArsenalAndControls;
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("OPCIONES Y AJUSTES", m_ButtonStyle, GUILayout.Height(46f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Options;
            }

            GUILayout.Space(16f);

            if (GUILayout.Button("SALIR DEL JUEGO", m_ButtonStyle, GUILayout.Height(42f)))
            {
                PlayClick();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void DrawDefeatHome()
        {
            var persist = PlayerCampaignPersistence.EnsureInstance();
            string retryScene = string.IsNullOrEmpty(persist.LastPlayedSceneName) ? "MainScene" : persist.LastPlayedSceneName;
            string retryTitle = string.IsNullOrEmpty(persist.LastPlayedLevelTitle) ? "E1M1: Santuario del Iniciado" : persist.LastPlayedLevelTitle;

            GUILayout.Space(18f);
            m_BigTitleStyle.normal.textColor = new Color(1.0f, 0.32f, 0.32f);
            GUILayout.Label("HAS CAIDO EN COMBATE", m_BigTitleStyle, GUILayout.Height(68f));
            m_BigTitleStyle.normal.textColor = new Color(1.0f, 0.84f, 0.25f);
            GUILayout.Space(8f);
            GUILayout.Label("NIVEL ACTUAL: " + retryTitle.ToUpper(), m_SubtitleStyle);
            GUILayout.Space(36f);

            if (GUILayout.Button("REINTENTAR NIVEL ACTUAL (" + retryTitle.ToUpper() + ")", m_PrimaryButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                persist.ClearKeys();
                persist.SavedHealth = -1f;
                SceneManager.LoadScene(retryScene);
            }

            GUILayout.Space(14f);

            if (GUILayout.Button("SELECCIONAR OTRO NIVEL (E1M1 - E1M3)", m_ButtonStyle, GUILayout.Height(48f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(14f);

            if (GUILayout.Button("VOLVER AL MENU PRINCIPAL", m_ButtonStyle, GUILayout.Height(48f)))
            {
                PlayClick();
                SceneManager.LoadScene("IntroMenu");
            }
        }

        void DrawVictoryHome()
        {
            GUILayout.Space(18f);
            m_BigTitleStyle.normal.textColor = new Color(0.35f, 1.0f, 0.65f);
            GUILayout.Label("VICTORIA!", m_BigTitleStyle, GUILayout.Height(68f));
            m_BigTitleStyle.normal.textColor = new Color(1.0f, 0.84f, 0.25f);
            GUILayout.Space(8f);
            GUILayout.Label("HAS COMPLETADO BOMB BREAKER", m_SubtitleStyle);
            GUILayout.Space(36f);

            if (GUILayout.Button("JUGAR NUEVA CAMPANA (DESDE E1M1)", m_PrimaryButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ResetCampaign();
                SceneManager.LoadScene("MainScene");
            }

            GUILayout.Space(14f);

            if (GUILayout.Button("SELECCIONAR NIVEL (E1M1 / E1M2 / E1M3)", m_ButtonStyle, GUILayout.Height(48f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(14f);

            if (GUILayout.Button("VOLVER AL MENU PRINCIPAL", m_ButtonStyle, GUILayout.Height(48f)))
            {
                PlayClick();
                SceneManager.LoadScene("IntroMenu");
            }
        }

        void DrawLevelSelect()
        {
            GUILayout.Label("SELECCION DE NIVEL", m_SectionTitleStyle);
            GUILayout.Space(18f);

            DrawLevelCard(
                "E1M1: SANTUARIO DEL INICIADO (TUTORIAL)",
                "Circuito lineal de 5 zonas: aprende Bunnyhop, Air-Strafe, prueba las 4 armas, descubre el Rocket Jump y abre el Porton de Plata. (4 Enemigos)",
                "MainScene");

            GUILayout.Space(10f);

            DrawLevelCard(
                "E1M2: LAS CATACUMBAS COSMICAS",
                "Estructura Hub No Lineal: explora el Ala de Plata y la Torre de Oro con Jump Pad para desbloquear la Doble Compuerta del Trono. (6 Enemigos)",
                "SecondaryScene");

            GUILayout.Space(10f);

            DrawLevelCard(
                "E1M3: EL COLISEO DEL ABISMO (CLIMAX)",
                "Islas flotantes sobre el vacio, Boveda de la Runa Cosmica y batalla final en el Coliseo Multinivel contra el Arconte Overlord. (8 Enemigos)",
                "ThirdScene");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("<- VOLVER", m_ButtonStyle, GUILayout.Height(42f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Home;
            }
        }

        void DrawLevelCard(string title, string desc, string sceneName)
        {
            GUIStyle cardBox = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_CardPanelTex },
                padding = new RectOffset(14, 14, 10, 10)
            };

            GUILayout.BeginVertical(cardBox);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(500f));
            GUILayout.Label(title, m_CardTitleStyle);
            GUILayout.Space(2f);
            GUILayout.Label(desc, m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(160f));
            GUILayout.Space(10f);
            if (GUILayout.Button("JUGAR", m_PrimaryButtonStyle, GUILayout.Height(42f)))
            {
                PlayClick();
                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ClearKeys();
                persist.SavedHealth = -1f;
                SceneManager.LoadScene(sceneName);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawArsenalAndControls()
        {
            GUILayout.Label("ARSENAL Y CONTROLES", m_SectionTitleStyle);
            GUILayout.Space(18f);

            GUIStyle cardBox = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_CardPanelTex },
                padding = new RectOffset(16, 16, 12, 12)
            };

            GUILayout.BeginVertical(cardBox);
            GUILayout.Label("MOVIMIENTO AVANZADO (ESTILO QUAKE CPM)", m_CardTitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(
                "• WASD: Movimiento base  |  RATON: Apuntar  |  TAB / ESC: Menu de Pausa\n" +
                "• AUTO-BUNNYHOP: Manten presionado [ESPACIO] para saltar continuamente sin perder inercia.\n" +
                "• AIR-STRAFING: En el aire, presiona [A] o [D] y gira suavemente el raton en la misma direccion para ganar velocidad.\n" +
                "• ROCKET JUMP: Equipa el Lanzacohetes [4], mira al suelo, salta y dispara para impulsarte a balcones secretos.",
                m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.Space(12f);

            GUILayout.BeginVertical(cardBox);
            GUILayout.Label("ARSENAL DE 4 ARMAS (TECLAS 1 A 4 / RUEDA DEL RATON)", m_CardTitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(
                "• [1] FUSIL COSMICO: Cadencia rapida y alta precision a media y larga distancia.\n" +
                "• [2] ESCOPETA DE VACIO: Perdigones devastadores a corta distancia contra Stalkers.\n" +
                "• [3] KATANA RUNICA: Ataque cuerpo a cuerpo veloz con municion infinita.\n" +
                "• [4] LANZACOHETES: Dano explosivo en area y empuje radial (65% resistencia al autodano).",
                m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("<- VOLVER", m_ButtonStyle, GUILayout.Height(42f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Home;
            }
        }

        void DrawOptions()
        {
            var persist = PlayerCampaignPersistence.EnsureInstance();

            GUILayout.Label("OPCIONES Y AJUSTES", m_SectionTitleStyle);
            GUILayout.Space(28f);

            GUIStyle cardBox = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_CardPanelTex },
                padding = new RectOffset(20, 20, 18, 18)
            };

            GUILayout.BeginVertical(cardBox);

            GUILayout.Label($"VOLUMEN MAESTRO: {Mathf.RoundToInt(persist.MasterVolume * 100f)}%", m_LabelStyle);
            float newVol = GUILayout.HorizontalSlider(persist.MasterVolume, 0.0f, 1.0f, GUILayout.Height(24f));
            if (!Mathf.Approximately(newVol, persist.MasterVolume))
            {
                persist.MasterVolume = newVol;
                AudioListener.volume = newVol;
            }

            GUILayout.Space(18f);

            GUILayout.Label($"SENSIBILIDAD DEL RATON: {persist.LookSensitivity:F2}", m_LabelStyle);
            persist.LookSensitivity = GUILayout.HorizontalSlider(persist.LookSensitivity, 0.2f, 3.0f, GUILayout.Height(24f));

            GUILayout.Space(18f);

            string lightModeLabel = persist.UseBrightWorkLighting
                ? "MODO DE ILUMINACION: CLARA DE TRABAJO (ACTIVO)"
                : "MODO DE ILUMINACION: FANTASIA OSCURA (ACTIVO)";

            if (GUILayout.Button(lightModeLabel, m_ButtonStyle, GUILayout.Height(44f)))
            {
                PlayClick();
                persist.UseBrightWorkLighting = !persist.UseBrightWorkLighting;
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("<- VOLVER", m_ButtonStyle, GUILayout.Height(42f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Home;
            }
        }

        void PlayClick()
        {
            if (m_AudioSource != null && m_ClickClip != null)
            {
                m_AudioSource.PlayOneShot(m_ClickClip, 0.7f);
            }
        }

        AudioClip CreateClickSound()
        {
            int sampleRate = 44100;
            float duration = 0.06f;
            int samplesCount = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[samplesCount];
            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 45f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 680f * t) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("MenuClickSfx", samplesCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
