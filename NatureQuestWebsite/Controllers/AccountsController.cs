using NatureQuestWebsite.Models;
using NatureQuestWebsite.Services;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
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
        /// set the account details page
        /// </summary>
        private readonly IPublishedContent _accountDetailsPage;

        /// <summary>
        /// set the shopping cart details page
        /// </summary>
        private readonly IPublishedContent _checkoutPage;

        /// <summary>
        /// set the home page
        /// </summary>
        private readonly IPublishedContent _homePage;

        /// <summary>
        /// set the registration/login page
        /// </summary>
        private readonly IPublishedContent _registrationLoginPage;


        /// <summary>
        /// initiate the controller with the services used
        /// </summary>
        /// <param name="siteMembersService"></param>
        /// <param name="memberTypeService"></param>
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

            //get the global site settings page to use
            var homePage = Umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
            if (homePage?.Id > 0)
            {
                //save the home page for use later
                _homePage = homePage;

                //get the account details page
                if (homePage.FirstChildOfType("customerDetailsPage")?.Id > 0)
                {
                    _accountDetailsPage = homePage.FirstChildOfType("customerDetailsPage");
                }

                //get the shopping checkout details page
                if (homePage.FirstChildOfType("checkoutPage")?.Id > 0)
                {
                    _checkoutPage = homePage.FirstChildOfType("checkoutPage");
                }

                //get the registration page
                if (homePage.FirstChildOfType("regisrtationPage")?.Id > 0)
                {
                    _registrationLoginPage = homePage.FirstChildOfType("regisrtationPage");
                }
            }
        }

        /// <summary>
        /// get the subscription form
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSubscribeForm()
        {
            // create the default model
            var model = GetMembersModel(new MembersModel(), _currentLoginStatus);

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
            // create the default model
            var model = GetMembersModel(new MembersModel(), _currentLoginStatus);

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
                string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.ContactDetails))
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
                var errorMessage = "Ops... Contact Error, There was an error submitting your contact inquiry.";
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

        /// <summary>
        /// Get the registration and login form
        /// </summary>
        /// <returns></returns>
        public ActionResult GetLoginRegisterForm()
        {
            //check if the user is already logged in
            if(_currentLoginStatus.IsLoggedIn)
            {
                RedirectToAction("GetMembersAccountDetailsView");
            }

            // create the default model for the view
            var model = GetMembersModel(new MembersModel(), _currentLoginStatus);

            // return the view with the model
            return View("/Views/Partials/Accounts/LoginRegisterForms.cshtml", model);
        }

        /// <summary>
        /// process the registration form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ProcessRegistrationForm(MembersModel model)
        {
            //check if the model is valid
            if (model == null || !ModelState.IsValid)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Registration Error, There was an error with your details please check them and try again.";
                TempData["registrationError"] = errorMessage;
                if (model != null) model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check if we have the email address to subscribe
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.HouseAddress)||
                string.IsNullOrWhiteSpace(model.MobileNumber))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Registration Error, Please fill in all the details and try again.";
                TempData["registrationError"] = errorMessage;
                model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check the passwords
            var password = model.Password;
            var confirmPassword = model.PasswordConfirm;
            var passwordCheck = string.CompareOrdinal(password, confirmPassword);
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                 passwordCheck != 0)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Registration Error, Make sure the passwords match.";
                TempData["registrationError"] = errorMessage;
                model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //finally check if the email address has got a user, and if that person is a registered account
            model.ModelMemberType = _memberType;
            var checkedMember = _siteMemberService.GetMemberByEmail(model, model.Email);
            if (checkedMember.LoggedInMember?.Id > 0 && checkedMember.IsShopCustomer)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Registration Error, This email address is already registered, please login to that account.";
                TempData["registrationError"] = errorMessage;
                model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //get the form data to use
            var formData = Request.Form;
            var captchaRequest = formData["g-recaptcha-response"];
            //check if the model is valid
            if (string.IsNullOrWhiteSpace(captchaRequest))
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Registration Error, Please check the Re-Captcha box and try again.";
                TempData["registrationError"] = errorMessage;
                model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //add the member type to the model
            model.ModelMemberType = _memberType;
            model.IsShopCustomer = true;

            //register the email
            var registeredModel = _siteMemberService.RegisterSiteMember(
                                                model,
                                                out MembershipCreateStatus createStatus);

            //check if we have a new member created
            if (registeredModel.LoggedInMember == null || createStatus != MembershipCreateStatus.Success)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Contact Error, There was an error registering your account.";
                TempData["registrationError"] = errorMessage;
                model.RegistrationMessage = errorMessage;
                return CurrentUmbracoPage();
            }
            else
            {
                // set the success message and return the current page
                var successMessage = "Your user account have been registered and ready to use, please login to place an order.";
                TempData["registrationSuccess"] = successMessage;
                model.RegistrationMessage = successMessage;
                return CurrentUmbracoPage();
            }
        }

        /// <summary>
        /// Process the login form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ProcessLoginForm(MembersModel model)
        {
            //check if the model is valid
            if (!ModelState.IsValid)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Login Error, There was an error with your details please check them and try again.";
                TempData["loginError"] = errorMessage;
                model.LoginMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //finally check if the email address has got a user, and if that person is a registered account
            model.ModelMemberType = _memberType;
            var checkedMember = _siteMemberService.GetMemberByEmail(model, model.Email);
            if (checkedMember.LoggedInMember?.Id == 0)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Login Error, There is no account associated with that email address, please register for an account.";
                TempData["loginError"] = errorMessage;
                model.LoginMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check if we have a logged in member and if they are approved
            if (checkedMember.LoggedInMember?.Id > 0 && !checkedMember.LoggedInMember.IsApproved)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Login Error, The account associated with that email address is not approved yet.";
                TempData["loginError"] = errorMessage;
                model.LoginMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //check if we can login the user with the details
            var isMemberValid = Membership.ValidateUser(model.Email, model.Password);
            var isMemberLoggedIn = Members.Login(model.Email, model.Password);
            if (!isMemberLoggedIn || !isMemberValid)
            {
                //save the error message to return
                var errorMessage = "Invalid username or password, please check and try again";
                ModelState.AddModelError("loginModel", errorMessage);
                TempData["loginError"] = errorMessage;
                model.LoginMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //if we have logged in successfully, and have a account details page, redirect to the details page
            if (_accountDetailsPage?.Id > 0)
            {
                return RedirectToUmbracoPage(_accountDetailsPage);
            }

            //redirect to the home page
            return RedirectToUmbracoPage(_homePage);
        }

        /// <summary>
        /// process the member log out
        /// </summary>
        /// <returns></returns>
        public ActionResult ProcessMemberLogout()
        {
            //check if we have a current user logged in
            if (_currentLoginStatus.IsLoggedIn)
            {
                FormsAuthentication.SignOut();
                //redirect to the home page after logging out
                return Redirect("/");
            }
            //just redirect to the current page
            return RedirectToCurrentUmbracoPage();
        }

        /// <summary>
        /// Get the registration and login form
        /// </summary>
        /// <returns></returns>
        public ActionResult GetMembersAccountDetailsView()
        {
            //check if the user is logged in, if not redirect to the login page
            if (!_currentLoginStatus.IsLoggedIn)
            {
                //if we have the registration page redirect there
                if (_registrationLoginPage?.Id > 0)
                {
                    return RedirectToUmbracoPage(_registrationLoginPage);
                }
                //otherwise redirect to the home page
                return RedirectToUmbracoPage(_homePage);
            }
            // create the default model
            var model = GetMembersModel(new MembersModel(), _currentLoginStatus);

            // return the view with the model
            return View("/Views/Partials/Accounts/MemberAcountDetails.cshtml", model);
        }

        /// <summary>
        /// update the member details from the account details page
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UpdateMemberDetails(MembersModel model)
        {
            //check if the model is valid
            if (!ModelState.IsValid)
            {
                // set the error message and return the current page
                var errorMessage = "Ops... Update Error, There was an error with your details please check them and try again.";
                TempData["updateError"] = errorMessage;
                model.UpdateMessage = errorMessage;
                return CurrentUmbracoPage();
            }

            //return to the current page
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// set the member model to use
        /// </summary>
        /// <param name="model"></param>
        /// <param name="loginStatusModel"></param>
        /// <returns></returns>
        public MembersModel GetMembersModel(MembersModel model, LoginStatusModel loginStatusModel)
        {
            // create the default properties
            model.MemberCurrentLoginStatus = loginStatusModel;
            model.ModelMemberType = _memberType;
            model.MemberTypeAlias = _memberType.Alias;

            //if there is a user logged in get the model for that user
            if (loginStatusModel?.IsLoggedIn == true
                && !string.IsNullOrWhiteSpace(loginStatusModel.Email))
            {
                //use the member service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model, loginStatusModel.Email);
            }
            else
            {
                //use the member service to check and get any user details
                model = _siteMemberService.GetMemberByEmail(model);
            }
            //return the set model
            return model;
        }
    }
}