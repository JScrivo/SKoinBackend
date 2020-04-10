using System;
using System.Collections.Generic;

namespace Sheridan.SKoin.API.Services
{
    public class EnterpriseService
    {
        /// <summary>
        /// API for upgrading a regular account to an enterprise account.
        /// </summary>
        /// <param name="text">The data sent by the client.</param>
        /// <returns>The response to send to the client.</returns>
        [Service("/api/enterprise/upgrade", ServiceType.Text)]
        public string UpgradeAccount(string text)
        {
            if (Json.TryDeserialize(text, out UpgradeRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new UpgradeResponse
                {
                    Success = Database.TrySetEnterprise(request.GetId(), true)
                };

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
            }

            return null;
        }

        /// <summary>
        /// API for retrieving current promotions.
        /// </summary>
        /// <param name="text">The data sent by the client.</param>
        /// <returns>The response to send to the client.</returns>
        [Service("/api/promotions", ServiceType.Text)]
        public static string GetPromotions(string text)
        {
            if (Json.TryDeserialize(text, out PromotionsRequest request))
            {
                var result = new PromotionsResponse
                {
                    Promotions = DeserializePromotions(Database.GetPromotions(request.MaximumResults))
                };

                if (Json.TrySerialize(result, out string json))
                {
                    return json;
                }
            }

            return null;
        }

        private static bool IsValidIdAndHash(Guid id, string hash)
        {
            return Database.TryGetHash(id, out string expected) && expected == hash;
        }

        private static Promotion[] DeserializePromotions(string[] promotions)
        {
            var result = new List<Promotion>();

            foreach (var promo in promotions)
            {
                if (Json.TryDeserialize(promo, out Promotion promotion))
                {
                    result.Add(promotion);
                }
            }

            return result.ToArray();
        }

        private class UpgradeRequest
        {
            public string Id { get; set; }
            public string Hash { get; set; }

            public bool IsValid()
            {
                try
                {
                    return Guid.TryParse(Id, out _) && Convert.FromBase64String(Hash).Length == 32;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
            }

            public Guid GetId()
            {
                return Guid.Parse(Id);
            }
        }

        private class UpgradeResponse
        {
            public bool Success { get; set; } = false;
        }

        private class PromotionsRequest
        {
            public ulong MaximumResults { get; set; }
        }

        private class PromotionsResponse
        {
            public Promotion[] Promotions { get; set; }
        }

        private class Promotion
        {
            public string Title { get; set; }
            public string IconURI { get; set; }
            public string CoverURI { get; set; }
            public ulong Cost { get; set; }
            public string Description { get; set; }
            public ulong Likes { get; set; }
            public DateTime StartTime { get; set; }
            public ulong Days { get; set; }
        }
    }
}
