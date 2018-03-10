using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace qqEd
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        [STAThread]
        public static void Main(string[] args)
        {
            qqEd.App app = new qqEd.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
