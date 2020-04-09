using QRCoder;
using System;

namespace Sheridan.SKoin.API.Services
{
    public class QRCodeService
    {
        /// <summary>
        /// API for obtaining QR Codes for GUIDs.
        /// </summary>
        /// <param name="text">The data sent by the client.</param>
        /// <returns>The response to send to the client.</returns>
        [Service("/api/qr", ServiceType.Text)]
        public string TextApi(string text)
        {
            //Deserialize the request JSON and validate the GUID.
            if (Json.TryDeserialize(text, out IdRequest request) && request.TryGetId(out Guid id))
            {
                //Generate the QR Code and resulting data URI from the GUID.
                var result = new ImageResponse(new PngByteQRCode(new QRCodeGenerator().CreateQrCode(id.ToByteArray(), QRCodeGenerator.ECCLevel.M)).GetGraphic(20));

                //Serialize the data URI to JSON.
                if (Json.TrySerialize(result, out string response))
                {
                    //Return the JSON to the client.
                    return response;
                }
            }

            return null;
        }

        private class IdRequest
        {
            public string Id { get; set; }

            public bool TryGetId(out Guid guid)
            {
                return Guid.TryParse(Id, out guid);
            }
        }

        private class ImageResponse
        {
            public ImageResponse(byte[] png)
            {
                Data = $"data:image/png;base64,{Convert.ToBase64String(png)}";
            }

            public string Data { get; set; }
        }
    }
}
