namespace ChatApp.Mobile.Navigation;

public class NavigationService : INavigationService
{
    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        return parameters != null
            ? Shell.Current.GoToAsync(route, parameters)
            : Shell.Current.GoToAsync(route);
    }

    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
