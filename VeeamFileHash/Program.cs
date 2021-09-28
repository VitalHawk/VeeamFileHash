using System.IO;

namespace VeeamFileHash
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!GetArgs(args, out string fileName, out int blockSize))
            {
                UsageInfo();
                return;
            }

            if (!File.Exists(fileName))
            {
                Output.WriteLine("File {0} not found", fileName);
                UsageInfo();
                return;
            }

            if (new FileInfo(fileName).Length < blockSize)
            {
                Output.WriteLine("File {0} is too small", fileName);
                UsageInfo();
                return;
            }

            HashIt(fileName, blockSize);
        }

        private static void UsageInfo()
        {
            Output.WriteLine("Usage: VeeamFileHash <file path> <block size>");
        }

        private static bool GetArgs(string[] args, out string fileName, out int blockSize)
        {
            blockSize = 0;
            fileName = null;

            if (args.Length < 2 || !int.TryParse(args[1], out blockSize) || blockSize <= 0)
                return false;

            fileName = args[0];

            return true;
        }

        private static void HashIt(string fileName, int blockSize)
        {
            var wasError = false;
            new HashAnalizer(fileName, blockSize).HashIt(
                (partNum, hash) =>
                {
                    if (!wasError)
                        Output.WriteLine("{0}: {1}", partNum, Output.ByteArrayToString(hash));
                },
                ex =>
                {
                    wasError = true;
                    Output.WriteLine(Output.TraceException(ex));
                });
        }
    }
}
