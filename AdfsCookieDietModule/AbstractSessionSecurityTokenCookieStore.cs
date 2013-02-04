using System;
using System.Web;

namespace VflIt.Samples.AdfsCookieDiet
{
    internal abstract class AbstractSessionSecurityTokenCookieStore
    {
        protected readonly HttpApplication m_CurrentApplication;

        protected AbstractSessionSecurityTokenCookieStore(HttpApplication currentApplication)
        {
            m_CurrentApplication = currentApplication;
        }

        public abstract Guid StoreTokenCookie(TokenCookie cookie);
        public abstract TokenCookie RetrieveTokenCookie(Guid sessionSecurityTokenKey);
    }
}