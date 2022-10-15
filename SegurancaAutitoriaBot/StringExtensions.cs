using System.Text.RegularExpressions;

namespace SegurancaAutitoriaBot
{
    public static class StringExtensions
    {
        public static string? RemoveInvalidCharacter(this string value)
        {
            if (value != null)
            {
                value = Regex.Replace(value, @"[ç]+", "c");
                value = Regex.Replace(value, @"[áäàãâ]+", "a");
                value = Regex.Replace(value, @"[éèëê&]+", "e");
                value = Regex.Replace(value, @"[íìïî]+", "i");
                value = Regex.Replace(value, @"[óòöõô]+", "o");
                value = Regex.Replace(value, @"[úùüû]+", "u");
                value = Regex.Replace(value, @"[Ç]+", "C");
                value = Regex.Replace(value, @"[ÁÁÄÃÂ]+", "A");
                value = Regex.Replace(value, @"[ÉÈËÊ]+", "E");
                value = Regex.Replace(value, @"[ÍÌÏÎ]+", "I");
                value = Regex.Replace(value, @"[ÓÒÖÕÔ]+", "O");
                value = Regex.Replace(value, @"[ÚÙÜÛ]+", "U");
                value = Regex.Replace(value, @"[&]+", "e");
                value = Regex.Replace(value, @"[^0-9a-zA-Záéíóúàèìòùâêîôûãõç ]+", "");
            }
            return value;
        }

        public static string? RemoveSpecialCharacter(this string value)
        {
            if (value != null)
            {
                value = Regex.Replace(value, @"[ç]+", "c");
                value = Regex.Replace(value, @"[&]+", "e");
                value = Regex.Replace(value, @"[Ç]+", "C");
                value = Regex.Replace(value, @"[^0-9a-zA-Záéíóúàèìòùâêîôûãõç/(). ]+", "");
            }

            return value;
        }
    }
}
