# Đường dẫn: D:\DoAnTotNghiep\FaceAttendance.AI\face_utils.py

import cv2
import numpy as np
from keras_facenet import FaceNet
import time
import os
import urllib.request

# 1. Hàm tự động tải file AI của OpenCV (nếu chưa có)
def load_opencv_dnn_model():
    model_dir = "dnn_model"
    if not os.path.exists(model_dir):
        os.makedirs(model_dir)
        
    prototxt_url = "https://raw.githubusercontent.com/opencv/opencv/master/samples/dnn/face_detector/deploy.prototxt"
    model_url = "https://raw.githubusercontent.com/opencv/opencv_3rdparty/dnn_samples_face_detector_20170830/res10_300x300_ssd_iter_140000.caffemodel"
    
    prototxt_path = os.path.join(model_dir, "deploy.prototxt")
    model_path = os.path.join(model_dir, "res10_300x300_ssd_iter_140000.caffemodel")
    
    if not os.path.exists(prototxt_path):
        print("Đang tải file cấu trúc AI (deploy.prototxt)...")
        urllib.request.urlretrieve(prototxt_url, prototxt_path)
    if not os.path.exists(model_path):
        print("Đang tải file trọng số AI (10MB)... Vui lòng đợi chút nhé...")
        urllib.request.urlretrieve(model_url, model_path)
        
    # Nạp model vào OpenCV
    net = cv2.dnn.readNetFromCaffe(prototxt_path, model_path)
    return net

print("Đang khởi tạo model...")
embedder = FaceNet()
face_net = load_opencv_dnn_model()

def test_webcam_and_embedding():
    cap = cv2.VideoCapture(0)
    print("Đang mở Camera... Bấm phím 'q' trên bàn phím để thoát.")
    
    prev_time = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            break
            
        h, w = frame.shape[:2]
        
        # Bắt đầu tính thời gian AI
        start_ai = time.time()
        
        # 2. Xử lý ảnh cho OpenCV DNN
        blob = cv2.dnn.blobFromImage(cv2.resize(frame, (300, 300)), 1.0, (300, 300), (104.0, 177.0, 123.0))
        face_net.setInput(blob)
        detections = face_net.forward()
        
        # 3. Duyệt qua các khuôn mặt tìm được
        for i in range(detections.shape[2]):
            confidence = detections[0, 0, i, 2]
            
            # Chỉ lấy các khuôn mặt có độ tin cậy > 60%
            if confidence > 0.6:
                box = detections[0, 0, i, 3:7] * np.array([w, h, w, h])
                (x, y, x1, y1) = box.astype("int")
                
                # Sửa lỗi tràn viền nếu khuôn mặt quá sát mép
                x, y = max(0, x), max(0, y)
                x1, y1 = min(w, x1), min(h, y1)
                
                # Vẽ khung chữ nhật (Xanh lá)
                cv2.rectangle(frame, (x, y), (x1, y1), (0, 255, 0), 2)
                
                ai_time = (time.time() - start_ai) * 1000
                cv2.putText(frame, f"AI: {int(ai_time)}ms - {int(confidence*100)}%", (x, y-10), 
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

        # Tính FPS
        curr_time = time.time()
        fps = 1 / (curr_time - prev_time)
        prev_time = curr_time
        cv2.putText(frame, f"FPS: {int(fps)}", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2)

        cv2.imshow("Test AI Face (OpenCV DNN)", frame)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
            
    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    test_webcam_and_embedding()