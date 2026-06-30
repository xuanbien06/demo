using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Repositories
{
    public class FaceEmbeddingRepository : IFaceEmbeddingRepository
    {
        private readonly AppDbContext _context;

        // Chỉ có Repository mới được phép chạm vào AppDbContext
        public FaceEmbeddingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FaceEmbedding>> GetAllWithStudentAsync()
        {
            return await _context.FaceEmbeddings.Include(f => f.Student).ToListAsync();
        }
    }
}