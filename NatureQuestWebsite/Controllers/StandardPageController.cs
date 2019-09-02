using NatureQuestWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using NatureQuestWebsite.Services;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace NatureQuestWebsite.Controllers
{
    public class StandardPageController : RenderMvcController
    {
        /// <summary>
        /// create the default site name string
        /// </summary>
        private readonly string _siteName;

        /// <summary>
        /// create the default site url host
        /// </summary>
        private string _urlHost = "https://www.naturesquest.com.au";

        /// <summary>
        /// set the global details page
        /// </summary>
        private readonly IPublishedContent _globalDetailsPage;

        /// <summary>
        /// set the home page
        /// </summary>
        private readonly IPublishedContent _homePage;

        /// <summary>
        /// create the default categories headline
        /// </summary>
        private readonly string _categoriesHeadline = "Shop Categories";

        /// <summary>
        /// create the default site email address
        /// </summary>
        private readonly string _siteEmailAddress = "info@naturesquest.com.au";

        /// <summary>
        /// create the default site phone number
        /// </summary>
        private readonly string _sitePhoneNumber = "(08) 83826005";

        /// <summary>
        /// create the default site facebook link
        /// </summary>
        private readonly string _siteFacebook;

        /// <summary>
        /// create the default site instagram
        /// </summary>
        private readonly string _siteInstagram;

        /// <summary>
        /// create the default site twitter
        /// </summary>
        private readonly string _siteTwitter;

        /// <summary>
        /// create the default site phone number
        /// </summary>
        private readonly string _siteGoogleMapsApiKey = "AIzaSyC-vhODoo9YtzzoEyUlf4XwFBs3ZmQ7X9I";

        /// <summary>
        /// get the products service
        /// </summary>
        private readonly IProductsService _productsService;

        /// <summary>
        /// initialise the controller
        /// </summary>
        public StandardPageController(IProductsService productsService)
        {
            //set the product service to use
            _productsService = productsService;

            //get the global site settings page to use
            var homePage = Umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
            if (homePage?.Id > 0)
            {
                //save the home page for use later
                _homePage = homePage;
            }

            //get the global site settings page to use
            var siteSetting = Umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
            if (siteSetting?.Id > 0)
            {
                //get the site details page
                var siteDetailsPage = siteSetting.Descendants("globalDetails").FirstOrDefault();
                if (siteDetailsPage?.Id > 0)
                {
                    //save the global details page to use later
                    _globalDetailsPage = siteDetailsPage;

                    //get the site name
                    if (siteDetailsPage.HasProperty("siteName") && siteDetailsPage.HasValue("siteName"))
                    {
                        //set the global site name
                        _siteName = siteDetailsPage.GetProperty("siteName").Value().ToString();
                    }

                    //get the site category headline
                    if (siteDetailsPage.HasProperty("categoriesHeadline") && siteDetailsPage.HasValue("categoriesHeadline"))
                    {
                        //set the global site category headline
                        _categoriesHeadline = siteDetailsPage.GetProperty("categoriesHeadline").Value().ToString();
                    }

                    //get the site email address
                    if (siteDetailsPage.HasProperty("siteEmailAddress") && siteDetailsPage.HasValue("siteEmailAddress"))
                    {
                        //set the global site email address
                        _siteEmailAddress = siteDetailsPage.GetProperty("siteEmailAddress").Value().ToString();
                    }

                    //get the site phone number
                    if (siteDetailsPage.HasProperty("sitePhoneNumber") && siteDetailsPage.HasValue("sitePhoneNumber"))
                    {
                        //set the global site phone number
                        _sitePhoneNumber = siteDetailsPage.GetProperty("sitePhoneNumber").Value().ToString();
                    }

                    //get the site facebook
                    if (siteDetailsPage.HasProperty("siteFacebook") && siteDetailsPage.HasValue("siteFacebook"))
                    {
                        //set the global site facebook
                        _siteFacebook = siteDetailsPage.GetProperty("siteFacebook").Value().ToString();
                    }

                    //get the site instagram
                    if (siteDetailsPage.HasProperty("siteInstagram") && siteDetailsPage.HasValue("siteInstagram"))
                    {
                        //set the global site instagram
                        _siteInstagram = siteDetailsPage.GetProperty("siteInstagram").Value().ToString();
                    }

                    //get the site twitter
                    if (siteDetailsPage.HasProperty("siteTwitter") && siteDetailsPage.HasValue("siteTwitter"))
                    {
                        //set the global site twitter
                        _siteTwitter = siteDetailsPage.GetProperty("siteTwitter").Value().ToString();
                    }

                    //get the google api key
                    if (siteDetailsPage.HasProperty("googleMapsAPIKey") && siteDetailsPage.HasValue("googleMapsAPIKey"))
                    {
                        //set the global site name
                        _siteGoogleMapsApiKey = siteDetailsPage.GetProperty("googleMapsAPIKey").Value().ToString();
                    }
                }
            }
        }

        /// <summary>
        /// add the over ride for the default index action
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override ActionResult Index(ContentModel model)
        {
            //get the url from the request
            if (!string.IsNullOrWhiteSpace(HttpContext.Request.Url?.Host))
            {
                _urlHost = HttpContext.Request.Url.Host;
            }

            //create the default custom model to use
            var viewModel = new StandardPageViewModel(model.Content)
            {
                CurrentPage = model.Content
            };

            //set the default page heading
            viewModel.PageHeading = viewModel.CurrentPage.Name;
            //check if we have the page heading set on the current page
            if (viewModel.CurrentPage.HasProperty("pageHeading") && viewModel.CurrentPage.HasValue("pageHeading"))
            {
                // set the page heading on the model
                viewModel.PageHeading = viewModel.CurrentPage.GetProperty("pageHeading").Value().ToString();
            }

            //set the default page title
            viewModel.PageTitle = viewModel.CurrentPage.Name;
            //check if we have the page heading set on the current page
            if (viewModel.CurrentPage.HasProperty("pageTitle") && viewModel.CurrentPage.HasValue("pageTitle"))
            {
                // set the page title on the model
                viewModel.PageTitle = viewModel.CurrentPage.GetProperty("pageTitle").Value().ToString();
            }

            //get the meta content
            viewModel = PrepareMetaDetails(viewModel);

            //get the menu items on the model
            viewModel = PrepareSiteMenu(viewModel);

            //get the page content for the model
            viewModel = PreparePageContent(viewModel);

            //return the template with our model
            return CurrentTemplate(viewModel);
        }

        #region Site Default Properties
        /// <summary>
        /// add the meta content to the model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public StandardPageViewModel PrepareMetaDetails(StandardPageViewModel model)
        {
            // save the google api key
            model.GoogleMapsApiKey = _siteGoogleMapsApiKey;
            //check if we have a global details page to use
            if (_globalDetailsPage?.Id > 0)
            {
                //get the default browser title
                model.BrowserTitle = model.PageTitle + " | " + _siteName;
                //check if the page has got a browser title to use and use that 1
                if (model.CurrentPage.HasProperty("browserTitle") && model.CurrentPage.HasValue("browserTitle"))
                {
                    //set the global site name
                    model.BrowserTitle = model.CurrentPage.GetProperty("browserTitle").Value().ToString();
                }

                //get the default meta description
                model.MetaDescription = model.PageTitle + " - " + _siteName;
                //check if the page has got the meta description and set that
                if (model.CurrentPage.HasProperty("metaDescription") && model.CurrentPage.HasValue("metaDescription"))
                {
                    //set the global site name
                    model.MetaDescription = model.CurrentPage.GetProperty("metaDescription").Value() + " - " + _siteName;
                }
                //if the page doesn't have the meta description then use the default from the global page
                else if (_globalDetailsPage.HasProperty("defaultDescription") && _globalDetailsPage.HasValue("defaultDescription"))
                {
                    //set the global site name
                    model.MetaDescription = model.PageTitle + " - "
                                                            + _globalDetailsPage.GetProperty("defaultDescription").Value()
                                                            + " - " + _siteName;
                }

                //get the default meta keywords
                model.MetaKeywords = model.PageTitle + " , " + _siteName;
                //check if the page has got the meta keywords and set that
                if (model.CurrentPage.HasProperty("metaKeyword") && model.CurrentPage.HasValue("metaKeyword"))
                {
                    //set the global site name
                    model.MetaKeywords = model.CurrentPage.GetProperty("metaKeyword").Value() + " , " + _siteName;
                }
                //if the page doesn't have the meta keywords then use the default from the global page
                else if (_globalDetailsPage.HasProperty("defaultKeyword") && _globalDetailsPage.HasValue("defaultKeyword"))
                {
                    //set the global site name
                    model.MetaKeywords = model.PageTitle + " , "
                                                            + _globalDetailsPage.GetProperty("defaultKeyword").Value()
                                                            + " , " + _siteName;
                }

                //get the default og page title
                model.OgPageTitle = model.PageTitle + " - " + _siteName;
                //check if the page has got the og page title and set that
                if (model.CurrentPage.HasProperty("openGraphTitle") && model.CurrentPage.HasValue("openGraphTitle"))
                {
                    //set the global site name
                    model.OgPageTitle = model.CurrentPage.GetProperty("openGraphTitle").Value() + " - " + _siteName;
                }
                //if the page doesn't have the meta description then use the default from the global page
                else if (_globalDetailsPage.HasProperty("openGraphTitle") && _globalDetailsPage.HasValue("openGraphTitle"))
                {
                    //set the global site name
                    model.OgPageTitle = model.PageTitle + " - "
                                                            + _globalDetailsPage.GetProperty("openGraphTitle").Value()
                                                            + " - " + _siteName;
                }

                //get the default og page description
                model.OgPageDescription = model.PageTitle + " - " + _siteName;
                //check if the page has got the meta description and set that
                if (model.CurrentPage.HasProperty("openGraphDescription") && model.CurrentPage.HasValue("openGraphDescription"))
                {
                    //set the global site name
                    model.OgPageDescription = model.CurrentPage.GetProperty("openGraphDescription").Value() + " - " + _siteName;
                }
                //if the page doesn't have the meta description then use the default from the global page
                else if (_globalDetailsPage.HasProperty("openGraphDescription") && _globalDetailsPage.HasValue("openGraphDescription"))
                {
                    //set the global site name
                    model.OgPageDescription = model.PageTitle + " - "
                                                            + _globalDetailsPage.GetProperty("openGraphDescription").Value()
                                                            + " - " + _siteName;
                }

                //set the current page url, if its not the home page get the page url to add to the host
                model.OgPageUrl = _urlHost;
                if (model.CurrentPage.GetTemplateAlias() != "homePage")
                {
                    model.OgPageUrl = _urlHost + model.CurrentPage.Url;
                }

                //set the og page image
                model.OgPageImage = _urlHost + "/Images/Nature-Quest-Open-Graph.jpg";
                //check if the page has got the meta description and set that
                if (model.CurrentPage.HasProperty("openGraphImage") && model.CurrentPage.HasValue("openGraphImage"))
                {
                    //set the global site name
                    var mediaItem = model.CurrentPage.Value<IPublishedContent>("openGraphImage");
                    if (mediaItem != null && mediaItem.Id > 0)
                    {
                        var defaultCropSize = mediaItem.GetCropUrl("openGraph");
                        var ogImagelink = !string.IsNullOrEmpty(defaultCropSize) ?
                            defaultCropSize :
                            mediaItem.GetCropUrl(1200, 630);
                        if (!string.IsNullOrWhiteSpace(ogImagelink))
                        {
                            model.OgPageImage = _urlHost + ogImagelink;
                        }
                    }
                }
                //if the page doesn't have the meta description then use the default from the global page
                else if (_globalDetailsPage.HasProperty("openGraphImage") && _globalDetailsPage.HasValue("openGraphImage"))
                {
                    //get the media item from the details page
                    var mediaItem = _globalDetailsPage.Value<IPublishedContent>("openGraphImage");
                    if (mediaItem != null && mediaItem.Id > 0)
                    {
                        var defaultCropSize = mediaItem.GetCropUrl("openGraph");
                        var ogImagelink = !string.IsNullOrEmpty(defaultCropSize) ?
                            defaultCropSize :
                            mediaItem.GetCropUrl(1200, 630);
                        if (!string.IsNullOrWhiteSpace(ogImagelink))
                        {
                            model.OgPageImage = _urlHost + ogImagelink;
                        }
                    }
                }

            }

            //return the model after adding the meta content
            return model;
        }

        /// <summary>
        /// add the menu items to the model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public StandardPageViewModel PrepareSiteMenu(StandardPageViewModel model)
        {
            //get the menu item from the model to use
            var menuItem = model.SiteMenu;
            //set the site name 
            menuItem.SiteName = _siteName;

            //get the site menu categories headline
            menuItem.CategoriesMenuTitle = _categoriesHeadline;

            //get the site menu email address
            menuItem.SiteEmailAddress = _siteEmailAddress;

            //get the side menu phone number
            menuItem.SitePhoneNumber = _sitePhoneNumber;

            //get the side menu facebook
            menuItem.SiteFacebook = _siteFacebook;

            //get the side menu instagram
            menuItem.SiteInstagram = _siteInstagram;

            //get the side menu twitter
            menuItem.SiteTwitter = _siteTwitter;

            //check if we have the home page set
            if (_homePage?.Id > 0)
            {
                //set the home link item
                menuItem.HomeLinkItem = new LinkItemModel
                {
                    LinkTitle = _siteName,
                    LinkUrl = "/",
                    LinkPage = _homePage
                };

                //get the 1st level child pages from the home page
                var landingPages = _homePage.Children.Where(page => 
                                                    !page.Value<bool>("hideFromMenu")
                                                    && page.IsPublished()).
                                                    ToList();
                //add these to the model
                if (landingPages.Any())
                {
                    //go through each landing page and add it to the list
                    foreach (var landingPage in landingPages)
                    {
                        //set the default landing page title
                        var landingPageTitle = landingPage.Name;
                        //check if we have the page title set on the current page
                        if (landingPage.HasProperty("pageTitle") && landingPage.HasValue("pageTitle"))
                        {
                            // set the page title to override the default
                            landingPageTitle = landingPage.GetProperty("pageTitle").Value().ToString();
                        }

                        //create the landing page item
                        var pageItemLink = new LinkItemModel
                        {
                            LinkTitle = landingPageTitle,
                            LinkUrl = landingPage.Url,
                            LinkPage = landingPage,
                            IsProductLinks = landingPage.ContentType.Alias == "productLandingPage"
                        };

                        //check if this landing page has got child pages which can be displayed in the menu
                        var landingPageChildren = landingPage.Children.Where(page => !page.Value<bool>("hideFromMenu")).ToList();
                        if (landingPageChildren.Any())
                        {
                            //set tht flag to indicate this has children
                            pageItemLink.HasChildLinks = true;

                            //go through each of the child links and add them to the item
                            foreach (var childPage in landingPageChildren)
                            {
                                //set the default child page title
                                var childPageTitle = childPage.Name;
                                //check if we have the page title set on the current page
                                if (childPage.HasProperty("pageTitle") && childPage.HasValue("pageTitle"))
                                {
                                    // set the page title to override the default
                                    childPageTitle = childPage.GetProperty("pageTitle").Value().ToString();
                                }

                                //create the child page item
                                var childPageLink = new LinkItemModel
                                {
                                    LinkTitle = childPageTitle,
                                    LinkUrl = childPage.Url,
                                    LinkPage = childPage
                                };

                                //check if the child link has got level 2 children for products
                                var childPageChildren = childPage.Children.Where(page => !page.Value<bool>("hideFromMenu")).ToList();
                                if (childPageChildren.Any())
                                {
                                    //set tht flag to indicate this has children
                                    childPageLink.HasChildLinks = true;


                                    //go through each of the child links and add them to the item
                                    foreach (var grandChildPage in childPageChildren)
                                    {
                                        //set the default child page title
                                        var grandChildPageTitle = grandChildPage.Name;
                                        //check if we have the page title set on the current page
                                        if (grandChildPage.HasProperty("pageTitle") && grandChildPage.HasValue("pageTitle"))
                                        {
                                            // set the page title to override the default
                                            grandChildPageTitle = grandChildPage.GetProperty("pageTitle").Value().ToString();
                                        }

                                        //create the child page item
                                        var grandChildPageLink = new LinkItemModel
                                        {
                                            LinkTitle = grandChildPageTitle,
                                            LinkUrl = grandChildPage.Url,
                                            LinkPage = grandChildPage
                                        };

                                        //add the grand child link to the child links children
                                        childPageLink.ChildLinkItems.Add(grandChildPageLink);
                                    }
                                }

                                //add this to the items child links
                                pageItemLink.ChildLinkItems.Add(childPageLink);
                            }
                        }
                        //after getting the children, add this item ot our menu links
                        menuItem.MenuLinks.Add(pageItemLink);
                    }
                }

                //get the feature products to display
                var featureProducts = _homePage.Descendants().Where(page => page.ContentType.Alias == "productPage"
                                                                            && !page.Value<bool>("hideFromMenu")
                                                                            && page.Value<bool>("featureProduct")
                                                                            && page.HasProperty("productImages")
                                                                            && page.HasValue("productImages")
                                                                            && page.IsPublished())
                                                                            .ToList();
                //check if we have any feature products
                if (featureProducts.Any())
                {
                    //order the features randomly
                    var r = new Random();
                    var menuFeatureProducts = featureProducts.OrderBy(x => r.Next()).ToList();
                    //create the mega menu items for the feature products
                    foreach (var featureProduct in menuFeatureProducts)
                    {
                        //set the default landing page title
                        var productTitle = featureProduct.Name;
                        //check if we have the page title set on the current page
                        if (featureProduct.HasProperty("pageTitle") && featureProduct.HasValue("pageTitle"))
                        {
                            // set the page title to override the default
                            productTitle = featureProduct.GetProperty("pageTitle").Value().ToString();
                        }

                        //create the landing page item
                        var featureProductLink = new LinkItemModel
                        {
                            LinkTitle = productTitle,
                            LinkUrl = featureProduct.Url,
                            LinkPage = featureProduct,
                            LinkImage = "/Images/NatureQuest-Logo-square.png",
                            ThumbLinkImage = "/Images/NatureQuest-Logo-square.png"
                        };

                        //get the price child items
                        var productPrices = featureProduct.Children().Where(page => page.ContentType.Alias == "productPrice").ToList();
                        if (productPrices.Any())
                        {
                            //if we have a featured price use that 1
                            var displayedPrice = productPrices.FirstOrDefault(price =>
                                price.HasProperty("featuredPrice")
                                && price.HasValue("featuredPrice")
                                && price.Value<bool>("featuredPrice"));

                            //if we don't have the featured price then just get the 1st 1
                            if (displayedPrice == null)
                            {
                                displayedPrice = productPrices.FirstOrDefault(price =>
                                    price.HasProperty("normalPrice")
                                    && price.HasValue("normalPrice")
                                    && price.Value<decimal>("normalPrice") != 0);
                            }

                            //go through the prices and add the
                            if (displayedPrice != null)
                            {
                                //set the default price
                                decimal productOriginalPrice = 0;
                                //check if we have a price set
                                if (displayedPrice.HasProperty("normalPrice") && displayedPrice.HasValue("normalPrice"))
                                {
                                    // set the product price
                                    productOriginalPrice = displayedPrice.Value<decimal>("normalPrice");
                                }

                                //set the default sale price
                                decimal productSalePrice = 0;
                                //check if we have a sale price
                                if (displayedPrice.HasProperty("salePrice") && displayedPrice.HasValue("salePrice"))
                                {
                                    // set the sale price
                                    productSalePrice = displayedPrice.Value<decimal>("salePrice");
                                }

                                //set the variant name
                                var priceVariant = displayedPrice.Name;
                                //check if we have the price variant set
                                if (displayedPrice.HasProperty("productVariant") &&
                                    displayedPrice.HasValue("productVariant"))
                                {
                                    // set the page title to override the default
                                    priceVariant = displayedPrice.GetProperty("productVariant").Value().ToString();
                                }

                                // get the flag ti indicate its a featured price
                                var isFeaturedPrice = false;
                                if (displayedPrice.HasProperty("featuredPrice") &&
                                    displayedPrice.HasValue("featuredPrice"))
                                {
                                    // set the page flag from the value
                                    isFeaturedPrice = displayedPrice.Value<bool>("featuredPrice");
                                }

                                // calculate the percentage
                                var salePercentage = 0;
                                if (productSalePrice > 0)
                                {
                                    salePercentage =
                                        100 - Convert.ToInt32(productSalePrice / productOriginalPrice * 100);
                                }

                                //create the new price model
                                var priceModel = new ProductPriceModel
                                {
                                    ProductPrice = productOriginalPrice,
                                    SalePrice = productSalePrice,
                                    ProductVariant = priceVariant,
                                    IsFeaturedPrice = isFeaturedPrice,
                                    SalePercentage = salePercentage
                                };

                                // add the price to the model
                                featureProductLink.ProductPrice = priceModel;
                            }
                        }

                        //set feature product image
                        var productImages = featureProduct.Value<IEnumerable<IPublishedContent>>("productImages").ToList();
                        if (productImages.Any())
                        {
                            var firstImage = productImages.FirstOrDefault();
                            if (firstImage != null && firstImage.Id > 0)
                            {
                                //get the normal image size
                                var defaultCropSize = firstImage.GetCropUrl("product");
                                var normaImagelink = !string.IsNullOrEmpty(defaultCropSize) ?
                                    defaultCropSize :
                                    firstImage.GetCropUrl(1000, 670);
                                if (!string.IsNullOrWhiteSpace(normaImagelink))
                                {
                                    featureProductLink.LinkImage = normaImagelink;
                                }
                                //get the thumbnail image
                                var thumbCropSize = firstImage.GetCropUrl("thumbNail");
                                var thumbImagelink = !string.IsNullOrEmpty(thumbCropSize) ?
                                    thumbCropSize :
                                    firstImage.GetCropUrl(250, 250);
                                if (!string.IsNullOrWhiteSpace(thumbImagelink))
                                {
                                    featureProductLink.ThumbLinkImage = thumbImagelink;
                                }
                            }
                        }

                        //add the feature link to the model
                        menuItem.FeaturedProductsLinks.Add(featureProductLink);
                    }

                }

                //get the category products to display
                menuItem.CategoryProductsLinks = _productsService.ProductCategoryLinks();

            }

            //check if we have the site details page for the footer
            if (_globalDetailsPage.Id > 0)
            {
                //get the opening hours if set
                if (_globalDetailsPage.HasProperty("openingHours") && _globalDetailsPage.HasValue("openingHours"))
                {
                    //set the list of opening hours
                    menuItem.OpeningHours = _globalDetailsPage.Value<string[]>("openingHours");
                }

                //get the footer links
                if (_globalDetailsPage.HasProperty("footerLinks") && _globalDetailsPage.HasValue("footerLinks"))
                {
                    //set the list of pages to use for the footer links
                    var footerPages = _globalDetailsPage.Value<List<IPublishedContent>>("footerLinks");
                    if (footerPages.Any())
                    {
                        foreach (var footerPage in footerPages)
                        {
                            //set the default landing page title
                            var footerPageTitle = footerPage.Name;
                            //check if we have the page title set on the current page
                            if (footerPage.HasProperty("pageTitle") && footerPage.HasValue("pageTitle"))
                            {
                                // set the page title to override the default
                                footerPageTitle = footerPage.GetProperty("pageTitle").Value().ToString();
                            }

                            //create the landing page item
                            var footerLink = new LinkItemModel
                            {
                                LinkTitle = footerPageTitle,
                                LinkUrl = footerPage.Url,
                                LinkPage = footerPage
                            };
                            //add the link to the footer links
                            menuItem.FooterLinks.Add(footerLink);
                        }
                    }
                }

                //get the opening hours if set
                if (_globalDetailsPage.HasProperty("subscribeText") && _globalDetailsPage.HasValue("subscribeText"))
                {
                    //set the list of opening hours
                    menuItem.SubscribeText = _globalDetailsPage.Value<string>("subscribeText");
                }
            }
            //return the view model with the menu items added
            return model;
        }

        public StandardPageViewModel PreparePageContent(StandardPageViewModel model)
        {
            //get the breadcrumb parents
            var pageAncestors = model.CurrentPage.Ancestors().OrderBy(page => page.Level).ToList();
            //get the breadcrumb links
            if (pageAncestors.Any())
            {
                foreach (var ancestor in pageAncestors)
                {
                    //get the page title
                    var pageTitle = ancestor.Name;
                    //check if we have the page title set on the current page
                    if (ancestor.HasProperty("pageTitle") && ancestor.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        pageTitle = ancestor.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the breadcrumb link
                    var breadcrumbLink = new LinkItemModel
                    {
                        LinkTitle = ancestor.Id == _homePage.Id ? "Home" : pageTitle,
                        LinkUrl = ancestor.Url,
                        LinkPage = ancestor
                    };
                    //add the link to the model
                    model.BreadCrumbLinks.Add(breadcrumbLink);
                }
            }

            //get the page image
            if (model.CurrentPage.HasProperty("bannerImage") && model.CurrentPage.HasValue("bannerImage"))
            {
                //set the global site name
                var mediaItem = model.CurrentPage.Value<IPublishedContent>("bannerImage");
                if (mediaItem != null && mediaItem.Id > 0)
                {
                    var defaultCropSize = mediaItem.GetCropUrl("pageHeader");
                    var headerImagelink = !string.IsNullOrEmpty(defaultCropSize) ?
                        defaultCropSize :
                        mediaItem.GetCropUrl(900, 420);
                    if (!string.IsNullOrWhiteSpace(headerImagelink))
                    {
                        model.PageHeaderImage =  headerImagelink;
                    }
                }
            }

            //get the page html text
            if (model.CurrentPage.HasProperty("pageText") && model.CurrentPage.HasValue("pageText"))
            {
                model.PageContentText = model.CurrentPage.GetProperty("pageText").Value().ToString();
            }

            //return the model with the page content
                return model;
        }

        #endregion
    }
}