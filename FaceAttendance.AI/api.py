# Đường dẫn: D:\DoAnTotNghiep\FaceAttendance.AI\api.py

from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
import cv2
import numpy as np
from keras_facenet import FaceNet
import os
import urllib.request

app = FastAPI(title="Face Recognition API Realtime (YuNet)")

# 1. Hàm tự động tải Model YuNet của OpenCV (Siêu mượt, bắt góc nghiêng cực tốt)
def load_yunet_model():
    model_dir = "dnn_model"
    if not os.path.exists(model_dir):
        os.makedirs(model_dir)
        
    model_url = "https://github.com/opencv/opencv_zoo/raw/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx"
    model_path = os.path.join(model_dir, "face_detection_yunet_2023mar.onnx")
    
    if not os.path.exists(model_path):
        print("Đang tải file AI YuNet (1.5MB)... Vui lòng đợi...")
        urllib.request.urlretrieve(model_url, model_path)
        
    # Khởi tạo thuật toán YuNet
    face_detector = cv2.FaceDetectorYN.create(
        model=model_path,
        config="",
        input_size=(320, 320), # Sẽ cập nhật lại theo từng frame ảnh ở bên dưới
        score_threshold=0.8,   # Chỉ lấy mặt có độ tin cậy > 80%
        nms_threshold=0.3,
        top_k=5000,
        backend_id=cv2.dnn.DNN_BACKEND_OPENCV,
        target_id=cv2.dnn.DNN_TARGET_CPU
    )
    return face_detector

# 2. Khởi tạo Model (Chỉ load 1 lần)
embedder = FaceNet()
face_detector = load_yunet_model()

@app.post("/api/extract-face")
async def extract_face(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        
        if img is None:
            return JSONResponse(status_code=400, content={"status": "error", "message": "File không hợp lệ"})
            
        h_orig, w_orig = img.shape[:2]
        
        # Resize để xử lý nhanh
        max_width = 640
        if w_orig > max_width:
            ratio = max_width / w_orig
            img = cv2.resize(img, (max_width, int(h_orig * ratio)))
            
        h, w = img.shape[:2]
        
        # 3. Đưa kích thước ảnh vào YuNet và nhận diện
        face_detector.setInputSize((w, h))
        _, faces = face_detector.detect(img)
        
        faces_data = []
        
        # 4. Trích xuất dữ liệu khuôn mặt
        if faces is not None:
            for face in faces:
                # YuNet trả về box ở 4 index đầu tiên: [x, y, width, height]
                box = face[0:4].astype(int)
                x, y, box_w, box_h = box
                
                # Fix lỗi tọa độ tràn viền
                x, y = max(0, x), max(0, y)
                x1 = min(w, x + box_w)
                y1 = min(h, y + box_h)
                box_w = x1 - x
                box_h = y1 - y
                
                if box_w <= 0 or box_h <= 0:
                    continue
                    
                face_crop = img[y:y+box_h, x:x+box_w]
                if face_crop.size == 0:
                    continue
                    
                # Ép kiểu cho FaceNet
                face_crop_rgb = cv2.cvtColor(face_crop, cv2.COLOR_BGR2RGB)
                face_crop_rgb = cv2.resize(face_crop_rgb, (160, 160))
                face_crop_rgb = np.expand_dims(face_crop_rgb, axis=0)
                
                embeddings = embedder.embeddings(face_crop_rgb)
                vector_list = embeddings[0].tolist()
                
                # Trả tọa độ về scale gốc
                scale = w_orig / w
                orig_x = int(x * scale)
                orig_y = int(y * scale)
                orig_bw = int(box_w * scale)
                orig_bh = int(box_h * scale)
                
                faces_data.append({
                    "box": [orig_x, orig_y, orig_bw, orig_bh],
                    "vector": vector_list
                })
                
        if not faces_data:
            return JSONResponse(status_code=400, content={"status": "error", "message": "Không tìm thấy khuôn mặt nào"})
            
        return {"status": "success", "faces": faces_data}
        
    except Exception as e:
        return JSONResponse(status_code=500, content={"status": "error", "message": str(e)})