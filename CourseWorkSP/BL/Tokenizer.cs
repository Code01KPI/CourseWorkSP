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
        public string[]? ArrayOfWord { get; set; }

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
            if(Str is not null && ArrayOfWord is not null && ArrayOfWord.Length > 0)
            {
                Console.WriteLine(".........................................................\n");
                Console.WriteLine("Analysis line: " + Str);
                Console.WriteLine("No word length - type");
                for (int i = 0; i < ArrayOfWord?.Length; i++)
                {

                    if (directives.Contains(ArrayOfWord[0]))
                    {
                        for (int j = 0; j < ArrayOfWord?.Length; j++)
                        {
                            if (directives.Contains(ArrayOfWord[j]))
                                Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - directive");
                        }
                        Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                        if (ArrayOfWord?.Length == 1)
                            Console.WriteLine("|  0    |  1  |  1  |  0  |  0  |  0  |  0  |");
                        else if (ArrayOfWord?.Length == 2)
                            Console.WriteLine("|  0    |  1  |  1  |  2  |  1  |  0  |  0  |");
                        else
                            Console.WriteLine("|  0    |  1  |  1  |  2  |  1  |  3  |  1  |");
                        break;
                    }
                    else
                    {
                        if (ArrayOfWord.Length >= 2)
                        {
                            if (ArrayOfWord[1] == ":")
                            {
                                Console.WriteLine($"{1}  {ArrayOfWord[0]}:  {ArrayOfWord[i].Length + 1} - label");
                                Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                                Console.WriteLine("|  1    |  0  |  0  |  0  |  0  |  0  |  0  |");
                                break;
                            }
                        }

                        if (machineCommand.Contains(ArrayOfWord[0]))
                        {
                            Console.WriteLine($"{1}  {ArrayOfWord[0]}  {ArrayOfWord[i].Length} - machine command");
                            if(ArrayOfWord.Length == 1)
                            {
                                Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                                Console.WriteLine("|  0    |  1  |  1  |  0  |  0  |  0  |  0  |");
                            }
                            if (ArrayOfWord.Length < 2)
                                break;
                            else
                            {
                                if(ArrayOfWord.Length == 2)
                                {
                                    if(ArrayOfWord[0] == "Jnbe" || ArrayOfWord[0] == "jnbe" || ArrayOfWord[0] == "Jmp" || ArrayOfWord[0] == "jmp")
                                    {
                                        Console.WriteLine($"{2}  {ArrayOfWord[1]}  {ArrayOfWord[i].Length} - label");
                                        Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                                        Console.WriteLine("|  0    |  1  |  1  |  2  |  1  |  0  |  0  |");
                                        break;
                                    }
                                }

                                for (int j = 1; j < ArrayOfWord.Length; j++)
                                {
                                    if (dataDirectives.ContainsValue(ArrayOfWord[j]))
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - data directive");
                                    else if (ArrayOfWord[j] == "SHORT" || ArrayOfWord[j] == "short")
                                    {
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - operator");
                                        Console.WriteLine($"{j + 2}  {ArrayOfWord[j + 1]}  {ArrayOfWord[j].Length} - label");
                                        break;
                                    }
                                    else if (ArrayOfWord[j] == "PTR" || ArrayOfWord[j] == "ptr")
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - data type operand");
                                    else if (registersOfSize32.Contains(ArrayOfWord[j]) || registersOfSize8.Contains(ArrayOfWord[j]))
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - register");
                                    else if (ArrayOfWord[j] == "[" || ArrayOfWord[j] == "]" || ArrayOfWord[j] == ":" || ArrayOfWord[j] == "," || ArrayOfWord[j] == "*")
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - one symbol");
                                    else if (CheckConst(ArrayOfWord[j]) == 1)
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - decimal number");
                                    else
                                    {
                                        Console.WriteLine($"{j + 1}  {ArrayOfWord[j]}  {ArrayOfWord[j].Length} - user id or unknown");
                                        break;
                                    }

                                }
                                if (ArrayOfWord.Contains(","))
                                {
                                    int index = Array.IndexOf(ArrayOfWord, ",");
                                    Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                                    Console.WriteLine($"|  0    |  1  |  1  |  2  |  {index - 1}  |  {index + 2}  |  {ArrayOfWord.Length - index - 1}  |");
                                }
                                else
                                {
                                    Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                                    Console.WriteLine($"|  0    |  1  |  1  |  2  |  {ArrayOfWord.Length - 1}  |  0  |  0  |");
                                }
                                break;
                            }
                        }
                    }

                    if (ArrayOfWord.Length >= 2)
                    {
                        if (dataDirectives.ContainsValue(ArrayOfWord[i]))
                        {
                            Console.WriteLine($"{1}  {ArrayOfWord[0]}  {ArrayOfWord[0].Length} - user id or unknown");
                            Console.WriteLine($"{2}  {ArrayOfWord[1]}  {ArrayOfWord[1].Length} - data directive");

                            if (CheckConst(ArrayOfWord[2]) == 1)
                                Console.WriteLine($"{3}  {ArrayOfWord[2]}  {ArrayOfWord[2].Length} - decimal number");
                            else if (CheckConst(ArrayOfWord[2]) == 2)
                                Console.WriteLine($"{3}  {ArrayOfWord[2]}  {ArrayOfWord[2].Length} - hex number");
                            else if (CheckConst(ArrayOfWord[2]) == 3)
                                Console.WriteLine($"{3}  {ArrayOfWord[2]}  {ArrayOfWord[2].Length}  - text constant");
                            Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                            Console.WriteLine("|  1    |  2  |  1  |  3  |  1  |  0  |  0  |");
                            break;
                        }
                        else if (ArrayOfWord[1] == "equ" || ArrayOfWord[1] == "EQU")
                        {
                            Console.WriteLine($"{1}  {ArrayOfWord[0]}  {ArrayOfWord[0].Length} - user id or unknown");
                            Console.WriteLine($"{2}  {ArrayOfWord[1]}  {ArrayOfWord[1].Length} - directive");
                            Console.WriteLine($"{3}  {ArrayOfWord[2]}  {ArrayOfWord[2].Length} - decimal number"); //TODO: доробити.
                            Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                            Console.WriteLine("|  1    |  2  |  1  |  3  |  1  |  0  |  0  |");
                            break;
                        }
                        else if (ArrayOfWord[1] == "=")
                        {
                            Console.WriteLine($"{1}  {ArrayOfWord[0]}  {ArrayOfWord[0].Length} - user id or unknown");
                            Console.WriteLine($"{2}  {ArrayOfWord[1]}  {ArrayOfWord[1].Length} - directive");
                            Console.WriteLine($"{3}  {ArrayOfWord[2]}  {ArrayOfWord[2].Length} - devimal number"); //TODO: доробити.
                            Console.WriteLine("| label | mnemocode | operand 1 | operand 2 |");
                            Console.WriteLine("|  1    |  2  |  1  |  3  |  1  |  0  |  0  |");
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
