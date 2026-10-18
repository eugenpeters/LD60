using UnityEngine;

namespace Rewind.View
{
    public class BrightFx : MonoBehaviour
    {
        const float RingLife = 0.9f;

        [SerializeField] CassetteSpinner spinner;
        [SerializeField] TrailRenderer[] streamers;
        [SerializeField] LineRenderer[] rings;
        [SerializeField] Transform raysRoot;
        [SerializeField] LineRenderer[] rays;

        float[] ringAge;
        int nextRing;
        float ringTimer;

        void Awake()
        {
            ringAge = new float[rings.Length];
            for (var i = 0; i < ringAge.Length; i++) ringAge[i] = RingLife;
        }

        public void Pulse()
        {
            ringAge[nextRing] = 0;
            rings[nextRing].enabled = true;
            nextRing = (nextRing + 1) % rings.Length;
        }

        public void Tick(float streamer, float shock, float rayWeight, float dt)
        {
            var sw = streamer * Mathf.Clamp01((spinner.DisplayedRps - 1.5f) / 3f);
            foreach (var tr in streamers)
            {
                tr.emitting = sw > 0.02f;
                tr.startColor = new Color(1, 1, 1, sw);
                tr.endColor = new Color(0.8f, 0.9f, 1f, 0);
            }

            if (shock > 0.01f)
            {
                ringTimer -= dt;
                if (ringTimer <= 0)
                {
                    Pulse();
                    ringTimer = Mathf.Lerp(2.5f, 0.25f, shock);
                }
            }
            for (var i = 0; i < rings.Length; i++)
            {
                if (ringAge[i] >= RingLife) continue;
                ringAge[i] += dt;
                var lr = rings[i];
                if (ringAge[i] >= RingLife) { lr.enabled = false; continue; }
                var u = ringAge[i] / RingLife;
                var radius = Mathf.Lerp(0.6f, 6.5f, 1 - (1 - u) * (1 - u));
                lr.transform.localScale = Vector3.one * radius;
                lr.widthMultiplier = Mathf.Lerp(0.32f, 0.04f, u);
                var c = new Color(1, 1, 1, 1 - u * u);
                lr.startColor = lr.endColor = c;
            }

            var raysOn = rayWeight > 0.01f;
            raysRoot.localRotation *= Quaternion.Euler(0, 0, dt * (8 + 30 * rayWeight));
            for (var i = 0; i < rays.Length; i++)
            {
                var lr = rays[i];
                lr.enabled = raysOn;
                if (!raysOn) continue;
                var breathe = 0.55f + 0.45f * Mathf.Sin(Time.time * (1.3f + i * 0.17f) + i);
                var a = rayWeight * breathe;
                lr.startColor = new Color(1, 1, 1, a);
                lr.endColor = new Color(0.9f, 0.95f, 1f, 0);
            }
        }
    }
}
