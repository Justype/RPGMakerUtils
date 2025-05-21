using CommunityToolkit.Mvvm.Messaging;
using RPGMakerUtils.Messages;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RPGMakerUtils.Resources
{
    internal class FileIOUtils
    {
        /// <summary>
        /// Load the game path and send a message to update the game info.
        /// </summary>
        /// <param name="gamePath">Game Path or Game.exe Path</param>
        public static void LoadGamePathAndSendMessage(string gamePath)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
                return;
            if (!File.GetAttributes(gamePath).HasFlag(FileAttributes.Directory))
                gamePath = Path.GetDirectoryName(gamePath) ?? string.Empty;

            RPGMakerVersion version = Utils.GetGameVersion(gamePath);

            switch (version)
            {
                case RPGMakerVersion.MV:
                    WeakReferenceMessenger.Default.Send(new GameInfoUpdatedMessage((gamePath, RPGMakerVersion.MV)));
                    break;
                case RPGMakerVersion.MZ:
                    WeakReferenceMessenger.Default.Send(new GameInfoUpdatedMessage((gamePath, RPGMakerVersion.MZ)));
                    break;
                default:
                    MessageBox.Show("无法确定此游戏的版本", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        public static void LoadTranslatePathAndSendMessage(string translateJsonPath)
        {
            if (string.IsNullOrWhiteSpace(translateJsonPath))
                return;
            if (!File.Exists(translateJsonPath))
            {
                MessageBox.Show("翻译文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Utils.IsJsonDictionary(File.ReadAllText(translateJsonPath)))
            {
                MessageBox.Show("翻译文件格式错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            WeakReferenceMessenger.Default.Send(new TranslateJsonUpdatedMessage(translateJsonPath));
        }
    }
}
