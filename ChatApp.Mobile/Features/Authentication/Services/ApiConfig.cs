namespace ChatApp.Mobile.Features.Authentication.Services;

public static class ApiConfig
{
    public static string BaseUrl
    {
        get
        {
#if ANDROID
            return "http://10.0.2.2:5000";
#else
            return "http://localhost:5000";
#endif
        }
    }
}
