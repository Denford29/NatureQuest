using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// create the products list model
    /// </summary>
    public class ProductsListModel
    {
        /// <summary>
        /// get or set the list of product models
        /// </summary>
        public List<ProductModel> ProductsList { get; set; } = new List<ProductModel>();

        /// <summary>
        /// get or set the paging model
        /// </summary>
        public PagingModel ProductsPaging { get; set; }

        /// <summary>
        /// get or set the sort options
        /// </summary>
        public List<SelectListItem> SortOptions { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// get or set the default or selected sort option
        /// </summary>
        public string SortOption { get; set; }

        /// <summary>
        /// get or set the category menu products
        /// </summary>
        public List<LinkItemModel> ProductCategoriesLinks { get; set; } = new List<LinkItemModel>();
    }

    /// <summary>
    /// Create the paging model
    /// </summary>
    public class PagingModel
    {
        /// <summary>
        /// get or set the total item to be paged
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// get or set the items displayed per page
        /// </summary>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// get or set the current page number
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// get the total pages by getting the full number of dividing the total items by the items per page
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
    }
}