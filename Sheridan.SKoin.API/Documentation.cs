using System;

namespace Sheridan.SKoin.API
{
    public class Documentation
    {
        [Description("The endpoint this documentation is associated with.")]
        public string Endpoint { get; set; } = null;
        [Description("The description of the endpoint.")]
        public string Description { get; set; } = null;
        [Children]
        [Description("Documentation about the request format.")]
        public JsonDoc[] Request { get; set; } = new JsonDoc[0];
        [Children]
        [Description("Documentation about the response format.")]
        public JsonDoc[] Response { get; set; } = new JsonDoc[0];

        public class JsonDoc
        {
            [Description("The name of the field.")]
            public string Field { get; set; } = null;
            [Description("The data type of the field.")]
            public string Type { get; set; } = typeof(void).Name;
            [Description("The description of the field.")]
            public string Description { get; set; } = null;
            [Description("The children fields of the field type.")]
            public JsonDoc[] Children { get; set; } = new JsonDoc[0];
        }

        [AttributeUsage(AttributeTargets.All)]
        public class DescriptionAttribute : Attribute
        {
            public string Value { get; set; }

            public DescriptionAttribute(string description)
            {
                Value = description;
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class ChildrenAttribute : Attribute { }
    }
}
