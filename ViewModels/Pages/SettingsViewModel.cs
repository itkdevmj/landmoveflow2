using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LMFS.Interfaces;

namespace LMFS.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IMessageBoxService _messageBoxService;

        [ObservableProperty]
        private bool _isAutoUpdateEnabled = false;

        [ObservableProperty]
        private bool _isNotificationEnabled = false;

        [ObservableProperty] private string _dbIp;
        [ObservableProperty] private string _dbPort;
        [ObservableProperty] private string _dbName;
        [ObservableProperty] private string _dbUserId;
        [ObservableProperty] private string _dbUserPassword;
        
        [RelayCommand]
        private void SaveIsAutoUpdateEnabled()
        {
            LMFS.Properties.Settings.Default.AutoUpdateEnabled = IsAutoUpdateEnabled;
            LMFS.Properties.Settings.Default.Save();

        }
        
        
        [RelayCommand]
        private void SaveIsNotificationEnabled()
        {
            LMFS.Properties.Settings.Default.NotificationEnabled = IsNotificationEnabled;
            LMFS.Properties.Settings.Default.Save();
        }

        [RelayCommand]
        private void UpdateDatabaseConnectInfo()
        {
            try
            {
                LMFS.Properties.Settings.Default.DbHostIP = DbIp;
                LMFS.Properties.Settings.Default.DbHostPort = DbPort;
                LMFS.Properties.Settings.Default.DbName = DbName;
                LMFS.Properties.Settings.Default.DbUserId = DbUserId;
                LMFS.Properties.Settings.Default.DbPassword = DbUserPassword;
                LMFS.Properties.Settings.Default.Save();

                _messageBoxService.ShowMessage("정상적으로 저장되었습니다.", "DB정보 저장");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"저장에 문제가 발생했습니다.\n\n{ex.Message}", "DB정보 저장");
            }
        }




        public SettingsViewModel(IMessageBoxService service)
        {
            _messageBoxService = service;

            IsAutoUpdateEnabled = LMFS.Properties.Settings.Default.AutoUpdateEnabled;
            IsNotificationEnabled = LMFS.Properties.Settings.Default.NotificationEnabled;

            DbIp = LMFS.Properties.Settings.Default.DbHostIP;
            DbPort = LMFS.Properties.Settings.Default.DbHostPort;
            DbName = LMFS.Properties.Settings.Default.DbName;
            DbUserId = LMFS.Properties.Settings.Default.DbUserId;
            DbUserPassword = LMFS.Properties.Settings.Default.DbPassword;
        }
    }
}
