using Microsoft.Extensions.Configuration;
using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appseting.json");
            var config = configurationBuilder.Build();

            using (Server server = new Server(config))
            {
                server.Start();

                Console.WriteLine($"server start, At port: {config.GetSection("BindPort").Value}");
                Console.ReadKey();
            }

           
        }
    }
}
