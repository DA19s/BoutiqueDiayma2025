using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace P2FixAnAppDotNetCode.Models.Services
{
    /// <summary>
    /// Provides services method to manage the application language
    /// </summary>
    public class LanguageService : ILanguageService
    {
        /// <summary>
        /// Set the UI language
        /// </summary>
        public void ChangeUiLanguage(HttpContext context, string language)
        {
            string culture = SetCulture(language);
            UpdateCultureCookie(context, culture);
        }

        /// <summary>
        /// Set the culture
        /// </summary>
        public string SetCulture(string language)
        {
            string culture = "";
            // TODO complete the code 
            // Default language is "en", french is "fr" and spanish is "es".
            // DONE OD
            switch (language)
            {
                case "French":
                    culture = "fr";
                    break;
                case "Spanish":
                    culture = "es";
                    break;
                case "Wolof":
                    culture = "wo";
                    break;
                default:
                    culture = "en";
                    break;
            }
            return culture;
        }

        /// <summary>
        /// Update the culture cookie
        /// </summary>
        public void UpdateCultureCookie(HttpContext context, string culture)
        {
            RequestCulture requestCulture = new RequestCulture(culture);

            // Pour le wolof : les textes sont en wolof mais on garde la culture
            // francaise pour le format des nombres, des prix et des dates
            if (culture == "wo")
                requestCulture = new RequestCulture("fr", "wo");

            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture));
        }
    }
}
