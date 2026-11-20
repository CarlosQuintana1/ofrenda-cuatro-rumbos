using UnityEngine;

namespace MemoriaViva
{
    // Movimiento ciclico simple: rebote, vaiven y giro. Lo usan danzantes, musicos y calaveritas.
    public sealed class Balanceo : MonoBehaviour
    {
        public float rebote = .05f;
        public float frecuencia = 2f;
        public float vaiven = 4f;
        public float giro;
        public float fase;
        private Vector3 posicion;
        private Quaternion rotacion;

        private void Start()
        {
            posicion = transform.localPosition;
            rotacion = transform.localRotation;
            if (fase == 0) fase = Random.value * 6.28f;
        }

        private void Update()
        {
            float t = Time.time * frecuencia + fase;
            transform.localPosition = posicion + Vector3.up * Mathf.Abs(Mathf.Sin(t)) * rebote;
            transform.localRotation = rotacion * Quaternion.Euler(0, giro * Time.time, Mathf.Sin(t) * vaiven);
        }
    }
}
