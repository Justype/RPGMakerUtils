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

        public EditFontViewModel()
        {
            EditFontCommand = new AsyncRelayCommand(EditFont, CanEditFont);

            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                EditFontCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(GameFontCssBackupPath));
                OnPropertyChanged(nameof(GameFontsDirPath));
                OnPropertyChanged(nameof(GameFontCssPath));
            });

            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                EditFontCommand.NotifyCanExecuteChanged();
            });
        }

        private async Task EditFont()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () => {
                await Utils.CreateZipFromFileAsync(GameFontCssPath, GameFontCssBackupPath);
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
            });

            MessageBox.Show("字体修改成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

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

        public bool CanEditFont() =>
            File.Exists(FontPath) &&
                File.Exists(GameFontCssPath) &&
                    GameVersion == RPGMakerVersion.MV &&
                        !File.Exists(GameFontCssBackupPath) &&
                            !IsRunning;

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

    }
}
