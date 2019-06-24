using Umbraco.Core;
using Umbraco.Core.Composing;
using Zone.UmbracoMapper.V8;

namespace NatureQuestWebsite.Services
{
    /// <summary>
    /// add the component composer for the umbraco mapper
    /// </summary>
    public class UmbracoMapperComposer : IUserComposer
    {
        /// <summary>
        /// extend the composer to add umbraco mapper
        /// </summary>
        /// <param name="composition"></param>
        public void Compose(Composition composition)
        {
            //register the umbraco mapper nd its interface
            composition.Register<IUmbracoMapper, UmbracoMapper>(Lifetime.Scope);
        }
    }
}