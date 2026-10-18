using Rewind.Core;
using Rewind.Game;
using Rewind.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rewind.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TextMeshProUGUI distance;
        [SerializeField] TextMeshProUGUI flavor;
        [SerializeField] TextMeshProUGUI perSecond;
        [SerializeField] Image goalFill;
        [SerializeField] TextMeshProUGUI goalLabel;
        [SerializeField] TextMeshProUGUI hint;
        [SerializeField] CassetteSpinner spinner;
        [SerializeField] Button menuButton;

        float refresh;
        float punch;

        void Start()
        {
            GameManager.Instance.Clicked += _ => punch = 1;
            menuButton.onClick.AddListener(() =>
            {
                Audio.AudioManager.Instance?.Play(Audio.Sfx.UiClick);
                GameManager.Instance.Pause();
            });
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var visible = gm.Flow != Flow.Menu;
            menuButton.gameObject.SetActive(gm.Flow == Flow.Playing);
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime * 4);

            punch = Mathf.MoveTowards(punch, 0, Time.deltaTime * 8);
            distance.rectTransform.localScale = Vector3.one * (1 + 0.05f * punch);

            var clicks = gm.Economy.State.Clicks;
            hint.alpha = Mathf.MoveTowards(hint.alpha, clicks < 15 && gm.Flow == Flow.Playing ? 0.6f + 0.4f * Mathf.Sin(Time.time * 4) : 0, Time.deltaTime * 2);

            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0) return;
            refresh = 0.05f;

            var e = gm.Economy;
            distance.text = NumberFormatter.FormatMeters(e.State.Distance);
            flavor.text = e.State.TotalDistance.IsZero
                ? NumberFormatter.Flavor(e.State.TotalDistance)
                : "TOTAL " + NumberFormatter.FormatMeters(e.State.TotalDistance) + "   " + NumberFormatter.Flavor(e.State.TotalDistance);
            var mps = e.MetersPerSecond;
            var visual = spinner != null && spinner.DisplayedRps >= spinner.VisibleCap * 0.98f ? "  <color=#ff9e2e>[ULTRA]</color>" : "";
            perSecond.text = mps.IsZero
                ? "click to rewind"
                : NumberFormatter.FormatMeters(mps) + "/s   ·   " + NumberFormatter.Format(e.PassiveRps) + " rot/s" + visual;
            goalFill.fillAmount = e.GoalProgress;
            goalLabel.text = $"END OF THE MULTIVERSE  {(e.GoalProgress * 100).ToString("0.0", NumberFormatter.Display)}%";
        }
    }
}
