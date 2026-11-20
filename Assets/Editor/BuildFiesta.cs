using System.Collections.Generic;
using MemoriaViva;
using UnityEditor;
using UnityEngine;
using static GeoKit;

// La fiesta alrededor de la ofrenda: camino de aserrin, calaveritas, mariachi de catrines, danzantes, jaguares y campo de cempasuchil.
public static partial class BuildMemoriaViva
{
    // Punto de arranque del jugador: bajo el arco de entrada al camino de aserrin.
    private static readonly Vector3 Inicio = new Vector3(-10.64f, 0, 51.83f);
    private const string TxtCaminoAserrin = "Camino de aserrín de colores bajo papel picado, como los que se tienden en las calles de pueblos como Real de Catorce para recibir a las ánimas. Lleva calaveras, flores, corazones y rombos, y lo bordean veladoras. (Inspirado en los tapetes de aserrín de Día de Muertos.)";
    private const string TxtCalaveritasFiesta = "Calaveritas de fiesta: la muerte representada con humor y cariño, como en las calaveras literarias y de azúcar. Son personajes originales de esta ofrenda.";
    private const string TxtMariachi = "Mariachi de catrines con guitarrón, vihuela, guitarra, violín y trompeta. El mariachi es Patrimonio Cultural Inmaterial de la Humanidad desde 2011. La música que suena es una composición original para esta ofrenda. (Fuente: UNESCO)";
    private const string TxtDanzantes = "Danzantes con penacho, sonajas y cascabeles en los tobillos bailan en círculo alrededor del huéhuetl y el copal, como en las danzas de tradición mexica que acompañan las celebraciones. (Interpretación popular.)";
    private const string TxtJaguar = "Jaguar de fiesta con los huesos pintados. Para los pueblos mesoamericanos el jaguar se relacionaba con la noche y el inframundo; aquí cuida la entrada del camino de las ánimas. (Interpretación popular.)";
    private const string TxtMonarcas = "Mariposas monarca. Llegan a los bosques de Michoacán y el Estado de México en estas fechas, y en la tradición purépecha representan a las almas de los difuntos que regresan a visitar a sus familias. (Fuente: Expansión, 20 de octubre de 2025; la coincidencia con su migración la señala Semarnat)";
    private const string TxtBandera = "Bandera de los Troyanos, el equipo y la identidad de los estudiantes de la Facultad de Informática. Ondea junto al campo de cempasúchil con su casco.";
    private const string TxtCampoTroyano = "Campo de cempasúchil sembrado con el casco de los Troyanos, el emblema de la Facultad de Informática: terciopelo para el casco, flor de nube en las placas y las letras FIF. La flor guía a las ánimas con su color, que representa al sol. (Fuente: INPI, gob.mx)";
    private const string TxtCampo = "Campo de cempasúchil sembrado en forma de calavera. La flor guía a las ánimas con su color, que representa al sol. Al conocerlo se ve desde arriba. (Fuente: INPI, gob.mx)";

    private static void Fiesta()
    {
        Group("12 | Camino de aserrín, mariachi, danzantes y jaguares");
        Camino();
        CampoCalavera(new Vector3(0, 0, 29f));
        CampoTroyano(new Vector3(-17.2f, 0, 26.6f));
        Bandera(new Vector3(-32f, 0, 37f));
        Vector3[] calas = { new Vector3(-1.5f, 0, 11.4f), new Vector3(1.6f, 0, 11.2f), new Vector3(-5.6f, 0, 5.6f), new Vector3(5.8f, 0, 5.2f), new Vector3(-7.4f, 0, 21.2f), new Vector3(-1.4f, 0, 19.8f) };
        for (int i = 0; i < calas.Length; i++)
            Calaverita(calas[i], Quaternion.LookRotation(Inicio - calas[i]).eulerAngles.y + Rnd(-25, 25), i);
        Mariachi(Pos(275, 17.2f), 95);
        Danzantes(Pos(97, 17.6f));
        CieloNocturno();
        Monarcas();
        SenderoUnion();
        LucesAcento();
        Indicadores();
        ArcoEntrada();
        Puesto(Pos(322, 21.4f), 322 + 180, true);
        Puesto(Pos(40, 21.4f), 40 + 180, false);
        Jaguar(Pos(334, 21.3f), Quaternion.LookRotation(Pos(348, 30) - Pos(334, 21.3f)).eulerAngles.y, 0);
        Jaguar(Pos(32, 21.3f), Quaternion.LookRotation(Pos(18, 30) - Pos(32, 21.3f)).eulerAngles.y, 1);
    }

    private const string TxtPuestoPan = "Puesto de pan de muerto. El pan es el ofrecimiento fraternal; llegó a la ofrenda con la tradición católica y hoy es uno de sus elementos más reconocibles. (Fuente: INPI, gob.mx) Sus tiras se suelen ver como huesos. (Interpretación popular.)";
    private const string TxtPuestoDulces = "Puesto de dulces típicos con calaveritas de azúcar, jarritos de barro y fruta de temporada. Las calaveras recuerdan que la muerte siempre está presente y se acostumbra escribir en la frente el nombre de quien se recuerda. (Fuente: Profeco e INPI, gob.mx)";

    private const string TxtArcoEntrada = "Arco de cempasúchil a la entrada del camino de aserrín. El arco de carrizo adornado con flores es común a las ofrendas de muchas regiones de México; popularmente se interpreta como la entrada de los difuntos al mundo de los vivos. (Interpretación popular.)";

