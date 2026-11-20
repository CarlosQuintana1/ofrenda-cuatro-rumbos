using UnityEngine;

namespace MemoriaViva
{
    // Cada tira de papel picado se mece alrededor de la cuerda que la sostiene.
    public sealed class PapelPicadoViento : MonoBehaviour
    {
        [SerializeField] private Transform[] strings;
        [SerializeField] private Vector3[] axes;
        [SerializeField] private float amplitude = 9f;
        private Quaternion[] rest;

        private void Awake()
        {
            if (strings == null) return;
            rest = new Quaternion[strings.Length];
            for (int i = 0; i < strings.Length; i++) rest[i] = strings[i].localRotation;
        }

        private void Update()
        {
            if (strings == null || rest == null) return;
            float t = Time.time;
            for (int i = 0; i < strings.Length; i++)
            {
                if (strings[i] == null) continue;
                float gust = Mathf.PerlinNoise(t * .25f, i * .37f);
                float angle = (Mathf.Sin(t * 1.3f + i * .9f) * .6f + Mathf.Sin(t * 2.7f + i * 1.7f) * .4f) * amplitude * (.4f + gust);
                strings[i].localRotation = Quaternion.AngleAxis(angle, axes[i]) * rest[i];
            }
        }

        public void Setup(Transform[] s, Vector3[] a)
        {
            strings = s;
            axes = a;
        }
    }
}
