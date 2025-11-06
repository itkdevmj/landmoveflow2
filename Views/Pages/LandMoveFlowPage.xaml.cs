using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using LMFS.Engine;
using LMFS.Messages;
using LMFS.ViewModels;
using LMFS.ViewModels.Pages;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// ProductsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveFlowPage : Page, IRecipient<LoadXmlMessage>
    {
        public LandMoveFlowPage()
        {
            InitializeComponent();
            DataContext = new LandMoveFlowViewModel();

            // LoadXmlMessage만 Page에서 처리 (UI 직접 접근 필요한 경우)
            WeakReferenceMessenger.Default.Unregister<LoadXmlMessage>(this);
            WeakReferenceMessenger.Default.Register<LoadXmlMessage>(this);
        }


        private void UpdateDataGridMaxHeight()
        {
            double windowHeight = this.ActualHeight;
            double otherControlsHeight = 30 + 10 + 10;
            double systemUiHeight = 40;
            double calculatedMaxHeight = windowHeight - otherControlsHeight - systemUiHeight;

            if (calculatedMaxHeight > 0)
            {
                FlowDataGrid.MaxHeight = calculatedMaxHeight;
            }
        }

        private void LandMoveFlowPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();

            // TrackBar 틱 라벨 설정
            SetupTrackBarLabels();
        }

        
        private void LandMoveFlowPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 페이지가 언로드될 때 메시지 수신 해제
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        private void LandMoveFlowPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }
        
        
        public async void Receive(LoadXmlMessage message)
        {
            // xml 파일 로드해서 다이어그램에 전달 처리
            if (message.filePath != null)
            {
                await using FileStream fs = new FileStream(message.filePath, FileMode.Open);
                LmfControl.ZoomFactor = 1.0;//251027// 
                LmfControl.LoadDocument(fs);                
            }
        }

        private void TextEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            var textEdit = sender as DevExpress.Xpf.Editors.TextEdit;
            if (textEdit != null)
            {
                Dispatcher.BeginInvoke(new Action(() => textEdit.SelectAll()));
            }
        }

        //✅ TrackBar의 범위가 0.5~3.0 (50%~300%)로 설정됩니다
        //✅ 오른쪽에 현재 배율이 "100%" 형식으로 표시됩니다
        //✅ ZoomPercentConverter를 사용하여 자동으로 퍼센트 변환됩니다
        private void SetupTrackBarLabels()
        {
            // TrackBar 아래에 퍼센트 라벨 표시
            if (ZoomTrackBar != null)
            {
                // DevExpress TrackBarEdit의 경우 CustomDrawTick 이벤트나
                // ItemTemplate을 사용하여 커스터마이징 가능
            }
        }



        private void OnPrintDiagram()
        {
            try
            {
                LmfControl.QuickPrint();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPrintPreviewDiagram()
        {
            try
            {
                LmfControl.ShowPrintPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 미리보기 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportDiagram(string filePath, ExportFormat format)
        {
            try
            {
                switch (format)
                {
                    case ExportFormat.Pdf:
                        LmfControl.ExportToPdf(filePath);
                        MessageBox.Show("PDF 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportFormat.Jpg:
                        LmfControl.ExportDiagram(filePath);
                        MessageBox.Show("JPG 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportFormat.Png:
                        LmfControl.ExportDiagram(filePath);
                        MessageBox.Show("PNG 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
