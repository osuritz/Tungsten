using System;
using System.Configuration;

namespace CorporateAppStore
{
    public class Branding
    {
        private static Branding _instance = new Branding();
        
        public static Branding CurrentBrand
        {
            get { return _instance; }
        }

        public virtual string CompanyName
        {
            get { return ConfigurationManager.AppSettings["Branding_CompanyName"]; }
        }
    }
}