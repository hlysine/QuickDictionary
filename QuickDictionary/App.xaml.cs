using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ////Add Custom assembly resolver
            //AppDomain.CurrentDomain.AssemblyResolve += Resolver;

            ////Any CefSharp references have to be in another method with NonInlining
            //// attribute so the assembly rolver has time to do it's thing.
            //InitializeCefSharp();
            var settings = new CefSettings();

            settings.CachePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "data\\cache");
            settings.PersistUserPreferences = true;
            settings.PersistSessionCookies = true;

            Cef.Initialize(settings);
        }

        Mutex myMutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool aIsNewInstance = false;
            myMutex = new Mutex(true, "QuickDictionary", out aIsNewInstance);
            if (!aIsNewInstance)
            {
                Current.Shutdown();
            }
        }
    }
}
