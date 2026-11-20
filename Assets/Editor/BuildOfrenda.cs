using System.Collections.Generic;
using MemoriaViva;
using UnityEditor;
using UnityEngine;
using static GeoKit;

// Ofrenda de los Cuatro Rumbos: siete niveles que miran a los cuatro puntos de la explanada.
// Cada cara es un elemento: norte fuego, oriente agua, poniente tierra, sur viento.
public static partial class BuildMemoriaViva
{
    private const int Niveles = 7;
    private const float NivelH = .38f, Base0 = 6.3f, Paso = .74f, Plinto = .12f;
    private const float ArcoR = 5.0f, ArcoPierna = 2.6f, ArcoAlto = 3.3f;
    private const float PedestalR = 4.05f, PosteR = 9.3f * E;
    private static readonly int[] FranjaNivel = { 3, 1, 0, 4, 3, 1, 6 };
    // Atlas: 0 rosa, 1 naranja, 2 turquesa, 3 morado, 4 amarillo, 5 verde, 6 rosa claro, 7 rojo.
    private static readonly int[][] ParNivel = { new[] { 3, 1 }, new[] { 1, 0 }, new[] { 0, 3 }, new[] { 4, 1 }, new[] { 3, 0 }, new[] { 1, 4 }, new[] { 6, 3 } };
    private static readonly int[] Paleta = { 1, 0, 3, 4 };
    private static float Side(int i) => Base0 - Paso * i;
    private static float Top(int i) => Plinto + NivelH * (i + 1);
    private static float Tread(int i) => (Side(i) + Side(i + 1)) / 4f;
    private static float Half(int i) => Side(i + 1) / 2f - .1f;
    private static float ApexY => ArcoPierna + ArcoAlto;
    private static readonly Vector3 Up = Vector3.up;

    private static readonly Vector3[] Caras = { Vector3.forward, Vector3.right, Vector3.left, Vector3.back };
    private static readonly float[] CaraYaw = { 0, 90, 270, 180 };
    private static Transform hotspotRoot, ofrendaRoot;
    private static readonly List<Transform> fuego = new List<Transform>(), agua = new List<Transform>(), tierra = new List<Transform>(), viento = new List<Transform>();
    private static readonly List<Transform> tiras = new List<Transform>();
    private static readonly List<Vector3> ejes = new List<Vector3>();
    private static readonly List<Renderer> retratos = new List<Renderer>();
    private static readonly List<TextMesh> progreso = new List<TextMesh>();
    private static GameObject finaleObject;
    private static readonly string[] Flor = { "cempasuchil", "cempasuchil_oscuro" };

    private static Vector3 Lat(int face) => Vector3.Cross(Up, Caras[face]);
    private static Vector3 OnTread(int face, int level, float x) => Caras[face] * Tread(level) + Lat(face) * x + Up * Top(level);
    private static string AnyFlor() => Flor[random.Next(2)];

    private static void Ofrenda()
    {
        foreach (var l in new[] { fuego, agua, tierra, viento, tiras }) l.Clear();
        ejes.Clear(); retratos.Clear(); fotosPorMarco.Clear(); progreso.Clear();
        ofrendaRoot = Group("10 | Ofrenda de los Cuatro Rumbos");
        hotspotRoot = new GameObject("Significados | puntos interactivos").transform;
        hotspotRoot.SetParent(ofrendaRoot);

        Estructura();
        Arcos();
        Petalos();
        Rituales();
        Velas();
        CaraAgua();
        CaraTierra();
        CaraViento();
        Retratos();
        PerroColima(new Vector3(-1.75f, 0, 4.35f), 0);
        Dosel();
        Final();
        Senales();
        Visitantes();
        FlushPending();
        Director();
    }

    // ---------- textos de cada elemento ----------

    private const string TxtNiveles = "Esta ofrenda tiene siete niveles. No hay una regla fija sobre cuántos debe tener una ofrenda; una interpretación popular relaciona los siete con los pasos que recorría el alma en el Mictlán antes del descanso. Aquí suben desde los cuatro lados para que la ofrenda se recorra desde cualquier punto de la explanada. (Interpretación popular, sin fuente institucional.)";
    private const string TxtVelas = "La flama de velas y veladoras significa la luz, la fe y la esperanza, y guía a las ánimas a su antiguo hogar. Cada vela puede representar a un difunto. Según el INPI, el copal es de origen indígena y las velas llegaron después. (Fuente: INPI, gob.mx)";
    private const string TxtCirios = "Los cirios de lo alto se mantienen encendidos durante la celebración. Su luz ilumina el camino de las ánimas a su antigua morada. (Fuente: Profeco, gob.mx)";
    private const string TxtAgua = "El agua es la fuente de la vida. Se ofrece a las ánimas para que mitiguen su sed después del largo recorrido y fortalezcan su regreso. (Fuente: INPI, gob.mx)";
    private const string TxtSal = "La sal es el elemento de purificación: sirve para que el cuerpo no se corrompa en su viaje de ida y vuelta para el siguiente año. (Fuente: INPI, gob.mx)";
    private const string TxtPan = "El pan es el ofrecimiento fraternal. Llegó a la ofrenda con la tradición católica y hoy es uno de sus elementos más reconocibles. (Fuente: INPI, gob.mx)";
    private const string TxtFruta = "Naranjas, tejocotes, guayabas y plátanos: la comida y los frutos de temporada se ponen para festejar a los difuntos que visitan. (Fuente: Profeco, gob.mx)";
    private const string TxtComida = "La buena comida tiene por objeto deleitar al ánima. El mole es uno de los platillos favoritos y se acompaña con chocolate de agua y tamales. (Fuente: INPI, gob.mx)";
    private const string TxtCalaveritas = "Las calaveras representan la réplica de los cráneos humanos y recuerdan que la muerte siempre está presente. Se acostumbra escribir en la frente el nombre de quien se recuerda. (Fuente: Profeco e INPI, gob.mx)";
    private const string TxtCana = "Las cañas se relacionan con el tzompantli: en ellas se ensartan los golletes, panes en forma de rueda que representan cráneos, como las varas donde se colocaban. (Fuente: INPI y Profeco, gob.mx)";
    private const string TxtPapel = "El papel picado representa al aire y añade un toque festivo a la ofrenda. Esta es una ofrenda alternativa: además de calaveras y flores, cada bandera lleva cortadas en código binario ASCII las letras FIF y UAQ. (Fuente: Profeco, gob.mx)";
    private const string TxtArco = "El arco de carrizo adornado con flores es común a las ofrendas de muchas regiones de México. Popularmente se interpreta como la entrada de los difuntos al mundo de los vivos. Aquí son dos arcos cruzados que forman cuatro entradas, una por cada rumbo. (Fuente: INPI, El camino del cempasúchil; la lectura como entrada es popular.)";
    private const string TxtCamino = "Los pétalos de cempasúchil deshojados trazan el camino de color y olor que guía a las ánimas a la ofrenda. Su nombre en náhuatl significa veinte flor. Aquí el camino forma una cruz hacia los cuatro rumbos. (Fuente: INPI, gob.mx)";
    private const string TxtRamos = "Las flores son símbolo de la festividad. El cempasúchil guía a las ánimas con su color, que representa al sol. La flor de nube y el alhelí significan pureza y ternura y acompañan a las ánimas de los niños. El terciopelo, de color morado rojizo, es otra flor tradicional de la temporada. (Fuente: INPI y Profeco, gob.mx)";
    private const string TxtPetate = "El petate sirve como cama, mesa o mortaja: permite que las ánimas descansen y funciona como mantel para los alimentos. (Fuente: INPI, gob.mx)";
    private const string TxtXolo = "El xoloitzcuintle ayuda a las almas a cruzar el río Chiconauhuapan, último paso hacia el Mictlán. Lleva un collar de cempasúchil para guiarlas. Es una raza sin pelo originaria de México y la Ciudad de México lo declaró patrimonio cultural en 2016. (Fuente: INPI, gob.mx; Wikipedia en español)";
    private const string TxtCruz = "La cruz llegó a la ofrenda con la tradición católica. En muchos altares también se hace una cruz de ceniza para que el ánima expíe sus culpas pendientes. (Fuente: INPI, gob.mx)";
    private const string TxtRetratos = "En lo más alto, Juan Gabriel (1950-2016) mira a los cuatro rumbos. El retrato indica a quién está dedicada la ofrenda y sugiere el ánima que la visitará. (Fuente: INPI, gob.mx)";

