//#define TEST_JS
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using RPGMakerUtils.Messages;
using RPGMakerUtils.Models;
using RPGMakerUtils.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;



namespace RPGMakerUtils.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class TranslateViewModel : CommonViewModel
    {
        public string TranslateJsonPath { get; set; } = string.Empty;

        public IAsyncRelayCommand TranslateCommand { get; }

        public TranslateViewModel()
        {
            TranslateCommand = new AsyncRelayCommand(TranslateAll, CanTranslate);

            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                TranslateCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                OnPropertyChanged(nameof(GameDataPath));
                OnPropertyChanged(nameof(GameWwwPath));
                OnPropertyChanged(nameof(GameDataBackupZipPath));
                UpdateGameDataFiles();
                TranslateCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<TranslateJsonUpdatedMessage>(this, (r, m) => {
                TranslateJsonPath = m.Value;
                TranslateCommand.NotifyCanExecuteChanged();
            });
        }


        public bool CanTranslate() => 
            GameDataFiles.Count() != 0 && 
                File.Exists(TranslateJsonPath) &&
                    !File.Exists(GameDataBackupZipPath) &&
                        !IsRunning;

        public string GamePluginsJsPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return Path.Combine(GameWwwPath, "js", "plugins.js");
                    default:
                        return string.Empty;
                }
            }
        }

        public string GamePluginsJsBackupPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return GamePluginsJsPath + ".zip";
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameDataBackupZipPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return GameDataPath + ".zip";
                    default:
                        return string.Empty;
                }
            }
        }

        public async Task TranslateAll()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                var translator = new RPGMakerMVZTranslator(TranslateJsonPath);

#if TEST_JS
                bool isGameDataTranslated = true;
#else
                await Utils.CreateZipFromFolderAsync(GameDataPath, GameDataBackupZipPath);
                bool isGameDataTranslated = translator.TranslateAllGameData(GameDataFiles);
#endif

                await Utils.CreateZipFromFileAsync(GamePluginsJsPath, GamePluginsJsBackupPath);
                bool isPluginsJsTranslated = translator.TranslatePluginsJs(GamePluginsJsPath);

                if (isGameDataTranslated && isPluginsJsTranslated)
                    MessageBox.Show("翻译成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(
                        isPluginsJsTranslated ? "翻译失败，请检查翻译文件" : "Plugins.js 翻译失败，请检查翻译文件", 
                        "失败", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }
    }
}
