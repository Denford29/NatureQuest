using System.Collections.Generic;
using NatureQuestWebsite.Models;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Umbraco.Core.Services;
using System.Web.Security;
using System;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.Web.Configuration;
using SendGrid;
using System.Net;
using Umbraco.Core.Models;

namespace NatureQuestWebsite.Services
{
    public class SiteMembersService : ISiteMembersService
    {
        /// <summary>
        /// set the global details page
        /// </summary>
        private readonly IPublishedContent _globalDetailsPage;

        /// <summary>
        /// create the logger to use
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// create the member service to use
        /// </summary>
        private readonly IMemberService _memberService;

        /// <summary>
        /// get the member type
        /// </summary>
        private readonly IMemberType _memberType;

        /// <summary>
        /// create a list of the members roles
        /// </summary>
        private readonly List<string> _memberRoles;

        /// <summary>
        /// create the list of admin email addresses
        /// </summary>
        private readonly List<EmailAddress> _siteToEmailAddresses = new List<EmailAddress>();

        /// <summary>
        /// create the default system email address
        /// </summary>
        private readonly EmailAddress _systemEmailAddress;

        /// <summary>
        /// create the default from email address
        /// </summary>
        private readonly EmailAddress _fromEmailAddress;

        /// <summary>
        /// create the default send grid key to use
        /// </summary>
        private readonly string _sendGridKey;

        /// <summary>
        /// create the default site name string
        /// </summary>
        private readonly string _siteName = "Natures Quest";

        /// <summary>
        /// set the home page
        /// </summary>
        internal readonly IPublishedContent HomePage;

        /// <summary>
        /// set the account details page
        /// </summary>
        internal readonly IPublishedContent AccountDetailsPage;

        /// <summary>
        /// set the account orders page
        /// </summary>
        internal readonly IPublishedContent AccountOrdersPage;

        /// <summary>
        /// set the stripe orders page
        /// </summary>
        internal readonly IPublishedContent StripeOrdersPage;

        /// <summary>
        /// set the global carts page
        /// </summary>
        private readonly IPublishedContent _globalOrdersPage;

