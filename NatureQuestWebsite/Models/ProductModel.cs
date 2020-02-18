using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// Create the model used for displaying products
    /// </summary>
    public class ProductModel 
    {
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
        /// get or set the featured price to display
        /// </summary>
        public ProductPriceModel FeaturedPrice { get; set; }

        /// <summary>
        /// get or set the sale price to display
        /// </summary>
        public ProductPriceModel SalePrice { get; set; }

        /// <summary>
        /// get the product title
        /// </summary>
        public string ProductTitle { get; set; }

        /// <summary>
        /// get or set the displayed page content text
        /// </summary>
        public string PageContentText { get; set; }

        /// <summary>
        /// get the product code for ordering
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// get the product short description
        /// </summary>
        public string ProductDescription { get; set; }

        /// <summary>
        /// get the product star rating
        /// </summary>
        public int ProductStarRating { get; set; }

        /// <summary>
        /// get or set the list of products ingredients
        /// </summary>
        public string[] ProductIngredients { get; set; }

        /// <summary>
        /// get a flag to indicate this products can be ordered i.e. at least 1 price plus product code
        /// </summary>
        public bool CanBeOrdered { get; set; }

        /// <summary>
        /// get the related products
        /// </summary>
        public List<ProductModel> RelatedProducts { get; set; } = new List<ProductModel>();

        /// <summary>
        /// get or set the displayed product prices
        /// </summary>
        public List<SelectListItem> ProductDisplayPrices { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// get or set the customer selected price
        /// </summary>
        [Display(Name = "Product Size")]
        [Required(ErrorMessage = "Please select the product size.")]
        public string SelectedPricePageId { get; set; }

        /// <summary>
        /// get or set the customer selected quantity
        /// </summary>
        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "Please enter the required quantity.")]
        public int SelectedQuantity { get; set; }

        /// <summary>
        /// get or set the product page id
        /// </summary>
        public int ProductPageId { get; set; }
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

        /// <summary>
        /// get or set the product image id e.g. variant id
        /// </summary>
        public string ImageProductId { get; set; }

        public bool IsFeaturedPriceImage { get; set; }
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
        /// get or set the product price page
        /// </summary>
        public IPublishedContent ProductPricePage { get; set; }

        /// <summary>
        /// get or set the product sale price
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// work out the sale percentage
        /// </summary>
        public int SalePercentage { get; set; } = 0;

        /// <summary>
        /// get or set the product variant
        /// </summary>
        public string ProductVariant { get; set; }

        /// <summary>
        /// get or set the flag if the product is set as a featured product
        /// </summary>
        public bool IsFeaturedPrice { get; set; }

        /// <summary>
        /// get or set the flag if the product is set as a sale product
        /// </summary>
        public bool IsSalePrice { get; set; }

        /// <summary>
        /// get or set the product variant code
        /// </summary>
        public string ProductVariantCode { get; set; }

        /// <summary>
        /// get or set the product variant image
        /// </summary>
        public ProductImageModel ProductVariantImage { get; set; }
    }
}