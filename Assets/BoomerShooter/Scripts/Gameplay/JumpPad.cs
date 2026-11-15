using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        [Tooltip("Upward launch impulse force")]
        public float VerticalForce = 20f;

        [Tooltip("Forward launch impulse force based on JumpPad's forward direction")]
        public float ForwardForce = 10f;

        [Tooltip("Audio played when launching")]
        public AudioClip LaunchSfx;

        AudioSource m_AudioSource;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerCharacterController player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                Vector3 launchVel = (Vector3.up * VerticalForce) + (transform.forward * ForwardForce);
                // Zero out downward velocity first
                if (player.CharacterVelocity.y < 0f)
                {
                    player.CharacterVelocity = new Vector3(player.CharacterVelocity.x, 0f, player.CharacterVelocity.z);
                }

                player.AddImpulse(launchVel);

                PlayLaunchSound();
            }
        }

        void PlayLaunchSound()
        {
            if (LaunchSfx != null)
            {
                m_AudioSource.PlayOneShot(LaunchSfx, 0.9f);
            }
            else
            {
                AudioClip proceduralLaunch = CreateLaunchClip();
                m_AudioSource.PlayOneShot(proceduralLaunch, 0.8f);
            }
        }

        AudioClip CreateLaunchClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Rising frequency pitch chirp (classic jump pad whoosh)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((t / duration) * Mathf.PI);
                float freq = Mathf.Lerp(120f, 600f, t / duration);
                samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("JumpPadLaunch", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, (Vector3.up * VerticalForce + transform.forward * ForwardForce).normalized * 3f);
        }
    }
}
