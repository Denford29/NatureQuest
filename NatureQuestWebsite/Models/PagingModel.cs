using System;

namespace NatureQuestWebsite.Models
{
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