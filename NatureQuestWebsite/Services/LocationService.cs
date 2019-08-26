using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using NatureQuestWebsite.Models;
using Newtonsoft.Json;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// Service to get location details
    /// </summary>
    public class LocationService : ILocationService
    {

        /// <summary>
        /// create the private classes to use
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// create the local content service to use
        /// </summary>
        private readonly IContentService _contentService;

        /// <summary>
        /// set the details page
        /// </summary>
        private readonly IPublishedContent _siteDetailsPage;

        /// <summary>
        /// create the default google api key to use
        /// </summary>
        private readonly string _googleApiKey;

        /// <summary>
        /// set the home page
        /// </summary>
        private readonly IPublishedContent _homePage;

        /// <summary>
        /// initialise the service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="contentService"></param>
        /// <param name="contextFactory"></param>
        public LocationService(
            ILogger logger,
            IContentService contentService,
            IUmbracoContextFactory contextFactory)
        {
            //set the local variables
            _logger = logger;
            _contentService = contentService;

            //get the context to use
            using (var contextReference = contextFactory.EnsureUmbracoContext())
            {
                IPublishedCache contentCache = contextReference.UmbracoContext.ContentCache;
                var siteSettingsPage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
                //check if we have the home page and set it to the global page
                if (siteSettingsPage?.Id > 0)
                {
                    //get the site details page
                    var siteDetailsPage = siteSettingsPage.Descendants("globalDetails").FirstOrDefault();
                    if (siteDetailsPage?.Id > 0)
                    {
                        //save the global details page to use later
                        _siteDetailsPage = siteDetailsPage;

                        //get the site name
                        if (siteDetailsPage.HasProperty("googleMapsAPIKey") && siteDetailsPage.HasValue("googleMapsAPIKey"))
                        {
                            //set the google api key to use
                            _googleApiKey = siteDetailsPage.GetProperty("googleMapsAPIKey").Value().ToString();
                        }
                    }
                }
                //if we cant get the setting page log an error
                else
                {
                    _logger.Info(Type.GetType("ProductsService"), "Cant get homepage to use");
                }

                //get the home as well
                var homePage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
                if (homePage?.Id > 0)
                {
                    _homePage = homePage;
                }
            }
        }

        /// <summary>
        /// get a flag to indicate the location page has been updated
        /// </summary>
        /// <param name="locationPage"></param>
        /// <returns></returns>
        public LocationModel GetPageLocationDetails(IPublishedContent locationPage)
        {
            try
            {
                var locationModel = new LocationModel
                {
                    //set the page on the model
                    LocationPage = locationPage
                };

                //check if we have an address and create the full address to use to search with
                var locationFullAddress = locationPage.Value<string>("streetAddress");
                //add the street address to the model 
                locationModel.StreetAddress = locationPage.GetProperty("streetAddress").Value().ToString();

                //add the suburb to the address
                if (locationPage.HasProperty("addressSuburb") && locationPage.HasValue("addressSuburb"))
                {
                    locationFullAddress += ", " + locationPage.GetProperty("addressSuburb").Value();
                    //add the address suburb to the model
                    locationModel.AddressSuburb = locationPage.GetProperty("addressSuburb").Value().ToString();
                }

                //add the suburb to the city
                if (locationPage.HasProperty("addressCity") && locationPage.HasValue("addressCity"))
                {
                    locationFullAddress += ", " + locationPage.GetProperty("addressCity").Value();
                    //add the address suburb to the model
                    locationModel.AddressCity = locationPage.GetProperty("addressCity").Value().ToString();
                }

                //add the suburb to the postcode
                if (locationPage.HasProperty("addressPostCode") && locationPage.HasValue("addressPostCode"))
                {
                    locationFullAddress += ", " + locationPage.GetProperty("addressPostCode").Value();
                    //add the address suburb to the model
                    locationModel.AddressPostCode = locationPage.GetProperty("addressPostCode").Value().ToString();
                }

                //check if the location page has got a full address and lat long set
                if (locationPage.HasProperty("fullAddress") && locationPage.HasValue("fullAddress")
                   && locationPage.HasProperty("LatLong") &&locationPage.HasValue("LatLong"))
                {
                    locationModel.LatLong = locationPage.GetProperty("LatLong").Value().ToString();
                    locationModel.FullAddress = locationPage.GetProperty("fullAddress").Value().ToString();
                    locationModel.Lat = locationPage.GetProperty("lat").Value().ToString();
                    locationModel.Long = locationPage.GetProperty("long").Value().ToString();
                }
                //if we don't have that set then get it from the service
                else
                {
                    //use the full address to search for the lat long
                    var geocodingModel = GetGeocodingModel(locationFullAddress);
                    if (!string.IsNullOrWhiteSpace(geocodingModel?.results[0].formatted_address))
                    {
                        var firstResult = geocodingModel.results[0];

                        locationModel.LatLong = $"{firstResult.geometry.location.lat},{firstResult.geometry.location.lng}";
                        locationModel.FullAddress = firstResult.formatted_address;
                        locationModel.Lat = firstResult.geometry.location.lat.ToString(CultureInfo.InvariantCulture);
                        locationModel.Long = firstResult.geometry.location.lng.ToString(CultureInfo.InvariantCulture);
                    }

                    //if we get a location model back then save the lat long value back
                    if (!string.IsNullOrWhiteSpace(locationModel.LatLong)
                        && !string.IsNullOrWhiteSpace(locationModel.FullAddress))
                    {
                        //get the content item
                        var locationContentItem = _contentService.GetById(locationPage.Id);
                        locationContentItem.SetValue("LatLong", locationModel.LatLong);
                        locationContentItem.SetValue("fullAddress", locationModel.FullAddress);
                        locationContentItem.SetValue("lat", locationModel.Lat);
                        locationContentItem.SetValue("long", locationModel.Long);
                        //save the content item
                        var saveResult = _contentService.SaveAndPublish(locationContentItem);

                        if (saveResult.Success)
                        {
                            _logger.Info(Type.GetType("LocationService"),
                                $"The location address: {locationModel.FullAddress} has been updated with the lat long:{locationModel.LatLong}");
                        }

                        _logger.Error(Type.GetType("LocationService"), $"Error updating location on page: {locationPage.Name}");
                    }
                }
                //return the location details model
                return locationModel;
            }
            catch (Exception ex)
            {
                ILogger logger = new DebugDiagnosticsLogger();
                logger.Error(Type.GetType("LocationService"), ex, "Error getting location model");
                return null;
            }
        }

        /// <summary>
        /// get the geo coding details from the address
        /// </summary>
        /// <param name="fullAddress"></param>
        /// <returns></returns>
        public GoogleGeocodingModel GetGeocodingModel(string fullAddress)
        {
            var geocodingModel = new GoogleGeocodingModel();
            //check if we have the api key and address
            if (!string.IsNullOrWhiteSpace(_googleApiKey) && !string.IsNullOrWhiteSpace(fullAddress))
            {
                var requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={fullAddress}&key={_googleApiKey}";
                //create the web request
                var request = WebRequest.Create(requestUrl);
                //get the response
                var response = request.GetResponse();
                if (response.ContentLength != 0)
                {
                    //read the data stream from the response
                    var data = response.GetResponseStream();
                    //read the response
                    if (data != null)
                    {
                        //create the stream reader
                        var reader = new StreamReader(data);
                        // json-formatted string from maps api
                        string responseJsonString = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(responseJsonString))
                        {
                            var responseModel = JsonConvert.DeserializeObject<GoogleGeocodingModel>(responseJsonString);
                            //check if this response model is valid
                            if (responseModel != null &&responseModel.results.Length > 0)
                            {
                                //set the response model to the return model
                                geocodingModel = responseModel;
                            }
                        }
                    }
                }
                response.Close();
            }

            //return the model
            return geocodingModel;
        }
    }
}