//using LMFS.Utils;
using CommunityToolkit.Mvvm.Messaging;
using LMFS.Messages;
using LMFS.Services;
using LMFS.ViewModels.Pages;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LMFS.Views.Windows
{
    public partial class LoginWindow : Window, IRecipient<LoginSuccessMessage>
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel(new KeyCloakLoginService(), new UpdateService());
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        // 닫기 버튼 클릭 이벤트
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }


        public async void Receive(LoginSuccessMessage message)
        {
            await Task.Delay(100); //[WarningFixed] 비동기 대기//async 사용 시 추가

            if (message.User != null && message.User.name != null)
            {
                // 응용프로그램 세션에 User 객체 저장
                GlobalDataManager.Instance.loginUser = message.User;
                
                this.Close();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
    }
}
