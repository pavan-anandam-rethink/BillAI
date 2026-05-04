using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Rethink.Services.Common.Helpers
{
    public static class NumberHelper
    {
        private const int _base = 36;
        private const string _chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// The  hexatridecimal format is a Base 36 number that contains characters 0-9 and A-Z 
        /// to represent the integer in a much shorter length (every position in the identifier 
        /// can contain the integer value 0-35 instead of 0-9). For example, a 5 digit 
        /// hexatridecimal ClientProfileId will allow for up to 60  million client identifiers.   
        /// </summary>
        /// <param name="value">Decimal value to be converted</param>
        /// <returns>Base 36 string</returns>
        public static string ToBase36(this int value)
        {
            StringBuilder result = new StringBuilder();

            while (value > 0)
            {
                result.Insert(0, _chars[value % _base]);
                value /= _base;
            }

            return result.ToString();
        }

        public static int FromBase36(this string base36Num)
        {

            if (String.IsNullOrEmpty(base36Num))
                return 0;

            // Make sure the number is in upper case to start
            base36Num = base36Num.ToUpperInvariant();

            var result = 0;
            var multiplier = 1;
            for (int i = base36Num.Length - 1; i >= 0; i--)
            {
                char c = base36Num[i];
                if (i == 0 && c == '-')
                {
                    // This is the negative sign symbol
                    result = -result;
                    break;
                }

                int digit = _chars.IndexOf(c);
                if (digit == -1)
                    throw new ArgumentException(
                        "Invalid character in the arbitrary numeral system number",
                        "base36Num");

                result += digit * multiplier;
                multiplier *= _base;
            }

            return result;
        }

    }
}
