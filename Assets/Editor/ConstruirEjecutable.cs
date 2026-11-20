using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class ConstruirEjecutable
{
    [MenuItem("Memoria Viva/Construir ejecutable Mac")]
    public static void Mac()
    {
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.resizableWindow = true;
#if UNITY_EDITOR_OSX
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
#endif
        IncluirShaders("Hidden/MemoriaViva/Bloom", "Sprites/Default", "MemoriaViva/Estrellas", "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MemoriaViva.unity" },
            locationPathName = "Ejecutable/Memoria Viva.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded) QuitarRuta(System.IO.Path.Combine(options.locationPathName, "Contents/Resources/Data/Managed/Assembly-CSharp.dll"), options.locationPathName);
        Debug.Log($"MEMORIA VIVA: ejecutable {report.summary.result}, {report.summary.totalSize / 1048576} MB, {report.summary.totalErrors} errores.");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    [MenuItem("Memoria Viva/Construir ejecutable Windows")]
    public static void Windows()
    {
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.resizableWindow = true;
        IncluirShaders("Hidden/MemoriaViva/Bloom", "Sprites/Default", "MemoriaViva/Estrellas", "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MemoriaViva.unity" },
            locationPathName = "Ejecutable/Windows/Ofrenda de los Cuatro Rumbos.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            QuitarRuta("Ejecutable/Windows/Ofrenda de los Cuatro Rumbos_Data/Managed/Assembly-CSharp.dll", null);
        Debug.Log($"MEMORIA VIVA: ejecutable Windows {report.summary.result}, {report.summary.totalSize / 1048576} MB, {report.summary.totalErrors} errores.");
        if (report.summary.result == BuildResult.Succeeded && !Application.isBatchMode) EditorUtility.RevealInFinder("Ejecutable/Windows/Ofrenda de los Cuatro Rumbos.exe");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    [MenuItem("Memoria Viva/Construir version web")]
    public static void Web()
    {
        IncluirShaders("Hidden/MemoriaViva/Bloom", "Sprites/Default", "MemoriaViva/Estrellas", "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended");
        // GitHub Pages no manda Content-Encoding: el navegador descomprime el gzip con el fallback de Unity.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MemoriaViva.unity" },
            locationPathName = "Ejecutable/Web",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"MEMORIA VIVA: version web {report.summary.result}, {report.summary.totalSize / 1048576} MB, {report.summary.totalErrors} errores.");
        if (report.summary.result == BuildResult.Succeeded && !Application.isBatchMode) EditorUtility.RevealInFinder("Ejecutable/Web/index.html");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    // El compilador deja la ruta del proyecto (con el usuario de la computadora) dentro de la dll.
    private static void QuitarRuta(string dll, string appMac)
    {
        if (!System.IO.File.Exists(dll)) return;
        string ruta = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath("."));
        var bytes = System.IO.File.ReadAllBytes(dll);
        var viejo = System.Text.Encoding.UTF8.GetBytes(ruta);
        var nuevo = System.Text.Encoding.UTF8.GetBytes("/proyecto".PadRight(ruta.Length, '_'));
        for (int i = 0; i <= bytes.Length - viejo.Length; i++)
        {
            int k = 0;
            while (k < viejo.Length && bytes[i + k] == viejo[k]) k++;
            if (k == viejo.Length) System.Array.Copy(nuevo, 0, bytes, i, nuevo.Length);
        }
        System.IO.File.WriteAllBytes(dll, bytes);
        if (appMac != null) System.Diagnostics.Process.Start("codesign", $"--force --deep -s - \"{appMac}\"").WaitForExit();
    }

    // Los shaders que se piden con Shader.Find no entran al build si ningun material los usa.
    private static void IncluirShaders(params string[] nombres)
    {
        var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset"));
        var lista = settings.FindProperty("m_AlwaysIncludedShaders");
        foreach (var nombre in nombres)
        {
            var shader = Shader.Find(nombre);
            if (shader == null) continue;
            bool ya = false;
            for (int i = 0; i < lista.arraySize; i++) ya |= lista.GetArrayElementAtIndex(i).objectReferenceValue == shader;
            if (ya) continue;
            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = shader;
        }
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
