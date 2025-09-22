using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public string GamePluginsJsBackupPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GamePluginsJsPath))
                    return string.Empty;
                return GamePluginsJsPath + ".zip";
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
                && !File.Exists(GameDataBackupZipPath) // Make sure it is not translated by TranslateViewModel
                && !File.Exists(GamePluginsJsBackupPath); // Make sure it is not translated
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
                if (!File.Exists(GamePluginsJsPath))
                    return;

                // === Step 1: 备份文件 ===
                Utils.CreateZipFromFileAsync(GamePluginsJsPath, GamePluginsJsBackupPath).Wait();

                // === Step 2: 读取整个文件内容 ===
                string content = File.ReadAllText(GamePluginsJsPath);

                try
                {
                    // === Step 3: 使用正则表达式提取插件数组 ===
                    Regex pluginRegex = new Regex(@"var\s*\$plugins\s*=\s*(?s)(?<json>\[.*\]);?", RegexOptions.Singleline);
                    Match match = pluginRegex.Match(content);
                    if (!match.Success)
                    {
                        MessageBox.Show("无法在 plugins.js 中找到插件数组", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string arrayString = match.Groups["json"].Value;

                    // === Step 4: 解析 JSON 数组 ===
                    JArray plugins = JArray.Parse(arrayString);

                    // === Step 5: 插入新插件（作为第一个元素） ===
                    JObject newPlugin = JObject.Parse(PluginLine.TrimEnd(',')); // 移除末尾逗号，避免多余逗号
                    plugins.Insert(0, newPlugin);

                    // === Step 6: 重新构建文件内容 ===
                    string newArrayString = plugins.ToString(Formatting.None);
                    string newContent = pluginRegex.Replace(content, $"var $plugins = {newArrayString};");

                    // === Step 7: 写回文件 ===
                    File.WriteAllText(GamePluginsJsPath, newContent);

                    // === Step 8: 复制插件文件 ===
                    await Utils.CopyEmbeddedFileAsync($"RPGMakerUtils.Resources.{SelectedPluginVersion}", TranslationPluginPath);

                    // === Step 9: 处理 translations.json ===
                    if (!string.IsNullOrWhiteSpace(TranslateJsonPath) && File.Exists(TranslateJsonPath))
                    {
                        var destPath = Path.Combine(GameWwwPath, "translations.json");
                        try
                        {
                            var jsonText = File.ReadAllText(TranslateJsonPath);
                            // Parse and re-serialize to remove comments and ensure valid JSON (JSON should not have comments)
                            var jsonObj = JsonConvert.DeserializeObject<object>(jsonText);
                            var cleanedJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                            File.WriteAllText(destPath, cleanedJson);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("JSON解析失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    MessageBox.Show("已成功添加翻译插件和翻译文件。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show("插件数组 JSON 解析失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        private bool CanRestore()
        {
            return File.Exists(GamePluginsJsBackupPath) && !IsRunning;
        }

        private async Task RestoreAsync()
        {
            if (!CanRestore())
            {
                MessageBox.Show("无法恢复，可能是因为没有备份文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            await Task.Run(async () =>
            {
                bool isSuccess = await Utils.ExtractZipAsync(GamePluginsJsBackupPath, Path.GetDirectoryName(GamePluginsJsPath), overwrite: true);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (isSuccess)
                    {
                        try
                        {
                            if (File.Exists(TargetJsonPath))
                                File.Delete(TargetJsonPath);
                            if (File.Exists(TranslationPluginPath))
                                File.Delete(TranslationPluginPath);
                            if (File.Exists(GamePluginsJsBackupPath))
                                File.Delete(GamePluginsJsBackupPath);
                            MessageBox.Show("已成功恢复到未添加翻译插件的状态。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("删除过程中出现错误，请手动删除文件: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                        MessageBox.Show("恢复失败，请手动解压并还原", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }
    }
}
