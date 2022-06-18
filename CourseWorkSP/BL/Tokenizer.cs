using System;
using System.Text.RegularExpressions;
using System.Globalization;
using CourseWorkSP.BL;
using System.Data;

namespace CourseWorkSP.BL
{
    internal class Tokenizer
    {
        /// <summary>
        /// Зміна яка визначає чи знаходиться команда в межах логічного сегмента.
        /// </summary>
        private static int activeSeg = 0;

        /// <summary>
        /// Лічильник рядків.
        /// </summary>
        private static int lineCount = 0;

        /// <summary>
        /// Додатковий лічильник рядків.
        /// </summary>
        private static int localLineCount = 0;

        /// <summary>
        /// Лічильник рядків для 2-го перегляду.
        /// </summary>
        private static int lineCount2Step = 0;

        /// <summary>
        /// Буфер для значень констант.
        /// </summary>
        public static string constBuffer = String.Empty;

        /// <summary>
        /// Розмір операнда.
        /// </summary>
        private static int size = 0;

        /// <summary>
        /// Адреса операнда в пам'яті.
        /// </summary>
        private static int adress = 0;

        /// <summary>
        /// Список з байтами.
        /// </summary>
        List<string> bytes = new List<string>();

        /// <summary>
        /// Рядок який буде аналізватися.
        /// </summary>
        public string? Str { get; set; }

        /// <summary>
        /// Словник для зберігання розміру сегментів.
        /// </summary>
        public Dictionary<string, string> segmentInfo = new Dictionary<string, string>();

        /// <summary>
        /// Список для зберігання об'єктів опису змінних чи міток.
        /// </summary>
        public static List<UserLabelAndVariable> UserVar = new List<UserLabelAndVariable>();

        /// <summary>
        /// Словник міток.
        /// </summary>
        public static Dictionary<string, int> labels = new Dictionary<string, int>();

        /// <summary>
        /// Список для зберігання об'єктів опису помилок.
        /// </summary>
        public List<Error> Errors = new List<Error>();

        /// <summary>
        /// Буферна змінна для назви сегмента.
        /// </summary>
        private string segmentName;

        /// <summary>
        /// Буферна змінна для розміру сегмента.
        /// </summary>
        private string segmentSize;

        /// <summary>
        /// Масив з всіма лексемами рядка.
        /// </summary>
        public string[]? ArrayOfWord { get; set; }

        /// <summary>
        /// Масив директив.
        /// </summary>
        private readonly string[] directives = new string[] { "end", "segment", "ends" };

        /// <summary>
        /// Словник для зберігення директив даних.
        /// </summary>
        private readonly Dictionary<string, int> dataDirectives = new Dictionary<string, int>()
        {
            ["db"] = 1,
            ["dw"] = 2,
            ["dd"] = 4,
            ["equ"] = 0,
            ["="] = 0
        };

        private readonly string[] types = new string[] { "byte", "word", "dword" };

        /// <summary>
        /// Словник машинних команд.
        /// </summary>
        private readonly Dictionary<string, int> machineCommand = new Dictionary<string, int>()
        {
            ["std"] = 1,
            ["dec"] = 1,
            ["not"] = 1,
            ["rcl"] = 1,
            ["imul"] = 2,
            ["and"] = 1,
            ["btr"] = 2,
            ["adc"] = 1,
            ["jnbe"] = 1,
            ["jmp"] = 1
        };

        /// <summary>
        /// Словник префіксів заміни сегментів.
        /// </summary>
        private readonly Dictionary<string, string> segmentRegisterPreficses = new Dictionary<string, string>()
        {
            ["cs"] = "2E:",
            ["es"] = "26:",
            ["ss"] = "36:",
            ["fs"] = "64:",
            ["gs"] = "65:"
        };

        /// <summary>
        /// Словник байтів машинних команд.
        /// </summary>
        /*private readonly Dictionary<string, string> machineCommandBytes = new Dictionary<string, string>()
        {
            ["jmp"] = "EB",
            ["jnbe"] = "77",
            ["std"] = "FD",
            ["dec"] = "4A",
            ["not"] = "F7",
            ["rcl"] = "D2",
            ["imul"] = "0F AF",
            ["and"] = "20",
            ["btr"] = "0F BA",
            ["adc"] = "83"
        };*/
        /// <summary>
        /// Масив 32 розрядних регістрів.
        /// </summary>
        private readonly string[] registersOfSize32 = new string[] { "eax", "ecx", "edx", "ebx", "esp", "ebp", "esi", "edi"};

        /// <summary>
        /// Масив 8 розрядних регістрів(cs - сегментний регістр).
        /// </summary>
        private readonly string[] registersOfSize8 = new string[] { "ah", "al", "ch", "cl", "dh", "dl", "bh", "bl", "spl", "dpl", "sil", "dil" };

        /// <summary>
        /// Масив сегментних регістрів.
        /// </summary>
        private readonly string[] segmentRegisters = new string[] { "cs", "es", "ss", "ds", "fs", "gs" };

