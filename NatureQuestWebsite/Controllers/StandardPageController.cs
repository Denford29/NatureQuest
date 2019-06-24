using System.Web.Mvc;
using NatureQuestWebsite.Models;
using Umbraco.Web.Models;
using Zone.UmbracoMapper.V8;

namespace NatureQuestWebsite.Controllers
{
    public class StandardPageController : BasePageController<StandardPageViewModel>
    {
        /// <summary>
        /// create the default site name string
        /// </summary>
        private readonly string _siteName;

        /// <summary>
        /// initiate the controller using the base page controller 
        /// </summary>
        /// <param name="umbracoMapper"></param>
        /// <param name="contentModel"></param>
        public StandardPageController(
            IUmbracoMapper umbracoMapper,
            ContentModel contentModel
        ) : base(umbracoMapper)
        {
            //create the default site name from the page name
            _siteName = contentModel.Content.Name;
        }

        /// <summary>
        /// add the over ride for the default index action
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override ActionResult Index(ContentModel model)
        {
            //set the models page title
            base.MapModel(model.Content).PageTitle = _siteName;
            //return the index
            return base.Index(model);
        }
    }
}