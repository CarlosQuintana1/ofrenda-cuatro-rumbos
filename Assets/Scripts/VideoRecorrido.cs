using System.Collections;
using System.IO;
using UnityEngine;

namespace MemoriaViva
{
    // Video del recorrido guiado completo, cuadro por cuadro a 30 fps y con la calidad al maximo, con sus barras y
    // textos: -video <carpeta>; -sin-textos deja la version limpia. Deja JPG numerados.
    public sealed class VideoRecorrido : MonoBehaviour
    {
        private const int Fps = 30;
        private string carpeta;
        private bool subtitulos;

        public static void Grabar(string carpeta, bool subtitulos)
        {
            var v = new GameObject("Video del recorrido").AddComponent<VideoRecorrido>();
            v.carpeta = carpeta;
            v.subtitulos = subtitulos;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            // Calidad maxima: el video no corre en vivo, cada cuadro puede tardar lo que necesite.
            QualitySettings.antiAliasing = 8;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 140;
            QualitySettings.pixelLightCount = 40;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.lodBias = 3;
            VRExplorer.EntradaBloqueada = true;
            var tour = FindFirstObjectByType<RecorridoGuiado>();
            yield return new WaitForSeconds(1.5f);
            RecorridoGuiado.ModoVideo = true;
            RecorridoGuiado.SinTextos = !subtitulos;
            Time.captureFramerate = Fps;
            tour.Iniciar();
            int total = Mathf.FloorToInt(tour.Duracion * Fps) - 1;
            var rt = new RenderTexture(1920, 1080, 0);
            var lectura = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            for (int f = 0; f < total; f++)
            {
                yield return new WaitForEndOfFrame();
                // Se captura al doble y se reduce a 1080p: supermuestreo para bordes limpios.
                var cuadro = ScreenCapture.CaptureScreenshotAsTexture(2);
                Graphics.Blit(cuadro, rt);
                var previo = RenderTexture.active;
                RenderTexture.active = rt;
                lectura.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                RenderTexture.active = previo;
                File.WriteAllBytes(Path.Combine(carpeta, $"{f:0000}.jpg"), lectura.EncodeToJPG(94));
                Destroy(cuadro);
            }
            Time.captureFramerate = 0;
            Debug.Log($"MEMORIA VIVA video: {total} cuadros en {carpeta}");
            Application.Quit();
        }
    }
}
