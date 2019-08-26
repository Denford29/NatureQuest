using System;
using System.Globalization;
using System.Linq;
using NatureQuestWebsite.Models;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// add the component composer for the content saving component
    /// </summary>
    public class ContentSavingComposer : IUserComposer
    {
        /// <summary>
        /// extend the composer to add our content saving component
        /// </summary>
        /// <param name="composition"></param>
        public void Compose(Composition composition)
        {
            //add the location service to the composer
            composition.Register<ILocationService, LocationService>();
            // Append our component to the collection of Components
            // It will be the last one to be run
            composition.Components().Append<ContentSavingComponent>();
        }
    }

    /// <summary>
    /// create the content saving component
    /// </summary>
    public class ContentSavingComponent : IComponent
    {
        /// <summary>
        /// get the products service
        /// </summary>
        private readonly ILocationService _locationService;

        /// <summary>
        /// get the local logger to use
        /// </summary>
        private readonly ILogger _logger;

        public ContentSavingComponent(
            ILogger logger,
            ILocationService locationService)
        {
            _logger = logger;
            _locationService = locationService;
        }

        // initialize: runs once when Umbraco starts
        public void Initialize()
        {
            ContentService.Saved += ContentService_Saving;
        }

        // terminate: runs once when Umbraco stops
        public void Terminate()
        {
        }

        /// <summary>
        /// create the extension that adds extra logic when a content page is saved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentService_Saving(IContentService sender, ContentSavedEventArgs e)
        {
            //Check if the content item type is a contact page
            foreach (var content in e.SavedEntities.Where(content => content.ContentType.Alias.InvariantEquals("contactPage") ||
                                                                     content.ContentType.Alias.InvariantEquals("locationAddress")))
            {
                //check if we have a full address and if the lat long is not already saved
                if (content.HasProperty("streetAddress") &&
                    !string.IsNullOrWhiteSpace(content.GetValue<string>("streetAddress")) &&
                    content.HasProperty("LatLong") &&
                    string.IsNullOrWhiteSpace(content.GetValue<string>("LatLong")))
                {
                    //update the location details
                    UpdateContentLocationDetails(content, sender);
                }
            }
        }

        /// <summary>
        /// get a flag to indicate the location page has been updated
        /// </summary>
        /// <param name="locationPage"></param>
        /// <param name="contentService"></param>
        /// <returns></returns>
        public bool UpdateContentLocationDetails(IContent locationPage, IContentService contentService)
        {
            try
            {
                var locationModel = new LocationModel();

                //check if we have an address and create the full address to use to search with
                var locationFullAddress = locationPage.GetValue<string>("streetAddress");
                //add the street address to the model
                locationModel.StreetAddress = locationPage.GetValue<string>("streetAddress");

                //add the suburb to the address
                if (locationPage.HasProperty("addressSuburb") &&
                    !string.IsNullOrWhiteSpace(locationPage.GetValue<string>("addressSuburb")))
                {
                    locationFullAddress += ", " + locationPage.GetValue<string>("addressSuburb");
                    //add the address suburb to the model
                    locationModel.AddressSuburb = locationPage.GetValue<string>("addressSuburb");
                }

                //add the suburb to the city
                if (locationPage.HasProperty("addressCity") &&
                    !string.IsNullOrWhiteSpace(locationPage.GetValue<string>("addressCity")))
                {
                    locationFullAddress += ", " + locationPage.GetValue<string>("addressCity");
                    //add the address suburb to the model
                    locationModel.AddressCity = locationPage.GetValue<string>("addressCity");
                }

                //add the suburb to the postcode
                if (locationPage.HasProperty("addressPostCode") &&
                    !string.IsNullOrWhiteSpace(locationPage.GetValue<string>("addressPostCode")))
                {
                    locationFullAddress += ", " + locationPage.GetValue<string>("addressPostCode");
                    //add the address suburb to the model
                    locationModel.AddressPostCode = locationPage.GetValue<string>("addressPostCode");
                }

                //use the full address to search for the lat long
                var geocodingModel = _locationService.GetGeocodingModel(locationFullAddress);
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
                    locationPage.SetValue("LatLong", locationModel.LatLong);
                    locationPage.SetValue("fullAddress", locationModel.FullAddress);
                    locationPage.SetValue("lat", locationModel.Lat);
                    locationPage.SetValue("long", locationModel.Long);
                    //save the content item
                    var saveResult = contentService.SaveAndPublish(locationPage);

                    if (saveResult.Success)
                    {
                        _logger.Info(Type.GetType("LocationService"),
                            $"The location address: {locationModel.FullAddress} has been updated with the lat long:{locationModel.LatLong}");
                        //return true if we have update the content fine
                        return true;
                    }

                    _logger.Error(Type.GetType("LocationService"), $"Error updating location on page: {locationPage.Name}");
                    //return true if we have update the content fine
                    return false;
                }
            }
            catch (Exception ex)
            {
                ILogger logger = new DebugDiagnosticsLogger();
                logger.Error(Type.GetType("LocationService"), ex, "Error getting location model");
                return false;
            }
            //if we get this far something went wrong return false
            return false;
        }
    }

}