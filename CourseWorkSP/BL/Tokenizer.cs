using System;
using System.Text.RegularExpressions;

namespace CourseWorkSP.BL
{
    internal class Tokenizer
    {
        /// <summary>
        /// Рядок який буде аналізватися.
        /// </summary>
        public string? Str { get; set; }

        /// <summary>
        /// Масив з всіма лексемами рядка.
        /// </summary>
        public string[]? Array { get; set; }

        private List<string> words = new List<string>();

        private readonly string[] directives = new string[] { "END", "SEGMENT", "EQU", "=", "end", "equ", "ends",
                                                                "use16", "use32", "data", "code", ".386", ".486", "Segment" };

        //TODO: Доробити словник.
        private Dictionary<int, string> dataDirectives = new Dictionary<int, string>()
        {
            [1] = "db",
            [2] = "dw",
            [3] = "dd",
            [4] = "BYTE",
            [5] = "DWORD"
        };

        /// <summary>
        /// Масив машинних команд.
        /// </summary>
        private readonly string[] machineCommand = new string[] { "Std", "mov", "Dec", "Not", "Rcl", "Imul",
                                                                    "And", "Btr", "Adc", "Jnbe", "Jmp" };

        /// <summary>
        /// Масив 32 розрядних регістрів.
        /// </summary>
        private readonly string[] registersOfSize32 = new string[] { "eax", "ecx", "edx", "ebx", "esp", "ebp", "esi", "edi"};

        /// <summary>
        /// Масив 8 розрядних регістрів(cs - сегментний регістр).
        /// </summary>
        private readonly string[] registersOfSize8 = new string[] { "ah", "al", "ch", "cl", "dh", "dl", "bh", "bl", "spl", "dpl", "sil", "dil", "cs" };

        /// <summary>
        /// Метод токінайзер.
        /// </summary>
        public void Analysis()
        {
            if(Str is not null && Array is not null && Array.Length > 0)
            {
                Console.WriteLine("------------------------------------------");
                Console.WriteLine("Analysis line: " + Str);
                for (int i = 0; i < Array?.Length; i++)
                {

                    if (directives.Contains(Array[0]))
                    {
                        for (int j = 0; j < Array?.Length; j++)
                        {
                            if (directives.Contains(Array[j]))
                                Console.WriteLine($"{j + 1}  {Array[j]} - directive");
                        }
                        break;
                    }
                    else
                    {
                        if (Array.Length >= 2)
                        {
                            if (Array[1] == ":")
                            {
                                Console.WriteLine($"{1}  {Array[0]}: - label");
                                break;
                            }
                        }

                        if (machineCommand.Contains(Array[0]))
                        {
                            Console.WriteLine($"{1}  {Array[0]} - machine command");
                            if (Array.Length < 2)
                                break;
                            else
                            {
                                if(Array.Length == 2)
                                {
                                    if(Array[0] == "Jnbe" || Array[0] == "jnbe" || Array[0] == "Jmp" || Array[0] == "jmp")
                                    {
                                        Console.WriteLine($"{2}  {Array[1]} - label");
                                        break;
                                    }
                                }

                                for (int j = 1; j < Array.Length; j++)
                                {
                                    if (dataDirectives.ContainsValue(Array[j]))
                                        Console.WriteLine($"{j + 1}  {Array[j]} - data directive");
                                    else if (Array[j] == "SHORT" || Array[j] == "short")
                                    {
                                        Console.WriteLine($"{j + 1}  {Array[j]} - operator");
                                        Console.WriteLine($"{j + 2}  {Array[j + 1]} - label");
                                        break;
                                    }
                                    else if (Array[j] == "PTR" || Array[j] == "ptr")
                                        Console.WriteLine($"{j + 1}  {Array[j]} - data type operand");
                                    else if (registersOfSize32.Contains(Array[j]) || registersOfSize8.Contains(Array[j]))
                                        Console.WriteLine($"{j + 1}  {Array[j]} - register");
                                    else if (Array[j] == "[" || Array[j] == "]" || Array[j] == ":" || Array[j] == "," || Array[j] == "*")
                                        Console.WriteLine($"{j + 1}  {Array[j]} - one symbol");
                                    else if (CheckConst(Array[j]) == 1)
                                        Console.WriteLine($"{j + 1}  {Array[j]} - decimal number");
                                    else
                                    {
                                        Console.WriteLine($"{j + 1}  {Array[j]} - user id or unknown");
                                        break;
                                    }

                                }
                                break;
                            }
                        }
                    }

                    if (Array.Length >= 2)
                    {
                        if (dataDirectives.ContainsValue(Array[i]))
                        {
                            Console.WriteLine($"{1}  {Array[0]} - user id or unknown");
                            Console.WriteLine($"{2}  {Array[i]} - data directive");

                            if (int.TryParse(Array[2], out int number))
                                Console.WriteLine($"{3}  {Array[2]} - decimal number");
                            else if (Array[2].EndsWith('h'))
                                Console.WriteLine($"{3}  {Array[2]} - hex number");
                            else if (Array[2].StartsWith('\'') && Array[2].EndsWith('\''))
                                Console.WriteLine($"{3}  {Array[2]}  - str");
                            Console.WriteLine();
                        }
                        else if (Array[1] == "equ" || Array[1] == "EQU")
                        {
                            Console.WriteLine($"{1}  {Array[0]} - user id or unknown");
                            Console.WriteLine($"{2}  {Array[1]} - directive");
                            Console.WriteLine($"{3}  {Array[2]} - decimal number"); //TODO: доробити.
                            break;
                        }
                        else if (Array[1] == "=")
                        {
                            Console.WriteLine($"{1}  {Array[0]} - user id or unknown");
                            Console.WriteLine($"{2}  {Array[1]} - directive");
                            Console.WriteLine($"{3}  {Array[2]} - devimal number"); //TODO: доробити.
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Метод для визначення типу констант.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int CheckConst(string value)
        {
            if (int.TryParse(value, out int result))
                return 1; // ціле число
            else if (value.EndsWith('h'))
                return 2; // шістнадцядкове число
            else if (value.StartsWith('\'') && value.EndsWith('\''))
                return 3; // текстова константа
            else
                return 0;
        }
    }
}
