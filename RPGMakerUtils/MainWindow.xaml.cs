using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RPGMakerUtils.ViewModels;
using RPGMakerUtils.Resources;

namespace RPGMakerUtils
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(App.StartupDropPath))
            {
                FileIOUtils.HandleDropPath(App.StartupDropPath, App.StartupJsonSupport);
            }
        }


        #region Windows Drag Drop
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(  DataFormats.FileDrop);
                if (!_viewModel.IsRunning && files != null && files.Length > 0)
                {
                    string extension = Path.GetExtension(files[0])?.ToLower();
                    if (extension == ".exe" || extension == ".json" || extension == ".txt")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (!_viewModel.IsRunning && files != null && files.Length > 0)
                {
                    if (File.GetAttributes(files[0]).HasFlag(FileAttributes.Directory))
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }

                    string extension = Path.GetExtension(files[0])?.ToLower();
                    if (extension == ".exe" || extension == ".json")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (!_viewModel.IsRunning && droppedPaths != null && droppedPaths.Length > 0)
                {
                    string path = droppedPaths[0];
                    FileIOUtils.HandleDropPath(path, jsonSupport: true);
                }
            }
            e.Handled = true;
        }
        #endregion
    }
}
