using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Exporta la ofrenda a un .glb para verla en realidad aumentada desde el celular (Scene Viewer y Quick Look).
internal static class ExportarAR
{
    private const string Salida = "AR/ofrenda.glb";
    private const float Radio = 5.4f;       // el altar, sus arcos y los pedestales
    private const float Tamano = .75f;      // ancho final en metros, para que quepa sobre una mesa
    private static readonly string[] Fuera = { "petalo", "humo", "mariposa", "llama", "brillo", "estrellas" };

    private sealed class Lote
    {
        public Material Mat;
        public readonly List<Vector3> P = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> U = new List<Vector2>();
        public readonly List<int> I = new List<int>();
        public readonly Dictionary<(Vector3, Vector3, Vector2), int> Vistos = new Dictionary<(Vector3, Vector3, Vector2), int>();
    }

    [MenuItem("Memoria Viva/Exportar ofrenda para AR")]
    public static void Exportar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MemoriaViva.unity");
        var raiz = GameObject.Find("10 | Ofrenda de los Cuatro Rumbos");
        var lotes = new Dictionary<Material, Lote>();
        float minY = float.MaxValue, ancho = 0;
        foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>(false))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || !mr.enabled || mf.sharedMesh == null) continue;
            var mesh = mf.sharedMesh;
            var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv;
            var m4 = mf.transform.localToWorldMatrix;
            for (int sm = 0; sm < mesh.subMeshCount && sm < mr.sharedMaterials.Length; sm++)
            {
                var mat = mr.sharedMaterials[sm];
                if (!Exportable(mat)) continue;
                if (!lotes.TryGetValue(mat, out var lote)) lotes[mat] = lote = new Lote { Mat = mat };
                Vector2 esc = mat.HasProperty("_MainTex") ? mat.mainTextureScale : Vector2.one, des = mat.HasProperty("_MainTex") ? mat.mainTextureOffset : Vector2.zero;
                var t = mesh.GetTriangles(sm);
                for (int k = 0; k < t.Length; k += 3)
                {
                    Vector3 a = m4.MultiplyPoint3x4(v[t[k]]), b = m4.MultiplyPoint3x4(v[t[k + 1]]), c = m4.MultiplyPoint3x4(v[t[k + 2]]);
                    if (Plano(a) > Radio || Plano(b) > Radio || Plano(c) > Radio) continue;
                    foreach (int idx in new[] { t[k], t[k + 2], t[k + 1] })
                    {
                        // glTF es de mano derecha: se invierte x y el orden de los triangulos.
                        Vector3 p = m4.MultiplyPoint3x4(v[idx]);
                        Vector3 nn = n.Length > idx ? m4.MultiplyVector(n[idx]).normalized : Vector3.up;
                        Vector2 u = uv.Length > idx ? Vector2.Scale(uv[idx], esc) + des : Vector2.zero;
                        var clave = (new Vector3(-p.x, p.y, p.z), new Vector3(-nn.x, nn.y, nn.z), new Vector2(u.x, 1 - u.y));
                        if (!lote.Vistos.TryGetValue(clave, out int i))
                        {
                            i = lote.P.Count;
                            lote.Vistos[clave] = i;
                            lote.P.Add(clave.Item1); lote.N.Add(clave.Item2); lote.U.Add(clave.Item3);
                            minY = Mathf.Min(minY, p.y);
                            ancho = Mathf.Max(ancho, Mathf.Abs(p.x) * 2, Mathf.Abs(p.z) * 2);
                        }
                        lote.I.Add(i);
                    }
                }
            }
        }
        foreach (var l in lotes.Values)
        {
            string nm = l.Mat.name;
            if (nm.StartsWith("cempasuchil") || nm.StartsWith("roseta")) Aclarar(l);
        }
        float escala = Tamano / ancho;
        var lista = lotes.Values.Where(l => l.I.Count > 0).OrderByDescending(l => l.P.Count).ToList();
        foreach (var l in lista)
            for (int i = 0; i < l.P.Count; i++) l.P[i] = new Vector3(l.P[i].x, l.P[i].y - minY, l.P[i].z) * escala;
        int vtot = lista.Sum(l => l.P.Count), ttot = lista.Sum(l => l.I.Count) / 3;
        foreach (var l in lista.Take(25))
            Debug.Log($"AR material {l.P.Count,8:N0} vert  {l.I.Count / 3,8:N0} tri  {l.Mat.name}  ({l.Mat.shader.name})");
        Escribir(lista);
        Debug.Log($"MEMORIA VIVA: AR {lista.Count} materiales, {vtot:N0} vertices, {ttot:N0} triangulos, ancho real {ancho:F1} m, {new FileInfo(Salida).Length / 1048576f:F1} MB en {Salida}");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    // Quita una de cada tres flores: cada flor (o su roseta) es un grupo de triangulos conectados,
    // y la decision depende de la celda donde cae su centro, asi flor y roseta se van juntas.
    private static void Aclarar(Lote l)
    {
        var padre = Enumerable.Range(0, l.P.Count).ToArray();
        int Raiz(int x) { while (padre[x] != x) x = padre[x] = padre[padre[x]]; return x; }
        for (int t = 0; t < l.I.Count; t += 3)
        {
            padre[Raiz(l.I[t + 1])] = Raiz(l.I[t]);
            padre[Raiz(l.I[t + 2])] = Raiz(l.I[t]);
        }
        var suma = new Dictionary<int, Vector3>(); var cuenta = new Dictionary<int, int>();
        for (int i = 0; i < l.P.Count; i++)
        {
            int r = Raiz(i);
            suma[r] = (suma.TryGetValue(r, out var v) ? v : Vector3.zero) + l.P[i];
            cuenta[r] = (cuenta.TryGetValue(r, out int c) ? c : 0) + 1;
        }
        var quedan = new List<int>();
        for (int t = 0; t < l.I.Count; t += 3)
        {
            int r = Raiz(l.I[t]);
            Vector3 c = suma[r] / cuenta[r];
            int h = Mathf.RoundToInt(c.x * 5) * 73856093 ^ Mathf.RoundToInt(c.y * 5) * 19349663 ^ Mathf.RoundToInt(c.z * 5) * 83492791;
            if ((h & 0x7fffffff) % 3 == 0) continue;
            quedan.Add(l.I[t]); quedan.Add(l.I[t + 1]); quedan.Add(l.I[t + 2]);
        }
        l.I.Clear(); l.I.AddRange(quedan);
    }

    // Las fotos van a la mitad de resolucion: en la mesa miden unos centimetros.
    private static byte[] Reducir(string ruta, Texture mt)
    {
        var datos = File.ReadAllBytes(ruta);
        if (!ruta.EndsWith(".jpg") || mt.height <= 512) return datos;
        var t = new Texture2D(2, 2);
        t.LoadImage(datos);
        var rt = RenderTexture.GetTemporary(t.width / 2, t.height / 2);
        Graphics.Blit(t, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var r = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        r.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); r.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        return r.EncodeToJPG(82);
    }

    private static float Plano(Vector3 p) => new Vector2(p.x, p.z).magnitude;

    private static bool Exportable(Material m)
    {
        if (m == null) return false;
        string s = m.shader.name;
        if (!s.StartsWith("Standard") && !s.StartsWith("Legacy Shaders/Diffuse") && !s.StartsWith("Unlit")) return false;
        string nombre = m.name.ToLowerInvariant();
        return !Fuera.Any(f => nombre.Contains(f));
    }

    private static string F(float x) => x.ToString("0.#####", CultureInfo.InvariantCulture);

    private static void Escribir(List<Lote> lotes)
    {
        var bin = new MemoryStream();
        var vistas = new List<string>();
        var accesores = new List<string>();
        int Vista(byte[] datos, int destino)
        {
            while (bin.Length % 4 != 0) bin.WriteByte(0);
            long ini = bin.Length;
            bin.Write(datos, 0, datos.Length);
            vistas.Add(destino > 0 ? $"{{\"buffer\":0,\"byteOffset\":{ini},\"byteLength\":{datos.Length},\"target\":{destino}}}" : $"{{\"buffer\":0,\"byteOffset\":{ini},\"byteLength\":{datos.Length}}}");
            return vistas.Count - 1;
        }
        byte[] Floats(IEnumerable<float> xs) { var ms = new MemoryStream(); var w = new BinaryWriter(ms); foreach (var x in xs) w.Write(x); return ms.ToArray(); }

        var imagenes = new List<string>();
        var texturas = new Dictionary<Texture, int>();
        var materiales = new List<string>();
        var primitivas = new List<string>();
        foreach (var l in lotes)
        {
            var m = l.Mat;
            Color c = (m.HasProperty("_Color") ? m.color : Color.white).linear;
            string tex = "";
            var mt = m.HasProperty("_MainTex") ? m.mainTexture : null;
            if (mt != null)
            {
                if (!texturas.TryGetValue(mt, out int ti))
                {
                    ti = -1;
                    string ruta = AssetDatabase.GetAssetPath(mt);
                    string ext = Path.GetExtension(ruta).ToLowerInvariant();
                    string mime = ext == ".png" ? "image/png" : ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" : null;
                    if (mime != null && File.Exists(ruta))
                    {
                        var datos = Reducir(ruta, mt);
                        imagenes.Add($"{{\"bufferView\":{Vista(datos, 0)},\"mimeType\":\"{mime}\"}}");
                        ti = imagenes.Count - 1;
                        Debug.Log($"AR textura {datos.Length / 1024,6} KB  {mt.width}x{mt.height}  {ruta}");
                    }
                    texturas[mt] = ti;
                }
                if (ti >= 0) tex = $",\"baseColorTexture\":{{\"index\":{ti}}}";
            }
            float rugosidad = 1 - (m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : .2f);
            float metal = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0;
            string emision = "";
            if (m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor"))
            {
                Color e = m.GetColor("_EmissionColor").linear;
                emision = $",\"emissiveFactor\":[{F(Mathf.Clamp01(e.r))},{F(Mathf.Clamp01(e.g))},{F(Mathf.Clamp01(e.b))}]";
            }
            string alfa = "";
            if (m.IsKeywordEnabled("_ALPHATEST_ON")) alfa = $",\"alphaMode\":\"MASK\",\"alphaCutoff\":{F(m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : .5f)},\"doubleSided\":true";
            else if (m.renderQueue >= 3000) alfa = ",\"alphaMode\":\"BLEND\"";
            materiales.Add($"{{\"name\":\"{m.name}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[{F(c.r)},{F(c.g)},{F(c.b)},{F(c.a)}]{tex},\"metallicFactor\":{F(metal)},\"roughnessFactor\":{F(rugosidad)}}}{emision}{alfa}}}");
            int mi = materiales.Count - 1;
            bool conUV = tex.Length > 0;

            // Trozos de menos de 65 mil vertices para usar indices de 16 bits.
            for (int t0 = 0; t0 < l.I.Count;)
            {
                var mapa = new Dictionary<int, int>();
                var idx = new List<ushort>();
                int t = t0;
                for (; t < l.I.Count; t += 3)
                {
                    int nuevos = 0;
                    for (int k = 0; k < 3; k++) if (!mapa.ContainsKey(l.I[t + k])) nuevos++;
                    if (mapa.Count + nuevos > 65000) break;
                    for (int k = 0; k < 3; k++)
                    {
                        if (!mapa.TryGetValue(l.I[t + k], out int ni)) { ni = mapa.Count; mapa[l.I[t + k]] = ni; }
                        idx.Add((ushort)ni);
                    }
                }
                t0 = t;
                var orden = new int[mapa.Count];
                foreach (var kv in mapa) orden[kv.Value] = kv.Key;
                var pos = orden.Select(o => l.P[o]).ToList();
                Vector3 mn = pos.Aggregate(Vector3.Min), mx = pos.Aggregate(Vector3.Max);
                accesores.Add($"{{\"bufferView\":{Vista(Floats(pos.SelectMany(q => new[] { q.x, q.y, q.z })), 34962)},\"componentType\":5126,\"count\":{pos.Count},\"type\":\"VEC3\",\"min\":[{F(mn.x)},{F(mn.y)},{F(mn.z)}],\"max\":[{F(mx.x)},{F(mx.y)},{F(mx.z)}]}}");
                int ap = accesores.Count - 1;
                accesores.Add($"{{\"bufferView\":{Vista(Floats(orden.SelectMany(o => new[] { l.N[o].x, l.N[o].y, l.N[o].z })), 34962)},\"componentType\":5126,\"count\":{pos.Count},\"type\":\"VEC3\"}}");
                string atributos = $"\"POSITION\":{ap},\"NORMAL\":{accesores.Count - 1}";
                if (conUV)
                {
                    accesores.Add($"{{\"bufferView\":{Vista(Floats(orden.SelectMany(o => new[] { l.U[o].x, l.U[o].y })), 34962)},\"componentType\":5126,\"count\":{pos.Count},\"type\":\"VEC2\"}}");
                    atributos += $",\"TEXCOORD_0\":{accesores.Count - 1}";
                }
                var ims = new MemoryStream(); var iw = new BinaryWriter(ims); foreach (var i in idx) iw.Write(i);
                accesores.Add($"{{\"bufferView\":{Vista(ims.ToArray(), 34963)},\"componentType\":5123,\"count\":{idx.Count},\"type\":\"SCALAR\"}}");
                primitivas.Add($"{{\"attributes\":{{{atributos}}},\"indices\":{accesores.Count - 1},\"material\":{mi}}}");
            }
        }
        while (bin.Length % 4 != 0) bin.WriteByte(0);

        var j = new StringBuilder();
        j.Append("{\"asset\":{\"version\":\"2.0\",\"generator\":\"Memoria Viva\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}],");
        j.Append("\"nodes\":[{\"name\":\"Ofrenda de los Cuatro Rumbos\",\"mesh\":0}],");
        j.Append("\"meshes\":[{\"primitives\":[").Append(string.Join(",", primitivas)).Append("]}],");
        j.Append("\"materials\":[").Append(string.Join(",", materiales)).Append("],");
        if (imagenes.Count > 0)
        {
            j.Append("\"samplers\":[{\"magFilter\":9729,\"minFilter\":9987,\"wrapS\":10497,\"wrapT\":10497}],");
            j.Append("\"images\":[").Append(string.Join(",", imagenes)).Append("],");
            j.Append("\"textures\":[").Append(string.Join(",", Enumerable.Range(0, imagenes.Count).Select(i => $"{{\"sampler\":0,\"source\":{i}}}"))).Append("],");
        }
        j.Append("\"accessors\":[").Append(string.Join(",", accesores)).Append("],");
        j.Append("\"bufferViews\":[").Append(string.Join(",", vistas)).Append("],");
        j.Append($"\"buffers\":[{{\"byteLength\":{bin.Length}}}]}}");
        var json = Encoding.UTF8.GetBytes(j.ToString());
        int pad = (4 - json.Length % 4) % 4;
        Directory.CreateDirectory(Path.GetDirectoryName(Salida));
        using (var w = new BinaryWriter(File.Create(Salida)))
        {
            w.Write(0x46546C67u); w.Write(2u); w.Write((uint)(12 + 8 + json.Length + pad + 8 + bin.Length));
            w.Write((uint)(json.Length + pad)); w.Write(0x4E4F534Au); w.Write(json); for (int i = 0; i < pad; i++) w.Write((byte)0x20);
            w.Write((uint)bin.Length); w.Write(0x004E4942u); w.Write(bin.ToArray());
        }
    }
}
