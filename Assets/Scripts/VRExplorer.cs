using UnityEngine;
using UnityEngine.XR;

namespace MemoriaViva
{
    public sealed class VRExplorer : MonoBehaviour
    {
        // Mientras alguien escribe un nombre, el teclado no mueve al explorador.
        public static bool EntradaBloqueada;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float desktopSpeed = 4.2f;
        [SerializeField] private float reach = 16f;
        [SerializeField] private float worldRadius = 140f;
        private InputDevice head;
        private InputDevice rightHand;
        private bool vrActive;
        private bool lastTrigger;
        private float yaw;
        private float pitch;
        private string messageTitle = "Ofrenda de los Cuatro Rumbos";
        private string messageBody = "Camina hacia el centro de la explanada. Cada cara de la ofrenda guarda un ritual: fuego, agua, tierra y viento. Señala cualquier elemento para conocer su significado.";
        private float messageUntil = 12f;
        private WorldInteractable target, lastTarget;
        private float stepPhase, targetSince;
        private Vector3 velocity;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private LineRenderer laser;
        private TextMesh vrPrompt;
        private ExperienceDirector director;

        private void Start()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            yaw = transform.eulerAngles.y;
            director = FindFirstObjectByType<ExperienceDirector>();
            var lineObject = new GameObject("Rayo del controlador derecho");
            lineObject.transform.SetParent(transform);
            laser = lineObject.AddComponent<LineRenderer>();
            laser.material = new Material(Shader.Find("Sprites/Default"));
            laser.startColor = new Color(.22f, 1f, .9f, .95f);
            laser.endColor = new Color(.22f, 1f, .9f, .15f);
            laser.startWidth = .014f;
            laser.endWidth = .003f;
            laser.positionCount = 2;
            laser.enabled = false;
            var promptObject = new GameObject("Indicador legible en visor");
            promptObject.transform.SetParent(viewCamera.transform);
            promptObject.transform.localPosition = new Vector3(0, -.31f, 1.08f);
            promptObject.transform.localRotation = Quaternion.identity;
            vrPrompt = promptObject.AddComponent<TextMesh>();
            vrPrompt.fontSize = 64;
            vrPrompt.characterSize = .0055f;
            vrPrompt.anchor = TextAnchor.MiddleCenter;
            vrPrompt.alignment = TextAlignment.Center;
            vrPrompt.color = new Color(1f, .89f, .7f);
            vrPrompt.text = "";
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (Ground(transform.position, 50f, out float y)) transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        // Altura del suelo bajo un punto, ignorando los volumenes de interaccion.
        private bool Ground(Vector3 p, float above, out float y)
        {
            y = 0;
            if (Physics.Raycast(p + Vector3.up * above, Vector3.down, out RaycastHit hit, above + 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (viewCamera == null) return;
            if (!head.isValid) head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!rightHand.isValid) rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            vrActive = head.isValid && head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;

            if (!vrActive && !EntradaBloqueada) DesktopMovement();
            else HeadTracking();

            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (vrActive && rightHand.isValid &&
                rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 handPos) &&
                rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion handRot))
                ray = new Ray(transform.TransformPoint(handPos), transform.rotation * handRot * Vector3.forward);

