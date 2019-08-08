using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// define the model for all links within the site for menus
    /// </summary>
    public class LinkItemModel
    {
        /// <summary>
        /// get or set the link title, default page name
        /// </summary>
        public string LinkTitle { get; set; }

        /// <summary>
        /// get or set the link url
        /// </summary>
        public string LinkUrl { get; set; }

        /// <summary>
        /// get or set the link image
        /// </summary>
        public string LinkImage { get; set; }

        /// <summary>
        /// get or set the link image
        /// </summary>
        public string ThumbLinkImage { get; set; }

        /// <summary>
        /// get or set the current links umbraco page
        /// </summary>
        public IPublishedContent LinkPage { get; set; }

        /// <summary>
        /// get or set a flag if the item has got child items
        /// </summary>
        public bool HasChildLinks { get; set; }

        /// <summary>
        /// get or set a flag if the item is a mega menu
        /// </summary>
        public bool IsProductLinks { get; set; }

        /// <summary>
        /// get or set the displayed price for products
        /// </summary>
        public ProductPriceModel ProductPrice { get; set; }

        /// <summary>
        /// get or set the links child items if it has children
        /// </summary>
        public List<LinkItemModel> ChildLinkItems { get; set; } = new List<LinkItemModel>();
    }
}