﻿using Detekonai.Core.Common;
using Detekonai.Networking.NetSync.Injector.Editor;
using System;
using System.Runtime.CompilerServices;

namespace Detekonai.Networking.NetSync.Injector
{
    class Program
    {

        class ConsoleLogger : ILogConnector
        {
            public bool Verbose { get; set; } = true;

            public ConsoleLogger()
            {

            }

            public void Log(object sender, string msg, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            {
                Log(sender, ex.Message + "\n" + ex.StackTrace.ToString(), ILogConnector.LogLevel.Error, memberName, sourceFilePath, sourceLineNumber);
            }

            public void Log(object sender, string msg, ILogConnector.LogLevel level = ILogConnector.LogLevel.Verbose, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            {
                string stringLevel = null;
                if(level == ILogConnector.LogLevel.Error)
                {
                    stringLevel = "error";
                }
                else if (level == ILogConnector.LogLevel.Warning)
                {
                    stringLevel = "warning";
                }
                if (stringLevel != null)
                {
                    Console.WriteLine($"NetSyncInjector : {level} {msg}");
                }
                else if(Verbose)
                {
                    Console.WriteLine($"NetSyncInjector: {msg}");
                }
            }
        }

        static void Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();
            logger.Log(null, "Running NetSyncInjector...");
            if(args.Length >= 2)
            {
                string target = args[0];
                string includeDir = args[1];
                logger.Log(null, $"NetSyncInjector: Target: {target}");
                logger.Log(null, $"NetSyncInjector: includeDir: {includeDir}");
                if (args.Length >= 3)
                {
                    logger.Log(null, "NETSYNC001 : NetSyncInjector Verbose logging is on", ILogConnector.LogLevel.Warning);
                    logger.Verbose = string.Equals(args[2], "true", StringComparison.OrdinalIgnoreCase);
                }
                NetSyncInjector injector = new NetSyncInjector(logger);
                injector.Inject(target, includeDir);
            }
            else
            {
                string help = @"
                <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
                    < Exec Command = ""dotnet run --project ..\Detekonai.Networking.NetSync.Injector\Detekonai.Networking.NetSync.Injector.csproj -- $(TargetPath) $(OutDir)"" ConsoleToMSBuild = ""true"" >
                        < Output TaskParameter = ""ConsoleOutput"" PropertyName = ""OutputOfExec"" />
                    </ Exec >
                </ Target > ";
                logger.Log(null, $"NETSYNCERR : NetSyncInjector missing arguments! Check build log for details!", ILogConnector.LogLevel.Error);
                logger.Log(null, $"Add this to the target project .csproj file: {help}");
            }

        }
    }
}