        /// <summary>
        /// initiate the site members service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="memberService"></param>
        /// <param name="contextFactory"></param>
        /// <param name="memberTypeService"></param>
        public SiteMembersService(
            ILogger logger,
            IMemberService memberService,
            IUmbracoContextFactory contextFactory,
            IMemberTypeService memberTypeService)
        {
            _logger = logger;
            _memberService = memberService;

            //get the member type
            _memberType = memberTypeService.Get("Member");

            //get all the roles
            _memberRoles = _memberService.GetAllRoles().ToList();

            //create the default system email address
            _systemEmailAddress = new EmailAddress("admin@rdmonline.com.au", "Admin");
            _fromEmailAddress = new EmailAddress("support@naturesquest.com.au", _siteName);

            //get the sendgrid api key
            _sendGridKey = WebConfigurationManager.AppSettings["sendGridKey"];

            //get the context to use
            using (var contextReference = contextFactory.EnsureUmbracoContext())
            {
                IPublishedCache contentCache = contextReference.UmbracoContext.ContentCache;
                var siteSettingsPage =
                    contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
                if (siteSettingsPage?.Id > 0)
                {
                    //get the site details page
                    var siteDetailsPage = siteSettingsPage.ChildrenOfType("globalDetails").FirstOrDefault();
                    if (siteDetailsPage?.Id > 0)
                    {
                        //save the global details page to use later
                        _globalDetailsPage = siteDetailsPage;

                        //get the site name
                        if (siteDetailsPage.HasProperty("siteName") && siteDetailsPage.HasValue("siteName"))
                        {
                            //set the global site name
                            _siteName = siteDetailsPage.GetProperty("siteName").Value().ToString();
                        }

                        //get the sites contact emails addresses
                        if (siteDetailsPage.HasProperty("contactToEmailAddress") &&
                            siteDetailsPage.HasValue("contactToEmailAddress"))
                        {
                            //set the global site name
                            var adminEmailAddresses = siteDetailsPage.Value<string[]>("contactToEmailAddress");
                            //if we have the contact addresses create the list of emails to send emails to
                            if (adminEmailAddresses.Length > 0)
                            {
                                //delete the default address and add the new ones
                                _siteToEmailAddresses.Clear();
                                foreach (var address in adminEmailAddresses)
                                {
                                    _siteToEmailAddresses.Add(new EmailAddress(address, "Admin"));
                                }
                            }
                        }

                        //get the send grid from the backend
                        if (siteDetailsPage.HasProperty("sendGridAPIKey") && siteDetailsPage.HasValue("sendGridAPIKey"))
                        {
                            _sendGridKey = siteDetailsPage.GetProperty("sendGridAPIKey").Value().ToString();
                        }

                        //get the send grid from the backend
                        if (siteDetailsPage.HasProperty("contactFromEmailAddress") &&
                            siteDetailsPage.HasValue("contactFromEmailAddress"))
                        {
                            var fromEmailAddress = siteDetailsPage.GetProperty("contactFromEmailAddress").Value()
                                .ToString();
                            _fromEmailAddress = new EmailAddress(fromEmailAddress, _siteName);
                        }
                    }
                }

                //get the home page
                var homePage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
                //check if we have the home page and set it to the global page
                if (homePage?.Id > 0)
                {
                    HomePage = homePage;

                    //get the shopping cart details page
                    if (homePage.FirstChildOfType("shoppingCartPage")?.Id > 0)
                    {
                        //get the account details page
                        var customerDetails = homePage.FirstChildOfType("customerDetailsPage");
                        if (customerDetails?.Id > 0)
                        {
                            AccountDetailsPage = customerDetails;
                        }

                        //get the account orders page
                        var accountOrders = AccountDetailsPage.FirstChildOfType("customerOrders");
                        if (accountOrders?.Id > 0)
                        {
                            AccountOrdersPage = accountOrders;
                        }

                        //get the admin stripe orders page
                        var stripeOrders = AccountDetailsPage.FirstChildOfType("stripeOrders");
                        if (stripeOrders?.Id > 0)
                        {
                            StripeOrdersPage = stripeOrders;
                        }
                    }
                }

                //get the carts and orders page
                var storeDetailsPage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "storeDetails");
                if (storeDetailsPage?.Id > 0)
                {
                    //get the site carts page
                    var cartsPage = storeDetailsPage.ChildrenOfType("ordersFolder").FirstOrDefault();
                    if (cartsPage?.Id > 0)
                    {
                        //save the global carts page to use later
                        _globalOrdersPage = cartsPage;
                    }
                }
            }
        }

