using System;

namespace VflIt.Samples.AdfsCookieDiet
{
    static class GuidExtensions
    {
        public static bool IsGuid(string possibleGuid)
        {
            try
            {
                var g = new Guid(possibleGuid);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
