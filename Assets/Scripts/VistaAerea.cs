using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace MemoriaViva
{
    // Al conocer un campo de flores la camara sube a verlo desde arriba unos segundos y regresa.
    public sealed class VistaAerea : MonoBehaviour
    {
        public Vector3 centro;
        public float alto = 13f, atras = 6f;

        private const float Subir = 2.2f, Quedarse = 6f, Bajar = 1.8f;
        private static bool activa;
        private string titulo, texto;
        private float inicio;
        private GUIStyle tituloSt, textoSt, barraSt;

        public static bool Activa => activa;

        public static bool EnVisor()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool t) && t;
        }

        public void Mostrar(VRExplorer explorer, string tit, string desc)
        {
            if (activa || explorer == null) return;
            int f = desc.LastIndexOf("(Fuente");
            titulo = tit;
            texto = f > 0 ? desc.Substring(0, f).TrimEnd() : desc;
            StartCoroutine(Volar(explorer));
        }

        private static bool Tecla()
        {
            var g = Gamepad.current;
            return Input.anyKeyDown || Input.GetMouseButtonDown(0) ||
                   (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
        }

        private IEnumerator Volar(VRExplorer explorer)
        {
            var cam = Camera.main;
            activa = true;
            inicio = Time.unscaledTime;
            VRExplorer.EntradaBloqueada = true;
            explorer.enabled = false;
            Vector3 p0 = cam.transform.position, pLocal = cam.transform.localPosition;
            Quaternion r0 = cam.transform.rotation, rLocal = cam.transform.localRotation;
            // Se mira desde el sur, que es como se leen los campos desde la plaza.
            Vector3 arriba = centro + new Vector3(0, alto, -atras), mira = centro + Vector3.forward * .6f;
            Quaternion rArriba = Quaternion.LookRotation(mira - arriba);
            AmbienteSonoro.Efecto("viaje", .6f);

            float t = 0;
            for (; t < Subir; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / Subir);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(p0, arriba, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2f, Quaternion.Slerp(r0, rArriba, k));
                yield return null;
            }
            for (t = 0; t < Quedarse; t += Time.deltaTime)
            {
                // Un giro lento alrededor del campo mientras se lee el texto.
                Vector3 orb = Quaternion.Euler(0, Mathf.Sin(t / Quedarse * Mathf.PI) * 14f, 0) * (arriba - centro);
                cam.transform.SetPositionAndRotation(centro + orb, Quaternion.LookRotation(mira - (centro + orb)));
                if (t > .4f && Tecla()) break;
                yield return null;
            }
            Vector3 p1 = cam.transform.position;
            Quaternion r1 = cam.transform.rotation;
            for (t = 0; t < Bajar; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / Bajar);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(p1, p0, k), Quaternion.Slerp(r1, r0, k));
                yield return null;
            }
            cam.transform.localPosition = pLocal;
            cam.transform.localRotation = rLocal;
            explorer.enabled = true;
            VRExplorer.EntradaBloqueada = false;
            activa = false;
        }

        private void OnGUI()
        {
            if (!activa || titulo == null) return;
            if (tituloSt == null)
            {
                var fuente = Font.CreateDynamicFontFromOSFont(new[] { "Avenir Next", "Helvetica Neue", "Arial" }, 20);
                tituloSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                tituloSt.normal.textColor = new Color(1f, .66f, .25f);
                textoSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 23, alignment = TextAnchor.UpperCenter, wordWrap = true };
                textoSt.normal.textColor = new Color(.97f, .94f, .88f);
                barraSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 16, alignment = TextAnchor.MiddleLeft };
                barraSt.normal.textColor = new Color(.85f, .81f, .74f);
            }
            float k = Mathf.Max(.75f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1));
            float sw = Screen.width / k, sh = Screen.height / k;
            float dentro = Time.unscaledTime - inicio;
            float abre = Mathf.SmoothStep(0, 1, dentro / .8f);
            float alto = 130 * abre;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, sw, alto), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh - alto, sw, alto), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, abre);
            float y = alto - 80;
            tituloSt.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(48, y, 400, 30), "VISTA DESDE ARRIBA", tituloSt);
            tituloSt.alignment = TextAnchor.UpperCenter;
            barraSt.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(sw - 548, y, 500, 30), "Cualquier tecla para volver", barraSt);
            float ancho = Mathf.Min(1100, sw - 96), x = sw / 2 - ancho / 2;
            float bh = textoSt.CalcHeight(new GUIContent(texto), ancho);
            float yb = sh - alto + (130 - 24 - bh) / 2;
            VRExplorer.Etiqueta(new Rect(x, yb, ancho, 22), titulo.ToUpper(), tituloSt);
            VRExplorer.Etiqueta(new Rect(x, yb + 24, ancho, bh), texto, textoSt);
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
