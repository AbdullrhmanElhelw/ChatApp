using Microsoft.Maui.ApplicationModel;

namespace ChatApp.Mobile.Navigation;

public class NavigationService : INavigationService
{
    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (MainThread.IsMainThread)
        {
            return ExecuteGoToAsync(route, parameters);
        }

        return MainThread.InvokeOnMainThreadAsync(() => ExecuteGoToAsync(route, parameters));
    }

    private Task ExecuteGoToAsync(string route, IDictionary<string, object>? parameters)
    {
        if (Shell.Current == null) return Task.CompletedTask;
        return parameters != null
            ? Shell.Current.GoToAsync(route, parameters)
            : Shell.Current.GoToAsync(route);
    }

    public Task GoBackAsync()
    {
        if (MainThread.IsMainThread)
        {
            return ExecuteGoBackAsync();
        }

        return MainThread.InvokeOnMainThreadAsync(ExecuteGoBackAsync);
    }

    private Task ExecuteGoBackAsync()
    {
        if (Shell.Current == null) return Task.CompletedTask;
        return Shell.Current.GoToAsync("..");
    }
}
