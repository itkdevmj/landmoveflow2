using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LMFS.ViewModels.Pages
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appName = "토지이동 흐름도 관리시스템";

        [ObservableProperty]
        private string _versionInfo = "";

        [ObservableProperty]
        private string _description = "토지이동연혁과 소유권 변동이력을 관리할 수 있는 토지이동흐름도 시스템입니다.";

        [ObservableProperty]
        private string _developer = "(주)아이티코리아";

        [ObservableProperty]
        private string _contact = "연락처: itkinfo@daum.net";

        
        public AboutViewModel()
        {
            Version verInfo = Assembly.GetExecutingAssembly().GetName().Version;
            if (verInfo != null) VersionInfo = $"v{verInfo.Major}.{verInfo.Minor}.{verInfo.Build}";
        }
    }
}
