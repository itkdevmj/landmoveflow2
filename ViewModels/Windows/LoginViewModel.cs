using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LMFS.Models;
using LMFS.Services;
using System.Threading.Tasks;
using System.Windows;

namespace LMFS.ViewModels.Pages
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly KeyCloakLoginService _keyCloakLoginService = new KeyCloakLoginService();
        private readonly UpdateService _updateService;

        public LoginViewModel(KeyCloakLoginService loginService, UpdateService updateService)
        {
            _keyCloakLoginService = loginService;
            _updateService = updateService;
            CheckForUpdateCommand.Execute(null);
        }


        [RelayCommand]
        private async Task CheckForUpdateAsync()
        {
            bool isUpdateCheckEnabled = LMFS.Properties.Settings.Default.AutoUpdateEnabled;
            if (isUpdateCheckEnabled)
            {
                var (needsUpdate, versionInfo) = await _updateService.CheckForUpdatesAsync();
                if (needsUpdate && versionInfo != null)
                {
                    var message = $"새로운 버전({versionInfo.Version})이 있습니다. 지금 업데이트하시겠습니까?\n\n[업데이트 내용]\n{versionInfo.ReleaseNotes}";
                    var result = MessageBox.Show(message, "업데이트 알림", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        _updateService.StartUpdater(versionInfo);
                    }
                }
            }
        }


        [RelayCommand]
        private async Task Login()
        {
            User user = await _keyCloakLoginService.LoginTask();
            if (user != null)
            {
                //var session = UserSession.Instance;
                //session.user = user;

                WeakReferenceMessenger.Default.Send(new Messages.LoginSuccessMessage(user));
            }
        }
    }
}
