using System;

namespace Sheridan.SKoin.API
{
    public struct Transaction
    {
        public Guid From { get; set; }
        public Guid To { get; set; }
        public ulong Amount { get; set; }

        public Transaction(Guid from, Guid to, ulong amount)
        {
            From = from;
            To = to;
            Amount = amount;
        }

        public override string ToString()
        {
            return $"{From},{To},{Amount}";
        }

        public static bool TryParse(string s, out Transaction result)
        {
            var parts = s.Split(',');

            if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid from) && Guid.TryParse(parts[1], out Guid to) && ulong.TryParse(parts[2], out ulong amount))
            {
                result = new Transaction(from, to, amount);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
