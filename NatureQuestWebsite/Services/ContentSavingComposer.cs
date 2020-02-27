using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using NatureQuestWebsite.Models;
using SendGrid.Helpers.Mail;
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
            //add the location services to the composer
            composition.Register<ILocationService, LocationService>();
            composition.Register<ISiteMembersService, SiteMembersService>();
            composition.Register<IMemberService, MemberService>();
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
        /// get the members service
        /// </summary>
        private readonly ISiteMembersService _siteMembersService;

        /// <summary>
        /// create the member service to use
        /// </summary>
        private readonly IMemberService _memberService;

        /// <summary>
        /// get the local logger to use
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// create the umbraco content service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="locationService"></param>
        /// <param name="siteMembersService"></param>
        /// <param name="memberService"></param>
        public ContentSavingComponent(
            ILogger logger,
            ILocationService locationService,
            ISiteMembersService siteMembersService,
            IMemberService memberService)
        {
            _logger = logger;
            _locationService = locationService;
            _siteMembersService = siteMembersService;
            _memberService = memberService;
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
            //Check if the content item type is a contact page or location
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

            //check if the content type is an order being saved
            foreach (var shopOrderContent in e.SavedEntities.Where(content => content.ContentType.Alias.InvariantEquals("shopOrder")))
            {
                //check if the order is being set as shipped
                if (shopOrderContent.HasProperty("isOrderShipped") 
                    && shopOrderContent.GetValue<bool>("isOrderShipped") &&
                    string.IsNullOrWhiteSpace(shopOrderContent.GetValue<string>("sendShipmentEmail")))
                {
                    SendShipmentEmail(shopOrderContent, sender);
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
                        _logger.Info(Type.GetType("UpdateContentLocationDetails"),
                            $"The location address: {locationModel.FullAddress} has been updated with the lat long:{locationModel.LatLong}");
                        //return true if we have update the content fine
                        return true;
                    }

                    _logger.Error(Type.GetType("UpdateContentLocationDetails"), $"Error updating location on page: {locationPage.Name}");
                    //return true if we have update the content fine
                    return false;
                }
            }
            catch (Exception ex)
            {
                ILogger logger = new DebugDiagnosticsLogger();
                logger.Error(Type.GetType("UpdateContentLocationDetails"), ex, "Error getting location model");
                return false;
            }
            //if we get this far something went wrong return false
            return false;
        }

        public bool SendShipmentEmail(IContent shopOrderPage, IContentService contentService)
        {
            var shipmentEmailSent = false;

            //get the member details
            IMember orderMember = null;
            var orderMemberUdi = shopOrderPage.GetValue<GuidUdi>("orderMember");
            if (orderMemberUdi != null)
            {
                orderMember = _memberService.GetByKey(orderMemberUdi.Guid);
            }

            //get the order shipping details page
            IContent orderShipping = null;
            var orderShippingUdi = shopOrderPage.GetValue<GuidUdi>("orderShipping");
            if (orderShippingUdi != null)
            {
                orderShipping = contentService.GetById(orderShippingUdi.Guid);
            }


            var orderId = shopOrderPage.GetValue<string>("orderId");
            var orderTrackingId = shopOrderPage.GetValue<string>("orderTrackingId");
            var orderSummary = shopOrderPage.GetValue<string>("orderSummary");

            //check if we have the order id
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                //create the string builder
                var emailBodyString = new StringBuilder();

                //build the email body
                emailBodyString.Append("<h2>Your Natures Quest Order Shipment.</h2>");

                //add the body introduction
                emailBodyString.Append($"<p>Your order : {orderId}, has been shipped and will be with you soon.</p>");

                //add the body shipping details
                emailBodyString.Append("<h3>Shipment Details.</h3>");
                //check the order tracking id
                if (!string.IsNullOrWhiteSpace(orderTrackingId))
                {
                    emailBodyString.Append($"<p>Your order tracking number is: {orderTrackingId}</p>");
                    //check if we have the shipping details page
                    if (orderShipping?.Id > 0)
                    {
                        emailBodyString.Append($"<p>Your order is shipped by: {orderShipping.Name}</p>");
                        if (orderShipping.HasProperty("websiteTrackingLink"))
                        {
                            var shippingLink = orderShipping.GetValue<string>("websiteTrackingLink");
                            if (!string.IsNullOrWhiteSpace(shippingLink))
                            {
                                emailBodyString.Append("<p>Visit the tracking website link here to track your order: " +
                                                       $"<a href=\"{shippingLink}\" target=\"_blank\">{orderShipping.Name}</a></p>");
                                
                            }
                        }
                    }
                }

                //create the email subject
                var customerSubject = "Your Order shipment from Natures Quest.";
                //create the customer email
                if (orderMember?.Id > 0)
                {
                    //create the default member properties to use
                    var memberEmail = orderMember.Email;
                    var memberName = "";
                    var mobileNumber = "";
                    var houseAddress = "";
                    var suburb = "";
                    var postCode = "";
                    var state = "";

                    //get the properties that can be edited
                    var editableProperties = orderMember.Properties.Where(property =>
                        property.Alias == "fullName" ||
                        property.Alias == "mobileNumber" ||
                        property.Alias == "houseAddress" ||
                        property.Alias == "suburb" ||
                        property.Alias == "postCode" ||
                        property.Alias == "state").ToList();

                    //get the values from the properties to set them on the model
                    if (editableProperties.Any())
                    {
                        foreach (var memberProperty in editableProperties)
                        {
                            var propertyAlias = memberProperty.Alias.ToLower();
                            switch (propertyAlias)
                            {
                                case "fullname":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        memberName = (string)memberProperty.GetValue();
                                    }
                                    break;
                                case "mobilenumber":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        mobileNumber = (string)memberProperty.GetValue();
                                    }
                                    break;
                                case "houseaddress":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        houseAddress = (string)memberProperty.GetValue();
                                    }
                                    break;
                                case "suburb":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        suburb = (string)memberProperty.GetValue();
                                    }
                                    break;
                                case "postcode":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        postCode = (string)memberProperty.GetValue();
                                    }
                                    break;
                                case "state":
                                    if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                    {
                                        state = (string)memberProperty.GetValue();
                                    }
                                    break;
                            }
                        }
                    }

                    //add the order shipping details
                    emailBodyString.Append("<p>Your order shipping details are below:" +
                                           $"Shipping name: {memberName} <br />" +
                                           $"Shipping email: {memberEmail} <br />" +
                                           $"Shipping phone: {mobileNumber} <br />" +
                                           $"Shipping address: {houseAddress} <br />" +
                                           $"Shipping suburb: {suburb} <br />" +
                                           $"Shipping post code: {postCode} <br />" +
                                           $"Shipping state: {state} </p>");

                    //check if we have the order summary saved on the page
                    if (!string.IsNullOrWhiteSpace(orderSummary))
                    {
                        //add the body summary
                        emailBodyString.Append("<h3>Order Summary.</h3>");
                        emailBodyString.Append($"{orderSummary}");
                    }

                    //add the email body footer
                    emailBodyString.Append("<p>Thank you for shopping in Natures Quest.<p/>");

                    //create the customer email address to send
                    var customerEmailAddress = new EmailAddress(
                        memberEmail,
                        memberName);

                    //add the string to the email body
                    var emailBody = new HtmlString(emailBodyString.ToString());

                    //create the email
                    var fromEmailAddress = new EmailAddress("support@naturesquest.com.au", "Natures Quest");

                    var memberShippingEmail = MailHelper.CreateSingleEmail(
                        fromEmailAddress,
                        customerEmailAddress,
                        customerSubject,
                        "",
                        emailBody.ToHtmlString());

                    //use the member service to send the email
                    _ = _siteMembersService.SendGridEmail(memberShippingEmail, true, true);

                    //save the date the email got sent
                    var shipmentSentDetails = $"Shipment Email sent: {DateTime.Now.ToShortDateString()}";
                    shopOrderPage.SetValue("sendShipmentEmail", shipmentSentDetails);

                    //save the content item
                    var saveResult = contentService.SaveAndPublish(shopOrderPage);

                    if (saveResult.Success)
                    {
                        _logger.Info(Type.GetType("SendShipmentEmail"),
                            $"The shipment email for order: {shopOrderPage.Name} has been sent and updated with message: {shipmentSentDetails}");
                        //return true if we have update the content fine
                        shipmentEmailSent = true;
                    }
                }
            }

            //return the result flag
            return shipmentEmailSent;
        }
    }

}