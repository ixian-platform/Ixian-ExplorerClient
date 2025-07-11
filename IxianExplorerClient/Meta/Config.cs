using Fclp;
using IXICore;
using IXICore.Meta;
using IXICore.Network;

namespace IxianExplorerClient.Meta
{
    class Config
    {
        public static int apiPort = 8001;
        public static Dictionary<string, string> apiUsers = new Dictionary<string, string>();
        public static List<string> apiAllowedIps = new List<string>();
        public static List<string> apiBinds = new List<string>();

        public static string configFile = "ixian.cfg";
        public static string walletFile = "ixian.wal";
        public static bool onlyShowAddresses = false;

        public static int maxLogSize = 50; // MB
        public static int maxLogCount = 10;

        public static int logVerbosity = (int)LogSeverity.info + (int)LogSeverity.warn + (int)LogSeverity.error;
        public static int explorerAPITransactionPaginationLimit = 100;
        public static string externalIp = "";

        public static readonly string version = "xexc-0.9.4"; // ExplorerClient version

        public static string explorerAPIBaseUrl = "https://explorer.ixian.io/api/v1";
        public static string explorerAPIKey = ""; // Set the API KEY here or supply it via commandline or config file
        public static bool enableActivityScanner = true;


        public static int maxRelaySectorNodesToConnectTo = 3;

        public static int maxConnectedStreamingNodes = 6;

        private static string outputHelp()
        {
            Program.noStart = true;

            Console.WriteLine("Starts a new instance of IxianExplorerClient");
            Console.WriteLine("");
            Console.WriteLine(" IxianExplorerClient.exe [-h] [-v] [-a 8081] [-i ip] [-w ixian.wal]");
            Console.WriteLine("   [--config ixian.cfg] [--maxLogSize 50] [--maxLogCount 10] [--logVerbosity 14]");
            Console.WriteLine("   [--apiUrl https://explorer.ixian.io/api/v1] [--apiKey YOUR_API_KEY] [--disableActivity]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t Displays version");
            Console.WriteLine("    -a\t\t\t\t HTTP/API port to listen on");
            Console.WriteLine("    -i\t\t\t\t External IP Address to use");
            Console.WriteLine("    -w\t\t\t Specify name of the wallet file");
            Console.WriteLine("    --config\t\t\t Specify config filename (default ixian.cfg)");
            Console.WriteLine("    --maxLogSize\t\t Specify maximum log file size in MB");
            Console.WriteLine("    --maxLogCount\t\t Specify maximum number of log files");
            Console.WriteLine("    --logVerbosity\t\t Sets log verbosity (0 = none, trace = 1, info = 2, warn = 4, error = 8)");
            Console.WriteLine("    --apiUrl\t\t Specify the explorer API URL (default https://explorer.ixian.io/api/v1)");
            Console.WriteLine("    --apiKey\t\t Specify the explorer API key");
            Console.WriteLine("    --disableActivity\t\t Disable the activity scanner");
            Console.WriteLine("");
            Console.WriteLine("----------- Config File Options -----------");
            Console.WriteLine(" Config file options should use parameterName = parameterValue syntax.");
            Console.WriteLine(" Each option should be specified in its own line. Example:");
            Console.WriteLine("    apiPort = 8081");
            Console.WriteLine("    apiKey = YOUR-KEY");
            Console.WriteLine("");
            Console.WriteLine(" Available options:");
            Console.WriteLine("    apiPort\t\t\t HTTP/API port to listen on (same as -a CLI)");
            Console.WriteLine("    apiAllowIp\t\t\t Allow API connections from specified source or sources (can be used multiple times)");
            Console.WriteLine("    apiBind\t\t\t Bind to given address to listen for API connections (can be used multiple times)");
            Console.WriteLine("    addApiUser\t\t\t Adds user:password that can access the API (can be used multiple times)");
            Console.WriteLine("    externalIp\t\t\t External IP Address to use (same as -i CLI)");
            Console.WriteLine("    maxLogSize\t\t\t Specify maximum log file size in MB (same as --maxLogSize CLI)");
            Console.WriteLine("    maxLogCount\t\t\t Specify maximum number of log files (same as --maxLogCount CLI)");
            Console.WriteLine("    disableActivity\t\t\t Disable the activity scanner (same as --maxLogCount CLI)");
            Console.WriteLine("    apiUrl\t\t\t Specify the explorer API URL (default https://explorer.ixian.io/api/v1)");
            Console.WriteLine("    apiKey\t\t\t Specify the explorer API key");
            return "";
        }

