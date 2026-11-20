using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MemoriaViva
{
    // Para el stand: R o Start reinician la ofrenda entre un juez y otro (los nombres se conservan),
    // y tras 45 s sin tocar nada corre el recorrido guiado en bucle; cualquier tecla devuelve el control.
    public sealed class Presentacion : MonoBehaviour
    {
        private const float Espera = 45f;
        private float ultimoUso;
        public static bool EnEspera { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (FindFirstObjectByType<VRExplorer>() == null) return;
            // Siempre en pantalla completa, a lo mas 1920 de ancho para que en pantallas Retina vaya fluido;
            // -screen-fullscreen 0 lo deja en ventana para pruebas.
            if (!Application.isEditor && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-screen-fullscreen") < 0)
            {
                float k = Mathf.Min(1f, 1920f / Display.main.systemWidth);
                Screen.SetResolution(Mathf.RoundToInt(Display.main.systemWidth * k), Mathf.RoundToInt(Display.main.systemHeight * k), FullScreenMode.FullScreenWindow);
            }
            var go = new GameObject("Presentacion | R reinicia");
            DontDestroyOnLoad(go);
            go.AddComponent<Presentacion>();
        }

        private void OnEnable() => SceneManager.sceneLoaded += AlCargar;
        private void OnDisable() => SceneManager.sceneLoaded -= AlCargar;

        private void AlCargar(Scene s, LoadSceneMode m)
        {
            ultimoUso = Time.unscaledTime;
            // Si la escena se recargo en modo espera, el recorrido sigue solo.
            if (EnEspera) Invoke(nameof(Arrancar), .5f);
        }

        private void Start() => ultimoUso = Time.unscaledTime;

        private void Update()
        {
            var g = Gamepad.current;
            bool uso = Input.anyKeyDown || Input.GetMouseButtonDown(0) || Mathf.Abs(Input.GetAxis("Mouse X")) > .3f ||
                       (g != null && (g.leftStick.ReadValue().sqrMagnitude > .04f || g.rightStick.ReadValue().sqrMagnitude > .04f || g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
            bool reiniciar = (!VRExplorer.EntradaBloqueada && Input.GetKeyDown(KeyCode.R)) || (g != null && g.startButton.wasPressedThisFrame);
            var tour = FindFirstObjectByType<RecorridoGuiado>();

            if (EnEspera)
            {
                // Alguien llego: se limpia la ofrenda y se le da el control.
                if (uso) { EnEspera = false; Recargar(); return; }
                if (tour != null && !tour.Activo && !IsInvoking(nameof(Arrancar))) Recargar();
                return;
            }
            if (reiniciar) { Recargar(); return; }
            if (uso || (tour != null && tour.Activo) || VRExplorer.EntradaBloqueada) ultimoUso = Time.unscaledTime;
            else if (Time.unscaledTime - ultimoUso > Espera) { EnEspera = true; Arrancar(); }
        }

        private void Arrancar() => FindFirstObjectByType<RecorridoGuiado>()?.Iniciar();

        private void Recargar()
        {
            VRExplorer.EntradaBloqueada = false;
            WorldInteractable.Reiniciar();
#if UNITY_EDITOR
            // En el editor la escena puede no estar en la lista de compilacion.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(0);
#endif
        }
    }
}
