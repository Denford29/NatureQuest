using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;
using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// create the members model
    /// </summary>
    public class MembersModel 
    {
        /// <summary>
        /// get or set the login status of the current member
        /// </summary>
        public LoginStatusModel MemberCurrentLoginStatus { get; set; }

        /// <summary>
        /// get or set the member published model
        /// </summary>
        public IMember LoggedInMember { get; set; }

        /// <summary>
        /// get or set the list of member roles
        /// </summary>
        public List<string> MemberRoles { get; set; } = new List<string>();

        /// <summary>
        /// set a flag to indicate this member is a customer
        /// </summary>
        public bool IsShopCustomer { get; set; }

        /// <summary>
        /// set a flag to indicate this member is a newsletter
        /// </summary>
        public bool IsNewsletterMember { get; set; }

        /// <summary>
        /// set a flag to indicate this member is a contact
        /// </summary>
        public bool IsContactMember { get; set; }

        /// <summary>
        /// get or set the subscribed message
        /// </summary>
        public string SubscribeMessage { get; set; }

        /// <summary>
        /// get or set the subscribe text
        /// </summary>
        public string SubscribeText { get; set; }

        /// <summary>
        /// get or set the contact message
        /// </summary>
        public string ContactMessage { get; set; }

        /// <summary>
        /// get or set the registration message
        /// </summary>
        public string RegistrationMessage { get; set; }

        /// <summary>
        /// get or set the login message
        /// </summary>
        public string LoginMessage { get; set; }

        /// <summary>
        /// get or set the update message
        /// </summary>
        public string UpdateMessage { get; set; }

        /// <summary>
        /// get or set the google site key
        /// </summary>
        public string GoogleSiteKey { get; set; }

        /// <summary>
        /// get or set the email address
        /// </summary>
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please check your email address, doesn't look valid")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        /// <summary>
        /// get or set the password
        /// </summary>
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// get or set the password confirm
        /// </summary>
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string PasswordConfirm { get; set; }

        /// <summary>
        /// /get or set the full name
        /// </summary>
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        /// <summary>
        /// /get or set the member mobile number
        /// </summary>
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        /// <summary>
        /// /get or set the member home address
        /// </summary>
        [Display(Name = "Home Address")]
        public string HouseAddress { get; set; }

        /// <summary>
        /// /get or set the inquiry details
        /// </summary>
        [Display(Name = "Inquiry Details")]
        public string ContactDetails { get; set; }

        /// <summary>
        /// get or set the member type of the user
        /// </summary>
        public IMemberType ModelMemberType { get; set; }

        /// <summary>
        /// get or set the member type alias of the user
        /// </summary>
        public string MemberTypeAlias { get; set; }

        /// <summary>
        /// get or set a flag to login the user or not after creation
        /// </summary>
        public bool LoginOnSuccess { get; set; }

        /// <summary>
        /// get or set the members model current cart
        /// </summary>
        public SiteShoppingCart MemberCart { get; set; }

        /// <summary>
        /// set a flag if the user is an admin user
        /// </summary>
        public bool IsAdminUser { get; set; }

        /// <summary>
        /// get or set the account details page
        /// </summary>
        public IPublishedContent AccountDetailsPage { get; set; }

        /// <summary>
        /// get or set the account orders page
        /// </summary>
        public IPublishedContent AccountOrdersPage { get; set; }

        /// <summary>
        /// get or set the stripe orders page
        /// </summary>
        public IPublishedContent StripeOrdersPage { get; set; }

        /// <summary>
        /// get or set the list member orders page
        /// </summary>
        public List<IPublishedContent> MemberOrdersPage { get; set; } = new List<IPublishedContent>();

        /// <summary>
        /// get or set the members orders list
        /// </summary>
        public List<OrderDetails> MemberOrderDetailsList { get; set; } = new List<OrderDetails>();
    }
}