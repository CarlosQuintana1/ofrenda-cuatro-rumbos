using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class MedirEscena
{
    // Vertices por grupo y por material, para saber donde recortar.
    [MenuItem("Memoria Viva/Medir escena")]
    public static void Medir()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MemoriaViva.unity");
        var raiz = GameObject.Find("MEMORIA VIVA | Facultad de Informática UAQ");
        foreach (Transform g in raiz.transform)
        {
            int v = g.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null).Sum(m => m.sharedMesh.vertexCount);
            Debug.Log($"MEDIDA grupo {v,9:N0}  {g.name}");
        }
        int luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Debug.Log("MEDIDA luces " + luces);
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
