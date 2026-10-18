using UnityEngine;

namespace Rewind.View
{
    public class FloatingMotion : MonoBehaviour
    {
        [SerializeField] Vector3 angleAmplitude = new Vector3(16f, 24f, 10f);
        [SerializeField] Vector3 positionAmplitude = new Vector3(0.15f, 0.12f, 0.2f);
        [SerializeField] float speed = 0.06f;

        Quaternion baseRotation;
        Vector3 basePosition;
        float seed;

        void Awake()
        {
            baseRotation = transform.localRotation;
            basePosition = transform.localPosition;
            seed = Random.Range(0f, 100f);
        }

        float Noise(float row) => Mathf.PerlinNoise(seed + Time.time * speed, row) * 2f - 1f;

        void Update()
        {
            var euler = new Vector3(
                Noise(1.3f) * angleAmplitude.x,
                Noise(7.1f) * angleAmplitude.y,
                Noise(13.7f) * angleAmplitude.z);
            transform.localRotation = baseRotation * Quaternion.Euler(euler);
            transform.localPosition = basePosition + new Vector3(
                Noise(21.9f) * positionAmplitude.x,
                Noise(29.3f) * positionAmplitude.y,
                Noise(37.5f) * positionAmplitude.z);
        }
    }
}
