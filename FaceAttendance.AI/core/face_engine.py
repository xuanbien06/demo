import os
import cv2
import numpy as np
import face_recognition
import urllib.request
from ultralytics import YOLO

class FaceRecognitionEngine:
    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(FaceRecognitionEngine, cls).__new__(cls)
            cls._instance.known_face_encodings = []
            cls._instance.known_face_names = []
            
            # TODO: THAY ĐỔI ĐƯỜNG DẪN NÀY
            # Bạn hãy trỏ đường dẫn này về đúng thư mục chứa ảnh của "Hồ sơ sinh viên" bên Web C#
            # Ví dụ: r"D:\DoAnTotNghiep\FaceAttendance.Web\wwwroot\dataset"
            cls._instance.dataset_path = r"D:\DoAnTotNghiep\FaceAttendance.Web\wwwroot\uploads\dataset"
            
            cls._instance.model_dir = "dnn_model/"
            cls._instance.yolo_model_path = os.path.join(cls._instance.model_dir, "yolov8n-face.pt")
        return cls._instance

    def __init__(self):
        if not os.path.exists(self.dataset_path):
            os.makedirs(self.dataset_path)
        if not os.path.exists(self.model_dir):
            os.makedirs(self.model_dir)
        
        if not os.path.exists(self.yolo_model_path):
            raise FileNotFoundError(f"Không tìm thấy file mô hình tại {self.yolo_model_path}")

        self.face_detector = YOLO(self.yolo_model_path)

    def _get_face_locations_yolo(self, rgb_image):
        """Dùng YOLO quét khuôn mặt và mở rộng khung (padding) để Dlib đọc tốt hơn"""
        results = self.face_detector(rgb_image, verbose=False)
        face_locations = []
        h_img, w_img, _ = rgb_image.shape
        
        for r in results:
            boxes = r.boxes
            for box in boxes:
                x1, y1, x2, y2 = box.xyxy[0]
                x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)
                
                # THÊM PADDING: Mở rộng khung ra thêm 15% để lấy cả trán và cằm
                w = x2 - x1
                h = y2 - y1
                pad_w = int(w * 0.15)
                pad_h = int(h * 0.15)
                
                x1 = max(0, x1 - pad_w)
                y1 = max(0, y1 - pad_h)
                x2 = min(w_img, x2 + pad_w)
                y2 = min(h_img, y2 + pad_h)
                
                # Cấu trúc lại theo chuẩn Dlib: (top, right, bottom, left)
                face_locations.append((y1, x2, y2, x1))
                
        return face_locations

    def load_and_encode_dataset(self):
        print("[INFO] Đang nạp lại Dataset bằng YOLOv8-Face...")
        temp_encodings = []
        temp_names = []

        for student_id in os.listdir(self.dataset_path):
            student_dir = os.path.join(self.dataset_path, student_id)
            
            if not os.path.isdir(student_dir):
                continue
                
            for file_name in os.listdir(student_dir):
                if file_name.endswith((".jpg", ".jpeg", ".png")):
                    image_path = os.path.join(student_dir, file_name)
                    
                    try:
                        image = face_recognition.load_image_file(image_path)
                        face_bounding_boxes = self._get_face_locations_yolo(image)
                        
                        if len(face_bounding_boxes) == 1:
                            face_encoding = face_recognition.face_encodings(image, known_face_locations=face_bounding_boxes)[0]
                            temp_encodings.append(face_encoding)
                            temp_names.append(student_id)
                        else:
                            print(f"[WARNING] Ảnh {image_path} không có/nhiều hơn 1 khuôn mặt. Bỏ qua.")
                    except Exception as e:
                        print(f"[ERROR] Lỗi khi xử lý ảnh {image_path}: {str(e)}")

        self.known_face_encodings = temp_encodings
        self.known_face_names = temp_names
        print(f"[INFO] Đã nạp thành công {len(self.known_face_names)} khuôn mặt vào RAM.")

    # Tăng tolerance lên 0.5 để dễ nhận diện hơn với hệ thống Hybrid
    def recognize_face(self, frame, tolerance=0.5):
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        face_locations = self._get_face_locations_yolo(rgb_frame)
        face_encodings = face_recognition.face_encodings(rgb_frame, face_locations)

        face_names = []
        for face_encoding in face_encodings:
            face_distances = face_recognition.face_distance(self.known_face_encodings, face_encoding)
            
            name = "Unknown"
            if len(face_distances) > 0:
                best_match_index = np.argmin(face_distances)
                if face_distances[best_match_index] <= tolerance:
                    name = self.known_face_names[best_match_index]
            
            face_names.append(name)

        return face_locations, face_names

face_engine = FaceRecognitionEngine()