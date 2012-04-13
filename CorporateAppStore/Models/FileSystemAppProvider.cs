using System;
using System.Linq;
using System.IO;
using Ionic.Zip;
using PList;
using System.Collections.Generic;

namespace CorporateAppStore.Models
{
    /// <summary>
    /// A <see cref="IAppProvider"/> that looks for apps on the file system.
    /// </summary>
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
            string[] allApps = Directory.GetFiles(this.AppDirectory, "*.ipa");
            var apps = new AppCollection(allApps.Select(LoadAppInfo).ToList());
            return apps;
        }

        private App LoadAppInfo(string appPath)
        {
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


            AppFactory factory = new AppFactory();
            var app = factory.Create(appPath);

            app.Filepath = Path.Combine(DefaultPackageDirectory, appRelativePath.OriginalString);

            if (app is IOSApp)
            {
                app.ManifestFilepath = Path.ChangeExtension(app.Filepath, IOSApp.ManifestFileExtension);
                LoadAppMetaData(appPath, app);
            }                        

            return app;
        }

        private static void LoadAppMetaData(string appPath, App app)
        {
            using (ZipFile zip = ZipFile.Read(appPath))
            {
                ZipEntry appRootFolder = zip.Entries.Skip(1).FirstOrDefault();
                string appRootFolderName = appRootFolder.FileName;

                if (appRootFolder == null)
                {
                    throw new InvalidOperationException("Expected .ipa file to contain an app folder under Payload/");
                }

                // Read Info.plist
                ZipEntry appInfo = zip[appRootFolderName + "Info.plist"];

                using (var memoryStream = new MemoryStream())
                {
                    appInfo.Extract(memoryStream);

                    // Rewind memory stream to the beginning.
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(memoryStream))
                    {
                        LoadMetaDataFromPlist(app, PList.PListRoot.Load(memoryStream));                        
                    }
                }

                // Load icon data files from zip file based filenames listed in plist
                List<AppIcon> iconList = new List<AppIcon>();
                if (app.IconFiles != null)
                {
                    foreach (string iconFilename in app.IconFiles)
                    {
                        ZipEntry zippedIcon = zip[appRootFolderName + iconFilename];
                        using (var memoryStream = new MemoryStream())
                        {
                            zippedIcon.Extract(memoryStream);

                            // Rewind memory stream to the beginning.
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            iconList.Add(new AppIcon
                            {
                                Filename = iconFilename,
                                Data = memoryStream.ToArray()
                            });
                        }
                    }
                }
                app.Icons = iconList.ToArray();
            }
        }

        private static void LoadMetaDataFromPlist(App app, PListRoot infoPlist)
        {
            var root = infoPlist.Root as PListDict;

            app.Name = root.Read("CFBundleDisplayName");
            app.Version = root.Read("CFBundleVersion");
            app.ShortVersion = root.ContainsKey("CFBundleShortVersionString") ? root.Read("CFBundleShortVersionString") : app.Version;
            
            PListArray arr = root.ReadSafe<PListArray>("CFBundleIconFiles");
            if (arr == null)
            {
                app.IconFiles = new[] { "Icon.png" };
            }
            else
            {
                List<string> icons = new List<string>();
                foreach (IPListElement item in arr)
                {
                    // Assume item is a string or discard.
                    PListString str = item as PListString;

                    if (str != null)
                    {
                        icons.Add(str.Value);
                    }
                }

                app.IconFiles = icons.ToArray();
            }
        }



        public App GetAppByFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) {
                throw new ArgumentNullException("filename", "Filename cannot be null or empty.");
            }
            
            App app = this.GetAllApps().FirstOrDefault(x => string.Equals(filename, x.Filename, StringComparison.OrdinalIgnoreCase));
            return app;
        }
    }

    public static class PListExtensions
    {
        public static TResult Read<TResult>(this PListDict element, string key) where TResult : class, IPListElement
        {
            return element[key] as TResult;
        }

        public static TResult ReadSafe<TResult>(this PListDict element, string key, TResult defaultValue = null) where TResult : class, IPListElement
        {
            if (element.ContainsKey(key)) {
                return element[key] as TResult;
            }
            
            return defaultValue;
        }

        
        public static string Read(this PListDict element, string key)
        {
            return Read<PListString>(element, key).Value;
        }
    }
}