using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGMakerUtils.Models;
using PropertyChanged;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RPGMakerUtils.Messages;

namespace RPGMakerUtils.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class MainViewModel : CommonViewModel
    {
        public MainViewModel()
        {
            GamePath = "请导入游戏目录或程序";

            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
            });
        }
    }
}