using System;
using System.IO;
using System.Reflection;

namespace rabbit
{
    class Program
    {
        private const string DefaultEndpoint = "http://localhost:4001";
        private const string DefaultWatchFolder = "./";

        private static void Main(string[] args)
        {
            var endpoint = DefaultEndpoint;
            var watchFolder = new DirectoryInfo(DefaultWatchFolder).FullName;
            if (args.Length >= 1)
            {
                watchFolder = new DirectoryInfo(args[0]).FullName;
            }
            if (args.Length >= 2)
            {
                endpoint = args[1];
            }

            var asm = Assembly.GetExecutingAssembly();
            var location = new FileInfo(asm.Location).Directory.FullName;
            var host = new ShellLauncher(endpoint, watchFolder, location);
            host.AutoRun();

            Console.WriteLine("Press enter to quit. Or press 'D' then enter to start a new shell in debug mode.");

            while (true)
            {
                var read = Console.ReadLine();
                if (read == "D" || read == "d")
                {
                    host.RestartInDebugMode();
                }
                else
                {
                    break;
                }
            }

            host.Dispose();
        }
    }
}
