using System;

namespace ChatRelay
{
    class Program
    {
        private static readonly Relay relay = new Relay();

        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Run();
        }

        public static void Run()
        {
            relay.Stop();
            try
            {
                relay.Start();
            }
            catch (Exception ex) when (ex.GetType() == typeof(RelayConfigurationException))
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return;
            }

            while (true)
            {
                char command = Console.ReadKey(true).KeyChar;

                if (command == 'q')
                {
                    Console.WriteLine("Quitting...");
                    relay.Stop();
                    return;
                }
                if (command == 'r')
                {
                    Console.WriteLine("Restarting...");
                    relay.Stop();
                    relay.Start();
                }
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"EXCEPTION: {e.ExceptionObject}");
            Run();
        }
    }
}
