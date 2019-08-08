using System;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// Create the model used for displaying products
    /// </summary>
    public class ProductModel : ContentModel
    {
        /// <summary>
        /// initiate the model with the content
        /// </summary>
        /// <param name="content"></param>
        public ProductModel(IPublishedContent content) : base(content)
        {
        }

        /// <summary>
        /// get or set the product page
        /// </summary>
        public IPublishedContent ProductPage { get; set; }

        /// <summary>
        /// get or set the product images
        /// </summary>
        public List<ProductImageModel> ProductImages { get; set; } = new List<ProductImageModel>();

        /// <summary>
        /// get the product prices
        /// </summary>
        public List<ProductPriceModel> ProductPrices { get; set; } = new List<ProductPriceModel>();

        /// <summary>
        /// get the product title
        /// </summary>
        public string ProductTitle { get; set; }
    }

    /// <summary>
    /// set the model used for product images
    /// </summary>
    public class ProductImageModel
    {
        /// <summary>
        /// get or set the product image url
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// get or set the product image alt text
        /// </summary>
        public string ImageAltText { get; set; }
    }

    /// <summary>
    /// set the model used for product prices
    /// </summary>
    public class ProductPriceModel
    {
        /// <summary>
        /// get or set the product price
        /// </summary>
        public decimal ProductPrice { get; set; }

        /// <summary>
        /// get or set the product sale price
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// work out the sale percentage
        /// </summary>
        public int SalePercentage { get; set; }

        /// <summary>
        /// get or set the product variant
        /// </summary>
        public string ProductVariant { get; set; }

        /// <summary>
        /// get or set the flag if the product is set as a featured product
        /// </summary>
        public bool IsFeaturedPrice { get; set; }
    }
}