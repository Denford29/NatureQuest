using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// define the model used for the main menu
    /// </summary>
    public class MainMenuModel
    {
        /// <summary>
        /// get or set the global site email address
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// get or set the title used for the off canvas category menu
        /// </summary>
        public string CategoriesMenuTitle { get; set; }

        /// <summary>
        /// get or set the global site email address
        /// </summary>
        public string SiteEmailAddress { get; set; }

        /// <summary>
        /// get or set the global site phone number
        /// </summary>
        public string SitePhoneNumber { get; set; }

        /// <summary>
        /// get or set the global site facebook
        /// </summary>
        public string SiteFacebook { get; set; }

        /// <summary>
        /// get or set the global site instagram
        /// </summary>
        public string SiteInstagram { get; set; }

        /// <summary>
        /// get or set the global site twitter
        /// </summary>
        public string SiteTwitter { get; set; }

        /// <summary>
        /// get or set the home page link item
        /// </summary>
        public LinkItemModel HomeLinkItem { get; set; }

        /// <summary>
        /// get or set the menu's links
        /// </summary>
        public List<LinkItemModel> MenuLinks { get; set; } = new List<LinkItemModel>();

        /// <summary>
        /// get or set the featured products
        /// </summary>
        public List<LinkItemModel> FeaturedProductsLinks { get; set; } = new List<LinkItemModel>();

        /// <summary>
        /// get or set the category menu products
        /// </summary>
        public List<LinkItemModel> CategoryProductsLinks { get; set; } = new List<LinkItemModel>();

        /// <summary>
        /// get or set the current logged in member
        /// </summary>
        public IPublishedContent CurrentMember { get; set; }

        /// <summary>
        /// get or set the current logged in members name
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// get or set the current logged in members cart details
        /// </summary>
        public string MemberCartDetails { get; set; }

        /// <summary>
        /// get or set the current logged in members profile image
        /// </summary>
        public string MemberProfileImage { get; set; }

        /// <summary>
        /// get or set the opening hours
        /// </summary>
        public string[] OpeningHours { get; set; }

        /// <summary>
        /// get or set the footer links
        /// </summary>
        public List<LinkItemModel> FooterLinks { get; set; } = new List<LinkItemModel>();

        /// <summary>
        /// get or set the subscribe text
        /// </summary>
        public string SubscribeText { get; set; }
    }
}