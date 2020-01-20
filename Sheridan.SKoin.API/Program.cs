using Sheridan.SKoin.API.Core;
using System;
using System.Security.Cryptography;
using System.Text;

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

            if (!Database.TryConnect())
            {
                Console.WriteLine("Failed to connect to database.");
                return;
            }

            Server = new HttpServer();

            Console.WriteLine("Initializing services...");

            InitServices();

            Console.WriteLine($"Starting server on port {port}...");

            Server.Start(port);

            Console.WriteLine("Server started. Enter 'stop' command to stop the server.");

            string command;
            while (true)
            {
                command = Console.ReadLine().ToLower().Trim();

                if (command == "stop")
                {
                    break;
                }
                else if (command == "newuser")
                {
                    Console.Write("Enter new password: ");

                    if (Database.TryCreateUser(Convert.ToBase64String(
                            SHA256.Create().ComputeHash(
                                Encoding.UTF8.GetBytes(Console.ReadLine())
                                )
                            ), out Guid newUser))
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
                        if (Database.TryGetBalance(user, out ulong balance) && Database.TryGetPassword(user, out string hash))
                        {
                            Console.WriteLine(string.Empty.PadLeft(Console.BufferWidth, '='));
                            Console.WriteLine($"User: {user}");
                            Console.WriteLine($"Balance: {balance}");
                            Console.WriteLine($"Password Hash: {hash}");
                            Console.WriteLine(string.Empty.PadLeft(Console.BufferWidth, '='));
                        }
                        else
                        {
                            Console.WriteLine($"Entry not found for user \"{user}\".");
                        }
                    }
                    else if (parts[0] == "passwd")
                    {
                        Console.Write("Enter new password: ");

                        if (Database.TrySetPassword(user, Convert.ToBase64String(
                            SHA256.Create().ComputeHash(
                                Encoding.UTF8.GetBytes(Console.ReadLine())
                                )
                            )))
                        {
                            Console.WriteLine("Password has been changed.");
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
