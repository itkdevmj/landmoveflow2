using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMFS.Messages
{
    // 인쇄 메시지
    public class PrintDiagramMessage { }
    // 인쇄 미리보기 메시지
    public class PrintPreviewDiagramMessage { }


    // MainWindow → LandMoveFlowViewModel 메시지
    public class ExportPdfDiagramMessage { }
    public class ExportJpgDiagramMessage { }
    public class ExportPngDiagramMessage { }

    // LandMoveFlowViewModel → LandMoveFlowPage 메시지
    public class ExportDiagramMessage
    {
        public string FilePath { get; set; }
        public ExportFormat Format { get; set; }
    }

    // Export 형식
    public enum ExportFormat
    {
        Pdf,
        Jpg,
        Png
    }
}
