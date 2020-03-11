using System;
using System.Collections.Generic;
using Stripe;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// Order processed details
    /// </summary>
    public class OrderDetails
    {
        /// <summary>
        /// get or set the order id
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// get or set the order processed message
        /// </summary>
        public string OrderProcessedMessage { get; set; }

        /// <summary>
        /// get or set the flag to indicate the order has been paid and processed successfully
        /// </summary>
        public bool OrderPaidSuccess { get; set; }

        /// <summary>
        /// get or set the flag to indicate the order has been shipped
        /// </summary>
        public bool OrderShipped { get; set; }

        /// <summary>
        /// get or set the flag to indicate the order has been completed
        /// </summary>
        public bool OrderCompleted { get; set; }

        /// <summary>
        /// get or set the list of order items from shopping cart
        /// </summary>
        public List<CartItem> OrderItems { get; set; } = new List<CartItem>();

        /// <summary>
        /// get or set the payment intent for the order
        /// </summary>
        public PaymentIntent OrderPaymentIntent { get; set; } = new PaymentIntent();

        /// <summary>
        /// get or set the site order member
        /// </summary>
        public IMember LoggedInMember { get; set; }

        /// <summary>
        /// get or set the order shipping details
        /// </summary>
        public ShippingDetails OrderShippingDetails { get; set; } = new ShippingDetails();

        /// <summary>
        /// get or set the site order page
        /// </summary>
        public IPublishedContent SiteOrderPage { get; set; }

        /// <summary>
        /// get or set the order total
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// get or set the order created date
        /// </summary>
        public DateTime OrderCreatedDate { get; set; }
    }
}