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

// TODO: The adapters use a Connect() Disconnect() naming convention,
// but right now they don't support reconnecting as Disconnect() is treated as more of a Dispose.
// Should probably change the naming structure or code layout to fix this. Maybe Connect/Open and Dispose?
// Alternatively, actually support disconnecting and reconnecting though more error handling is required.

// TODO: Relay emotes.

// TODO: Bot commands.
// Request that the bot tell you who is in the room in another service (reply via DM).
// Request information on the other services, mainly URL.
// Tell you how many people are on the other services (status?).

// TODO: Test on Mono.

// TODO: Logging. Would be good to basically have two log providers, rotating file on disk and console output.
// Ex, replace Console.WriteLine calls with logging calls.

// TODO: Connection monitoring. Heartbeat? IRC has Ping/Pong.

// TODO: Really think through error handling and resiliency in terms of disconnects/reconnects.
// May have to look for an alternative to the Slack API library we're using as it doesn't handling things like this well.
