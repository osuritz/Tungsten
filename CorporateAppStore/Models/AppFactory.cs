using System;
using System.IO;

namespace CorporateAppStore.Models
{
    public class AppFactory
    {
        public App Create(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            string extension = Path.GetExtension(filepath);

            App app;

            switch (extension.ToLowerInvariant())
            {
                case ".ipa":
                case ".plist":
                    app = new IOSApp();
                    app.ManifestFilename = Path.ChangeExtension(filename, IOSApp.ManifestFileExtension);

                    break;
                default:
                    app = new App();
                    break;
            }

            app.Filename = filename;


            return app;
        }
    }
}