        /// <summary>
        /// Метод токінайзер.
        /// </summary>
        public void Analysis()
        {
            int tmp;
            string sizeOperand = String.Empty;

            if(Str is not null && ArrayOfWord is not null && ArrayOfWord.Length > 0)
            {
                if (ArrayOfWord.Length > 1 && directives.Contains(ArrayOfWord[1].ToLower()))
                {
                    if (ArrayOfWord[1].ToLower() == "segment")
                    {
                        Save.w.WriteLine();
                        Console.WriteLine();
                        activeSeg = 1;
                        adress = 0;
                        segmentName = ArrayOfWord[0];
                    }
                    else if (ArrayOfWord[1].ToLower() == "ends")
                    {
                        activeSeg = 0;
                        segmentSize = GetHexNumber(adress);
                        segmentInfo[segmentName] = segmentSize;
                    }

                    ++lineCount;
                    size = 0;
                    //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                }
                else if (ArrayOfWord.Length > 2 && dataDirectives.ContainsKey(ArrayOfWord[1]))
                {
                    if (ArrayOfWord[1].ToLower() == "equ" || ArrayOfWord[1] == "=")
                    {
                        ++lineCount;
                        if (activeSeg == 0 && ArrayOfWord[1].ToLower() == "equ")
                            Errors.Add(new Error("Active_segment = 0", lineCount));
                        
                        tmp = Calculator.Calc(String.Join(' ', ArrayOfWord, 2, ArrayOfWord.Length - 2));
                        if (tmp == int.MinValue)
                        {
                            Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount, constBuffer));
                            //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{constBuffer} \t{getStr()}");
                            //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{constBuffer} \t{getStr()}");
                            constBuffer = String.Empty;
                        }
                        else
                        {
                            size = Math.Abs(tmp);
                            Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount, GetHexNumber(size)));
                            //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size, 1)} \t{getStr()}");
                            //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size, 1)} \t{getStr()}");
                        }
                    }
                    else
                    {
                        if (activeSeg == 0)
                            Errors.Add(new Error("Active_segment = 0", lineCount));

                        ++lineCount;
                        size = dataDirectives[ArrayOfWord[1]];
                        for (int i = 0; i < size; i++)
                            sizeOperand += "00";

                        Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount, sizeOperand));
                        //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        adress += size;
                    }
                }
                else if (ArrayOfWord.Length >= 2 && ArrayOfWord[1] == ":")
                {
                    if (activeSeg == 0)
                        Errors.Add(new Error("Active_segment = 0", lineCount));

                    ++lineCount;  
                    size = 0;
                    Console.WriteLine(GetHexNumber(adress));
                    Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount, null));
                    //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                }
                else if (machineCommand.ContainsKey(ArrayOfWord[0].ToLower()) || ArrayOfWord.Length >= 3 && machineCommand.ContainsKey(ArrayOfWord[2].ToLower()))
                {
                    if (activeSeg == 0)
                        Errors.Add(new Error("Active_segment = 0", lineCount));

                    int segmReplacePrefix = 0;
                    int MRM = 0;
                    int SIB = 0;
                    int adressField = 0;
                    int operandField = 0;
                    int it1 = 0;
                    int it2 = 0;

                    ++lineCount;

                    for (int i = 0; i < ArrayOfWord.Length; i++)
                    {
                        if (ArrayOfWord[i].ToLower() == "short" && ArrayOfWord[i - 1].ToLower() == "jmp" || ArrayOfWord[0].ToLower() == "jnbe")
                        {
                            /*if (ArrayOfWord[1].ToLower() == "short" && FindLabel(ArrayOfWord[2]) > lineCount)
                            {
                                MRM = 5;
                            }*/
                            if (ArrayOfWord[0].ToLower() == "jnbe" && FindLabel(ArrayOfWord[1]) > lineCount)
                            {
                                MRM = 5;
                            }
                            else if ((ArrayOfWord[1].ToLower() == "short" && FindLabel(ArrayOfWord[2]) == 0) || ((ArrayOfWord[0].ToLower() == "jnbe" && FindLabel(ArrayOfWord[1]) == 0)))
                            {
                                Errors.Add(new Error("The required label is missing", lineCount));
                                MRM = 5;
                            }
                            else
                            {
                                MRM = 1;
                            }
                            break;
                        }
                        else if (ArrayOfWord[i] == ":" && segmentRegisters.Contains(ArrayOfWord[i - 1].ToLower()))
                        {
                            if ((ArrayOfWord[0].ToLower() == "not" || ArrayOfWord[0].ToLower() == "imul" || ArrayOfWord[0].ToLower() == "and") && ArrayOfWord[Array.IndexOf(ArrayOfWord, ":") - 1].ToLower() == "ds")
                                segmReplacePrefix = 0;
                            else if (ArrayOfWord[0].ToLower() == "adc" && ArrayOfWord[Array.IndexOf(ArrayOfWord, ":")].ToLower() == "ss")
                                segmReplacePrefix = 0;
                            else
                                segmReplacePrefix = 1;

                            MRM = 1;
                            adressField = 4;
                            if (ArrayOfWord.Contains("[") || ArrayOfWord.Contains("]"))
                            {
                                MRM = 1;
                                SIB = 1;
                                adressField = 4;
                            }
                        }
                        else if (ArrayOfWord.Contains("[") || ArrayOfWord.Contains("]"))
                        {
                            List<string> tmpList = new List<string>();
                            bool flag1 = false;

                            for (int j = 0; j < ArrayOfWord.Length; j++)
                            {
                                if (ArrayOfWord[j] == "[")
                                    flag1 = true;
                                else if (ArrayOfWord[j] == "]")
                                    flag1 = false;

                                if (flag1)
                                {
                                    tmpList.Add(ArrayOfWord[j]);
                                }
                            }

                            if (tmpList.Contains("esp") && it1 == 0)
                            {
                                Errors.Add(new Error("Use of the forbidden esp register in addressing", lineCount));
                                ++it1;
                            }

                            if (!(ArrayOfWord.Contains("PTR") && ArrayOfWord.Contains("WORD") || ArrayOfWord.Contains("DWORD") || ArrayOfWord.Contains("BYTE")) && !ArrayOfWord.Contains(":") && it2 == 0)//TODO: доробити/переробити
                            {
                                Errors.Add(new Error("Argument needs type override", lineCount));
                                ++it2;
                            }

                            MRM = 1;
                            SIB = 1;
                            adressField = 4;
                        }
                        else if (registersOfSize8.Contains(ArrayOfWord[i].ToLower()))
                            MRM = 1;

                        if (ArrayOfWord[0].ToLower() == "btr")
                        {
                            MRM = 1;
                            SIB = 1;
                        }
                        if (ArrayOfWord[0].ToLower() == "adc")
                        {
                            operandField = GetSize(ArrayOfWord[3]);
                        }
                    }
                    size = machineCommand[ArrayOfWord[0].ToLower()] + segmReplacePrefix + MRM + SIB + adressField + operandField;

                    //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    adress += size;
                }
                else
                {
                    if(ArrayOfWord[0].ToLower() == "end")
                    {
                        size = 0;
                        //Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        //Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    }
                    else if (!ArrayOfWord.Contains("end") && !ArrayOfWord.Contains(";"))
                        Errors.Add(new Error("Error, invalid instruction", lineCount));
                }
                    
            }
        }

        /// <summary>
        /// Аналіз 2 перегляду.
        /// </summary>
        public void Analysis2()
        {
            int tmp;
            string result = string.Empty;
            char[] bufferArray;

            if (Str is not null && ArrayOfWord is not null && ArrayOfWord.Length > 0)
            {
                if (ArrayOfWord.Length > 1 && directives.Contains(ArrayOfWord[1].ToLower()))
                {
                    if (ArrayOfWord[1].ToLower() == "segment")
                    {
                        Save.w.WriteLine();
                        Console.WriteLine();
                        activeSeg = 1;
                        adress = 0;
                        //segmentName = ArrayOfWord[0];
                    }
                    else if (ArrayOfWord[1].ToLower() == "ends")
                    {
                        activeSeg = 0;
                        //segmentSize = GetHexNumber(adress);
                        //segmentInfo[segmentName] = segmentSize;
                    }

                    ++lineCount2Step;
                    size = 0;
                    Save.w.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t \t{getStr()}");
                    Console.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t \t{getStr()}");
                }
                else if (ArrayOfWord.Length > 2 && dataDirectives.ContainsKey(ArrayOfWord[1]))
                {
                    if (ArrayOfWord[1].ToLower() == "equ" || ArrayOfWord[1] == "=")
                    {
                        ++lineCount2Step;
                        if (activeSeg == 0 && ArrayOfWord[1].ToLower() == "equ")
                            Errors.Add(new Error("Active_segment = 0", lineCount));

                        //tmp = Calculator.Calc(String.Join(' ', ArrayOfWord, 2, ArrayOfWord.Length - 2));
                        Save.w.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t={FindConstAndVar(ArrayOfWord[0])}\t{getStr()}");
                        Console.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t={FindConstAndVar(ArrayOfWord[0])}\t{getStr()}");
                    }
                    else
                    {
                        if (activeSeg == 0)
                            Errors.Add(new Error("Active_segment = 0", lineCount));

                        ++lineCount2Step;
                        size = dataDirectives[ArrayOfWord[1]];
                        if (ArrayOfWord[ArrayOfWord.Length - 1].EndsWith('\''))
                            size = ArrayOfWord[ArrayOfWord.Length - 1].Length - 2;

                        string buffer = string.Empty;
                        int bufferInt = 0;
                        if (ArrayOfWord[1].ToLower() == "db")
                        {
                            if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 2)
                            {
                                buffer = ArrayOfWord[2];
                                buffer = buffer.Trim('h');
                                if (Convert.ToInt32(buffer, 16) >= 256)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    if (buffer.Length > 2 && buffer.StartsWith('0'))
                                        buffer = buffer.TrimStart('0');

                                    if (buffer.Length < 2)
                                        buffer = buffer.Insert(0, "0");
                                    bytes.Add(buffer);
                                }
                            }
                            else if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 3)
                            {
                                buffer = ArrayOfWord[2].Trim('\'');
                                bufferArray = new char[buffer.Length];
                                bufferArray = buffer.ToArray();
                                foreach (var el in bufferArray)
                                {
                                    bufferInt = (int)el;
                                    bytes.Add(GetHexNumber(bufferInt) + " ");
                                }
                            }
                            else
                            {
                                buffer = ArrayOfWord[ArrayOfWord.Length - 1];
                                if (Convert.ToInt32(ArrayOfWord[ArrayOfWord.Length - 1]) < 0)
                                    buffer = buffer.Trim('-');

                                if (Convert.ToInt32(buffer) > 255)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    bytes.Add(GetHexNumber(Convert.ToInt32(buffer)));
                                }
                            }
                        }
                        else if (ArrayOfWord[1].ToLower() == "dw")
                        {
                            if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 2)
                            {
                                buffer = ArrayOfWord[2];
                                buffer = buffer.Trim('h');
                                if (Convert.ToInt32(buffer, 16) >= 32768)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    if (buffer.Length > 4 && buffer.StartsWith('0'))
                                        buffer = buffer.TrimStart('0');

                                    if (buffer.Length < 4)
                                    {
                                        while (buffer.Length != 4)
                                            buffer = buffer.Insert(0, "0");
                                    }
                                    bytes.Add(buffer);
                                }
                            }
                            else
                            {
                                buffer = ArrayOfWord[ArrayOfWord.Length - 1];

                                if (Convert.ToInt32(buffer) >= 32768 && Convert.ToInt32(buffer) < -32768)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    buffer = Convert.ToInt32(buffer).ToString("X");
                                    if (buffer.Length > 4 && (buffer.StartsWith('0') || buffer.StartsWith("F")))
                                    {
                                        buffer = buffer.TrimStart('0');
                                        while (buffer.Length > 4)
                                            buffer = buffer.Remove(0, 1);
                                    }

                                    if (buffer.Length < 4)
                                    {
                                        while (buffer.Length != 4)
                                            buffer = buffer.Insert(0, "0");
                                    }

                                    bytes.Add(buffer);
                                }
                            }
                        }
                        else if (ArrayOfWord[1].ToLower() == "dd")
                        {
                            if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 2)
                            {
                                buffer = ArrayOfWord[2];
                                buffer = buffer.Trim('h');
                                if (Convert.ToInt32(buffer, 16) >= 2147483647)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    if (buffer.Length > 8 && buffer.StartsWith('0'))
                                        buffer = buffer.TrimStart('0');

                                    if (buffer.Length < 8)
                                    {
                                        while (buffer.Length != 8)
                                            buffer = buffer.Insert(0, "0");
                                    }
                                    bytes.Add(buffer);
                                }
                            }
                            else
                            {
                                buffer = ArrayOfWord[ArrayOfWord.Length - 1];

                                if (Convert.ToInt32(buffer) >= 2147483647 && Convert.ToInt32(buffer) < -2147483647)
                                    Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                else
                                {
                                    buffer = Convert.ToInt32(buffer).ToString("X");
                                    if (buffer.Length > 8 && (buffer.StartsWith('0') || buffer.StartsWith("F")))
                                    {
                                        buffer = buffer.TrimStart('0');
                                        while (buffer.Length > 8)
                                            buffer = buffer.Remove(0, 1);
                                    }

                                    if (buffer.Length < 8)
                                    {
                                        while (buffer.Length != 8)
                                            buffer = buffer.Insert(0, "0");
                                    }

                                    bytes.Add(buffer);
                                }
                            }
                        }

                        result = string.Join("", bytes.ToArray());
                        Save.w.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t{result}\t{getStr()}");
                        Console.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t{result}\t{getStr()}");
                        adress += size;
                        bytes.Clear();
                    }
                }
                else if (ArrayOfWord.Length >= 2 && ArrayOfWord[1] == ":")
                {
                    if (activeSeg == 0)
                        Errors.Add(new Error("Active_segment = 0", lineCount));

                    ++lineCount2Step;
                    Save.w.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t\t{getStr()}");
                    Console.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t\t{getStr()}");
                }
                else if (machineCommand.ContainsKey(ArrayOfWord[0].ToLower()) || ArrayOfWord.Length >= 3 && machineCommand.ContainsKey(ArrayOfWord[2].ToLower()))
                {
                    ++lineCount2Step;
                    size = machineCommand[ArrayOfWord[0].ToLower()];
                    if (activeSeg == 0)
                        Errors.Add(new Error("Active_segment = 0", lineCount));

                    int changeAdress = 0;
                    int segmReplacePrefix = 0;
                    int MRM = 0;
                    int SIB = 0;
                    int adressField = 0;
                    int operandField = 0;

                    string changeAdressByte = String.Empty;
                    string segmReplacePrefixByte = String.Empty;
                    string machineCommandByte = String.Empty;
                    string MRMByte = String.Empty;
                    string SIBByte = String.Empty;
                    string adressFieldByte = String.Empty;
                    string operandFieldByte = String.Empty;

                    if (ArrayOfWord.Length >= 1)
                    {
                        if (ArrayOfWord[0].ToLower() == "jmp")
                        {
                            machineCommandByte = "EB";
                            MRM = 1;
                            MRMByte = GetHexNumber(Convert.ToInt32(FindAdressOfLabel(ArrayOfWord[2]), 16) - (adress + 2));
                            while (MRMByte.Length > 2)
                                MRMByte = MRMByte.Remove(0, 1);
                        }
                        else if (ArrayOfWord[0].ToLower() == "jnbe")
                        {
                            machineCommandByte = "77";
                            if (lineCount2Step > FindLabel(ArrayOfWord[1]))
                            {
                                MRM = 1;
                                MRMByte = GetHexNumber(Convert.ToInt32(FindAdressOfLabel(ArrayOfWord[1]), 16) - (adress + 2));
                                while (MRMByte.Length > 2)
                                    MRMByte = MRMByte.Remove(0, 1);
                            }
                            else
                            {
                                MRM = 5;
                                MRMByte = GetHexNumber(Convert.ToInt32(FindAdressOfLabel(ArrayOfWord[1]), 16) - (adress + 2));
                                while (MRMByte.Length > 2)
                                    MRMByte = MRMByte.Remove(0, 1);
                                MRMByte = MRMByte + " 90 90 90 90";
                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "std")
                        {
                            Console.WriteLine("lol");
                            machineCommandByte = "FD";
                        }
                        else if (ArrayOfWord[0].ToLower() == "dec")
                        {
                            if (!registersOfSize8.Contains(ArrayOfWord[0].ToLower()) && registersOfSize32.Contains(ArrayOfWord[1].ToLower()))
                            {
                                if (ArrayOfWord[1].ToLower() == "eax")
                                    machineCommandByte = "48";
                                else if (ArrayOfWord[1].ToLower() == "ebx")
                                    machineCommandByte = "4B";
                                else if (ArrayOfWord[1].ToLower() == "ecx")
                                    machineCommandByte = "49";
                                else if (ArrayOfWord[1].ToLower() == "edx")
                                    machineCommandByte = "4A";
                                else if (ArrayOfWord[1].ToLower() == "esi")
                                    machineCommandByte = "4E";
                                else if (ArrayOfWord[1].ToLower() == "edi")
                                    machineCommandByte = "4F";
                                else if (ArrayOfWord[1].ToLower() == "esp")
                                    machineCommandByte = "4C";
                                else if (ArrayOfWord[1].ToLower() == "ebp")
                                    machineCommandByte = "4D";
                            }
                            else
                            {
                                machineCommandByte = "FE";
                                if (ArrayOfWord[1].ToLower() == "ah")
                                    MRMByte = "CC";
                                else if (ArrayOfWord[1].ToLower() == "al")
                                    MRMByte = "C8";
                                else if (ArrayOfWord[1].ToLower() == "bh")
                                    MRMByte = "CF";
                                else if (ArrayOfWord[1].ToLower() == "bl")
                                    MRMByte = "CB";
                                else if (ArrayOfWord[1].ToLower() == "ch")
                                    MRMByte = "CD";
                                else if (ArrayOfWord[1].ToLower() == "cl")
                                    MRMByte = "C9";
                                else if (ArrayOfWord[1].ToLower() == "dh")
                                    MRMByte = "CE";
                                else if (ArrayOfWord[1].ToLower() == "dl")
                                    MRMByte = "CA";
                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "not")
                        {
                            string buffer = String.Empty;

                            for (int i = 0; i < ArrayOfWord.Length; i++)
                            {
                                if (ArrayOfWord[i].ToLower() == "ptr" && ArrayOfWord[i - 1].ToLower() == "word")
                                {
                                    changeAdress = 1;
                                    changeAdressByte = "66|";
                                }

                                if (FindConstAndVar(ArrayOfWord[2]) != null && FindConstAndVar(ArrayOfWord[2]).Length <= 2)
                                    machineCommandByte = "F6";
                                else
                                    machineCommandByte = "F7";

                                MRM = 1;
                                MRMByte = "14";

                                SIB = 1;
                                if (ArrayOfWord[i].ToLower() == "eax")
                                    SIBByte = "45";
                                else if (ArrayOfWord[i].ToLower() == "ebx")
                                    SIBByte = "5D";
                                else if (ArrayOfWord[i].ToLower() == "ecx")
                                    SIBByte = "4D";
                                else if (ArrayOfWord[i].ToLower() == "edx")
                                    SIBByte = "55";
                                else if (ArrayOfWord[i].ToLower() == "esi")
                                    SIBByte = "75";
                                else if (ArrayOfWord[i].ToLower() == "edi")
                                    SIBByte = "7D";
                                else if (ArrayOfWord[i].ToLower() == "ebp")
                                    SIBByte = "6D";

                                operandField = 4;
                                if (FindConstAndVar(ArrayOfWord[i], true) != null)
                                {
                                    buffer = FindConstAndVar(ArrayOfWord[i], true);
                                    while (buffer.Length < 8)
                                        buffer = buffer.Insert(0, "0");

                                    operandFieldByte = buffer;
                                }

                                if (FindConstAndVar(ArrayOfWord[i]) != null)
                                {
                                    if (FindConstAndVar(ArrayOfWord[i]).Length > 2)
                                        buffer = "00000001";
                                    else
                                        buffer = "00000000";
                                }

                                if (registersOfSize32.Contains(ArrayOfWord[i].ToLower()))
                                    buffer = "00000000";

                                if (ArrayOfWord[i] == ":" && segmentRegisters.Contains(ArrayOfWord[i - 1].ToLower()))
                                {
                                    buffer = buffer + "r";
                                    if (ArrayOfWord[i - 1].ToLower() == "es")
                                    {
                                        segmReplacePrefix = 1;
                                        segmReplacePrefixByte = "26";
                                    }
                                    if (ArrayOfWord[i - 1].ToLower() == "cs")
                                    {
                                        segmReplacePrefix = 1;
                                        segmReplacePrefixByte = "2E";
                                    }
                                    if (ArrayOfWord[i - 1].ToLower() == "ss")
                                    {
                                        segmReplacePrefix = 1;
                                        segmReplacePrefixByte = "36";
                                    }
                                    if (ArrayOfWord[i - 1].ToLower() == "fs")
                                    {
                                        segmReplacePrefix = 1;
                                        segmReplacePrefixByte = "64";
                                    }
                                    if (ArrayOfWord[i - 1].ToLower() == "gs")
                                    {
                                        segmReplacePrefix = 1;
                                        segmReplacePrefixByte = "65";
                                    }
                                }

                                adressFieldByte = buffer;
                            }

                        }
                        else if (ArrayOfWord[0].ToLower() == "rcl")
                        {
                            MRM = 1;
                            if (registersOfSize8.Contains(ArrayOfWord[1].ToLower()))
                            {
                                machineCommandByte = "D2";
                                if (ArrayOfWord[1].ToLower() == "al")
                                    MRMByte = "D0";
                                else if (ArrayOfWord[1].ToLower() == "ah")
                                    MRMByte = "D4";
                                else if (ArrayOfWord[1].ToLower() == "bl")
                                    MRMByte = "D3";
                                else if (ArrayOfWord[1].ToLower() == "bh")
                                    MRMByte = "D7";
                                else if (ArrayOfWord[1].ToLower() == "cl")
                                    MRMByte = "D1";
                                else if (ArrayOfWord[1].ToLower() == "ch")
                                    MRMByte = "D5";
                                else if (ArrayOfWord[1].ToLower() == "dl")
                                    MRMByte = "D2";
                                else if (ArrayOfWord[1].ToLower() == "dh")
                                    MRMByte = "D6";
                            }
                            else if (registersOfSize32.Contains(ArrayOfWord[1].ToLower()))
                            {
                                machineCommandByte = "D3";
                                if (ArrayOfWord[1].ToLower() == "eax")
                                    MRMByte = "D0";
                                else if (ArrayOfWord[1].ToLower() == "ebx")
                                    MRMByte = "D3";
                                else if (ArrayOfWord[1].ToLower() == "ecx")
                                    MRMByte = "D1";
                                else if (ArrayOfWord[1].ToLower() == "edx")
                                    MRMByte = "D2";
                                else if (ArrayOfWord[1].ToLower() == "esi")
                                    MRMByte = "D6";
                                else if (ArrayOfWord[1].ToLower() == "edi")
                                    MRMByte = "D7";
                                else if (ArrayOfWord[1].ToLower() == "esp")
                                    MRMByte = "D4";
                                else if (ArrayOfWord[1].ToLower() == "ebp")
                                    MRMByte = "D5";
                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "imul")
                        {
                            string buffer = String.Empty;
                            int exp = 0;
                            machineCommandByte = "0F AF";
                            for (int i = 0; i < ArrayOfWord.Length; i++)
                            {
                                if (ArrayOfWord[i] == ":" && segmentRegisters.Contains(ArrayOfWord[i - 1].ToLower()))
                                {
                                    segmReplacePrefix = 1;
                                    if (ArrayOfWord[i - 1].ToLower() == "ds")
                                        segmReplacePrefix = 0;
                                    else
                                        segmReplacePrefixByte = segmentRegisterPreficses[ArrayOfWord[i - 1].ToLower()];
                                }

                                if ((FindConstAndVar(ArrayOfWord[i]) != null || FindConstAndVar(ArrayOfWord[i], true) != null) && ArrayOfWord.Contains("*"))
                                {
                                    MRM = 1;
                                    SIB = 1;

                                    if (FindConstAndVar(ArrayOfWord[i]) != null)
                                    {
                                        adressField = 4;
                                        if (FindConstAndVar(ArrayOfWord[i]).Length > 2)
                                            adressFieldByte = "00000001r";
                                        else
                                            adressFieldByte = "00000000r";
                                    }
                                    else if (FindConstAndVar(ArrayOfWord[i], true) != null)
                                    {
                                        adressField = 4;
                                        buffer = FindConstAndVar(ArrayOfWord[i], true);
                                        while (buffer.Length < 8)
                                            buffer = buffer.Insert(0, "0");
                                        adressFieldByte = buffer;
                                    }
                                    else if (ArrayOfWord[i] == "[" && FindConstAndVar(ArrayOfWord[i - 1]) == null)
                                        adressFieldByte = "00000000";

                                    if (ArrayOfWord[1].ToLower() == "eax")
                                        MRMByte = "04";
                                    else if (ArrayOfWord[1].ToLower() == "ebx")
                                        MRMByte = "AF";
                                    else if (ArrayOfWord[1].ToLower() == "ecx")
                                        MRMByte = "0C";
                                    else if (ArrayOfWord[1].ToLower() == "edx")
                                        MRMByte = "14";
                                    else if (ArrayOfWord[1].ToLower() == "esi")
                                        MRMByte = "34";
                                    else if (ArrayOfWord[1].ToLower() == "edi")
                                        MRMByte = "3C";
                                    else if (ArrayOfWord[1].ToLower() == "esp")
                                        MRMByte = "24";
                                    else if (ArrayOfWord[1].ToLower() == "ebp")
                                        MRMByte = "2C";

                                    buffer = String.Join(' ', ArrayOfWord, Array.IndexOf(ArrayOfWord, "[") + 1, Array.IndexOf(ArrayOfWord, "]") - (Array.IndexOf(ArrayOfWord, "[") + 1));
                                    if (buffer == "eax * 2")
                                        SIBByte = "45";
                                    else if (buffer == "eax * 4")
                                        SIBByte = "85";
                                    else if (buffer == "eax * 8")
                                        SIBByte = "C5";
                                    else if (buffer == "ebx * 2")
                                        SIBByte = "5D";
                                    else if (buffer == "ebx * 4")
                                        SIBByte = "9D";
                                    else if (buffer == "ebx * 8")
                                        SIBByte = "DD";
                                    else if (buffer == "ecx * 2")
                                        SIBByte = "4D";
                                    else if (buffer == "ecx * 4")
                                        SIBByte = "8D";
                                    else if (buffer == "ecx * 8")
                                        SIBByte = "CD";
                                    else if (buffer == "edx * 2")
                                        SIBByte = "55";
                                    else if (buffer == "edx * 4")
                                        SIBByte = "95";
                                    else if (buffer == "edx * 8")
                                        SIBByte = "D5";
                                    else if (buffer == "edi * 2")
                                        SIBByte = "7D";
                                    else if (buffer == "edi * 4")
                                        SIBByte = "BD";
                                    else if (buffer == "edi * 8")
                                        SIBByte = "FD";
                                    else if (buffer == "esi * 2")
                                        SIBByte = "75";
                                    else if (buffer == "esi * 4")
                                        SIBByte = "B5";
                                    else if (buffer == "esi * 8")
                                        SIBByte = "F5";
                                    else if (buffer == "ebp * 2")
                                        SIBByte = "6D";
                                    else if (buffer == "ebp * 4")
                                        SIBByte = "AD";
                                    else if (buffer == "ebp * 8")
                                        SIBByte = "ED";
                                }
                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "and")
                        {
                            string buffer = String.Empty;
                            machineCommandByte = "20";
                            if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "al")
                                MRMByte = "04";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "ah")
                                MRMByte = "24";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "bl")
                                MRMByte = "3C";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "bh")
                                MRMByte = "1C";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "cl")
                                MRMByte = "2C";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "ch")
                                MRMByte = "0C";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "dl")
                                MRMByte = "34";
                            else if (ArrayOfWord[ArrayOfWord.Length - 1].ToLower() == "dh")
                                MRMByte = "14";
                            for (int i = 0; i < ArrayOfWord.Length; i++)
                            {
                                if (ArrayOfWord[i] == ":" && segmentRegisters.Contains(ArrayOfWord[i - 1].ToLower()))
                                {
                                    segmReplacePrefix = 1;
                                    if (ArrayOfWord[i - 1].ToLower() == "ds")
                                        segmReplacePrefix = 0;
                                    else
                                        segmReplacePrefixByte = segmentRegisterPreficses[ArrayOfWord[i - 1].ToLower()];
                                }

                                if ((FindConstAndVar(ArrayOfWord[i]) != null || FindConstAndVar(ArrayOfWord[i], true) != null) && ArrayOfWord.Contains("*"))
                                {
                                    MRM = 1;
                                    SIB = 1;
                                    adressField = 4;

                                    if (FindConstAndVar(ArrayOfWord[i]) != null)
                                    {
                                        adressField = 4;
                                        if (FindConstAndVar(ArrayOfWord[i]).Length > 2)
                                            adressFieldByte = "00000001r";
                                        else
                                            adressFieldByte = "00000000r";
                                    }
                                    else if (FindConstAndVar(ArrayOfWord[i], true) != null)
                                    {
                                        adressField = 4;
                                        buffer = FindConstAndVar(ArrayOfWord[i], true);
                                        while (buffer.Length < 8)
                                            buffer = buffer.Insert(0, "0");
                                        adressFieldByte = buffer;
                                    }
                                }
                                else if (ArrayOfWord[i] == "[" && FindConstAndVar(ArrayOfWord[i - 1]) == null)
                                    adressFieldByte = "00000000";

                                buffer = String.Join(' ', ArrayOfWord, Array.IndexOf(ArrayOfWord, "[") + 1, Array.IndexOf(ArrayOfWord, "]") - (Array.IndexOf(ArrayOfWord, "[") + 1));
                                if (buffer == "eax * 2")
                                    SIBByte = "45";
                                else if (buffer == "eax * 4")
                                    SIBByte = "85";
                                else if (buffer == "eax * 8")
                                    SIBByte = "C5";
                                else if (buffer == "ebx * 2")
                                    SIBByte = "5D";
                                else if (buffer == "ebx * 4")
                                    SIBByte = "9D";
                                else if (buffer == "ebx * 8")
                                    SIBByte = "DD";
                                else if (buffer == "ecx * 2")
                                    SIBByte = "4D";
                                else if (buffer == "ecx * 4")
                                    SIBByte = "8D";
                                else if (buffer == "ecx * 8")
                                    SIBByte = "CD";
                                else if (buffer == "edx * 2")
                                    SIBByte = "55";
                                else if (buffer == "edx * 4")
                                    SIBByte = "95";
                                else if (buffer == "edx * 8")
                                    SIBByte = "D5";
                                else if (buffer == "edi * 2")
                                    SIBByte = "7D";
                                else if (buffer == "edi * 4")
                                    SIBByte = "BD";
                                else if (buffer == "edi * 8")
                                    SIBByte = "FD";
                                else if (buffer == "esi * 2")
                                    SIBByte = "75";
                                else if (buffer == "esi * 4")
                                    SIBByte = "B5";
                                else if (buffer == "esi * 8")
                                    SIBByte = "F5";
                                else if (buffer == "ebp * 2")
                                    SIBByte = "6D";
                                else if (buffer == "ebp * 4")
                                    SIBByte = "AD";
                                else if (buffer == "ebp * 8")
                                    SIBByte = "ED";

                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "btr")
                        {
                            machineCommandByte = "0F BA";
                            MRM = 1;
                            operandField = 1;

                            if (ArrayOfWord[1].ToLower() == "eax")
                                MRMByte = "F0";
                            else if (ArrayOfWord[1].ToLower() == "ebx")
                                MRMByte = "F3";
                            else if (ArrayOfWord[1].ToLower() == "ecx")
                                MRMByte = "F1";
                            else if (ArrayOfWord[1].ToLower() == "edx")
                                MRMByte = "F2";
                            else if (ArrayOfWord[1].ToLower() == "esi")
                                MRMByte = "F6";
                            else if (ArrayOfWord[1].ToLower() == "edi")
                                MRMByte = "F7";
                            else if (ArrayOfWord[1].ToLower() == "esp")
                                MRMByte = "F4";
                            else if (ArrayOfWord[1].ToLower() == "ebp")
                                MRMByte = "F5";

                            if (FindConstAndVar(ArrayOfWord[ArrayOfWord.Length - 1], true) != null)
                                operandFieldByte = FindConstAndVar(ArrayOfWord[ArrayOfWord.Length - 1], true);
                            else
                            {
                                string buffer1 = String.Empty;
                                int bufferInt1 = 0;
                                char bufferChar = default;
                                if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 2)
                                {
                                    buffer1 = ArrayOfWord[3];
                                    buffer1 = buffer1.Trim('h');
                                    if (Convert.ToInt32(buffer1, 16) >= 256)
                                        Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                    else
                                    {
                                        if (buffer1.Length > 2 && buffer1.StartsWith('0'))
                                            buffer1 = buffer1.TrimStart('0');

                                        if (buffer1.Length < 2)
                                            buffer1 = buffer1.Insert(0, "0");
                                        operandFieldByte = buffer1;
                                    }
                                }
                                else if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 3)
                                {
                                    buffer1 = ArrayOfWord[3].Trim('\'');
                                    bufferChar = Convert.ToChar(buffer1);
                                    buffer1 = ((int)bufferChar).ToString();
                                    operandFieldByte = buffer1;
                                }
                                else
                                {
                                    buffer1 = ArrayOfWord[ArrayOfWord.Length - 1];
                                    if (Convert.ToInt32(ArrayOfWord[ArrayOfWord.Length - 1]) < 0)
                                        buffer1 = buffer1.Trim('-');

                                    if (Convert.ToInt32(buffer1) > 255)
                                        Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                                    else
                                    {
                                        bytes.Add(GetHexNumber(Convert.ToInt32(buffer1)));
                                    }
                                }
                            }
                        }
                        else if (ArrayOfWord[0].ToLower() == "adc")
                        {
                            string buffer = String.Empty;
                            machineCommandByte = "83";
                            MRM = 1;
                            SIB = 1;
                            MRMByte = "14";

                            buffer = String.Join(' ', ArrayOfWord, Array.IndexOf(ArrayOfWord, "[") + 1, Array.IndexOf(ArrayOfWord, "]") - (Array.IndexOf(ArrayOfWord, "[") + 1));
                            if (buffer == "eax * 2")
                                SIBByte = "45";
                            else if (buffer == "eax * 4")
                                SIBByte = "85";
                            else if (buffer == "eax * 8")
                                SIBByte = "C5";
                            else if (buffer == "ebx * 2")
                                SIBByte = "5D";
                            else if (buffer == "ebx * 4")
                                SIBByte = "9D";
                            else if (buffer == "ebx * 8")
                                SIBByte = "DD";
                            else if (buffer == "ecx * 2")
                                SIBByte = "4D";
                            else if (buffer == "ecx * 4")
                                SIBByte = "8D";
                            else if (buffer == "ecx * 8")
                                SIBByte = "CD";
                            else if (buffer == "edx * 2")
                                SIBByte = "55";
                            else if (buffer == "edx * 4")
                                SIBByte = "95";
                            else if (buffer == "edx * 8")
                                SIBByte = "D5";
                            else if (buffer == "edi * 2")
                                SIBByte = "7D";
                            else if (buffer == "edi * 4")
                                SIBByte = "BD";
                            else if (buffer == "edi * 8")
                                SIBByte = "FD";
                            else if (buffer == "esi * 2")
                                SIBByte = "75";
                            else if (buffer == "esi * 4")
                                SIBByte = "B5";
                            else if (buffer == "esi * 8")
                                SIBByte = "F5";
                            else if (buffer == "ebp * 2")
                                SIBByte = "6D";
                            else if (buffer == "ebp * 4")
                                SIBByte = "AD";
                            else if (buffer == "ebp * 8")
                                SIBByte = "ED";
                        }
                    }

                    if (FindConstAndVar(ArrayOfWord[ArrayOfWord.Length - 1], true) != null)
                        operandFieldByte = FindConstAndVar(ArrayOfWord[ArrayOfWord.Length - 1], true);
                    else
                    {
                        string buffer1 = String.Empty;
                        int bufferInt1 = 0;
                        char[] bufferArray1;
                        if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 2)
                        {
                            buffer1 = ArrayOfWord[ArrayOfWord.Length - 1];
                            buffer1 = buffer1.Trim('h');
                            if (Convert.ToInt32(buffer1, 16) >= 2147483647)
                                Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                            else
                            {
                                if (buffer1.Length > 8 && buffer1.StartsWith('0'))
                                    buffer1 = buffer1.TrimStart('0');

                                if (buffer1.Length < 2 || buffer1.Length % 2 == 1)
                                    buffer1 = buffer1.Insert(0, "0");
                                operandFieldByte = buffer1;
                            }
                        }
                        else if (CheckConst(ArrayOfWord[ArrayOfWord.Length - 1]) == 3)
                        {
                            buffer1 = ArrayOfWord[3].Trim('\'');
                            bufferArray1 = new char[buffer1.Length];
                            bufferArray1 = buffer1.ToCharArray();
                            buffer1 = String.Empty;
                            foreach(var el in bufferArray1)
                            {
                                buffer1 += ((int)el).ToString();
                            }
                            operandFieldByte = buffer1;
                        }
                        else
                        {
                            buffer1 = ArrayOfWord[ArrayOfWord.Length - 1];
                            if (int.TryParse(buffer1, out int result1))
                                buffer1 = buffer1.Trim('-');

                            if (int.TryParse(buffer1, out int result2) && result2 > 2147483647)
                                Errors.Add(new Error("The value of the variable or constant is too large", lineCount2Step));
                            else
                            {
                                operandFieldByte = buffer1;
                            }
                        }
                    }
                    result = changeAdressByte + " " + segmReplacePrefixByte + " " + machineCommandByte + " " + MRMByte + " " + SIBByte + " " + adressFieldByte + " " + operandFieldByte;
                    size += changeAdress + segmReplacePrefix + MRM + SIB + adressField + operandField;
                    Save.w.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t{result}\t{getStr()}");
                    Console.WriteLine($"{lineCount2Step,0:d3}\t{GetHexNumber(adress)}\t{result}\t{getStr()}");
                    adress += size;
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
                return result; // ціле число
            else if (value.EndsWith('h'))
                return 2; // шістнадцядкове число
            else if (value.StartsWith('\'') && value.EndsWith('\''))
                return 3; // текстова константа
            else
                return 0;
        }

        /// <summary>
        /// Метод який нормалізує рядок з операндами.
        /// </summary>
        /// <returns></returns>
        private string getStr()
        {
            Regex regex = new Regex(@"\s+");

            if(Str is not null)
            {
                string str = Str.Trim();
                str = regex.Replace(str, " ");
                return str;
            }

            return null;
        }

        /// <summary>
        /// Конвертація в шістнадцяткову систему числення.
        /// </summary>
        /// <param name="intValue"></param>
        /// <returns></returns>
        private string GetHexNumber(int intValue, int x = 0) 
        {
            string hexValue;
            if (intValue < 0 && x == 1)
                 hexValue = "-" + Math.Abs(intValue).ToString("X");
            else
                hexValue = intValue.ToString("X");
            
            if (x == 1)
                return "=" + hexValue;

            return hexValue;
        }

        /// <summary>
        /// Визначення типу ідентифікаторів.
        /// </summary>
        /// <param name="varDirOrLabel"></param>
        /// <returns></returns>
        private string GetVarOrLabelType(string varDirOrLabel)
        {
            if (varDirOrLabel.ToLower() == "db")
                return "BYTE";
            else if (varDirOrLabel.ToLower() == "dw")
                return "WORD";
            else if (varDirOrLabel.ToLower() == "dd")
                return "DWORD";
            else if (varDirOrLabel.ToLower() == ":")
                return "NEAR";
            else if (varDirOrLabel.ToLower() == "equ")
                return "equ";
            else if (varDirOrLabel.ToLower() == "=")
                return "=";
            return null;
        }

        /// <summary>
        /// Метод для додавання опису міток чи імен змінних/констант до списку.
        /// Якщо опис(ім'я) змінної чи мутки дублюється лічильник помилок плюсується.
        /// </summary>
        /// <param name="labelOrVar"></param>
        public void Add(UserLabelAndVariable labelOrVar)
        {
            bool flag1 = true;
            bool flag2 = true;
            foreach(var el in UserVar)
            {
                if (el.name.ToLower() == labelOrVar.name.ToLower())
                    flag1 = false;
            }

            if (Char.IsDigit(labelOrVar.name[0]))
            {
                Errors.Add(new Error("Invalid name of var/const/label", lineCount));
                flag2 = false;
            }

            if (flag1 && flag2)
                UserVar.Add(labelOrVar);
            else if (flag2)
            {
                if (labelOrVar.type != "=")
                    Errors.Add(new Error("Duplication user var or label", lineCount));
            }


        }

        /// <summary>
        /// Метод для запису у файл і виведення на консоль опису ідентифікаторів.
        /// </summary>
        public void SaveAndPrintInfo()
        {
            foreach (var el in UserVar)
            {
                Save.w.WriteLine($"{el.name}\t{el.type}\t{el.adress}\t{el.segment}");
                Console.WriteLine($"{el.name}\t{el.type}\t{el.adress}\t{el.segment}");
            }
        }

        /// <summary>
        /// Метод для визначення розміру в байтах операнда.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private int GetSize(string str)
        {
            int tmp;
            if (CheckConst(str) == 2)
            {
                tmp = Convert.ToInt32(str, 16);
                if (tmp <= 127 && tmp >= -128)
                    return 1;
                else
                    return 4;
            }
            else if (CheckConst(str) == 3)
                return str.Length - 2;
            else
            {
                if (CheckConst(str) <= 127 && CheckConst(str) >= -128)
                    return 1;
                else
                    return 4;
            }
                
        }

        /// <summary>
        /// Метод для пошуку і опису міток.
        /// </summary>
        public void VariableProcessing()
        {
            if (Str is not null && ArrayOfWord is not null && ArrayOfWord.Length > 0)
            {
                ++localLineCount;
                if (ArrayOfWord.Length >= 2 && ArrayOfWord[1] == ":")
                    labels.Add(ArrayOfWord[0], localLineCount);
            }
        }

        /// <summary>
        /// Метод який повертає рядок на якому знаходиться певна мітка.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public int FindLabel(string label)
        {
            foreach (var el in labels)
            {
                if (labels.ContainsKey(label))
                    return labels[label];
            }
            return 0;
        }

        /// <summary>
        /// Метод для знаходження адреси конкретної мітки чи змінної.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public string FindAdressOfLabel(string label)
        {
            foreach (var el in UserVar)
            {
                if (el.name == label.ToLower())
                    return el.adress;
            }
            return null;
        }

        /// <summary>
        /// Метод для знаходження констант і змінних.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string FindConstAndVar(string name, bool findConst = false) 
        {
            if (!findConst)
            {
                foreach (var el in UserVar)
                {
                    if (el.name == name.Trim())
                        return el.size;
                }
            }
            else
            {
                foreach (var el in UserVar)
                {
                    if (el.name == name.Trim() && (el.type == "=" || el.type == "equ"))
                        return el.size;
                }
            }
            return null;
        }

    }
}

