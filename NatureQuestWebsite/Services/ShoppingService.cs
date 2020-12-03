using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using NatureQuestWebsite.Models;
using Newtonsoft.Json;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using Stripe.Checkout;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Order = PayPalCheckoutSdk.Orders.Order;
using ShippingOption = NatureQuestWebsite.Models.ShippingOption;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// class for the shopping cart service
    /// </summary>
    public class ShoppingService : IShoppingService
    {
        /// <summary>
        /// create the logger to use
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// create the list of admin email addresses
        /// </summary>
        private readonly List<EmailAddress> _siteToEmailAddresses = new List<EmailAddress>();

        /// <summary>
        /// create the default system email address
        /// </summary>
        private readonly EmailAddress _systemEmailAddress;

        /// <summary>
        /// create the default from email address
        /// </summary>
        private readonly EmailAddress _fromEmailAddress;

        /// <summary>
        /// create the default send grid key to use
        /// </summary>
        private readonly string _sendGridKey;

        /// <summary>
        /// create the default site name string
        /// </summary>
        private readonly string _siteName = "Natures Quest";

        /// <summary>
        /// set the home page
        /// </summary>
        internal readonly IPublishedContent HomePage;

        /// <summary>
        /// set the global details page
        /// </summary>
        internal readonly IPublishedContent GlobalDetailsPage;

        /// <summary>
        /// set the global carts page
        /// </summary>
        private readonly IPublishedContent _globalCartsPage;

        /// <summary>
        /// set the global orders page
        /// </summary>
        private readonly IPublishedContent _globalOrdersPage;

        /// <summary>
        /// set the store details page
        /// </summary>
        internal readonly IPublishedContent StoreDetailsPage;

        /// <summary>
        /// create the member service to use
        /// </summary>
        private readonly IMemberService _memberService;

        /// <summary>
        /// global carts page alias
        /// </summary>
        private const string GlobalCartsPageAlias = "cartsFolder";

        /// <summary>
        /// cart page alias
        /// </summary>
        private const string CartPageAlias = "shopCart";

        /// <summary>
        /// cart page item alias
        /// </summary>
        private const string CartPageItemAlias = "cartItem";

        /// <summary>
        /// set the shopping cart details page
        /// </summary>
        private readonly IPublishedContent _shoppingCartPage;

        /// <summary>
        /// set the shopping cart success page
        /// </summary>
        private readonly IPublishedContent _shoppingSuccessPage;

        /// <summary>
        /// set the shopping checkout page
        /// </summary>
        private readonly IPublishedContent _checkoutPage;

        /// <summary>
        /// set the products page
        /// </summary>
        private readonly IPublishedContent _productsPage;

        /// <summary>
        /// global order page alias
        /// </summary>
        private const string GlobalOrdersPageAlias = "ordersFolder";

        /// <summary>
        /// order page alias
        /// </summary>
        private const string OrderPageAlias = "shopOrder";

        /// <summary>
        /// create the local content service to use
        /// </summary>
        private readonly IContentService _contentService;

        /// <summary>
        /// get the umbraco helper
        /// </summary>
        private readonly UmbracoHelper _umbracoHelper;

        /// <summary>
        /// get the products service
        /// </summary>
        private readonly IProductsService _productsService;

        /// <summary>
        /// get the local shipping options
        /// </summary>
        private readonly List<IPublishedContent> _shippingOptionPages = new List<IPublishedContent>();

        /// <summary>
        /// get the local stripe test publish key
        /// </summary>
        public string StripeTestPublishableKey = WebConfigurationManager.AppSettings["stripeTestPublishableKey"];

        /// <summary>
        /// get the local stripe test secret key
        /// </summary>
        public string StripeTestSecretKey = WebConfigurationManager.AppSettings["stripeTestSecretKey"];

        /// <summary>
        /// get the local stripe live publish key
        /// </summary>
        public string StripeLivePublishableKey = WebConfigurationManager.AppSettings["stripeLivePublishableKey"];

        /// <summary>
        /// get the local stripe live secret key
        /// </summary>
        public string StripeLiveSecretKey = WebConfigurationManager.AppSettings["stripeLiveSecretKey"];

        /// <summary>
        /// get or set the flag that stripe is in live mode
        /// </summary>
        public bool StripeLiveMode;

        /// <summary>
        /// get the local paypal test client id
        /// </summary>
        public string PaypalTestClientId = WebConfigurationManager.AppSettings["paypalTestClientId"];

        /// <summary>
        /// get the local paypal test secret 
        /// </summary>
        public string PaypalTestSecret = WebConfigurationManager.AppSettings["paypalTestSecret"];

        /// <summary>
        /// get the local paypal live client id
        /// </summary>
        public string PaypalLiveClientId = WebConfigurationManager.AppSettings["paypalLiveClientId"];

        /// <summary>
        /// get the local paypal live secret 
        /// </summary>
        public string PaypalLiveSecret = WebConfigurationManager.AppSettings["paypalLiveSecret"];

        /// <summary>
        /// get the local site member service
        /// </summary>
        private readonly ISiteMembersService _siteMembersService;

        /// <summary>
        /// initialise the shopping service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="contextFactory"></param>
        /// <param name="memberService"></param>
        /// <param name="contentService"></param>
        /// <param name="umbracoHelper"></param>
        /// <param name="productsService"></param>
        /// <param name="siteMembersService"></param>
        public ShoppingService(
            ILogger logger,
            IUmbracoContextFactory contextFactory,
            IMemberService memberService,
            IContentService contentService,
            UmbracoHelper umbracoHelper,
            IProductsService productsService,
            ISiteMembersService siteMembersService
        )
        {
            //set the local logger to use
            _logger = logger;
            //set the member service to use
            _memberService = memberService;
            //set the content service to use
            _contentService = contentService;
            //get the umbraco helper
            _umbracoHelper = umbracoHelper;
            //get the product service
            _productsService = productsService;
            //get the site member service
            _siteMembersService = siteMembersService;

            //create the default system email address
            _systemEmailAddress = new EmailAddress("admin@rdmonline.com.au", "Admin");
            _fromEmailAddress = new EmailAddress("support@naturesquest.com.au", _siteName);

            //get the sendgrid api key
            _sendGridKey = WebConfigurationManager.AppSettings["sendGridKey"];

            //get the context to use
            using (var contextReference = contextFactory.EnsureUmbracoContext())
            {
                IPublishedCache contentCache = contextReference.UmbracoContext.ContentCache;
                var siteSettingsPage =
                    contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
                if (siteSettingsPage?.Id > 0)
                {
                    //get the site details page
                    var siteDetailsPage = siteSettingsPage.ChildrenOfType("globalDetails").FirstOrDefault();
                    if (siteDetailsPage?.Id > 0)
                    {
                        //save the global details page to use later
                        GlobalDetailsPage = siteDetailsPage;

                        //get the site name
                        if (siteDetailsPage.HasProperty("siteName") && siteDetailsPage.HasValue("siteName"))
                        {
                            //set the global site name
                            _siteName = siteDetailsPage.GetProperty("siteName").Value().ToString();
                        }

                        //get the sites contact emails addresses
                        if (siteDetailsPage.HasProperty("contactToEmailAddress") &&
                            siteDetailsPage.HasValue("contactToEmailAddress"))
                        {
                            //set the global site name
                            var adminEmailAddresses = siteDetailsPage.Value<string[]>("contactToEmailAddress");
                            //if we have the contact addresses create the list of emails to send emails to
                            if (adminEmailAddresses.Length > 0)
                            {
                                //delete the default address and add the new ones
                                //_siteToEmailAddresses.Clear();
                                foreach (var address in adminEmailAddresses)
                                {
                                    _siteToEmailAddresses.Add(new EmailAddress(address, "Admin"));
                                }
                            }
                        }

                        //get the send grid from the backend
                        if (siteDetailsPage.HasProperty("sendGridAPIKey") && siteDetailsPage.HasValue("sendGridAPIKey"))
                        {
                            _sendGridKey = siteDetailsPage.GetProperty("sendGridAPIKey").Value().ToString();
                        }

                        //get the send grid from the backend
                        if (siteDetailsPage.HasProperty("contactFromEmailAddress") &&
                            siteDetailsPage.HasValue("contactFromEmailAddress"))
                        {
                            var fromEmailAddress = siteDetailsPage.GetProperty("contactFromEmailAddress").Value()
                                .ToString();
                            _fromEmailAddress = new EmailAddress(fromEmailAddress, _siteName);
                        }
                    }

                    //get the store details page
                    var storeDetailsPage = siteSettingsPage.ChildrenOfType("storeSettingsDetails").FirstOrDefault();
                    if (storeDetailsPage?.Id > 0)
                    {
                        //save the store details page for use later
                        StoreDetailsPage = storeDetailsPage;
                        //get the shipping child items that have got prices set
                        var shippingOptions = storeDetailsPage.Children().Where(page =>
                            page.ContentType.Alias == "shippingOption"
                            && page.HasProperty("shippingFee")
                            && page.HasValue("shippingFee")
                            && page.IsPublished()).ToList();
                        if (shippingOptions.Any())
                        {
                            _shippingOptionPages = shippingOptions;
                        }

                        //get the stored stripe details
                        if (storeDetailsPage.HasProperty("testPublishableKey") &&
                            storeDetailsPage.HasValue("testPublishableKey"))
                        {
                            StripeTestPublishableKey = storeDetailsPage.Value<string>("testPublishableKey");
                        }

                        //get the stored stripe details
                        if (storeDetailsPage.HasProperty("testSecretKey") && storeDetailsPage.HasValue("testSecretKey"))
                        {
                            StripeTestSecretKey = storeDetailsPage.Value<string>("testSecretKey");
                        }

                        //get the stored stripe details
                        if (storeDetailsPage.HasProperty("livePublishableKey") &&
                            storeDetailsPage.HasValue("livePublishableKey"))
                        {
                            StripeLivePublishableKey = storeDetailsPage.Value<string>("livePublishableKey");
                        }

                        //get the stored stripe details
                        if (storeDetailsPage.HasProperty("liveSecretKey") && storeDetailsPage.HasValue("liveSecretKey"))
                        {
                            StripeLiveSecretKey = storeDetailsPage.Value<string>("liveSecretKey");
                        }

                        //get the stored stripe flag to use live settings
                        if (storeDetailsPage.HasProperty("stripeLiveMode") && storeDetailsPage.HasValue("stripeLiveMode"))
                        {
                            StripeLiveMode = storeDetailsPage.Value<bool>("stripeLiveMode");
                        }
                        //for debugging set this to false
                        //StripeLiveMode = false;

                        //get the stored paypal details
                        if (storeDetailsPage.HasProperty("testClientId") && storeDetailsPage.HasValue("testClientId"))
                        {
                            PaypalTestClientId = storeDetailsPage.Value<string>("testClientId");
                        }

                        //get the stored paypal details
                        if (storeDetailsPage.HasProperty("testSecret") && storeDetailsPage.HasValue("testSecret"))
                        {
                            PaypalTestSecret = storeDetailsPage.Value<string>("testSecret");
                        }

                        //get the stored paypal details
                        if (storeDetailsPage.HasProperty("liveClientId") && storeDetailsPage.HasValue("liveClientId"))
                        {
                            PaypalLiveClientId = storeDetailsPage.Value<string>("liveClientId");
                        }

                        //get the stored paypal details
                        if (storeDetailsPage.HasProperty("liveSecret") && storeDetailsPage.HasValue("liveSecret"))
                        {
                            PaypalLiveSecret = storeDetailsPage.Value<string>("liveSecret");
                        }
                    }
                }

                //get the carts and orders page
                var cartsOrdersPage =
                    contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "storeDetails");
                if (cartsOrdersPage?.Id > 0)
                {
                    //get the site carts page
                    var cartsPage = cartsOrdersPage.ChildrenOfType("cartsFolder").FirstOrDefault();
                    if (cartsPage?.Id > 0)
                    {
                        //save the global carts page to use later
                        _globalCartsPage = cartsPage;
                    }

                    //get the site orders page
                    var ordersPage = cartsOrdersPage.ChildrenOfType("ordersFolder").FirstOrDefault();
                    if (ordersPage?.Id > 0)
                    {
                        //save the global orders page to use later
                        _globalOrdersPage = ordersPage;
                    }
                }

                //get the home page
                var homePage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
                //check if we have the home page and set it to the global page
                if (homePage?.Id > 0)
                {
                    HomePage = homePage;

                    //get the shopping cart details page
                    if (homePage.FirstChildOfType("shoppingCartPage")?.Id > 0)
                    {
                        //get the shopping cart page
                        _shoppingCartPage = homePage.FirstChildOfType("shoppingCartPage");
                        //get the checkout page
                        _checkoutPage = homePage.FirstChildOfType("checkoutPage");
                        //set the success page
                        if (_checkoutPage?.Id > 0 && _checkoutPage.FirstChildOfType("checkoutConfirmPage") != null)
                        {
                            _shoppingSuccessPage = _checkoutPage.FirstChildOfType("checkoutConfirmPage");
                        }
                        else
                        {
                            _shoppingSuccessPage = homePage;
                        }
                    }

                    //get the checkout page
                    if (homePage.FirstChildOfType("checkoutPage")?.Id > 0)
                    {
                        _checkoutPage = homePage.FirstChildOfType("checkoutPage");
                    }

                    //get the products page
                    if (homePage.FirstChildOfType("productLandingPage")?.Id > 0)
                    {
                        _productsPage = homePage.FirstChildOfType("productLandingPage");
                    }
                }
            }

        }

        /// <summary>
        /// get the current shopping cart with an optional member email if the member is logged in
        /// </summary>
        /// <param name="memberEmailAddress"></param>
        /// <returns></returns>
        public SiteShoppingCart GetCurrentCart(string memberEmailAddress = "")
        {
            //get the cart from the session
            var currentCart = (SiteShoppingCart)HttpContext.Current.Session["Cart"];
            //if we don't have 1 in the session, then create a new 1
            if (currentCart == null)
            {
                currentCart = new SiteShoppingCart();
                HttpContext.Current.Session["Cart"] = currentCart;
            }

            //set the cart pages
            currentCart.ShoppingCartPage = _shoppingCartPage;
            currentCart.CheckoutPage = _checkoutPage;
            currentCart.ProductsPage = _productsPage;
            currentCart.ShoppingSuccessPage = _shoppingSuccessPage;

            //set the stripe keys
            currentCart.StripeTestPublishableKey = StripeTestPublishableKey;
            currentCart.StripeTestSecretKey = StripeTestSecretKey;
            currentCart.StripeLivePublishableKey = StripeLivePublishableKey;
            currentCart.StripeLiveSecretKey = StripeLiveSecretKey;
            currentCart.IsStripeLiveMode = StripeLiveMode;

            //set the paypal keys
            currentCart.PayPalTestClientId = PaypalTestClientId;
            currentCart.PayPalTestSecret = PaypalTestSecret;
            currentCart.PayPalLiveClientId = PaypalLiveClientId;
            currentCart.PayPalLiveSecret = PaypalLiveSecret;

            //add the shipping options
            if (_shippingOptionPages.Any() && !currentCart.DisplayShippingOptions.Any())
            {
                var optionsCounter = 0;

                foreach (var shippingOptionPage in _shippingOptionPages)
                {
                    // increase the counter
                    optionsCounter++;
                    //create the shipping option item to display
                    var displayShippingOption = new ShippingOption
                    {
                        ShippingPricePage = shippingOptionPage,
                        ShippingPageId = shippingOptionPage.Id
                    };

                    var shippingFee = "";
                    //check if we have a shipping fee set
                    if (shippingOptionPage.HasProperty("shippingFee") && shippingOptionPage.HasValue("shippingFee"))
                    {
                        // set the shipping price
                        displayShippingOption.ShippingFee = shippingOptionPage.Value<decimal>("shippingFee");
                        shippingFee = shippingOptionPage.Value<decimal>("shippingFee").ToString("c");
                    }

                    //check if we have a shipping details set
                    if (shippingOptionPage.HasProperty("shippingDetails") &&
                        shippingOptionPage.HasValue("shippingDetails"))
                    {
                        // set the shipping details
                        displayShippingOption.ShippingDetails = shippingOptionPage.Value<string>("shippingDetails");
                    }

                    //check if we have a delivery time set
                    if (shippingOptionPage.HasProperty("deliveryTime") && shippingOptionPage.HasValue("deliveryTime"))
                    {
                        // set the delivery time
                        displayShippingOption.DeliveryTime = shippingOptionPage.Value<string>("deliveryTime");
                    }

                    //set the shipping option and the default 
                    if (optionsCounter == 1)
                    {
                        //add the options for select radio buttons
                        var shippingSelectOption = new SelectListItem
                        {
                            Text = shippingFee,
                            Value = shippingOptionPage.Id.ToString(),
                            Selected = true
                        };
                        currentCart.SelectShippingOptions.Add(shippingSelectOption);
                        //set the default selected option
                        currentCart.SelectedShippingOption = shippingOptionPage.Id.ToString();
                        currentCart.ShippingTotal = displayShippingOption.ShippingFee;
                    }
                    else
                    {
                        //add the options for select radio buttons
                        var shippingSelectOption = new SelectListItem
                        {
                            Text = shippingFee,
                            Value = shippingOptionPage.Id.ToString(),
                            Selected = false
                        };
                        currentCart.SelectShippingOptions.Add(shippingSelectOption);
                    }

                    //add the displayed option
                    currentCart.DisplayShippingOptions.Add(displayShippingOption);
                }
            }

            //if we have an email address passed in try and get a saved cart for the user
            if (!string.IsNullOrWhiteSpace(memberEmailAddress))
            {
                //see if we can get the a saved cart
                var cartMember = GetMemberByEmail(memberEmailAddress);
                if (cartMember != null && _globalCartsPage != null)
                {
                    //get the stored cart page for the member
                    var cartPage = _globalCartsPage.Children.FirstOrDefault(page => page.HasProperty("cartMember") &&
                                                                                    page.HasValue("cartMember") &&
                                                                                    page.Value<IPublishedContent>(
                                                                                        "cartMember")?.Name ==
                                                                                    cartMember.Name);
                    //check if we have the page and use it to generate the cart with
                    if (cartPage?.Id > 0)
                    {
                        //save the cart member and member cart page to the current cart
                        currentCart.CartMember = cartMember;
                        currentCart.MemberCartPage = cartPage;

                        //check if the cart has got a shipping page set
                        if (cartPage.HasProperty("cartShipping") && cartPage.HasValue("cartShipping"))
                        {
                            //save the umbraco cart page id to the cart item
                            var cartSavedShippingPage = cartPage.Value<IPublishedContent>("cartShipping");
                            if (cartSavedShippingPage?.Id > 0)
                            {
                                //get the current selected option
                                var currentSelectedOption =
                                    currentCart.SelectShippingOptions.FirstOrDefault(option => option.Selected);
                                if (currentSelectedOption != null)
                                {
                                    currentSelectedOption.Selected = false;
                                }

                                //set the new selected option
                                var newSelectedOption = currentCart.SelectShippingOptions.FirstOrDefault(option =>
                                    option.Value == cartSavedShippingPage.Id.ToString());
                                if (newSelectedOption != null)
                                {
                                    newSelectedOption.Selected = true;
                                }

                                var shippingOptionPage =
                                    currentCart.DisplayShippingOptions.FirstOrDefault(page =>
                                        page.ShippingPageId == cartSavedShippingPage.Id);
                                if (shippingOptionPage != null)
                                {
                                    currentCart.SelectedShippingOption = shippingOptionPage.ShippingPageId.ToString();
                                    currentCart.ShippingTotal = shippingOptionPage.ShippingFee;
                                }

                                currentCart.ComputeTotalWithShippingValue();
                            }
                        }

                        //check if the cart page has got items saved and add them to the saved cart
                        if (cartPage.Children("cartItem").Any())
                        {
                            //reset the cart items before we add any new ones
                            currentCart.CartItems.Clear();
                            foreach (var cartItemPage in cartPage.Children("cartItem"))
                            {
                                var savedCartItem = new CartItem();
                                IPublishedContent mainProductPage = null;
                                //get the cart's product page
                                if (cartItemPage.HasProperty("product") && cartItemPage.HasValue("product"))
                                {
                                    //save the umbraco cart page id to the cart item
                                    savedCartItem.CartItemPageId = cartItemPage.Id;

                                    var cartProduct = cartItemPage.Value<List<IPublishedContent>>("product")
                                        .FirstOrDefault();
                                    if (cartProduct?.Id > 0)
                                    {
                                        savedCartItem.ProductLinePage = cartProduct;
                                        savedCartItem.MainProductPage = cartProduct.Parent;
                                        //save the main product page
                                        mainProductPage = cartProduct.Parent;
                                    }
                                }

                                //get the cart items quantity
                                if (cartItemPage.HasProperty("quantity") && cartItemPage.HasValue("quantity"))
                                {
                                    savedCartItem.Quantity = cartItemPage.Value<int>("quantity");
                                }

                                //get the cart items total
                                if (cartItemPage.HasProperty("itemTotal") && cartItemPage.HasValue("itemTotal"))
                                {
                                    savedCartItem.Price = cartItemPage.Value<decimal>("itemTotal");
                                }

                                //get the cart items price discount
                                if (cartItemPage.HasProperty("itemDiscount") && cartItemPage.HasValue("itemDiscount"))
                                {
                                    savedCartItem.PriceDiscount = cartItemPage.Value<decimal>("itemDiscount");
                                }

                                //get the cart items description
                                if (cartItemPage.HasProperty("itemDescription") &&
                                    cartItemPage.HasValue("itemDescription"))
                                {
                                    savedCartItem.Description = cartItemPage.Value<string>("itemDescription");
                                }

                                //add the cart item image
                                savedCartItem.CartItemImage = "/Images/Nature-Quest-Product-Default.png";
                                if (cartItemPage.HasProperty("product") && cartItemPage.HasValue("product"))
                                {
                                    var cartProduct = cartItemPage
                                        .Value<IEnumerable<IPublishedContent>>("product").FirstOrDefault();
                                    if (cartProduct?.Id > 0 &&
                                        cartProduct.HasProperty("variantImage")
                                        && cartProduct.HasValue("variantImage"))
                                    {
                                        var variantImage = cartProduct.Value<IPublishedContent>("variantImage");
                                        if (variantImage != null && variantImage?.Id != 0)
                                        {
                                            //get the image url
                                            var defaultCropSize = variantImage.GetCropUrl("product");
                                            var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                                ? defaultCropSize
                                                : variantImage.GetCropUrl(350, 500);
                                            if (!string.IsNullOrWhiteSpace(productImagelink))
                                            {
                                                savedCartItem.CartItemImage = productImagelink;
                                            }
                                        }
                                    }
                                }
                                else if (mainProductPage.HasProperty("productImages") &&
                                         mainProductPage.HasValue("productImages"))
                                {
                                    //set feature product image
                                    var productImage = mainProductPage
                                        .Value<IEnumerable<IPublishedContent>>("productImages").FirstOrDefault();
                                    if (productImage?.Id != 0)
                                    {
                                        //get the image url
                                        var defaultCropSize = productImage.GetCropUrl("product");
                                        var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                            ? defaultCropSize
                                            : productImage.GetCropUrl(350, 500);
                                        if (!string.IsNullOrWhiteSpace(productImagelink))
                                        {
                                            savedCartItem.CartItemImage = productImagelink;
                                        }
                                    }
                                }

                                //add the cart item to the carts list
                                currentCart.CartItems.Add(savedCartItem);
                            }
                        }
                    }
                    else
                    {
                        // if we cant find an existing cart create one and use that
                        var newCartPage = CreateMemberCartPage(_globalCartsPage, cartMember);
                        // if the new cart page is not null create the session with that and return it
                        if (newCartPage != null && newCartPage.Id != 0)
                        {
                            //save the cart member and member cart page to the current cart
                            currentCart.CartMember = cartMember;
                            currentCart.MemberCartPage = newCartPage;
                        }
                    }
                }
            }

            //clear the current session and add the new cart
            HttpContext.Current.Session["Cart"] = null;
            HttpContext.Current.Session["Cart"] = currentCart;
            // return the saved cart
            return currentCart;
        }

        /// <summary>
        /// get and update the system order number
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public string SystemOrderId(SiteShoppingCart currentShoppingCart)
        {
            //check if we have the shopping cart to use
            if (currentShoppingCart == null && StoreDetailsPage == null)
            {
                return string.Empty;
            }

            //check if the cart already has got an order number set
            if (!string.IsNullOrWhiteSpace(currentShoppingCart?.SystemOrderId))
            {
                return currentShoppingCart.SystemOrderId;
            }

            //get the order number details to use
            var orderPrefix = "NQ";
            //get the stored order prefix details
            if (StoreDetailsPage.HasProperty("orderPrefix") &&
                StoreDetailsPage.HasValue("orderPrefix"))
            {
                orderPrefix = StoreDetailsPage.Value<string>("orderPrefix");
            }

            var currentOrderNumber = 0;
            //get the stored order prefix details
            if (StoreDetailsPage.HasProperty("currentOrderNumber") &&
                StoreDetailsPage.HasValue("currentOrderNumber"))
            {
                currentOrderNumber = StoreDetailsPage.Value<int>("currentOrderNumber");
            }

            //check if we have the current order number
            if (currentOrderNumber == 0)
            {
                //as a back up just generate a random number to use
                var random = new Random();
                currentOrderNumber = random.Next(10000, 100000);
            }
            //for testing use the order number 999999
            //currentOrderNumber = 999999;

            //generate the order number to use
            var systemOrderNumber = $"{orderPrefix}-{currentOrderNumber}";

            //save the order number on the cart
            if (currentShoppingCart != null)
            {
                currentShoppingCart.SystemOrderId = systemOrderNumber;
                //once we have this saved update the stored order number to the next 1 for the next order
                var storeOrderContentPage = _contentService.GetById(StoreDetailsPage.Id);
                if (storeOrderContentPage?.Id > 0)
                {
                    // ReSharper disable once RedundantAssignment
                    var updatedOrderNumber = currentOrderNumber + 1;
                    storeOrderContentPage.SetValue("currentOrderNumber", updatedOrderNumber);
                    //save the content item
                    var saveResult = _contentService.SaveAndPublish(storeOrderContentPage);

                    if (saveResult.Success)
                    {
                        return systemOrderNumber;
                    }
                }
            }
            //if we get this far something went wrong
            return string.Empty;
        }

        /// <summary>
        /// create the cart page for a member
        /// </summary>
        /// <param name="cartsPage"></param>
        /// <param name="cartMember"></param>
        /// <returns></returns>
        public IPublishedContent CreateMemberCartPage(IPublishedContent cartsPage, IMember cartMember)
        {
            try
            {
                //check if we have the carts page for the parent
                if (cartsPage != null && cartsPage.Id != 0 && cartsPage.ContentType.Alias == GlobalCartsPageAlias
                    && cartMember != null && cartMember.Id != 0 && !string.IsNullOrWhiteSpace(cartMember.Email))
                {
                    //create the name to use
                    var memberName = "Member";
                    var memberNameProperty =
                        cartMember.Properties.FirstOrDefault(property => property.Alias == "fullName");
                    if (!string.IsNullOrWhiteSpace((string)memberNameProperty?.GetValue()))
                    {
                        memberName = (string)memberNameProperty.GetValue();
                    }

                    //generate the cart name
                    var memberCartName = $"{memberName.Trim().Replace(" ", "-")}-({cartMember.Email})-Cart";
                    //get the parent carts page
                    var cartsParentPage = _contentService.GetById(cartsPage.Id);

                    //get the stored cart page for the member
                    var existingMemberCartPage = _globalCartsPage.Children.FirstOrDefault(page => page.HasProperty("cartMember") &&
                                                                                    page.HasValue("cartMember") &&
                                                                                    page.Value<IPublishedContent>(
                                                                                        "cartMember")?.Name ==
                                                                                    cartMember.Name);
                    //if we have have an existing one use that
                    var newMemberCartPage = existingMemberCartPage?.Id > 0 ?
                        _contentService.GetById(existingMemberCartPage.Id) : 
                        _contentService.CreateAndSave(memberCartName, cartsParentPage, CartPageAlias);

                    
                    //check if the new page has been created
                    if (newMemberCartPage != null)
                    {
                        //set the properties
                        newMemberCartPage.SetValue("cartMember", cartMember.Id);
                        newMemberCartPage.SetValue("activeCart", true);

                        //add some notes on the cart item
                        var currentComments = $"{newMemberCartPage.GetValue("cartNotes")} {System.Environment.NewLine} " +
                                              $"- {DateTime.Now.ToShortDateString()} - New cart created for: {cartMember.Name}";

                        newMemberCartPage.SetValue("cartNotes", currentComments);
                        //save the content item
                        var saveResult = _contentService.SaveAndPublish(newMemberCartPage);

                        if (saveResult.Success)
                        {
                            _logger.Info(Type.GetType("ShoppingService"),
                                $"A new shopping cart has been created for member: {cartMember.Email}");
                        }

                        // convert the content to the ipublished content to return
                        var cartPublishedPage = _umbracoHelper.Content(newMemberCartPage.Id);
                        //check if we now have the published page, send an email out and return this page
                        if (cartPublishedPage != null)
                        {
                            if (existingMemberCartPage == null)
                            {
                                //create the admin email to notify of a new cart page
                                var newCartSubject = "A new member cart has been created.";
                                var newCartBody =
                                    $"<p>A new member cart with the name: {cartPublishedPage.Name} has been created." +
                                    "<br /> <br />Regards, <br /> Website Team</p>";
                                //create the admin email
                                var adminNewCartMessage = MailHelper.CreateSingleEmail(
                                    _fromEmailAddress,
                                    _systemEmailAddress,
                                    newCartSubject,
                                    "",
                                    newCartBody);
                                //send the admin email
                                var unused1 = SendGridEmail(adminNewCartMessage, autoAddAdminBcc: true);
                            }
                            
                            //return the page
                            return cartPublishedPage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex,
                    $"Error creating new cart for member :{cartMember?.Email}");
                //send an admin email with the error
                var errorMessage =
                    $"Error creating new cart for member :{cartMember?.Email}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");
            }

            //if we don''t have the cart page and member then just return null
            return null;
        }

        /// <summary>
        /// Add the product to the shopping cart
        /// </summary>
        /// <param name="productModel"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        public bool AddProductToCart(
            ProductModel productModel,
            SiteShoppingCart currentShoppingCart,
            out string resultMessage)
        {
            resultMessage = "There was an error adding the product to the cart";
            // check if we have the product model and cart to use
            if (productModel == null || productModel.SelectedQuantity <= 0 || currentShoppingCart == null)
            {
                resultMessage = "The product cant be added to the cart";
                return false;
            }

            //try and add the product
            try
            {
                //get the product from the model
                var selectedPriceProduct = _umbracoHelper.Content(productModel.SelectedPricePageId);
                if (selectedPriceProduct != null)
                {
                    //get the parent product page
                    var productPage = selectedPriceProduct.Parent;
                    var selectedProductModel = _productsService.GetProductModel(productPage);
                    var modelPricePage = selectedProductModel.ProductPrices.FirstOrDefault(
                        price => price.ProductPricePage.Id == selectedPriceProduct.Id);

                    //check if we have the model and the price page to use
                    if (modelPricePage != null)
                    {
                        var cartPrice = modelPricePage.SalePrice == 0
                            ? modelPricePage.ProductPrice
                            : modelPricePage.SalePrice;
                        var cartDiscount = modelPricePage.SalePrice == 0
                            ? 0
                            : modelPricePage.ProductPrice - modelPricePage.SalePrice;
                        //check if the cart has got any items with the same price product
                        if (currentShoppingCart.CartItems.Any() &&
                            currentShoppingCart.CartItems.FirstOrDefault(item =>
                                item.ProductLinePage.Id == selectedPriceProduct.Id) != null)
                        {
                            //get the current item in the cart to update
                            var currentCartItem = currentShoppingCart.CartItems.FirstOrDefault(item =>
                                item.ProductLinePage.Id == selectedPriceProduct.Id);
                            //increase the quantity
                            if (currentCartItem != null)
                            {
                                //remove the item from the cart
                                currentShoppingCart.CartItems.Remove(currentCartItem);
                                //update the item
                                currentCartItem.Quantity += productModel.SelectedQuantity;
                                var cartDescription = $"{productPage.Name}-{selectedPriceProduct.Name}, " +
                                                      $"quantity :{currentCartItem.Quantity} @ {cartPrice:c} ";
                                currentCartItem.Description = cartDescription;
                                currentCartItem.Price = cartPrice;
                                currentCartItem.PriceDiscount =
                                    cartDiscount == 0 ? 0 : cartDiscount * currentCartItem.Quantity;
                                currentCartItem.ProductLinePage = selectedPriceProduct;
                                currentCartItem.MainProductPage = productPage;
                                currentCartItem.ProductVariantCode = modelPricePage.ProductVariantCode;
                                //add the cart item page id to the cart item
                                currentCartItem.CartItemPageId = 0;
                                //add it back to the cart
                                currentShoppingCart.CartItems.Add(currentCartItem);

                                //update the member cart if the member is logged in
                                if (currentShoppingCart.CartMember != null &&
                                    currentShoppingCart.MemberCartPage != null)
                                {
                                    var memberCartUpdate = UpdateMemberSavedCart(
                                        currentShoppingCart,
                                        selectedPriceProduct,
                                        false,
                                        out int cartItemPageId);
                                    //if there was an error updating the umbraco cart then return the error
                                    if (!memberCartUpdate)
                                    {
                                        resultMessage = "There was an error updating the saved cart";
                                        return false;
                                    }

                                    //add the cart item page id to the cart item
                                    currentCartItem.CartItemPageId = cartItemPageId;
                                }
                            }
                        }
                        //this item is not in the cart so add it
                        else
                        {
                            //create a new cart item to add
                            var newCartItem = new CartItem
                            {
                                Quantity = productModel.SelectedQuantity,
                                Price = cartPrice,
                                PriceDiscount = cartDiscount == 0 ? 0 : cartDiscount * productModel.SelectedQuantity,
                                Description = $"{productPage.Name}-{selectedPriceProduct.Name}, " +
                                              $"quantity :{productModel.SelectedQuantity} @ {cartPrice:c} ",
                                ProductLinePage = selectedPriceProduct,
                                MainProductPage = productPage,
                                CartItemImage = "/Images/Nature-Quest-Product-Default.png",
                                CartItemPageId = selectedPriceProduct.Id,
                                ProductVariantCode = modelPricePage.ProductVariantCode
                            };

                            if (selectedPriceProduct.HasProperty("variantImage") && selectedPriceProduct.HasValue("variantImage"))
                            {
                                //add the cart item image
                                var variantImage =
                                    selectedPriceProduct.Value<IPublishedContent>("variantImage");
                                if (variantImage?.Id != 0)
                                {
                                    //get the image url
                                    var defaultCropSize = variantImage.GetCropUrl("product");
                                    var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                        ? defaultCropSize
                                        : variantImage.GetCropUrl(350, 500);
                                    if (!string.IsNullOrWhiteSpace(productImagelink))
                                    {
                                        newCartItem.CartItemImage = productImagelink;
                                    }
                                }
                            }

                            else if (productPage.HasProperty("productImages") && productPage.HasValue("productImages"))
                            {
                                //set feature product image
                                var productImage = productPage.Value<IEnumerable<IPublishedContent>>("productImages")
                                    .FirstOrDefault();
                                if (productImage?.Id != 0)
                                {
                                    //get the image url
                                    var defaultCropSize = productImage.GetCropUrl("product");
                                    var productImagelink = !string.IsNullOrEmpty(defaultCropSize)
                                        ? defaultCropSize
                                        : productImage.GetCropUrl(350, 500);
                                    if (!string.IsNullOrWhiteSpace(productImagelink))
                                    {
                                        newCartItem.CartItemImage = productImagelink;
                                    }
                                }
                            }

                            //add it to the cart
                            currentShoppingCart.CartItems.Add(newCartItem);

                            //update the member cart if the member is logged in
                            if (currentShoppingCart.CartMember != null && currentShoppingCart.MemberCartPage != null)
                            {
                                var memberCartUpdate = UpdateMemberSavedCart(
                                    currentShoppingCart,
                                    selectedPriceProduct,
                                    true,
                                    out int cartItemPageId);
                                //if there was an error updating the umbraco cart then return the error
                                if (!memberCartUpdate)
                                {
                                    resultMessage = "There was an error updating the saved cart";
                                    return false;
                                }

                                //add the cart item page id to the cart item
                                newCartItem.CartItemPageId = cartItemPageId;
                            }
                        }

                        //update the cart
                        //clear the current session and add the new cart
                        HttpContext.Current.Session["Cart"] = null;
                        HttpContext.Current.Session["Cart"] = currentShoppingCart;
                        resultMessage =
                            $"The product: {productPage.Name}-{selectedPriceProduct.Name} has been added to the cart";
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error adding the product:{productModel.ProductPage?.Name} to the cart.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                resultMessage = errorMessage;
                return false;
            }

            //if we get this far, something has gone wrong somewhere
            return false;
        }

        /// <summary>
        /// Clear all items from the current cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        public bool ClearShoppingCart(SiteShoppingCart currentShoppingCart, out string resultMessage)
        {
            resultMessage = "There was an error clearing the shopping cart";
            // check if we have the product model and cart to use
            if (currentShoppingCart == null || !currentShoppingCart.CartItems.Any())
            {
                resultMessage = "The shopping cart is empty";
                return true;
            }

            // try and clear the cart
            try
            {
                // clear the cart items
                currentShoppingCart.CartItems.Clear();
                //if we have a logged in member with a backend cart clear that as well
                if (currentShoppingCart.CartMember != null && currentShoppingCart.MemberCartPage?.Id > 0)
                {
                    //use the service to delete the saved items
                    var memberCartUpdated = UpdateMemberCartItems(currentShoppingCart, true, out resultMessage);
                    //check the result of clearing the saved cart
                    if (memberCartUpdated)
                    {
                        //return the flag
                        return true;
                    }

                    // the error flag here
                    return false;
                }

                //also clear the cart session if there is one
                currentShoppingCart.StripeCartSession = null;
                currentShoppingCart.StripeCartSessionId = string.Empty;
                //reset the session cart
                HttpContext.Current.Session["Cart"] = null;
                //set the result message
                resultMessage = "The shopping cart has been cleared";
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error clearing the shopping cart.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                resultMessage = errorMessage;
                return false;
            }
        }

        /// <summary>
        /// Remove a cart item from the shopping cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="cartItemPage"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        public bool RemoveShoppingCartItem(
            SiteShoppingCart currentShoppingCart,
            IPublishedContent cartItemPage,
            out string resultMessage)
        {
            resultMessage = "There was an error removing the item from the shopping cart";
            // check if we have the product model and cart to use
            if (currentShoppingCart == null || !currentShoppingCart.CartItems.Any())
            {
                resultMessage = "The shopping cart is empty";
                return false;
            }

            // try and remove the item from the cart
            try
            {
                // get the cart item to remove
                var cartItem =
                    currentShoppingCart.CartItems.FirstOrDefault(item => item.CartItemPageId == cartItemPage.Id);
                if (cartItem != null)
                {
                    //remove the cart item
                    currentShoppingCart.CartItems.Remove(cartItem);
                    //if we have a logged in member with a backend cart remove it from the saved cart
                    if (currentShoppingCart.CartMember != null && currentShoppingCart.MemberCartPage?.Id > 0)
                    {
                        //use the service to delete the cart item
                        var memberCartUpdated =
                            UpdateMemberCartItems(currentShoppingCart, false, out resultMessage, cartItemPage);
                        //check the result of delete the cart item
                        if (memberCartUpdated)
                        {
                            //return the flag
                            return true;
                        }

                        // the error flag here
                        return false;
                    }

                    //set the result message
                    resultMessage = "The shopping cart item has been removed";
                    return true;
                }

                //set the result message
                resultMessage = "The shopping cart item is not in the cart";
                return false;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error clearing the shopping cart.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                resultMessage = errorMessage;
                return false;
            }
        }

        /// <summary>
        /// Update a cart item from the shopping cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="cartItemPage"></param>
        /// <param name="cartItemQuantity"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        public bool UpdateShoppingCartItem(
            SiteShoppingCart currentShoppingCart,
            IPublishedContent cartItemPage,
            int cartItemQuantity,
            out string resultMessage)
        {
            resultMessage = "There was an error updating the item from the shopping cart";
            // check if we have the product model and cart to use
            if (currentShoppingCart == null || !currentShoppingCart.CartItems.Any())
            {
                resultMessage = "The shopping cart is empty";
                return false;
            }

            // try and update the item from the cart
            try
            {
                // get the cart item to update
                var cartItem =
                    currentShoppingCart.CartItems.FirstOrDefault(item => item.ProductLinePage.Id == cartItemPage.Id);
                if (cartItem != null)
                {
                    //update the cart item
                    cartItem.Quantity = cartItemQuantity;
                    cartItem.Description = $"{cartItem.MainProductPage.Name}-{cartItem.ProductLinePage.Name}, " +
                                           $"quantity :{cartItemQuantity} @ {cartItem.Price:c} ";
                    //if we have a logged in member with a backend cart remove it from the saved cart
                    if (currentShoppingCart.CartMember != null && currentShoppingCart.MemberCartPage?.Id > 0)
                    {
                        //use the service to delete the cart item
                        var memberCartUpdated = UpdateMemberSavedCart(
                            currentShoppingCart,
                            cartItemPage,
                            false,
                            out int cartItemPageId);
                        //check the result of delete the cart item
                        if (memberCartUpdated && cartItemPageId > 0)
                        {
                            //return the flag
                            return true;
                        }

                        // the error flag here
                        return false;
                    }

                    //set the result message
                    resultMessage = "The shopping cart item has been removed";
                    return true;
                }

                //set the result message
                resultMessage = "The shopping cart item is not in the cart";
                return false;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error clearing the shopping cart.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                resultMessage = errorMessage;
                return false;
            }
        }

        /// <summary>
        /// Update the cart items, either delete all of them or just 1
        /// </summary>
        /// <param name="memberShoppingCart"></param>
        /// <param name="clearAllItems"></param>
        /// <param name="cartItemsUpdateMessage"></param>
        /// <param name="priceProductPage"></param>
        /// <returns></returns>
        public bool UpdateMemberCartItems(
            SiteShoppingCart memberShoppingCart,
            bool clearAllItems,
            out string cartItemsUpdateMessage,
            IPublishedContent priceProductPage = null)
        {
            //set the default message to return
            cartItemsUpdateMessage = "There was an error updating you saved cart";
            //check if we have the cart to update
            if (memberShoppingCart?.MemberCartPage == null)
            {
                return false;
            }

            //try and update the saved cart
            try
            {
                var memberCartPage = memberShoppingCart.MemberCartPage;
                //check if we are clearing all items
                if (clearAllItems && priceProductPage == null)
                {
                    var allItemsDeleted = false;
                    // go through each of the child items and delete them
                    foreach (var cartPage in memberCartPage.Children)
                    {
                        var contentCartPage = _contentService.GetById(cartPage.Id);
                        //delete the page
                        var deleteResult = _contentService.MoveToRecycleBin(contentCartPage);
                        if (deleteResult.Success)
                        {
                            allItemsDeleted = true;
                        }
                    }

                    //then delete the cart page
                    var contentCartParentPage = _contentService.GetById(memberCartPage.Id);
                    var deleteCartResult = _contentService.MoveToRecycleBin(contentCartParentPage);
                    if (deleteCartResult.Success)
                    {
                        allItemsDeleted = true;
                    }


                    //after deleting all of them if the flag is false then there was an error somewhere
                    if (allItemsDeleted)
                    {
                        _logger.Info(Type.GetType("ShoppingService"),
                            $"The cart items for user {memberShoppingCart.CartMember.Email} has been deleted.");
                        cartItemsUpdateMessage = "The cart items have been deleted.";
                        return true;
                    }

                    //there was an error saving the content item
                    _logger.Error(Type.GetType("ShoppingService"),
                        $"There was an error deleting the cart items for user {memberShoppingCart.CartMember.Email}.");
                    return false;
                }

                //we are not deleting all the items, check if the product to delete
                if (!clearAllItems && priceProductPage != null)
                {
                    //get the cart item that matches the product model to use for values
                    var productCartItem = memberCartPage.Children.FirstOrDefault(
                        cartItem => cartItem.Id == priceProductPage.Id);
                    //check if we have the cart item to match the price page
                    if (productCartItem != null)
                    {
                        var cartItemContentPage = _contentService.GetById(productCartItem.Id);
                        //delete the page
                        var deleteResult = _contentService.MoveToRecycleBin(cartItemContentPage);
                        if (deleteResult.Success)
                        {
                            _logger.Info(Type.GetType("ShoppingService"),
                                $"The cart item: {cartItemContentPage.Name}  for user {memberShoppingCart.CartMember.Email} has been deleted.");
                            cartItemsUpdateMessage = $"The cart item: {cartItemContentPage.Name} has been deleted.";
                            return true;
                        }

                        //there was an error saving the content item
                        _logger.Error(Type.GetType("ShoppingService"),
                            $"There was an error deleting the cart item: {cartItemContentPage.Name} " +
                            $"for user {memberShoppingCart.CartMember.Email}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating the saved cart for:{memberShoppingCart.CartMember?.Email}.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error updating cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //return false
                return false;
            }

            //if we get this far something has gone wrong
            return false;
        }

        /// <summary>
        /// update the umbraco cart item for the logged in member
        /// </summary>
        /// <param name="memberShoppingCart"></param>
        /// <param name="priceProductPage"></param>
        /// <param name="isNewCartItem"></param>
        /// <param name="cartItemPageId"></param>
        /// <returns></returns>
        public bool UpdateMemberSavedCart(
            SiteShoppingCart memberShoppingCart,
            IPublishedContent priceProductPage,
            bool isNewCartItem,
            out int cartItemPageId)
        {
            //set the default page id to return
            cartItemPageId = 0;
            //check if we have the cart and page
            if (memberShoppingCart == null && priceProductPage == null)
            {
                return false;
            }

            //try and update the saved cart
            try
            {
                //get the cart item that matches the product model to use for values
                var productCartItem = memberShoppingCart?.CartItems.FirstOrDefault(
                    item => item.ProductLinePage.Id == priceProductPage.Id);

                if (memberShoppingCart?.MemberCartPage != null && productCartItem != null)
                {
                    var memberCartPage = memberShoppingCart.MemberCartPage;
                    //get the parent cart item page
                    var cartItemPage = _contentService.GetById(memberCartPage.Id);
                    IContent cartItemContentPage = null;

                    //check if we are creating a new cart item or update
                    if (isNewCartItem)
                    {
                        //generate the cart item name
                        var newCartItemName = $"{priceProductPage.Parent.Name.Trim().Replace(" ", "-")}" +
                                              $"-{priceProductPage.Name.Trim().Replace(" ", "-")}";

                        //use the content service to create the cart item page
                        cartItemContentPage =
                            _contentService.CreateAndSave(newCartItemName, cartItemPage, CartPageItemAlias);
                    }
                    //we are updating an existing cart item so we need to get it first
                    else
                    {
                        var memberExistingCartPage = memberCartPage.Children.FirstOrDefault(page =>
                            page.HasProperty("productId") &&
                            page.Value<int>("productId") == productCartItem.ProductLinePage.Id);
                        if (memberExistingCartPage != null)
                        {
                            cartItemContentPage = _contentService.GetById(memberExistingCartPage.Id);
                        }
                    }

                    //check if the new cart item page has been created
                    if (cartItemContentPage != null)
                    {
                        //set the cart page id
                        cartItemPageId = cartItemContentPage.Id;

                        //set the product value to add
                        var productIdsList = new List<string>
                        {
                            _contentService.GetById(productCartItem.ProductLinePage.Id).GetUdi().ToString()
                        };

                        //set the properties
                        cartItemContentPage.SetValue("product", string.Join(",", productIdsList));
                        cartItemContentPage.SetValue("quantity", productCartItem.Quantity);
                        cartItemContentPage.SetValue("itemTotal", productCartItem.Price);
                        cartItemContentPage.SetValue("itemDiscount", productCartItem.PriceDiscount);
                        cartItemContentPage.SetValue("itemDescription", productCartItem.Description);
                        cartItemContentPage.SetValue("productId", productCartItem.ProductLinePage.Id);
                        cartItemContentPage.SetValue("productCode", productCartItem.ProductVariantCode);

                        //save the content item
                        var saveResult = _contentService.SaveAndPublish(cartItemContentPage);

                        if (saveResult.Success)
                        {
                            //add some notes on the cart item
                            var currentComments = $"{cartItemPage.GetValue("cartNotes")} {System.Environment.NewLine} " +
                                                  $"- {DateTime.Now.ToShortDateString()} - {productCartItem.Description}";

                            cartItemPage.SetValue("cartNotes", currentComments);
                            //publish the parent page
                            var cartPageUpdated = _contentService.SaveAndPublish(cartItemPage);
                            if (cartPageUpdated.Success)
                            {
                                var updatedCartPage = _umbracoHelper.Content(cartPageUpdated.Content.Id);
                                if (updatedCartPage?.Id > 0)
                                {
                                    memberShoppingCart.MemberCartPage = updatedCartPage;
                                }
                                _logger.Info(Type.GetType("ShoppingService"),
                                    $"The cart item: {cartItemContentPage.Name}  for user {memberShoppingCart.CartMember.Email} has been added/updated");
                                return true;
                            }
                        }

                        //there was an error saving the content item
                        _logger.Error(Type.GetType("ShoppingService"),
                            $"There was an error updating the cart item: {cartItemContentPage.Name} " +
                            $"for user {memberShoppingCart.CartMember.Email} has been added/updated");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating the saved cart for:{memberShoppingCart?.CartMember?.Email}.";
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error updating cart on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //return false
                return false;
            }

            //if we get this far something has gone wrong
            return false;
        }

        /// <summary>
        /// update the shipping for the shopping cart
        /// </summary>
        /// <param name="shippingOption"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        public bool AddShippingToCart(
            ShippingOption shippingOption,
            SiteShoppingCart currentShoppingCart,
            out string resultMessage)
        {
            resultMessage = "There was an error updating the shipping for the cart";
            // check if we have the shipping option and cart to use
            if (shippingOption == null || shippingOption.ShippingPageId <= 0 || currentShoppingCart == null)
            {
                return false;
            }

            //update the cart shipping option and re-calculate the shipping total
            currentShoppingCart.SelectedShippingOption = shippingOption.ShippingPageId.ToString();
            currentShoppingCart.ShippingTotal = shippingOption.ShippingFee;
            //get the current selected option
            var currentSelectedOption =
                currentShoppingCart.SelectShippingOptions.FirstOrDefault(option => option.Selected);
            if (currentSelectedOption != null)
            {
                currentSelectedOption.Selected = false;
            }

            //set the new selected option
            var newSelectedOption =
                currentShoppingCart.SelectShippingOptions.FirstOrDefault(option =>
                    option.Value == shippingOption.ShippingPageId.ToString());
            if (newSelectedOption != null)
            {
                newSelectedOption.Selected = true;
            }

            currentShoppingCart.ComputeTotalWithShippingValue();

            //try and add the shipping to the saved cart
            if (currentShoppingCart.CartMember != null && currentShoppingCart.MemberCartPage != null)
            {
                try
                {
                    //get the shipping cart content and member cart page to update
                    var shippingOptionContent = _contentService.GetById(shippingOption.ShippingPageId);
                    var cartMemberContent = _contentService.GetById(currentShoppingCart.MemberCartPage.Id);
                    if (shippingOptionContent != null && cartMemberContent != null)
                    {
                        //update the member cart page
                        var shippingOptionUdi = shippingOptionContent.GetUdi().ToString();
                        cartMemberContent.SetValue("cartShipping", shippingOptionUdi);
                        //save the content item
                        var saveResult = _contentService.SaveAndPublish(cartMemberContent);

                        //check the save results
                        if (!saveResult.Success)
                        {
                            //there was an error saving the content item
                            _logger.Error(Type.GetType("ShoppingService"),
                                "There was an error updating the shipping on your saved cart");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage =
                        $"Error adding the shopping option:{shippingOption.ShippingPricePage?.Name} to the cart.";
                    //there was an error updating the shipping option 
                    _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                    //send an admin email with the error
                    var errorEmailMessage =
                        $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                    var errorSubject = $"Error Creating new cart on {_siteName}";

                    //system email
                    var systemMessage = MailHelper.CreateSingleEmail(
                        _fromEmailAddress,
                        _systemEmailAddress,
                        errorSubject,
                        "",
                        errorEmailMessage);
                    var systemMessageSent = SendGridEmail(systemMessage);
                    _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                    //set the error message to return
                    resultMessage = errorMessage;
                    return false;
                }
            }

            //if we get this far all has gone well, return
            resultMessage = "Your shipping has been updated on the cart";
            return true;
        }

        /// <summary>
        /// create the order page for a member
        /// </summary>
        /// <param name="ordersPage"></param>
        /// <param name="cartMember"></param>
        /// <param name="orderId"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="paymentMethod"></param>
        /// <returns></returns>
        public IPublishedContent CreateMemberOrderPage(
            IPublishedContent ordersPage,
            IMember cartMember,
            string orderId,
            SiteShoppingCart currentShoppingCart,
            string paymentMethod)
        {
            try
            {
                //check if we have the orders page for the parent
                if (ordersPage?.Id != 0 && ordersPage?.ContentType.Alias == GlobalOrdersPageAlias
                                        && cartMember?.Id != 0 && !string.IsNullOrWhiteSpace(cartMember?.Email)
                                        && !string.IsNullOrWhiteSpace(orderId)
                                        && !string.IsNullOrWhiteSpace(paymentMethod))
                {
                    //create the name to use
                    var memberName = "Member";
                    var memberNameProperty =
                        cartMember.Properties.FirstOrDefault(property => property.Alias == "fullName");
                    if (!string.IsNullOrWhiteSpace((string)memberNameProperty?.GetValue()))
                    {
                        memberName = (string)memberNameProperty.GetValue();
                    }

                    //generate the order summary
                    var orderSummary = CartSummaryHtml(currentShoppingCart, new StringBuilder());

                    //generate the cart name
                    var memberOrderName = $"{memberName.Trim().Replace(" ", "-")}-{paymentMethod}-{orderId}";
                    //get the parent carts page
                    var ordersParentPage = _contentService.GetById(ordersPage.Id);
                    //use the content service to create the cart page
                    var newMemberOrderPage =
                        _contentService.CreateAndSave(memberOrderName, ordersParentPage, OrderPageAlias);
                    //check if the new page has been created
                    if (newMemberOrderPage != null)
                    {
                        //get the member cart page from the cart to move to the order
                        var memberCartPage = currentShoppingCart.MemberCartPage;
                        //set the payment method
                        var paymentMethodValue = JsonConvert.SerializeObject(new[] { paymentMethod });
                        //set the properties 
                        newMemberOrderPage.SetValue("orderMember", cartMember.Id);
                        newMemberOrderPage.SetValue("isOrder", true);
                        newMemberOrderPage.SetValue("orderId", orderId);
                        newMemberOrderPage.SetValue("paymentMethod", paymentMethodValue);
                        newMemberOrderPage.SetValue("paymentId", currentShoppingCart.StripeCartSession.PaymentIntentId);

                        //save the order summary
                        newMemberOrderPage.SetValue("orderSummary", orderSummary);

                        //set the shipping on the order
                        if (!string.IsNullOrWhiteSpace(currentShoppingCart.SelectedShippingOption) &&
                            Convert.ToInt32(currentShoppingCart.SelectedShippingOption) > 0)
                        {
                            var selectedCartShipping = Convert.ToInt32(currentShoppingCart.SelectedShippingOption);
                            var shippingContentPage = _contentService.GetById(selectedCartShipping);
                            if (shippingContentPage?.Id > 0)
                            {
                                var shippingOptionUdi = shippingContentPage.GetUdi().ToString();
                                newMemberOrderPage.SetValue("orderShipping", shippingOptionUdi);
                            }
                        }

                        //save the content item
                        var saveResult = _contentService.SaveAndPublish(newMemberOrderPage);

                        //move the cart items to the order
                        if (saveResult.Success && memberCartPage.Children.Any())
                        {
                            foreach (var cartPage in memberCartPage.Children)
                            {
                                var cartContentPage = _contentService.GetById(cartPage.Id);
                                _contentService.Copy(cartContentPage, newMemberOrderPage.Id, true);
                            }
                        }

                        //save the content item
                        saveResult = _contentService.SaveAndPublish(newMemberOrderPage);

                        if (saveResult.Success)
                        {
                            _logger.Info(Type.GetType("ShoppingService"),
                                $"A new shop order has been created for member: {cartMember.Email}");
                        }

                        // convert the content to the ipublished content to return
                        var orderPublishedPage = _umbracoHelper.Content(newMemberOrderPage.Id);
                        //check if we now have the published page, send an email out and return this page
                        if (orderPublishedPage != null)
                        {
                            //return the page
                            return orderPublishedPage;
                        }
                    }
                }
                else
                {
                    //log an error
                    _logger.Error(Type.GetType("ShoppingService"),
                        $"Error creating new order for member :{cartMember?.Email}");
                }
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex,
                    $"Error creating new order for member :{cartMember?.Email}");
                //send an admin email with the error
                var errorMessage = $"Error creating new order for member :{cartMember?.Email}." +
                                   $"<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error Creating new order on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");
            }

            //if we don''t have the cart page and member then just return null
            return null;
        }

        #region Stripe Cart Methods

        /// <summary>
        /// Get the carts stripe session
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public string GetCartStripeSessionId(SiteShoppingCart currentShoppingCart)
        {
            var stripeSessionId = string.Empty;
            //check if the current cart has already got 1
            if (!string.IsNullOrWhiteSpace(currentShoppingCart.StripeCartSession?.Id))
            {
                stripeSessionId = currentShoppingCart.StripeCartSession.Id;
                return stripeSessionId;
            }

            //create the stripe line options
            if (currentShoppingCart.CartItems.Any())
            {
                var lineItemOptions = new List<SessionLineItemOptions>();
                //get the shipping details
                var cartShippingDetails = currentShoppingCart.CartShippingDetails;

                foreach (var cartItem in currentShoppingCart.CartItems)
                {
                    var stripeItemOption = new SessionLineItemOptions
                    {
                        Name = $"{cartItem.MainProductPage.Name}-{cartItem.ProductLinePage.Name}",
                        Description = $"{cartItem.Description} - Product Code: {cartItem.ProductVariantCode}",
                        Amount = (int)(cartItem.Price * 100),
                        Currency = "aud",
                        Quantity = cartItem.Quantity,
                        Images = new List<string> { $"{HomePage.UrlAbsolute()}{cartItem.CartItemImage}" }
                    };
                    //add it to the list
                    lineItemOptions.Add(stripeItemOption);
                }

                //add the shipping
                var shippingOption = new SessionLineItemOptions
                {
                    Name = $"{cartShippingDetails.ShippingOptionDetails}",
                    Description = cartShippingDetails.ShippingOptionDetails,
                    Amount = (int)(currentShoppingCart.ShippingTotal * 100),
                    Currency = "aud",
                    Quantity = 1,
                    Images = new List<string> { $"{HomePage.UrlAbsolute()}/Images/NatureQuest-Logo-square.png" }
                };
                lineItemOptions.Add(shippingOption);

                //get the stripe api key to use
                StripeConfiguration.ApiKey = currentShoppingCart.IsStripeLiveMode
                    ? currentShoppingCart.StripeLiveSecretKey
                    : currentShoppingCart.StripeTestSecretKey;

                //create the customer to use
                var stripeCustomer = GetStripeCustomer(currentShoppingCart);

                //get the system order number to use when creating the payment intent
                var systemOrderNumber = SystemOrderId(currentShoppingCart);

                //check if we have an order id
                if (!string.IsNullOrWhiteSpace(systemOrderNumber) && !string.IsNullOrWhiteSpace(currentShoppingCart.SystemOrderId))
                {
                    //create the session
                    var options = new SessionCreateOptions
                    {
                        Customer = stripeCustomer.Id,
                        PaymentMethodTypes = new List<string>
                    {
                        "card"
                    },
                        LineItems = lineItemOptions,
                        SuccessUrl = currentShoppingCart.ShoppingSuccessPage?.UrlAbsolute(),
                        CancelUrl = currentShoppingCart.CheckoutPage?.UrlAbsolute(),
                        Mode = "payment",
                        PaymentIntentData = new SessionPaymentIntentDataOptions
                        {
                            CaptureMethod = "automatic",
                            Shipping = new ChargeShippingOptions
                            {
                                TrackingNumber = $"Order-{stripeCustomer.Id}",
                                Name = cartShippingDetails.ShippingFullname,
                                Address = new AddressOptions
                                {
                                    Line1 = cartShippingDetails.ShippingAddress,
                                    Line2 = cartShippingDetails.ShippingMobileNumber,
                                    City = cartShippingDetails.ShippingSuburb,
                                    PostalCode = cartShippingDetails.ShippingPostCode,
                                    State = cartShippingDetails.ShippingState
                                }
                            }
                        },
                        SubmitType = "pay",
                        ClientReferenceId = currentShoppingCart.SystemOrderId
                    };

                    var sessionService = new SessionService();
                    var stripeSession = sessionService.Create(options);
                    //check if we have a session and save it on the cart
                    if (!string.IsNullOrWhiteSpace(stripeSession?.Id))
                    {
                        currentShoppingCart.StripeCartSession = stripeSession;
                        currentShoppingCart.StripeCartSessionId = stripeSession.Id;
                        stripeSessionId = stripeSession.Id;
                    }
                }
            }

            //return the session id 
            return stripeSessionId;
        }

        /// <summary>
        /// clear the stripe session saved on the cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public bool ClearStripeSessionId(SiteShoppingCart currentShoppingCart)
        {
            var stripeSessionCleared = false;

            //check the cart
            if (!string.IsNullOrWhiteSpace(currentShoppingCart?.StripeCartSession?.Id))
            {
                currentShoppingCart.StripeCartSession = null;
                currentShoppingCart.StripeCartSessionId = String.Empty;
                stripeSessionCleared = true;
            }
            //return the cleared flag
            return stripeSessionCleared;
        }

        /// <summary>
        /// Finalise the stripe payment
        /// </summary>
        /// <param name="stripePaymentIntent"></param>
        /// <returns></returns>
        public bool FinaliseStripePayment(PaymentIntent stripePaymentIntent)
        {
            //set the payment finalising flag(
            var paymentFinalised = false;

            //check the payment intent 
            if (!string.IsNullOrWhiteSpace(stripePaymentIntent?.Id))
            {
                var currentCart = GetCurrentCart();
                //check if this intent matches the cart
                if (currentCart.StripeCartSession?.Id == stripePaymentIntent.Id
                    && stripePaymentIntent.Status == "succeeded")
                {
                    paymentFinalised = true;
                }
            }

            //return the intent flag
            return paymentFinalised;
        }

        /// <summary>
        /// get or create the stripe customer from the cart details
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public Customer GetStripeCustomer(SiteShoppingCart currentShoppingCart)
        {
            //check if we have am email address to use to get or create an account with
            if (string.IsNullOrWhiteSpace(currentShoppingCart?.CartMember?.Email) &&
                string.IsNullOrWhiteSpace(currentShoppingCart?.CartShippingDetails?.ShippingEmail))
            {
                return null;
            }

            //get the shipping details to use
            var cartShippingDetails = currentShoppingCart.CartShippingDetails;

            //create the email to use
            var cartEmail = !string.IsNullOrWhiteSpace(currentShoppingCart.CartMember?.Email)
                ? currentShoppingCart.CartMember?.Email
                : cartShippingDetails?.ShippingEmail;
            //get the shipping details to use
            //var cartMembersModel = currentShoppingCart.CartMembersModel;

            //create the customer service to use
            var customerService = new CustomerService();

            var options = new CustomerListOptions
            {
                Email = cartEmail
            };
            //check if we can get the customer from stripe
            var stripeCustomer = customerService.List(options).FirstOrDefault(customer => customer.Email == cartEmail);
            //if we cant find a matching customer create 1
            if (stripeCustomer == null && cartShippingDetails != null)
            {
                //create the options to use to create the customer with
                var createOptions = new CustomerCreateOptions
                {
                    Email = cartEmail,
                    Name = cartShippingDetails.ShippingFullname,
                    Description = cartShippingDetails.ShippingOptionDetails,
                    Shipping = new ShippingOptions
                    {
                        Name = cartShippingDetails.ShippingFullname,
                        Phone = cartShippingDetails.ShippingMobileNumber,
                        Address = new AddressOptions
                        {
                            Line1 = cartShippingDetails.ShippingAddress,
                            City = cartShippingDetails.ShippingSuburb,
                            PostalCode = cartShippingDetails.ShippingPostCode,
                            State = cartShippingDetails.ShippingState
                        }
                    }
                };

                //use the service to create the customer
                stripeCustomer = customerService.Create(createOptions);
            }

            //return the new or old customer
            return stripeCustomer;
        }

        /// <summary>
        /// place the stripe order and clear the cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <param name="stripeOrderDetails"></param>
        /// <returns></returns>
        public bool PlaceStripeCartOrder(
            SiteShoppingCart currentShoppingCart,
            out string resultMessage,
            out OrderDetails stripeOrderDetails)
        {
            // set the default result
            var orderPlaced = false;
            //create the initial response
            resultMessage = "There was an error placing your order";
            stripeOrderDetails = new OrderDetails();
            stripeOrderDetails.OrderItems.AddRange(currentShoppingCart.CartItems);
            try
            {
                //get the stripe api key to use
                StripeConfiguration.ApiKey = currentShoppingCart.IsStripeLiveMode
                    ? currentShoppingCart.StripeLiveSecretKey
                    : currentShoppingCart.StripeTestSecretKey;

                // get the cart stripe customer to check for order and payment
                var orderCustomer = GetStripeCustomer(currentShoppingCart);
                if (!string.IsNullOrWhiteSpace(orderCustomer?.Id))
                {
                    //get the intent and check if it has succeeded
                    var cartSessionPaymentIntentId = currentShoppingCart.StripeCartSession?.PaymentIntentId;
                    if (!string.IsNullOrWhiteSpace(cartSessionPaymentIntentId))
                    {
                        var paymentIntentService = new PaymentIntentService();
                        var paymentIntent = paymentIntentService.Get(cartSessionPaymentIntentId);
                        if (paymentIntent?.Status == "succeeded")
                        {
                            //if we have a member save the order
                            if (currentShoppingCart.CartMember?.Id > 0)
                            {
                                var memberOrder = CreateMemberOrderPage(
                                    _globalOrdersPage,
                                    currentShoppingCart.CartMember,
                                    currentShoppingCart.SystemOrderId,
                                    //paymentIntent.Id,
                                    currentShoppingCart,
                                    "Stripe");

                                //create the member order page first
                                if (memberOrder?.Id > 0)
                                {

                                    //create the user email
                                    var customerEmailBody = CustomerEmailBody(
                                        currentShoppingCart,
                                        currentShoppingCart.SystemOrderId,
                                        //paymentIntent.Id,
                                        currentShoppingCart.CartMembersModel.FullName);

                                    if (!string.IsNullOrWhiteSpace(customerEmailBody?.ToString()))
                                    {
                                        //create the customer email
                                        var customerEmailAddress = new EmailAddress(currentShoppingCart.CartMember.Email, currentShoppingCart.CartMembersModel.FullName);

                                        var customerSubject = "Your Order on Natures Quest.";

                                        //create the global emails
                                        var customerMessage = MailHelper.CreateSingleEmail(
                                            _fromEmailAddress,
                                            customerEmailAddress,
                                            customerSubject,
                                            "",
                                            customerEmailBody.ToString());
                                        //send the global email
                                        var unused3 = SendGridEmail(customerMessage, true, true);
                                    }

                                    //clear the cart once we have the member order
                                    var cartCleared = ClearShoppingCart(currentShoppingCart, out _);
                                    if (cartCleared)
                                    {
                                        orderPlaced = true;
                                        resultMessage = "Your Order has been placed and saved in your account";
                                    }
                                }
                            }
                            //its an anonymous cart
                            else
                            {
                                //get the default values to use
                                var shippingDetails = currentShoppingCart.CartShippingDetails;

                                //check if we already have a registered member from the cart shipping email used
                                var anonymousMember = _siteMembersService.GetMemberByEmail(shippingDetails.ShippingEmail.Trim());
                                //if we don't have an existing one then create a new one
                                if (anonymousMember == null)
                                {
                                    //update the anonymous member model to use
                                    var anonymousMemberModel = currentShoppingCart.CartMembersModel;
                                    anonymousMemberModel.Email = shippingDetails.ShippingEmail.Trim();
                                    anonymousMemberModel.FullName = shippingDetails.ShippingFullname.Trim();
                                    anonymousMemberModel.MobileNumber = shippingDetails.ShippingMobileNumber.Trim();
                                    anonymousMemberModel.HouseAddress = shippingDetails.ShippingAddress.Trim();
                                    anonymousMemberModel.Suburb = shippingDetails.ShippingSuburb.Trim();
                                    anonymousMemberModel.PostCode = shippingDetails.ShippingPostCode.Trim();
                                    anonymousMemberModel.State = shippingDetails.ShippingState.Trim();

                                    //create a default member account here for the anonymous user
                                    var newMember = _siteMembersService.RegisterSiteMember(anonymousMemberModel,
                                        out MembershipCreateStatus createStatus);
                                    if (newMember != null && createStatus == MembershipCreateStatus.Success)
                                    {
                                        anonymousMember = newMember.LoggedInMember;
                                    }
                                }

                                //we should have a new anonymous member by now
                                if (anonymousMember != null)
                                {
                                    //create the cart page for the user
                                    var newCartPage = CreateMemberCartPage(_globalCartsPage, anonymousMember);

                                    // if the new cart page is not null create the session with that and return it
                                    if (newCartPage != null && newCartPage.Id != 0)
                                    {
                                        //save the cart member and member cart page to the current cart
                                        currentShoppingCart.CartMember = anonymousMember;
                                        currentShoppingCart.MemberCartPage = newCartPage;
                                    }

                                    //update the saved cart
                                    var cartUpdated = false;
                                    //update the saved cart
                                    if (currentShoppingCart.CartItems.Any())
                                    {
                                        foreach (var cartItem in currentShoppingCart.CartItems)
                                        {
                                            //get the price product page
                                            var priceProduct = _umbracoHelper.Content(cartItem.CartItemPageId);
                                            if (priceProduct != null)
                                            {
                                                cartUpdated = UpdateMemberSavedCart(
                                                    currentShoppingCart,
                                                    priceProduct,
                                                    true,
                                                    out _);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        cartUpdated = true;
                                    }

                                    //check if the cart has been updated
                                    if (cartUpdated)
                                    {
                                        var anonymousMemberOrder = CreateMemberOrderPage(
                                            _globalOrdersPage,
                                            anonymousMember,
                                            currentShoppingCart.SystemOrderId,
                                            //paymentIntent.Id,
                                            currentShoppingCart,
                                            "Stripe");

                                        //create the member order page first
                                        if (anonymousMemberOrder?.Id > 0)
                                        {
                                            //create the user email
                                            var customerEmailBody = CustomerEmailBody(
                                                currentShoppingCart,
                                                currentShoppingCart.SystemOrderId,
                                                //paymentIntent.Id,
                                                shippingDetails.ShippingFullname);

                                            if (!string.IsNullOrWhiteSpace(customerEmailBody?.ToString()))
                                            {
                                                //create the customer email
                                                var customerEmailAddress = new EmailAddress(
                                                    shippingDetails.ShippingEmail.Trim(),
                                                    shippingDetails.ShippingFullname.Trim());

                                                var customerSubject = "Your Order on Natures Quest.";

                                                //create the global emails
                                                var customerMessage = MailHelper.CreateSingleEmail(
                                                    _fromEmailAddress,
                                                    customerEmailAddress,
                                                    customerSubject,
                                                    "",
                                                    customerEmailBody.ToString());
                                                //send the global email
                                                var unused3 = SendGridEmail(customerMessage, true, true);
                                            }

                                            //clear the cart once we have the member order
                                            var cartCleared = ClearShoppingCart(currentShoppingCart, out _);
                                            if (cartCleared)
                                            {
                                                orderPlaced = true;
                                                resultMessage = "Your Order has been placed and saved in your account";
                                            }
                                        }
                                    }
                                }
                            }

                            //fill in the order details to return to the view page
                            stripeOrderDetails.OrderId = cartSessionPaymentIntentId;
                            stripeOrderDetails.OrderProcessedMessage = resultMessage;
                            stripeOrderDetails.OrderPaidSuccess = orderPlaced;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService > PlaceStripeCartOrder"), ex, "Error placing stripe order.");
                //send an admin email with the error
                var errorMessage =
                    $"Error placing stripe order.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error placing stripe order {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorMessage);
                var unused = SendGridEmail(systemMessage);
            }

            //return the result
            return orderPlaced;
        }

        /// <summary>
        /// get the member model orders
        /// </summary>
        /// <param name="membersModel"></param>
        /// <param name="adminOrders"></param>
        /// <returns></returns>
        public MembersModel GetMemberOrderDetails(MembersModel membersModel, bool adminOrders = false)
        {
            //check if we have a member email to use
            if (!string.IsNullOrWhiteSpace(membersModel.LoggedInMember?.Email))
            {
                //get the model member
                var modelMember = membersModel.LoggedInMember;
                //get the user's stripe payment intents for the orders
                var memberStripePayments = adminOrders ? GetStripePaidIntents() : GetStripePaidIntents(modelMember.Email);
                
                //get the members site orders
                var siteOrders = adminOrders ? membersModel.AdminOrdersPage : membersModel.MemberOrdersPage;

                //create the order models from the stripe payments list and site orders list
                if (siteOrders.Any() && memberStripePayments.Any())
                {
                    //go through each of the site orders 
                    foreach (var siteOrder in siteOrders)
                    {
                        var orderPaymentIntentId = "";
                        //get the order intent id saved on the order
                        if (siteOrder.HasProperty("paymentId") && siteOrder.HasValue("paymentId"))
                        {
                            // set the page title to override the default
                            orderPaymentIntentId = siteOrder.GetProperty("paymentId").Value().ToString();
                        }

                        var orderId = "";
                        if (siteOrder.HasProperty("orderId") && siteOrder.HasValue("orderId"))
                        {
                            // set the page title to override the default
                            orderId = siteOrder.GetProperty("orderId").Value().ToString();
                        }
                        //get the payment intent matching the site order
                        var siteOrderIntent = memberStripePayments.FirstOrDefault(payment => payment.Id == orderPaymentIntentId) ??
                                                        memberStripePayments.FirstOrDefault(payment => payment.Id == orderId);

                        //check if we have the intent id and payment intent to match
                        if (siteOrder != null)
                        {
                            var modelOrderDetails = new OrderDetails
                            {
                                OrderId = orderId,
                                OrderPaymentIntent = siteOrderIntent,
                                LoggedInMember = modelMember
                            };

                            //get the saved order payment status
                            if (siteOrder.HasProperty("isOrder") && siteOrder.HasValue("isOrder"))
                            {
                                // set the page title to override the default
                                modelOrderDetails.OrderPaidSuccess = siteOrder.Value<bool>("isOrder");
                            }

                            //get the saved order shipment status
                            if (siteOrder.HasProperty("isOrderShipped") && siteOrder.HasValue("isOrderShipped"))
                            {
                                // set the page title to override the default
                                modelOrderDetails.OrderShipped = siteOrder.Value<bool>("isOrderShipped");
                            }

                            //get the saved order complete status
                            if (siteOrder.HasProperty("isOrderComplete") && siteOrder.HasValue("isOrderComplete"))
                            {
                                // set the page title to override the default
                                modelOrderDetails.OrderCompleted = siteOrder.Value<bool>("isOrderComplete");
                            }

                            //get the saved order invoice status
                            if (siteOrder.HasProperty("invoicePrinted") && siteOrder.HasValue("invoicePrinted"))
                            {
                                // set the page title to override the default
                                modelOrderDetails.InvoicePrinted = siteOrder.Value<bool>("invoicePrinted");
                            }

                            //get the payment intent shipping details
                            var stripeShippingDetails = siteOrderIntent?.Shipping;
                            if (!string.IsNullOrWhiteSpace(stripeShippingDetails?.TrackingNumber))
                            {
                                var modelShipping = new ShippingDetails
                                {
                                    ShippingTrackingNumber = stripeShippingDetails.TrackingNumber,
                                    ShippingMobileNumber = stripeShippingDetails.Address.Line2,
                                    ShippingFullname = stripeShippingDetails.Name,
                                    ShippingAddress = stripeShippingDetails.Address.Line1,
                                    ShippingSuburb = stripeShippingDetails.Address.City,
                                    ShippingPostCode = stripeShippingDetails.Address.PostalCode,
                                    ShippingState = stripeShippingDetails.Address.State
                                };
                                //add it to the model
                                modelOrderDetails.OrderShippingDetails = modelShipping;
                            }


                            //get the saved order html 
                            if (siteOrder.HasProperty("orderSummary") && siteOrder.HasValue("orderSummary"))
                            {
                                // save the html on the order details
                                var orderEmailHtml = CustomerEmailBody(null, orderId, stripeShippingDetails?.Name);
                                var orderCartSummary = siteOrder.Value<string>("orderSummary");
                                modelOrderDetails.OrderInvoiceHtml =
                                    orderEmailHtml.ToString().Replace("**Cart-Summary**", orderCartSummary);
                            }

                            //set the site order page id
                            modelOrderDetails.SiteOrderPage = siteOrder;
                            //get the stripe payment date
                            var orderDisplayedDate = siteOrder.CreateDate;
                            var ausTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
                            if (ausTimeZoneInfo != null)
                            {
                                var localAusDate = TimeZoneInfo.ConvertTime(orderDisplayedDate, ausTimeZoneInfo);
                                modelOrderDetails.OrderCreatedDate = localAusDate;
                            }
                            else
                            {
                                modelOrderDetails.OrderCreatedDate = orderDisplayedDate;
                            }

                            //get the order total
                            if (siteOrderIntent?.Amount != null)
                            {
                                modelOrderDetails.OrderTotal = Convert.ToDecimal(siteOrderIntent.Amount) / Convert.ToDecimal(100);
                            }

                            //finally add the order details to the model list
                            if (adminOrders)
                            {
                                membersModel.AdminOrderDetailsList.Add(modelOrderDetails);
                            }
                            else
                            {
                                membersModel.MemberOrderDetailsList.Add(modelOrderDetails);
                            }
                        }
                    }
                }
            }
            //return the model
            return membersModel;
        }

        ///// <summary>
        ///// get the admin model orders
        ///// </summary>
        ///// <param name="membersModel"></param>
        ///// <returns></returns>
        //public MembersModel GetAdminOrderDetails(MembersModel membersModel)
        //{
        //    //check if we have a member email to use and if they are an admin user
        //    if (!string.IsNullOrWhiteSpace(membersModel.LoggedInMember?.Email) && membersModel.IsAdminUser)
        //    {
        //        //get the model member
        //        var modelMember = membersModel.LoggedInMember;
        //        //add the current cart for use later
        //        membersModel.MemberCart = GetCurrentCart(modelMember.Email);
        //        //get the user's stripe payment intents for the orders
        //        var memberStripePayments = GetStripePaidIntents();

        //        //create the order models from the stripe payments list
        //        if (memberStripePayments.Any())
        //        {
        //            //go through each of the site orders 
        //            foreach (var stripePaymentIntent in memberStripePayments)
        //            {
        //                var orderPaymentIntentId = stripePaymentIntent.Id;

        //                var modelOrderDetails = new OrderDetails
        //                {
        //                    OrderId = orderPaymentIntentId,
        //                    OrderPaidSuccess = true,
        //                    OrderPaymentIntent = stripePaymentIntent
        //                };

        //                //get the payment intent shipping details
        //                var stripeShippingDetails = stripePaymentIntent.Shipping;
        //                if (!string.IsNullOrWhiteSpace(stripeShippingDetails?.TrackingNumber))
        //                {
        //                    var modelShipping = new ShippingDetails
        //                    {
        //                        ShippingTrackingNumber = stripeShippingDetails.TrackingNumber,
        //                        ShippingMobileNumber = stripeShippingDetails.Address.Line2,
        //                        ShippingFullname = stripeShippingDetails.Name,
        //                        ShippingAddress = stripeShippingDetails.Address.Line1,
        //                        ShippingSuburb = stripeShippingDetails.Address.City,
        //                        ShippingPostCode = stripeShippingDetails.Address.PostalCode,
        //                        ShippingState = stripeShippingDetails.Address.State
        //                    };
        //                    //add it to the model
        //                    modelOrderDetails.OrderShippingDetails = modelShipping;
        //                }

        //                //get the stripe payment date
        //                if (stripePaymentIntent.Created.HasValue)
        //                {
        //                    var orderDisplayedDate = stripePaymentIntent.Created.Value;
        //                    var ausTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
        //                    if (ausTimeZoneInfo != null)
        //                    {
        //                        TimeZoneInfo.ConvertTimeFromUtc(orderDisplayedDate, ausTimeZoneInfo);
        //                    }
        //                    modelOrderDetails.OrderCreatedDate = orderDisplayedDate;
        //                }

        //                //get the order total
        //                if (stripePaymentIntent.Amount != null)
        //                {
        //                    modelOrderDetails.OrderTotal = Convert.ToDecimal(stripePaymentIntent.Amount) / Convert.ToDecimal(100);
        //                }

        //                //add the invoice html for the order
        //                //modelOrderDetails.OrderInvoiceHtml = 

        //                //finally add the order details to the model list
        //                membersModel.AdminOrderDetailsList.Add(modelOrderDetails);
        //            }
        //        }
        //    }
        //    //return the model
        //    return membersModel;
        //}

        /// <summary>
        /// Get the stripe list of paid payment intents, with optional customer emails
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <param name="orderPaymentIntentId"></param>
        /// <returns></returns>
        public List<PaymentIntent> GetStripePaidIntents(string emailAddress = null, string orderPaymentIntentId = null)
        {
            //get the stripe api key to use
            StripeConfiguration.ApiKey = StripeLiveMode
                ? StripeLiveSecretKey
                : StripeTestSecretKey;

            //create the default list
            var paidStripePayments = new List<PaymentIntent>();

            //create the payment options
            var paymentOptions = new PaymentIntentListOptions
            {
                Limit = 100
            };

            //create the default payment service
            var paymentIntentService = new PaymentIntentService();

            //if we have an email get the stripe customer for that email
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                //create the customer service to use
                var customerService = new CustomerService();

                var customerOptions = new CustomerListOptions
                {
                    Email = emailAddress
                };
                //check if we can get the customer from stripe
                var stripeCustomer = customerService.List(customerOptions).FirstOrDefault(customer => customer.Email == emailAddress);
                //add the customer id to the list option
                if (!string.IsNullOrWhiteSpace(stripeCustomer?.Id))
                {
                    paymentOptions.Customer = stripeCustomer.Id;
                }
            }

            //get the list of payment intents
            var paymentIntents = paymentIntentService.List(paymentOptions).Data.
                                                                                    Where(paymentIntent => paymentIntent.Status == "succeeded").
                                                                                    ToList();

            //if we have a payment intent id to filter with filter the list here
            if (!string.IsNullOrWhiteSpace(orderPaymentIntentId))
            {
                paymentIntents = paymentIntents.
                    Where(paymentIntent => paymentIntent.Id == orderPaymentIntentId)
                    .ToList();
            }

            //if we have any payment intents save these
            if (paymentIntents.Any())
            {
                paidStripePayments = paymentIntents.ToList();
            }

            //return the list
            return paidStripePayments;
        }

        #endregion

        #region PayPal helpers

        /// <summary>
        /// Get the paypal order request object
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public async Task<OrderRequest> GetCartPayPalOrderRequest(SiteShoppingCart currentShoppingCart)
        {
            //check if the current cart has already got 1
            if (currentShoppingCart.PayPalRequestOrder != null)
            {
                return currentShoppingCart.PayPalRequestOrder;
            }

            //create the paypal line item
            var lineItems = new List<Item>();
            if (currentShoppingCart.CartItems.Any())
            {
                //go through the cart items an add them to the order list
                foreach (var cartItem in currentShoppingCart.CartItems)
                {
                    var stripeItemOption = new Item
                    {
                        Name = $"{cartItem.MainProductPage.Name}-{cartItem.ProductLinePage.Name}",
                        Description = $"{cartItem.Description} - Product Code: {cartItem.ProductVariantCode}",
                        Sku = cartItem.ProductVariantCode,
                        UnitAmount = new Money
                        {
                            CurrencyCode = "AUD",
                            Value = cartItem.Price.ToString("0.##")
                        },
                        Quantity = cartItem.Quantity.ToString()
                    };
                    //add it to the list
                    lineItems.Add(stripeItemOption);
                }

                //create the cart purchase units
                var cartPurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        ReferenceId = "NQ",
                        Description = "Natures Quest order",
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = "AUD",
                            Value = currentShoppingCart.ComputeTotalWithShippingValue().ToString("0.##"),
                            AmountBreakdown = new AmountBreakdown
                            {
                                ItemTotal = new Money
                                {
                                    CurrencyCode = "AUD",
                                    Value = currentShoppingCart.ComputeTotalValue().ToString("0.##")
                                },
                                Shipping = new Money
                                {
                                    CurrencyCode = "AUD",
                                    Value = currentShoppingCart.ShippingTotal.ToString("0.##")
                                }
                            }
                        },
                        Items = lineItems
                    }
                };

                //create the order request
                var orderRequest = new OrderRequest
                {
                    CheckoutPaymentIntent = "CAPTURE",

                    ApplicationContext = new ApplicationContext
                    {
                        BrandName = "Natures Quest",
                        CancelUrl = currentShoppingCart.CheckoutPage?.UrlAbsolute(),
                        ReturnUrl = currentShoppingCart.ShoppingSuccessPage?.UrlAbsolute()
                    },
                    PurchaseUnits = cartPurchaseUnits
                };

                currentShoppingCart.PayPalRequestOrder = orderRequest;
                currentShoppingCart.PayPalPurchaseUnits = cartPurchaseUnits;

                _ = await CreatePayPayOrder(orderRequest, currentShoppingCart);

                //return orderRequest;
                return orderRequest;

                //if (!string.IsNullOrWhiteSpace(payPalOrder?.Id))
                //{
                //    currentShoppingCart.PayPalOrder = payPalOrder;
                //    currentShoppingCart.PayPalOrderId = payPalOrder.Id;
                //    currentShoppingCart.PayPalRequestOrder = orderRequest;
                //    currentShoppingCart.PayPalPurchaseUnits = cartPurchaseUnits;
                //    return orderRequest;
                //}
            }
            //else if we don't have any cart items return null
            return null;
        }

        /// <summary>
        /// Place a paypal order
        /// </summary>
        /// <param name="payPalOrderId"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <param name="payPalOrderDetails"></param>
        /// <returns></returns>
        public bool PlacePayPalOrder(
            string payPalOrderId,
            SiteShoppingCart currentShoppingCart,
            out string resultMessage,
            out OrderDetails payPalOrderDetails)
        {
            //create the initial response
            resultMessage = "There was an placing your order";
            payPalOrderDetails = new OrderDetails();
            payPalOrderDetails.OrderItems.AddRange(currentShoppingCart.CartItems);

            //check if we have an order id
            if (string.IsNullOrWhiteSpace(payPalOrderId))
            {
                return false;
            }

            // set the default result
            bool orderPlaced;
            //try and create the order
            try
            {

                _ = GetPayPalOrder(payPalOrderId, currentShoppingCart);
                orderPlaced = true;
                resultMessage =
                    "Finalising your PayPal order, once fully completed you will get a confirmation email with your order details";
            }
            catch (Exception)
            {
                orderPlaced = false;
            }
            //return the processing flag
            return orderPlaced;
        }

        /// <summary>
        /// Create the pay pal order
        /// </summary>
        /// <param name="orderRequest"></param>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public async Task<Order> CreatePayPayOrder(OrderRequest orderRequest, SiteShoppingCart currentShoppingCart)
        {
            //try and create the order
            try
            {
                var request = new OrdersCreateRequest();
                request.Headers.Add("prefer", "return=representation");
                request.RequestBody(orderRequest);
                var response = await PayPalHttpClient(currentShoppingCart).Execute(request);

                //check the response result
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var cartOrder = response.Result<Order>();
                    return cartOrder;
                }
            }
            catch (Exception ex)
            {
                var errorMessage =
                    $"Error creating PayPal order for :{currentShoppingCart.CartShippingDetails?.ShippingFullname}, details {ex.Message}.";
                //there was an error updating the shipping option 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error creating PayPal order on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                return null;
            }

            //there was an error getting order response
            return null;
        }

        /// <summary>
        /// Get a PayPal order by id
        /// </summary>
        /// <param name="payPalOrderId"></param>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public async Task<Order> GetPayPalOrder(string payPalOrderId, SiteShoppingCart currentShoppingCart)
        {
            //try and create the order
            try
            {
                //create the order request
                var request = new OrdersGetRequest(payPalOrderId);
                var response = await PayPalHttpClient(currentShoppingCart).Execute(request);

                //check the response result
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var cartOrder = response.Result<Order>();
                    if (!string.IsNullOrWhiteSpace(cartOrder?.Id))
                    {
                        //if we have a member save the order
                        if (currentShoppingCart.CartMember?.Id > 0)
                        {
                            var memberOrder = CreateMemberOrderPage(
                                _globalOrdersPage,
                                currentShoppingCart.CartMember,
                                cartOrder.Id,
                                currentShoppingCart,
                                "PayPal");
                            //create the member order page first
                            if (memberOrder?.Id > 0)
                            {
                                //clear the cart once we have the member order
                                var cartCleared = ClearShoppingCart(currentShoppingCart, out _);
                                if (cartCleared)
                                {
                                    return cartOrder;
                                }
                            }
                        }
                        //its an anonymous cart
                        else
                        {
                            //clear the cart
                            var cartCleared = ClearShoppingCart(currentShoppingCart, out _);
                            if (cartCleared)
                            {
                                return cartOrder;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage =
                    $"Error creating pay pal order for :{currentShoppingCart.CartShippingDetails?.ShippingFullname}, details {ex.Message}.";
                //there was an error updating the shipping option 
                _logger.Error(Type.GetType("ShoppingService"), ex, errorMessage);
                //send an admin email with the error
                var errorEmailMessage =
                    $"{errorMessage}.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Error finalising PayPal order on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorEmailMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");

                //set the error message to return
                return null;
            }

            //there was an error getting order response
            return null;
        }

        /// <summary>
        /// get the Pay Pal http client o use
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public HttpClient PayPalHttpClient(SiteShoppingCart currentShoppingCart)
        {
            return new PayPalHttpClient(PayPalEnvironment(currentShoppingCart));
        }

        /// <summary>
        /// create the Pay Pal environment
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        public PayPalEnvironment PayPalEnvironment(SiteShoppingCart currentShoppingCart)
        {
            //if the cart is in live mode create the live environment
            if (currentShoppingCart.IsPayPalLiveMode)
            {
                return new LiveEnvironment(currentShoppingCart.PayPalLiveClientId, currentShoppingCart.PayPalLiveSecret);
            }
            //otherwise just use the sandbox environment
            return new SandboxEnvironment(currentShoppingCart.PayPalTestClientId, currentShoppingCart.PayPalTestSecret);
        }

        /// <summary>
        /// convert a response object to a json string
        /// </summary>
        /// <param name="serializableObject"></param>
        /// <returns></returns>
        public string ObjectToJsonString(Object serializableObject)
        {
            //create the memory stream to use
            var memoryStream = new MemoryStream();
            //create the json writer to use
            var writer = JsonReaderWriterFactory.CreateJsonWriter(
                memoryStream, Encoding.UTF8, true, true, "  ");
            //create the serializer and pass in the object
            var ser = new DataContractJsonSerializer(
                serializableObject.GetType(),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            ser.WriteObject(writer, serializableObject);
            memoryStream.Position = 0;
            var streamReader = new StreamReader(memoryStream);
            return streamReader.ReadToEnd();
        }

        #endregion

        #region Cart Helpers

        /// <summary>
        /// get a member by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public IMember GetMemberByEmail(string email)
        {
            return _memberService.GetByEmail(email);
        }

        /// <summary>
        /// send the send grid message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="autoAddBcc"></param>
        /// <param name="autoAddAdminBcc"></param>
        /// <returns></returns>
        public async Task<bool> SendGridEmail(SendGridMessage message, bool autoAddBcc = false, bool autoAddAdminBcc = false)
        {
            //set the flag for a message sent
            var messageSent = false;

            try
            {
                //check the key
                if (!string.IsNullOrWhiteSpace(_sendGridKey))
                {
                    //check if we need to add the auto bcc
                    if (autoAddBcc)
                    {
                        message.AddBcc(_systemEmailAddress);
                    }

                    //check if we need to add the admin auto bcc as well
                    if (autoAddAdminBcc && _siteToEmailAddresses.Any())
                    {
                        foreach (var emailAddress in _siteToEmailAddresses)
                        {
                            message.AddBcc(emailAddress);
                        }
                    }

                    //get the client to use
                    var client = new SendGridClient(_sendGridKey);
                    //send the email and get the response
                    var response = await client.SendEmailAsync(message);

                    //check the response
                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        messageSent = true;
                    }
                }
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("ShoppingService"), ex, "Error sending email with sendgrid");
                //send an admin email with the error
                var errorMessage =
                    $"Error sending email with sendgrid.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Sendgrid email error on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(
                    _fromEmailAddress,
                    _systemEmailAddress,
                    errorSubject,
                    "",
                    errorMessage);
                var systemMessageSent = SendGridEmail(systemMessage);
                _logger.Info(Type.GetType("ShoppingService"), $"Error email send result:{systemMessageSent}");
            }

            //return the flag
            return messageSent;
        }

        /// <summary>
        /// Create the html string for the body
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="orderNumber"></param>
        /// <param name="customerName"></param>
        /// <returns></returns>
        public HtmlString CustomerEmailBody(
            SiteShoppingCart currentShoppingCart,
            string orderNumber,
            string customerName)
        {
            //create the string builder
            var emailBodyString = new StringBuilder();

            //create the email wrapper
            emailBodyString.Append("<center><table width='600px' border='0' cellpadding='0' cellspacing='0' style='max-width: 600px;margin:0 auto;'><tr><td>");
            //create the header details table
            emailBodyString.Append("<table width='100%' border='0' cellpadding='5' cellspacing='0'>");

            //add the table header
            emailBodyString.Append("<tr>");
            emailBodyString.Append("<td style='width: 50%;background:#2ecc71;'>" +
                                                   "<p><a href='https://www.naturesquest.com.au' tittle='Natures Quest'>" +
                                                   "<img src='https://www.naturesquest.com.au/Images/NatureQuest-Site-Logo.png' alt='Natures Quest' width='185' />" +
                                                   "</a><p/></td>");
            emailBodyString.Append("<td style='width: 50%;background:#2ecc71;'></td>");
            emailBodyString.Append("</tr>");

            //add the details row 
            emailBodyString.Append("<tr>");
            emailBodyString.Append("<td style='width: 50%;'>" +
                                                   $"<h3>Your Natures Quest order: {orderNumber} ,has been confirmed.</h3>" +
                                                   "</td>");
            emailBodyString.Append("<td style='width: 50%;'>" +
                                                   "<h4>Got a question?</h4>" +
                                                   "<p>Call us on: 08 8382 6005<br />" +
                                                   "<p>Email: info@naturesquest.com.au</p>" +
                                                   "</td>");
            emailBodyString.Append("</tr>");

            //close the shipping and details table
            emailBodyString.Append("</table>");

            //add the body introduction
            emailBodyString.Append($"<p>Hi {customerName}, <p/>" +
                                                   "<p>Thank you for shopping online with Natures Quest. " +
                                                   "We've received your order and have confirmed your payment. " +
                                                   "Your order will now process through our warehouse.</p>");

            emailBodyString.Append("<p>Keep an eye out for another email, advising when your order has been shipped. " +
                                                "This will include your tracking number.<p/>");

            //add the cart products summary
            if (currentShoppingCart != null)
            {
                emailBodyString = CartSummaryHtml(currentShoppingCart, emailBodyString);
            }
            //add the text to replace the cart summary with
            else
            {
                emailBodyString.Append("**Cart-Summary**");
            }
            //close the email wrapper
            emailBodyString.Append("</tr></td></table></center>");

            //add the string to the email body
            var emailBody = new HtmlString(emailBodyString.ToString());

            //return the body string}
            return emailBody;
        }

        /// <summary>
        /// get the shopping cart html summary for emails
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="emailBodyString"></param>
        /// <returns></returns>
        public StringBuilder CartSummaryHtml(SiteShoppingCart currentShoppingCart, StringBuilder emailBodyString)
        {
            if (currentShoppingCart != null)
            {
                //add the body heading
                emailBodyString.Append("<h3>Here is a Summary of your order</h3>");

                //create the products table
                emailBodyString.Append("<table width='100%' border='0' cellpadding='5' cellspacing='0'>");

                //add the table header
                emailBodyString.Append("<tr style='background:#f5f5f5;'>");
                emailBodyString.Append("<th><h4>Image</h4></th>");
                emailBodyString.Append("<th><h4>Description</h4></th>");
                emailBodyString.Append("<th style='text-align:center;'><h4>Price</h4></th>");
                emailBodyString.Append("<th style='text-align:center;'><h4>Quantity</h4></th>");
                emailBodyString.Append("<th style='text-align:center;'><h4>Total</h4></th>");
                emailBodyString.Append("</tr>");

                //add the products 
                foreach (var cartItem in currentShoppingCart.CartItems)
                {
                    var cartItemPage = cartItem.ProductLinePage;
                    var cartProduct = cartItem.MainProductPage;
                    var cartItemName = $"{cartProduct.Name} - {cartItemPage.Name}";
                    var itemTotal = cartItem.Quantity * cartItem.Price;

                    //add the product rows
                    emailBodyString.Append("<tr>");
                    emailBodyString.Append($"<td><p><img src='https://www.naturesquest.com.au/{cartItem.CartItemImage}' alt='{cartItemName}' width='100' /></p></td>");
                    //emailBodyString.Append($"<td><p><img src='{HomePage.UrlAbsolute()}{cartItem.CartItemImage}' alt='{cartItemName}' width='100' /></p></td>");
                    emailBodyString.Append($"<td><p>{cartItemName}</p></td>");
                    emailBodyString.Append($"<td style='text-align:center;'><p>{cartItem.Price:c}</p></td>");
                    emailBodyString.Append($"<td style='text-align:center;'><p>{cartItem.Quantity}</p></td>");
                    emailBodyString.Append($"<td style='text-align:center;'><p>{itemTotal:c}</p></td>");
                    emailBodyString.Append("</tr>");
                }

                //add the sub total row 
                emailBodyString.Append("<tr style='background:#f5f5f5;'>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td style='text-align:center;'><h4>Subtotal</h4></td>");
                emailBodyString.Append($"<td style='text-align:center;'><p>{currentShoppingCart.ComputeTotalValue():c}</p></td>");
                emailBodyString.Append("</tr>");

                //add the shipping row 
                emailBodyString.Append("<tr style='background:#f5f5f5;'>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td style='text-align:center;'><h4>Freight</h4></td>");
                emailBodyString.Append($"<td style='text-align:center;'><p>{currentShoppingCart.ShippingTotal:c}</p></td>");
                emailBodyString.Append("</tr>");

                //add the cart total row 
                emailBodyString.Append("<tr style='background:#f5f5f5;'>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td></td>");
                emailBodyString.Append("<td style='text-align:center;'><h4>Total inc GST</h4></td>");
                emailBodyString.Append($"<td style='text-align:center;'><p>{currentShoppingCart.ComputeTotalWithShippingValue():c}</p></td>");
                emailBodyString.Append("</tr>");

                //close the table
                emailBodyString.Append("</table>");
                emailBodyString.Append("<div>&nbsp;<br /></div>");

                var shippingDetails = currentShoppingCart.CartShippingDetails;
                //create the shipping and details table
                emailBodyString.Append("<table width='100%' border='0' cellpadding='5' cellspacing='0'>");

                //add the table header
                emailBodyString.Append("<tr style='background:#f5f5f5;'>");
                emailBodyString.Append("<th style='width: 50%;'><h4>Customer Information</h4></th>");
                emailBodyString.Append("<th style='width: 50%;'><h4>Shipping Information </h4></th>");
                emailBodyString.Append("</tr>");

                //add the details row 
                emailBodyString.Append("<tr>");
                emailBodyString.Append("<td style='width: 50%;'>" +
                                                       $"<p>{shippingDetails.ShippingFullname} <br />" +
                                                       $"{shippingDetails.ShippingEmail} <br />" +
                                                       $"{shippingDetails.ShippingMobileNumber} <br />" +
                                                       $"{shippingDetails.ShippingAddress} <br />" +
                                                       $"{shippingDetails.ShippingSuburb}, " +
                                                       $"{shippingDetails.ShippingPostCode}, " +
                                                       $"{shippingDetails.ShippingState}<p/>" +
                                                       "</td>");
                emailBodyString.Append("<td style='width: 50%;'>" +
                                                       $"<p>{currentShoppingCart.CartShippingDetails.ShippingOptionDetails}</p>" +
                                                       "</td>");
                emailBodyString.Append("</tr>");

                //close the shipping and details table
                emailBodyString.Append("</table>");
            }

            return emailBodyString;
        }

        #endregion
    }
}