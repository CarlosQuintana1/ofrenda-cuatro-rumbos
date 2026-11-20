using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Planta ortografica de la escena para empalmarla con la vista satelital de Google Maps.
internal static class CapturaPlanta
{
    // Metros reales por pixel de la captura satelital (barra de 10 m = 74 px en 1400 px, vista a 800 px).
    private const float MetrosPorPixel = 10f / (74f * 800f / 1400f);
    private const float E = 1.25f;

    [MenuItem("Memoria Viva/Capturar planta")]
    public static void Capturar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MemoriaViva.unity");
        int w = 1600, h = 1142;
        RenderSettings.fog = false;
        var go = new GameObject("Camara planta");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = h / 4f * MetrosPorPixel * E;
        cam.transform.SetPositionAndRotation(new Vector3(0, 300, 0), Quaternion.Euler(90, 0, 0));
        cam.nearClipPlane = 1;
        cam.farClipPlane = 600;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes("Capturas/planta.png", tex.EncodeToPNG());
        RenderTexture.active = null;
        Object.DestroyImmediate(go);
        Debug.Log("MEMORIA VIVA: planta guardada en Capturas/planta.png");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
