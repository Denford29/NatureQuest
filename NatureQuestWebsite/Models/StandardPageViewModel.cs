using System.Collections.Generic;
using System.Web;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// create the standard page view model
    /// </summary>
    public class StandardPageViewModel : ContentModel
    {

        /// <summary>
        /// initiate the model with the content
        /// </summary>
        /// <param name="content"></param>
        public StandardPageViewModel(IPublishedContent content) : base(content) { }

        /// <summary>
        /// get or set the current content page
        /// </summary>
        public IPublishedContent CurrentPage { get; set; }

        /// <summary>
        /// get or set the page title used for all links
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        /// get or set the displayed page heading
        /// </summary>
        public string PageHeading { get; set; }

        /// <summary>
        /// get or set the displayed page content text
        /// </summary>
        public string PageContentText { get; set; }

        /// <summary>
        /// get or set the displayed page header image
        /// </summary>
        public string PageHeaderImage { get; set; }

        /// <summary>
        /// get or set the displayed browser title
        /// </summary>
        public string BrowserTitle { get; set; }

        /// <summary>
        /// get or set the meta keywords
        /// </summary>
        public string MetaKeywords { get; set; }

        /// <summary>
        /// get or set the meta description
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// get or set the open graph url
        /// </summary>
        public string OgPageUrl { get; set; }

        /// <summary>
        /// get or set the open graph page title
        /// </summary>
        public string OgPageTitle { get; set; }

        /// <summary>
        /// get or set the open graph page description
        /// </summary>
        public string OgPageDescription { get; set; }

        /// <summary>
        /// get or set the open graph page image
        /// </summary>
        public string OgPageImage { get; set; }

        /// <summary>
        /// get or set the google maps api key
        /// </summary>
        public string GoogleMapsApiKey { get; set; }

        /// <summary>
        /// get or set the site menu on the standard model
        /// </summary>
        public MainMenuModel SiteMenu { get; set; } = new MainMenuModel();

        /// <summary>
        /// get or set the page bread crumbs
        /// </summary>
        public List<LinkItemModel> BreadCrumbLinks { get; set; } = new List<LinkItemModel>();
    }
}