using Unity.FPS.Game;
using UnityEngine;

namespace BoomerShooter.Audio
{
    public class RetroHitsoundManager : MonoBehaviour
    {
        [Tooltip("Optional custom hit audio clip. If null, a classic retro chime is generated dynamically.")]
        public AudioClip CustomHitClip;

        [Range(0.1f, 2f)]
        public float PitchMin = 0.9f;

        [Range(0.1f, 2f)]
        public float PitchMax = 1.3f;

        [Range(0f, 1f)]
        public float Volume = 0.7f;

        AudioSource m_AudioSource;
        AudioClip m_GeneratedClip;
        GameObject m_Player;

        void Awake()
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 0f; // 2D sound for immediate crisp feedback

            if (CustomHitClip == null)
            {
                m_GeneratedClip = CreateProceduralHitsound();
                m_AudioSource.clip = m_GeneratedClip;
            }
            else
            {
                m_AudioSource.clip = CustomHitClip;
            }
        }

        void OnEnable()
        {
            Damageable.OnAnyDamageInflicted += OnDamageInflicted;
        }

        void OnDisable()
        {
            Damageable.OnAnyDamageInflicted -= OnDamageInflicted;
        }

        void Start()
        {
            var playerController = FindAnyObjectByType<Unity.FPS.Gameplay.PlayerCharacterController>();
            if (playerController != null)
            {
                m_Player = playerController.gameObject;
            }
        }

        void OnDamageInflicted(float damage, GameObject damageSource, GameObject target)
        {
            if (damageSource == null) return;

            // Check if player or child of player caused the damage
            if (m_Player == null)
            {
                var playerController = FindAnyObjectByType<Unity.FPS.Gameplay.PlayerCharacterController>();
                if (playerController != null) m_Player = playerController.gameObject;
            }

            if (m_Player != null && (damageSource == m_Player || damageSource.transform.IsChildOf(m_Player.transform)))
            {
                // Don't play hit sound when damaging yourself
                if (target == m_Player || target.transform.IsChildOf(m_Player.transform))
                    return;

                PlayHitsound(damage);
            }
        }

        public void PlayHitsound(float damage)
        {
            if (m_AudioSource == null) return;

            // Pitch variation based on damage amount (higher damage = higher crisp tone)
            float pitch = Mathf.Lerp(PitchMin, PitchMax, Mathf.Clamp01(damage / 80f));
            m_AudioSource.pitch = pitch;
            m_AudioSource.PlayOneShot(m_AudioSource.clip, Volume);
        }

        AudioClip CreateProceduralHitsound()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Classic Quake / retro chime: high bell overtone with sharp attack and exponential decay
            float freq1 = 880f; // A5
            float freq2 = 1760f; // A6 overtone
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 35f); // Fast decay
                float wave = (Mathf.Sin(2 * Mathf.PI * freq1 * t) * 0.7f) + (Mathf.Sin(2 * Mathf.PI * freq2 * t) * 0.3f);
                samples[i] = wave * envelope;
            }

            AudioClip clip = AudioClip.Create("RetroHitsoundChime", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
