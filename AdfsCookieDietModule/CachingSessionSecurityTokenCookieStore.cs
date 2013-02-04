using System;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using log4net;

namespace VflIt.Samples.AdfsCookieDiet
{
    internal class CachingSessionSecurityTokenCookieStore : AbstractSessionSecurityTokenCookieStore
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Ideally, keep this value in sync with ADFS "Federation Service Properties" -> "Web SSO lifetime"
        private readonly TimeSpan m_WebSsoLifeTime = TimeSpan.FromMinutes(480);
        private readonly Cache m_Cache;
        private readonly AbstractSessionSecurityTokenCookieStore m_InnerStore;

        public CachingSessionSecurityTokenCookieStore(Cache cache, AbstractSessionSecurityTokenCookieStore innerStore) : base(null)
        {
            if (cache == null)
            {
                throw new ArgumentNullException("cache");
            }
            m_Cache = cache;
            m_InnerStore = innerStore;
        }

        public override Guid StoreTokenCookie(TokenCookie cookie)
        {
            Log.Debug("Using inner store to persist cookie");
            var key = m_InnerStore.StoreTokenCookie(cookie);
            Log.DebugFormat("Cache key: {0}", key);
            CacheCookie(key, cookie);
            return key;
        }

        private void CacheCookie(Guid key, TokenCookie cookie)
        {
            var expirationDate = DateTime.Now.Add(m_WebSsoLifeTime);
            Log.DebugFormat("Caching cookie (expiration {0})", expirationDate);
            m_Cache.Insert(key.ToString(), cookie, null, expirationDate, Cache.NoSlidingExpiration);
        }

        public override TokenCookie RetrieveTokenCookie(Guid sessionSecurityTokenKey)
        {
            Log.DebugFormat("Looking for key in cache: {0}", sessionSecurityTokenKey);
            var cachedCookie = m_Cache.Get(sessionSecurityTokenKey.ToString()) as TokenCookie;
            if (cachedCookie != null)
            {
                Log.Debug("Cache hit; returning in-memory cached token");
                return cachedCookie;
            }
            Log.Debug("Cache miss; delegating to inner store");
            var cookie = m_InnerStore.RetrieveTokenCookie(sessionSecurityTokenKey);
            CacheCookie(sessionSecurityTokenKey, cookie);
            return cookie;
        }
    }
}