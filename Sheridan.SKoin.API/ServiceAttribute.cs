﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheridan.SKoin.API
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceAttribute : Attribute
    {
        public string Path { get; private set; }
        public ServiceType Type { get; private set; }
        public Type RequestType { get; private set; }
        public Type ResponseType { get; private set; }
        public bool MetaService { get; private set; }

        public ServiceAttribute(string path) : this(path, ServiceType.Text) { }

        public ServiceAttribute(string path, ServiceType type) : this(path, type, typeof(void), typeof(void)) { }

        public ServiceAttribute(string path, ServiceType type, Type request, Type response) : this(path, type, request, response, false) { }

        public ServiceAttribute(string path, ServiceType type, Type request, Type response, bool metaService)
        {
            Path = path;
            Type = type;
            RequestType = request;
            ResponseType = response;
            MetaService = metaService;
        }

        public static IEnumerable<Type> GetServices()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetMethods().Where(method => !(method.GetCustomAttribute<ServiceAttribute>() is null)).Any());
        }

        public Documentation GetDocs(MethodInfo info)
        {
            var result = new Documentation { Endpoint = Path };

            {
                if (info.GetCustomAttribute<Documentation.DescriptionAttribute>() is Documentation.DescriptionAttribute attrib)
                {
                    result.Description = attrib.Value;
                }
            }

            {
                var fields = RequestType.GetProperties().Where(prop => prop.CanWrite).ToArray();
                result.Request = new Documentation.JsonDoc[fields.Length];

                for (int i = 0; i < fields.Length; i++)
                {
                    result.Request[i] = GetDocs(fields[i]);
                }
            }

            {
                var fields = ResponseType.GetProperties().Where(prop => prop.CanWrite).ToArray();
                result.Response = new Documentation.JsonDoc[fields.Length];

                for (int i = 0; i < fields.Length; i++)
                {
                    result.Response[i] = GetDocs(fields[i]);
                }
            }
            
            return result;
        }

        private static Documentation.JsonDoc GetDocs(PropertyInfo property)
        {
            var result = new Documentation.JsonDoc { Field = property.Name, Type = property.PropertyType.Name };

            if (property.GetCustomAttribute<Documentation.DescriptionAttribute>() is Documentation.DescriptionAttribute attrib)
            {
                result.Description = attrib.Value;
            }

            if (!(property.GetCustomAttribute<Documentation.ChildrenAttribute>() is null))
            {
                var type = property.PropertyType;

                if (type.IsArray) type = type.GetElementType();

                result.Children = type.GetProperties().Select(prop => GetDocs(prop)).ToArray();
            }

            return result;
        }
    }
}
