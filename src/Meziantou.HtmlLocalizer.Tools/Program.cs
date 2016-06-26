using System.Globalization;
using System.IO;
using System.Linq;
using Meziantou.Framework.Utilities;

namespace Meziantou.HtmlLocalizer.Tools
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var commandLineParser = new CommandLineParser();
            commandLineParser.Parse(args);
            if (commandLineParser.HelpRequested)
            {
                PrintHelp();
                return;
            }

            var path = commandLineParser.GetArgument(0);
            var extractOnly = ConvertUtilities.ChangeType(commandLineParser.GetArgument("extractOnly"), false);
            var fileLayout = ConvertUtilities.ChangeType(commandLineParser.GetArgument("fileLayout"), FileLayout.SubDirectory);

            if (string.IsNullOrEmpty(path))
            {
                PrintHelp();
                return;
            }

            Project project = new Project();
            if (File.Exists(path))
            {
                project.Load(path);
            }

            project.OpenDirectory(new DirectoryInfo(Path.GetDirectoryName(path)));
            
            project.Save(path);

            if (!extractOnly)
            {
                var cultures = project.Files
                    .SelectMany(file => file.Fields)
                    .SelectMany(field => field.Values.Values)
                    .SelectMany(tuple => tuple.Keys)
                    .Where(cultureName => !string.IsNullOrEmpty(cultureName))
                    .Distinct()
                    .Select(cultureName => new CultureInfo(cultureName))
                    .ToList();

                foreach (var cultureInfo in cultures)
                {
                    project.Localize(cultureInfo, fileLayout);
                }
            }
        }

        private static void PrintHelp()
        {
            System.Console.WriteLine("dotnet Meziantou.HtmlLocalizer.Console <localization.json> [/extractOnly] [/fileLayout:SubDirectory]");
        }
    }
}
