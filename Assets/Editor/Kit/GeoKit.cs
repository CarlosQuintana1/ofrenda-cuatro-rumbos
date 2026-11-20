using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Geo
{
    public readonly List<Vector3> V = new List<Vector3>();
    public readonly List<Vector3> N = new List<Vector3>();
    public readonly List<Vector2> U = new List<Vector2>();
    public readonly List<int> T = new List<int>();
    public List<Vector2> U1;
    public List<Vector4> U2;
    public List<Color> C;
}

// Acumula geometria por material y la vuelca en pocas mallas guardadas como asset.
public static class GeoKit
{
    public static string MeshFolder = "Assets/MemoriaViva/Meshes";
    public static Dictionary<string, Material> Materials;
    private static readonly Dictionary<string, Geo> open = new Dictionary<string, Geo>();
    private static int meshCounter;

    public static void Reset() { open.Clear(); meshCounter = 0; }

    // Aparta la geometria abierta para construir una pieza independiente.
    public static Dictionary<string, Geo> Swap(Dictionary<string, Geo> next)
    {
        var old = new Dictionary<string, Geo>(open);
        open.Clear();
        if (next != null) foreach (var p in next) open[p.Key] = p.Value;
        return old;
    }

    public static Geo Get(string key)
    {
        if (!open.TryGetValue(key, out var g)) { g = new Geo(); open[key] = g; }
        return g;
    }

    public static Vector3 Pos(float compass, float r, float y = 0)
    {
        float a = compass * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
    }

    public static Vector3 Radial(float compass) => Pos(compass, 1);

    // ---------- primitivas basicas ----------

