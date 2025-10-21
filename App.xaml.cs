using DevExpress.Xpf.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Windows;
using LMFS.Models;
using System.IO;
using System.Reflection;

namespace LMFS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            CompatibilitySettings.UseLightweightThemes = true;
            ApplicationThemeHelper.Preload(PreloadCategories.Core);
            ConfigureNLog();
        }


        private void ConfigureNLog()
        {
            var logPath = LMFS.Properties.Settings.Default.LogPath;

            if (string.IsNullOrEmpty(logPath))
            {
                logPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Logs/"; // 기본 경로 설정
            }

            if (!System.IO.Directory.Exists(logPath))
            {
                System.IO.Directory.CreateDirectory(logPath);
            }

            var config = new LoggingConfiguration();

            // 파일 로그 타겟
            var logfile = new FileTarget("logfile")
            {
                FileName = $"{logPath}/log-${{shortdate}}.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message} ${exception:format=toString}"
            };
            config.AddTarget(logfile);

            // 로그 레벨 규칙
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
            
            
            // DbConInfo 초기화
            DbConInfo.ip = LMFS.Properties.Settings.Default.DbHostIP;
            DbConInfo.port = LMFS.Properties.Settings.Default.DbHostPort;
            DbConInfo.id = LMFS.Properties.Settings.Default.DbUserId;
            DbConInfo.password = LMFS.Properties.Settings.Default.DbPassword;
            DbConInfo.db = LMFS.Properties.Settings.Default.DbName;


            MakeTempDir();//Vit.G//XML저장을 위한 임시 폴더 생성
        }

        private void MakeTempDir()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);

            // 상대경로 폴더명 지정
            string folderPath = Path.Combine(exeDir, "_tempdir");

            // 폴더 생성 (있으면 무시)
            Directory.CreateDirectory(folderPath);
        }

        //프로그램 종료
        protected override void OnExit(ExitEventArgs e)
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            string folderPath = Path.Combine(exeDir, "_tempdir");

            // 폴더 삭제, 하위 파일/폴더도 함께 삭제(true)
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }

            base.OnExit(e);
        }
    }
}
