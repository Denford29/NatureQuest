using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Zone.UmbracoMapper.V8;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// add the component composer for the site services
    /// </summary>
    public class ServicesComposer : IUserComposer
    {
        /// <summary>
        /// extend the composer to add product service
        /// </summary>
        /// <param name="composition"></param>
        public void Compose(Composition composition)
        {
            //register the umbraco mapper and its interface
            composition.Register<IUmbracoMapper, UmbracoMapper>(Lifetime.Scope);
            //register the products service and its interface
            composition.Register<IProductsService, ProductsService>(Lifetime.Scope);
            //register the site member service and its interface
            composition.Register<IMemberService, MemberService>(Lifetime.Scope);
            //register the site member service and its interface
            composition.Register<ISiteMembersService, SiteMembersService>(Lifetime.Scope);
        }
    }
}