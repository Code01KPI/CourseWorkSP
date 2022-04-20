using CourseWorkSP.BL;

Reader reader = new Reader("C:\\test12.asm");
Parser parser = new Parser();
Tokenizer tokenizer = new Tokenizer();
GetStr(parser);


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

