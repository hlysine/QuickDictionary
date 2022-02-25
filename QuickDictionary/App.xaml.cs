using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using QuickDictionary.Models.WordLists;
using Squirrel;

namespace QuickDictionary;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        try
        {
            using var mgr = UpdateManager.GitHubUpdateManager("https://github.com/Henry-YSLin/QuickDictionary").Result;
            // Note, in most of these scenarios, the app exits after this method
            // completes!
            // ReSharper disable AccessToDisposedClosure
            SquirrelAwareApp.HandleEvents(
                onInitialInstall: _ => mgr.CreateShortcutForThisExe(),
                onAppUpdate: _ => mgr.CreateShortcutForThisExe(),
                onAppUninstall: _ =>
                {
                    mgr.RemoveShortcutForThisExe();
                    mgr.RemoveUninstallerRegistryEntry();
                },
                onFirstRun: () => { });
            // ReSharper restore AccessToDisposedClosure
        }
        catch (Exception)
        {
            // expects an exception from Squirrel if this instance isn't installed
        }

        var settings = new CefSettings();

        settings.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\cache");
        settings.PersistUserPreferences = true;
        settings.PersistSessionCookies = true;
        settings.UserAgent = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.101 Mobile Safari/537.36";

        Cef.Initialize(settings);
    }

    private Mutex appMutex;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        appMutex = new Mutex(true, "QuickDictionary", out var isNewInstance);
        if (!isNewInstance)
        {
            Current.Shutdown();
        }

        setupExceptionHandling();
    }

    private void setupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        DispatcherUnhandledException += (_, e) =>
        {
            LogException(e.Exception, "Application.Current.DispatcherUnhandledException");
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    public static void LogException(Exception exception, string source)
    {
        var message = new StringBuilder();
        message.AppendLine($"[{DateTime.Now:R}] Exception ({source})");
        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            message.AppendLine($" in {assemblyName.Name} v{assemblyName.Version}");
            message.AppendLine(exception.Message);
            message.AppendLine(exception.StackTrace);
            message.AppendLine();
            message.AppendLine();
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\log.txt"), message.ToString());
            if (exception.InnerException != null)
            {
                LogException(exception.InnerException, source + ".InnerException");
            }
        }
        catch (Exception)
        {
            // It is possible for more exceptions to appear if the app is in a very bad state
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        WordListStore.CommitDeletions();
        appMutex.Dispose();
    }
}