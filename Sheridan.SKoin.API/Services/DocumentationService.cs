using System.Linq;

namespace Sheridan.SKoin.API.Services
{
    public class DocumentationService
    {
        [Service("/api/docs", ServiceType.Text, typeof(void), typeof(Documentation), true)]
        [Documentation.Description("API for retrieving documentation about API endpoints.")]
        public string GetDocumentation(string endpoint, Core.HttpServer.Service[] services)
        {
            foreach (var service in services)
            {
                if (service.Path == endpoint)
                {
                    var doc = service.Attribute.GetDocs(service.Method);

                    if (Json.TrySerialize(doc, out string json))
                    {
                        return json;
                    }
                }
            }

            return null;
        }

        [Service("/api/docs/all", ServiceType.Text, typeof(void), typeof(DocumentationResponse), true)]
        [Documentation.Description("API for retrieving documentation about all active API endpoints.")]
        public string GetAllDocumentation(string text, Core.HttpServer.Service[] services)
        {
            if (Json.TrySerialize(new DocumentationResponse
            {
                Documentation = services.Select(service => service.Attribute.GetDocs(service.Method)).ToArray()
            }, out string json))
            {
                return json;
            }

            return null;
        }

        private class DocumentationResponse
        {
            [Documentation.Description("Documentation for every active API endpoint.")]
            public Documentation[] Documentation { get; set; }
        }
    }
}
