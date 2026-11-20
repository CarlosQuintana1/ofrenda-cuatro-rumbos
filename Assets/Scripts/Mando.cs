using UnityEngine;
using UnityEngine.InputSystem;

namespace MemoriaViva
{
    // Control de Xbox, PlayStation o Switch: sticks para caminar y mirar, botones para las acciones.
    public static class Mando
    {
        // Se vuelve true al tocar el mando y false al usar teclado o raton, para cambiar las ayudas en pantalla.
        public static bool EnUso;
        private static int frame = -1;

        // En el editor OpenXR registra su perfil "DPad" y tapa el control "Dpad" del mando; aqui se restaura.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RestaurarDpad() => InputSystem.RegisterLayout<UnityEngine.InputSystem.Controls.DpadControl>("Dpad");

        private static Gamepad Actual
        {
            get
            {
                var g = Gamepad.current;
                if (frame != Time.frameCount)
                {
                    frame = Time.frameCount;
                    if (g != null && (g.wasUpdatedThisFrame && (g.leftStick.ReadValue().sqrMagnitude > .04f || g.rightStick.ReadValue().sqrMagnitude > .04f || AlgunBoton(g)))) EnUso = true;
                    else if (Input.anyKeyDown || Mathf.Abs(Input.GetAxis("Mouse X")) > .5f) EnUso = false;
                }
                return g;
            }
        }

        private static bool AlgunBoton(Gamepad g) =>
            g.buttonSouth.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame ||
            g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame;

        private static Vector2 Zona(Vector2 v) => v.magnitude < .15f ? Vector2.zero : v * ((v.magnitude - .15f) / .85f / v.magnitude);

        public static Vector2 Mover => Actual != null ? Zona(Actual.leftStick.ReadValue()) : Vector2.zero;
        public static Vector2 Mirar => Actual != null ? Zona(Actual.rightStick.ReadValue()) : Vector2.zero;
        public static bool Correr => Actual != null && (Actual.leftStickButton.isPressed || Actual.leftTrigger.ReadValue() > .4f);
        public static bool Interactuar => Actual != null && (Actual.buttonSouth.wasPressedThisFrame || Actual.rightTrigger.wasPressedThisFrame);
        public static bool Luz => Actual != null && Actual.buttonNorth.wasPressedThisFrame;
        public static bool Recorrido => Actual != null && Actual.buttonWest.wasPressedThisFrame;
        public static bool Salir => Actual != null && Actual.buttonEast.wasPressedThisFrame;

        // Vibracion corta al agarrar o activar algo.
        public static void Vibrar(float fuerza, float segundos)
        {
            var g = Gamepad.current;
            if (g == null) return;
            g.SetMotorSpeeds(fuerza * .6f, fuerza);
            Detener.Programar(g, segundos);
        }

        private sealed class Detener : MonoBehaviour
        {
            private static Detener instancia;
            private Gamepad gamepad;
            private float hasta;

            public static void Programar(Gamepad g, float segundos)
            {
                if (instancia == null)
                {
                    var go = new GameObject("Vibracion del mando") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    instancia = go.AddComponent<Detener>();
                }
                instancia.gamepad = g;
                instancia.hasta = Time.unscaledTime + segundos;
            }

            private void Update()
            {
                if (gamepad != null && Time.unscaledTime > hasta)
                {
                    gamepad.SetMotorSpeeds(0, 0);
                    gamepad = null;
                }
            }

            private void OnDisable() => gamepad?.SetMotorSpeeds(0, 0);
        }
    }
}
