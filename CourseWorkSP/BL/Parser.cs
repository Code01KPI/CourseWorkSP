using System.Text.RegularExpressions;

namespace CourseWorkSP.BL
{
    /// <summary>
    /// Класс який відповідає за розбив рядка на слова.
    /// </summary>
    internal class Parser
    {
        /// <summary>
        /// Масив з словами одного рядка.
        /// </summary>
        //public string[]? array;

        /// <summary>
        /// Рядок.
        /// </summary>
        public string? Str { get; set; }

        // Регулярки для розбивки рядка.
        private Regex regex1 = new Regex(@"\s+");

        private Regex regex2 = new Regex(@"^\s+");

        private Regex regex3 = new Regex(@"\s+$");

        private Regex regex4 = new Regex(@"\[");

        private Regex regex5 = new Regex(@"\]");

        private Regex regex6 = new Regex(@"\*");

        private Regex regex7 = new Regex(@"\:");

        private Regex regex8 = new Regex(@"\,");

        public Parser() => Str = String.Empty;

        public string[]? ParseStr()
        {
            if (!string.IsNullOrEmpty(Str))
            {
                string tmp = Str;
                string[]? result = null;

                if (tmp is not null)
                {
                    tmp = regex1.Replace(tmp, " ");
                    tmp = regex2.Replace(tmp, "");
                    tmp = regex3.Replace(tmp, "");
                    tmp = regex4.Replace(tmp, "[ ");
                    tmp = regex5.Replace(tmp, " ]");
                    tmp = regex6.Replace(tmp, " * ");
                    tmp = regex7.Replace(tmp, " : ");
                    tmp = regex8.Replace(tmp, " ,");
                }

                if (tmp is not null)
                    result = tmp.Split(' ');

                /*Console.WriteLine();
                foreach (var s in result)
                    Console.Write(s + " ");*/

                return result;
            }
            return null;
        }
    }
}
