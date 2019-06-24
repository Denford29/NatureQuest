using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Zone.UmbracoMapper.V8;

namespace NatureQuestWebsite.Controllers
{
    /// <summary>
    /// Create the base page controller which inherits from the render mvc controller
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class BasePageController<TModel> : RenderMvcController where TModel : class, new()
    {
        /// <summary>
        /// create the local mapper to use
        /// </summary>
        private readonly IUmbracoMapper _umbracoMapper;

        /// <summary>
        /// initialise the controller with the mapper
        /// </summary>
        /// <param name="umbracoMapper"></param>
        protected BasePageController(IUmbracoMapper umbracoMapper)
        {
            _umbracoMapper = umbracoMapper;
        }

        /// <summary>
        /// over ride the default index action
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override ActionResult Index(ContentModel model)
        {
            //return the view with the custom model combined with the umbraco content model
            return View(MapModel(model.Content));
        }

        /// <summary>
        /// create the model mapper with the umbraco content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public virtual TModel MapModel(IPublishedContent content)
        {
            //create the new local model
            var model = new TModel();
            //use he mapper to join the 2 models
            _umbracoMapper.Map(content, model);
            //return the combined model
            return model;
        }
    }
}