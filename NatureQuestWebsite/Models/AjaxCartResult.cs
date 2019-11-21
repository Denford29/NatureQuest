namespace NatureQuestWebsite.Models
{
    /// <summary>
    /// class for the result of an ajax call
    /// </summary>
    public class AjaxCartResult
    {
        /// <summary>
        /// set a flag to indicate of the result
        /// </summary>
        public bool ResultSuccess { get; set; }

        /// <summary>
        /// set the result message to show
        /// </summary>
        public string ResultMessage { get; set; }
    }
}