using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PropertyChanged;
using RPGMakerUtils.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace RPGMakerUtils.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class PasswordViewModel : CommonViewModel
    {
        public string PasswordKeyword { get; set; } = "礼包码"; // 汉化组喜欢用这个关键词

        public int PasswordDigit { get; set; } = 8; // 密码一般是8位数

        private string _selectedItem = string.Empty;

        public string SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                CopySelectedCommand.NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<string> PasswordList { get; private set; } = new ObservableCollection<string>();

        public IAsyncRelayCommand SearchCommand { get; }

        public IRelayCommand CopySelectedCommand { get; }

        public PasswordViewModel()
        {
            SearchCommand = new AsyncRelayCommand(Search, CanSearch);
            CopySelectedCommand = new RelayCommand(CopySelected, CanCopySelected);

            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                SearchCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                UpdateGameDataFiles();
                SearchCommand.NotifyCanExecuteChanged();
            });
        }

        public async Task Search()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            PasswordList.Clear();
            IEnumerable<string> newCodeList = await Task.Run(() =>
            {
                List<string> codeList = new List<string>();
                var files = GameDataFiles.ToList();
                foreach (var file in files)
                {
                    string text = File.ReadAllText(file.FilePath);
                    if (text.Contains(PasswordKeyword))
                    {
                        var results = Regex.Matches(text, $"\\d{{{PasswordDigit}}}");
                        foreach (var result in results)
                        {
                            codeList.Add(result.ToString());
                        }
                    }
                }
                return codeList.Distinct();
            });

            foreach (var code in newCodeList)
                PasswordList.Add(code);

            MessageBox.Show(
                $"搜索完成, 找到 {PasswordList.Count} 个可能的密码",
                "搜索完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        public bool CanSearch() =>
            GameDataFiles.Count() != 0 &&
                    PasswordDigit > 0 &&
                        !IsRunning;

        public void CopySelected() =>
            Clipboard.SetText(SelectedItem);

        public bool CanCopySelected() =>
            !string.IsNullOrWhiteSpace(SelectedItem) &&
                !IsRunning;
    }
}
