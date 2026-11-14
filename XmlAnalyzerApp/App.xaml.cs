namespace XmlAnalyzerApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell()) { Title = "Аналіз XML: конференції", MinimumWidth = 900, MinimumHeight = 600 };
	}
}
