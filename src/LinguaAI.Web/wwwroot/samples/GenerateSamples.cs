// This is a simple C# console app to generate the sample files
// Run this once to create the sample Excel and Word files

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// Generate Excel
using (var workbook = new XLWorkbook())
{
    var worksheet = workbook.Worksheets.Add("Vocabulary");

    // Headers
    worksheet.Cell(1, 1).Value = "Từ vựng";
    worksheet.Cell(1, 2).Value = "Nghĩa";
    worksheet.Cell(1, 3).Value = "Phát âm";
    worksheet.Cell(1, 4).Value = "Ví dụ";

    // Style headers
    var headerRow = worksheet.Row(1);
    headerRow.Style.Font.Bold = true;
    headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

    // Sample data
    worksheet.Cell(2, 1).Value = "안녕하세요";
    worksheet.Cell(2, 2).Value = "Xin chào";
    worksheet.Cell(2, 3).Value = "annyeonghaseyo";
    worksheet.Cell(2, 4).Value = "안녕하세요! 만나서 반갑습니다.";

    worksheet.Cell(3, 1).Value = "감사합니다";
    worksheet.Cell(3, 2).Value = "Cảm ơn";
    worksheet.Cell(3, 3).Value = "gamsahamnida";
    worksheet.Cell(3, 4).Value = "도와주셔서 감사합니다.";

    worksheet.Cell(4, 1).Value = "사랑해요";
    worksheet.Cell(4, 2).Value = "Tôi yêu bạn";
    worksheet.Cell(4, 3).Value = "saranghaeyo";
    worksheet.Cell(4, 4).Value = "엄마, 사랑해요!";

    worksheet.Cell(5, 1).Value = "맛있어요";
    worksheet.Cell(5, 2).Value = "Ngon quá";
    worksheet.Cell(5, 3).Value = "masisseoyo";
    worksheet.Cell(5, 4).Value = "이 음식은 정말 맛있어요.";

    worksheet.Cell(6, 1).Value = "괜찮아요";
    worksheet.Cell(6, 2).Value = "Không sao";
    worksheet.Cell(6, 3).Value = "gwaenchanayo";
    worksheet.Cell(6, 4).Value = "괜찮아요, 걱정 마세요.";

    worksheet.Columns().AdjustToContents();
    workbook.SaveAs("vocabulary_template.xlsx");
}

Console.WriteLine("Excel file created!");

// Generate Word
using (var document = WordprocessingDocument.Create("vocabulary_template.docx", WordprocessingDocumentType.Document))
{
    var mainPart = document.AddMainDocumentPart();
    mainPart.Document = new Document();
    var body = mainPart.Document.AppendChild(new Body());

    // Title
    var titlePara = body.AppendChild(new Paragraph());
    var titleRun = titlePara.AppendChild(new Run());
    titleRun.AppendChild(new Text("Danh sách từ vựng mẫu"));
    titleRun.RunProperties = new RunProperties(new Bold());

    // Instructions
    var instructPara = body.AppendChild(new Paragraph());
    instructPara.AppendChild(new Run(new Text("Định dạng: Từ | Nghĩa | Phát âm | Ví dụ (hoặc Từ - Nghĩa)")));

    body.AppendChild(new Paragraph());

    // Sample data
    string[] samples = {
        "안녕하세요 | Xin chào | annyeonghaseyo | 안녕하세요! 만나서 반갑습니다.",
        "감사합니다 | Cảm ơn | gamsahamnida | 도와주셔서 감사합니다.",
        "사랑해요 | Tôi yêu bạn | saranghaeyo | 엄마, 사랑해요!",
        "맛있어요 | Ngon quá | masisseoyo | 이 음식은 정말 맛있어요.",
        "괜찮아요 | Không sao | gwaenchanayo | 괜찮아요, 걱정 마세요."
    };

    foreach (var sample in samples)
    {
        var para = body.AppendChild(new Paragraph());
        para.AppendChild(new Run(new Text(sample)));
    }

    mainPart.Document.Save();
}

Console.WriteLine("Word file created!");
