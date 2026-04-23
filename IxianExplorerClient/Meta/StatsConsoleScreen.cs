using IXICore.Meta;
using IXICore.Network;
using IXICore;
using IXICore.Utils;
using System;
using System.Threading;
using System.Linq;

namespace IxianExplorerClient.Meta
{
    public class StatsConsoleScreen
    {
        private DateTime startTime;

        private Thread? thread = null;
        private bool running = false;

        private int consoleWidth = 55;
        private uint drawCycle = 0; // Keep a count of screen draw cycles as a basic method of preventing visual artifacts
        public StatsConsoleScreen()
        {
            Console.Clear();

            Console.CursorVisible = false;// ConsoleHelpers.verboseConsoleOutput;

            // Start thread
            running = true;
            thread = new Thread(new ThreadStart(threadLoop));
            thread.Name = "Stats_Console_Thread";
            thread.Start();

            startTime = DateTime.UtcNow;
        }

        // Shutdown console thread
        public void stop()
        {
            running = false;
        }

        private void threadLoop()
        {
            while (running)
            {
                if (ConsoleHelpers.verboseConsoleOutput == false)
                {
                    // Clear the screen every 10 seconds to prevent any persisting visual artifacts
                    if (drawCycle > 5)
                    {
                        clearScreen();
                        drawCycle = 0;
                    }
                    else
                    {
                        drawScreen();
                        drawCycle++;
                    }
                }

                Thread.Sleep(2000);
            }
        }

        public void clearScreen()
        {
            Console.Clear();
            drawScreen();
        }

        public void drawScreen()
        {
            Console.SetCursorPosition(0, 0);

            bool update_avail = false;

            int connectionsOut = NetworkClientManager.getConnectedClients(true).Count();
            string url = Config.apiBinds.First();

            writeLine("Ixian Explorer Lite Client");
            writeLine("Version: {0}", Config.version + " BETA ");
            writeLine("API: {0}", url);
            writeLine("──────────────────────────────────────────────────────");
            if (update_avail)
            {

            }
            else
            {
                if (!NetworkServer.isConnectable() && connectionsOut == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    writeLine(" Your node isn't connectable from the internet.");
                    writeLine(" Please set-up port forwarding for port " + IxianHandler.publicPort + ".");
                    writeLine(" Make sure you can connect to: " + IxianHandler.getFullPublicAddress());
                    Console.ResetColor();
                }
                else
                {
                    writeLine("For help please visit https://www.ixian.io");
                }
            }
            writeLine("──────────────────────────────────────────────────────");


            Console.Write(" Status:               ");
            string dltStatus = "active";

            if (connectionsOut < 1)
                dltStatus = "connecting   ";

            writeLine(dltStatus);
            Console.ResetColor();

            writeLine("");

            writeLine(" Connections:          {0}", connectionsOut);
            writeLine(" Presences:            {0}", PresenceList.getTotalPresences());

            writeLine("");
            writeLine(" Wallet File:          {0}", Config.walletFile);
            writeLine(" Number of addresses:  {0}", IxianHandler.getWalletStorage().getMyAddresses().Count);
            writeLine(" Primary Wallet Address:\n {0}", IxianHandler.getWalletStorage().getPrimaryAddress().ToString());           

            writeLine("");           
            writeLine(" Transactions Added:   {0}", Node.transactionsAdded);

            string activityStatus = "off";
            if (ActivityScanner.isActive()) activityStatus = "active";
            if (ActivityScanner.isSyncing()) activityStatus = "syncing";
            writeLine(" Activity Scanner:     {0}", activityStatus);

            writeLine("──────────────────────────────────────────────────────");
            
            TimeSpan elapsed = DateTime.UtcNow - startTime;

            writeLine(" Running for {0} days {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
            writeLine("");
            writeLine(" Press V to toggle stats. Esc key to exit.");

        }

        private void writeLine(string str, params object[] arguments)
        {
            Console.WriteLine(string.Format(str, arguments).PadRight(consoleWidth));
        }
    }
}
