using System.Globalization;

namespace SurfPool
{
    /// <summary>Money as text: "$12,450" for exact amounts, "$48.2k" / "$1.24M" for short popups.</summary>
    public static class MoneyText
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string Format(long amount) =>
            (amount < 0 ? "-$" : "$") + System.Math.Abs(amount).ToString("#,0", Culture);

        public static string Short(long amount)
        {
            long abs = System.Math.Abs(amount);
            string sign = amount < 0 ? "-$" : "$";
            if (abs >= 1_000_000) return sign + (abs / 1_000_000f).ToString(abs >= 10_000_000 ? "0.#" : "0.##", Culture) + "M";
            if (abs >= 10_000) return sign + (abs / 1000f).ToString(abs >= 100_000 ? "0" : "0.#", Culture) + "k";
            return sign + abs.ToString("#,0", Culture);
        }
    }
}
