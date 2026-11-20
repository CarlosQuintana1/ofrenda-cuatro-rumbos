using UnityEngine;

namespace MemoriaViva
{
    // Pone en cada marco la foto que le toca desde Assets/Resources/Fotos; sin foto queda la silueta.
    public sealed class RetratosOfrenda : MonoBehaviour
    {
        [SerializeField] private Renderer[] frames;
        [SerializeField] private string[] fotos;

        private void Start()
        {
            if (frames == null || fotos == null) return;
            for (int i = 0; i < frames.Length && i < fotos.Length; i++)
            {
                var foto = Resources.Load<Texture2D>("Fotos/" + fotos[i]);
                if (frames[i] != null && foto != null) frames[i].material.mainTexture = foto;
            }
        }

        public void Setup(Renderer[] f, string[] nombres)
        {
            frames = f;
            fotos = nombres;
        }
    }
}
