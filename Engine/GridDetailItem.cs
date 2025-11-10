/*
 📊 초기 로드 (DB 데이터)
└─> FromLandMoveInfo() 
    └─> DisableTracking() 
        └─> 저장 버튼 숨김 ✓

➕ 사용자가 행 추가
└─> OnAddRow()
    └─> EnableTracking()
        └─> 사용자가 값 입력
            └─> PropertyChanged 이벤트 발생
                └─> IsModified = true
                    └─> 저장 버튼 표시 ✓

💾 저장 완료
└─> OnSave()
    └─> IsNewRow = false, IsModified = false
        └─> IsSaveButtonVisible = false
            └─> 저장 버튼 숨김 ✓
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using DevExpress.Diagram.Core.Native;
using DevExpress.Mvvm.Native;
using LMFS.Db;
using LMFS.Models;
using LMFS.Services;
using MySqlConnector;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;


namespace LMFS.Engine;


public partial class GridDetailItem : ObservableObject  // ← partial + ObservableObject 상속
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // ===========================================================
    // 개요
    // 
    // 토지이동흐름도 상세화면 필지 관리
    // ===========================================================

    #region Observable Properties

    [ObservableProperty] private bool _isNewRow;//[필지 추가]용 새로운 행
    [ObservableProperty] private bool _isModified;//새로운 행이 표시될 경우에만 [변경사항 저장] 버튼 표시하기 위함
    [ObservableProperty] private bool _allowEditing;
    [ObservableProperty] private string _regDt;//
    [ObservableProperty] private string _rsn;//
    [ObservableProperty] private string _bfPnu;
    [ObservableProperty] private string _afPnu;
    [ObservableProperty] private string _bfJimok;
    [ObservableProperty] private double _bfArea;
    [ObservableProperty] private string _afJimok;
    [ObservableProperty] private double _afArea;
    [ObservableProperty] private string _ownName;
    [ObservableProperty] private bool _isTracking = false; // 추적 활성화 플래그

    #endregion

    #region Constructor

    public GridDetailItem()
    {
        _isNewRow = false;
        _isModified = false;
        _allowEditing = false;
        _regDt = string.Empty;
        _rsn = string.Empty;
        _bfPnu = string.Empty;
        _afPnu = string.Empty;
        _bfJimok = string.Empty;
        _bfArea = 0.0;
        _afJimok = string.Empty;
        _afArea = 0.0;
        _ownName = string.Empty;

        _logger.Debug("GridDetailItem 인스턴스 생성");

        // PropertyChanged 이벤트 구독
        this.PropertyChanged += OnPropertyChanged;
    }

    #endregion

    #region Methods
        /// <summary>
    /// 변경 추적 활성화 (새 행 추가 시 사용)
    /// </summary>
    public void EnableTracking()
    {
        IsTracking = true;
        _logger.Debug("GridDetailItem 변경 추적 활성화");
    }

    /// <summary>
    /// 변경 추적 비활성화 (DB에서 로드된 기존 데이터)//DB 로드 시 추적 비활성화 (불필요한 저장 버튼 표시 방지)
    /// </summary>
    public void DisableTracking()
    {
        IsTracking = false;
        _logger.Debug("GridDetailItem 변경 추적 비활성화");
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // IsNewRow나 IsModified 자체의 변경은 무시
        if (e.PropertyName == nameof(IsNewRow) ||
            e.PropertyName == nameof(IsModified) ||
            !IsTracking)
            return;

        //[데이터 자동채움]
        if (Rsn == "지목변경" && e.PropertyName == nameof(GridDetailItem.BfPnu))
        {
            var item = sender as GridDetailItem;
            item.AfPnu = item.BfPnu;
        }


        // 다른 속성이 변경되면 IsModified = true
        IsModified = true;
        _logger.Debug($"GridDetailItem 속성 변경 감지: {e.PropertyName}");
    }


    /// <summary>
    /// 유효성 검사
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(BfPnu))  // ← 자동 생성된 Property 사용
        {
            _logger.Warn("이동전 PNU가 비어 있습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(AfPnu))
        {
            _logger.Warn("이동후 PNU가 비어 있습니다.");
            return false;
        }

        if (BfArea <= 0 || AfArea <= 0)
        {
            _logger.Warn("면적 정보가 비어 있습니다.");
            return false;
        }

        return true;
    }

    //자동으로 DisableTracking() 호출 - 기존 데이터는 추적 안 함
    public static GridDetailItem FromLandMoveInfo(LandMoveInfo info, string regDt, string rsn)
    {
        var item = new GridDetailItem
        {
            IsNewRow = false, // DB에서 온 데이터는 기존 행
            IsModified = false,
            AllowEditing = false,
            RegDt = regDt,
            Rsn = rsn,
            BfPnu = info.bfPnu ?? "",
            AfPnu = info.afPnu ?? "",
            BfJimok = info.bfJimok ?? "",
            BfArea = info.bfArea,
            AfJimok = info.afJimok ?? "",
            AfArea = info.afArea,
            OwnName = info.ownName ?? ""
        };
        Debug.WriteLine($"Row: BfPnu={item.BfPnu}, AfPnu={item.AfPnu}, IsNewRow={item.IsNewRow}");


        // DB에서 로드된 데이터는 추적 비활성화 (초기 로드 시 저장 버튼 표시 안 함)
        item.DisableTracking();

  
        return item;
    }

    // 반대 변환도 필요하면
    public LandMoveInfo ToLandMoveInfo()
    {
        return new LandMoveInfo
        {
            bfPnu = this.BfPnu,
            afPnu = this.AfPnu,
            bfJimok = this.BfJimok,
            bfArea = this.BfArea,
            afJimok = this.AfJimok,
            afArea = this.AfArea,
            ownName = this.OwnName
        };
    }

    #endregion

}