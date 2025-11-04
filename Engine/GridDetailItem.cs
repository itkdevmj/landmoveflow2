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

    [ObservableProperty] private bool _isNewRow;
    [ObservableProperty] private string _bfPnu;
    [ObservableProperty] private string _afPnu;
    [ObservableProperty] private string _bfJimok;
    [ObservableProperty] private string _bfArea;
    [ObservableProperty] private string _afJimok;
    [ObservableProperty] private string _afArea;
    [ObservableProperty] private string _ownName;

    #endregion

    #region Constructor

    public GridDetailItem()
    {
        _isNewRow = false;
        _bfPnu = string.Empty;
        _afPnu = string.Empty;
        _bfJimok = string.Empty;
        _bfArea = string.Empty;
        _afJimok = string.Empty;
        _afArea = string.Empty;
        _ownName = string.Empty;

        _logger.Debug("GridDetailItem 인스턴스 생성");
    }

    #endregion

    #region Methods

    /// <summary>
    /// 유효성 검사
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(BfPnu))  // ← 자동 생성된 Property 사용
        {
            _logger.Warn("이동전 PNU가 비어있습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(AfPnu))
        {
            _logger.Warn("이동후 PNU가 비어있습니다.");
            return false;
        }

        return true;
    }

    #endregion

}