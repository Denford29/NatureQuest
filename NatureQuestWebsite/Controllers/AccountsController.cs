using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace NatureQuestWebsite.Controllers
{
    public class AccountsController : SurfaceController
    {
        /// <summary>
        /// set the global details page
        /// </summary>
        private readonly IPublishedContent _globalDetailsPage;

        /// <summary>
        /// get the custom site members service
        /// </summary>
        private readonly ISiteMembersService _siteMemberService;

        /// <summary>
        /// get the member type service
        /// </summary>
        private readonly IMemberTypeService _memberTypeService;

        /// <summary>
        /// get the member type
        /// </summary>
        private readonly IMemberType _memberType;

        /// <summary>
        /// set the current login status to use
        /// </summary>
        private readonly LoginStatusModel _currentLoginStatus;

        /// <summary>
        /// initiate the controller with the services used
        /// </summary>
        /// <param name="siteMembersService"></param>
        public AccountsController(
            ISiteMembersService siteMembersService,
            IMemberTypeService memberTypeService)
        {
            //set the member service to use
            _siteMemberService = siteMembersService;

            //set the member type service to use
            _memberTypeService = memberTypeService;

            //get the member type
            _memberType = _memberTypeService.Get("Member");

            // get the login status
            _currentLoginStatus = Members.GetCurrentLoginStatus();

            //get the global site settings page to use
            var siteSetting = Umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
            if (siteSetting?.Id > 0)
            {
                //get the site details page
                var siteDetailsPage = siteSetting.Children.FirstOrDefault(child => child.ContentType.Alias == "globalDetails");
                if (siteDetailsPage?.Id > 0)
                {
                    //save the global details page to use later
                    _globalDetailsPage = siteDetailsPage;
                }
            }
        }

        /// <summary>
        /// get the subscription form
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSubscribeForm()
        {
            // create the default
            var model = new MembersModel
            {
                MemberCurrentLoginStatus = _currentLoginStatus,
                ModelMemberType = _memberType
            };

            //if there is a user logged in get the model for that user
            if (_currentLoginStatus?.IsLoggedIn == true
                && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                //use themember service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model, _currentLoginStatus.Email);
            }
            else
            {
                //use themember service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model);
            }

            //return the model with the view
            return View("/Views/Partials/Accounts/Subscribe.cshtml", model);
        }

        /// <summary>
        /// process the subscribe form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ProcessSubscribeForm(MembersModel model)
        {
            //check if the model is valid
            if (!ModelState.IsValid)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Subscription Error, There was an error with your details please check them and try again.";
                TempData["subscriptionError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check if we have the email address to subscribe
            if (string.IsNullOrWhiteSpace(model?.Email))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Subscription Error, Please fill in your email address and try again.";
                TempData["subscriptionError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //get the form data to use
            var formData = Request.Form;
            var captchaRequest = formData["g-recaptcha-response"];
            //check if the model is valid
            if (string.IsNullOrWhiteSpace(captchaRequest))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Subscription Error, Please check the Re-Captcha box and try again.";
                TempData["subscriptionError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //finally check if the email address has already subscribed
            var checkedMember = _siteMemberService.GetMemberByEmail(model, model.Email);
            if (checkedMember.LoggedInMember != null && checkedMember.IsNewsletterMember)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Subscription Error, This email address is already subscribed and try again.";
                TempData["subscriptionError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //set which the subscription flag on the model to indicate which account to create
            model.IsNewsletterMember = true;
            //subscribe the email
            var registeredModel = _siteMemberService.RegisterSiteMember(
                                                model,
                                                out MembershipCreateStatus createStatus);

            //check if we have a new member created
            if (registeredModel.LoggedInMember == null || createStatus != MembershipCreateStatus.Success)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Subscription Error, There was an error registration your newsletter account.";
                TempData["subscriptionError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }
            else
            {
                // set the success message and return the current page
                var successMessage = "Your newsletter account has been successfully created.";
                TempData["subscriptionSuccess"] = successMessage;
                model.SubscribeMessage = successMessage;
                return CurrentUmbracoPage();
            }
        }

        /// <summary>
        /// get the contact form with the model
        /// </summary>
        /// <returns></returns>
        public ActionResult GetContactForm()
        {
            // create the default
            var model = new MembersModel
            {
                MemberCurrentLoginStatus = _currentLoginStatus,
                ModelMemberType = _memberType
            };

            //if there is a user logged in get the model for that user
            if (_currentLoginStatus?.IsLoggedIn == true
                && !string.IsNullOrWhiteSpace(_currentLoginStatus.Email))
            {
                //use themember service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model, _currentLoginStatus.Email);
            }
            else
            {
                //use themember service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model);
            }

            //return the model with the view
            return View("/Views/Partials/Accounts/ContactForm.cshtml", model);
        }

        /// <summary>
        /// process the contact form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ProcessContactForm(MembersModel model)
        {
            //check if the model is valid
            if (!ModelState.IsValid)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Contact Error, There was an error with your details please check them and try again.";
                TempData["contactError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check if we have the email address to subscribe
            if (string.IsNullOrWhiteSpace(model?.Email) ||
                string.IsNullOrWhiteSpace(model?.FullName) ||
                string.IsNullOrWhiteSpace(model?.ContactDetails))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Contact Error, Please fill in all the details and try again.";
                TempData["contactError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //get the form data to use
            var formData = Request.Form;
            var captchaRequest = formData["g-recaptcha-response"];
            //check if the model is valid
            if (string.IsNullOrWhiteSpace(captchaRequest))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Contact Error, Please check the Re-Captcha box and try again.";
                TempData["contactError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //add the member type to the model
            model.ModelMemberType = _memberType;

            //finally check if the email address has got a user, and if that person is logged in and save the login status
            var checkedMember = _siteMemberService.GetMemberByEmail(model, model.Email);
            if(checkedMember.LoggedInMember?.Id > 0 &&
                _currentLoginStatus?.IsLoggedIn == true &&
                _currentLoginStatus.Email == checkedMember.LoggedInMember.Email)
            {
                model.MemberCurrentLoginStatus = _currentLoginStatus;
            }
            //set which the subscription flag on the model to indicate which account to create
            model.IsContactMember = true;
            model.IsNewsletterMember = model.IsNewsletterMember;

            //subscribe the email
            var registeredModel = _siteMemberService.RegisterSiteMember(
                                                model,
                                                out MembershipCreateStatus createStatus);

            //check if we have a new member created
            if (registeredModel.LoggedInMember == null || createStatus != MembershipCreateStatus.Success)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Contact Error, There was an error sub.";
                TempData["contactError"] = errorMessage;
                model.SubscribeMessage = errorMessage;
                return CurrentUmbracoPage();
            }
            else
            {
                // set the success message and return the current page
                var successMessage = "Your contact details have been submitted.";
                TempData["contactSuccess"] = successMessage;
                model.SubscribeMessage = successMessage;
                return CurrentUmbracoPage();
            }
        }
    }
}