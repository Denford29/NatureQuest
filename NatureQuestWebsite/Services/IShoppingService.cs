using System.Collections.Generic;
using System.Threading.Tasks;
using NatureQuestWebsite.Models;
using PayPalCheckoutSdk.Orders;
using Stripe;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Order = PayPalCheckoutSdk.Orders.Order;
using ShippingOption = NatureQuestWebsite.Models.ShippingOption;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// interface to the shopping cart service class
    /// </summary>
    public interface IShoppingService
    {
        /// <summary>
        /// get the current shopping cart with an optional member email if the member is logged in
        /// </summary>
        /// <param name="memberEmailAddress"></param>
        /// <returns></returns>
        SiteShoppingCart GetCurrentCart(string memberEmailAddress = "");

        /// <summary>
        /// get a member by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        IMember GetMemberByEmail(string email);

        /// <summary>
        /// create the cart page for a member
        /// </summary>
        /// <param name="cartsPage"></param>
        /// <param name="cartMember"></param>
        /// <returns></returns>
        IPublishedContent CreateMemberCartPage(IPublishedContent cartsPage, IMember cartMember);

        /// <summary>
        /// Add the product to the shopping cart
        /// </summary>
        /// <param name="productModel"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        bool AddProductToCart(
            ProductModel productModel, 
            SiteShoppingCart currentShoppingCart,
            out string resultMessage);

        /// <summary>
        /// Clear all items from the current cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        bool ClearShoppingCart(SiteShoppingCart currentShoppingCart, out string resultMessage);

        /// <summary>
        /// Remove a cart item from the shopping cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="cartItemPage"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        bool RemoveShoppingCartItem(
            SiteShoppingCart currentShoppingCart,
            IPublishedContent cartItemPage,
            out string resultMessage);

        /// <summary>
        /// Update the cart items, either delete all of them or just 1
        /// </summary>
        /// <param name="memberShoppingCart"></param>
        /// <param name="clearAllItems"></param>
        /// <param name="cartItemsUpdateMessage"></param>
        /// <param name="priceProductPage"></param>
        /// <returns></returns>
        bool UpdateMemberCartItems(
            SiteShoppingCart memberShoppingCart,
            bool clearAllItems,
            out string cartItemsUpdateMessage,
            IPublishedContent priceProductPage = null);

        /// <summary>
        /// Update a cart item from the shopping cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="cartItemPage"></param>
        /// <param name="cartItemQuantity"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        bool UpdateShoppingCartItem(
            SiteShoppingCart currentShoppingCart,
            IPublishedContent cartItemPage,
            int cartItemQuantity,
            out string resultMessage);

        /// <summary>
        /// Add the shipping to the shopping cart
        /// </summary>
        /// <param name="shippingOption"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <returns></returns>
        bool AddShippingToCart(
            ShippingOption shippingOption,
            SiteShoppingCart currentShoppingCart,
            out string resultMessage);

        /// <summary>
        /// create the order page for a member
        /// </summary>
        /// <param name="ordersPage"></param>
        /// <param name="cartMember"></param>
        /// <param name="orderId"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="paymentMethod"></param>
        /// <returns></returns>
        IPublishedContent CreateMemberOrderPage(IPublishedContent ordersPage,
            IMember cartMember,
            string orderId,
            SiteShoppingCart currentShoppingCart, string paymentMethod);

        /// <summary>
        /// Get the carts stripe session
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        string GetCartStripeSessionId(SiteShoppingCart currentShoppingCart);

        /// <summary>
        /// Finalise the stripe payment
        /// </summary>
        /// <param name="stripePaymentIntent"></param>
        /// <returns></returns>
        bool FinaliseStripePayment(PaymentIntent stripePaymentIntent);

        /// <summary>
        /// get or create the stripe customer from the cart details
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        Customer GetStripeCustomer(SiteShoppingCart currentShoppingCart);

        /// <summary>
        /// place the stripe order and clear the cart
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <param name="stripeOrderDetails"></param>
        /// <returns></returns>
        bool PlaceStripeCartOrder(SiteShoppingCart currentShoppingCart,
            out string resultMessage, out OrderDetails stripeOrderDetails);

        /// <summary>
        /// Get the stripe list of paid payment intents, with optional customer emails
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        List<PaymentIntent> GetStripePaidIntents(string emailAddress = null);

        /// <summary>
        /// Get the paypal order request object
        /// </summary>
        /// <param name="currentShoppingCart"></param>
        /// <returns></returns>
        Task<OrderRequest> GetCartPayPalOrderRequest(SiteShoppingCart currentShoppingCart);

        /// <summary>
        /// Place a paypal order
        /// </summary>
        /// <param name="payPalOrderId"></param>
        /// <param name="currentShoppingCart"></param>
        /// <param name="resultMessage"></param>
        /// <param name="payPalOrderDetails"></param>
        /// <returns></returns>
        bool PlacePayPalOrder(
            string payPalOrderId,
            SiteShoppingCart currentShoppingCart,
            out string resultMessage,
            out OrderDetails payPalOrderDetails);

        /// <summary>
        /// get the member model orders
        /// </summary>
        /// <param name="membersModel"></param>
        /// <returns></returns>
        MembersModel GetMemberOrderDetails(MembersModel membersModel);

        /// <summary>
        /// get the admin model orders
        /// </summary>
        /// <param name="membersModel"></param>
        /// <returns></returns>
        MembersModel GetAdminOrderDetails(MembersModel membersModel);
    }
}