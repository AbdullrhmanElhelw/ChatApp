using Microsoft.Maui.ApplicationModel;

namespace ChatApp.Mobile.Navigation;

public class NavigationService : INavigationService
{
    private bool _isNavigating;

    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (MainThread.IsMainThread)
        {
            return ExecuteGoToAsync(route, parameters);
        }

        return MainThread.InvokeOnMainThreadAsync(() => ExecuteGoToAsync(route, parameters));
    }

    private async Task ExecuteGoToAsync(string route, IDictionary<string, object>? parameters)
    {
        if (Shell.Current == null || _isNavigating) return;
        _isNavigating = true;

        try
        {
            if (parameters != null)
            {
                await Shell.Current.GoToAsync(route, animate: false, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route, animate: false);
            }
        }
        catch
        {
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                Page? page = route switch
                {
                    AppRoutes.StickerStudio or "sticker-studio" or "chat/sticker-studio" =>
                        services.GetService<Features.Stickers.Pages.StickerStudioPage>(),
                    AppRoutes.UserSearch or "user-search" =>
                        services.GetService<Features.Conversations.Pages.UserSearchPage>(),
                    _ => null
                };

                if (page != null && Shell.Current?.Navigation != null)
                {
                    await Shell.Current.Navigation.PushAsync(page, animated: false);
                    return;
                }
            }
            throw;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public Task GoBackAsync()
    {
        if (MainThread.IsMainThread)
        {
            return ExecuteGoBackAsync();
        }

        return MainThread.InvokeOnMainThreadAsync(ExecuteGoBackAsync);
    }

    private async Task ExecuteGoBackAsync()
    {
        if (Shell.Current == null) return;

        try
        {
            if (Shell.Current.Navigation != null)
            {
                if (Shell.Current.Navigation.ModalStack.Count > 0)
                {
                    await Shell.Current.Navigation.PopModalAsync(animated: false);
                    return;
                }

                if (Shell.Current.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.Navigation.PopAsync(animated: false);
                    return;
                }
            }

            await Shell.Current.GoToAsync("..", animate: false);
        }
        catch
        {
            if (Shell.Current.Navigation?.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: false);
            }
            else
            {
                await Shell.Current.GoToAsync("..", animate: false);
            }
        }
    }
}
