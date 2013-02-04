AdfsCookieDiet
==============

A HttpModule to reduce the size of the ADFS MSISAuth cookie to fit within the iOS/Safari limit (by offloading the content to a database)

Things to look out for:
- this code probably puts your ADFS in an unsupported state
- this code has known interop problems with POST-based SAML RPs (GET-based SAML RPs and WS-Federation-based RPs work)
