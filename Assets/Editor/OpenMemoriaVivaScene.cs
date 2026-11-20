using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class OpenMemoriaVivaScene
{
    private const string ScenePath = "Assets/Scenes/MemoriaViva.unity";
    private const string SessionKey = "MemoriaViva.StartupSceneChecked";

    static OpenMemoriaVivaScene()
    {
        EditorApplication.delayCall += OpenOnFirstLaunch;
        // Play siempre arranca en la escena del altar, aunque haya otra abierta.
        EditorApplication.delayCall += () => EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    }

    [MenuItem("Memoria Viva/Abrir escena principal", priority = 0)]
    private static void OpenFromMenu()
    {
        EditorSceneManager.OpenScene(ScenePath);
    }

    private static void OpenOnFirstLaunch()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;

        Scene current = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(current.path) || current.name != "Untitled") return;

        var roots = current.GetRootGameObjects();
        if (roots.Length != 2) return;
        bool camera = false;
        bool light = false;
        foreach (var root in roots)
        {
            camera |= root.name == "Main Camera";
            light |= root.name == "Directional Light";
        }
        if (camera && light) EditorSceneManager.OpenScene(ScenePath);
    }
}
