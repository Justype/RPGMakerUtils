using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PropertyChanged;
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
    [AddINotifyPropertyChangedInterface]
    internal class EditFontViewModel : CommonViewModel
    {
        public string SystemFontPath = "C:\\Windows\\Fonts";

        private string _font = "msyh.ttc";

        public string Font
        {
            get => _font;
            set
            {
                _font = value;
                OnPropertyChanged(nameof(Font));
                OnPropertyChanged(nameof(FontPath));
                EditFontCommand.NotifyCanExecuteChanged();
            }
        }

        public string FontPath => Path.Combine(SystemFontPath, Font);

        public IAsyncRelayCommand EditFontCommand { get; }

        public IAsyncRelayCommand RestoreCommand { get; }

        public string GameFontsDirPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        return Path.Combine(GamePath, "www", "Fonts");
                    case RPGMakerVersion.MZ:
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameFontCssPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        return Path.Combine(GameFontsDirPath, "gamefont.css");
                    case RPGMakerVersion.MZ:
                    default:
                        return string.Empty;
                }
            }
        }

        public string GameFontCssBackupPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePath))
                    return string.Empty;
                switch (GameVersion)
                {
                    case RPGMakerVersion.MV:
                        return GameFontCssPath + ".zip";
                    case RPGMakerVersion.MZ:
                    default:
                        return string.Empty;
                }
            }
        }

        public EditFontViewModel()
        {
            EditFontCommand = new AsyncRelayCommand(EditFontAsync, CanEditFont);
            RestoreCommand = new AsyncRelayCommand(RestoreAsync, CanRestore);

            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                EditFontCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameFontCssBackupPath));
                OnPropertyChanged(nameof(GameFontsDirPath));
                OnPropertyChanged(nameof(GameFontCssPath));
            });

            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                EditFontCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameFontCssBackupPath));
            });
        }

        private async Task EditFontAsync()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                try 
                {
                    await Utils.CreateZipFromFileAsync(GameFontCssPath, GameFontCssBackupPath);
                    string targetFontPath = Path.Combine(GameFontsDirPath, Font);
                    File.Copy(FontPath, Path.Combine(GameFontsDirPath, Font), true);

                    /* // Edit gamefont.css to use msyh.ttc
                     * // Replace the string in url
                     * @font-face {
                     *     font-family: GameFont;
                     *     src: url("msyh.ttc");
                     * }
                     */

                    string fontUrlPattern = @"(@font-face\s*\{[^}]*?src:\s*url\("")[^""]+(""\))";
                    string replacement = $"$1{Font}$2";

                    string fontCssContent = File.ReadAllText(GameFontCssPath);
                    string newFontCssContent = System.Text.RegularExpressions.Regex.Replace(fontCssContent, fontUrlPattern, replacement, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    File.WriteAllText(GameFontCssPath, newFontCssContent);
                    MessageBox.Show("字体修改成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法创建字体目录，可能路径不正确或没有权限。\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        public bool CanEditFont() =>
            File.Exists(FontPath) &&
                File.Exists(GameFontCssPath) &&
                    GameVersion == RPGMakerVersion.MV &&
                        !File.Exists(GameFontCssBackupPath) &&
                            !IsRunning;

        private async Task RestoreAsync()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                bool isSuccess = false;

                if (File.Exists(GameFontCssBackupPath))
                {
                    isSuccess = await Utils.ExtractZipAsync(GameFontCssBackupPath, Path.GetDirectoryName(GameFontCssPath), overwrite: true);
                    File.Delete(GameFontCssBackupPath);
                }

                if (isSuccess)
                {
                    MessageBox.Show("字体还原成功，请自己删除不用的字体", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("字体还原失败", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        private bool CanRestore() =>
            File.Exists(GameFontCssBackupPath) &&
                !IsRunning;
    }
}
