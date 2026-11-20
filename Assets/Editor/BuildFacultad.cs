using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GeoKit;

// Explanada, gradas, edificio curvo y entorno de la FIF, a partir de fotos y vista aerea.
public static partial class BuildMemoriaViva
{
    // Medidas tomadas de la vista aerea de Google Maps con su barra de 20 m (6.85 px/m),
    // con el trazado ampliado por E para que el recorrido no se sienta apretado. Alturas y objetos van a tamano real.
    private const float E = 1.25f;
    private static Vector3 Lay(float x, float y, float z) => new Vector3(x * E, y, z * E);
    private const float GradasIn = 10.6f * E, GradaDepth = 1.1f, GradaH = .42f;
    private const int GradaRows = 5;
    private const float TerraceY = GradaH * GradaRows;
    private const float GradasA0 = 112f, GradasA1 = 252f;
    private const float FacadeR = 21.5f * E, BuildA0 = 128f, BuildA1 = 236f, BuildOuterR = 33f * E;
    private const float FacadeH = 11.6f;
    private const float TerraceOuter = 34f * E, PlazaR = 19f * E;
    private static float GradasOut => GradasIn + GradaDepth * GradaRows;
    private static readonly float[] Aisles = { 150f, 182f, 214f };
    private const float AisleHalf = .8f;
    private const float UvLadrillo = 1.2f, UvConcreto = 3f, UvPasto = 3f, UvFachada = 3.6f, UvHex = .9f;

    private static float SIn(float r) => Mathf.Clamp01((r - GradasIn) / (GradaDepth * GradaRows));
    private static float SOut(float r) => Mathf.Clamp01((TerraceOuter + 6 - r) / 6f);

    private static Vector3 Tangent(float compass)
    {
        float a = compass * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
    }

    // Malla polar con altura variable (terraza, taludes, colinas).
    private static void PolarSurface(string key, float[] rs, float a0, float a1, int segsA, Func<float, float, float> h, float uv)
    {
        for (int k = 0; k < rs.Length - 1; k++)
        for (int i = 0; i < segsA; i++)
        {
            float b0 = Mathf.Lerp(a0, a1, (float)i / segsA), b1 = Mathf.Lerp(a0, a1, (float)(i + 1) / segsA);
            float r0 = rs[k], r1 = rs[k + 1];
            Vector3 P(float r, float a) => Pos(a, r, h(r, a));
            Vector3 p0 = P(r0, b0), p1 = P(r1, b0), p2 = P(r1, b1), p3 = P(r0, b1);
            Vector2 U(Vector3 p) => new Vector2(p.x, p.z) / uv;
            Quad(key, p0, p1, p2, p3, Vector3.up, U(p0), U(p1), U(p2), U(p3));
        }
    }

    private static void Terreno()
    {
        Group("02 | Terreno, terraza y colinas");
        Ring("tierra", Vector3.zero, 0, 1200, -.08f, 0, 360, 48, 6);
        Quad("pasto|c", new Vector3(-170, -.02f, -170), new Vector3(-170, -.02f, 170), new Vector3(170, -.02f, 170), new Vector3(170, -.02f, -170),
            Vector3.up, new Vector2(-57, -57), new Vector2(-57, 57), new Vector2(57, 57), new Vector2(57, -57));
        // Terraza al nivel del edificio, mas alta que la explanada.
        PolarSurface("pasto|c", new[] { GradasOut, 25f * E, TerraceOuter, TerraceOuter + 6 }, GradasA0, GradasA1, 80, (r, a) => TerraceY * SOut(r), UvPasto);
        float[] slopes = { GradasIn, GradasIn + 1.5f, GradasIn + 3f, GradasOut, 20f * E, 27f * E, TerraceOuter, TerraceOuter + 6 };
        PolarSurface("pasto|c", slopes, GradasA1, 264, 16, (r, a) => TerraceY * SIn(r) * SOut(r) * Mathf.Clamp01((264 - a) / 12f), UvPasto);
        Hills();
    }

    private static void Hills()
    {
        float[] rs = { 260, 340, 440, 560, 700, 860, 1040, 1200 };
        PolarSurface("colinas", rs, 0, 360, 120, (r, a) =>
        {
            float ang = a * Mathf.Deg2Rad;
            float n = Mathf.PerlinNoise(Mathf.Cos(ang) * 2.1f + 10, Mathf.Sin(ang) * 2.1f + 10);
            float m = Mathf.PerlinNoise(Mathf.Cos(ang) * 6f + 30, Mathf.Sin(ang) * 6f + 30);
            float ramp = Mathf.Clamp01((r - 260) / 600f);
            float far = 1 - Mathf.Clamp01((r - 950) / 250f) * .5f;
            // Las sierras de Juriquilla se ven mas altas al poniente y al norte.
            float west = .6f + .4f * Mathf.Clamp01(-Mathf.Sin(ang) * .8f + Mathf.Cos(ang) * .4f + .4f);
            return ramp * ramp * far * west * (14 + 95 * n * n + 22 * m) - 3;
        }, 8);
    }

    private static void Plaza()
    {
        Group("03 | Explanada circular");
        Vector3 c = Vector3.zero;
        // Medallon central (gris con anillo negro y coladera), textura centrada.
        int segs = 72;
        for (int i = 0; i < segs; i++)
        {
            float b0 = i * 360f / segs, b1 = (i + 1) * 360f / segs;
            Vector3 p1 = Pos(b0, 2.2f * E, .025f), p2 = Pos(b1, 2.2f * E, .025f), p0 = new Vector3(0, .025f, 0);
            Vector2 U(Vector3 p) => new Vector2(p.x / (4.4f * E) + .5f, p.z / (4.4f * E) + .5f);
            Tri("medallon|c", p0, p1, p2, Vector3.up, U(p0), U(p1), U(p2));
        }
        Ring("estampado|c", c, 2.2f * E, 6.4f * E, .02f, 0, 360, 96, 3f);
        Ring("concreto|c", c, 6.4f * E, 6.8f * E, .02f, 0, 360, 96, UvConcreto);
        for (int k = 0; k < 16; k++)
        {
            float ac = k * 22.5f + 11.25f;
            Ring("pasto|c", c, 6.95f * E, 8.85f * E, .03f, ac - 7.2f, ac + 7.2f, 4, UvPasto);
            Ring("concreto|c", c, 6.8f * E, 9f * E, .02f, ac + 7.2f, ac + 15.3f, 3, UvConcreto);
            // Guarnicion delgada alrededor de cada isleta.
            Ring("concreto|c", c, 6.8f * E, 6.95f * E, .02f, ac - 7.2f, ac + 7.2f, 4, UvConcreto);
            Ring("concreto|c", c, 8.85f * E, 9f * E, .02f, ac - 7.2f, ac + 7.2f, 4, UvConcreto);
        }
        // Borde de cempasuchil en cada isleta.
        for (int k = 0; k < 16; k++)
        {
            float ac = k * 22.5f + 11.25f;
            foreach (float rr in new[] { 6.95f * E + .14f, 8.85f * E - .14f })
                for (float a = ac - 6.6f; a <= ac + 6.6f; a += 13.2f / Mathf.Round(rr * .23f * 1.15f / .26f))
                {
                    Vector3 q = Pos(a, rr, .03f);
                    Ellipsoid("hoja_flor", q + Vector3.up * .06f, new Vector3(.09f, .07f, .09f), Quaternion.identity, 2, 5);
                    Cabeza(q + Vector3.up * .17f, Vector3.up, .07f, k % 2 == 0 ? "cempasuchil" : "cempasuchil_oscuro");
                }
        }
        Ring("concreto|c", c, 9f * E, 9.6f * E, .02f, 0, 360, 128, UvConcreto);
        Ring("ladrillo|c", c, 9.6f * E, GradasIn, .015f, 0, 360, 128, UvLadrillo);
        Ring("ladrillo|c", c, GradasIn, PlazaR, .015f, GradasA1, GradasA0 + 360, 80, UvLadrillo);
        Ring("banqueta", c, PlazaR, PlazaR + .3f, .05f, GradasA1 + 12, GradasA0 + 360, 80, UvConcreto);
        CurvedWall("banqueta", c, PlazaR + .3f, GradasA1 + 12, GradasA0 + 360, -.02f, .05f, 80, false, UvConcreto);
    }

    private static void Gradas()
    {
        Group("04 | Gradas y terraza superior");
        Vector3 c = Vector3.zero;
        for (int k = 0; k < GradaRows; k++)
        {
            float r0 = GradasIn + k * GradaDepth, r1 = r0 + GradaDepth;
            float y0 = k * GradaH, y1 = y0 + GradaH;
            float rm = r0 + GradaDepth * .5f;
            float delta = AisleHalf / rm * Mathf.Rad2Deg;
            float start = GradasA0;
            for (int s = 0; s <= Aisles.Length; s++)
            {
                float end = s < Aisles.Length ? Aisles[s] - delta : GradasA1;
                int n = Mathf.Max(4, Mathf.RoundToInt((end - start) / 1.2f));
                Ring("gradas|c", c, r0, r1, y1, start, end, n, UvConcreto);
                CurvedWall("gradas|c", c, r0, start, end, y0, y1, n, true, UvConcreto);
                // Arista oscura por el desgaste del borde.
                Ring("ojo_buey", c, r0, r0 + .05f, y1 + .002f, start, end, n, 1);
                if (s > 0) RadialCap(start, r0, r1, y1, -1);
                if (s < Aisles.Length) RadialCap(end, r0, r1, y1, 1);
                if (s < Aisles.Length) start = Aisles[s] + delta;
            }
        }
        foreach (float a in Aisles)
            for (int j = 0; j < 12; j++)
                Block("concreto|c", Pos(a, GradasIn + (GradasOut - GradasIn) / 12 * (j + .5f)), new Vector3(AisleHalf * 2, TerraceY / 12 * (j + 1), (GradasOut - GradasIn) / 12), a, UvConcreto);
        foreach (float a in new[] { GradasA0, GradasA1 })
            Block("gradas|c", Pos(a, (GradasIn + GradasOut) / 2), new Vector3(.45f, TerraceY + .45f, GradasOut - GradasIn + .4f), a, UvConcreto);
        foreach (float a in new[] { GradasA1 + 5.5f })
        {
            for (int j = 0; j < 12; j++)
                Block("concreto|c", Pos(a, GradasIn + (GradasOut - GradasIn) / 12 * (j + .5f)), new Vector3(2.2f, TerraceY / 12 * (j + 1), (GradasOut - GradasIn) / 12), a, UvConcreto);
            foreach (int sd in new[] { -1, 1 })
            {
                Vector3 b0 = Pos(a, GradasIn, .95f) + Tangent(a) * sd * 1.15f, b1 = Pos(a, GradasOut, TerraceY + .95f) + Tangent(a) * sd * 1.15f;
                Rod("barandal", b0, b1, .03f, 6);
                for (int k = 0; k <= 4; k++)
                {
                    Vector3 q = Vector3.Lerp(b0, b1, k / 4f);
                    Rod("barandal", q - Vector3.up * .95f, q, .025f, 5);
                }
            }
        }
        // Andador y rejilla en lo alto de las gradas, banqueta al pie del edificio.
        float go = GradasOut;
        Ring("ojo_buey", c, go, go + .3f, TerraceY + .035f, GradasA0, GradasA1, 120, 1);
        Ring("concreto|c", c, go + .3f, go + 1.2f, TerraceY + .03f, GradasA0, GradasA1, 120, UvConcreto);
        Ring("hexagonal|c", c, go + 1.2f, go + 2.4f, TerraceY + .03f, GradasA0, GradasA1 + 4, 120, UvHex);
        Ring("concreto|c", c, FacadeR - .9f, FacadeR, TerraceY + .03f, BuildA0 - 2, BuildA1 + 2, 100, UvConcreto);
    }

    private static void RadialCap(float a, float r0, float r1, float y1, int side)
    {
        Vector3 want = Tangent(a) * side;
        Vector3 p0 = Pos(a, r0, 0), p1 = Pos(a, r0, y1), p2 = Pos(a, r1, y1), p3 = Pos(a, r1, 0);
        QuadUV("gradas|c", p0, p1, p2, p3, want, UvConcreto);
    }

    // ---------- edificio curvo ----------

    private static void Window(Vector3 center, Vector3 facing, float w, float h)
    {
        Vector3 up = Vector3.up;
        Vector3 lat = Vector3.Cross(up, facing).normalized;
        Quaternion rot = Quaternion.LookRotation(facing);
        float roll = Rnd();
        string glass = roll < .12f ? "ventana_luz" : roll < .27f ? "vidrio_oscuro" : "vidrio";
        Vector3 g = center + facing * .015f;
        Quad(glass, g - lat * w / 2 - up * h / 2, g - lat * w / 2 + up * h / 2, g + lat * w / 2 + up * h / 2, g + lat * w / 2 - up * h / 2,
            facing, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        Box("remate", center + up * (h / 2 + .04f) + facing * .05f, new Vector3(w + .16f, .08f, .1f), rot);
        Box("remate", center - up * (h / 2 + .045f) + facing * .07f, new Vector3(w + .26f, .09f, .16f), rot);
        Box("remate", center + lat * (w / 2 + .04f) + facing * .05f, new Vector3(.08f, h, .1f), rot);
        Box("remate", center - lat * (w / 2 + .04f) + facing * .05f, new Vector3(.08f, h, .1f), rot);
        Box("marco", center - up * (h / 2 - .48f) + facing * .04f, new Vector3(w, .07f, .08f), rot);
        Box("barandal", center - up * (h / 2 - .25f) + facing * .14f, new Vector3(w + .1f, .04f, .04f), rot);
        Box("marco", center + facing * .03f, new Vector3(.05f, h, .05f), rot);
    }

    private static void Porthole(Vector3 center, Vector3 facing, float r)
    {
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, facing);
        Cylinder("remate", center, r + .07f, r + .07f, .03f, 14, rot);
        Cylinder("ojo_buey", center, r, r, .06f, 14, rot);
    }

    private static void EdificioPrincipal()
    {
        Group("05 | Edificio curvo de la Facultad");
        Vector3 c = Vector3.zero;
        float y0 = TerraceY, top = TerraceY + FacadeH;
        CurvedWall("fachada|c", c, FacadeR, BuildA0, BuildA1, y0, top, 116, true, UvFachada);
        CurvedWall("fachada", c, BuildOuterR, BuildA0, BuildA1, y0, top, 80, false, UvFachada);
        // Azotea: losa con textura y pretiles de un metro que no proyectan sombra sobre ella.
        float azotea = top + .3f, pretil = top + 1.1f;
        Ring("azotea", c, FacadeR, BuildOuterR, azotea, BuildA0, BuildA1, 80, 6);
        CurvedWall("remate|s", c, FacadeR, BuildA0, BuildA1, top, pretil, 116, true, 4);
        CurvedWall("remate|s", c, FacadeR + .3f, BuildA0, BuildA1, azotea, pretil, 116, false, 4);
        Ring("remate|s", c, FacadeR, FacadeR + .3f, pretil, BuildA0, BuildA1, 116, 4);
        CurvedWall("remate|s", c, BuildOuterR, BuildA0, BuildA1, top, pretil, 80, false, 4);
        CurvedWall("remate|s", c, BuildOuterR - .3f, BuildA0, BuildA1, azotea, pretil, 80, true, 4);
        Ring("remate|s", c, BuildOuterR - .3f, BuildOuterR, pretil, BuildA0, BuildA1, 80, 4);
        foreach (float a in new[] { BuildA0, BuildA1 })
            Box("remate|s", Pos(a, (FacadeR + BuildOuterR) / 2, top + .55f) + Tangent(a) * (a == BuildA0 ? .15f : -.15f), new Vector3(.3f, 1.1f, BuildOuterR - FacadeR), Quaternion.Euler(0, a, 0));
        foreach (float a in new[] { BuildA0, BuildA1 })
            QuadUV("fachada", Pos(a, FacadeR, y0), Pos(a, FacadeR, top), Pos(a, BuildOuterR, top), Pos(a, BuildOuterR, y0), Tangent(a) * (a == BuildA0 ? -1 : 1), UvFachada);

        int cols = Mathf.RoundToInt((BuildA1 - BuildA0) / 3.2f);
        for (int i = 0; i < cols; i++)
        {
            float a = Mathf.Lerp(BuildA0 + 2.2f, BuildA1 - 2.2f, (float)i / (cols - 1));
            Vector3 n = -Radial(a);
            for (int f = 0; f < 3; f++)
                Window(Pos(a, FacadeR, y0 + .75f + f * 3.7f + .95f), n, 1.0f, 1.9f);
            Porthole(Pos(a, FacadeR, y0 + 10.9f), n, .26f);
        }
        Visera(c, y0 + 11.55f);
        for (int i = 0; i < 116; i++)
        {
            float b0 = Mathf.Lerp(BuildA0, BuildA1, i / 116f), b1 = Mathf.Lerp(BuildA0, BuildA1, (i + 1) / 116f);
            Vector3 nn = -Radial((b0 + b1) * .5f);
            Vector3 q0 = Pos(b0, FacadeR - .02f, y0), q1 = Pos(b1, FacadeR - .02f, y0);
            float u0 = FacadeR * b0 * Mathf.Deg2Rad / 4f, u1 = FacadeR * b1 * Mathf.Deg2Rad / 4f;
            Quad("mancha", q0, q0 + Vector3.up * 1.1f, q1 + Vector3.up * 1.1f, q1, nn, new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0));
        }

