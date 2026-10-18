using System;
using System.Globalization;

namespace Rewind.Core
{
    [Serializable]
    public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
    {
        public readonly double Mantissa;
        public readonly long Exponent;

        public static readonly BigNumber Zero = new BigNumber(0, 0);
        public static readonly BigNumber One = new BigNumber(1, 0);

        const int PrecisionDigits = 17;

        BigNumber(double mantissa, long exponent, bool normalize)
        {
            if (!normalize) { Mantissa = mantissa; Exponent = exponent; return; }
            if (mantissa == 0 || double.IsNaN(mantissa) || double.IsInfinity(mantissa))
            {
                Mantissa = 0; Exponent = 0; return;
            }
            var shift = (int)Math.Floor(Math.Log10(Math.Abs(mantissa)));
            mantissa /= Pow10(shift);
            exponent += shift;
            if (Math.Abs(mantissa) >= 10) { mantissa /= 10; exponent++; }
            else if (Math.Abs(mantissa) < 1) { mantissa *= 10; exponent--; }
            Mantissa = mantissa; Exponent = exponent;
        }

        public BigNumber(double mantissa, long exponent) : this(mantissa, exponent, true) { }

        public static BigNumber FromDouble(double v) => new BigNumber(v, 0);
        public static implicit operator BigNumber(double v) => FromDouble(v);

        public bool IsZero => Mantissa == 0;

        public double ToDouble()
        {
            if (IsZero) return 0;
            if (Exponent > 308) return Mantissa > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            if (Exponent < -324) return 0;
            return Mantissa * Pow10(Exponent);
        }

        public double Log10() => IsZero ? double.NegativeInfinity : Exponent + Math.Log10(Mantissa);

        public static BigNumber FromLog10(double log)
        {
            if (double.IsNegativeInfinity(log)) return Zero;
            var e = Math.Floor(log);
            return new BigNumber(Math.Pow(10, log - e), (long)e);
        }

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            if (a.Exponent < b.Exponent) (a, b) = (b, a);
            var diff = a.Exponent - b.Exponent;
            if (diff > PrecisionDigits) return a;
            return new BigNumber(a.Mantissa + b.Mantissa / Pow10(diff), a.Exponent);
        }

        public static BigNumber operator -(BigNumber a) => new BigNumber(-a.Mantissa, a.Exponent, false);
        public static BigNumber operator -(BigNumber a, BigNumber b) => a + (-b);

        public static BigNumber operator *(BigNumber a, BigNumber b) =>
            new BigNumber(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);

        public static BigNumber operator /(BigNumber a, BigNumber b)
        {
            if (b.IsZero) throw new DivideByZeroException();
            return new BigNumber(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
        }

        public static BigNumber Pow(BigNumber b, double power)
        {
            if (b.IsZero) return power == 0 ? One : Zero;
            if (b.Mantissa < 0) throw new ArgumentException("Pow of negative BigNumber");
            return FromLog10(b.Log10() * power);
        }

        public static BigNumber Max(BigNumber a, BigNumber b) => a >= b ? a : b;
        public static BigNumber Min(BigNumber a, BigNumber b) => a <= b ? a : b;

        public int CompareTo(BigNumber other)
        {
            int sa = Math.Sign(Mantissa), sb = Math.Sign(other.Mantissa);
            if (sa != sb) return sa.CompareTo(sb);
            if (sa == 0) return 0;
            if (Exponent != other.Exponent)
                return sa > 0 ? Exponent.CompareTo(other.Exponent) : other.Exponent.CompareTo(Exponent);
            return Mantissa.CompareTo(other.Mantissa);
        }

        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigNumber a, BigNumber b) => a.Equals(b);
        public static bool operator !=(BigNumber a, BigNumber b) => !a.Equals(b);

        public bool Equals(BigNumber other) => Mantissa == other.Mantissa && Exponent == other.Exponent;
        public override bool Equals(object obj) => obj is BigNumber o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

        public string Serialize() =>
            Mantissa.ToString("R", CultureInfo.InvariantCulture) + "|" + Exponent.ToString(CultureInfo.InvariantCulture);

        public static BigNumber Deserialize(string s)
        {
            if (string.IsNullOrEmpty(s)) return Zero;
            var bar = s.IndexOf('|');
            if (bar < 0) return Zero;
            if (!double.TryParse(s.Substring(0, bar), NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) return Zero;
            if (!long.TryParse(s.Substring(bar + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var e)) return Zero;
            return new BigNumber(m, e);
        }

        public override string ToString() =>
            Mantissa.ToString("0.###", CultureInfo.InvariantCulture) + "e" + Exponent.ToString(CultureInfo.InvariantCulture);

        static double Pow10(long e) => Math.Pow(10, e);
    }
}
