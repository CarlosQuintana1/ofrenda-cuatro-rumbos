using System.Collections;
using UnityEngine;

namespace MemoriaViva
{
    // Particulas de los rituales y de las interacciones, armadas en codigo para no depender de prefabs.
    public sealed class Efectos : MonoBehaviour
    {
        public enum Tipo { Fuego, Agua, Tierra, Viento, Brillo, Final }

        private static Efectos instancia;
        private static Material aditivo, mezcla;

        private static Efectos Instancia
        {
            get
            {
                if (instancia == null) instancia = new GameObject("Efectos").AddComponent<Efectos>();
                return instancia;
            }
        }

        private static Material Mat(bool suma)
        {
            if (aditivo == null)
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                    float a = Mathf.Clamp01(1 - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a * (3 - 2 * a)));
                }
                tex.Apply();
                aditivo = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { mainTexture = tex };
                mezcla = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = tex };
            }
            return suma ? aditivo : mezcla;
        }

        public static void Lanzar(Tipo tipo, Vector3 pos) => Instancia.Crear(tipo, pos);

        private void Crear(Tipo tipo, Vector3 pos)
        {
            var go = new GameObject("Efecto | " + tipo);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emision = ps.emission;
            emision.rateOverTime = 0;
            var forma = ps.shape;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var tamano = ps.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .4f), new Keyframe(.2f, 1), new Keyframe(1, 0)));
            var vel = ps.velocityOverLifetime;
            var ruido = ps.noise;
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = Mat(tipo != Tipo.Tierra);
            Color a, b;
            int n;
            switch (tipo)
            {
                case Tipo.Fuego:
                    a = new Color(1f, .75f, .3f); b = new Color(1f, .25f, .05f);
                    n = 140; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 2.2f); main.startSize = new ParticleSystem.MinMaxCurve(0.064f, 0.192f);
                    main.gravityModifier = -.25f;
                    forma.shapeType = ParticleSystemShapeType.Cone; forma.angle = 25; forma.radius = .5f;
                    forma.rotation = new Vector3(-90, 0, 0);
                    ruido.enabled = true; ruido.strength = .6f; ruido.frequency = 1.2f;
                    StartCoroutine(Destello(pos, new Color(1f, .6f, .25f), 6f, 1.2f));
                    break;
                case Tipo.Agua:
                    a = new Color(.55f, .9f, 1f); b = new Color(.2f, .55f, 1f);
                    n = 150; main.startLifetime = new ParticleSystem.MinMaxCurve(.9f, 1.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f); main.startSize = new ParticleSystem.MinMaxCurve(0.048f, 0.128f);
                    main.gravityModifier = .9f;
                    forma.shapeType = ParticleSystemShapeType.Cone; forma.angle = 30; forma.radius = .25f;
                    forma.rotation = new Vector3(-90, 0, 0);
                    StartCoroutine(Destello(pos, new Color(.4f, .8f, 1f), 3f, 1f));
                    break;
                case Tipo.Tierra:
                    a = new Color(.75f, .55f, .3f, .7f); b = new Color(.45f, .3f, .18f, 0);
                    n = 70; main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1f); main.startSize = new ParticleSystem.MinMaxCurve(0.32f, 0.8f);
                    main.gravityModifier = -.03f;
                    forma.shapeType = ParticleSystemShapeType.Circle; forma.radius = 1.2f;
                    forma.rotation = new Vector3(-90, 0, 0);
                    ruido.enabled = true; ruido.strength = .3f;
                    break;
                case Tipo.Viento:
                    a = new Color(1f, .6f, .1f); b = new Color(1f, .35f, .05f);
                    n = 180; main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f); main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                    forma.shapeType = ParticleSystemShapeType.Circle; forma.radius = 1.6f;
                    forma.rotation = new Vector3(-90, 0, 0);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
                    vel.orbitalY = new ParticleSystem.MinMaxCurve(2.2f); vel.y = new ParticleSystem.MinMaxCurve(.9f);
                    vel.x = vel.z = new ParticleSystem.MinMaxCurve(0);
                    render.sharedMaterial = Mat(false);
                    break;
                case Tipo.Final:
                    a = new Color(1f, .9f, .6f); b = new Color(1f, .5f, .15f);
                    n = 360; main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f); main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.256f);
                    main.gravityModifier = -.05f;
                    forma.shapeType = ParticleSystemShapeType.Sphere; forma.radius = .6f;
                    ruido.enabled = true; ruido.strength = .8f;
                    StartCoroutine(Destello(pos, new Color(1f, .8f, .5f), 10f, 2.5f));
                    break;
                default:
                    a = new Color(1f, .9f, .55f); b = new Color(1f, .7f, .3f);
                    n = 40; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(.4f, 1.4f); main.startSize = new ParticleSystem.MinMaxCurve(0.032f, 0.096f);
                    main.gravityModifier = -.1f;
                    forma.shapeType = ParticleSystemShapeType.Sphere; forma.radius = .15f;
                    break;
            }
            main.maxParticles = n;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0), new GradientColorKey(b, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(a.a, .1f), new GradientAlphaKey(a.a * .8f, .6f), new GradientAlphaKey(0, 1) });
            color.color = g;
            main.startColor = Color.white;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(n * .6f)), new ParticleSystem.Burst(.25f, (short)(n * .4f)) });
            ps.Play();
        }

        // Cohetes de feria: suben con estela y revientan en colores sobre la explanada.
        public static void Cohetes(int cantidad, float segundos) => Instancia.StartCoroutine(Instancia.LanzarCohetes(cantidad, segundos));

        private IEnumerator LanzarCohetes(int cantidad, float segundos)
        {
            Color[] colores = { new Color(1f, .55f, .1f), new Color(1f, .25f, .6f), new Color(.4f, .7f, 1f), new Color(1f, .9f, .3f), new Color(.5f, 1f, .5f), new Color(.8f, .4f, 1f) };
            for (int i = 0; i < cantidad; i++)
            {
                Vector3 origen = Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * Random.Range(12f, 20f);
                Vector3 cima = origen * .6f + Vector3.up * Random.Range(18f, 28f);
                StartCoroutine(Cohete(origen, cima, colores[i % colores.Length], colores[(i + 2) % colores.Length]));
                yield return new WaitForSeconds(segundos / cantidad * Random.Range(.5f, 1.5f));
            }
        }

        private IEnumerator Cohete(Vector3 origen, Vector3 cima, Color a, Color b)
        {
            var estela = new GameObject("Efecto | cohete");
            var ps = estela.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = .6f; main.startSpeed = 0; main.startSize = .12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, .8f, .5f);
            var em = ps.emission; em.rateOverTime = 70;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .5f, .1f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            estela.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(true);
            ps.Play();
            for (float t = 0; t < 1; t += Time.deltaTime / 1.3f)
            {
                estela.transform.position = Vector3.Lerp(origen, cima, 1 - (1 - t) * (1 - t));
                yield return null;
            }
            em.rateOverTime = 0;
            Destroy(estela, 1f);
            var burst = new GameObject("Efecto | estallido");
            burst.transform.position = cima;
            var bp = burst.AddComponent<ParticleSystem>();
            bp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var bm = bp.main;
            bm.loop = false; bm.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            bm.startSpeed = new ParticleSystem.MinMaxCurve(6f, 9f); bm.startSize = new ParticleSystem.MinMaxCurve(.25f, .45f);
            bm.gravityModifier = .25f; bm.maxParticles = 220;
            bm.simulationSpace = ParticleSystemSimulationSpace.World;
            bm.stopAction = ParticleSystemStopAction.Destroy;
            var be = bp.emission; be.rateOverTime = 0; be.SetBursts(new[] { new ParticleSystem.Burst(0, 200) });
            var bs = bp.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = .2f;
            var bl = bp.limitVelocityOverLifetime; bl.enabled = true; bl.limit = 2.5f; bl.dampen = .08f;
            var bc = bp.colorOverLifetime; bc.enabled = true;
            var bg = new Gradient();
            bg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(a, .2f), new GradientColorKey(b, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.9f, .6f), new GradientAlphaKey(0, 1) });
            bc.color = bg;
            burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(true);
            bp.Play();
            AmbienteSonoro.Efecto("cohete", .5f, .15f);
            yield return Destello(cima, a, 4f, 1.2f);
        }

        // Chispas que suben sin parar desde las veladoras una vez encendido el fuego.
        public static void Brasas(Vector3 centro, float radio)
        {
            var go = new GameObject("Efecto | brasas de las veladoras");
            go.transform.position = centro;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.024f, 0.064f);
            main.gravityModifier = -.06f;
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 28;
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(radio * 2, 2.4f, radio * 2);
            var ruido = ps.noise;
            ruido.enabled = true;
            ruido.strength = .35f;
            ruido.frequency = .8f;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, .8f, .4f), 0), new GradientColorKey(new Color(1f, .35f, .08f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(.6f, .7f), new GradientAlphaKey(0, 1) });
            color.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(true);
            ps.Play();
        }

        private IEnumerator Destello(Vector3 pos, Color c, float intensidad, float segundos)
        {
            var luz = new GameObject("Destello").AddComponent<Light>();
            luz.transform.position = pos + Vector3.up * .4f;
            luz.type = LightType.Point;
            luz.color = c;
            luz.range = 7;
            luz.shadows = LightShadows.None;
            for (float t = 0; t < segundos; t += Time.deltaTime)
            {
                float k = t / segundos;
                luz.intensity = intensidad * Mathf.Min(1, k * 8) * (1 - k) * (1 - k);
                yield return null;
            }
            Destroy(luz.gameObject);
        }
    }
}
