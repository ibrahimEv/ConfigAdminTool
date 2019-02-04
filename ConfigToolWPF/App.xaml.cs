using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ConfigToolWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var result = MessageBox.Show("Error occurred: " + e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            if (result == MessageBoxResult.OK)
            {
                MainWindow temp = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                temp.LoaderGrid.Visibility = Visibility.Hidden;
            }
            
        }
    }
}
