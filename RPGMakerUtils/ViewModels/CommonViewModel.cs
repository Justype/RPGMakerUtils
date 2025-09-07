using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using PropertyChanged;
using RPGMakerUtils.Messages;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RPGMakerUtils.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class CommonViewModel : ObservableObject
    {
        public IRelayCommand HyperlinkCommand { get; }

        public CommonViewModel()
        {
            HyperlinkCommand = new RelayCommand<string>(OpenLink);
        }

        public string GamePath { get; set; } = string.Empty;

        public string GameDataPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return Path.Combine(GameWwwPath, "data");
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameJsPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        return Path.Combine(GamePath, "www", "js");
                    case RPGMakerVersion.MZ:
                        return Path.Combine(GamePath, "js");
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameWwwPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        return Path.Combine(GamePath, "www");
                    case RPGMakerVersion.MZ:
                        return GamePath;
                    default:
                        return string.Empty;
                }
            }
        }

        public ObservableCollection<GameDataFile> GameDataFiles { get; set; } = new ObservableCollection<GameDataFile>();

        public RPGMakerVersion GameVersion { get; set; } = RPGMakerVersion.Unknown;

        public bool IsRunning { get; set; } = false;

        public void UpdateGameDataFiles()
        {
            if (string.IsNullOrEmpty(GameDataPath))
                return;

            GameDataFiles.Clear();
            if (Directory.Exists(GameDataPath))
            {
                var files = Directory.GetFiles(GameDataPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    GameDataFiles.Add(new GameDataFile
                    {
                        FilePath = file,
                        IsDone = false
                    });
                }
            }
        }

        private void OpenLink(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            try
            {
                var result = MessageBox.Show(
                    $"你确定要打开此链接吗?\n\n{url}",
                    "打开链接",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"无法打开链接: {url}");
            }
        }
    }
}
