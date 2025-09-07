using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RPGMakerUtils.Resources;

namespace RPGMakerUtils
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string StartupDropPath { get; set; }
        public static bool StartupJsonSupport { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args != null && e.Args.Length > 0)
            {
                StartupDropPath = e.Args[0];
                StartupJsonSupport = false;
            }
        }
    }
}
