using System.Web.Mvc;
using Moq;
using NatureQuestWebsite.Controllers;
using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace NatureQuestWebsite.Testing
{
    /// <summary>
    /// create the standard page controller test
    /// </summary>
    [TestFixture]
    public class StandardPageControllerTest
    {
        //create the private controller and mock published content
        private StandardPageController _controller;
        private Mock<IPublishedContent> _content;
        private IProductsService _productService;
        private ILogger _logger;
        private IUmbracoContextFactory _contextFactory;
        private IMemberService _memberService;
        private IShoppingService _shoppingService;
        private IContentService _contentService;
        private UmbracoHelper _umbracoHelper;
        private ISiteMembersService _siteMembersService;
        /// <summary>
        /// set up the test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            //create the mock items to use for testing
            Current.Factory = Mock.Of<IFactory>();
            _content = new Mock<IPublishedContent>();
            _logger = Mock.Of<DebugDiagnosticsLogger>(); 
            _contextFactory = Mock.Of<IUmbracoContextFactory>();
            //create the product service
            _productService = new ProductsService(_logger, _contextFactory);
            //create the mock content service
            _contentService = Mock.Of<IContentService>();
            //get the umbraco helper to use
            _umbracoHelper = Mock.Of<UmbracoHelper>();

            _memberService = Mock.Of<MemberService>();
            //create the mock content service
            _siteMembersService = Mock.Of<ISiteMembersService>();
            _shoppingService = new ShoppingService(
                                                    _logger, 
                                                    _contextFactory, 
                                                    _memberService,
                                                    _contentService,
                                                    _umbracoHelper,
                                                    _productService, 
                                                    _siteMembersService);
            //pass this to the standard page mock controller
            _controller = new StandardPageController(_productService, _shoppingService);
        }

        /// <summary>
        /// define the logic executed at the end
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Current.Reset();
        }

        /// <summary>
        /// define the test and the test cases
        /// </summary>
        /// <param name="heading"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase("", null)]
        [TestCase(null, null)]
        [TestCase("My Heading", "My Heading")]
        [TestCase("Another Heading", "Another Heading")]
        public void GivenContentHasHeading_WhenIndexAction_ThenReturnViewModelWithHeading(string heading, string expected)
        {
            SetupPropertyValue("heading", heading);

            var viewModel = (StandardPageViewModel)((ViewResult)_controller.Index(new ContentModel(_content.Object))).Model;

            Assert.AreEqual(expected, viewModel.PageTitle);
        }

        /// <summary>
        /// set the property values
        /// </summary>
        /// <param name="propertyAlias"></param>
        /// <param name="value"></param>
        /// <param name="culture"></param>
        /// <param name="segments"></param>
        public void SetupPropertyValue(string propertyAlias, object value, string culture = null, string segments = null)
        {
            var property = new Mock<IPublishedProperty>();
            property.Setup(x => x.Alias).Returns(propertyAlias);
            property.Setup(x => x.HasValue(culture, segments)).Returns(value != null);
            property.Setup(x => x.GetValue(culture, segments)).Returns(value);
            _content.Setup(x => x.GetProperty(propertyAlias)).Returns(property.Object);
        }

    }
}

