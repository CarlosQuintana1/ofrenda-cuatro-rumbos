using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace MemoriaViva
{
    // Recorrido automatico para presentar sin aprender los controles. Tecla T o boton B del control.
    public sealed class RecorridoGuiado : MonoBehaviour
    {
        private struct Toma
        {
            public Vector3 pos, mira;
            public float dur;
            public string titulo, texto;
            public int ritual, ambiente;
            public bool fluida;
        }

        private readonly List<Toma> tomas = new List<Toma>();
        private VRExplorer explorer;
        private Camera cam;
        private AudioSource narrador;
        private bool activo, lastButton;
        private float t;
        private int idx;
        private string titulo = "", subtitulo = "";
        private float inicio, cambio;
        private Vector3 rigPos;
        private Quaternion rigRot, camRot;

        private void Agregar(Vector3 p, Vector3 m, float d, string tit, string txt, int ritual = -1, int amb = -1)
            => tomas.Add(new Toma { pos = p, mira = m, dur = d, titulo = tit, texto = txt, ritual = ritual, ambiente = amb });

        // Toma de grua: la camara viaja toda la toma sin detenerse y enlaza con la siguiente; sin titulo conserva el texto anterior.
        private Toma Fluida(Vector3 p, Vector3 m, float d, string tit = null, string txt = null)
            => new Toma { pos = p, mira = m, dur = d, titulo = tit, texto = txt, ritual = -1, ambiente = -1, fluida = true };

        // Video de 1:27 como maximo: apertura desde el arco de entrada por el camino, voltea a las mariposas, sube en grua a la
        // panoramica de la Facultad y baja a la ofrenda; luego las mejores tomas del recorrido, rodeando la piramide por el poniente
        // y el sur, hasta el troyano.
        private bool apertura;
        private void AperturaVideo()
        {
            if (apertura) return;
            apertura = true;
            Vector3 inicio = new Vector3(-10.64f, 0, 51.83f), dir = new Vector3(.195f, 0, -.981f), c = new Vector3(0, 1.6f, 0);
            Vector3 Camino(float s, float y) => inicio + dir * s + Vector3.up * y;
            var recorrido = new List<Toma>(tomas);
            tomas.Clear();
            tomas.AddRange(new[]
            {
                new Toma { pos = Camino(0, 1.65f), mira = Camino(30, 1.4f), dur = 3, titulo = "Bienvenidos", texto = "Un camino de papel picado, tapetes y veladoras lleva hasta la ofrenda.", ritual = -1, ambiente = recorrido[0].ambiente },
                Fluida(Camino(12, 1.5f), Camino(34, 1.3f), 5.5f),
                Fluida(Camino(21, 2.2f), new Vector3(-1, 1f, 26), 5.5f, "Mariposas monarca", "Para los purépechas son las almas que regresan a visitar a sus familias."),
                Fluida(new Vector3(-12, 11, 31), new Vector3(0, 7, -27), 5.5f, "Facultad de Informática", "Esta es la explanada de la Facultad, en el Campus Juriquilla de la UAQ."),
                Fluida(new Vector3(9, 14, 19), new Vector3(0, 5, -22), 5.5f),
                Fluida(new Vector3(11, 8, 15), c, 5.5f, "La ofrenda", "En el medallón central se levanta la Ofrenda de los Cuatro Rumbos."),
            });
            var seleccion = new (string titulo, float dur)[]
            {
                ("La Catrina y el Catrín", 5), ("Norte · Fuego", 6), ("Poniente · Tierra", 6), ("Juan Gabriel", 5), ("Cae la noche", 4.5f),
                ("Mariachi", 5), ("El camino de cempasúchil", 5), ("Danzantes", 5), ("Nuestro máximo Troyano", 6), ("Siempre con nosotros", 9),
            };
            foreach (var (titulo, dur) in seleccion)
            {
                var tm = recorrido.Find(x => x.titulo == titulo);
                tm.dur = dur;
                tomas.Add(tm);
            }
            total = 0;
        }

        private void Start()
        {
            explorer = FindFirstObjectByType<VRExplorer>();
            cam = Camera.main;
            Vector3 c = new Vector3(0, 1.6f, 0);
            // El orden le da la vuelta a la ofrenda: ningun tramo pasa por en medio de la piramide.
            Agregar(new Vector3(-10.6f, 1.7f, 51.8f), c + Vector3.up * 1.5f, 6, "Facultad de Informática", "Esta es la explanada de la Facultad, en el Campus Juriquilla de la UAQ.", -1, 0);
            Agregar(new Vector3(4.5f, 1.8f, 24.5f), new Vector3(0, .9f, 29), 6, "Mariposas monarca", "Para los purépechas son las almas que regresan a visitar a sus familias.");
            Agregar(new Vector3(14, 12, 30), c, 7, "La ofrenda", "En el medallón central se levanta la Ofrenda de los Cuatro Rumbos.");
            Agregar(new Vector3(18, 7, 4), c, 6, "Siete niveles", "Suben desde los cuatro lados, para recorrerla desde cualquier punto de la explanada.");
            Agregar(new Vector3(9, 3.2f, -9), c, 6, "Cuatro rumbos", "Cada cara mira a un rumbo, guarda un elemento y recuerda a un grupo de personas.");
            Agregar(new Vector3(0, 1.7f, 8), new Vector3(0, 1.4f, 0), 7, "Norte · Fuego", "Las veladoras guían a las ánimas a su antiguo hogar. Aquí están las voces de la canción mexicana.", 0);
            Agregar(new Vector3(8, 1.7f, .5f), new Vector3(0, 1.3f, 0), 7, "Oriente · Agua", "El agua calma la sed de las ánimas después del largo recorrido. Aquí está el cine mexicano.", 1);
            Agregar(new Vector3(0, 2.2f, -8.5f), new Vector3(0, 2.6f, 0), 7, "Sur · Viento", "El papel picado lleva FIF y UAQ cortadas en binario. Aquí, la historia y la cultura de México.", 3);
            Agregar(new Vector3(-8, 1.7f, -.5f), new Vector3(0, 1.3f, 0), 7, "Poniente · Tierra", "Pan, fruta, mole y tamales para el ánima. Aquí, la televisión y un chihuahua por los amigos de cuatro patas que se nos adelantaron.", 2);
            Agregar(new Vector3(-4.6f, 1.25f, -1.3f), new Vector3(-3.2f, .9f, -.2f), 6, "Calaveritas y pan", "Calaveritas de azúcar, pan de muerto y fruta de temporada.");
            Agregar(new Vector3(-1.2f, .95f, 5.6f), new Vector3(0, .7f, 4.05f), 5, "Copal", "Su humo limpia el lugar para que el alma pueda entrar.");
            Agregar(new Vector3(-2.4f, .7f, 5.8f), new Vector3(-1.75f, .55f, 4.35f), 5, "Xoloitzcuintle", "Ayuda a las almas a cruzar el río hacia el Mictlán.");
            Agregar(new Vector3(0, 3.6f, 4.6f), new Vector3(0, 3.5f, 0), 6, "Juan Gabriel", "En lo más alto, mira hacia los cuatro rumbos.");
            Agregar(new Vector3(0, 2.4f, 16f), new Vector3(0, 2.2f, 9.3f), 6, "La Catrina y el Catrín", "Reciben a quien llega a la plaza.");
            Agregar(new Vector3(-10, 5, -6), c, 5, "Cae la noche", "Las veladoras alumbran el camino de las ánimas.", -1, 1);
            Agregar(new Vector3(-12f, 1.9f, 1f), new Vector3(-17.1f, 1.5f, 1.5f), 6, "Mariachi", "Un mariachi de catrines toca para las ánimas.");
            Agregar(new Vector3(0, 3.8f, -20f), c + Vector3.up, 9, "El camino de cempasúchil", "Con los cuatro elementos, las ánimas siguen el camino de flores hasta la ofrenda.");
            Agregar(new Vector3(12f, 1.9f, -1.5f), new Vector3(17.5f, 1.2f, -2.1f), 6, "Danzantes", "Bailan al ritmo del huéhuetl.");
            Agregar(new Vector3(6, 2, 9), new Vector3(0, 2.2f, 0), 9, "Tu turno", "Deja encendida una veladora con el nombre de alguien que extrañas: presiona O.");
            // Al final, el catrin troyano del techo y la vista desde su espalda hacia toda la Facultad.
            Agregar(new Vector3(4.1f, 9f, -16f), new Vector3(-1.03f, 19.5f, -29.56f), 8, "Nuestro máximo Troyano", "Por último, desde lo alto, nuestro máximo Troyano cuida de la Facultad.");
            Agregar(new Vector3(2.48f, 26f, -35.41f), new Vector3(0, 1.5f, 0), 12, "Siempre con nosotros", "En memoria de todos nuestros troyanos, que nos cuidan desde los cielos, siempre con alegría.");
        }

        private void Update()
        {
            bool button = false;
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.isValid) right.TryGetFeatureValue(CommonUsages.secondaryButton, out button);
            bool pressed = (!VRExplorer.EntradaBloqueada && (Input.GetKeyDown(KeyCode.T) || Mando.Recorrido)) || (button && !lastButton);
            lastButton = button;
            if (pressed) { if (activo) Terminar(); else Iniciar(); }
            if (!activo) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Mando.Salir) { Terminar(); return; }

            t += Time.deltaTime;
            while (idx < tomas.Count - 1 && t > tomas[idx].dur)
            {
                t -= tomas[idx].dur;
                idx++;
                Entrar(idx);
            }
            if (idx >= tomas.Count - 1 && t > tomas[idx].dur) { Terminar(); return; }
            // Cinematografico: el viaje dura segun la distancia, arranca y frena suave, y la mirada sigue la misma curva.
            int n = tomas.Count - 1;
            bool previaFluida = idx > 0 && tomas[idx - 1].fluida, siguienteFluida = idx < n && tomas[idx + 1].fluida;
            // Una toma normal que sigue a una de grua arranca donde esta termino, sin el 10 % de deriva.
            float k0 = previaFluida ? 0 : .1f;
            float distancia = idx == 0 ? 0 : Vector3.Distance(tomas[idx - 1].pos, tomas[idx].pos);
            float viaje = idx == 0 ? 0 : tomas[idx].fluida ? tomas[idx].dur : Mathf.Clamp(distancia / 5f, 2.5f, tomas[idx].dur * .6f);
            int desde = t < viaje ? idx - 1 : idx;
            float s = t < viaje ? t / viaje : 0;
            float suave = s * s * s * (s * (s * 6 - 15) + 10);
            // En la grua solo frena al entrar y al salir de la cadena; en medio va a velocidad pareja.
            if (tomas[idx].fluida && idx > 0)
                suave = !previaFluida && !siguienteFluida ? suave : !previaFluida ? s * s * (2 - s) : !siguienteFluida ? s + s * s - s * s * s : s;
            float deriva = t < viaje ? 0 : Mathf.SmoothStep(0, 1, (t - viaje) / Mathf.Max(.01f, tomas[idx].dur - viaje));
            float k = t < viaje ? k0 + (1 - k0) * suave : .1f * deriva;
            Toma p0 = tomas[Mathf.Max(desde - 1, 0)], a = tomas[desde], b = tomas[Mathf.Min(desde + 1, n)], p3 = tomas[Mathf.Min(desde + 2, n)];
            Vector3 pos = Afuera(CatmullRom(p0.pos, a.pos, b.pos, p3.pos, k));
            Vector3 mira = CatmullRom(p0.mira, a.mira, b.mira, p3.mira, k);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(mira - pos), 1 - Mathf.Exp(-Time.deltaTime * 5));
        }

        // Si una curva roza la piramide, la camara se recorre hacia afuera de sus escalones.
        internal static Vector3 Afuera(Vector3 p)
        {
            if (p.y > 4.4f) return p;
            float h = Mathf.Max(3.15f - .974f * (p.y - .12f), .5f) + .6f, m = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
            if (m >= h) return p;
            if (m < 1e-4f) return new Vector3(h, p.y, p.z);
            return new Vector3(p.x * h / m, p.y, p.z * h / m);
        }

        internal static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            => .5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);

        private void Entrar(int i)
        {
            var tm = tomas[i];
            // Narracion opcional grabada por el equipo: Audio/narracion/01, 02...
            var voz = Resources.Load<AudioClip>($"Audio/narracion/{i + 1:00}");
            if (voz != null)
            {
                if (narrador == null) narrador = cam.gameObject.AddComponent<AudioSource>();
                narrador.Stop();
                narrador.PlayOneShot(voz);
            }
            if (tm.titulo != null) { titulo = tm.titulo; subtitulo = tm.texto; cambio = Time.time; }
            if (tm.ambiente >= 0) FindFirstObjectByType<CicloDeLuz>()?.Poner(tm.ambiente);
            if (tm.ritual >= 0)
                foreach (var wi in FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None))
                    if (wi.action == WorldInteractable.Action.Offering && wi.offeringIndex == tm.ritual) wi.Activate(explorer);
        }

        public bool Activo => activo;
        // Para el video: tomas propias, barras con otro rotulo y fundido a negro al final.
        public static bool ModoVideo, SinTextos;
        public float Duracion { get { float d = 0; foreach (var tm in tomas) d += tm.dur; return d; } }

        public int Paso => idx;

        public void Iniciar()
        {
            if (explorer == null || cam == null) return;
            if (ModoVideo) AperturaVideo();
            activo = true; idx = 0; t = 0; inicio = Time.time;
            rigPos = explorer.transform.position; rigRot = explorer.transform.rotation; camRot = cam.transform.localRotation;
            explorer.enabled = false;
            cam.transform.position = tomas[0].pos;
            cam.transform.rotation = Quaternion.LookRotation(tomas[0].mira - tomas[0].pos);
            Entrar(0);
        }

        public void Iniciar(float desde)
        {
            Iniciar();
            t = desde;
        }

        public void Terminar()
        {
            activo = false;
            titulo = subtitulo = "";
            if (narrador != null) narrador.Stop();
            explorer.transform.SetPositionAndRotation(rigPos, rigRot);
            cam.transform.localPosition = Vector3.up * 1.65f;
            cam.transform.localRotation = camRot;
            explorer.enabled = true;
        }

        private GUIStyle tituloSt, textoSt, barraSt;
        private Texture2D naranja;
        private float total;

        private void Estilos()
        {
            if (tituloSt != null) return;
            var fuente = Font.CreateDynamicFontFromOSFont(new[] { "Avenir Next", "Helvetica Neue", "Arial" }, 20);
            tituloSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            tituloSt.normal.textColor = new Color(1f, .66f, .25f);
            textoSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 29, alignment = TextAnchor.UpperCenter, wordWrap = true };
            textoSt.normal.textColor = new Color(.97f, .94f, .88f);
            barraSt = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 16, alignment = TextAnchor.MiddleLeft };
            barraSt.normal.textColor = new Color(.85f, .81f, .74f);
            naranja = new Texture2D(1, 1);
            naranja.SetPixel(0, 0, new Color(1f, .58f, .1f));
            naranja.Apply();
            if (total == 0) foreach (var tm in tomas) total += tm.dur;
        }

        // Barras de cine: arriba el avance, abajo el titulo de la toma y su texto.
        private void OnGUI()
        {
            if (!activo) return;
            Estilos();
            if (total == 0) foreach (var tm in tomas) total += tm.dur;
            if (SinTextos)
            {
                // Version limpia del video: sin barras ni textos, solo los fundidos.
                float avance = t;
                for (int i = 0; i < idx; i++) avance += tomas[i].dur;
                float fundido = Mathf.Max(1 - Mathf.Clamp01((Time.time - inicio) / .7f), 1 - Mathf.Clamp01((total - avance) / 1.5f));
                GUI.color = new Color(0, 0, 0, fundido);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }
            float k = Mathf.Max(.75f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1));
            float sw = Screen.width / k, sh = Screen.height / k;
            float abre = Mathf.SmoothStep(0, 1, (Time.time - inicio) / .9f);

            GUI.color = Color.black;
            float alto = 130 * abre;
            GUI.DrawTexture(new Rect(0, 0, sw, alto), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh - alto, sw, alto), Texture2D.whiteTexture);

            float hecho = t;
            for (int i = 0; i < idx; i++) hecho += tomas[i].dur;
            GUI.color = new Color(1, 1, 1, abre);
            GUI.DrawTexture(new Rect(0, alto - 3, sw * Mathf.Clamp01(hecho / total), 3), naranja);

            float y = alto - 80;
            string rotulo = ModoVideo ? "OFRENDA DE LOS CUATRO RUMBOS" : "RECORRIDO GUIADO";
            float rw = tituloSt.CalcSize(new GUIContent(rotulo)).x;
            tituloSt.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(48, y, rw + 10, 30), rotulo, tituloSt);
            tituloSt.alignment = TextAnchor.UpperCenter;
            barraSt.alignment = TextAnchor.MiddleLeft;
            if (!ModoVideo) GUI.Label(new Rect(48 + rw + 22, y, 200, 30), (idx + 1).ToString("00") + " / " + tomas.Count.ToString("00"), barraSt);
            barraSt.alignment = TextAnchor.MiddleRight;
            string salir = ModoVideo ? "Facultad de Informática · UAQ · 2026" : Presentacion.EnEspera ? "Presiona cualquier tecla para explorar" : Mando.EnUso ? "B para salir" : "T o Esc para salir";
            GUI.Label(new Rect(sw - 548, y, 500, 30), salir, barraSt);

            // El texto nuevo entra con un fundido y sube unos pixeles.
            float aparece = Mathf.SmoothStep(0, 1, (Time.time - cambio) / .55f);
            float ancho = Mathf.Min(1100, sw - 96), x = sw / 2 - ancho / 2;
            float bh = textoSt.CalcHeight(new GUIContent(subtitulo), ancho);
            float yb = sh - alto + (130 - 26 - bh) / 2 + 8 * (1 - aparece);
            GUI.color = new Color(1, 1, 1, abre * aparece);
            VRExplorer.Etiqueta(new Rect(x, yb, ancho, 24), titulo.ToUpper(), tituloSt);
            VRExplorer.Etiqueta(new Rect(x, yb + 26, ancho, bh), subtitulo, textoSt);

            // Fundido desde negro para que no se note el salto de camara al empezar.
            float negro = 1 - Mathf.Clamp01((Time.time - inicio) / .7f);
            if (ModoVideo) negro = Mathf.Max(negro, 1 - Mathf.Clamp01((total - hecho) / 1.5f));
            GUI.color = new Color(0, 0, 0, negro);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
