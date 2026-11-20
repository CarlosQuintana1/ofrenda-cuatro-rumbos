using UnityEngine;
using UnityEngine.XR;

namespace MemoriaViva
{
    // Bloom ligero en la camara de escritorio; en visor se apaga para no romper el render estereo.
    [RequireComponent(typeof(Camera))]
    public sealed class BrilloNocturno : MonoBehaviour
    {
        public float intensity = .4f;
        public float threshold = 1.35f;
        private Material mat;
        private readonly RenderTexture[] chain = new RenderTexture[6];

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (XRSettings.enabled || intensity <= .001f)
            {
                Graphics.Blit(src, dst);
                return;
            }
            if (mat == null) mat = new Material(Shader.Find("Hidden/MemoriaViva/Bloom")) { hideFlags = HideFlags.HideAndDontSave };
            mat.SetFloat("_Threshold", threshold);
            mat.SetFloat("_Intensity", intensity);
            int w = src.width / 2, h = src.height / 2, n = 0;
            RenderTexture cur = RenderTexture.GetTemporary(w, h, 0, src.format);
            Graphics.Blit(src, cur, mat, 0);
            chain[n++] = cur;
            while (n < chain.Length && h > 8)
            {
                w /= 2; h /= 2;
                var next = RenderTexture.GetTemporary(w, h, 0, src.format);
                Graphics.Blit(cur, next, mat, 1);
                chain[n++] = cur = next;
            }
            for (int i = n - 2; i >= 0; i--)
            {
                Graphics.Blit(cur, chain[i], mat, 2);
                cur = chain[i];
            }
            mat.SetTexture("_BloomTex", cur);
            Graphics.Blit(src, dst, mat, 3);
            for (int i = 0; i < n; i++) RenderTexture.ReleaseTemporary(chain[i]);
        }
    }
}
