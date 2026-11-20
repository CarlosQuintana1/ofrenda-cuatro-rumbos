using System.Collections.Generic;
using System.IO;
using MemoriaViva;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static GeoKit;

// Generador de la escena: explanada de la Facultad de Informatica UAQ (Juriquilla)
// con la Ofrenda de los Cuatro Rumbos en el medallon central.
// Norte = +Z (estacionamiento), sur = -Z (edificio curvo y gradas), este = +X.
public static partial class BuildMemoriaViva
{
    private const string Root = "Assets/MemoriaViva";
    private const string ScenePath = "Assets/Scenes/MemoriaViva.unity";
    private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
    private static Transform sceneRoot;
    private static Transform currentGroup;
    private static System.Random random;
    private static Font font;
    private static Light sunLight, moonLight;

    private static float Rnd() => (float)random.NextDouble();
    private static float Rnd(float a, float b) => Mathf.Lerp(a, b, Rnd());

    public static void BuildValidateCapture()
    {
        Generate();
        ValidateScene();
        CapturePreview();
    }

    [MenuItem("Memoria Viva/Borrar nombres de visitantes")]
    public static void BorrarVisitantes()
    {
        PlayerPrefs.DeleteKey("MemoriaViva.Visitantes");
        Debug.Log("MEMORIA VIVA: nombres de visitantes borrados.");
    }

    [MenuItem("Memoria Viva/Reconstruir escena")]
    public static void Generate()
    {
        random = new System.Random(20261102);
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "MemoriaViva");
        foreach (var sub in new[] { "Meshes", "Materials", "Texturas" })
            if (AssetDatabase.IsValidFolder(Root + "/" + sub)) AssetDatabase.DeleteAsset(Root + "/" + sub);
        AssetDatabase.CreateFolder(Root, "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        Mats.Clear();
        GeoKit.Reset();
        GeoKit.Materials = Mats;
        GeoKit.MeshFolder = Root + "/Meshes";
        TexKit.Folder = Root + "/Texturas";
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        QualitySettings.pixelLightCount = 12;
        QualitySettings.shadowDistance = 90;
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowCascades = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

        CreateMaterials();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        sceneRoot = new GameObject("MEMORIA VIVA | Facultad de Informática UAQ").transform;
        currentGroup = null;

        Lighting();
        Terreno();
        Plaza();
        Gradas();
        EdificioPrincipal();
        EdificiosVecinos();
        Entorno();
        Mobiliario();
        Vegetacion();
        Ofrenda();
        Decoracion();
        Fiesta();
        Player();
        FlushPending();
        var cicloSo = new SerializedObject(Object.FindFirstObjectByType<CicloDeLuz>());
        cicloSo.FindProperty("reflectores").objectReferenceValue = reflectoresGO;
        var nocturnos = cicloSo.FindProperty("nocturnos");
        var brillan = new[] { "tapete_0", "tapete_1", "tapete_2", "tapete_3", "alebrije_0", "alebrije_1", "camino_0", "camino_1", "camino_2", "camino_3", "jaguar_0", "jaguar_1" };
        nocturnos.arraySize = brillan.Length;
        for (int i = 0; i < brillan.Length; i++) nocturnos.GetArrayElementAtIndex(i).objectReferenceValue = Mats[brillan[i]];
        cicloSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("MEMORIA VIVA: escena creada y guardada correctamente.");
    }

    // ---------- grupos ----------

    private static void FlushPending()
    {
        if (currentGroup != null) GeoKit.Flush(currentGroup, currentGroup.name);
    }

    private static Transform Group(string name)
    {
        FlushPending();
        currentGroup = new GameObject(name).transform;
        currentGroup.SetParent(sceneRoot);
        return currentGroup;
    }

    // Pieza independiente con pivote propio (para animarla o activarla aparte).
    private static Transform Piece(string name, Vector3 origin, System.Action build, Transform parent = null)
    {
        var saved = GeoKit.Swap(null);
        var t = new GameObject(name).transform;
        t.SetParent(parent != null ? parent : currentGroup);
        t.position = origin;
        build();
        GeoKit.Flush(t, name, origin, false);
        GeoKit.Swap(saved);
        return t;
    }

