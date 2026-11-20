using UnityEngine;

namespace MemoriaViva
{
    public sealed class WorldInteractable : MonoBehaviour
    {
        public enum Action { Offering, Story, Teleport }
        // Significados que ya se leyeron; se reinicia al recargar la escena entre jueces.
        public static readonly System.Collections.Generic.HashSet<string> Descubiertos = new System.Collections.Generic.HashSet<string>();
        private static int total;
        public static int Total()
        {
            if (total == 0)
            {
                var titulos = new System.Collections.Generic.HashSet<string>();
                foreach (var w in FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None)) if (w.action == Action.Story) titulos.Add(w.title);
                total = titulos.Count;
            }
            return total;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() { Descubiertos.Clear(); total = 0; }

        public static void Reiniciar() { Descubiertos.Clear(); total = 0; }
        public Action action;
        public string title;
        [TextArea] public string description;
        public string verb;
        public int offeringIndex = -1;
        public Vector3 destination;
        public GameObject activeVisual;
        private bool used;

        public string Prompt
        {
            get
            {
                if (action == Action.Teleport) return "Viajar a " + title;
                if (action == Action.Offering && !used && !string.IsNullOrEmpty(verb)) return verb;
                return "Conocer: " + title;
            }
        }

        public void Activate(VRExplorer explorer)
        {
            if (action == Action.Teleport)
            {
                AmbienteSonoro.Efecto("viaje", .9f);
                explorer.Vibrar(.4f, .25f);
                explorer.Teleport(destination);
                Efectos.Lanzar(Efectos.Tipo.Brillo, explorer.transform.position + Vector3.up);
                return;
            }
            if (action == Action.Offering && !used)
            {
                used = true;
                AmbienteSonoro.Efecto("agarrar", .9f);
                explorer.Vibrar(.7f, .2f);
                var director = FindFirstObjectByType<ExperienceDirector>();
                if (director != null) director.ReceiveOffering(offeringIndex);
                if (activeVisual != null) StartCoroutine(Desvanecer(activeVisual.transform));
            }
            else
            {
                AmbienteSonoro.Efecto("conocer", .7f, .04f);
                if (action == Action.Story && Descubiertos.Add(title) && Descubiertos.Count == Total())
                {
                    explorer.ShowMessage("Conociste toda la ofrenda", "Encontraste los " + Total() + " significados. Gracias por recorrerla completa.", 6f);
                    return;
                }
                Efectos.Lanzar(Efectos.Tipo.Brillo, GetComponent<Collider>() != null ? GetComponent<Collider>().bounds.center : transform.position);
                explorer.Vibrar(.25f, .06f);
            }
            var vista = GetComponent<VistaAerea>();
            if (vista != null && !VistaAerea.EnVisor()) { vista.Mostrar(explorer, title, description); return; }
            explorer.ShowMessage(title, description, 11f);
        }

        // El anillo del pedestal gira, sube y se encoge al tomar la ofrenda.
        private System.Collections.IEnumerator Desvanecer(Transform t)
        {
            Vector3 escala = t.localScale, pos = t.position;
            Efectos.Lanzar(Efectos.Tipo.Brillo, pos);
            for (float k = 0; k < 1; k += Time.deltaTime / .8f)
            {
                float e = k * k;
                t.position = pos + Vector3.up * .5f * Mathf.Sin(k * Mathf.PI * .5f);
                t.Rotate(0, 720 * Time.deltaTime * (1 - k), 0, Space.World);
                t.localScale = escala * (1 + .3f * Mathf.Sin(k * Mathf.PI) - e);
                yield return null;
            }
            t.gameObject.SetActive(false);
            t.position = pos;
            t.localScale = escala;
        }
    }
}
