# Đường dẫn: D:\DoAnTotNghiep\FaceAttendance.AI\api.py

from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
import cv2
import numpy as np
from mtcnn import MTCNN
from keras_facenet import FaceNet

app = FastAPI(title="Face Recognition API")

# Khởi tạo model (chỉ load 1 lần khi server bật lên để API chạy nhanh)
detector = MTCNN()
embedder = FaceNet()

@app.post("/api/extract-face")
async def extract_face(file: UploadFile = File(...)):
    try:
        # 1. Đọc luồng byte của file ảnh được gửi tới
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        
        # 2. Chuyển byte thành ma trận ảnh OpenCV
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if img is None:
            return JSONResponse(status_code=400, content={"status": "error", "message": "File không phải là ảnh hợp lệ"})
            
        rgb_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        
        # 3. MTCNN tìm khuôn mặt
        results = detector.detect_faces(rgb_img)
        if not results:
            return JSONResponse(status_code=400, content={"status": "error", "message": "Không tìm thấy khuôn mặt nào"})
            
        # Lấy khuôn mặt to nhất/đầu tiên tìm được
        x, y, w, h = results[0]['box']
        
        # Xử lý trường hợp MTCNN trả về tọa độ âm (lọt ra ngoài viền ảnh)
        x, y = max(0, x), max(0, y)
        face_crop = rgb_img[y:y+h, x:x+w]
        
        if face_crop.size == 0:
            return JSONResponse(status_code=400, content={"status": "error", "message": "Lỗi cắt khuôn mặt"})
            
        # 4. Ép kích thước về 160x160 cho FaceNet
        face_crop = cv2.resize(face_crop, (160, 160))
        face_crop = np.expand_dims(face_crop, axis=0)
        
        # 5. Trích xuất Vector Embedding
        embeddings = embedder.embeddings(face_crop)
        
        # Chuyển mảng Numpy thành mảng List chuẩn của Python để có thể chuyển thành JSON
        vector_list = embeddings[0].tolist()
        
        # 6. Trả về kết quả JSON (Request -> Response)
        return {"status": "success", "vector": vector_list}
        
    except Exception as e:
        return JSONResponse(status_code=500, content={"status": "error", "message": str(e)})

# Để chạy file này, ta dùng lệnh uvicorn ở Terminal