/// <summary>
/// Метод для опису змінних і міток.
/// </summary>
class UserLabelAndVariable
{
    /// <summary>
    /// Ім'я змінної чи мітки.
    /// </summary>
    public string name;

    /// <summary>
    /// Розмір змінної чи константи в байтах.
    /// </summary>
    public string size;

    /// <summary>
    /// Тип пам'яті.
    /// </summary>
    public string type;

    /// <summary>
    /// Номер рядка.
    /// </summary>
    public int line;

    /// <summary>
    /// Адреса в пам'яті.
    /// </summary>
    public string adress;

    /// <summary>
    /// Назва сегмента в якому знаходиться мітка чи змінна.
    /// </summary>
    public string segment;

    /// <summary>
    /// Створення об'єкта який буде описувати нашу змінну чи мітку.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="adress"></param>
    /// <param name="segment"></param>
    public UserLabelAndVariable(string name, string type, string adress, string segment, int line, string size)
    {
        this.name = name;
        this.segment = segment;
        this.type = type;
        this.adress = adress;
        this.line = line;
        this.size = size;
    }
}

/// <summary>
/// Клас для опису помилок.
/// </summary>
class Error
{
    /// <summary>
    /// Рядок коду на якому була виявлена помилка.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Повідомлення помилки.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Лічильник помилок.
    /// </summary>
    public static int errorsCount = 0;

    public Error(string message, int line = 0)
    {
        Line = line;
        Message = message;
        ++errorsCount;
    }
}

/// <summary>
/// Клас для розрахунку математичних виразів.
/// </summary>
class Calculator
{
    private static DataTable Table { get; } = new DataTable();

    /// <summary>
    /// Метод для обрахунку.
    /// </summary>
    /// <param name="Expression"></param>
    /// <returns></returns>
    public static int Calc(string Expression)
    {
        string tmp = String.Empty;
        char[] tmpArray;
        if (Expression.Contains('h'))
            return Convert.ToInt32(Expression, 16);
        else if (Expression.StartsWith('\'') && Expression.EndsWith('\''))
        {
            tmp = Expression.Trim('\'');
            tmpArray = new char[tmp.Length];
            tmpArray = tmp.ToCharArray();
            for(int i = tmpArray.Length - 1; i >= 0; i--)
            {
                Tokenizer.constBuffer += ((int)tmpArray[i]).ToString();
            }
            return int.MinValue;
        }
        return Convert.ToInt32(Table.Compute(Expression, string.Empty));
    }
}