        private static void readConfigFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            Logging.info("Reading config file: " + filename);
            List<string> lines = File.ReadAllLines(filename).ToList();
            foreach (string line in lines)
            {
                string[] option = line.Split('=');
                if (option.Length < 2)
                {
                    continue;
                }
                string key = option[0].Trim(new char[] { ' ', '\t', '\r', '\n' });
                string value = option[1].Trim(new char[] { ' ', '\t', '\r', '\n' });

                if (key.StartsWith(";"))
                {
                    continue;
                }
                Logging.info("Processing config parameter '" + key + "' = '" + value + "'");
                switch (key)
                {
                    case "apiPort":
                        apiPort = int.Parse(value);
                        break;
                    case "apiAllowIp":
                        apiAllowedIps.Add(value);
                        break;
                    case "apiBind":
                        apiBinds.Add(value);
                        break;
                    case "addApiUser":
                        string[] credential = value.Split(':');
                        if (credential.Length == 2)
                        {
                            apiUsers.Add(credential[0], credential[1]);
                        }
                        break;
                    case "externalIp":
                        externalIp = value;
                        break;
                    case "addPeer":
                        NetworkUtils.seedNodes.Add(new string[2] { value, null });
                        break;
                    case "addTestnetPeer":
                        NetworkUtils.seedTestNetNodes.Add(new string[2] { value, null });
                        break;
                    case "maxLogSize":
                        maxLogSize = int.Parse(value);
                        break;
                    case "maxLogCount":
                        maxLogCount = int.Parse(value);
                        break;
                    case "logVerbosity":
                        logVerbosity = int.Parse(value);
                        break;
                    case "disableActivity":
                        if (int.Parse(value) != 0)
                        {
                            Config.enableActivityScanner = false;
                        }
                        break;
                    case "apiUrl":
                        explorerAPIBaseUrl = value;
                        break;
                    case "apiKey":
                        explorerAPIKey = value;
                        break;
                    default:
                        // unknown key
                        Logging.warn("Unknown config parameter was specified '" + key + "'");
                        break;
                }
            }
        }

        private static string outputVersion()
        {
            Program.noStart = true;

            // Do nothing since version is the first thing displayed

            return "";
        }
        public static bool init(string[] args)
        {
            var cmd_parser = new FluentCommandLineParser();

            cmd_parser.SetupHelp("h", "help").Callback(text => outputHelp());
            cmd_parser.Setup<bool>('v', "version").Callback(text => outputVersion());

            // config file
            cmd_parser.Setup<string>("config").Callback(value => configFile = value).Required();

            cmd_parser.Setup<int>('a', "apiPort").Callback(value => apiPort = value).Required();

            cmd_parser.Setup<string>('i', "ip").Callback(value => externalIp = value).Required();

            cmd_parser.Setup<string>('w', "wallet").Callback(value => walletFile = value).Required();
            cmd_parser.Setup<int>("maxLogSize").Callback(value => maxLogSize = value).Required();
            cmd_parser.Setup<int>("maxLogCount").Callback(value => maxLogCount = value).Required();
            cmd_parser.Setup<bool>("disableActivity").Callback(value => Config.enableActivityScanner = false).Required();

            cmd_parser.Setup<string>("apiUrl").Callback(value => explorerAPIBaseUrl = value).Required();
            cmd_parser.Setup<string>("apiKey").Callback(value => explorerAPIKey = value).Required();
            cmd_parser.Parse(args);

            readConfigFile(configFile);

            if (explorerAPIKey == "")
            {
                Console.WriteLine("ERROR: Ixian Explorer API KEY is missing!\nAdd it using the --apiKey commandline parameter");
                return false;
            }

            return true;
        }
    }
}
