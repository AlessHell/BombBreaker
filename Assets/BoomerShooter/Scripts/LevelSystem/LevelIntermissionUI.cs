using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    public class LevelIntermissionUI : MonoBehaviour
    {
        bool m_IsVisible;
        string m_LevelName = "";
        string m_TimeString = "00:00";
        string m_KillsString = "0 / 0 (100%)";
        string m_SecretsString = "0 / 0";

        GUIStyle m_TitleStyle;
        GUIStyle m_SubtitleStyle;
        GUIStyle m_StatStyle;
        GUIStyle m_PromptStyle;

        public void Show(string levelName, float timeSeconds, int kills, int totalEnemies, int secrets, int totalSecrets)
        {
            m_IsVisible = true;
            m_LevelName = levelName.ToUpper();

            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeSeconds % 60f);
            m_TimeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            int killPercent = totalEnemies > 0 ? Mathf.RoundToInt(((float)kills / totalEnemies) * 100f) : 100;
            m_KillsString = $"{kills} / {totalEnemies} ({killPercent}%)";

            int secretPercent = totalSecrets > 0 ? Mathf.RoundToInt(((float)secrets / totalSecrets) * 100f) : 100;
            m_SecretsString = $"{secrets} / {totalSecrets} ({secretPercent}%)";
        }

        void OnGUI()
        {
            if (!m_IsVisible) return;

            InitStyles();

            // Dark semi-transparent background
            GUI.color = new Color(0.05f, 0.05f, 0.12f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelWidth = Mathf.Min(650, Screen.width - 40);
            float panelHeight = 420;
            float startX = (Screen.width - panelWidth) * 0.5f;
            float startY = (Screen.height - panelHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, startY, panelWidth, panelHeight));

            GUILayout.Label("LEVEL COMPLETED", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label(m_LevelName, m_SubtitleStyle);
            GUILayout.Space(30);

            DrawStatRow("TIME", m_TimeString);
            GUILayout.Space(12);
            DrawStatRow("KILLS", m_KillsString);
            GUILayout.Space(12);
            DrawStatRow("SECRETS", m_SecretsString);

            GUILayout.Space(40);
            GUILayout.Label("[ PRESS SPACE OR CLICK TO PROCEED ]", m_PromptStyle);

            GUILayout.EndArea();
        }

        void DrawStatRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, m_StatStyle, GUILayout.Width(220));
            GUILayout.Label(value, m_StatStyle);
            GUILayout.EndHorizontal();
        }

        void InitStyles()
        {
            if (m_TitleStyle == null)
            {
                m_TitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 38,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.75f, 0.2f) } // Quake gold
                };

                m_SubtitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.3f, 0.9f, 1f) } // Cosmic cyan
                };

                m_StatStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };

                m_PromptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f, Mathf.PingPong(Time.time * 2f, 1f)) }
                };
            }
        }
    }
}
