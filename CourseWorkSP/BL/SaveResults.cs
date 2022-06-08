
namespace CourseWorkSP.BL
{
    /// <summary>
    /// Клас для запису результатів в файл лістингу.
    /// </summary>
    internal class Save
    {
        /// <summary>
        /// Ім'я файла результатів.
        /// </summary>
        private string _name;

        /// <summary>
        /// Потік для запису.
        /// </summary>
        public static StreamWriter w;

        public Save(string name)
        {
            _name = name;
            w = new StreamWriter(name, true);

            w.WriteLine("Assembly translator STEP 1");
            w.WriteLine("Written by Statechniy Serhii KV-03");
            w.WriteLine($"File name: ");
            w.WriteLine("1-line, 2-address, 3-size, 4-assembly operator");
        }
    }
}
