
namespace CourseWorkSP.BL
{
    /// <summary>
    /// Класс який відповідає за зчитування по одному рядку файла.
    /// </summary>
    internal class Reader
    {   
        /// <summary>
        /// Стан зчитування.
        /// </summary>
        public bool IsReadStr { get; private set; }

        /// <summary>
        /// Назва файла.
        /// </summary>
        public string FileName { get; } 

        public Reader(string fileName)
        {
            FileName = fileName;
            IsReadStr = false;
        }   
        
        /// <summary>
        /// Метод для зчитування.
        /// </summary>
        /// <returns>Один зчитаний рядок</returns>
        public string? ReadFile(StreamReader f)
        {
            IsReadStr = false;
            if(!f.EndOfStream)
            {
                string str;
                str = f.ReadLine();

                IsReadStr = true;
                return str;
            }
            return null;
        }
    }
}
