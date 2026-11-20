using UnityEngine;
using UnityEngine.XR;

namespace MemoriaViva
{
    // Tres ambientes para recorrer la plaza: atardecer, noche y dia. Tecla N o boton A del control.
    public sealed class CicloDeLuz : MonoBehaviour
    {
        [System.Serializable]
        public struct Ambiente
        {
            public string nombre;
            public Vector3 sol;
            public Color colorSol;
            public float intensidadSol;
            public float intensidadLuna;
            public Color cielo, horizonte, suelo;
            public Color niebla;
            public float densidadNiebla;
            public float exposicion;
            public float atmosfera;
            public Color tinte;
            public float ventanas;
            public Color nubes;
            public float bloom;
        }

        [SerializeField] private Light sun;
        [SerializeField] private Light moon;
        [SerializeField] private Material litWindows;
        [SerializeField] private Material clouds;
        [SerializeField] private GameObject reflectores;
        // Tapetes de aserrin y alebrijes: casi apagados de dia, encendidos de noche.
        [SerializeField] private Material[] nocturnos;
        private Color[] nocturnosBase;
        private Color cloudBase;
        [SerializeField] private int current;
        private Ambiente[] presets;
        private Material skyInstance;
        private Color windowBase;
        private bool lastButton;
        private Ambiente desde, objetivo;
        private float transicion = 1;
        private const float Duracion = 2.5f;

        public static Ambiente[] Presets() => new[]
        {
            new Ambiente
            {
                nombre = "Atardecer", sol = new Vector3(3.5f, 100f, 0), colorSol = new Color(1f, .5f, .24f), intensidadSol = 1.35f,
                intensidadLuna = .32f, cielo = new Color(.36f, .38f, .52f), horizonte = new Color(.46f, .34f, .32f), suelo = new Color(.16f, .13f, .13f),
                niebla = new Color(.6f, .42f, .38f), densidadNiebla = .0035f, exposicion = 1.15f, atmosfera = 1.2f,
                tinte = new Color(.52f, .48f, .56f), ventanas = 1f, nubes = new Color(1f, .78f, .68f, .85f), bloom = .12f
            },
            new Ambiente
            {
                nombre = "Noche", sol = new Vector3(-12f, 96f, 0), colorSol = new Color(1f, .5f, .3f), intensidadSol = 0f,
                intensidadLuna = .38f, cielo = new Color(.12f, .14f, .26f), horizonte = new Color(.10f, .10f, .17f), suelo = new Color(.05f, .05f, .07f),
                niebla = new Color(.07f, .07f, .12f), densidadNiebla = .006f, exposicion = .5f, atmosfera = .8f,
                tinte = new Color(.3f, .35f, .55f), ventanas = 1.6f, nubes = new Color(.12f, .13f, .2f, .7f), bloom = .55f
            },
            new Ambiente
            {
                nombre = "Día", sol = new Vector3(52f, 130f, 0), colorSol = new Color(1f, .95f, .86f), intensidadSol = 1.15f,
                intensidadLuna = 0f, cielo = new Color(.55f, .65f, .80f), horizonte = new Color(.62f, .64f, .62f), suelo = new Color(.28f, .26f, .22f),
                niebla = new Color(.70f, .76f, .84f), densidadNiebla = .0018f, exposicion = 1.25f, atmosfera = .9f,
                tinte = new Color(.5f, .5f, .5f), ventanas = 0f, nubes = new Color(1f, 1f, 1f, .92f), bloom = .03f
            }
        };

        private void Start()
        {
            QualitySettings.pixelLightCount = 12;
            QualitySettings.shadowDistance = 90;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            presets = Presets();
            if (RenderSettings.skybox != null)
            {
                skyInstance = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyInstance;
            }
            if (litWindows != null) windowBase = litWindows.GetColor("_EmissionColor");
            if (clouds != null) cloudBase = clouds.color;
            if (nocturnos != null)
            {
                nocturnosBase = new Color[nocturnos.Length];
                for (int i = 0; i < nocturnos.Length; i++) if (nocturnos[i] != null) nocturnosBase[i] = nocturnos[i].GetColor("_EmissionColor");
            }
            objetivo = presets[current];
            Apply(presets[current], sun, moon, skyInstance);
            ApplyWindows();
        }

        private void Update()
        {
            bool button = false;
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.isValid) right.TryGetFeatureValue(CommonUsages.primaryButton, out button);
            if ((!VRExplorer.EntradaBloqueada && (Input.GetKeyDown(KeyCode.N) || Mando.Luz)) || (button && !lastButton))
            {
                AmbienteSonoro.Efecto("luz", .6f);
                desde = transicion < 1 ? Mezclar(desde, objetivo, Mathf.SmoothStep(0, 1, transicion)) : objetivo;
                transicion = 0;
                current = (current + 1) % presets.Length;
                objetivo = presets[current];
                FindFirstObjectByType<AmbienteSonoro>()?.Noche(current == 1 ? 1 : current == 0 ? .5f : 0);
                var explorer = FindFirstObjectByType<VRExplorer>();
                if (explorer != null) explorer.ShowMessage(presets[current].nombre, "Cambia el ambiente con N (Y en el mando, A en el visor).", 3f);
            }
            lastButton = button;
            if (transicion < 1)
            {
                transicion = Mathf.Min(1, transicion + Time.deltaTime / Duracion);
                var a = Mezclar(desde, objetivo, Mathf.SmoothStep(0, 1, transicion));
                Apply(a, sun, moon, skyInstance);
                ApplyWindows(a);
            }
        }

