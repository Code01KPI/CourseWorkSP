using CourseWorkSP.BL;

Console.WriteLine("Assembly translator STEP 1");
Console.WriteLine("Written by Statechniy Serhii KV-03");
Console.WriteLine($"File name: ");
Console.WriteLine("1-line, 2-address, 3-size, 4-assembly operator");

Reader reader = new Reader("C:\\testm5.asm");
Parser parser = new Parser();
Save saver = new Save("results.lst");
Tokenizer tokenizer = new Tokenizer();
GetStr(parser);

Save.w.WriteLine("\nSEGMENT  SIZE");
Console.WriteLine("\nSEGMENT  SIZE");
foreach (var segment in tokenizer.segmentInfo)
{
    Save.w.WriteLine($"{segment.Key}  {segment.Value}");
    Console.WriteLine($"{segment.Key}  {segment.Value}");
}

Save.w.WriteLine("\nName\tType\tAdress\tSegment");
Console.WriteLine("\nName\tType\tAdress\tSegment");
tokenizer.SaveAndPrintInfo();

Console.WriteLine($"\nErrors {Error.errorsCount}");
Save.w.WriteLine($"\nErrors {Error.errorsCount}");

Console.WriteLine();
foreach (var el in tokenizer.Errors)
    Console.WriteLine($"Message: {el.Message}; Line: {el.Line}");
Save.w.Close();

void GetStr(Parser parser)
{
    StreamReader f = new StreamReader(reader.FileName);
    while (true)
    {
        parser.Str = reader.ReadFile(f);

        tokenizer.Str = parser.Str;
        tokenizer.ArrayOfWord = parser.ParseStr();

        tokenizer.Analysis();

        if (!reader.IsReadStr)
            break;
    }
    f.Close();
}

