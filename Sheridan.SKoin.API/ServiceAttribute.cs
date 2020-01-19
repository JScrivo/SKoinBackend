using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheridan.SKoin.API
{
    public class ServiceAttribute : Attribute
    {
        public string Path { get; private set; }
        public ServiceType Type { get; set; }

        public ServiceAttribute(string path) : this(path, ServiceType.Text) { }

        public ServiceAttribute(string path, ServiceType type)
        {
            Path = path;
            Type = type;
        }

        public static IEnumerable<Type> GetServices()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetMethods().Where(method => !(method.GetCustomAttribute<ServiceAttribute>() is null)).Any());
        }
    }
}
