using System.Collections;
using UnityEngine;

namespace MemoriaViva
{
    // Con -captura <archivo.png> el ejecutable guarda una captura a los 15 s y se cierra.
    // Con -recorrido <s> ademas corre el recorrido guiado para que la captura caiga en el segundo s.
    // Con -video <carpeta> graba el video corto del recorrido (VideoRecorrido).
    // Con -activar <titulo> usa ese punto a los 9 s, para ver lo que dispara.
    public sealed class CapturaEjecutable : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            var args = System.Environment.GetCommandLineArgs();
            int vi = System.Array.IndexOf(args, "-video");
            if (vi >= 0 && vi + 1 < args.Length)
            {
                Application.runInBackground = true;
                VideoRecorrido.Grabar(args[vi + 1], System.Array.IndexOf(args, "-sin-textos") < 0);
                return;
            }
            int pf = System.Array.IndexOf(args, "-perfil");
            if (pf >= 0 && pf + 1 < args.Length)
            {
                Application.runInBackground = true;
                var m = new GameObject("Perfil").AddComponent<CapturaEjecutable>();
                m.StartCoroutine(Perfil(args[pf + 1]));
                return;
            }
            int i = System.Array.IndexOf(args, "-captura");
            if (i < 0 || i + 1 >= args.Length) return;
            // Sin foco Unity se pausa y la captura nunca llegaba.
            Application.runInBackground = true;
            var go = new GameObject("Captura").AddComponent<CapturaEjecutable>();
            go.StartCoroutine(Capturar(args[i + 1]));
            int r = System.Array.IndexOf(args, "-recorrido");
            if (r >= 0 && r + 1 < args.Length && float.TryParse(args[r + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s))
                go.StartCoroutine(Recorrido(s));
            int a = System.Array.IndexOf(args, "-activar");
            if (a >= 0 && a + 1 < args.Length) go.StartCoroutine(Activar(args[a + 1]));
        }

        // Con -perfil <prefijo> corre el recorrido completo en tiempo real y anota cada cuadro lento con una captura.
        private static IEnumerator Perfil(string prefijo)
        {
            yield return new WaitForSeconds(2f);
            var tour = FindFirstObjectByType<RecorridoGuiado>();
            tour.Iniciar();
            float inicio = Time.unscaledTime;
            int capturas = 0;
            while (tour.Activo)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                if (dt > .1f)
                {
                    Debug.Log($"MEMORIA VIVA LENTO {dt * 1000:0} ms en t={Time.unscaledTime - inicio:0.0} toma {tour.Paso + 1}");
                    if (capturas < 6) ScreenCapture.CaptureScreenshot($"{prefijo}{capturas++}.png");
                }
            }
            Debug.Log($"MEMORIA VIVA fin del recorrido en {Time.unscaledTime - inicio:0.0} s");
            Application.Quit();
        }

        private static IEnumerator Activar(string titulo)
        {
            yield return new WaitForSeconds(9f);
            var explorer = FindFirstObjectByType<VRExplorer>();
            foreach (var wi in FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None))
                if (wi.title == titulo) { wi.Activate(explorer); break; }
        }

        private static IEnumerator Recorrido(float s)
        {
            yield return new WaitForSeconds(1f);
            FindFirstObjectByType<RecorridoGuiado>()?.Iniciar(Mathf.Max(0, s - 14f));
        }

        private static IEnumerator Capturar(string archivo)
        {
            yield return new WaitForSeconds(15f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(archivo);
            yield return new WaitForSeconds(2f);
            Debug.Log($"MEMORIA VIVA: {1f / Time.smoothDeltaTime:0} FPS");
            Application.Quit();
        }
    }
}
