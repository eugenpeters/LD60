using System;
using UnityEngine;

namespace Rewind.Game
{
    [Serializable]
    public class TierConfig
    {
        public string name;
        [Tooltip("Click upgrade: baseRps is extra rotations per manual click per unit, no passive output.")]
        public bool clickUpgrade;
        [TextArea] public string joke;
        public double baseCost;
        public double baseRps;
        public double costGrowth;
        public double selfMultiplier;
    }

    [CreateAssetMenu(menuName = "Rewind/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        public double metersPerRotation = 1;
        public double clickPassiveFraction = 0.02;
        public double goalLog10 = 60;
        public int[] milestones = { 10, 25, 50, 100, 150, 200, 250, 300, 400, 500 };
        public TierConfig[] tiers = DefaultTiers();

        public static TierConfig[] DefaultTiers() => new[]
        {
            new TierConfig { name = "Sharpener", clickUpgrade = true, joke = "Sharper pencil, firmer grip",
                baseCost = 10, baseRps = 1, costGrowth = 1.2, selfMultiplier = 1 },
            new TierConfig { name = "Auto-Pencil", joke = "A pencil that believes in itself",
                baseCost = 30, baseRps = 1.5, costGrowth = 1.15, selfMultiplier = 1 },
            new TierConfig { name = "Electromotor", joke = "Ripped out of a VCR",
                baseCost = 400, baseRps = 40, costGrowth = 1.15, selfMultiplier = 1 },
            new TierConfig { name = "Nuclear Walkman", joke = "Side B glows in the dark",
                baseCost = 1e4, baseRps = 1500, costGrowth = 1.3, selfMultiplier = 1.2 },
            new TierConfig { name = "Galaxy Drive", joke = "A spiral galaxy for a capstan",
                baseCost = 2e6, baseRps = 5e4, costGrowth = 50, selfMultiplier = 46.5 },
        };

        public static EconomyConfig CreateDefault()
        {
            var c = CreateInstance<EconomyConfig>();
            c.tiers = DefaultTiers();
            return c;
        }
    }
}
