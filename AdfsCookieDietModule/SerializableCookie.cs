using System;
using System.Runtime.Serialization;
using System.Web;

namespace VflIt.Samples.AdfsCookieDiet
{
    [DataContract]
    internal class SerializableCookie
    {
        public SerializableCookie(HttpCookie cookie)
        {
            Domain = cookie.Domain;
            Expires = cookie.Expires;
            HttpOnly = cookie.HttpOnly;
            Name = cookie.Name;
            Path = cookie.Path;
            Secure = cookie.Secure;
            Value = cookie.Value;
        }

        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public DateTime Expires { get; set; }

        [DataMember]
        public bool HttpOnly { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public bool Secure { get; set; }

        [DataMember]
        public string Value { get; set; }

        public HttpCookie ToCookie()
        {
            var cookie = new HttpCookie(Name)
                             {
                                 Domain = Domain,
                                 Expires = Expires,
                                 HttpOnly = HttpOnly,
                                 Path = Path,
                                 Secure = Secure,
                                 Value = Value
                             };
            return cookie;
        }
    }
}