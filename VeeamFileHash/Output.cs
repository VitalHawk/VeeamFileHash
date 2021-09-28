using System;
using System.Text;

namespace VeeamFileHash
{
    public static class Output
    {
        public static void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);

        public static string ByteArrayToString(byte[] array)
        {
            var s = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                s.Append($"{array[i]:X2}");
                if (i % 4 == 3) s.Append(" ");
            }
            return s.ToString();
        }

        public static string TraceException(Exception ex) => $"{ex}";
    }
}