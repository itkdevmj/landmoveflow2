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
    public partial class LandMoveFlowPage : Page, IRecipient<LoadXmlMessage>
    {
        public LandMoveFlowPage()
        {
            InitializeComponent();
            DataContext = new LandMoveFlowViewModel();
            
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

    }
}
