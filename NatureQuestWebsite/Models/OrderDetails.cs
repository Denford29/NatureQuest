using System.Collections.Generic;

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
        /// get or set the list of order items from shopping cart
        /// </summary>
        public List<CartItem> OrderItems { get; set; } = new List<CartItem>();
    }
}