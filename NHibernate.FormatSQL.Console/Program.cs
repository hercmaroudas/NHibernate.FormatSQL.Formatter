using NHibernate.FormatSQL.Formatter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;

namespace NHibernate.FormatSQL.Console
{
    public class Program
    {
        const string inputFileName = "input.txt";
        const string outputFileName = "output.txt";
        const string errorFileName = "error.txt";

        static bool monitorStarted = false;

        static FileInfo inputFile = null;
        static FileInfo outputFile = null;
        static FileInfo errorFile = null;

        static SqlStatementFactory uniqueNameFactory = new SqlStatementFactory();

        static FileSystemWatcher fileMonitor;
        static ConsoleOption consoleOptions = new ConsoleOption();

        // ( you dont need to worry about any of this ( its a bit procedural down there. 'Procede(ural)' at own risk! :) )
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void Main(string[] args)
        {
            initializeConsole();
            showHeader();
            writeMessage(getMonitorStartText());
            tryGetSettings();
            setupFiles(consoleOptions.WorkingDirectory, false);

            string input = string.Empty;
            while (canContinue(input))
            {
                StringBuilder messageBuilder = new StringBuilder();
                showHelp(input);
                input = System.Console.ReadLine();

                bool valid = true;
                string message = string.Empty;
                string command = tryGetSettings(input, out valid, out message);
                if (valid) setupFiles(consoleOptions.WorkingDirectory, false);
                if (valid && Directory.Exists(consoleOptions.WorkingDirectory))
                {
                    input = command;
                    trySaveSettings();

                    if (input.ToLower().Trim() == "view-settings")
                    {
                        tryShowSetting();
                    }
                    else if (input.ToLower().Trim() == "monitor-start")
                    {
                        monitorStarted = true;
                        fileMonitor.EnableRaisingEvents = true;
                        setupFiles(consoleOptions.WorkingDirectory, false);
                        writeMessage(string.Empty);
                    }
                    else if (input.ToLower().Trim() == "monitor-stop")
                    {
                        monitorStarted = false;
                        fileMonitor.EnableRaisingEvents = false;
                        writeMessage(getMonitorStartText());
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(input) && !input.Contains("mode") && !input.Contains("work-dir") && !input.Contains("help") && !input.Contains("open") && !input.Contains("error") && !input.Contains("dir"))
                        {
                            writeMessage(string.Format("Incorrect sequence used. <{0}> is not a recognized command.", input));
                            System.Console.WriteLine();
                        }
                        else
                        {
                            System.Console.Clear();
                            showHeader();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                System.Console.WriteLine();
                                System.Console.WriteLine(message);
                            }
                            System.Console.WriteLine();
                            System.Console.WriteLine(getMonitorStartText());
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        messageBuilder.AppendLine(message);
                        messageBuilder.AppendLine();
                    }
                    messageBuilder.AppendLine("Please add a valid working directory <work-dir> and mode <mode> to continue.");
                    writeMessage(messageBuilder.ToString());
                }
            }
        }
        static void initializeConsole()
        {
            System.Console.Title = "NHibernate Sql Output Formatter";
            System.Console.WindowHeight = 28;
            System.Console.WindowWidth = 80;
        }
        static bool canContinue(string input)
        {
            return (input.ToLower().Trim() != "e" && input.ToLower().Trim() != "exit");
        }
        static string getLogErrorMessage(string errorMessage, string actualSql, string parameterSection)
        {
            string logErrorMessage = string.Format("Error Message: {0}\nActual Sql:{1}\nParameter Section: {2}\n",
                errorMessage,
                actualSql,
                parameterSection);

            return logErrorMessage;
        }
        static string getMonitorStartText()
        {
            StringBuilder message = new StringBuilder();
            if (monitorStarted)
            {
                message.AppendLine("<Monitoring Active>");
                message.AppendLine();
                message.AppendLine(string.Format("Monitoring input file using mode <{0}>...\n\nInput file <{1}>", consoleOptions.Mode, Path.Combine(consoleOptions.WorkingDirectory, inputFileName)));
                message.AppendLine();
                message.AppendLine("1. Copy the NHibernate unformatted SQL to the input file and click save.");
                message.AppendLine("2. Get the latest output file with the formatted SQL.");
                message.AppendLine();
                message.AppendLine(string.Format("The output file can be found in your working directory...\n\nOutput file <{0}>", Path.Combine(consoleOptions.WorkingDirectory, "output")));
                message.AppendLine();
                message.AppendLine();
                message.AppendLine("Monitoring for sql input ...");
            }
            else
            {
                message.AppendLine("<Monitoring Inactive>");
            }
            return message.ToString();
        }
        static void logErrorMessage(string message)
        {
            using (StreamWriter streamWriter = errorFile.AppendText())
            {
                streamWriter.WriteLine(message);
            }
        }
        static void logErrorMessage(ParsingException exception, SqlSelectStatement sqlStatement)
        {
            if (exception is ParameterParsingException)
            {
                ParameterParsingException paramException = sqlStatement.SqlStatementParsingException as ParameterParsingException;
                if (exception != null)
                {
                    using (StreamWriter streamWriter = errorFile.AppendText())
                    {
                        streamWriter.WriteLine(getLogErrorMessage(paramException.Message, paramException.ActualSql, paramException.ParameterSection));
                    }
                }
            }
            else
            {
                using (StreamWriter streamWriter = errorFile.AppendText())
                {
                    streamWriter.WriteLine(getLogErrorMessage(sqlStatement.SqlStatementParsingException.Message, exception.ActualSql, string.Empty));
                }
            }
        }
        static void showHeader()
        {
            System.Console.WriteLine("-------------------------------------------");
            System.Console.WriteLine("Welcome to NHibernate Sql Output Formatter");
            System.Console.WriteLine();
            System.Console.WriteLine("Enter --help for help");
            System.Console.WriteLine("-------------------------------------------");
        }
        static void showHelp(string input)
        {
            if (input.ToLower().Contains("help"))
            {
                System.Console.Clear();
                showHeader();
                StringBuilder helpbuilder = new StringBuilder();
                helpbuilder.AppendLine("");
                helpbuilder.AppendLine("Enter --work-dir  E.G. C:\\users\\<your_name>\\documents\\nhibernate");
                helpbuilder.AppendLine("Enter --mode ( 1 = FSI < v13 ), ( 2 = FSI >= v13 ), ( 3 = LINQPad )");
                helpbuilder.AppendLine("Enter exit or e to exit application");
                helpbuilder.AppendLine("");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("<Example> : start minitor and set settings and save settings");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("monitor-start --work-dir <working directory> --mode <1,2,3>");
                helpbuilder.AppendLine("");
                //helpbuilder.AppendLine("");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("<Example> : set and save settings");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("--work-dir <working directory> --mode <1,2,3>");
                helpbuilder.AppendLine("");
                //helpbuilder.AppendLine("");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("<Example> : start monitor");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("monitor-start");
                helpbuilder.AppendLine("");
                //helpbuilder.AppendLine("");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("<Example> : stop monitor");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("monitor-stop");
                helpbuilder.AppendLine("");
                //helpbuilder.AppendLine("");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("<Example> : view settings");
                helpbuilder.AppendLine("---------");
                helpbuilder.AppendLine("view-settings");
                System.Console.WriteLine(helpbuilder.ToString());
            }
        }
        static void setupFiles(string monitorPath, bool createNewOutputFile, bool deleteErrorFile = false)
        {
            string fullPathandFile = string.Empty;
            if (!string.IsNullOrWhiteSpace(monitorPath))
            {
                string outputMonitorPath = Path.Combine(monitorPath, "output");

                if (!Directory.Exists(monitorPath))
                {
                    Directory.CreateDirectory(Path.GetFullPath(monitorPath));
                }
                if (!Directory.Exists(outputMonitorPath))
                {
                    Directory.CreateDirectory(Path.GetFullPath(outputMonitorPath));
                }

                // input file
                fullPathandFile = Path.Combine(monitorPath, inputFileName);
                if (!File.Exists(fullPathandFile))
                {
                    using (File.Create(fullPathandFile)) { }
                }
                inputFile = new FileInfo(fullPathandFile);

                // output file 
                var latestFile = Directory.GetFiles(outputMonitorPath, "output*.txt")
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.LastWriteTime)
                    .FirstOrDefault();

                string newFileName = string.Empty;
                if (latestFile == null)
                {
                    newFileName = string.Format("{0}\\{1}_1.txt", outputMonitorPath, Path.GetFileNameWithoutExtension(outputFileName));
                }
                else
                {
                    string[] fileNumberArray = latestFile.FullName.Split('_', '.');
                    int fileNumber = Convert.ToInt32(fileNumberArray[fileNumberArray.Length - 2]);
                    if (createNewOutputFile) fileNumber += 1;
                    newFileName = string.Format("{0}\\{1}_{2}.txt", outputMonitorPath, Path.GetFileNameWithoutExtension(outputFileName), fileNumber);
                }
                fullPathandFile = Path.Combine(outputMonitorPath, newFileName);
                if (createNewOutputFile) using (File.Create(newFileName)) { }
                outputFile = new FileInfo(fullPathandFile);

                // error file 
                fullPathandFile = Path.Combine(monitorPath, errorFileName);
                //if (!File.Exists(fullPathandFile))
                {
                    if (deleteErrorFile) using (File.Create(fullPathandFile)) { }
                }
                errorFile = new FileInfo(fullPathandFile);

                if (fileMonitor == null)
                {
                    fileMonitor = new FileSystemWatcher(monitorPath, Path.GetFileName(inputFileName));
                    fileMonitor.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes;
                    fileMonitor.Changed += new FileSystemEventHandler(fileMonitor_Changed); ;
                    fileMonitor.Changed += new FileSystemEventHandler(fileMonitor_Changed);
                    fileMonitor.Created += new FileSystemEventHandler(fileMonitor_Changed);
                    fileMonitor.Deleted += new FileSystemEventHandler(fileMonitor_Changed);
                }
            }
        }
        static void setupMonitoring(string monitorPath)
        {
            if (!string.IsNullOrWhiteSpace(monitorPath) && Directory.Exists(monitorPath))
            {
                if (fileMonitor == null)
                {
                    fileMonitor = new FileSystemWatcher(monitorPath, Path.GetFileName(inputFileName));
                    fileMonitor.NotifyFilter = NotifyFilters.LastWrite; // NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes;
                    fileMonitor.Changed += new FileSystemEventHandler(fileMonitor_Changed);
                }
            }
        }
        static void tryGetSettings()
        {
            consoleOptions.WorkingDirectory = ConsoleApp.Default.WorkPath;
            setupMonitoring(consoleOptions.WorkingDirectory);
            consoleOptions.Mode = ConsoleApp.Default.Mode;
        }
        static string tryGetSettings(string input, out bool valid, out string message)
        {
            valid = true;
            message = string.Empty;
            StringBuilder builder = new StringBuilder();
            List<string> optionList = new List<string>();
            string[] options = input.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < options.Length; ix++)
            {
                if (!valid) break;
                string current = options[ix];
                if (current.ToLower().Trim() != "monitor-start")
                {
                    string[] value = current.Split(' ');
                    if (value.Length > 1)
                    {
                        switch (value[0])
                        {
                            case "work-dir":
                                if (string.IsNullOrWhiteSpace(value[1]))
                                {
                                    valid = false;
                                    builder.AppendLine("work-dir must contain a value. E.g. --work-dir c:\\mydir");
                                }
                                else
                                {
                                    consoleOptions.WorkingDirectory = value[1];
                                    builder.AppendLine(string.Format("work-dir successfully set to <{0}>", value[1]));
                                }
                                break;
                            case "mode":
                                // ( valid mode ) 
                                if (value.Length > 1 && value[1].Length == 1 && ("1 2 3".ToList().Contains(Convert.ToChar(value[1]))))
                                {
                                    consoleOptions.Mode = value[1];
                                    builder.AppendLine(string.Format("Mode successfully set to <{0}>", value[1]));
                                }
                                else
                                {
                                    valid = false;
                                    if (string.IsNullOrWhiteSpace(value[1]))
                                    {
                                        builder.AppendLine("Mode must contain a value. E.g. --mode 1");
                                    }
                                    else
                                    {
                                        builder.AppendLine(string.Format("<{0}> is not a valid mode", value[1]));
                                    }
                                }
                                break;
                            case "open":
                                string[] parameters = value[1].Split(' ');
                                if (!(parameters.Length > 0 && (parameters.Contains("input") || parameters.Contains("output") || parameters.Contains("error") || parameters.Contains("dir"))))
                                {
                                    valid = false;
                                    if (string.IsNullOrWhiteSpace(value[1]))
                                    {
                                        builder.AppendLine("Open must contain a value. E.g. --open [input], [output], [error], [dir]");
                                    }
                                    else
                                    {
                                        builder.AppendLine(string.Format("<{0}> is not a valid Open parameter", value[1]));
                                    }
                                }
                                else
                                {
                                    // ( open the file for user )
                                    switch (parameters[parameters.Length - 1])
                                    {
                                        case "output":
                                            Process.Start(outputFile.FullName);
                                            break;
                                        case "input":
                                            Process.Start(inputFile.FullName);
                                            break;
                                        case "error":
                                            Process.Start(errorFile.FullName);
                                            break;
                                        case "dir":
                                            Process.Start(consoleOptions.WorkingDirectory);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;
                            default:
                                builder.AppendLine(string.Format("Unrecognised sequence used. <{0}> is not a recognized command.", input));
                                valid = false;
                                break;
                        }
                    }
                    else
                    {
                        if (!value[0].Contains("help") && !value[0].Contains("view-settings") && !value[0].Contains("monitor-start") && !value[0].Contains("monitor-stop"))
                        {
                            valid = false;
                            builder.AppendLine("Commands must contain values. E.g. --command value ( --mode 1 )");
                        }
                    }
                }
            }
            message = builder.ToString();
            return valid ? (options.Length == 0 ? string.Empty : options[0]) : input;
        }
        static void trySaveSettings()
        {
            ConsoleApp.Default.WorkPath = consoleOptions.WorkingDirectory;
            setupMonitoring(consoleOptions.WorkingDirectory);

            ConsoleApp.Default.Mode = consoleOptions.Mode;
            ConsoleApp.Default.Save();
        }
        static void tryShowSetting()
        {
            tryGetSettings();
            System.Console.Clear();
            showHeader();
            System.Console.WriteLine();
            if (string.IsNullOrWhiteSpace(consoleOptions.WorkingDirectory))
            {
                System.Console.WriteLine("work-dir = {0}", "<no setting>");
            }
            else
            {
                System.Console.WriteLine("work-dir = {0}", consoleOptions.WorkingDirectory);
            }

            if (string.IsNullOrWhiteSpace(consoleOptions.Mode))
            {
                System.Console.WriteLine("mode {0} = {1}", "".PadRight(3), "<no setting>");
            }
            else
            {
                string description = string.Empty;
                switch (consoleOptions.Mode)
                {
                    case "1":
                        description = "( 1 = FSI < v13 )";
                        break;
                    case "2":
                        description = "( 2 = FSI >= v13 )";
                        break;
                    case "3":
                        description = "( 3 = LINQPad )";
                        break;
                }

                System.Console.WriteLine("mode {0} = {1} {2}", "".PadRight(3), consoleOptions.Mode, description);

            }
            System.Console.WriteLine();
            System.Console.WriteLine(getMonitorStartText());
        }
        static void writeMessage(string input)
        {
            System.Console.Clear();
            showHeader();
            if (!string.IsNullOrWhiteSpace(input))
            {
                System.Console.WriteLine("");
                System.Console.WriteLine(input);
                //System.Console.WriteLine("");
            }
            if (monitorStarted)
            {
                System.Console.WriteLine("");
                System.Console.Write(getMonitorStartText());
                System.Console.WriteLine("");
            }
        }
        static void fileMonitor_Changed(object sender, FileSystemEventArgs e)
        {
            fileMonitor.EnableRaisingEvents = false;
            StringBuilder taskMessage = new StringBuilder();
            try
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Formatting started. Please wait until the task completes ...\n");

                taskMessage.AppendLine("Formatting completed...\n");
                taskMessage.AppendLine();
                taskMessage.AppendLine(string.Format("Attempted to write the fomratted SQL to:\n{0}", Path.Combine(consoleOptions.WorkingDirectory, outputFileName)));

                // create a new output file for each monitored session
                setupFiles(consoleOptions.WorkingDirectory, true, true);

                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    NHibernateSqlOutputFormatter nhibernateFormatter;
                    taskMessage.AppendLine();
                    taskMessage.AppendLine(string.Format("Mode set to <{0}>", consoleOptions.Mode));
                    switch (consoleOptions.Mode)
                    {
                        case "1":
                            nhibernateFormatter = new NHibernateSqlOutputFormatter();
                            nhibernateFormatter.SqlIdentifiers = new string[] { "NHibernate.SQL:", Environment.NewLine };
                            break;
                        case "2":
                            nhibernateFormatter = new NHibernateSqlOutputFormatter();
                            nhibernateFormatter.SqlIdentifiers = new string[] { "NHibernate.SQL:", "\'w3wp.exe\'" };
                            break;
                        case "3":
                            nhibernateFormatter = new NHibernateSqlOutputFormatter();
                            nhibernateFormatter.SqlIdentifiers = new string[] { "-- EndRegion" };
                            break;
                        default:
                            nhibernateFormatter = new NHibernateSqlOutputFormatter();
                            break;
                    }

                    bool containErrors = false;
                    IList<ISqlStatement> sqlStatements = nhibernateFormatter.GetSqlFromDebugOutput(File.ReadAllText(Path.Combine(consoleOptions.WorkingDirectory, inputFileName)));
                    foreach (var sqlStatement in sqlStatements)
                    {
                        if (sqlStatement is SqlSelectStatement)
                        {
                            string appliedFormattedSql = sqlStatement.ApplySuggestedFormat();
                            using (StreamWriter streamWriter = outputFile.AppendText())
                                streamWriter.WriteLine(appliedFormattedSql);

                            // Log any handled exceptions (these are exceptions that dont break the iteration of creating SQL statements).
                            if (sqlStatement.SqlStatementParsingException != null)
                            {
                                containErrors = true;
                                logErrorMessage(sqlStatement.SqlStatementParsingException, (SqlSelectStatement)sqlStatement);
                            }
                        }
                    }

                    if (containErrors)
                    {
                        taskMessage.AppendLine();
                        taskMessage.AppendLine(string.Format("Errors detected and logged to:\n{0}", Path.Combine(consoleOptions.WorkingDirectory, errorFileName)));
                    }

                    taskMessage.AppendLine();
                    taskMessage.AppendLine(string.Format("File monitoring complete and logged to:{0}", outputFile.FullName));
                }
            }
            catch (ParameterParsingException exception)
            {
                logErrorMessage(getLogErrorMessage(exception.Message, exception.ActualSql, exception.ParameterSection));
                taskMessage.AppendLine();
                taskMessage.AppendLine(string.Format("Critical errors that stopped the application detected and logged to:\n{0}", Path.Combine(consoleOptions.WorkingDirectory, errorFileName)));
            }
            catch (Exception exception)
            {
                logErrorMessage(getLogErrorMessage(exception.Message, string.Empty, string.Empty));
                taskMessage.AppendLine();
                taskMessage.AppendLine(string.Format("Critical errors that stopped the application detected and logged to:\n{0}", Path.Combine(consoleOptions.WorkingDirectory, errorFileName)));
                taskMessage.AppendLine();
                taskMessage.AppendLine("Errors may occur if you are attempting to parse a file in the incorrect mode.");
            }
            finally
            {
                // hack for file system watcher firing unecessary events
                fileMonitor.EnableRaisingEvents = true;
                writeMessage(taskMessage.ToString());
            }
        }
        internal class ConsoleOption
        {
            public string Mode { get; set; }
            public string WorkingDirectory { get; set; }
        }
    }
}
