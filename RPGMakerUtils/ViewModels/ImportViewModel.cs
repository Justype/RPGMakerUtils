using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using RPGMakerUtils.Messages;
using RPGMakerUtils.Models;
using RPGMakerUtils.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RPGMakerUtils.ViewModels
{
    internal class ImportViewModel: CommonViewModel
    {
        public RelayCommand ImportGameCommand => new RelayCommand(ImportGame, CanImport);

        public RelayCommand ImportTranslateJsonCommand => new RelayCommand(ImportTranslateJson, CanImport);

        public string TranslateJsonPath { get; set; } = string.Empty;

        public ImportViewModel()
        {
            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                ImportGameCommand.NotifyCanExecuteChanged();
                ImportTranslateJsonCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
            });
            WeakReferenceMessenger.Default.Register<TranslateJsonUpdatedMessage>(this, (r, m) => TranslateJsonPath = m.Value);
        }

        private void ImportGame()
        {
            var filePath = SelectFile("可执行文件 (*.exe)|*.exe", "请选择游戏 Game.exe");
            if (string.IsNullOrWhiteSpace(filePath))
                return; // Cancelled

            if (Path.GetFileNameWithoutExtension(filePath) != "Game")
                MessageBox.Show("请选择游戏 Game.exe", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            
            FileIOUtils.LoadGamePathAndSendMessage(filePath);
        }

        public bool CanImport() => !IsRunning;

        private void ImportTranslateJson()
        {
            var filePath = SelectFile("JSON 文件 (*.json)|*.json", "请选择翻译文件");
            if (string.IsNullOrWhiteSpace(filePath))
                return; // Cancelled
            FileIOUtils.LoadTranslatePathAndSendMessage(filePath);
        }

        public static string SelectFile(string filter, string title)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }
    }
}
