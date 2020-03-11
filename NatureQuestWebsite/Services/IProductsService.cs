using System.Collections.Generic;
using NatureQuestWebsite.Models;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// create the product service interface to use for product related calls
    /// </summary>
    public interface IProductsService
    {
        /// <summary>
        /// get the product model from an umbraco page
        /// </summary>
        /// <param name="productPage"></param>
        /// <param name="getThumbnail"></param>
        /// <param name="featurePriceId"></param>
        /// <returns></returns>
        ProductModel GetProductModel(IPublishedContent productPage, bool getThumbnail = false, string featurePriceId = "");

        /// <summary>
        /// get the list of product categories as links
        /// </summary>
        /// <returns></returns>
        List<LinkItemModel> ProductCategoryLinks();

        /// <summary>
        /// get the product categories
        /// </summary>
        /// <returns></returns>
        List<ProductCategory> ProductCategories(bool includeSpecials = false);
    }
}