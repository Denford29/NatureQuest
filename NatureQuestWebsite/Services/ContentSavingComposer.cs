using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
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
        // initialize: runs once when Umbraco starts
        public void Initialize()
        {
            ContentService.Saving += ContentService_Saving;
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
        private void ContentService_Saving(IContentService sender, ContentSavingEventArgs e)
        {
            //Check if the content item type has a specific alias
            foreach (var content in e.SavedEntities.Where(c => c.ContentType.Alias.InvariantEquals("StandardPage")))
            {
                //Do something if the content is using the MyContentType doctype
            }
        }
    }

}