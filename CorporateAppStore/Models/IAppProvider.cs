namespace CorporateAppStore.Models
{
    public interface IAppProvider
    {
        /// <summary>
        /// Gets all the apps.
        /// </summary>
        /// <returns></returns>
        AppCollection GetAllApps();
    }
}