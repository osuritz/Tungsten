using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CorporateAppStore.Models;

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

            return View();
        }

        public ActionResult Browse()
        {
            AppCollection allApps = this.AppProvider.GetAllApps();
            
            return View(allApps);
        }        
    }
}
