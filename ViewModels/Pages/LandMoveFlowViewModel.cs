using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Data.Browsing;
using DevExpress.Dialogs.Core.View;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;// WPF용만 유지
using DevExpress.XtraPrinting;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Messages;
using LMFS.Models;
using LMFS.Services;
using LMFS.Views;
using LMFS.Views.Pages;
using Microsoft.Win32;// SaveFileDialog를 위해 필요
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveFlowViewModel : ObservableObject
    {
        public static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        
        [ObservableProperty] private List<SidoCode> _umdList;
        [ObservableProperty] private List<SidoCode> _riList;
        [ObservableProperty] private SidoCode _selectedUmd;
        [ObservableProperty] private SidoCode _selectedRi;
        [ObservableProperty] private bool _isSan;
        [ObservableProperty] private string _bobn;
        [ObservableProperty] private string _bubn;

        [ObservableProperty] private bool _jimokChg;//vm.JimokChg
        [ObservableProperty] private bool _portrait;//vm.Portrait
        [ObservableProperty] private bool _isOwnName;
        [ObservableProperty] private bool _isJimok;
        [ObservableProperty] private bool _isArea;
        [ObservableProperty] private string _currentPnu;
        [ObservableProperty] private string _currentPnuNm;//for Saving Files
        [ObservableProperty] private List<LandMoveInfo> _gridDataSource;
        [ObservableProperty] private List<LandMoveInfoCategory> _gridCategoryDataSource;
        [ObservableProperty] private MemoryStream _landMoveFlowData;

        [ObservableProperty] private int _currentGSeq;//상세화면에서 insert 시 사용될 예정//


        [ObservableProperty] private bool _isFlowData = false;
        //검색결과 많은 경우, 코드=>명칭 변경과 다이어그램 그리는데 시간이 소요되어 그리드가 깔끔하게 갱신되지 안아서 처리//
        [ObservableProperty] private bool isDiagramReady = false;


        public LandMoveFlowConverter Converter { get; set; }
        public LandMoveSettingViewModel SettingVM { get; }
        public BusyWindow BusPopup;

        //[Warning Fixed]이 호출이 대기되지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다. 호출 결과에 'await' 연산자를 적용해 보세요.
        //public RelayCommand SearchCommand => new(async () => await OnSearch());


        public record RequestExportGridMessage(string ExportPath, string SheetName);


        public LandMoveFlowViewModel(LandMoveSettingViewModel settingVM)
        {
            SettingVM = settingVM;
            //251117//Converter = new LandMoveFlowConverter(settingVM);

            // 코드 데이터 가져오기
            GetSidoCodeList();
            GetJimokCodeDictionary();
            GetReasonCodeDictionary();
            GetActionCodeDictionary();

            //테이블명 가져오기
            GetTableNameLandMoveInfo();//메인            
            GetTableNameUserHist();//사용자로그
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }


        [RelayCommand]
        public async Task OnSearch()//private => public//async 추가
        {
            if (Bobn == "" && Bubn == "")
            {
                ShowNoInputPopup();

                return;// 본번 & 부번 데이터가 없으면 이후 처리 중단
            }

            // [1]. PNU 구성
            // [2]. 그리드 데이터 조회(검색)
            SearchLandMoveData();

            // [3]. GridDataSource가 비어있는지 확인
            if (GridDataSource == null || !GridDataSource.Any())
            {
                IsFlowData = false;//표시 설정 [이동정리목록 내보내기(엑셀), 다이어그램 내보내기(Pdf, Jpg, Png)]

                ShowNoDataPopup(false);

                // 다이어그램 초기화 (null이 가장 안전)
                //XAML에 바인딩 되어 있으므로 XML 다이어그램 화면도 초기화됨//
                LandMoveFlowData = null;

                return; // 데이터가 없으면 이후 처리 중단
            }

            // 데이터 준비 및 명칭 변환 전
            IsDiagramReady = false;
            //--------------------------------------------------------
            //팝업 띄우기 (팝업 윈도우 새로 생성) - // 4. 그리드 데이터 처리
            //await DrawDiagramAsync();//async 추가 필수
            BusPopup = new BusyWindow();
            BusPopup.Owner = Application.Current.MainWindow;
            BusPopup.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Owner의 중앙에 뜨게 설정
            BusPopup.Show();

            try
            {
                // XML 구성 등 시간이 오래 걸릴 수 있는 작업은 비동기로 처리
                await Task.Run(() =>
                {
                    // [4]. 그리드 데이터 처리
                    System.Threading.Thread.Sleep(2000); // 작업 지연 실험 (문제 원인 판별용)
                    UpdateFlowXml();
                });
            }
            finally
            {
                // 검색필지 정보 User_Hist 테이블에 추가
                var actCd = Converter.GetCodeValue(6, "검색");
                DBService.InsertUserHist(CurrentPnu, actCd);


                // [5]. UI 업데이트가 완료될 수 있도록 한 프레임 "더 기다림"
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    BusPopup.Close();           // BusyWindow**를 여기서** 닫는다!
                    IsDiagramReady = true; // 필요할 경우 UI 표시 활성화(Visibility 등)
                }, DispatcherPriority.Background);

                //실제 데이터가 없는 경우가 아니라, '지목변경'건만 존재하는데, 체크되어 있지 않은 경우에 표시
                if (GridDataSource == null || !GridDataSource.Any())//표시 설정 [이동정리목록 내보내기(엑셀), 다이어그램 내보내기(Pdf, Jpg, Png)]
                {
                    ShowNoDataPopup(true);
                }
            }
            //--------------------------------------------------------
        }

        private void ShowNoDataPopup(bool exitsJimokChg)
        {
            if ( !exitsJimokChg )
            {
                // WPF MessageBox 사용
                MessageBox.Show(Application.Current.MainWindow,
                    "조회된 데이터가 없습니다.",
                    "알림",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                // WPF MessageBox 사용
                MessageBox.Show(Application.Current.MainWindow,
                    "조회된 데이터가 없습니다.(지목변경 자료 존재)",
                    "알림",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }

            // 또는 커스텀 팝업을 사용하는 경우
            // var popup = new CustomPopup("조회된 데이터가 없습니다.");
            // popup.ShowDialog();
        }

        private void ShowNoInputPopup()
        {
            // WPF MessageBox 사용
            MessageBox.Show(
                "지번정보를 입력해주세요.",
                "알림",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            // 또는 커스텀 팝업을 사용하는 경우
            // var popup = new CustomPopup("조회된 데이터가 없습니다.");
            // popup.ShowDialog();
        }

        [RelayCommand]
        private void OnEnter()
        {
            OnSearch();
        }


        [RelayCommand]
        private void OnFlowDataDoubleClick(MouseButtonEventArgs arg)
        {
            var currentItem = (arg.Source as GridControl)?.CurrentItem;
            var item = currentItem as LandMoveInfo;


            System.Diagnostics.Debug.WriteLine($"행이 더블클릭됨: {item}");
        }

        [RelayCommand]
        //CommandParameter로 선택된 행의 데이터 받기
        private void OnRowClick(LandMoveInfoCategory cate)
        {
            if (cate != null)
            {
                // this는 이미 LandMoveFlowViewModel 인스턴스이므로 DataContext 불필요
                var detailPage = new LandMoveDetailPage(this, GridDataSource.ToList(), cate.regDt, cate.rsn);//생성자에 값 전달
                Window window = new Window
                {
                    Content = detailPage,
                    Title = "토지이동흐름도 일자/종목별 필지정보(상세)",
                    Width = 800,
                    Height = 320,
                    Owner = Application.Current.MainWindow
                };
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;//부모화면의 가운데 표시
                window.ShowDialog();//부모화면 제어 불가
            }
        }

        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnSettingColor()
        {
            var page = new LandMoveSettingPage(this);
            Window window = new Window
            {
                Content = page,
                Title = "토지이동흐름도 다이어그램 색상 설정",
                Width = 340,
                Height = 240,
                Owner = Application.Current.MainWindow,

                //[닫기]버튼만 남긴다.
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize
            };
            window.WindowStartupLocation = WindowStartupLocation.Manual;//부모화면의 임의위치로 지정

            // 부모(Window)의 실제 스크린 위치 좌표 구하기
            var parent = Application.Current.MainWindow;
            // 부모창의 스크린 좌표를 가져오기
            var parentTopLeft = parent.PointToScreen(new System.Windows.Point(0, 0));

            // 부모창의 우측 상단 위치에 새 창을 붙이기
            window.Left = parentTopLeft.X + parent.ActualWidth - window.Width - 10;
            window.Top = parentTopLeft.Y + 100;

            window.ShowDialog();//부모화면 제어 불가
        }

        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnLoadCsvFileData()
        {
            var page = new CsvUploaderPage(this);
            Window window = new Window
            {
                Content = page,
                Title = "토지이동흐름도 자료(CSV) 업로드",
                Width = 800,
                Height = 350,
                Owner = Application.Current.MainWindow,

                //[닫기]버튼만 남긴다.
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize
            };
            //window.WindowStartupLocation = WindowStartupLocation.Manual;//부모화면의 임의위치로 지정

            //// 부모(Window)의 실제 스크린 위치 좌표 구하기
            //var parent = Application.Current.MainWindow;
            //// 부모창의 스크린 좌표를 가져오기
            //var parentTopLeft = parent.PointToScreen(new System.Windows.Point(0, 0));

            //// 부모창의 우측 상단 위치에 새 창을 붙이기
            //window.Left = parentTopLeft.X + parent.ActualWidth - window.Width - 10;
            //window.Top = parentTopLeft.Y + 100;

            window.ShowDialog();//부모화면 제어 불가
        }


        [RelayCommand]
        //검색필지내역 화면으로 이동//
        private void OnQuery()
        {
            var page = new LandMoveQueryPage();
            Window window = new Window
            {
                Content = page,
                Title = "검색필지내역",
                Width = 400,
                Height = 300,
                Owner = Application.Current.MainWindow,

                //[닫기]버튼만 남긴다.
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize
            };

            window.ShowDialog();//부모화면 제어 불가
        }

        partial void OnSelectedUmdChanged(SidoCode value)
        {
            if (value == null)
            {
                RiList = new List<SidoCode>(); // 빈 리스트 등 초기화
                SelectedRi = null;
                return;
            }
            RiList = GenerateRiList(value!.umdCd);
            SelectedRi = RiList.FirstOrDefault()!;
        }


        private void GetSidoCodeList()
        {
            List<SidoCode> list = DBService.ListSidoCode(GlobalDataManager.Instance.loginUser.areaCd);
            GlobalDataManager.Instance.sidoCodeList = list;
            UmdList = GenerateUmdList();
            SelectedUmd = UmdList.FirstOrDefault()!;
        }


        private List<SidoCode> GenerateUmdList()
        {
            List<SidoCode> list = null;
            try
            {
                list = GlobalDataManager.Instance.sidoCodeList
                    .Where(x => x.riCd == "00" && x.umdNm != "").
                    OrderBy(x => x.sidoSggCd + x.umdCd + x.riCd).ToList();
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
            }
            return list;
        }

        private List<SidoCode> GenerateRiList(string umdCd)
        {
            //Vit.G//
            Bobn = string.Empty;
            Bubn = string.Empty;


            List<SidoCode> list = null;
            try
            {
                list = GlobalDataManager.Instance.sidoCodeList
                    .Where(x => x.umdCd == umdCd && x.riCd != "00")
                    .OrderBy(x => x.sidoSggCd + x.umdCd + x.riCd).ToList();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex.Message);
            }
            return list;
        }

        private void GetJimokCodeDictionary()
        {
            Dictionary<string, string> dict = DBService.GetJimokDictionary("CD01");
            GlobalDataManager.Instance.JimokCode = dict;
        }


        private void GetReasonCodeDictionary()
        {
            Dictionary<string, string> dict = DBService.GetReasonDictionary("CD02");

            //'분할' > '지목변경'
            //'지목변경' > '합병'으로 변경
            // 사용자가 정의한 우선순위 리스트 (변할 수 있음)
            var desiredOrder = new List<string> { "10", "20", "40", "30" };
            // 리스트로 변환
            var list = dict.ToList();
            // "30"과 "40"의 순서 바꾸기
            int idx30 = list.FindIndex(x => x.Key == "30");
            int idx40 = list.FindIndex(x => x.Key == "40");

            // swap
            var temp = list[idx30];
            list[idx30] = list[idx40];
            list[idx40] = temp;

            GlobalDataManager.Instance.ReasonCode = dict;
        }

        private void GetActionCodeDictionary()
        {
            Dictionary<string, string> dict = DBService.GetActionCodeDictionary("CD03");
            GlobalDataManager.Instance.ActionCode = dict;
        }

        private void GetTableNameLandMoveInfo()
        {
            GlobalDataManager.Instance.TB_LandMoveInfo = $"landmove_info_{GlobalDataManager.Instance.loginUser.areaCd}";
        }
        private void GetTableNameUserHist()
        {
            GlobalDataManager.Instance.TB_UserHistory = $"user_hist";
        }


        partial void OnJimokChgChanged(bool value)
        {
            // 검색 로직 실행
            OnSearch();
        }
        
        partial void OnPortraitChanged(bool value)
        {
            // 검색 로직 실행
            OnSearch();
        }
        partial void OnIsOwnNameChanged(bool value)
        {
            // 검색 로직 실행
            OnSearch();
        }

        partial void OnIsJimokChanged(bool value)
        {
            // 검색 로직 실행
            OnSearch();
        }
        partial void OnIsAreaChanged(bool value)
        {
            // 검색 로직 실행
            OnSearch();
        }


        private string BuildPnu()
        {
            string umdCd = SelectedUmd.umdCd;
            string riCd = SelectedRi == null ? "00" : SelectedRi.riCd;
            string gbn = IsSan == true ? "2" : "1";
            string bobn = Bobn != null ? Bobn.PadLeft(4, '0') : "0000";
            string bubn = Bubn != null ? Bubn.PadLeft(4, '0') : "0000";

            string pnu = SelectedUmd.sidoSggCd + umdCd + riCd + gbn + bobn + bubn;

            try
            {
                if (pnu.Length != 19)
                {
                    _logger.Debug($"지번 오류 {pnu}");
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"{pnu} : {ex.Message}");
            }
            return pnu;
        }

        private void SearchLandMoveData()
        {
            CurrentPnu = BuildPnu(); // 기존 방식
            GridDataSource = DBService.ListLandMoveHistory(CurrentPnu);
            GridCategoryDataSource = DBService.ListLandMoveCategory(CurrentPnu);
        }

        private void UpdateFlowXml()
        {
            Converter = new LandMoveFlowConverter(SettingVM);

            var filteredList = GridDataSource;
            var categoryList = GridCategoryDataSource;

            //코드=>명칭 변경 작업이 오래 걸려 AnalyzeData 내부에서 여기로 가져옴//(BusyWindow가 일찍 사라지기에 전처리)
            GridDataSource = Converter.ChangeCodeToNameBatch(GridDataSource);

            if (!JimokChg)
            {
                filteredList = GridDataSource.Where(item => item.rsn != "지목변경").ToList();
                GridDataSource = filteredList;
                categoryList = GridCategoryDataSource.Where(item => item.rsn != "40").ToList();
                GridCategoryDataSource = categoryList;
            }

            ////Vit.G//TEST// 3th argu //
            if (filteredList.Count > 0)
            {
                IsFlowData = true;//표시 설정 [이동정리목록 내보내기(엑셀), 다이어그램 내보내기(Pdf, Jpg, Png)]

                CurrentGSeq = filteredList[filteredList.Count - 1].gSeq;//상세화면에서 insert 시 사용될 예정//

                //251118//
                ProcessDiagramLandMoveFlow(Converter.Run(filteredList, this, categoryList, CurrentPnu));
                //XDocument rtnXml = Converter.Run(filteredList, this, categoryList, CurrentPnu);
                //CurrentPnuNm = Converter.GetJibun(CurrentPnu, 2);

                //// ... 이하 xml 스트림 처리
                //string str = rtnXml.ToString();

                //byte[] xmlBytes;
                //using (var ms = new MemoryStream())
                //{
                //    rtnXml.Save(ms);
                //    xmlBytes = ms.ToArray();
                //}
                ////251118//[Advice]
                //LandMoveFlowData = new MemoryStream(xmlBytes);
                ////using (var stream = new MemoryStream(xmlBytes))
                ////{
                ////    LandMoveFlowData = stream;
                ////}
            }//if (filteredList.Count > 0)
            else
            {
                IsFlowData = false;//표시 설정 [이동정리목록 내보내기(엑셀), 다이어그램 내보내기(Pdf, Jpg, Png)]
            }
        }

        #region XML 관련

        /*
        버튼 클릭
            ↓
        XAML: Command="{Binding PrintCommand}"
            ↓
        ViewModel: OnPrint() 메서드 실행 (자동 생성된 PrintCommand)
            ↓
        ViewModel: Messenger로 PrintDiagramMessage 전송
            ↓
        View: Messenger 수신 → OnPrintDiagram() 실행
            ↓
        View: LmfControl.QuickPrint() 호출       
         */

        //Messenger 패턴 사용 (MVVM 유지, 권장)


        //------------------------------------------------------------
        //해결 원칙: 메시지 전송과 수신 역할 명확히 분리
        //ViewModel
        //사용자의 행동(버튼 클릭 → RelayCommand → SaveFileDialog → 메시지 전송)만 담당!
        //ExportDiagramMessage 등을 "보내기만" 하고, 메시지 "수신(받기)"는 하면 안 됨!
        //즉, ViewModel은 반드시 Register를 안해야 안전. (절대 Register<ExportDiagramMessage> XX)
        //Page(LandMoveFlowPage 등)
        //ExportDiagramMessage 등 메시지를 "수신(Register)"해서 실제 Export 작업만 하면 됨!
        //수신 시 OnExportDiagram(string filePath, Format format)처럼 파일 저장 코드를 실행
        //Page는 ViewModel의 Export 메서드를 다시 호출하면 절대 안 되고, 사내에서 Export 작업만 담당
        //------------------------------------------------------------

        //ViewModel에서는 Message 보내기만 담당
        [RelayCommand] //[RelayCommand] 속성이 존재해야 Command가 생성됩니다. On 접두사 필수!!!
        public void OnPrint()
        {
            WeakReferenceMessenger.Default.Send(new PrintDiagramMessage());
        }

        [RelayCommand]
        public void OnPrintPreview()
        {
            WeakReferenceMessenger.Default.Send(new PrintPreviewDiagramMessage());
        }


        //[오류발생-무한루프]
        //1. MainWindow 버튼 클릭 → MainWindowViewModel.ExportPdf() 실행
        //2. MainWindowViewModel에서 ExportPdfDiagramMessage 전송
        //3. LandMoveFlowViewModel의 OnExportPdf() 실행(메시지 수신)
        //4. OnExportPdf()에서 또다시 ExportPdfDiagramMessage 전송 ❌
        //5. 3번으로 다시 돌아감 → 무한 루프!
        [RelayCommand]
        public void OnExportPdf()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF 파일|*.pdf",
                Title = "PDF로 저장",
                FileName = $"다이어그램(Pdf)_{CurrentPnuNm}_{DateTime.Now.ToString("yyyy-MM-dd")}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportDiagramFormat.Pdf
                });
            }
        }

        [RelayCommand]
        public void OnExportJpg()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "JPG 파일|*.jpg",
                Title = "JPG로 저장",
                FileName = $"다이어그램(Jpg)_{CurrentPnuNm}_{DateTime.Now.ToString("yyyy-MM-dd")}.jpg"
            };

            if (saveDialog.ShowDialog() == true)
            {
                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportDiagramFormat.Jpg
                });
            }
        }

        [RelayCommand]
        public void OnExportPng()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG 파일|*.png",
                Title = "PNG로 저장",
                FileName = $"다이어그램(Png)_{CurrentPnuNm}_{DateTime.Now.ToString("yyyy-MM-dd")}.png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportDiagramFormat.Png
                });
            }
        }


        //[주의사항] 테두리 설정이 어렵다. (데이터 행까지만 하고 싶었음)
        //DevExpress WPF GridControl의 Export 기능만으로는 "UsedRange(데이터가 있는 범위까지만)"에만 테두리를 설정하는 기능이 직접적으로 제공되지 않습니다.
        //1. NuGet에서 ClosedXML 설치
        //  Install-Package ClosedXML
        //2. 코드 가이드(DevExpress Export → ClosedXML 후처리)
        /*
using ClosedXML.Excel;
using System.IO;

// --- 1) DevExpress TableView Export ---
string exportPath = "저장할경로\\file.xlsx";
var options = new DevExpress.XtraPrinting.XlsxExportOptionsEx() { SheetName = "Sheet1" };
((DevExpress.Xpf.Grid.TableView)FlowDataGrid.View).ExportToXlsx(exportPath, options);

// --- 2) ClosedXML로 UsedRange 까지만 테두리 ---
using (var wb = new XLWorkbook(exportPath))
{
    var ws = wb.Worksheet(1);
    int lastRow = ws.LastRowUsed().RowNumber();
    int lastCol = ws.LastColumnUsed().ColumnNumber();

    // UsedRange에만 테두리(Outside + Inside)
    var range = ws.Range(1, 1, lastRow, lastCol);
    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

    wb.Save();
}*/
        [RelayCommand]
        public void OnExportGrid()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Excel (2010) (.xlsx)|*.xlsx|Excel (2003)(.xls)|*.xls";
            saveDialog.FileName = $"이동정리목록(엑셀)_{CurrentPnuNm}_{DateTime.Now.ToString("yyyy-MM-dd")}";
            if (saveDialog.ShowDialog() == true)
            {
                string path = saveDialog.FileName;
                // 파일 경로 등 정보 선택 후
                Messenger.Default.Send(new RequestExportGridMessage(path, CurrentPnuNm));
            }
        }
        #endregion


        //251117//NotUsed//
        #region 읍면동, 리 코드값 처리
        //private object _editValueUmd;
        //public object EditValueUmd
        //{
        //    get => _editValueUmd;
        //    set
        //    {
        //        if (SetProperty(ref _editValueUmd, value))
        //        {
        //            SelectedUmd = null; 
        //            if (value != null)
        //            {
        //                // 코드값과 동일한 umdCd를 가진 항목 검색
        //                var item = UmdList?.FirstOrDefault(x => x.umdCd.ToString() == value.ToString());
        //                SelectedUmd = item != null ? item : null/*없으면 선택 해제*/;
        //            }
        //        }
        //    }
        //}
        //
        //private object _editValueRi;
        //public object EditValueRi
        //{
        //    get => _editValueRi;
        //    set
        //    {
        //        if (SetProperty(ref _editValueRi, value))
        //        {
        //            SelectedRi = null;
        //            if (value != null)
        //            {
        //                // 코드값과 동일한 umdCd를 가진 항목 검색
        //                var item = RiList?.FirstOrDefault(x => x.riCd.ToString() == value.ToString());
        //                SelectedRi = item != null ? item : null/*없으면 선택 해제*/;
        //            }
        //        }
        //    }
        //}
        #endregion



        #region 대기 팝업
        //private async Task DrawDiagramAsync()
        //{
        //    var busy = new BusyWindow();
        //    busy.Show();

        //    await Task.Run(() => {
        //        // 긴 다이어그램 생성/처리 작업

        //        // 4. 그리드 데이터 처리
        //        UpdateFlowXml();
        //    });

        //    busy.Close();
        //}
        #endregion


        #region 다이어그램 다시그리기
        public void ProcessDiagramLandMoveFlow(XDocument rtnXml)
        {
            // ... 이하 xml 스트림 처리
            string str = rtnXml.ToString();

            byte[] xmlBytes;
            using (var ms = new MemoryStream())
            {
                rtnXml.Save(ms);
                xmlBytes = ms.ToArray();
            }
            //[Advice] no using
            LandMoveFlowData = new MemoryStream(xmlBytes);

            //필지 주소(for Saving)
            CurrentPnuNm = Converter.GetJibun(CurrentPnu, 2);
        }
        #endregion


    }
}
