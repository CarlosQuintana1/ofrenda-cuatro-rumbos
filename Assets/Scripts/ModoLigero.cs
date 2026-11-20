using UnityEngine;
using UnityEngine.InputSystem;

namespace MemoriaViva
{
    // Tecla P o boton Select: quita sombras, antialias y bloom y baja la resolucion para equipos lentos.
    public sealed class ModoLigero : MonoBehaviour
    {
        private bool ligero, avisado;
        private float fps = 60, inicio;
        private int ancho, alto;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (FindFirstObjectByType<VRExplorer>() == null) return;
            var go = new GameObject("Modo ligero | tecla P");
            DontDestroyOnLoad(go);
            go.AddComponent<ModoLigero>();
        }

        private void Start()
        {
            inicio = Time.time;
            ancho = Screen.width;
            alto = Screen.height;
        }

        private void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, .001f), .05f);
            bool boton = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
            if ((!VRExplorer.EntradaBloqueada && Input.GetKeyDown(KeyCode.P)) || boton) Cambiar(!ligero);
            if (!avisado && !ligero && Time.time - inicio > 10 && fps < 35)
            {
                avisado = true;
                FindFirstObjectByType<VRExplorer>()?.ShowMessage("¿Va lento?", "Presiona P (o Select en el mando) para activar el modo ligero: sin sombras y con menos resolución.", 8f);
            }
        }

        private void Cambiar(bool valor)
        {
            ligero = valor;
            QualitySettings.shadows = ligero ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.pixelLightCount = ligero ? 3 : 12;
            QualitySettings.antiAliasing = ligero ? 0 : 4;
            QualitySettings.shadowDistance = ligero ? 40 : 90;
            var brillo = FindFirstObjectByType<BrilloNocturno>();
            if (brillo != null) brillo.enabled = !ligero;
            if (!Application.isEditor && Screen.fullScreen)
                Screen.SetResolution(ligero ? ancho * 3 / 4 : ancho, ligero ? alto * 3 / 4 : alto, true);
            FindFirstObjectByType<VRExplorer>()?.ShowMessage(ligero ? "Modo ligero" : "Calidad completa",
                ligero ? "Sin sombras ni antialias y con menos resolución. Presiona P para volver." : "Sombras, antialias y bloom activados. Presiona P para el modo ligero.", 4f);
        }
    }
}
