using System;
using System.Collections.Generic;

namespace Sheridan.SKoin.API.Services
{
    public class EnterpriseService
    {
        /// <summary>
        /// The JSON response that is returned if the request is unsuccessful.
        /// </summary>
        private static readonly string FailedRequest = $"{{\"{nameof(UpgradeResponse.Success)}\": {false}}}";

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
                else
                {
                    return FailedRequest;
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
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        /// <summary>
        /// API for creating promotions.
        /// </summary>
        /// <param name="text">The data sent by the client.</param>
        /// <returns>The response to send to the client.</returns>
        [Service("/api/enterprise/promotion", ServiceType.Text)]
        public static string CreatePromotion(string text)
        {
            if (Json.TryDeserialize(text, out PromotionPostRequest request) && request.IsValid() && 
                IsValidIdAndHash(request.GetId(), request.Hash) && IsEnterprise(request.GetId()))
            {
                var response = new UpgradeResponse();

                var promotion = new Promotion
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = request.Title,
                    IconURI = request.IconURI,
                    CoverURI = request.CoverURI,
                    Cost = request.Cost,
                    Description = request.Description,
                    Days = request.Days,
                    Likes = 0,
                    StartTime = DateTime.UtcNow
                };

                if (Json.TrySerialize(promotion, out string promoJson) && Database.TryCreatePromotion(promotion.Id, promoJson))
                {
                    response.Success = true;
                }

                if (Json.TrySerialize(response, out string json))
                {
                    return json;
                }
                else
                {
                    return FailedRequest;
                }
            }

            return null;
        }

        private static bool IsValidIdAndHash(Guid id, string hash)
        {
            return Database.TryGetHash(id, out string expected) && expected == hash;
        }

        private static bool IsEnterprise(Guid id)
        {
            if (Database.TryGetEnterprise(id, out bool result))
            {
                return result;
            }
            else
            {
                return false;
            }
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

            public virtual bool IsValid()
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
            public string Id { get; set; }
            public string Title { get; set; }
            public string IconURI { get; set; }
            public string CoverURI { get; set; }
            public ulong Cost { get; set; }
            public string Description { get; set; }
            public ulong Days { get; set; }
            public ulong Likes { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime => StartTime.AddDays(Days);

            public bool IsExpired()
            {
                return EndTime < DateTime.UtcNow;
            }
        }

        private class PromotionPostRequest : UpgradeRequest
        {
            public string Title { get; set; }
            public string IconURI { get; set; }
            public string CoverURI { get; set; }
            public ulong Cost { get; set; }
            public string Description { get; set; } = string.Empty;
            public ulong Days { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && !((Title is null) || (IconURI is null) || (CoverURI is null) || (Description is null) || (Days == 0) || (Days > 365));
            }
        }
    }
}
