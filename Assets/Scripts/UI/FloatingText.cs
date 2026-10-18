using Rewind.Core;
using Rewind.Game;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rewind.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] RectTransform layer;
        [SerializeField] TextMeshProUGUI template;
        [SerializeField] TextMeshProUGUI toast;
        [SerializeField] int poolSize = 24;

        TextMeshProUGUI[] pool;
        float[] age;
        Vector2[] velocity;
        int next;
        float toastTimer;
        const float Life = 0.9f;

        void Start()
        {
            pool = new TextMeshProUGUI[poolSize];
            age = new float[poolSize];
            velocity = new Vector2[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                pool[i] = Instantiate(template, layer);
                pool[i].gameObject.SetActive(false);
                age[i] = Life;
            }
            template.gameObject.SetActive(false);
            toast.alpha = 0;

            var gm = GameManager.Instance;
            gm.Clicked += Spawn;
            gm.TierRevealed += t => Toast($"NEW UPGRADE: {gm.Economy.Config.tiers[t].name.ToUpperInvariant()}");
            gm.MilestoneReached += t => Toast($"{gm.Economy.Config.tiers[t].name.ToUpperInvariant()} OUTPUT x2!");
        }

        void Spawn(BigNumber gain)
        {
            var screen = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
            var t = pool[next];
            t.gameObject.SetActive(true);
            t.text = "+" + NumberFormatter.FormatMeters(gain);
            t.rectTransform.anchoredPosition = local + new Vector2(Random.Range(-20f, 20f), 20);
            velocity[next] = new Vector2(Random.Range(-60f, 60f), Random.Range(160f, 240f));
            age[next] = 0;
            next = (next + 1) % pool.Length;
        }

        public void Toast(string message)
        {
            toast.text = message;
            toastTimer = 2.5f;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < pool.Length; i++)
            {
                if (age[i] >= Life) continue;
                age[i] += dt;
                var t = pool[i];
                if (age[i] >= Life) { t.gameObject.SetActive(false); continue; }
                t.rectTransform.anchoredPosition += velocity[i] * dt;
                velocity[i] *= 1 - dt * 2;
                t.alpha = 1 - age[i] / Life;
                var s = 1 + 0.3f * Mathf.Max(0, 1 - age[i] * 8);
                t.rectTransform.localScale = new Vector3(s, s, 1);
            }

            toastTimer -= dt;
            toast.alpha = Mathf.Clamp01(toastTimer * 2);
        }
    }
}
