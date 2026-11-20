using System.Collections;
using UnityEngine;

namespace MemoriaViva
{
    // Grillos y viento de fondo, crepitar 3D en el altar, campanas en los rituales y marimba al final.
    public sealed class AmbienteSonoro : MonoBehaviour
    {
        [SerializeField] private Transform altar;
        private AudioSource grillos, viento, velas, musica, efectos, interfaz, pasos, fondo, mariachi;
        private static AmbienteSonoro instancia;
        private static readonly System.Collections.Generic.Dictionary<string, AudioClip> clips = new System.Collections.Generic.Dictionary<string, AudioClip>();
        private static readonly string[] ritualClip = { "encender", "agua", "tierra", "papel" };
        private float nocheObjetivo = .5f, volMariachi;

        private AudioSource Fuente(string clip, bool loop, float vol, Transform parent = null, bool espacial = false)
        {
            var go = new GameObject("Audio | " + clip);
            go.transform.SetParent(parent != null ? parent : transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = Resources.Load<AudioClip>("Audio/" + clip);
            a.loop = loop;
            a.volume = vol;
            a.playOnAwake = false;
            if (espacial)
            {
                a.spatialBlend = 1;
                a.rolloffMode = AudioRolloffMode.Linear;
                a.minDistance = 2;
                a.maxDistance = 22;
            }
            if (loop && a.clip != null) a.Play();
            return a;
        }

        private void Start()
        {
            grillos = Fuente("grillos", true, .012f);
            viento = Fuente("viento", true, .18f);
            velas = Fuente("velas", true, 0, altar != null ? altar : transform, true);
            musica = Fuente("musica", true, 0);
            efectos = Fuente("campana", false, .7f);
            interfaz = Fuente("seleccion", false, 1);
            pasos = Fuente("pasos", false, .35f);
            // Musica de fondo propia del equipo: si existe Assets/Resources/Audio/fondo, suena en bucle.
            fondo = Fuente("fondo", true, .3f);
            if (fondo.clip == null) { Destroy(fondo.gameObject); fondo = null; }
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.clip != null && a.clip.name == "mariachi") { mariachi = a; volMariachi = a.volume; }
            instancia = this;
        }

        private static AudioClip Clip(string nombre)
        {
            if (!clips.TryGetValue(nombre, out var c)) clips[nombre] = c = Resources.Load<AudioClip>("Audio/" + nombre);
            return c;
        }

        // Sonido corto de interaccion; el tono varia un poco para que no suene repetido.
        public static void Paso(bool corriendo)
        {
            if (instancia == null || instancia.pasos == null || instancia.pasos.clip == null) return;
            instancia.pasos.pitch = Random.Range(.85f, 1.15f) * (corriendo ? 1.1f : 1);
            instancia.pasos.PlayOneShot(instancia.pasos.clip, corriendo ? .5f : .35f);
        }

        public static void Efecto(string nombre, float volumen = 1, float variacion = 0)
        {
            if (instancia == null || instancia.interfaz == null) return;
            var clip = Clip(nombre);
            if (clip == null) return;
            instancia.interfaz.pitch = 1 + Random.Range(-variacion, variacion);
            instancia.interfaz.PlayOneShot(clip, volumen);
        }

        private void Update()
        {
            if (grillos == null) return;
            grillos.volume = Mathf.MoveTowards(grillos.volume, .006f + .018f * nocheObjetivo, Time.deltaTime * .2f);
            viento.volume = Mathf.MoveTowards(viento.volume, .22f - .1f * nocheObjetivo, Time.deltaTime * .2f);
            if (fondo != null)
            {
                // La cancion baja cerca del mariachi y cuando suena la marimba del final, para que no se encimen.
                float objetivo = .3f;
                var oyente = Camera.main != null ? Camera.main.transform : null;
                if (mariachi != null && oyente != null)
                {
                    float d = Vector3.Distance(oyente.position, mariachi.transform.position);
                    objetivo *= Mathf.Lerp(.15f, 1, Mathf.InverseLerp(5, 16, d));
                    mariachi.volume = volMariachi * Mathf.Lerp(1, .35f, Mathf.InverseLerp(5, 16, d));
                }
                if (musica != null && musica.isPlaying) objetivo *= .25f;
                fondo.volume = Mathf.MoveTowards(fondo.volume, objetivo, Time.deltaTime * .3f);
            }
        }

        // 0 dia, .5 atardecer, 1 noche.
        public void Noche(float n) => nocheObjetivo = n;

        public void Ritual(int index)
        {
            if (efectos == null) return;
            if (index >= 0 && index < ritualClip.Length && Clip(ritualClip[index]) != null) efectos.PlayOneShot(Clip(ritualClip[index]), .9f);
            efectos.PlayOneShot(Resources.Load<AudioClip>("Audio/destello"), .5f);
            efectos.PlayOneShot(Resources.Load<AudioClip>("Audio/campana"), .55f);
            if (index == 0 && velas != null && velas.clip != null)
            {
                velas.volume = .9f;
                if (!velas.isPlaying) velas.Play();
            }
        }

        public void Final() => StartCoroutine(FinalRoutine());

        private IEnumerator FinalRoutine()
        {
            var bell = Resources.Load<AudioClip>("Audio/campana");
            for (int i = 0; i < 3; i++)
            {
                efectos.PlayOneShot(bell, .7f);
                yield return new WaitForSeconds(1.6f);
            }
            if (musica.clip == null) yield break;
            musica.Play();
            for (float v = 0; v < .45f; v += Time.deltaTime * .15f)
            {
                musica.volume = v;
                yield return null;
            }
        }
    }
}
