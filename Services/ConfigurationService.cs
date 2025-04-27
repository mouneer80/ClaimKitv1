using System.Web.Configuration;

namespace ClaimKitv1.Services
{
    public static class ConfigurationService
    {
        public static int HospitalId => int.Parse(WebConfigurationManager.AppSettings["HospitalId"]);

        public static string ClaimKitApiKey => WebConfigurationManager.AppSettings["ClaimKitApiKey"];
    }
}