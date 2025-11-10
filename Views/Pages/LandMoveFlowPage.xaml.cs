using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Diagram.Core;// DiagramImageExportFormat
using DevExpress.Diagram.Core.Native;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;// WPF용
using DevExpress.XtraPrinting;
using LMFS.Engine;
using LMFS.Messages;
using LMFS.ViewModels;
using LMFS.ViewModels.Pages;
using System;
using System.IO;// FileStream
using System.Windows;// Rect
using System.Windows.Controls;
using System.Windows.Shapes;
using static LMFS.ViewModels.Pages.LandMoveFlowViewModel;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// LandMoevePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveFlowPage : System.Windows.Controls.Page, IRecipient<LoadXmlMessage>
    {
        public LandMoveFlowViewModel FlowViewModel { get; set; }


        public LandMoveFlowPage()
        {
            InitializeComponent();

            //
            FlowViewModel = new LandMoveFlowViewModel();
            this.DataContext = FlowViewModel;

            // LoadXmlMessage만 Page에서 처리 (UI 직접 접근 필요한 경우)
            WeakReferenceMessenger.Default.Unregister<LoadXmlMessage>(this);
            WeakReferenceMessenger.Default.Register<LoadXmlMessage>(this);

            RegisterMessages();// 생성자에서 이 함수 한번만!
        }


        private void RegisterMessages()
        {
            // 기존 등록이 있다면 먼저 해제
            WeakReferenceMessenger.Default.UnregisterAll(this);

            // 메시지 수신 등록
            WeakReferenceMessenger.Default.Register<PrintDiagramMessage>(this, (r, m) => OnPrintDiagram());//실제 인쇄 작업만! 다시 Send 호출 금지!
            WeakReferenceMessenger.Default.Register<PrintPreviewDiagramMessage>(this, (r, m) => OnPrintPreviewDiagram());
            WeakReferenceMessenger.Default.Register<ExportDiagramMessage>(this, (r, m) => OnExportDiagram(m.FilePath, m.Format));
            //WeakReferenceMessenger.Default.Register<ExportDiagramMessage>(this, (r, m) => OnExportDiagram(m.FilePath, m.Format));
            //WeakReferenceMessenger.Default.Register<ExportDiagramMessage>(this, (r, m) => OnExportDiagram(m.FilePath, m.Format));

            Messenger.Default.Register<RequestExportGridMessage>(this, (msg) =>
            {
                string ext = System.IO.Path.GetExtension(msg.ExportPath); // 예: ".xls", ".xlsx"                

                if (ext == ".xls")
                {
                    var options = new XlsExportOptionsEx() { SheetName = msg.SheetName };
                    ((TableView)FlowDataGrid.View).ExportToXls(msg.ExportPath, options);
                }
                else
                {
                    var options = new XlsxExportOptionsEx() { SheetName = msg.SheetName };
                    ((TableView)FlowDataGrid.View).ExportToXlsx(msg.ExportPath, options);
                }
                MessageBox.Show("엑셀 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            });
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
                // 프린터 확인
                if (System.Drawing.Printing.PrinterSettings.InstalledPrinters.Count == 0)
                {
                    MessageBox.Show("설치된 프린터가 없습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 다이어그램이 비어있는지 확인
                if (LmfControl.Items.Count == 0)
                {
                    MessageBox.Show("인쇄할 내용이 없습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Print 메서드로 Print Dialog 표시 후 인쇄
                LmfControl.Print();

                // 또는 바로 인쇄
                // LmfControl.QuickPrint();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 실패: {ex.Message}\n\n상세 정보: {ex.StackTrace}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPrintPreviewDiagram()
        {
            try
            {
                // 다이어그램이 비어있는지 확인
                if (LmfControl.Items.Count == 0)
                {
                    MessageBox.Show("미리보기할 내용이 없습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 매개변수 없이 호출 (기본 배율)
                LmfControl.ShowPrintPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 미리보기 실패: {ex.Message}\n\n상세 정보: {ex.StackTrace}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportDiagram(string filePath, ExportDiagramFormat format)
        {
            try
            {
                switch (format)
                {
                    case ExportDiagramFormat.Pdf:
                        LmfControl.ExportToPdf(filePath);
                        MessageBox.Show("PDF 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportDiagramFormat.Jpg:
                        // JPG의 경우 - Stream 사용
                        ExportImageWithSettings(filePath, DiagramImageExportFormat.JPEG);
                        MessageBox.Show("JPG 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportDiagramFormat.Png:
                        // PNG의 경우 - Stream 사용
                        ExportImageWithSettings(filePath, DiagramImageExportFormat.PNG);
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

        private void ExportImageWithSettings(string filePath, DiagramImageExportFormat format)
        {
            // DPI 설정 (96 = 기본, 150 = 중간, 300 = 고해상도)
            double dpi = 300;

            // Scale 설정 (1.0 = 100%, 2.0 = 200%)
            double scale = 1.0;

            // Export bounds (null이면 전체 다이어그램)
            System.Windows.Rect? exportBounds = null;

            // Stream을 사용하여 Export
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                LmfControl.ExportToImage(
                    stream,           // Stream
                    format,           // DiagramImageExportFormat
                    exportBounds,     // Nullable<Rect> - 내보낼 영역 (null = 전체)
                    dpi,              // Nullable<Double> - 해상도
                    scale             // Nullable<Double> - 배율
                );
            }
        }

        /*
         더 나아가 사용자가 설정을 선택할 수 있도록 대화상자를 추가할 수 있습니다:

        csharp
        private void OnExportDiagramWithSettings(string filePath, ExportDiagramFormat format)
        {
            try
            {
                // 설정 대화상자 표시
                var settingsDialog = new ImageExportSettingsDialog();
                if (settingsDialog.ShowDialog() == true)
                {
                    double dpi = settingsDialog.SelectedDpi;      // 예: 96, 150, 300
                    double scale = settingsDialog.SelectedScale;  // 예: 0.5, 1.0, 2.0
                    int quality = settingsDialog.JpegQuality;     // JPG의 경우 1-100

                    var imageFormat = format == ExportFormat.Jpg 
                        ? System.Drawing.Imaging.ImageFormat.Jpeg 
                        : System.Drawing.Imaging.ImageFormat.Png;

                    // ExportToImage는 직접적인 품질 설정이 없으므로
                    // BitmapEncoder를 사용하여 수동으로 저장
                    if (format == ExportFormat.Jpg)
                    {
                        SaveAsJpegWithQuality(filePath, dpi, scale, quality);
                    }
                    else
                    {
                        LmfControl.ExportToImage(filePath, imageFormat, dpi, scale, 
                            new System.Windows.Thickness(10));
                    }

                    MessageBox.Show("이미지가 저장되었습니다.", "성공",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsJpegWithQuality(string filePath, double dpi, double scale, int quality)
        {
            // RenderTargetBitmap으로 다이어그램 렌더링
            double width = LmfControl.ActualWidth * scale;
            double height = LmfControl.ActualHeight * scale;
    
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)width, (int)height, dpi, dpi, PixelFormats.Pbgra32);
    
            renderBitmap.Render(LmfControl);
    
            // JPEG Encoder로 품질 설정
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = quality; // 1-100
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
    
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
        */

    }
}
