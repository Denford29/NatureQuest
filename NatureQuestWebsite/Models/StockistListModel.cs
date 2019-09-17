﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    public class StockistListModel
    {
        /// <summary>
        /// get or set the current content page
        /// </summary>
        public IPublishedContent CurrentPage { get; set; }

        /// <summary>
        /// get or set the page title displayed
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        /// get or set the page text displayed
        /// </summary>
        public string PageContentText { get; set; }

        /// <summary>
        /// set a flag to indicate this is the main stockist landing page
        /// </summary>
        public bool IsStockistLanding { get; set; }

        /// <summary>
        /// set a flag to indicate this is the main state landing page
        /// </summary>
        public bool IsStateLanding { get; set; }

        /// <summary>
        /// get or set the list of stockist links
        /// </summary>
        public List<LinkItemModel> StockistLinks { get; set; } = new List<LinkItemModel>();

        /// <summary>
        /// get or set the stockist location
        /// </summary>
        public List<LocationModel> StockistLocations { get; set; } = new List<LocationModel>();
    }
}