    // Archivo en Resources/Fotos, nombre en la placa y significado. El primero va en lo más alto.
    private static readonly (string foto, string nombre, string texto)[] Personas =
    {
        ("juan-gabriel", "Juan Gabriel", "Alberto Aguilera Valadez nació en Parácuaro, Michoacán. Cantautor, compositor y actor, es uno de los artistas más influyentes de la música en español. Ocupa el lugar más alto de la ofrenda. (Fuente: Wikipedia en español)"),
        ("josefa-ortiz", "Josefa Ortiz de Domínguez\n1768 - 1829", "La Corregidora de Querétaro. Fue de las primeras en la conspiración de Querétaro y pieza clave para el inicio de la Independencia de México. (Fuente: Wikipedia en español)"),
        ("frida-kahlo", "Frida Kahlo\n1907 - 1954", "Pintora nacida en Coyoacán. Realizó unas 150 obras, sobre todo autorretratos, y es un ícono de la cultura mexicana. (Fuente: Wikipedia en español)"),
        ("jose-jose", "José José\n1948 - 2019", "José Rómulo Sosa Ortiz, el Príncipe de la Canción. Cantante de balada romántica nacido en la Ciudad de México. (Fuente: Wikipedia en español)"),
        ("chavela-vargas", "Chavela Vargas\n1919 - 2012", "Cantante nacida en Costa Rica y nacionalizada mexicana; su voz marcó la canción ranchera. (Fuente: Wikipedia en español)"),
        ("vicente-fernandez", "Vicente Fernández\n1940 - 2021", "Chente, el Charro de Huentitán. Cantante y actor de música ranchera, ganó dos premios Grammy y ocho Grammy Latinos. (Fuente: Wikipedia en español)"),
        ("gonzalez-camarena", "Guillermo González Camarena\n1917 - 1965", "Ingeniero e inventor de Guadalajara. Patentó un sistema de televisión a color y en 1946 hizo la primera transmisión en color desde su laboratorio. Representa a la tecnología, el campo de esta facultad. (Fuente: Wikipedia en español)"),
        ("cantinflas", "Cantinflas\n1911 - 1993", "Mario Moreno, actor y comediante de la Ciudad de México. Su forma de hablar dio origen al verbo cantinflear. (Fuente: Wikipedia en español)"),
        ("maria-felix", "María Félix\n1914 - 2002", "La Doña. Actriz nacida en Álamos, Sonora, figura de la Época de Oro del cine mexicano. (Fuente: Wikipedia en español)"),
        ("homero-gomez", "Homero Gómez González\n1970 - 2020", "Ingeniero agrónomo de Chapingo y defensor de la mariposa monarca. Dirigió la reserva El Rosario en Ocampo, Michoacán, restauró el bosque dañado por la tala clandestina y sumó a los campesinos a cuidarlo. Las monarcas de esta ofrenda lo recuerdan. (Fuente: Wikipedia en español)"),
        ("pedro-infante", "Pedro Infante\n1917 - 1957", "Actor y cantante nacido en Mazatlán, figura clave de la Época de Oro del cine mexicano. (Fuente: Wikipedia en español)"),
        ("chespirito", "Chespirito\n1929 - 2014", "Roberto Gómez Bolaños, escritor y actor, creador de El Chavo del 8 y El Chapulín Colorado. (Fuente: Wikipedia en español)"),
        ("diego-rivera", "Diego Rivera\n1886 - 1957", "Muralista nacido en Guanajuato. En Sueño de una tarde dominical en la Alameda Central pintó a La Catrina y le dio ese nombre. (Fuente: Wikipedia en español)"),
        ("sor-juana", "Sor Juana Inés de la Cruz\n1648 - 1695", "Poeta y monja de la Nueva España, una de las grandes escritoras en lengua española. (Fuente: Wikipedia en español)"),
        ("jose-guadalupe-posada", "José Guadalupe Posada\n1852 - 1913", "Grabador de Aguascalientes, creador de La Calavera Garbancera, la figura que hoy conocemos como La Catrina. (Fuente: Wikipedia en español)"),
        ("jorge-negrete", "Jorge Negrete\n1911 - 1953", "El Charro Cantor. Cantante y actor nacido en Guanajuato, figura de la Época de Oro del cine mexicano. (Fuente: Wikipedia en español)"),
        ("octavio-ocana", "Octavio Ocaña\n1998 - 2021", "Actor y comediante. Es recordado por interpretar a Benito Rivers en la serie Vecinos. (Fuente: Wikipedia en español)"),
        ("ramon-valdes", "Ramón Valdés\n1924 - 1988", "Actor y comediante, recordado sobre todo como Don Ramón en El Chavo del 8; se le considera uno de los mejores comediantes de México. (Fuente: Wikipedia en español)"),
        ("ruben-aguirre", "Rubén Aguirre\n1934 - 2016", "Actor y comediante, conocido por interpretar al profesor Jirafales en El Chavo del 8. (Fuente: Wikipedia en español)"),
        ("tin-tan", "Tin Tan\n1915 - 1973", "Germán Valdés, actor, cantante y comediante mexicano. (Fuente: Wikipedia en español)"),
        ("silvia-pinal", "Silvia Pinal\n1931 - 2024", "Actriz de radio, cine, teatro y televisión, considerada pionera de la comedia musical y de la televisión mexicana. (Fuente: Wikipedia en español)"),
        ("lopez-tarso", "Ignacio López Tarso\n1925 - 2023", "Ignacio López López, actor y político mexicano. (Fuente: Wikipedia en español)"),
        ("chabelo", "Chabelo\n1935 - 2023", "Javier López Rodríguez, actor, comediante y presentador. Condujo En Familia con Chabelo durante cuarenta y ocho años. (Fuente: Wikipedia en español)"),
        ("katy-jurado", "Katy Jurado\n1924 - 2002", "Actriz que empezó en el cine mexicano en 1943 y desde 1951 hizo carrera en Hollywood. (Fuente: Wikipedia en español)"),
        ("chihuahua", "Chihuahua\nNuestras mascotas", "El chihuahua es una raza de perro originaria de México, una de las más antiguas del continente americano y la más pequeña del mundo. Recordar a las mascotas en la ofrenda es una costumbre popular reciente, sin fuente institucional. (Fuente: Wikipedia en español; Infobae, 2025)"),
        ("gaspi", "Gaspi\nEn nuestra memoria", "Gaspi. Su recuerdo acompaña a la ofrenda junto a las personas que queremos. (Recuerdo del equipo)"),
    };
    private static readonly List<string> fotosPorMarco = new List<string>();

    private static string RetratoMat(string foto) => Mats.ContainsKey("retrato_" + foto) ? "retrato_" + foto : "retrato";
    private const string TxtConcepto = "Una ofrenda de siete niveles en el centro de la explanada de la Facultad de Informática. Sus cuatro caras miran a los cuatro rumbos y cada una guarda un elemento: fuego al norte, agua al oriente, tierra al poniente y viento al sur, hacia las gradas. Activa los cuatro rituales para completar la ofrenda.";

    private static WorldInteractable Hotspot(string title, string desc, Vector3 center, Vector3 size, float yaw = 0,
        WorldInteractable.Action action = WorldInteractable.Action.Story)
    {
        var go = new GameObject("Significado | " + title);
        go.transform.SetParent(hotspotRoot);
        go.transform.position = center;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        var wi = go.AddComponent<WorldInteractable>();
        wi.action = action;
        wi.title = title;
        wi.description = desc;
        return wi;
    }

    // ---------- estructura ----------

