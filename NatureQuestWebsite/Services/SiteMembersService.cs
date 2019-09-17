using System.Collections.Generic;
using NatureQuestWebsite.Models;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Umbraco.Core.Services;
using System.Web.Security;
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
        /// create a list of the members roles
        /// </summary>
        private readonly List<string> _memberRoles = new List<string>();

        /// <summary>
        /// initiate the site members service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="memberService"></param>
        public SiteMembersService(
            ILogger logger,
            IMemberService memberService,
            IUmbracoContextFactory contextFactory)
        {
            _logger = logger;
            _memberService = memberService;

            //get all the roles
            _memberRoles = _memberService.GetAllRoles().ToList();

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

            //check if we have an email address passed in to search the member with
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                //use the member service to find the member
                var existingMember = _memberService.GetByEmail(emailAddress);
                if (existingMember?.Id != 0)
                {
                    // add the member to the model
                    memberModel.LoggedInMember = existingMember;

                    //check which roles the member belongs to
                    memberModel.MemberRoles = _memberService.GetAllRoles(existingMember.Id).ToList();

                    //if we have the members roles set flags on which roles they belong to
                    if (memberModel.MemberRoles.Any())
                    {
                        //set the flags for which roles the member is in
                        memberModel.IsContactMember = memberModel.MemberRoles.FirstOrDefault(role => role == "Contact").Any();
                        memberModel.IsNewsletterMember = memberModel.MemberRoles.FirstOrDefault(role => role == "Newsletter").Any();
                        memberModel.IsShopCustomer = memberModel.MemberRoles.FirstOrDefault(role => role == "Customer").Any();
                    }
                }
            }

            //return the model 
            return memberModel;
        }

        /// <summary>
        /// Register a new site member from the model
        /// </summary>
        /// <param name="memberModel"></param>
        /// <param name="memberType"></param>
        /// <param name="status"></param>
        /// <param name="logMemberIn"></param>
        /// <returns></returns>
        public MembersModel RegisterSiteMember(
            MembersModel memberModel,
            IMemberType memberType,
            out MembershipCreateStatus status,
            bool logMemberIn = false)
        {
            //create the default status
            status = MembershipCreateStatus.UserRejected;

            //create the flags to indicate which account we are registering
            var isSubscriptionMembership = memberModel.IsNewsletterMember;
            var isContactMembership = memberModel.IsContactMember;
            var isShopMembership = memberModel.IsShopCustomer;

            //check if we have an email to use
            if (!string.IsNullOrWhiteSpace(memberModel.Email))
            {
                //create the default name, if the account is a news letter use the email otherwise use the full name
                var memberName = isSubscriptionMembership ? memberModel.Email : memberModel.FullName;

                //create the user
                var newMember = _memberService.CreateMemberWithIdentity(
                    memberModel.Email,
                    memberModel.Email,
                    memberName,
                    memberType);

            }
            //return the model after registration
            return memberModel;
        }
    }
}