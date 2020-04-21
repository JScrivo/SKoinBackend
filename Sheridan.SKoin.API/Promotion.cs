using System;

namespace Sheridan.SKoin.API
{
    public class Promotion
    {
        [Documentation.Description("The account that owns this promotion.")]
        public string Owner { get; set; }
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
}
