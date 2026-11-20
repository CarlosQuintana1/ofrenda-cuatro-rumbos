using System.Collections.Generic;
using UnityEngine;
using static GeoKit;

// Catrina y Catrin a la entrada de la ofrenda, alebrijes en la explanada y tapetes de aserrin en los andadores.
public static partial class BuildMemoriaViva
{
    private const string TxtCatrina = "La Catrina nació como La Calavera Garbancera, figura del grabador José Guadalupe Posada, que la usó para burlarse de quienes aparentaban lo que no eran. El muralista Diego Rivera la rebautizó como La Catrina. Recuerda que ante la muerte todos somos iguales. (Fuente: Wikipedia en español)";
    private const string TxtCatrin = "El Catrín acompaña a La Catrina: un caballero de sombrero de copa, bastón y frac, también convertido en calavera. Juntos se burlan de la vanidad frente a la muerte. (Figura popular inspirada en la obra de Posada.)";
    private const string TxtAlebrije = "Los alebrijes son seres imaginarios que combinan partes de distintos animales, pintados con colores vivos. Los creó el artesano Pedro Linares López (1906-1992) en la Ciudad de México, con cartón y alambre; también se tallan en madera, tradicionalmente de copal. (Fuente: Wikipedia en español)";
    private const string TxtTapete = "Los tapetes de aserrín son capas de aserrín de colores colocadas en el suelo como decoración. En pueblos de Tláhuac como San Nicolás Tetelco y San Juan Ixtayopan se hacen para acompañar las ofrendas de Día de Muertos. Aquí se iluminan al caer la noche. (Fuente: Wikipedia en español)";

    private const float EscalaCatrin = 1.5f;

    private static void Decoracion()
    {
        Group("11 | Catrines, alebrijes y tapetes de aserrín");
        foreach (var (pos, yaw, catrina) in new[] { (new Vector3(-3.8f, 0, 9.3f), 18f, true), (new Vector3(3.8f, 0, 9.3f), -18f, false) })
        {
            // Catrines monumentales: se arman a escala natural y la pieza se agranda 1.5 veces.
            Block("cantera", pos, new Vector3(1.7f, .16f, 1.7f), yaw, 1);
            Block("cempasuchil", pos + Vector3.up * .16f, new Vector3(1.72f, .03f, 1.72f), yaw, 1);
            var figura = Piece(catrina ? "La Catrina" : "El Catrín", pos + Vector3.up * .14f, () => { if (catrina) Catrina(pos, yaw); else Catrin(pos, yaw); });
            figura.localScale = Vector3.one * EscalaCatrin;
            var bal = figura.gameObject.AddComponent<MemoriaViva.Balanceo>();
            bal.rebote = 0; bal.frecuencia = .9f; bal.vaiven = 1.6f;
        }
        // Catrin troyano en el techo del edificio curvo, al centro, mirando la explanada, sobre un pedestal
        // con placa conmemorativa, guirnalda de cempasuchil y veladoras.
        {
            float rT = FacadeR + 2.7f, yT = TerraceY + FacadeH + .3f;
            Vector3 pt = Pos(182, rT, yT), up = Vector3.up, frente = -Radial(182), lado = Vector3.Cross(up, frente);
            float yawT = Quaternion.LookRotation(frente).eulerAngles.y;
            Block("cantera", pt, new Vector3(3.6f, .3f, 3.6f), yawT, 1);
            Block("cantera", pt + up * .3f, new Vector3(2.9f, 1.05f, 2.9f), yawT, 1);
            Block("marco_dorado", pt + up * 1.35f, new Vector3(3.05f, .07f, 3.05f), yawT, 1);
            Block("cantera", pt + up * 1.42f, new Vector3(3.2f, .14f, 3.2f), yawT, 1);
            Quaternion placa = Quaternion.LookRotation(-frente);
            Box("tablero_morado", pt + up * .82f + frente * 1.46f, new Vector3(2.6f, .72f, .03f), placa);
            Texto3D("EN MEMORIA DE TODOS", pt + up * .96f + frente * 1.48f, placa, .17f, "marco_dorado", "tablero_morado");
            Texto3D("NUESTROS TROYANOS", pt + up * .7f + frente * 1.48f, placa, .17f, "marco_dorado", "tablero_morado");
            for (int k = 0; k < 4; k++)
            {
                Quaternion q = Quaternion.AngleAxis(k * 90, up);
                Vector3 cf = q * frente, cl = q * lado;
                for (float x = -1.55f; x <= 1.55f; x += .16f)
                    Cabeza(pt + up * 1.58f + cf * 1.56f + cl * x, (cf + up).normalized, .07f, ((int)(x * 10) + k) % 5 == 0 ? "terciopelo" : null);
            }
            for (int k = 0; k < 16; k++)
            {
                Vector3 vp = pt + Quaternion.AngleAxis(k * 22.5f, up) * frente * 2.3f;
                if (new Vector2(vp.x, vp.z).magnitude < FacadeR + .6f) continue;
                Cylinder(k % 3 == 0 ? "veladora_blanca" : "veladora_roja", vp, .06f, .06f, .22f, 8, Quaternion.identity, true);
                Sprite("flama", vp + up * .3f, new Vector2(.03f, .06f), Color.white, Rnd());
                Sprite("halo_calido", vp + up * .3f, Vector2.one * .4f, new Color(1f, .58f, .25f), Rnd());
            }
            var troyano = Piece("Catrín troyano", pt + up * 1.56f, () => CatrinTroyano(pt + up * 1.56f, yawT));
            troyano.localScale = Vector3.one * 3.2f;
        }
        int v = 0;
        foreach (float a in new[] { 45f, 135f, 225f, 315f })
        {
            Vector3 p = Pos(a, 12.1f);
            float yaw = Quaternion.LookRotation(-p).eulerAngles.y + (v % 2 == 0 ? 35 : -35);
            Alebrije(p, yaw, v % 2 == 0 ? 1f : .85f, v);
            v++;
        }
        int d = 0;
        foreach (var (a0, b0) in new[] { (Pos(18, PlazaR + .2f), Andador(9f * E)) })
            foreach (float t in new[] { .18f, .45f, .72f })
            {
                Vector3 c = Vector3.Lerp(a0, b0, t);
                c.y = .036f;
                Tapete(c, Quaternion.LookRotation(b0 - a0).eulerAngles.y, 1.9f, 2.6f, d++);
            }
        foreach (float a in new[] { 0f, 90f, 270f })
        {
            Vector3 c = Pos(a, a == 0 ? 14.2f : 12.35f);
            c.y = .04f;
            Tapete(c, a + 90, 1.6f, 2.3f, d++);
        }
    }

    private static void Cono(string key, Vector3 a, Vector3 b, float r0, float r1, int segs = 8)
    {
        Vector3 d = b - a;
        Cylinder(key, a, r0, r1, d.magnitude, segs, Quaternion.FromToRotation(Vector3.up, d.normalized), true);
    }

