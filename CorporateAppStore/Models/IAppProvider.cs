namespace CorporateAppStore.Models
{
    public interface IAppProvider
    {
        /// <summary>
        /// Gets all the apps.
        /// </summary>
        /// <returns></returns>
        AppCollection GetAllApps();

        /// <summary>
        /// Gets the app by filename.
        /// </summary>
        /// <param name="filename">The file name of the app to retrieve.</param>
        /// <returns></returns>
        App GetAppByFilename(string filename);
    }
}