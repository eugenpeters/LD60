using Rewind.Audio;
using Rewind.Core;
using Rewind.Game;
using Rewind.Rendering;
using UnityEngine;

namespace Rewind.View
{
    public class EffectsDirector : MonoBehaviour
    {
        [SerializeField] CassetteSpinner spinner;
        [SerializeField] Camera sceneCamera;
        [SerializeField] LowResScreen screen;
        [SerializeField] Color baseBackground = new Color(0.06f, 0.04f, 0.1f);

        [Header("Scene effects")]
        [SerializeField] ParticleSystem sparks;
        [SerializeField] ParticleSystem dust;
        [SerializeField] ParticleSystem stars;
        [SerializeField] ParticleSystem plasma;
        [SerializeField] ParticleSystem accretion;
        [SerializeField] ParticleSystem infall;
        [SerializeField] ParticleSystem warp;
        [SerializeField] ParticleSystem[] galaxies;
        [SerializeField] LightningArcs arcs;
        [SerializeField] Transform arcRoot;
        [SerializeField] LineRenderer[] motionArcs;
        [SerializeField] Transform blackHole;
        [SerializeField] Transform[] minis;
        [SerializeField] BrightFx bright;

        [Header("Layer thresholds (intensity)")]
        [SerializeField] float streamersAt = 0.2f;
        [SerializeField] float speedLinesAt = 0.5f;
        [SerializeField] float shockwavesAt = 1.2f;
        [SerializeField] float lightningAt = 2;
        [SerializeField] float lightRaysAt = 3;
        [SerializeField] float plasmaAt = 4;
        [SerializeField] float blackHoleAt = 9;
        [SerializeField] float miniCassettesAt = 14;
        [SerializeField] float warpAt = 20;
        [SerializeField] float galaxiesAt = 26;
        [SerializeField] float realityTearAt = 38;

        float arcAngle;
        Vector3 cameraHome;
        float shake;
        float invertFlash;
        float tearTimer;
        float whiteFlash;

        static readonly int NoiseId = Shader.PropertyToID("_Noise");
        static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int AberrationId = Shader.PropertyToID("_Aberration");
        static readonly int InvertId = Shader.PropertyToID("_Invert");
        static readonly int LensId = Shader.PropertyToID("_Lens");
        static readonly int CurvatureId = Shader.PropertyToID("_Curvature");
        static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        static readonly int FlashId = Shader.PropertyToID("_Flash");

        float baseNoise, baseAberration, baseCurvature, baseBrightness;
        Material crtMaterial;

        void Start()
        {
            cameraHome = sceneCamera.transform.localPosition;
            crtMaterial = screen != null ? screen.Material : null;
            if (crtMaterial != null)
            {
                baseNoise = crtMaterial.GetFloat(NoiseId);
                baseAberration = crtMaterial.GetFloat(AberrationId);
                baseCurvature = crtMaterial.GetFloat(CurvatureId);
                baseBrightness = crtMaterial.GetFloat(BrightnessId);
            }
            arcs.ArcStruck = () => { AudioManager.Instance?.Play(Sfx.Zap, 0.35f, 0.2f); shake += 0.05f; };
            var gm = GameManager.Instance;
            gm.Clicked += OnClicked;
            gm.TierPurchased += _ => { Burst(sparks, 30); bright.Pulse(); whiteFlash = Mathf.Max(whiteFlash, 0.35f); };
            gm.MilestoneReached += _ => { Burst(sparks, 80); shake += 0.4f; bright.Pulse(); whiteFlash = 1; };
            gm.GoalReached += () => { invertFlash = 1; shake += 2; Burst(sparks, 300); whiteFlash = 1.5f; };
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Clicked -= OnClicked;
        }

        void OnClicked(BigNumber _)
        {
            Burst(sparks, 6 + Mathf.RoundToInt(spinner.Intensity));
            Burst(dust, 3);
            shake += 0.03f;
        }

        static void Burst(ParticleSystem ps, int count)
        {
            if (ps != null) ps.Emit(count);
        }

        float Layer(float threshold, float span = 4) => Mathf.Clamp01((spinner.Intensity - threshold) / span);

        static void Rate(ParticleSystem ps, float rate)
        {
            var e = ps.emission;
            e.rateOverTime = rate;
        }

