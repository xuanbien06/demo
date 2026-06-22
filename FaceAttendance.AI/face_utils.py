# Đường dẫn: D:\DoAnTotNghiep\FaceAttendance.AI\face_utils.py

import cv2 # Thư viện OpenCV xử lý ảnh
import numpy as np # Thư viện xử lý mảng số
from mtcnn import MTCNN # Thư viện nhận diện vị trí khuôn mặt
from keras_facenet import FaceNet # Thư viện trích xuất đặc trưng khuôn mặt

# 1. Khởi tạo 2 mô hình AI (Chỉ khởi tạo 1 lần cho nhẹ máy)
detector = MTCNN() 
embedder = FaceNet()

def test_webcam_and_embedding():
    # 2. Bật Webcam (Số 0 là camera mặc định của laptop)
    cap = cv2.VideoCapture(0)
    
    print("Đang mở Camera... Bấm phím 'q' trên bàn phím để thoát.")

    while True:
        # 3. Đọc từng khung hình từ video
        ret, frame = cap.read()
        if not ret:
            break
            
        # 4. OpenCV đọc ảnh dạng BGR, nhưng AI cần ảnh dạng RGB. Ta phải chuyển đổi:
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        
        # 5. Đưa ảnh cho MTCNN để tìm khuôn mặt
        results = detector.detect_faces(rgb_frame)
        
        # Nếu tìm thấy ít nhất 1 khuôn mặt
        if results:
            for res in results:
                # Lấy tọa độ góc trên bên trái (x,y) và chiều rộng, chiều cao (w,h)
                x, y, w, h = res['box']
                
                # 6. Vẽ khung chữ nhật màu xanh lá (0, 255, 0) bao quanh mặt
                cv2.rectangle(frame, (x, y), (x+w, y+h), (0, 255, 0), 2)
                
                # 7. Cắt đúng phần khuôn mặt ra khỏi ảnh lớn (Chú ý: y trước x sau trong ma trận ảnh)
                face_crop = rgb_frame[y:y+h, x:x+w]
                
                # Bỏ qua nếu ảnh cắt bị lỗi (quá nhỏ hoặc lọt ra ngoài khung hình)
                if face_crop.size == 0:
                    continue
                    
                # 8. Ép kích thước khuôn mặt về chuẩn 160x160 pixel mà FaceNet yêu cầu
                face_crop = cv2.resize(face_crop, (160, 160))
                
                # Thêm 1 chiều vào ma trận ảnh (từ 3D thành 4D) để đưa vào mạng Nơ-ron
                face_crop = np.expand_dims(face_crop, axis=0)
                
                # 9. Đưa vào FaceNet để lấy mảng 512 con số
                embeddings = embedder.embeddings(face_crop)
                
                # In ra độ dài của mảng để kiểm tra (chắc chắn sẽ in ra số 512)
                cv2.putText(frame, f"Vector: {len(embeddings[0])} dims", (x, y-10), 
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

        # 10. Hiển thị khung hình lên màn hình
        cv2.imshow("Test AI Face Recognition", frame)
        
        # 11. Chờ 1ms, nếu người dùng bấm phím 'q' thì thoát vòng lặp
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
            
    # 12. Dọn dẹp: Tắt camera và đóng cửa sổ
    cap.release()
    cv2.destroyAllWindows()

# Dòng lệnh này để báo cho Python biết: Hãy chạy hàm test_webcam_and_embedding() ngay khi mở file này
if __name__ == "__main__":
    test_webcam_and_embedding()