            target = null;
            int count = Physics.RaycastNonAlloc(ray, hits, reach);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].collider.GetComponentInParent<WorldInteractable>();
                if (candidate != null && hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    target = candidate;
                }
            }
            if (target != null && target != lastTarget)
            {
                targetSince = Time.time;
                AmbienteSonoro.Efecto("seleccion", .35f, .05f);
                Vibrar(.15f, .04f);
            }
            lastTarget = target;
            laser.enabled = vrActive;
            if (vrActive)
            {
                laser.SetPosition(0, ray.origin);
                laser.SetPosition(1, ray.origin + ray.direction * (nearest < float.MaxValue ? nearest : 7f));
                if (Time.time < messageUntil)
                    vrPrompt.text = messageTitle.ToUpper() + "\n" + Wrap(messageBody, 46) + (target != null ? "\n\nGATILLO  ·  " + target.Prompt : "");
                else
                    vrPrompt.text = target != null ? "GATILLO  ·  " + target.Prompt :
                        "RITUALES  " + (director != null ? director.Collected : 0) + " / 4";
            }
            bool trigger = false;
            if (vrActive && rightHand.isValid)
                rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
            bool activate = !EntradaBloqueada && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0) || Mando.Interactuar) || (trigger && !lastTrigger);
            lastTrigger = trigger;
            if (activate && target != null) target.Activate(this);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !vrActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void DesktopMovement()
        {
            Vector2 look = Mando.Mirar;
            yaw += Input.GetAxis("Mouse X") * 2.1f + look.x * 150f * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.1f - look.y * 110f * Time.deltaTime, -80f, 80f);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 stick = Mando.Mover;
            Vector3 movement = transform.right * (Input.GetAxisRaw("Horizontal") + stick.x) + transform.forward * (Input.GetAxisRaw("Vertical") + stick.y);
            if (movement.sqrMagnitude > 1f) movement.Normalize();
            bool running = Input.GetKey(KeyCode.LeftShift) || Mando.Correr;
            float speed = desktopSpeed * (running ? 2.2f : 1f);
            // Acelera y frena en unas decimas de segundo en lugar de arrancar en seco.
            velocity = Vector3.MoveTowards(velocity, movement * speed, (movement.sqrMagnitude > .01f ? 22f : 30f) * Time.deltaTime);
            Vector3 step = velocity * Time.deltaTime;
            Vector3 p = transform.position;
            if (Ground(p, .6f, out float floor) && floor < p.y - .005f)
                transform.position = p = new Vector3(p.x, Mathf.Max(floor, p.y - 7f * Time.deltaTime), p.z);
            if (step.sqrMagnitude < 1e-8f) { velocity = Vector3.zero; return; }
            // Choca con muros y con la ofrenda; sube escalones de hasta medio metro.
            if (Physics.CapsuleCast(p + Vector3.up * .65f, p + Vector3.up * 1.5f, .28f, step.normalized, out _, step.magnitude + .05f, ~0, QueryTriggerInteraction.Ignore))
            {
                velocity = Vector3.zero;
                return;
            }
            Vector3 next = p + step;
            Vector2 flat = new Vector2(next.x, next.z);
            if (flat.magnitude > worldRadius) return;
            if (Ground(next, .6f, out float y))
            {
                if (y - p.y > .55f) return;
                next.y = y >= p.y ? y : Mathf.Max(y, p.y - 7f * Time.deltaTime);
            }
            // Cada medio ciclo del balanceo es un paso: el sonido cae cuando el pie toca el suelo.
            float before = stepPhase;
            stepPhase += Vector2.Distance(new Vector2(p.x, p.z), new Vector2(next.x, next.z)) * Mathf.PI / (running ? 1.05f : .72f);
            if (Mathf.Floor(before / Mathf.PI) != Mathf.Floor(stepPhase / Mathf.PI)) AmbienteSonoro.Paso(running);
            transform.position = next;
        }

        // Vibracion en el mando o en el control de VR al apuntar o activar algo.
        public void Vibrar(float strength, float seconds)
        {
            if (vrActive && rightHand.isValid) rightHand.SendHapticImpulse(0, strength, seconds);
            else Mando.Vibrar(strength, seconds);
        }

        private static string Wrap(string text, int width)
        {
            var sb = new System.Text.StringBuilder();
            int line = 0;
            foreach (var word in text.Split(' '))
            {
                if (line + word.Length > width && line > 0) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line++; }
                sb.Append(word);
                line += word.Length;
            }
            return sb.ToString();
        }

        private void HeadTracking()
        {
            if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p))
                viewCamera.transform.localPosition = p.y < .6f ? p + Vector3.up * 1.65f : p;
            if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q))
                viewCamera.transform.localRotation = q;
        }

        public void Teleport(Vector3 destination)
        {
            Vector3 cameraOffset = viewCamera.transform.localPosition;
            if (Ground(destination, 6f, out float y)) destination.y = y;
            transform.position = destination - new Vector3(cameraOffset.x, 0, cameraOffset.z);
            ShowMessage("Has llegado", "Explora las historias y las ofrendas de este lugar.", 4f);
        }

        public void ShowMessage(string title, string body, float seconds)
        {
            messageTitle = title;
            messageBody = body;
            // Que alcance a leerse completo: unas tres palabras por segundo.
            messageUntil = Time.time + Mathf.Max(seconds, 3 + body.Split(' ').Length * .33f);
            messageStart = Time.time;
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private Texture2D panelTex, accentTex, pillTex;
        private GUIStyle titleStyle, bodyStyle, sourceStyle, hintStyle, promptStyle;
        private float messageStart;

        private void EnsureStyles()
        {
            if (panelTex != null) return;
            panelTex = Solid(new Color(.06f, .045f, .08f, .86f));
            accentTex = Solid(new Color(1f, .58f, .1f, 1f));
            pillTex = Solid(new Color(0, 0, 0, .55f));
            var fuente = Font.CreateDynamicFontFromOSFont(new[] { "Avenir Next", "Helvetica Neue", "Arial" }, 20);
            titleStyle = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 27, fontStyle = FontStyle.Bold, wordWrap = true };
            titleStyle.normal.textColor = new Color(1f, .74f, .32f);
            bodyStyle = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 20, wordWrap = true, richText = true };
            bodyStyle.normal.textColor = new Color(.96f, .93f, .88f);
            sourceStyle = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 14, fontStyle = FontStyle.Italic, wordWrap = true };
            sourceStyle.normal.textColor = new Color(.75f, .7f, .78f);
            hintStyle = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 15, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(.95f, .92f, .86f);
            promptStyle = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            promptStyle.normal.textColor = Color.white;
        }

        // Texto con sombra suave para que se lea sobre cualquier fondo.
        internal static void Etiqueta(Rect r, string texto, GUIStyle estilo)
        {
            Color c = estilo.normal.textColor;
            estilo.normal.textColor = new Color(0, 0, 0, .6f * GUI.color.a);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), texto, estilo);
            estilo.normal.textColor = c;
            GUI.Label(r, texto, estilo);
        }

        private void OnGUI()
        {
            if (vrActive) return; // La señalización 3D se ve dentro del visor.
            EnsureStyles();
            // Escala la interfaz a la resolución para que se lea igual en cualquier pantalla.
            float k = Mathf.Max(.75f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1));
            float sw = Screen.width / k, sh = Screen.height / k;

            // La mira crece y se pinta de naranja al apuntar a algo que se puede usar.
            float pop = target != null ? Mathf.Clamp01((Time.time - targetSince) / .18f) : 0;
            float ease = 1 - (1 - pop) * (1 - pop);
            int size = promptStyle.fontSize;
            promptStyle.fontSize = Mathf.RoundToInt(18 + 12 * ease);
            GUI.color = Color.Lerp(new Color(1, 1, 1, .85f), new Color(1f, .62f, .2f, 1), ease);
            GUI.Label(new Rect(sw / 2 - 20, sh / 2 - 20, 40, 40), "+", promptStyle);
            promptStyle.fontSize = size;
            GUI.color = Color.white;

            if (target != null)
            {
                string txt = (Mando.EnUso ? "A" : "E / clic") + "   ·   " + target.Prompt;
                float w = promptStyle.CalcSize(new GUIContent(txt)).x + 48;
                var r = new Rect(sw / 2 - w / 2, sh * .7f + 14 * (1 - ease), w, 44);
                GUI.color = new Color(1, 1, 1, ease);
                GUI.DrawTexture(r, pillTex);
                GUI.DrawTexture(new Rect(r.x + r.width * (1 - ease) / 2, r.yMax - 3, r.width * ease, 3), accentTex);
                Etiqueta(r, txt, promptStyle);
                GUI.color = Color.white;
            }

            if (Time.time < messageUntil)
            {
                float fade = Mathf.Clamp01((Time.time - messageStart) / .25f) * Mathf.Clamp01((messageUntil - Time.time) / .6f);
                string body = messageBody, source = null;
                int f = body.LastIndexOf("(Fuente");
                if (f < 0) f = body.LastIndexOf("(Interpretación");
                if (f > 0) { source = body.Substring(f).Trim('(', ')', ' '); body = body.Substring(0, f).TrimEnd(); }
                float w = Mathf.Min(700, sw - 64), pad = 28, inner = w - pad * 2 - 6;
                float th = titleStyle.CalcHeight(new GUIContent(messageTitle), inner);
                float bh = bodyStyle.CalcHeight(new GUIContent(body), inner);
                float shh = source != null ? sourceStyle.CalcHeight(new GUIContent(source), inner) + 10 : 0;
                var box = new Rect(32 - 24 * (1 - Mathf.Clamp01((Time.time - messageStart) / .3f)), 32, w, pad + th + 10 + bh + shh + pad);
                GUI.color = new Color(1, 1, 1, fade);
                GUI.DrawTexture(box, panelTex);
                GUI.DrawTexture(new Rect(box.x, box.y, 6, box.height), accentTex);
                float x = box.x + pad + 6, y = box.y + pad;
                Etiqueta(new Rect(x, y, inner, th), messageTitle, titleStyle);
                y += th + 10;
                Etiqueta(new Rect(x, y, inner, bh), body, bodyStyle);
                y += bh + 10;
                if (source != null) GUI.Label(new Rect(x, y, inner, shh), source, sourceStyle);
                GUI.color = Color.white;
            }

            // Cuantos significados lleva leidos quien recorre la ofrenda.
            int total = WorldInteractable.Total();
            if (total > 0)
            {
                string cuenta = "Significados  " + WorldInteractable.Descubiertos.Count + " / " + total;
                float cw = hintStyle.CalcSize(new GUIContent(cuenta)).x + 36;
                var cr = new Rect(sw - cw - 24, 24, cw, 34);
                GUI.DrawTexture(cr, pillTex);
                GUI.DrawTexture(new Rect(cr.x, cr.yMax - 3, cr.width * WorldInteractable.Descubiertos.Count / total, 3), accentTex);
                Etiqueta(cr, cuenta, hintStyle);
            }

            string hints = Mando.EnUso
                ? "Stick izq. mover   ·   Stick der. mirar   ·   LT correr   ·   A interactuar   ·   Y luz   ·   X recorrido   ·   Start reiniciar"
                : "WASD mover   ·   Shift correr   ·   E interactuar   ·   N luz   ·   T recorrido   ·   O dejar un nombre   ·   R reiniciar   ·   P ligero   ·   Esc cursor";
            float hw = hintStyle.CalcSize(new GUIContent(hints)).x + 40;
            var hr = new Rect(sw / 2 - hw / 2, sh - 46, hw, 32);
            GUI.DrawTexture(hr, pillTex);
            Etiqueta(hr, hints, hintStyle);
            GUI.matrix = Matrix4x4.identity;
        }

    }
}
