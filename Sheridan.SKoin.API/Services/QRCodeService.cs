using QRCoder;
using System;

namespace Sheridan.SKoin.API.Services
{
    public class QRCodeService
    {
        [Service("/api/qr", ServiceType.Text, typeof(IdRequest), typeof(ImageResponse))]
        [Documentation.Description("API for obtaining QR Codes for GUIDs.")]
        public string GetQRCode(string text)
        {
            if (Json.TryDeserialize(text, out IdRequest request) && request.TryGetId(out Guid id))
            {
                var result = new ImageResponse(new PngByteQRCode(new QRCodeGenerator().CreateQrCode(id.ToString(), QRCodeGenerator.ECCLevel.M)).GetGraphic(20));

                if (Json.TrySerialize(result, out string response))
                {
                    return response;
                }
            }

            return null;
        }

        private class IdRequest
        {
            [Documentation.Description("The GUID to convert to a QR code.")]
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

            [Documentation.Description("The data URI for the resulting QR code.")]
            public string Data { get; set; }
        }
    }
}
