using Rewind.Audio;
using Rewind.Core;
using Rewind.Game;
using UnityEngine;

namespace Rewind.View
{
    public class CassetteSpinner : MonoBehaviour
    {
        [SerializeField] Transform cassette;
        [SerializeField] Transform wobbleRoot;
        [SerializeField] float visibleCap = 8f;
        [SerializeField] float clickImpulse = 1.6f;
        [SerializeField] float impulseDecay = 3f;
        [SerializeField] float direction = -1f;

        [Header("Motion ghosts")]
        [SerializeField] Transform[] ghosts;
        [SerializeField] float ghostStartSpeed = 4f;
        [SerializeField] float ghostSpacingDegrees = 9f;

        public float DisplayedRps { get; private set; }
        public float Speed01 => Mathf.Clamp01(DisplayedRps / visibleCap);
        public float Intensity { get; private set; }
        public float VisibleCap => visibleCap;
        public Transform SpinTransform => cassette;

        float clickRps;
        float angle;
        float punch;
        MaterialPropertyBlock mpb;
        Renderer[] ghostRenderers;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            ghostRenderers = new Renderer[ghosts.Length];
            for (var i = 0; i < ghosts.Length; i++) ghostRenderers[i] = ghosts[i].GetComponentInChildren<Renderer>();
            var gm = GameManager.Instance;
            gm.Clicked += _ => { clickRps += clickImpulse; punch = 1; };
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var playing = gm.Flow == Flow.Playing;
            var dt = Time.deltaTime;

            clickRps = Mathf.Max(0, clickRps - clickRps * impulseDecay * dt - 0.2f * dt);

            var passive = gm.Economy.PassiveRps;
            var passiveLog = passive.IsZero ? double.NegativeInfinity : passive.Log10();
            var passiveVisible = passiveLog > 2 ? visibleCap : (float)passive.ToDouble();
            var target = playing ? Mathf.Min(visibleCap, passiveVisible + clickRps) : 0.15f;

            var k = target > DisplayedRps ? 10f : 2.5f;
            DisplayedRps = Mathf.Lerp(DisplayedRps, target, 1 - Mathf.Exp(-k * dt));

            var realRps = passive.ToDouble() + clickRps;
            var realLog = passiveLog > 300 ? (float)passiveLog : (float)System.Math.Log10(System.Math.Max(1, realRps));
            Intensity = playing || gm.Flow == Flow.Won ? Mathf.Lerp(Intensity, realLog, 1 - Mathf.Exp(-3 * dt)) : Mathf.Lerp(Intensity, 0, dt);

            angle = Mathf.Repeat(angle + direction * DisplayedRps * 360f * dt, 360f);
            cassette.localRotation = Quaternion.Euler(0, 0, angle);

            punch = Mathf.MoveTowards(punch, 0, dt * 6);
            var s = 1 + 0.06f * Mathf.Sin(punch * Mathf.PI);
            wobbleRoot.localScale = new Vector3(s, s, 1);
            var shake = Mathf.Clamp01((Intensity - 2) / 20f) * 2f;
            wobbleRoot.localRotation = Quaternion.Euler(
                Mathf.Sin(Time.time * 23) * shake, Mathf.Cos(Time.time * 19) * shake, 0);

            UpdateGhosts();
            AudioManager.Instance?.SetMotor(Speed01, playing);
        }

        void UpdateGhosts()
        {
            if (ghosts == null) return;
            var a = Mathf.Clamp01((DisplayedRps - ghostStartSpeed) / (visibleCap - ghostStartSpeed));
            for (var i = 0; i < ghosts.Length; i++)
            {
                var g = ghostRenderers[i];
                var on = a > 0.01f;
                if (g.enabled != on) g.enabled = on;
                if (!on) continue;
                ghosts[i].localRotation = Quaternion.Euler(0, 0, angle - direction * ghostSpacingDegrees * (i + 1) * (0.5f + a));
                mpb.SetColor(ColorId, new Color(1, 1, 1, a * 0.45f / (i + 1)));
                g.SetPropertyBlock(mpb);
            }
        }
    }
}
