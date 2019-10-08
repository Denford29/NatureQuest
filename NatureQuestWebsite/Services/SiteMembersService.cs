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
        /// initiate the site members service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="memberService"></param>
        /// <param name="contextFactory"></param>
        public SiteMembersService(
            ILogger logger,
            IMemberService memberService,
            IUmbracoContextFactory contextFactory)
        {
            _logger = logger;
            _memberService = memberService;

            //get all the roles
            _memberRoles = _memberService.GetAllRoles().ToList();

            //add the default address to the admin list
            _siteToEmailAddresses.Add(new EmailAddress("denfordmutseriwa@yahoo.com", "Admin"));
            //create the default system email address
            _systemEmailAddress = new EmailAddress("denfordmutseriwa@yahoo.com", "Admin");
            _fromEmailAddress = new EmailAddress("support@naturesquest.com.au", _siteName);

            //get the sendgrid api key
            _sendGridKey = WebConfigurationManager.AppSettings["sendGridKey"];

            //get the context to use
            using (var contextReference = contextFactory.EnsureUmbracoContext())
            {
                IPublishedCache contentCache = contextReference.UmbracoContext.ContentCache;
                var siteSettingsPage = contentCache.GetAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
                if (siteSettingsPage?.Id > 0)
                {
                    //get the site details page
                    var siteDetailsPage = siteSettingsPage.Descendants("globalDetails").FirstOrDefault();
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
                        if (siteDetailsPage.HasProperty("contactToEmailAddress") && siteDetailsPage.HasValue("contactToEmailAddress"))
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
                        if (siteDetailsPage.HasProperty("contactFromEmailAddress") && siteDetailsPage.HasValue("contactFromEmailAddress"))
                        {
                            var fromEmailAddress = siteDetailsPage.GetProperty("contactFromEmailAddress").Value().ToString();
                            _fromEmailAddress = new EmailAddress(fromEmailAddress, _siteName);
                        }
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

                //emailAddress = "babybwoy2001@yahoo.co.uk";
                //check if we have an email address passed in to search the member with
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    //use the member service to find the member
                    var existingMember = _memberService.GetByEmail(emailAddress);
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
                                                                                    property.Alias == "houseAddress").ToList();


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
                                }
                            }
                        }

                        //check which roles the member belongs to
                        memberModel.MemberRoles = _memberService.GetAllRoles(existingMember.Id).ToList();

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
                    }
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
                SendGridEmail(systemMessage);
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
                    //get the properties that can be edited
                    var editableProperties = memberModel.LoggedInMember.Properties.Where(property =>
                                                            memberModel.ModelMemberType.MemberCanEditProperty(property.Alias) &&
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
                            }
                        }

                        //get the comments and add the contact details
                        var memberCommentsProperty = memberModel.LoggedInMember.Properties.
                                                                            FirstOrDefault(property => property.Alias == "umbracoMemberComments");
                        if(memberCommentsProperty != null && !string.IsNullOrWhiteSpace(memberModel.ContactDetails))
                        {
                            var currentComments = $"{memberCommentsProperty.GetValue()} {Environment.NewLine} " +
                                                               $"- {DateTime.Now.ToShortDateString()} - {memberModel.ContactDetails}";
                            //set the updated comments value
                            memberCommentsProperty.SetValue(currentComments);

                        }
                    }

                    //set the new members group
                    if (isSubscriptionMembership)
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

                    if (isContactMembership)
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

                    if (isShopMembership)
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
                SendGridEmail(systemMessage);
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
                var userNewsletterSent = SendGridEmail(userNewsletterMessage);

                //create the admin email to notify of a new news letter user
                var newsletterSubject = "A new newsletter member has been created.";
                var newsletterBody = $"<p>A new user with the email address: {newsletterModel.Email} has signed up for the newsletter." +
                                                "<br /> <br />Regards, <br /> Website Team</p>";
                //create the admin email
                var adminNewsletterMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                _systemEmailAddress,
                                                                                newsletterSubject,
                                                                                "",
                                                                                newsletterBody);
                //send the admin email
                var adminNewsletterSent = SendGridEmail(adminNewsletterMessage);

                //create the global emails
                var globalNewsletterMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 newsletterSubject,
                                                                                "",
                                                                                newsletterBody);
                //send the global email
                var globalNewsletterSent = SendGridEmail(globalNewsletterMessage);

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
                var systemMessageSent = SendGridEmail(systemMessage);
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
                var userContactBody = $"<p>Thank you for submitting the contact form, we have recieved you inquiry and a member of the {_siteName}" +
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
                var userContactSent = SendGridEmail(userContactMessage);

                //create the admin email to notify of a contact inquiry
                var adminContactSubject = $"A new contact inquiry has been submitted on {_siteName}.";
                var adminContactBody = "<p>A customer has submitted a user a contact inquiry on the website, with the details below.<br /> <br />" +
                                                    $"Email address: {contactModel.Email} <br />" +
                                                    $"Full name: {contactModel.FullName} <br />" +
                                                    $"Inquiry details: {contactModel.ContactDetails} <br />" +
                                                "<br /> <br />Regards, <br /> Website Team</p>";
                //create the admin email
                var adminContactMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                _systemEmailAddress,
                                                                                adminContactSubject,
                                                                                "",
                                                                                adminContactBody);
                //send the admin email
                var adminContactSent = SendGridEmail(adminContactMessage);

                //create the global emails
                var globalContactMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 adminContactSubject,
                                                                                "",
                                                                                adminContactBody);
                //send the global email
                var globalContactSent = SendGridEmail(globalContactMessage);

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
                var systemMessageSent = SendGridEmail(systemMessage);
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
                var userRegisterBody = $"<p>Thank you for registering for an account, we have recieved your details and the account is active now {_siteName}" +
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
                var userRegisterSent = SendGridEmail(userRegisterMessage);

                //create the admin email to notify of a registration account
                var adminRegisterSubject = $"A new shop account has been created on {_siteName}.";
                var adminRegisterBody = "<p>A customer has created a new shop account on the website, with the details below.<br /> <br />" +
                                                    $"Email address: {registerModel.Email} <br />" +
                                                    $"Full name: {registerModel.FullName} <br />" +
                                                    $"Home address: {registerModel.HouseAddress} <br />" +
                                                    $"Mobile number: {registerModel.MobileNumber} <br />" +
                                                    $"Password: {registerModel.Password} <br />" +
                                                "<br /> <br />Regards, <br /> Website Team</p>";
                //create the admin email
                var adminRegisterMessage = MailHelper.CreateSingleEmail(
                                                                                _fromEmailAddress,
                                                                                _systemEmailAddress,
                                                                                adminRegisterSubject,
                                                                                "",
                                                                                adminRegisterBody);
                //send the admin email
                var adminRegisterSent = SendGridEmail(adminRegisterMessage);

                //create the global emails
                var globalRegisterMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                                                                                _fromEmailAddress,
                                                                                _siteToEmailAddresses,
                                                                                 adminRegisterSubject,
                                                                                "",
                                                                                adminRegisterBody);
                //send the global email
                var globalRegisterSent = SendGridEmail(globalRegisterMessage);

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
                var systemMessageSent = SendGridEmail(systemMessage);
            }

            //return the flag after sending the email
            return registerEmailSent;
        }

        /// <summary>
        /// send the send grid message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> SendGridEmail(SendGridMessage message)
        {
            //set the flag for a message sent
            var messageSent = false;

            //check the key
            if (!string.IsNullOrWhiteSpace(_sendGridKey))
            {
                //get the client to use
                var client = new SendGridClient(_sendGridKey);
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
    }
}

//var newMember = _memberService.CreateMemberWithIdentity(
//    memberModel.Email,
//    memberModel.Email,
//    memberName,
//    memberType);
