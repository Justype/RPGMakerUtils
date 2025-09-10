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

        public IAsyncRelayCommand RestoreCommand { get; }

        public AddCheatViewModel()
        {
            AddCheatCommand = new AsyncRelayCommand(AddCheatAsync, CanAddCheat);
            RestoreCommand = new AsyncRelayCommand(RestoreAsync, CanRestore);

            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                AddCheatCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameJsPath));
                OnPropertyChanged(nameof(GameMainJsPath));
                OnPropertyChanged(nameof(GameMainJsBackupPath));
            });
            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                AddCheatCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameMainJsBackupPath));
            });

        }

        private async Task AddCheatAsync()
        {
            if (!CanAddCheat())
            {
                MessageBox.Show("无法添加作弊，可能是因为游戏目录无效或已经添加过作弊", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
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


        private bool CanAddCheat() =>
            Directory.Exists(GameJsPath) &&
                !File.Exists(GameMainJsBackupPath) &&
                    !IsRunning;

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

        private async Task RestoreAsync()
        {
            if (!File.Exists(GameMainJsBackupPath))
            {
                MessageBox.Show("没有找到备份文件，无法恢复", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                try
                {
                    await Utils.ExtractZipAsync(GameMainJsBackupPath, Path.GetDirectoryName(GameMainJsPath), true);

                    // Also Remove cheat cheat-settings folder and cheat-version-description.json
                    string versionJsonPath = Path.Combine(GameWwwPath, "cheat-version-description.json");
                    string cheatFolderPath = Path.Combine(GameWwwPath, "cheat");
                    string cheatSettingsFolderPath = Path.Combine(GameWwwPath, "cheat-settings");

                    if (GameVersion == RPGMakerVersion.MZ)
                    {
                        // In MZ, www/cheat-settings
                        cheatSettingsFolderPath = Path.Combine(GamePath, "www", "cheat-settings");
                    }

                    if (File.Exists(versionJsonPath))
                        File.Delete(versionJsonPath);
                    if (Directory.Exists(cheatFolderPath))
                        Directory.Delete(cheatFolderPath, true);
                    if (Directory.Exists(cheatSettingsFolderPath))
                        Directory.Delete(cheatSettingsFolderPath, true);
                    File.Delete(GameMainJsBackupPath);

                    MessageBox.Show("恢复成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("恢复失败：" + ex.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        private bool CanRestore() =>
            File.Exists(GameMainJsBackupPath) &&
                !IsRunning;
    }
}
