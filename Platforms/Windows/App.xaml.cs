namespace Aquatir.WinUI
{
    // Leave auto-generated code intact

    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Console.WriteLine("Unhandled exception: " + ex);
                }
            };

        }


        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}