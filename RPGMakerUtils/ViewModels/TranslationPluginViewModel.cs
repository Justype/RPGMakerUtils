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
                if (!File.Exists(GamePluginsJsPath))
                    return;

                // === Step 1: 备份文件 ===
                string backupPath = GamePluginsJsPath + ".bak";
                File.Copy(GamePluginsJsPath, backupPath, true); // 覆盖已存在的备份

                // === Step 2: 读取整个文件内容 ===
                string content = File.ReadAllText(GamePluginsJsPath);

                // === Step 3: 检查是否已添加插件 ===
                if (content.Contains("JtJsonTranslationManager"))
                {
                    MessageBox.Show("插件 JtJsonTranslationManager 已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // === Step 4: 提取 $plugins 数组部分 ===
                int startIndex = content.IndexOf("var $plugins =");
                if (startIndex == -1)
                {
                    MessageBox.Show("未找到 'var $plugins =' 声明。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 找到 = 后的第一个 [ 和匹配的 ]
                int arrayStart = content.IndexOf('[', startIndex);
                if (arrayStart == -1)
                {
                    MessageBox.Show("未找到插件数组起始 '['。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 手动匹配括号层级，找到对应的 ]
                int bracketCount = 0;
                int arrayEnd = -1;
                for (int i = arrayStart; i < content.Length; i++)
                {
                    if (content[i] == '[') bracketCount++;
                    else if (content[i] == ']') bracketCount--;

                    if (bracketCount == 0)
                    {
                        arrayEnd = i;
                        break;
                    }
                }

                if (arrayEnd == -1)
                {
                    MessageBox.Show("插件数组括号不匹配。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // === Step 5: 提取数组字符串并解析 ===
                string arrayString = content.Substring(arrayStart, arrayEnd - arrayStart + 1);

                try
                {
                    JArray plugins = JArray.Parse(arrayString);

                    // === Step 6: 插入新插件（作为第一个元素） ===
                    JObject newPlugin = JObject.Parse(PluginLine.TrimEnd(',')); // 移除末尾逗号，避免多余逗号
                    plugins.Insert(0, newPlugin);

                    // === Step 7: 重新构建文件内容 ===
                    string newArrayString = plugins.ToString(Formatting.None); // 不要缩进，保持原风格或后续统一格式
                                                                               // 如果你希望美化，可以用 Formatting.Indented

                    string newContent = content.Substring(0, arrayStart) +
                                        newArrayString +
                                        content.Substring(arrayEnd + 1);

                    // === Step 8: 写回文件 ===
                    File.WriteAllText(GamePluginsJsPath, newContent);

                    // === Step 9: 复制插件文件 ===
                    await Utils.CopyEmbeddedFileAsync($"RPGMakerUtils.Resources.{SelectedPluginVersion}", TranslationPluginPath);

                    // === Step 10: 处理 translations.json ===
                    if (!string.IsNullOrWhiteSpace(TranslateJsonPath) && File.Exists(TranslateJsonPath))
                    {
                        var destPath = Path.Combine(GameWwwPath, "translations.json");
                        try
                        {
                            var jsonText = File.ReadAllText(TranslateJsonPath);
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
            if (IsRunning || string.IsNullOrWhiteSpace(GamePath))
                return false;

            if (GameVersion != RPGMakerVersion.MV && GameVersion != RPGMakerVersion.MZ)
                return false;

            if (string.IsNullOrWhiteSpace(GamePluginsJsPath))
                return false;

            // 只要有备份文件，或者当前文件包含插件，就允许恢复
            string bakPath = GamePluginsJsPath + ".bak";
            return File.Exists(GamePluginsJsPath) && (
                File.Exists(bakPath) ||
                File.ReadAllText(GamePluginsJsPath).Contains("JtJsonTranslationManager")
            );
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
                string bakPath = GamePluginsJsPath + ".bak";

                if (File.Exists(bakPath))
                {
                    // 优先从备份恢复
                    try
                    {
                        File.Copy(bakPath, GamePluginsJsPath, true); // 覆盖当前文件
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"从备份恢复失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                }
                else
                {
                    // 没有备份 → 引导用户手动操作
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "未找到备份文件（plugins.js.bak）。\n" +
                            "为确保安全，程序不会自动修改当前文件。\n" +
                            "请手动从 plugins.js 中删除包含 \"JtJsonTranslationManager\" 的插件对象。",
                            "需要手动操作",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    });
                    return; // 不再继续删除其他文件
                }

                // 删除插件文件（无论是否从备份恢复，只要执行到这一步就删）
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

                // 删除翻译文件
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("已成功恢复到未添加翻译插件的状态。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }
    }
}
