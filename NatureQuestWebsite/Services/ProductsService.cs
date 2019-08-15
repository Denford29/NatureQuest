using NatureQuestWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// create the product service class to use for product related calls
    /// </summary>
    public class ProductsService : IProductsService
    {
        /// <summary>
        /// create the private classes to use
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// set the home page
        /// </summary>
        private readonly IPublishedContent _homePage;

        /// <summary>
        /// initialise the service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="contextFactory"></param>
        public ProductsService(
            ILogger logger,
            IUmbracoContextFactory contextFactory
        )
        {
            //set the local classes
            _logger = logger;

            //get the context to use
            using (var contextReference = contextFactory.EnsureUmbracoContext())
            {
                var contentCache = contextReference.UmbracoContext.ContentCache;
                var homePage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
                //check if we have the home page and set it to the global page
                if (homePage?.Id > 0)
                {
                    _homePage = homePage;
                }
                //if we can get the home page log an error
                else
                {
                    _logger.Info(Type.GetType("ProductsService"), "Cant get homepage to use");
                }
            }
        }

        /// <summary>
        /// get the product model from an umbraco page
        /// </summary>
        /// <param name="productPage"></param>
        /// <returns></returns>
        public ProductModel GetProductModel(IPublishedContent productPage)
        {
            try
            {
                //create the default model to use
                var model = new ProductModel(productPage);
                //check if we can get the productPage from the homepage descendants
                if (_homePage?.Id > 0 &&
                    _homePage.Descendants().FirstOrDefault(page => page.Id == productPage.Id) != null)
                {
                    //get the properties from the product page and set them to the model
                    model.ProductPage = productPage;

                    //set the default product page title
                    var productTitle = productPage.Name;
                    //check if we have the page title set on the current page
                    if (productPage.HasProperty("pageTitle") && productPage.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        productTitle = productPage.GetProperty("pageTitle").Value().ToString();
                    }
                    //set the model title
                    model.ProductTitle = productTitle;

                    //set feature product image
                    var productImages = productPage.Value<IEnumerable<IPublishedContent>>("productImages").ToList();
                    if (productImages.Any())
                    {
                        //go through each of the images and add them
                        foreach (var productImage in productImages)
                        {
                            if (productImage != null && productImage.Id > 0)
                            {
                                //get the image url
                                var imageLink = "/Images/Nature-Quest-Product-Default.png";
                                var defaultCropSize = productImage.GetCropUrl("product");
                                var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                    ? defaultCropSize
                                    : productImage.GetCropUrl(1000, 670);
                                if (!string.IsNullOrWhiteSpace(productImagelink))
                                {
                                    imageLink = productImagelink;
                                }

                                //create the product model
                                var imageModel = new ProductImageModel
                                {
                                    ImageUrl = imageLink,
                                    ImageAltText = productImage.Name
                                };
                                //add the image model to the model
                                model.ProductImages.Add(imageModel);
                            }
                        }
                    }
                    //add a default image model 
                    else
                    {
                        var imageLink = "/Images/Nature-Quest-Product-Default.png";
                        //create the product model
                        var imageModel = new ProductImageModel
                        {
                            ImageUrl = imageLink,
                            ImageAltText = productTitle
                        };
                        //add the image model to the model
                        model.ProductImages.Add(imageModel);
                    }

                    //get the price child items
                    var productPrices = productPage.Children().Where(page => page.ContentType.Alias == "productPrice").ToList();
                    if (productPrices.Any())
                    {
                        //go through the prices and add the
                        foreach (var productPrice in productPrices)
                        {
                            //set the default price
                            decimal productOriginalPrice = 0;
                            //check if we have a price set
                            if (productPrice.HasProperty("normalPrice") && productPrice.HasValue("normalPrice"))
                            {
                                // set the product price
                                productOriginalPrice = productPrice.Value<decimal>("normalPrice");
                            }

                            //set the default sale price
                            decimal productSalePrice = 0;
                            //check if we have a sale price
                            if (productPrice.HasProperty("salePrice") && productPrice.HasValue("salePrice"))
                            {
                                // set the sale price
                                productSalePrice = productPrice.Value<decimal>("salePrice");
                            }

                            //set the variant name
                            var priceVariant = productPrice.Name;
                            //check if we have the price variant set
                            if (productPrice.HasProperty("productVariant") && productPrice.HasValue("productVariant"))
                            {
                                // set the page title to override the default
                                priceVariant = productPrice.GetProperty("productVariant").Value().ToString();
                            }

                            // get the flag ti indicate its a featured price
                            var isFeaturedPrice = false;
                            if (productPrice.HasProperty("featuredPrice") && productPrice.HasValue("featuredPrice"))
                            {
                                // set the page flag from the value
                                isFeaturedPrice = productPrice.Value<bool>("featuredPrice");
                            }

                            // calculate the percentage
                            var salePercentage = 0;
                            if (productSalePrice > 0)
                            {
                                salePercentage = 100 - Convert.ToInt32(productSalePrice / productOriginalPrice * 100);
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
                            model.ProductPrices.Add(priceModel);
                        }
                    }
                }

                //return the model
                return model;
            }
            catch (Exception ex)
            {
                _logger.Error(Type.GetType("ProductsService"), ex, "Error getting product model");
                return null;
            }

        }
    }
}