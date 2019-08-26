﻿using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace NatureQuestWebsite.Controllers
{
    /// <summary>
    /// create the general content controller
    /// </summary>
    public class PageContentController : SurfaceController
    {
        /// <summary>
        /// create the default site name string
        /// </summary>
        private readonly string _siteName;

        /// <summary>
        /// set the global details page
        /// </summary>
        private readonly IPublishedContent _globalDetailsPage;

        /// <summary>
        /// set the global details page
        /// </summary>
        private readonly IPublishedContent _slidersParentPage;

        /// <summary>
        /// set the home page
        /// </summary>
        private readonly IPublishedContent _homePage;

        /// <summary>
        /// get the products service
        /// </summary>
        private readonly IProductsService _productsService;

        /// <summary>
        /// get the location service to use
        /// </summary>
        private readonly ILocationService _locationService;

        /// <summary>
        /// initialise the controller
        /// </summary>
        public PageContentController(
            IProductsService productsService,
            ILocationService locationService)
        {
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
                //get the slider details page
                var sliderParentsPage = siteSetting.Children().FirstOrDefault(child => child.ContentType.Alias == "sliderItemsContainer");
                if (sliderParentsPage?.Id > 0)
                {
                    //save the slider details page to use later
                    _slidersParentPage = sliderParentsPage;
                }

                //get the site details page
                var siteDetailsPage = siteSetting.Children.FirstOrDefault(child => child.ContentType.Alias == "globalDetails");
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
                }
            }

            //get the local product service to use
            _productsService = productsService;

            //get the local location service to use
            _locationService = locationService;
        }

        /// <summary>
        /// Get the home page slider
        /// </summary>
        /// <returns></returns>
        public ActionResult GetHomeSlider()
        {
            // create the default model to return
            var model = new SliderItemModel(CurrentPage);

            //check if we have the slider parent page
            if (_slidersParentPage?.Id > 0)
            {
                //get he slider items pages
                var sliderItemPages = _slidersParentPage.Descendants("sliderItem").
                                                                                            Where(page => page.IsPublished()
                                                                                            && page.HasProperty("sliderImage")
                                                                                             && page.HasValue("sliderImage"))
                                                                                            .ToList();
                //if we have some slider items, then add them to the model
                if (sliderItemPages.Any())
                {
                    foreach (var sliderItem in sliderItemPages)
                    {
                        //create the new slider item
                        var slider = new SliderItem();

                        //set the default slider heading
                        var sliderHeading = sliderItem.Name;
                        //check if we have the slider heading set
                        if (sliderItem.HasProperty("sliderHeading") && sliderItem.HasValue("sliderHeading"))
                        {
                            // set the slider heading to override the default
                            sliderHeading = sliderItem.GetProperty("sliderHeading").Value().ToString();
                        }
                        //add the heading
                        slider.SliderHeading = sliderHeading;

                        //check if we have the slider text set
                        if (sliderItem.HasProperty("sliderText") && sliderItem.HasValue("sliderText"))
                        {
                            // set the slider heading to override the default
                            slider.SliderText = sliderItem.GetProperty("sliderText").Value().ToString();
                        }

                        //set slider image
                        var sliderImage = sliderItem.Value<IPublishedContent>("sliderImage");
                        if (sliderImage?.Id > 0)
                        {
                            var defaultCropSize = sliderImage.GetCropUrl("product");
                            var sliderImageUrl = !string.IsNullOrEmpty(defaultCropSize) ?
                                defaultCropSize :
                                sliderImage.GetCropUrl(1000, 670);
                            if (!string.IsNullOrWhiteSpace(sliderImageUrl))
                            {
                                slider.SliderImage = sliderImageUrl;
                            }
                        }

                        //set slider link page and url
                        var sliderLinkPage = sliderItem.Value<IPublishedContent>("sliderLinkPage");
                        if (sliderLinkPage?.Id > 0)
                        {
                            slider.SliderLinkPage = sliderLinkPage;
                            slider.SliderUrl = sliderLinkPage.Url;
                        }

                        //add the slider item to the list
                        model.SliderItems.Add(slider);
                    }
                }
            }

            //return the view with the model
            return View("/Views/Partials/Global/ProductsSlider.cshtml", model);
        }

        /// <summary>
        /// Get the products for the feature products slider
        /// </summary>
        /// <returns></returns>
        public ActionResult GetFeatureProductsSlider()
        {
            //create the default model for feature products
            var model = new List<ProductModel>();

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
                //go through each of the products and get the product model for each
                foreach (var featureProduct in featureProducts)
                {
                    //get the feature model
                    var featureProductModel = _productsService.GetProductModel(featureProduct);

                    //add it to the feature model
                    model.Add(featureProductModel);
                }
            }

            //return the view with the model
            return View("/Views/Partials/Global/FeatureProductsSlider.cshtml", model);
        }

        /// <summary>
        /// get the product categories
        /// </summary>
        /// <returns></returns>
        public ActionResult GetProductCategories()
        {
            // create the default model
            var model = new ProductCategoriesModel();

            //get the feature products to display
            var productCategories = _homePage.Descendants().Where(page => page.ContentType.Alias == "productCategoryPage"
                                                                        && !page.Value<bool>("hideFromMenu")
                                                                        && page.IsPublished())
                .ToList();

            //add each category to the model
            if (productCategories.Any())
            {
                foreach (var productCategory in productCategories)
                {
                    //set the default category page title
                    var categoryTitle = productCategory.Name;
                    //check if we have the page title set on the current page
                    if (productCategory.HasProperty("pageTitle") && productCategory.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        categoryTitle = productCategory.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the default category model
                    var categoryModel = new ProductCategory
                    {
                        CategoryLinkTitle = categoryTitle,
                        CategoryLinkUrl = productCategory.Url,
                        ProductCategoryPage = productCategory
                    };

                    //get the products for each category to add to the model
                    var categoryProducts = productCategory.Children().Where(page => page.ContentType.Alias == "productPage"
                                                                                && !page.Value<bool>("hideFromMenu")
                                                                                && page.IsPublished())
                        .ToList();

                    //if we have some products, take 4 random ones
                    if (categoryProducts.Any())
                    {
                        var r = new Random();
                        var modelProducts = categoryProducts.OrderBy(x => r.Next()).Take(4).ToList();
                        //get the model for each of the products
                        foreach (var product in modelProducts)
                        {
                            var productModel = _productsService.GetProductModel(product);
                            //add it to the category model
                            categoryModel.CategoriesProducts.Add(productModel);
                        }
                    }

                    //add the category model to the view model
                    model.ProductCategories.Add(categoryModel);
                }
            }

            //return the view with the model
            return View("/Views/Partials/Global/ProductCategories.cshtml", model);
        }

        /// <summary>
        /// get the page services
        /// </summary>
        /// <returns></returns>
        public ActionResult GetPageServices()
        {
            //return the view with the global page to get the services from
            return View("/Views/Partials/Global/PageServices.cshtml", _globalDetailsPage);
        }

        /// <summary>
        /// get the stockist pages with logos
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStockistSliders()
        {
            //create the default model
            var displayModel = new List<LinkItemModel>();
            //get the stockist pages with logo images only
            var stockistPages = _homePage.Descendants().Where(page => page.ContentType.Alias == "stockistPage"
                                                                        && !page.Value<bool>("hideFromMenu")
                                                                        && page.HasProperty("stockistLogo")
                                                                        && page.HasValue("stockistLogo")
                                                                        && page.IsPublished())
                                                                    .ToList();

            //if we have any stockist then add them to the model
            if (stockistPages.Any())
            {
                //order the stockist randomly
                var r = new Random();
                var orderedStockists = stockistPages.OrderBy(x => r.Next()).ToList();
                //create the menu item links for the slider
                foreach (var stockist in orderedStockists)
                {
                    //set the default stockist page title
                    var stockistName = stockist.Name;
                    //check if we have the page title set on the current page
                    if (stockist.HasProperty("pageTitle") && stockist.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        stockistName = stockist.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the stockist page item
                    var stockistLink = new LinkItemModel
                    {
                        LinkTitle = stockistName,
                        LinkUrl = stockist.Url,
                        LinkPage = stockist,
                        ThumbLinkImage = "/Images/NatureQuest-Logo-square.png"
                    };

                    //set feature product image
                    var stockistLogo = stockist.Value<IPublishedContent>("stockistLogo");
                    if (stockistLogo != null && stockistLogo.Id > 0)
                    {
                        var defaultCropSize = stockistLogo.GetCropUrl("thumbNail");
                        var logoImage = !string.IsNullOrEmpty(defaultCropSize) ?
                            defaultCropSize :
                            stockistLogo.GetCropUrl(250, 250);
                        if (!string.IsNullOrWhiteSpace(logoImage))
                        {
                            stockistLink.ThumbLinkImage = logoImage;
                        }
                    }

                    //add the menu link to the model
                    displayModel.Add(stockistLink);
                }
            }

            return View("/Views/Partials/Global/StockistSlider.cshtml", displayModel);
        }

        /// <summary>
        /// get the stockist list to display
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStockistListView()
        {
            //create the new model to use
            var model = new StockistListModel
            {
                PageTitle = CurrentPage.Name
            };

            //get the page title
            if (CurrentPage.HasProperty("pageTitle") && CurrentPage.HasValue("pageTitle"))
            {
                model.PageTitle = CurrentPage.GetProperty("pageTitle").Value().ToString();
            }

            //get the page html text
            if (CurrentPage.HasProperty("pageText") && CurrentPage.HasValue("pageText"))
            {
                model.PageContentText = CurrentPage.GetProperty("pageText").Value().ToString();
            }

            //create the states list
            var statePages = new List<IPublishedContent>();

            //check if the current page is the stockist landing page
            if (CurrentPage.ContentType.Alias == "stockistLandingPage")
            {
                //set the landing flag
                model.IsStockistLanding = true;

                //get the state page
                statePages = CurrentPage.Children.Where(page =>
                    !page.Value<bool>("hideFromMenu")
                    && page.ContentType.Alias == "stockistRegionPage"
                    && page.IsPublished()).ToList();
            }
            //if we are on a state page
            else if (CurrentPage.ContentType.Alias == "stockistRegionPage")
            {
                //set the landing flag
                model.IsStateLanding = true;
                statePages.Add(CurrentPage);
            }

            //create the link models for the state pages
            if (statePages.Any())
            {
                foreach (var statePage in statePages)
                {
                    //set the default page title
                    var pageTitle = statePage.Name;
                    //check if we have the page title set on the current page
                    if (statePage.HasProperty("pageTitle") && statePage.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        pageTitle = statePage.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the landing page item
                    var stateLinkItem = new LinkItemModel
                    {
                        LinkTitle = pageTitle,
                        LinkUrl = statePage.Url,
                        LinkPage = statePage
                    };

                    //check if this state page has got stockist pages 
                    var stockistPages = statePage.Children.Where(page =>
                            !page.Value<bool>("hideFromMenu")
                            && page.ContentType.Alias == "stockistPage"
                            && page.HasProperty("stockistLogo")
                            && page.HasValue("stockistLogo")
                            && page.IsPublished())
                        .ToList();

                    //get the stockist links
                    if (stockistPages.Any())
                    {
                        foreach (var stockistPage in stockistPages)
                        {
                            stateLinkItem.HasChildLinks = true;
                            //set the default page title
                            var stockistPageTitle = stockistPage.Name;
                            //check if we have the page title set on the current page
                            if (stockistPage.HasProperty("pageTitle") && stockistPage.HasValue("pageTitle"))
                            {
                                // set the page title to override the default
                                stockistPageTitle = stockistPage.GetProperty("pageTitle").Value().ToString();
                            }

                            //create the landing page item
                            var stockistLinkItem = new LinkItemModel
                            {
                                LinkTitle = stockistPageTitle,
                                LinkUrl = stockistPage.Url,
                                LinkPage = stockistPage
                            };

                            //set stockist logo image
                            var stockistLogo = stockistPage.Value<IPublishedContent>("stockistLogo");
                            if (stockistLogo != null && stockistLogo.Id > 0)
                            {
                                var defaultCropSize = stockistLogo.GetCropUrl("thumbNail");
                                var logoImage = !string.IsNullOrEmpty(defaultCropSize) ?
                                    defaultCropSize :
                                    stockistLogo.GetCropUrl(250, 250);
                                if (!string.IsNullOrWhiteSpace(logoImage))
                                {
                                    stockistLinkItem.ThumbLinkImage = logoImage;
                                }
                            }
                            //add the stockist page to the state link
                            stateLinkItem.ChildLinkItems.Add(stockistLinkItem);
                        }
                    }

                    //add the stockist link item to the model
                    model.StockistLinks.Add(stateLinkItem);

                }
            }

            //get the locations
            model.StockistLocations.AddRange(GetPageLocations(CurrentPage));

            return View("/Views/Partials/Stockist/StockistsList.cshtml", model);
        }

        /// <summary>
        /// get the stockist details
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStockistDetailsView()
        {
            //create the default model
            var model = new StockistDetailsModel
            {
                PageTitle = CurrentPage.Name
            };

            //get the page title
            if (CurrentPage.HasProperty("pageTitle") && CurrentPage.HasValue("pageTitle"))
            {
                model.PageTitle = CurrentPage.GetProperty("pageTitle").Value().ToString();
            }

            //get the page html text
            if (CurrentPage.HasProperty("pageText") && CurrentPage.HasValue("pageText"))
            {
                model.PageContentText = CurrentPage.GetProperty("pageText").Value().ToString();
            }

            //get the stockist website
            if (CurrentPage.HasProperty("stockistWebsite") && CurrentPage.HasValue("stockistWebsite"))
            {
                model.StockistWebsite = CurrentPage.GetProperty("stockistWebsite").Value().ToString();
            }

            //get the stockist phone number
            if (CurrentPage.HasProperty("stockistPhoneNumber") && CurrentPage.HasValue("stockistPhoneNumber"))
            {
                model.StockistPhone = CurrentPage.GetProperty("stockistPhoneNumber").Value().ToString();
            }

            //get the page html text
            if (CurrentPage.HasProperty("stockistEmailAddress") && CurrentPage.HasValue("stockistEmailAddress"))
            {
                model.StockistEmail = CurrentPage.GetProperty("stockistEmailAddress").Value().ToString();
            }

            //set stockist logo image
            var stockistLogo = CurrentPage.Value<IPublishedContent>("stockistLogo");
            if (stockistLogo != null && stockistLogo.Id > 0)
            {
                var defaultCropSize = stockistLogo.GetCropUrl("thumbNail");
                var logoImage = !string.IsNullOrEmpty(defaultCropSize) ?
                    defaultCropSize :
                    stockistLogo.GetCropUrl(250, 250);
                if (!string.IsNullOrWhiteSpace(logoImage))
                {
                    model.StockistLogo = logoImage;
                }
            }

            //get the locations
            model.StockistLocations.AddRange(GetPageLocations(CurrentPage));

            return View("/Views/Partials/Stockist/StockistDetails.cshtml", model);
        }

        /// <summary>
        /// get the locations for published page
        /// </summary>
        /// <param name="locationsParent"></param>
        /// <returns></returns>
        public List<LocationModel> GetPageLocations(IPublishedContent locationsParent)
        {
            //create the default locations
            var pageLocations = new List<LocationModel>();

            //check if the location page has got location address descendants
            var locationPages = locationsParent.Descendants().Where(page =>
                                                !page.Value<bool>("hideFromMenu")
                                                && page.ContentType.Alias == "locationAddress"
                                                && page.HasProperty("lat")
                                                && page.HasValue("lat")
                                                && page.HasProperty("long")
                                                && page.HasValue("long")
                                                && page.IsPublished())
                                            .ToList();

            //if we have the locations, send each one to the service to get the location models
            if (locationPages.Any())
            {
                foreach (var locationPage in locationPages)
                {
                    var locationModel = _locationService.GetPageLocationDetails(locationPage);
                    //check if the location model has the lat and long set
                    if (!string.IsNullOrWhiteSpace(locationModel?.Lat) && 
                        !string.IsNullOrWhiteSpace(locationModel.Long))
                    {
                        pageLocations.Add(locationModel);
                    }
                }
            }

            //return the list of locations
            return pageLocations;
        }

        /// <summary>
        /// get the product details view with the model
        /// </summary>
        /// <returns></returns>
        public ActionResult GetProductDisplayModel()
        {
            //check if the current page is a product
            if (CurrentPage.ContentType.Alias == "productPage" && !CurrentPage.Value<bool>("hideFromMenu"))
            {
                var model = _productsService.GetProductModel(CurrentPage);

                //return the view with the model
                return View("/Views/Partials/Products/ProductDetails.cshtml", model);
            }

            //if the request is not from a visible product page
            return HttpNotFound();
        }

        /// <summary>
        /// get the product page's related products
        /// </summary>
        /// <returns></returns>
        public ActionResult GeRelatedProducts()
        {
            //check if the current page is a product
            if (CurrentPage.ContentType.Alias == "productPage" && !CurrentPage.Value<bool>("hideFromMenu"))
            {
                var model = _productsService.GetProductModel(CurrentPage);

                //get the products parent page
                var productCategory = CurrentPage.Parent;
                //check if the parent has got products to add as related products
                if (productCategory.Children.FirstOrDefault(
                        product => product.ContentType.Alias == "productPage") != null)
                {
                    var relatedProducts = productCategory.Children.Where(
                            product => product.ContentType.Alias == "productPage"
                                       && !product.Value<bool>("hideFromMenu")
                                       && product.Id != CurrentPage.Id)
                        .ToList();
                    // get a model for each of related products
                    foreach (var relatedProduct in relatedProducts)
                    {
                        var relatedProductModel = _productsService.GetProductModel(relatedProduct);
                        if (!string.IsNullOrWhiteSpace(relatedProductModel?.ProductTitle))
                        {
                            model.RelatedProducts.Add(relatedProductModel);
                        }
                    }
                }

                //return the view with the model
                return View("/Views/Partials/Products/RelatedProducts.cshtml", model);
            }
            //if the request is not from a visible product page
            return HttpNotFound();
        }

        /// <summary>
        /// get the list of products for a product listing page
        /// </summary>
        /// <returns></returns>
        public ActionResult GetProductsList()
        {
            //check if the current page is a product
            if ((CurrentPage.ContentType.Alias == "productLandingPage" 
                 || CurrentPage.ContentType.Alias == "productCategoryPage")
                && !CurrentPage.Value<bool>("hideFromMenu"))
            {
                //return the view with the model
                return View("/Views/Partials/Products/ProductsList.cshtml");
            }
            //if the request is not from a visible product landing or category page return a 404
            return HttpNotFound();
        }

    }
}