        private static Ambiente Mezclar(Ambiente a, Ambiente b, float k) => new Ambiente
        {
            nombre = b.nombre,
            sol = (Quaternion.Slerp(Quaternion.Euler(a.sol), Quaternion.Euler(b.sol), k)).eulerAngles,
            colorSol = Color.Lerp(a.colorSol, b.colorSol, k),
            intensidadSol = Mathf.Lerp(a.intensidadSol, b.intensidadSol, k),
            intensidadLuna = Mathf.Lerp(a.intensidadLuna, b.intensidadLuna, k),
            cielo = Color.Lerp(a.cielo, b.cielo, k),
            horizonte = Color.Lerp(a.horizonte, b.horizonte, k),
            suelo = Color.Lerp(a.suelo, b.suelo, k),
            niebla = Color.Lerp(a.niebla, b.niebla, k),
            densidadNiebla = Mathf.Lerp(a.densidadNiebla, b.densidadNiebla, k),
            exposicion = Mathf.Lerp(a.exposicion, b.exposicion, k),
            atmosfera = Mathf.Lerp(a.atmosfera, b.atmosfera, k),
            tinte = Color.Lerp(a.tinte, b.tinte, k),
            ventanas = Mathf.Lerp(a.ventanas, b.ventanas, k),
            nubes = Color.Lerp(a.nubes, b.nubes, k),
            bloom = Mathf.Lerp(a.bloom, b.bloom, k)
        };

        public void Poner(int i)
        {
            if (presets != null && i != current)
            {
                desde = transicion < 1 ? Mezclar(desde, objetivo, Mathf.SmoothStep(0, 1, transicion)) : objetivo;
                transicion = 0;
                current = i;
                objetivo = presets[current];
                FindFirstObjectByType<AmbienteSonoro>()?.Noche(current == 1 ? 1 : current == 0 ? .5f : 0);
                return;
            }
            current = i;
            objetivo = presets[current];
            Apply(presets[current], sun, moon, skyInstance);
            ApplyWindows();
            FindFirstObjectByType<AmbienteSonoro>()?.Noche(current == 1 ? 1 : current == 0 ? .5f : 0);
        }

        // Cada ritual oscurece el atardecer un paso; con el cuarto ya es de noche.
        public void Oscurecer(float k)
        {
            if (presets == null || current == 2) return;
            desde = transicion < 1 ? Mezclar(desde, objetivo, Mathf.SmoothStep(0, 1, transicion)) : objetivo;
            objetivo = Mezclar(presets[0], presets[1], Mathf.Clamp01(k));
            current = k >= 1 ? 1 : 0;
            transicion = 0;
            FindFirstObjectByType<AmbienteSonoro>()?.Noche(Mathf.Lerp(.5f, 1, k));
        }

        private void ApplyWindows() => ApplyWindows(presets[current]);

        private void ApplyWindows(Ambiente a)
        {
            if (litWindows != null) litWindows.SetColor("_EmissionColor", windowBase * a.ventanas);
            if (clouds != null) clouds.color = a.nubes;
            if (nocturnosBase != null)
                for (int i = 0; i < nocturnos.Length; i++) if (nocturnos[i] != null) nocturnos[i].SetColor("_EmissionColor", nocturnosBase[i] * (.04f + .5f * a.ventanas));
            if (reflectores != null) reflectores.SetActive(a.ventanas > 1.3f);
            var glow = FindFirstObjectByType<BrilloNocturno>();
            if (glow != null) glow.intensity = a.bloom;
        }

        private void OnDestroy()
        {
            if (litWindows != null && windowBase != default) litWindows.SetColor("_EmissionColor", windowBase);
            if (clouds != null && cloudBase != default) clouds.color = cloudBase;
            if (nocturnosBase != null)
                for (int i = 0; i < nocturnos.Length; i++) if (nocturnos[i] != null) nocturnos[i].SetColor("_EmissionColor", nocturnosBase[i]);
        }

        public static void Apply(Ambiente a, Light sun, Light moon, Material sky)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(a.sol);
                sun.color = a.colorSol;
                sun.intensity = a.intensidadSol;
                sun.enabled = a.intensidadSol > 0;
            }
            if (moon != null)
            {
                moon.intensity = a.intensidadLuna;
                moon.enabled = a.intensidadLuna > 0;
            }
            Shader.SetGlobalFloat("_Noche", Mathf.Clamp01((a.ventanas - .8f) / .8f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = a.cielo;
            RenderSettings.ambientEquatorColor = a.horizonte;
            RenderSettings.ambientGroundColor = a.suelo;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = a.niebla;
            RenderSettings.fogDensity = a.densidadNiebla;
            if (sky != null)
            {
                sky.SetFloat("_Exposure", a.exposicion);
                sky.SetFloat("_AtmosphereThickness", a.atmosfera);
                sky.SetColor("_SkyTint", a.tinte);
            }
        }
    }
}