    private static void Estructura()
    {
        float pw = Base0 + .4f;
        Block("cantera", Vector3.zero, new Vector3(pw, Plinto, pw), 0, 1);
        float ph = Base0 / 2 + .78f;
        Quad("petate", new Vector3(-ph, .036f, -ph), new Vector3(-ph, .036f, ph), new Vector3(ph, .036f, ph), new Vector3(ph, .036f, -ph),
            Up, Vector2.zero, new Vector2(0, ph * 2 / .6f), new Vector2(ph * 2 / .6f, ph * 2 / .6f), new Vector2(ph * 2 / .6f, 0));
        ColliderBox("Ofrenda", Up * 1.7f, new Vector3(pw, 3.4f, pw));

        for (int i = 0; i < Niveles; i++)
        {
            float s = Side(i);
            // Un mantel liso por nivel, alternando morado, rosa y naranja, con liston de contraste.
            string mantel = new[] { "mantel_morado", "mantel_rosa", "mantel_naranja" }[i % 3];
            Block(mantel, Up * (Top(i) - NivelH), new Vector3(s, NivelH, s), 0, 1);
            Block(mantel == "mantel_morado" ? "mantel_naranja" : "mantel_morado", Up * (Top(i) - .075f), new Vector3(s + .012f, .045f, s + .012f), 0, 1);
            for (int face = 0; face < 4; face++)
            {
                Vector3 f = Caras[face], l = Lat(face);
                int count = Mathf.Max(3, Mathf.RoundToInt(s / .27f));
                float w = s / count;
                for (int k = 0; k < count; k++)
                {
                    float x = -s / 2 + w * (k + .5f);
                    PapelFlag(f * (s / 2 + .008f) + l * x + Up * (Top(i) - .085f), l, f, w * .46f, .25f, false, ParNivel[i][k % 2]);
                }
                for (float x = -s / 2 + .04f; x <= s / 2 - .04f; x += .075f)
                    Cabeza(f * (s / 2 - .04f) + l * x + Up * (Top(i) + .025f), Up, Rnd(.042f, .055f));
                // Cordon de flores al fondo del escalon, contra el nivel de arriba.
                if (i < Niveles - 1)
                {
                    float back = Side(i + 1) / 2 + .05f;
                    for (float x = -back; x <= back; x += .07f)
                        Cabeza(f * back + l * x + Up * (Top(i) + .035f), (f + Up).normalized, Rnd(.045f, .06f));
                }
            }
        }

        // Nicho de retratos en lo alto, con cruz y cirios.
        float ty = Top(Niveles - 1), ns = .92f, nh = .95f;
        Block("mantel_blanco", Up * ty, new Vector3(ns, nh, ns), 0, 1);
        for (int face = 0; face < 4; face++)
        {
            Vector3 f = Caras[face], l = Lat(face);
            Quaternion rot = Quaternion.LookRotation(f);
            Vector3 center = f * (ns / 2) + Up * (ty + nh / 2 + .02f);
            Box("marco_dorado", center + f * .02f, new Vector3(.68f, .8f, .04f), rot);
            var piece = Piece("Retrato del nicho " + face, center, () =>
            {
                Vector3 c = center + f * .045f;
                Quad(RetratoMat(Personas[0].foto), c + l * .29f - Up * .35f, c + l * .29f + Up * .35f, c - l * .29f + Up * .35f, c - l * .29f - Up * .35f, f,
                    Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            });
            retratos.Add(piece.GetComponentInChildren<Renderer>());
            fotosPorMarco.Add(Personas[0].foto);
            // Corona de cempasuchil y terciopelo alrededor del retrato de honor.
            Vector3 marco = center + f * .05f;
            for (float t = 0; t < 1; t += 1f / 44)
            {
                float per = t * 2 * (.68f + .8f), x, y;
                if (per < .68f) { x = per - .34f; y = -.4f; }
                else if (per < 1.48f) { x = .34f; y = per - .68f - .4f; }
                else if (per < 2.16f) { x = .34f - (per - 1.48f); y = .4f; }
                else { x = -.34f; y = .4f - (per - 2.16f); }
                Cabeza(marco + l * x + Up * y, (f + Up * .15f).normalized, Rnd(.035f, .045f), ((int)(t * 44)) % 5 == 0 ? "terciopelo" : null);
            }
            for (float x = -ns / 2; x <= ns / 2; x += .08f)
                Ellipsoid(AnyFlor(), f * (ns / 2 + .02f) + l * x + Up * (ty + nh + .01f), new Vector3(.05f, .04f, .05f), Quaternion.identity, 3, 5);
        }
        Vector3 crossBase = Up * (ty + nh);
        Block("madera", crossBase, new Vector3(.07f, .62f, .07f), 0, .5f);
        Box("madera", crossBase + Up * .45f, new Vector3(.34f, .07f, .07f), Quaternion.identity, .5f);
        Hotspot("Retratos", TxtRetratos, Up * (ty + .48f), new Vector3(1f, .95f, 1f));
        Hotspot("La cruz", TxtCruz, crossBase + Up * .32f, new Vector3(.4f, .64f, .4f));
        foreach (int lv in new[] { 0, 2, 4 })
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
        {
            float cxz = Side(lv + 1) / 2 + Paso / 4;
            Jarron(new Vector3(sx * cxz, Top(lv), sz * cxz), 1.15f - lv * .1f);
        }
        Hotspot("Ramos de flores", TxtRamos, new Vector3(Side(1) / 2 + Paso / 4, Top(0) + .35f, Side(1) / 2 + Paso / 4), new Vector3(.5f, .7f, .5f));
        // Cruz de ceniza sobre el petate, al pie de la cara del fuego.
        Vector3 cc = new Vector3(-1.35f, .04f, Base0 / 2 + .32f);
        Box("ceniza", cc, new Vector3(.09f, .012f, .5f), Quaternion.identity);
        Box("ceniza", cc + Vector3.forward * .08f, new Vector3(.34f, .012f, .09f), Quaternion.identity);
        Hotspot("Cruz de ceniza", TxtCruz, cc, new Vector3(.5f, .15f, .6f));
        Hotspot("Siete niveles", TxtNiveles, Pos(45, 2.9f, 1.4f), new Vector3(.35f, 2.7f, 3.3f), 45);
        Hotspot("Petate", TxtPetate, Pos(315, (Base0 / 2 + .55f) * 1.414f, .06f), new Vector3(.9f, .12f, .9f), 45);
    }

    // Bandera de papel picado del atlas (4x2). left = lado izquierdo visto de frente.
    private static void PapelFlag(Vector3 topCenter, Vector3 left, Vector3 facing, float halfW, float h, bool twoSided, int tile = -1)
    {
        if (tile < 0) tile = Paleta[random.Next(Paleta.Length)];
        float u0 = (tile % 4) * .25f, v0 = (tile / 4) * .5f;
        Vector3 a = topCenter + left * halfW - Up * h, b = topCenter + left * halfW, c = topCenter - left * halfW, d = topCenter - left * halfW - Up * h;
        Quad("papel", a, b, c, d, facing, new Vector2(u0, v0), new Vector2(u0, v0 + .5f), new Vector2(u0 + .25f, v0 + .5f), new Vector2(u0 + .25f, v0), twoSided);
    }

    // ---------- arcos de cempasuchil ----------

    private static void Arcos()
    {
        foreach (float diag in new[] { 45f, 135f })
        {
            Vector3 d = Radial(diag);
            var path = new List<Vector3>();
            for (float y = .25f; y < ArcoPierna; y += .1f) path.Add(d * ArcoR + Up * y);
            int arcN = 130;
            for (int i = 0; i <= arcN; i++)
            {
                float th = Mathf.PI * i / arcN;
                path.Add(d * ArcoR * Mathf.Cos(th) + Up * (ArcoPierna + ArcoAlto * Mathf.Sin(th)));
            }
            for (float y = ArcoPierna - .1f; y >= .25f; y -= .1f) path.Add(-d * ArcoR + Up * y);
            var core = new List<Vector3> { d * ArcoR };
            core.AddRange(path);
            core.Add(-d * ArcoR);
            Tube("carrizo", core, .045f, 6);
            Vector3 across = Vector3.Cross(d, Up);
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector3 t = (path[i + 1] - path[i - 1]).normalized;
                Vector3 n1 = Vector3.Cross(t, across).normalized, n2 = Vector3.Cross(t, n1).normalized;
                // Domos achatados de tamano y acomodo irregular, con hojas, para que no parezca collar de canicas.
                for (int k = 0; k < 8; k++)
                {
                    float ph = (k * 45 + (i % 2) * 22 + Rnd(-14, 14)) * Mathf.Deg2Rad;
                    Vector3 o = n1 * Mathf.Cos(ph) + n2 * Mathf.Sin(ph);
                    Cabeza(path[i] + o * Rnd(.065f, .09f) + t * Rnd(-.025f, .025f), o, Rnd(.05f, .072f));
                }
                if (i % 2 == 0) Hoja(path[i], (n1 * Mathf.Cos(Rnd(0, 6.28f)) + n2 * Mathf.Sin(Rnd(0, 6.28f))), t, Rnd(.12f, .17f));
            }
        }
        for (int i = 0; i < 40; i++)
        {
            Vector3 o = Random3(1).normalized;
            Cabeza(Up * ApexY + o * Rnd(.18f, .3f), o, Rnd(.055f, .085f));
        }

        // Macetas de barro con ramos en el pie de cada arco, caña en los del poniente.
        foreach (float diag in new[] { 45f, 135f, 225f, 315f })
        {
            Vector3 p = Pos(diag, ArcoR + .45f, 0);
            Lathe("barro", p, new[] { new Vector2(0, 0), new Vector2(.16f, 0), new Vector2(.23f, .14f), new Vector2(.21f, .34f), new Vector2(.25f, .38f), new Vector2(.22f, .4f), new Vector2(0, .4f) }, 12);
            Ramo(p + Up * .38f, 1.3f);
            if (diag == 225f || diag == 315f)
                for (int s = 0; s < 3; s++)
                {
                    Vector3 b = Pos(diag + (s - 1) * 4, ArcoR + .2f, 0);
                    Vector3 top = Pos(diag + (s - 1) * 7, ArcoR - .05f, 2.9f + s * .25f);
                    Cana(b, top);
                }
            else
                Hotspot("Arco de cempasúchil", TxtArco, Pos(diag, ArcoR + .2f, 1.25f), new Vector3(.7f, 2.5f, 1.2f), diag);
        }
        Hotspot("Caña de azúcar", TxtCana, Pos(225, ArcoR + .1f, 1.4f), new Vector3(.8f, 2.8f, .8f), 225);
        Hotspot("Caña de azúcar", TxtCana, Pos(315, ArcoR + .1f, 1.4f), new Vector3(.8f, 2.8f, .8f), 315);
        Hotspot("Arco de cempasúchil", TxtArco, Up * ApexY, new Vector3(1.1f, 1f, 1.1f));
    }

    // Cabeza de cempasuchil: domo achatado que mira hacia n.
    private static void Cabeza(Vector3 p, Vector3 n, float r, string mat = null)
    {
        string m = mat ?? AnyFlor();
        Quaternion q = Quaternion.LookRotation(n) * Quaternion.Euler(90, Rnd(0, 360), 0);
        Ellipsoid(m, p, new Vector3(r * .78f, r * .55f, r * .78f), q, 2, 6);
        Vector3 x = q * Vector3.right * r * 1.15f, z = q * Vector3.forward * r * 1.15f, c = p - n * r * .08f;
        string ros = m == "cempasuchil_oscuro" ? "roseta_oscura" : "roseta";
        Quad(ros, c - x - z, c - x + z, c + x + z, c + x - z, n, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, true);
    }

    private static void Hoja(Vector3 p, Vector3 n, Vector3 along, float len)
    {
        Vector3 tip = p + n * len + along * len * .3f;
        Vector3 side = Vector3.Cross(n, along).normalized * len * .22f;
        Vector3 mid = (p + tip) * .5f;
        Tri("hoja_flor", p, mid + side, tip, Vector3.Cross(side, tip - p));
        Tri("hoja_flor", p, mid - side, tip, -Vector3.Cross(side, tip - p));
        Tri("hoja_flor", p, mid + side, tip, -Vector3.Cross(side, tip - p));
        Tri("hoja_flor", p, mid - side, tip, Vector3.Cross(side, tip - p));
    }

    // Ramo en jarron: cempasuchil, terciopelo y nube sobre tallos que se abren.
    private static void Ramo(Vector3 baseTop, float s)
    {
        int heads = Mathf.RoundToInt(14 * s);
        for (int k = 0; k < heads; k++)
        {
            float az = Rnd(0, 360), tilt = Rnd(5, 42);
            Vector3 dir = (Pos(az, Mathf.Sin(tilt * Mathf.Deg2Rad)) + Up * Mathf.Cos(tilt * Mathf.Deg2Rad)).normalized;
            Vector3 head = baseTop + dir * Rnd(.22f, .36f) * s;
            Rod("hoja_flor", baseTop, head, .006f * s, 4);
            float roll = Rnd();
            if (roll < .62f) Cabeza(head, dir, Rnd(.045f, .065f) * s);
            else if (roll < .85f)
            {
                // Terciopelo: cresta ondulada magenta.
                Quaternion q = Quaternion.LookRotation(dir) * Quaternion.Euler(90, Rnd(0, 360), 0);
                Ellipsoid("terciopelo", head, new Vector3(.055f, .045f, .022f) * s, q, 3, 7);
                Ellipsoid("terciopelo", head + dir * .02f * s, new Vector3(.04f, .035f, .02f) * s, q * Quaternion.Euler(0, 40, 0), 3, 6);
            }
            else
                for (int b = 0; b < 7; b++) Sphere("nube", head + Random3(.04f * s), .011f * s, 2, 4);
            if (k % 3 == 0) Hoja(baseTop + dir * .1f * s, dir, Up, .12f * s);
        }
    }

    private static void Jarron(Vector3 p, float s)
    {
        var prof = new[] { new Vector2(0, 0), new Vector2(.06f, 0), new Vector2(.1f, .07f), new Vector2(.11f, .14f), new Vector2(.07f, .22f), new Vector2(.06f, .26f), new Vector2(.085f, .3f), new Vector2(0, .29f) };
        for (int i = 0; i < prof.Length; i++) prof[i] *= s;
        Lathe("barro", p, prof, 12);
        Ring("barro_oscuro", p, .108f * s, .113f * s, .14f * s, 0, 360, 14, 1);
        Ramo(p + Up * .29f * s, .8f * s);
    }

    private static Vector3 Random3(float r)
    {
        Vector3 v;
        do v = new Vector3(Rnd(-1, 1), Rnd(-1, 1), Rnd(-1, 1)); while (v.sqrMagnitude > 1);
        return v * r;
    }

