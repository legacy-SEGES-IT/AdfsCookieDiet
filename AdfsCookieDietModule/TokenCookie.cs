using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace VflIt.Samples.AdfsCookieDiet
{
    [DataContract]
    internal class TokenCookie
    {
        [DataMember] private Dictionary<string, SerializableCookie> m_Cookies;
        [DataMember] private Guid m_Key;

        public TokenCookie(IEnumerable<HttpCookie> msisAuthCookies)
        {
            m_Cookies = new Dictionary<string, SerializableCookie>();
            foreach (HttpCookie cookie in msisAuthCookies)
            {
                m_Cookies.Add(cookie.Name, new SerializableCookie(cookie));
            }
            m_Key = Guid.NewGuid();
        }


        public Guid GetKey()
        {
            return m_Key;
        }

        public IEnumerable<HttpCookie> GetCookies()
        {
            return m_Cookies
                .Select(cookie => cookie.Value)
                .Select(serializedCookie => new HttpCookie(serializedCookie.Name)
                                                {
                                                    Domain = serializedCookie.Domain,
                                                    Expires = serializedCookie.Expires,
                                                    HttpOnly = serializedCookie.HttpOnly,
                                                    Path = serializedCookie.Path,
                                                    Secure = serializedCookie.Secure,
                                                    Value = serializedCookie.Value
                                                });
        }
    }
}