    private static GameObject ColliderBox(string name, Vector3 center, Vector3 size, float yaw = 0)
    {
        var go = new GameObject("Colisión | " + name);
        go.transform.SetParent(currentGroup);
        go.transform.position = center;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    // ---------- materiales ----------

    private static Material Mat(string name, Color c, float smooth = .25f, float metal = 0, Texture2D tex = null,
        float emission = 0, Color? emColor = null)
    {
        var m = new Material(Shader.Find("Standard")) { name = name };
        m.color = c;
        m.SetFloat("_Glossiness", smooth);
        m.SetFloat("_Metallic", metal);
        if (tex != null) m.mainTexture = tex;
        if (emission > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", (emColor ?? c) * emission);
            if (tex != null) m.SetTexture("_EmissionMap", tex);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        m.enableInstancing = true;
        Mats[name] = m;
        return m;
    }

    private static Material Cutout(Material m)
    {
        m.SetFloat("_Mode", 1);
        m.SetOverrideTag("RenderType", "TransparentCutout");
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
        m.SetFloat("_Cutoff", .5f);
        return m;
    }

    private static Material Transparent(Material m)
    {
        m.SetFloat("_Mode", 3);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        return m;
    }

    private static Material SpriteMat(string name, string shader, Texture2D tex, Color c, float intensity, float flicker = -1)
    {
        var s = Shader.Find(shader);
        if (s == null) throw new System.Exception("Falta el shader " + shader);
        var m = new Material(s) { name = name };
        m.mainTexture = tex;
        m.SetColor("_Color", c);
        m.SetFloat("_Intensity", intensity);
        if (flicker >= 0) m.SetFloat("_Flicker", flicker);
        Mats[name] = m;
        return m;
    }

    private static void SaveMaterials()
    {
        foreach (var pair in Mats)
            AssetDatabase.CreateAsset(pair.Value, Root + "/Materials/" + Sanitize(pair.Key) + ".mat");
    }

    private static string Sanitize(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('|', '_').Replace(' ', '_');
    }

    private static void CreateMaterials()
    {
        var ladrillo = TexKit.Ladrillo();
        var hexagonal = TexKit.Hexagonal();
        var concreto = TexKit.Concreto("concreto", new Color(.72f, .72f, .70f), 3, .09f);
        var estampado = TexKit.Concreto("estampado", new Color(.80f, .64f, .57f), 9, .07f);
        var asfalto = TexKit.Concreto("asfalto", new Color(.25f, .25f, .26f), 13, .12f);
        var pasto = TexKit.Pasto();
        var fachada = TexKit.Fachada();
        var fachadaBlanca = TexKit.Fachada("fachada_blanca", new Color(.95f, .95f, .92f));
        var madera = TexKit.Madera();
        var petate = TexKit.Petate();
        var medallon = TexKit.Medallon();
        var papel = TexKit.PapelPicado();
        var halo = TexKit.Halo();
        var humo = TexKit.Humo();
        var flama = TexKit.Flama();
        var monarca = TexKit.Monarca();
        var retrato = TexKit.Retrato();
        var nubes = TexKit.Nubes();
        var petalos = TexKit.Petalos();
        var roseta = TexKit.Roseta();
        var fronda = TexKit.Fronda();
        var follajeTex = TexKit.Follaje();
        var mancha = TexKit.Mancha();

        // Facultad
        Mat("fachada", Color.white, .12f, 0, fachada);
        Mat("fachada_blanca", Color.white, .12f, 0, fachadaBlanca);
        Mat("remate", new Color(.95f, .92f, .84f), .15f);
        Mat("vidrio", new Color(.36f, .64f, .58f), .88f, .15f);
        Mat("vidrio_oscuro", new Color(.07f, .11f, .12f), .9f, .1f);
        Mat("ventana_luz", new Color(.95f, .80f, .55f), .6f, 0, null, .9f, new Color(1f, .76f, .45f));
        Mat("marco", new Color(.45f, .21f, .15f), .3f);
        Mat("ojo_buey", new Color(.13f, .15f, .16f), .7f, .1f);
        Mat("foco", new Color(1f, .93f, .78f), .2f, 0, null, 1.6f);
        Mat("blanco_edificio", new Color(.94f, .95f, .93f), .15f);
        Mat("panel_solar", Color.white, .85f, .3f, TexKit.PanelSolar());
        Mat("azotea", Color.white, .08f, 0, TexKit.Concreto("azotea", new Color(.8f, .78f, .73f), 21, .1f));
        Mat("pasto_seco", Color.white, .04f, 0, TexKit.Concreto("pasto_seco", new Color(.6f, .55f, .36f), 33, .2f));
        Mat("metal", new Color(.42f, .43f, .45f), .5f, .6f);
        Mat("rojo_edificio", new Color(.74f, .30f, .20f), .15f);

        // Pavimentos y terreno
        Mat("ladrillo", Color.white, .12f, 0, ladrillo);
        // La celda hexagonal mide 4·raiz(3) por 6 radios; v se repite con esa proporcion.
        Mat("hexagonal", Color.white, .12f, 0, hexagonal).mainTextureScale = new Vector2(1, 4 * Mathf.Sqrt(3) / 6f);
        Mat("barandal", new Color(.42f, .27f, .23f), .35f, .3f);
        Mat("concreto", Color.white, .1f, 0, concreto);
        Mat("gradas", new Color(.93f, .93f, .92f), .08f, 0, concreto);
        Mat("estampado", Color.white, .42f, 0, estampado);
        Mat("medallon", Color.white, .45f, 0, medallon);
        Mat("pasto", Color.white, .05f, 0, pasto);
        Mat("asfalto", Color.white, .1f, 0, asfalto);
        Mat("tierra", new Color(.46f, .40f, .30f), .05f, 0, concreto);
        Mat("colinas", new Color(.30f, .31f, .22f), .02f);
        Mat("linea", new Color(.92f, .92f, .88f), .2f);
        Mat("banqueta", new Color(.80f, .79f, .76f), .1f, 0, concreto);

        // Mobiliario y vegetacion
        Mat("lona", new Color(.95f, .95f, .93f), .2f);
        Mat("mesa", new Color(.52f, .53f, .54f), .35f, .2f);
        Mat("tronco_palma", new Color(.44f, .40f, .35f), .08f);
        Mat("hoja_palma", new Color(.24f, .36f, .13f), .3f);
        Mat("hoja_seca", new Color(.62f, .54f, .27f), .2f);
        Mat("follaje", new Color(.20f, .33f, .14f), .15f);
        Mat("arbusto_seco", new Color(.48f, .45f, .30f), .1f);
        Mat("zacate", new Color(.70f, .62f, .38f), .15f);
        Mat("piedra_blanca", new Color(.93f, .92f, .88f), .2f);
        Mat("roca", new Color(.55f, .52f, .48f), .1f, 0, concreto);
        Mat("verde_senal", new Color(.05f, .55f, .3f), .3f);
        Mat("azul_marino", new Color(.07f, .1f, .17f), .45f, .2f);
        Mat("plata", new Color(.82f, .83f, .85f), .75f, .6f);
        Mat("arena", new Color(.72f, .62f, .48f), .35f, .2f);
        Mat("concreto_sucio", new Color(.62f, .62f, .6f), .05f, 0, concreto);
        Mat("agave", new Color(.36f, .50f, .42f), .35f);
        Mat("bote_azul", new Color(.12f, .30f, .70f), .4f);
        Mat("bote_amarillo", new Color(.95f, .75f, .10f), .4f);
        Mat("bote_rojo", new Color(.80f, .15f, .12f), .4f);
        Mat("bote_gris", new Color(.45f, .47f, .48f), .4f);
        Color[] cars = { new Color(.62f, .08f, .08f), new Color(.88f, .88f, .89f), new Color(.1f, .1f, .12f), new Color(.6f, .62f, .65f), new Color(.75f, .72f, .62f), new Color(.15f, .22f, .45f), new Color(.38f, .4f, .43f) };
        for (int i = 0; i < cars.Length; i++) Mat("auto" + i, cars[i], .7f, .4f);

        // Ofrenda
        Mat("piedra", new Color(.30f, .28f, .27f), .2f, 0, concreto);
        Mat("cantera", new Color(.86f, .66f, .58f), .15f, 0, concreto);
        Mat("petate", Color.white, .05f, 0, petate);
        Mat("mantel_morado", new Color(.36f, .09f, .46f), .55f);
        Mat("mantel_blanco", new Color(.9f, .86f, .78f), .3f);
        Mat("mantel_naranja", new Color(.98f, .47f, .05f), .45f);
        Mat("mantel_rosa", new Color(.89f, 0f, .49f), .45f);
        Mat("cempasuchil", new Color(1f, .9f, .8f), .15f, 0, petalos, .12f, new Color(1f, .55f, .05f)).mainTextureScale = new Vector2(7, 4);
        Mat("cempasuchil_oscuro", new Color(1f, .7f, .55f), .15f, 0, petalos, .1f, new Color(1f, .4f, .03f)).mainTextureScale = new Vector2(7, 4);
        Mat("terciopelo", new Color(.72f, .05f, .32f), .3f, 0, null, .08f);
        Mat("carrizo", new Color(.62f, .52f, .32f), .3f);
        Mat("cera", new Color(.97f, .94f, .86f), .35f);
        Mat("veladora_roja", new Color(.75f, .10f, .10f), .85f, 0, null, .25f);
        Mat("veladora_blanca", new Color(.95f, .92f, .85f), .85f, 0, null, .15f);
        Mat("veladora_ambar", new Color(.95f, .55f, .10f), .85f, 0, null, .25f);
        Mat("barro", new Color(.64f, .31f, .17f), .25f);
        Mat("barro_oscuro", new Color(.38f, .17f, .10f), .3f);
        Mat("brasa", new Color(1f, .3f, .05f), .2f, 0, null, 2f);
        Mat("pan", new Color(.80f, .50f, .22f), .3f);
        Mat("pan_azucar", new Color(.93f, .80f, .62f), .2f);
        Mat("azucar", new Color(.98f, .96f, .93f), .35f);
        Mat("naranja", new Color(1f, .50f, .05f), .5f);
        Mat("tejocote", new Color(.96f, .62f, .10f), .55f);
        Mat("guayaba", new Color(.80f, .82f, .30f), .45f);
        Mat("platano", new Color(.98f, .82f, .20f), .4f);
        Mat("cana", new Color(.55f, .60f, .26f), .4f);
        Mat("cana_nudo", new Color(.40f, .42f, .18f), .3f);
        Mat("mole", new Color(.24f, .07f, .03f), .65f);
        Mat("chocolate", new Color(.35f, .17f, .08f), .5f);
        Mat("hoja_maiz", new Color(.85f, .76f, .52f), .2f);
        Mat("sal", new Color(.98f, .98f, .98f), .3f);
        Mat("barro_brunido", new Color(.60f, .27f, .13f), .72f);
        Mat("madera", Color.white, .25f, 0, madera);
        Mat("marco_dorado", new Color(.85f, .60f, .16f), .6f, .7f);
        Mat("retrato", Color.white, .3f, 0, retrato);
        Mat("rosa", new Color(.95f, .35f, .62f), .4f);
        Mat("turquesa", new Color(.08f, .72f, .72f), .4f);
        Mat("amarillo", new Color(1f, .82f, .12f), .4f);
        Mat("verde", new Color(.28f, .70f, .22f), .4f);
        Mat("negro", new Color(.05f, .05f, .05f), .3f);
        Mat("cordel", new Color(.92f, .90f, .82f), .2f);
        Mat("farol_naranja", new Color(1f, .52f, .08f), .2f, 0, null, 1.4f);
        Mat("farol_rosa", new Color(1f, .25f, .58f), .2f, 0, null, 1.4f);
        Mat("farol_morado", new Color(.62f, .25f, 1f), .2f, 0, null, 1.4f);
        Mat("guia", new Color(.15f, .85f, .82f), .2f, 0, null, 1.5f);
        Mat("hoja_flor", new Color(.16f, .34f, .12f), .3f);
        Mat("nube", new Color(.97f, .97f, .95f), .2f, 0, null, .1f);
        Mat("ceniza", new Color(.45f, .44f, .42f), .05f);
        Mat("masa", new Color(.93f, .84f, .55f), .25f);
        Cutout(Mat("roseta", new Color(1f, .9f, .75f), .2f, 0, roseta, .15f, new Color(1f, .55f, .05f)));
        Cutout(Mat("roseta_oscura", new Color(1f, .62f, .45f), .2f, 0, roseta, .12f, new Color(1f, .4f, .03f)));
        Cutout(Mat("fronda", Color.white, .25f, 0, fronda)).SetFloat("_Cutoff", .4f);
        Cutout(Mat("fronda_seca", new Color(1.4f, 1.1f, .6f), .2f, 0, fronda)).SetFloat("_Cutoff", .4f);
        Cutout(Mat("hojas", Color.white, .2f, 0, follajeTex)).SetFloat("_Cutoff", .45f);
        Transparent(Mat("mancha", Color.white, .05f, 0, mancha));
        Mat("piel1", new Color(.62f, .43f, .32f), .3f); Mat("piel2", new Color(.47f, .31f, .22f), .3f); Mat("piel3", new Color(.75f, .56f, .43f), .3f);
        Mat("ropa1", new Color(.12f, .14f, .25f), .2f); Mat("ropa2", new Color(.7f, .1f, .12f), .2f); Mat("ropa3", new Color(.85f, .85f, .82f), .2f);
        Mat("ropa4", new Color(.15f, .15f, .15f), .2f); Mat("ropa5", new Color(.25f, .45f, .6f), .2f); Mat("mezclilla", new Color(.2f, .27f, .42f), .2f);
        Mat("pelo", new Color(.08f, .06f, .05f), .35f);
        Mat("muro_crema", new Color(.92f, .88f, .76f), .1f, 0, concreto);
        Mat("muro_blanco", new Color(.95f, .95f, .93f), .1f, 0, concreto);
        Mat("manta", new Color(.1f, .16f, .4f), .3f);
        Mat("linea_amarilla", new Color(.95f, .75f, .1f), .2f);
        Mat("azul_disc", new Color(.12f, .35f, .78f), .3f);
        Mat("block", new Color(.62f, .62f, .6f), .05f, 0, concreto);
        Mat("ciprés", new Color(.1f, .2f, .1f), .15f);
        // Autos de Kenney (CC0), con la paleta recoloreada a colores de autos reales.
        foreach (var c in new[] { "blanco", "blanco", "plata", "plata", "gris", "negro", "rojo", "azul" })
        {
            string tp = "Assets/MemoriaViva/Modelos/Autos/Textures/colormap_" + c + ".png";
            // Paleta por celdas: sin mipmaps, para que de lejos no se mezclen los colores vecinos.
            if (AssetImporter.GetAtPath(tp) is TextureImporter ti && ti.mipmapEnabled)
            {
                ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
            if (tex != null && !Mats.ContainsKey("auto_" + c)) Mat("auto_" + c, Color.white, .55f, .25f, tex);
        }
        Mat("reflector", new Color(1f, 1f, .95f), .2f, 0, null, 3f);
        Mat("anillo_ritual", new Color(1f, .7f, .25f), .3f, .5f, null, 1.2f);
        Cutout(Mat("papel", Color.white, .1f, 0, papel, .35f, Color.white));
        Transparent(Mat("vidrio_vaso", new Color(.85f, .95f, 1f, .22f), .95f));
        Transparent(Mat("agua", new Color(.35f, .62f, .85f, .55f), .95f));

        // Sprites animados
        var suelo = new Material(Shader.Find("MemoriaViva/LuzSuelo")) { name = "luz_suelo", mainTexture = halo };
        suelo.SetColor("_Color", new Color(1f, .55f, .22f));
        suelo.SetFloat("_Intensity", .55f);
        Mats["luz_suelo"] = suelo;
        var bandera = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imagenes/bandera-troyanos.jpg");
        if (bandera != null) Mat("bandera", Color.white, .1f, 0, bandera, .35f);
        Mats["texto"] = new Material(Shader.Find("MemoriaViva/Texto")) { name = "texto", mainTexture = font.material.mainTexture };
        SpriteMat("halo_calido", "MemoriaViva/Halo", halo, new Color(1f, .62f, .25f), .55f, .18f);
        SpriteMat("halo_suave", "MemoriaViva/Halo", halo, new Color(1f, .70f, .40f), .7f, .05f);
        SpriteMat("flama", "MemoriaViva/Halo", flama, Color.white, 1.4f, .25f);
        SpriteMat("anima", "MemoriaViva/Anima", halo, new Color(1f, .85f, .6f), 1.2f);
        SpriteMat("mariposa", "MemoriaViva/Mariposa", monarca, Color.white, 1f);
        SpriteMat("humo", "MemoriaViva/Flotante", humo, new Color(.85f, .82f, .78f, .55f), 1f);
        SpriteMat("petalo_cae", "MemoriaViva/Flotante", halo, new Color(1f, .55f, .05f, 1f), 1.2f);

        var nubesMat = new Material(Shader.Find("MemoriaViva/Nubes")) { name = "nubes", mainTexture = nubes };
        nubesMat.mainTextureScale = new Vector2(3, 3);
        Mats["nubes"] = nubesMat;
        foreach (var persona in Personas)
        {
            var foto = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Fotos/" + persona.foto + ".jpg");
            if (foto != null) Mat("retrato_" + persona.foto, Color.white, .04f, 0, foto); // mate, para que el brillo no se coma la foto
        }

        Mat("hueso", new Color(.96f, .94f, .9f), .15f, 0, null, .06f);
        Mat("tela_magenta", new Color(.82f, .1f, .45f), .08f);
        Mat("tela_morada", new Color(.36f, .1f, .5f), .08f);
        Mat("tela_naranja", new Color(1f, .5f, .06f), .08f);
        Mat("tela_negra", new Color(.05f, .05f, .07f), .08f);
        Mat("tela_turquesa", new Color(.04f, .6f, .66f), .08f);
        Mat("tela_blanca", new Color(.95f, .93f, .9f), .08f);
        Mat("xolo", new Color(.17f, .16f, .17f), .66f);
        Mat("xolo_rosa", new Color(.72f, .44f, .44f), .4f);
        Mat("ambar", new Color(.95f, .6f, .12f), .9f, 0, null, .35f);
        for (int i = 0; i < 2; i++) Mat("alebrije_" + i, Color.white, .55f, 0, TexKit.Alebrije(i), .6f, Color.white);
        for (int i = 0; i < 4; i++) Mat("tapete_" + i, Color.white, .04f, 0, TexKit.Tapete(i), 1f, Color.white);
        for (int i = 0; i < 4; i++) Mat("camino_" + i, Color.white, .04f, 0, TexKit.Camino(i), 1f, Color.white);
        for (int i = 0; i < 2; i++) Mat("jaguar_" + i, Color.white, .5f, 0, TexKit.JaguarHuesos(i), .5f, Color.white);
        EspiralMat("espiral_morada", new Color(.42f, .15f, .55f), new Color(.85f, .7f, .95f));
        EspiralMat("espiral_azul", new Color(.15f, .3f, .7f), new Color(.7f, .85f, 1f));
        Mat("cuero", new Color(.86f, .74f, .55f), .2f);
        Mat("letra_crema", new Color(1f, .9f, .62f), .45f, 0, null, .12f); Mat("letra_morada", new Color(.45f, .12f, .45f), .4f);
        Mat("letra_cafe", new Color(.28f, .16f, .12f), .35f); Mat("letra_negra", new Color(.08f, .08f, .08f), .5f);
        Mat("troyano_azul", new Color(.12f, .28f, .72f), .35f); Mat("troyano_celeste", new Color(.38f, .72f, .95f), .35f, 0, null, .08f);
        Mat("tablero_morado", new Color(.17f, .06f, .22f), .18f); Mat("letra_naranja", new Color(1f, .56f, .14f), .4f, 0, null, .1f);
        Mat("letra_verde", new Color(.25f, .65f, .2f), .4f);
        Mats["estrellas"] = new Material(Shader.Find("MemoriaViva/Estrellas")) { name = "estrellas", mainTexture = halo };
        Mats["luna"] = new Material(Shader.Find("MemoriaViva/Estrellas")) { name = "luna", mainTexture = TexKit.Luna() };
        Mats["luna"].SetFloat("_Titileo", 0);
        Mat("foco_rojo", new Color(1f, .3f, .25f), .6f, 0, null, 1.4f); Mat("foco_ambar", new Color(1f, .7f, .25f), .6f, 0, null, 1.4f);
        Mat("foco_verde", new Color(.35f, 1f, .45f), .6f, 0, null, 1.4f); Mat("foco_rosa", new Color(1f, .4f, .8f), .6f, 0, null, 1.4f);
        Mat("foco_azul", new Color(.4f, .6f, 1f), .6f, 0, null, 1.4f);
        Mat("madera_guitarra", new Color(.78f, .48f, .2f), .7f);
        Mat("madera_oscura", new Color(.28f, .14f, .06f), .6f);
        Mat("pluma_verde", new Color(.1f, .6f, .25f), .5f);
        Mat("tela_roja", new Color(.78f, .08f, .1f), .08f);
        Mat("ropa6", new Color(.95f, .75f, .15f), .2f); Mat("ropa7", new Color(.35f, .55f, .3f), .2f); Mat("ropa8", new Color(.6f, .3f, .55f), .2f);
        Mat("pelo2", new Color(.3f, .18f, .1f), .35f); Mat("tenis", new Color(.92f, .92f, .9f), .3f);
        Mat("labios", new Color(.6f, .3f, .28f), .4f); Mat("iris", new Color(.22f, .13f, .07f), .7f);
        Mat("piel4", new Color(.55f, .37f, .26f), .3f); Mat("pelo3", new Color(.45f, .3f, .15f), .35f); Mat("pelo4", new Color(.35f, .12f, .4f), .35f);

        SaveMaterials();
    }

    // ---------- luz, cielo, jugador ----------

    private static void Lighting()
    {
        Group("01 | Luz, cielo y ambientes");
        var sky = new Material(Shader.Find("Skybox/Procedural")) { name = "cielo" };
        sky.SetFloat("_SunDisk", 2);
        sky.SetFloat("_SunSize", .045f);
        sky.SetFloat("_SunSizeConvergence", 6);
        sky.SetColor("_GroundColor", new Color(.30f, .27f, .25f));
        AssetDatabase.CreateAsset(sky, Root + "/Materials/cielo.mat");
        RenderSettings.skybox = sky;

        sunLight = new GameObject("Sol").AddComponent<Light>();
        sunLight.transform.SetParent(currentGroup);
        sunLight.type = LightType.Directional;
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = .85f;
        sunLight.shadowBias = .04f;
        RenderSettings.sun = sunLight;

        moonLight = new GameObject("Luna | relleno azul").AddComponent<Light>();
        moonLight.transform.SetParent(currentGroup);
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(.45f, .58f, .95f);
        moonLight.shadows = LightShadows.None;
        moonLight.transform.rotation = Quaternion.Euler(38, -140, 0);

        Quad("nubes", new Vector3(-2500, 420, -2500), new Vector3(-2500, 420, 2500), new Vector3(2500, 420, 2500), new Vector3(2500, 420, -2500), Vector3.down,
            Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        var presets = CicloDeLuz.Presets();
        CicloDeLuz.Apply(presets[0], sunLight, moonLight, sky);
        Mats["nubes"].color = presets[0].nubes;
        var ciclo = new GameObject("Ciclo de luz | tecla N").AddComponent<CicloDeLuz>();
        ciclo.transform.SetParent(currentGroup);
        var so = new SerializedObject(ciclo);
        so.FindProperty("sun").objectReferenceValue = sunLight;
        so.FindProperty("moon").objectReferenceValue = moonLight;
        so.FindProperty("litWindows").objectReferenceValue = Mats["ventana_luz"];
        so.FindProperty("clouds").objectReferenceValue = Mats["nubes"];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Player()
    {
        Group("99 | Explorador de escritorio y visor XR");
        var rig = new GameObject("Explorador VR | origen en suelo");
        rig.transform.SetParent(currentGroup);
        // Arranca bajo el arco de entrada, mirando por el camino de aserrin hacia la ofrenda.
        rig.transform.position = Inicio;
        rig.transform.rotation = Quaternion.LookRotation(-new Vector3(Inicio.x, 0, Inicio.z));
        var camGo = new GameObject("Vista | cámara estereoscópica");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        camGo.transform.localPosition = Vector3.up * 1.65f;
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 72;
        cam.nearClipPlane = .07f;
        cam.farClipPlane = 1800;
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<BrilloNocturno>();
        var explorer = rig.AddComponent<VRExplorer>();
        var serialized = new SerializedObject(explorer);
        serialized.FindProperty("viewCamera").objectReferenceValue = cam;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMesh Text(string content, Vector3 pos, Quaternion rot, float size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, Transform parent = null)
    {
        var go = new GameObject("Texto | " + content.Split('\n')[0]);
        go.transform.SetParent(parent != null ? parent : currentGroup);
        go.transform.position = pos;
        go.transform.rotation = rot;
        var t = go.AddComponent<TextMesh>();
        t.font = font;
        t.text = content;
        t.fontSize = 96;
        t.characterSize = size / 9.6f;
        t.anchor = anchor;
        t.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft ? TextAlignment.Left : TextAlignment.Center;
        t.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mats["texto"];
        return t;
    }

    private static void PointLight(string name, Vector3 pos, Color color, float intensity, float range, bool flicker, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : currentGroup);
        go.transform.position = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        if (flicker) go.AddComponent<Parpadeo>();
    }

    // ---------- validacion y vistas previas ----------

    public static void ValidateScene()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var camera = Camera.main;
        var explorer = Object.FindFirstObjectByType<VRExplorer>();
        var director = Object.FindFirstObjectByType<ExperienceDirector>();
        var stations = Object.FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None);
        if (camera == null || explorer == null || director == null)
            throw new System.Exception("Falta la cámara, el explorador o el director.");
        int offerings = 0, stories = 0, travel = 0;
        foreach (var s in stations)
        {
            if (s.GetComponent<Collider>() == null) throw new System.Exception("Sin colisión: " + s.name);
            if (s.action == WorldInteractable.Action.Offering) offerings++;
            if (s.action == WorldInteractable.Action.Story) stories++;
            if (s.action == WorldInteractable.Action.Teleport) travel++;
        }
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.sharedMaterial == null) throw new System.Exception("Material vacío en " + r.name);
        if (offerings != 4 || stories < 14 || travel < 4)
            throw new System.Exception($"Interacciones incompletas: rituales {offerings}, significados {stories}, viajes {travel}");
        long verts = 0; int renderers = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) { if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount; renderers++; }
        Debug.Log($"MEMORIA VIVA: validación correcta. {offerings} rituales, {stories} significados, {travel} puntos de viaje. {renderers} mallas, {verts} vértices.");
    }

    public static void CapturePreview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("Capturas");
        var camGo = new GameObject("Cámara de captura");
        var cam = camGo.AddComponent<Camera>();
        var glow = camGo.AddComponent<BrilloNocturno>();
        cam.fieldOfView = 60;
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 1800;
        cam.clearFlags = CameraClearFlags.Skybox;
        var main = Camera.main;
        if (main != null) main.enabled = false;

        void Shot(string file, Vector3 from, Vector3 to, float fov = 60)
        {
            from = Lay(from.x, from.y, from.z);
            to = Lay(to.x, to.y, to.z);
            cam.fieldOfView = fov;
            cam.transform.position = from;
            cam.transform.LookAt(to);
            SaveCameraImage(cam, "Capturas/" + file + ".png");
        }

        ShotW("01_entrada", Inicio + Vector3.up * 1.65f, new Vector3(0, 2.4f, 0), 72);
        Shot("02_ofrenda_fuego", new Vector3(0, 1.65f, 9.5f), new Vector3(0, 2.3f, 0));
        Shot("03_desde_gradas", new Vector3(-2, 2.1f + 1.65f, -15.8f), new Vector3(0, 1.5f, 6));
        Shot("04_dron", new Vector3(0, 32, 38), new Vector3(0, 0, -8), 55);
        Shot("05_lateral_noroeste", new Vector3(-17, 1.65f, 18), new Vector3(3, 6, -16));
        Shot("06_vela_noreste", new Vector3(-9, 1.65f, 5), new Vector3(17.5f, 7, 21.5f));
        Shot("07_fachada", new Vector3(-6.5f, 2.1f + 1.65f, -15.4f), new Vector3(12, 8, -15), 70);
        Shot("08_cenital", new Vector3(10, 110, 30.01f), new Vector3(10, 0, 30), 60);
        ShotW("61_planta_oriente", GeoKit.Pos(105, 30f) + Vector3.up * 45, GeoKit.Pos(105, 30f) + new Vector3(0, 0, -.01f), 60);
        Shot("16_como_foto_letrero", new Vector3(-13, 1.4f, 43), new Vector3(20, 5.5f, 29), 62);
        Shot("17_como_foto_gradas_este", GeoKit.Pos(122, 15.6f, 2.1f + 1.5f), new Vector3(22, 6, 24), 70);
        Shot("18_como_foto_troyanos", GeoKit.Pos(150, 13.5f, 1.6f), GeoKit.Pos(298, 19, 1.8f), 62);
        Shot("19_troyanos", GeoKit.Pos(296, 15.6f, 1.2f), GeoKit.Pos(298f, 19.5f, 2.6f), 60);
        Shot("20_jardinera", new Vector3(-12, 1.5f, 22.5f), new Vector3(-2, 0, 30), 62);
        // Shot amplia el trazado por E; el altar no se amplia, por eso se divide.
        Shot("21_altar_detalle", new Vector3(5.2f, 1.7f, 4.1f) / 1.25f, new Vector3(2.4f, 1.1f, 2.2f) / 1.25f, 55);
        Shot("22_arco_cerca", new Vector3(5.6f, 1.6f, 1.6f) / 1.25f, new Vector3(3.5f, 2.6f, 3.5f) / 1.25f, 55);
        ShotW("23_edificio_oriente", GeoKit.Pos(90, 24f) + Vector3.up * 1.7f, GeoKit.Pos(99, 45f) + Vector3.up * 6f, 60);
        Shot("24_jardinera_sendero", new Vector3(-6, 1.7f, 24), new Vector3(.4f, 0, 33.6f), 62);
        Shot("25_como_foto_estacionamiento", new Vector3(8f, 7.5f, 24f), new Vector3(-4f, 0, 58f), 70);
        Shot("26_cipreses", new Vector3(24, 1.7f, 4), new Vector3(32, 4, 20), 65);
        Shot("27_zona_oriente", GeoKit.Pos(50, 27.5f, 1.7f) / 1.25f, GeoKit.Pos(108, 27f, 2.2f) / 1.25f, 68);
        Shot("28_rampa_cerca", GeoKit.Pos(70, 21.5f, 1.7f) / 1.25f, GeoKit.Pos(100, 24f, 1.4f) / 1.25f, 66);
        ShotW("59_rampa_plaza", GeoKit.Pos(92, 16f) + Vector3.up * 1.7f, GeoKit.Pos(112, 25f) + Vector3.up * 1.2f, 72);
        ShotW("60_rampa_arriba", GeoKit.Pos(80, 30f) + Vector3.up * 1.7f, GeoKit.Pos(108, 30f) + Vector3.up * 1f, 72);
        ShotW("62_como_streetview", GeoKit.Pos(98, 21.5f) + Vector3.up * 1.6f, GeoKit.Pos(116, 26f) + Vector3.up * 2.4f, 75);
        // Mismos encuadres que las fotos de los dos edificios del oriente.
        ShotW("65_como_foto_plaza_oriente", GeoKit.Pos(322, 14.5f) + Vector3.up * 1.6f, GeoKit.Pos(80, 40f) + Vector3.up * 6f, 72);
        ShotW("66_como_foto_rampa", GeoKit.Pos(112, 23.6f) + new Vector3(.45f, 3.7f, 1.1f), new Vector3(43, 5, 13.6f), 72);
        ShotW("67_como_foto_puntos", new Vector3(38.5f, 1.6f, 7.5f), new Vector3(28, 4, -16), 72);
        ShotW("68_como_foto_letrero", new Vector3(-8, 1.6f, 40), new Vector3(30, 7, 22), 66);
        ShotW("69_andador_letrero", new Vector3(19.7f, 1.6f, 26.2f), new Vector3(43.3f, 4, 41.3f), 66);
        ShotW("70_planta_oriente", new Vector3(45, 110, 12), new Vector3(45, 0, 11.99f), 60);
        ShotW("71_piso_corredor", new Vector3(38, 1.6f, 2), new Vector3(22, .8f, -10), 70);
        ShotW("72_cara_norte", new Vector3(0, 1.75f, 6.2f), new Vector3(0, 1.45f, 0), 58);
        ShotW("73_cara_oriente", new Vector3(6.2f, 1.75f, 0), new Vector3(0, 1.45f, 0), 58);
        ShotW("74_cara_poniente", new Vector3(-6.2f, 1.75f, 0), new Vector3(0, 1.45f, 0), 58);
        ShotW("75_cara_sur", new Vector3(0, 1.75f, -6.2f), new Vector3(0, 1.45f, 0), 58);
        ShotW("83_planta_entrada", new Vector3(-8, 70, 33), new Vector3(-8, 0, 32.99f), 36);
        ShotW("84_campo_troyano", new Vector3(-14.6f, 7.5f, 14.4f), new Vector3(-17.2f, 0, 26.6f), 55);
        ShotW("85_campo_troyano_puesto", new Vector3(-12.5f, 1.65f, 19.5f), new Vector3(-17.2f, .3f, 26.6f), 62);
        ShotW("93_sentados", GeoKit.Pos(176, 14.6f) + Vector3.up * 1.2f, GeoKit.Pos(196, 15.8f) + Vector3.up * 1f, 60);
        ShotW("95_catrin_troyano", GeoKit.Pos(176, 13f) + Vector3.up * 3f, GeoKit.Pos(182, 29.6f) + Vector3.up * 19f, 40);
        ShotW("97_catrin_troyano_cerca", GeoKit.Pos(182, 22.6f) + Vector3.up * 19.5f, GeoKit.Pos(182, 29.6f) + Vector3.up * 19.3f, 45);
        ShotW("98_catrin_troyano_atras", GeoKit.Pos(177, 36.4f) + Vector3.up * 20f, GeoKit.Pos(182, 29.6f) + Vector3.up * 19f, 45);
        ShotW("96_catrin_troyano_espalda", GeoKit.Pos(176, 35.5f) + Vector3.up * 26f, new Vector3(0, 1.5f, 0), 70);
        ShotW("102_techo", GeoKit.Pos(205, 22f) + Vector3.up * 24f, GeoKit.Pos(185, 34f) + Vector3.up * 13f, 60);
        ShotW("103_poniente", GeoKit.Pos(192, 32f) + Vector3.up * 20f, Lay(-90f, 0, 5f) + Vector3.up * 4f, 70);
        ShotW("104_planta_poniente", Lay(-75f, 0, -15f) + Vector3.up * 190f, Lay(-75f, 0, -15.01f), 60);
        ShotW("105_catrina_completa", new Vector3(-2.4f, 2f, 14.6f), new Vector3(-3.8f, 2.3f, 9.3f), 38);
        ShotW("106_catrin_completo", new Vector3(2.4f, 2f, 14.6f), new Vector3(3.8f, 2.3f, 9.3f), 38);
        ShotW("107_estudiantes_cerca", new Vector3(-10.4f, 1.62f, 12.6f), new Vector3(-12.05f, 1.45f, 14.36f), 45);
        ShotW("108_capa_troyano", GeoKit.Pos(182, 33.6f) + Vector3.up * 18.2f, GeoKit.Pos(182, 29.6f) + Vector3.up * 18.1f, 40);
        ShotW("110_escudo_troyano", new Vector3(-2.83f, 18.8f, -25.98f), new Vector3(-2.07f, 18.8f, -28.88f), 32);
        ShotW("111_techo_punta", GeoKit.Pos(132, 25f) + Vector3.up * 18f, GeoKit.Pos(136, 30f) + Vector3.up * 14f, 60);
        ShotW("112_techo_oriente", GeoKit.Pos(122, 30f) + Vector3.up * 21f, GeoKit.Pos(134, 31f) + Vector3.up * 14f, 60);
        ShotW("113_techo_poniente", GeoKit.Pos(242, 30f) + Vector3.up * 21f, GeoKit.Pos(230, 31f) + Vector3.up * 14f, 60);
        ShotW("114_publico_ofrenda", GeoKit.Pos(62, 6.4f) + Vector3.up * 1.6f, GeoKit.Pos(58, 9.2f) + Vector3.up * 1.35f, 50);
        ShotW("115_danzantes_cerca", GeoKit.Pos(97, 13.2f) + Vector3.up * 1.7f, GeoKit.Pos(97, 17.6f) + Vector3.up * 1.4f, 45);
        ShotW("117_estudiantes_frente", GeoKit.Pos(315, 16.2f) + Vector3.up * 1.55f, GeoKit.Pos(315, 18f) + Vector3.up * 1.25f, 50);
        ShotW("118_estudiantes_gradas", GeoKit.Pos(203, 13.6f) + Vector3.up * 1.5f, GeoKit.Pos(198, 16.2f) + Vector3.up * 1.1f, 50);
        ShotW("92_bandera", new Vector3(-27f, 1.65f, 26f), new Vector3(-32f, 4.5f, 37f), 60);
        ShotW("86_campo_desde_plaza", GeoKit.Pos(322, 22.5f) + Vector3.up * 1.65f, new Vector3(-17.2f, .3f, 26.6f), 66);
        ShotW("79_nombres", new Vector3(3.5f, 1.62f, 0), new Vector3(2.2f, 1.45f, 0), 50);
        Shot("29_fotografias", new Vector3(0, 1.45f, -5.2f) / 1.25f, new Vector3(0, 1.15f, -2.2f) / 1.25f, 50);
        Shot("30_retrato_arriba", new Vector3(1.2f, 2.2f, -6.5f) / 1.25f, new Vector3(0, 3.4f, 0) / 1.25f, 42);
        Shot("15_estacionamiento", new Vector3(-20, 9, 18), new Vector3(15, 0, 62), 60);

        // Ofrenda completa, de noche.
        var director = Object.FindFirstObjectByType<ExperienceDirector>();
        if (director != null) director.ForceComplete();
        var presets = CicloDeLuz.Presets();
        var sun = GameObject.Find("Sol")?.GetComponent<Light>();
        var nub = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/nubes.mat");
        var moon = GameObject.Find("Luna | relleno azul")?.GetComponent<Light>();
        CicloDeLuz.Apply(presets[1], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[1].nubes;
        glow.intensity = presets[1].bloom;
        var refl = GameObject.Find("09c | Reflectores de la explanada")?.transform.Find("Luz de reflectores")?.gameObject;
        if (refl != null) refl.SetActive(true);
        Shot("09_noche_completa", new Vector3(0, 1.65f, 12f), new Vector3(0, 2.8f, 0));
        Shot("10_noche_dron", new Vector3(12, 18, 21), new Vector3(0, 1, -4), 55);
        CicloDeLuz.Apply(presets[0], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[0].nubes;
        glow.intensity = presets[0].bloom;
        Shot("11_atardecer_completa", new Vector3(5.5f, 1.65f, 7.5f), new Vector3(0, 2.4f, 0));
        Shot("12_agua_tierra", new Vector3(8.5f, 1.65f, -3), new Vector3(0, 1.6f, 0));
        CicloDeLuz.Apply(presets[2], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[2].nubes;
        glow.intensity = presets[2].bloom;
        if (refl != null) refl.SetActive(false);
        ShotW("13_dia_entrada", Inicio + Vector3.up * 1.65f, new Vector3(0, 2.4f, 0), 72);
        CicloDeLuz.Apply(presets[0], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[0].nubes;
        glow.intensity = presets[0].bloom;
        void ShotW(string file, Vector3 from, Vector3 to, float fov = 50) => Shot(file, new Vector3(from.x / E, from.y, from.z / E), new Vector3(to.x / E, to.y, to.z / E), fov);
        ShotW("32_catrines", new Vector3(0, 1.6f, 13.5f), new Vector3(0, 1.3f, 8f), 62);
        ShotW("33_catrina_cerca", new Vector3(-2.3f, 1.65f, 10.6f), new Vector3(-3.4f, 1.3f, 8.4f), 48);
        ShotW("34_catrin_cerca", new Vector3(2.3f, 1.65f, 10.6f), new Vector3(3.4f, 1.3f, 8.4f), 48);
        ShotW("35_xolo", new Vector3(-1.2f, .95f, 5.7f), new Vector3(-1.75f, .6f, 4.4f), 42);
        Vector3 ap = GeoKit.Pos(60, 12.4f);
        ShotW("36_alebrije", ap * .72f + Vector3.up * 1.5f + Vector3.Cross(Vector3.up, ap.normalized) * 1.2f, ap + Vector3.up * .8f, 50);
        Vector3 tp = Vector3.Lerp(GeoKit.Pos(348, PlazaR + .2f), Andador(-9f * E), .45f);
        ShotW("37_tapete", tp + new Vector3(1.8f, 2.6f, 2.4f), tp, 55);
        Vector3 ca0 = GeoKit.Pos(348, PlazaR + .2f), cb0 = Andador(-9f * E);
        ShotW("41_camino", Vector3.Lerp(ca0, cb0, .9f) + Vector3.up * 1.7f, Vector3.Lerp(ca0, cb0, .3f) + Vector3.up * .6f, 62);
        ShotW("42_mariachi", GeoKit.Pos(275, 13.4f) + Vector3.up * 1.6f, GeoKit.Pos(275, 17.2f) + Vector3.up * 1.2f, 55);
        ShotW("43_danzantes", GeoKit.Pos(97, 13f) + Vector3.up * 1.8f, GeoKit.Pos(97, 17.6f) + Vector3.up * 1f, 55);
        ShotW("44_jaguar", GeoKit.Pos(340, 18.4f) + Vector3.up * 1.4f, GeoKit.Pos(334, 21.3f) + Vector3.up * .7f, 50);
        ShotW("45_calaveritas", new Vector3(0, 1.1f, 13.8f), new Vector3(0, .5f, 11.2f), 55);
        ShotW("46_campo_calavera", new Vector3(0, 9f, 38f), new Vector3(0, 0, 28.5f), 50);
        Vector3 arcoC = Andador(-9f * E) - (Andador(-9f * E) - GeoKit.Pos(348, PlazaR + .2f)).normalized * 4.2f;
        ShotW("51_arco_entrada", arcoC + (Andador(-9f * E) - GeoKit.Pos(348, PlazaR)).normalized * 6f + Vector3.up * 1.7f, arcoC + Vector3.up * 2.2f, 55);
        Vector3 lb = new Vector3(-11f, 0, 30.5f), lv = (Inicio - lb); lv.y = 0; lv.Normalize();
        ShotW("52_letrero_bienvenida", lb + lv * 4.2f + Vector3.up * 1.7f, lb + Vector3.up * 2f, 50);
        ShotW("53_indicadores", Pos(348, PlazaR + 5.5f) + Vector3.up * 1.7f + Vector3.Cross(Vector3.up, (Andador(-9f * E) - GeoKit.Pos(348, PlazaR)).normalized) * .5f, Pos(348, PlazaR + 2.4f) - Vector3.Cross(Vector3.up, (Andador(-9f * E) - GeoKit.Pos(348, PlazaR)).normalized) * 2.4f + Vector3.up * 2.1f, 50);
        ShotW("54_catrines_noche_ref", new Vector3(0, 2f, 16f), new Vector3(0, 2f, 8f), 62);
        ShotW("55_juan_gabriel", new Vector3(0, 3.6f, 4.2f), new Vector3(0, 3.6f, 0), 45);
        ShotW("49_puesto_pan", GeoKit.Pos(322, 17.6f) + Vector3.up * 1.7f, GeoKit.Pos(322, 21.4f) + Vector3.up * 1.1f, 55);
        ShotW("50_puesto_dulces", GeoKit.Pos(40, 17.6f) + Vector3.up * 1.6f, GeoKit.Pos(40, 21.4f) + Vector3.up * 1f, 55);
        ShotW("47_publico", GeoKit.Pos(20, 11f) + Vector3.up * 1.7f, GeoKit.Pos(200, 6f) + Vector3.up * 1f, 60);
        CicloDeLuz.Apply(presets[1], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[1].nubes;
        glow.intensity = presets[1].bloom;
        if (refl != null) refl.SetActive(true);
        ShotW("38_tapetes_noche", tp + new Vector3(-3f, 2.2f, 6f), tp + new Vector3(0, 0, -6f), 60);
        Vector3 ca = GeoKit.Pos(348, PlazaR + .2f), cb = Andador(-9f * E);
        ShotW("39_camino_noche", Vector3.Lerp(ca, cb, .95f) + Vector3.up * 1.7f, Vector3.Lerp(ca, cb, .2f) + Vector3.up * .5f, 62);
        Vector3 mc = GeoKit.Pos(275, 17.2f), dc = GeoKit.Pos(97, 17.6f);
        ShotW("40_mariachi_noche", GeoKit.Pos(262, 15.5f) + Vector3.up * 2.4f, mc + Vector3.up * 1.2f, 58);
        ShotW("56_danzantes_noche", dc * .76f + Vector3.up * 1.7f, dc + Vector3.up * 1f, 58);
        ShotW("89_juan_gabriel_noche", new Vector3(0, 3.6f, 4.2f), new Vector3(0, 3.6f, 0), 45);
        ShotW("57_entrada_noche", Inicio + Vector3.up * 1.65f, new Vector3(0, 2.4f, 0), 72);
        ShotW("58_rampa_noche", GeoKit.Pos(78, 15.5f) + Vector3.up * 1.7f, GeoKit.Pos(92, 23f) + Vector3.up * .8f, 70);
        if (refl != null) refl.SetActive(false);
        CicloDeLuz.Apply(presets[0], sun, moon, RenderSettings.skybox);
        if (nub != null) nub.color = presets[0].nubes;
        glow.intensity = presets[0].bloom;
        AssetDatabase.SaveAssets();
        Object.DestroyImmediate(camGo);
        Debug.Log("MEMORIA VIVA: vistas previas guardadas en Capturas/.");
    }

    private static void SaveCameraImage(Camera camera, string file)
    {
        var texture = new RenderTexture(1600, 900, 24) { antiAliasing = 8 };
        camera.targetTexture = texture;
        camera.Render();
        RenderTexture.active = texture;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes(file, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(texture);
    }
}
