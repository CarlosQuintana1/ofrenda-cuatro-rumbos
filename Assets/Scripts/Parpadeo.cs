using UnityEngine;

namespace MemoriaViva
{
    // Titileo de las luces de vela y farol.
    public sealed class Parpadeo : MonoBehaviour
    {
        [SerializeField] private float amount = .25f;
        [SerializeField] private float speed = 6f;
        private Light lightSource;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource != null ? lightSource.intensity : 1;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            if (lightSource == null) return;
            float n = Mathf.PerlinNoise(Time.time * speed, seed) - .5f;
            lightSource.intensity = baseIntensity * (1 + n * 2 * amount);
        }
    }
}
