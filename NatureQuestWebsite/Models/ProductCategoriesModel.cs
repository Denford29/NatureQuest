using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    public class ProductCategoriesModel
    {
        /// <summary>
        /// get or set the product categories
        /// </summary>
        public List<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

        /// <summary>
        /// get or set the category menu products
        /// </summary>
        public List<LinkItemModel> ProductCategoriesLinks { get; set; } = new List<LinkItemModel>();
    }

    /// <summary>
    /// create the model for each category
    /// </summary>
    public class ProductCategory
    {
        /// <summary>
        /// get or set the product category page
        /// </summary>
        public IPublishedContent ProductCategoryPage { get; set; }

        /// <summary>
        /// get or set the product category title, default page name
        /// </summary>
        public string CategoryLinkTitle { get; set; }

        /// <summary>
        /// get or set the product category url
        /// </summary>
        public string CategoryLinkUrl { get; set; }

        /// <summary>
        /// get or set the product category image model
        /// </summary>
        public ProductImageModel CategoryImageModel { get; set; }

        /// <summary>
        /// get or set the categories products
        /// </summary>
        public List<ProductModel> CategoriesProducts { get; set; } = new List<ProductModel>();

    }
}