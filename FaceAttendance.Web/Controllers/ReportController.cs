// Đường dẫn: FaceAttendance.Web/Controllers/ReportController.cs
using ClosedXML.Excel;
using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FaceAttendance.Web.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;

            // Yêu cầu bắt buộc của QuestPDF: Khai báo sử dụng bản quyền miễn phí (Community)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ==========================================
        // HÀM 1: XUẤT FILE EXCEL
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            // 1. Lấy danh sách sinh viên từ Database
            var students = await _context.Students.ToListAsync();

            // 2. Khởi tạo một file Excel ảo trong bộ nhớ (Workbook)
            using var workbook = new XLWorkbook();

            // 3. Tạo một tab (Sheet) mới tên là "DanhSachSinhVien"
            var worksheet = workbook.Worksheets.Add("DanhSachSinhVien");

            // 4. Tạo dòng Tiêu đề (Header) ở dòng 1
            worksheet.Cell(1, 1).Value = "Mã Sinh Viên";
            worksheet.Cell(1, 2).Value = "Họ và Tên";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Trạng thái";

            // Tô màu nền xanh, chữ trắng cho dòng tiêu đề thêm chuyên nghiệp
            worksheet.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.BlueGray;
            worksheet.Range("A1:D1").Style.Font.FontColor = XLColor.White;
            worksheet.Range("A1:D1").Style.Font.Bold = true;

            // 5. Vòng lặp đổ dữ liệu từ Database vào các dòng tiếp theo (Bắt đầu từ dòng 2)
            int currentRow = 2;
            foreach (var stu in students)
            {
                worksheet.Cell(currentRow, 1).Value = stu.StudentID;
                worksheet.Cell(currentRow, 2).Value = stu.FullName;
                worksheet.Cell(currentRow, 3).Value = stu.Email;
                worksheet.Cell(currentRow, 4).Value = stu.IsActive ? "Đang học" : "Bảo lưu";
                currentRow++;
            }

            // Tự động căn chỉnh độ rộng các cột cho vừa với chữ
            worksheet.Columns().AdjustToContents();

            // 6. Ghi file Excel ra luồng bộ nhớ (MemoryStream) để gửi về trình duyệt
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            // 7. Trả về file cho người dùng tải xuống (MIME type chuẩn của file .xlsx)
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachSinhVien.xlsx");
        }

        // ==========================================
        // HÀM 2: XUẤT FILE PDF
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var students = await _context.Students.ToListAsync();

            // 1. Khởi tạo cấu trúc file PDF bằng QuestPDF
            var document = Document.Create(container =>
            {
                // Cấu hình trang: Khổ A4, viền 50 point
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // 2. Tạo phần Đầu trang (Header)
                    page.Header().Element(ComposeHeader);

                    // 3. Tạo phần Nội dung chứa Bảng (Content)
                    page.Content().Element(x => ComposeContent(x, students));

                    // 4. Tạo phần Chân trang (Footer) đánh số trang
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            // Ghi PDF ra mảng byte và trả về trình duyệt
            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", "DanhSachSinhVien.pdf");
        }

        // --- Các hàm phụ trợ vẽ giao diện PDF ---
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HỆ THỐNG ĐIỂM DANH AI").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            });
        }

        private void ComposeContent(IContainer container, List<Student> students)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(5);
                column.Item().Text("DANH SÁCH SINH VIÊN").FontSize(14).SemiBold();

                // Vẽ bảng
                column.Item().Table(table =>
                {
                    // Định nghĩa 4 cột với tỉ lệ độ rộng khác nhau
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Mã SV (chiếm 2 phần)
                        columns.RelativeColumn(4); // Tên (chiếm 4 phần)
                        columns.RelativeColumn(4); // Email
                        columns.RelativeColumn(2); // Trạng thái
                    });

                    // Vẽ dòng Tiêu đề bảng
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Mã SV").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Họ Tên").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Email").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text("Trạng thái").SemiBold();
                    });

                    // Vòng lặp vẽ từng dòng dữ liệu
                    foreach (var stu in students)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(stu.StudentID);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(stu.FullName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(stu.Email);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(stu.IsActive ? "Đang học" : "Bảo lưu");
                    }
                });
            });
        }
    }
}