using NatureQuestWebsite.Controllers;
using Umbraco.Core.Composing;
using Umbraco.Web;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// add the component composer to override the default umbraco mvc
    /// </summary>
    public class DefaultControllerComposer : IUserComposer
    {
        /// <summary>
        /// extend the composer to add the override
        /// </summary>
        /// <param name="composition"></param>
        public void Compose(Composition composition)
        {
            //set the default controller to use
            composition.SetDefaultRenderMvcController<StandardPageController>();
        }
    }
}