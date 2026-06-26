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
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        
        if img is None:
            return JSONResponse(status_code=400, content={"status": "error", "message": "File không hợp lệ"})
            
        rgb_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        results = detector.detect_faces(rgb_img)
        
        if not results:
            return JSONResponse(status_code=400, content={"status": "error", "message": "Không tìm thấy khuôn mặt"})
            
        faces_data = [] # Chứa danh sách các vector
        
        # VÒNG LẶP: Quét toàn bộ khuôn mặt trong khung hình
        for res in results:
            x, y, w, h = res['box']
            x, y = max(0, x), max(0, y)
            face_crop = rgb_img[y:y+h, x:x+w]
            
            if face_crop.size > 0:
                face_crop = cv2.resize(face_crop, (160, 160))
                face_crop = np.expand_dims(face_crop, axis=0)
                embeddings = embedder.embeddings(face_crop)
                faces_data.append(embeddings[0].tolist())
        
        # Trả về mảng các vector (Thay đổi từ "vector" thành "vectors")
        return {"status": "success", "vectors": faces_data}
        
    except Exception as e:
        return JSONResponse(status_code=500, content={"status": "error", "message": str(e)})