    private static void Cana(Vector3 b, Vector3 top)
    {
        Vector3 d = top - b;
        int nodes = Mathf.RoundToInt(d.magnitude / .3f);
        for (int i = 0; i < nodes; i++)
        {
            Vector3 p0 = b + d * i / nodes, p1 = b + d * (i + 1) / nodes;
            Rod("cana", p0, p1, .026f, 6);
            Ellipsoid("cana_nudo", p1, new Vector3(.03f, .012f, .03f), Quaternion.FromToRotation(Up, d.normalized), 2, 6);
        }
        for (int k = 0; k < 5; k++)
        {
            Vector3 dir = (Pos(k * 72 + Rnd(-20, 20), 1) + Up * Rnd(.2f, .8f)).normalized;
            Vector3 tip = top + dir * Rnd(.6f, .9f) + Vector3.down * .25f;
            Vector3 side = Vector3.Cross(dir, Up).normalized * .035f;
            Quad("hoja_palma", top - side, top + side, tip + side * .2f, tip - side * .2f, Up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, true);
        }
    }

    // ---------- petalos en el piso ----------

    private static void Petalos()
    {
        void Petal(Vector3 p, string mat)
        {
            float yaw = Rnd(0, 360);
            Quaternion r = Quaternion.Euler(Rnd(-10, 10), yaw, Rnd(-10, 10));
            Vector3 a = r * new Vector3(-.022f, 0, -.016f), b = r * new Vector3(-.022f, 0, .016f), c = r * new Vector3(.022f, 0, .016f), d = r * new Vector3(.022f, 0, -.016f);
            Vector3 y = Vector3.up * (.04f + Rnd() * .014f);
            p.y = 0;
            Quad(mat, p + a + y, p + b + y, p + c + y, p + d + y, Up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }
        void Head(Vector3 p, string mat)
        {
            p.y = .04f;
            Ellipsoid(mat, p, new Vector3(.05f, .03f, .05f), Quaternion.identity, 3, 6, .5f);
        }
        // Circulo alrededor del altar.
        for (int i = 0; i < 4200; i++)
        {
            float ri = PedestalR + .45f, ro = ri + .7f;
            float a = Rnd(0, 360), r = Mathf.Sqrt(Rnd(ri * ri, ro * ro));
            bool edge = r < ri + .12f || r > ro - .12f;
            Petal(Pos(a, r), edge && Rnd() < .7f ? "terciopelo" : AnyFlor());
            if (i % 28 == 0) Head(Pos(a, r), AnyFlor());
        }
        // Cruz de los cuatro rumbos.
        for (int face = 0; face < 4; face++)
        {
            Vector3 f = Caras[face], l = Lat(face);
            for (int i = 0; i < 2700; i++)
            {
                float along = Rnd(PedestalR + 1.15f, 8.9f * E), x = Rnd(-.62f, .62f);
                bool edge = Mathf.Abs(x) > .48f;
                Vector3 p = f * along + l * x;
                Petal(p, edge && Rnd() < .75f ? "terciopelo" : AnyFlor());
                if (i % 30 == 0 && !edge) Head(p, AnyFlor());
            }
            for (int i = 0; i < 520; i++)
            {
                Vector3 p = f * 9.3f * E + Pos(Rnd(0, 360), Mathf.Sqrt(Rnd()) * .6f);
                Petal(p, AnyFlor());
                if (i % 20 == 0) Head(p, AnyFlor());
            }
            Hotspot("Camino de cempasúchil", TxtCamino, f * (PedestalR + 1.15f + 8.9f * E) / 2 + Up * .06f, new Vector3(1.3f, .12f, 8.9f * E - PedestalR - 1.15f), CaraYaw[face]);
        }
    }

    // ---------- rituales ----------

    private static void Rituales()
    {
        string[] titles = { "Fuego: veladoras y copal", "Agua", "Tierra: el alimento", "Viento: papel picado" };
        string[] verbs = { "Encender las veladoras y el copal", "Servir el agua", "Ofrecer el pan, la fruta y la comida", "Colgar el papel picado" };
        string[] after =
        {
            "Encendiste las veladoras de los siete niveles y el copal. La luz guía a las ánimas; el copal, de origen indígena, limpia el lugar para que el alma pueda entrar sin peligro. (Fuente: INPI, gob.mx)",
            "Serviste el agua en los vasos y el cántaro, para que las ánimas mitiguen su sed después del largo recorrido. (Fuente: INPI, gob.mx)",
            "Ofreciste el pan, la fruta de temporada, el mole y los tamales. La buena comida tiene por objeto deleitar al ánima. (Fuente: INPI, gob.mx)",
            "Colgaste el papel picado sobre la explanada. Representa al aire; mira de cerca las banderas: tienen FIF y UAQ cortadas en binario. (Fuente: Profeco, gob.mx)"
        };
        for (int face = 0; face < 4; face++)
        {
            Vector3 f = Caras[face], l = Lat(face);
            Vector3 p = f * PedestalR;
            Cylinder("cantera", p, .3f, .34f, .5f, 16, Quaternion.identity, true, false, 1);
            Cylinder("cantera", p + Up * .5f, .36f, .36f, .05f, 16, Quaternion.identity, true, false, 1);
            ColliderBox("Pedestal " + face, p + Up * .3f, new Vector3(.6f, .6f, .6f));
            Vector3 t = p + Up * .5f;
            switch (face)
            {
                case 0:
                    Lathe("barro", t, new[] { new Vector2(0, 0), new Vector2(.07f, 0), new Vector2(.05f, .08f), new Vector2(.06f, .1f), new Vector2(.16f, .18f), new Vector2(.17f, .22f), new Vector2(.14f, .22f), new Vector2(0, .2f) }, 14);
                    for (int k = 0; k < 9; k++) Ellipsoid(k % 3 == 0 ? "brasa" : "negro", t + Up * .2f + Pos(Rnd(0, 360), Rnd(0, .09f)), Vector3.one * .03f, Quaternion.identity, 3, 5);
                    var humo = Piece("Fuego | copal encendido", t, () =>
                    {
                        // Columna de copal: densa al salir, se abre y se dispersa al subir.
                        for (int k = 0; k < 120; k++)
                            Sprite("humo", t + Up * .22f + Pos(Rnd(0, 360), Rnd(0, .04f)), Vector2.one * Rnd(.05f, .1f), new Color(1, 1, 1, 1), Rnd(), new Vector4(Rnd(.22f, .38f), Rnd(2.6f, 4.2f), Rnd(.35f, .8f), Rnd(3f, 5f)));
                        for (int k = 0; k < 10; k++)
                            Sprite("halo_calido", t + Up * .21f + Pos(Rnd(0, 360), Rnd(0, .06f)), Vector2.one * Rnd(.04f, .07f), new Color(1f, .35f, .05f), Rnd());
                        Sprite("halo_calido", t + Up * .22f, Vector2.one * .35f, new Color(1f, .4f, .1f), Rnd());
                    });
                    fuego.Add(humo);
                    break;
                case 1:
                    Cantaro(t, .9f);
                    break;
                case 2:
                    Canasta(t);
                    break;
                default:
                    string[] colors = { "rosa", "turquesa", "amarillo", "verde", "farol_morado" };
                    for (int k = 0; k < 5; k++)
                        Cylinder(colors[k], t + Up * (.06f + (k % 2) * .1f) + l * ((k - 2) * .07f) - f * .14f, .055f, .055f, .3f, 10,
                            Quaternion.LookRotation(l) * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 0, 0), true, true);
                    break;
            }
            var ring = Piece("Guía del ritual " + face, p, () => Ring("anillo_ritual", p, .5f, .58f, .045f, 0, 360, 36, 1));
            var wi = Hotspot(titles[face], after[face], p + Up * .55f, new Vector3(.9f, 1.1f, .9f), CaraYaw[face], WorldInteractable.Action.Offering);
            wi.offeringIndex = face;
            wi.verb = verbs[face];
            wi.activeVisual = ring.gameObject;
        }
    }

    // ---------- velas ----------

    // 0 lamparita, 1 veladora en vaso, 2 cirio alto. Devuelve donde va la flama.
    private static Vector3 Vela(Vector3 p, int type, int seed)
    {
        if (type == 1) { Veladora(p, seed); return p + Up * .2f; }
        if (type == 0)
        {
            Cylinder("barro", p, .03f, .026f, .03f, 8, Quaternion.identity, true);
            Cylinder("cera", p + Up * .03f, .022f, .022f, .012f, 8, Quaternion.identity, true);
            return p + Up * .075f;
        }
        float h = Rnd(.24f, .4f);
        Cylinder("cera", p, .026f, .022f, h, 8, Quaternion.identity, true);
        Ellipsoid("cera", p + Up * (h - .03f) + new Vector3(.02f, 0, 0), new Vector3(.009f, .03f, .009f), Quaternion.identity, 2, 5);
        return p + Up * (h + .05f);
    }

    private static void Veladora(Vector3 p, int kind)
    {
        string glass = kind % 3 == 0 ? "veladora_roja" : kind % 3 == 1 ? "veladora_blanca" : "veladora_ambar";
        Cylinder(glass, p, .036f, .036f, .15f, 8, Quaternion.identity, true);
        Cylinder("cera", p + Up * .15f, .028f, .028f, .006f, 8, Quaternion.identity, true);
    }

