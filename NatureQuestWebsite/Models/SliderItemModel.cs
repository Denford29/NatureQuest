using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// create the model for the slider items
    /// </summary>
    public class SliderItemModel : ContentModel
    {
        /// <summary>
        /// initiate the model with the content
        /// </summary>
        /// <param name="content"></param>
        public SliderItemModel(IPublishedContent content) : base(content)
        {
        }

        /// <summary>
        /// create the list of slider items
        /// </summary>
        public List<SliderItem> SliderItems { get; set; }  = new List<SliderItem>();

    }

    //define the class for each item
    public class SliderItem
    {
        /// <summary>
        /// get or set the slider heading
        /// </summary>
        public string SliderHeading { get; set; }

        /// <summary>
        /// get or set the slider heading
        /// </summary>
        public string SliderText { get; set; }

        /// <summary>
        /// get or set the slider url
        /// </summary>
        public string SliderUrl { get; set; }

        /// <summary>
        /// get or set the slider image
        /// </summary>
        public string SliderImage { get; set; }

        /// <summary>
        /// get or set the slider link page
        /// </summary>
        public IPublishedContent SliderLinkPage { get; set; }
    }
}