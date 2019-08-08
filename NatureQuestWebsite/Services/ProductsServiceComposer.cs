using Umbraco.Core;
using Umbraco.Core.Composing;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// add the component composer for the product service
    /// </summary>
    public class ProductsServiceComposer : IUserComposer
    {
        /// <summary>
        /// extend the composer to add product service
        /// </summary>
        /// <param name="composition"></param>
        public void Compose(Composition composition)
        {
            //register the umbraco mapper nd its interface
            composition.Register<IProductsService, ProductsService>(Lifetime.Scope);
        }
    }
}