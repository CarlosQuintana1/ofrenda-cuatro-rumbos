using UnityEditor;

[InitializeOnLoad]
internal static class MandoEditor
{
    static MandoEditor()
    {
        EditorApplication.delayCall += MemoriaViva.Mando.RestaurarDpad;
    }
}
