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
        /// Розмір операнда.
        /// </summary>
        private static int size = 0;

        /// <summary>
        /// Адреса операнда в пам'яті.
        /// </summary>
        private static int adress = 0;

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
                    Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                }
                else if (ArrayOfWord.Length > 2 && dataDirectives.ContainsKey(ArrayOfWord[1]))
                {
                    if (ArrayOfWord[1].ToLower() == "equ" || ArrayOfWord[1] == "=")
                    {
                        ++lineCount;
                        if (activeSeg == 0 && ArrayOfWord[1].ToLower() == "equ")
                            Errors.Add(new Error("Active_segment = 0", lineCount));
                        
                        tmp = Calculator.Calc(String.Join(' ', ArrayOfWord, 2, ArrayOfWord.Length - 2));
                        Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount));
                        size = tmp;
                        Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size, 1)} \t{getStr()}");
                        Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size, 1)} \t{getStr()}");
                    }
                    else
                    {
                        if (activeSeg == 0)
                            Errors.Add(new Error("Active_segment = 0", lineCount));

                        ++lineCount;
                        size = dataDirectives[ArrayOfWord[1]];
                        Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount));
                        Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        adress += size;
                    }
                }
                else if (ArrayOfWord.Length >= 2 && ArrayOfWord[1] == ":")
                {
                    if (activeSeg == 0)
                        Errors.Add(new Error("Active_segment = 0", lineCount));

                    ++lineCount;
                    size = 0;
                    Add(new UserLabelAndVariable(ArrayOfWord[0], GetVarOrLabelType(ArrayOfWord[1]), GetHexNumber(adress), segmentName, lineCount));
                    Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
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
                            if (ArrayOfWord[1].ToLower() == "short" && FindLabel(ArrayOfWord[2]) > lineCount)
                                MRM = 5;
                            else if (ArrayOfWord[0].ToLower() == "jnbe" && FindLabel(ArrayOfWord[1]) > lineCount)
                                MRM = 5;
                            else if ((ArrayOfWord[1].ToLower() == "short" && FindLabel(ArrayOfWord[2]) == 0) || ((ArrayOfWord[0].ToLower() == "jnbe" && FindLabel(ArrayOfWord[1]) == 0)))
                                Errors.Add(new Error("The required label is missing", lineCount));
                            else
                                MRM = 1;
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

                    Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    adress += size;
                }
                else
                {
                    if(ArrayOfWord[0].ToLower() == "end")
                    {
                        size = 0;
                        Save.w.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                        Console.WriteLine($"{lineCount,0:d3}\t{GetHexNumber(adress)}\t{GetHexNumber(size)} \t{getStr()}");
                    }
                    else if (!ArrayOfWord.Contains("end") && !ArrayOfWord.Contains(";"))
                        Errors.Add(new Error("Error, invalid instruction", lineCount));
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
            if (intValue < 0)
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
    public int size;

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
    public UserLabelAndVariable(string name, string type, string adress, string segment, int line)
    {
        this.name = name;
        this.segment = segment;
        this.type = type;
        this.adress = adress;
        this.line = line;
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
        if (Expression.Contains('h'))
            return Convert.ToInt32(Expression, 16);
        return Convert.ToInt32(Table.Compute(Expression, string.Empty));
    }
}
