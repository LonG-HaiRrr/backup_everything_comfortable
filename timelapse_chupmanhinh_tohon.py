import os
import shutil
import time
from datetime import datetime
import threading
import cv2
import math
import numpy as np
import pyautogui  # Thư viện chụp màn hình (Cần chạy: pip install pyautogui)

# ÉP OPENCV DÙNG GIAO THỨC TCP VÀ THÊM TIMEOUT ĐỂ CHỐNG TREO NETWORK
os.environ["OPENCV_FFMPEG_CAPTURE_OPTIONS"] = "rtsp_transport;tcp"
os.environ["OPENCV_FFMPEG_READ_TIMEOUT"] = "3000"  # Timeout 3 giây nếu luồng RTSP bị đơ

# --- CẤU HÌNH ---
CAMERA_INDEX = 2    # USB Cam chính 
CAMERA_IP = "192.168.1.181"
CAMERA_VERIFY_CODE = "Longym2004"
RTSP_URL = f"rtsp://admin:{CAMERA_VERIFY_CODE}@{CAMERA_IP}:554/Streaming/Channels/101"

INTERVAL_SECONDS = 5  # 5 giây chụp 1 lần
FPS = 6
FRAMES_DIR = "frames"

def reset_frames_folder(folder_path):
    if os.path.exists(folder_path):
        shutil.rmtree(folder_path)
    # TẠO CÁC THƯ MỤC LƯU TRỮ
    os.makedirs(folder_path, exist_ok=True)
    os.makedirs(os.path.join(folder_path, "ez"), exist_ok=True)
    os.makedirs(os.path.join(folder_path, "camphu"), exist_ok=True)
    os.makedirs(os.path.join(folder_path, "manhinh"), exist_ok=True)

class USBCamReader:
    def __init__(self, src=0):
        self.src = src
        self.latest_frame = None
        self.ret = False
        self.running = True
        self.lock = threading.Lock()
        
        self.thread = threading.Thread(target=self._update_stream, daemon=True)
        self.thread.start()

    def _update_stream(self):
        cap = cv2.VideoCapture(self.src, cv2.CAP_DSHOW)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1920)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 1080)
        
        while self.running:
            if not cap.isOpened():
                time.sleep(0.1)
                continue
            
            ret, frame = cap.read()
            if not ret:
                time.sleep(0.1)
                continue
                
            with self.lock:
                self.latest_frame = frame
                self.ret = ret
            time.sleep(0.01)

        if cap.isOpened():
            cap.release()
            print("[Hệ thống] Đã giải phóng USB Camera an toàn.")

    def get_latest_frame(self):
        with self.lock:
            if self.ret and self.latest_frame is not None:
                return True, self.latest_frame.copy()
            return False, None

    def stop(self):
        self.running = False
        if self.thread.is_alive(): 
            self.thread.join(timeout=2)

class RTSPStreamReader:
    def __init__(self, url):
        self.url = url
        self.latest_frame = None
        self.ret = False
        self.running = True
        self.lock = threading.Lock()
        
        self.thread = threading.Thread(target=self._update_stream, daemon=True)
        self.thread.start()

    def _update_stream(self):
        cap = cv2.VideoCapture(self.url)
        cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
        
        while self.running:
            if not cap.isOpened():
                time.sleep(1)
                cap = cv2.VideoCapture(self.url)
                cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
                continue

            ret, frame = cap.read()
            if ret:
                with self.lock:
                    self.latest_frame = frame
                    self.ret = ret
            else:
                with self.lock:
                    self.ret = False
                cap.release()
                time.sleep(1)
            time.sleep(0.01)

        if cap.isOpened():
            cap.release()
            print("[Hệ thống] Đã giải phóng Camera IP an toàn.")

    def get_latest_frame(self):
        with self.lock:
            if self.ret and self.latest_frame is not None:
                return True, self.latest_frame.copy()
            return False, None

    def stop(self):
        self.running = False
        if self.thread.is_alive(): 
            self.thread.join(timeout=4)

