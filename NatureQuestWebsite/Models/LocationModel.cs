using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// Model to store location details
    /// </summary>
    public class LocationModel
    {
        /// <summary>
        /// get or set the product page
        /// </summary>
        public IPublishedContent LocationPage { get; set; }

        /// <summary>
        /// get or set the street address
        /// </summary>
        public string StreetAddress { get; set; }

        /// <summary>
        /// get or set the address suburb
        /// </summary>
        public string AddressSuburb { get; set; }

        /// <summary>
        /// get or set the address city
        /// </summary>
        public string AddressCity { get; set; }

        /// <summary>
        /// get or set the address post code
        /// </summary>
        public string AddressPostCode { get; set; }

        /// <summary>
        /// get or set the full address
        /// </summary>
        public string FullAddress { get; set; }

        /// <summary>
        /// get or set the address lat long
        /// </summary>
        public string LatLong { get; set; }

        /// <summary>
        /// get or set the address latitude
        /// </summary>
        public string Lat { get; set; }

        /// <summary>
        /// get or set the address longitude
        /// </summary>
        public string Long { get; set; }

        /// <summary>
        /// get or set the location phone number
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}