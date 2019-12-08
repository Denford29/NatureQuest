using System;
using System.Linq;
using System.Web.Mvc;
using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using Stripe;
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
            //return the view with the model
            return View("/Views/Partials/Accounts/MiniCartDetails.cshtml", CurrentShoppingCart);
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

        /// <summary>
        /// Update the shipping details
        /// </summary>
        /// <param name="shippingFullname"></param>
        /// <param name="shippingEmail"></param>
        /// <param name="shippingAddress"></param>
        /// <param name="shippingMobileNumber"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UpdateShippingDetails(
            string shippingFullname,
            string shippingEmail,
            string shippingAddress,
            string shippingMobileNumber)
        {
            var shippingDetailsResult = new AjaxCartResult();
            //check if we have the page id set
            if (string.IsNullOrWhiteSpace(shippingFullname) ||
                string.IsNullOrWhiteSpace(shippingEmail) ||
                string.IsNullOrWhiteSpace(shippingAddress) ||
                string.IsNullOrWhiteSpace(shippingMobileNumber))
            {
                shippingDetailsResult.ResultSuccess = false;
                shippingDetailsResult.ResultMessage = "Please add all the shipping details";
                TempData["shippingDetailsResult"] = shippingDetailsResult;
                return CurrentUmbracoPage();
            }

            //get the selected shipping option
            var shippingOption = CurrentShoppingCart.SelectShippingOptions.FirstOrDefault(option => option.Selected);
            var shippingCartDetails = "";
            if (shippingOption != null)
            {
                //get the details that match to save
                var shippingDetails = CurrentShoppingCart.DisplayShippingOptions.FirstOrDefault(details =>
                    details.ShippingPageId.ToString() == shippingOption.Value);
                if (shippingDetails != null)
                {
                    shippingCartDetails = $"Shipping fee: {shippingDetails.ShippingFee:c}, details :{shippingDetails.ShippingDetails}";
                }
            }

            //create the shipping details
            var newShippingDetails = new ShippingDetails
            {
                ShippingFullname = shippingFullname,
                ShippingEmail = shippingEmail,
                ShippingAddress = shippingAddress,
                ShippingMobileNumber = shippingMobileNumber,
                ShippingOptionDetails = shippingCartDetails
            };
            CurrentShoppingCart.CartShippingDetails = newShippingDetails;

            //check if we have a current stripe session , if not then create 1
            if (string.IsNullOrWhiteSpace(CurrentShoppingCart.StripeCartSession?.Id))
            {
                CurrentShoppingCart.StripeCartSessionId = _shoppingService.GetCartStripeSessionId(CurrentShoppingCart);
            }

            //we had an error updating the shipping selected
            shippingDetailsResult.ResultSuccess = true;
            shippingDetailsResult.ResultMessage = "Shipping details updated";
            TempData["shippingDetailsResult"] = shippingDetailsResult;
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// Get the stripe checkout script
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStripeCheckoutScript()
        {
            //return the view with the model
            return View("/Views/Partials/Shop/StripeCheckoutScript.cshtml", CurrentShoppingCart);
        }

        /// <summary>
        /// process the checkout process after stripe card has been validated
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ProcessStripeCheckoutPayment()
        {
            //check if the submitted model is valid
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //get the token in the submitted form
            var formData = Request.Form;
            var stripeToken = formData["stripeToken"];

            StripeConfiguration.ApiKey = CurrentShoppingCart.IsStripeLiveMode
                                                            ? CurrentShoppingCart.StripeLiveSecretKey
                                                            : CurrentShoppingCart.StripeTestSecretKey;

            var basketDescription = "";
            //get the descriptions
            foreach (var basketItem in CurrentShoppingCart.CartItems)
            {
                basketDescription += basketItem.Description + Environment.NewLine;
            }

            var options = new ChargeCreateOptions
            {
                Amount = (int)(CurrentShoppingCart.ComputeTotalWithShippingValue() * 100),
                Currency = "aud",
                Description = basketDescription,
                Source = stripeToken,
            };
            var service = new ChargeService();
            Charge charge = service.Create(options);

            return CurrentUmbracoPage();
        }

        /// <summary>
        /// Get the stripe payment result
        /// </summary>
        /// <returns></returns>
        public ActionResult CheckStripePaymentResult()
        {
            //create the payment result
            var paymentResult = new AjaxCartResult();

            //use the service to check the order
            var orderPlaced = _shoppingService.PlaceStripeCartOrder(
                CurrentShoppingCart, 
                out string paymentResultMessage,
                out OrderDetails orderDetails);
            if (orderPlaced)
            {
                paymentResult.ResultSuccess = true;
                paymentResult.ResultMessage = paymentResultMessage;
                TempData["paymentResult"] = paymentResult;
            }
            else
            {
                paymentResult.ResultSuccess = false;
                paymentResult.ResultMessage = paymentResultMessage;
                TempData["paymentResult"] = paymentResult;
            }
            return View("/Views/Partials/Shop/CheckoutProcessedDetails.cshtml", orderDetails);
        }
    }
}