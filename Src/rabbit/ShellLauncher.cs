using System;
using System.Diagnostics;
using System.IO;

namespace rabbit
{
    [Serializable]
    public class ShellLauncher : IDisposable
    {
        private readonly Uri _endpoint;
        private readonly string _sourceWatchFolder;
        private readonly string _libraryLocation;
        private FileSystemWatcher _watcher;
        private Process _currentShell;
        private bool _isDebugMode;

        public ShellLauncher(string endpoint, string sourceWatchFolder, string libraryLocation)
        {
            _endpoint = new Uri(endpoint);
            _sourceWatchFolder = sourceWatchFolder;
            _libraryLocation = libraryLocation;
        }

        public void AutoRun()
        {
            Start();
        }

        public void Start()
        {
            if (!Directory.Exists(_sourceWatchFolder))
            {
                throw new DirectoryNotFoundException(_sourceWatchFolder);
            }

            Console.WriteLine("Starting to watch folder for changes : ");
            Console.WriteLine(_sourceWatchFolder);
                
            _watcher = CreateFolderWatcher(_sourceWatchFolder);

            _watcher.EnableRaisingEvents = true;
            OnChange(null);
        }

        private void OnChange(FileSystemEventArgs args)
        {
            if (args != null)
            {
                Console.WriteLine("File Change Found:");
                Console.WriteLine("   [{0}] {1}", args.ChangeType, args.Name);
            }

            if (_currentShell != null)
            {
                Console.WriteLine("Killing previous shell {0}", _currentShell.Id);
                _currentShell.Kill();
                _currentShell = null;
            }

            var psi = new ProcessStartInfo
                {
                    FileName = "rabbit.shell.exe",
                    Arguments = string.Format("{0} {1} {2} ", _libraryLocation, _sourceWatchFolder, _endpoint),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = _sourceWatchFolder, // Start in the watched folder so that relative paths work
                };

            if (_isDebugMode)
            {
                psi.Arguments += " /debug";
            }

            var p = new Process {StartInfo = psi, EnableRaisingEvents = true};
            p.ErrorDataReceived += 
                (sender, eventArgs) =>
                    Console.WriteLine("### " + eventArgs.Data);
            p.Exited +=
                (sender, eventArgs) =>
                    Console.WriteLine("Shell exited.");
            p.OutputDataReceived +=
                (sender, eventArgs) =>
                    Console.WriteLine("*** " + eventArgs.Data);

            _currentShell = p;
            _currentShell.Start();
            Console.WriteLine("Started new shell {0}", _currentShell.Id);

            _currentShell.BeginOutputReadLine();
        }

        private FileSystemWatcher CreateFolderWatcher(string sourceWatchFolder)
        {
            var watcher = new FileSystemWatcher(sourceWatchFolder, "*.cs")
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };
            watcher.Changed += (sender, args) => OnChange(args);
            watcher.Created += (sender, args) => OnChange(args);
            watcher.Deleted += (sender, args) => OnChange(args);
            return watcher;
        }

        public void RestartInDebugMode()
        {
            _isDebugMode = true;
            OnChange(null);
        }

        public void Dispose()
        {
            if (_currentShell != null)
            {
                _currentShell.Kill();
                _currentShell = null;
            }
        }
    }
}