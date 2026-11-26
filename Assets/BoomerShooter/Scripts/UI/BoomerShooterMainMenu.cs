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
        public string GameTitleLine1 = "BONE";
        public string GameTitleLine2 = "BREAKER";
        public Color TitleMainColor = new Color(1.0f, 0.88f, 0.22f, 1f);
        public Color TitleOutlineColor = new Color(0.02f, 0.03f, 0.05f, 1f);
        public Texture2D BackgroundTexture;
        public Font TitleFont;
        public Font SubtitleFont;

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

        GUIStyle m_MetallicBoxStyle;
        GUIStyle m_CardBoxStyle;
        GUIStyle m_BigTitleStyle;
        GUIStyle m_BigTitleOutlineStyle;
        GUIStyle m_SectionTitleStyle;
        GUIStyle m_SubtitleStyle;
        GUIStyle m_BarButtonStyle;
        GUIStyle m_PrimaryBarButtonStyle;
        GUIStyle m_CardTitleStyle;
        GUIStyle m_CardBodyStyle;
        GUIStyle m_LabelStyle;

        Texture2D m_FallbackBgTex;
        Texture2D m_MetallicBoxTex;
        Texture2D m_CardPanelTex;
        Texture2D m_BarBtnNormalTex;
        Texture2D m_BarBtnHoverTex;
        Texture2D m_BarBtnActiveTex;

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
            if (m_MetallicBoxStyle != null) return;

            if (BackgroundTexture == null && m_FallbackBgTex == null)
            {
                m_FallbackBgTex = MakeSolidTex(4, 4, new Color(0.08f, 0.14f, 0.16f, 1f));
            }

            m_MetallicBoxTex = CreateMetallicNotchedBoxTex(128, 128);
            m_CardPanelTex = CreateBeveledCardTex(64, 64, new Color(0.04f, 0.08f, 0.12f, 0.97f), new Color(0.45f, 0.72f, 0.88f, 1.0f));
            m_BarBtnNormalTex = CreateGlossyBarButtonTex(128, 56, false, false);
            m_BarBtnHoverTex  = CreateGlossyBarButtonTex(128, 56, true, false);
            m_BarBtnActiveTex = CreateGlossyBarButtonTex(128, 56, true, true);

            m_MetallicBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_MetallicBoxTex },
                border = new RectOffset(20, 20, 44, 20),
                padding = new RectOffset(34, 34, 48, 28)
            };

            m_CardBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_CardPanelTex },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(20, 20, 16, 16)
            };

            m_BigTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = TitleFont,
                fontSize = 112,
                fontStyle = TitleFont != null ? FontStyle.Normal : FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TitleMainColor }
            };

            m_BigTitleOutlineStyle = new GUIStyle(m_BigTitleStyle)
            {
                normal = { textColor = TitleOutlineColor }
            };

            m_SectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = SubtitleFont != null ? SubtitleFont : TitleFont,
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.88f, 0.28f) }
            };

            m_SubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = SubtitleFont,
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.96f, 1.0f) }
            };

            m_BarButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = SubtitleFont,
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(12, 12, 12, 12),
                normal = { background = m_BarBtnNormalTex, textColor = Color.white },
                hover = { background = m_BarBtnHoverTex, textColor = new Color(1.0f, 0.90f, 0.30f) },
                active = { background = m_BarBtnActiveTex, textColor = Color.cyan }
            };

            m_PrimaryBarButtonStyle = new GUIStyle(m_BarButtonStyle)
            {
                fontSize = 30,
                normal = { background = m_BarBtnNormalTex, textColor = new Color(1.0f, 0.92f, 0.38f) },
                hover = { background = m_BarBtnHoverTex, textColor = Color.white }
            };

            m_CardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = SubtitleFont,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.88f, 0.28f) }
            };

            m_CardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.97f, 1.0f) }
            };

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = SubtitleFont,
                fontSize = 24,
                normal = { textColor = Color.white }
            };
        }

        Texture2D MakeSolidTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        Texture2D CreateMetallicNotchedBoxTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[w * h];

            Color outerRim = new Color(0.55f, 0.78f, 0.92f, 1.0f);
            Color innerHighlight = new Color(0.75f, 0.92f, 1.0f, 1.0f);
            Color darkRecess = new Color(0.03f, 0.06f, 0.10f, 0.98f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    bool topOrBottomNotch = (y < 6 || y >= h - 6) && ((x >= 10 && x < 18) || (x >= w - 18 && x < w - 10));
                    bool leftOrRightNotch = (x < 6 || x >= w - 6) && ((y >= 10 && y < 18) || (y >= h - 18 && y < h - 10));
                    bool cornerCut = (x < 3 && y < 3) || (x >= w - 3 && y < 3) || (x < 3 && y >= h - 3) || (x >= w - 3 && y >= h - 3);

                    if (topOrBottomNotch || leftOrRightNotch || cornerCut)
                    {
                        pixels[y * w + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    int distEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));

                    if (distEdge <= 2)
                    {
                        pixels[y * w + x] = outerRim;
                    }
                    else if (distEdge == 3)
                    {
                        pixels[y * w + x] = innerHighlight;
                    }
                    else if (distEdge <= 5)
                    {
                        pixels[y * w + x] = darkRecess;
                    }
                    else
                    {
                        if (y >= h - 36)
                        {
                            float headerGrad = (float)(y - (h - 36)) / 36f;
                            Color headerCol = Color.Lerp(new Color(0.16f, 0.24f, 0.30f, 0.98f), new Color(0.26f, 0.36f, 0.44f, 0.98f), headerGrad);
                            if (y == h - 36) headerCol = new Color(0.05f, 0.09f, 0.13f, 1.0f);
                            pixels[y * w + x] = headerCol;
                        }
                        else
                        {
                            Color bodyCol = Color.Lerp(new Color(0.06f, 0.10f, 0.15f, 0.97f), new Color(0.13f, 0.20f, 0.26f, 0.97f), ny);
                            pixels[y * w + x] = bodyCol;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        Texture2D CreateGlossyBarButtonTex(int w, int h, bool hover, bool active)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[w * h];

            Color borderCol = hover
                ? new Color(0.55f, 0.92f, 1.0f, 1f)
                : new Color(0.35f, 0.54f, 0.66f, 1f);

            Color innerRimCol = hover
                ? new Color(0.24f, 0.54f, 0.70f, 1f)
                : new Color(0.16f, 0.27f, 0.36f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Min(x, w - 1 - x);
                    int dy = Mathf.Min(y, h - 1 - y);
                    if (dx + dy < 4)
                    {
                        pixels[y * w + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    int dist = Mathf.Min(dx, dy);
                    if (dist <= 1 || dx + dy == 4)
                    {
                        pixels[y * w + x] = borderCol;
                    }
                    else if (dist <= 3)
                    {
                        pixels[y * w + x] = innerRimCol;
                    }
                    else
                    {
                        Color core;
                        if (ny > 0.55f)
                        {
                            float t = (ny - 0.55f) / 0.45f;
                            core = Color.Lerp(
                                hover ? new Color(0.08f, 0.18f, 0.28f, 1f) : new Color(0.03f, 0.06f, 0.11f, 1f),
                                hover ? new Color(0.22f, 0.38f, 0.52f, 1f) : new Color(0.13f, 0.21f, 0.28f, 1f),
                                t);
                        }
                        else
                        {
                            float t = ny / 0.55f;
                            core = Color.Lerp(
                                hover ? new Color(0.10f, 0.24f, 0.35f, 1f) : new Color(0.05f, 0.10f, 0.16f, 1f),
                                new Color(0.01f, 0.03f, 0.05f, 1f),
                                t);
                        }

                        if (active)
                        {
                            core += new Color(0.10f, 0.18f, 0.25f, 0f);
                        }
                        pixels[y * w + x] = core;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        Texture2D CreateBeveledCardTex(int w, int h, Color fill, Color border)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    pixels[y * w + x] = d <= 2 ? border : fill;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void OnGUI()
        {
            InitStyles();

            // 1. Full-screen static background image (FondoMenu.jpg)
            Texture2D bg = BackgroundTexture != null ? BackgroundTexture : m_FallbackBgTex;
            if (bg != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), bg, ScaleMode.ScaleAndCrop);
            }

            bool isHomePage = (m_CurrentPage == SubPage.Home);

            if (isHomePage)
            {
                // 2. Huge high-contrast stacked title above the metallic box
                DrawStackedFloatingTitle();

                // 3. Large, clearly readable lower-center metallic notched container
                float boxW = Mathf.Min(680f, Screen.width - 36f);
                float boxH = (Mode == BoomerMenuMode.MainMenu) ? 450f : 360f;
                float boxX = (Screen.width - boxW) * 0.5f;
                float boxY = Mathf.Clamp(Screen.height * 0.41f, 240f, Mathf.Max(240f, Screen.height - boxH - 20f));

                GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none, m_MetallicBoxStyle);

                GUILayout.BeginArea(new Rect(boxX + 34f, boxY + 46f, boxW - 68f, boxH - 68f));
                if (Mode == BoomerMenuMode.DefeatScreen)
                {
                    DrawDefeatButtons();
                }
                else if (Mode == BoomerMenuMode.VictoryScreen)
                {
                    DrawVictoryButtons();
                }
                else
                {
                    DrawMainHomeButtons();
                }
                GUILayout.EndArea();
            }
            else
            {
                // Large sub-page container (Level Select, Arsenal & Controls, Options)
                float panelW = Mathf.Min(960f, Screen.width - 36f);
                float panelH = Mathf.Min(660f, Screen.height - 36f);
                float panelX = (Screen.width - panelW) * 0.5f;
                float panelY = (Screen.height - panelH) * 0.5f;

                GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none, m_MetallicBoxStyle);

                GUILayout.BeginArea(new Rect(panelX + 36f, panelY + 48f, panelW - 72f, panelH - 74f));
                switch (m_CurrentPage)
                {
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
                GUILayout.EndArea();
            }
        }

        void DrawStackedFloatingTitle()
        {
            string line1 = GameTitleLine1.ToUpper();
            string line2 = GameTitleLine2.ToUpper();

            Color mainCol = TitleMainColor;
            if (Mode == BoomerMenuMode.DefeatScreen)
            {
                line1 = "CAIDO EN";
                line2 = "COMBATE";
                mainCol = new Color(1.0f, 0.35f, 0.30f, 1f);
            }
            else if (Mode == BoomerMenuMode.VictoryScreen)
            {
                line1 = "VICTORIA";
                line2 = "TOTAL";
                mainCol = new Color(0.35f, 1.0f, 0.65f, 1f);
            }

            int dynamicFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.145f), 76, 136);
            m_BigTitleStyle.fontSize = dynamicFontSize;
            m_BigTitleStyle.normal.textColor = mainCol;
            m_BigTitleOutlineStyle.fontSize = dynamicFontSize;

            float titleBlockWidth = Screen.width;
            float line1Y = Screen.height * 0.035f;
            float lineHeight = dynamicFontSize * 1.12f;
            float line2Y = line1Y + lineHeight * 0.84f;

            DrawTextWithHeavyOutline(new Rect(0f, line1Y, titleBlockWidth, lineHeight), line1);
            if (!string.IsNullOrEmpty(line2))
            {
                DrawTextWithHeavyOutline(new Rect(0f, line2Y, titleBlockWidth, lineHeight), line2);
            }
        }

        void DrawTextWithHeavyOutline(Rect rect, string text)
        {
            // Thick dark drop shadow + 8-way outline so the bright gold title stands out 100% against FondoMenu.jpg
            float d = 4.0f;
            GUI.Label(new Rect(rect.x + 6f, rect.y + 7f, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x - d, rect.y - d, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x + d, rect.y - d, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x - d, rect.y + d, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x + d, rect.y + d, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x - d, rect.y, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x + d, rect.y, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x, rect.y - d, rect.width, rect.height), text, m_BigTitleOutlineStyle);
            GUI.Label(new Rect(rect.x, rect.y + d, rect.width, rect.height), text, m_BigTitleOutlineStyle);

            // Bright gold/amber foreground
            GUI.Label(rect, text, m_BigTitleStyle);
        }

        void DrawMainHomeButtons()
        {
            float btnH = 58f;
            float spacing = 12f;

            if (GUILayout.Button("INICIAR CAMPANA", m_PrimaryBarButtonStyle, GUILayout.Height(btnH)))
            {
                PlayClick();
                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ResetCampaign();
                SceneManager.LoadScene("MainScene");
            }

            GUILayout.Space(spacing);

            if (GUILayout.Button("SELECCIONAR NIVEL", m_BarButtonStyle, GUILayout.Height(btnH)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(spacing);

            if (GUILayout.Button("ARSENAL Y CONTROLES", m_BarButtonStyle, GUILayout.Height(btnH)))
            {
                PlayClick();
                m_CurrentPage = SubPage.ArsenalAndControls;
            }

            GUILayout.Space(spacing);

            if (GUILayout.Button("OPCIONES", m_BarButtonStyle, GUILayout.Height(btnH)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Options;
            }

            GUILayout.Space(spacing);

            if (GUILayout.Button("SALIR", m_BarButtonStyle, GUILayout.Height(btnH)))
            {
                PlayClick();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void DrawDefeatButtons()
        {
            var persist = PlayerCampaignPersistence.EnsureInstance();
            string retryScene = string.IsNullOrEmpty(persist.LastPlayedSceneName) ? "MainScene" : persist.LastPlayedSceneName;
            string retryTitle = string.IsNullOrEmpty(persist.LastPlayedLevelTitle) ? "E1M1" : persist.LastPlayedLevelTitle;

            GUILayout.Label("NIVEL: " + retryTitle.ToUpper(), m_SubtitleStyle);
            GUILayout.Space(14f);

            if (GUILayout.Button("REINTENTAR NIVEL ACTUAL", m_PrimaryBarButtonStyle, GUILayout.Height(58f)))
            {
                PlayClick();
                persist.ClearKeys();
                persist.SavedHealth = -1f;
                SceneManager.LoadScene(retryScene);
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("SELECCIONAR NIVEL", m_BarButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("MENU PRINCIPAL", m_BarButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                SceneManager.LoadScene("IntroMenu");
            }
        }

        void DrawVictoryButtons()
        {
            GUILayout.Label("HAS COMPLETADO BONE BREAKER!", m_SubtitleStyle);
            GUILayout.Space(14f);

            if (GUILayout.Button("NUEVA CAMPANA (E1M1)", m_PrimaryBarButtonStyle, GUILayout.Height(58f)))
            {
                PlayClick();
                var persist = PlayerCampaignPersistence.EnsureInstance();
                persist.ResetCampaign();
                SceneManager.LoadScene("MainScene");
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("SELECCIONAR NIVEL", m_BarButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.LevelSelect;
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("MENU PRINCIPAL", m_BarButtonStyle, GUILayout.Height(56f)))
            {
                PlayClick();
                SceneManager.LoadScene("IntroMenu");
            }
        }

        void DrawLevelSelect()
        {
            GUILayout.Label("SELECCIONAR NIVEL", m_SectionTitleStyle);
            GUILayout.Space(16f);

            DrawLevelCard(
                "E1M1: SANTUARIO DEL INICIADO (TUTORIAL)",
                "Circuito de 5 zonas: Bunnyhop, Air-Strafe, las 4 armas, Rocket Jump y Llave de Plata. (4 Enemigos)",
                "MainScene");

            GUILayout.Space(12f);

            DrawLevelCard(
                "E1M2: LAS CATACUMBAS COSMICAS",
                "Exploracion Hub No Lineal: Ala de Plata, Torre de Oro con Jump Pad y Camara del Trono. (6 Enemigos)",
                "SecondaryScene");

            GUILayout.Space(12f);

            DrawLevelCard(
                "E1M3: EL COLISEO DEL ABISMO (FINAL)",
                "Islas flotantes sobre el vacio, Boveda de la Runa Cosmica y Jefe Arconte Overlord. (8 Enemigos)",
                "ThirdScene");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("VOLVER", m_BarButtonStyle, GUILayout.Height(54f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Home;
            }
        }

        void DrawLevelCard(string title, string desc, string sceneName)
        {
            GUILayout.BeginVertical(m_CardBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(640f));
            GUILayout.Label(title, m_CardTitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(desc, m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(190f));
            GUILayout.Space(10f);
            if (GUILayout.Button("JUGAR", m_PrimaryBarButtonStyle, GUILayout.Height(50f)))
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
            GUILayout.Space(16f);

            GUILayout.BeginVertical(m_CardBoxStyle);
            GUILayout.Label("MOVIMIENTO AVANZADO (QUAKE CPM)", m_CardTitleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(
                "• WASD: Moverse  |  Raton: Apuntar  |  TAB / ESC: Pausa\n" +
                "• Auto-Bunnyhop: Manten presionado [ESPACIO] para saltar sin perder inercia.\n" +
                "• Air-Strafe: En el aire, presiona [A] o [D] girando el raton hacia el mismo lado.\n" +
                "• Rocket Jump: Con el Lanzacohetes [4], mira al suelo, salta y dispara.",
                m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.Space(14f);

            GUILayout.BeginVertical(m_CardBoxStyle);
            GUILayout.Label("ARSENAL DE 4 ARMAS (TECLAS 1 A 4)", m_CardTitleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(
                "• [1] Fusil Cosmico: Cadencia rapida a media y larga distancia.\n" +
                "• [2] Escopeta de Vacio: Alto impacto a corta distancia.\n" +
                "• [3] Katana Runica: Corte cuerpo a cuerpo veloz con municion infinita.\n" +
                "• [4] Lanzacohetes: Explosivo de alto dano e impulso radial.",
                m_CardBodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("VOLVER", m_BarButtonStyle, GUILayout.Height(54f)))
            {
                PlayClick();
                m_CurrentPage = SubPage.Home;
            }
        }

        void DrawOptions()
        {
            var persist = PlayerCampaignPersistence.EnsureInstance();

            GUILayout.Label("OPCIONES Y AJUSTES", m_SectionTitleStyle);
            GUILayout.Space(14f);

            GUILayout.BeginVertical(m_CardBoxStyle);

            GUILayout.Label($"VOLUMEN MAESTRO: {Mathf.RoundToInt(persist.MasterVolume * 100f)}%", m_LabelStyle);
            GUILayout.Space(4f);
            float newVol = GUILayout.HorizontalSlider(persist.MasterVolume, 0.0f, 1.0f, GUILayout.Height(26f));
            if (!Mathf.Approximately(newVol, persist.MasterVolume))
            {
                persist.MasterVolume = newVol;
                AudioListener.volume = newVol;
            }

            GUILayout.Space(14f);

            GUILayout.Label($"SENSIBILIDAD DEL RATON: {persist.LookSensitivity:F2}", m_LabelStyle);
            GUILayout.Space(4f);
            persist.LookSensitivity = GUILayout.HorizontalSlider(persist.LookSensitivity, 0.2f, 3.0f, GUILayout.Height(26f));

            GUILayout.Space(14f);

            string lightModeLabel = persist.UseBrightWorkLighting
                ? "ILUMINACION: LUZ PLANA DE TRABAJO (ACTIVO)"
                : "ILUMINACION: ATMOSFERA BONE BREAKER (ACTIVO)";

            if (GUILayout.Button(lightModeLabel, m_BarButtonStyle, GUILayout.Height(50f)))
            {
                PlayClick();
                persist.UseBrightWorkLighting = !persist.UseBrightWorkLighting;
            }

            GUILayout.Space(12f);

            string screenModeLabel = persist.IsFullscreen
                ? "PANTALLA: PANTALLA COMPLETA (ACTIVO)"
                : "PANTALLA: MODO VENTANA (ACTIVO)";

            if (GUILayout.Button(screenModeLabel, m_BarButtonStyle, GUILayout.Height(50f)))
            {
                PlayClick();
                persist.ApplyDisplayMode(!persist.IsFullscreen, true);
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("VOLVER", m_BarButtonStyle, GUILayout.Height(50f)))
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