    public static void Tri(string key, Vector3 a, Vector3 b, Vector3 c, Vector3 want,
        Vector2 ua = default, Vector2 ub = default, Vector2 uc = default, bool smooth = false,
        Vector3 na = default, Vector3 nb = default, Vector3 nc = default)
    {
        var g = Get(key);
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(n, want) < 0)
        {
            (b, c) = (c, b); (ub, uc) = (uc, ub); (nb, nc) = (nc, nb);
            n = -n;
        }
        n.Normalize();
        int i = g.V.Count;
        g.V.Add(a); g.V.Add(b); g.V.Add(c);
        g.N.Add(smooth ? na : n); g.N.Add(smooth ? nb : n); g.N.Add(smooth ? nc : n);
        g.U.Add(ua); g.U.Add(ub); g.U.Add(uc);
        g.T.Add(i); g.T.Add(i + 1); g.T.Add(i + 2);
    }

    // Quad convexo a-b-c-d en orden perimetral, orientado hacia want.
    public static void Quad(string key, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want,
        Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, bool twoSided = false)
    {
        Tri(key, a, b, c, want, ua, ub, uc);
        Tri(key, a, c, d, want, ua, uc, ud);
        if (twoSided)
        {
            Tri(key, a, b, c, -want, ua, ub, uc);
            Tri(key, a, c, d, -want, ua, uc, ud);
        }
    }

    public static void QuadUV(string key, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want, float uvScale)
    {
        // UV planar sobre el propio quad, en metros.
        Vector3 ax = (b - a).normalized;
        Vector3 ay = Vector3.Cross(want.normalized, ax).normalized;
        Vector2 P(Vector3 p) => new Vector2(Vector3.Dot(p - a, ax), Vector3.Dot(p - a, ay)) / uvScale;
        Quad(key, a, b, c, d, want, P(a), P(b), P(c), P(d));
    }

    public static void Box(string key, Vector3 center, Vector3 size, Quaternion rot, float uvScale = 1f, bool skipBottom = true)
    {
        Vector3 h = size * .5f;
        Vector3 X = rot * Vector3.right, Y = rot * Vector3.up, Z = rot * Vector3.forward;
        void Face(Vector3 n, Vector3 u, Vector3 v, float hn, float hu, float hv)
        {
            Vector3 c = center + n * hn;
            Vector3 a = c - u * hu - v * hv, b = c - u * hu + v * hv, cc = c + u * hu + v * hv, d = c + u * hu - v * hv;
            Vector2 A = Vector2.zero, B = new Vector2(0, 2 * hv / uvScale), C = new Vector2(2 * hu / uvScale, 2 * hv / uvScale), D = new Vector2(2 * hu / uvScale, 0);
            Quad(key, a, b, cc, d, n, A, B, C, D);
        }
        Face(Z, X, Y, h.z, h.x, h.y);
        Face(-Z, X, Y, h.z, h.x, h.y);
        Face(X, Z, Y, h.x, h.z, h.y);
        Face(-X, Z, Y, h.x, h.z, h.y);
        Face(Y, X, Z, h.y, h.x, h.z);
        if (!skipBottom) Face(-Y, X, Z, h.y, h.x, h.z);
    }

    public static void Box(string key, Vector3 center, Vector3 size, float yaw = 0, float uvScale = 1f)
        => Box(key, center, size, Quaternion.Euler(0, yaw, 0), uvScale);

    // Caja definida por su base (centro inferior).
    public static void Block(string key, Vector3 baseCenter, Vector3 size, float yaw = 0, float uvScale = 1f)
        => Box(key, baseCenter + Vector3.up * size.y * .5f, size, Quaternion.Euler(0, yaw, 0), uvScale);

    public static void Cylinder(string key, Vector3 baseCenter, float r0, float r1, float height, int segs,
        Quaternion rot, bool capTop = true, bool capBottom = false, float uvScale = 1f)
    {
        var g = Get(key);
        Vector3 up = rot * Vector3.up;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * Mathf.PI * 2 / segs, a1 = (i + 1) * Mathf.PI * 2 / segs;
            Vector3 d0 = rot * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 d1 = rot * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 p0 = baseCenter + d0 * r0, p1 = baseCenter + d1 * r0;
            Vector3 q0 = baseCenter + up * height + d0 * r1, q1 = baseCenter + up * height + d1 * r1;
            float slope = (r0 - r1) / Mathf.Max(height, .0001f);
            Vector3 n0 = (d0 + up * slope).normalized, n1 = (d1 + up * slope).normalized;
            float u0 = i * (r0 * Mathf.PI * 2 / segs) / uvScale, u1 = (i + 1) * (r0 * Mathf.PI * 2 / segs) / uvScale;
            float vv = height / uvScale;
            Tri(key, p0, q0, q1, (n0 + n1), new Vector2(u0, 0), new Vector2(u0, vv), new Vector2(u1, vv), true, n0, n0, n1);
            Tri(key, p0, q1, p1, (n0 + n1), new Vector2(u0, 0), new Vector2(u1, vv), new Vector2(u1, 0), true, n0, n1, n1);
            if (capTop && r1 > 0)
                Tri(key, baseCenter + up * height, q0, q1, up, new Vector2(.5f, .5f), new Vector2(.5f + d0.x * .5f, .5f + d0.z * .5f), new Vector2(.5f + d1.x * .5f, .5f + d1.z * .5f));
            if (capBottom && r0 > 0)
                Tri(key, baseCenter, p0, p1, -up);
        }
    }

    public static void Cylinder(string key, Vector3 baseCenter, float radius, float height, int segs = 10, bool capTop = true)
        => Cylinder(key, baseCenter, radius, radius, height, segs, Quaternion.identity, capTop);

    // Cilindro entre dos puntos.
    public static void Rod(string key, Vector3 a, Vector3 b, float radius, int segs = 6)
    {
        Vector3 d = b - a;
        if (d.sqrMagnitude < 1e-6f) return;
        Cylinder(key, a, radius, radius, d.magnitude, segs, Quaternion.FromToRotation(Vector3.up, d.normalized), false);
    }

    public static void Ellipsoid(string key, Vector3 center, Vector3 radii, Quaternion rot, int rings = 6, int segs = 8, float lowerCut = 0)
    {
        // lowerCut > 0 recorta la parte inferior (0..1 de la altura) para domos.
        int r0 = Mathf.RoundToInt(rings * lowerCut);
        for (int i = 0; i < rings - r0; i++)
        {
            float t0 = Mathf.PI * (1 - (float)(i + r0) / rings) - Mathf.PI / 2;
            float t1 = Mathf.PI * (1 - (float)(i + r0 + 1) / rings) - Mathf.PI / 2;
            t0 = -t0; t1 = -t1;
            for (int j = 0; j < segs; j++)
            {
                float p0 = j * Mathf.PI * 2 / segs, p1 = (j + 1) * Mathf.PI * 2 / segs;
                Vector3 S(float t, float p) => new Vector3(Mathf.Cos(t) * Mathf.Cos(p), Mathf.Sin(t), Mathf.Cos(t) * Mathf.Sin(p));
                Vector3 a = S(t0, p0), b = S(t1, p0), c = S(t1, p1), d = S(t0, p1);
                Vector3 W(Vector3 s) => center + rot * Vector3.Scale(s, radii);
                Vector3 Nn(Vector3 s) => (rot * new Vector3(s.x / radii.x, s.y / radii.y, s.z / radii.z)).normalized;
                Vector3 mid = (a + b + c + d) * .25f;
                Vector2 Uv(float t, float p) => new Vector2(p / (Mathf.PI * 2), t / Mathf.PI + .5f);
                Tri(key, W(a), W(b), W(c), Nn(mid), Uv(t0, p0), Uv(t1, p0), Uv(t1, p1), true, Nn(a), Nn(b), Nn(c));
                Tri(key, W(a), W(c), W(d), Nn(mid), Uv(t0, p0), Uv(t1, p1), Uv(t0, p1), true, Nn(a), Nn(c), Nn(d));
            }
        }
    }

    public static void Sphere(string key, Vector3 center, float r, int rings = 5, int segs = 7)
        => Ellipsoid(key, center, Vector3.one * r, Quaternion.identity, rings, segs);

    // Superficie de revolucion: perfil (radio, altura).
    public static void Lathe(string key, Vector3 baseCenter, Vector2[] profile, int segs, float uvScale = 1f)
    {
        for (int k = 0; k < profile.Length - 1; k++)
        {
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2 / segs, a1 = (i + 1) * Mathf.PI * 2 / segs;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 p00 = baseCenter + d0 * profile[k].x + Vector3.up * profile[k].y;
                Vector3 p01 = baseCenter + d1 * profile[k].x + Vector3.up * profile[k].y;
                Vector3 p10 = baseCenter + d0 * profile[k + 1].x + Vector3.up * profile[k + 1].y;
                Vector3 p11 = baseCenter + d1 * profile[k + 1].x + Vector3.up * profile[k + 1].y;
                Vector2 e = profile[k + 1] - profile[k];
                if (e.sqrMagnitude < 1e-8f) continue;
                // El perfil se recorre de abajo hacia arriba por fuera; la normal sale a la derecha del recorrido.
                Vector3 out0 = (d0 * e.y - Vector3.up * e.x).normalized, out1 = (d1 * e.y - Vector3.up * e.x).normalized;
                Vector3 want = out0 + out1;
                if (profile[k].x < 1e-4f && profile[k + 1].x < 1e-4f) continue;
                Tri(key, p00, p10, p11, want, new Vector2(a0, profile[k].y) / uvScale, new Vector2(a0, profile[k + 1].y) / uvScale, new Vector2(a1, profile[k + 1].y) / uvScale, true, out0, out0, out1);
                Tri(key, p00, p11, p01, want, new Vector2(a0, profile[k].y) / uvScale, new Vector2(a1, profile[k + 1].y) / uvScale, new Vector2(a1, profile[k].y) / uvScale, true, out0, out1, out1);
            }
        }
    }

    // ---------- piezas curvas ----------

    // Anillo horizontal (sector) con UV planar en XZ.
    public static void Ring(string key, Vector3 center, float inner, float outer, float y, float a0, float a1, int segs,
        float uvScale = 1f, bool down = false)
    {
        Vector3 want = down ? Vector3.down : Vector3.up;
        for (int i = 0; i < segs; i++)
        {
            float b0 = Mathf.Lerp(a0, a1, (float)i / segs), b1 = Mathf.Lerp(a0, a1, (float)(i + 1) / segs);
            Vector3 p0 = center + Pos(b0, inner, y), p1 = center + Pos(b0, outer, y), p2 = center + Pos(b1, outer, y), p3 = center + Pos(b1, inner, y);
            Vector2 U(Vector3 p) => new Vector2(p.x, p.z) / uvScale;
            if (inner <= .0001f)
                Tri(key, p0, p1, p2, want, U(p0), U(p1), U(p2));
            else
                Quad(key, p0, p1, p2, p3, want, U(p0), U(p1), U(p2), U(p3));
        }
    }

    // Muro curvo vertical a radio fijo; inward=true mira hacia el centro.
    public static void CurvedWall(string key, Vector3 center, float r, float a0, float a1, float y0, float y1, int segs,
        bool inward, float uvScale = 1f, float uOffset = 0)
    {
        for (int i = 0; i < segs; i++)
        {
            float b0 = Mathf.Lerp(a0, a1, (float)i / segs), b1 = Mathf.Lerp(a0, a1, (float)(i + 1) / segs);
            Vector3 n0 = Radial(b0) * (inward ? -1 : 1), n1 = Radial(b1) * (inward ? -1 : 1);
            Vector3 p0 = center + Pos(b0, r, y0), p1 = center + Pos(b0, r, y1), p2 = center + Pos(b1, r, y1), p3 = center + Pos(b1, r, y0);
            float u0 = uOffset + r * b0 * Mathf.Deg2Rad / uvScale, u1 = uOffset + r * b1 * Mathf.Deg2Rad / uvScale;
            Tri(key, p0, p1, p2, n0 + n1, new Vector2(u0, y0 / uvScale), new Vector2(u0, y1 / uvScale), new Vector2(u1, y1 / uvScale), true, n0, n0, n1);
            Tri(key, p0, p2, p3, n0 + n1, new Vector2(u0, y0 / uvScale), new Vector2(u1, y1 / uvScale), new Vector2(u1, y0 / uvScale), true, n0, n1, n1);
        }
    }

    // Poligono horizontal (piso o azotea) con UV planar en metros; acepta poligonos concavos.
    public static void Piso(string key, IList<Vector3> poly, float y, float uvScale)
    {
        var pts = new List<Vector2>();
        foreach (var p in poly) pts.Add(new Vector2(p.x, p.z));
        float area = 0;
        for (int i = 0; i < pts.Count; i++) { var p0 = pts[i]; var p1 = pts[(i + 1) % pts.Count]; area += p0.x * p1.y - p1.x * p0.y; }
        if (area < 0) pts.Reverse();
        var idx = new List<int>();
        for (int i = 0; i < pts.Count; i++) idx.Add(i);
        float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        Vector3 W(Vector2 p) => new Vector3(p.x, y, p.y);
        void T(Vector2 a, Vector2 b, Vector2 c) => Tri(key, W(a), W(b), W(c), Vector3.up, a / uvScale, b / uvScale, c / uvScale);
        int guard = 0;
        while (idx.Count > 3 && guard++ < 20000)
        {
            int ear = -1;
            for (int k = 0; k < idx.Count && ear < 0; k++)
            {
                int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                Vector2 a = pts[ia], b = pts[ib], c = pts[ic];
                if (Cross(a, b, c) <= 1e-7f) continue;
                bool inside = false;
                foreach (int j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    Vector2 q = pts[j];
                    if (Cross(a, b, q) > 1e-7f && Cross(b, c, q) > 1e-7f && Cross(c, a, q) > 1e-7f) { inside = true; break; }
                }
                if (!inside) ear = k;
            }
            if (ear < 0)
            {
                float best = float.MaxValue;
                for (int k = 0; k < idx.Count; k++)
                {
                    float cr = Mathf.Abs(Cross(pts[idx[(k + idx.Count - 1) % idx.Count]], pts[idx[k]], pts[idx[(k + 1) % idx.Count]]));
                    if (cr < best) { best = cr; ear = k; }
                }
            }
            Vector2 ea = pts[idx[(ear + idx.Count - 1) % idx.Count]], eb = pts[idx[ear]], ec = pts[idx[(ear + 1) % idx.Count]];
            if (Cross(ea, eb, ec) > 1e-7f) T(ea, eb, ec);
            idx.RemoveAt(ear);
        }
        if (idx.Count == 3) T(pts[idx[0]], pts[idx[1]], pts[idx[2]]);
    }

    // Silueta 2D extruida: poly en (u, v), u y v son ejes del mundo, depth hacia -normal.
    public static void Extrude(string key, IList<Vector2> poly, Vector3 origin, Vector3 u, Vector3 v, float depth)
    {
        var pts = new List<Vector2>(poly);
        float area = 0;
        for (int i = 0; i < pts.Count; i++) { var p0 = pts[i]; var p1 = pts[(i + 1) % pts.Count]; area += p0.x * p1.y - p1.x * p0.y; }
        if (area < 0) pts.Reverse();
        Vector3 n = Vector3.Cross(u, v).normalized;
        Vector3 W(Vector2 p, float d) => origin + u * p.x + v * p.y - n * d;
        // Triangulacion por recorte de orejas; los vertices se comparan por indice y los bordes no bloquean.
        var idx = new List<int>();
        for (int i = 0; i < pts.Count; i++) idx.Add(i);
        float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        int guard = 0;
        while (idx.Count > 3 && guard++ < 20000)
        {
            int ear = -1;
            for (int k = 0; k < idx.Count && ear < 0; k++)
            {
                int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                Vector2 a = pts[ia], b = pts[ib], c = pts[ic];
                if (Cross(a, b, c) <= 1e-7f) continue;
                bool inside = false;
                foreach (int j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    Vector2 q = pts[j];
                    if (Cross(a, b, q) > 1e-7f && Cross(b, c, q) > 1e-7f && Cross(c, a, q) > 1e-7f) { inside = true; break; }
                }
                if (!inside) ear = k;
            }
            if (ear < 0)
            {
                // Sin oreja limpia: quita el vertice mas degenerado para no dejar huecos.
                float best = float.MaxValue;
                for (int k = 0; k < idx.Count; k++)
                {
                    float cr = Mathf.Abs(Cross(pts[idx[(k + idx.Count - 1) % idx.Count]], pts[idx[k]], pts[idx[(k + 1) % idx.Count]]));
                    if (cr < best) { best = cr; ear = k; }
                }
            }
            Vector2 ea = pts[idx[(ear + idx.Count - 1) % idx.Count]], eb = pts[idx[ear]], ec = pts[idx[(ear + 1) % idx.Count]];
            if (Cross(ea, eb, ec) > 1e-7f)
            {
                Tri(key, W(ea, 0), W(eb, 0), W(ec, 0), n);
                Tri(key, W(ea, depth), W(eb, depth), W(ec, depth), -n);
            }
            idx.RemoveAt(ear);
        }
        if (idx.Count == 3)
        {
            Tri(key, W(pts[idx[0]], 0), W(pts[idx[1]], 0), W(pts[idx[2]], 0), n);
            Tri(key, W(pts[idx[0]], depth), W(pts[idx[1]], depth), W(pts[idx[2]], depth), -n);
        }
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 p0 = pts[i], p1 = pts[(i + 1) % pts.Count];
            Vector2 e = p1 - p0;
            Vector3 outward = (u * e.y - v * e.x).normalized;
            Quad(key, W(p0, 0), W(p1, 0), W(p1, depth), W(p0, depth), outward, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        }
    }

    // Superficie continua f(u, v), u alrededor (cerrada) y v a lo largo, con vertices compartidos y normales suaves.
    public static void Superficie(string key, System.Func<float, float, Vector3> f, int nu, int nv)
    {
        var g = Get(key);
        var P = new Vector3[nu, nv + 1];
        for (int j = 0; j <= nv; j++)
            for (int i = 0; i < nu; i++) P[i, j] = f((float)i / nu, (float)j / nv);
        var C = new Vector3[nv + 1];
        for (int j = 0; j <= nv; j++) { for (int i = 0; i < nu; i++) C[j] += P[i, j]; C[j] /= nu; }
        var N = new Vector3[nu, nv + 1];
        float signo = 0;
        for (int j = 0; j <= nv; j++)
            for (int i = 0; i < nu; i++)
            {
                Vector3 du = P[(i + 1) % nu, j] - P[(i + nu - 1) % nu, j];
                Vector3 dv = P[i, Mathf.Min(j + 1, nv)] - P[i, Mathf.Max(j - 1, 0)];
                N[i, j] = Vector3.Cross(du, dv);
                signo += Vector3.Dot(N[i, j], P[i, j] - C[j]);
            }
        float s = signo < 0 ? -1 : 1;
        for (int j = 0; j <= nv; j++)
            for (int i = 0; i < nu; i++)
            {
                Vector3 n = N[i, j] * s;
                // En los polos el anillo se colapsa: la normal sigue el eje.
                if (n.sqrMagnitude < 1e-12f) n = P[i, j] - C[j == 0 ? 1 : j == nv ? nv - 1 : j];
                N[i, j] = n.normalized;
            }
        int b = g.V.Count;
        for (int j = 0; j <= nv; j++)
            for (int i = 0; i < nu; i++) { g.V.Add(P[i, j]); g.N.Add(N[i, j]); g.U.Add(new Vector2((float)i / nu, (float)j / nv)); }
        for (int j = 0; j < nv; j++)
            for (int i = 0; i < nu; i++)
            {
                int a = b + j * nu + i, c = b + j * nu + (i + 1) % nu, d = a + nu, e = c + nu;
                if (s > 0) { g.T.Add(a); g.T.Add(c); g.T.Add(e); g.T.Add(a); g.T.Add(e); g.T.Add(d); }
                else { g.T.Add(a); g.T.Add(e); g.T.Add(c); g.T.Add(a); g.T.Add(d); g.T.Add(e); }
            }
    }

    // Tubo a lo largo de una polilinea.
    public static void Tube(string key, IList<Vector3> pts, float radius, int segs = 6)
    {
        for (int i = 0; i < pts.Count - 1; i++) Rod(key, pts[i], pts[i + 1], radius, segs);
    }

    // ---------- sprites animados en GPU ----------

    public static void Sprite(string key, Vector3 center, Vector2 halfSize, Color color, float phase, Vector4 data = default)
    {
        var g = Get(key);
        if (g.U1 == null) { g.U1 = new List<Vector2>(); g.U2 = new List<Vector4>(); g.C = new List<Color>(); }
        int i = g.V.Count;
        Vector2[] corners = { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) };
        foreach (var c in corners)
        {
            g.V.Add(center);
            g.N.Add(Vector3.up);
            g.U.Add(new Vector2(c.x * .5f + .5f, c.y * .5f + .5f));
            g.U1.Add(Vector2.Scale(c, halfSize));
            g.U2.Add(data);
            g.C.Add(new Color(color.r, color.g, color.b, phase));
        }
        g.T.Add(i); g.T.Add(i + 1); g.T.Add(i + 2);
        g.T.Add(i); g.T.Add(i + 2); g.T.Add(i + 3);
    }

    // ---------- salida ----------

    public static List<GameObject> Flush(Transform parent, string name, Vector3 origin = default, bool isStatic = true)
    {
        var made = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(MeshFolder))
        {
            string up = Path.GetDirectoryName(MeshFolder).Replace('\\', '/');
            AssetDatabase.CreateFolder(up, Path.GetFileName(MeshFolder));
        }
        foreach (var pair in open)
        {
            var g = pair.Value;
            if (g.V.Count == 0) continue;
            string[] parts = pair.Key.Split('|');
            string matName = parts[0];
            bool collide = parts.Length > 1 && parts[1] == "c";
            bool sinSombra = System.Array.IndexOf(parts, "s") > 0;
            var verts = new List<Vector3>(g.V.Count);
            foreach (var v in g.V) verts.Add(v - origin);
            var mesh = new Mesh { name = name + " · " + matName, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(g.N);
            mesh.SetUVs(0, g.U);
            if (g.U1 != null) { mesh.SetUVs(1, g.U1); mesh.SetUVs(2, g.U2); mesh.SetColors(g.C); }
            mesh.SetTriangles(g.T, 0);
            mesh.RecalculateBounds();
            if (g.U1 != null)
            {
                var b = mesh.bounds; b.Expand(24f); mesh.bounds = b;
            }
            else mesh.RecalculateTangents();
            var go = new GameObject(name + " · " + matName);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (!Materials.TryGetValue(matName, out var mat)) throw new System.Exception("Material no definido: " + matName);
            mr.sharedMaterial = mat;
            if (g.U1 != null) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
            else if (sinSombra) mr.shadowCastingMode = ShadowCastingMode.Off;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            if (isStatic && g.U1 == null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            string path = MeshFolder + "/m" + (meshCounter++).ToString("0000") + ".asset";
            AssetDatabase.CreateAsset(mesh, path);
            made.Add(go);
        }
        open.Clear();
        return made;
    }
}
