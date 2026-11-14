using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using XmlAnalyzer;
using XmlAnalyzer.Search;
using XmlAnalyzer.Services;

namespace XmlAnalyzerApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureLifecycleEvents(events =>
			{
#if WINDOWS
				events.AddWindows(w => w.OnWindowCreated(window =>
				{
					window.AppWindow.Closing += async (_, e) =>
					{
						if (MainPage.ExitConfirmed) return;
						e.Cancel = true;
						if (Application.Current?.Windows.FirstOrDefault()?.Page is Shell { CurrentPage: MainPage page })
							await page.RequestExitAsync();
					};
				}));
#endif
			});

		builder.Services.AddSingleton<IXmlSearchStrategy, SaxSearchStrategy>();
		builder.Services.AddSingleton<IXmlSearchStrategy, DomSearchStrategy>();
		builder.Services.AddSingleton<IXmlSearchStrategy, LinqSearchStrategy>();
		builder.Services.AddSingleton<XmlSearchContext>();
		builder.Services.AddSingleton<AttributeCatalog>();
		builder.Services.AddSingleton<ConferenceXmlWriter>();
		builder.Services.AddSingleton<HtmlTransformer>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
