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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace RPGMakerUtils.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal class TranslationPluginViewModel : CommonViewModel
    {
        public string TranslateJsonPath { get; set; } = string.Empty;

        public readonly string PluginLine = "{\"name\":\"JtJsonTranslationManager\",\"status\":true,\"description\":\"Loads translations.json and applies it to game text.\",\"parameters\":{}},";

        public IAsyncRelayCommand AddTranslationPluginCommand { get; }

        public IAsyncRelayCommand RestoreCommand { get; }

        public TranslationPluginViewModel()
        {
            AddTranslationPluginCommand = new AsyncRelayCommand(AddTranslationPluginAsync, CanAddTranslationPlugin);
            RestoreCommand = new AsyncRelayCommand(RestoreAsync, CanRestore);

            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                AddTranslationPluginCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(TranslationPluginPath));
            });
            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                OnPropertyChanged(nameof(GameJsPath));
                OnPropertyChanged(nameof(GamePluginsJsPath));
                OnPropertyChanged(nameof(TranslationPluginPath));
                OnPropertyChanged(nameof(GameWwwPath));
                OnPropertyChanged(nameof(TargetJsonPath));
                AddTranslationPluginCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<TranslateJsonUpdatedMessage>(this, (r, m) =>
            {
                TranslateJsonPath = m.Value;
                OnPropertyChanged(nameof(TranslateJsonPath));
                AddTranslationPluginCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
            });

            // Plugin ComboBox
            SelectedPluginVersion = PluginVersions.BroaderTranslation;
            AvailablePluginVersions = new List<string>
            {
                PluginVersions.BroaderTranslation,
                PluginVersions.FasterTranslation,
                PluginVersions.ComprehensiveTranslation
            };
        }

        public string SelectedPluginVersion { get; set; }

        public IEnumerable<string> AvailablePluginVersions { get; private set; }

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
                        return Path.Combine(GameJsPath, "plugins.js");
                    default:
                        return string.Empty;
                }
            }
        }

        public string TranslationPluginPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GameJsPath))
                    return string.Empty;
                return Path.Combine(GameJsPath, "plugins", "JtJsonTranslationManager.js");
            }
        }

        public string TargetJsonPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GameWwwPath))
                    return string.Empty;
                return Path.Combine(GameWwwPath, "translations.json");
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

        private bool CanAddTranslationPlugin()
        {
            return !IsRunning
                && !string.IsNullOrWhiteSpace(GamePath)
                && (GameVersion == RPGMakerVersion.MV || GameVersion == RPGMakerVersion.MZ)
                && !string.IsNullOrWhiteSpace(TranslateJsonPath)
                && File.Exists(TranslateJsonPath)
                && !string.IsNullOrWhiteSpace(GamePluginsJsPath)
                && File.Exists(GamePluginsJsPath)
                && !File.ReadAllText(GamePluginsJsPath).Contains("JtJsonTranslationManager")
                && !File.Exists(GameDataBackupZipPath); // Make sure it is not translated by TranslateViewModel
        }

        private async Task AddTranslationPluginAsync()
        {
            if (!CanAddTranslationPlugin())
            {
                MessageBox.Show("无法翻译，可能是因为游戏目录无效、已经翻译过或翻译文件无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                // Update plugins.js to include JtJsonTranslationManager.js
                if (File.Exists(GamePluginsJsPath))
                {
                    // Add the line to the first entry
                    List<string> lines = File.ReadAllLines(GamePluginsJsPath)
                                         .Select(line => line.Trim())
                                         .ToList();

                    // Make sure the plugin is not already added
                    if (lines.Any(line => line.Contains("JtJsonTranslationManager")))
                        return; // Already added

                    // Replace the incomplete line with a valid assignment for firstPluginIndex
                    Regex pluginRegex = new Regex(@"^\s*\{\s*""name""\s*:\s*""[^""]+""\s*,\s*""status""\s*:\s*(true|false)\s*,\s*""description""\s*:\s*""[^""]*""\s*,\s*""parameters""\s*:\s*\{[^}]*\}\s*\}\s*,?\s*$");
                    int firstPluginIndex = lines.FindIndex(line => pluginRegex.IsMatch(line));
                    if (firstPluginIndex == -1)
                    {
                        MessageBox.Show("无法找到plugins.js中的插件列表。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    lines.Insert(firstPluginIndex, PluginLine);
                    File.WriteAllText(GamePluginsJsPath, string.Join("\n", lines)); // Use \n to keep the original line endings

                }

                // Copy Resources/JtJsonTranslationManager.js to TranslationPluginPath
                await Utils.CopyEmbeddedFileAsync($"RPGMakerUtils.Resources.{SelectedPluginVersion}", TranslationPluginPath);

                // Copy TranslateJsonPath to GameWwwPath/translations.json using JSON parser
                if (!string.IsNullOrWhiteSpace(TranslateJsonPath) && File.Exists(TranslateJsonPath))
                {
                    var destPath = Path.Combine(GameWwwPath, "translations.json");
                    try
                    {
                        var jsonText = File.ReadAllText(TranslateJsonPath);
                        // Parse and re-serialize to remove comments and ensure valid JSON (JSON should not have comments)
                        var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(jsonText);
                        var cleanedJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(destPath, cleanedJson);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("JSON解析失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                MessageBox.Show("已成功添加翻译插件和翻译文件。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        private bool CanRestore()
        {
            return !IsRunning
                && !string.IsNullOrWhiteSpace(GamePath)
                && (GameVersion == RPGMakerVersion.MV || GameVersion == RPGMakerVersion.MZ)
                && !string.IsNullOrWhiteSpace(GamePluginsJsPath)
                && File.Exists(GamePluginsJsPath)
                && File.ReadAllText(GamePluginsJsPath).Contains("JtJsonTranslationManager");
        }

        private async Task RestoreAsync()
        {
            if (!CanRestore())
            {
                MessageBox.Show("无法恢复，可能是因为游戏目录无效或插件未在列表中", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(() =>
            {
                // Remove the plugin line from plugins.js
                if (File.Exists(GamePluginsJsPath))
                {
                    List<string> lines = File.ReadAllLines(GamePluginsJsPath)
                                         .Select(line => line.Trim())
                                         .ToList();
                    lines = lines.Where(line => !line.Contains("JtJsonTranslationManager")).ToList();
                    File.WriteAllText(GamePluginsJsPath, string.Join("\n", lines)); // Use \n to keep the original line endings
                }

                // Delete JtJsonTranslationManager.js
                if (File.Exists(TranslationPluginPath))
                {
                    try
                    {
                        File.Delete(TranslationPluginPath);
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("无法删除翻译插件文件: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                }

                // Delete translations.json
                if (File.Exists(TargetJsonPath))
                {
                    try
                    {
                        File.Delete(TargetJsonPath);
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("无法删除翻译文件: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                }

                MessageBox.Show("已成功恢复到未添加翻译插件的状态。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }
    }
}
