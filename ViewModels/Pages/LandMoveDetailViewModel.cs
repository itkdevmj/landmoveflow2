using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
using DevExpress.Mvvm;
using DevExpress.Utils.About;
using DevExpress.Xpf.Grid;
using DevExpress.XtraScheduler.Native;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Models;
using LMFS.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveDetailViewModel : ObservableObject
    {
        public static readonly Logger _logger = LogManager.GetCurrentClassLogger();


        #region 기능설명 가이드        
        /*
        1. 기존 행들은 편집 불가, 마지막 컬럼엔 '추가'버튼은 숨기고 빈 컬럼만 표시        
        View (XAML > GridControl.Columns)
        기존 행(= IsNewRow==false, IsModified==false)은 CellTemplate의 Button을 숨김 처리
        AllowEditing 속성을 TableView 전체 또는 기존행엔 False로 설정

        2. 이미 구성된 행들(기존 데이터)는 '추가' 버튼 불필요
        위 XAML에서 IsNewRow==false면 버튼 미노출로 처리됨 (위와 동일)

        3. 우클릭 시 '필지 추가' 팝업/버튼 노출
        View XAML - GridControl 이벤트에 ContextMenu 추가
        ViewModel - AddRowCommand가 새 행(맨 마지막, IsNewRow==true)을 추가

        4. '필지 추가'로 마지막에 새 행 추가
        ViewModel (LandMoveDetailViewModel.cs)의 OnAddRow 구현됨
        추가 시 IsNewRow = true이고, AllowEditing True로 바뀌어야 하며, 그외 기존 행은 AllowEditing = false

        5. 추가된 마지막 행은 '추가'버튼 표시
        이미 XAML의 Button Visibility에서 IsNewRow==true일 때만 표시

        6. '추가'버튼 누르면 DB에서 필지 조회
        ConfirmAddCommand 커맨드를 ViewModel에 구현해야 함(아직 없음)
        Button.Command로 ConfirmAddCommand 바인딩
        ConfirmAddCommand에서 팝업 또는 Dialog로 필요값 입력받아 DB조회 후 갱신(미구현 시 MessageBox/Toast + 저장버튼 활성 처리 로 임시 구현)

        7. 조회된 데이터가 없다면, 상단 우측 '변경사항 저장' 버튼 표시
        ConfirmAddCommand/DB조회에서 결과 없을 시, IsSaveButtonVisible = true 처리

        8. '변경사항 저장' 수행 후 버튼 미표시
        ViewModel의 OnSave가 저장 후 IsSaveButtonVisible = false 처리
         */
        #endregion


        // (UI 바인딩과 동적 추가/삭제에 최적)
        [ObservableProperty] private ObservableCollection<GridDetailItem> _gridDetailDataSource;
        [ObservableProperty] private string _labelRegDt;
        [ObservableProperty] private string _labelRsn;

        [ObservableProperty] private int _gSeq;
        [ObservableProperty] private int _idx;
        [ObservableProperty] private string _regDt;
        [ObservableProperty] private string _rsn;
        [ObservableProperty] private string _bfPnu;
        [ObservableProperty] private string _afPnu;
        [ObservableProperty] private string _ownName;

        [ObservableProperty] private string _bfJibun;
        [ObservableProperty] private string _afJibun;
        [ObservableProperty] private string _bfJimok;
        [ObservableProperty] private string _afJimok;
        // 저장 버튼 표시 여부 (처음에는 false)
        [ObservableProperty] private bool _isSaveButtonVisible = false;
        [ObservableProperty] private bool _isTracking = false;

        //[필지 추가 관련]
        [ObservableProperty] private SidoCode _selectedUmd;
        [ObservableProperty] private SidoCode _selectedRi;
        [ObservableProperty] private bool _isSan;
        [ObservableProperty] private string _bobn;
        [ObservableProperty] private string _bubn;
        [ObservableProperty] private List<LandMoveInfo> _gridDataSource;

        [ObservableProperty] private LandMoveFlowConverter _converter;

        public LandMoveFlowViewModel FlowVM { get; }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public ICommand RowDoubleClickCommand { get; private set; }
        public ICommand AddRowCommand { get; private set; }
        public ICommand ConfirmAddCommand { get; }
        // 저장 Command 추가
        public ICommand SaveChangesCommand { get; private set; }


        // 1. 생성자에 상위 VM을 파라미터로 받음
        public LandMoveDetailViewModel(LandMoveFlowViewModel parent)
        {
            FlowVM = parent;

            _converter = FlowVM.Converter;

            // 필요시 바로 복사
            this.SelectedUmd = parent.SelectedUmd;
            this.SelectedRi = parent.SelectedRi;
            // ... 등등


            CloseCommand = new RelayCommand(ExecuteClose);
            RowDoubleClickCommand = new DelegateCommand<MouseButtonEventArgs>(OnRowDoubleClick);
            AddRowCommand = new DelegateCommand(OnAddRow);

            ConfirmAddCommand = new DelegateCommand<GridDetailItem>(OnConfirmAdd);

            // 저장 Command 초기화
            SaveChangesCommand = new DelegateCommand(OnSave);
        }

        private void ExecuteClose()
        {
            CloseAction?.Invoke();
        }

        // GridDetailItem.cs - PropertyChanged 추적 활성화//새 행 추가 시 변경 추적 활성화
        public void EnableTracking()
        {
            IsTracking = true;
        }

        public void FilterDetail(List<LandMoveInfo> sourceList, string regDt, string rsn)
        {
            var filtered = sourceList
                .Where(x => x.regDt == regDt && x.rsn == rsn)
                .ToList();

            if(filtered.Count == 0)
            {
                _logger.Debug($"필터 오류 {regDt} {rsn}"); 
                //[TODO]
            }

            GSeq = filtered[filtered.Count-1].gSeq;
            Idx = filtered[filtered.Count - 1].idx + 1;
            DateTime parsedDate = DateTime.ParseExact(regDt, "yyyyMMdd", null);
            RegDt = parsedDate.ToString("yyyy-MM-dd");
            Rsn = rsn;
            LabelRegDt = $"정리일자 : {RegDt}";
            LabelRsn = $"이동종목 : {rsn}";

            //기존
            //GridDetailDataSource = filtered;
            //GridDetailItem으로 변환하여 할당
            //selector는 TSource만 받아야 하지, 추가 인수(regDt, rsn)는 받을 수 없습니다.
            //람다를 사용해서 필요한 인수를 바인딩하세요.
            GridDetailDataSource = new ObservableCollection<GridDetailItem>(
                filtered.Select(item => GridDetailItem.FromLandMoveInfo(item, RegDt, Rsn))
            );


            // 기존 데이터는 추적 비활성화 (저장 버튼 표시 안 함)
            int index = 0;
            foreach (var item in GridDetailDataSource)
            {
                item.PropertyChanged += OnItemPropertyChanged;

                if (index == 0)
                {
                    if (Rsn == "합병")
                    {
                        AfJibun = item.AfJibun ?? "";
                    }
                    else if(Rsn == "분할")
                    {
                        BfJibun = item.BfJibun ?? "";
                    }


                    BfJimok = item.BfJimok;
                    AfJimok = item.AfJimok;
                    OwnName = item.OwnName;
                }

                index++;
            }

            // 저장 버튼 초기 숨김
            IsSaveButtonVisible = false;
        }

        private void OnRowDoubleClick(MouseButtonEventArgs e)
        {
            // 기존 더블클릭 로직
        }

        // 항목 변경 감지
        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(GridDetailItem.IsNewRow) &&
                e.PropertyName != nameof(GridDetailItem.IsModified))
            {
                IsSaveButtonVisible = true;
            }
        }

        //그리드 우클릭 [필지 추가] 선택
        private void OnAddRow()
        {
            // 새 행 추가
            var newItem = new GridDetailItem
            {
                IsNewRow = true,
                //DB Data
                GSeq = this.GSeq,
                Idx = this.Idx,
                BfPnu = "",
                AfPnu = "",
                BfJimokCd = "",
                BfArea = 0.0,
                AfJimokCd = "",
                AfArea = 0.0,
                OwnName = this.OwnName,//[데이터 자동채움]
                //Input Data
                BfJibun = (Rsn == "분할" ? this.BfJibun : ""),//[데이터 자동채움]
                AfJibun = (Rsn == "합병" ? this.AfJibun : ""),//[데이터 자동채움]
                BfJimok = this.BfJimok,//[데이터 자동채움]
                AfJimok = this.AfJimok//[데이터 자동채움]
            };

            //
            newItem.PropertyChanged += OnItemPropertyChanged;
            GridDetailDataSource.Add(newItem);

            // 기존행들은
            foreach (var item in GridDetailDataSource)
            {
                if (!item.IsNewRow)
                {
                    item.AllowEditing = false;
                }
            }

            newItem.EnableTracking(); // 추적 활성화

            // 필지 추가 시 저장 버튼 표시
            //251111//IsSaveButtonVisible = true;
        }

        //그리드 마지막 행 - 마지막 컬럼 [추가] 선택
        //항상 GridDetailDataSource의 마지막 행만 다룬다면
        //var lastItem = GridDetailDataSource.LastOrDefault();
        private void OnConfirmAdd(GridDetailItem item)
        {
            // 예) '이동전', '이동후' 등 필수입력 체크
            if (string.IsNullOrWhiteSpace(item.BfJibun) || string.IsNullOrWhiteSpace(item.AfJibun))
            {
                MessageBox.Show(Application.Current.MainWindow, "이동전, 이동후 정보를 모두 입력해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;            
            }
            // 여러 값 동시에 검사 필요시
            //if (new[] { item.BfPnu, item.AfPnu, item.BfJimok, item.AfJimok }.Any(string.IsNullOrWhiteSpace))
            //{
            //    MessageBox.Show("모든 필수 항목을 입력해주세요.", ...);
            //    return;
            //}


            //[조회할 필지 정보]
            // DB 조회 로직 수행
            var jibun = Rsn == "합병" ? item.BfJibun : item.AfJibun;
            SearchLandMoveDetailData(jibun);

            // 3. GridDataSource가 비어있는지 확인
            if (GridDataSource == null || !GridDataSource.Any())
            {
                IsSaveButtonVisible = true; // 조회 결과 부재 시 저장 버튼 노출
                MessageBox.Show(Application.Current.MainWindow, "현재 데이터베이스에 필지 정보 없습니다.\r\n입력 필지는 저장 가능합니다.", "알림");

                //------------------------------------------
                //[추가할 필지 정보]
                //------------------------------------------
                item.RegDt = RegDt;
                item.Rsn = Rsn;
                if (Rsn == "합병")
                {
                    item.BfPnu = DetailSearchPnu(jibun);
                    item.AfPnu = DetailSearchPnu(item.AfJibun);
                }
                else if (Rsn == "분할")
                {
                    item.BfPnu = DetailSearchPnu(item.BfJibun);
                    item.AfPnu = DetailSearchPnu(jibun);
                }
                else
                {
                    item.BfPnu = DetailSearchPnu(item.BfJibun);
                    item.AfPnu = BfPnu;
                }

                item.BfJimokCd = Converter.GetCodeValue(4, item.BfJimok);
                item.AfJimokCd = Converter.GetCodeValue(4, item.AfJimok);
            }
            else
            {
                IsSaveButtonVisible = false; // 조회 결과 부재 시 저장 버튼 노출

                var message = "입력한 필지는 다른 그룹에 존재합니다. \r\n검색화면에서 해당 필지로 조회하시겠습니까? (예:현재 화면 종료됨)";
                var result = MessageBox.Show(Application.Current.MainWindow, message, "알림", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    // 검색 로직 실행
                    FlowVM.IsSan = IsSan;
                    FlowVM.Bobn = Bobn;
                    FlowVM.Bubn = Bubn;
                    FlowVM.OnSearch();

                    CloseAction?.Invoke(); // Yes일 때만 닫기!
                }
            }
        }


        // 저장 메서드 구현
        private void OnSave()
        {
            try
            {
                bool hasChanges = false;
                int newRow = 0;

                //[TODO] 여러 행 추가 후 '변경사항 저장'을 할 때 필요
                foreach (var item in GridDetailDataSource)
                //if (GridDetailDataSource.Count > 0)
                {
                    //var item = GridDetailDataSource[GridDetailDataSource.Count - 1];
                    if (item.IsNewRow)
                    {
                        if (!item.Validate())
                        {
                            MessageBox.Show(Application.Current.MainWindow, "유효성 검사 실패\n필수 항목을 입력해주세요.", "알림",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        newRow++;
                        
                        // 신규 추가
                        SaveToDatabase(item);
                        item.IsNewRow = false; // 저장 후 플래그 변경
                        hasChanges = true;
                    }
                    else if (item.IsModified) // GridDetailItem에 IsModified 속성이 있다면
                    {
                        // 기존 데이터 업데이트
                        UpdateDatabase(item);
                        item.IsModified = false;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    var message = "입력한 필지는 그룹처리되어 추가되었습니다.";
                    if (newRow > 1)
                        message = "입력한 필지들은 그룹처리되어 추가되었습니다.";
                    MessageBox.Show(Application.Current.MainWindow, message, "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }


                // 저장 후 버튼 숨김
                IsSaveButtonVisible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, $"저장 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveToDatabase(GridDetailItem item)
        {
            item.RegDt = RegDt.Replace("-","");
            item.Rsn = Converter.GetCodeValue(5, Rsn);
            // 실제 DB 저장 로직
            var landMoveInfo = item.ToLandMoveInfo();

            //------------------------------------------
            // (6) DB 에 <필지추가 레코드> Insert
            //------------------------------------------
            DBService.InsertLandMoveInfo(landMoveInfo);

            // 검색필지 [추가(개별)] 정보 User_Hist 테이블에 추가
            var actCd = FlowVM.Converter.GetCodeValue(6, "추가(개별)");
            DBService.InsertUserHist(FlowVM.CurrentPnu, actCd);

        }

        private void UpdateDatabase(GridDetailItem item)
        {
            // 실제 DB 업데이트 로직
            var landMoveInfo = item.ToLandMoveInfo();

            // TODO: DB Update 코드 구현
            // 예: _service.UpdateLandMoveInfo(landMoveInfo);
        }





        #region 상세화면 필지 [추가]
        private string DetailSearchPnu(string jibun)
        {
            string umdCd = SelectedUmd.umdCd;
            string riCd = SelectedRi == null ? "00" : SelectedRi.riCd;
            string gbn = "1";
            IsSan = false;
            Bobn = string.Empty;
            Bubn = string.Empty;

            if (jibun.Contains("산"))
            { 
                jibun = jibun.Replace("산", "");
                IsSan = true;
                gbn = "2";
            }
            jibun = jibun.Replace(" ", "");
            
            int idx = jibun.IndexOf("-");
            if(idx >= 0)
            {
                Bobn = idx >= 0 ? jibun.Substring(0, idx).PadLeft(4, '0') : jibun.PadLeft(4, '0'); // '-'가 없으면 원본 문자열 전체
                jibun = jibun.Substring(idx+1, jibun.Length-idx-1);
                Bubn = jibun != null ? jibun.PadLeft(4, '0') : "0000";
            }
            else
            {
                Bobn = jibun.PadLeft(4, '0'); // '-'가 없으면 원본 문자열 전체
                Bubn = "0000";
            }

            string pnu = SelectedUmd.sidoSggCd + umdCd + riCd + gbn + Bobn + Bubn;

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

        public void SearchLandMoveDetailData(string pnu)
        {            
            GridDataSource = DBService.ListLandMoveHistory(pnu);
        }
        #endregion

    }
}