    private static void Velas()
    {
        // Grupos de un cirio alto, veladoras y lamparitas, en lugar de filas parejas.
        var flames = new List<Vector3>[Niveles];
        for (int i = 0; i < Niveles - 1; i++)
        {
            flames[i] = new List<Vector3>();
            float half = Half(i) - .12f;
            for (int face = 0; face < 4; face++)
            {
                var centers = new List<float>();
                if (face == 0 && (i == 2 || i == 4)) { centers.Add(-half); centers.Add(0); centers.Add(half); }
                else if (face == 0)
                {
                    int n = Mathf.Max(2, Mathf.FloorToInt(half * 2 / .75f) + 1);
                    for (int k = 0; k < n; k++) centers.Add(Mathf.Lerp(-half, half, (float)k / (n - 1)));
                }
                else { centers.Add(-half); centers.Add(half); }
                foreach (float cx in centers)
                {
                    int count = face == 0 ? random.Next(3, 6) : random.Next(2, 4);
                    for (int c = 0; c < count; c++)
                    {
                        float x = Mathf.Clamp(cx + (c - (count - 1) * .5f) * .085f + Rnd(-.015f, .015f), -Half(i), Half(i));
                        Vector3 p = OnTread(face, i, x) + Caras[face] * Rnd(-.07f, .07f);
                        flames[i].Add(Vela(p, c == count / 2 ? 2 : c % 2 == 0 ? 1 : 0, c + i + face));
                    }
                }
            }
        }
        // Cirios del nivel superior, encendidos desde el inicio.
        float ty = Top(Niveles - 1);
        var cirios = new List<Vector3>();
        foreach (int sx in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
        {
            Vector3 p = new Vector3(sx * .64f, ty, sz * .64f);
            Cylinder("cera", p, .055f, .048f, .56f, 10, Quaternion.identity, true);
            Ellipsoid("cera", p + Up * .5f + new Vector3(.04f, 0, 0), new Vector3(.015f, .05f, .015f), Quaternion.identity, 3, 5);
            cirios.Add(p + Up * .62f);
            Hotspot("Cirios", TxtCirios, p + Up * .3f, new Vector3(.28f, .75f, .28f));
        }
        foreach (var c in cirios)
        {
            Sprite("flama", c, new Vector2(.03f, .06f), Color.white, Rnd());
            Sprite("halo_calido", c, Vector2.one * .26f, new Color(1f, .62f, .3f), Rnd());
        }
        PointLight("Luz de los cirios", Up * (ty + 1.1f), new Color(1f, .62f, .32f), 1.3f, 7f, true);

        for (int i = 0; i < Niveles - 1; i++)
        {
            int level = i;
            var piece = Piece("Fuego | veladoras nivel " + (i + 1), Up * Top(i), () =>
            {
                foreach (var p in flames[level])
                {
                    float ph = Rnd();
                    Sprite("flama", p, new Vector2(.018f, .036f), Color.white, ph);
                    Sprite("halo_calido", p, Vector2.one * .15f, new Color(1f, .58f, .25f), ph);
                }
            });
            if (i == 0)
            {
                Piece("Fuego | luz en el piso", Vector3.zero, () =>
                {
                    for (int face = 0; face < 4; face++)
                    {
                        Vector3 f = Caras[face], l = Lat(face), c = f * (Base0 / 2 + 1.1f) + Up * .05f;
                        Quad("luz_suelo", c - f * 1.4f - l * 4.2f, c + f * 1.4f - l * 4.2f, c + f * 1.4f + l * 4.2f, c - f * 1.4f + l * 4.2f, Up,
                            new Vector2(0, .5f), new Vector2(0, 1f), new Vector2(1, 1f), new Vector2(1, .5f));
                    }
                }, piece);
            }
            if (i == 0)
                for (int face = 0; face < 4; face++)
                    PointLight("Luz de veladoras " + face, Caras[face] * 4.4f + Up * 1.7f, new Color(1f, .58f, .3f), 1.4f, 7.5f, true, piece);
            fuego.Add(piece);
        }
        var camino = new List<Vector3>();
        for (int face = 0; face < 4; face++)
            for (float d = PedestalR + 1.6f; d < 8.6f * E; d += .9f)
                foreach (int sd in new[] { -1, 1 })
                {
                    Vector3 p = Caras[face] * d + Lat(face) * sd * .78f;
                    Veladora(p, (int)(d * 3) + sd);
                    camino.Add(p + Up * .2f);
                }
        fuego.Add(Piece("Fuego | camino de veladoras", Vector3.zero, () =>
        {
            foreach (var p in camino)
            {
                float ph = Rnd();
                Sprite("flama", p, new Vector2(.018f, .036f), Color.white, ph);
                Sprite("halo_calido", p, Vector2.one * .2f, new Color(1f, .58f, .25f), ph);
            }
        }));
        Hotspot("Veladoras", TxtVelas, Vector3.forward * 2.8f + Up * .65f, new Vector3(5f, 1f, 1f));
    }

    // ---------- piezas de barro, comida y adornos ----------

    private static void Cantaro(Vector3 p, float s)
    {
        var prof = new[] { new Vector2(0, 0), new Vector2(.11f, 0), new Vector2(.19f, .09f), new Vector2(.21f, .19f), new Vector2(.16f, .31f), new Vector2(.075f, .37f), new Vector2(.065f, .44f), new Vector2(.085f, .46f), new Vector2(.06f, .46f), new Vector2(0, .4f) };
        for (int i = 0; i < prof.Length; i++) prof[i] *= s;
        Lathe("barro", p, prof, 14);
        Ring("barro_oscuro", p, .2f * s, .212f * s, .2f * s, 0, 360, 16, 1);
    }

    private static void Plato(Vector3 p, float r)
        => Lathe("barro", p, new[] { new Vector2(0, 0), new Vector2(r * .6f, 0), new Vector2(r, r * .2f), new Vector2(r * 1.04f, r * .26f), new Vector2(r * .9f, r * .22f), new Vector2(0, r * .12f) }, 14);

    private static void Cazuela(Vector3 p, string surface)
    {
        Lathe("barro", p, new[] { new Vector2(0, 0), new Vector2(.12f, 0), new Vector2(.2f, .07f), new Vector2(.22f, .11f), new Vector2(.2f, .11f), new Vector2(.17f, .08f) }, 14);
        Cylinder(surface, p + Up * .06f, .18f, .19f, .025f, 14, Quaternion.identity, true);
    }

    private static void Canasta(Vector3 p)
    {
        Lathe("carrizo", p, new[] { new Vector2(0, 0), new Vector2(.13f, 0), new Vector2(.2f, .12f), new Vector2(.215f, .14f), new Vector2(.19f, .13f), new Vector2(0, .05f) }, 14);
        PanDeMuerto(p + Up * .08f + new Vector3(.06f, 0, .03f), .7f);
        PanDeMuerto(p + Up * .09f + new Vector3(-.07f, 0, -.02f), .65f);
        PanDeMuerto(p + Up * .12f + new Vector3(0, 0, .09f), .6f);
    }

    private static void PanDeMuerto(Vector3 p, float s)
    {
        Ellipsoid("pan", p, new Vector3(.11f, .065f, .11f) * s, Quaternion.identity, 6, 10, .5f);
        for (int k = 0; k < 4; k++)
            Ellipsoid("pan", p + Up * .05f * s, new Vector3(.1f, .02f, .024f) * s, Quaternion.Euler(0, k * 45, 0), 3, 6);
        Sphere("pan", p + Up * .07f * s, .026f * s, 4, 6);
        for (int k = 0; k < 10; k++)
        {
            Vector3 o = Pos(Rnd(0, 360), Rnd(0, .08f) * s);
            Ellipsoid("pan_azucar", p + o + Up * (.06f * s - o.magnitude * .25f), Vector3.one * .008f * s, Quaternion.identity, 2, 4);
        }
    }

    private static void Calaverita(Vector3 p, Vector3 facing, float s)
    {
        Quaternion r = Quaternion.LookRotation(facing);
        Vector3 l = Vector3.Cross(Up, facing);
        Ellipsoid("azucar", p + Up * .06f * s, new Vector3(.068f, .062f, .062f) * s, r, 5, 8);
        Ellipsoid("azucar", p + Up * .022f * s + facing * .022f * s, new Vector3(.045f, .025f, .04f) * s, r, 3, 6);
        string[] c = { "rosa", "turquesa", "amarillo", "verde" };
        string eye = c[random.Next(4)];
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 e = p + Up * .062f * s + l * sx * .026f * s + facing * .052f * s;
            Ellipsoid(eye, e, new Vector3(.02f, .02f, .01f) * s, r, 3, 6);
            Ellipsoid("negro", e + facing * .006f * s, new Vector3(.011f, .011f, .006f) * s, r, 2, 5);
        }
        Ellipsoid("negro", p + Up * .04f * s + facing * .06f * s, new Vector3(.007f, .009f, .004f) * s, r, 2, 4);
        string petal = c[random.Next(4)];
        for (int k = 0; k < 5; k++)
        {
            float a = k * Mathf.PI * 2 / 5;
            Vector3 o = l * Mathf.Cos(a) * .013f * s + Up * Mathf.Sin(a) * .013f * s;
            Ellipsoid(petal, p + Up * .097f * s + facing * .04f * s + o, new Vector3(.009f, .009f, .005f) * s, r, 2, 4);
        }
        Box("negro", p + Up * .024f * s + facing * .061f * s, new Vector3(.04f, .004f, .004f) * s, r);
    }

    private static void Frutero(Vector3 p, string fruit, float fr, int layers)
    {
        Plato(p, .16f);
        float y = .035f;
        for (int L = layers; L >= 1; L--)
        {
            float off = (L - 1) * fr;
            for (int i = 0; i < L; i++)
            for (int j = 0; j < L; j++)
                Sphere(fruit, p + new Vector3(i * fr * 2 - off, y + fr, j * fr * 2 - off), fr * .98f, 4, 7);
            y += fr * 1.45f;
        }
    }

    // ---------- caras de la ofrenda ----------

