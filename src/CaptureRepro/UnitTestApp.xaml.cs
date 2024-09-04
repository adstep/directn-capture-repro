// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CaptureRepro
{
    using Microsoft.UI.Dispatching;
    using Microsoft.UI.Xaml;
    using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
    using System;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class UnitTestApp : Application
    {
        public static Window Window;

        public static DispatcherQueue DispatcherQueue => Window.DispatcherQueue;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public UnitTestApp()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

            m_window = new UnitTestAppWindow();
            m_window.Activate();

            Window = m_window;

            UITestMethodAttribute.DispatcherQueue = m_window.DispatcherQueue;

            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
        }

        private Window m_window;
    }
}
