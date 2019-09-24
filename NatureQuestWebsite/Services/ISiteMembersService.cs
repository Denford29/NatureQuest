using NatureQuestWebsite.Models;
using System.Web.Security;
using Umbraco.Core.Models;

namespace NatureQuestWebsite.Services
{
    public interface ISiteMembersService
    {
        /// <summary>
        /// get the member model by searching with the email
        /// </summary>
        /// <param name="memberModel"></param>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        MembersModel GetMemberByEmail(
            MembersModel memberModel, 
            string emailAddress = null);

        /// <summary>
        /// Register a new site member from the model
        /// </summary>
        /// <param name="memberModel"></param>
        /// <param name="status"></param>
        /// <param name="logMemberIn"></param>
        /// <returns></returns>
        MembersModel RegisterSiteMember(
            MembersModel memberModel,
            out MembershipCreateStatus status,
            bool logMemberIn = false);
    }
}