﻿using NatureQuestWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
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
        /// <param name="getThumbnail"></param>
        /// <param name="featurePriceId"></param>
        /// <returns></returns>
        public ProductModel GetProductModel(IPublishedContent productPage, bool getThumbnail = false, string featurePriceId = "")
        {
            try
            {
                //create the default model to use
                var model = new ProductModel();
                //check if we can get the productPage from the homepage descendants
                if (_homePage?.Id > 0 &&
                    _homePage.Descendants().FirstOrDefault(page => page.Id == productPage.Id) != null)
                {
                    //get the properties from the product page and set them to the model
                    model.ProductPage = productPage;
                    model.ProductPageId = productPage.Id;

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

                    //check if we have the product code set on the current product
                    if (productPage.HasProperty("productCode") && productPage.HasValue("productCode"))
                    {
                        // save it to the model
                        model.ProductCode = productPage.GetProperty("productCode").Value().ToString();
                    }

                    //check if we have the product code set on the current product
                    if (productPage.HasProperty("productDescription") && productPage.HasValue("productDescription"))
                    {
                        // save it to the model
                        model.ProductDescription = productPage.GetProperty("productDescription").Value().ToString();
                    }

                    //check if we have the product star rating set on the current product
                    if (productPage.HasProperty("productStarRating") && productPage.HasValue("productStarRating"))
                    {
                        // save it to the model
                        model.ProductStarRating = productPage.Value<int>("productStarRating");
                    }

                    //check if we have the product ingredients set on the current product
                    if (productPage.HasProperty("productIngredients") && productPage.HasValue("productIngredients"))
                    {
                        // save it to the model
                        model.ProductIngredients = productPage.Value<string[]>("productIngredients");
                    }

                    //check if we have the product code set on the current product
                    if (productPage.HasProperty("pageText") && productPage.HasValue("pageText"))
                    {
                        // save it to the model
                        model.PageContentText = productPage.GetProperty("pageText").Value().ToString();
                    }

                    //add the default price
                    var defaultPrice = new SelectListItem
                    {
                        Text = "Select size require.",
                        Value = "",
                        Selected = string.IsNullOrWhiteSpace(featurePriceId)
                    };
                    model.ProductDisplayPrices.Add(defaultPrice);

                    //get the price child items
                    var productPrices = productPage.Children().Where(page =>
                                                            page.ContentType.Alias == "productPrice"
                                                            && page.IsPublished()).
                                                            ToList();
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
                            // set a flag for the sale price
                            var isSalePrice = false;
                            //check if we have a sale price
                            if (productPrice.HasProperty("salePrice") && productPrice.HasValue("salePrice"))
                            {
                                // set the sale price
                                productSalePrice = productPrice.Value<decimal>("salePrice");
                                if(productSalePrice >0)
                                {
                                    isSalePrice = true;
                                }
                            }

                            //set the variant name
                            var priceVariant = productPrice.Name;
                            //check if we have the price variant set
                            if (productPrice.HasProperty("productVariant") && productPrice.HasValue("productVariant"))
                            {
                                // set the page title to override the default
                                priceVariant = productPrice.GetProperty("productVariant").Value().ToString();
                            }

                            // get the flag to indicate its a featured price
                            var isFeaturedPrice = false;
                            //if there is a feature price id passed in use that to set the price marked as feature price
                            if (!string.IsNullOrWhiteSpace(featurePriceId))
                            {
                                isFeaturedPrice = productPrice.Id.ToString() == featurePriceId;
                            }
                            else
                            {
                                if (productPrice.HasProperty("featuredPrice") && productPrice.HasValue("featuredPrice"))
                                {
                                    // set the page flag from the value
                                    isFeaturedPrice = productPrice.Value<bool>("featuredPrice");
                                }
                            }
                            

                            var productCode = string.Empty;
                            //check if we have the product code set on the current product
                            if (productPrice.HasProperty("productCode") && productPrice.HasValue("productCode"))
                            {
                                // save it to the model
                                productCode = productPrice.GetProperty("productCode").Value().ToString();
                            }

                            // calculate the percentage
                            var salePercentage = 0;
                            if (productSalePrice > 0)
                            {
                                salePercentage = 100 - Convert.ToInt32(productSalePrice / productOriginalPrice * 100);
                            }

                            //set the displayed details
                            var selectPrice = productSalePrice != 0 ? productSalePrice : productOriginalPrice;
                            var priceDisplayed = $"{priceVariant} - {selectPrice}";
                            var productDisplayPrice = new SelectListItem
                            {
                                Text = priceDisplayed,
                                Value = productPrice.Id.ToString(),
                                Selected = !string.IsNullOrWhiteSpace(featurePriceId) && productPrice.Id.ToString() == featurePriceId
                            };
                            model.ProductDisplayPrices.Add(productDisplayPrice);


                            //create the new price model
                            var priceModel = new ProductPriceModel
                            {
                                ProductPrice = productOriginalPrice,
                                ProductPricePage = productPrice,
                                SalePrice = productSalePrice,
                                ProductVariant = priceVariant,
                                IsFeaturedPrice = isFeaturedPrice,
                                SalePercentage = salePercentage,
                                ProductVariantCode = productCode,
                                IsSalePrice = isSalePrice
                            };

                            //get the variant image if there is an image set
                            if (productPrice.HasProperty("variantImage") && productPrice.HasValue("variantImage"))
                            {
                                //set feature product image
                                var productVariantImage = productPrice.Value<IPublishedContent>("variantImage");
                                if (productVariantImage?.Id > 0)
                                {
                                    //get the image url
                                    var imageLink = "/Images/Nature-Quest-Product-Default.png";
                                    var defaultCropSize = getThumbnail
                                        ? productVariantImage.GetCropUrl("thumbNail")
                                        : productVariantImage.GetCropUrl("product");
                                    var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                        ? defaultCropSize
                                        : getThumbnail
                                            ? productVariantImage.GetCropUrl(250, 250)
                                            : productVariantImage.GetCropUrl(350, 500);
                                    if (!string.IsNullOrWhiteSpace(productImagelink))
                                    {
                                        imageLink = productImagelink;
                                    }

                                    //create the product model
                                    var variantImageModel = new ProductImageModel
                                    {
                                        ImageUrl = imageLink,
                                        ImageAltText = productVariantImage.Name,
                                        ImageProductId = productPrice.Id.ToString(),
                                        IsFeaturedPriceImage = isFeaturedPrice
                                    };
                                    //add the image model to the product images
                                    model.ProductImages.Add(variantImageModel);
                                    //add the image to the variant image
                                    priceModel.ProductVariantImage = variantImageModel;
                                }
                            }
                            //add a default image model 
                            else
                            {
                                var imageLink = getThumbnail ? "/Images/Nature-Quest-Product-Default-thumb.png" : "/Images/Nature-Quest-Product-Default.png";
                                //create the product model
                                var variantImageModel = new ProductImageModel
                                {
                                    ImageUrl = imageLink,
                                    ImageAltText = productTitle,
                                    ImageProductId = productPrice.Id.ToString()
                                };
                                //add the default image to the variant image
                                priceModel.ProductVariantImage = variantImageModel;
                            }

                            // add the price to the model
                            model.ProductPrices.Add(priceModel);
                        }
                    }

                    //if we have a feature price id, use that
                    if (!string.IsNullOrWhiteSpace(featurePriceId))
                    {
                        //from the prices get the featured price to show
                        model.FeaturedPrice = model.ProductPrices.FirstOrDefault(price => price.ProductPricePage.Id.ToString() == featurePriceId) ??
                                              model.ProductPrices.FirstOrDefault();
                    }
                    else
                    {
                        //from the prices get the featured price to show
                        //model.FeaturedPrice = model.ProductPrices.FirstOrDefault(price => price.IsFeaturedPrice) ??
                        //                      model.ProductPrices.FirstOrDefault();
                        //set the feature price as the 1st product
                        model.FeaturedPrice = model.ProductPrices.FirstOrDefault();
                    }

                    //get the 1st sale price
                    var setSalePrice = model.ProductPrices.FirstOrDefault(price => price.IsSalePrice);
                    if (setSalePrice != null)
                    {
                        model.SalePrice = setSalePrice;
                    }

                    //check if this product i valid for ordering
                    model.CanBeOrdered = model.FeaturedPrice != null;
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

        /// <summary>
        /// get the list of product categories as links
        /// </summary>
        /// <returns></returns>
        public List<LinkItemModel> ProductCategoryLinks()
        {
            //create the default list to return
            var categoriesList = new List<LinkItemModel>();

            //get the category products to display
            var categoryProducts = _homePage.Descendants().Where(page => page.ContentType.Alias == "productCategoryPage"
                                                                         && page.IsPublished()
                                                                         && !page.Value<bool>("hideFromMenu"))
                                                                         .ToList();
            //check if we have the category pages
            if (categoryProducts.Any())
            {
                //go through each category page and add it to the list
                foreach (var categoryPage in categoryProducts)
                {
                    //set the default category page title
                    var categoryPageTitle = categoryPage.Name;
                    //check if we have the page title set on the current page
                    if (categoryPage.HasProperty("pageTitle") && categoryPage.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        categoryPageTitle = categoryPage.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the category page item
                    var categoryItemLink = new LinkItemModel
                    {
                        LinkTitle = categoryPageTitle,
                        LinkUrl = categoryPage.Url,
                        LinkPage = categoryPage
                    };

                    //check if this category page has got child pages which can be displayed in the menu
                    var categoryPageChildren = categoryPage.Children.Where(page => !page.Value<bool>("hideFromMenu")).ToList();
                    //go through each of the product pages and add them to the item
                    foreach (var productPage in categoryPageChildren)
                    {
                        //set the default product page title
                        var productPageTitle = productPage.Name;
                        //check if we have the page title set on the current page
                        if (productPage.HasProperty("pageTitle") && productPage.HasValue("pageTitle"))
                        {
                            // set the page title to override the default
                            productPageTitle = productPage.GetProperty("pageTitle").Value().ToString();
                        }

                        //create the child page item
                        var productPageLink = new LinkItemModel
                        {
                            LinkTitle = productPageTitle,
                            LinkUrl = productPage.Url,
                            LinkPage = productPage
                        };

                        //add the product link to the category
                        categoryItemLink.ChildLinkItems.Add(productPageLink);
                    }

                    //add the category link to the menu
                    categoriesList.Add(categoryItemLink);
                }
            }

            //return the list
            return categoriesList;
        }

        /// <summary>
        /// get the product categories
        /// </summary>
        /// <returns></returns>
        public List<ProductCategory> ProductCategories(bool includeSpecials = false)
        {
            //create the default list to return
            var productCategories = new List<ProductCategory>();

            //if we need to include specials add theme here first
            if (includeSpecials)
            {
                //get the specials page
                var productSpecialsPage = _homePage.Descendants().
                    FirstOrDefault(page =>
                    page.ContentType.Alias == "productSpecialsPage"
                    && !page.Value<bool>("hideFromMenu")
                    && page.IsPublished());

                //check the specials page and get the products set as featured/specials
                if (productSpecialsPage?.Id > 0)
                {
                    //set the default specials category page title
                    var specialsTitle = productSpecialsPage.Name;
                    //check if we have the page title set on the current page
                    if (productSpecialsPage.HasProperty("pageTitle") && productSpecialsPage.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        specialsTitle = productSpecialsPage.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the default specials category model
                    var specialsCategoryModel = new ProductCategory
                    {
                        CategoryLinkTitle = specialsTitle,
                        CategoryLinkUrl = productSpecialsPage.Url,
                        ProductCategoryPage = productSpecialsPage
                    };

                    //get the product specials
                    var specialsProducts = _homePage.Descendants().Where(page =>
                            page.ContentType.Alias == "productPage"
                            && !page.Value<bool>("hideFromMenu")
                            && page.Value<bool>("featureProduct"))
                        .ToList();

                    //add the specials products
                    if (specialsProducts.Any())
                    {
                        // get the products list
                        var modelProducts = specialsProducts.ToList();

                        //get the model for each of the products
                        foreach (var product in modelProducts)
                        {
                            var productModel = GetProductModel(product);
                            //add it to the category model
                            specialsCategoryModel.CategoriesProducts.Add(productModel);
                        }
                    }

                    //get the specials category image
                    if (productSpecialsPage.HasProperty("bannerImage") && productSpecialsPage.HasValue("bannerImage"))
                    {
                        //set feature product image
                        var productVariantImage = productSpecialsPage.Value<IPublishedContent>("bannerImage");
                        if (productVariantImage?.Id > 0)
                        {
                            //get the image url
                            var imageLink = "/Images/Nature-Quest-Product-Default.png";
                            var defaultCropSize = productVariantImage.GetCropUrl("product");
                            var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                ? defaultCropSize
                                : productVariantImage.GetCropUrl(350, 500);
                            if (!string.IsNullOrWhiteSpace(productImagelink))
                            {
                                imageLink = productImagelink;
                            }

                            //create the product model
                            var variantImageModel = new ProductImageModel
                            {
                                ImageUrl = imageLink,
                                ImageAltText = productVariantImage.Name,
                                ImageProductId = productSpecialsPage.Id.ToString(),
                                IsFeaturedPriceImage = false
                            };
                            //add the image model to the product images
                            specialsCategoryModel.CategoryImageModel = variantImageModel;
                        }
                    }
                    //if we don't have a image set on the category then get the first one from the products
                    else if (specialsCategoryModel.CategoriesProducts.Any())
                    {
                        var productFirstImage =
                            specialsCategoryModel.CategoriesProducts.FirstOrDefault(productModel => productModel.ProductImages.Any());
                        if (productFirstImage != null)
                        {
                            specialsCategoryModel.CategoryImageModel = productFirstImage.ProductImages.FirstOrDefault();
                        }
                    }

                    //add the specials category model to the view model
                    productCategories.Add(specialsCategoryModel);
                }

                //get the best sellers page
                var productBestSellersPage = _homePage.Descendants().
                    FirstOrDefault(page =>
                        page.ContentType.Alias == "productBestSellersPage"
                        && !page.Value<bool>("hideFromMenu")
                        && page.IsPublished());

                //check the specials page and get the products set as featured/specials
                if (productBestSellersPage?.Id > 0)
                {
                    //set the default best sellers category page title
                    var bestSpecialsTitle = productBestSellersPage.Name;
                    //check if we have the page title set on the current page
                    if (productSpecialsPage.HasProperty("pageTitle") && productSpecialsPage.HasValue("pageTitle"))
                    {
                        // set the page title to override the default
                        bestSpecialsTitle = productBestSellersPage.GetProperty("pageTitle").Value().ToString();
                    }

                    //create the default best sellers category model
                    var bestSellersCategoryModel = new ProductCategory
                    {
                        CategoryLinkTitle = bestSpecialsTitle,
                        CategoryLinkUrl = productBestSellersPage.Url,
                        ProductCategoryPage = productBestSellersPage
                    };

                    //get the product specials
                    var bestSellersProducts = _homePage.Descendants().Where(page =>
                            page.ContentType.Alias == "productPage"
                            && !page.Value<bool>("hideFromMenu")
                            && page.Value<bool>("bestSellerProduct"))
                        .ToList();

                    //add the best sellers products
                    if (bestSellersProducts.Any())
                    {
                        // get the products list
                        var modelProducts = bestSellersProducts.ToList();

                        //get the model for each of the products
                        foreach (var product in modelProducts)
                        {
                            var productModel = GetProductModel(product);
                            //add it to the category model
                            bestSellersCategoryModel.CategoriesProducts.Add(productModel);
                        }
                    }

                    //get the specials category image
                    if (productBestSellersPage.HasProperty("bannerImage") && productBestSellersPage.HasValue("bannerImage"))
                    {
                        //set feature product image
                        var productVariantImage = productBestSellersPage.Value<IPublishedContent>("bannerImage");
                        if (productVariantImage?.Id > 0)
                        {
                            //get the image url
                            var imageLink = "/Images/Nature-Quest-Product-Default.png";
                            var defaultCropSize = productVariantImage.GetCropUrl("product");
                            var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                ? defaultCropSize
                                : productVariantImage.GetCropUrl(350, 500);
                            if (!string.IsNullOrWhiteSpace(productImagelink))
                            {
                                imageLink = productImagelink;
                            }

                            //create the product model
                            var variantImageModel = new ProductImageModel
                            {
                                ImageUrl = imageLink,
                                ImageAltText = productVariantImage.Name,
                                ImageProductId = productBestSellersPage.Id.ToString(),
                                IsFeaturedPriceImage = false
                            };
                            //add the image model to the product images
                            bestSellersCategoryModel.CategoryImageModel = variantImageModel;
                        }
                    }
                    //if we don't have a image set on the category then get the first one from the products
                    else if (bestSellersCategoryModel.CategoriesProducts.Any())
                    {
                        var productFirstImage =
                            bestSellersCategoryModel.CategoriesProducts.FirstOrDefault(productModel => productModel.ProductImages.Any());
                        if (productFirstImage != null)
                        {
                            bestSellersCategoryModel.CategoryImageModel = productFirstImage.ProductImages.FirstOrDefault();
                        }
                    }

                    //add the specials category model to the view model
                    productCategories.Add(bestSellersCategoryModel);
                }
            }

            //get the feature products to display
            var productCategoriesList = _homePage.Descendants().Where(page => 
                                                                        page.ContentType.Alias == "productCategoryPage"
                                                                        && !page.Value<bool>("hideFromMenu")
                                                                        && page.IsPublished())
                .ToList();

            //add each category to the model
            if (productCategoriesList.Any())
            {
                foreach (var productCategory in productCategoriesList)
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
                    var categoryProducts = productCategory.Children().Where(page => 
                                                                                page.ContentType.Alias == "productPage"
                                                                                && !page.Value<bool>("hideFromMenu")
                                                                                && page.IsPublished())
                        .ToList();

                    //if we have some products add them to the model
                    if (categoryProducts.Any())
                    {
                        // get the products list
                        var modelProducts = categoryProducts.ToList();

                        //get the model for each of the products
                        foreach (var product in modelProducts)
                        {
                            var productModel = GetProductModel(product);
                            //add it to the category model
                            categoryModel.CategoriesProducts.Add(productModel);
                        }
                    }

                    //get the category image
                    if (productCategory.HasProperty("bannerImage") && productCategory.HasValue("bannerImage"))
                    {
                        //set feature product image
                        var productVariantImage = productCategory.Value<IPublishedContent>("bannerImage");
                        if (productVariantImage?.Id > 0)
                        {
                            //get the image url
                            var imageLink = "/Images/Nature-Quest-Product-Default.png";
                            var defaultCropSize = productVariantImage.GetCropUrl("product");
                            var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                ? defaultCropSize
                                : productVariantImage.GetCropUrl(350, 500);
                            if (!string.IsNullOrWhiteSpace(productImagelink))
                            {
                                imageLink = productImagelink;
                            }

                            //create the product model
                            var variantImageModel = new ProductImageModel
                            {
                                ImageUrl = imageLink,
                                ImageAltText = productVariantImage.Name,
                                ImageProductId = productCategory.Id.ToString(),
                                IsFeaturedPriceImage = false
                            };
                            //add the image model to the product images
                            categoryModel.CategoryImageModel = variantImageModel;
                        }
                    }
                    //if we don't have a image set on the category then get the first one from the products
                    else if(categoryModel.CategoriesProducts.Any())
                    {
                        var productFirstImage =
                            categoryModel.CategoriesProducts.FirstOrDefault(productModel => productModel.ProductImages.Any());
                        if (productFirstImage != null)
                        {
                            categoryModel.CategoryImageModel = productFirstImage.ProductImages.FirstOrDefault();
                        }
                    }

                    //add the category model to the view model
                    productCategories.Add(categoryModel);

                }
            }

            //return the list of categories
            return productCategories;
        }
    }
}