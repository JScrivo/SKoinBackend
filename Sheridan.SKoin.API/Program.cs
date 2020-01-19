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

                if (command == "stop") break;
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
