using FaceAttendance.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly FaceRecognitionService _faceApi;
        private readonly AppDbContext _context;

        // Đã gỡ bỏ hoàn toàn FaceCacheService vì C# không cần tự tính toán Vector nữa
        public AttendanceService(FaceRecognitionService faceApi, AppDbContext context)
        {
            _faceApi = faceApi;
            _context = context;
        }

        public async Task<List<FaceResultResponse>> ProcessAttendanceAsync(string base64Image, int classId)
        {
            var resultList = new List<FaceResultResponse>();

            // 1. Convert Base64 thành file ảnh đẩy qua Python
            var bytes = Convert.FromBase64String(base64Image.Split(',')[1]);
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "file", "frame.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            // 2. Nhận kết quả Bounding Box và Tên (Mã SV) thẳng từ AI
            var aiFaces = await _faceApi.GetFaceEmbeddingAsync(formFile);

            // 3. Lấy danh sách ID sinh viên CHỈ THUỘC LỚP ĐANG CHỌN
            var studentIdsInClass = await _context.ClassStudents
                .Where(cs => cs.ClassID == classId)
                .Select(cs => cs.StudentID)
                .ToListAsync();

            // 4. Xử lý nghiệp vụ điểm danh dựa trên quyết định của AI
            foreach (var (box, name) in aiFaces)
            {
                if (name != "Unknown")
                {
                    // AI đã nhận ra người này. Kiểm tra xem có học lớp này không?
                    if (studentIdsInClass.Contains(name))
                    {
                        var student = await _context.Students.FindAsync(name);
                        resultList.Add(new FaceResultResponse
                        {
                            Box = box,
                            StudentId = name,
                            StudentName = student?.FullName ?? name,
                            Percent = 99.9, // Gán cứng độ tự tin vì AI đã chốt kết quả
                            Success = true
                        });
                    }
                    else
                    {
                        // Nhận ra sinh viên trong DB nhưng đi nhầm lớp
                        resultList.Add(new FaceResultResponse
                        {
                            Box = box,
                            StudentName = "Unknown (Sai lớp)",
                            Percent = 0,
                            Success = false
                        });
                    }
                }
                else
                {
                    // AI Python trả về Unknown (Người lạ)
                    resultList.Add(new FaceResultResponse
                    {
                        Box = box,
                        StudentName = "Unknown",
                        Percent = 0,
                        Success = false
                    });
                }
            }

            return resultList;
        }
    }
}