    // Arco de cempasuchil donde empieza el camino de aserrin, del lado del estacionamiento.
    private static void ArcoEntrada()
    {
        Vector3 a = Pos(348, PlazaR + .2f), b = Andador(-9f * E);
        Vector3 d = (b - a).normalized, l = Vector3.Cross(Vector3.up, d);
        Vector3 c = b - d * 4.2f;
        c.y = 0;
        const float medio = 1.85f, pierna = 2.3f, alto = 1.3f;
        var path = new List<Vector3>();
        for (float y = .1f; y < pierna; y += .09f) path.Add(c - l * medio + Vector3.up * y);
        for (int i = 0; i <= 60; i++)
        {
            float th = Mathf.PI * i / 60;
            path.Add(c - l * medio * Mathf.Cos(th) + Vector3.up * (pierna + alto * Mathf.Sin(th)));
        }
        for (float y = pierna - .09f; y >= .1f; y -= .09f) path.Add(c + l * medio + Vector3.up * y);
        Tube("carrizo", path, .04f, 6);
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 t = (path[i + 1] - path[i - 1]).normalized;
            Vector3 n1 = Vector3.Cross(t, d).normalized, n2 = Vector3.Cross(t, n1).normalized;
            for (int k = 0; k < 6; k++)
            {
                float ph = (k * 60 + (i % 2) * 30 + Rnd(-12, 12)) * Mathf.Deg2Rad;
                Vector3 o = n1 * Mathf.Cos(ph) + n2 * Mathf.Sin(ph);
                Cabeza(path[i] + o * Rnd(.06f, .08f), o, Rnd(.05f, .068f), i % 7 == 0 ? "terciopelo" : null);
            }
        }
        foreach (int sx in new[] { -1, 1 }) Jarron(c + l * sx * (medio + .45f), .9f);
        // Letrero colgando del arco.
        Vector3 top = c + Vector3.up * (pierna + alto - .55f);
        Quaternion cara = Quaternion.LookRotation(-d);
        foreach (int sx in new[] { -1, 1 }) Rod("cordel", top + l * sx * .95f + Vector3.up * .5f, top + l * sx * .95f + Vector3.up * .19f, .006f, 3);
        Box("tela_morada", top, new Vector3(2.3f, .4f, .03f), cara);
        Box("tela_naranja", top - Vector3.up * .22f, new Vector3(2.35f, .05f, .035f), cara);
        Texto3D("BIENVENIDAS, ÁNIMAS", top + d * .015f, Quaternion.LookRotation(-d), .16f, "letra_crema", "tela_morada");
        Hotspot("Arco de entrada", TxtArcoEntrada, c + Vector3.up * 2.4f, new Vector3(4.2f, 1.6f, .6f), Quaternion.LookRotation(d).eulerAngles.y);
    }

    // Petalos que unen el final del camino de aserrin con la cruz de petalos del altar: una sola ruta de las animas.
    private static void SenderoUnion()
    {
        Vector3 a = Pos(348, PlazaR + .1f), b = new Vector3(0, 0, 8.9f * E - .2f);
        Vector3 d = (b - a).normalized, l = Vector3.Cross(Vector3.up, d);
        float len = (b - a).magnitude;
        var rng = new System.Random(8);
        for (int i = 0; i < (int)(len * 260); i++)
        {
            float t = (float)rng.NextDouble(), x = (float)rng.NextDouble() * 1.24f - .62f;
            Vector3 p = a + d * (t * len) + l * x;
            bool borde = Mathf.Abs(x) > .48f;
            string mat = borde && rng.NextDouble() < .75 ? "terciopelo" : rng.NextDouble() < .5 ? "cempasuchil" : "cempasuchil_oscuro";
            Quaternion r = Quaternion.Euler((float)rng.NextDouble() * 20 - 10, (float)rng.NextDouble() * 360, (float)rng.NextDouble() * 20 - 10);
            Vector3 y = Vector3.up * (.04f + (float)rng.NextDouble() * .014f);
            p.y = 0;
            Quad(mat, p + r * new Vector3(-.022f, 0, -.016f) + y, p + r * new Vector3(-.022f, 0, .016f) + y, p + r * new Vector3(.022f, 0, .016f) + y, p + r * new Vector3(.022f, 0, -.016f) + y, Vector3.up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
        for (float t = .8f; t < len; t += 1.6f)
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 v = a + d * t + l * sx * .78f;
                v.y = .02f;
                Veladora(v, (int)(t * 3) + sx);
                Sprite("flama", v + Vector3.up * .19f, new Vector2(.025f, .05f), Color.white, Rnd());
                Sprite("halo_calido", v + Vector3.up * .2f, Vector2.one * .2f, new Color(1f, .45f, .12f), Rnd());
            }
    }

    // De noche: luz calida sobre la ofrenda, los catrines, el mariachi y los danzantes; el resto queda tenue.
    private static void LucesAcento()
    {
        if (reflectoresGO == null) return;
        void Acento(string nombre, Vector3 desde, Vector3 hacia, float angulo, float intensidad)
        {
            var go = new GameObject("Acento | " + nombre);
            go.transform.SetParent(reflectoresGO.transform);
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            var lt = go.AddComponent<Light>();
            lt.type = LightType.Spot; lt.spotAngle = angulo; lt.range = (hacia - desde).magnitude * 4.5f;
            lt.intensity = intensidad; lt.color = new Color(1f, .78f, .5f); lt.shadows = LightShadows.None;
            lt.renderMode = LightRenderMode.ForcePixel;
        }
        Acento("ofrenda", new Vector3(0, 9f, 9f), new Vector3(0, 1.8f, 0), 55, 2.2f);
        Acento("retrato de Juan Gabriel", new Vector3(0, 6.8f, 6f), new Vector3(0, 3.6f, .5f), 16, 1.1f);
        Acento("catrina", new Vector3(-3.8f, 5.5f, 13f), new Vector3(-3.8f, 1.8f, 9.3f), 30, 2f);
        Acento("catrín troyano", Pos(182, 14f, 9f), Pos(182, FacadeR + 2.7f, TerraceY + FacadeH + 5.2f), 22, 2.8f);
        Acento("catrín", new Vector3(3.8f, 5.5f, 13f), new Vector3(3.8f, 1.8f, 9.3f), 30, 2f);
        Acento("mariachi", Pos(275, 12f, 6f), Pos(275, 17.2f, 1.2f), 45, 2.6f);
        Acento("danzantes", Pos(97, 12.5f, 6.5f), Pos(97, 17.6f, .8f), 50, 2.6f);
        Vector3 ca = Andador(-9f * E) - (Andador(-9f * E) - Pos(348, PlazaR + .2f)).normalized * 4.2f;
        Acento("arco de entrada", ca + (Andador(-9f * E) - Pos(348, PlazaR)).normalized * 5f + Vector3.up * 4.5f, ca + Vector3.up * 2.2f, 45, 2.4f);
    }

    // Poste con flechas en relieve a la entrada de la explanada.
    private static void Indicadores()
    {
        Vector3 a = Pos(348, PlazaR + .2f), b = Andador(-9f * E);
        Vector3 d = (b - a).normalized, l = Vector3.Cross(Vector3.up, d);
        Vector3 poste = a + d * 2.2f - l * 2.4f;
        poste.y = 0;
        Rod("madera", poste, poste + Vector3.up * 2.75f, .05f, 6);
        Quaternion cara = Quaternion.LookRotation(-d);
        Vector3 u = cara * Vector3.right, v = Vector3.up, n = cara * Vector3.forward;
        void Flecha(float alto, int dir, string texto, string mat)
        {
            Vector3 c = poste + Vector3.up * alto - n * .06f;
            var poly = new List<Vector2>();
            if (dir == 0) poly.AddRange(new[] { new Vector2(-.62f, -.15f), new Vector2(.62f, -.15f), new Vector2(.62f, .15f), new Vector2(.12f, .15f), new Vector2(0, .27f), new Vector2(-.12f, .15f), new Vector2(-.62f, .15f) });
            else
            {
                float s = dir;
                poly.AddRange(new[] { new Vector2(-.7f * s, -.15f), new Vector2(.55f * s, -.15f), new Vector2(.55f * s, -.22f), new Vector2(.82f * s, 0), new Vector2(.55f * s, .22f), new Vector2(.55f * s, .15f), new Vector2(-.7f * s, .15f) });
            }
            Extrude(mat, poly, c, u, v, .04f);
            Texto3D(texto, c - n * .04f + u * (dir * -.08f), cara, .095f, "letra_crema", null);
        }
        Flecha(2.45f, 0, "OFRENDA", "tela_morada");
        Flecha(2.08f, 1, "MARIACHI · PAN", "tela_magenta");
        Flecha(1.71f, -1, "DANZANTES · DULCES", "tela_turquesa");
    }

    // Monarcas que revolotean sobre el campo de cempasuchil y alrededor de la explanada.
    private static void Monarcas()
    {
        Vector3 campo = new Vector3(0, 0, 29f);
        Hotspot("Mariposas monarca", TxtMonarcas, campo + new Vector3(3.6f, 1.1f, 0), new Vector3(1.4f, 1.6f, 1.4f));
        for (int i = 0; i < 24; i++)
            Sprite("mariposa", campo + Vector3.up * .3f, Vector2.one * Rnd(.1f, .14f), Color.white, Rnd(),
                new Vector4(Rnd(.8f, 3.4f), Rnd(.5f, 1.8f), Rnd(.25f, .6f) * (Rnd() < .5f ? -1 : 1), Rnd(.15f, .4f)));
        for (int i = 0; i < 20; i++)
            Sprite("mariposa", Vector3.zero, Vector2.one * Rnd(.1f, .13f), Color.white, Rnd(),
                new Vector4(Rnd(6f, 16f), Rnd(1.4f, 4f), Rnd(.12f, .25f) * (Rnd() < .5f ? -1 : 1), Rnd(.3f, .7f)));
    }

    // Puesto de feria con toldo a rayas, mantel y mercancia.
    private static void Puesto(Vector3 p, float yaw, bool pan)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        const float w = 2.4f, d = 1.1f;
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Rod("madera", p + l * sx * w / 2 + f * sz * d / 2, p + l * sx * w / 2 + f * sz * d / 2 + up * (sz > 0 ? 2.1f : 2.35f), .035f, 6);
        string[] rayas = pan ? new[] { "tela_magenta", "tela_blanca" } : new[] { "tela_naranja", "tela_morada" };
        int n = 8;
        for (int k = 0; k < n; k++)
        {
            float x = -w / 2 - .1f + (k + .5f) * (w + .2f) / n;
            Vector3 c = p + l * x + up * 2.28f;
            Box(rayas[k % 2], c, new Vector3((w + .2f) / n, .025f, d + .5f), r * Quaternion.Euler(-12, 0, 0));
            Box(rayas[k % 2], c + f * (d / 2 + .3f) - up * .16f, new Vector3((w + .2f) / n, .25f, .02f), r);
        }
        Box("madera", p + up * .82f, new Vector3(w - .1f, .05f, d - .1f), r);
        Box("tela_blanca", p + up * .85f, new Vector3(w, .01f, d), r);
        Box(pan ? "tela_turquesa" : "tela_magenta", p + up * .6f + f * (d / 2 + .005f), new Vector3(w, .5f, .01f), r);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Rod("madera", p + l * sx * (w / 2 - .1f) + f * sz * (d / 2 - .1f), p + l * sx * (w / 2 - .1f) + f * sz * (d / 2 - .1f) + up * .8f, .025f, 4);
        Vector3 mesa = p + up * .86f;
        if (pan)
        {
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
            {
                Vector3 canasta = mesa + l * (-.75f + i * .75f) + f * (.15f - j * .35f);
                Cylinder("madera", canasta, .2f, .22f, .08f, 12, Quaternion.identity, false);
                for (int k = 0; k < 4; k++) PanDeMuerto(canasta + up * .06f + Pos(k * 90 + 45, .1f), .8f);
            }
        }
        else
        {
            for (int i = 0; i < 18; i++)
            {
                Vector3 q = mesa + l * Rnd(-1f, 1f) + f * Rnd(-.3f, .35f);
                Calaverita(q, (f + l * Rnd(-.3f, .3f)).normalized, .9f);
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 q = mesa + l * (-1f + i * .4f) - f * .38f;
                Lathe("barro", q, new[] { new Vector2(0, 0), new Vector2(.035f, 0), new Vector2(.045f, .05f), new Vector2(.03f, .09f), new Vector2(.035f, .1f), new Vector2(0, .1f) }, 8);
            }
            for (int i = 0; i < 3; i++) Ellipsoid("naranja", mesa + l * (-.6f + i * .6f) - f * .1f + up * .05f, new Vector3(.08f, .06f, .08f), r, 4, 8, .4f);
        }
        // El tablero se mide con el texto para que las letras no se salgan.
        string rotulo = pan ? "PAN DE MUERTO" : "DULCES TÍPICOS";
        float em = .16f, tabla = AnchoTexto3D(rotulo) * em + .3f;
        Box("madera", p + up * 2.55f + f * (d / 2 + .3f), new Vector3(tabla, .32f, .03f), r);
        Texto3D(rotulo, p + up * 2.55f + f * (d / 2 + .315f), Quaternion.LookRotation(-f), em, "letra_crema", "madera");
        Rod("madera", p + up * 2.35f + f * (d / 2 + .3f) - l * (tabla / 2 - .1f), p + up * 2.1f + f * (d / 2) - l * (tabla / 2 - .1f), .015f, 4);
        Rod("madera", p + up * 2.35f + f * (d / 2 + .3f) + l * (tabla / 2 - .1f), p + up * 2.1f + f * (d / 2) + l * (tabla / 2 - .1f), .015f, 4);
        ColliderBox(pan ? "Puesto de pan" : "Puesto de dulces", p + up * .5f, new Vector3(w, 1f, d), yaw);
        Hotspot(pan ? "Puesto de pan de muerto" : "Puesto de dulces típicos", pan ? TxtPuestoPan : TxtPuestoDulces, p + up * 1.1f, new Vector3(w, 1.2f, d + .4f), yaw);
    }

    // Cupula de estrellas y luna llena sobre el edificio curvo; el shader las apaga de dia.
    private static void CieloNocturno()
    {
        var rng = new System.Random(21);
        const float R = 600;
        for (int i = 0; i < 1100; i++)
        {
            float az = (float)rng.NextDouble() * 360, el = Mathf.Asin(Mathf.Lerp(.12f, 1f, (float)rng.NextDouble())) * Mathf.Rad2Deg;
            Vector3 d = Quaternion.Euler(-el, az, 0) * Vector3.forward;
            float brillo = Mathf.Pow((float)rng.NextDouble(), 3);
            float tam = Mathf.Lerp(.9f, 3.2f, brillo);
            Color c = Color.Lerp(new Color(.75f, .82f, 1f), new Color(1f, .92f, .8f), (float)rng.NextDouble()) * Mathf.Lerp(.35f, 1f, brillo);
            c.a = (float)rng.NextDouble();
            EstrellaQuad("estrellas", d * R, d, tam, c);
        }
        Vector3 luna = new Vector3(-.38f, .5f, -.78f).normalized;
        EstrellaQuad("luna", luna * (R - 20), luna, 70, new Color(1, 1, 1, 0));
    }

    private static void EstrellaQuad(string key, Vector3 c, Vector3 n, float tam, Color col)
    {
        Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) > .95f ? Vector3.forward : Vector3.up).normalized * tam / 2, v = Vector3.Cross(u, n).normalized * tam / 2;
        var g = GeoKit.Get(key);
        if (g.C == null) g.C = new List<Color>();
        while (g.C.Count < g.V.Count) g.C.Add(Color.white);
        int i0 = g.V.Count;
        Vector3[] p = { c - u - v, c - u + v, c + u + v, c + u - v };
        Vector2[] uv = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        for (int k = 0; k < 4; k++) { g.V.Add(p[k]); g.N.Add(-n); g.U.Add(uv[k]); g.C.Add(col); }
        g.T.Add(i0); g.T.Add(i0 + 1); g.T.Add(i0 + 2);
        g.T.Add(i0); g.T.Add(i0 + 2); g.T.Add(i0 + 3);
    }

    // Series de foquitos de colores siguiendo las tiras de papel picado que bajan de la ofrenda.
    private static void Foquitos()
    {
        string[] focos = { "foco_rojo", "foco_ambar", "foco_verde", "foco_rosa", "foco_azul" };
        Color[] halos = { new Color(1f, .25f, .2f), new Color(1f, .65f, .2f), new Color(.3f, 1f, .4f), new Color(1f, .35f, .75f), new Color(.35f, .55f, 1f) };
        Vector3 apex = Vector3.up * (ArcoPierna + ArcoAlto + .05f);
        int n = 0;
        for (int k = 0; k < 16; k += 2)
        {
            Vector3 q = Pos(11.25f + k * 22.5f, 9.3f * E, 4.55f);
            for (float t = .12f; t < .96f; t += .055f)
            {
                Vector3 p = Vector3.Lerp(apex, q, t) - Vector3.up * (.55f * 4 * t * (1 - t) + .1f);
                int c = n++ % focos.Length;
                Sphere(focos[c], p, .03f, 3, 6);
                Sprite("halo_suave", p, Vector2.one * .16f, halos[c], Rnd());
            }
        }
    }

    // ---------- camino de aserrin con papel picado ----------

    private static void Camino()
    {
        Vector3 a = Pos(348, PlazaR + .2f), b = Andador(-9f * E);
        a.y = b.y = .037f;
        Vector3 d = (b - a).normalized, l = Vector3.Cross(Vector3.up, d);
        float len = (b - a).magnitude, ancho = 2.5f, tramo = 2.5f;
        int n = Mathf.FloorToInt((len - .6f) / tramo);
        for (int i = 0; i < n; i++)
        {
            Vector3 c0 = a + d * (.3f + i * tramo), c1 = c0 + d * tramo;
            Quad("camino_" + (i % 4), c0 - l * ancho / 2, c0 + l * ancho / 2, c1 + l * ancho / 2, c1 - l * ancho / 2, Vector3.up,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
        }
        int kind = 0;
        for (float t = .6f; t < .3f + n * tramo; t += 1.25f)
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 v = a + d * t + l * sx * (ancho / 2 + .14f);
                v.y = .02f;
                Veladora(v, kind++);
                Sprite("flama", v + Vector3.up * .19f, new Vector2(.025f, .05f), Color.white, Rnd());
                Sprite("halo_calido", v + Vector3.up * .2f, Vector2.one * .2f, new Color(1f, .45f, .12f), Rnd());
            }
        // Postes con tiras de papel picado cruzando el andador, y otras a lo largo.
        var postes = new List<(Vector3, Vector3)>();
        for (float t = 1.2f; t < len - .5f; t += 3.4f)
        {
            Vector3 pl = a + d * t - l * 1.75f, pr = a + d * t + l * 1.75f;
            pl.y = pr.y = 0;
            Vector3 letrero = new Vector3(-11f, 0, 30.5f);
            if (Mathf.Abs(t - (len - 4.2f)) < 1f) continue;
            if ((pl - letrero).magnitude < 3.2f || (pr - letrero).magnitude < 3.2f) continue;
            foreach (var pp in new[] { pl, pr }) Rod("madera", pp, pp + Vector3.up * 3.9f, .045f, 6);
            Tira(pl + Vector3.up * 3.85f, pr + Vector3.up * 3.85f, .35f, .36f, .3f, .36f, .2f);
            postes.Add((pl + Vector3.up * 3.85f, pr + Vector3.up * 3.85f));
        }
        for (int i = 0; i < postes.Count - 1; i++)
        {
            Tira(postes[i].Item1, postes[i + 1].Item2, .5f, .38f, .3f, .36f, .3f);
            Tira(postes[i].Item2, postes[i + 1].Item1, .5f, .38f, .3f, .36f, .3f);
        }
        Hotspot("Camino de aserrín", TxtCaminoAserrin, (a + b) * .5f + Vector3.up * .15f, new Vector3(ancho, .3f, len), Quaternion.LookRotation(d).eulerAngles.y);
    }

    // ---------- campo de cempasuchil en forma de calavera ----------

    private static void CampoCalavera(Vector3 centro)
    {
        // Mascara en metros: x a lo ancho, z hacia la entrada. Devuelve 0 fuera, 1 craneo naranja, 2 cuencas y nariz de terciopelo, 3 flores blancas de nube.
        int Zona(float x, float z)
        {
            float zz = z - .4f;
            float craneo = x * x / (3.1f * 3.1f) + zz * zz / (3.4f * 3.4f);
            bool quijada = Mathf.Abs(x) < 1.9f && z < -2.2f && z > -4.3f;
            if (craneo > 1 && !quijada) return 0;
            foreach (int sx in new[] { -1, 1 })
            {
                float ojo = ((x - sx * 1.25f) * (x - sx * 1.25f)) / (.85f * .85f) + ((z - .5f) * (z - .5f)) / (1f * 1f);
                if (ojo < 1) return ojo < .3f ? 3 : 2;
            }
            if (Mathf.Abs(x) < .45f - (z + .6f) * .4f && z < -.6f && z > -1.7f) return 2;
            if (quijada && z < -3f && z > -3.5f && Mathf.Repeat(x * 1.6f, 1) < .3f) return 2;
            if (craneo > .9f && craneo <= 1) return 2;
            return 1;
        }
        var rng = new System.Random(4);
        float paso = .17f;
        for (float x = -3.4f; x <= 3.4f; x += paso)
        for (float z = -4.4f; z <= 4.1f; z += paso)
        {
            float jx = x + (float)rng.NextDouble() * .12f - .06f, jz = z + (float)rng.NextDouble() * .12f - .06f;
            int zona = Zona(jx, jz);
            if (zona == 0) continue;
            Vector3 p = centro + new Vector3(jx, 0, jz);
            float alto = .28f + (float)rng.NextDouble() * .18f;
            string mat = zona == 1 ? ((float)rng.NextDouble() < .3f ? "cempasuchil_oscuro" : "cempasuchil") : zona == 2 ? "terciopelo" : "nube";
            if (rng.Next(2) == 0) Ellipsoid("hoja_flor", p + Vector3.up * alto * .45f, new Vector3(.1f, .08f, .1f), Quaternion.Euler(0, rng.Next(360), 0), 2, 5);
            if (zona == 1) Cabeza(p + Vector3.up * alto, (Vector3.up + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .5f).normalized, .085f, mat);
            else Ellipsoid(mat, p + Vector3.up * alto, new Vector3(.1f, .065f, .1f), Quaternion.Euler(0, rng.Next(360), 0), 2, 6);
        }
        Hotspot("Campo de cempasúchil", TxtCampo, centro + Vector3.up * .3f, new Vector3(6.8f, .6f, 8.6f)).gameObject.AddComponent<VistaAerea>().centro = centro;
    }

    // ---------- campo de cempasuchil con el casco de los Troyanos ----------

    private static bool DentroPoligono(Vector2 p, Vector2[] poly)
    {
        bool dentro = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                dentro = !dentro;
        return dentro;
    }

    // Se lee derecho desde la plaza: la cama sube hacia el estacionamiento para que el casco se vea de pie.
    private static void CampoTroyano(Vector3 centro)
    {
        const float alto = 8.6f, rx = 5.2f, rz = 6f, sube = .9f;
        float Y(float z) => .04f + sube * (z + rz) / (2 * rz);
        string Zona(float x, float z)
        {
            float e = x * x / (rx * rx) + z * z / (rz * rz);
            if (e > 1) return null;
            if (e > .92f) return "cempasuchil_oscuro";
            var c = new Vector2(x / alto + .447f, z / alto + .5f);
            if (DentroPoligono(c, CascoTroyanos.Contorno))
            {
                foreach (var l in CascoTroyanos.Letras) if (DentroPoligono(c, l)) return "terciopelo";
                foreach (var pl in CascoTroyanos.Placas) if (DentroPoligono(c, pl)) return "nube";
                foreach (var b in CascoTroyanos.Bordes) if (DentroPoligono(c, b)) return "cempasuchil";
                return "terciopelo";
            }
            return "cempasuchil";
        }
        // Cama de tierra inclinada con su borde.
        const int lados = 48;
        Vector3 E(int i, float y0)
        {
            float a = i * Mathf.PI * 2 / lados, x = Mathf.Cos(a) * (rx + .15f), z = Mathf.Sin(a) * (rz + .15f);
            return centro + new Vector3(x, y0 < 0 ? Y(z) : y0, z);
        }
        Vector3 medio = centro + Vector3.up * Y(0);
        for (int i = 0; i < lados; i++)
        {
            Tri("tierra", medio, E(i, -1), E(i + 1, -1), Vector3.up);
            Vector3 a0 = E(i, -1), a1 = E(i + 1, -1), b0 = E(i, 0), b1 = E(i + 1, 0);
            Vector3 fuera = ((a0 + a1) / 2 - centro); fuera.y = 0;
            Tri("tierra", a0, b0, b1, fuera); Tri("tierra", a0, b1, a1, fuera);
        }
        var rng = new System.Random(7);
        const float paso = .16f;
        for (float x = -rx; x <= rx; x += paso)
        for (float z = -rz; z <= rz; z += paso)
        {
            float jx = x + (float)rng.NextDouble() * .08f - .04f, jz = z + (float)rng.NextDouble() * .08f - .04f;
            string zona = Zona(jx, jz);
            if (zona == null) continue;
            Vector3 p = centro + new Vector3(jx, Y(jz), jz);
            float h = .22f + (float)rng.NextDouble() * .1f;
            if (rng.Next(6) == 0) Ellipsoid("hoja_flor", p + Vector3.up * h * .45f, new Vector3(.09f, .07f, .09f), Quaternion.Euler(0, rng.Next(360), 0), 2, 5);
            if (zona.StartsWith("cempasuchil"))
            {
                string mat = zona == "cempasuchil" && rng.NextDouble() < .2 ? "cempasuchil_oscuro" : zona;
                Cabeza(p + Vector3.up * h, (Vector3.up + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .5f).normalized, .085f, mat);
            }
            else Ellipsoid(zona, p + Vector3.up * h, new Vector3(.095f, .065f, .095f), Quaternion.Euler(0, rng.Next(360), 0), 2, 6);
        }
        ColliderBox("Campo del casco troyano", centro + Vector3.up * .3f, new Vector3(rx * 1.6f, .6f, rz * 1.6f), 0);
        var vista = Hotspot("Campo del casco troyano", TxtCampoTroyano, centro + Vector3.up * .6f, new Vector3(rx * 2, 1.2f, rz * 2)).gameObject.AddComponent<VistaAerea>();
        vista.centro = centro + Vector3.up * .5f;
        vista.alto = 15f;
    }

    // Asta con la bandera de los Troyanos; el frente mira a la plaza y el asta queda a la izquierda de quien la ve.
    private static void Bandera(Vector3 b)
    {
        const float asta = 9f, largo = 3.3f, alto = 2.2f;
        Vector3 up = Vector3.up, aPlaza = -b; aPlaza.y = 0; aPlaza.Normalize();
        Vector3 e = Vector3.Cross(up, -aPlaza).normalized;
        Block("concreto", b, new Vector3(.7f, .3f, .7f), Quaternion.LookRotation(aPlaza).eulerAngles.y, 1);
        Rod("metal", b + up * .3f, b + up * asta, .055f, 10);
        Sphere("plata", b + up * (asta + .08f), .1f, 5, 8);
        // Ondas suaves a lo largo de la tela, mas marcadas hacia la punta.
        const int n = 14;
        Vector3 P(int i, float y)
        {
            float t = i / (float)n;
            return b + up * y + e * (.06f + t * largo) + aPlaza * Mathf.Sin(t * Mathf.PI * 2.2f) * .16f * t;
        }
        float y1 = asta - .1f, y0 = y1 - alto;
        for (int i = 0; i < n; i++)
        {
            float u0 = i / (float)n, u1 = (i + 1) / (float)n;
            Vector3 a = P(i, y0), c = P(i + 1, y0), d = P(i + 1, y1), f = P(i, y1);
            Vector3 nrm = Vector3.Cross(d - a, c - a).normalized;
            if (Vector3.Dot(nrm, aPlaza) < 0) nrm = -nrm;
            Quad("bandera", a, c, d, f, nrm, new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1));
            Quad("bandera", a, f, d, c, -nrm, new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0));
        }
        ColliderBox("Asta de la bandera", b + up * 1.5f, new Vector3(.7f, 3f, .7f), 0);
        Hotspot("Bandera de los Troyanos", TxtBandera, b + up * (y0 + alto / 2) + e * (largo / 2), new Vector3(largo + .4f, alto + .4f, .6f), Quaternion.LookRotation(aPlaza).eulerAngles.y);
    }

    // ---------- calaveritas ----------

    private static Material EspiralMat(string nombre, Color fondo, Color linea)
    {
        if (Mats.TryGetValue(nombre, out var m)) return m;
        m = Mat(nombre, Color.white, .5f, 0, TexKit.Espiral(fondo, linea));
        m.SetFloat("_Mode", 1);
        m.SetFloat("_Cutoff", .5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
        m.SetOverrideTag("RenderType", "TransparentCutout");
        return m;
    }

    private static void Calaverita(Vector3 p, float yaw, int i)
    {
        string[] ropa = { "tela_morada", "tela_magenta", "tela_turquesa", "tela_naranja", "tela_morada", "tela_magenta" };
        string ojo = i % 2 == 0 ? "espiral_morada" : "espiral_azul";
        var t = Piece("Calaverita " + (i + 1), p, () =>
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);
            Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
            foreach (int sx in new[] { -1, 1 })
            {
                Cylinder("hueso", p + l * sx * .06f, .022f, .02f, .16f, 6, Quaternion.identity, false);
                Ellipsoid("hueso", p + l * sx * .06f + f * .03f + up * .015f, new Vector3(.035f, .02f, .05f), r, 3, 6, .4f);
            }
            Lathe(ropa[i], p + up * .14f, new[] { new Vector2(.15f, 0), new Vector2(.12f, .12f), new Vector2(.09f, .26f), new Vector2(0, .27f) }, 12);
            Lathe("tela_naranja", p + up * .14f, new[] { new Vector2(.155f, .0f), new Vector2(.152f, .03f) }, 12);
            Box("marco_dorado", p + up * .36f + f * .085f, new Vector3(.08f, .05f, .01f), r * Quaternion.Euler(0, 0, 45));
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 hombro = p + up * .38f + l * sx * .09f;
                Vector3 mano = hombro + l * sx * .14f + up * (i % 3 == 0 ? .1f : -.06f) + f * .06f;
                Tube(ropa[i], new[] { hombro, Vector3.Lerp(hombro, mano, .7f) }, .028f, 6);
                Ellipsoid("hueso", mano, new Vector3(.03f, .022f, .03f), r, 3, 6);
            }
            Vector3 h = p + up * .6f;
            Ellipsoid("hueso", h, new Vector3(.22f, .2f, .21f), r, 8, 14);
            Ellipsoid("hueso", h - up * .14f + f * .07f, new Vector3(.11f, .07f, .1f), r, 5, 9);
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 n = (f * .92f + l * sx * .38f).normalized;
                Vector3 o = h + n * .207f + up * .01f;
                Ellipsoid("negro", o - n * .012f, new Vector3(.085f, .095f, .02f), Quaternion.LookRotation(n), 3, 10);
                Vector3 ou = Vector3.Cross(n, up).normalized * .08f, ov = up * .09f;
                Quad(ojo, o - ou - ov, o - ou + ov, o + ou + ov, o + ou - ov, n, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            }
            Ellipsoid("negro", h + f * .2f - up * .06f, new Vector3(.018f, .025f, .01f), r, 3, 5);
            Box("negro", h + f * .155f - up * .15f, new Vector3(.08f, .004f, .004f), r);
            for (int k = -2; k <= 2; k++) Box("negro", h + f * .155f - up * .15f + l * k * .016f, new Vector3(.003f, .024f, .004f), r);
            if (i % 2 == 1)
            {
                foreach (int sx in new[] { -1, 1 }) Ellipsoid("tela_magenta", h + up * .17f + l * (sx * .07f + .08f) - f * .02f, new Vector3(.06f, .04f, .03f), r * Quaternion.Euler(0, 0, sx * 25), 3, 6);
                Sphere("tela_magenta", h + up * .18f + l * .08f - f * .02f, .025f, 3, 5);
            }
            else
                Ellipsoid("tela_turquesa", h + up * .165f, new Vector3(.12f, .03f, .1f), r, 3, 8);
            if (i % 3 == 0) Cabeza(h + up * .2f - l * .1f, (up + f * .3f).normalized, .045f, "cempasuchil");
        });
        var bal = t.gameObject.AddComponent<Balanceo>();
        bal.rebote = .03f; bal.frecuencia = 2.2f; bal.vaiven = 5f;
        Hotspot("Calaverita", TxtCalaveritasFiesta, p + Vector3.up * .45f, new Vector3(.6f, .9f, .6f), yaw);
    }

    // ---------- mariachi de catrines ----------

    private static void Guitarra(Vector3 c, Vector3 frente, Vector3 arriba, float s, string mat, bool guitarron)
    {
        Vector3 lado = Vector3.Cross(arriba, frente).normalized;
        var cuerpo = new List<Vector2>();
        for (int k = 0; k < 40; k++)
        {
            float a = k / 40f * Mathf.PI * 2;
            float y = Mathf.Sin(a), x = Mathf.Cos(a);
            float ancho = y > 0 ? .78f : 1f;
            float cintura = 1 - .18f * Mathf.Exp(-Mathf.Pow((y - .15f) * 3f, 2));
            cuerpo.Add(new Vector2(x * ancho * cintura * .19f * s, (y * .25f + (y > 0 ? .05f : 0)) * s));
        }
        float fondo = (guitarron ? .2f : .09f) * s;
        Extrude(mat, cuerpo, c + frente * fondo / 2, lado, arriba, fondo);
        Cylinder("negro", c + arriba * .07f * s + frente * (fondo / 2 + .001f), .045f * s, .045f * s, .003f, 12, Quaternion.FromToRotation(Vector3.up, frente), true);
        Box("madera_oscura", c + arriba * .5f * s + frente * fondo * .4f, new Vector3(.05f * s, .46f * s, .03f), Quaternion.LookRotation(frente, arriba));
        Box("madera_oscura", c + arriba * .78f * s + frente * fondo * .35f, new Vector3(.07f * s, .12f * s, .03f), Quaternion.LookRotation(frente, arriba));
        Box("negro", c - arriba * .12f * s + frente * (fondo / 2 + .004f), new Vector3(.12f * s, .025f * s, .008f), Quaternion.LookRotation(frente, arriba));
        for (int k = -2; k <= 2; k++) Rod("plata", c - arriba * .12f * s + lado * k * .008f * s + frente * (fondo / 2 + .012f), c + arriba * .72f * s + lado * k * .005f * s + frente * fondo * .55f, .0012f, 3);
    }

    private static void Musico(Vector3 p, float yaw, int instrumento, int i)
    {
        var t = Piece("Mariachi catrín " + (i + 1), p, () =>
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);
            Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
            foreach (int sx in new[] { -1, 1 })
            {
                Ellipsoid("negro", p + l * sx * .09f + f * .05f + up * .03f, new Vector3(.05f, .04f, .1f), r, 4, 7, .5f);
                Cylinder("tela_negra", p + l * sx * .09f + up * .04f, .058f, .066f, .84f, 10, Quaternion.identity, false);
                for (int k = 0; k < 7; k++) Sphere("plata", p + l * sx * .155f + up * (.2f + k * .1f), .011f, 2, 4);
            }
            Lathe("tela_negra", p + up * .84f, new[] { new Vector2(.14f, 0), new Vector2(.15f, .2f), new Vector2(.165f, .44f), new Vector2(.12f, .57f), new Vector2(0, .58f) }, 14);
            Box("tela_negra", p + up * 1.2f + f * .02f, new Vector3(.34f, .2f, .26f), r);
            Box("tela_blanca", p + up * 1.3f + f * .125f, new Vector3(.08f, .14f, .02f), r);
            foreach (int sx in new[] { -1, 1 })
            {
                Ellipsoid("tela_naranja", p + up * 1.38f + f * .14f + l * sx * .04f, new Vector3(.05f, .03f, .015f), r * Quaternion.Euler(0, 0, sx * 15), 3, 6);
                for (int k = 0; k < 4; k++) Sphere("plata", p + up * (1.12f + k * .06f) + f * .14f + l * sx * .1f, .01f, 2, 4);
            }
            Cylinder("hueso", p + up * 1.4f, .042f, .04f, .11f, 8, Quaternion.identity, false);
            Vector3 h = p + up * 1.59f + f * .01f;
            Calavera(h, f, l, i % 2 == 0);
            // Sombrero de charro: ala ancha levantada en la orilla y copa alta.
            Lathe("tela_negra", h + up * .07f, new[] { new Vector2(.42f, .07f), new Vector2(.4f, .03f), new Vector2(.3f, 0), new Vector2(.16f, .02f), new Vector2(.12f, .2f), new Vector2(.06f, .25f), new Vector2(0, .25f) }, 22);
            Lathe("plata", h + up * .07f, new[] { new Vector2(.425f, .065f), new Vector2(.415f, .085f) }, 22);
            Lathe("plata", h + up * .07f, new[] { new Vector2(.155f, .03f), new Vector2(.142f, .07f) }, 16);
            Vector3 c = p + up * 1.05f + f * .2f;
            Vector3 hombroD = p + up * 1.36f + l * .17f, hombroI = p + up * 1.36f - l * .17f;
            switch (instrumento)
            {
                case 0:
                case 1:
                case 2:
                {
                    float s = instrumento == 0 ? 1.55f : instrumento == 1 ? .9f : 1.05f;
                    Vector3 arriba = (up * .45f - l * .9f).normalized;
                    Guitarra(c, f, arriba, s, instrumento == 1 ? "madera_guitarra" : instrumento == 0 ? "madera_oscura" : "madera_guitarra", instrumento == 0);
                    Vector3 manoD = c + l * .06f + f * .12f, manoI = c + arriba * .62f * s + f * .06f;
                    Tube("tela_negra", new[] { hombroD, hombroD - up * .2f + l * .08f + f * .05f, manoD }, .04f, 6);
                    Tube("tela_negra", new[] { hombroI, hombroI - up * .15f + f * .18f, manoI }, .04f, 6);
                    Ellipsoid("hueso", manoD, Vector3.one * .03f, r, 3, 5);
                    Ellipsoid("hueso", manoI, Vector3.one * .03f, r, 3, 5);
                    break;
                }
                case 3:
                {
                    // Violin al hombro con su arco.
                    Vector3 v = p + up * 1.38f + f * .14f - l * .12f;
                    Vector3 eje = (f * .8f - l * .45f + up * .1f).normalized;
                    Ellipsoid("madera_guitarra", v, new Vector3(.1f, .035f, .17f), Quaternion.LookRotation(eje, up), 4, 10);
                    Box("madera_oscura", v + eje * .25f, new Vector3(.025f, .02f, .2f), Quaternion.LookRotation(eje, up));
                    Vector3 manoI = v + eje * .3f, manoD = v + l * .3f + f * .05f - up * .05f;
                    Rod("madera_oscura", manoD - l * .2f + up * .06f, manoD + l * .35f - up * .1f, .005f, 3);
                    Tube("tela_negra", new[] { hombroI, hombroI + f * .2f - up * .08f, manoI }, .04f, 6);
                    Tube("tela_negra", new[] { hombroD, hombroD - up * .2f + l * .12f, manoD }, .04f, 6);
                    Ellipsoid("hueso", manoD, Vector3.one * .03f, r, 3, 5);
                    Ellipsoid("hueso", manoI, Vector3.one * .03f, r, 3, 5);
                    break;
                }
                default:
                {
                    // Trompeta dorada levantada.
                    Vector3 boca = p + up * 1.5f + f * .2f;
                    Vector3 eje = (f + up * .25f).normalized;
                    Cono("marco_dorado", boca, boca + eje * .32f, .011f, .013f, 6);
                    Lathe("marco_dorado", boca + eje * .32f, new[] { new Vector2(.015f, 0), new Vector2(.03f, .06f), new Vector2(.065f, .12f) }, 14);
                    Box("marco_dorado", boca + eje * .14f - up * .04f, new Vector3(.04f, .05f, .12f), Quaternion.LookRotation(eje, up));
                    Vector3 manoD = boca + eje * .12f - up * .05f + l * .02f, manoI = boca + eje * .2f - up * .06f - l * .02f;
                    Tube("tela_negra", new[] { hombroD, hombroD - up * .18f + f * .1f + l * .05f, manoD }, .04f, 6);
                    Tube("tela_negra", new[] { hombroI, hombroI - up * .18f + f * .12f - l * .05f, manoI }, .04f, 6);
                    Ellipsoid("hueso", manoD, Vector3.one * .03f, r, 3, 5);
                    Ellipsoid("hueso", manoI, Vector3.one * .03f, r, 3, 5);
                    break;
                }
            }
        });
        var bal = t.gameObject.AddComponent<Balanceo>();
        bal.rebote = .015f; bal.frecuencia = 2.6f; bal.vaiven = 2.5f;
    }

    private static void Mariachi(Vector3 centro, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 l = r * Vector3.right, f = r * Vector3.forward;
        Block("cantera", centro - f * .3f, new Vector3(6.4f, .18f, 2.2f), yaw, 1);
        int[] orden = { 4, 3, 0, 2, 1 };
        for (int i = 0; i < orden.Length; i++)
        {
            float x = (i - 2) * 1.15f;
            Vector3 p = centro + l * x - f * (.5f - .25f * Mathf.Abs(i - 2)) + Vector3.up * .18f;
            Musico(p, yaw - x * 6, orden[i], i);
        }
        var audio = new GameObject("Mariachi | música original").AddComponent<AudioSource>();
        audio.transform.SetParent(currentGroup);
        audio.transform.position = centro + Vector3.up * 1.4f;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/mariachi.wav");
        audio.loop = true;
        audio.playOnAwake = true;
        audio.volume = .55f;
        audio.spatialBlend = 1;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 3;
        audio.maxDistance = 30;
        ColliderBox("Mariachi", centro - f * .3f + Vector3.up * 1f, new Vector3(6.4f, 2f, 2.2f), yaw);
        Hotspot("Mariachi de catrines", TxtMariachi, centro + Vector3.up * 1.1f, new Vector3(6.4f, 2.2f, 2.4f), yaw);
    }

    // ---------- danzantes ----------

    // Danzante conchero a media danza: penacho doble con diadema y espejo, pectoral, collares, maxtlatl bordado,
    // ayoyotes en los tobillos, huaraches, sonaja, chimalli con plumas y pintura en la cara.
    private static void Danzante(Vector3 p, float yaw, int i)
    {
        string[] plumas = { "pluma_verde", "tela_roja", "tela_turquesa", "tela_naranja", "pluma_verde", "tela_morada", "tela_naranja", "tela_turquesa", "tela_roja", "pluma_verde", "tela_turquesa" };
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        string piel = "piel" + (i % 3 + 1), traje = i % 2 == 0 ? "tela_roja" : "tela_turquesa", borde = i % 2 == 0 ? "tela_turquesa" : "tela_roja";
        int levanta = i % 2 == 0 ? 1 : -1;
        Vector3 cad = p + up * .92f;
        foreach (int sx in new[] { -1, 1 })
        {
            bool arriba = sx == levanta;
            Vector3 cadera = cad + l * sx * .085f;
            Vector3 rodilla = arriba ? cadera + f * .3f - up * .22f + l * sx * .03f : p + up * .49f + l * sx * .11f + f * .02f;
            Vector3 tobillo = arriba ? rodilla - up * .36f + f * .06f : p + up * .08f + l * sx * .12f;
            Miembro(piel, new[] { cadera - up * .05f, rodilla, tobillo }, t => Perfil(t, 0, .078f, .25f, .066f, .5f, .046f, .62f, .053f, .9f, .034f, 1, .032f), f, 1, 12, 18);
            // Ayoyotes: dos vueltas de semillas cafes sobre el tobillo.
            for (int vuelta = 0; vuelta < 2; vuelta++)
                for (int q = 0; q < 10; q++)
                {
                    float a = (q / 10f + vuelta * .05f) * Mathf.PI * 2;
                    Ellipsoid("madera_oscura", tobillo + up * (.035f + vuelta * .035f) + (l * Mathf.Cos(a) + f * Mathf.Sin(a)) * .052f, new Vector3(.016f, .022f, .016f), r, 3, 5);
                }
            Vector3 pie = tobillo - up * .045f + f * .05f;
            Ellipsoid(piel, pie, new Vector3(.042f, .03f, .1f), r, 4, 7, .35f);
            Box("cuero", pie - up * .022f, new Vector3(.09f, .014f, .22f), r);
            foreach (float z in new[] { -.02f, .04f }) Box("cuero", pie + f * z + up * .015f, new Vector3(.088f, .01f, .014f), r);
        }
        // Maxtlatl: panel al frente y atras con orilla y grecas.
        Tronco(traje, cad, r, false, -.13f, .11f, .004f);
        foreach (int lado in new[] { 1, -1 })
        {
            Vector3 c = cad - up * .2f + f * lado * .11f;
            Box(traje, c, new Vector3(.2f, .4f, .014f), r * Quaternion.Euler(lado * 6, 0, 0));
            Box(borde, c - up * .18f + f * lado * .006f, new Vector3(.2f, .04f, .006f), r * Quaternion.Euler(lado * 6, 0, 0));
            for (int q = 0; q < 4; q++)
                Box("marco_dorado", c + up * (.1f - q * .07f) + f * lado * .008f + l * ((q % 2) * .05f - .025f), new Vector3(.04f, .025f, .004f), r * Quaternion.Euler(lado * 6, 0, 45));
        }
        Box("marco_dorado", cad + up * .08f + f * .1f, new Vector3(.24f, .04f, .02f), r);
        // Torso, pectoral redondo, collares de cuentas y brazaletes.
        Tronco(piel, cad, r, false, .08f, .53f, 0);
        Quaternion pec = r * Quaternion.Euler(90, 0, 0);
        Cylinder("marco_dorado", cad + up * .33f + f * .1f, .075f, .075f, .012f, 20, pec, true, true);
        Cylinder("tela_turquesa", cad + up * .33f + f * .111f, .055f, .055f, .006f, 20, pec, true, false);
        Cylinder("vidrio_oscuro", cad + up * .33f + f * .116f, .025f, .025f, .004f, 14, pec, true, false);
        for (int fila = 0; fila < 2; fila++)
            for (int q = 0; q < 15; q++)
            {
                float a = (-70 + q * 10) * Mathf.Deg2Rad;
                Vector3 d = f * Mathf.Cos(a) + l * Mathf.Sin(a);
                Sphere(fila == 0 ? "tela_turquesa" : "tela_roja", cad + up * (.47f - fila * .035f - .04f * Mathf.Cos(a)) + d * (.11f + fila * .02f), .011f, 3, 4);
            }
        Vector3 manoSonaja = Vector3.zero, manoEscudo = Vector3.zero;
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 hombro = cad + up * .425f + l * sx * .155f;
            Vector3 codo = hombro + l * sx * .2f + up * (sx > 0 ? .06f : -.08f) + f * .05f;
            Vector3 mano = codo + l * sx * .13f + up * (sx > 0 ? .18f : .02f) + f * .1f;
            Miembro(piel, new[] { hombro - l * sx * .025f, codo, mano }, t => Perfil(t, 0, .05f, .15f, .051f, .45f, .04f, .5f, .037f, .62f, .039f, 1, .03f), f, 1, 12, 16);
            Lathe("marco_dorado", Vector3.Lerp(hombro, codo, .35f) - up * .02f, new[] { new Vector2(.052f, 0), new Vector2(.052f, .035f) }, 10);
            Lathe(borde, Vector3.Lerp(codo, mano, .7f) - up * .015f, new[] { new Vector2(.036f, 0), new Vector2(.036f, .03f) }, 10);
            Vector3 dm = (mano - codo).normalized;
            Mano(piel, mano - dm * .015f, dm, Vector3.Cross(dm, f).normalized, f);
            if (sx > 0) manoSonaja = mano; else manoEscudo = mano;
        }
        // Sonaja de guaje en la derecha y chimalli con plumas en la izquierda.
        Rod("madera_oscura", manoSonaja - up * .04f, manoSonaja + up * .12f, .009f, 5);
        Ellipsoid("barro_brunido", manoSonaja + up * .17f, new Vector3(.045f, .055f, .045f), r, 5, 8);
        for (int q = 0; q < 3; q++) Ellipsoid(plumas[q + 1], manoSonaja + up * (.24f + q * .02f) + l * (q - 1) * .02f, new Vector3(.012f, .04f, .006f), r * Quaternion.Euler(0, 0, (q - 1) * 25), 2, 5);
        Vector3 ce = manoEscudo + f * .05f - l * .03f;
        Quaternion qe = Quaternion.LookRotation(f - l * .6f) * Quaternion.Euler(90, 0, 0);
        Cylinder(traje, ce, .14f, .14f, .015f, 20, qe, true, true);
        Cylinder("marco_dorado", ce - (qe * Vector3.up) * .004f, .152f, .152f, .01f, 20, qe, true, true);
        Cylinder(borde, ce + (qe * Vector3.up) * .015f, .07f, .07f, .004f, 16, qe, true, false);
        for (int q = 0; q < 9; q++)
        {
            float a = (200 + q * 17.5f) * Mathf.Deg2Rad;
            Vector3 eu = qe * Vector3.right, ev = qe * Vector3.forward;
            Vector3 d = (eu * Mathf.Cos(a) + ev * Mathf.Sin(a)).normalized;
            Ellipsoid(plumas[q], ce + d * .19f, new Vector3(.018f, .006f, .06f), Quaternion.LookRotation(d, qe * Vector3.up), 3, 5);
        }
        // Cuello, cabeza con pintura roja y negra, cabello largo y penacho doble.
        Vector3 h = cad + up * .665f;
        Miembro(piel, new[] { cad + up * .47f - f * .012f, h - up * .07f - f * .012f }, t => Mathf.Lerp(.047f, .04f, t), f, 1.05f, 12, 5);
        CabezaHumana(h, r, piel, "pelo", false, new[] { "tela_roja" });
        foreach (int sx in new[] { -1, 1 })
        {
            Box("tela_roja", h + f * .066f + l * sx * .048f - up * .02f, new Vector3(.03f, .006f, .004f), r * Quaternion.Euler(0, sx * 30, 0));
            Box("negro", h + f * .064f + l * sx * .05f - up * .032f, new Vector3(.028f, .005f, .004f), r * Quaternion.Euler(0, sx * 30, 0));
        }
        Ellipsoid("pelo", h + up * .03f - f * .012f, new Vector3(.09f, .09f, .102f), r * Quaternion.Euler(-12, 0, 0), 7, 12, .42f);
        Ellipsoid("pelo", h - up * .1f - f * .065f, new Vector3(.085f, .17f, .045f), r, 6, 10);
        Lathe("marco_dorado", h + up * .035f, new[] { new Vector2(.1f, 0), new Vector2(.102f, .045f) }, 16);
        Cylinder("vidrio_oscuro", h + up * .058f + f * .1f, .02f, .02f, .006f, 12, r * Quaternion.Euler(90, 0, 0), true);
        for (int q = 0; q < 8; q++)
        {
            float a = (-70 + q * 20) * Mathf.Deg2Rad;
            Sphere(q % 2 == 0 ? "tela_turquesa" : "tela_roja", h + up * .058f + (f * Mathf.Cos(a) + l * Mathf.Sin(a)) * .104f, .008f, 2, 4);
        }
        for (int fila = 0; fila < 2; fila++)
            for (int q = 0; q < plumas.Length; q++)
            {
                float a = Mathf.Lerp(-85, 85, q / (plumas.Length - 1f)) * Mathf.Deg2Rad;
                Vector3 dir = (up * Mathf.Cos(a) + l * Mathf.Sin(a) - f * (.2f + fila * .15f)).normalized;
                float largo = (fila == 0 ? .62f : .45f) - Mathf.Abs(q - 5) * .03f;
                string color = fila == 0 ? plumas[q] : plumas[(q + 3) % plumas.Length];
                Vector3 base0 = h + up * .07f - f * (.06f + fila * .02f);
                Ellipsoid(color, base0 + dir * largo * .55f, new Vector3(.05f, .01f, largo * .55f), Quaternion.LookRotation(dir, f), 3, 7);
                if (fila == 0) Ellipsoid("tela_blanca", base0 + dir * largo * 1.07f, new Vector3(.03f, .008f, .05f), Quaternion.LookRotation(dir, f), 2, 5);
            }
    }

    private static void Danzantes(Vector3 centro)
    {
        Cylinder("madera_oscura", centro, .24f, .26f, .7f, 14, Quaternion.identity, true);
        Lathe("tela_turquesa", centro, new[] { new Vector2(.265f, .2f), new Vector2(.255f, .35f) }, 14);
        Ellipsoid("cuero", centro + Vector3.up * .7f, new Vector3(.24f, .02f, .24f), Quaternion.identity, 2, 12);
        Vector3 sahumador = centro + Vector3.right * .55f;
        Lathe("barro", sahumador, new[] { new Vector2(0, 0), new Vector2(.06f, 0), new Vector2(.04f, .18f), new Vector2(.11f, .26f), new Vector2(0, .25f) }, 10);
        for (int k = 0; k < 14; k++)
            Sprite("humo", sahumador + Vector3.up * .3f, Vector2.one * Rnd(.06f, .12f), Color.white, Rnd(), new Vector4(Rnd(.22f, .38f), Rnd(2.6f, 4.2f), Rnd(.35f, .8f), Rnd(3f, 5f)));
        var circulo = Piece("Danzantes | círculo", centro, () =>
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * 360;
                Vector3 p = centro + Quaternion.Euler(0, a, 0) * Vector3.forward * 2.1f;
                Danzante(p, a + 90, i);
            }
        });
        var bal = circulo.gameObject.AddComponent<Balanceo>();
        bal.rebote = .06f; bal.frecuencia = 3.2f; bal.vaiven = 0; bal.giro = 14;
        ColliderBox("Huéhuetl", centro + Vector3.up * .4f, new Vector3(.6f, .8f, .6f));
        var tambor = new GameObject("Danzantes | huéhuetl").AddComponent<AudioSource>();
        tambor.transform.SetParent(currentGroup);
        tambor.transform.position = centro + Vector3.up * .7f;
        tambor.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/tambor.wav");
        tambor.loop = true;
        tambor.playOnAwake = true;
        tambor.volume = .7f;
        tambor.spatialBlend = 1;
        tambor.rolloffMode = AudioRolloffMode.Linear;
        tambor.minDistance = 3;
        tambor.maxDistance = 26;
        Hotspot("Danzantes", TxtDanzantes, centro + Vector3.up * 1f, new Vector3(5f, 2.2f, 5f));
    }

    // ---------- jaguares con huesos ----------

    private static void Jaguar(Vector3 p, float yaw, int variante)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right, up = Vector3.up;
        string piel = "jaguar_" + variante;
        const float s = 1.15f;
        Block("cantera", p, new Vector3(1.1f, .25f, 1.9f) * s, yaw, 1);
        Vector3 b = p + up * .25f * s;
        Vector3 L(float x, float y, float z) => b + (l * x + up * y + f * z) * s;
        Ellipsoid(piel, L(0, .55f, -.05f), new Vector3(.22f, .21f, .52f) * s, r, 8, 14);
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
        {
            Vector3 hip = L(sx * .13f, .45f, sz * .3f - .05f), pie = L(sx * .14f, .05f, sz * .34f);
            Cono(piel, hip, pie, .08f * s, .05f * s, 8);
            Ellipsoid("hueso", pie + up * .02f * s + f * .03f * s, new Vector3(.06f, .035f, .08f) * s, r, 3, 7, .4f);
        }
        Cono(piel, L(0, .62f, .38f), L(0, .78f, .55f), .15f * s, .12f * s, 10);
        Vector3 h = L(0, .82f, .66f);
        Ellipsoid(piel, h, new Vector3(.17f, .15f, .16f) * s, r, 7, 12);
        // Mascara de calavera pintada en la cara.
        Ellipsoid("hueso", h + f * .07f * s, new Vector3(.14f, .12f, .11f) * s, r, 6, 10);
        Ellipsoid("hueso", L(0, .76f, .8f), new Vector3(.08f, .06f, .07f) * s, r, 4, 8);
        Ellipsoid("negro", L(0, .79f, .86f), new Vector3(.03f, .02f, .02f) * s, r, 3, 5);
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 ojo = L(sx * .065f, .86f, .78f);
            Ellipsoid("negro", ojo, new Vector3(.045f, .04f, .025f) * s, r, 4, 7);
            Ellipsoid("ambar", ojo + f * .012f * s, new Vector3(.02f, .02f, .015f) * s, r, 3, 5);
            Ellipsoid(piel, L(sx * .11f, .97f, .62f), new Vector3(.05f, .05f, .025f) * s, r, 4, 7);
            Ellipsoid("tela_magenta", L(sx * .11f, .97f, .635f), new Vector3(.03f, .03f, .012f) * s, r, 3, 5);
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2;
                Ellipsoid(k % 2 == 0 ? "tela_turquesa" : "tela_naranja", ojo + (l * Mathf.Cos(a) + up * Mathf.Sin(a)) * .06f * s - f * .01f * s, new Vector3(.012f, .012f, .006f) * s, r, 2, 4);
            }
        }
        for (int k = -2; k <= 2; k++) Box("negro", L(k * .016f, .72f, .865f), new Vector3(.003f, .03f, .004f) * s, r);
        Cabeza(L(0, .97f, .7f), (up + f * .4f).normalized, .05f * s, "cempasuchil");
        var cola = new[] { L(0, .58f, -.55f), L(0, .45f, -.85f), L(.15f, .3f, -1.05f), L(.32f, .35f, -1.1f), L(.38f, .55f, -1f) };
        Tube(piel, cola, .045f * s, 8);
        for (int k = 1; k < cola.Length; k++) Sphere("hueso", cola[k], .05f * s, 3, 6);
        ColliderBox("Jaguar", p + up * .6f * s, new Vector3(1.1f, 1.2f, 1.9f) * s, yaw);
        Hotspot("Jaguar de fiesta", TxtJaguar, b + up * .6f * s, new Vector3(1f, 1.3f, 2f) * s, yaw);
    }
}
