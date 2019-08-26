using NatureQuestWebsite.Models;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// Interface to the service to get location details
    /// </summary>
    public interface ILocationService
    {
        ///// <summary>
        ///// get a flag to indicate the location page has been updated
        ///// </summary>
        ///// <param name="locationPage"></param>
        ///// <returns></returns>
        //bool UpdateContentLocationDetails(IContent locationPage);

        /// <summary>
        /// get a flag to indicate the location page has been updated
        /// </summary>
        /// <param name="locationPage"></param>
        /// <returns></returns>
        LocationModel GetPageLocationDetails(IPublishedContent locationPage);

        /// <summary>
        /// get the geo coding details from the address
        /// </summary>
        /// <param name="fullAddress"></param>
        /// <returns></returns>
        GoogleGeocodingModel GetGeocodingModel(string fullAddress);
    }
}