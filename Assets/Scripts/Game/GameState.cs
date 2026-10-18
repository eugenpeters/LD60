using System;
using Rewind.Core;

namespace Rewind.Game
{
    public class GameState
    {
        public const int TierCount = 5;

        public BigNumber Distance;
        public BigNumber TotalDistance;
        public int[] Owned = new int[TierCount];
        public double PlayTime;
        public long Clicks;
        public bool Won;
        public double WinTime;

        [Serializable]
        class Dto
        {
            public int version = 1;
            public string distance;
            public string total;
            public int[] owned;
            public double playTime;
            public long clicks;
            public bool won;
            public double winTime;
        }

        public string ToJson() => UnityEngine.JsonUtility.ToJson(new Dto
        {
            distance = Distance.Serialize(),
            total = TotalDistance.Serialize(),
            owned = (int[])Owned.Clone(),
            playTime = PlayTime,
            clicks = Clicks,
            won = Won,
            winTime = WinTime,
        });

        public static GameState FromJson(string json)
        {
            var s = new GameState();
            if (string.IsNullOrEmpty(json)) return s;
            Dto d;
            try { d = UnityEngine.JsonUtility.FromJson<Dto>(json); }
            catch (ArgumentException) { return s; }
            if (d == null) return s;
            s.Distance = BigNumber.Deserialize(d.distance);
            s.TotalDistance = BigNumber.Deserialize(d.total);
            if (d.owned != null)
                for (var i = 0; i < TierCount && i < d.owned.Length; i++) s.Owned[i] = Math.Max(0, d.owned[i]);
            s.PlayTime = d.playTime;
            s.Clicks = d.clicks;
            s.Won = d.won;
            s.WinTime = d.winTime;
            return s;
        }
    }
}
