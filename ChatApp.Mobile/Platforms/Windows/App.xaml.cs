using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatApp.Mobile.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.UnhandledException += (sender, e) =>
		{
			try
			{
				System.IO.File.AppendAllText(@"c:\Users\Abdullrhman Elhelw\Downloads\Chat\crash_log.txt", 
					$"[WinUI UnhandledException] {DateTime.UtcNow:O}\nMessage: {e.Message}\nException: {e.Exception}\nStackTrace: {e.Exception?.StackTrace}\nInner: {e.Exception?.InnerException}\n-----------------------------------\n");
			}
			catch { }
		};

		AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
		{
			try
			{
				System.IO.File.AppendAllText(@"c:\Users\Abdullrhman Elhelw\Downloads\Chat\crash_log.txt", 
					$"[AppDomain UnhandledException] {DateTime.UtcNow:O}\nExceptionObject: {e.ExceptionObject}\n-----------------------------------\n");
			}
			catch { }
		};

		TaskScheduler.UnobservedTaskException += (sender, e) =>
		{
			try
			{
				System.IO.File.AppendAllText(@"c:\Users\Abdullrhman Elhelw\Downloads\Chat\crash_log.txt", 
					$"[TaskScheduler UnobservedTaskException] {DateTime.UtcNow:O}\nException: {e.Exception}\n-----------------------------------\n");
			}
			catch { }
		};

		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