        // Azotea: cuarto de maquinas con escalera marina y paneles solares.
        float ta = 153f;
        Block("blanco_edificio", Pos(ta, 25.5f * E, top), new Vector3(4.2f, 3.4f, 4f), ta, 2);
        Vector3 ladder = Pos(ta, FacadeR + 1.95f, top);
        Vector3 lat = Tangent(ta);
        for (int s = -1; s <= 1; s += 2) Rod("metal", ladder + lat * s * .25f, ladder + lat * s * .25f + Vector3.up * 4.4f, .025f);
        for (int s = 0; s < 13; s++) Rod("metal", ladder - lat * .25f + Vector3.up * (.3f + s * .3f), ladder + lat * .25f + Vector3.up * (.3f + s * .3f), .015f);
        // Paneles con marco y patas; alrededor del catrin troyano el techo queda libre.
        Vector3 troyano = Pos(182, FacadeR + 2.7f, top);
        for (float r = FacadeR + 2.6f; r < BuildOuterR - 1.4f; r += 1.6f)
        for (float a = 162; a < 224; a += 2.3f / r * Mathf.Rad2Deg)
        {
            Vector3 pp = Pos(a, r, top + .85f);
            if (Vector3.Distance(new Vector3(pp.x, 0, pp.z), new Vector3(troyano.x, 0, troyano.z)) < 6.5f) continue;
            Quaternion qp = Quaternion.Euler(0, a, 0) * Quaternion.Euler(-16, 0, 0);
            Box("panel_solar", pp, new Vector3(2.0f, .05f, 1.1f), qp);
            Box("metal", pp - Vector3.up * .05f, new Vector3(1.9f, .04f, 1.0f), qp);
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 pie = pp + Tangent(a) * s * .8f;
                Rod("metal", new Vector3(pie.x, azotea, pie.z) + Radial(a) * .35f, pie + Radial(a) * .35f - Vector3.up * .2f, .025f, 4);
                Rod("metal", new Vector3(pie.x, azotea, pie.z) - Radial(a) * .35f, pie - Radial(a) * .35f + Vector3.up * .05f, .025f, 4);
            }
        }
        // Equipos de aire acondicionado junto al pretil exterior.
        foreach (float a in new[] { 140f, 146f, 228f, 232f })
        {
            Vector3 eq = Pos(a, BuildOuterR - 2.2f, azotea);
            Block("blanco_edificio", eq, new Vector3(1.6f, 1.1f, 1.3f), a, 1);
            Cylinder("negro", eq + Vector3.up * 1.1f, .42f, .42f, .04f, 14, Quaternion.identity, true);
        }

        // Remates radiales en ambos extremos y anexos al sur, como se ven en la vista aerea.
        // El bloque del oriente queda pegado a la terraza, con su costado ciego detras de la rampa.
        Remate(GradasA0, 8f, 11f, -1, 4.3f, true);
        Remate(244f, 8.5f, 16f * E, 1);
        Block("fachada", Pos(188, 39.5f * E), new Vector3(12, 10, 9), 188, UvFachada);
        Block("fachada", Pos(193, 48.5f * E), new Vector3(10, 8.5f, 8), 193, UvFachada);
    }

    // Visera curva con las puntas levantadas, como en las fotos.
    private static void Visera(Vector3 c, float ey)
    {
        // Solo vuela hacia la plaza: si entrara bajo la azotea, sus puntas levantadas la atravesarian.
        float a0 = BuildA0 - 1.2f, a1 = BuildA1 + 1.2f, ri = FacadeR - 1.65f, ro = FacadeR - .02f, th = .26f;
        int segs = 140;
        float Lift(float a)
        {
            float d = Mathf.Min(a - a0, a1 - a);
            float k = 1 - Mathf.Clamp01(d / 6f);
            return .25f * k * k;
        }
        for (int i = 0; i < segs; i++)
        {
            float b0 = Mathf.Lerp(a0, a1, (float)i / segs), b1 = Mathf.Lerp(a0, a1, (float)(i + 1) / segs);
            float l0 = Lift(b0), l1 = Lift(b1);
            Vector3 A(float r, float b, float l, float dy) => Pos(b, r, ey + l + dy);
            Quad("remate|s", A(ri, b0, l0, 0), A(ro, b0, l0, 0), A(ro, b1, l1, 0), A(ri, b1, l1, 0), Vector3.down, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            Quad("remate|s", A(ri, b0, l0, th), A(ro, b0, l0, th), A(ro, b1, l1, th), A(ri, b1, l1, th), Vector3.up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            Quad("remate|s", A(ri, b0, l0, 0), A(ri, b0, l0, th), A(ri, b1, l1, th), A(ri, b1, l1, 0), -Radial((b0 + b1) * .5f), Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            if (i % 4 == 2)
                Cylinder("foco", Pos(b0, FacadeR - .8f, ey + l0 - .025f), .07f, .07f, .03f, 8, Quaternion.identity, true);
        }
        foreach (float a in new[] { a0, a1 })
        {
            float l = Lift(a);
            Quad("remate|s", Pos(a, ri, ey + l), Pos(a, ri, ey + l + th), Pos(a, ro, ey + l + th), Pos(a, ro, ey + l), Tangent(a) * (a == a0 ? -1 : 1),
                Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
    }

    // Bloque de remate: caja radial un poco adelantada respecto a la fachada curva.
    private static void Remate(float a, float width, float depth, int side, float corrimiento = 0, bool ciego = false)
    {
        Vector3 up = Vector3.up;
        Vector3 n = -Radial(a);
        Vector3 t = Tangent(a) * side;
        float r0 = FacadeR - .6f, H = 12.2f;
        Vector3 front = Pos(a, r0, TerraceY) + Tangent(a) * corrimiento;
        Vector3 center = Pos(a, r0 + depth / 2, TerraceY + H / 2) + Tangent(a) * corrimiento;
        Box("fachada", center, new Vector3(width, H, depth), Quaternion.Euler(0, a, 0), UvFachada);
        ColliderBox("Remate", center, new Vector3(width, H, depth), a);
        Box("remate", front + up * (H + .05f) - n * depth / 2, new Vector3(width + .3f, .25f, depth + .3f), Quaternion.Euler(0, a, 0));
        for (int i = 0; i < 3; i++)
        {
            Vector3 b = front + Tangent(a) * ((i - 1) * 2.6f) + n * .0f;
            for (int f = 0; f < 3; f++) Window(b + up * (1.7f + f * 3.7f), n, 1.0f, 1.9f);
        }
        for (int i = 0; i < 6; i++) Porthole(front + Tangent(a) * ((i - 2.5f) * 1.25f) + up * 11.1f, n, .24f);
        Vector3 sideFace = front + t * (width / 2);
        int count = ciego ? 0 : Mathf.FloorToInt((depth - 2) / 2.4f);
        for (int i = 0; i < count; i++)
        {
            Vector3 b = sideFace - n * (1.8f + i * 2.4f);
            for (int f = 0; f < 3; f++) Window(b + up * (1.7f + f * 3.7f), t, 1.0f, 1.9f);
            Porthole(b + up * 11.1f - n * .6f, t, .24f);
            Porthole(b + up * 11.1f + n * .6f, t, .24f);
        }
    }

    // ---------- edificios vecinos ----------

    // Al poniente, segun la vista satelital (0.18 m por pixel desde el pin): campo seco con mezquites,
    // la avenida Campus UNAM, el Laboratorio Nacional de Visualizacion, un corral en ruinas e Ingenieria al fondo.
    private static void ZonaPoniente()
    {
        Vector3 M(float px, float py) => Lay((px - 1053) * .18f, 0, -(py - 620) * .18f);
        Piso("pasto", new[] { M(845, 425), M(860, 700), M(905, 1000), M(840, 1340), M(0, 1340), M(0, 330), M(330, 440), M(600, 560), M(700, 430), M(780, 405) }, .012f, 6);

        // Avenida de doble sentido con camellon.
        var ruta = new[] { M(0, 300), M(220, 390), M(450, 500), M(580, 580), M(640, 660), M(655, 760), M(660, 950), M(665, 1340) };
        for (int i = 0; i < ruta.Length - 1; i++)
        {
            Vector3 a = ruta[i], c = ruta[i + 1], d = (c - a).normalized, n = Vector3.Cross(Vector3.up, d);
            Vector3 da = i > 0 ? (a - ruta[i - 1]).normalized : d, dc = i < ruta.Length - 2 ? (ruta[i + 2] - c).normalized : d;
            Vector3 na = Vector3.Cross(Vector3.up, (da + d).normalized), nc = Vector3.Cross(Vector3.up, (d + dc).normalized);
            float y = .03f;
            Quad("asfalto", a - na * 8f + Vector3.up * y, a + na * 8f + Vector3.up * y, c + nc * 8f + Vector3.up * y, c - nc * 8f + Vector3.up * y, Vector3.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            Quad("pasto", a - na * .9f + Vector3.up * (y + .12f), a + na * .9f + Vector3.up * (y + .12f), c + nc * .9f + Vector3.up * (y + .12f), c - nc * .9f + Vector3.up * (y + .12f), Vector3.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            foreach (int sx in new[] { -1, 1 })
                Quad("banqueta", a + na * sx * 8f + Vector3.up * .14f, a + na * sx * 9.6f + Vector3.up * .14f, c + nc * sx * 9.6f + Vector3.up * .14f, c + nc * sx * 8f + Vector3.up * .14f, Vector3.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            for (float t = 0; t < 1; t += 4.5f / Vector3.Distance(a, c))
                foreach (int sx in new[] { -1, 1 })
                    Box("blanco_edificio", Vector3.Lerp(a, c, t) + n * sx * 4.3f + Vector3.up * (y + .005f), new Vector3(.15f, .01f, 2f), Quaternion.LookRotation(d));
        }

        // Laboratorio Nacional de Visualizacion: cuerpo de ladrillo y ala en abanico con franjas de ventanas.
        Vector3 lab = M(375, 895), fan = M(410, 955);
        Block("ladrillo", lab, new Vector3(12.6f * E, 10.5f, 23.4f * E), 0, 3);
        Block("remate", lab + Vector3.up * 10.5f, new Vector3(12.6f * E + .4f, .4f, 23.4f * E + .4f), 0, 2);
        for (int fl = 0; fl < 3; fl++)
            Box("vidrio_oscuro", lab + new Vector3(6.3f * E + .02f, 2f + fl * 3.3f, 0), new Vector3(.05f, 1.3f, 20f * E), Quaternion.identity);
        float rf = 12.6f * E;
        CurvedWall("ladrillo", fan, rf, 0, 90, 0, 9f, 20, false, 3);
        Ring("remate", fan, 0, rf + .3f, 9f, 0, 90, 20, 3);
        for (int fl = 0; fl < 2; fl++) CurvedWall("vidrio_oscuro", fan, rf + .03f, 4, 86, 2.2f + fl * 3.3f, 3.5f + fl * 3.3f, 20, false, 3);
        foreach (float a in new[] { 0f, 90f })
            Box("ladrillo", fan + Pos(a, rf / 2, 4.5f), new Vector3(.3f, 9f, rf), Quaternion.Euler(0, a, 0));

        // Corral en ruinas al otro lado de la avenida.
        Vector3 c0 = M(110, 520), c1 = M(240, 520), c2 = M(240, 630), c3 = M(110, 630);
        foreach (var (a, c) in new[] { (c0, c1), (c1, c2), (c2, c3), (c3, c0) })
            Box("concreto_sucio", (a + c) / 2 + Vector3.up * .6f, new Vector3(.3f, 1.2f, Vector3.Distance(a, c)), Quaternion.LookRotation(c - a));
        Block("blanco_edificio", M(170, 600), new Vector3(6, 3, 5), 20, 2);

        // Ingenieria Juriquilla al fondo, al suroeste, y una bodega de techo rojo junto a los anexos del sur.
        Block("rojo_edificio", M(320, 1270), new Vector3(60, 7.5f, 14), 0, 3);
        Block("blanco_edificio", M(140, 1300), new Vector3(28, 9f, 22), 0, 3);
        Block("blanco_edificio", M(470, 1240), new Vector3(16, 6f, 12), 0, 3);
        Block("rojo_edificio", M(860, 1000), new Vector3(7, 3.5f, 14), 0, 3);

        // Mezquites y huizaches del campo, a ambos lados de la avenida.
        var rng = new System.Random(12);
        for (int i = 0; i < 46; i++)
        {
            float px = i < 14 ? 690 + (float)rng.NextDouble() * 130 : (float)rng.NextDouble() * 590;
            float py = i < 14 ? 460 + (float)rng.NextDouble() * 560 : 470 + (float)rng.NextDouble() * 700;
            if (px > 320 && px < 500 && py > 790 && py < 980) continue;
            if (px > 95 && px < 255 && py > 505 && py < 645) continue;
            Vector3 q = M(px, py);
            if (Mathf.Abs(q.x - M(650, py).x) < 12) continue;
            Arbol(q);
        }
    }

    private static void EdificiosVecinos()
    {
        Group("06 | Edificios vecinos");
        EdificioLetrero();
        EdificioPaneles();
        ZonaPoniente();

        // Edificio de enfrente, cruzando el bulevar.
        // Edificios de techo rojo al poniente del estacionamiento.
        foreach (var (c, sz) in new[] { (Lay(-73, 0, 63), new Vector3(16, 6f, 11)), (Lay(-74, 0, 84), new Vector3(15, 6.5f, 15)), (Lay(-73, 0, 101), new Vector3(14, 6f, 9)) })
        {
            Block("rojo_edificio", c, sz, 0, 3);
            Block("remate", c + Vector3.up * sz.y, new Vector3(sz.x + .3f, .35f, sz.z + .3f), 0, 2);
            for (int k = 0; k < 4; k++) Window(c + new Vector3(sz.x / 2, 2.2f, -sz.z / 2 + 2 + k * (sz.z - 4) / 3), Vector3.right, 1.1f, 1.4f);
        }
        Block("blanco_edificio", Lay(-75, 0, 40), new Vector3(7, 3.2f, 9), 0, 2);
        {
            Vector3 rb = LoteNO + new Vector3(4, 0, 18);
            Block("rojo_edificio", rb, new Vector3(36, 9f, 15), 0, 3);
            for (int fl = 0; fl < 2; fl++)
                for (int k = 0; k < 8; k++) Box("vidrio_oscuro", rb + new Vector3(-14 + k * 4, 2.6f + fl * 4, -7.52f), new Vector3(2.4f, 1.6f, .05f), Quaternion.identity);
            Block("concreto", rb + new Vector3(-15, 0, 2), new Vector3(5, 15, 5), 0, 3);
            Block("concreto", rb + new Vector3(-15, 15, 2), new Vector3(5.6f, .5f, 5.6f), 0, 3);
            Block("rojo_edificio", rb + new Vector3(-26, 0, -2), new Vector3(10, 4.5f, 8), 0, 3);
        }
        // Tienda grande al otro lado del bulevar y torres de departamentos a lo lejos.
        float byaw2 = Quaternion.LookRotation(BardaDir).eulerAngles.y;
        Vector3 tienda = Barda(70f, 48f);
        Quaternion tq = Quaternion.Euler(0, byaw2, 0);
        Vector3 bd = BardaDir, bn = BardaNorte;
        Box("blanco_edificio", tienda + Vector3.up * 3.5f, new Vector3(30, 7, 70), tq, 3);
        Box("vidrio_oscuro", tienda + Vector3.up * 2f - bn * 15.02f, new Vector3(.1f, 3f, 66), tq);
        Box("remate", tienda + Vector3.up * 7.2f, new Vector3(30.4f, .4f, 70.4f), tq);
        Vector3 cartel = tienda - bn * 15.2f - bd * 26 + Vector3.up * 5.4f;
        Box("blanco_edificio", cartel, new Vector3(.4f, 3f, 12f), tq);
        Texto3D("fresko", cartel - bn * .2f, Quaternion.LookRotation(-bn), 1.4f, "letra_verde", "blanco_edificio");
        // Techo del estacionamiento superior, colgado de dos mastiles con tirantes.
        Vector3 roof = tienda + Vector3.up * 12f + bd * 8;
        Box("negro", roof, new Vector3(26, .5f, 44), tq);
        foreach (float m in new[] { -12f, 12f })
        {
            Vector3 mb = tienda + bd * (8 + m) + Vector3.up * 12.5f, mt = mb + Vector3.up * 8;
            Rod("metal", mb, mt, .25f, 8);
            foreach (float k in new[] { -20f, -10f, 10f, 20f }) Rod("metal", mt, roof + bd * (m + k * .5f) + bn * Mathf.Sign(k) * 6 + Vector3.up * .25f, .06f, 4);
        }
        for (int k = 0; k < 6; k++) Rod("metal", roof + bd * (-18 + k * 7.2f) - bn * 10 - Vector3.up * 12f, roof + bd * (-18 + k * 7.2f) - bn * 10, .2f, 6);
        foreach (var t in new[] { new Vector3(-230, 0, 260), new Vector3(-190, 0, 300), new Vector3(-150, 0, 330), new Vector3(120, 0, 320), new Vector3(170, 0, 280), new Vector3(-280, 0, 180) })
        {
            float h = Rnd(28, 62), w = Rnd(16, 24), dp = Rnd(14, 20);
            Block(Rnd() < .5f ? "blanco_edificio" : "banqueta", t, new Vector3(w, h, dp), 0, 3);
            for (float y = 3; y < h - 2; y += 3.2f) Block("vidrio_oscuro", t + Vector3.up * y, new Vector3(w + .1f, 1.1f, dp + .1f), 0, 3);
        }
    }

    // ---------- entorno ----------

    // Estacionamiento medido en la vista aerea: lote de asfalto sin cajones, autos en una sola fila contra la guarnicion sur.
    private static readonly Vector3[] Guarnicion =
    {
        Lay(-41, 0, 30.5f), Lay(-37, 0, 31.7f), Lay(-30, 0, 36.8f),
        Lay(46, 0, 60.1f), Lay(55, 0, 64.5f), Lay(72, 0, 64.5f)
    };
    private static readonly Vector3 LoteNO = Lay(-44.5f, 0, 90.8f), LoteNE = Lay(76.6f, 0, 70.4f);
    private static Vector3 BardaDir => (LoteNE - LoteNO).normalized;
    private static Vector3 BardaNorte => Vector3.Cross(BardaDir, Vector3.up).normalized;
    private static Vector3 Barda(float t, float off) => LoteNO + BardaDir * t + BardaNorte * off;

    // Punto del andador al sur de la guarnicion principal, a la altura x indicada.
    private static Vector3 Andador(float x)
    {
        Vector3 a = Guarnicion[2], b = Guarnicion[3];
        Vector3 d = (b - a).normalized;
        Vector3 p = a + d * ((x - a.x) / d.x);
        return p + Vector3.Cross(Vector3.up, d) * 1.6f;
    }

    private static void Franja(string key, float t0, float t1, float off0, float off1, float y, float uv)
    {
        Vector3 a = Barda(t0, off0), b = Barda(t0, off1), c = Barda(t1, off1), d = Barda(t1, off0);
        a.y = b.y = c.y = d.y = y;
        QuadUV(key, a, b, c, d, Vector3.up, uv);
    }

    private static void Entorno()
    {
        Group("07 | Estacionamiento, bulevar y jardines");
        Vector3 up = Vector3.up;
        var poly = new List<Vector3> { LoteNO, LoteNE };
        for (int i = Guarnicion.Length - 1; i >= 0; i--) poly.Add(Guarnicion[i]);
        Vector3 cen = Vector3.zero;
        foreach (var v in poly) cen += v;
        cen /= poly.Count;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 a = poly[i] + up * .012f, b = poly[(i + 1) % poly.Count] + up * .012f, cc = cen + up * .012f;
            Tri("asfalto|c", cc, a, b, up, new Vector2(cc.x, cc.z) / 4, new Vector2(a.x, a.z) / 4, new Vector2(b.x, b.z) / 4);
        }
        // Guarnicion, andador y la fila de autos.
        for (int i = 0; i < Guarnicion.Length - 1; i++)
        {
            Vector3 a = Guarnicion[i], b = Guarnicion[i + 1];
            Vector3 d = (b - a).normalized, south = Vector3.Cross(up, d);
            float len = (b - a).magnitude, yaw = Quaternion.LookRotation(d).eulerAngles.y;
            Block("banqueta", (a + b) * .5f + south * .1f, new Vector3(.22f, .16f, len + .25f), yaw, UvConcreto);
            QuadUV("concreto|c", a + south * .22f + up * .045f, a + south * 2.6f + up * .045f, b + south * 2.6f + up * .045f, b + south * .22f + up * .045f, up, UvConcreto);
            if (i < 2 || i > 3) continue;
            for (float s = 1.6f; s < len - 1.2f; s += 2.65f)
            {
                if (Mathf.PerlinNoise((a.x + s) * .11f, 4.2f) < .36f) continue;
                Vector3 p = a + d * (s + Rnd(-.2f, .2f)) - south * (2.55f + Rnd(-.15f, .25f));
                Auto(p, Quaternion.LookRotation(south).eulerAngles.y + Rnd(-4, 4));
            }
        }
        Block("banqueta", (LoteNO + Guarnicion[0]) * .5f, new Vector3(.22f, .16f, (LoteNO - Guarnicion[0]).magnitude), Quaternion.LookRotation(LoteNO - Guarnicion[0]).eulerAngles.y, UvConcreto);
        // Dia de clases: el lote se llena en filas encontradas, paralelas a la guarnicion.
        {
            Vector3 a = Guarnicion[2], d = (Guarnicion[3] - a).normalized, inward = -Vector3.Cross(up, d);
            float[] rows = { 9.3f, 14.3f, 21.3f, 26.3f, 33.3f, 38.3f, 45.3f, 50.3f, 57.3f };
            for (int ri = 0; ri < rows.Length; ri++)
            for (float sAlong = -25; sAlong < 140; sAlong += 2.65f)
            {
                Vector3 p = a + d * sAlong + inward * (rows[ri] + Rnd(-.2f, .2f));
                if (!DentroDe(p, poly, 2.8f) || Rnd() > .5f) continue;
                Auto(p, Quaternion.LookRotation(ri % 2 == 0 ? -inward : inward).eulerAngles.y + Rnd(-4, 4));
            }
            // Cajones pintados: lineas blancas entre autos y amarilla al final de cada fila, como en las fotos.
            Quaternion lineRot = Quaternion.LookRotation(inward);
            for (int ri = 0; ri < rows.Length; ri++)
            {
                float rr = rows[ri] + (ri % 2 == 0 ? .3f : -.3f);
                for (float sAlong = -25; sAlong < 140; sAlong += 2.65f)
                {
                    Vector3 p = a + d * (sAlong - 1.32f) + inward * rr;
                    if (!DentroDe(p, poly, 1.5f)) continue;
                    bool fin = !DentroDe(p + d * 2.65f, poly, 1.5f) || !DentroDe(p - d * 2.65f, poly, 1.5f);
                    Box(fin ? "linea_amarilla" : "linea", p + up * .016f, new Vector3(.12f, .01f, 4.6f), lineRot);
                }
            }
            // Flechas de circulacion en los pasillos.
            foreach (float rr in new[] { 17.8f, 29.8f, 41.8f, 53.8f })
                for (float sAlong = 5; sAlong < 130; sAlong += 18)
                {
                    Vector3 p = a + d * sAlong + inward * rr + up * .016f;
                    if (!DentroDe(p, poly, 3f)) continue;
                    Quaternion q = Quaternion.LookRotation(((int)(sAlong / 18) % 2 == 0 ? d : -d));
                    Box("linea", p, new Vector3(.18f, .01f, 1.6f), q);
                    Box("linea", p + q * new Vector3(.25f, 0, .55f), new Vector3(.15f, .01f, .7f), q * Quaternion.Euler(0, -40, 0));
                    Box("linea", p + q * new Vector3(-.25f, 0, .55f), new Vector3(.15f, .01f, .7f), q * Quaternion.Euler(0, 40, 0));
                }
            // Lugares para discapacitados junto a cada sendero, con rampa de barandal cafe y botes de colores.
            foreach (float sx in new[] { -9f * E, 9f * E })
            {
                Vector3 ramp = Andador(sx) - Vector3.Cross(up, d) * 2.2f;
                Quaternion rq = Quaternion.LookRotation(inward);
                Box("concreto|c", ramp + up * .06f + inward * .9f, new Vector3(2.2f, .1f, 1.8f), rq);
                foreach (int sd in new[] { -1, 1 })
                {
                    Vector3 r0 = ramp + d * sd * 1.15f + up * .9f, r1 = r0 + inward * 1.8f - up * .1f;
                    Rod("barandal", r0, r1, .03f, 5);
                    Rod("barandal", r0 - up * .9f, r0, .03f, 5);
                    Rod("barandal", r1 - up * .8f, r1, .03f, 5);
                }
                foreach (int sd in new[] { -1, 1 })
                {
                    Vector3 c = ramp + inward * 3.4f + d * sd * 3.2f + up * .015f;
                    Box("azul_disc", c, new Vector3(3.4f, .01f, 4.8f), rq);
                    Box("linea", c + up * .002f, new Vector3(1.3f, .01f, 1.3f), rq * Quaternion.Euler(0, 45, 0));
                    Box("azul_disc", c + up * .004f, new Vector3(.9f, .01f, .9f), rq * Quaternion.Euler(0, 45, 0));
                    for (int k = 0; k < 6; k++) Box("linea", c + d * -sd * 1.95f + inward * (-1.9f + k * .75f) + up * .003f, new Vector3(.7f, .01f, .22f), rq);
                }
                string[] cols = { "bote_rojo", "bote_amarillo", "bote_amarillo", "bote_azul", "bote_azul" };
                for (int k = 0; k < cols.Length; k++)
                    Cylinder(cols[k], ramp - Vector3.Cross(up, d) * 1.2f + d * (k - 2) * .32f, .14f, .14f, .6f, 10, Quaternion.identity, true);
            }
        }
        Auto(Barda(12, -6), 100);
        Auto(Barda(70, -9), 260);
        Auto(Barda(74, -12), 255);

        // Barda norte, caseta de acceso y parada de camion sobre el bulevar.
        float L = (LoteNE - LoteNO).magnitude, byaw = Quaternion.LookRotation(BardaDir).eulerAngles.y;
        // Barda de block gris que alterna con tramos de reja blanca, como en las fotos.
        for (float t = -15; t < L + 15; t += 10)
        {
            Block("block", Barda(t + 2.5f, .2f), new Vector3(.25f, 2f, 5f), byaw, 1.2f);
            Block("blanco_edificio", Barda(t + 2.5f, .2f) + up * 2f, new Vector3(.3f, .08f, 5.05f), byaw, 1);
            Vector3 f0 = Barda(t + 5, .2f);
            Block("block", f0, new Vector3(.25f, .4f, 5f) + Vector3.zero, byaw, 1.2f);
            for (float k = .1f; k < 5; k += .14f) Rod("blanco_edificio", Barda(t + 5 + k, .2f) + up * .4f, Barda(t + 5 + k, .2f) + up * 1.85f, .02f, 4);
            foreach (float h in new[] { .55f, 1.7f }) Rod("blanco_edificio", Barda(t + 5, .2f) + up * h, Barda(t + 10, .2f) + up * h, .025f, 4);
        }
        Franja("banqueta", -60, L + 80, .35f, 3.2f, .1f, UvConcreto);
        Franja("asfalto", -220, L + 220, 3.2f, 20.5f, .01f, 4);
        Franja("banqueta", -220, L + 220, 11.2f, 12.4f, .18f, UvConcreto);
        Franja("banqueta", -220, L + 220, 20.5f, 23.5f, .1f, UvConcreto);
        for (float t = -200; t < L + 200; t += 6)
        {
            Block("linea", Barda(t, 7.2f) + up * .012f, new Vector3(.15f, .01f, 3f), byaw);
            Block("linea", Barda(t, 16.3f) + up * .012f, new Vector3(.15f, .01f, 3f), byaw);
        }
        Vector3 bus = Barda(53f * E, 1.8f);
        Box("metal", bus + up * 2.6f, new Vector3(1.9f, .08f, 4.4f), Quaternion.Euler(0, byaw, 0));
        Box("vidrio_oscuro", bus + up * 1.35f + BardaNorte * .85f, new Vector3(.04f, 2.2f, 4.2f), Quaternion.Euler(0, byaw, 0));
        foreach (int sd in new[] { -1, 1 }) Rod("metal", bus + BardaDir * sd * 2.1f + BardaNorte * .85f, bus + BardaDir * sd * 2.1f + BardaNorte * .85f + up * 2.6f, .04f, 6);
        Box("mesa", bus + up * .45f + BardaNorte * .55f, new Vector3(.4f, .05f, 3.2f), Quaternion.Euler(0, byaw, 0));
        Vector3 booth = Barda(16f * E, -3f);
        Block("blanco_edificio", booth, new Vector3(2.6f, 2.5f, 2.6f), byaw, 1);
        Block("vidrio_oscuro", booth + up * 1.15f, new Vector3(2.66f, .85f, 2.66f), byaw, 1);
        Block("remate", booth + up * 2.5f, new Vector3(3.4f, .16f, 3.4f), byaw, 1);
        Rod("bote_rojo", booth + up * 1f + BardaDir * 1.5f, booth + up * 1f + BardaDir * 6f, .04f, 6);

        // Av. de las Ciencias al oriente, con su glorieta y el estacionamiento de ese lado.
        Vector3 c0 = Lay(93, 0, 74), c1 = Lay(116, 0, -45);
        Vector3 ad = (c1 - c0).normalized, an = Vector3.Cross(Vector3.up, ad);
        foreach (var (o0, o1, key, y) in new[] { (-8f, 8f, "asfalto", .011f), (-.6f, .6f, "banqueta", .18f), (8f, 10.5f, "banqueta", .1f), (-10.5f, -8f, "banqueta", .1f) })
            QuadUV(key, c0 + an * o0 - ad * 20 + up * y, c0 + an * o1 - ad * 20 + up * y, c1 + an * o1 + ad * 30 + up * y, c1 + an * o0 + ad * 30 + up * y, up, 4);
        Vector3 glorieta = Lay(97, 0, 87);
        Ring("asfalto", glorieta, 9f, 17f, .013f, 0, 360, 48, 4);
        Ring("banqueta", glorieta, 8.6f, 9f, .16f, 0, 360, 48, 1);
        Ring("pasto", glorieta, 0, 8.6f, .1f, 0, 360, 48, 3);
        Arbol(glorieta + new Vector3(2, .1f, -1));
        Quad("asfalto|c", Lay(74, .012f, -36), Lay(74, .012f, 10), Lay(92, .012f, 14), Lay(96, .012f, -36), up,
            new Vector2(18.5f, -9), new Vector2(18.5f, 2.5f), new Vector2(23, 3.5f), new Vector2(24, -9));
        foreach (float x in new[] { 78f, 87.5f })
            for (float z = -33; z < 8; z += 2.7f)
                if (Rnd() < .55f) Auto(Lay(x + (x > 80 ? 2.5f : 0), 0, z), (x > 80 ? 270 : 90) + Rnd(-4, 4));

        // Franja de tierra y cerca al poniente del lote.
        Quad("tierra", Lay(-63, .004f, 26), Lay(-63, .004f, 130), Lay(-44.3f, .004f, 130), Lay(-44.3f, .004f, 26), up,
            new Vector2(-31, 13), new Vector2(-31, 52), new Vector2(-22, 52), new Vector2(-22, 13));
        for (float z = 28 * E; z <= 102 * E; z += 3f) Rod("metal", new Vector3(-62.5f * E, 0, z), new Vector3(-62.5f * E, 1.6f, z), .03f, 5);
        foreach (float h in new[] { .8f, 1.5f }) Rod("metal", Lay(-62.5f, h, 28), Lay(-62.5f, h, 102), .02f, 4);

        // Senderos de adoquin entre el pasto.
        Sendero(Pos(348, PlazaR + .2f), Andador(-9f * E), 3f);
        Sendero(Pos(18, PlazaR + .2f), Andador(9f * E), 3f);
        Sendero(Pos(292, PlazaR + .2f), Lay(-38.5f, 0, 29.6f), 2.6f);

        // Jardinera del pasto norte: orilla de piedras blancas, rocas, agaves y un arbolito, como en las fotos.
        Vector3 g = new Vector3(.5f, 0, 42f);
        float jx = 3.9f, jz = 2.6f;
        for (int i = 0; i < 70; i++)
        {
            float ang = i * Mathf.PI * 2 / 70 + Rnd(-.02f, .02f);
            Vector3 p = g + new Vector3(Mathf.Cos(ang) * jx, .05f, Mathf.Sin(ang) * jz);
            Ellipsoid("piedra_blanca", p, new Vector3(Rnd(.13f, .22f), Rnd(.06f, .1f), Rnd(.1f, .16f)), Quaternion.Euler(0, Rnd(0, 360), 0), 3, 6);
        }
        for (int i = 0; i < 16; i++)
        {
            Vector3 p = g + new Vector3(Rnd(-1.9f, 1.4f), .05f, Rnd(-.7f, .7f));
            Ellipsoid("roca", p, new Vector3(Rnd(.18f, .38f), Rnd(.12f, .25f), Rnd(.16f, .32f)), Quaternion.Euler(Rnd(-15, 15), Rnd(0, 360), 0), 4, 7);
        }
        Agave(g + new Vector3(-.6f, 0, .9f), .62f);
        Agave(g + new Vector3(1.3f, 0, .7f), .5f);
        Agave(g + new Vector3(.2f, 0, -1.2f), .45f);
        for (int i = 0; i < 5; i++) Zacate(g + new Vector3(Rnd(-2.5f, 2.5f), 0, Rnd(-1.2f, 1.2f)), Rnd(.25f, .35f), 18);
        Arbol(g + new Vector3(-2.7f, 0, -.2f));
        Vector3 sp = g + new Vector3(4.6f, 0, -2.2f);
        Rod("metal", sp, sp + up * 2.6f, .03f, 6);
        Box("verde_senal", sp + up * 2.4f, new Vector3(.6f, .3f, .03f), Quaternion.Euler(0, 160, 0));
        Box("blanco_edificio", sp + up * 2.4f + Quaternion.Euler(0, 160, 0) * Vector3.back * .02f, new Vector3(.4f, .06f, .01f), Quaternion.Euler(0, 160, 0));

        // Postes de luz.
        foreach (var p in new[] { Andador(-25 * E), Andador(5 * E), Andador(35 * E), Pos(318, PlazaR + 1), Pos(42, PlazaR + 1), Barda(30, -1.2f), Barda(85, -1.2f) })
        {
            Rod("metal", p, p + up * 9f, .09f, 8);
            Block("metal", p + up * 9f, new Vector3(.5f, .18f, .9f));
            Block("foco", p + up * 8.98f, new Vector3(.4f, .03f, .7f));
        }
    }

    private static bool DentroDe(Vector3 p, List<Vector3> poly, float margin)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector3 a = poly[i], b = poly[j];
            if ((a.z > p.z) != (b.z > p.z) && p.x < (b.x - a.x) * (p.z - a.z) / (b.z - a.z) + a.x) inside = !inside;
            Vector3 ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            if ((a + ab * t - p).magnitude < margin) return false;
        }
        return inside;
    }

    // Mata de zacate seco: hojas delgadas que se abren desde el centro.
    private static void Zacate(Vector3 p, float r, int blades)
    {
        for (int i = 0; i < blades; i++)
        {
            float az = Rnd(0, 360);
            Vector3 dir = Pos(az, 1);
            Vector3 b = p + dir * Rnd(0, r * .3f) + Vector3.up * .05f;
            float h = Rnd(.45f, 1f) * (r / .5f);
            Vector3 tip = b + dir * h * Rnd(.25f, .55f) + Vector3.up * h;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * .025f;
            string mat = Rnd() < .25f ? "follaje" : "zacate";
            Tri(mat, b - side, b + side, tip, Vector3.Cross(side, tip - b));
            Tri(mat, b - side, b + side, tip, -Vector3.Cross(side, tip - b));
        }
    }

    // Arriate eliptico con guarnicion blanca, zacate y agaves.
    private static void Arriate(Vector3 c, float rx, float rz, float yaw)
    {
        Quaternion q = Quaternion.Euler(0, yaw, 0);
        int n = 40;
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
            Vector3 p0 = c + q * new Vector3(Mathf.Cos(a0) * rx, 0, Mathf.Sin(a0) * rz), p1 = c + q * new Vector3(Mathf.Cos(a1) * rx, 0, Mathf.Sin(a1) * rz);
            Vector3 d = p1 - p0;
            Block("linea", (p0 + p1) * .5f, new Vector3(.22f, .14f, d.magnitude + .05f), Quaternion.LookRotation(d).eulerAngles.y);
            Tri("tierra", c + Vector3.up * .04f, p0 + Vector3.up * .04f, p1 + Vector3.up * .04f, Vector3.up);
        }
        for (int i = 0; i < 9; i++)
        {
            float a = Rnd(0, Mathf.PI * 2), k = Mathf.Sqrt(Rnd()) * .75f;
            Vector3 p = c + q * new Vector3(Mathf.Cos(a) * rx * k, 0, Mathf.Sin(a) * rz * k);
            if (i % 3 == 0) Agave(p, Rnd(.5f, .8f));
            else Zacate(p, Rnd(.4f, .6f), 28);
        }
    }

    private static void Agave(Vector3 p, float s)
    {
        for (int i = 0; i < 14; i++)
        {
            float az = i * 360f / 14 + Rnd(-8, 8);
            Vector3 dir = Pos(az, 1);
            float lift = Rnd(.5f, 1.2f);
            Vector3 tip = p + (dir + Vector3.up * lift).normalized * s * Rnd(.8f, 1.1f);
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * .06f * s / .6f;
            Vector3 b = p + Vector3.up * .05f;
            Tri("agave", b - side, b + side, tip, Vector3.up);
            Tri("agave", b - side, b + side, tip, Vector3.down);
        }
    }

    private static void Sendero(Vector3 a, Vector3 b, float w)
    {
        a.y = b.y = .02f;
        Vector3 d = (b - a).normalized;
        Vector3 l = Vector3.Cross(Vector3.up, d).normalized;
        QuadUV("ladrillo|c", a - l * w / 2, a + l * w / 2, b + l * w / 2, b - l * w / 2, Vector3.up, UvLadrillo);
        float len = (b - a).magnitude;
        float yaw = Quaternion.LookRotation(d).eulerAngles.y;
        foreach (int s in new[] { -1, 1 })
            Block("banqueta", (a + b) * .5f + l * s * (w / 2 + .08f) - Vector3.up * .02f, new Vector3(.16f, .08f, len), yaw, UvConcreto);
    }

    private static GameObject[] modelosAuto;
    private static readonly string[] coloresAuto = { "blanco", "blanco", "blanco", "plata", "plata", "plata", "gris", "gris", "negro", "negro", "rojo", "azul" };

    private static void Auto(Vector3 p, float yaw)
    {
        if (modelosAuto == null)
            modelosAuto = new[] { "sedan", "sedan", "sedan", "suv", "hatchback-sports", "sedan-sports", "suv-luxury", "van" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MemoriaViva/Modelos/Autos/" + n + ".fbx")).ToArray();
        if (modelosAuto[0] != null && Mats.ContainsKey("auto_blanco"))
        {
            var src = modelosAuto[random.Next(modelosAuto.Length)];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.SetParent(currentGroup, true);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            // El kit tiene proporciones de juguete: se estira a un auto real de 1.8 x 1.45 x 4.4 m.
            go.transform.localScale = new Vector3(1.2f, 1.1f, 1.68f);
            var mat = Mats["auto_" + coloresAuto[random.Next(coloresAuto.Length)]];
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return;
        }
        AutoProcedural(p, yaw);
    }

    private static void AutoProcedural(Vector3 p, float yaw)
    {
        // Como en las fotos: casi todos blancos, plata, grises o negros.
        float pick = Rnd();
        string body = "auto" + (pick < .3f ? 1 : pick < .52f ? 3 : pick < .72f ? 2 : pick < .8f ? 6 : pick < .88f ? 0 : pick < .95f ? 5 : 4);
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right;
        bool suv = Rnd() < .3f;
        float hb = suv ? .95f : .72f, len = suv ? 4.6f : 4.3f;
        Box(body, p + Vector3.up * (.36f + hb / 2), new Vector3(1.8f, hb, len), r);
        float cy = .36f + hb + .27f;
        Box("vidrio_oscuro", p + Vector3.up * cy - f * (suv ? .45f : .25f), new Vector3(1.6f, .55f, suv ? 2.9f : 2.2f), r);
        Box(body, p + Vector3.up * (cy + .29f) - f * (suv ? .45f : .25f), new Vector3(1.55f, .05f, suv ? 2.8f : 2f), r);
        Box("negro", p + Vector3.up * .42f + f * (len / 2), new Vector3(1.7f, .16f, .06f), r);
        foreach (int sx in new[] { -1, 1 }) Box("foco", p + Vector3.up * (.36f + hb * .7f) + f * (len / 2 + .01f) + l * sx * .65f, new Vector3(.3f, .1f, .03f), r);
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Cylinder("negro", p + l * sx * .78f + f * sz * 1.35f + Vector3.up * .33f - l * sx * .12f, .33f, .33f, .24f, 10,
                r * Quaternion.Euler(0, 0, sx > 0 ? -90 : 90), true, true);
    }

    // ---------- mobiliario ----------

    private static void Mobiliario()
    {
        Group("08 | Mesas, sombrillas y botes");
        float[] terrace = { 155f, 167f, 197f, 209f, 221f };
        foreach (float a in terrace)
        {
            Vector3 p = Pos(a, 19.1f * E, TerraceY + .03f);
            Block("concreto", p, new Vector3(2.8f, .04f, 2.2f), a, UvConcreto);
            MesaSombrilla(p, a + 90);
        }
        MesaSombrilla(Pos(296, 15.6f * E, .015f), 296 + 90);
        // Mesas frente a la vela del edificio del letrero, entre las palmas.
        foreach (float a in new[] { 47.5f, 58.5f, 69.5f })
            MesaSombrilla(Pos(a, 27.6f, .03f), a + 90);

        string[] bins = { "bote_azul", "bote_amarillo", "bote_rojo", "bote_gris" };
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = Pos(176 + i * 1.6f, 20.3f * E, TerraceY + .03f);
            Cylinder(bins[i], p, .28f, .3f, .85f, 12, Quaternion.identity, false);
            Cylinder(bins[i], p + Vector3.up * .85f, .32f, .2f, .1f, 12, Quaternion.identity, true);
        }

        // Letrero de los Troyanos: muro de concreto, panel blanco y casco espartano extruido, como en las fotos.
        {
            float a = 298f;
            Vector3 n = Radial(a);
            Quaternion rq = Quaternion.LookRotation(n);
            Vector3 right = rq * Vector3.right, back = n, up = Vector3.up;
            Vector3 c = Pos(a, PlazaR + .55f);
            Box("concreto_sucio|c", c + up * .45f + right * 1.2f, new Vector3(9.5f, .9f, .55f), rq, 2);
            Vector3 panel = c + up * 1.48f - back * .05f;
            Box("blanco_edificio", panel, new Vector3(5.1f, 1.16f, .22f), rq);
            // Letras en relieve trazadas de la foto; los huecos de R, O y A llevan el blanco del panel encima.
            Vector3 face = panel - back * .11f;
            foreach (var letra in LetrasTroyanos.Contornos) Extrude("azul_marino", letra, face, right, up, .025f);
            foreach (var hueco in LetrasTroyanos.Huecos) Extrude("blanco_edificio", hueco, face - back * .001f, right, up, .025f);
            foreach (int k in new[] { 0, 1 })
            {
                Vector3 can = c + up * .9f - right * (2.85f + k * .4f) - back * .05f;
                Cylinder("negro", can, .19f, .19f, .48f - k * .05f, 12, Quaternion.identity, true);
            }
            foreach (float x in new[] { -1.2f, .9f, 2.6f })
            {
                Vector3 fl = c + up * .62f + right * x - back * .3f;
                Box("negro", fl, new Vector3(.3f, .24f, .06f), rq);
                Box("foco", fl - back * .035f, new Vector3(.22f, .16f, .01f), rq);
            }
            // Casco: silueta azul marino, placas plateadas y letras FIF caladas, trazadas de la foto del letrero.
            const float hc = 3.1f;
            Vector3 o = panel + up * .6f - right * .85f + back * .02f;
            Vector2[] Sc(Vector2[] pts) { var r = new Vector2[pts.Length]; for (int k = 0; k < pts.Length; k++) r[k] = pts[k] * hc; return r; }
            // La extrusion crece hacia quien mira desde la plaza; cada capa empieza donde termina la anterior.
            Extrude("azul_marino", Sc(CascoTroyanos.Contorno), o, right, up, .22f);
            foreach (var borde in CascoTroyanos.Bordes) Extrude("arena", Sc(borde), o - back * .22f, right, up, .03f);
            foreach (var placa in CascoTroyanos.Placas) Extrude("plata", Sc(placa), o - back * .22f, right, up, .035f);
            foreach (var letra in CascoTroyanos.Letras) Extrude("azul_marino", Sc(letra), o - back * .255f, right, up, .02f);
            foreach (var pt in CascoTroyanos.Postes)
                Rod("blanco_edificio", o + right * pt.x * hc - up * .6f + back * .1f, o + right * pt.x * hc + up * (pt.y * hc + .05f) + back * .1f, .045f, 8);
            Rod("negro", o + right * .25f * hc + up * .45f * hc + back * .2f, c - right * 1.9f + back * 1.1f, .045f, 6);
        }
        foreach (float a in new[] { 300f, 312, 48, 58 })
        {
            Vector3 p = Pos(a, 17.3f * E, .015f);
            Cylinder("bote_gris", p, .27f, .27f, .95f, 14, Quaternion.identity, true);
            Cylinder("negro", p + Vector3.up * .95f, .29f, .24f, .08f, 14, Quaternion.identity, true);
        }
    }

    private static void MesaSombrilla(Vector3 p, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right;
        Box("blanco_edificio", p + Vector3.up * .76f, new Vector3(1.8f, .05f, .75f), r);
        foreach (int s in new[] { -1, 1 })
        {
            Box("blanco_edificio", p + Vector3.up * .45f + f * s * .62f, new Vector3(1.8f, .04f, .28f), r);
            Rod("metal", p + l * .7f + f * s * .7f, p + l * .7f + Vector3.up * .75f, .025f, 5);
            Rod("metal", p - l * .7f + f * s * .7f, p - l * .7f + Vector3.up * .75f, .025f, 5);
        }
        Rod("metal", p, p + Vector3.up * 2.55f, .035f, 6);
        Vector3 apex = p + Vector3.up * 2.62f;
        float h = 1.35f;
        Vector3[] c = { p + Vector3.up * 2.3f + (f + l) * h, p + Vector3.up * 2.3f + (f - l) * h, p + Vector3.up * 2.3f + (-f - l) * h, p + Vector3.up * 2.3f + (-f + l) * h };
        for (int i = 0; i < 4; i++)
        {
            Tri("lona", apex, c[i], c[(i + 1) % 4], Vector3.up);
            Tri("lona", apex, c[i], c[(i + 1) % 4], Vector3.down);
        }
    }

    // ---------- vegetacion ----------

    private static void Vegetacion()
    {
        ZonaOriente();
        Gente();
        Reflectores();
        Detalles();
        Group("09 | Palmas y arboles");
        foreach (float a in new[] { 150f, 161, 173, 191, 203, 215, 228 })
            Palma(Pos(a + Rnd(-1, 1), 18.0f * E, TerraceY + .02f), Rnd(4.5f, 6.5f));
        foreach (var p in new[] { Pos(42, 27.2f), Pos(53, 27.4f), Pos(64, 27.4f), Pos(75, 27.2f), Pos(282, 22f * E), Pos(297, 21.5f * E), Pos(312, 22f * E), Pos(94, 22f * E), Pos(120, 16.6f * E, TerraceY), Pos(244, 16.6f * E, TerraceY) })
            Palma(p, Rnd(5f, 7.5f));
        foreach (var p in new[] { Lay(-26, 0, 13), Lay(-21, 0, 26), Lay(-36, 0, 4), Lay(-12, 0, 38) })
            Arbol(p);
    }

    private static void Palma(Vector3 b, float height)
    {
        float leanAz = Rnd(0, 360);
        Vector3 leanDir = Pos(leanAz, 1);
        float lean = Rnd(.05f, .16f);
        Vector3 Trunk(float t) => b + Vector3.up * height * t + leanDir * (height * lean * t * t);
        int segs = 8;
        for (int i = 0; i < segs; i++)
        {
            float t0 = (float)i / segs, t1 = (float)(i + 1) / segs;
            Vector3 a = Trunk(t0), c = Trunk(t1);
            Cylinder("tronco_palma", a, .15f - .04f * t0, .15f - .04f * t1, (c - a).magnitude, 8, Quaternion.FromToRotation(Vector3.up, (c - a).normalized), false);
            Ring("ojo_buey", a, .15f - .04f * t0, .155f - .04f * t0, 0, 0, 360, 8, 1);
        }
        Vector3 crown = Trunk(1);
        Ellipsoid("tronco_palma", crown, new Vector3(.22f, .34f, .22f), Quaternion.identity, 3, 8);
        // Frondas de palma reina: tiras curvas con textura de foliolos que cuelgan.
        int fronds = 18;
        for (int k = 0; k < fronds; k++)
        {
            float az = k * 360f / fronds + Rnd(-10, 10);
            float len = Rnd(2.6f, 3.4f);
            float pitch = Rnd(30, 70);
            bool dry = k % 7 == 0;
            if (dry) pitch = Rnd(-50, -25);
            string leaf = dry ? "fronda_seca" : "fronda";
            Vector3 p = crown + Vector3.up * .12f;
            int n = 8;
            for (int j = 0; j < n; j++)
            {
                float a0 = (pitch - j * (pitch + 95) / n) * Mathf.Deg2Rad, a1 = (pitch - (j + 1) * (pitch + 95) / n) * Mathf.Deg2Rad;
                Vector3 d0 = (Pos(az, Mathf.Cos(a0)) + Vector3.up * Mathf.Sin(a0)).normalized, d1 = (Pos(az, Mathf.Cos(a1)) + Vector3.up * Mathf.Sin(a1)).normalized;
                Vector3 q = p + d0 * (len / n);
                Vector3 side = Vector3.Cross(Pos(az, 1), Vector3.up).normalized;
                float w0 = .95f * Mathf.Sin(Mathf.PI * Mathf.Max(.12f, (float)j / n)), w1 = .95f * Mathf.Sin(Mathf.PI * Mathf.Min(.97f, (float)(j + 1) / n));
                Vector3 droop = Vector3.down * .4f;
                Vector3 l0 = (side + droop).normalized * w0, r0 = (-side + droop).normalized * w0, l1 = (side + droop).normalized * w1, r1 = (-side + droop).normalized * w1;
                float v0 = (float)j / n, v1 = (float)(j + 1) / n;
                Quad(leaf, p + l0, q + l1, q, p, Vector3.up, new Vector2(0, v0), new Vector2(0, v1), new Vector2(.5f, v1), new Vector2(.5f, v0), true);
                Quad(leaf, p, q, q + r1, p + r0, Vector3.up, new Vector2(.5f, v0), new Vector2(.5f, v1), new Vector2(1, v1), new Vector2(1, v0), true);
                p = q;
            }
        }
    }

    private static void Arbol(Vector3 b)
    {
        float h = Rnd(3.5f, 5f);
        Rod("tronco_palma", b, b + Vector3.up * h * .62f, .15f, 7);
        Vector3 top = b + Vector3.up * h * .62f;
        for (int i = 0; i < 4; i++) Rod("tronco_palma", top, top + Pos(i * 90 + Rnd(-20, 20), Rnd(.8f, 1.3f)) + Vector3.up * Rnd(.5f, 1f), .06f, 5);
        Vector3 c = b + Vector3.up * h * .82f;
        Ellipsoid("follaje", c, new Vector3(1.2f, .85f, 1.2f), Quaternion.identity, 4, 8);
        // Copa de racimos de hojas en tarjetas, para que el contorno no sea una bola lisa.
        for (int i = 0; i < 46; i++)
        {
            Vector3 o = Vector3.Scale(Random3(1).normalized, new Vector3(1.55f, 1.05f, 1.55f)) * Rnd(.75f, 1f);
            Vector3 p = c + o;
            Quaternion q = Quaternion.LookRotation(o.normalized + Random3(.5f));
            Vector3 x = q * Vector3.right * .55f, y = q * Vector3.up * .55f;
            Quad("hojas", p - x - y, p - x + y, p + x + y, p + x - y, o, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, true);
        }
    }

    // Curva suave (Hermite) por claves (x, valor), para perfiles de cuerpo, cabeza y ropa.
    private static float Curva(float x, float[] xs, float[] ys)
    {
        int n = xs.Length;
        if (x <= xs[0]) return ys[0];
        if (x >= xs[n - 1]) return ys[n - 1];
        int i = 0;
        while (x > xs[i + 1]) i++;
        float h = xs[i + 1] - xs[i], s = (x - xs[i]) / h;
        float M(int k) => k == 0 ? (ys[1] - ys[0]) / (xs[1] - xs[0]) : k == n - 1 ? (ys[n - 1] - ys[n - 2]) / (xs[n - 1] - xs[n - 2]) : (ys[k + 1] - ys[k - 1]) / (xs[k + 1] - xs[k - 1]);
        float s2 = s * s, s3 = s2 * s;
        return (2 * s3 - 3 * s2 + 1) * ys[i] + (s3 - 2 * s2 + s) * h * M(i) + (-2 * s3 + 3 * s2) * ys[i + 1] + (s3 - s2) * h * M(i + 1);
    }

    private static float Perfil(float t, params float[] k)
    {
        var xs = new float[k.Length / 2]; var ys = new float[k.Length / 2];
        for (int i = 0; i < xs.Length; i++) { xs[i] = k[i * 2]; ys[i] = k[i * 2 + 1]; }
        return Curva(t, xs, ys);
    }

    // Punto de una seccion superelíptica: ancho a los lados, fondo distinto al frente y atras.
    private static Vector3 Seccion(Vector3 c, Vector3 ex, Vector3 ey, float ax, float ayF, float ayB, float u, float n)
    {
        float a = u * Mathf.PI * 2, co = Mathf.Cos(a), si = Mathf.Sin(a), e = 2f / n;
        float x = Mathf.Sign(co) * Mathf.Pow(Mathf.Abs(co), e), y = Mathf.Sign(si) * Mathf.Pow(Mathf.Abs(si), e);
        return c + ex * x * ax + ey * y * (y > 0 ? ayF : ayB);
    }

    // Extremidad de una pieza por una curva suave, con grosor variable y puntas redondeadas.
    private static void Miembro(string key, Vector3[] ctrl, System.Func<float, float> radio, Vector3 frente, float plano = 1, int nu = 12, int nv = 16, bool tapaIni = true, bool tapaFin = true)
    {
        Vector3 Pt(float t)
        {
            t = Mathf.Clamp01(t);
            float x = t * (ctrl.Length - 1);
            int i = Mathf.Min((int)x, ctrl.Length - 2);
            float s = x - i;
            Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Length - 1)];
            return .5f * (2 * p1 + (p2 - p0) * s + (2 * p0 - 5 * p1 + 4 * p2 - p3) * s * s + (3 * p1 - p0 - 3 * p2 + p3) * s * s * s);
        }
        Vector3 Tan(float t) => (Pt(t + .01f) - Pt(t - .01f)).normalized;
        int ti = tapaIni ? 3 : 0, tf = tapaFin ? 3 : 0, total = nv + ti + tf;
        GeoKit.Superficie(key, (u, v) =>
        {
            float fila = v * total, t, r, avance = 0;
            if (fila < ti) { float ang = (1 - fila / ti) * Mathf.PI / 2; t = 0; r = radio(0) * Mathf.Cos(ang); avance = -radio(0) * Mathf.Sin(ang); }
            else if (fila > total - tf) { float ang = (fila - (total - tf)) / tf * Mathf.PI / 2; t = 1; r = radio(1) * Mathf.Cos(ang); avance = radio(1) * Mathf.Sin(ang); }
            else { t = (fila - ti) / nv; r = radio(t); }
            Vector3 T = Tan(t), ey = frente - T * Vector3.Dot(frente, T);
            if (ey.sqrMagnitude < 1e-4f) ey = Vector3.up - T * T.y;
            ey.Normalize();
            Vector3 ex = Vector3.Cross(ey, T).normalized;
            return Seccion(Pt(t) + T * avance, ex, ey, r, r * plano, r * plano, u, 2);
        }, nu, total);
    }

    // Cuerpo: claves por altura sobre la cadera (ancho, frente, espalda y corrimiento al frente).
    private static readonly float[] CuerpoY = { -.13f, -.08f, -.03f, .03f, .1f, .18f, .26f, .33f, .4f, .445f, .48f, .51f, .53f };
    private static readonly float[] CuerpoW = { .07f, .15f, .165f, .158f, .148f, .14f, .148f, .16f, .175f, .185f, .14f, .07f, .05f };
    private static readonly float[] CuerpoF = { .05f, .085f, .095f, .095f, .093f, .09f, .096f, .1f, .098f, .085f, .07f, .05f, .045f };
    private static readonly float[] CuerpoB = { .05f, .1f, .115f, .105f, .095f, .088f, .09f, .095f, .095f, .085f, .072f, .055f, .045f };
    private static readonly float[] CuerpoZ = { 0, -.005f, -.01f, 0, 0, .005f, .005f, .005f, 0, -.01f, -.015f, -.012f, -.01f };

    private static float[] CuerpoAncho(bool mujer)
    {
        var w = (float[])CuerpoW.Clone();
        if (mujer) { w[1] += .004f; w[2] += .008f; w[3] += .006f; w[5] -= .016f; w[6] -= .012f; w[7] -= .014f; w[8] -= .02f; w[9] -= .022f; w[10] -= .012f; }
        return w;
    }

    // Tramo del tronco entre dos alturas, con holgura para la ropa encima.
    private static void Tronco(string key, Vector3 cad, Quaternion r, bool mujer, float y0, float y1, float holgura, float vuelo = 0)
    {
        Vector3 f = r * Vector3.forward, l = r * Vector3.right;
        var w = CuerpoAncho(mujer);
        var fr = (float[])CuerpoF.Clone();
        if (mujer) { fr[6] += .008f; fr[7] += .016f; fr[8] += .006f; }
        GeoKit.Superficie(key, (u, v) =>
        {
            float y = Mathf.Lerp(y0, y1, v);
            // El vuelo abre el dobladillo de abajo, como playera suelta.
            float extra = holgura + vuelo * Mathf.Clamp01(1 - v * 4);
            return Seccion(cad + Vector3.up * y + f * Curva(y, CuerpoY, CuerpoZ), l, f, Curva(y, CuerpoY, w) + extra, Curva(y, CuerpoY, fr) + extra, Curva(y, CuerpoY, CuerpoB) + extra, u, 2.4f);
        }, 22, Mathf.Max(4, Mathf.RoundToInt((y1 - y0) * 40)));
    }

    // Frente del pecho a cierta altura, para pegar botones y estampados sobre la ropa.
    private static Vector3 PechoFrente(Vector3 cad, Quaternion r, bool mujer, float y, float holgura)
    {
        var fr = (float[])CuerpoF.Clone();
        if (mujer) { fr[6] += .008f; fr[7] += .016f; fr[8] += .006f; }
        return cad + Vector3.up * y + r * Vector3.forward * (Curva(y, CuerpoY, CuerpoZ) + Curva(y, CuerpoY, fr) + holgura);
    }

    // Cabeza: claves por altura sobre el centro (ancho, frente, nuca y corrimiento de la quijada al frente).
    private static readonly float[] CabezaY = { -.118f, -.108f, -.09f, -.065f, -.035f, 0, .03f, .06f, .085f, .102f, .112f };
    private static readonly float[] CabezaW = { .012f, .03f, .046f, .06f, .069f, .074f, .076f, .072f, .06f, .036f, 0 };
    private static readonly float[] CabezaF = { .01f, .022f, .038f, .058f, .072f, .08f, .082f, .076f, .062f, .036f, 0 };
    private static readonly float[] CabezaB = { .01f, .025f, .048f, .07f, .085f, .094f, .097f, .094f, .082f, .05f, 0 };
    private static readonly float[] CabezaZ = { .062f, .052f, .035f, .018f, .006f, 0, -.004f, -.008f, -.012f, -.014f, -.014f };

    private static Vector3 CabezaLocal(float u, float y) =>
        Seccion(new Vector3(0, y, Curva(y, CabezaY, CabezaZ)), Vector3.right, Vector3.forward, Curva(y, CabezaY, CabezaW), Curva(y, CabezaY, CabezaF), Curva(y, CabezaY, CabezaB), u, 2.2f);

    // Cabeza humana de una pieza: craneo, quijada y menton continuos; nariz, orejas, ojos con iris y brillo, cejas y labios.
    // Pintada = cara de catrina.
    private static void CabezaHumana(Vector3 h, Quaternion r, string piel, string cejas, bool pintada, string[] vivos)
    {
        Vector3 H(float x, float y, float z) => h + r * new Vector3(x, y, z);
        Vector3 f = r * Vector3.forward, up = r * Vector3.up;
        string cara = pintada ? "hueso" : piel;
        GeoKit.Superficie(cara, (u, v) => h + r * CabezaLocal(u, Mathf.Lerp(-.118f, .112f, v)), 22, 18);
        Miembro(cara, new[] { H(0, .012f, .072f), H(0, -.024f, .094f) }, t => Mathf.Lerp(.006f, .012f, t), up, .75f, 8, 5);
        Ellipsoid(cara, H(0, -.028f, .088f), new Vector3(.016f, .009f, .01f), r, 4, 8);
        foreach (int sx in new[] { -1, 1 })
            Ellipsoid(piel, H(sx * .073f, -.01f, -.006f), new Vector3(.007f, .027f, .017f), r * Quaternion.Euler(0, sx * -12, 0), 5, 8);
        if (!pintada)
        {
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 ojo = H(sx * .03f, .013f, .07f);
                Ellipsoid("tela_blanca", ojo, new Vector3(.0135f, .008f, .008f), r, 4, 8);
                Ellipsoid("iris", ojo + f * .0058f, new Vector3(.0062f, .0062f, .003f), r, 3, 6);
                Sphere("negro", ojo + f * .0075f, .0028f, 2, 4);
                Sphere("tela_blanca", ojo + f * .0085f + up * .002f + r * Vector3.right * .002f, .0011f, 2, 3);
                // Parpado de arriba: media ceja de piel que hunde el ojo.
                Ellipsoid(piel, ojo + up * .005f + f * .002f, new Vector3(.015f, .005f, .0085f), r * Quaternion.Euler(-8, 0, 0), 3, 8);
                Box(cejas, H(sx * .031f, .03f, .077f), new Vector3(.028f, .005f, .006f), r * Quaternion.Euler(-12, sx * 14, sx * -6));
            }
            Ellipsoid("labios", H(0, -.046f, .075f), new Vector3(.018f, .0045f, .006f), r, 3, 8);
            Ellipsoid("labios", H(0, -.054f, .073f), new Vector3(.015f, .0055f, .006f), r, 3, 8);
        }
        else
        {
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 ojo = H(sx * .031f, .012f, .068f);
                Ellipsoid("negro", ojo, new Vector3(.021f, .022f, .012f), r, 4, 8);
                for (int q = 0; q < 10; q++)
                {
                    float a = q / 10f * Mathf.PI * 2;
                    Ellipsoid(vivos[(q + (sx > 0 ? 1 : 0)) % vivos.Length], H(sx * .031f + Mathf.Cos(a) * .03f, .012f + Mathf.Sin(a) * .03f, .072f - Mathf.Abs(Mathf.Cos(a)) * .006f), new Vector3(.008f, .006f, .004f), r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg), 2, 4);
                }
                Ellipsoid("negro", H(sx * .006f, -.026f, .095f), new Vector3(.005f, .006f, .004f), r, 2, 4);
            }
            Box("negro", H(0, -.05f, .076f), new Vector3(.05f, .004f, .004f), r);
            for (int q = -3; q <= 3; q++) Box("negro", H(q * .0072f, -.05f, .0765f - q * q * .0006f), new Vector3(.0026f, .015f, .004f), r);
            for (int q = 0; q < 5; q++)
            {
                float a = (35 + q * 27.5f) * Mathf.Deg2Rad;
                Sphere(vivos[q % vivos.Length], H(Mathf.Cos(a) * .03f, .05f + Mathf.Sin(a) * .015f, .078f), .005f, 2, 4);
            }
        }
    }

    // Peinados como casco continuo sobre el craneo, que se adelgaza hasta la linea del pelo:
    // corto con fleco, largo, coleta, rizado, rapado, gorra y chongo.
    private static void Peinado(Vector3 h, Quaternion r, string pelo, int estilo, string adorno)
    {
        Vector3 H(float x, float y, float z) => h + r * new Vector3(x, y, z);
        Vector3 up = r * Vector3.up;
        void Casco(string mat, float grosor, float yF, float yS, float yB, float rizo = 0, float fleco = 0)
        {
            var centro = new Vector3(0, -.01f, -.005f);
            GeoKit.Superficie(mat, (u, v) =>
            {
                float a = u * Mathf.PI * 2, s = Mathf.Sin(a);
                float linea = s > 0 ? Mathf.Lerp(yS, yF, s * s) : Mathf.Lerp(yS, yB, s * s);
                float y = Mathf.Lerp(linea, .112f, v * v * (3 - 2 * v) * .4f + v * .6f);
                Vector3 b = CabezaLocal(u, y);
                float g = grosor * Mathf.SmoothStep(0, 1, v / .22f) + fleco * Mathf.Max(0, s) * Mathf.Max(0, s) * Mathf.Sin(v * Mathf.PI) * 1.4f;
                g += rizo * (.6f + .4f * Mathf.Sin(a * 9 + v * 11) * Mathf.Sin(v * 17 + a * 4)) * Mathf.SmoothStep(0, 1, v / .3f);
                return h + r * (b + (b - centro).normalized * g);
            }, 24, 12);
        }
        switch (estilo)
        {
            case 0:
                Casco(pelo, .012f, .066f, .018f, -.045f, 0, .01f);
                break;
            case 1:
                Casco(pelo, .014f, .072f, -.005f, -.06f);
                // Melena que cae por la espalda y enmarca la cara a los lados.
                float[] ys = { .05f, .0f, -.06f, -.14f, -.22f, -.29f, -.32f };
                float[] ws = { .084f, .09f, .092f, .1f, .1f, .092f, .05f };
                float[] fs = { .07f, .062f, .05f, .036f, .028f, .022f, .01f };
                float[] bs = { .105f, .1f, .088f, .07f, .056f, .045f, .02f };
                float[] zs = { -.008f, -.018f, -.035f, -.055f, -.068f, -.075f, -.075f };
                GeoKit.Superficie(pelo, (u, v) =>
                {
                    float y = Mathf.Lerp(.05f, -.32f, v);
                    return h + r * Seccion(new Vector3(0, y, Curva(y, ys, zs)), Vector3.right, Vector3.forward, Curva(y, ys, ws), Curva(y, ys, fs), Curva(y, ys, bs), u, 2.2f);
                }, 20, 12);
                break;
            case 2:
                Casco(pelo, .012f, .066f, .0f, -.05f);
                Ellipsoid(adorno, H(0, .03f, -.104f), new Vector3(.02f, .02f, .012f), r, 3, 8);
                Miembro(pelo, new[] { H(0, .032f, -.106f), H(0, -.02f, -.138f), H(0, -.12f, -.135f), H(0, -.21f, -.11f) }, t => Perfil(t, 0, .02f, .25f, .03f, 1, .006f), up * -1, .8f, 10, 12);
                break;
            case 3:
                Casco(pelo, .02f, .068f, .0f, -.05f, .018f);
                break;
            case 4:
                Casco(pelo, .004f, .056f, .02f, -.05f);
                break;
            case 5:
                Casco(pelo, .006f, .06f, .015f, -.05f);
                Casco(adorno, .02f, .045f, .03f, .0f);
                // Visera curva: su mitad de atras queda dentro de la copa.
                Ellipsoid(adorno, H(0, .05f, .07f), new Vector3(.08f, .006f, .075f), r * Quaternion.Euler(-6, 0, 0), 4, 16);
                Sphere(adorno, H(0, .128f, -.012f), .008f, 2, 5);
                break;
            default:
                Casco(pelo, .012f, .068f, .0f, -.055f);
                Ellipsoid(pelo, H(0, .095f, -.075f), new Vector3(.046f, .04f, .046f), r * Quaternion.Euler(-30, 0, 0), 6, 10);
                Ellipsoid(adorno, H(0, .072f, -.07f), new Vector3(.04f, .012f, .04f), r * Quaternion.Euler(-30, 0, 0), 3, 8);
                break;
        }
    }

    // Mano en forma de palma con pulgar, no de bola.
    private static void Mano(string piel, Vector3 muneca, Vector3 dir, Vector3 palma, Vector3 pulgar)
    {
        Miembro(piel, new[] { muneca, muneca + dir * .05f, muneca + dir * .09f }, t => Perfil(t, 0, .026f, .4f, .041f, .85f, .037f, 1, .024f), palma, .36f, 10, 6);
        Miembro(piel, new[] { muneca + dir * .02f + pulgar * .016f, muneca + dir * .045f + pulgar * .03f, muneca + dir * .065f + pulgar * .032f }, t => Mathf.Lerp(.011f, .008f, t), palma, .9f, 6, 4);
    }

    // Tenis: horma continua del talon a la punta, suela blanca y agujetas.
    private static void Zapato(string mat, Vector3 tobillo, Vector3 f, Vector3 l, float piso)
    {
        Vector3 up = Vector3.up, base0 = new Vector3(tobillo.x, piso, tobillo.z) - f * .06f;
        float largo = .27f;
        Vector3 Horma(float u, float v, float crece, float alto, float bajo)
        {
            float t = v, env = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs(2 * t - 1), 6)));
            float w = Perfil(t, 0, .036f, .3f, .044f, .65f, .05f, .9f, .042f, 1, .03f) * env + crece;
            float arriba = Perfil(t, 0, .075f, .3f, .07f, .55f, .05f, .8f, .032f, 1, .022f) * env + crece;
            Vector3 c = base0 + f * (t * largo) + up * bajo;
            return Seccion(c, l, up, w, Mathf.Max(arriba - bajo, .002f), bajo + crece * .3f, u, 2.6f);
        }
        GeoKit.Superficie(mat, (u, v) => Horma(u, v, 0, 0, .028f), 16, 14);
        GeoKit.Superficie("tela_blanca", (u, v) =>
        {
            float t = v, env = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs(2 * t - 1), 8)));
            float w = Perfil(t, 0, .04f, .3f, .047f, .65f, .053f, .9f, .045f, 1, .034f) * env;
            return Seccion(base0 + f * (t * largo - .004f) + up * .011f, l, up, w, .012f * env, .011f * env, u, 3f);
        }, 16, 12);
        for (int q = 0; q < 3; q++)
        {
            float t = .42f + q * .1f;
            Vector3 c = base0 + f * (t * largo) + up * (Perfil(t, 0, .075f, .3f, .07f, .55f, .05f, .8f, .032f, 1, .022f) + .002f);
            Box("tela_blanca", c, new Vector3(.036f, .004f, .007f), Quaternion.LookRotation(f, up) * Quaternion.Euler(-14, 0, 0));
        }
    }

    // Persona del publico: cuerpo de piezas continuas (tronco, extremidades y cabeza sin uniones de bolas), ropa con holgura,
    // poses distintas y accesorios.
    private static void Persona(Vector3 p, float yaw, bool sentada, bool veladora = false)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        string piel = "piel" + (random.Next(3) + 1);
        string[] playeras = { "ropa1", "ropa2", "ropa3", "ropa5", "ropa6", "ropa7", "ropa8", "tela_morada", "tela_turquesa" };
        string camisa = playeras[random.Next(playeras.Length)];
        string pantalon = random.Next(3) == 0 ? "ropa4" : random.Next(2) == 0 ? "mezclilla" : "ropa1";
        string zapato = random.Next(2) == 0 ? "tenis" : "ropa4";
        string pelo = random.Next(3) == 0 ? "pelo2" : "pelo";
        int peinado = random.Next(4);
        float k = Rnd(.92f, 1.06f);
        float j1 = Rnd(-.04f, .04f), j2 = Rnd(-.04f, .04f), j3 = Rnd(-.08f, .08f), j4 = Rnd(-.08f, .08f);
        // La variedad nueva usa su propia semilla para no mover el resto de la escena.
        var rp = new System.Random((Mathf.RoundToInt(p.x * 73.1f) * 397) ^ Mathf.RoundToInt(p.z * 131.7f));
        string[] vivos = { "tela_magenta", "tela_turquesa", "cempasuchil", "tela_morada" };
        if (rp.Next(5) == 0) piel = "piel4";
        if (rp.Next(6) == 0) pelo = rp.Next(2) == 0 ? "pelo3" : "pelo4";
        bool mujer = rp.Next(2) == 0, pintada = rp.Next(4) == 0, corona = pintada && rp.Next(2) == 0;
        int top = rp.Next(4);                  // 0 playera, 1 sudadera, 2 camisa, 3 playera con estampado
        bool falda = mujer && !sentada && rp.Next(3) == 0, lentes = rp.Next(5) == 0, mochila = rp.Next(4) == 0;
        int estilo = mujer ? new[] { 1, 2, 3, 6, 1 }[rp.Next(5)] : new[] { 0, 3, 4, 5, 0 }[rp.Next(5)];
        int pose = sentada ? -1 : veladora ? 9 : rp.Next(5);   // 0 relajado, 1 bolsillos, 2 brazos cruzados, 3 platicando, 4 celular
        string adorno = vivos[rp.Next(4)];
        bool manga = top == 1 || top == 2;
        string tenis = zapato == "tenis" ? vivos[rp.Next(4)] : zapato;
        float holgura = top == 1 ? .016f : .006f;
        float hombros = (mujer ? .163f : .185f) - .03f;

        Vector3 cad = p + up * (sentada ? .12f : .92f * k);
        // Piernas de una pieza de la cadera al tobillo, con rodilla y pantorrilla marcadas.
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 cadera = cad - up * .05f + l * sx * .085f;
            Vector3 rodilla = sentada ? cad - up * .03f + f * .42f + l * sx * .095f : p + up * .5f * k + l * sx * .095f + f * (sx < 0 ? j1 : j2);
            Vector3 tobillo = sentada ? rodilla - up * .44f - f * .03f : p + up * .09f + l * sx * .1f + f * (sx < 0 ? j3 : j4) * sx;
            if (falda) Miembro(piel, new[] { cadera, rodilla, tobillo }, t => Perfil(t, 0, .074f, .25f, .064f, .5f, .044f, .62f, .05f, .9f, .033f, 1, .031f), f, 1, 12, 18);
            else
            {
                Miembro(pantalon, new[] { cadera, rodilla, tobillo }, t => Perfil(t, 0, .088f, .2f, .076f, .5f, .056f, .62f, .055f, .88f, .049f, 1, .048f), f, 1, 12, 18);
                Cylinder(pantalon, tobillo - up * .012f, .05f, .05f, .02f, 12, Quaternion.FromToRotation(up, (rodilla - tobillo).normalized), true);
            }
            Zapato(tenis, tobillo, f, l, sentada ? tobillo.y - .09f : p.y);
        }
        if (falda)
            Lathe(adorno, cad - up * .34f, new[] { new Vector2(.19f, 0), new Vector2(.2f, .03f), new Vector2(.18f, .2f), new Vector2(.158f, .38f), new Vector2(0, .39f) }, 22);
        // Tronco continuo: pantalon de la cadera a la cintura, cinturon y ropa de arriba con su holgura.
        Tronco(falda ? adorno : pantalon, cad, r, mujer, -.13f, .11f, 0);
        if (!falda && top == 2)
        {
            Tronco("ropa4", cad, r, mujer, .072f, .098f, .004f);
            Box("marco_dorado", PechoFrente(cad, r, mujer, .085f, .009f), new Vector3(.032f, .022f, .004f), r);
        }
        Tronco(camisa, cad, r, mujer, top == 1 ? .02f : top == 2 && !falda ? .1f : .05f, .53f, holgura, top == 2 ? 0 : top == 1 ? .004f : .008f);
        Cylinder(camisa, cad + up * .505f - f * .01f, .052f, .048f, .018f, 12, Quaternion.identity, false);
        if (top == 1)
        {
            // Capucha caida sobre la espalda, bolsa de canguro y cordones.
            Miembro(camisa, new[] { cad + up * .47f + l * .085f + f * .035f, cad + up * .505f + l * .075f - f * .06f, cad + up * .52f - f * .1f, cad + up * .505f - l * .075f - f * .06f, cad + up * .47f - l * .085f + f * .035f }, t => .03f + .012f * Mathf.Sin(t * Mathf.PI), up, 1.2f, 10, 18);
            Ellipsoid(camisa, cad + up * .43f - f * .1f, new Vector3(.095f, .085f, .028f), r * Quaternion.Euler(12, 0, 0), 5, 12);
            Vector3 bolsa = PechoFrente(cad, r, mujer, .19f, holgura + .003f);
            Box(camisa, bolsa, new Vector3(.19f, .085f, .008f), r);
            Box("negro", bolsa + up * .03f + f * .0045f, new Vector3(.17f, .004f, .001f), r);
            foreach (int sx in new[] { -1, 1 }) Rod("tela_blanca", cad + up * .49f + f * .08f + l * sx * .028f, PechoFrente(cad, r, mujer, .37f, holgura + .004f) + l * sx * .032f, .0035f, 4);
        }
        else if (top == 2)
        {
            // Cuello de camisa y botones sobre la tapeta.
            foreach (int sx in new[] { -1, 1 })
                Tri(camisa, cad + up * .52f + l * sx * .05f - f * .0f, cad + up * .48f + l * sx * .012f + f * .085f, cad + up * .465f + l * sx * .06f + f * .07f, f + up * .3f);
            for (int q = 0; q < 5; q++)
            {
                Vector3 b = PechoFrente(cad, r, mujer, .14f + q * .07f, holgura + .002f);
                Sphere("tela_blanca", b, .0055f, 2, 5);
            }
            Box(camisa, PechoFrente(cad, r, mujer, .3f, holgura + .001f), new Vector3(.03f, .34f, .003f), r);
        }
        else if (top == 3)
        {
            Vector3 c = PechoFrente(cad, r, mujer, .34f, holgura + .002f);
            Box(adorno, c, new Vector3(.1f, .08f, .004f), r);
            Box("tela_blanca", c + f * .003f, new Vector3(.06f, .012f, .002f), r);
        }
        if (mochila)
        {
            Vector3 m = cad + up * .3f - f * .17f;
            GeoKit.Superficie(adorno, (u, v) =>
            {
                float y = Mathf.Lerp(-.16f, .16f, v), env = Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs(y / .16f), 5)), .3f);
                return Seccion(m + up * y, l, f, .125f * (.4f + .6f * env), .05f * env, .06f * env, u, 3.5f);
            }, 18, 10);
            GeoKit.Superficie("ropa4", (u, v) =>
            {
                float y = Mathf.Lerp(-.12f, -.01f, v), env = Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs((y + .065f) / .055f), 4)), .3f);
                return Seccion(m + up * y - f * .055f, l, f, .09f * (.5f + .5f * env), .01f, .03f * env, u, 3.5f);
            }, 14, 8);
            foreach (int sx in new[] { -1, 1 })
                Tube("ropa4", new[] { m + up * .13f + l * sx * .07f + f * .06f, cad + up * .48f + l * sx * .1f + f * .0f, cad + up * .4f + l * sx * .12f + f * .1f, cad + up * .2f + l * sx * .115f + f * .085f }, .009f, 4);
        }
        // Brazos de una pieza segun la pose, con manga encima.
        Vector3 manoVela = Vector3.zero;
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = cad + up * .425f + l * sx * hombros;
            Vector3 codo, muneca;
            bool oculta = false;
            if (sentada) { codo = hombro - up * .26f + f * .06f + l * sx * .035f; muneca = cad + f * .36f + l * sx * .11f + up * .06f; }
            else if (pose == 9 && sx > 0) { codo = hombro - up * .26f + l * .03f + f * .06f; muneca = cad + up * .26f + l * .06f + f * .26f; manoVela = muneca + up * .02f; }
            else if (pose == 1) { codo = hombro - up * .25f + l * sx * .07f - f * .03f; muneca = cad + up * .0f + l * sx * .135f + f * .06f; oculta = true; }
            else if (pose == 2) { codo = hombro - up * .25f + l * sx * .03f + f * .07f; muneca = cad + up * .31f + f * .16f - l * sx * .09f; oculta = true; }
            else if (pose == 3 && sx > 0) { codo = hombro - up * .25f + l * .08f + f * .07f; muneca = codo + f * .16f + up * .13f - l * .03f; }
            else if (pose == 4 && sx > 0) { codo = hombro - up * .27f + l * .04f + f * .05f; muneca = hombro - up * .18f - l * .08f + f * .29f; }
            else { codo = hombro - up * .28f + l * sx * .04f; muneca = codo - up * .25f + l * sx * .014f + f * .045f; }
            var brazo = new[] { hombro - l * sx * .025f, codo, muneca };
            System.Func<float, float> grosor = t => Perfil(t, 0, .047f, .15f, .048f, .42f, .038f, .5f, .035f, .62f, .037f, .9f, .028f, 1, .026f);
            Vector3 frenteBrazo = f;
            if (manga)
            {
                Miembro(camisa, brazo, t => grosor(t) + (top == 1 ? .012f : .007f), frenteBrazo, 1, 12, 16, true, oculta);
                Cylinder(piel, muneca - (muneca - codo).normalized * .01f, .027f, .026f, .02f, 10, Quaternion.FromToRotation(up, (muneca - codo).normalized), true);
            }
            else
            {
                Miembro(piel, brazo, grosor, frenteBrazo, 1, 12, 16, false);
                // Manga corta abierta: el brazo sale por dentro, sin borde redondo.
                Miembro(camisa, new[] { hombro - l * sx * .025f, Vector3.Lerp(hombro, codo, .5f) }, t => Perfil(t, 0, .054f, 1, .05f), frenteBrazo, 1, 12, 5, true, false);
            }
            if (sx < 0 && rp.Next(3) == 0) Cylinder("negro", Vector3.Lerp(codo, muneca, .88f), .032f, .032f, .018f, 12, Quaternion.FromToRotation(up, (muneca - codo).normalized), false);
            if (!oculta)
            {
                Vector3 dirMano = (muneca - codo).normalized;
                Vector3 palma = Vector3.Cross(dirMano, f).normalized;
                if (palma.sqrMagnitude < .1f) palma = l;
                Mano(piel, muneca, dirMano, palma, (f - dirMano * Vector3.Dot(f, dirMano)).normalized);
                if (pose == 4 && sx > 0 && !sentada)
                {
                    Vector3 mano = muneca + dirMano * .05f;
                    Quaternion qc = r * Quaternion.Euler(-50, 0, 0);
                    Box("negro", mano + f * .02f + up * .03f, new Vector3(.042f, .085f, .009f), qc);
                    Box("ventana_luz", mano + f * .02f + up * .03f + (qc * Vector3.back) * .005f, new Vector3(.036f, .075f, .002f), qc);
                }
            }
        }
        if (manoVela != Vector3.zero)
        {
            Cylinder("veladora_blanca", manoVela, .03f, .03f, .1f, 8, Quaternion.identity, true);
            Sprite("flama", manoVela + up * .14f, new Vector2(.022f, .045f), Color.white, Rnd());
            Sprite("halo_calido", manoVela + up * .14f, Vector2.one * .25f, new Color(1f, .45f, .12f), Rnd());
        }
        // Cuello con trapecio, cabeza, peinado y accesorios.
        Vector3 h = cad + up * .665f;
        Quaternion rc = r * Quaternion.Euler(rp.Next(3) == 0 ? 8 : 0, (float)rp.NextDouble() * 24 - 12, 0);
        Miembro(piel, new[] { cad + up * .47f - f * .012f, h - up * .07f - f * .012f }, t => Mathf.Lerp(.045f, .039f, t), f, 1.05f, 12, 5);
        CabezaHumana(h, rc, piel, pelo, pintada, vivos);
        Peinado(h, rc, pelo, peinado == 3 && !mujer ? 5 : estilo, adorno);
        Vector3 fc = rc * Vector3.forward, lc = rc * Vector3.right;
        Vector3 H(float x, float y, float z) => h + rc * new Vector3(x, y, z);
        if (lentes && !pintada)
        {
            Quaternion qa = rc * Quaternion.Euler(90, 0, 0);
            foreach (int sx in new[] { -1, 1 })
            {
                Cylinder("negro", H(sx * .031f, .013f, .081f), .019f, .019f, .004f, 14, qa, true, true);
                Cylinder("vidrio_vaso", H(sx * .031f, .013f, .0835f), .016f, .016f, .002f, 14, qa, true, true);
                Rod("negro", H(sx * .05f, .016f, .079f), H(sx * .077f, .018f, -.005f), .003f, 4);
            }
            Box("negro", H(0, .016f, .085f), new Vector3(.024f, .004f, .004f), rc);
        }
        if (mujer && !pintada) foreach (int sx in new[] { -1, 1 }) Sphere("marco_dorado", H(sx * .077f, -.034f, -.004f), .006f, 2, 5);
        if (corona)
            for (int q = 0; q < 7; q++)
            {
                Vector3 d = Quaternion.AngleAxis(-90 + q * 30, up) * fc;
                Cabeza(h + up * .09f + d * .075f, (up + d * .6f).normalized, .028f, q % 3 == 1 ? "terciopelo" : "cempasuchil");
            }
    }



    private static GameObject reflectoresGO;

    private static void Reflectores()
    {
        Group("09c | Reflectores de la explanada");
        var luces = new GameObject("Luz de reflectores").transform;
        luces.SetParent(currentGroup);
        // El poste del centro se quito: en su lugar, sobre el techo, esta el catrin troyano.
        foreach (float a in new[] { 160f, 204f })
        {
            Vector3 b = Pos(a, GradasOut + 1.8f, TerraceY);
            Rod("metal", b, b + Vector3.up * 13f, .14f, 8);
            Vector3 h = b + Vector3.up * 13f;
            Box("metal", h, new Vector3(1.8f, .12f, .12f), Quaternion.Euler(0, a + 90, 0));
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 lamp = h + Tangent(a) * k * .6f + Vector3.up * .25f;
                Box("negro", lamp, new Vector3(.5f, .4f, .2f), Quaternion.LookRotation(-Radial(a) + Vector3.down * .5f));
                Box("reflector", lamp - Radial(a) * .11f - Vector3.up * .05f, new Vector3(.42f, .32f, .02f), Quaternion.LookRotation(-Radial(a) + Vector3.down * .5f));
            }
            var go = new GameObject("Reflector");
            go.transform.SetParent(luces);
            go.transform.position = h + Vector3.up * .2f;
            go.transform.rotation = Quaternion.LookRotation(Vector3.zero - h + Vector3.up * 2);
            var lt = go.AddComponent<Light>();
            lt.type = LightType.Spot; lt.spotAngle = 70; lt.range = 45; lt.intensity = .75f; lt.color = new Color(.95f, .97f, 1f);
        }
        luces.gameObject.SetActive(false);
        reflectoresGO = luces.gameObject;
    }

    private static void Detalles()
    {
        Group("09d | Detalles del campus");
        Vector3 up = Vector3.up;
        // Postes de luz electrica con cables sobre el bulevar, como en las fotos.
        var postes = new List<Vector3>();
        for (float t = -60; t <= 190; t += 32) postes.Add(Barda(t, 3.6f));
        foreach (var p in postes)
        {
            Rod("madera", p, p + up * 10.5f, .13f, 7);
            Box("madera", p + up * 9.8f, new Vector3(1.8f, .12f, .12f), Quaternion.Euler(0, Quaternion.LookRotation(BardaDir).eulerAngles.y + 90, 0));
            Cylinder("metal", p + up * 8.6f + BardaNorte * .3f, .22f, .22f, .7f, 10, Quaternion.identity, true, true);
        }
        for (int i = 0; i < postes.Count - 1; i++)
            foreach (float dx in new[] { -.8f, 0, .8f })
            {
                Vector3 side = Vector3.Cross(BardaDir, up) * dx;
                Vector3 a = postes[i] + up * 9.85f + side, c = postes[i + 1] + up * 9.85f + side;
                var cable = new List<Vector3>();
                for (int k = 0; k <= 10; k++) { float t = k / 10f; cable.Add(Vector3.Lerp(a, c, t) - up * (1.1f * 4 * t * (1 - t))); }
                Tube("negro", cable, .012f, 3);
            }
        // Balizas de luz a lo largo de los senderos.
        foreach (var (s0, s1) in new[] { (Pos(348, PlazaR + .2f), Andador(-9f * E)), (Pos(18, PlazaR + .2f), Andador(9f * E)) })
        {
            Vector3 d = (s1 - s0); float len = d.magnitude; d /= len;
            Vector3 lat = Vector3.Cross(up, d);
            for (float t = 3; t < len - 2; t += 6)
                foreach (int sd in new[] { -1, 1 })
                {
                    Vector3 p = s0 + d * t + lat * sd * 1.85f;
                    Cylinder("negro", p, .07f, .07f, .7f, 8, Quaternion.identity, true);
                    Cylinder("foco", p + up * .55f, .072f, .072f, .1f, 8, Quaternion.identity, false);
                }
        }
        // Cipreses y una palma en el pasto entre los dos edificios del oriente; gabinete junto a la esquina sur del letrero.
        foreach (var c in new[] { new Vector3(64.5f, 0, 11f), new Vector3(67f, 0, 8.5f), new Vector3(72f, 0, 3f), new Vector3(74.5f, 0, .5f) })
            Ciprés(c, Rnd(7.5f, 9.5f));
        Palma(new Vector3(69.5f, 0, 5.8f), 7f);
        Block("metal", new Vector3(28.4f, 0, 3.4f), new Vector3(.6f, 1.15f, .4f), 60, 1);
        Block("metal", new Vector3(28.4f, 1.15f, 3.4f), new Vector3(.66f, .05f, .46f), 60, 1);
        // Florecitas silvestres en el pasto y matorral seco en los terrenos baldios.
        for (int i = 0; i < 260; i++)
        {
            Vector3 p = new Vector3(Rnd(-45, 45), 0, Rnd(22, 52));
            if (Mathf.Abs(p.x - .5f) < 4.5f && Mathf.Abs(p.z - 42) < 3.2f) continue;
            if (new Vector2(p.x, p.z).magnitude < PlazaR + 2) continue;
            string m = Rnd() < .6f ? "amarillo" : "ropa3";
            for (int k = 0; k < 4; k++) Sphere(m, p + new Vector3(Rnd(-.15f, .15f), .06f, Rnd(-.15f, .15f)), .025f, 2, 4);
        }
        for (int i = 0; i < 70; i++)
        {
            Vector3 p = new Vector3(Rnd(-95, -60), 0, Rnd(-40, 110));
            if (Rnd() < .25f) Arbol(p);
            else Zacate(p, Rnd(.5f, .9f), 22);
        }
    }

    private static void Ciprés(Vector3 p, float h)
    {
        Rod("tronco_palma", p, p + Vector3.up * .8f, .1f, 6);
        for (int k = 0; k < 7; k++)
        {
            float t = k / 6f;
            Ellipsoid("ciprés", p + Vector3.up * (1 + t * (h - 1.6f)), new Vector3(.48f * (1 - t * .7f), h * .17f, .48f * (1 - t * .7f)), Quaternion.Euler(0, k * 40, 0), 5, 9);
        }
        for (int k = 0; k < 18; k++)
        {
            float t = Rnd(.05f, .9f);
            Vector3 c = p + Vector3.up * (1 + t * (h - 1.6f)) + Pos(Rnd(0, 360), .38f * (1 - t * .65f));
            Vector3 x = Vector3.Cross(c - p, Vector3.up).normalized * .22f, y = Vector3.up * .4f;
            Quad("hojas", c - x - y, c - x + y, c + x + y, c + x - y, (c - p).normalized, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, true);
        }
    }

    // Muro de contencion crema, escalera junto a muro blanco, rampa sobre el talud y adocreto hexagonal.
    private static void ZonaOriente()
    {
        Group("09e | Escalera y rampa al costado de las gradas");
        Vector3 up = Vector3.up, c = Vector3.zero;
        float top = TerraceY;
        // Fin de la terraza del edificio grande: muro crema desde las gradas hasta el talud de afuera.
        float[] rs = { GradasOut, PlazaR + .3f, TerraceOuter, TerraceOuter + 6 };
        for (int k = 0; k < rs.Length - 1; k++)
        {
            float h0 = TerraceY * SOut(rs[k]) + .05f, h1 = TerraceY * SOut(rs[k + 1]) + .05f;
            QuadUV("muro_crema|c", Pos(GradasA0, rs[k], 0), Pos(GradasA0, rs[k], h0), Pos(GradasA0, rs[k + 1], h1), Pos(GradasA0, rs[k + 1], 0), -Tangent(GradasA0), UvConcreto);
            Quad("remate", Pos(GradasA0, rs[k], h0), Pos(GradasA0, rs[k], h0) - Tangent(GradasA0) * .2f, Pos(GradasA0, rs[k + 1], h1) - Tangent(GradasA0) * .2f, Pos(GradasA0, rs[k + 1], h1), up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
        // Escalera pegada a las gradas: sube de la plaza al andador de arriba.
        Vector3 sube = Tangent(GradasA0), lado = Radial(GradasA0);
        Vector3 pie = Pos(GradasA0, 20.5f) - sube * 3.6f;
        const int escalones = 12;
        Quaternion qe = Quaternion.LookRotation(sube);
        for (int k = 0; k < escalones; k++)
            Block("concreto|c", pie + sube * (.15f + k * .3f), new Vector3(1.8f, top / escalones * (k + 1), .3f), qe.eulerAngles.y, UvConcreto);
        Vector3 nrm = Vector3.Cross(sube, up).normalized;
        foreach (int sd in new[] { -1, 1 })
        {
            Vector3 o = pie - sube * .2f + lado * sd * 1f + nrm * .1f;
            var perfil = new[] { new Vector2(0, 0), new Vector2(3.8f, 0), new Vector2(3.8f, top + .3f), new Vector2(3.5f, top + .3f), new Vector2(0, .3f) };
            Extrude("muro_blanco", perfil, o, sube, up, .2f);
            Vector3 q0 = o - nrm * .1f + up * .3f, q1 = q0 + sube * 3.6f + up * top;
            Box("banqueta", (q0 + q1) / 2 + up * .03f, new Vector3(.26f, .05f, (q1 - q0).magnitude), Quaternion.LookRotation(q1 - q0));
        }
        // Rampa en zigzag frente al muro de la terraza: sube hacia afuera, da vuelta y regresa pegada al muro.
        RampaGradas(Pos(GradasA0, PlazaR + .7f), lado, -sube);
        PisosOriente();
    }

    // u: hacia afuera junto al muro; v: alejandose del muro. Tramo A (afuera) sube a media altura, tramo B (junto al muro) llega arriba.
    private static void RampaGradas(Vector3 o, Vector3 u, Vector3 v)
    {
        float top = TerraceY, mid = top / 2, L = 9.5f, d = 1.7f;
        Vector3 up = Vector3.up;
        Vector3 P(float a, float b, float y) => o + u * a + v * b + up * y;
        float A(float a) => mid * a / L;
        float B(float a) => mid + mid * (1 - a / L);
        void Piso(float a0, float a1, float b0, float b1, float y0, float y1)
        {
            float w = (b1 - b0) / UvConcreto;
            Quad("concreto|c", P(a0, b0, y0 + .04f), P(a0, b1, y0 + .04f), P(a1, b1, y1 + .04f), P(a1, b0, y1 + .04f), up, new Vector2(a0 / UvConcreto, 0), new Vector2(a0 / UvConcreto, w), new Vector2(a1 / UvConcreto, w), new Vector2(a1 / UvConcreto, 0));
        }
        // Muro crema con remate de concreto, de piso a la altura dada.
        void Muro(float a0, float b0, float a1, float b1, float h0, float h1)
        {
            Vector3 p0 = P(a0, b0, 0), p1 = P(a1, b1, 0), n = Vector3.Cross(up, p1 - p0).normalized;
            float len = (p1 - p0).magnitude;
            Quad("muro_crema|c", p0, p0 + up * h0, p1 + up * h1, p1, n, Vector2.zero, new Vector2(0, h0 / UvConcreto), new Vector2(len / UvConcreto, h1 / UvConcreto), new Vector2(len / UvConcreto, 0), true);
            Quad("banqueta", p0 + up * h0 - n * .1f, p0 + up * h0 + n * .1f, p1 + up * h1 + n * .1f, p1 + up * h1 - n * .1f, up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
        float v1 = .15f, v2 = v1 + d, v3 = v2 + .2f, v4 = v3 + d;
        int tramos = 6;
        for (int i = 0; i < tramos; i++)
        {
            float a0 = L * i / tramos, a1 = L * (i + 1) / tramos;
            Piso(a0, a1, v3, v4, A(a0), A(a1));
            Piso(a0, a1, v1, v2, B(a0), B(a1));
            Muro(a0, v4 + .1f, a1, v4 + .1f, A(a0) + .1f, A(a1) + .1f);
            Muro(a0, v2 + .1f, a1, v2 + .1f, B(a0) + .1f, B(a1) + .1f);
        }
        // Descanso al fondo y descanso de llegada junto a la terraza.
        Piso(L, L + d, v1, v4, mid, mid);
        Muro(L, v4 + .1f, L + d, v4 + .1f, mid + .1f, mid + .1f);
        Muro(L + d, v1, L + d, v4 + .1f, mid + .1f, mid + .1f);
        Piso(-d, 0, v1, v2, top, top);
        Muro(-d, v2 + .1f, 0, v2 + .1f, top + .1f, top + .1f);
        Muro(-d, v1, -d, v2 + .1f, top + .1f, top + .1f);
        // Barandal: uno por fuera de todo y otro que hace la U alrededor del muro de en medio.
        var fuera = new List<Vector3>();
        for (int i = 0; i <= tramos; i++) fuera.Add(P(L * i / tramos, v4 - .12f, A(L * i / tramos) + .04f));
        fuera.Add(P(L + d - .12f, v4 - .12f, mid + .04f));
        fuera.Add(P(L + d - .12f, v1 + .12f, mid + .04f));
        for (int i = tramos; i >= 0; i--) fuera.Add(P(L * i / tramos, v1 + .12f, B(L * i / tramos) + .04f));
        Barandal(fuera);
        var centro = new List<Vector3>();
        for (int i = 0; i <= tramos; i++) centro.Add(P(L * i / tramos, v3 + .12f, A(L * i / tramos) + .04f));
        centro.Add(P(L + .5f, (v2 + v3) / 2, mid + .04f));
        for (int i = tramos; i >= 0; i--) centro.Add(P(L * i / tramos, v2 - .12f, B(L * i / tramos) + .04f));
        centro.Add(P(-d + .12f, v2 - .12f, top + .04f));
        centro.Add(P(-d + .12f, v1 + .12f, top + .04f));
        Barandal(centro);
    }

    // Puntos a nivel de piso sobre un arco, cada metro y medio.
    private static List<Vector3> Arco(float r, float a0, float a1, float y0, float y1)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(r * Mathf.Abs(a1 - a0) * Mathf.Deg2Rad / 1.5f));
        var pts = new List<Vector3>();
        for (int i = 0; i <= n; i++) pts.Add(Pos(Mathf.Lerp(a0, a1, (float)i / n), r, Mathf.Lerp(y0, y1, (float)i / n)));
        return pts;
    }

    // Barandal de tubo grueso como el de la facultad: pasamanos doble, tubo guia abajo y remate horizontal en las puntas.
    private static void Barandal(IList<Vector3> pts)
    {
        Vector3 up = Vector3.up;
        float[] alturas = { .92f, .72f, .12f };
        for (int i = 0; i < pts.Count; i++)
        {
            Rod("barandal", pts[i], pts[i] + up * .95f, .03f, 6);
            Sphere("barandal", pts[i] + up * .95f, .033f, 3, 6);
            if (i + 1 < pts.Count)
                foreach (float h in alturas) Rod("barandal", pts[i] + up * h, pts[i + 1] + up * h, .026f, 6);
        }
        foreach (var (p, q) in new[] { (pts[0], pts[1]), (pts[pts.Count - 1], pts[pts.Count - 2]) })
        {
            Vector3 d = p - q;
            d.y = 0;
            Vector3 e = p + d.normalized * .3f;
            Rod("barandal", p + up * .92f, e + up * .92f, .026f, 6);
            Rod("barandal", p + up * .72f, e + up * .72f, .026f, 6);
            Rod("barandal", e + up * .92f, e + up * .72f, .026f, 6);
        }
    }

    // ---------- edificios del oriente, medidos en la capa de mapa de Google (zoom 20) ----------

    // Esquinas del edificio del letrero en metros desde el medallon; el trazado se amplia por E como el resto.
    private static readonly Vector3 NorW = Lay(15f, 0, 19.2f), NorS = Lay(24.9f, 0, 4.9f), NorN = Lay(57.9f, 0, 46.7f), NorE = Lay(69.65f, 0, 27.8f);

    // Pie de la vela entre la esquina oeste (t = 0) y la sur (t = 1); a la mitad se mete 1.6 m hacia el edificio.
    private static Vector3 Vela(float t)
    {
        Vector3 dentro = Vector3.Cross(NorS - NorW, Vector3.up).normalized;
        if (Vector3.Dot(dentro, NorN - NorW) < 0) dentro = -dentro;
        return Vector3.Lerp(NorW, NorS, t) + dentro * (1.6f * 4 * t * (1 - t));
    }

    private static float AlturaVela(float t) => 12.6f + 3f * Mathf.Pow(t, 1.7f);

    // Muro vertical con las juntas de la textura en horizontal, como en los edificios reales.
    private static void Pared(string key, Vector3 a, Vector3 b, float y0, float y1, Vector3 fuera, float uv)
    {
        Vector3 up = Vector3.up;
        QuadUV(key, a + up * y0, b + up * y0, b + up * y1, a + up * y1, fuera, uv);
    }

    private static void Pretil(Vector3 a, Vector3 b, float y)
    {
        Vector3 d = b - a;
        d.y = 0;
        if (d.sqrMagnitude < .01f) return;
        Box("remate", (a + b) * .5f + Vector3.up * (y + .22f), new Vector3(.28f, .45f, d.magnitude + .28f), Quaternion.LookRotation(d));
    }

    // Edificio del letrero, al noreste: vela curva hacia la plaza, atrio de vidrio en la esquina oeste,
    // cuerpo de tres pisos y remate de dos pisos con el letrero hacia el estacionamiento.
    private static void EdificioLetrero()
    {
        Vector3 up = Vector3.up, w = NorW, s = NorS, n = NorN, e = NorE;
        Vector3 ax = (n - w).normalized, axSE = (e - s).normalized, axNE = (e - n).normalized;
        Vector3 haciaLote = Vector3.Cross(ax, up).normalized, haciaSur = Vector3.Cross(up, axSE).normalized, haciaOriente = Vector3.Cross(axNE, up).normalized;
        float largo = Vector3.Dot(n - w, ax);
        Vector3 pNO = w + ax * (largo * .54f);
        Vector3 pSE = s + (e - s) * ((largo * .54f - Vector3.Dot(s - w, ax)) / Vector3.Dot(e - s, ax));
        const float hA = 12.2f, hB = 8.8f, uv = 4.5f;
        const int nv = 24;

        // Vela: sube hacia la esquina sur y por detras asoma sobre la azotea.
        for (int i = 0; i < nv; i++)
        {
            float t0 = (float)i / nv, t1 = (float)(i + 1) / nv;
            Vector3 a = Vela(t0), b = Vela(t1);
            float h0 = AlturaVela(t0), h1 = AlturaVela(t1);
            Vector3 fuera = Vector3.Cross(b - a, up).normalized;
            if (Vector3.Dot(fuera, a - n) < 0) fuera = -fuera;
            QuadUV("fachada", a, b, b + up * h1, a + up * h0, fuera, uv);
            Vector3 ai = a - fuera * .45f, bi = b - fuera * .45f;
            QuadUV("fachada", ai + up * hA, bi + up * hA, bi + up * h1, ai + up * h0, -fuera, uv);
            Quad("remate", a + up * h0, b + up * h1, bi + up * h1, ai + up * h0, up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
        foreach (float t in new[] { 0f, 1f })
        {
            Vector3 p = Vela(t), q = Vela(t == 0 ? .01f : .99f);
            Vector3 fuera = Vector3.Cross(q - p, up).normalized;
            if (Vector3.Dot(fuera, p - n) < 0) fuera = -fuera;
            QuadUV("fachada", p, p - fuera * .45f, p - fuera * .45f + up * AlturaVela(t), p + up * AlturaVela(t), (p - q).normalized, uv);
        }
        // Tres columnas de puntos cerca de la esquina sur, como se ven desde la plaza y desde el estacionamiento.
        foreach (float yb in new[] { 1.4f, 5.4f, 9.4f })
            for (int col = 0; col < 2; col++)
            for (int row = 0; row < 4; row++)
            {
                float t = .8f + (col - .5f) * .035f;
                Vector3 p = Vela(t), q = Vela(t + .01f);
                Vector3 fuera = Vector3.Cross(q - p, up).normalized;
                if (Vector3.Dot(fuera, p - n) < 0) fuera = -fuera;
                Porthole(p + up * (yb + row * .72f), fuera, .26f);
            }

        // Fachada noroeste, la del estacionamiento: atrio de vidrio de tres pisos metido bajo la losa, ventanas y letrero.
        Vector3 a0 = w + ax * 1.2f, a1 = w + ax * 10.6f, hondo = -haciaLote;
        float hv = hA - .7f;
        Pared("fachada", w, a0, 0, hA, haciaLote, uv);
        Pared("fachada", a0, a1, hv, hA, haciaLote, uv);
        Quad("vidrio", a0 + hondo + up * .05f, a1 + hondo + up * .05f, a1 + hondo + up * hv, a0 + hondo + up * hv, haciaLote, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        QuadUV("fachada", a0, a0 + hondo, a0 + hondo + up * hv, a0 + up * hv, ax, uv);
        QuadUV("fachada", a1, a1 + hondo, a1 + hondo + up * hv, a1 + up * hv, -ax, uv);
        Quad("remate", a0 + up * hv, a1 + up * hv, a1 + hondo + up * hv, a0 + hondo + up * hv, Vector3.down, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        Quaternion qL = Quaternion.LookRotation(haciaLote);
        for (int k = 0; k <= 6; k++)
            Box(k % 2 == 0 ? "remate" : "blanco_edificio", Vector3.Lerp(a0, a1, k / 6f) + hondo * .94f + up * (hv / 2), new Vector3(k % 2 == 0 ? .18f : .06f, hv, k % 2 == 0 ? .3f : .08f), qL);
        foreach (float y in new[] { 2f, 3.85f, 5.7f, 7.55f, 9.4f })
        {
            bool losa = y == 3.85f || y == 7.55f;
            Box(losa ? "remate" : "blanco_edificio", (a0 + a1) / 2 + hondo * .94f + up * y, new Vector3(Vector3.Distance(a0, a1), losa ? .22f : .06f, losa ? .3f : .08f), qL);
        }
        Pared("fachada", a1, pNO, 0, hA, haciaLote, uv);
        float lA = Vector3.Distance(a1, pNO);
        for (int fl = 0; fl < 3; fl++)
            for (float d = 1.3f; d < lA - .9f; d += 1.45f) Window(a1 + ax * d + up * (1.75f + fl * 3.6f), haciaLote, .95f, 1.6f);
        Pared("fachada", pNO, n, 0, hB, haciaLote, uv);
        float lB = Vector3.Distance(pNO, n);
        for (int fl = 0; fl < 2; fl++)
            for (float d = 1.3f; d < lB - .9f; d += 1.45f) Window(pNO + ax * d + up * (1.75f + fl * 3.6f), haciaLote, .95f, 1.6f);
        // Como en la foto: dos escudos y el texto condensado alineado a la izquierda; "Facultad de Informatica"
        // abarca unas siete ventanas y termina a unos 2.6 m del escalon del edificio.
        Quaternion qLetrero = Quaternion.LookRotation(-haciaLote);
        Vector3 muro = pNO + haciaLote * .02f;
        const string lineaU = "Universidad Autónoma de Querétaro", lineaF = "Facultad de Informática";
        float emF = 1f, emU = .51f, anchoF = 10.7f, anchoU = 9.2f, fin = 2.6f;
        float ini = fin + anchoF;
        Texto3D(lineaF, muro + ax * (ini - anchoF / 2) + up * (6.95f + .36f * emF), qLetrero, emF, "letra_negra", "fachada", .05f, anchoF / (AnchoTexto3D(lineaF) * emF));
        Texto3D(lineaU, muro + ax * (ini - anchoU / 2) + up * (7.8f + .36f * emU), qLetrero, emU, "letra_negra", "fachada", .04f, anchoU / (AnchoTexto3D(lineaU) * emU));
        for (int k = 0; k < 2; k++) Escudo(muro + ax * (ini + .45f + .525f + k * 1.2f) + up * 7.2f, qLetrero, k == 1);

        // Esquina noreste.
        Pared("fachada", n, e, 0, hB, haciaOriente, uv);
        float lNE = Vector3.Distance(n, e);
        for (int fl = 0; fl < 2; fl++)
            for (float d = 1.6f; d < lNE - 1.2f; d += 1.6f) Window(n + axNE * d + up * (1.75f + fl * 3.6f), haciaOriente, .95f, 1.6f);

        // Fachada sureste: ranura de vidrio verde junto a la vela, con la entrada abajo, y filas de ventanas.
        Vector3 r0 = s + axSE * .4f, r1 = s + axSE * 3.4f, fondo = -haciaSur * .8f;
        float hr = hA - .5f;
        Pared("fachada", s, r0, 0, hA, haciaSur, uv);
        Pared("fachada", r0, r1, hr, hA, haciaSur, uv);
        Quad("vidrio", r0 + fondo, r1 + fondo, r1 + fondo + up * hr, r0 + fondo + up * hr, haciaSur, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        QuadUV("fachada", r0, r0 + fondo, r0 + fondo + up * hr, r0 + up * hr, axSE, uv);
        QuadUV("fachada", r1, r1 + fondo, r1 + fondo + up * hr, r1 + up * hr, -axSE, uv);
        Quad("remate", r0 + up * hr, r1 + up * hr, r1 + fondo + up * hr, r0 + fondo + up * hr, Vector3.down, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        Quaternion qS = Quaternion.LookRotation(haciaSur);
        for (int k = 1; k < 3; k++) Box("remate", Vector3.Lerp(r0, r1, k / 3f) + fondo * .9f + up * (hr / 2), new Vector3(.08f, hr, .12f), qS);
        foreach (float y in new[] { 3.7f, 7.4f }) Box("remate", (r0 + r1) / 2 + fondo * .9f + up * y, new Vector3(3f, .16f, .14f), qS);
        Box("vidrio_oscuro", (r0 + r1) / 2 + fondo * .95f + up * 1.15f, new Vector3(1.8f, 2.3f, .04f), qS);
        Pared("fachada", r1, pSE, 0, hA, haciaSur, uv);
        float lS = Vector3.Distance(r1, pSE);
        for (int fl = 0; fl < 3; fl++)
            for (float d = 1.2f; d < lS - .8f; d += 1.45f) Window(r1 + axSE * d + up * (1.75f + fl * 3.6f), haciaSur, .95f, 1.6f);
        Pared("fachada", pSE, e, 0, hB, haciaSur, uv);
        float lS2 = Vector3.Distance(pSE, e);
        for (int fl = 0; fl < 2; fl++)
            for (float d = 1.2f; d < lS2 - .8f; d += 1.45f) Window(pSE + axSE * d + up * (1.75f + fl * 3.6f), haciaSur, .95f, 1.6f);

        // Escalon entre el cuerpo de tres pisos y el de dos, azoteas con pretil y equipos.
        Vector3 paso = Vector3.Cross(pSE - pNO, up).normalized;
        if (Vector3.Dot(paso, ax) < 0) paso = -paso;
        Pared("fachada", pNO, pSE, hB, hA, paso, uv);
        var techoA = new List<Vector3> { w, pNO, pSE, s };
        for (int i = nv - 1; i > 0; i--) techoA.Add(Vela((float)i / nv));
        Piso("remate", techoA, hA, 3f);
        Piso("remate", new List<Vector3> { pNO, n, e, pSE }, hB, 3f);
        Pretil(w, pNO, hA); Pretil(s, pSE, hA); Pretil(pNO, pSE, hA);
        Pretil(pNO, n, hB); Pretil(n, e, hB); Pretil(pSE, e, hB);
        float yaw = Quaternion.LookRotation(ax).eulerAngles.y;
        Vector3 cA = (w + pNO + pSE + s) / 4, cB = (pNO + n + e + pSE) / 4;
        for (int k = 0; k < 3; k++)
        {
            Block("metal", cA + ax * (k * 7 - 7) + haciaLote * ((k % 2) * 4 - 2) + up * hA, new Vector3(1.6f, 1f, 2.2f), yaw, 1);
            Block("metal", cB + ax * (k * 6 - 6) + haciaLote * ((k % 2) * 3 - 1.5f) + up * hB, new Vector3(1.4f, .9f, 1.8f), yaw, 1);
        }
        ColliderBox("Edificio del letrero", cA + up * hA / 2, new Vector3((Vector3.Distance(w, s) + Vector3.Distance(pNO, pSE)) / 2, hA, largo * .54f), yaw);
        ColliderBox("Edificio del letrero", cB + up * hB / 2, new Vector3((Vector3.Distance(pNO, pSE) + Vector3.Distance(n, e)) / 2, hB, largo * .46f), yaw);
    }

    // Edificio de los paneles solares, al oriente: muro ciego con puntos hacia la plaza, ranuras y terrazas al sur,
    // ventanas al norte y remate redondo de dos pisos con una torre al centro.
    private static void EdificioPaneles()
    {
        Vector3 up = Vector3.up;
        Vector3 no = Lay(35.2f, 0, -1.7f), so = Lay(32.9f, 0, -12f), ne = Lay(58f, 0, -4.15f), se = Lay(54.5f, 0, -20.9f);
        Vector3 dN = (ne - no).normalized, dS = (se - so).normalized;
        Vector3 fueraO = Vector3.Cross(no - so, up).normalized, fueraN = Vector3.Cross(dN, up).normalized, fueraS = Vector3.Cross(up, dS).normalized;
        Vector3 bn = no + dN * 7.5f, bs = so + dS * 7.5f;
        const float hBloque = 13f, hCuerpo = 11.2f, hD = 7.6f, uv = 4.2f;
        const string blanco = "fachada_blanca";

        // Muro ciego hacia la plaza: juntas horizontales y tres columnas de puntos cerca de la esquina sur.
        Pared(blanco, so, no, 0, hBloque, fueraO, uv);
        foreach (float yb in new[] { 1.1f, 4.9f, 8.7f })
            for (int col = 0; col < 2; col++)
            for (int row = 0; row < 4; row++)
                Porthole(Vector3.Lerp(no, so, .79f + (col - .5f) * .058f) + up * (yb + row * .62f), fueraO, .22f);
        // Bancas y estacion de reciclaje al pie del muro, como en las fotos.
        Quaternion qb = Quaternion.LookRotation(fueraO);
        foreach (float t in new[] { .22f, .71f })
        {
            Vector3 b = Vector3.Lerp(no, so, t) + fueraO * .55f;
            Box("madera", b + up * .45f, new Vector3(2f, .06f, .4f), qb);
            foreach (int sd in new[] { -1, 1 }) Box("metal", b + up * .22f + qb * Vector3.right * sd * .85f, new Vector3(.06f, .44f, .38f), qb);
        }
        string[] botes = { "bote_gris", "bote_azul", "bote_amarillo", "bote_rojo" };
        Vector3 rb = Vector3.Lerp(no, so, .5f) + fueraO * .45f;
        Box("metal", rb + up * .5f, new Vector3(2.5f, .05f, .05f), qb);
        for (int k = 0; k < botes.Length; k++)
        {
            Vector3 b = rb + qb * Vector3.right * ((k - 1.5f) * .6f);
            Cylinder("bote_gris", b + up * .12f, .24f, .24f, .72f, 12, Quaternion.identity, false);
            Ellipsoid(botes[k], b + up * .86f, new Vector3(.27f, .18f, .27f), Quaternion.identity, 3, 10, .5f);
        }

        // Costados del bloque ciego y fachadas largas; ventanas al norte.
        Pared(blanco, no, ne, 0, hCuerpo, fueraN, uv);
        Pared(blanco, no, bn, hCuerpo, hBloque, fueraN, uv);
        Pared(blanco, so, se, 0, hCuerpo, fueraS, uv);
        Pared(blanco, so, bs, hCuerpo, hBloque, fueraS, uv);
        Pared(blanco, bs, bn, hCuerpo, hBloque, -fueraO, uv);
        float lN = Vector3.Distance(no, ne);
        for (int fl = 0; fl < 3; fl++)
            for (float d = 2.4f; d < lN - 1.4f; d += 1.7f) Window(no + dN * d + up * (1.75f + fl * 3.6f), fueraN, 1f, 1.7f);
        // Al sur: ranuras verticales en el bloque y terrazas con columnas en el resto.
        for (int fl = 1; fl < 3; fl++)
            for (int k = 0; k < 7; k++)
            {
                Vector3 p = so + dS * (1.4f + k * .62f) + up * (1.9f + fl * 3.6f) + fueraS * .02f;
                Quad("vidrio_oscuro", p - dS * .08f - up * .85f, p + dS * .08f - up * .85f, p + dS * .08f + up * .85f, p - dS * .08f + up * .85f, fueraS, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
        Quaternion qS = Quaternion.LookRotation(fueraS);
        Box("vidrio_oscuro", so + dS * 5.6f + up * 1.15f + fueraS * .02f, new Vector3(1.4f, 2.3f, .04f), qS);
        float lS = Vector3.Distance(bs, se);
        int nl = Mathf.FloorToInt((lS - 2f) / 3.8f);
        float m0 = (lS - nl * 3.8f) / 2f;
        for (int k = 0; k < nl; k++)
        {
            Vector3 c0 = bs + dS * (m0 + 1.9f + k * 3.8f);
            Window(c0 + up * 1.75f, fueraS, 1.6f, 1.6f);
            for (int fl = 1; fl < 3; fl++)
            {
                Vector3 c = c0 + up * (fl * 3.6f + 1.45f);
                Box("vidrio_oscuro", c + fueraS * .02f, new Vector3(3.2f, 2.5f, .04f), qS);
                Box("blanco_edificio", c - up * 1.3f + fueraS * .3f, new Vector3(3.3f, .16f, .6f), qS);
                Rod("barandal", c - up * .2f + fueraS * .55f - dS * 1.6f, c - up * .2f + fueraS * .55f + dS * 1.6f, .025f, 5);
            }
        }
        for (int k = 0; k <= nl; k++)
            Box("blanco_edificio", bs + dS * (m0 + k * 3.8f) + up * 7.2f + fueraS * .3f, new Vector3(.5f, 7.2f, .6f), qS);

        // Remate en D al oriente: dos pisos con ventanas corridas y la torre redonda al centro.
        Vector3 dc = Lay(60.6f, 0, -13.2f);
        float rx = 8.9f * E, rz = 9.5f * E;
        var dPts = new List<Vector3> { ne };
        for (int i = 0; i <= 20; i++)
        {
            float th = Mathf.Lerp(21f, 187f, i / 20f) * Mathf.Deg2Rad;
            dPts.Add(dc + new Vector3(Mathf.Sin(th) * rx, 0, Mathf.Cos(th) * rz));
        }
        dPts.Add(se);
        for (int i = 0; i < dPts.Count - 1; i++)
        {
            Vector3 a = dPts[i], b = dPts[i + 1];
            Vector3 fuera = Vector3.Cross(b - a, up).normalized;
            if (Vector3.Dot(fuera, (a + b) * .5f - dc) < 0) fuera = -fuera;
            Pared(blanco, a, b, 0, hD, fuera, uv);
            Pretil(a, b, hD);
            if (i == 0 || i == dPts.Count - 2) continue;
            foreach (float y in new[] { 1.9f, 5.5f })
                Quad("vidrio", a + fuera * .02f + up * (y - .65f), b + fuera * .02f + up * (y - .65f), b + fuera * .02f + up * (y + .65f), a + fuera * .02f + up * (y + .65f), fuera, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        }
        Piso("remate", dPts, hD, 3f);
        Vector3 fueraE = Vector3.Cross(se - ne, up).normalized;
        if (Vector3.Dot(fueraE, dN) < 0) fueraE = -fueraE;
        Pared(blanco, ne, se, hD, hCuerpo, fueraE, uv);
        Vector3 tc = Lay(59.6f, 0, -12f);
        float tr = 4.6f * E;
        Cylinder("blanco_edificio", tc, tr, tr, 13.6f, 32, Quaternion.identity, true, false, 3);
        Cylinder("vidrio_oscuro", tc + up * 9.6f, tr + .05f, tr + .05f, 1.3f, 32, Quaternion.identity, false);

        // Azoteas con pretil y paneles solares en tres hileras inclinadas hacia el sur.
        Piso("remate", new List<Vector3> { no, bn, bs, so }, hBloque, 3f);
        var techo = new List<Vector3> { bn, ne, se, bs };
        Piso("remate", techo, hCuerpo, 3f);
        Pretil(no, bn, hBloque); Pretil(bn, bs, hBloque); Pretil(bs, so, hBloque); Pretil(so, no, hBloque);
        Pretil(bn, ne, hCuerpo); Pretil(bs, se, hCuerpo); Pretil(ne, se, hCuerpo);
        Quaternion qp = Quaternion.LookRotation(-fueraN) * Quaternion.Euler(16, 0, 0);
        for (int row = 0; row < 3; row++)
            for (float d = 1.2f; d < 40f; d += 2.15f)
            {
                Vector3 p = bn + dN * d - fueraN * (2.6f + row * 3.6f);
                if (!DentroDe(p, techo, 1.3f)) continue;
                Box("panel_solar", p + up * (hCuerpo + .55f), new Vector3(2f, .05f, 1.1f), qp);
            }
        float yawN = Quaternion.LookRotation(dN).eulerAngles.y;
        ColliderBox("Edificio de los paneles", (no + bn + bs + so) / 4 + up * hBloque / 2, new Vector3(Vector3.Distance(no, so), hBloque, 7.5f), yawN);
        ColliderBox("Edificio de los paneles", (bn + ne + se + bs) / 4 + up * hCuerpo / 2, new Vector3((Vector3.Distance(bn, bs) + Vector3.Distance(ne, se)) / 2, hCuerpo, Vector3.Distance((bn + bs) / 2, (ne + se) / 2)), yawN);
        ColliderBox("Edificio de los paneles", dc + up * hD / 2, new Vector3(rx * 1.9f, hD, rz * 1.9f), 0);
    }

    // Pisos entre la plaza y los edificios del oriente sin encimarse: adocreto hexagonal hasta los muros,
    // banqueta de concreto y andador de ladrillo junto al edificio del letrero, y la rampita frente a su entrada.
    private static void PisosOriente()
    {
        Vector3 up = Vector3.up;
        Vector3 axSE = (NorE - NorS).normalized, haciaSur = Vector3.Cross(up, axSE).normalized;
        Vector3 ax = (NorN - NorW).normalized, haciaLote = Vector3.Cross(ax, up).normalized;
        Vector3 axNE = (NorE - NorN).normalized, haciaOriente = Vector3.Cross(axNE, up).normalized;
        Vector3 no = Lay(35.2f, 0, -1.7f), so = Lay(32.9f, 0, -12f), ne = Lay(58f, 0, -4.15f);
        Vector3 sC = NorS + axSE * 13f;
        var hex = new List<Vector3>();
        for (int i = 0; i <= 20; i++) hex.Add(Pos(Mathf.Lerp(38, GradasA0, i / 20f), PlazaR + .3f));
        hex.Add(Pos(GradasA0, FacadeR - .6f) + Tangent(GradasA0) * .3f);
        hex.Add(Pos(GradasA0, FacadeR + 10.4f) + Tangent(GradasA0) * .3f);
        hex.Add(so);
        hex.Add(no);
        hex.Add(no + (ne - no).normalized * 4.5f);
        hex.Add(sC + haciaSur * 2.2f);
        hex.Add(sC);
        for (int i = 24; i >= 0; i--) hex.Add(Vela(i / 24f));
        Piso("hexagonal|c", hex, .026f, UvHex);
        // Banqueta de concreto pegada al edificio del letrero y andador de ladrillo hacia el estacionamiento.
        Piso("concreto|c", new List<Vector3> { NorW, NorN, NorN + haciaLote * 2.6f, NorW + haciaLote * 2.6f }, .03f, UvConcreto);
        Piso("ladrillo|c", new List<Vector3> { NorW + haciaLote * 2.6f, NorN + haciaLote * 2.6f, NorN + haciaLote * 4.8f, NorW + haciaLote * 4.8f }, .028f, UvLadrillo);
        Block("banqueta", (NorW + NorN) * .5f + haciaLote * 2.6f, new Vector3(.18f, .1f, Vector3.Distance(NorW, NorN)), Quaternion.LookRotation(ax).eulerAngles.y, UvConcreto);
        Piso("concreto|c", new List<Vector3> { NorN, NorE, NorE + haciaOriente * 2.6f, NorN + haciaOriente * 2.6f }, .03f, UvConcreto);
        Piso("concreto|c", new List<Vector3> { NorN, NorN + haciaOriente * 2.6f, NorN + haciaOriente * 2.6f + haciaLote * 2.6f, NorN + haciaLote * 2.6f }, .03f, UvConcreto);
        Piso("concreto|c", new List<Vector3> { sC, NorE, NorE + haciaSur * 2.2f, sC + haciaSur * 2.2f }, .03f, UvConcreto);
        // Rampita con pasamanos frente a la entrada de vidrio.
        Vector3 rc = NorS + axSE * 1.9f, q0 = rc + haciaSur * .3f, q1 = rc + haciaSur * 3.6f, l = axSE * .75f;
        Quad("concreto", q0 - l + up * .17f, q0 + l + up * .17f, q1 + l + up * .035f, q1 - l + up * .035f, up, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        foreach (int sd in new[] { -1, 1 })
        {
            Tri("concreto", q0 + l * sd + up * .17f, q0 + l * sd + up * .026f, q1 + l * sd + up * .035f, axSE * sd);
            Vector3 p0 = q0 + l * sd * 1.2f + up * .17f, p1 = q1 + l * sd * 1.2f + up * .035f;
            Rod("barandal", p0, p0 + up * .9f, .03f, 6);
            Rod("barandal", p1, p1 + up * .9f, .03f, 6);
            Rod("barandal", p0 + up * .9f, p1 + up * .9f, .028f, 6);
            Rod("barandal", p0 + up * .45f, p1 + up * .45f, .022f, 6);
        }
    }

    // Escudo de linea negra pintado en el muro: silueta doble y un motivo sencillo, sin copiar el logotipo.
    private static void Escudo(Vector3 c, Quaternion rot, bool banda)
    {
        Vector3 u = rot * Vector3.right, v = rot * Vector3.up;
        const float w = .525f, h = .95f;
        var borde = new List<Vector2> { new Vector2(-w, h), new Vector2(w, h) };
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            Vector2 a = new Vector2(w, -h * .2f), m = new Vector2(w, -h * .78f), b = new Vector2(0, -h);
            borde.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * m + t * t * b);
        }
        for (int i = 9; i >= 0; i--) borde.Add(new Vector2(-borde[2 + i].x, borde[2 + i].y));
        void Anillo(float encoge, float grueso)
        {
            for (int i = 0; i < borde.Count; i++)
            {
                Vector2 p0 = borde[i] * encoge, p1 = borde[(i + 1) % borde.Count] * encoge;
                Vector2 q0 = p0 * (1 - grueso / h), q1 = p1 * (1 - grueso / h);
                Extrude("letra_negra", new List<Vector2> { p0, p1, q1, q0 }, c, u, v, .03f);
            }
        }
        Anillo(1f, .06f);
        Anillo(.86f, .03f);
        float hw = w * .86f * .92f;
        Extrude("letra_negra", new List<Vector2> { new Vector2(-hw, h * .5f), new Vector2(hw, h * .5f), new Vector2(hw, h * .53f), new Vector2(-hw, h * .53f) }, c, u, v, .03f);
        if (banda)
            Extrude("letra_negra", new List<Vector2> { new Vector2(-hw, h * .05f), new Vector2(-hw, h * .2f), new Vector2(hw * .9f, -h * .45f), new Vector2(hw * .75f, -h * .58f) }, c, u, v, .03f);
        else
            for (int i = 0; i < 16; i++)
            {
                float a0 = i * Mathf.PI / 8, a1 = (i + 1) * Mathf.PI / 8, r0 = .26f, r1 = .22f;
                Vector2 o = new Vector2(0, -h * .1f);
                Extrude("letra_negra", new List<Vector2> { o + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r0, o + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r0,
                    o + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r1, o + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r1 }, c, u, v, .03f);
            }
    }

    private static void Gente()
    {
        Group("09b | Estudiantes");
        // Nadie sentado en los pasillos de escalones de las gradas.
        bool EnPasillo(float ang, float r) { foreach (float a in Aisles) if (Mathf.Abs(Mathf.DeltaAngle(ang, a)) * Mathf.Deg2Rad * r < AisleHalf + .7f) return true; return false; }
        // Sentados en las gradas, en grupos.
        int[] filas = { 0, 1, 1, 2, 3, 3, 4 };
        float[] angs = { 158, 163, 196, 201, 230, 236, 170, 206, 140 };
        for (int i = 0; i < angs.Length; i++)
        {
            int k = filas[i % filas.Length];
            float r0 = GradasIn + k * GradaDepth + .3f;
            if (EnPasillo(angs[i], r0)) continue;
            Persona(Pos(angs[i], r0, (k + 1) * GradaH - .02f), angs[i] + 180, true);
        }
        // En la terraza y en la plaza.
        Persona(Pos(190, 20.5f * E, TerraceY), 30, false);
        Persona(Pos(320, 15f * E), 70, false);
        Persona(Pos(322, 15.3f * E), 250, false);
        Persona(Pos(40, 16.5f * E), 200, false);
        // Mas sentados en las gradas.
        for (int i = 0; i < 22; i++)
        {
            float ang = Rnd(140, 240);
            int fila = random.Next(GradaRows);
            float r0 = GradasIn + fila * GradaDepth + .3f;
            if (EnPasillo(ang, r0)) continue;
            Persona(Pos(ang, r0, (fila + 1) * GradaH - .02f), ang + 180 + Rnd(-15, 15), true);
        }
        // Alrededor de la ofrenda, viendo hacia ella; algunos con veladora.
        for (int i = 0; i < 18; i++)
        {
            float ang = i * 20 + Rnd(-6, 6);
            // Nadie parado sobre la cruz de petalos, frente a la cara del fuego ni junto a los catrines.
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 0)) < 34 || Mathf.Abs(Mathf.DeltaAngle(ang, 90)) < 8 || Mathf.Abs(Mathf.DeltaAngle(ang, 180)) < 8 || Mathf.Abs(Mathf.DeltaAngle(ang, 270)) < 8) continue;
            Vector3 q = Pos(ang, Rnd(8.2f, 9.6f));
            Persona(q, Quaternion.LookRotation(-q).eulerAngles.y + Rnd(-20, 20), false, random.Next(3) == 0);
        }
        // Publico del mariachi y de los danzantes.
        for (int i = 0; i < 9; i++)
        {
            Vector3 q = Pos(Rnd(262, 288), Rnd(13.6f, 15f));
            Persona(q, Quaternion.LookRotation(Pos(275, 17.2f) - q).eulerAngles.y, false);
        }
        for (int i = 0; i < 9; i++)
        {
            float a = Rnd(0, 360);
            Vector3 c = Pos(97, 17.6f);
            Vector3 q = c + Quaternion.Euler(0, a, 0) * Vector3.forward * Rnd(3.4f, 4f);
            if ((q - Vector3.zero).magnitude > 22.5f || Mathf.Atan2(q.x, q.z) * Mathf.Rad2Deg > 101) continue;
            Persona(q, Quaternion.LookRotation(c - q).eulerAngles.y, false);
        }
        // Grupitos en la explanada; nadie parado en el andador de la entrada.
        foreach (float ang in new[] { 315f, 250f })
            for (int i = 0; i < 3; i++)
            {
                Vector3 c = Pos(ang, 18f), q = c + Quaternion.Euler(0, i * 120, 0) * Vector3.forward * .6f;
                Persona(q, Quaternion.LookRotation(c - q).eulerAngles.y, false);
            }
    }

}
