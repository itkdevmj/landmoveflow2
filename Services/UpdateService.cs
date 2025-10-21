using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using LMFS.Db;
using LMFS.Models;
using NLog;

namespace LMFS.Services
{
    public class UpdateService
    {
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        public async Task<(bool, VersionInfo?)> CheckForUpdatesAsync()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion == null) return (false, null);

                // DB에서 버전정보 추출
                VersionInfo verInfo = DBService.SelectLastAppVersion();
                if (verInfo == null) return (false, null);
                
                var serverVersion = new Version(verInfo.Version);

                if (serverVersion > currentVersion)
                {
                    return (true, verInfo); // 업데이트 필요
                }

                return (false, null); // 최신 버전
            }
            catch (Exception ex)
            {
                logger.Debug($"업데이트 확인 중 오류: {ex.Message}");
                return (false, null);
            }
        }

        public void StartUpdater(VersionInfo versionInfo)
        {
            string updaterPath = Path.Combine(AppContext.BaseDirectory, "AutoUpdater.exe");

            if (!File.Exists(updaterPath))
            {
                MessageBox.Show("업데이트 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string arguments = $"--url \"{versionInfo.Url}\" --pid {Process.GetCurrentProcess().Id}";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = arguments,
                UseShellExecute = true,
                // ⚠️ 권한 상승이 필요하다면 아래 주석을 해제하세요. (Updater 프로젝트에도 app.manifest 설정 필요)
                Verb = "runas" 
            };

            Process.Start(processStartInfo);
            Application.Current.Shutdown();
        }
    }
}
