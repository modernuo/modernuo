/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Main.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Json;
using Server.Network;

namespace Server
{
    public static class Core
    {
        private static bool _crashed;
        private static Thread _timerThread;
        private static string _baseDirectory;

        private static bool _profiling;
        private static DateTime _profileStart;
        private static TimeSpan _profileTime;
#nullable enable
        private static bool? _isRunningFromXUnit;
#nullable disable

        private static int _itemCount;
        private static int _mobileCount;
        private static EventLoopContext _eventLoopContext;

        private static readonly Type[] _serialTypeArray = { typeof(Serial) };

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
        public static readonly bool Unix = IsDarwin || IsFreeBSD || IsLinux;

        private const string AssembliesConfiguration = "Data/assemblies.json";

#nullable enable
        // TODO: Find a way to get rid of this
        public static bool IsRunningFromXUnit
        {
            get
            {
                if (_isRunningFromXUnit != null)
                {
                    return _isRunningFromXUnit.Value;
                }

                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.FullName.InsensitiveStartsWith("xunit"))
                    {
                        _isRunningFromXUnit = true;
                        return true;
                    }
                }

                _isRunningFromXUnit = false;
                return false;
            }
        }
#nullable disable

        public static bool Profiling
        {
            get => _profiling;
            set
            {
                if (_profiling == value)
                {
                    return;
                }

                _profiling = value;

                if (_profileStart > DateTime.MinValue)
                {
                    _profileTime += DateTime.UtcNow - _profileStart;
                }

                _profileStart = _profiling ? DateTime.UtcNow : DateTime.MinValue;
            }
        }

        public static TimeSpan ProfileTime =>
            _profileStart > DateTime.MinValue
                ? _profileTime + (DateTime.UtcNow - _profileStart)
                : _profileTime;

        public static Assembly Assembly { get; set; }

        // Assembly file version
        public static Version Version => new(ThisAssembly.AssemblyFileVersion);

        public static Process Process { get; private set; }

        public static Thread Thread { get; private set; }

        // Milliseconds
        public static long TickCount => Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;

        public static bool MultiProcessor { get; private set; }

        public static int ProcessorCount { get; private set; }

        public static string BaseDirectory
        {
            get
            {
                if (_baseDirectory == null)
                {
                    try
                    {
                        _baseDirectory = Assembly.Location;

                        if (_baseDirectory.Length > 0)
                        {
                            _baseDirectory = Path.GetDirectoryName(_baseDirectory);
                        }
                    }
                    catch
                    {
                        _baseDirectory = "";
                    }
                }

                return _baseDirectory;
            }
        }

        public static CancellationTokenSource ClosingTokenSource { get; } = new();

        public static bool Closing => ClosingTokenSource.IsCancellationRequested;

        public static string Arguments
        {
            get
            {
                var sb = new StringBuilder();

                if (_profiling)
                {
                    Utility.Separate(sb, "-profile", " ");
                }

                return sb.ToString();
            }
        }

        public static int GlobalUpdateRange { get; set; } = 18;

        public static int GlobalMaxUpdateRange { get; set; } = 24;

        public static int ScriptItems => _itemCount;
        public static int ScriptMobiles => _mobileCount;

        public static Expansion Expansion { get; set; }

        public static bool T2A => Expansion >= Expansion.T2A;

        public static bool UOR => Expansion >= Expansion.UOR;

        public static bool UOTD => Expansion >= Expansion.UOTD;

        public static bool LBR => Expansion >= Expansion.LBR;

        public static bool AOS => Expansion >= Expansion.AOS;

        public static bool SE => Expansion >= Expansion.SE;

        public static bool ML => Expansion >= Expansion.ML;

        public static bool SA => Expansion >= Expansion.SA;

        public static bool HS => Expansion >= Expansion.HS;

        public static bool TOL => Expansion >= Expansion.TOL;

        public static bool EJ => Expansion >= Expansion.EJ;

        public static string FindDataFile(string path, bool throwNotFound = true, bool warnNotFound = false)
        {
            string fullPath = null;

            foreach (var p in ServerConfiguration.DataDirectories)
            {
                fullPath = Path.Combine(p, path);

                if (File.Exists(fullPath))
                {
                    break;
                }

                fullPath = null;
            }

            if (fullPath == null && (throwNotFound || warnNotFound))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Data: {path} was not found");
                Console.WriteLine("Make sure modernuo.json is properly configured");
                Utility.PopColor();
                if (throwNotFound)
                {
                    throw new FileNotFoundException($"Data: {path} was not found");
                }
            }

            return fullPath;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
            Console.WriteLine(e.ExceptionObject);

            if (e.IsTerminating)
            {
                _crashed = true;

                var close = false;

                try
                {
                    var args = new ServerCrashedEventArgs(e.ExceptionObject as Exception);

                    EventSink.InvokeServerCrashed(args);

                    close = args.Close;
                }
                catch
                {
                    // ignored
                }

                if (!close)
                {
                    Console.WriteLine("This exception is fatal, press return to exit");
                    Console.ReadLine();
                }

                Kill();
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (!Closing)
            {
                HandleClosed();
            }
        }

        private static void Console_CancelKeyPressed(object sender, ConsoleCancelEventArgs e)
        {
            var keypress = e.SpecialKey switch
            {
                ConsoleSpecialKey.ControlBreak => "CTRL+BREAK",
                _ => "CTRL+C"
            };

            Console.WriteLine("Core: Detected {0} pressed.", keypress);
            e.Cancel = true;
            Kill();
        }

        public static void Kill(bool restart = false)
        {
            if (Closing)
            {
                return;
            }

            HandleClosed();

            if (restart)
            {
                if (IsWindows)
                {
                    Process.Start("dotnet", Assembly.Location);
                }
                else
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = Assembly.Location,
                            UseShellExecute = true
                        }
                    };

                    process.Start();
                }
            }

            Process.Kill();
        }

        private static void HandleClosed()
        {
            ClosingTokenSource.Cancel();

            Console.Write("Core: Shutting down...");

            World.WaitForWriteCompletion();

            if (!_crashed)
            {
                EventSink.InvokeShutdown();
            }

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _eventLoopContext = new EventLoopContext();

            SynchronizationContext.SetSynchronizationContext(_eventLoopContext);

            foreach (var a in args)
            {
                if (a.InsensitiveEquals("-profile"))
                {
                    Profiling = true;
                }
            }

            Thread = Thread.CurrentThread;
            Process = Process.GetCurrentProcess();
            Assembly = Assembly.GetEntryAssembly();

            if (Assembly == null)
            {
                throw new Exception("Core: Assembly entry is missing.");
            }

            if (Thread != null)
            {
                Thread.Name = "Core Thread";
            }

            if (BaseDirectory.Length > 0)
            {
                Directory.SetCurrentDirectory(BaseDirectory);
            }

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine(
                "ModernUO - [https://github.com/modernuo/modernuo] Version {0}.{1}.{2}.{3}",
                Version.Major,
                Version.Minor,
                Version.Build,
                Version.Revision
            );
            Utility.PopColor();

            Utility.PushColor(ConsoleColor.DarkGray);
            Console.WriteLine(@"Copyright 2019-2020 ModernUO Development Team
                This program comes with ABSOLUTELY NO WARRANTY;
                This is free software, and you are welcome to redistribute it under certain conditions.

                You should have received a copy of the GNU General Public License
                along with this program. If not, see <https://www.gnu.org/licenses/>.
            ".TrimMultiline());
            Utility.PopColor();

            Console.WriteLine("Core: Running on {0}", RuntimeInformation.FrameworkDescription);

            var ttObj = new Timer.TimerThread();
            _timerThread = new Thread(ttObj.TimerMain)
            {
                Name = "Timer Thread"
            };

            var s = Arguments;

            if (s.Length > 0)
            {
                Console.WriteLine("Core: Running with arguments: {0}", s);
            }

            ProcessorCount = Environment.ProcessorCount;

            if (ProcessorCount > 1)
            {
                MultiProcessor = true;
            }

            if (MultiProcessor)
            {
                Console.WriteLine("Core: Optimizing for {0} processor{1}", ProcessorCount, ProcessorCount == 1 ? "" : "s");
            }

            Console.CancelKeyPress += Console_CancelKeyPressed;

            if (GCSettings.IsServerGC)
            {
                Console.WriteLine("Core: Server garbage collection mode enabled");
            }

            Console.WriteLine(
                "Core: High resolution timing ({0})",
                Stopwatch.IsHighResolution ? "Supported" : "Unsupported"
            );

            ServerConfiguration.Load();

            var assemblyPath = Path.Join(BaseDirectory, AssembliesConfiguration);

            // Load UOContent.dll
            var assemblyFiles = JsonConfig.Deserialize<List<string>>(assemblyPath).ToArray();
            for (var i = 0; i < assemblyFiles.Length; i++)
            {
                assemblyFiles[i] = Path.Join(BaseDirectory, "Assemblies", assemblyFiles[i]);
            }

            AssemblyHandler.LoadScripts(assemblyFiles);

            VerifySerialization();

            MapLoader.LoadMaps();
            AssemblyHandler.Invoke("Configure");

            TileMatrixLoader.LoadTileMatrix();
            RegionLoader.LoadRegions();
            World.Load();

            AssemblyHandler.Invoke("Initialize");

            _timerThread.Start();

            TcpServer.Start();
            EventSink.InvokeServerStarted();
            RunEventLoop();
        }

        public static void RunEventLoop()
        {
            try
            {
                long last = TickCount;

                const int interval = 100;
                int idleCount = 0;

                while (!Closing)
                {
                    var events = Mobile.ProcessDeltaQueue();
                    events += Item.ProcessDeltaQueue();

                    events += Timer.Slice();

                    // Handle networking
                    events += TcpServer.Slice();
                    events += NetState.HandleAllReceives();
                    events += NetState.FlushAll();

                    // Execute captured post-await methods (like timers)
                    events += _eventLoopContext.ExecuteTasks();

                    if (events > 0)
                    {
                        idleCount = 0;
                        continue;
                    }

                    if (++idleCount > interval)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception e)
            {
                CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
            }
        }

        public static void VerifySerialization()
        {
            _itemCount = 0;
            _mobileCount = 0;

            var callingAssembly = Assembly.GetCallingAssembly();

            VerifySerialization(callingAssembly);

            foreach (var assembly in AssemblyHandler.Assemblies)
            {
                if (assembly != callingAssembly)
                {
                    VerifySerialization(assembly);
                }
            }
        }

        private static void VerifyType(Type type)
        {
            var isItem = type.IsSubclassOf(typeof(Item));

            if (!isItem && !type.IsSubclassOf(typeof(Mobile)))
            {
                return;
            }

            if (isItem)
            {
                Interlocked.Increment(ref _itemCount);
            }
            else
            {
                Interlocked.Increment(ref _mobileCount);
            }

            StringBuilder warningSb = null;

            try
            {
                if (type.GetConstructor(_serialTypeArray) == null)
                {
                    warningSb = new StringBuilder();
                    warningSb.AppendLine("       - No serialization constructor");
                }

                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.DeclaredOnly;
                if (type.GetMethod("Serialize", bindingFlags) == null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Serialize() method");
                }

                if (type.GetMethod("Deserialize", bindingFlags) == null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Deserialize() method");
                }

                if (warningSb?.Length > 0)
                {
                    Console.WriteLine("Warning: {0}\n{1}", type, warningSb);
                }
            }
            catch
            {
                Console.WriteLine("Warning: Exception in serialization verification of type {0}", type);
            }
        }

        private static void VerifySerialization(Assembly assembly)
        {
            if (assembly != null)
            {
                Parallel.ForEach(assembly.GetTypes(), VerifyType);
            }
        }
    }
}
