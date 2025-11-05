using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Printing;
using DevExpress.Xpf.Printing.Native;
using DevExpress.XtraPrinting;
using LMFS.Views.Pages;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace LMFS.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Frame _frame;

        [ObservableProperty]
        private string _applicationInfo;

        #region 생성자
        public MainWindowViewModel(Frame frame)
        {
            //Title에 버전 표시
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            ApplicationInfo = @$"토지이동흐름도 관리시스템 [{version}]";

            _frame = frame;
            // 시작 페이지를 Home으로 설정
            NavigateToHome();
        }
        #endregion


        #region 파일-읽기
        
        private void LoadData()
        {
            // 파일 탐색기를 열어서 xml 파일 읽음
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                WeakReferenceMessenger.Default.Send(new Messages.LoadXmlMessage(filePath));
            }
        }
        #endregion


        #region XML 관련

        //Messenger 패턴 사용 (MVVM 유지, 권장)

        [RelayCommand] //[RelayCommand] 속성이 존재해야 Command가 생성됩니다. On 접두사 필수!!!
        private void OnPrintCommand()
        {
            //try
            //{
            //    var page = new LandMoveFlowPage();
            //    page.LmfControl.QuickPrint();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"인쇄 실패: {ex.Message}", "오류",
            //        MessageBoxButton.OK, MessageBoxImage.Error);
            //}

            WeakReferenceMessenger.Default.Send(new PrintDiagramMessage());
        }

        [RelayCommand]
        private void OnPrintPreviewCommand()
        {
            //try
            //{
            //    var page = new LandMoveFlowPage();
            //    page.LmfControl.ShowPrintPreview();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"인쇄 미리보기 실패: {ex.Message}", "오류",
            //        MessageBoxButton.OK, MessageBoxImage.Error);
            //}

            WeakReferenceMessenger.Default.Send(new PrintPreviewDiagramMessage());
        }

        [RelayCommand]
        private void OnExportPdfCommand()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF 파일|*.pdf",
                Title = "PDF로 저장",
                FileName = "diagram.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                //try
                //{
                //    var page = new LandMoveFlowPage();
                //    page.LmfControl.ExportToPdf(saveDialog.FileName);
                //    MessageBox.Show("PDF 파일로 저장되었습니다.", "성공",
                //        MessageBoxButton.OK, MessageBoxImage.Information);
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                //        MessageBoxButton.OK, MessageBoxImage.Error);
                //}

                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportFormat.Pdf
                });
            }
        }

        [RelayCommand]
        private void OnExportJpgCommand()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "JPG 파일|*.jpg",
                Title = "JPG로 저장",
                FileName = "diagram.jpg"
            };

            if (saveDialog.ShowDialog() == true)
            {
                //try
                //{
                //    var page = new LandMoveFlowPage();
                //    page.LmfControl.ExportDiagram(saveDialog.FileName);
                //    MessageBox.Show("JPG 파일로 저장되었습니다.", "성공",
                //        MessageBoxButton.OK, MessageBoxImage.Information);
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                //        MessageBoxButton.OK, MessageBoxImage.Error);
                //}

                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportFormat.Jpg
                });
            }
        }

        [RelayCommand]
        private void OnExportPngCommand()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG 파일|*.png",
                Title = "PNG로 저장",
                FileName = "diagram.png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                //try
                //{
                //    var page = new LandMoveFlowPage();
                //    page.LmfControl.ExportDiagram(saveDialog.FileName);
                //    MessageBox.Show("PNG 파일로 저장되었습니다.", "성공",
                //        MessageBoxButton.OK, MessageBoxImage.Information);
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                //        MessageBoxButton.OK, MessageBoxImage.Error);
                //}

                WeakReferenceMessenger.Default.Send(new ExportDiagramMessage
                {
                    FilePath = saveDialog.FileName,
                    Format = ExportFormat.Png
                });
            }
        }

        [RelayCommand]
        private void OnExportGridCommand()
        {
//
        }

    /*
    // 내보내기 대화상자 (사용자가 형식 선택)
    private void ExportDialog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // DevExpress의 내장 Export 대화상자 표시
            diagramControl.ExportDiagram();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }         
     */

    /*
    // 고급 Export 옵션 설정 (선택사항)
    //Export 품질과 설정을 조정하려면 다음 속성을 사용하세요:​
    // Export 설정 예제
    private void ExportWithCustomSettings_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveDialog = new SaveFileDialog
        {
            Filter = "PNG 파일|*.png",
            FileName = "diagram_high_quality.png"
        };

        if (saveDialog.ShowDialog() == true)
        {
            // 고해상도 설정
            diagramControl.ExportToImage(
                saveDialog.FileName,
                DiagramImageExportFormat.PNG,
                exportBounds: null,  // 전체 다이어그램
                dpi: 300,            // 고해상도 (기본값 96)
                scale: 1.0           // 스케일
            );
        }
    } 
    // PDF 다중 페이지 Export
    private void ExportMultiPagePdf_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveDialog = new SaveFileDialog
        {
            Filter = "PDF 파일|*.pdf",
            FileName = "diagram_multipage.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            diagramControl.PrintToPdf(saveDialog.FileName);
        }
    }        
     */
    #endregion



    [RelayCommand]
        private void NavigateToHome()
        {
            _frame.Navigate(new LandMoveFlowPage());
        }

        [RelayCommand]
        private void NavigateToLandMoveFlow()
        {
            _frame.Navigate(new LandMoveFlowPage());
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            _frame.Navigate(new SettingsPage());
        }

        [RelayCommand]
        private void NavigateToAbout()
        {
            _frame.Navigate(new AboutPage());
        }
    }



    // Message 클래스 정의
    public class PrintDiagramMessage { }

    public class PrintPreviewDiagramMessage { }

    public class ExportDiagramMessage
    {
        public string FilePath { get; set; }
        public ExportFormat Format { get; set; }
    }

    public enum ExportFormat
    {
        Pdf,
        Jpg,
        Png
    }

}
