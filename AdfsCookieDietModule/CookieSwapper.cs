using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using log4net;

namespace VflIt.Samples.AdfsCookieDiet
{
    internal class CookieSwapper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string MsisAuthCookiePattern = @"^MSISAuth\d*$";
        //private const string MsisAuthCookiePattern = "^idsrvauth\d*$";

        private const string SessionCookieReferenceKeyName = "SessionCookieKey";
        private readonly HttpApplication m_HttpApplication;
        private readonly Regex m_MsisAuthCookieRegex;
        private readonly AbstractSessionSecurityTokenCookieStore m_SessionSessionSecurityTokenCookieStore;

        public CookieSwapper(AbstractSessionSecurityTokenCookieStore sessionSessionSecurityTokenCookieStore,
                             HttpApplication httpApplication)
        {
            m_SessionSessionSecurityTokenCookieStore = sessionSessionSecurityTokenCookieStore;
            m_HttpApplication = httpApplication;
            Log.DebugFormat("Cookie regex pattern is {0}", MsisAuthCookiePattern);
            m_MsisAuthCookieRegex = new Regex(MsisAuthCookiePattern, RegexOptions.IgnoreCase);
        }


        private IEnumerable<HttpCookie> GetMsisCookies(HttpCookieCollection cookies)
        {

            IOrderedEnumerable<HttpCookie> msisAuthCookies = 
                cookies
                    .AllKeys
                    .Select(key => cookies[key])
                    .Where(cookie => m_MsisAuthCookieRegex.IsMatch(cookie.Name))
                    .OrderBy(cookie => cookie.Name);
            return msisAuthCookies;

        }

        private HttpCookie GetReferenceCookie(HttpCookieCollection cookies)
        {

            HttpCookie referenceCookie = 
                cookies
                    .AllKeys
                    .Select(key => cookies[key])
                    .Where(cookie => cookie.Name.Equals(SessionCookieReferenceKeyName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
            return referenceCookie;
        }

        public void SwapSessionSecurityTokenCookieWithReference()
        {
            HttpResponse response = m_HttpApplication.Response;
            HttpCookieCollection cookies = response.Cookies;

            LogCookies("SwapSessionSecurityTokenCookieWithReference cookies pre-replacement", cookies);

            IEnumerable<HttpCookie> msisAuthCookies = GetMsisCookies(cookies);

            if (!msisAuthCookies.Any())
            {
                Log.Debug("No MSISAuth cookies found");
                return;
            }
            var tokenCookie = new TokenCookie(msisAuthCookies);
            Guid cookieKeyValue = m_SessionSessionSecurityTokenCookieStore.StoreTokenCookie(tokenCookie);

            HttpCookie templateCookie = msisAuthCookies.First();
            var keyCookie = new HttpCookie(SessionCookieReferenceKeyName, cookieKeyValue.ToString())
                                {
                                    Domain = templateCookie.Domain,
                                    Expires = templateCookie.Expires,
                                    HttpOnly = templateCookie.HttpOnly,
                                    Path = templateCookie.Path,
                                    Secure = templateCookie.Secure
                                };
            cookies.Add(keyCookie);
            foreach (HttpCookie cookie in msisAuthCookies)
            {
                cookies.Remove(cookie.Name);
            }
            LogCookies("SwapSessionSecurityTokenCookieWithReference cookies post-replacement", cookies);
        }

        public void SwapReferenceWithSessionSecurityTokenCookie()
        {
            HttpRequest request = m_HttpApplication.Request;
            HttpCookieCollection cookies = request.Cookies;

            LogCookies("SwapReferenceWithSessionSecurityTokenCookie cookies pre-replacement", cookies);

            HttpCookie referenceCookie = GetReferenceCookie(cookies);
            if (referenceCookie == null)
            {
                Log.Debug("Null reference cookie");
                return;
            }
            var referenceCookieValue = referenceCookie.Value;
            if (string.IsNullOrEmpty(referenceCookieValue) || !GuidExtensions.IsGuid(referenceCookieValue))
            {
                Log.DebugFormat("Non-guid cookie value '{0}'", referenceCookieValue);
                return;
            }
            var key = new Guid(referenceCookieValue);
            TokenCookie tokenCookie = m_SessionSessionSecurityTokenCookieStore.RetrieveTokenCookie(key);
            if (tokenCookie == null)
            {
                Log.Debug("Null token cookie");
                return;
            }
            
            cookies.Remove(SessionCookieReferenceKeyName);
            foreach (HttpCookie cookie in tokenCookie.GetCookies())
            {
                cookies.Add(cookie);
            }

            LogCookies("SwapReferenceWithSessionSecurityTokenCookie cookies post-replacement", cookies);
        }

        private static void LogCookies(string message, HttpCookieCollection cookieCollection)
        {
            if (!Log.IsDebugEnabled)
            {
                return;
            }
            var cookies = cookieCollection.AllKeys.Select(key => cookieCollection[key]);
            Log.Debug(message);
            Log.DebugFormat("{0} cookies found", cookies.Count());
            foreach (var cookie in cookies)
            {
                Log.DebugFormat("{0}={1}", cookie.Name, cookie.Value);
            }
        }
    }
}