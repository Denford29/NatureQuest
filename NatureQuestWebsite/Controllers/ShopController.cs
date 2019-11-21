using System.Linq;
using System.Web.Mvc;
using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace NatureQuestWebsite.Controllers
{
    //public class ShopController : UmbracoApiController
    public class ShopController : SurfaceController
    {
        /// <summary>
        /// set the current login status to use
        /// </summary>
        private readonly LoginStatusModel _currentLoginStatus;

        /// <summary>
        /// get the current shopping cart
        /// </summary>
        public SiteShoppingCart CurrentShoppingCart;

        /// <summary>
        /// create the local read only shipping service
        /// </summary>
        private readonly IShoppingService _shoppingService;

        /// <summary>
        /// set the shopping cart details page
        /// </summary>
        private readonly IPublishedContent _shoppingCartPage;

        /// <summary>
        /// set the shopping checkout page
        /// </summary>
        private readonly IPublishedContent _checkoutPage;

        /// <summary>
        /// set the products page
        /// </summary>
        private readonly IPublishedContent _productsPage;

        /// <summary>
        /// set the umbraco helper
        /// </summary>
        private readonly UmbracoHelper _umbracoHelper;

        /// <summary>
        /// get the custom site members service
        /// </summary>
        private readonly ISiteMembersService _siteMemberService;

        /// <summary>
        /// initialise the shop controller
        /// </summary>
        /// <param name="shoppingService"></param>
        /// <param name="umbracoHelper"></param>
        /// <param name="siteMembersService"></param>
        public ShopController(
            IShoppingService shoppingService,
            UmbracoHelper umbracoHelper,
            ISiteMembersService siteMembersService
        )
        {
            //set the shopping cart service to use
            _shoppingService = shoppingService;

            //set the member service to use
            _siteMemberService = siteMembersService;

            //set the umbraco helper to use
            _umbracoHelper = umbracoHelper;

            // get the login status
            _currentLoginStatus = Members.GetCurrentLoginStatus();

            //if there is a user currently logged in use their email to get the cart
            if (_currentLoginStatus.IsLoggedIn && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                CurrentShoppingCart = _shoppingService.GetCurrentCart(_currentLoginStatus.Email);
            }
            else
            {
                CurrentShoppingCart = _shoppingService.GetCurrentCart();
            }

            //get the global site settings page to use
            var homePage = Umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
            if (homePage?.Id > 0)
            {
                //get the shopping cart details page
                if (homePage.FirstChildOfType("shoppingCartPage")?.Id > 0)
                {
                    _shoppingCartPage = homePage.FirstChildOfType("shoppingCartPage");
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

        /// <summary>
        /// Add the product model to the cart
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddProductToCart(ProductModel model)
        {
            var cartResult = new AjaxCartResult();
            //check if we have the quantity set
            if (model == null)
            {
                cartResult.ResultSuccess = false;
                cartResult.ResultMessage = "There was a problem adding your product to the cart.";
                TempData["cartResult"] = cartResult;
                return CurrentUmbracoPage();
            }

            //check if we have the quantity set
            if (model.SelectedQuantity <= 0)
            {
                cartResult.ResultSuccess = false;
                cartResult.ResultMessage = "Please select the quantity you need for the product";
                TempData["cartResult"] = cartResult;
                return CurrentUmbracoPage();
            }

            //check if we have the selected price and product id
            if (string.IsNullOrWhiteSpace(model.SelectedPricePageId))
            {
                cartResult.ResultSuccess = false;
                cartResult.ResultMessage = "Please select the product size required.";
                TempData["cartResult"] = cartResult;
                return CurrentUmbracoPage();
            }

            //check if we the current shopping cart to use
            if (CurrentShoppingCart == null)
            {
                cartResult.ResultSuccess = false;
                cartResult.ResultMessage = "There was an error getting your shopping cart";
                TempData["cartResult"] = cartResult;
                return CurrentUmbracoPage();
            }

            //use the service to add the product
            var productAdded = _shoppingService.AddProductToCart(
                model,
                CurrentShoppingCart,
                out var resultMessage);

            //check the result of the basket
            if (productAdded)
            {
                cartResult.ResultSuccess = true;
                cartResult.ResultMessage = resultMessage;
                TempData["cartResult"] = cartResult;
                return CurrentUmbracoPage();
            }

            //we had an error adding the product
            cartResult.ResultSuccess = false;
            cartResult.ResultMessage = resultMessage;
            TempData["cartResult"] = cartResult;
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// get the menu mini cart
        /// </summary>
        /// <returns></returns>
        public ActionResult GetMiniCart()
        {
            SiteShoppingCart currentCart;

            //if there is a user currently logged in use their email to get the cart
            if (_currentLoginStatus.IsLoggedIn && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                currentCart = _shoppingService.GetCurrentCart(_currentLoginStatus.Email);
            }
            //just get the browser cart to use
            else
            {
                currentCart = _shoppingService.GetCurrentCart();
            }
            //set the cart page
            currentCart.ShoppingCartPage = _shoppingCartPage;
            //return the view with the model
            return View("/Views/Partials/Accounts/MiniCartDetails.cshtml", currentCart);
        }

        /// <summary>
        /// get the main shopping cart
        /// </summary>
        /// <returns></returns>
        public ActionResult GetShoppingCart()
        {
            SiteShoppingCart currentCart;

            //if there is a user currently logged in use their email to get the cart
            if (_currentLoginStatus.IsLoggedIn && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                currentCart = _shoppingService.GetCurrentCart(_currentLoginStatus.Email);
            }
            //just get the browser cart to use
            else
            {
                currentCart = _shoppingService.GetCurrentCart();
            }
            //set the checkout page
            currentCart.CheckoutPage = _checkoutPage;
            //set the products page
            currentCart.ProductsPage = _productsPage;
            //return the view with the model
            return View("/Views/Partials/Shop/CartDetails.cshtml", currentCart);
        }

        /// <summary>
        /// clear all  items from the shopping cart
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ClearShoppingCart()
        {
            var clearCartResult = new AjaxCartResult();
 
            //check if we the current shopping cart to use
            if (CurrentShoppingCart == null)
            {
                clearCartResult.ResultSuccess = false;
                clearCartResult.ResultMessage = "There was an error getting your shopping cart";
                TempData["clearCartResult"] = clearCartResult;
                return CurrentUmbracoPage();
            }

            //use the service to clear the cart
            var cartCleared = _shoppingService.ClearShoppingCart(
                CurrentShoppingCart,
                out var resultMessage);

            //check the result of the basket
            if (cartCleared)
            {
                clearCartResult.ResultSuccess = true;
                clearCartResult.ResultMessage = resultMessage;
                TempData["clearCartResult"] = clearCartResult;
                return CurrentUmbracoPage();
            }

            //we had an error adding the product
            clearCartResult.ResultSuccess = false;
            clearCartResult.ResultMessage = resultMessage;
            TempData["clearCartResult"] = clearCartResult;
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// remove a cart item from the shopping cart
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RemoveCartItemProduct(int cartItemPageId)
        {
            var removeCartItemResult = new AjaxCartResult();

            //check if we the current shopping cart to use
            if (CurrentShoppingCart == null)
            {
                removeCartItemResult.ResultSuccess = false;
                removeCartItemResult.ResultMessage = "There was an error getting your shopping cart";
                TempData["removeCartItemResult"] = removeCartItemResult;
                return CurrentUmbracoPage();
            }

            //check if we have the cart item page
            if (cartItemPageId == 0)
            {
                removeCartItemResult.ResultSuccess = false;
                removeCartItemResult.ResultMessage = "There was a problem getting the cart item to remove.";
                TempData["removeCartItemResult"] = removeCartItemResult;
                return CurrentUmbracoPage();
            }

            //get the cart item product to remove
            var cartItemPage = _umbracoHelper.Content(cartItemPageId);

            //use the service to update the cart
            var cartCleared = _shoppingService.RemoveShoppingCartItem(
                CurrentShoppingCart,
                cartItemPage,
                out var resultMessage
                );

            //check the result of the basket
            if (cartCleared)
            {
                removeCartItemResult.ResultSuccess = true;
                removeCartItemResult.ResultMessage = resultMessage;
                TempData["removeCartItemResult"] = removeCartItemResult;
                return CurrentUmbracoPage();
            }

            //we had an error adding the product
            removeCartItemResult.ResultSuccess = false;
            removeCartItemResult.ResultMessage = resultMessage;
            TempData["removeCartItemResult"] = removeCartItemResult;
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// update a cart item from the shopping cart
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UpdateCartItemProduct(int cartItemPageId, int cartItemQuantity)
        {
            var updateCartItemResult = new AjaxCartResult();

            //check if we the current shopping cart to use
            if (CurrentShoppingCart == null)
            {
                updateCartItemResult.ResultSuccess = false;
                updateCartItemResult.ResultMessage = "There was an error getting your shopping cart";
                TempData["removeCartItemResult"] = updateCartItemResult;
                return CurrentUmbracoPage();
            }

            //check if we have the cart item page
            if (cartItemPageId == 0)
            {
                updateCartItemResult.ResultSuccess = false;
                updateCartItemResult.ResultMessage = "There was a problem getting the cart item to remove.";
                TempData["removeCartItemResult"] = updateCartItemResult;
                return CurrentUmbracoPage();
            }

            //check if we have the cart item page
            if (cartItemQuantity == 0)
            {
                updateCartItemResult.ResultSuccess = false;
                updateCartItemResult.ResultMessage = "The quantity must be more than 1.";
                TempData["removeCartItemResult"] = updateCartItemResult;
                return CurrentUmbracoPage();
            }

            //get the cart item product to remove
            var cartItemPage = _umbracoHelper.Content(cartItemPageId);

            //use the service to update the cart
            var cartCleared = _shoppingService.UpdateShoppingCartItem(
                CurrentShoppingCart,
                cartItemPage,
                cartItemQuantity,
                out var resultMessage
                );

            //check the result of the basket
            if (cartCleared)
            {
                updateCartItemResult.ResultSuccess = true;
                updateCartItemResult.ResultMessage = resultMessage;
                TempData["removeCartItemResult"] = updateCartItemResult;
                return CurrentUmbracoPage();
            }

            //we had an error adding the product
            updateCartItemResult.ResultSuccess = false;
            updateCartItemResult.ResultMessage = resultMessage;
            TempData["removeCartItemResult"] = updateCartItemResult;
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// get the main checkout view
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCheckoutView()
        {
            SiteShoppingCart currentCart;

            //create the cart member model
            var cartMemberModel = new MembersModel
            {
                MemberCurrentLoginStatus = _currentLoginStatus
            };

            //if there is a user currently logged in use their email to get the cart
            if (_currentLoginStatus.IsLoggedIn && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                currentCart = _shoppingService.GetCurrentCart(_currentLoginStatus.Email);

                // create the default model using the logged in email address
                cartMemberModel = _siteMemberService.GetMemberByEmail(cartMemberModel, _currentLoginStatus.Email);

                //add the cart member model to the current cart
                currentCart.CartMembersModel = cartMemberModel;
            }
            //just get the browser cart to use
            else
            {
                currentCart = _shoppingService.GetCurrentCart();

                // create the default model using the logged in email address
                cartMemberModel = _siteMemberService.GetMemberByEmail(cartMemberModel);

                //add the cart member model to the current cart
                currentCart.CartMembersModel = cartMemberModel;
            }
            //set the checkout page
            currentCart.CheckoutPage = _checkoutPage;
            //set the products page
            currentCart.ProductsPage = _productsPage;
            //return the view with the model
            return View("/Views/Partials/Shop/CheckoutView.cshtml", currentCart);
        }

        /// <summary>
        /// Update the shipping for the cart
        /// </summary>
        /// <param name="selectedShippingOption"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddShippingToCart(string selectedShippingOption)
        {
            var shippingResult = new AjaxCartResult();
            //check if we have the page id set
            if (string.IsNullOrWhiteSpace(selectedShippingOption))
            {
                shippingResult.ResultSuccess = false;
                shippingResult.ResultMessage = "Please select the shipping option you need for the product";
                TempData["shippingResult"] = shippingResult;
                return CurrentUmbracoPage();
            }

            // get the shipping option from the current cart options
            var selectedCartShipping = CurrentShoppingCart.DisplayShippingOptions.FirstOrDefault(options =>
                    options.ShippingPageId.ToString() == selectedShippingOption);
            if (selectedCartShipping?.ShippingPricePage == null)
            {
                shippingResult.ResultSuccess = false;
                shippingResult.ResultMessage = "Please select the shipping option you need for the product";
                TempData["shippingResult"] = shippingResult;
                return CurrentUmbracoPage();
            }

            //use the service to update the shipping option
            var shippingAdded = _shoppingService.AddShippingToCart(
                selectedCartShipping,
                CurrentShoppingCart,
                out var resultMessage);

            //check the result of updating the shipping selected
            if (shippingAdded)
            {
                shippingResult.ResultSuccess = true;
                shippingResult.ResultMessage = resultMessage;
                TempData["shippingResult"] = shippingResult;
                return CurrentUmbracoPage();
            }

            //we had an error updating the shipping selected
            shippingResult.ResultSuccess = false;
            shippingResult.ResultMessage = resultMessage;
            TempData["shippingResult"] = shippingResult;
            return CurrentUmbracoPage();
        }
    }
}