    // Calavera de azucar: craneo con pomulos, quijada con dientes, cuencas con petalos y brillo, nariz de corazon,
    // flor en la frente y puntos de colores; con flores=true lleva todos los adornos de alfenique.
    private static void Calavera(Vector3 h, Vector3 f, Vector3 l, bool flores)
    {
        Quaternion r = Quaternion.LookRotation(f);
        Vector3 up = Vector3.up, c = h + up * .01f;
        Vector3 S(float x, float y) => c + l * x * .105f + up * y * .118f + f * Mathf.Sqrt(Mathf.Max(0, 1 - x * x - y * y)) * .112f;
        Ellipsoid("hueso", c, new Vector3(.105f, .118f, .112f), r, 8, 14);
        foreach (int sx in new[] { -1, 1 }) Ellipsoid("hueso", h - up * .035f + l * sx * .058f + f * .062f, new Vector3(.034f, .03f, .034f), r, 4, 7);
        Ellipsoid("hueso", h - up * .085f + f * .03f, new Vector3(.07f, .048f, .07f), r, 6, 10);
        string[] petalos = flores ? new[] { "tela_magenta", "tela_turquesa" } : new[] { "cempasuchil", "tela_naranja" };
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 ojo = h + f * .09f + l * sx * .042f + up * .012f;
            Ellipsoid("negro", ojo, new Vector3(.03f, .034f, .018f), r, 5, 8);
            Sphere("tela_blanca", ojo + f * .016f + up * .01f + l * sx * .006f, .0055f, 3, 5);
            for (int k = 0; k < 10; k++)
            {
                float a = k / 10f * Mathf.PI * 2;
                Vector3 o = ojo + (l * Mathf.Cos(a) + up * Mathf.Sin(a)) * .046f + f * .002f;
                Ellipsoid(petalos[k % 2], o, new Vector3(.012f, .009f, .006f), r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg), 2, 5);
            }
        }
        foreach (int sx in new[] { -1, 1 })
            Ellipsoid("negro", h + f * .105f - up * .026f + l * sx * .008f, new Vector3(.011f, .015f, .007f), r * Quaternion.Euler(0, 0, sx * 25), 3, 5);
        Box("negro", h + f * .1f - up * .083f, new Vector3(.074f, .02f, .004f), r);
        for (int fila = 0; fila < 2; fila++)
            for (int k = -3; k <= 3; k++)
                Box("hueso", h + f * .103f - up * (.077f + fila * .011f) + l * k * .0105f, new Vector3(.0085f, .008f, .003f), r);
        // Flor de la frente con hojas.
        Vector3 fr = S(0, .55f);
        for (int k = 0; k < 8; k++)
        {
            float a = k / 8f * Mathf.PI * 2;
            Ellipsoid(k % 2 == 0 ? "tela_magenta" : "tela_naranja", fr + (l * Mathf.Cos(a) + up * Mathf.Sin(a)) * .017f, new Vector3(.011f, .007f, .005f), r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg), 2, 5);
        }
        Sphere("cempasuchil", fr + f * .004f, .008f, 3, 5);
        foreach (int sx in new[] { -1, 1 })
            Ellipsoid("verde", S(sx * .3f, .5f), new Vector3(.016f, .006f, .005f), r * Quaternion.Euler(0, 0, sx * -30), 2, 5);
        if (flores)
        {
            // Puntos de color en mejillas, menton y alrededor de la flor.
            string[] colores = { "tela_turquesa", "tela_magenta", "cempasuchil", "tela_morada" };
            int n = 0;
            foreach (int sx in new[] { -1, 1 })
                for (int k = 0; k < 5; k++)
                {
                    float a = (200 + k * 30) * Mathf.Deg2Rad;
                    Sphere(colores[n++ % 4], S(sx * (.62f + .14f * Mathf.Cos(a)), -.25f + .14f * Mathf.Sin(a)), .0065f, 3, 4);
                }
            for (int k = -2; k <= 2; k++) Sphere(colores[n++ % 4], h + f * .101f - up * .108f + l * k * .014f, .006f, 3, 4);
            for (int k = 0; k < 7; k++)
            {
                float a = (20 + k * 23.3f) * Mathf.Deg2Rad;
                Sphere(colores[n++ % 4], S(Mathf.Cos(a) * .42f, .55f + Mathf.Sin(a) * .2f), .0055f, 3, 4);
            }
        }
        Cabeza(h + f * .075f + up * .075f, (f + up * .6f).normalized, .03f, flores ? "terciopelo" : "cempasuchil");
    }


    private static void Catrina(Vector3 p, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        Vector3 b = p + up * .14f;
        Vector3 Dir(float ang) => Quaternion.AngleAxis(ang, up) * f;
        // Falda de campana con cinco holanes de orilla ondulada, encaje blanco y flores bordadas.
        var falda = new[] { new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(.48f, .12f), new Vector2(.36f, .45f), new Vector2(.22f, .8f), new Vector2(.14f, 1f), new Vector2(0, 1f) };
        Lathe("tela_magenta", b, falda, 28);
        float Radio(float y) { for (int i = 1; i < falda.Length - 1; i++) if (y <= falda[i + 1].y) return Mathf.Lerp(falda[i].x, falda[i + 1].x, (y - falda[i].y) / (falda[i + 1].y - falda[i].y)); return .14f; }
        string[] holan = { "tela_naranja", "tela_morada", "tela_turquesa", "tela_naranja", "tela_morada" };
        for (int k = 0; k < 5; k++)
        {
            float y = .02f + k * .15f, rr = Radio(y);
            Lathe(holan[k], b, new[] { new Vector2(rr + .04f, y), new Vector2(Radio(y + .08f) + .012f, y + .08f) }, 28);
            int n = Mathf.Max(12, Mathf.RoundToInt(rr * 64));
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Dir(i * 360f / n);
                Ellipsoid(holan[k], b + up * (y + .004f) + d * (rr + .046f), new Vector3(.03f, .016f, .02f), Quaternion.LookRotation(d), 3, 5);
                if (i % 2 == 0) Sphere("tela_blanca", b + up * (y + .028f) + d * (Radio(y + .028f) + .034f), .0065f, 2, 4);
            }
        }
        for (int k = 0; k < 4; k++)
            for (int i = 0; i < 8; i++)
            {
                float y = .1f + k * .15f;
                Vector3 d = Dir(i * 45 + k * 22.5f);
                Cabeza(b + up * y + d * (Radio(y) + .014f), d, .03f, k % 2 == 0 ? "terciopelo" : "cempasuchil");
            }
        // Corpino con agujeta dorada, escote de encaje, collar de perlas y aretes.
        Lathe("tela_morada", b + up * 1f, new[] { new Vector2(.14f, 0), new Vector2(.15f, .12f), new Vector2(.17f, .22f), new Vector2(.18f, .32f), new Vector2(.13f, .42f), new Vector2(0, .43f) }, 20);
        for (int k = 0; k < 5; k++)
            foreach (int sx in new[] { -1, 1 })
                Box("marco_dorado", b + up * (1.05f + k * .05f) + f * .158f, new Vector3(.05f, .006f, .006f), r * Quaternion.Euler(0, 0, sx * 35));
        for (int k = 0; k < 15; k++)
        {
            Vector3 d = Dir(-75 + k * 10.7f);
            Ellipsoid("tela_blanca", b + up * 1.36f + d * .162f, new Vector3(.022f, .028f, .01f), Quaternion.LookRotation(d) * Quaternion.Euler(-30, 0, 0), 3, 5);
        }
        for (int k = 0; k < 13; k++)
        {
            float a = (-60 + k * 10) * Mathf.Deg2Rad;
            Sphere("tela_blanca", b + up * (1.43f - .05f * Mathf.Cos(a)) + f * (.05f + .04f * Mathf.Cos(a)) + l * Mathf.Sin(a) * .065f, .009f, 3, 5);
        }
        Cabeza(b + up * 1.36f + f * .1f, f, .022f, "terciopelo");
        Cylinder("hueso", b + up * 1.41f, .042f, .04f, .11f, 8, Quaternion.identity, false);
        Vector3 h = b + up * 1.6f + f * .01f;
        Calavera(h, f, l, true);
        foreach (int sx in new[] { -1, 1 })
            for (int k = 0; k < 3; k++) Sphere("marco_dorado", h + l * sx * .112f - up * (.03f + k * .016f), .007f - k * .001f, 3, 5);
        // Sombrero ancho e inclinado con orilla de encaje, listones, flores y plumas.
        Quaternion tilt = r * Quaternion.Euler(-10, 0, 8);
        Vector3 ala = h + up * .095f - f * .02f, tu = tilt * Vector3.up;
        Cylinder("tela_morada", ala, .38f, .38f, .014f, 30, tilt, true, true);
        for (int k = 0; k < 30; k++)
        {
            float a = k / 30f * Mathf.PI * 2;
            Sphere("tela_blanca", ala + tilt * new Vector3(Mathf.Sin(a) * .38f, .008f, Mathf.Cos(a) * .38f), .012f, 2, 4);
        }
        Cylinder("tela_morada", ala, .13f, .115f, .12f, 16, tilt, true);
        Cylinder("tela_naranja", ala + tu * .014f, .133f, .133f, .03f, 16, tilt, false);
        for (int k = 0; k < 11; k++)
        {
            float a = (k / 11f - .1f) * Mathf.PI * 1.5f;
            Vector3 o = ala + tilt * (new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)) * .2f + Vector3.up * .04f);
            Cabeza(o, tu, Rnd(.045f, .06f), k % 3 == 1 ? "terciopelo" : "cempasuchil");
        }
        foreach (var (dx, key) in new[] { (-.06f, "tela_magenta"), (-.02f, "tela_turquesa"), (.02f, "tela_naranja"), (.06f, "tela_morada") })
        {
            Vector3 o = ala + tilt * new Vector3(dx, .1f, -.06f);
            for (int k = 0; k < 9; k++)
            {
                float t = k / 8f;
                Vector3 q = o + tilt * new Vector3(dx * (1 + t * 1.4f), .14f * Mathf.Sin(t * 2.4f) + .02f, -.3f * t);
                Ellipsoid(key, q, new Vector3(.034f - t * .018f, .012f, .055f), tilt * Quaternion.Euler(-20 + t * 60, dx * 300, 0), 3, 6);
            }
        }
        foreach (int sx in new[] { -1, 1 })
            Tube("tela_turquesa", new[] { ala - tilt * Vector3.forward * .13f + tilt * Vector3.right * sx * .03f, h - f * .16f - up * .1f + l * sx * .05f, h - f * .2f - up * .32f + l * sx * .08f }, .012f, 5);
        // Mangas abullonadas; con la derecha sostiene un ramo y con la izquierda un abanico abierto.
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = b + up * 1.37f + l * sx * .19f;
            Ellipsoid("tela_morada", hombro + up * .01f, new Vector3(.095f, .09f, .095f), r, 6, 10);
            Lathe("tela_blanca", hombro - up * .08f, new[] { new Vector2(.06f, 0), new Vector2(.07f, .025f) }, 10);
            Vector3 codo = hombro - up * .25f + l * sx * .04f + f * .08f;
            Vector3 mano = sx < 0 ? b + up * 1.3f + l * -.12f + f * .26f : b + up * 1.22f + l * .08f + f * .26f;
            Tube("tela_morada", new[] { hombro, codo }, .04f, 7);
            Tube("tela_morada", new[] { codo, Vector3.Lerp(codo, mano, .8f) }, .034f, 7);
            Lathe("tela_blanca", Vector3.Lerp(codo, mano, .78f) - up * .02f, new[] { new Vector2(.045f, 0), new Vector2(.062f, .045f) }, 10);
            Ellipsoid("hueso", mano, new Vector3(.032f, .042f, .022f), r, 4, 6);
            if (sx > 0) Ramo(mano + up * .02f, .55f);
        }
        {
            // Abanico abierto: gajos de tela alternados con varillas y orilla doradas.
            Vector3 eje = b + up * 1.3f + l * -.12f + f * .3f;
            Vector3 Arco(float ang, float rad) => eje + (r * Quaternion.Euler(0, 0, ang)) * Vector3.up * rad;
            for (int k = 0; k < 12; k++)
            {
                float a0 = -66 + k * 11, a1 = a0 + 11;
                Tri(k % 2 == 0 ? "tela_magenta" : "tela_naranja", eje, Arco(a0, .2f), Arco(a1, .2f), f);
                Tri(k % 2 == 0 ? "tela_magenta" : "tela_naranja", eje, Arco(a1, .2f), Arco(a0, .2f), -f);
                Rod("marco_dorado", eje + f * .003f, Arco(a0, .2f) + f * .003f, .002f, 3);
                Sphere("tela_blanca", Arco(a0 + 5.5f, .17f) + f * .004f, .006f, 2, 4);
            }
            var borde = new Vector3[13];
            for (int k = 0; k <= 12; k++) borde[k] = Arco(-66 + k * 11, .2f);
            Tube("marco_dorado", borde, .005f, 4);
            Sphere("marco_dorado", eje + f * .005f, .012f, 3, 5);
        }
        // Rebozo naranja sobre los hombros con fleco en las puntas.
        var rebozo = new[] { b + up * 1.12f + l * -.13f + f * .2f, b + up * 1.3f + l * -.19f + f * .12f, b + up * 1.42f + l * -.15f - f * .07f, b + up * 1.44f - f * .13f, b + up * 1.42f + l * .15f - f * .07f, b + up * 1.3f + l * .19f + f * .12f, b + up * 1.1f + l * .15f + f * .21f };
        Tube("tela_naranja", rebozo, .036f, 7);
        foreach (var punta in new[] { rebozo[0], rebozo[rebozo.Length - 1] })
            for (int k = 0; k < 6; k++) Rod("tela_naranja", punta + l * (k - 2.5f) * .012f, punta + l * (k - 2.5f) * .012f - up * .08f, .004f, 3);
        Lathe("tela_naranja", b + up * .97f, new[] { new Vector2(.155f, 0), new Vector2(.15f, .06f) }, 16);
        Cabeza(b + up * 1f + f * .15f + l * .06f, f, .04f, "terciopelo");
        Lathe("tela_blanca", b, new[] { new Vector2(.55f, .0f), new Vector2(.535f, .04f) }, 28);
        ColliderBox("Catrina", p + up * 1.5f, new Vector3(1.6f, 3f, 1.6f), yaw);
        Hotspot("La Catrina", TxtCatrina, b + up * 1.5f, new Vector3(1.5f, 3.1f, 1.5f), yaw);
    }


    private static void Catrin(Vector3 p, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        Vector3 b = p + up * .14f;
        // Pantalon a rayas, zapatos de charol con polainas blancas.
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 cad = b + l * sx * .09f + up * .86f, rod = b + l * sx * .095f + up * .45f + f * .015f, tob = b + l * sx * .095f + up * .06f;
            Cono("tela_negra", cad, rod, .07f, .058f, 10);
            Cono("tela_negra", rod, tob, .058f, .05f, 10);
            for (int k = -1; k <= 1; k++) Box("tela_morada", Vector3.Lerp(cad, tob, .5f) + l * k * .03f + f * .055f, new Vector3(.004f, .8f, .004f), r);
            Ellipsoid("negro", b + l * sx * .095f + f * .06f + up * .03f, new Vector3(.055f, .038f, .12f), r, 5, 9, .45f);
            Ellipsoid("tela_blanca", tob + up * .02f + f * .02f, new Vector3(.06f, .05f, .075f), r, 4, 8);
            Sphere("marco_dorado", tob + up * .02f + l * sx * .055f, .008f, 3, 4);
        }
        // Frac: cuerpo, solapas de satin, faldones partidos atras, chaleco naranja, camisa, mono y flor en el ojal.
        Lathe("tela_negra", b + up * .84f, new[] { new Vector2(.14f, 0), new Vector2(.15f, .2f), new Vector2(.165f, .44f), new Vector2(.12f, .57f), new Vector2(0, .58f) }, 20);
        foreach (int sx in new[] { -1, 1 })
        {
            Box("tela_negra", b + up * .6f - f * .13f + l * sx * .065f, new Vector3(.12f, .5f, .02f), r * Quaternion.Euler(12, sx * 8, sx * -4));
            Box("tela_morada", b + up * 1.2f + f * .15f + l * sx * .085f, new Vector3(.055f, .3f, .015f), r * Quaternion.Euler(-6, 0, sx * 16));
        }
        Box("tela_naranja", b + up * 1.08f + f * .145f, new Vector3(.12f, .26f, .02f), r * Quaternion.Euler(-4, 0, 0));
        for (int k = 0; k < 4; k++) Sphere("marco_dorado", b + up * (.98f + k * .06f) + f * .157f, .01f, 3, 5);
        Tube("marco_dorado", new[] { b + up * 1.1f + f * .158f, b + up * 1.05f + f * .16f + l * .05f, b + up * 1.07f + f * .155f + l * .1f }, .004f, 4);
        Cylinder("marco_dorado", b + up * 1.07f + f * .158f + l * .1f, .016f, .016f, .006f, 10, r * Quaternion.Euler(90, 0, 0), true);
        Box("tela_blanca", b + up * 1.32f + f * .125f, new Vector3(.09f, .14f, .02f), r);
        foreach (int sx in new[] { -1, 1 })
            Ellipsoid("tela_morada", b + up * 1.4f + f * .135f + l * sx * .032f, new Vector3(.034f, .021f, .013f), r * Quaternion.Euler(0, 0, sx * 10), 3, 6);
        Sphere("tela_morada", b + up * 1.4f + f * .14f, .012f, 3, 5);
        Cabeza(b + up * 1.28f + f * .15f + l * -.11f, f, .034f, "cempasuchil");
        Cylinder("hueso", b + up * 1.41f, .042f, .04f, .11f, 8, Quaternion.identity, false);
        Vector3 h = b + up * 1.6f + f * .01f;
        Calavera(h, f, l, false);
        // Monoculo en el ojo derecho con su cadena.
        Vector3 ojo = h + f * .108f + l * .042f + up * .012f;
        Cylinder("marco_dorado", ojo - f * .002f, .033f, .033f, .004f, 16, r * Quaternion.Euler(90, 0, 0), true, true);
        Cylinder("vidrio_vaso", ojo + f * .002f, .029f, .029f, .003f, 16, r * Quaternion.Euler(90, 0, 0), true, true);
        Tube("marco_dorado", new[] { ojo + l * .03f - up * .02f, h + l * .1f - up * .12f + f * .08f, b + up * 1.24f + f * .16f + l * .09f }, .003f, 3);
        // Sombrero de copa con banda morada, flor y pluma.
        Vector3 copa = h + up * .1f;
        Quaternion qc = r * Quaternion.Euler(-4, 0, 0);
        Cylinder("tela_negra", copa, .19f, .19f, .014f, 22, qc, true, true);
        Cylinder("tela_negra", copa, .11f, .118f, .25f, 18, qc, true);
        Cylinder("tela_morada", copa + up * .015f, .121f, .121f, .045f, 18, qc, false);
        Cabeza(copa + up * .04f + l * .115f + f * .02f, l, .035f, "cempasuchil");
        Tube("tela_naranja", new[] { copa + up * .05f + l * .12f - f * .02f, copa + up * .17f + l * .14f - f * .06f, copa + up * .25f + l * .12f - f * .12f }, .01f, 4);
        // Baston en la izquierda, la derecha en la solapa; guantes blancos y punos.
        Vector3 piso = b + l * -.34f + f * .18f, puno = piso + up * .96f + l * .02f;
        Rod("negro", piso, puno, .012f, 6);
        Sphere("marco_dorado", puno + up * .025f, .032f, 5, 8);
        Cono("marco_dorado", piso + up * .04f, piso, .013f, .009f, 6);
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = b + up * 1.37f + l * sx * .16f;
            Sphere("tela_negra", hombro, .062f, 5, 8);
            Vector3 mano = sx < 0 ? puno + up * .03f : b + up * 1.22f + l * .05f + f * .17f;
            Vector3 codo = sx < 0 ? hombro - up * .28f + l * -.07f + f * .08f : hombro - up * .3f + l * .1f + f * .04f;
            Tube("tela_negra", new[] { hombro, codo, Vector3.Lerp(codo, mano, .8f) }, .042f, 8);
            Lathe("tela_blanca", Vector3.Lerp(codo, mano, .78f) - up * .015f, new[] { new Vector2(.044f, 0), new Vector2(.046f, .03f) }, 10);
            Ellipsoid("tela_blanca", mano, new Vector3(.036f, .044f, .03f), r, 4, 7);
        }
        ColliderBox("Catrín", p + up * 1.5f, new Vector3(1.6f, 3f, 1.6f), yaw);
        Hotspot("El Catrín", TxtCatrin, b + up * 1.5f, new Vector3(1.5f, 3.1f, 1.5f), yaw);
    }


    // Alebrije: cuerpo de jaguar, cuello de dragon, cuernos dorados, alas y cola enroscada.
    // Catrin troyano monumental: calavera de azucar con yelmo corintio azul y penacho de cempasuchil, coraza
    // con musculos, faldon de tiras, grebas doradas, capa al viento, escudo con el casco de los Troyanos y lanza.
    private static void CatrinTroyano(Vector3 p, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up, b = p;
        Vector3 Dir(float ang) => Quaternion.AngleAxis(ang, up) * f;

        // Piernas de hueso con grebas, rodilleras y sandalias.
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 cadera = b + up * .84f + l * sx * .085f, rodilla = b + up * .47f + l * sx * .1f + f * .015f, tobillo = b + up * .075f + l * sx * .105f;
            Cono("hueso", cadera, rodilla, .04f, .032f, 8);
            Sphere("hueso", rodilla, .042f, 5, 8);
            Cono("hueso", rodilla, tobillo, .03f, .024f, 8);
            Lathe("marco_dorado", tobillo + up * .03f, new[] { new Vector2(.036f, 0), new Vector2(.045f, .1f), new Vector2(.05f, .24f), new Vector2(.046f, .34f), new Vector2(.04f, .38f) }, 12);
            Ellipsoid("marco_dorado", rodilla + f * .032f, new Vector3(.046f, .05f, .028f), r, 4, 8);
            Box("madera", b + l * sx * .105f + f * .04f + up * .012f, new Vector3(.085f, .024f, .2f), r);
            Ellipsoid("hueso", b + l * sx * .105f + f * .06f + up * .04f, new Vector3(.038f, .025f, .08f), r, 4, 7);
            for (int k = 0; k < 3; k++) Box("marco_dorado", b + l * sx * .105f + f * (.02f + k * .045f) + up * .05f, new Vector3(.09f, .012f, .016f), r);
        }

        // Faldon de tiras en dos capas con puntas doradas y cinturon con broche de cempasuchil.
        for (int capa = 0; capa < 2; capa++)
            for (int k = 0; k < 20; k++)
            {
                Vector3 d = Dir(k * 18 + capa * 9);
                float largo = capa == 0 ? .3f : .22f;
                Quaternion q = Quaternion.LookRotation(d) * Quaternion.Euler(-10, 0, 0);
                Vector3 c = b + up * (.86f - largo / 2) + d * (.17f + capa * .014f + largo * .12f);
                Box(capa == 0 ? "troyano_azul" : "troyano_celeste", c, new Vector3(.056f, largo, .012f), q);
                Box("marco_dorado", c - (q * Vector3.up) * (largo / 2 - .012f) + d * .007f, new Vector3(.056f, .022f, .006f), q);
            }
        Cylinder("marco_dorado", b + up * .84f, .18f, .18f, .055f, 20, Quaternion.identity, true);
        Cabeza(b + up * .87f + f * .19f, f, .035f, "cempasuchil");

        // Coraza musculada: pectorales y abdomen celestes, omoplatos y costuras doradas.
        Lathe("troyano_azul", b + up * .88f, new[] { new Vector2(.17f, 0), new Vector2(.172f, .1f), new Vector2(.19f, .26f), new Vector2(.205f, .4f), new Vector2(.185f, .48f), new Vector2(.12f, .52f), new Vector2(0, .53f) }, 24);
        foreach (int sx in new[] { -1, 1 })
        {
            Ellipsoid("troyano_celeste", b + up * 1.22f + l * sx * .08f + f * .165f, new Vector3(.085f, .07f, .045f), r * Quaternion.Euler(0, sx * 12, 0), 5, 9);
            for (int fila = 0; fila < 3; fila++)
                Ellipsoid("troyano_celeste", b + up * (1.08f - fila * .065f) + l * sx * .045f + f * .17f, new Vector3(.038f, .028f, .022f), r, 4, 7);
            Ellipsoid("troyano_azul", b + up * 1.24f + l * sx * .09f - f * .165f, new Vector3(.08f, .09f, .04f), r, 4, 8);
        }
        Box("marco_dorado", b + up * 1.07f + f * .19f, new Vector3(.012f, .22f, .01f), r);
        Box("marco_dorado", b + up * 1.13f - f * .2f, new Vector3(.012f, .4f, .01f), r);
        Cylinder("marco_dorado", b + up * 1.37f, .125f, .12f, .03f, 16, Quaternion.identity, true);
        // Medallon dorado al pecho con el casco de los Troyanos y banda de cempasuchil cruzada del hombro a la cadera.
        Quaternion medalla = r * Quaternion.Euler(90, 0, 0);
        Cylinder("marco_dorado", b + up * 1.17f + f * .2f, .058f, .058f, .016f, 20, medalla, true, true);
        Cylinder("troyano_azul", b + up * 1.17f + f * .213f, .046f, .046f, .006f, 20, medalla, true, false);
        Vector2[] Mini(Vector2[] pts) { var rr = new Vector2[pts.Length]; for (int k = 0; k < pts.Length; k++) rr[k] = pts[k] * .075f; return rr; }
        Extrude("blanco_edificio", Mini(CascoTroyanos.Contorno), b + up * (1.17f - .0375f) + f * .22f - l * .0335f, l, up, -.004f);
        for (int k = 0; k <= 14; k++)
        {
            float t = k / 14f;
            Vector3 q = Vector3.Lerp(b + up * 1.35f + l * .15f + f * .1f, b + up * .9f - l * .13f + f * .12f, t);
            Vector3 plano = Vector3.ProjectOnPlane(q - b, up), radial = plano.magnitude < .01f ? f : plano.normalized;
            Cabeza(b + up * Vector3.Dot(q - b, up) + radial * (.205f + .03f * Mathf.Sin(t * Mathf.PI)), radial, .03f, k % 4 == 2 ? "terciopelo" : "cempasuchil");
        }

        // Hombreras de tres placas.
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = b + up * 1.36f + l * sx * .2f;
            for (int k = 0; k < 3; k++)
                Ellipsoid(k == 0 ? "marco_dorado" : k == 1 ? "troyano_azul" : "troyano_celeste", hombro + up * (.03f - k * .045f) + l * sx * (k * .018f),
                    new Vector3(.085f + k * .006f, .045f, .09f), r * Quaternion.Euler(0, 0, -sx * (18 + k * 8)), 4, 9, .45f);
        }

        // Brazos de hueso con brazales: la derecha toma la lanza, la izquierda el escudo.
        Vector3 escudo = b + up * 1.02f - l * .33f + f * .2f;
        Vector3 ef = (f - l * .3f).normalized, el = Vector3.Cross(up, ef).normalized;
        Vector3 piso = b + l * .36f + f * .12f, agarre = piso + up * 1.12f;
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = b + up * 1.34f + l * sx * .2f;
            Vector3 mano = sx > 0 ? agarre - l * .015f : escudo - ef * .06f;
            Vector3 codo = sx > 0 ? hombro - up * .27f + l * .07f + f * .07f : hombro - up * .22f - l * .05f + f * .1f;
            Cono("hueso", hombro, codo, .032f, .027f, 8);
            Sphere("hueso", codo, .03f, 4, 7);
            Cono("hueso", codo, mano, .026f, .022f, 8);
            Cono("marco_dorado", Vector3.Lerp(codo, mano, .2f), Vector3.Lerp(codo, mano, .78f), .036f, .031f, 10);
            Ellipsoid("hueso", mano, new Vector3(.035f, .045f, .032f), r, 4, 7);
        }

        // Cuello y calavera de azucar.
        Cylinder("hueso", b + up * 1.4f, .036f, .034f, .12f, 8, Quaternion.identity, false);
        for (int k = 0; k < 3; k++) Cylinder("hueso", b + up * (1.42f + k * .035f), .043f, .043f, .012f, 8, Quaternion.identity, true);
        Vector3 h = b + up * 1.6f + f * .01f;
        Calavera(h, f, l, true);

        // Yelmo corintio: copa, ceja dorada con cuentas, nasal, carrilleras que enmarcan la cara y guardanuca.
        Ellipsoid("troyano_azul", h + up * .015f - f * .01f, new Vector3(.132f, .155f, .14f), r, 7, 14, .5f);
        Ellipsoid("troyano_azul", h - up * .02f - f * .05f, new Vector3(.128f, .1f, .1f), r, 6, 12);
        for (int k = 0; k <= 12; k++)
        {
            Vector3 d = Dir(-105 + k * 17.5f);
            Box("marco_dorado", h + up * .028f + d * .136f, new Vector3(.03f, .028f, .012f), Quaternion.LookRotation(d));
            if (k % 2 == 0) Sphere("cempasuchil", h + up * .028f + d * .145f, .008f, 3, 5);
        }
        Box("troyano_azul", h - up * .005f + f * .126f, new Vector3(.026f, .09f, .02f), r);
        Box("marco_dorado", h - up * .05f + f * .128f, new Vector3(.03f, .014f, .022f), r);
        foreach (int sx in new[] { -1, 1 })
        {
            for (int k = 0; k < 4; k++)
            {
                Vector3 d = Dir(sx * (62 + k * 16));
                Box("troyano_azul", h - up * .04f + d * .128f, new Vector3(.042f, k == 0 ? .12f : .14f, .014f), Quaternion.LookRotation(d) * Quaternion.Euler(-6, 0, 0));
            }
            Vector3 borde = Dir(sx * 56);
            Box("marco_dorado", h - up * .04f + borde * .132f, new Vector3(.008f, .13f, .016f), Quaternion.LookRotation(borde));
            Box("troyano_celeste", h - up * .04f + Dir(sx * 84) * .136f, new Vector3(.03f, .07f, .006f), Quaternion.LookRotation(Dir(sx * 84)));
        }
        for (int k = 0; k < 7; k++)
        {
            Vector3 d = Dir(120 + k * 20);
            Box("troyano_azul", h - up * .07f + d * .142f, new Vector3(.05f, .08f, .012f), Quaternion.LookRotation(d) * Quaternion.Euler(-25, 0, 0));
        }

        // Penacho de cempasuchil de la frente a la nuca, sobre su soporte dorado, y cola que cae por la espalda.
        Vector3 arco = h + up * .1f - f * .01f;
        for (int k = 0; k <= 16; k++)
        {
            float t = Mathf.Lerp(20, 178, k / 16f) * Mathf.Deg2Rad;
            Vector3 dir = f * Mathf.Cos(t) + up * Mathf.Sin(t);
            Box("marco_dorado", arco + dir * .12f, new Vector3(.03f, .02f, .03f), Quaternion.LookRotation(f, dir));
            for (int fila = -1; fila <= 1; fila++)
                Cabeza(arco + dir * (fila == 0 ? .2f : .17f) + l * fila * .03f, (dir + l * fila * .18f).normalized, fila == 0 ? .04f : .034f,
                    (k + fila) % 4 == 0 ? "cempasuchil_oscuro" : "cempasuchil");
        }
        for (int k = 0; k < 9; k++)
            Cabeza(arco - f * (.2f + k * .008f + .02f * Mathf.Sin(k * .5f)) - up * (k * .045f), (-f + up * .3f).normalized, .036f - k * .002f, k % 3 == 2 ? "cempasuchil_oscuro" : "cempasuchil");

        Vector2[] Sc(Vector2[] pts) { var rr = new Vector2[pts.Length]; for (int k = 0; k < pts.Length; k++) rr[k] = pts[k] * .27f; return rr; }
        // Capa al viento desde los hombros: azul por fuera, celeste por dentro, con orilla dorada y broches de flor.
        const int filas = 10, cols = 7;
        Vector3 Capa(int i, int j, float hacia)
        {
            float t = i / (float)filas, s = j / (float)(cols - 1) * 2 - 1;
            float pliegue = .05f * Mathf.Sin(j * 2.1f + t * 4f) * t;
            return b + up * Mathf.Lerp(1.38f, .14f, t) - f * (.23f + t * t * .2f + pliegue + hacia) + l * (s * Mathf.Lerp(.2f, .44f, t) + .1f * t * t);
        }
        for (int i = 0; i < filas; i++)
            for (int j = 0; j < cols - 1; j++)
            {
                Quad("troyano_azul", Capa(i, j, .004f), Capa(i, j + 1, .004f), Capa(i + 1, j + 1, .004f), Capa(i + 1, j, .004f), -f, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                Quad("troyano_celeste", Capa(i, j, 0), Capa(i + 1, j, 0), Capa(i + 1, j + 1, 0), Capa(i, j + 1, 0), f, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            }
        var orilla = new Vector3[cols];
        for (int j = 0; j < cols; j++) orilla[j] = Capa(filas, j, .002f);
        Tube("marco_dorado", orilla, .012f, 5);
        foreach (int j in new[] { 0, cols - 1 })
        {
            var lateral = new Vector3[filas + 1];
            for (int i = 0; i <= filas; i++) lateral[i] = Capa(i, j, .006f);
            Tube("marco_dorado", lateral, .009f, 5);
        }
        // Casco de los Troyanos bordado en oro en la espalda de la capa, para la toma desde atras.
        float atras = 0;
        for (int i = 3; i <= 7; i++)
            for (int j = 1; j <= cols - 2; j++) atras = Mathf.Max(atras, Vector3.Dot(Capa(i, j, .004f) - b, -f));
        Vector3 bordado = b + up * .8f - f * (atras + .03f);
        Vector2[] Bordado(Vector2[] pts) { var rr = new Vector2[pts.Length]; for (int k = 0; k < pts.Length; k++) rr[k] = pts[k] * .28f; return rr; }
        Extrude("marco_dorado", Bordado(CascoTroyanos.Contorno), bordado + l * .125f - up * .14f, -l, up, -.008f);
        foreach (var pl in CascoTroyanos.Placas) Extrude("troyano_azul", Bordado(pl), bordado + l * .125f - up * .14f - f * .009f, -l, up, -.003f);
        foreach (var le in CascoTroyanos.Letras) Extrude("marco_dorado", Bordado(le), bordado + l * .125f - up * .14f - f * .013f, -l, up, -.003f);
        foreach (int sx in new[] { -1, 1 }) Cabeza(b + up * 1.36f + l * sx * .16f + f * .12f, f, .03f, "cempasuchil");

        // Escudo redondo: rin dorado, cara azul con tachones y el casco de los Troyanos; por dentro, celeste con asas.
        Quaternion re = Quaternion.LookRotation(ef), eje = re * Quaternion.Euler(90, 0, 0);
        Cylinder("troyano_celeste", escudo - ef * .042f, .262f, .262f, .012f, 28, eje, true, true);
        Cylinder("marco_dorado", escudo - ef * .032f, .285f, .285f, .026f, 28, eje, true, true);
        Cylinder("troyano_azul", escudo - ef * .008f, .255f, .25f, .02f, 28, eje, true, true);
        for (int k = 0; k < 20; k++)
        {
            float a = k * 18 * Mathf.Deg2Rad;
            Sphere("marco_dorado", escudo + ef * .012f + (el * Mathf.Cos(a) + up * Mathf.Sin(a)) * .243f, .009f, 3, 5);
        }
        // Greca dorada alrededor, como la del logo de los Troyanos.
        for (int k = 0; k < 28; k++)
        {
            float a = k / 28f * Mathf.PI * 2, b2 = (k + .5f) / 28f * Mathf.PI * 2;
            Vector3 rd = el * Mathf.Cos(a) + up * Mathf.Sin(a), rd2 = el * Mathf.Cos(b2) + up * Mathf.Sin(b2);
            Box("marco_dorado", escudo + ef * .014f + rd * .225f, new Vector3(.036f, .007f, .006f), Quaternion.LookRotation(ef, rd));
            Box("marco_dorado", escudo + ef * .014f + rd2 * .212f, new Vector3(.007f, .026f, .006f), Quaternion.LookRotation(ef, rd2));
            Box("marco_dorado", escudo + ef * .014f + rd * .2f, new Vector3(.02f, .007f, .006f), Quaternion.LookRotation(ef, rd));
        }
        // Ramas de laurel a los lados, flores de cempasuchil en el rin y el nombre abajo.
        foreach (int sx in new[] { -1, 1 })
            for (int k = 0; k < 7; k++)
            {
                float a = (sx < 0 ? 245 - k * 13 : 295 + k * 13) * Mathf.Deg2Rad;
                Vector3 rd = el * Mathf.Cos(a) + up * Mathf.Sin(a), tg = (el * -Mathf.Sin(a) + up * Mathf.Cos(a)) * -sx;
                Vector3 c = escudo + ef * .015f + rd * .165f;
                foreach (int lado in new[] { -1, 1 })
                    Ellipsoid("marco_dorado", c + rd * lado * .012f, new Vector3(.007f, .016f, .004f), Quaternion.LookRotation(ef, tg + rd * lado * .7f), 2, 5);
            }
        for (int k = 0; k < 4; k++)
        {
            float a = (45 + k * 90) * Mathf.Deg2Rad;
            Cabeza(escudo + ef * .02f + (el * Mathf.Cos(a) + up * Mathf.Sin(a)) * .27f, ef, .028f, k % 2 == 0 ? "cempasuchil" : "terciopelo");
        }
        Texto3D("TROYANOS", escudo + ef * .016f - up * .155f, Quaternion.LookRotation(-ef), .036f, "marco_dorado", null, .006f);
        Vector3 o = escudo + ef * .013f - el * .1207f - up * .1f;
        Extrude("blanco_edificio", Sc(CascoTroyanos.Contorno), o, el, up, -.01f);
        foreach (var pl in CascoTroyanos.Placas) Extrude("plata", Sc(pl), o + ef * .01f, el, up, -.004f);
        foreach (var le in CascoTroyanos.Letras) Extrude("azul_marino", Sc(le), o + ef * .014f, el, up, -.003f);
        Box("marco_dorado", escudo - ef * .058f, new Vector3(.17f, .035f, .025f), re);
        Rod("madera", escudo - ef * .052f + el * .2f - up * .05f, escudo - ef * .052f + el * .2f + up * .05f, .008f, 5);

        // Lanza con regaton y punta dorada en hoja, atada con flores y un liston de terciopelo.
        Rod("madera", piso, piso + up * 2.3f, .013f, 6);
        Cono("marco_dorado", piso, piso - up * .06f, .016f, .003f, 6);
        Vector3 punta = piso + up * 2.3f;
        Cylinder("marco_dorado", punta - up * .02f, .02f, .02f, .04f, 8, Quaternion.identity, true);
        Cono("marco_dorado", punta + up * .02f, punta + up * .1f, .012f, .04f, 8);
        Cono("marco_dorado", punta + up * .1f, punta + up * .32f, .04f, .002f, 8);
        for (int k = 0; k < 4; k++) Cabeza(punta - up * (.03f + k * .035f) + l * (k % 2 == 0 ? .02f : -.02f), l, .028f, "cempasuchil");
        Tube("terciopelo", new[] { punta - up * .04f, punta - up * .2f + f * .05f + l * .04f, punta - up * .36f + f * .03f + l * .07f }, .007f, 4);
    }

    private static void Alebrije(Vector3 p, float yaw, float s, int variante)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        string piel = "alebrije_" + (variante % 2), otra = "alebrije_" + ((variante + 1) % 2);
        Block("cantera", p, new Vector3(1.5f, .22f, 1.9f) * s, yaw, 1);
        Vector3 b = p + up * .22f * s;
        Vector3 L(float x, float y, float z) => b + (l * x + up * y + f * z) * s;
        Ellipsoid(piel, L(0, .68f, 0), new Vector3(.26f, .25f, .5f) * s, r, 8, 14);
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
        {
            Vector3 hip = L(sx * .15f, .55f, sz * .28f), pie = L(sx * .2f, .06f, sz * .34f + .04f);
            Cono(otra, hip, Vector3.Lerp(hip, pie, .55f) + f * .05f * s * sz, .085f * s, .06f * s);
            Cono(otra, Vector3.Lerp(hip, pie, .55f) + f * .05f * s * sz, pie, .06f * s, .045f * s);
            for (int k = -1; k <= 1; k++) Cono("marco_dorado", pie + l * k * .025f * s, pie + (f * .07f + l * k * .025f) * s - up * .03f * s, .014f * s, .002f * s, 5);
        }
        Cono(piel, L(0, .78f, .38f), L(0, 1.08f, .62f), .14f * s, .09f * s, 10);
        Vector3 h = L(0, 1.14f, .72f);
        Ellipsoid(otra, h, new Vector3(.14f, .13f, .17f) * s, r, 6, 10);
        Cono(otra, L(0, 1.1f, .82f), L(0, 1.05f, 1.02f), .085f * s, .045f * s, 9);
        Ellipsoid("tela_magenta", L(0, 1.02f, .9f), new Vector3(.07f, .025f, .1f) * s, r, 3, 7);
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 ojo = L(sx * .085f, 1.19f, .81f);
            Ellipsoid("ambar", ojo, Vector3.one * .042f * s, r, 4, 7);
            Ellipsoid("negro", ojo + (f * .03f + l * sx * .01f) * s, new Vector3(.012f, .03f, .012f) * s, r, 3, 5);
            Tube("marco_dorado", new[] { L(sx * .06f, 1.24f, .68f), L(sx * .11f, 1.33f, .63f), L(sx * .15f, 1.36f, .54f), L(sx * .13f, 1.31f, .49f) }, .02f * s, 6);
            if (variante % 2 == 0)
            {
                // Alas de plumas en abanico, cada una de otro color.
                string[] plumas = { "tela_magenta", otra, "tela_turquesa", "tela_naranja", otra, "tela_morada" };
                Vector3 raiz = L(sx * .17f, .88f, .08f);
                for (int k = 0; k < plumas.Length; k++)
                {
                    float a = Mathf.Lerp(15, 75, k / (plumas.Length - 1f)) * Mathf.Deg2Rad;
                    float largo = (.5f - Mathf.Abs(k - 2.5f) * .05f) * s;
                    Vector3 dir = (l * sx * Mathf.Cos(a) + up * Mathf.Sin(a) - f * .35f).normalized;
                    Ellipsoid(plumas[k], raiz + dir * largo * .55f, new Vector3(.06f * s, .015f * s, largo * .55f), Quaternion.LookRotation(dir, f), 3, 7);
                }
            }
            else
                for (int k = 0; k < 4; k++)
                    Cono(k % 2 == 0 ? "tela_turquesa" : "tela_naranja", L(sx * .1f, 1.18f, .62f - k * .03f), L(sx * (.32f + k * .03f), 1.3f - k * .08f, .52f - k * .07f), .04f * s, .004f * s, 6);
        }
        for (int k = 0; k < 9; k++)
        {
            float z = .55f - k * .13f;
            Vector3 lomo = L(0, .68f + .25f * Mathf.Sqrt(Mathf.Max(0, 1 - z * z / .25f)) + (k < 2 ? .1f : 0), z);
            Cono(k % 2 == 0 ? "tela_naranja" : "tela_turquesa", lomo - up * .03f * s, lomo + (up * .13f - f * .05f) * s, .04f * s, .003f * s, 5);
        }
        var cola = new[] { L(0, .66f, -.45f), L(0, .58f, -.78f), L(.18f, .45f, -1.02f), L(.38f, .55f, -1.12f), L(.4f, .8f, -1.05f), L(.26f, .9f, -.94f) };
        Tube(piel, cola, .055f * s, 8);
        Cono("marco_dorado", cola[cola.Length - 1], cola[cola.Length - 1] + (l * -.1f + up * .08f + f * .06f) * s, .06f * s, .002f * s, 6);
        ColliderBox("Alebrije", p + up * .8f * s, new Vector3(1.5f, 1.6f, 2f) * s, yaw);
        Hotspot("Alebrije", TxtAlebrije, b + up * .8f * s, new Vector3(1.4f, 1.6f, 2.3f) * s, yaw);
    }

    // Tapete de aserrin con su cenefa de veladoras.
    private static void Tapete(Vector3 c, float yaw, float ancho, float largo, int diseno)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward * (largo / 2), l = r * Vector3.right * (ancho / 2);
        string key = "tapete_" + (diseno % 4);
        Quad(key, c - l - f, c - l + f, c + l + f, c + l - f, Vector3.up, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0));
        int kind = diseno;
        for (int k = 0; k < 4; k++)
        {
            float t = (k + .5f) / 4f * 2 - 1;
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 v = c + l * sx * 1.12f + f * t;
                v.y = c.y - .02f;
                Veladora(v, kind++);
                Sprite("flama", v + Vector3.up * .19f, new Vector2(.025f, .05f), Color.white, Rnd());
                Sprite("halo_calido", v + Vector3.up * .2f, Vector2.one * .22f, new Color(1f, .45f, .12f), Rnd());
            }
        }
        Hotspot("Tapete de aserrín", TxtTapete, c + Vector3.up * .1f, new Vector3(ancho, .25f, largo), yaw);
    }

    // Texto en relieve: cada letra se extruye desde el letrero hacia el visitante. Los huecos (A, O, R...) se tapan con el color del letrero.
    // Ancho de una linea en unidades de em.
    private static float AnchoTexto3D(string linea)
    {
        float ancho = 0;
        foreach (char ch in linea) ancho += Letras3D.Glifos.TryGetValue(ch, out var gl) ? gl.avance : .35f;
        return ancho;
    }

    // sx comprime en horizontal, para letras condensadas como las de la fachada.
    private static void Texto3D(string texto, Vector3 centro, Quaternion rot, float em, string mat, string fondo, float grosor = -1, float sx = 1)
    {
        if (grosor < 0) grosor = em * .12f;
        Vector3 u = rot * Vector3.right * sx, v = rot * Vector3.up, n = rot * Vector3.forward;
        string[] lineas = texto.Split('\n');
        for (int li = 0; li < lineas.Length; li++)
        {
            float ancho = 0;
            foreach (char ch in lineas[li]) ancho += Letras3D.Glifos.TryGetValue(ch, out var gl) ? gl.avance : .35f;
            float x = -ancho / 2, base0 = ((lineas.Length - 1) / 2f - li) * 1.17f - .36f;
            foreach (char ch in lineas[li])
            {
                if (!Letras3D.Glifos.TryGetValue(ch, out var g)) { x += .35f; continue; }
                Vector3 origen = centro + (u * x + v * base0) * em;
                foreach (var c in g.contornos)
                {
                    var poly = new List<Vector2>(c.Puntos.Length / 2);
                    for (int k = 0; k < c.Puntos.Length; k += 2) poly.Add(new Vector2(c.Puntos[k], c.Puntos[k + 1]) * em);
                    if (!c.Hueco) Extrude(mat, poly, origen, u, v, grosor);
                    else if (fondo != null) Extrude(fondo, poly, origen + n * .001f, u, v, grosor + .003f);
                }
                x += g.avance;
            }
        }
    }
}