    private static void CaraAgua()
    {
        const int face = 1;
        var water = new List<Vector3>[Niveles];
        for (int i = 0; i < Niveles; i++) water[i] = new List<Vector3>();
        void Vaso(int level, float x)
        {
            Vector3 p = OnTread(face, level, x);
            Cylinder("vidrio_vaso", p, .045f, .05f, .13f, 10, Quaternion.identity, false, true);
            water[level].Add(p);
        }
        Cantaro(OnTread(face, 0, 0), 1f);
        foreach (float x in new[] { -1.5f, -.75f, .75f, 1.5f }) Vaso(0, x);
        foreach (int level in new[] { 1, 3, 5 })
        {
            float half = Half(level) - .15f;
            int n = Mathf.Max(2, Mathf.FloorToInt(half * 2 / .42f) + 1);
            for (int k = 0; k < n; k++) Vaso(level, Mathf.Lerp(-half, half, (float)k / (n - 1)));
        }
        foreach (float x in new[] { -1.55f, 0f, 1.55f })
        {
            Vector3 p = OnTread(face, 2, x);
            Lathe("barro_oscuro", p, new[] { new Vector2(0, 0), new Vector2(.04f, 0), new Vector2(.058f, .06f), new Vector2(.05f, .11f), new Vector2(.058f, .12f), new Vector2(.045f, .12f), new Vector2(0, .1f) }, 12);
            water[2].Add(p + Up * -.02f);
        }
        Vector3 sal = OnTread(face, 4, 0);
        Plato(sal, .14f);
        Lathe("sal", sal + Up * .026f, new[] { new Vector2(.105f, 0), new Vector2(.09f, .03f), new Vector2(.06f, .06f), new Vector2(.025f, .078f), new Vector2(0, .082f) }, 16);
        for (int k = 0; k < 40; k++)
        {
            Vector3 g = sal + Up * .028f + Pos(Rnd(0, 360), Rnd(.02f, .13f));
            float r = (g - sal).magnitude;
            Box("sal", g + Up * Mathf.Max(0, .08f - r * .7f), Vector3.one * Rnd(.006f, .011f), Quaternion.Euler(Rnd(0, 90), Rnd(0, 90), 0));
        }
        Vaso(4, -.75f); Vaso(4, .75f);
        for (int level = 0; level < Niveles; level++)
        {
            if (water[level].Count == 0) continue;
            int lv = level;
            agua.Add(Piece("Agua | nivel " + (level + 1), Up * Top(level), () =>
            {
                foreach (var p in water[lv]) Cylinder("agua", p + Up * .008f, .041f, .046f, .1f, 10, Quaternion.identity, true);
            }));
        }
        Hotspot("Agua", TxtAgua, Vector3.right * 2.8f + Up * .65f, new Vector3(5f, 1f, 1f), 90);
        Hotspot("Sal", TxtSal, sal + Up * .08f, new Vector3(.5f, .3f, .4f), 90);
    }

    private static void CaraTierra()
    {
        const int face = 2;
        Vector3 f = Caras[face];
        var food = new Dictionary<int, List<System.Action>>();
        void Add(int level, System.Action a)
        {
            if (!food.ContainsKey(level)) food[level] = new List<System.Action>();
            food[level].Add(a);
        }
        Add(0, () => Cazuela(OnTread(face, 0, -.95f), "mole"));
        Add(0, () =>
        {
            Vector3 p = OnTread(face, 0, -.95f);
            for (int k = 0; k < 14; k++) Sphere("azucar", p + Up * .088f + Pos(Rnd(0, 360), Rnd(0, .15f)), .006f, 2, 4);
        });
        Add(0, () => Canasta(OnTread(face, 0, .95f)));
        Add(0, () =>
        {
            Vector3 p = OnTread(face, 0, 0);
            Lathe("barro_oscuro", p, new[] { new Vector2(0, 0), new Vector2(.05f, 0), new Vector2(.07f, .08f), new Vector2(.06f, .15f), new Vector2(.07f, .16f), new Vector2(.055f, .16f), new Vector2(0, .13f) }, 12);
            Cylinder("chocolate", p + Up * .12f, .055f, .058f, .02f, 12, Quaternion.identity, true);
        });
        foreach (int s in new[] { -1, 1 })
            Add(0, () =>
            {
                Vector3 p = OnTread(face, 0, s * 1.75f);
                Plato(p, .15f);
                for (int k = 0; k < 3; k++)
                {
                    // Tamal abierto: hoja de maiz extendida y la masa al centro.
                    Vector3 tp = p + Up * .035f + Lat(face) * (k - 1) * .085f;
                    Quaternion q = Quaternion.Euler(0, 90 + Rnd(-15, 15), 0);
                    Vector3 fw = q * Vector3.forward, sd = q * Vector3.right;
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        Vector3 a0 = tp - fw * .075f, a1 = tp + fw * .075f, b0 = tp - fw * .06f + sd * sgn * .05f + Up * .012f, b1 = tp + fw * .06f + sd * sgn * .05f + Up * .012f;
                        Quad("hoja_maiz", a0, a1, b1, b0, Up, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, true);
                    }
                    Ellipsoid("masa", tp + Up * .016f, new Vector3(.03f, .017f, .055f), q, 3, 7);
                    if (k == 1) Ellipsoid("mole", tp + Up * .03f, new Vector3(.018f, .006f, .035f), q, 2, 6);
                }
            });
        Add(1, () => Frutero(OnTread(face, 1, -1.25f), "naranja", .055f, 3));
        Add(1, () => Frutero(OnTread(face, 1, 0), "naranja", .055f, 3));
        Add(1, () => Frutero(OnTread(face, 1, 1.25f), "naranja", .055f, 3));
        foreach (float x in new[] { -.62f, .62f })
            Add(1, () =>
            {
                Vector3 p = OnTread(face, 1, x);
                Plato(p, .12f);
                for (int k = 0; k < 12; k++) Sphere("tejocote", p + Up * .04f + Pos(Rnd(0, 360), Rnd(0, .07f)) + Up * Rnd(0, .02f), .021f, 3, 6);
            });
        Add(2, () => PanDeMuerto(OnTread(face, 2, 0), 1.55f));
        foreach (float x in new[] { -1.5f, 1.5f })
            Add(2, () => PanDeMuerto(OnTread(face, 2, x), 1.1f));
        Add(4, () => Frutero(OnTread(face, 4, 0), "guayaba", .036f, 2));
        Add(5, () => PanDeMuerto(OnTread(face, 5, -.3f), 1.25f));
        Add(5, () => PanDeMuerto(OnTread(face, 5, .3f), 1.25f));

        foreach (var pair in food)
        {
            int level = pair.Key;
            var actions = pair.Value;
            tierra.Add(Piece("Tierra | nivel " + (level + 1), Up * Top(level), () => { foreach (var a in actions) a(); }));
        }
        // Calaveritas siempre presentes.
        float h3 = Half(3);
        for (int k = 0; k < 5; k++) Calaverita(OnTread(face, 3, Mathf.Lerp(-h3 + .1f, h3 - .1f, k / 4f)) + f * .06f, f, 1.8f);

