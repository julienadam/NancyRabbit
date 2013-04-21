using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Nancy.Hosting.Self;

namespace rabbit.shell
{
    public class Program
    {
        private static string _sourceWatchFolder;
        private static string _libraryLocation;
        private static Uri _endpoint;

        static void Main(string[] args)
        {
            // Parse args
            Console.WriteLine("Parsing args");
            _libraryLocation = args[0];
            _sourceWatchFolder = args[1];
            _endpoint = new Uri(args[2]);
            
            bool requiresDebugger = (args.Length >= 4) && (args[3] == "/debug");

            if (requiresDebugger)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    Debugger.Launch();
                }
            }

            // Compile and load assemblies
            Console.WriteLine("Compiling and loading assemblies");
            SourceBuild();

            // Run host
            RunHost(_endpoint);
            Console.Read();
        }


        public static void Compile(string[] sources)
        {
            var compilerParams = new CompilerParameters
            {
                CompilerOptions = "/target:library /optimize",
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = true
            };
            compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");

            compilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

            AddAllAssemblies(compilerParams, _libraryLocation);
            AddAllAssemblies(compilerParams, _sourceWatchFolder);

            CodeDomProvider codeProvider = new CSharpCodeProvider();
            CompilerResults results = codeProvider.CompileAssemblyFromFile(compilerParams, sources);

            if (results.Errors.Count > 0)
            {
                Console.WriteLine("Error found while recompiling your files:");

                foreach (string line in results.Output)
                {
                    Console.WriteLine(line);
                }
            }
        }

        private static void AddAllAssemblies(CompilerParameters compilerParams, string folder)
        {
            foreach (var library in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
            {
                var libFileName = Path.GetFileName(library);

                // Skip libraries that are already referenced
                if (compilerParams.ReferencedAssemblies
                    .Cast<string>()
                    .All(asm => string.Compare(Path.GetFileName(asm), libFileName, true) != 0))
                {
                    compilerParams.ReferencedAssemblies.Add(library);
                }
            }
        }

        private static void SourceBuild()
        {
            var sourceFiles = Directory.GetFiles(_sourceWatchFolder, "*.cs", SearchOption.AllDirectories);
            Console.WriteLine("{0} source files found", sourceFiles.Length);
            Console.WriteLine("Start compiling");
            Compile(sourceFiles);
        }


        public static NancyHost RunHost(Uri endpoint)
        {
            var host = new NancyHost(endpoint);
            host.Start();

            Console.WriteLine("Running Nancy host on {0}", endpoint.AbsoluteUri);
            return host;
        }
    }
}
