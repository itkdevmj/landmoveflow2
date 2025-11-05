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
    [ObservableProperty] private string _bfPnu;
    [ObservableProperty] private string _afPnu;
    [ObservableProperty] private string _bfJimok;
    [ObservableProperty] private double _bfArea;
    [ObservableProperty] private string _afJimok;
    [ObservableProperty] private double _afArea;
    [ObservableProperty] private string _ownName;

    private bool _isTracking = false; // 추적 활성화 플래그

    #endregion

    #region Constructor

    public GridDetailItem()
    {
        _isNewRow = false;
        IsModified = false;
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
    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // IsNewRow나 IsModified 자체의 변경은 무시
        if (e.PropertyName == nameof(IsNewRow) ||
            e.PropertyName == nameof(IsModified) ||
            !_isTracking)
            return;

        // 다른 속성이 변경되면 IsModified = true
        IsModified = true;
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

    public static GridDetailItem FromLandMoveInfo(LandMoveInfo info)
    {
        return new GridDetailItem
        {
            IsNewRow = false, // DB에서 온 데이터는 기존 행
            IsModified = false,
            BfPnu = info.bfPnu ?? "",
            AfPnu = info.afPnu ?? "",
            BfJimok = info.bfJimok ?? "",
            BfArea = info.bfArea,
            AfJimok = info.afJimok ?? "",
            AfArea = info.afArea,
            OwnName = info.ownName ?? ""
        };
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