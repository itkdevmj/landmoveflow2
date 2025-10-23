using System;
using System.IO;
using System.Windows;
using DevExpress.Xpf.Core.Native;
using LMFS.ViewModels.Pages;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using LMFS.Messages;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// ProductsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveDetailPage : Page
    {
        public LandMoveDetailPage()
        {
            InitializeComponent();
            DataContext = new LandMoveDetailViewModel();            
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

        private void LandMoveDetailPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

        private void LandMoveDetailPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }        
     
    }
}
