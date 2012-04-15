using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CorporateAppStore.Models;
using CorporateAppStore.Helpers;
using PNGNormalizer;

namespace CorporateAppStore.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
            : this(new FileSystemAppProvider())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="appProvider">The app provider.</param>
        public HomeController(IAppProvider appProvider)
        {
            this.AppProvider = appProvider;
        }

        public IAppProvider AppProvider { get; private set; }
        
        public ActionResult Index()
        {
            string siteName = "Coporate App Store";            
            ViewBag.Message = string.Format("Welcome to the {0}!", siteName);

            return this.RedirectToAction("Browse");
        }

        public ActionResult Browse()
        {
            AppCollection allApps = this.AppProvider.GetAllApps();
            
            return View(allApps);
        }

        [ActionName("AppIcon")]
        public ActionResult GetAppIcon(string id, string version)
        {
            App app = this.AppProvider.GetAppByFilename(id);

            if (app == null)
            {
                return this.HttpNotFound();
            }

            if (app.Icons == null)
            {
                // TODO: Return a default icon.
                return this.HttpNotFound();
            }

            AppIcon icon = app.Icons.FirstOrDefault();
            if (icon == null)
            {
                // TODO: Return a default icon.
                return this.HttpNotFound();
            }

            byte[] iconBinaryContents;
            string contentType;
            if (ImageHelper.GetImageFormat(icon.Data) == ImageFormat.Png)
            {
                contentType = MimeTypes.ImagePng;
                var png = new PngFile(icon.Data);
                iconBinaryContents = png.Data;
            } else
            {
                iconBinaryContents = null;
                contentType = MimeTypes.GetMimeType(icon.Filename);
            }

            return this.File(iconBinaryContents, contentType);
        }
    }
}