# --- BẮT ĐẦU CHƯƠNG TRÌNH ---
reset_frames_folder(FRAMES_DIR)
start_time_file = datetime.now().strftime("%Hh%M ngày %d`%m`%Y")
OUTPUT_VIDEO = f"vlog_{start_time_file}.mp4"

print("Đang khởi động hệ thống Camera & Màn hình...")
usb_stream = USBCamReader(src=CAMERA_INDEX)
rtsp_stream = RTSPStreamReader(RTSP_URL)
time.sleep(3)

out = None
frame_count = 0

try:
    while True:
        ret_usb, frame_usb = usb_stream.get_latest_frame()
        ret_rtsp, frame_rtsp = rtsp_stream.get_latest_frame()

        if ret_usb:
            main_h, main_w, _ = frame_usb.shape
            
            # 1. Lưu bản gốc các nguồn
            clean_camphu = frame_usb.copy()
            clean_ez = frame_rtsp.copy() if ret_rtsp else None

            # 2. Chụp màn hình máy tính
            screenshot = pyautogui.screenshot()
            frame_screen = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
            clean_screen = frame_screen.copy()

            # --- TÍNH TOÁN KÍCH THƯỚC KHUNG PIP ĐỘC LẬP ---
            
            # Khung Màn hình máy tính (Góc dưới trái) - Mẫu số càng nhỏ khung càng TO
            scale_screen = math.sqrt(5)  # Bạn có thể đổi số này (ví dụ: sqrt(1) to hơn nữa, sqrt(20) nhỏ lại)
            screen_pip_w = int(main_w / scale_screen)
            screen_pip_h = int(main_h / scale_screen)

            # --- CHÈN ẢNH MÀN HÌNH VÀO GÓC DƯỚI BÊN TRÁI ---
            pip_screen = cv2.resize(frame_screen, (screen_pip_w, screen_pip_h), interpolation=cv2.INTER_AREA)
            cv2.rectangle(pip_screen, (0, 0), (screen_pip_w-1, screen_pip_h-1), (0, 0, 0), 1)
            frame_usb[main_h - screen_pip_h : main_h, 0 : screen_pip_w] = pip_screen

            # --- CHÈN 2 KHUNG CAMERA EZVIZ ---
            if ret_rtsp:
                # 1. KHUNG EZVIZ TOÀN CẢNH (GÓC TRÊN BÊN TRÁI - KHÔNG CROP)
                scale_ez_full = math.sqrt(12)
                ez_full_w = int(main_w / scale_ez_full)
                ez_full_h = int(main_h / scale_ez_full)
                
                pip_ez_full = cv2.resize(frame_rtsp, (ez_full_w, ez_full_h), interpolation=cv2.INTER_AREA)
                cv2.rectangle(pip_ez_full, (0, 0), (ez_full_w - 1, ez_full_h - 1), (0, 0, 0), 1)
                frame_usb[0 : ez_full_h, 0 : ez_full_w] = pip_ez_full

                # 2. KHUNG EZVIZ PHÓNG TO & CROP (GÓC DƯỚI BÊN PHẢI)
                scale_ez_zoom = math.sqrt(5)
                ez_zoom_w = int(main_w / scale_ez_zoom)
                ez_zoom_h = int(main_h / scale_ez_zoom)
                
                pip_ez_zoom = cv2.resize(frame_rtsp, (ez_zoom_w, ez_zoom_h), interpolation=cv2.INTER_AREA)
                
                # Tỷ lệ cắt bỏ ở 4 cạnh như bạn đã chỉnh
                crop_left_pct   = 0.08  # Cắt trái 5%
                crop_right_pct  = 0.50  # Cắt phải 35%
                crop_top_pct    = 0.20  # Cắt trên 30%
                crop_bottom_pct = 0.15  # Cắt dưới 5%
                
                x1 = int(ez_zoom_w * crop_left_pct)
                x2 = int(ez_zoom_w * (1 - crop_right_pct))
                y1 = int(ez_zoom_h * crop_top_pct)
                y2 = int(ez_zoom_h * (1 - crop_bottom_pct))
                
                pip_ez_cropped = pip_ez_zoom[y1:y2, x1:x2].copy()
                vis_h, vis_w, _ = pip_ez_cropped.shape
                
                # Kẻ viền đen cạnh trên và cạnh trái
                cv2.line(pip_ez_cropped, (0, 0), (vis_w - 1, 0), (0, 0, 0), 1)
                cv2.line(pip_ez_cropped, (0, 0), (0, vis_h - 1), (0, 0, 0), 1)
                
                # Dán sát vào góc dưới cùng bên phải
                frame_usb[main_h - vis_h : main_h, main_w - vis_w : main_w] = pip_ez_cropped
           
            # --- HIỂN THỊ THỜI GIAN RA CHÍNH GIỮA DƯỚI ĐÁY ---
            current_time = datetime.now().strftime("%H:%M:%S %d/%m/%y")
            font = cv2.FONT_HERSHEY_SIMPLEX
            font_scale = 0.6
            thickness = 2

            (text_w, text_h), baseline = cv2.getTextSize(current_time, font, font_scale, thickness)
            
            # Tính toán vị trí X để căn giữa màn hình
            text_x = (main_w - text_w) // 2
            text_y = main_h - 20

            # Tạo background mờ cho chữ thời gian
            overlay = frame_usb.copy()
            padding = 8
            cv2.rectangle(
                overlay, 
                (text_x - padding, text_y - text_h - padding), 
                (text_x + text_w + padding, text_y + baseline + padding), 
                (0, 0, 0), 
                -1
            )
            alpha = 0.6
            cv2.addWeighted(overlay, alpha, frame_usb, 1 - alpha, 0, frame_usb)

            # Vẽ chữ màu cam lên giữa
            cv2.putText(frame_usb, current_time, (text_x, text_y), font, font_scale, (0, 165, 255), thickness, cv2.LINE_AA)

            # Khởi tạo VideoWriter nếu chưa có
            if out is None:
                fourcc = cv2.VideoWriter_fourcc(*'mp4v')
                out = cv2.VideoWriter(OUTPUT_VIDEO, fourcc, FPS, (main_w, main_h))

            frame_count += 1
            file_timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            
            out.write(frame_usb)
            
            # --- XUẤT ẢNH RA CÁC THƯ MỤC ---
            base_filename = f"frame_{frame_count:05d}_{file_timestamp}.jpg"
            
            # 1. Ảnh tổng hợp (Chứa PIP Màn hình to + PIP Ezviz nhỏ + Timestamp ở giữa)
            cv2.imwrite(os.path.join(FRAMES_DIR, base_filename), frame_usb)
            
            # 2. Ảnh gốc Cam USB (Laptop)
            cv2.imwrite(os.path.join(FRAMES_DIR, "camphu", base_filename), clean_camphu)
            
            # 3. Ảnh gốc Ezviz
            if ret_rtsp:
                cv2.imwrite(os.path.join(FRAMES_DIR, "ez", base_filename), clean_ez)

            # 4. Ảnh gốc Màn hình máy tính
            cv2.imwrite(os.path.join(FRAMES_DIR, "manhinh", base_filename), clean_screen)

            print(f"✓ {datetime.now().strftime('%H:%M:%S')} - Đã chụp frame {frame_count} (Gốc + Màn hình + Ezviz)")
        else:
            print("⚠️ Đang chờ tín hiệu từ USB Camera chính...")

        time.sleep(INTERVAL_SECONDS)

except KeyboardInterrupt:
    print("\n[!] Đã nhận lệnh dừng (Ctrl+C), đang dọn dẹp hệ thống...")

finally:
    print("⏳ Đang ra lệnh ngắt luồng camera...")
    usb_stream.stop()
    rtsp_stream.stop()
    
    if out is not None:
        out.release()
    cv2.destroyAllWindows()
    print("✅ Hoàn tất! File video và toàn bộ thư mục ảnh (bao gồm cả màn hình) đã lưu an toàn.")
