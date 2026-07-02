-- 1. Tạo Database
CREATE DATABASE FaceAttendanceDB;
GO
USE FaceAttendanceDB;
GO

-- 2. Bảng Roles (Vai trò hệ thống)
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY, -- IDENTITY(1,1) tự động tăng từ 1
    RoleName VARCHAR(50) UNIQUE NOT NULL  -- Tên vai trò: Admin, Lecturer. UNIQUE để không tạo trùng.
);

-- 3. Bảng Users (Tài khoản đăng nhập)
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,   -- Mật khẩu phải được mã hóa (Hash), tuyệt đối không lưu chữ thường.
    RoleID INT NOT NULL,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) -- Liên kết FK với bảng Roles
);

-- 4. Bảng Lecturers (Thông tin Giảng viên)
CREATE TABLE Lecturers (
    LecturerID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,      -- NVARCHAR để lưu tiếng Việt có dấu
    Email VARCHAR(100) UNIQUE NOT NULL,
    Phone VARCHAR(15),
    UserID INT UNIQUE,                    -- 1 Giảng viên chỉ có 1 tài khoản (UNIQUE)
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- 5. Bảng Students (Thông tin Sinh viên)
CREATE TABLE Students (
    StudentID VARCHAR(20) PRIMARY KEY,    -- Khóa chính tự nhập (VD: SV001)
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    IsActive BIT DEFAULT 1                -- 1: Đang học, 0: Đã nghỉ/bảo lưu
);

-- 6. Bảng FaceEmbeddings (Lưu vector khuôn mặt của FaceNet)
CREATE TABLE FaceEmbeddings (
    EmbeddingID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID VARCHAR(20) NOT NULL,
    VectorData VARCHAR(MAX) NOT NULL,     -- Lưu chuỗi 512 con số (Embedding) dưới dạng chuỗi JSON
    CreatedAt DATETIME DEFAULT GETDATE(), -- Tự động lấy giờ hiện tại khi tạo
    FOREIGN KEY (StudentID) REFERENCES Students(StudentID)
);

-- 7. Bảng Subjects (Môn học)
CREATE TABLE Subjects (
    SubjectID VARCHAR(20) PRIMARY KEY,
    SubjectName NVARCHAR(100) NOT NULL,
    TotalCredits INT NOT NULL,            -- Số tín chỉ
    MaxAbsencePercent INT DEFAULT 20      -- Tối đa vắng mặt (mặc định 20%)
);

-- 8. Bảng Classes (Lớp học)
CREATE TABLE Classes (
    ClassID VARCHAR(50) PRIMARY KEY,
    SubjectID VARCHAR(20) NOT NULL,
    LecturerID INT NOT NULL,
    Semester VARCHAR(20) NOT NULL,        -- Học kỳ (VD: HK1_2025)
    FOREIGN KEY (SubjectID) REFERENCES Subjects(SubjectID),
    FOREIGN KEY (LecturerID) REFERENCES Lecturers(LecturerID)
);

-- 9. Bảng ClassStudents (Danh sách sinh viên trong lớp - Cầu nối nhiều-nhiều)
CREATE TABLE ClassStudents (
    ClassID VARCHAR(50) NOT NULL,
    StudentID VARCHAR(20) NOT NULL,
    PRIMARY KEY (ClassID, StudentID),     -- Khóa chính kép, 1 sinh viên không thể vào 1 lớp 2 lần
    FOREIGN KEY (ClassID) REFERENCES Classes(ClassID),
    FOREIGN KEY (StudentID) REFERENCES Students(StudentID)
);

-- 10. Bảng Schedules (Lịch học)
CREATE TABLE Schedules (
    ScheduleID INT IDENTITY(1,1) PRIMARY KEY,
    ClassID VARCHAR(50) NOT NULL,
    ClassDate DATE NOT NULL,              -- Ngày học
    StartTime TIME NOT NULL,              -- Giờ bắt đầu
    EndTime TIME NOT NULL,                -- Giờ kết thúc
    Room VARCHAR(50),                     -- Phòng học
    FOREIGN KEY (ClassID) REFERENCES Classes(ClassID)
);

-- 11. Bảng Attendances (Ghi nhận điểm danh)
CREATE TABLE Attendances (
    AttendanceID INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleID INT NOT NULL,
    StudentID VARCHAR(20) NOT NULL,
    CheckInStatus INT NOT NULL,           -- 1: Có mặt, 0: Vắng, -1: Đi trễ
    CheckInTime DATETIME,                 -- Lưu thời gian thực tế camera nhận diện được
    FOREIGN KEY (ScheduleID) REFERENCES Schedules(ScheduleID),
    FOREIGN KEY (StudentID) REFERENCES Students(StudentID),
    UNIQUE (ScheduleID, StudentID)        -- 1 buổi học 1 sinh viên chỉ có 1 record điểm danh
);

-- Đánh Index để tăng tốc độ tìm kiếm khi điểm danh
CREATE INDEX IDX_Attendances_StudentID ON Attendances(StudentID);
CREATE INDEX IDX_FaceEmbeddings_StudentID ON FaceEmbeddings(StudentID);