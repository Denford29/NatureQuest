using System.Text;
using System.Web.Mvc;
using NatureQuestWebsite.Models;

namespace NatureQuestWebsite.Services
{
    public static class SiteHelpers
    {
        /// <summary>
        /// build a mvc html string for the paging links
        /// </summary>
        /// <param name="html"></param>
        /// <param name="pagingModel"></param>
        /// <param name="pageUrl"></param>
        /// <param name="sortOrder"></param>
        /// <param name="useSorting"></param>
        /// <returns></returns>
        public static MvcHtmlString PagingLink(this HtmlHelper html,
            PagingModel pagingModel,
            string pageUrl,
            string sortOrder = "",
            bool useSorting = true)
        {
            //create the default html string to return
            var pagingListItems = new StringBuilder();
            //go through the total pages and create a li item for each
            for (int pageCount = 1; pageCount <= pagingModel.TotalPages; pageCount++)
            {
                //generate the lin url
                var itemUrl = $"{pageUrl}?page={pageCount}&sortOption={sortOrder}";
                if (!useSorting)
                {
                    itemUrl = $"{pageUrl}?page={pageCount}";
                }


                //build the li item tag and set the class if its the active link
                var listTag = new TagBuilder("li");
                if (pageCount == pagingModel.CurrentPage)
                {
                    listTag.AddCssClass("active");
                }

                //create the link tag to go inside the li
                var linkTag = new TagBuilder("a");
                linkTag.MergeAttribute("href", itemUrl);
                linkTag.InnerHtml = pageCount.ToString();

                //add the link tag to the li tag
                listTag.InnerHtml = linkTag.ToString();

                //add the li tag to the list of paging links to return
                pagingListItems.Append(listTag);
            }
            //return the mvc string
            return MvcHtmlString.Create(pagingListItems.ToString());
        }
    }
}