namespace Sheridan.SKoin.API.Services
{
    public class ExampleService
    {
        /// <summary>
        /// Example API for combining a person's first and last name into their full name.
        /// </summary>
        /// <param name="text">The data sent by the client, often as a POST request.</param>
        /// <returns>The response to send to the client.</returns>
        [Service("/api/example/text", ServiceType.Text)]
        public string TextApi(string text)
        {
            //Try to deserialize the JSON into the expected format.
            if (Json.TryDeserialize(text, out ExampleRequest request))
            {
                //Form the response.
                var result = new ExampleResponse
                {
                    FullName = request.FirstName + " " + request.LastName
                };

                //Try to serialize the response back to JSON.
                if (Json.TrySerialize(result, out string response))
                {
                    return response;
                }
            }

            //Return null if something goes wrong, which has the HttpServer return error 400.
            return null;
        }

        /// <summary>
        /// Example request format with first and last name.
        /// </summary>
        private class ExampleRequest
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        /// <summary>
        /// Example response format with full name.
        /// </summary>
        private class ExampleResponse
        {
            public string FullName { get; set; }
        }
    }
}
