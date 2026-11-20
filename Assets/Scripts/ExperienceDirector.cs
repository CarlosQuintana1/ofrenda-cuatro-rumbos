using System.Collections;
using UnityEngine;

namespace MemoriaViva
{
    // Los cuatro rituales: 0 fuego, 1 agua, 2 tierra, 3 viento.
    public sealed class ExperienceDirector : MonoBehaviour
    {
        [SerializeField] private TextMesh[] progressTexts;
        [SerializeField] private GameObject finale;
        [SerializeField] private Transform[] fuegoGroups;
        [SerializeField] private Transform[] aguaGroups;
        [SerializeField] private Transform[] tierraGroups;
        [SerializeField] private Transform[] vientoGroups;
        private readonly bool[] offerings = new bool[4];
        private int collected;

        public int Collected => collected;

        private void Start()
        {
            if (finale != null) finale.SetActive(false);
            // Comida y agua ya estan servidas; los rituales de agua y tierra las hacen brotar de nuevo.
            foreach (var set in new[] { fuegoGroups, vientoGroups })
                if (set != null) foreach (var g in set) if (g != null) g.gameObject.SetActive(false);
            UpdateText();
        }

        public void ReceiveOffering(int index)
        {
            if (index < 0 || index >= offerings.Length || offerings[index]) return;
            offerings[index] = true;
            collected++;
            UpdateText();
            FindFirstObjectByType<CicloDeLuz>()?.Oscurecer(collected / (float)offerings.Length);
            FindFirstObjectByType<AmbienteSonoro>()?.Ritual(index);
            Efectos.Lanzar((Efectos.Tipo)index, Centro(GroupsFor(index)));
            if (index == 0) Efectos.Brasas(Vector3.up * 1.4f, 2.6f);
            StartCoroutine(Ritual(index));
        }

        private static Vector3 Centro(Transform[] groups)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            if (groups != null) foreach (var g in groups) if (g != null) { sum += g.position; n++; }
            return n > 0 ? sum / n + Vector3.up * .3f : Vector3.up;
        }

        private Transform[] GroupsFor(int index) =>
            index == 0 ? fuegoGroups : index == 1 ? aguaGroups : index == 2 ? tierraGroups : vientoGroups;

        private IEnumerator Ritual(int index)
        {
            var groups = GroupsFor(index);
            if (groups != null)
            {
                foreach (var g in groups)
                {
                    if (g == null) continue;
                    StartCoroutine(Grow(g, index == 3));
                    yield return new WaitForSeconds(index == 3 ? .12f : .28f);
                }
            }
            if (collected == offerings.Length)
            {
                yield return new WaitForSeconds(1.2f);
                if (finale != null) finale.SetActive(true);
                Efectos.Lanzar(Efectos.Tipo.Final, Vector3.up * 3.2f);
                FindFirstObjectByType<AmbienteSonoro>()?.Final();
                var explorer = FindFirstObjectByType<VRExplorer>();
                if (explorer != null)
                    explorer.ShowMessage("Las ánimas llegan a la ofrenda",
                        "Con el fuego, el agua, la tierra y el viento la ofrenda está completa. Las luces siguen el camino de cempasúchil. La monarca llega a México en estas fechas y, para pueblos como el purépecha, representa a las almas que regresan. (Fuente: Expansión, 2025) Deja encendida una veladora con el nombre de alguien que extrañas: presiona O.", 16f);
            }
        }

        // Fuego, agua y tierra crecen hacia arriba; el viento se despliega desde la cima del arco.
        private IEnumerator Grow(Transform t, bool uniform)
        {
            Vector3 target = t.localScale;
            t.gameObject.SetActive(true);
            float time = 0, duration = uniform ? 1.4f : .7f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / duration);
                float e = 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2);
                t.localScale = uniform ? target * e : new Vector3(target.x, target.y * e, target.z);
                yield return null;
            }
            t.localScale = target;
        }

        // Para vistas previas en el editor: deja todo como si ya se hubieran hecho los rituales.
        public void ForceComplete()
        {
            for (int i = 0; i < 4; i++)
            {
                offerings[i] = true;
                var groups = GroupsFor(i);
                if (groups != null) foreach (var g in groups) if (g != null) g.gameObject.SetActive(true);
            }
            collected = 4;
            if (finale != null) finale.SetActive(true);
            UpdateText();
        }

        private void UpdateText()
        {
            if (progressTexts == null) return;
            foreach (var t in progressTexts)
            {
                if (t == null) continue;
                t.text = collected < 4
                    ? "RITUALES  " + collected + " / 4"
                    : "LA OFRENDA ESTÁ COMPLETA";
            }
        }
    }
}
