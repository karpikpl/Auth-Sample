using Colorful;
using System;
using System.Diagnostics;
using System.Drawing;
using Console = Colorful.Console;

namespace consoleClient;

    internal static class Info
    {
        public static void DisplayInfo(string serverAddress, string targetingFramework)
        {
            string info = @"
Targeting:                {0}
.NET Framework Version:   {1}
.NET Framework Version:   {2}
Operating System Version: {3}
Server Address:           {4}
";
            // Get the .NET Framework version
            var fileVersion = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
            string version = fileVersion.ProductVersion;


            Formatter[] infoValues = new Formatter[]
            {
                new Formatter(targetingFramework, Color.Red),
                new Formatter(Environment.Version, Color.Red),
                new Formatter(version.Substring(0, version.LastIndexOf('.')), Color.Red),
                new Formatter(Environment.OSVersion, Color.Red),
                new Formatter(serverAddress, Color.Red)
            };

            Console.WriteLineFormatted(info, Color.Gray, infoValues);
        }
    }
