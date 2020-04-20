using System;
using System.Collections.Generic;

namespace Sheridan.SKoin.API.Services
{
    public class EnterpriseService
    {
        [Documentation.Description("The JSON response that is returned if the request is unsuccessful.")]
        private static readonly string FailedRequest = $"{{\"{nameof(UpgradeResponse.Success)}\": {false}}}";

        [Service("/api/enterprise/upgrade", ServiceType.Text, typeof(UpgradeRequest), typeof(UpgradeResponse))]
        [Documentation.Description("API for upgrading a regular account to an enterprise account.")]
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

        [Service("/api/promotions", ServiceType.Text, typeof(PromotionsRequest), typeof(PromotionsResponse))]
        [Documentation.Description("API for retrieving current promotions.")]
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

        [Service("/api/promotions/like", ServiceType.Text, typeof(PromotionLikeRequest), typeof(UpgradeResponse))]
        [Documentation.Description("API for liking promotions.")]
        public static string LikePromotion(string text)
        {
            if (Json.TryDeserialize(text, out PromotionLikeRequest request) && request.IsValid() && IsValidIdAndHash(request.GetId(), request.Hash))
            {
                var result = new UpgradeResponse { Success = false };

                if (Database.TryGetPromotion(request.Id, out string json))
                {
                    if (Json.TryDeserialize(json, out Promotion promotion))
                    {
                        promotion.Likes++;

                        if (Json.TrySerialize(promotion, out json))
                        {
                            if (Database.TryCreatePromotion(request.Id, json, true))
                            {
                                result.Success = true;
                            }
                        }
                    }
                }

                if (Json.TrySerialize(result, out json))
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

        [Service("/api/enterprise/promotion", ServiceType.Text, typeof(PromotionPostRequest), typeof(UpgradeResponse))]
        [Documentation.Description("API for creating promotions.")]
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
            [Documentation.Description("The user id that was registered with the client's secret hash.")]
            public string Id { get; set; }
            [Documentation.Description("The secret hash for the client.")]
            public string Hash { get; set; }

            public virtual bool IsValid()
            {
                try
                {
                    return Guid.TryParse(Id, out _); //&& Convert.FromBase64String(Hash).Length == 32; TODO: Uncomment this. Only for development.
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
            [Documentation.Description("Whether or not the request was successful.")]
            public bool Success { get; set; } = false;
        }

        private class PromotionsRequest
        {
            [Documentation.Description("The maximum number of results to return.")]
            public ulong MaximumResults { get; set; }
        }

        private class PromotionsResponse
        {
            [Documentation.Children]
            [Documentation.Description("The currently active promotions")]
            public Promotion[] Promotions { get; set; }
        }

        private class PromotionLikeRequest : UpgradeRequest
        {
            [Documentation.Description("The id of the promotion to like.")]
            public string PromotionId { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && Guid.TryParse(PromotionId, out _);
            }

            public Guid GetPromotionId()
            {
                return Guid.Parse(PromotionId);
            }
        }

        private class Promotion
        {
            [Documentation.Description("The id of the promotion.")]
            public string Id { get; set; }
            [Documentation.Description("The title of the promotion.")]
            public string Title { get; set; }
            [Documentation.Description("The URI to the icon image for the promotion.")]
            public string IconURI { get; set; }
            [Documentation.Description("The URI to the cover image for the promotion.")]
            public string CoverURI { get; set; }
            [Documentation.Description("The cost of the advertised item. 0 if the promotion is not for a specific item with a cost.")]
            public ulong Cost { get; set; }
            [Documentation.Description("The description of the promotion.")]
            public string Description { get; set; }
            [Documentation.Description("The length of the promotion, in days.")]
            public ulong Days { get; set; }
            [Documentation.Description("The number of likes this promotion has.")]
            public ulong Likes { get; set; }
            [Documentation.Description("The start date and time of the promotion, in UTC.")]
            public DateTime StartTime { get; set; }
            [Documentation.Description("The end date and time of the promotion, in UTC.")]
            public DateTime EndTime => StartTime.AddDays(Days);
            [Documentation.Description("The number of days remaining for this promotion.")]
            public ulong DaysLeft => (ulong)Math.Max(1, Math.Round((EndTime - DateTime.UtcNow).TotalDays));

            public bool IsExpired()
            {
                return EndTime < DateTime.UtcNow;
            }
        }

        private class PromotionPostRequest : UpgradeRequest
        {
            [Documentation.Description("The title of the promotion.")]
            public string Title { get; set; }
            [Documentation.Description("The URI to the icon image for the promotion.")]
            public string IconURI { get; set; }
            [Documentation.Description("The URI to the cover image for the promotion.")]
            public string CoverURI { get; set; }
            [Documentation.Description("The cost of the advertised item. 0 if the promotion is not for a specific item with a cost.")]
            public ulong Cost { get; set; }
            [Documentation.Description("The description of the promotion.")]
            public string Description { get; set; } = string.Empty;
            [Documentation.Description("The length of the promotion, in days.")]
            public ulong Days { get; set; }

            public override bool IsValid()
            {
                return base.IsValid() && !((Title is null) || (IconURI is null) || (CoverURI is null) || (Description is null) || (Days == 0) || (Days > 365));
            }
        }
    }
}
