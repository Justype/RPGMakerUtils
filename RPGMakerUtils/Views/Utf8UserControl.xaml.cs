using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Ude;


namespace RPGMakerUtils.Views
{
    /// <summary>
    /// Interaction logic for Utf8UserControl.xaml
    /// </summary>
    public partial class Utf8UserControl : UserControl
    {
        public Utf8UserControl()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string outputPath = PathTextBlock.Text;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show("请拖入需要修改的文件！");
                return;
            }
            string text = InputTextBox.Text;
            // save as UTF-8
            if (!File.Exists(outputPath))
            {
                MessageBox.Show($"{outputPath} 不存在！", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(RestoreEncodingFileNameTextBlock.Text))
                File.WriteAllText(outputPath, text, Encoding.UTF8);
            else
            {
                string outputFolder = Path.GetDirectoryName(outputPath);
                string outputFileName = RestoreEncodingFileNameTextBlock.Text;
                string outputNewNamePath = Path.Combine(outputFolder, outputFileName);
                if (!File.Exists(outputPath))
                {
                    MessageBox.Show($"{outputPath} 不存在！", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (File.Exists(outputNewNamePath))
                {
                    MessageBox.Show(outputNewNamePath + " 已存在！", "文件已存在", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                File.Delete(outputPath);
                File.WriteAllText(outputNewNamePath, text, Encoding.UTF8);

                PathTextBlock.Text = outputNewNamePath;
            }
        }

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Make sure only one txt file is dropped
            if (files.Length == 1 &&
                Path.GetExtension(files[0]).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    string inputFilePath = files[0];
                    PathTextBlock.Text = inputFilePath;
                    // Auto detect encoding?
                    using (FileStream stream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var detector = new CharsetDetector();
                        detector.Feed(stream);
                        detector.DataEnd();
                        //Encoding detectedEncoding = Encoding.GetEncoding("shift-jis");
                        Encoding detectedEncoding = Encoding.UTF8;

                        if (detector.Charset != null)
                        {
                            // Using the detected encoding
                            detectedEncoding = Encoding.GetEncoding(detector.Charset);
                            EncodingTextBlock.Text = detectedEncoding.EncodingName;
                        }
                        else
                        {
                            EncodingTextBlock.Text = "Fallback to UTF8";
                        }
                        var content = File.ReadAllText(inputFilePath, detectedEncoding);
                        InputTextBox.Text = content;
                        RestoreEncodingFileNameTextBlock.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                PathTextBlock.Text = $"ERROR\n{ex.Message}";
            }
        }

        private void InputTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void RestoreEncodingButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PathTextBlock.Text))
            {
                MessageBox.Show("请拖入需要修改的文件！");
                return;
            }

            Encoding wrongEncoding;

            try
            {
                wrongEncoding = Encoding.GetEncoding(EncodingTextBox.Text);
            }
            catch
            {
                MessageBox.Show("编码错误！", "编码错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
                //wrongEncoding = Encoding.GetEncoding("GBK");
            }

            string fileName = Path.GetFileName(PathTextBlock.Text);
            byte[] bytes = wrongEncoding.GetBytes(fileName); // GBK
            string correctName = Encoding.GetEncoding("SHIFT-JIS").GetString(bytes);

            RestoreEncodingFileNameTextBlock.Text = correctName;
        }
    }
}
