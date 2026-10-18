using System.Globalization;

namespace Rewind.Core
{
    public static class NumberFormatter
    {
        static readonly string[] Names =
        {
            "Thousand", "Million", "Billion", "Trillion", "Quadrillion", "Quintillion", "Sextillion",
            "Septillion", "Octillion", "Nonillion", "Decillion", "Undecillion", "Duodecillion",
            "Tredecillion", "Quattuordecillion", "Quindecillion", "Sexdecillion", "Septendecillion",
            "Octodecillion", "Novemdecillion", "Vigintillion", "Unvigintillion", "Duovigintillion",
            "Trevigintillion", "Quattuorvigintillion", "Quinvigintillion", "Sexvigintillion",
            "Septenvigintillion", "Octovigintillion", "Novemvigintillion", "Trigintillion",
            "Untrigintillion", "Duotrigintillion",
        };

        public static readonly NumberFormatInfo Display = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = " ",
        };

        public static string Format(BigNumber v)
        {
            if (v.IsZero) return "0";
            if (v.Exponent < 6)
            {
                var d = v.ToDouble();
                return d < 100 && d != System.Math.Floor(d)
                    ? d.ToString("0.0", Display)
                    : System.Math.Floor(d).ToString("#,0", Display);
            }
            var group = v.Exponent / 3;
            var nameIndex = (int)group - 1;
            if (nameIndex < Names.Length)
            {
                var scaled = v.Mantissa * System.Math.Pow(10, v.Exponent - group * 3);
                return scaled.ToString("0.00", Display) + " " + Names[nameIndex];
            }
            return v.Mantissa.ToString("0.00", Display) + "e" + v.Exponent.ToString(Display);
        }

        public static string FormatMeters(BigNumber v) => Format(v) + " m";

        struct Landmark
        {
            public readonly double Log10Meters;
            public readonly string Name;
            public Landmark(double meters, string name) { Log10Meters = System.Math.Log10(meters); Name = name; }
            public Landmark(string name, double log10) { Log10Meters = log10; Name = name; }
        }

        static readonly Landmark[] Landmarks =
        {
            new Landmark(0.19, "pencils"),
            new Landmark(105, "football fields"),
            new Landmark(8_849, "Mount Everests"),
            new Landmark(40_075_000, "trips around Earth"),
            new Landmark(384_400_000, "Earth to Moon trips"),
            new Landmark(1.496e11, "Earth to Sun trips"),
            new Landmark(9.461e15, "light-years"),
            new Landmark(9.5e20, "Milky Way widths"),
            new Landmark(8.8e26, "observable universes"),
            new Landmark("universes of universes", 54),
            new Landmark("multiverses", 60),
        };

        public static string Flavor(BigNumber meters)
        {
            if (meters.IsZero) return "Grab a pencil, start rewinding";
            var log = meters.Log10();
            Landmark best = Landmarks[0];
            foreach (var l in Landmarks)
                if (l.Log10Meters <= log && l.Log10Meters >= best.Log10Meters) best = l;
            if (log < best.Log10Meters) return "less than one pencil";
            var ratio = BigNumber.FromLog10(log - best.Log10Meters);
            return "≈ " + Format(ratio) + " " + best.Name;
        }
    }
}