        void Update()
        {
            var I = spinner.Intensity;
            var dt = Time.deltaTime;

            var arcAlpha = Layer(speedLinesAt, 2) * Mathf.Clamp01(spinner.Speed01 * 1.5f);
            arcAngle -= spinner.DisplayedRps * 360f * 1.3f * dt;
            arcRoot.localRotation = Quaternion.Euler(0, 0, arcAngle);
            arcRoot.localScale = Vector3.one * (1 + Layer(plasmaAt, 10) * 0.25f);
            for (var i = 0; i < motionArcs.Length; i++)
            {
                var lr = motionArcs[i];
                lr.enabled = arcAlpha > 0.01f;
                var head = new Color(1, 1, 1, arcAlpha);
                lr.startColor = head;
                lr.endColor = new Color(0.5f, 0.7f, 1f, 0);
            }

            arcs.Amount = Layer(lightningAt, 6);
            arcs.Reach = 1 + Layer(plasmaAt, 20) * 1.5f;

            Rate(plasma, Layer(plasmaAt) * 400);

            bright.Tick(Layer(streamersAt, 1), Layer(shockwavesAt, 6), Layer(lightRaysAt, 6), dt);
            whiteFlash = Mathf.MoveTowards(whiteFlash, 0, dt * 3);

            var bh = Layer(blackHoleAt, 3);
            var bhSize = bh * (1.5f + Layer(blackHoleAt + 3, 30) * 8f);
            blackHole.localScale = Vector3.one * bhSize;
            blackHole.localPosition = new Vector3(0, 0, 3f + bhSize * 0.6f);
            blackHole.gameObject.SetActive(bh > 0.001f);
            Rate(accretion, bh * 350);
            Rate(infall, bh * 120);

            var mini = Layer(miniCassettesAt, 6);
            var miniCount = Mathf.CeilToInt(mini * minis.Length);
            for (var i = 0; i < minis.Length; i++)
            {
                var on = i < miniCount;
                if (minis[i].gameObject.activeSelf != on) minis[i].gameObject.SetActive(on);
                if (!on) continue;
                var t = Time.time * (0.4f + 0.05f * i) + i * Mathf.PI * 2 / minis.Length;
                var r = 3.2f + (i % 3) * 0.6f;
                minis[i].localPosition = new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r * 0.55f, 1 + Mathf.Sin(t) * 1.5f);
                minis[i].localRotation = Quaternion.Euler(Time.time * 40 * (i + 1), Time.time * 90, Time.time * 700);
                minis[i].localScale = Vector3.one * 0.6f;
            }

            Rate(warp, Layer(warpAt) * 500);
            Rate(stars, 40 * (1 - Layer(warpAt)));
            for (var i = 0; i < galaxies.Length; i++)
            {
                Rate(galaxies[i], Layer(galaxiesAt + i * 3) * 250);
                galaxies[i].transform.Rotate(0, 0, -dt * 20, Space.Self);
            }

            var tear = Layer(realityTearAt, 10);
            tearTimer -= dt;
            if (tear > 0 && tearTimer <= 0)
            {
                tearTimer = Random.Range(1.5f, 4f) / (0.5f + tear);
                invertFlash = Random.value < 0.4f * tear ? 1 : invertFlash;
                shake += 0.3f * tear;
            }
            invertFlash = Mathf.MoveTowards(invertFlash, 0, dt * 6);

            var bg = baseBackground;
            if (tear > 0)
            {
                var cycle = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.05f, 1), 0.85f, 0.14f);
                bg = Color.Lerp(baseBackground, cycle, tear);
            }
            sceneCamera.backgroundColor = bg;

            shake = Mathf.Min(shake, 2.5f);
            shake = Mathf.MoveTowards(shake, 0, dt * (1 + shake * 3));
            var constantShake = Layer(lightningAt, 30) * 0.05f;
            var amp = shake * 0.15f + constantShake;
            sceneCamera.transform.localPosition = cameraHome + (Vector3)(Random.insideUnitCircle * amp);

            UpdateCrt(I, bh, tear);
        }

        void UpdateCrt(float I, float bh, float tear)
        {
            if (crtMaterial == null) return;
            crtMaterial.SetFloat(NoiseId, baseNoise + Layer(plasmaAt, 30) * 0.08f + tear * 0.15f);
            crtMaterial.SetFloat(AberrationId, baseAberration + Layer(lightningAt, 40) * 0.006f + shake * 0.002f);
            var pulse = Layer(plasmaAt, 8) * 0.25f * Mathf.Max(0, Mathf.Sin(Time.time * 2.3f)) * 0.04f;
            crtMaterial.SetFloat(CurvatureId, baseCurvature + pulse);
            crtMaterial.SetFloat(GlitchId, Mathf.Max(tear * 0.4f * (Mathf.PerlinNoise(Time.time * 3, 0) > 0.6f ? 1 : 0.1f), invertFlash * 0.8f));
            crtMaterial.SetFloat(InvertId, invertFlash > 0.5f ? 1 : 0);
            crtMaterial.SetFloat(BrightnessId, baseBrightness + Layer(lightRaysAt, 30) * 0.2f);
            crtMaterial.SetFloat(FlashId, Mathf.Clamp01(whiteFlash) * 0.7f);

            if (bh > 0.001f)
            {
                var vp = sceneCamera.WorldToViewportPoint(blackHole.position);
                var worldRadius = blackHole.lossyScale.x * 0.5f;
                var dist = Mathf.Max(0.1f, vp.z);
                var screenRadius = worldRadius / (2 * dist * Mathf.Tan(sceneCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));
                crtMaterial.SetVector(LensId, new Vector4(vp.x, vp.y, screenRadius, bh * (1 + Layer(blackHoleAt, 30))));
            }
            else crtMaterial.SetVector(LensId, Vector4.zero);
        }
    }
}
