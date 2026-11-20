using System.IO;
using System.Reflection;
using MemoriaViva;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prueba en modo Play: suelo, colisiones, los cuatro rituales y el final.
[InitializeOnLoad]
public static class PruebaJuego
{
    private const string Key = "MemoriaViva.PruebaJuego";
    private static int step;
    private static float until;

    static PruebaJuego()
    {
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MemoriaViva.unity");
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + ".step", 0);
        EditorApplication.EnterPlaymode();
    }

    private static void Captura(string archivo, Vector3 desde, Vector3 hacia)
    {
        var cam = new GameObject("Cámara de captura").AddComponent<Camera>();
        cam.farClipPlane = 1800;
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var img = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        Directory.CreateDirectory("Capturas");
        File.WriteAllBytes(archivo, img.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(cam.gameObject);
    }

    private static void Fail(string msg)
    {
        Debug.LogError("PRUEBA FALLÓ: " + msg);
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(1);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        step = SessionState.GetInt(Key + ".step", 0);
        if (Time.time < until) return;
        var explorer = Object.FindFirstObjectByType<VRExplorer>();
        var director = Object.FindFirstObjectByType<ExperienceDirector>();
        try
        {
            switch (step)
            {
                case 0:
                    until = Time.time + 1f;
                    break;
                case 1:
                {
                    var ground = typeof(VRExplorer).GetMethod("Ground", BindingFlags.NonPublic | BindingFlags.Instance);
                    float H(Vector3 p)
                    {
                        object[] args = { p, 40f, 0f };
                        bool ok = (bool)ground.Invoke(explorer, args);
                        if (!ok) throw new System.Exception("Sin suelo en " + p);
                        return (float)args[2];
                    }
                    Vector3 Rampa(float u, float v)
                    {
                        float a = 112 * Mathf.Deg2Rad;
                        return GeoKit.Pos(112, 19f * 1.25f + .7f) + GeoKit.Radial(112) * u - new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a)) * v;
                    }
                    Debug.Log($"PRUEBA suelo: entrada {H(new Vector3(2.5f, 0, 36.25f)):F2}, plaza {H(new Vector3(5, 0, 5)):F2}, grada 3 {H(GeoKit.Pos(165, 16.0f)):F2}, terraza {H(GeoKit.Pos(185, 23.25f)):F2}, talud {H(GeoKit.Pos(106, 16f)):F2}, rampa {H(Rampa(4.75f, 2.9f)):F2} / {H(Rampa(4.75f, 1f)):F2} / {H(Rampa(-.8f, 1f)):F2}, escalera {H(GeoKit.Pos(110, 20.5f)):F2}");
                    if (Mathf.Abs(H(GeoKit.Pos(185, 23.25f)) - 2.12f) > .05f) Fail("la terraza no está a 2.1 m");
                    if (Mathf.Abs(explorer.transform.position.y) > .1f) Fail("el jugador no arrancó sobre el pasto: " + explorer.transform.position);
                    bool blocked = Physics.CapsuleCast(new Vector3(0, .65f, 6), new Vector3(0, 1.5f, 6), .28f, Vector3.back, out _, 4f, ~0, QueryTriggerInteraction.Ignore);
                    if (!blocked) Fail("se puede atravesar la ofrenda");
                    bool wall = Physics.CapsuleCast(GeoKit.Pos(180, 24.4f, 2.13f + .65f), GeoKit.Pos(180, 24.4f, 2.13f + 1.5f), .28f, Vector3.back, out _, 4f, ~0, QueryTriggerInteraction.Ignore);
                    if (!wall) Fail("se puede atravesar la fachada");
                    if (director.Collected != 0) Fail("los rituales no arrancan en cero");
                    if (Object.FindFirstObjectByType<ModoLigero>() == null) Fail("no se creo el modo ligero");
                    foreach (var name in new[] { "Papel picado | tira 1", "Fuego | veladoras nivel 1", "Final | llegada de las ánimas" })
                        if (GameObject.Find(name) != null) Fail(name + " debería empezar oculto");
                    foreach (var wi in Object.FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None))
                        if (wi.action == WorldInteractable.Action.Offering) wi.Activate(explorer);
                    explorer.Teleport(GeoKit.Pos(182, 19.5f, 2.2f));
                    until = Time.time + .9f;
                    break;
                }
                case 2:
                {
                    int efectos = 0;
                    foreach (var go in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) if (go.name.StartsWith("Efecto |")) efectos++;
                    if (efectos < 4) Fail("no salieron las particulas de los rituales: " + efectos);
                    if (GameObject.Find("Efecto | brasas de las veladoras") == null) Fail("no salieron las brasas del fuego");
                    Captura("Capturas/31_play_rituales.png", new Vector3(-5.5f, 1.8f, -6.5f), new Vector3(0, 1.6f, 0));
                    Debug.Log("PRUEBA efectos: " + efectos + " sistemas de particulas activos tras los rituales");
                    until = Time.time + 4.6f;
                    break;
                }
                case 3:
                {
                    if (director.Collected != 4) Fail("faltan rituales: " + director.Collected);
                    var fin = GameObject.Find("Final | llegada de las ánimas");
                    if (fin == null) Fail("no apareció el final");
                    var tira = GameObject.Find("Papel picado | tira 1");
                    if (tira == null || (tira.transform.localScale - Vector3.one).magnitude > .01f) Fail("el papel picado no terminó de desplegarse");
                    if (tira.transform.localRotation == Quaternion.identity) Fail("el papel picado no se mueve con el viento");
                    var agua = GameObject.Find("Agua | nivel 2");
                    if (agua == null || Mathf.Abs(agua.transform.localScale.y - 1) > .01f) Fail("el agua no se sirvió");
                    if (explorer.transform.position.y < 2f) Fail("el viaje a las gradas no quedó sobre la terraza: " + explorer.transform.position);
                    foreach (var clip in new[] { "grillos", "viento", "velas", "campana", "destello", "musica", "seleccion", "conocer", "agarrar", "encender", "agua", "tierra", "papel", "viaje", "pasos", "luz", "mariachi", "cohete", "tambor" })
                        if (Resources.Load<AudioClip>("Audio/" + clip) == null) Fail("falta el audio " + clip);
                    var visit = Object.FindFirstObjectByType<OfrendaVisitantes>();
                    PlayerPrefs.DeleteKey("MemoriaViva.Visitantes");
                    visit.AgregarNombre("Ada Lovelace");
                    if (GameObject.Find("Veladora de Ada Lovelace") == null) Fail("no se encendio la veladora del visitante");
                    Debug.Log("PRUEBA audio y visitantes: 19 audios cargados, veladora con nombre encendida");
                    Debug.Log("PRUEBA rituales: 4/4, final visible, papel picado desplegado y en movimiento, agua servida, viaje a gradas en y=" + explorer.transform.position.y.ToString("F2"));
                    var cam = new GameObject("Cámara de prueba").AddComponent<Camera>();
                    cam.farClipPlane = 1800;
                    cam.transform.position = new Vector3(-6.5f, 1.65f, 8.5f);
                    cam.transform.LookAt(new Vector3(0, 2.6f, 0));
                    var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var img = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    Directory.CreateDirectory("Capturas");
                    File.WriteAllBytes("Capturas/14_play_final.png", img.EncodeToPNG());
                    RenderTexture.active = null;
                    var tour = Object.FindFirstObjectByType<RecorridoGuiado>();
                    tour.Iniciar();
                    SessionState.SetInt(Key + ".step", 4);
                    until = Time.time + 4f;
                    return;
                }
                case 4:
                {
                    var tour = Object.FindFirstObjectByType<RecorridoGuiado>();
                    var cam = Camera.main;
                    if (!tour.Activo || explorer.enabled) Fail("el recorrido guiado no tomo el control");
                    Vector3 p = cam.transform.position;
                    if ((p - new Vector3(-10.6f, 1.7f, 51.8f)).magnitude < .5f) Fail("la camara no se movio en el recorrido");
                    tour.Terminar();
                    if (!explorer.enabled) Fail("el recorrido no devolvio el control");
                    Debug.Log("PRUEBA recorrido: camara en " + p.ToString("F1") + ", control devuelto al terminar");
                    var pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = new Vector2(0, 1), rightStick = new Vector2(1, 0) });
                    SessionState.SetFloat(Key + ".yaw", explorer.transform.eulerAngles.y);
                    SessionState.SetString(Key + ".pos", JsonUtility.ToJson(explorer.transform.position));
                    SessionState.SetInt(Key + ".step", 5);
                    until = Time.time + 1.5f;
                    return;
                }
                case 5:
                {
                    var pad = UnityEngine.InputSystem.Gamepad.current;
                    Vector3 antes = JsonUtility.FromJson<Vector3>(SessionState.GetString(Key + ".pos", "{}"));
                    float giro = Mathf.Abs(Mathf.DeltaAngle(SessionState.GetFloat(Key + ".yaw", 0), explorer.transform.eulerAngles.y));
                    float avance = Vector3.Distance(antes, explorer.transform.position);
                    UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
                    if (giro < 20f) Fail("el stick derecho del mando no gira la vista: " + giro);
                    if (avance < .5f) Fail("el stick izquierdo del mando no mueve al jugador: " + avance);
                    Debug.Log($"PRUEBA mando: giro {giro:F0} grados y avance {avance:F1} m con los sticks");
                    PlayerPrefs.DeleteKey("MemoriaViva.Visitantes");
                    SessionState.SetBool(Key, false);
                    Debug.Log("PRUEBA COMPLETA");
                    EditorApplication.Exit(0);
                    return;
                }
            }
            SessionState.SetInt(Key + ".step", step + 1);
        }
        catch (System.Exception e)
        {
            Fail(e.ToString());
        }
    }
}
