using UnityEngine;

namespace Rewind.View
{
    public class LightningArcs : MonoBehaviour
    {
        [SerializeField] LineRenderer[] lines;
        public float radiusMin = 0.4f, radiusMax = 1.4f;
        public float jitter = 0.12f;
        public float refreshRate = 20f;
        public float width = 0.06f;

        public float Amount { get; set; }
        public float Reach { get; set; } = 1;
        public System.Action ArcStruck;

        float timer;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0) return;
            timer = 1f / refreshRate;
            var active = Mathf.RoundToInt(Amount * lines.Length);
            var struck = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var lr = lines[i];
                var on = i < active && Random.value < 0.75f;
                lr.enabled = on;
                if (!on) continue;
                struck = true;
                var segments = lr.positionCount;
                var ang = Random.Range(0, Mathf.PI * 2);
                var rad = Random.Range(radiusMin, radiusMax) * Reach;
                var end = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad, Random.Range(-0.2f, 0.2f));
                for (var s = 0; s < segments; s++)
                {
                    var t = s / (float)(segments - 1);
                    var p = Vector3.Lerp(Vector3.zero, end, t);
                    if (s > 0 && s < segments - 1) p += (Vector3)Random.insideUnitCircle * jitter * Reach;
                    lr.SetPosition(s, p);
                }
                lr.widthMultiplier = width * Random.Range(0.6f, 1.4f) * Mathf.Lerp(1, 2.5f, Reach - 1);
            }
            if (struck && Random.value < 0.25f) ArcStruck?.Invoke();
        }
    }
}
