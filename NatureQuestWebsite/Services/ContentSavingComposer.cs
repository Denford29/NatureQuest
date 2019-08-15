using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;

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
        private ILocationService _locationService;

        /// <summary>
        /// create the context factory to use
        /// </summary>
        public readonly IUmbracoContextFactory _contextFactory;

        public ContentSavingComponent(IUmbracoContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
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
            ILogger logger = new DebugDiagnosticsLogger();
            //create the local service to use
            _locationService = new LocationService(logger, sender, _contextFactory);

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
                    _locationService.UpdateContentLocationDetails(content);
                }
            }
        }
    }

}