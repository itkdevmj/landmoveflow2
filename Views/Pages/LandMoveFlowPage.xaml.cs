using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Diagram.Core;// DiagramImageExportFormat
using DevExpress.Diagram.Core.Native;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;// WPF용
using DevExpress.XtraBars;
using DevExpress.XtraPrinting;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Messages;
using LMFS.ViewModels;
using LMFS.ViewModels.Pages;
using System;
using System.ComponentModel;
using System.IO;// FileStream
using System.Threading.Tasks;
using System.Windows;// Rect
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using static LMFS.ViewModels.Pages.LandMoveFlowViewModel;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// LandMoevePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveFlowPage : System.Windows.Controls.Page, IRecipient<LoadXmlMessage>
    {
        public LandMoveFlowViewModel FlowVM { get; set; }

        private bool _diagramShown = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public LandMoveFlowPage(LandMoveSettingViewModel settingVM)
        {
            InitializeComponent();

            //
            FlowVM = new LandMoveFlowViewModel(settingVM);
            this.DataContext = FlowVM;//자신의 ViewModel//

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
                MessageBox.Show(Application.Current.MainWindow, "엑셀 파일로 저장되었습니다.", "성공",
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
                    MessageBox.Show(Application.Current.MainWindow, "설치된 프린터가 없습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 다이어그램이 비어있는지 확인
                if (LmfControl.Items.Count == 0)
                {
                    MessageBox.Show(Application.Current.MainWindow, "인쇄할 내용이 없습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Print 메서드로 Print Dialog 표시 후 인쇄
                LmfControl.Print();

                // 또는 바로 인쇄
                // LmfControl.QuickPrint();

                // 검색필지 [인쇄] 정보 User_Hist 테이블에 추가
                var actCd = FlowVM.Converter.GetCodeValue(6, "인쇄");
                DBService.InsertUserHist(FlowVM.CurrentPnu, actCd);

            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, $"인쇄 실패: {ex.Message}\n\n상세 정보: {ex.StackTrace}", "오류",
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
                    MessageBox.Show(Application.Current.MainWindow, "미리보기할 내용이 없습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 매개변수 없이 호출 (기본 배율)
                LmfControl.ShowPrintPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, $"인쇄 미리보기 실패: {ex.Message}\n\n상세 정보: {ex.StackTrace}", "오류",
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
                        MessageBox.Show(Application.Current.MainWindow, "PDF 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportDiagramFormat.Jpg:
                        // JPG의 경우 - Stream 사용
                        ExportImageWithSettings(filePath, DiagramImageExportFormat.JPEG);
                        MessageBox.Show(Application.Current.MainWindow, "JPG 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case ExportDiagramFormat.Png:
                        // PNG의 경우 - Stream 사용
                        ExportImageWithSettings(filePath, DiagramImageExportFormat.PNG);
                        MessageBox.Show(Application.Current.MainWindow, "PNG 파일로 저장되었습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }

                // 검색필지 [내보내기] 정보 User_Hist 테이블에 추가
                var actCd = FlowVM.Converter.GetCodeValue(6, "내보내기");
                DBService.InsertUserHist(FlowVM.CurrentPnu, actCd);

            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, $"저장 실패: {ex.Message}", "오류",
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

        private void LmfControl_Loaded(object sender, RoutedEventArgs e)
        {   
            //#DiagramPanning//다이어그램 로딩 후, 기본 툴을 Pan으로
            var diagram = (DiagramControl)sender;
            diagram.ActiveTool = diagram.PanTool;


            // 작업 끝나면 Busy 팝업 닫기 (예외 있어도 안전하게 처리)
            if (!FlowVM.IsDiagramReady || _diagramShown)
                return;

            _diagramShown = true;
            FlowVM.BusPopup?.Close();

            // 3. 모든 작업이 끝난 시점에서
            FlowVM.IsDiagramReady = true;
        }

        ////FitToDrawing, BringIntoView, 코드에서 직접 ZoomFactor 변경 등 “모든 Zoom 변경”이 TrackBar에 반영
        //private void ZoomTrackBar_EditValueChanged(object sender, RoutedEventArgs e)
        //{
        //    // 예시: TrackBar의 Value 값을 얻어서 처리
        //    var trackBar = sender as TrackBarEdit;
        //    if (trackBar != null)
        //    {
        //        //[디버깅용]
        //        //trackBar.Value = 0.7;

        //        // 필요시 직접 ZoomFactor에 할당 (MVVM이 아니라면)
        //        this.ZoomFactor = trackBar.Value / 100.0;                
        //        // 혹은 다이어그램/컨트롤에 확대 적용
        //        LmfControl.ZoomFactor = this.ZoomFactor;
        //    }
        //    // 추가적으로 디버깅 출력해볼 수도 있음
        //    // Debug.WriteLine($"TrackBar 현재값: {newZoom}");
        //}

        private void LmfControl_ZoomFactorChanged(object sender, EventArgs e)
        {
            //// DiagramControl의 실제 ZoomFactor
            //double z = LmfControl.ZoomFactor;           // 예: 1.25 → 125%
            //this.ZoomFactor = z;                        // 속성 갱신
            //this.ZoomPercent = (int)Math.Round(z * 100);
            //ZoomTrackBar.Value = ZoomPercent;          // 필요시 직접 대입

            // DataContext가 LandMoveFlowViewModel이라고 가정
            if (DataContext is LandMoveFlowViewModel vm)
            {
                double z = LmfControl.ZoomFactor; // 실제 줌 값 (예: 1.25 = 125%)
                vm.ZoomFactor = z;                // VM에 반영 (VM 내부에서 ZoomPercent도 같이 계산됨)
                                                  // TrackBar는 ZoomPercent에 바인딩되어 있으므로 이 한 줄만으로도 충분
                                                  // 필요하다면 아래 직접 세팅은 생략 가능
                                                  // ZoomTrackBar.Value = vm.ZoomPercent;
            }
        }

        private void LmfControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var diagram = sender as DiagramControl;
            Point clickPoint = e.GetPosition(diagram);
            var inputElement = diagram.InputHitTest(clickPoint) as DependencyObject;

            while (inputElement != null && !(inputElement is DiagramShape))
            {
                inputElement = VisualTreeHelper.GetParent(inputElement);
            }

            if (inputElement is DiagramShape shape)
            {
                ParseAndSearch(shape.Content);
                e.Handled = true;
            }
        }

        private void ParseAndSearch(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            //소유자명, 지목, 면적 등 표기한 경우 '지번'만 가져오기 위함//
            content = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];

            bool isSan = content.StartsWith("산 ");
            string cleanContent = isSan ? content.Substring(2) : content; // "산 " 제거

            string[] parts = cleanContent.Split('-');
            if (parts.Length == 2)            
            {
                string bobn = parts[0].Trim();
                string bubn = parts[1].Trim();

                // 선택한 지번으로 포커싱 하기 위함(실제 재검색 아니고 이미 구성한 xml 그대로 이용)
                FlowVM.IsSan = isSan;
                FlowVM.Bobn = int.Parse(bobn).ToString("D4");
                FlowVM.Bubn = int.Parse(bubn).ToString("D4");
                FlowVM.Converter._pnu = FlowVM.BuildPnu();
                FlowVM.Converter.SaveMakedXmlData();
                // 다이어그램 다시 그리기
                FlowVM.Converter.RefreshDiagramLandMoveFlow(FlowVM);
                //XDocument rtnXml = FlowVM.Converter.RefreshDiagramLandMoveFlow();
                //FlowVM.ProcessDiagramLandMoveFlow(rtnXml);
            }
        }

        #region 다이어그램 패닝

        #endregion
    }
}
