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
        private readonly FaceCacheService _faceCache;
        private readonly AppDbContext _context; // Dùng để truy vấn Database

        public AttendanceService(FaceRecognitionService faceApi, FaceCacheService faceCache, AppDbContext context)
        {
            _faceApi = faceApi;
            _faceCache = faceCache;
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

            // 2. Nhận kết quả Bounding Box và Vector từ AI
            var aiFaces = await _faceApi.GetFaceEmbeddingAsync(formFile);

            // 3. Lấy danh sách ID sinh viên CHỈ THUỘC LỚP ĐANG CHỌN (Yêu cầu 8)
            var studentIdsInClass = await _context.ClassStudents
                .Where(cs => cs.ClassID == classId)
                .Select(cs => cs.StudentID)
                .ToListAsync();

            // 4. So khớp khuôn mặt
            foreach (var (box, vector) in aiFaces)
            {
                var (bestMatchId, distance) = _faceCache.FindBestMatch(vector);

                // Tính % tự tin cho đẹp
                double percent = Math.Round(Math.Max(0, 1 - distance) * 100, 2);

                if (bestMatchId != null && distance < 0.6) // Ngưỡng 0.6 của FaceNet
                {
                    // KIỂM TRA BẢO MẬT: Sinh viên này có học lớp hiện tại không?
                    if (studentIdsInClass.Contains(bestMatchId))
                    {
                        var student = await _context.Students.FindAsync(bestMatchId);
                        resultList.Add(new FaceResultResponse
                        {
                            Box = box,
                            StudentId = student?.StudentID, // <-- BỔ SUNG DÒNG NÀY
                            StudentName = student?.FullName ?? "Unknown",
                            Percent = percent,
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
                            Percent = percent,
                            Success = false // Bật cờ false để vẽ khung đỏ
                        });
                    }
                }
                else
                {
                    // Người hoàn toàn lạ (Không có trong hệ thống)
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