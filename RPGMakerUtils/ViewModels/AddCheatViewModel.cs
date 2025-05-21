using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

namespace RPGMakerUtils.ViewModels
{
    internal class AddCheatViewModel : CommonViewModel
    {
        public IAsyncRelayCommand AddCheatCommand { get; }

        public AddCheatViewModel()
        {
            AddCheatCommand = new AsyncRelayCommand(AddCheat, CanAddCheat);

            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                AddCheatCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameJsPath));
                OnPropertyChanged(nameof(GameMainJsPath));
                OnPropertyChanged(nameof(GameMainJsBackupPath));
            });
            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                AddCheatCommand.NotifyCanExecuteChanged();
            });

        }

        private async Task AddCheat()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () => {
                await Utils.CreateZipFromFileAsync(GameMainJsPath, GameMainJsBackupPath);
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        if (await Utils.ExtractEmbeddedZipAsync("RPGMakerUtils.Resources.rpg_mv_cheat.zip", GameWwwPath))
                            MessageBox.Show("添加作弊成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        else
                            MessageBox.Show("添加作弊失败", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    case RPGMakerVersion.MZ:
                        if (await Utils.ExtractEmbeddedZipAsync("RPGMakerUtils.Resources.rpg_mz_cheat.zip", GameWwwPath))
                            MessageBox.Show("添加作弊成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        else
                            MessageBox.Show("添加作弊失败", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    default:
                        break;
                }
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }


        public bool CanAddCheat() =>
            Directory.Exists(GameJsPath) &&
                !File.Exists(GameMainJsBackupPath) &&
                    !IsRunning;

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

        public string GameMainJsPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return Path.Combine(GameJsPath, "main.js");
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameMainJsBackupPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                    case RPGMakerVersion.MZ:
                        return GameMainJsPath + ".zip";
                    default:
                        return string.Empty;
                }
            }
        }

    }
}