        float yaw = CaraYaw[face];
        Hotspot("Mole y comida favorita", TxtComida, OnTread(face, 0, 0) + Up * .14f, new Vector3(4.6f, .32f, .4f), yaw);
        Hotspot("Fruta de temporada", TxtFruta, OnTread(face, 1, 0) + Up * .15f, new Vector3(3.6f, .3f, .36f), yaw);
        Hotspot("Fruta de temporada", TxtFruta, OnTread(face, 4, 0) + Up * .12f, new Vector3(.38f, .24f, .34f), yaw);
        Hotspot("Pan de muerto", TxtPan, OnTread(face, 2, 0) + Up * .1f, new Vector3(.6f, .22f, .36f), yaw);
        Hotspot("Pan de muerto", TxtPan, OnTread(face, 5, 0) + Up * .1f, new Vector3(.9f, .22f, .34f), yaw);
        Hotspot("Calaveritas de azúcar", TxtCalaveritas, OnTread(face, 3, 0) + Up * .1f, new Vector3(2.6f, .22f, .34f), yaw);
    }

    // Retratos en los niveles 3 y 5 de cada cara, para que se vean desde cualquier rumbo:
    // musica al norte, cine al oriente, television y mascotas al poniente, historia y cultura al sur.
    private static readonly string[][] RetratosPorCara =
    {
        new[] { "jose-jose", "chavela-vargas", "vicente-fernandez", "pedro-infante", "jorge-negrete", "tin-tan" },
        new[] { "cantinflas", "maria-felix", "katy-jurado", "silvia-pinal", "lopez-tarso", "octavio-ocana" },
        new[] { "chespirito", "ramon-valdes", "ruben-aguirre", "chabelo", "chihuahua", "gaspi" },
        new[] { "josefa-ortiz", "frida-kahlo", "sor-juana", "diego-rivera", "jose-guadalupe-posada", "gonzalez-camarena", "homero-gomez" },
    };

    private static void Retratos()
    {
        for (int face = 0; face < 4; face++)
        {
            var fotos = RetratosPorCara[face];
            int abajo = fotos.Length - 2;
            for (int j = 0; j < fotos.Length; j++)
            {
                int level = j < abajo ? 2 : 4;
                // El hueco central queda para el pan, el agua o las veladoras de cada rumbo.
                float x = level == 4 ? (j - abajo == 0 ? -.4f : .4f)
                    : abajo == 4 ? new[] { -1.1f, -.55f, .55f, 1.1f }[j] : Mathf.Lerp(-1.1f, 1.1f, j / (abajo - 1f));
                var persona = System.Array.Find(Personas, q => q.foto == fotos[j]);
                Retrato(face, level, x, persona.foto, persona.nombre, persona.texto);
            }
        }
    }

    private static void Retrato(int face, int level, float x, string foto, string nombre, string texto)
    {
        Vector3 f = Caras[face];
        Vector3 p = OnTread(face, level, x) - f * .04f;
        Quaternion r = Quaternion.LookRotation(f) * Quaternion.Euler(-12, 0, 0);
        Vector3 fr = r * Vector3.forward, upr = r * Vector3.up;
        Vector3 c = p + upr * .26f;
        Box("marco_dorado", c, new Vector3(.36f, .5f, .03f), r);
        Box("tablero_morado", c - upr * .19f + fr * .017f, new Vector3(.32f, .115f, .006f), r);
        Rod("madera", p - fr * .06f, c - fr * .06f + upr * .1f, .01f, 4);
        Vector3 pc = c + upr * .05f;
        var piece = Piece("Fotografía " + foto, pc, () =>
        {
            Vector3 cc = pc + fr * .017f;
            Vector3 lr = Vector3.Cross(upr, fr).normalized;
            Quad(RetratoMat(foto), cc + lr * .14f - upr * .175f, cc + lr * .14f + upr * .175f, cc - lr * .14f + upr * .175f, cc - lr * .14f - upr * .175f, fr,
                Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        });
        retratos.Add(piece.GetComponentInChildren<Renderer>());
        fotosPorMarco.Add(foto);
        // Placa oscura con el nombre en crema y los anos en naranja; los nombres largos se achican para caber.
        string[] partes = nombre.Split('\n');
        var tn = Text(partes[0], c - upr * .168f + fr * .022f, Quaternion.LookRotation(-fr, upr), Mathf.Min(.036f, .29f / (partes[0].Length * .52f)), new Color(1f, .93f, .78f));
        tn.fontStyle = FontStyle.Bold;
        if (partes.Length > 1)
            Text(partes[1], c - upr * .218f + fr * .022f, Quaternion.LookRotation(-fr, upr), .026f, Color.white);
        Hotspot(nombre.Split('\n')[0], texto, c, new Vector3(.4f, .52f, .3f), CaraYaw[face]);
        if (foto == "homero-gomez")
            for (int m = 0; m < 6; m++)
                Sprite("mariposa", c + fr * .5f - upr * .35f, Vector2.one * Rnd(.07f, .09f), Color.white, Rnd(),
                    new Vector4(Rnd(.15f, .35f), Rnd(-.05f, .35f), Rnd(.5f, 1f) * (Rnd() < .5f ? -1 : 1), Rnd(.08f, .18f)));
    }

    private static void CaraViento()
    {
        const int face = 3;
        Vector3 f = Caras[face];
        foreach (int sx in new[] { -1, 1 })
            Jarron(OnTread(face, 3, sx * .55f), .7f);
        foreach (float x in new[] { -1.8f, -1.2f, -.6f, .6f, 1.2f, 1.8f }) Calaverita(OnTread(face, 0, x) + f * .06f, f, 2.1f);
        Hotspot("Calaveritas de azúcar", TxtCalaveritas, OnTread(face, 0, 0) + Up * .12f, new Vector3(4f, .25f, .36f), CaraYaw[face]);
        Hotspot("Papel picado", TxtPapel, f * (Side(0) / 2 + .06f) + Up * .3f, new Vector3(5.6f, .4f, .2f), CaraYaw[face]);
        Hotspot("Papel picado", TxtPapel, f * (Side(1) / 2 + .06f) + Up * .64f, new Vector3(4.9f, .3f, .2f), CaraYaw[face]);
    }

    // Xoloitzcuintle sentado: piel gris pizarra brillante, orejas de murcielago, ojos ambar y collar de cempasuchil.
    private static void PerroColima(Vector3 p, float yaw)
    {
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Vector3 f = r * Vector3.forward, l = r * Vector3.right;
        const float s = 1.3f;
        Block("cantera", p, new Vector3(.62f, .14f, .8f), yaw, 1);
        Vector3 b = p + Up * .14f;
        Vector3 L(float x, float y, float z) => b + (l * x + Up * y + f * z) * s;
        // Ancas, patas traseras y vientre.
        foreach (int sx in new[] { -1, 1 })
        {
            Ellipsoid("xolo", L(sx * .062f, .12f, -.05f), new Vector3(.068f, .115f, .14f) * s, r * Quaternion.Euler(-20, sx * 6, 0), 7, 12);
            Ellipsoid("xolo", L(sx * .08f, .022f, .06f), new Vector3(.03f, .022f, .075f) * s, r, 3, 6, .5f);
        }
        Ellipsoid("xolo", L(0, .23f, 0), new Vector3(.09f, .17f, .11f) * s, r * Quaternion.Euler(-22, 0, 0), 8, 12);
        Ellipsoid("xolo", L(0, .31f, .065f), new Vector3(.082f, .11f, .088f) * s, r * Quaternion.Euler(-12, 0, 0), 7, 12);
        // Patas delanteras rectas y finas.
        foreach (int sx in new[] { -1, 1 })
        {
            Cono("xolo", L(sx * .046f, .28f, .1f), L(sx * .048f, .04f, .13f), .028f * s, .018f * s, 8);
            Ellipsoid("xolo", L(sx * .048f, .018f, .15f), new Vector3(.022f, .016f, .035f) * s, r, 3, 6, .5f);
        }
        // Cuello largo y cabeza en cuna.
        Cono("xolo", L(0, .38f, .07f), L(0, .5f, .12f), .056f * s, .04f * s, 10);
        Vector3 h = L(0, .54f, .13f);
        Ellipsoid("xolo", h, new Vector3(.053f, .05f, .062f) * s, r, 7, 12);
        Cono("xolo", L(0, .532f, .17f), L(0, .51f, .265f), .033f * s, .016f * s, 10);
        Ellipsoid("negro", L(0, .512f, .266f), new Vector3(.013f, .01f, .009f) * s, r, 3, 5);
        foreach (int sx in new[] { -1, 1 })
        {
            Vector3 ojo = L(sx * .028f, .552f, .182f);
            Ellipsoid("ambar", ojo, new Vector3(.011f, .007f, .006f) * s, r * Quaternion.Euler(0, 0, sx * -12), 3, 6);
            Ellipsoid("negro", ojo + f * .004f * s, new Vector3(.004f, .006f, .003f) * s, r, 2, 4);
            // Orejas grandes, paradas y un poco abiertas.
            Vector3 raiz = L(sx * .03f, .58f, .12f);
            Vector3 u = (l * sx * Mathf.Sin(.3f) + Up * Mathf.Cos(.3f)).normalized, w = f;
            var oreja = new[] { new Vector2(0, -.028f), new Vector2(.1f, -.004f), new Vector2(0, .026f) };
            for (int i = 0; i < oreja.Length; i++) oreja[i] = new Vector2(oreja[i].x, oreja[i].y) * s;
            Extrude("xolo", oreja, raiz, u, w, .006f * s);
            var interior = new[] { new Vector2(.012f, -.016f), new Vector2(.078f, -.003f), new Vector2(.012f, .014f) };
            for (int i = 0; i < interior.Length; i++) interior[i] *= s;
            Extrude("xolo_rosa", interior, raiz + f * .004f * s, u, w, .002f * s);
        }
        for (int k = 0; k < 5; k++) Sphere("tela_negra", L(Rnd(-.012f, .012f), .595f + k * .005f, .13f - k * .012f), .009f * s, 2, 4);
        // Collar de cempasuchil y cola fina enroscada.
        for (int k = 0; k < 11; k++)
        {
            float a = k / 11f * Mathf.PI * 2;
            Vector3 o = L(Mathf.Sin(a) * .058f, .41f - Mathf.Cos(a) * .012f, .085f + Mathf.Cos(a) * .056f);
            Cabeza(o, (o - L(0, .41f, .085f)).normalized, .02f * s, k % 4 == 0 ? "terciopelo" : "cempasuchil");
        }
        Tube("xolo", new[] { L(0, .06f, -.18f), L(.06f, .02f, -.22f), L(.14f, .015f, -.16f), L(.18f, .02f, -.04f), L(.16f, .04f, .04f) }, .012f * s, 6);
        Hotspot("Xoloitzcuintle", TxtXolo, p + Up * .5f, new Vector3(.65f, 1f, .9f), yaw);
    }

    // ---------- dosel de papel picado y faroles ----------

    private static void Dosel()
    {
        Vector3 apex = Up * (ApexY + .05f);
        string[] farol = { "farol_naranja", "farol_rosa", "farol_morado" };
        for (int k = 0; k < 16; k++)
        {
            float a = 11.25f + k * 22.5f;
            // Seis tiras en vez de ocho: de noche el cielo se veia saturado.
            bool conTira = k % 2 == 0 && k % 6 != 4;
            Vector3 b = Pos(a, PosteR, 0), q = Pos(a, PosteR, 4.55f);
            Block("cantera", b, new Vector3(.36f, .16f, .36f), a, 1);
            Rod("madera", b, q + Up * .2f, .055f, 6);
            Ellipsoid(farol[k % 3], q + Up * .46f, new Vector3(.17f, .23f, .17f), Quaternion.identity, 5, 8);
            Cylinder("negro", q + Up * .2f, .07f, .07f, .05f, 8, Quaternion.identity, true);
            Cylinder("negro", q + Up * .68f, .06f, .03f, .06f, 8, Quaternion.identity, true);
            Color glow = k % 3 == 0 ? new Color(1f, .55f, .2f) : k % 3 == 1 ? new Color(1f, .3f, .55f) : new Color(.65f, .35f, 1f);
            Sprite("halo_suave", q + Up * .46f, Vector2.one * .9f, glow, Rnd());
            if (k % 4 == 0) PointLight("Farol " + k, q + Up * .46f, new Color(1f, .6f, .35f), 1.25f, 11f, true);

            if (!conTira) continue;
            var tira = Piece("Papel picado | tira " + (k / 2 + 1), apex, () => Tira(apex, q, .55f, .62f, .38f, .42f, .9f));
            viento.Add(tira);
            tiras.Add(tira);
            ejes.Add((q - apex).normalized);
        }
        Vector3 sw = Pos(225, ArcoR, 2.15f), se = Pos(135, ArcoR, 2.15f);
        Vector3 mid = (sw + se) * .5f;
        var cortina = Piece("Papel picado | cortina sur", mid, () => Tira(sw, se, .22f, .44f, .42f, .5f, .4f));
        viento.Add(cortina);
        tiras.Add(cortina);
        ejes.Add((se - sw).normalized);
    }

    // Cordel con banderas colgando a lo largo de una catenaria aproximada.
    private static void Tira(Vector3 a, Vector3 b, float sag, float spacing, float w, float h, float startGap = 1.1f)
    {
        float len = (b - a).magnitude;
        Vector3 At(float t) => Vector3.Lerp(a, b, t) - Up * (sag * 4 * t * (1 - t));
        var cord = new List<Vector3>();
        for (int i = 0; i <= 24; i++) cord.Add(At(i / 24f));
        Tube("cordel", cord, .007f, 3);
        Vector3 dir = b - a; dir.y = 0; dir.Normalize();
        Vector3 facing = Vector3.Cross(dir, Up);
        for (float s = startGap; s < len - .35f; s += spacing)
            PapelFlag(At(s / len), -dir, facing, w / 2, h, true);
    }

    // ---------- final: animas, monarcas y petalos ----------

    private static void Final()
    {
        // Pocos petalos cayendo siempre; el final suelta muchos mas.
        for (int n = 0; n < 45; n++)
            Sprite("petalo_cae", Pos(Rnd(0, 360), Mathf.Sqrt(Rnd()) * 6.5f * E, Rnd(4.4f, 6f)), Vector2.one * Rnd(.025f, .035f),
                new Color(1f, Rnd(.35f, .65f), .05f), Rnd(), new Vector4(-Rnd(.25f, .4f), 4.8f, .5f, 0));
        var fin = Piece("Final | llegada de las ánimas", Vector3.zero, () =>
        {
            Color[] warm = { new Color(1f, .8f, .5f), new Color(1f, .62f, .3f), new Color(1f, .9f, .75f), new Color(1f, .5f, .6f) };
            for (int n = 0; n < 84; n++)
            {
                int face = n % 4;
                Vector3 f = Caras[face], l = Lat(face);
                Vector3 start = f * Rnd(9.3f * E, 12.5f * E) + l * Rnd(-.6f, .6f) + Up * Rnd(.3f, .9f);
                Vector3 dest = f * Rnd(2.6f, 3.3f) + l * Rnd(-1.2f, 1.2f) + Up * Rnd(1.2f, 2.8f);
                Sprite("anima", start, Vector2.one * Rnd(.11f, .2f), warm[random.Next(warm.Length)], Rnd(), new Vector4(dest.x, dest.y, dest.z, Rnd(.6f, 1.8f)));
            }
            for (int n = 0; n < 48; n++)
                Sprite("mariposa", Vector3.zero, Vector2.one * Rnd(.07f, .1f), Color.white, Rnd(),
                    new Vector4(Rnd(3.2f, 8f), Rnd(1.4f, 5.8f), Rnd(.22f, .5f) * (Rnd() < .5f ? -1 : 1), Rnd(.3f, .8f)));
            for (int n = 0; n < 170; n++)
                Sprite("petalo_cae", Pos(Rnd(0, 360), Mathf.Sqrt(Rnd()) * 8.8f * E, Rnd(4.4f, 6f)), Vector2.one * Rnd(.025f, .04f),
                    new Color(1f, Rnd(.35f, .65f), .05f), Rnd(), new Vector4(-Rnd(.35f, .6f), 4.8f, .7f, 0));
            Sprite("halo_suave", Up * (Top(Niveles - 1) + .55f), Vector2.one * 1.8f, new Color(1f, .6f, .3f), .3f);
        });
        PointLight("Resplandor de la ofrenda", Up * (ApexY - .4f), new Color(1f, .55f, .28f), 2.4f, 18f, true, fin);
        finaleObject = fin.gameObject;
    }

    // ---------- senalizacion y viajes ----------

    private static void Senales()
    {
        Vector3 viewer = Inicio;
        Vector3 pp = new Vector3(-11f, 0, 30.5f);
        Vector3 toViewer = viewer - pp; toViewer.y = 0; toViewer.Normalize();
        Quaternion rot = Quaternion.LookRotation(-toViewer);
        Vector3 lr = rot * Vector3.right;
        Box("tablero_morado", pp + Up * 1.85f, new Vector3(3.7f, 2.3f, .16f), rot, 1);
        foreach (int s in new[] { -1, 1 }) Box("cantera", pp + Up * .35f + lr * s * 1.5f, new Vector3(.3f, .7f, .14f), rot, 1);
        Box("cempasuchil", pp + Up * 3.02f, new Vector3(3.7f, .05f, .18f), rot);
        Vector3 front = pp + toViewer * .09f;
        Vector3 tablero = pp + toViewer * .08f;
        // Marco naranja delgado, titulo crema y los cuatro rumbos en columnas.
        foreach (int s in new[] { -1, 1 })
        {
            Box("letra_naranja", tablero + Up * (1.85f + s * 1.04f), new Vector3(3.44f, .025f, .012f), rot);
            Box("letra_naranja", tablero + Up * 1.85f + lr * s * 1.71f, new Vector3(.025f, 2.1f, .012f), rot);
        }
        Texto3D("OFRENDA DE LOS CUATRO RUMBOS", tablero + Up * 2.64f, rot, .15f, "letra_crema", "tablero_morado");
        Texto3D("Facultad de Informática  ·  UAQ  ·  Concurso de Altares y Ofrendas FIF 2026", tablero + Up * 2.4f, rot, .062f, "letra_naranja", "tablero_morado");
        Box("letra_naranja", tablero + Up * 2.27f, new Vector3(.9f, .012f, .01f), rot);
        string[] rumbos = { "NORTE", "ORIENTE", "PONIENTE", "SUR" }, elementos = { "Fuego", "Agua", "Tierra", "Viento" };
        for (int i = 0; i < 4; i++)
        {
            Vector3 col = tablero + lr * ((i - 1.5f) * .82f);
            Texto3D(rumbos[i], col + Up * 2.06f, rot, .062f, "letra_naranja", "tablero_morado");
            Texto3D(elementos[i], col + Up * 1.88f, rot, .11f, "letra_crema", "tablero_morado");
        }
        Texto3D("Camina al centro y activa los cuatro rituales.", tablero + Up * 1.55f, rot, .078f, "letra_crema", "tablero_morado");
        Texto3D("E  conocer   ·   T  recorrido guiado   ·   O  deja un nombre", tablero + Up * 1.36f, rot, .062f, "letra_naranja", "tablero_morado");
        progreso.Add(Text("RITUALES  0 / 4", front + Up * 1.05f, rot, .12f, new Color(1f, .62f, .2f)));
        Hotspot("Ofrenda de los Cuatro Rumbos", TxtConcepto, pp + Up * 1.85f, new Vector3(3.7f, 2.3f, .4f), rot.eulerAngles.y);

        (string label, Vector3 p)[] viajes =
        {
            ("la entrada", Lay(2, .02f, 27.5f)),
            ("la plaza oriente", Pos(45, 7.9f * E, .03f)),
            ("la plaza poniente", Pos(315, 7.9f * E, .03f)),
            ("lo alto de las gradas", Pos(182, GradasOut + .75f, TerraceY + .04f)),
            ("la terraza", Pos(185, 19.3f * E, TerraceY + .04f)),
        };
        foreach (var v in viajes)
        {
            Cylinder("guia", v.p, .42f, .42f, .03f, 24, Quaternion.identity, true);
            var wi = Hotspot(v.label, "", v.p + Up * .1f, new Vector3(.9f, .2f, .9f), 0, WorldInteractable.Action.Teleport);
            wi.destination = new Vector3(v.p.x, v.p.y, v.p.z);
            Text("VIAJE", v.p + Up * .05f, Quaternion.Euler(90, Quaternion.LookRotation(new Vector3(-v.p.x, 0, -v.p.z) + Vector3.forward * .001f).eulerAngles.y, 0), .1f, new Color(.05f, .2f, .2f));
        }
    }

    // ---------- ofrenda de los visitantes ----------

    // Veladora de plantilla y lugares en el piso, fuera del circulo de petalos, sin tapar caminos ni macetas.
    private static void Visitantes()
    {
        var root = new GameObject("Ofrenda de los visitantes | tecla O");
        root.transform.SetParent(ofrendaRoot);
        var plantilla = Piece("Plantilla de veladora de visitante", Vector3.zero, () =>
        {
            Veladora(Vector3.zero, 1);
            Sprite("flama", Up * .2f, new Vector2(.018f, .036f), Color.white, Rnd());
            Sprite("halo_calido", Up * .2f, Vector2.one * .18f, new Color(1f, .6f, .28f), Rnd());
            Box("mantel_blanco", new Vector3(0, .07f, .09f), new Vector3(.22f, .1f, .012f), Quaternion.Euler(-15, 0, 0));
        }, root.transform);
        var t = Text("nombre", new Vector3(0, .072f, .083f), Quaternion.Euler(-15, 180, 0), .045f, new Color(.25f, .12f, .3f), TextAnchor.MiddleCenter, plantilla);
        t.transform.localPosition = new Vector3(0, .072f, .082f);
        t.transform.localRotation = Quaternion.Euler(-15, 180, 0);
        var lugares = new List<Vector3>();
        var miradas = new List<Vector3>();
        foreach (float r in new[] { PedestalR + 1.45f, PedestalR + 1.85f })
            for (float a = 0; a < 360; a += 5.2f)
            {
                float m45 = Mathf.Repeat(a, 90);
                if (m45 < 8 || m45 > 82 || Mathf.Abs(m45 - 45) < 9) continue;
                lugares.Add(Pos(a, r, .04f));
                miradas.Add(Radial(a));
            }
        root.AddComponent<OfrendaVisitantes>().Setup(plantilla.gameObject, lugares.ToArray(), miradas.ToArray());
        Hotspot("Ofrenda de los visitantes", "Pulsa O para escribir el nombre de alguien que recuerdas: se encenderá una veladora con su nombre alrededor del altar.", Pos(30, PedestalR + 1.6f, .3f), new Vector3(1.2f, .5f, .6f), 30);
    }

    // ---------- conexion con los scripts ----------

    private static void Director()
    {
        currentGroup = ofrendaRoot;
        var director = new GameObject("Director | cuatro rituales").AddComponent<ExperienceDirector>();
        director.transform.SetParent(ofrendaRoot);
        var so = new SerializedObject(director);
        void Fill(string prop, IList<Transform> items)
        {
            var p = so.FindProperty(prop);
            p.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        Fill("fuegoGroups", fuego);
        Fill("aguaGroups", agua);
        Fill("tierraGroups", tierra);
        Fill("vientoGroups", viento);
        var texts = so.FindProperty("progressTexts");
        texts.arraySize = progreso.Count;
        for (int i = 0; i < progreso.Count; i++) texts.GetArrayElementAtIndex(i).objectReferenceValue = progreso[i];
        so.FindProperty("finale").objectReferenceValue = finaleObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        var sonido = new GameObject("Ambiente sonoro").AddComponent<AmbienteSonoro>();
        sonido.transform.SetParent(ofrendaRoot);
        var sso = new SerializedObject(sonido);
        sso.FindProperty("altar").objectReferenceValue = ofrendaRoot;
        sso.ApplyModifiedPropertiesWithoutUndo();
        new GameObject("Recorrido guiado | tecla T").AddComponent<RecorridoGuiado>().transform.SetParent(ofrendaRoot);
        var wind = ofrendaRoot.gameObject.AddComponent<PapelPicadoViento>();
        wind.Setup(tiras.ToArray(), ejes.ToArray());
        var fotos = ofrendaRoot.gameObject.AddComponent<RetratosOfrenda>();
        fotos.Setup(retratos.ToArray(), fotosPorMarco.ToArray());
        EditorUtility.SetDirty(wind);
        EditorUtility.SetDirty(fotos);
        finaleObject.SetActive(false);
        foreach (var set in new[] { fuego, viento })
            foreach (var g in set) g.gameObject.SetActive(false);
    }
}
