using System.Collections.Generic;
using UnityEngine;

namespace MemoriaViva
{
    // Quien visita escribe el nombre de alguien que recuerda y se enciende una veladora con ese nombre.
    public sealed class OfrendaVisitantes : MonoBehaviour
    {
        private const string Clave = "MemoriaViva.Visitantes";
        [SerializeField] private GameObject plantilla;
        [SerializeField] private Vector3[] lugares;
        [SerializeField] private Vector3[] miradas;
        private readonly List<string> nombres = new List<string>();
        private bool escribiendo;
        private string borrador = "";

        private void Start()
        {
            if (plantilla != null) plantilla.SetActive(false);
            string guardado = PlayerPrefs.GetString(Clave, "");
            foreach (var n in guardado.Split('\n'))
                if (!string.IsNullOrWhiteSpace(n)) Encender(n, false);
        }

        private void Update()
        {
            if (!escribiendo && !VRExplorer.EntradaBloqueada && Input.GetKeyDown(KeyCode.O))
            {
                escribiendo = true;
                borrador = "";
                VRExplorer.EntradaBloqueada = true;
            }
        }

        public void AgregarNombre(string nombre) => Encender(nombre, true);

        private void Encender(string nombre, bool guardar)
        {
            if (plantilla == null || lugares == null || lugares.Length == 0) return;
            int i = nombres.Count % lugares.Length;
            nombres.Add(nombre);
            var v = Instantiate(plantilla, transform);
            v.name = "Veladora de " + nombre;
            v.transform.position = lugares[i];
            v.transform.rotation = Quaternion.LookRotation(miradas[i]);
            v.SetActive(true);
            var texto = v.GetComponentInChildren<TextMesh>();
            if (texto != null) texto.text = nombre;
            if (guardar)
            {
                PlayerPrefs.SetString(Clave, string.Join("\n", nombres));
                PlayerPrefs.Save();
                FindFirstObjectByType<VRExplorer>()?.ShowMessage("Una luz más en la ofrenda", "La veladora de " + nombre + " quedó encendida junto al altar.", 6f);
                AmbienteSonoro.Efecto("encender", .8f);
                FindFirstObjectByType<AmbienteSonoro>()?.Ritual(-1);
            }
        }

        private void OnGUI()
        {
            if (!escribiendo) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                string n = borrador.Trim();
                if (n.Length > 0) Encender(n.Length > 28 ? n.Substring(0, 28) : n, true);
                Cerrar();
                e.Use();
                return;
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { Cerrar(); e.Use(); return; }
            var box = new Rect(Screen.width / 2f - 260, Screen.height / 2f - 80, 520, 160);
            GUI.color = new Color(0, 0, 0, .75f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = new Color(1f, .93f, .8f);
            GUI.Label(new Rect(box.x + 20, box.y + 14, box.width - 40, 30), "¿A quién recuerdas?", new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.SetNextControlName("nombre");
            borrador = GUI.TextField(new Rect(box.x + 20, box.y + 56, box.width - 40, 36), borrador, 28, new GUIStyle(GUI.skin.textField) { fontSize = 20 });
            GUI.FocusControl("nombre");
            GUI.Label(new Rect(box.x + 20, box.y + 104, box.width - 40, 40), "Enter para encender su veladora · Esc para cancelar", new GUIStyle(GUI.skin.label) { fontSize = 14 });
        }

        private void Cerrar()
        {
            escribiendo = false;
            VRExplorer.EntradaBloqueada = false;
        }

        public void Setup(GameObject p, Vector3[] l, Vector3[] m) { plantilla = p; lugares = l; miradas = m; }
    }
}
