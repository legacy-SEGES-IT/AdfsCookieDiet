using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;
using log4net;

namespace VflIt.Samples.AdfsCookieDiet
{
    public class MsisAuthCookieDietModule : IHttpModule, IRequiresSessionState
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const string ConnectionStringName = "MsisAuthCookieDietConnectionString";
        private string m_ConnectionString;

        public void Init(HttpApplication context)
        {
            Log.Debug("Loading connection string");
            var connectionString = WebConfigurationManager.ConnectionStrings[ConnectionStringName];
           
            if (connectionString == null || string.IsNullOrEmpty(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException(string.Format("Missing or invalid connection string '{0}'", ConnectionStringName));
            }
            m_ConnectionString = connectionString.ConnectionString;

            Log.Debug("Registering for events");
            context.BeginRequest += SwapReferenceWithSessionSecurityTokenCookie;
            context.EndRequest += SwapSessionSecurityTokenCookieWithReference;
        }

        private CookieSwapper ConstructInstance(HttpApplication httpApplication)
        {
            return 
                new CookieSwapper(
                    new CachingSessionSecurityTokenCookieStore(
                        httpApplication.Context.Cache,
                        new DatabaseBackedSessionSecurityTokenCookieStore(
                            httpApplication,
                            m_ConnectionString)),
                    httpApplication);
        }

        private void SwapSessionSecurityTokenCookieWithReference(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = (HttpApplication)sender;

                var cookieSwapper = ConstructInstance(httpApplication);
                Log.Debug("Swapping session security token for reference key");
                cookieSwapper.SwapSessionSecurityTokenCookieWithReference();
            }
            catch (Exception ex)
            {
                Log.Fatal("SwapSessionSecurityTokenCookieWithReference terminated", ex);
                throw;
            }
        }

        private void SwapReferenceWithSessionSecurityTokenCookie(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = (HttpApplication)sender;
                var cookieSwapper = ConstructInstance(httpApplication);
                Log.Debug("Swapping reference key for session security token");
                cookieSwapper.SwapReferenceWithSessionSecurityTokenCookie();
            }
            catch (Exception ex)
            {
                Log.Fatal("SwapReferenceWithSessionSecurityTokenCookie terminated",ex);              
                throw;
            }
        }

        public void Dispose()
        {
            
        }
    }
}