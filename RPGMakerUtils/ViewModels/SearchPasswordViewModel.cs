using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PropertyChanged;
using RPGMakerUtils.Messages;
using RPGMakerUtils.Models;
using RPGMakerUtils.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RPGMakerUtils.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class SearchPasswordViewModel : CommonViewModel
    {
        public ObservableCollection<PasswordDialog> PasswordDialogs { get; private set; } = new ObservableCollection<PasswordDialog>();

        private PasswordDialog _selectedPasswordDialog = new PasswordDialog();

        public PasswordDialog SelectedPasswordDialog
        {
            get => _selectedPasswordDialog;
            set
            {
                _selectedPasswordDialog = value;
                CopySelectedCommand.NotifyCanExecuteChanged();
            }
        }

        public IAsyncRelayCommand SearchPasswordCommand { get; private set; }

        public IRelayCommand CopySelectedCommand { get; private set; }

        public SearchPasswordViewModel()
        {
            SearchPasswordCommand = new AsyncRelayCommand(SearchPassword, CanSearchPassword);
            CopySelectedCommand = new RelayCommand(CopySelected, CanCopySelected);
            WeakReferenceMessenger.Default.Register<ProgramRunningMessage>(this, (r, m) =>
            {
                IsRunning = m.Value;
                SearchPasswordCommand.NotifyCanExecuteChanged();
            });
            WeakReferenceMessenger.Default.Register<GameInfoUpdatedMessage>(this, (r, m) =>
            {
                GamePath = m.Value.GamePath;
                GameVersion = m.Value.GameVersion;
                UpdateGameDataFiles();
                SearchPasswordCommand.NotifyCanExecuteChanged();
            });
        }

        public async Task SearchPassword()
        {
            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(true));

            PasswordDialogs.Clear();
            IEnumerable<PasswordDialog> newPasswordDialogs = await Task.Run(() =>
                RPGMakerPasswordFinder.GetPasswordsFromGameData(GameDataFiles));

            foreach (var passwordDialog in newPasswordDialogs)
            {
                PasswordDialogs.Add(passwordDialog);
            }

            MessageBox.Show(
                $"搜索完成, 找到 {PasswordDialogs.Count} 个可能的密码",
                "搜索完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            WeakReferenceMessenger.Default.Send(new ProgramRunningMessage(false));
        }

        public bool CanSearchPassword() => GameDataFiles.Count() != 0 && !IsRunning;

        public void CopySelected()
        {
            if (SelectedPasswordDialog != null)
            {
                Clipboard.SetText(SelectedPasswordDialog.Password);
            }
        }

        public bool CanCopySelected() => SelectedPasswordDialog != null && !string.IsNullOrWhiteSpace(SelectedPasswordDialog.Password);
    }
}
