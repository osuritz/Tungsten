using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Ionic.Zip;
using System.Xml;
using CorporateAppStore.Helpers.CoreFoundation;
using PList;

namespace CorporateAppStore.Models
{
    public class FileSystemAppProvider : IAppProvider
    {
        public const string DefaultPackageDirectory = "~/apps/";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemAppProvider"/> class.
        /// </summary>
        /// <param name="appDirectory">The app directory.</param>
        public FileSystemAppProvider(string appDirectory)
        {
            if (string.IsNullOrEmpty(appDirectory))
            {
                throw new ArgumentNullException("appDirectory");
            }

            ////this.AppDirectory = Path.Combine(Directory.GetCurrentDirectory(), appDirectory);
            this.AppDirectory = System.Web.HttpContext.Current.Server.MapPath(appDirectory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemAppProvider"/> class.
        /// </summary>
        public FileSystemAppProvider()
            : this(DefaultPackageDirectory)
        {
        }

        /// <summary>
        /// Gets the package directory.
        /// </summary>
        public string AppDirectory { get; private set; }
        
        public AppCollection GetAllApps()
        {
            AppCollection apps = new AppCollection();

            string[] allApps = Directory.GetFiles(this.AppDirectory, "*.ipa");

            foreach (var appPath in allApps)
            {
                App app = LoadAppInfo(appPath);
                apps.Add(app);
            }

            return apps;
        }

        private App LoadAppInfo(string appPath)
        {
            var app = new App();

            string appDir;
            if (this.AppDirectory.EndsWith("/") || this.AppDirectory.EndsWith("\\"))
            {
                appDir = this.AppDirectory;
            }
            else
            {
                appDir = this.AppDirectory + "\\";
            }

            Uri localAppBase = new Uri(appDir);
            Uri appUri = new Uri(appPath);
            Uri appRelativePath = localAppBase.MakeRelativeUri(appUri);
            
            app.Filename = Path.GetFileName(appPath);
            app.ManifestFilename = Path.ChangeExtension(app.Filename, App.ManifestFileExtension);

            app.Filepath = Path.Combine(DefaultPackageDirectory, appRelativePath.OriginalString);
            app.ManifestFilepath = Path.ChangeExtension(app.Filepath, App.ManifestFileExtension);
            
            // TODO: Fix this to show the actual name
            app.Name = Path.GetFileNameWithoutExtension(appPath);

            LoadAppMetaData(appPath, app);

            return app;
        }

        private static void LoadAppMetaData(string appPath, App app)
        {
            using (ZipFile zip = ZipFile.Read(appPath))
            {
                //ZipEntry appRootFolder = zip.Entries.FirstOrDefault(x => x.IsDirectory && x.FileName.EndsWith(".app"));

                ZipEntry payload = zip["Payload/"];
                ZipEntry appRootFolder = zip.Entries.Skip(1).FirstOrDefault();

                // Read Info.plist
                ZipEntry appInfo = zip[appRootFolder.FileName + "Info.plist"];

                //string infoXml;
                ////CFPropertyList infoPlist;
                PList.PListRoot infoPlist;
                using (MemoryStream ms = new MemoryStream())
                {
                    appInfo.Extract(ms);

                    // Rewind memory stream to the beginning.
                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(ms))
                    {
                        LoadMetaDataFromPlist(app, PList.PListRoot.Load(ms));                        
                        //infoPlist = new CFPropertyList(data: reader.ReadToEnd());
                    }
                }

                //ZipEntry appRootFolder = zip.Entries.FirstOrDefault(x => x.IsDirectory);
            }
        }

        private static void LoadMetaDataFromPlist(App app, PListRoot infoPlist)
        {
            var root = infoPlist.Root as PListDict;


            app.Name = root.Read("CFBundleDisplayName");
            app.ShortVersion = root.Read("CFBundleShortVersionString");
            app.Version = root.Read("CFBundleVersion");
        }        
    }

    public static class PListExtensions
    {
        public static TResult Read<TResult>(this PListDict element, string key) where TResult : class, IPListElement
        {
            return element[key] as TResult;
        }

        public static string Read(this PListDict element, string key)
        {
            return Read<PListString>(element, key).Value;
        }
    }
}