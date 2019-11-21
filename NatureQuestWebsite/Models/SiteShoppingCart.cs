﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// site shopping cart
    /// </summary>
    public class SiteShoppingCart
    {
        /// <summary>
        /// get or set the cart member
        /// </summary>
        public IMember CartMember { get; set; }

        /// <summary>
        /// get or set the member cart page from umbraco
        /// </summary>
        public IPublishedContent MemberCartPage { get; set; }

        /// <summary>
        /// get or set the list of cart items in the shopping cart
        /// </summary>
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        /// <summary>
        /// compute the total price all items in the cart
        /// </summary>
        /// <returns></returns>
        public decimal ComputeTotalValue()
        {
            return CartItems.Sum(cartItem => cartItem.Price * cartItem.Quantity);
        }

        /// <summary>
        /// compute the total price all items in the cart with shipping
        /// </summary>
        /// <returns></returns>
        public decimal ComputeTotalWithShippingValue()
        {
            var productsTotal = ComputeTotalValue();
            //check if we have the selected shipping
            if (!string.IsNullOrWhiteSpace(SelectedShippingOption) && DisplayShippingOptions.Any())
            {
                var selectedOption = DisplayShippingOptions.FirstOrDefault(option =>
                    option.ShippingPricePage.Id.ToString() == SelectedShippingOption);
                if (selectedOption?.ShippingFee > 0)
                {
                    productsTotal += selectedOption.ShippingFee;
                }
            }
            //return the calculated total with shipping
            return productsTotal;
        }


        /// <summary>
        /// get or set the shopping cart page
        /// </summary>
        public IPublishedContent ShoppingCartPage { get; set; }

        /// <summary>
        /// get or set the checkout page
        /// </summary>
        public IPublishedContent CheckoutPage { get; set; }

        /// <summary>
        /// get or set the products page
        /// </summary>
        public IPublishedContent ProductsPage { get; set; }

        /// <summary>
        /// get or set the cart member model
        /// </summary>
        public MembersModel CartMembersModel { get; set; }

        /// <summary>
        /// get or set the display shipping options
        /// </summary>
        public List<ShippingOption> DisplayShippingOptions { get; set; } = new List<ShippingOption>();

        /// <summary>
        /// get or set the select shipping options
        /// </summary>
        public List<SelectListItem> SelectShippingOptions { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// get or set the selected shipping option
        /// </summary>
        public string SelectedShippingOption { get; set; }

        /// <summary>
        /// get or set the selected shipping total
        /// </summary>
        public decimal ShippingTotal { get; set; }
    }

    /// <summary>
    /// create the class for the cart item
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// get or set the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// get or set the price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// get or set the price discount applied to the item
        /// </summary>
        public decimal PriceDiscount { get; set; }

        /// <summary>
        /// get or set the basket description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// get or set the main product page
        /// </summary>
        public IPublishedContent MainProductPage { get; set; }

        /// <summary>
        /// get or set the product line variant page
        /// </summary>
        public IPublishedContent ProductLinePage { get; set; }

        /// <summary>
        /// Umbraco cart page id
        /// </summary>
        public int CartItemPageId { get; set; }

        /// <summary>
        /// get the cart item image
        /// </summary>
        public string CartItemImage { get; set; }
    }

    /// <summary>
    /// create the class for shipping options
    /// </summary>
    public class ShippingOption
    {
        /// <summary>
        /// get or set the shipping details
        /// </summary>
        public string ShippingDetails { get; set; }

        /// <summary>
        /// get or set the delivery time
        /// </summary>
        public string DeliveryTime { get; set; }

        /// <summary>
        /// get or set the shipping fee
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// get or set the shipping price page
        /// </summary>
        public IPublishedContent ShippingPricePage { get; set; }

        /// <summary>
        /// get or set the shipping page id
        /// </summary>
        public int ShippingPageId { get; set; }
    }
}