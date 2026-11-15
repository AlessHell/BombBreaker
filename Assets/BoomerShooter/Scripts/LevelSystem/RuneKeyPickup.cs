using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    public enum RuneKeyType
    {
        Silver,
        Gold,
        Cosmic
    }

    [RequireComponent(typeof(Collider))]
    public class RuneKeyPickup : MonoBehaviour
    {
        [Tooltip("Type of rune key")]
        public RuneKeyType KeyType = RuneKeyType.Silver;

        [Tooltip("Audio played when picked up")]
        public AudioClip PickupSfx;

        [Tooltip("Rotation speed in degrees per second")]
        public float RotateSpeed = 90f;

        [Tooltip("Bobbing speed and amplitude")]
        public float BobSpeed = 2f;
        public float BobHeight = 0.2f;

        Vector3 m_StartPos;
        bool m_PickedUp;

        void Awake()
        {
            m_StartPos = transform.position;
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            // Rotate and bob
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime, Space.World);
            transform.position = m_StartPos + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobHeight);
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_PickedUp) return;

            var player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                m_PickedUp = true;
                string keyId = KeyType.ToString();

                // Save to persistence
                if (PlayerCampaignPersistence.Instance != null)
                {
                    PlayerCampaignPersistence.Instance.AddKey(keyId);
                }

                // Play audio
                if (PickupSfx != null)
                {
                    AudioSource.PlayClipAtPoint(PickupSfx, transform.position, 1f);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(CreateKeyPickupClip(), transform.position, 0.9f);
                }

                // Broadcast HUD message
                DisplayMessageEvent msg = Events.DisplayMessageEvent;
                msg.Message = $"Obtained {KeyType} Rune Key!";
                msg.DelayBeforeDisplay = 0f;
                EventManager.Broadcast(msg);

                Destroy(gameObject);
            }
        }

        AudioClip CreateKeyPickupClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Harmonic chime (C6 - G6 arpeggio)
            float freq1 = 1046.50f; // C6
            float freq2 = 1567.98f; // G6
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 12f);
                float wave = Mathf.Sin(2 * Mathf.PI * freq1 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * freq2 * t) * 0.4f;
                samples[i] = wave * env;
            }

            AudioClip clip = AudioClip.Create("RuneKeyPickupSound", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
