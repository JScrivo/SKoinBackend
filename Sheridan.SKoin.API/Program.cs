using Sheridan.SKoin.API.Core;
using System;

namespace Sheridan.SKoin.API
{
    class Program
    {
        public const ushort DefaultPort = 8080;

        private static HttpServer Server;

        private static void Main(string[] args)
        {
            var port = GetSpecifiedPort(args);

            Console.WriteLine("Connecting to database...");

            if (!Database.TryInitialize())
            {
                Console.WriteLine("Failed to init to database.");
                return;
            }

            Server = new HttpServer();

            Console.WriteLine("Initializing services...");

            InitServices();

            Console.WriteLine($"Starting server on port {port}...");

            Server.Start(port);

            Console.WriteLine("Server started. Enter 'stop' command to stop the server. Enter 'help' for a detailed list of commands.");

            string command;
            while (true)
            {
                command = Console.ReadLine().ToLower().Trim();

                if (command == "stop")
                {
                    break;
                }
                else if (command == "help")
                {
                    WriteSeperator();

                    Console.WriteLine("help                          - Display this information.");
                    Console.WriteLine("newuser                       - Create a new user.");
                    Console.WriteLine("stop                          - Stops the server.");
                    Console.WriteLine("transfer <from> <to> <amount> - Transfer funds from one user to another.");
                    Console.WriteLine("user <user>                   - Display user information.");

                    WriteSeperator();
                }
                else if (command == "newuser")
                {
                    Console.Write("Enter new password hash: ");

                    var input = Console.ReadLine();
                    var buffer = new Span<byte>(new byte[input.Length * 3 / 4]);
                    if (Convert.TryFromBase64String(input, buffer, out int written) && Database.TryCreateUser(Convert.ToBase64String(buffer.Slice(0, written)), out Guid newUser))
                    {
                        Console.WriteLine($"User \"{newUser}\" created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create new user.");
                    }
                }

                var parts = command.Split(' ');

                if (parts.Length == 2 && Guid.TryParse(parts[1], out Guid user))
                {
                    if (parts[0] == "user")
                    {
                        if (Database.TryGetBalance(user, out ulong balance) && Database.TryGetHash(user, out string hash))
                        {
                            WriteSeperator();

                            Console.WriteLine($"User: {user}");
                            Console.WriteLine($"Balance: {balance}");
                            Console.WriteLine($"Hash: {hash}");

                            WriteSeperator();
                        }
                        else
                        {
                            Console.WriteLine($"Entry not found for user \"{user}\".");
                        }
                    }
                }
                else if (parts.Length == 4 && Guid.TryParse(parts[1], out Guid from) && Guid.TryParse(parts[2], out Guid to) && ulong.TryParse(parts[3], out ulong amount))
                {
                    if (parts[0] == "transfer")
                    {
                        if (Database.TryTransact(from, to, amount))
                        {
                            Console.WriteLine("Transaction completed.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to transfer {amount} from \"{from}\" to \"{to}\".");
                        }
                    }
                }
            }

            Console.WriteLine("Stopping server...");

            Server.Stop();

            Console.WriteLine("Server stopped.");
        }

        private static void WriteSeperator()
        {
            Console.WriteLine(string.Empty.PadLeft(Console.BufferWidth, '='));
        }

        private static void InitServices()
        {
            var services = ServiceAttribute.GetServices();

            foreach (var service in services)
            {
                Console.WriteLine($"\tLoaded service {service.Name}...");
                Server.AddService(service);
            }

            Console.WriteLine(string.Empty);
        }

        private static ushort GetSpecifiedPort(string[] args)
        {
            foreach (var arg in args)
            {
                if (ushort.TryParse(arg, out ushort value)) return value;
            }

            return DefaultPort;
        }
    }
}
