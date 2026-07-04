// Đường dẫn: FaceAttendance.Web/Controllers/ClassController.cs
using ClosedXML.Excel;
using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FaceAttendance.Web.Controllers
{
    public class ClassController : Controller
    {
        private readonly AppDbContext _context;

        public ClassController(AppDbContext context)
        {
            _context = context;
            // Yêu cầu bắt buộc của QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Classes.Include(c => c.ClassStudents).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.ClassName.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            int totalItems = await query.CountAsync();
            var classes = await query.OrderByDescending(c => c.CreatedAt)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(classes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassName")] ClassRoom newClass)
        {
            if (ModelState.IsValid)
            {
                bool isExist = await _context.Classes.AnyAsync(c => c.ClassName.ToLower() == newClass.ClassName.ToLower());
                if (isExist)
                {
                    ModelState.AddModelError("ClassName", "Tên lớp này đã tồn tại trong hệ thống.");
                    return View(newClass);
                }

                newClass.CreatedAt = DateTime.Now;
                _context.Add(newClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(newClass);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            return View(classObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassID,ClassName,CreatedAt")] ClassRoom classObj)
        {
            if (id != classObj.ClassID) return NotFound();

            if (ModelState.IsValid)
            {
                bool isExist = await _context.Classes.AnyAsync(c => c.ClassName.ToLower() == classObj.ClassName.ToLower() && c.ClassID != id);
                if (isExist)
                {
                    ModelState.AddModelError("ClassName", "Tên lớp này đã bị trùng với một lớp khác.");
                    return View(classObj);
                }

                try
                {
                    _context.Update(classObj);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(classObj.ClassID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(classObj);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                .Include(c => c.AttendanceSessions)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classObj != null)
            {
                _context.ClassStudents.RemoveRange(classObj.ClassStudents);
                _context.Classes.Remove(classObj);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                    .ThenInclude(cs => cs.Student)
                .FirstOrDefaultAsync(m => m.ClassID == id);

            if (classObj == null) return NotFound();

            return View(classObj);
        }

        public async Task<IActionResult> AddStudents(int id, string searchString)
        {
            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            ViewBag.ClassID = classObj.ClassID;
            ViewBag.ClassName = classObj.ClassName;

            var existingStudentIds = await _context.ClassStudents
                .Where(cs => cs.ClassID == id)
                .Select(cs => cs.StudentID)
                .ToListAsync();

            var query = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID));

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.StudentID.Contains(searchString) || s.FullName.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            var availableStudents = await query.ToListAsync();
            return View(availableStudents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudents(int id, List<string> selectedStudents)
        {
            if (selectedStudents != null && selectedStudents.Any())
            {
                foreach (var studentId in selectedStudents)
                {
                    bool exists = await _context.ClassStudents.AnyAsync(cs => cs.ClassID == id && cs.StudentID == studentId);
                    if (!exists)
                    {
                        _context.ClassStudents.Add(new ClassStudent
                        {
                            ClassID = id,
                            StudentID = studentId
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classId, string studentId)
        {
            var record = await _context.ClassStudents.FirstOrDefaultAsync(x => x.ClassID == classId && x.StudentID == studentId);
            if (record != null)
            {
                _context.ClassStudents.Remove(record);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = classId });
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassID == id);
        }

        // =========================================================
        // XUẤT EXCEL CHO MỘT LỚP CỤ THỂ
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                .ThenInclude(cs => cs.Student)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classObj == null) return NotFound();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("DanhSachSinhVien");

            // Thông tin chung của Lớp
            worksheet.Cell(1, 1).Value = "Tên Lớp:";
            worksheet.Cell(1, 2).Value = classObj.ClassName;
            worksheet.Cell(2, 1).Value = "Ngày xuất:";
            worksheet.Cell(2, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // In đậm thông tin Lớp
            worksheet.Range("A1:A2").Style.Font.Bold = true;

            // Tiêu đề bảng
            worksheet.Cell(4, 1).Value = "STT";
            worksheet.Cell(4, 2).Value = "Mã Sinh Viên";
            worksheet.Cell(4, 3).Value = "Họ và Tên";
            worksheet.Cell(4, 4).Value = "Email";
            worksheet.Cell(4, 5).Value = "Trạng thái";

            worksheet.Range("A4:E4").Style.Fill.BackgroundColor = XLColor.BlueGray;
            worksheet.Range("A4:E4").Style.Font.FontColor = XLColor.White;
            worksheet.Range("A4:E4").Style.Font.Bold = true;
            worksheet.Range("A4:E4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Đổ dữ liệu sinh viên
            int currentRow = 5;
            int stt = 1;
            foreach (var item in classObj.ClassStudents)
            {
                worksheet.Cell(currentRow, 1).Value = stt++;
                worksheet.Cell(currentRow, 2).Value = item.Student.StudentID;
                worksheet.Cell(currentRow, 3).Value = item.Student.FullName;
                worksheet.Cell(currentRow, 4).Value = item.Student.Email;
                worksheet.Cell(currentRow, 5).Value = item.Student.IsActive ? "Đang học" : "Bảo lưu";

                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DanhSachLop_{classObj.ClassName}.xlsx");
        }

        // =========================================================
        // XUẤT PDF CHO MỘT LỚP CỤ THỂ
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                .ThenInclude(cs => cs.Student)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classObj == null) return NotFound();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    // BẮT BUỘC dùng font Arial để hỗ trợ Unicode tiếng Việt
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(x => ComposePdfHeader(x, classObj));
                    page.Content().Element(x => ComposePdfContent(x, classObj.ClassStudents.Select(cs => cs.Student).ToList()));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"DanhSachLop_{classObj.ClassName}.pdf");
        }

        private void ComposePdfHeader(IContainer container, ClassRoom classObj)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("TRƯỜNG ĐH CÔNG NGHIỆP QUẢNG NINH").FontSize(14).SemiBold();
                    column.Item().Text("HỆ THỐNG ĐIỂM DANH AI").FontSize(12).FontColor(Colors.Blue.Darken2);
                    column.Item().PaddingTop(10).Text($"DANH SÁCH SINH VIÊN LỚP: {classObj.ClassName}").FontSize(16).Bold();
                    column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                    column.Item().Text($"Sĩ số: {classObj.ClassStudents.Count} sinh viên").FontSize(10);
                });
            });
        }

        private void ComposePdfContent(IContainer container, List<Student> students)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // STT
                        columns.RelativeColumn(2);   // Mã SV
                        columns.RelativeColumn(4);   // Tên
                        columns.RelativeColumn(4);   // Email
                        columns.RelativeColumn(2);   // Trạng thái
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignCenter().Text("STT").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Mã SV").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Họ Tên").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Email").FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Trạng thái").FontColor(Colors.White).SemiBold();
                    });

                    int stt = 1;
                    foreach (var stu in students)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(stt.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(stu.StudentID);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(stu.FullName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(stu.Email);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(stu.IsActive ? "Đang học" : "Bảo lưu");
                        stt++;
                    }
                });
            });
        }
    }
}