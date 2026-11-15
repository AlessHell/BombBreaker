using UnityEngine;

namespace BoomerShooter.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class GibPiece : MonoBehaviour
    {
        [Tooltip("Lifetime in seconds before shrinking and destroying")]
        public float Lifetime = 5f;

        [Tooltip("Audio played when hitting surfaces")]
        public AudioClip SplatSfx;

        Rigidbody m_Rb;
        float m_SpawnTime;
        bool m_Shrinking;

        void Awake()
        {
            m_Rb = GetComponent<Rigidbody>();
            m_SpawnTime = Time.time;
        }

        public void Initialize(Vector3 velocity, Vector3 angularVelocity)
        {
            if (m_Rb == null) m_Rb = GetComponent<Rigidbody>();
            m_Rb.linearVelocity = velocity;
            m_Rb.angularVelocity = angularVelocity;
        }

        void Update()
        {
            float age = Time.time - m_SpawnTime;
            if (age >= Lifetime && !m_Shrinking)
            {
                m_Shrinking = true;
            }

            if (m_Shrinking)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 4f * Time.deltaTime);
                if (transform.localScale.x <= 0.05f)
                {
                    Destroy(gameObject);
                }
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 2f)
            {
                // Play squish SFX if assigned
                if (SplatSfx != null)
                {
                    AudioSource.PlayClipAtPoint(SplatSfx, collision.contacts[0].point, 0.4f);
                }
            }
        }
    }
}
