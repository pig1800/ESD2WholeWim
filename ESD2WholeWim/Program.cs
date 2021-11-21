using System;

namespace ESD2WholeWim
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Syntax Error.");
                Console.WriteLine("ESD2WholeWim.exe srcpath dstpath fileListPath");
            }

            var proc = new ESDProc(args[0], args[1], args[2]);
            proc.Start();
        }
    }
}
