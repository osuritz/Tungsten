using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.ObjectModel;
using System.Web.Mvc;

namespace CorporateAppStore.Models
{
    public class App
    {
        public const string ManifestFileExtension = @".plist";
        public const string AppFileExtension = @".ipa";
        
        public string Filename { get; set; }
        public string Filepath { get; set; }
        public string Name { get; set; }
        public string ShortVersion { get; set; }
        public string Version { get; set; }
        public string ManifestFilename { get; set; }
        public string ManifestFilepath { get; set; }

        public string GetDownloadManifestUri(UrlHelper helper)
        {
            string absolutePath = helper.Content(this.ManifestFilepath);
            string fullUrl = string.Format(@"itms-services://?action=download-manifest&url={0}", absolutePath);
            return fullUrl;
        }
        
    }

    public class AppCollection : Collection<App>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppCollection"/> class.
        /// </summary>
        public AppCollection()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppCollection"/> class.
        /// </summary>
        /// <param name="list">The list.</param>
        public AppCollection(IList<App> list)
            : base(list)
        {
        }
    }
}