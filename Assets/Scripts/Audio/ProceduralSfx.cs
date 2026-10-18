using UnityEngine;

namespace Rewind.Audio
{
    public static class ProceduralSfx
    {
        const int Rate = 22050;

        delegate float Synth(float t);

        static readonly System.Random Rng = new System.Random(1234);
        static float Noise() => (float)(Rng.NextDouble() * 2 - 1);

        static float Sine(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float Warm(float t, float f) => (Sine(t, f) + Sine(t, f * 1.004f) * 0.7f + Sine(t, f * 0.5f) * 0.25f) * 0.5f;
        static float Env(float t, float attack, float decay) => t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);

        static AudioClip Make(string name, float seconds, Synth synth, float cutoff, float reverb, float peak = 0.6f, bool loop = false)
        {
            var n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (var i = 0; i < n; i++) data[i] = synth(i / (float)Rate);

            LowPass(data, cutoff, loop);
            if (reverb > 0) Reverb(data, reverb);
            if (!loop)
            {
                var fade = Mathf.Min(Rate / 20, n);
                for (var i = 0; i < fade; i++) data[n - 1 - i] *= i / (float)fade;
            }
            Normalize(data, peak);

            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void LowPass(float[] data, float cutoff, bool loop)
        {
            var rc = 1f / (2 * Mathf.PI * cutoff);
            var a = (1f / Rate) / (rc + 1f / Rate);
            float y1 = 0, y2 = 0;
            if (loop)
            {
                foreach (var v in data)
                {
                    y1 += a * (v - y1);
                    y2 += a * (y1 - y2);
                }
            }
            for (var i = 0; i < data.Length; i++)
            {
                y1 += a * (data[i] - y1);
                y2 += a * (y1 - y2);
                data[i] = y2;
            }
        }

        static void Reverb(float[] data, float mix)
        {
            var delays = new[] { 1116, 1188, 1277, 1356 };
            var wet = new float[data.Length];
            foreach (int d in delays)
            {
                var buf = new float[d];
                var idx = 0;
                for (var i = 0; i < data.Length; i++)
                {
                    var y = buf[idx];
                    buf[idx] = data[i] + y * 0.78f;
                    idx = (idx + 1) % d;
                    wet[i] += y * 0.25f;
                }
            }
            for (var i = 0; i < data.Length; i++) data[i] = data[i] * (1 - mix * 0.5f) + wet[i] * mix;
        }

        static void Normalize(float[] data, float peak)
        {
            var max = 0.0001f;
            foreach (var v in data) max = Mathf.Max(max, Mathf.Abs(v));
            var k = peak / max;
            for (var i = 0; i < data.Length; i++) data[i] *= k;
        }

        public static AudioClip Click() => Make("p_click", 0.35f,
            t => Sine(t, 120 - 40 * Mathf.Min(1, t * 20)) * Env(t, 0.003f, 0.045f) + Noise() * 0.35f * Env(t, 0.002f, 0.02f),
            cutoff: 1800, reverb: 0.25f, peak: 0.5f);

        public static AudioClip Buy() => Make("p_buy", 0.9f,
            t => Warm(t, 330) * Env(t, 0.01f, 0.18f) + Warm(t, 495) * Env(Mathf.Max(0, t - 0.08f), 0.01f, 0.25f) * (t > 0.08f ? 1 : 0),
            cutoff: 2400, reverb: 0.4f);

        public static AudioClip Denied() => Make("p_denied", 0.5f,
            t => Sine(t, 110 - 30 * t) * Env(t, 0.01f, 0.12f) + Sine(t, 82) * 0.5f * Env(t, 0.02f, 0.15f),
            cutoff: 900, reverb: 0.2f, peak: 0.5f);

        public static AudioClip Milestone() => Make("p_milestone", 2.2f,
            t => (Warm(t, 262) + Warm(t, 330) * 0.8f + Warm(t, 392) * 0.7f) * Env(t, 0.25f, 0.6f),
            cutoff: 2000, reverb: 0.55f);

        public static AudioClip Unlock() => Make("p_unlock", 1.6f,
            t => Warm(t, 220 + 110 * Mathf.SmoothStep(0, 1, t / 0.6f)) * Env(t, 0.15f, 0.45f),
            cutoff: 1800, reverb: 0.5f);

        public static AudioClip Zap() => Make("p_zap", 0.6f,
            t => Noise() * Env(t, 0.03f, 0.12f) * (0.6f + 0.4f * Sine(t, 18)) + Sine(t, 70) * 0.4f * Env(t, 0.01f, 0.1f),
            cutoff: 1200, reverb: 0.35f, peak: 0.45f);

        public static AudioClip Win() => Make("p_win", 5f, t =>
        {
            var a = (Warm(t, 196) + Warm(t, 247) * 0.8f + Warm(t, 294) * 0.7f) * Env(t, 0.6f, 1.2f);
            var b = t > 1.6f ? (Warm(t, 262) + Warm(t, 330) * 0.8f + Warm(t, 392) * 0.8f) * Env(t - 1.6f, 0.8f, 1.6f) : 0;
            return a + b;
        }, cutoff: 1800, reverb: 0.6f);

        public static AudioClip UiClick() => Make("p_ui", 0.25f,
            t => Sine(t, 420) * Env(t, 0.002f, 0.03f) + Sine(t, 210) * 0.5f * Env(t, 0.002f, 0.05f),
            cutoff: 2200, reverb: 0.2f, peak: 0.4f);

        public static AudioClip MotorLoop() => Make("p_motor", 1f,
            t => Sine(t, 55) * 0.5f + Sine(t, 110) * 0.25f + Sine(t, 165) * 0.08f + Noise() * 0.08f,
            cutoff: 500, reverb: 0f, peak: 0.5f, loop: true);
    }
}
