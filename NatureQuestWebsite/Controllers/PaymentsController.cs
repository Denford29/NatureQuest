using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using NatureQuestWebsite.Services;
using Stripe;
using Umbraco.Web.WebApi;

namespace NatureQuestWebsite.Controllers
{
    public class PaymentsController : UmbracoApiController
    {
        /// <summary>
        /// create the local read only shipping service
        /// </summary>
        private readonly IShoppingService _shoppingService;

        /// <summary>
        /// initiate the payment controller api
        /// </summary>
        /// <param name="shoppingService"></param>
        public PaymentsController(
            IShoppingService shoppingService
            )
        {
            _shoppingService = shoppingService;
        }

        /// <summary>
        /// process the stripe payment intent , url will be /Umbraco/Api/Payments/ProcessStripePayment
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ProcessStripePayment()
        {
            var eventBody = await new StreamReader(Request.Content.ToString()).ReadToEndAsync();
            //if we cant get the string sent in then return an error
            if (string.IsNullOrWhiteSpace(eventBody))
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }

            try
            {
                //convert the string to the stripe event
                var stripeEvent = EventUtility.ParseEvent(eventBody);

                // Handle the event
                var paymentProcessed = false;
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    paymentProcessed = _shoppingService.FinaliseStripePayment(paymentIntent);
                }
                //else if (stripeEvent.Type == Events.PaymentMethodAttached)
                //{
                //    var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                //    handlePaymentMethodAttached(paymentMethod);
                //}
                // ... handle other event types
                else
                {
                    // Unexpected event type
                    //return BadRequest();
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }
                //return Ok() if the service processing flag is true
                if (paymentProcessed)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                //return an error
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
            catch (StripeException ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
    }
}