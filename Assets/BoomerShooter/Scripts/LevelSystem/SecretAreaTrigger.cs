using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    [RequireComponent(typeof(Collider))]
    public class SecretAreaTrigger : MonoBehaviour
    {
        [Tooltip("Name or description of the secret area")]
        public string SecretName = "Hidden Cosmic Relic";

        [Tooltip("Audio played when secret is discovered")]
        public AudioClip SecretSfx;

        bool m_Discovered;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Discovered) return;

            var player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                m_Discovered = true;

                // Register with LevelManager
                var lm = FindAnyObjectByType<BoomerShooterLevelManager>();
                if (lm != null)
                {
                    lm.RegisterSecretFound();
                }

                // Play secret discovery chime
                if (SecretSfx != null)
                {
                    AudioSource.PlayClipAtPoint(SecretSfx, transform.position, 1f);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(CreateSecretChimeClip(), transform.position, 0.9f);
                }

                // Broadcast HUD message
                DisplayMessageEvent msg = Events.DisplayMessageEvent;
                msg.Message = "Secret Area Found!";
                msg.DelayBeforeDisplay = 0f;
                EventManager.Broadcast(msg);
            }
        }

        AudioClip CreateSecretChimeClip()
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Classic Quake / Wolfenstein secret fanfare: C5 -> E5 -> G5 -> C6 arpeggio
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, notes.Length - 1);
                float noteT = t - (noteIdx * noteDuration);
                float env = Mathf.Exp(-noteT * 8f);
                float wave = Mathf.Sin(2 * Mathf.PI * notes[noteIdx] * t);
                samples[i] = wave * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SecretFoundChime", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
