using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// Model to display the details for the stockist
    /// </summary>
    public class StockistDetailsModel
    {
        /// <summary>
        /// get or set the current content page
        /// </summary>
        public IPublishedContent CurrentPage { get; set; }


        /// <summary>
        /// get or set the page title displayed
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        /// get or set the page text displayed
        /// </summary>
        public string PageContentText { get; set; }

        /// <summary>
        /// get or set the stockist logo
        /// </summary>
        public string StockistLogo { get; set; }

        /// <summary>
        /// get or set the stockist logo
        /// </summary>
        public string StockistWebsite { get; set; }

        /// <summary>
        /// get or set the stockist phone
        /// </summary>
        public string StockistPhone { get; set; }

        /// <summary>
        /// get or set the stockist email
        /// </summary>
        public string StockistEmail { get; set; }

        /// <summary>
        /// get or set the stockist location
        /// </summary>
        public List<LocationModel> StockistLocations { get; set; } = new List<LocationModel>();
    }
}