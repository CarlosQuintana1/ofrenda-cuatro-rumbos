using System.IO;
using UnityEditor;
using UnityEngine;

// Texturas procedurales: pavimentos, fachada, papel picado, flamas, mariposa.
public static class TexKit
{
    public static string Folder = "Assets/MemoriaViva/Texturas";

    // ---------- ruido periodico (se repite sin costuras) ----------

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
        }
    }

    private static float Value(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        int M(int v) => ((v % period) + period) % period;
        float a = Hash(M(x0), M(y0), seed), b = Hash(M(x0 + 1), M(y0), seed);
        float c = Hash(M(x0), M(y0 + 1), seed), d = Hash(M(x0 + 1), M(y0 + 1), seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    // u,v en 0..1; base = celdas en la octava mas gruesa.
    public static float Fbm(float u, float v, int baseCells, int octaves, int seed)
    {
        float sum = 0, amp = .5f, norm = 0;
        int cells = baseCells;
        for (int o = 0; o < octaves; o++)
        {
            sum += Value(u * cells, v * cells, cells, seed + o * 31) * amp;
            norm += amp; amp *= .5f; cells *= 2;
        }
        return sum / norm;
    }

    private static Texture2D Save(string name, Color[] px, int w, int h, bool alpha, bool repeat = true)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        string path = Folder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.alphaIsTransparency = alpha;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 6;
        imp.filterMode = FilterMode.Trilinear;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    // ---------- pavimentos y materiales del edificio ----------

    // Adoquin cuadrado de la plaza en tonos rojizo y morado; la textura cubre 1.2 m (piezas de 20 cm).
    public static Texture2D Ladrillo()
    {
        int s = 512; var px = new Color[s * s];
        Color[] tones = { new Color(.58f, .36f, .34f), new Color(.52f, .33f, .33f), new Color(.64f, .42f, .40f), new Color(.48f, .32f, .33f), new Color(.60f, .45f, .44f), new Color(.55f, .30f, .28f) };
        Color grout = new Color(.55f, .52f, .48f);
        int n = 6; float cell = (float)s / n;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            int cx = (int)(x / cell), cy = (int)(y / cell);
            float lx = x - cx * cell, ly = y - cy * cell;
            float edge = Mathf.Min(Mathf.Min(lx, cell - lx), Mathf.Min(ly, cell - ly));
            Color c = tones[(int)(Hash(cx, cy, 7) * tones.Length) % tones.Length];
            float nn = Fbm((float)x / s, (float)y / s, 16, 4, 3);
            c = Shade(c, .84f + nn * .3f);
            if (edge < 2.5f) c = Shade(grout, .85f + nn * .25f);
            else if (edge < 5f) c = Shade(c, .9f);
            float wear = Fbm((float)x / s, (float)y / s, 3, 4, 97);
            c = Color.Lerp(c, new Color(.66f, .58f, .55f), Mathf.Clamp01((wear - .58f) * 2.2f));
            if (Hash(x / 3, y / 3, 41) > .9975f) c = new Color(.82f, .8f, .78f);
            px[y * s + x] = c;
        }
        return Save("ladrillo", px, s, s, false);
    }

    // Adocreto hexagonal rojo de la terraza. Celda de 4 x 2 pares de filas; el material corrige la proporcion.
    public static Texture2D Hexagonal()
    {
        int cols = 4, rowPairs = 2;
        float r = 1f;
        float W = cols * Mathf.Sqrt(3) * r, H = rowPairs * 3f * r;
        int w = 296, h = Mathf.RoundToInt(w * H / W);
        var px = new Color[w * h];
        Color[] tones = { new Color(.66f, .34f, .30f), new Color(.60f, .30f, .27f), new Color(.70f, .40f, .35f), new Color(.56f, .31f, .30f) };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float wx = (x + .5f) / w * W, wy = (y + .5f) / h * H;
            float q = (Mathf.Sqrt(3) / 3 * wx - wy / 3) / r, rr = (2f / 3 * wy) / r;
            float cxx = q, czz = rr, cyy = -q - rr;
            float rx = Mathf.Round(cxx), rz = Mathf.Round(czz), ry = Mathf.Round(cyy);
            float dx = Mathf.Abs(rx - cxx), dy = Mathf.Abs(ry - cyy), dz = Mathf.Abs(rz - czz);
            if (dx > dy && dx > dz) rx = -ry - rz; else if (dy > dz) ry = -rx - rz; else rz = -rx - ry;
            float hx = r * Mathf.Sqrt(3) * (rx + rz / 2), hy = r * 1.5f * rz;
            float ox = wx - hx, oy = wy - hy;
            float d = 0;
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                d = Mathf.Max(d, ox * Mathf.Cos(a) + oy * Mathf.Sin(a));
            }
            float edge = r * Mathf.Sqrt(3) / 2 - d;
            int row = ((int)rz % (rowPairs * 2) + rowPairs * 2) % (rowPairs * 2);
            int col = (((int)(rx + (rz - ((int)rz & 1)) / 2)) % cols + cols) % cols;
            Color c = tones[(int)(Hash(col, row, 13) * tones.Length) % tones.Length];
            float nn = Fbm((float)x / w, (float)y / h, 8, 4, 5);
            c = Shade(c, .85f + nn * .28f);
            if (edge < .07f) c = Shade(new Color(.42f, .38f, .35f), .9f + nn * .2f);
            else if (edge < .14f) c = Shade(c, .88f);
            px[y * w + x] = c;
        }
        return Save("hexagonal", px, w, h, false);
    }

    // Panel solar: celdas azul oscuro con rejilla clara y marco de aluminio.
    public static Texture2D PanelSolar()
    {
        int w = 128, h = 256; var px = new Color[w * h];
        Color celda = new Color(.07f, .11f, .27f), linea = new Color(.5f, .55f, .64f), marco = new Color(.74f, .76f, .8f);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool borde = x < 5 || x >= w - 5 || y < 5 || y >= h - 5;
            bool reja = (x - 5) % 20 == 0 || (y - 5) % 25 == 0;
            float brillo = .85f + .3f * Fbm((float)x / w, (float)y / h, 3, 3, 9);
            px[y * w + x] = borde ? marco : reja ? linea : Shade(celda, brillo);
        }
        return Save("panel_solar", px, w, h, false);
    }

    public static Texture2D Concreto(string name, Color baseColor, int seed, float contrast)
    {
        int s = 256; var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (float)x / s, v = (float)y / s;
            float n = Fbm(u, v, 4, 6, seed);
            float speck = Hash(x, y, seed + 5) > .985f ? .82f : 1f;
            px[y * s + x] = Shade(baseColor, (1 - contrast) + n * contrast * 2) * speck;
        }
        return Save(name, px, s, s, false);
    }

    public static Texture2D Pasto()
    {
        int s = 256; var px = new Color[s * s];
        Color a = new Color(.22f, .36f, .12f), b = new Color(.36f, .48f, .17f), dry = new Color(.50f, .49f, .28f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (float)x / s, v = (float)y / s;
            float n = Fbm(u, v, 4, 6, 11);
            float d = Fbm(u, v, 2, 3, 19);
            Color c = Color.Lerp(a, b, n);
            c = Color.Lerp(c, dry, Mathf.Clamp01((d - .62f) * 3f));
            c = Shade(c, .82f + Hash(x, y, 4) * .3f);
            px[y * s + x] = c;
        }
        return Save("pasto", px, s, s, false);
    }

    // Fachada crema con juntas de paneles; la textura cubre 3.6 m.
    public static Texture2D Fachada() => Fachada("fachada", new Color(.91f, .87f, .76f));

    public static Texture2D Fachada(string nombre, Color c0)
    {
        int s = 512; var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (float)x / s, v = (float)y / s;
            float n = Fbm(u, v, 8, 5, 23);
            float stain = Fbm(u, v * .5f, 2, 3, 41);
            Color c = Shade(c0, .95f + n * .08f - Mathf.Clamp01(stain - .6f) * .12f);
            float drip = Fbm(u * 3, 0, 8, 3, 33);
            c = Shade(c, 1 - Mathf.Clamp01(drip - .62f) * .35f * (1 - v * .6f));
            int jy = y % (s / 3);
            if (jy < 2) c = Shade(c, .86f);
            if (x < 2) c = Shade(c, .88f);
            px[y * s + x] = c;
        }
        return Save(nombre, px, s, s, false);
    }

    public static Texture2D Madera()
    {
        int s = 256; var px = new Color[s * s];
        Color c0 = new Color(.42f, .25f, .14f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (float)x / s, v = (float)y / s;
            float g = Mathf.Sin((u * 18 + Fbm(u, v, 4, 4, 9) * 6) * Mathf.PI) * .5f + .5f;
            px[y * s + x] = Shade(c0, .8f + g * .35f);
        }
        return Save("madera", px, s, s, false);
    }

    // Petate tejido de palma.
    public static Texture2D Petate()
    {
        int s = 256; var px = new Color[s * s];
        Color straw = new Color(.80f, .66f, .42f);
        int band = 16;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            int bx = x / band, by = y / band;
            bool horizontal = ((bx + by) % 2) == 0;
            float local = horizontal ? (y % band) / (float)band : (x % band) / (float)band;
            float along = horizontal ? (x % band) / (float)band : (y % band) / (float)band;
            float shade = .78f + Mathf.Sin(local * Mathf.PI) * .3f - Mathf.Abs(along - .5f) * .12f;
            float fiber = Fbm((float)x / s, (float)y / s, 32, 2, 5);
            px[y * s + x] = Shade(straw, shade * (.92f + fiber * .16f));
        }
        return Save("petate", px, s, s, false);
    }

    // Medallon central de la plaza: disco gris con anillo negro y coladera al centro.
    public static Texture2D Medallon()
    {
        int s = 512; var px = new Color[s * s];
        Color grey = new Color(.40f, .41f, .42f), black = new Color(.07f, .07f, .08f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
            float r = Mathf.Sqrt(u * u + v * v);
            float ang = Mathf.Atan2(v, u) * Mathf.Rad2Deg;
            float n = Fbm((float)x / s, (float)y / s, 8, 5, 77);
            Color c = Shade(grey, .85f + n * .3f);
            if (Mathf.Abs((r * 7f) % 1f - .5f) > .47f) c = Shade(c, .8f);
            if (Mathf.Abs(((ang + 360f) % 22.5f) - 11.25f) > 10.9f && r > .16f) c = Shade(c, .82f);
            if (r < .12f)
            {
                c = Shade(black, 1.4f);
                if ((x / 5) % 2 == 0) c = black;
            }
            if (r > .83f) c = Shade(black, .9f + n * .3f);
            px[y * s + x] = c;
        }
        return Save("medallon", px, s, s, false, false);
    }

    // ---------- papel picado ----------

    // Atlas 4x2. Fila de abajo: calavera con "FIF" en binario ASCII. Fila de arriba: cempasuchil con "UAQ".
    public static Texture2D PapelPicado()
    {
        int tile = 256, w = tile * 4, h = tile * 2;
        var px = new Color[w * h];
        Color[] row0 = { new Color(.93f, .12f, .55f), new Color(1f, .55f, .02f), new Color(.02f, .74f, .74f), new Color(.52f, .16f, .80f) };
        Color[] row1 = { new Color(1f, .84f, .05f), new Color(.32f, .76f, .20f), new Color(1f, .43f, .70f), new Color(.86f, .10f, .16f) };
        string fif = "010001100100100101000110";
        string uaq = "010101010100000101010001";
        for (int ty = 0; ty < 2; ty++)
        for (int tx = 0; tx < 4; tx++)
        {
            Color paper = ty == 0 ? row0[tx] : row1[tx];
            string bits = ty == 0 ? fif : uaq;
            for (int y = 0; y < tile; y++)
            for (int x = 0; x < tile; x++)
            {
                bool hole = PapelHole(x, y, ty == 0, bits);
                float n = Fbm((float)x / tile, (float)y / tile, 8, 3, tx + ty * 4);
                Color c = Shade(paper, .9f + n * .2f);
                c.a = hole ? 0 : 1;
                px[(ty * tile + y) * w + tx * tile + x] = c;
            }
        }
        return Save("papel_picado", px, w, h, true, false);
    }

    private static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return dx * dx + dy * dy < 1;
    }

    private static bool PapelHole(int x, int y, bool skull, string bits)
    {
        // Margenes solidos y doblez superior por donde pasa el cordel.
        if (x < 8 || x > 247 || y > 234) return false;
        // Picos del borde inferior.
        if (y < 14)
        {
            float cx = (x % 24) - 12;
            return cx * cx + (y - 0) * (y - 0) < 11 * 11;
        }
        if (y < 22) return false;
        // Banda binaria.
        if (y >= 204 && y <= 226)
        {
            if (y < 206 || y > 224) return false;
            int xx = x - 14;
            if (xx < 0) return false;
            int group = xx / 75;
            int inGroup = xx - group * 75;
            int digit = inGroup / 9;
            if (group > 2 || digit > 7) return false;
            float lx = inGroup - digit * 9 - 4f, ly = y - 215f;
            char b = bits[group * 8 + digit];
            if (b == '1') return Mathf.Abs(lx) < 1.6f && Mathf.Abs(ly) < 8;
            bool outer = InEllipse(lx, ly, 0, 0, 3.6f, 8.5f);
            bool inner = InEllipse(lx, ly, 0, 0, 1.5f, 5.5f);
            return outer && !inner;
        }
        if (y > 196) return false;
        float px = x, py = y;
        bool solid;
        bool cut = false;
        if (skull)
        {
            solid = InEllipse(px, py, 128, 122, 64, 60) || (Mathf.Abs(px - 128) < 38 && py > 54 && py < 112);
            if (solid)
            {
                cut |= InEllipse(px, py, 104, 116, 17, 19) || InEllipse(px, py, 152, 116, 17, 19);
                cut |= InEllipse(px, py, 128, 90, 7, 10) && py < 96 - Mathf.Abs(px - 128) * .2f;
                if (py > 62 && py < 78 && Mathf.Abs(px - 128) < 30) cut |= ((int)px % 10) < 4;
                for (int k = 0; k < 5; k++)
                {
                    float a = k * Mathf.PI * 2 / 5 + Mathf.PI / 2;
                    cut |= InEllipse(px, py, 128 + Mathf.Cos(a) * 11, 160 + Mathf.Sin(a) * 11, 5.5f, 5.5f);
                }
                cut |= InEllipse(px, py, 128, 160, 3, 3);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 5 + Mathf.PI * .1f;
                    cut |= InEllipse(px, py, 128 + Mathf.Cos(a) * 50, 122 + Mathf.Sin(a) * 47, 4, 4);
                }
                return cut;
            }
        }
        else
        {
            solid = InEllipse(px, py, 128, 112, 76, 76);
            if (solid)
            {
                float dx = px - 128, dy = py - 112;
                float r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                for (int k = 0; k < 14; k++)
                {
                    float a = k * Mathf.PI * 2 / 14;
                    Vector2 c = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 p = new Vector2(dx, dy);
                    float along = Vector2.Dot(p, c), side = Mathf.Abs(c.x * p.y - c.y * p.x);
                    cut |= along > 34 && along < 66 && side < 6.5f * (1 - Mathf.Abs(along - 50) / 18f);
                }
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 2 / 8 + .2f;
                    cut |= InEllipse(px, py, 128 + Mathf.Cos(a) * 22, 112 + Mathf.Sin(a) * 22, 5, 5);
                }
                cut |= r < 9;
                return cut;
            }
        }
        // Reticula de rombos alrededor del motivo.
        int gx = (x - 8) % 14, gy = (y - 22) % 14;
        float ddx = Mathf.Abs(gx - 7), ddy = Mathf.Abs(gy - 7);
        return ddx + ddy < 4.6f;
    }

    // ---------- luces, flama, mariposa, retrato ----------

    public static Texture2D Halo()
    {
        int s = 128; var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
            float r = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
            float a = Mathf.Pow(1 - r, 2.4f) + Mathf.Pow(1 - r, 12f) * .6f;
            px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
        }
        return Save("halo", px, s, s, true, false);
    }

    public static Texture2D Humo()
    {
        int s = 128; var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
            float r = Mathf.Sqrt(u * u + v * v);
            float n = Fbm((float)x / s, (float)y / s, 4, 4, 3);
            float a = Mathf.Clamp01(1 - r * (1.1f + (1 - n) * .6f));
            px[y * s + x] = new Color(1, 1, 1, a * a * .8f);
        }
        return Save("humo", px, s, s, true, false);
    }

    public static Texture2D Flama()
    {
        int w = 64, h = 128; var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = (x + .5f) / w * 2 - 1, v = (y + .5f) / h;
            float width = Mathf.Sin(Mathf.Clamp01(v * 1.15f) * Mathf.PI) * (1 - v * .55f) * .95f;
            float d = Mathf.Abs(u) / Mathf.Max(width, .001f);
            float a = Mathf.Clamp01(1 - d) * Mathf.Clamp01(v * 6) * Mathf.Clamp01((1 - v) * 3);
            Color c = Color.Lerp(new Color(1f, .35f, .05f), new Color(1f, .93f, .62f), Mathf.Clamp01((1 - d) * 1.4f - v * .5f));
            if (v < .2f && d < .45f) c = Color.Lerp(new Color(.4f, .55f, 1f), c, v / .2f);
            c.a = Mathf.Pow(a, .8f);
            px[y * w + x] = c;
        }
        return Save("flama", px, w, h, true, false);
    }

    // Monarca con alas abiertas; el shader la hace aletear.
    public static Texture2D Monarca()
    {
        int s = 128; var px = new Color[s * s];
        Color orange = new Color(.98f, .50f, .05f), black = new Color(.05f, .04f, .03f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
            float au = Mathf.Abs(u);
            bool upper = InEllipse(au, v, .48f, .25f, .50f, .42f) && v > -.05f;
            bool lower = InEllipse(au, v, .36f, -.30f, .34f, .36f);
            bool body = InEllipse(u, v, 0, 0, .05f, .55f);
            Color c = new Color(0, 0, 0, 0);
            if (upper || lower)
            {
                c = orange;
                float edgeU = upper ? 1 - Mathf.Sqrt(Mathf.Pow((au - .48f) / .5f, 2) + Mathf.Pow((v - .25f) / .42f, 2)) : 1;
                float edgeL = lower ? 1 - Mathf.Sqrt(Mathf.Pow((au - .36f) / .34f, 2) + Mathf.Pow((v + .30f) / .36f, 2)) : 1;
                float edge = Mathf.Min(upper ? edgeU : 1, lower ? edgeL : 1);
                if (upper && lower) edge = Mathf.Max(edgeU, edgeL);
                float ang = Mathf.Atan2(v, au);
                bool vein = Mathf.Abs(Mathf.Sin(ang * 7f)) < .12f || au < .1f;
                if (vein || edge < .14f) c = black;
                if (edge < .1f && Hash(x / 4, y / 4, 2) > .55f) c = Color.white;
                c.a = 1;
            }
            if (body) c = black;
            px[y * s + x] = c;
        }
        return Save("monarca", px, s, s, true, false);
    }

    // Petalos de cempasuchil en escamas superpuestas, para que las flores no se vean como canicas.
    public static Texture2D Petalos()
    {
        int s = 256; var px = new Color[s * s];
        Color dark = new Color(.78f, .30f, .02f), lite = new Color(1f, .72f, .12f);
        int cells = 8; float cw = (float)s / cells;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float best = 9; float shade = 0;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int cy = (int)(y / cw) + dy, cx = (int)(x / cw) + dx;
                float ox = (cy & 1) * cw * .5f;
                float px0 = cx * cw + ox + cw * .5f, py0 = cy * cw;
                float d = Mathf.Sqrt((x - px0) * (x - px0) + (y - py0) * (y - py0)) / cw;
                if (d < .75f && y >= py0 - 1 && (py0 > best || best > 8)) { best = py0; shade = d / .75f; }
            }
            float vein = .9f + .1f * Mathf.Sin(Mathf.Atan2(y % cw, x % cw) * 18);
            Color c = Color.Lerp(dark, lite, Mathf.Pow(shade, .7f)) * vein;
            c.a = 1;
            px[y * s + x] = c;
        }
        return Save("petalos", px, s, s, false);
    }

    // Roseta de cempasuchil vista de frente: tres coronas de petalos rizados, mas oscura al centro.
    public static Texture2D Roseta()
    {
        int s = 128; var px = new Color[s * s];
        Color edge = new Color(1f, .78f, .25f), mid = new Color(1f, .52f, .04f), core = new Color(.7f, .25f, .02f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
            float r = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
            float a = 0, shade = 0;
            int[] petals = { 15, 19, 23 };
            float[] radii = { .97f, .74f, .5f };
            for (int k = 0; k < 3; k++)
            {
                float wob = Mathf.Abs(Mathf.Sin(th * petals[k] * .5f + k * 1.3f));
                float R = radii[k] * (.8f + .2f * wob) * (1 + .04f * Mathf.Sin(th * 7 + k));
                if (r < R) { a = 1; shade = r / R; }
            }
            float ruffle = .82f + .18f * Mathf.Sin(th * 40 + r * 30);
            Color c = r < .22f ? Color.Lerp(core, mid, r / .22f) : Color.Lerp(mid, edge, Mathf.Pow(shade, 2));
            c *= ruffle;
            c.a = a;
            px[y * s + x] = c;
        }
        return Save("roseta", px, s, s, true, false);
    }

    // Hoja de palma reina: raquis al centro y foliolos delgados que cuelgan hacia la punta.
    public static Texture2D Fronda()
    {
        int w = 128, h = 512; var px = new Color[w * h];
        Color dark = new Color(.16f, .28f, .08f), lite = new Color(.42f, .55f, .18f), rachis = new Color(.55f, .5f, .3f);
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        int count = 95;
        for (int k = 0; k < count; k++)
        {
            float t = (k + .5f) / count;
            float y0 = t * h;
            float len = w * .5f * Mathf.Sin(Mathf.PI * Mathf.Clamp(t * 1.05f, .05f, .98f)) * (.85f + .15f * Hash(k, 1, 3));
            foreach (int side in new[] { -1, 1 })
            {
                Color col = Color.Lerp(dark, lite, Hash(k, side + 2, 9));
                for (int j = 0; j < (int)len; j++)
                {
                    float f = j / Mathf.Max(1, len);
                    int px0 = w / 2 + side * j;
                    int py0 = (int)(y0 + f * 22 + f * f * 10);
                    int thick = Mathf.Max(2, (int)(4.5f * (1 - f) + 1.5f));
                    for (int dy = 0; dy < thick; dy++)
                    {
                        int yy = py0 + dy;
                        if (px0 < 0 || px0 >= w || yy < 0 || yy >= h) continue;
                        px[yy * w + px0] = Color.Lerp(col, col * .8f, f);
                    }
                }
            }
        }
        for (int y = 0; y < h; y++)
            for (int dx = -2; dx <= 2; dx++)
                px[y * w + w / 2 + dx] = rachis;
        return Save("fronda", px, w, h, true, false);
    }

    // Racimo de hojas para la copa de los arboles.
    public static Texture2D Follaje()
    {
        int s = 128; var px = new Color[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        for (int k = 0; k < 46; k++)
        {
            float cx = 20 + Hash(k, 1, 5) * 88, cy = 20 + Hash(k, 2, 5) * 88;
            float ang = Hash(k, 3, 5) * Mathf.PI, L = 9 + Hash(k, 4, 5) * 8, W = L * .45f;
            Color c = Color.Lerp(new Color(.14f, .26f, .1f), new Color(.34f, .48f, .18f), Hash(k, 6, 5));
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx, dy = y - cy;
                float a = dx * Mathf.Cos(ang) + dy * Mathf.Sin(ang), b = -dx * Mathf.Sin(ang) + dy * Mathf.Cos(ang);
                if ((a * a) / (L * L) + (b * b) / (W * W) < 1) px[y * s + x] = c * (.85f + .15f * (1 - Mathf.Abs(b) / W));
            }
        }
        return Save("follaje", px, s, s, true, false);
    }

    // Mancha de humedad para la base de los muros: oscura abajo, se desvanece hacia arriba con escurridos.
    public static Texture2D Mancha()
    {
        int w = 256, h = 64; var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = (float)y / h;
            float streak = Fbm((float)x / w, 0, 16, 3, 61);
            float top = .35f + streak * .65f;
            float a = Mathf.Clamp01(1 - v / top) * (.55f + .45f * Fbm((float)x / w, v, 8, 4, 71));
            px[y * w + x] = new Color(.35f, .32f, .27f, a * .6f);
        }
        return Save("mancha", px, w, h, true);
    }

    // Capa de nubes con bordes suaves, se repite sin costuras.
    public static Texture2D Nubes()
    {
        int s = 512; var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float u = (float)x / s, v = (float)y / s;
            float n = Fbm(u, v, 3, 6, 91);
            float a = Mathf.Clamp01((n - .5f) * 3.2f);
            float shade = .8f + Fbm(u, v, 6, 3, 17) * .25f;
            px[y * s + x] = new Color(shade, shade, shade, a * a * (3 - 2 * a));
        }
        return Save("nubes", px, s, s, true);
    }

    // Marcador para fotografias: el equipo pone sus fotos en Resources/Fotos.
    public static Texture2D Retrato()
    {
        int w = 256, h = 320; var px = new Color[w * h];
        Color top = new Color(.86f, .74f, .56f), bottom = new Color(.58f, .42f, .30f), sil = new Color(.30f, .21f, .16f);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = (float)y / h;
            Color c = Color.Lerp(bottom, top, v);
            bool head = InEllipse(x, y, 128, 196, 50, 60);
            bool shoulders = InEllipse(x, y, 128, 40, 104, 92) && y < 120;
            if (head || shoulders) c = Color.Lerp(sil, c, .25f);
            float dx = (x - 128f) / 128f, dy = (y - 160f) / 160f;
            c = Shade(c, 1.05f - (dx * dx + dy * dy) * .35f);
            px[y * w + x] = c;
        }
        return Save("retrato", px, w, h, false, false);
    }

    // ---------- tapetes de aserrin y alebrijes ----------

    private static float Caja(Vector2 p, Vector2 b) { Vector2 d = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y); return Mathf.Max(d.x, d.y); }

    // Tapete de aserrin con cenefa de colores y un motivo al centro; el grano se nota en cada pixel.
    public static Texture2D Tapete(int diseno)
    {
        const int w = 512, h = 384;
        var px = new Color[w * h];
        Color[][] paletas =
        {
            new[] { new Color(.36f, .16f, .53f), new Color(.94f, .54f, .11f), new Color(.89f, 0f, .49f), new Color(.95f, .91f, .82f), new Color(.96f, .77f, .19f) },
            new[] { new Color(.89f, 0f, .49f), new Color(.94f, .54f, .11f), new Color(.96f, .77f, .19f), new Color(.36f, .16f, .53f), new Color(.95f, .91f, .82f) },
            new[] { new Color(.36f, .16f, .53f), new Color(.94f, .54f, .11f), new Color(.11f, .11f, .11f), new Color(.95f, .91f, .82f), new Color(.96f, .77f, .19f) },
            new[] { new Color(.55f, .1f, .25f), new Color(.96f, .77f, .19f), new Color(.36f, .16f, .53f), new Color(.94f, .54f, .11f), new Color(.95f, .91f, .82f) },
        };
        var c = paletas[diseno % paletas.Length];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Vector2 q = new Vector2((x - w / 2f) / (h / 2f), (y - h / 2f) / (h / 2f));
            float r = q.magnitude, a = Mathf.Atan2(q.y, q.x);
            Color col = c[0];
            float borde = -Caja(q, new Vector2(1.33f, 1f));
            if (borde < .06f) col = c[3];
            else if (borde < .14f) col = (Mathf.Repeat((q.x + q.y) * 6f, 1f) < .5f) ? c[1] : c[2];
            else if (borde < .18f) col = c[3];
            else
            {
                switch (diseno % 4)
                {
                    case 0:
                    {
                        // Calavera con flores en la frente.
                        Vector2 k = q - new Vector2(0, .08f);
                        float craneo = Mathf.Sqrt(k.x * k.x / .3f + k.y * k.y / .36f);
                        float mandibula = Caja(q - new Vector2(0, -.42f), new Vector2(.3f, .14f));
                        if (craneo < 1 || mandibula < 0) col = c[3];
                        foreach (int sx in new[] { -1, 1 })
                        {
                            float ojo = (q - new Vector2(sx * .22f, .02f)).magnitude;
                            if (ojo < .16f) col = c[2];
                            if (ojo < .1f) col = c[4];
                            if (ojo > .16f && ojo < .2f && Mathf.Repeat(Mathf.Atan2(q.y - .02f, q.x - sx * .22f) * 8 / 6.283f, 1) < .5f) col = c[1];
                        }
                        if (Mathf.Abs(q.x) < .05f && q.y > -.28f && q.y < -.14f) col = c[0];
                        if (q.y < -.4f && q.y > -.48f && Mathf.Abs(q.x) < .26f && Mathf.Repeat(q.x * 14, 1) > .25f) col = c[0];
                        float flor = (q - new Vector2(0, .38f)).magnitude;
                        if (flor < .13f + .04f * Mathf.Cos(a * 6)) col = c[1];
                        if (flor < .05f) col = c[2];
                        if (craneo > 1.15f && Mathf.Repeat(r * 5 - a * .5f, 1) < .12f) col = c[1];
                        break;
                    }
                    case 1:
                    {
                        // Cempasuchil en mandala.
                        for (int ring = 5; ring >= 0; ring--)
                        {
                            float rr = .12f + ring * .13f;
                            float petalo = rr + .05f * Mathf.Abs(Mathf.Cos(a * (6 + ring * 2) / 2));
                            if (r < petalo) col = ring % 2 == 0 ? c[1] : c[2];
                        }
                        if (r < .1f) col = c[3];
                        if (r > .9f && Mathf.Repeat(a * 12 / 6.283f, 1) < .5f && r < 1.0f) col = c[4];
                        foreach (int sx in new[] { -1, 1 })
                            if ((q - new Vector2(sx * 1.08f, 0)).magnitude < .14f + .03f * Mathf.Cos(Mathf.Atan2(q.y, q.x - sx * 1.08f) * 8)) col = c[2];
                        break;
                    }
                    case 2:
                    {
                        // Monarca con las alas abiertas.
                        Vector2 k = new Vector2(Mathf.Abs(q.x), q.y);
                        float arriba = ((k - new Vector2(.42f, .22f)) * new Vector2(1, 1.25f)).magnitude;
                        float abajo = ((k - new Vector2(.32f, -.3f)) * new Vector2(1.2f, 1)).magnitude;
                        bool ala = arriba < .42f || abajo < .3f;
                        if (ala)
                        {
                            col = c[1];
                            float vena = Mathf.Abs(Mathf.Sin(Mathf.Atan2(k.y + .05f, k.x) * 7));
                            if (vena < .12f || arriba > .36f && arriba < .42f || abajo > .25f && abajo < .3f) col = c[2];
                            if ((arriba > .37f && arriba < .41f || abajo > .26f && abajo < .29f) && Mathf.Repeat(a * 30 / 6.283f, 1) < .4f) col = c[3];
                        }
                        if (Mathf.Abs(q.x) < .04f && q.y > -.45f && q.y < .4f) col = c[2];
                        if ((q - new Vector2(0, .44f)).magnitude < .06f) col = c[2];
                        if (!ala && r > .85f && Mathf.Repeat(a * 10 / 6.283f + r, 1) < .15f) col = c[4];
                        break;
                    }
                    default:
                    {
                        // Cuatro rumbos: estrella de ocho puntas sobre cuatro cuadrantes.
                        col = (q.x > 0) == (q.y > 0) ? c[0] : c[2];
                        float estrella = .55f + .22f * Mathf.Abs(Mathf.Cos(a * 4)) - .1f * Mathf.Abs(Mathf.Cos(a * 8));
                        if (r < estrella) col = c[1];
                        if (r < estrella - .1f) col = c[3];
                        if (r < .22f) col = c[4];
                        if (r < .1f) col = c[1];
                        if (Mathf.Abs(Mathf.Abs(q.x) - Mathf.Abs(q.y)) < .03f && r > estrella) col = c[3];
                        break;
                    }
                }
            }
            // Grano del aserrin: motas finas claras y oscuras.
            float g = Hash(x, y, 77 + diseno) - .5f, f = Fbm(x / (float)w, y / (float)h, 16, 3, 9 + diseno) - .5f;
            col = Shade(col, 1 + g * .22f + f * .25f);
            px[y * w + x] = col;
        }
        return Save("tapete_" + diseno, px, w, h, false, false);
    }

    // Piel de alebrije: franjas, puntos y zigzag en colores vivos.
    public static Texture2D Alebrije(int variante)
    {
        const int n = 256;
        var px = new Color[n * n];
        Color[][] paletas =
        {
            new[] { new Color(.95f, .2f, .55f), new Color(.98f, .85f, .1f), new Color(.05f, .7f, .75f), new Color(.15f, .1f, .35f), new Color(1f, .5f, .05f) },
            new[] { new Color(1f, .45f, .05f), new Color(.5f, .15f, .7f), new Color(.2f, .75f, .25f), new Color(.98f, .92f, .3f), new Color(.9f, .1f, .2f) },
        };
        var c = paletas[variante % 2];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n, v = y / (float)n;
            int banda = Mathf.FloorToInt(v * 8);
            Color col = c[banda % 3];
            float fv = Mathf.Repeat(v * 8, 1);
            if (fv < .1f || fv > .9f) col = c[3];
            if (banda % 2 == 0)
            {
                float zig = Mathf.Abs(Mathf.Repeat(u * 16, 2) - 1);
                if (Mathf.Abs(fv - .2f - zig * .6f) < .09f) col = c[4];
            }
            else
            {
                Vector2 d = new Vector2(Mathf.Repeat(u * 16, 1) - .5f, fv - .5f);
                if (d.magnitude < .22f) col = c[(banda + 1) % 3 == 0 ? 4 : 3];
                if (d.magnitude < .1f) col = c[1];
            }
            px[y * n + x] = Shade(col, 1 + (Hash(x, y, 5 + variante) - .5f) * .08f);
        }
        return Save("alebrije_" + variante, px, n, n, false);
    }

    // Tramo del camino de aserrin: rombos arriba y abajo, motivo grande al centro, contorno negro como en los tapetes de Real de Catorce.
    public static Texture2D Camino(int tramo)
    {
        const int w = 512, h = 512;
        var px = new Color[w * h];
        Color[] vivos = { new Color(.96f, .77f, .19f), new Color(.94f, .54f, .11f), new Color(.89f, 0f, .49f), new Color(.55f, .1f, .25f), new Color(.36f, .16f, .53f), new Color(.6f, .3f, .7f), new Color(.95f, .91f, .82f) };
        Color negro = new Color(.08f, .06f, .06f);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w, v = y / (float)h;
            Color col;
            if (v < .2f || v > .8f)
            {
                // Franja de rombos.
                float vv = v < .2f ? v / .2f : (v - .8f) / .2f;
                float cu = Mathf.Repeat(u * 5, 1) - .5f, cv = vv - .5f;
                float d = Mathf.Abs(cu) * 2 + Mathf.Abs(cv) * 2;
                int k = Mathf.FloorToInt(u * 5) + tramo;
                col = d < .55f ? vivos[k % 6] : d < .8f ? vivos[(k + 2) % 6] : vivos[(k + 4) % 6];
                if (Mathf.Abs(d - .55f) < .04f || Mathf.Abs(d - .8f) < .04f) col = negro;
            }
            else
            {
                float cu = (u - .5f) * 2, cv = (v - .5f) / .3f;
                float r = Mathf.Sqrt(cu * cu + cv * cv), a = Mathf.Atan2(cv, cu);
                col = vivos[(tramo + 3) % 6];
                switch (tramo % 4)
                {
                    case 0:
                    {
                        float craneo = Mathf.Sqrt(cu * cu / .2f + (cv - .1f) * (cv - .1f) / .5f);
                        if (craneo < 1 || Mathf.Abs(cu) < .3f && cv < -.3f && cv > -.75f) col = vivos[6];
                        foreach (int sx in new[] { -1, 1 })
                        {
                            float ojo = new Vector2(cu - sx * .2f, cv - .05f).magnitude;
                            if (ojo < .17f) col = vivos[(tramo + 1) % 6];
                            if (ojo < .09f) col = negro;
                        }
                        if (Mathf.Abs(cu) < .05f && cv < -.15f && cv > -.35f) col = negro;
                        if (craneo > .97f && craneo < 1.05f) col = negro;
                        break;
                    }
                    case 1:
                        for (int ring = 4; ring >= 0; ring--)
                            if (r < .25f + ring * .2f + .05f * Mathf.Cos(a * (8 + ring * 2))) col = vivos[(ring + tramo) % 6];
                        if (r < .15f) col = vivos[0];
                        break;
                    case 2:
                    {
                        float corazon = Mathf.Pow(cu * cu + cv * cv - .5f, 3) - cu * cu * cv * cv * cv;
                        if (corazon < 0) col = vivos[2];
                        if (corazon < 0 && corazon > -.02f) col = negro;
                        float sol = .95f + .1f * Mathf.Cos(a * 16);
                        if (r > sol && r < sol + .08f) col = vivos[0];
                        break;
                    }
                    default:
                        if (Mathf.Abs(cu) + Mathf.Abs(cv) < .9f) col = vivos[(tramo + 4) % 6];
                        if (Mathf.Abs(cu) + Mathf.Abs(cv) < .6f) col = vivos[1];
                        if (Mathf.Abs(cu) + Mathf.Abs(cv) < .3f) col = vivos[6];
                        if (Mathf.Abs(Mathf.Abs(cu) + Mathf.Abs(cv) - .9f) < .03f) col = negro;
                        break;
                }
                if (Mathf.Abs(v - .2f) < .012f || Mathf.Abs(v - .8f) < .012f) col = negro;
            }
            float g = Hash(x, y, 300 + tramo) - .5f, f = Fbm(u, v, 16, 3, 40 + tramo) - .5f;
            px[y * w + x] = Shade(col, 1 + g * .25f + f * .2f);
        }
        return Save("camino_" + tramo, px, w, h, false, false);
    }

    // Ojo de calaverita: espiral morada sobre circulo, con alfa para recortarlo.
    public static Texture2D Espiral(Color fondo, Color linea)
    {
        const int n = 128;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float cu = (x - 63.5f) / 64f, cv = (y - 63.5f) / 64f;
            float r = Mathf.Sqrt(cu * cu + cv * cv), a = Mathf.Atan2(cv, cu);
            float espiral = Mathf.Repeat(r * 3.2f - a / 6.283f, 1);
            Color c = espiral < .28f && r < .85f && r > .08f ? linea : fondo;
            if (r > .9f) c = Shade(fondo, .55f);
            c.a = r < 1 ? 1 : 0;
            px[y * n + x] = c;
        }
        return Save("espiral_" + ColorUtility.ToHtmlStringRGB(fondo), px, n, n, true, false);
    }

    // Jaguar de fiesta: piel de color con rosetas y huesos blancos pintados.
    public static Texture2D JaguarHuesos(int variante)
    {
        const int n = 512;
        var px = new Color[n * n];
        Color[] bases = { new Color(1f, .55f, .08f), new Color(.85f, .15f, .5f) };
        Color fondo = bases[variante % 2], negro = new Color(.06f, .05f, .07f), hueso = new Color(.97f, .95f, .9f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n, v = y / (float)n;
            Color c = Color.Lerp(fondo, variante % 2 == 0 ? new Color(.98f, .85f, .2f) : new Color(.45f, .15f, .7f), Mathf.Abs(v - .5f) * 1.6f);
            // Rosetas.
            Vector2 cel = new Vector2(Mathf.Repeat(u * 12 + Mathf.Floor(v * 8) * .5f, 1) - .5f, Mathf.Repeat(v * 8, 1) - .5f);
            float rr = cel.magnitude + (Hash(Mathf.FloorToInt(u * 12), Mathf.FloorToInt(v * 8), 3) - .5f) * .08f;
            if (rr > .2f && rr < .32f && Mathf.Repeat(Mathf.Atan2(cel.y, cel.x) * 3 / 6.283f, 1) < .8f) c = negro;
            if (rr < .12f) c = new Color(.1f, .62f, .66f);
            // Costillas y columna.
            float costilla = Mathf.Abs(Mathf.Repeat(u * 10, 1) - .5f);
            if (v > .35f && v < .65f && costilla < .07f && u > .2f && u < .8f) c = hueso;
            if (Mathf.Abs(v - .5f) < .025f) c = hueso;
            if (Mathf.Abs(v - .5f) < .045f && Mathf.Repeat(u * 30, 1) < .35f) c = hueso;
            px[y * n + x] = Shade(c, 1 + (Hash(x, y, 9 + variante) - .5f) * .06f);
        }
        return Save("jaguar_" + variante, px, n, n, false);
    }

    // Luna llena con mares oscuros y un halo suave alrededor.
    public static Texture2D Luna()
    {
        const int n = 256;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float cu = (x - 127.5f) / 128f, cv = (y - 127.5f) / 128f, r = Mathf.Sqrt(cu * cu + cv * cv);
            float disco = r / .62f;
            Color c;
            if (disco < 1)
            {
                float mar = Fbm(x / (float)n, y / (float)n, 3, 4, 71);
                float crater = Fbm(x / (float)n, y / (float)n, 12, 2, 72);
                float k = .95f - Mathf.Clamp01((mar - .48f) * 3f) * .35f - Mathf.Clamp01((crater - .6f) * 4f) * .12f;
                k *= 1 - Mathf.Pow(disco, 6) * .25f;
                c = new Color(1f * k, .97f * k, .9f * k, 1);
            }
            else
            {
                float h = Mathf.Clamp01(1 - (r - .62f) / .38f);
                c = new Color(.55f, .6f, .75f, h * h * .35f);
            }
            px[y * n + x] = c;
        }
        return Save("luna", px, n, n, true, false);
    }
}
