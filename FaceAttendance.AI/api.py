from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import JSONResponse
import os
import shutil
import cv2
import numpy as np
import face_recognition

# Import AI Engine vừa tạo
from core.face_engine import face_engine 

app = FastAPI(title="Face Attendance AI API")

# 1. Nạp Dataset vào RAM ngay khi Server khởi động
@app.on_event("startup")
async def startup_event():
    face_engine.load_and_encode_dataset()

# 2. API Upload ảnh sinh viên vào Dataset
@app.post("/api/v1/dataset/upload")
async def upload_student_face(student_id: str = Form(...), file: UploadFile = File(...)):
    # Bước 1: Tạo thư mục cho sinh viên nếu chưa có
    student_dir = os.path.join(face_engine.dataset_path, student_id)
    if not os.path.exists(student_dir):
        os.makedirs(student_dir)
        
    file_path = os.path.join(student_dir, file.filename)
    
    # Bước 2: Lưu file ảnh vào ổ cứng
    try:
        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Không thể lưu file: {str(e)}")
        
    # Bước 3: Sau khi lưu file thành công, BẮT BUỘC phải reload lại bộ nhớ AI
    try:
        face_engine.load_and_encode_dataset()
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lưu ảnh thành công nhưng lỗi khi train AI: {str(e)}")
        
    return JSONResponse(content={
        "status": "success",
        "message": f"Đã thêm ảnh cho sinh viên {student_id} và cập nhật AI thành công."
    }, status_code=200)

# 3. API thủ công để Admin ép AI học lại toàn bộ
@app.post("/api/v1/dataset/retrain")
async def retrain_ai_model():
    try:
        face_engine.load_and_encode_dataset()
        return JSONResponse(content={
            "status": "success",
            "message": "Đã huấn luyện lại (reload) AI Model thành công."
        }, status_code=200)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi khi huấn luyện lại AI: {str(e)}")

# =====================================================================
# PHẦN CODE MỚI BỔ SUNG: API xử lý Frame Camera từ Frontend
# =====================================================================
@app.post("/api/extract-face")
async def extract_face(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

        if frame is None:
            return JSONResponse(status_code=400, content={"status": "error", "message": "Không thể đọc frame camera"})

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        # AI thực hiện nhận diện và đưa ra kết quả cuối cùng
        face_locations, face_names = face_engine.recognize_face(rgb_frame)

        faces_data = []
        for i in range(len(face_locations)):
            top, right, bottom, left = face_locations[i]
            
            # Tọa độ chuẩn C#
            x = left
            y = top
            w = right - left
            h = bottom - top
            
            face_info = {
                "box": [x, y, w, h], 
                "name": face_names[i] # DỊCH CHUYỂN KIẾN TRÚC: Trả thẳng Tên/Mã SV cho C#
            }
            faces_data.append(face_info)

        print(f"AI Debug: Đã gửi cho C# danh sách -> {face_names}")

        return JSONResponse(content={
            "status": "success",
            "faces": faces_data
        }, status_code=200)

    except Exception as e:
        return JSONResponse(status_code=500, content={"status": "error", "message": str(e)})