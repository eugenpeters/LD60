using System;
using Rewind.Core;

namespace Rewind.Game
{
    public class Economy
    {
        public readonly EconomyConfig Config;
        public GameState State { get; private set; }

        public event Action<int, int> Purchased;
        public event Action<int> MilestoneReached;
        public event Action GoalReached;

        BigNumber cachedRps;
        bool rpsDirty = true;

        public Economy(EconomyConfig config, GameState state)
        {
            Config = config;
            State = state;
        }

        public void Reset(GameState state)
        {
            State = state;
            rpsDirty = true;
        }

        public BigNumber Goal => BigNumber.FromLog10(Config.goalLog10);

        public int MilestonesReached(int owned)
        {
            var m = 0;
            foreach (var t in Config.milestones) if (owned >= t) m++;
            return m;
        }

        public int NextMilestone(int owned)
        {
            foreach (var t in Config.milestones) if (owned < t) return t;
            return -1;
        }

        public BigNumber TierRps(int tier, int owned)
        {
            if (owned <= 0) return BigNumber.Zero;
            var t = Config.tiers[tier];
            if (t.clickUpgrade) return BigNumber.Zero;
            var log = Math.Log10(owned * t.baseRps)
                         + MilestonesReached(owned) * Math.Log10(2)
                         + owned * Math.Log10(t.selfMultiplier);
            return BigNumber.FromLog10(log);
        }

        public BigNumber TierRps(int tier) => TierRps(tier, State.Owned[tier]);

        public BigNumber PassiveRps
        {
            get
            {
                if (!rpsDirty) return cachedRps;
                var sum = BigNumber.Zero;
                for (var i = 0; i < Config.tiers.Length; i++) sum += TierRps(i);
                cachedRps = sum;
                rpsDirty = false;
                return sum;
            }
        }

        public BigNumber MetersPerSecond => PassiveRps * Config.metersPerRotation;
        public BigNumber RotationsPerClick => ClickRotations(-1, 0);

        public BigNumber ClickRotations(int tier, int extra)
        {
            var sum = BigNumber.One;
            for (var i = 0; i < Config.tiers.Length; i++)
            {
                if (!Config.tiers[i].clickUpgrade) continue;
                sum += ClickUpgradeBonus(i, State.Owned[i] + (i == tier ? extra : 0));
            }
            return sum + PassiveRps * Config.clickPassiveFraction;
        }

        public BigNumber ClickUpgradeBonus(int tier, int owned)
        {
            if (owned <= 0) return BigNumber.Zero;
            return BigNumber.FromLog10(Math.Log10(owned * Config.tiers[tier].baseRps) + MilestonesReached(owned) * Math.Log10(2));
        }
        public BigNumber MetersPerClick => RotationsPerClick * Config.metersPerRotation;

        public BigNumber Cost(int tier, int owned)
        {
            var t = Config.tiers[tier];
            return BigNumber.FromLog10(Math.Log10(t.baseCost) + owned * Math.Log10(t.costGrowth));
        }

        public BigNumber NextCost(int tier) => Cost(tier, State.Owned[tier]);

        public BigNumber CostFor(int tier, int count)
        {
            if (count <= 0) return BigNumber.Zero;
            var g = Config.tiers[tier].costGrowth;
            var logGeom = count * Math.Log10(g);
            var logSum = logGeom > 15 ? logGeom : Math.Log10(Math.Pow(g, count) - 1);
            return BigNumber.FromLog10(NextCost(tier).Log10() + logSum - Math.Log10(g - 1));
        }

        public int MaxAffordable(int tier)
        {
            if (State.Distance < NextCost(tier)) return 0;
            var g = Config.tiers[tier].costGrowth;
            var ratioLog = State.Distance.Log10() - NextCost(tier).Log10() + Math.Log10(g - 1);
            var x = ratioLog > 15 ? ratioLog : Math.Log10(Math.Pow(10, ratioLog) + 1);
            var k = Math.Max(1, (int)Math.Floor(x / Math.Log10(g)));
            while (k > 1 && CostFor(tier, k) > State.Distance) k--;
            return k;
        }

        public bool IsRevealed(int tier) =>
            State.Owned[tier] > 0 || tier == 0 ||
            State.TotalDistance >= BigNumber.FromDouble(Config.tiers[tier].baseCost * 0.5);

        public bool TryBuy(int tier, int count)
        {
            if (count <= 0) return false;
            var cost = CostFor(tier, count);
            if (State.Distance < cost) return false;
            var before = MilestonesReached(State.Owned[tier]);
            State.Distance = BigNumber.Max(BigNumber.Zero, State.Distance - cost);
            State.Owned[tier] += count;
            rpsDirty = true;
            Purchased?.Invoke(tier, count);
            if (MilestonesReached(State.Owned[tier]) > before) MilestoneReached?.Invoke(tier);
            return true;
        }

        public BigNumber Click()
        {
            var gain = MetersPerClick;
            State.Clicks++;
            Add(gain);
            return gain;
        }

        public void Tick(double dt)
        {
            State.PlayTime += dt;
            var rps = PassiveRps;
            if (!rps.IsZero) Add(rps * (Config.metersPerRotation * dt));
        }

        public void Add(BigNumber meters)
        {
            State.Distance += meters;
            State.TotalDistance += meters;
            if (!State.Won && State.TotalDistance >= Goal)
            {
                State.Won = true;
                State.WinTime = State.PlayTime;
                GoalReached?.Invoke();
            }
        }

        public float GoalProgress
        {
            get
            {
                if (State.TotalDistance <= BigNumber.One) return 0;
                return (float)Math.Min(1, State.TotalDistance.Log10() / Config.goalLog10);
            }
        }

        public int HighestOwnedTier
        {
            get
            {
                for (var i = Config.tiers.Length - 1; i >= 0; i--) if (State.Owned[i] > 0) return i;
                return -1;
            }
        }
    }
}
