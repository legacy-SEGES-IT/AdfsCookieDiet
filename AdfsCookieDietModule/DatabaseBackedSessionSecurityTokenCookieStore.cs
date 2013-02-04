using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Transactions;
using System.Web;
using System.Xml;
using log4net;

namespace VflIt.Samples.AdfsCookieDiet
{
    internal class DatabaseBackedSessionSecurityTokenCookieStore : AbstractSessionSecurityTokenCookieStore
    {
        private const int ValueLimit = 7999;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string m_ConnectionString;

        public DatabaseBackedSessionSecurityTokenCookieStore(HttpApplication currentApplication, string connectionString)
            : base(currentApplication)
        {
            m_ConnectionString = connectionString;
        }

        public override Guid StoreTokenCookie(TokenCookie cookie)
        {
            Log.Debug("Storing token cookie");
            var key = cookie.GetKey();
            Log.DebugFormat("Storage key is '{0}'", key);
            string serializedCookie = Serialize(cookie);
            if (serializedCookie.Length > ValueLimit)
            {
                throw new InvalidOperationException(
                    string.Format("Value size {0} is greater than the DB allowed limit of {1}", serializedCookie.Length,
                                  ValueLimit));
            }
            Log.DebugFormat("Token cookie serialized ({0} chars)", serializedCookie.Length);
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                using (var connection = new SqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("DELETE FROM CookieCache WHERE ([key] = @key)", connection))
                    {
                        var keyParam = new SqlParameter("key", SqlDbType.UniqueIdentifier) {Value = key};
                        command.Parameters.Add(keyParam);
                        command.ExecuteNonQuery();
                    }
                    using (
                        var command = new SqlCommand("INSERT INTO CookieCache ([key],value) VALUES (@key,@value)",
                                                     connection))
                    {
                        var keyParam = new SqlParameter("key", SqlDbType.UniqueIdentifier) {Value = key};
                        command.Parameters.Add(keyParam);
                        var valueParam = new SqlParameter("value", SqlDbType.VarChar, ValueLimit)
                                             {Value = serializedCookie};
                        command.Parameters.Add(valueParam);
                        command.ExecuteNonQuery();
                    }
                }
                scope.Complete();
            }
            Log.Debug("Token cookie persisted)");
            return key;
        }

        private string Serialize(TokenCookie cookie)
        {
            var serializer = new DataContractSerializer(typeof (TokenCookie), new[] {typeof (SerializableCookie)});
            var buffer = new StringBuilder();
            using (var sw = new StringWriter(buffer))
            {
                using (var xtw = new XmlTextWriter(sw))
                {
                    serializer.WriteObject(xtw, cookie);
                    xtw.Flush();
                }
                sw.Flush();
            }
            return buffer.ToString();
        }

        public override TokenCookie RetrieveTokenCookie(Guid sessionSecurityTokenKey)
        {
            Log.DebugFormat("Retrieving token cookie with key {0}", sessionSecurityTokenKey);

            string serializedToken;
            using (var connection = new SqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT value FROM CookieCache WHERE ([key] = @key)", connection))
                {
                    var keyParam = new SqlParameter("key", SqlDbType.UniqueIdentifier) { Value = sessionSecurityTokenKey };
                    command.Parameters.Add(keyParam);
                    serializedToken = command.ExecuteScalar() as string;
                }
            }

            if (string.IsNullOrEmpty(serializedToken))
            {
                Log.Debug("Deserialized null or empty token");
                return null;
            }
            try
            {
                return Deserialize(serializedToken);
            }
            catch (Exception ex)
            {
                Log.Warn(string.Format("Unable to deserialize token from string: {0}", serializedToken), ex);
                return null;
            }
        }

        private TokenCookie Deserialize(string serializedToken)
        {
            var serializer = new DataContractSerializer(typeof (TokenCookie), new[] {typeof (SerializableCookie)});
            using (var sr = new StringReader(serializedToken))
            {
                using (var xtr = new XmlTextReader(sr))
                {
                    var tokenCookie = (TokenCookie) serializer.ReadObject(xtr);
                    Log.Debug("Token cookie deserialized ");

                    return tokenCookie;
                }
            }
        }
    }
}