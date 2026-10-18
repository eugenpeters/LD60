using Rewind.Core;
using Rewind.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rewind.UI
{
    public class ShopUI : MonoBehaviour
    {
        enum BuyMode { One, Ten, Max }

        [SerializeField] CanvasGroup group;
        [SerializeField] ShopRow[] rows;
        [SerializeField] Button[] modeButtons;
        [SerializeField] Color affordTextColor = new Color(1f, 0.62f, 0.18f);
        [SerializeField] Color tooExpensiveTextColor = new Color(0.66f, 0.62f, 0.54f);

        ColorBlock[] modeColorBlocks;

        BuyMode mode = BuyMode.One;
        float refresh;
        static readonly Color Affordable = new Color(0.3f, 0.23f, 0.08f, 1f);
        static readonly Color TooExpensive = new Color(0.13f, 0.11f, 0.07f, 1f);

        void Start()
        {
            for (var i = 0; i < rows.Length; i++)
            {
                var tier = i;
                rows[i].button.onClick.AddListener(() => Buy(tier));
            }
            modeColorBlocks = new ColorBlock[modeButtons.Length];
            for (var i = 0; i < modeButtons.Length; i++)
            {
                modeColorBlocks[i] = modeButtons[i].colors;
                var m = (BuyMode)i;
                modeButtons[i].onClick.AddListener(() => { mode = m; refresh = 0; Audio.AudioManager.Instance?.Play(Audio.Sfx.UiClick); });
            }
        }

        int CountFor(Economy e, int tier) => mode switch
        {
            BuyMode.Ten => 10,
            BuyMode.Max => Mathf.Max(1, e.MaxAffordable(tier)),
            _ => 1,
        };

        void Buy(int tier)
        {
            var gm = GameManager.Instance;
            gm.TryBuy(tier, CountFor(gm.Economy, tier));
            refresh = 0;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var visible = gm.Flow == Flow.Playing || gm.Flow == Flow.Paused || gm.Flow == Flow.Won;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime * 4);
            group.interactable = group.blocksRaycasts = gm.Flow == Flow.Playing;

            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0) return;
            refresh = 0.1f;

            var e = gm.Economy;
            for (var i = 0; i < modeButtons.Length; i++)
            {
                var colors = modeColorBlocks[i];
                if ((int)mode == i) colors.normalColor = colors.highlightedColor;
                modeButtons[i].colors = colors;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var t = e.Config.tiers[i];
                var revealed = e.IsRevealed(i);
                var count = CountFor(e, i);
                var cost = e.CostFor(i, count);
                var canAfford = revealed && e.State.Distance >= cost;
                var owned = e.State.Owned[i];

                row.group.alpha = revealed ? 1 : 0.45f;
                row.background.color = canAfford ? Affordable : TooExpensive;
                row.button.interactable = revealed;
                row.title.text = revealed ? t.name : "???";
                row.joke.text = revealed ? t.joke : "Keep rewinding to discover";
                row.owned.text = owned.ToString();
                row.cost.text = (count > 1 ? $"COST x{count}: " : "COST: ") + NumberFormatter.FormatMeters(cost);
                row.cost.color = canAfford ? affordTextColor : tooExpensiveTextColor;

                if (!revealed) { row.output.text = ""; continue; }
                var next = e.NextMilestone(owned);
                var milestone = next > 0 ? $"   x2 at {next}" : "";
                if (t.clickUpgrade)
                {
                    var perClick = (e.ClickUpgradeBonus(i, owned + 1) - e.ClickUpgradeBonus(i, owned)) * e.Config.metersPerRotation;
                    row.output.text = $"+{NumberFormatter.FormatMeters(perClick)} per click{milestone}";
                    continue;
                }
                var each = e.TierRps(i, owned + 1) - e.TierRps(i, owned);
                row.output.text = $"+{NumberFormatter.Format(each)} rot/s{milestone}";
            }
        }
    }
}
