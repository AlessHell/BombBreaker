using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    [RequireComponent(typeof(Collider))]
    public class TutorialZoneTrigger : MonoBehaviour
    {
        public static TutorialZoneTrigger ActiveTrigger { get; private set; }

        [Header("Tutorial Prompt")]
        public string ZoneTitle = "TUTORIAL";
        [TextArea(2, 4)]
        public string InstructionText = "Use WASD to move and SPACE to jump.";
        public float DisplayDuration = 7f;
        public bool TriggerOnce = false;

        bool m_HasTriggered;
        float m_HideTime;
        GUIStyle m_BoxStyle;
        GUIStyle m_TitleStyle;
        GUIStyle m_BodyStyle;

        void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (TriggerOnce && m_HasTriggered) return;

            var player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                m_HasTriggered = true;
                ActiveTrigger = this;
                m_HideTime = Time.time + DisplayDuration;
            }
        }

        void OnGUI()
        {
            if (ActiveTrigger != this) return;
            if (Time.time > m_HideTime) return;
            if (BoomerShooterLevelManager.Instance != null && BoomerShooterLevelManager.Instance.IsLevelComplete) return;

            if (m_TitleStyle == null)
            {
                m_BoxStyle = new GUIStyle(GUI.skin.box);
                m_TitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.85f, 0.25f) }
                };
                m_BodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            float width = Mathf.Min(620f, Screen.width - 60f);
            float height = 88f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - height - 110f;

            GUI.color = new Color(0.05f, 0.06f, 0.14f, 0.85f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, m_BoxStyle);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 16f, y + 10f, width - 32f, height - 20f));
            GUILayout.Label(ZoneTitle, m_TitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(InstructionText, m_BodyStyle);
            GUILayout.EndArea();
        }
    }
}