        /// <summary>
        /// get the member model by searching with the email
        /// </summary>
        /// <param name="memberModel"></param>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public MembersModel GetMemberByEmail(
            MembersModel memberModel,
            string emailAddress = null)
        {
            //try and get the member model details 
            try
            {
                //set the values from the global details page
                if (_globalDetailsPage?.Id > 0)
                {
                    //get the subscribe text
                    if (_globalDetailsPage.HasProperty("subscribeText") && _globalDetailsPage.HasValue("subscribeText"))
                    {
                        //set the subscribe text
                        memberModel.SubscribeText = _globalDetailsPage.Value<string>("subscribeText");
                    }

                    //get the re-captcha site key
                    if (_globalDetailsPage.HasProperty("recaptchaSiteKey") && _globalDetailsPage.HasValue("recaptchaSiteKey"))
                    {
                        //set the re-captcha site key
                        memberModel.GoogleSiteKey = _globalDetailsPage.Value<string>("recaptchaSiteKey");
                    }
                }

                //set the customer pages
                memberModel.AccountDetailsPage = AccountDetailsPage;
                memberModel.AccountOrdersPage = AccountOrdersPage;
                memberModel.StripeOrdersPage = StripeOrdersPage;

                //check if we have an email address passed in to search the member with
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    //use the member service to find the member
                    var existingMember = GetMemberByEmail(emailAddress);
                    if (existingMember != null && existingMember.Id != 0)
                    {
                        // add the member to the model
                        memberModel.LoggedInMember = existingMember;

                        //set the model details from the existing member
                        memberModel.Email = existingMember.Email;

                        //get the properties that can be edited
                        var editableProperties = existingMember.Properties.Where(property =>
                                                                                    property.Alias == "fullName" ||
                                                                                    property.Alias == "mobileNumber" ||
                                                                                    property.Alias == "houseAddress" ||
                                                                                    property.Alias == "suburb" ||
                                                                                    property.Alias == "postCode" ||
                                                                                    property.Alias == "state").ToList();


                        //get the values from the properties to set them on the model
                        if (editableProperties.Any())
                        {
                            foreach (var memberProperty in editableProperties)
                            {
                                var propertyAlias = memberProperty.Alias.ToLower();
                                switch (propertyAlias)
                                {
                                    case "fullname":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.FullName = (string)memberProperty.GetValue();
                                        }
                                        break;
                                    case "mobilenumber":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.MobileNumber = (string)memberProperty.GetValue();
                                        }
                                        break;
                                    case "houseaddress":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.HouseAddress = (string)memberProperty.GetValue();
                                        }
                                        break;
                                    case "suburb":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.Suburb = (string)memberProperty.GetValue();
                                        }
                                        break;
                                    case "postcode":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.PostCode = (string)memberProperty.GetValue();
                                        }
                                        break;
                                    case "state":
                                        if (memberProperty.GetValue() != null && !string.IsNullOrWhiteSpace((string)memberProperty.GetValue()))
                                        {
                                            memberModel.State = (string)memberProperty.GetValue();
                                        }
                                        break;
                                }
                            }
                        }

                        //check which roles the member belongs to
                        var memberRoles = _memberService.GetAllRoles(existingMember.Id).ToList();
                        memberModel.MemberRoles = memberRoles;

                        //check if the model is set to update the newsletter
                        if (memberModel.IsNewsletterMember)
                        {
                            //get the subscription group
                            var subscriptionGroup = _memberRoles.FirstOrDefault(role => role == "Newsletter");
                            if (subscriptionGroup != null)
                            {
                                //set the role on the user
                                _memberService.AssignRole(memberModel.LoggedInMember.Username, subscriptionGroup);
                                // send the newsletter emails
                                CreateAndSendNewsletterEmail(memberModel);
                                //save the updated details
                                _memberService.Save(memberModel.LoggedInMember);
                            }
                        }

                        //if we have the members roles set flags on which roles they belong to
                        if (memberModel.MemberRoles.Any())
                        {
                            //set the flags for which roles the member is in
                            memberModel.IsContactMember = memberModel.MemberRoles.FirstOrDefault(role => role == "Contact")?.Any() == true;
                            memberModel.IsNewsletterMember = memberModel.MemberRoles.FirstOrDefault(role => role == "Newsletter")?.Any() == true;
                            memberModel.IsShopCustomer = memberModel.MemberRoles.FirstOrDefault(role => role == "Customer")?.Any() == true;
                        }

                        //set the members site orders
                        var memberOrders = _globalOrdersPage.Children.
                                                                                        Where(page => page.HasProperty("orderMember") &&
                                                                                        page.HasValue("orderMember") &&
                                                                                        page.Value<IPublishedContent>("orderMember")?.Name == existingMember.Name)
                                                                                        .ToList();
                        if (memberOrders.Any())
                        {
                            memberModel.MemberOrdersPage = memberOrders;
                        }

                        //get the admin orders 
                        var adminOrders = _globalOrdersPage.Children.
                            Where(page => page.IsPublished())
                            .ToList();

                        if (adminOrders.Any())
                        {
                            memberModel.AdminOrdersPage = adminOrders;
                        }

                        //set a flag if the member is an admin user
                        memberModel.IsAdminUser = !string.IsNullOrWhiteSpace(memberModel.MemberRoles.FirstOrDefault(role => role == "Site Admins"));
                    }
                }
                else
                {
                    //set the default values for the model
                    memberModel.IsNewsletterMember = true;
                }

            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("SiteMembersService"), ex, "Error getting member details model");
                //send an admin email with the error
                var errorMessage = $"Error getting member details model.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Member member details model error on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(_fromEmailAddress, _systemEmailAddress, errorSubject, "", errorMessage);
                _ = SendGridEmail(systemMessage);
            }

            //return the model 
            return memberModel;
        }

        /// <summary>
        /// Register a new site member from the model
        /// </summary>
        /// <param name="memberModel"></param>
        /// <param name="status"></param>
        /// <param name="logMemberIn"></param>
        /// <returns></returns>
        public MembersModel RegisterSiteMember(
            MembersModel memberModel,
            out MembershipCreateStatus status,
            bool logMemberIn = false)
        {
            //create the default status
            status = MembershipCreateStatus.UserRejected;

            //try and create the new member
            try
            {
                //create the flags to indicate which account we are registering
                var isSubscriptionMembership = memberModel.IsNewsletterMember;
                var isContactMembership = memberModel.IsContactMember;
                var isShopMembership = memberModel.IsShopCustomer;

                //check if we already have a member on the model if not then create a new one
                if (memberModel.LoggedInMember == null)
                {
                    //check if we have an email to use
                    if (!string.IsNullOrWhiteSpace(memberModel.Email))
                    {
                        //create the values to use for the new member, depending on the member group
                        var memberEmail = memberModel.Email;
                        var memberName = memberModel.Email;
                        var memberPassword = isShopMembership ? memberModel.Password : "user";
                        var passwordQst = "question";
                        var passwordAsn = "answer";
                        var approveUser = isShopMembership;

                        //create the new member
                        var newMember = Membership.CreateUser(
                            memberName,
                            memberPassword,
                            memberEmail,
                            passwordQst,
                            passwordAsn,
                            approveUser,
                            out status);

                        //check if the member has been created fine
                        if (newMember != null && status == MembershipCreateStatus.Success)
                        {
                            var newCreatedMember = _memberService.GetByEmail(memberEmail);
                            if (newCreatedMember != null)
                            {
                                memberModel.LoggedInMember = newCreatedMember;
                            }
                        }
                    }
                }

                //check if we have a member on the model either new or existing one
                if (memberModel.LoggedInMember?.Id > 0)
                {
                    //get the current member to use
                    var currentMember = memberModel.LoggedInMember;
                    //get the properties that can be edited
                    var editableProperties = currentMember.Properties.Where(property =>
                                                            _memberType.MemberCanEditProperty(property.Alias) &&
                                                            !string.IsNullOrWhiteSpace(property.Alias)).ToList();

                    //get the values from the properties to set them on the model
                    if (editableProperties.Any())
                    {
                        foreach (var memberProperty in editableProperties)
                        {
                            var propertyAlias = memberProperty.Alias.ToLower();
                            switch (propertyAlias)
                            {
                                case "fullname":
                                    if (!string.IsNullOrWhiteSpace(memberModel.FullName))
                                    {
                                        memberProperty.SetValue(memberModel.FullName);
                                    }
                                    break;
                                case "mobilenumber":
                                    if (!string.IsNullOrWhiteSpace(memberModel.MobileNumber))
                                    {
                                        memberProperty.SetValue(memberModel.MobileNumber);
                                    }
                                    break;
                                case "houseaddress":
                                    if (!string.IsNullOrWhiteSpace(memberModel.HouseAddress))
                                    {
                                        memberProperty.SetValue(memberModel.HouseAddress);
                                    }
                                    break;
                                case "suburb":
                                    if (!string.IsNullOrWhiteSpace(memberModel.Suburb))
                                    {
                                        memberProperty.SetValue(memberModel.Suburb);
                                    }
                                    break;
                                case "postcode":
                                    if (!string.IsNullOrWhiteSpace(memberModel.PostCode))
                                    {
                                        memberProperty.SetValue(memberModel.PostCode);
                                    }
                                    break;
                                case "state":
                                    if (!string.IsNullOrWhiteSpace(memberModel.State))
                                    {
                                        memberProperty.SetValue(memberModel.State);
                                    }
                                    break;
                            }
                        }

                        //get the comments and add the contact details
                        var memberCommentsProperty = currentMember.Properties.
                                                                            FirstOrDefault(property => property.Alias == "umbracoMemberComments");
                        if(memberCommentsProperty != null && !string.IsNullOrWhiteSpace(memberModel.ContactDetails))
                        {
                            var currentComments = $"{memberCommentsProperty.GetValue()} {Environment.NewLine} " +
                                                               $"- {DateTime.Now.ToShortDateString()} - {memberModel.ContactDetails}";
                            //set the updated comments value
                            memberCommentsProperty.SetValue(currentComments);

                        }
                    }

                    //check which roles the member belongs to
                    memberModel.MemberRoles = _memberService.GetAllRoles(currentMember.Id).ToList();

                    //set the default flags for the members roles
                    var isCurrentlySubscribe = false;
                    var isCurrentlyContact = false;
                    var isCurrentlyCustomer = false;

                    //if we have the members roles set flags on which roles they belong to
                    if (memberModel.MemberRoles.Any())
                    {
                        //set the flags for which roles the member is in
                        isCurrentlyContact = memberModel.MemberRoles.FirstOrDefault(role => role == "Contact")?.Any() == true;
                        isCurrentlySubscribe = memberModel.MemberRoles.FirstOrDefault(role => role == "Newsletter")?.Any() == true;
                        isCurrentlyCustomer = memberModel.MemberRoles.FirstOrDefault(role => role == "Customer")?.Any() == true;
                    }

                    //set the new members group , if the member is not already in the groups
                    if (isSubscriptionMembership && !isCurrentlySubscribe)
                    {
                        //get the subscription group
                        var subscriptionGroup = _memberRoles.FirstOrDefault(role => role == "Newsletter");
                        if (subscriptionGroup != null)
                        {
                            //set the role on the user
                            _memberService.AssignRole(memberModel.LoggedInMember.Username, subscriptionGroup);
                            // send the newsletter emails
                            CreateAndSendNewsletterEmail(memberModel);
                        }
                    }

                    if (isContactMembership && !isCurrentlyContact)
                    {
                        //get the subscription group
                        var contactGroup = _memberRoles.FirstOrDefault(role => role == "Contact");
                        if (contactGroup != null)
                        {
                            //set the role on the user
                            _memberService.AssignRole(memberModel.LoggedInMember.Username, contactGroup);
                            // send the contact emails
                            CreateAndSendContactEmail(memberModel);
                        }
                    }

                    if (isShopMembership && !isCurrentlyCustomer)
                    {
                        //get the subscription group
                        var shopGroup = _memberRoles.FirstOrDefault(role => role == "Customer");
                        if (shopGroup != null)
                        {
                            //set the role on the user
                            _memberService.AssignRole(memberModel.LoggedInMember.Username, shopGroup);
                            // send the contact emails
                            CreateAndSendRegisterEmail(memberModel);
                        }
                    }

                    //save the updated details
                    _memberService.Save(memberModel.LoggedInMember);
                    //update the status to return
                    status = MembershipCreateStatus.Success;
                }
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("SiteMembersService"), ex, "Error creating a new member");
                //send an admin email with the error
                var errorMessage = $"Error creating a new member<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Member creation error on {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(_fromEmailAddress, _systemEmailAddress, errorSubject, "", errorMessage);
                _ = SendGridEmail(systemMessage);
            }

            //return the model after registration
            return memberModel;
        }

        /// <summary>
        /// create and send the newsletter sign-up emails
        /// </summary>
        /// <param name="newsletterModel"></param>
        /// <returns></returns>
        public bool CreateAndSendNewsletterEmail(MembersModel newsletterModel)
        {
            //create the default flag
            var signupEmailSent = false;

            try
            {
                //create the user email to confirm news letter signup
                var userNewsletterSubject = $"Your email has been subscribed to the {_siteName} newsletter";
                var userNewsletterBody = $"<p>Thank you for subscribing to our newsletter, your email address: {newsletterModel.Email} " +
                                                        $"has been added to the list<br /> <br />Regards, <br /> {_siteName} Team</p>";
                var userEmail = new EmailAddress(newsletterModel.Email);
                //send the user email
                var userNewsletterMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                userEmail,
                                                                                userNewsletterSubject,
                                                                                "",
                                                                                userNewsletterBody);
                //send the user email
                _ = SendGridEmail(userNewsletterMessage, true);

                //create the admin email to notify of a new news letter user
                var newsletterSubject = "A new newsletter member has been created.";
                var newsletterBody = $"<p>A new user with the email address: {newsletterModel.Email} has signed up for the newsletter." +
                                                "<br /> <br />Regards, <br /> Website Team</p>";

                //create the global emails
                var globalNewsletterMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 newsletterSubject,
                                                                                "",
                                                                                newsletterBody);
                //send the global email
               _ = SendGridEmail(globalNewsletterMessage, true);

                signupEmailSent = true;
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("SiteMembersService"), ex, "Error creating and sending newsletter email");
                //send an admin email with the error
                var errorMessage = $"Error creating and sending newsletter email.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Newsletter email error on _siteName {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(_fromEmailAddress, _systemEmailAddress, errorSubject, "", errorMessage);
                _ = SendGridEmail(systemMessage);
            }

            //return the flag after sending the email
            return signupEmailSent;
        }

        /// <summary>
        /// create and send the contact submitted emails
        /// </summary>
        /// <param name="contactModel"></param>
        /// <returns></returns>
        public bool CreateAndSendContactEmail(MembersModel contactModel)
        {
            //create the default flag
            var contactEmailSent = false;

            try
            {
                //create the admin email to notify of a new news letter user
                var userContactSubject = $"Your contact has been submitted on {_siteName}.";
                var userContactBody = $"<p>Thank you for submitting the contact form, we have received you inquiry and a member of the {_siteName}" +
                                                        $" will get back to you shortly.<br /> <br />Regards, <br /> {_siteName} Team</p>";
                var userEmail = new EmailAddress(contactModel.Email);
                //send the user email
                var userContactMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                userEmail,
                                                                                userContactSubject,
                                                                                "",
                                                                                userContactBody);
                //send the user email
               _ = SendGridEmail(userContactMessage, true);

                //create the admin email to notify of a contact inquiry
                var adminContactSubject = $"A new contact inquiry has been submitted on {_siteName}.";
                var adminContactBody = "<p>A customer has submitted a user a contact inquiry on the website, with the details below.<br /> <br />" +
                                                    $"Email address: {contactModel.Email} <br />" +
                                                    $"Full name: {contactModel.FullName} <br />" +
                                                    $"Inquiry details: {contactModel.ContactDetails} <br />" +
                                                "<br /> <br />Regards, <br /> Website Team</p>";

                //create the global emails
                var globalContactMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 adminContactSubject,
                                                                                "",
                                                                                adminContactBody);
                //send the global email
                _ = SendGridEmail(globalContactMessage, true);

                contactEmailSent = true;
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("SiteMembersService"), ex, "Error creating and sending contact email");
                //send an admin email with the error
                var errorMessage = $"Error creating and sending contact email.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Contact email error on _siteName {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(_fromEmailAddress, _systemEmailAddress, errorSubject, "", errorMessage);
                _ = SendGridEmail(systemMessage);
            }

            //return the flag after sending the email
            return contactEmailSent;
        }

        /// <summary>
        /// create and send the registration submitted emails
        /// </summary>
        /// <param name="registerModel"></param>
        /// <returns></returns>
        public bool CreateAndSendRegisterEmail(MembersModel registerModel)
        {
            //create the default flag
            var registerEmailSent = false;

            try
            {
                //create the admin email to notify of a new shop user
                var userRegisterSubject = $"Your account registration has been submitted on {_siteName}.";
                var userRegisterBody = $"<p>Thank you for registering for an account, we have received your details and the account is active now {_siteName}" +
                                                        $"<br /> <br />Regards, <br /> {_siteName} Team</p>";
                var userEmail = new EmailAddress(registerModel.Email);
                //send the user email
                var userRegisterMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                userEmail,
                                                                                userRegisterSubject,
                                                                                "",
                                                                                userRegisterBody);
                //send the user email
               _ = SendGridEmail(userRegisterMessage, true);

                //create the admin email to notify of a registration account
                var adminRegisterSubject = $"A new shop account has been created on {_siteName}.";
                var adminRegisterBody = "<p>A customer has created a new shop account on the website, with the details below.<br /> <br />" +
                                                    $"Email address: {registerModel.Email} <br />" +
                                                    $"Full name: {registerModel.FullName} <br />" +
                                                    $"Mobile number: {registerModel.MobileNumber} <br />" +
                                                    $"Home address: {registerModel.HouseAddress} <br />" +
                                                    $"Address suburb: {registerModel.Suburb} <br />" +
                                                    $"Address post code: {registerModel.PostCode} <br />" +
                                                    $"Address state: {registerModel.State} <br />" +
                                                    $"Password: {registerModel.Password} <br />" +
                                                "<br /> <br />Regards, <br /> Website Team</p>";

                //create the global emails
                var globalRegisterMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 adminRegisterSubject,
                                                                                "",
                                                                                adminRegisterBody);
                //send the global email
              _ = SendGridEmail(globalRegisterMessage,true);

                registerEmailSent = true;
            }
            catch (Exception ex)
            {
                //there was an error getting member details 
                _logger.Error(Type.GetType("SiteMembersService"), ex, "Error creating and sending registration email");
                //send an admin email with the error
                var errorMessage = $"Error creating and sending registration email.<br /><br />{ex.Message}<br /><br />{ex.StackTrace}<br /><br />{ex.InnerException}";
                var errorSubject = $"Contact email error on _siteName {_siteName}";

                //system email
                var systemMessage = MailHelper.CreateSingleEmail(_fromEmailAddress, _systemEmailAddress, errorSubject, "", errorMessage);
                _ = SendGridEmail(systemMessage);
            }

            //return the flag after sending the email
            return registerEmailSent;
        }

        /// <summary>
        /// send the send grid message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="autoAddBcc"></param>
        /// <param name="autoAddAdminBcc"></param>
        /// <returns></returns>
        public async Task<bool> SendGridEmail(SendGridMessage message, bool autoAddBcc = false, bool autoAddAdminBcc = false)
        {
            //set the flag for a message sent
            var messageSent = false;

            //check the key
            if (!string.IsNullOrWhiteSpace(_sendGridKey))
            {
                //get the client to use
                var client = new SendGridClient(_sendGridKey);

                //check if we need to add the auto bcc
                if (autoAddBcc)
                {
                    message.AddBcc(_systemEmailAddress);
                }

                //check if we need to add the admin auto bcc as well
                if (autoAddAdminBcc && _siteToEmailAddresses.Any())
                {
                    foreach (var emailAddress in _siteToEmailAddresses)
                    {
                        message.AddBcc(emailAddress);
                    }
                }

                //send the email and get the response
                var response = await client.SendEmailAsync(message);

                //check the response
                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    messageSent = true;
                }

            }
            //return the flag
            return messageSent;
        }

        /// <summary>
        /// get a member by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public IMember GetMemberByEmail(string email)
        {
            return _memberService.GetByEmail(email);
        }
    }
}

