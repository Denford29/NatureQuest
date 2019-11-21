using NatureQuestWebsite.Models;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

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
    }
}