import os
import glob
import cv2
import numpy as np
from tkinter import filedialog, Tk

def nothing(x):
    pass

# 1. Mở hộp thoại chọn thư mục
root = Tk()
root.withdraw()
folder_path = filedialog.askdirectory(title="Chọn thư mục chứa ảnh cần crop 1:1")

if not folder_path:
    print("Chưa chọn thư mục. Dừng chương trình!")
    exit()

extensions = ('*.jpg', '*.jpeg', '*.png', '*.JPG', '*.PNG')
image_files = []
for ext in extensions:
    image_files.extend(glob.glob(os.path.join(folder_path, ext)))
image_files = sorted(list(set(image_files)))

if not image_files:
    print("Không tìm thấy ảnh trong thư mục!")
    exit()

WINDOW_NAME = "Crop 1:1 Tool - Native OpenCV"
cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)

# Tạo các thanh trượt Trackbar
cv2.createTrackbar("Size (%)", WINDOW_NAME, 100, 100, nothing) # Mặc định 100%
cv2.createTrackbar("Shift X", WINDOW_NAME, 50, 100, nothing)   # Mặc định 50 (Chính giữa)
cv2.createTrackbar("Shift Y", WINDOW_NAME, 50, 100, nothing)   # Mặc định 50 (Chính giữa)

current_idx = 0

def get_crop_coords(img_h, img_w):
    size_pct = cv2.getTrackbarPos("Size (%)", WINDOW_NAME) / 100.0
    shift_x_pct = (cv2.getTrackbarPos("Shift X", WINDOW_NAME) - 50) / 50.0  # Range -1.0 -> 1.0
    shift_y_pct = (cv2.getTrackbarPos("Shift Y", WINDOW_NAME) - 50) / 50.0  # Range -1.0 -> 1.0

    size_pct = max(0.1, size_pct)
    max_square = min(img_h, img_w)
    square_size = int(max_square * size_pct)

    center_x = img_w / 2.0
    center_y = img_h / 2.0

    max_shift_x = (img_w - square_size) / 2.0
    max_shift_y = (img_h - square_size) / 2.0

    actual_center_x = center_x + (shift_x_pct * max_shift_x)
    actual_center_y = center_y + (shift_y_pct * max_shift_y)

    x1 = max(0, int(actual_center_x - square_size / 2.0))
    y1 = max(0, int(actual_center_y - square_size / 2.0))
    x2 = min(img_w, x1 + square_size)
    y2 = min(img_h, y1 + square_size)

    return x1, y1, x2, y2

print("\n--- HƯỚNG DẪN BẮM PHÍM ---")
print("[ENTER / SPACE]: Cắt đè HÀNG LOẠT tất cả ảnh")
print("[C]            : Cắt đè duy nhất ảnh hiện tại")
print("[D / Phải]     : Xem ảnh tiếp theo")
print("[A / Trái]     : Xem ảnh trước")
print("[Q / ESC]      : Thoát\n")

while True:
    img_path = image_files[current_idx]
    img = cv2.imread(img_path)

    if img is None:
        current_idx = (current_idx + 1) % len(image_files)
        continue

    h, w, _ = img.shape
    x1, y1, x2, y2 = get_crop_coords(h, w)

    # Vẽ khung xem trước
    display_img = img.copy()
    cv2.rectangle(display_img, (x1, y1), (x2, y2), (0, 0, 255), 3)

    # Hiển thị thông tin
    info_str = f"[{current_idx + 1}/{len(image_files)}] {os.path.basename(img_path)} | Size: {x2-x1}x{y2-y1}"
    cv2.putText(display_img, info_str, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)

    cv2.imshow(WINDOW_NAME, display_img)

    key = cv2.waitKey(30) & 0xFF

    # Nút D hoặc Phím Mũi Tên Phải -> Next
    if key in [ord('d'), 83]:
        current_idx = min(current_idx + 1, len(image_files) - 1)
    # Nút A hoặc Phím Mũi Tên Trái -> Prev
    elif key in [ord('a'), 81]:
        current_idx = max(current_idx - 1, 0)
    # Nút C -> Crop duy nhất ảnh này
    elif key == ord('c'):
        cropped = img[y1:y2, x1:x2]
        cv2.imwrite(img_path, cropped)
        print(f"✓ Đã cắt đè 1:1 file: {os.path.basename(img_path)}")
    # Nút ENTER hoặc SPACE -> Cắt HÀNG LOẠT
    elif key in [13, 32]:
        print(f"\n⏳ Đang cắt hàng loạt {len(image_files)} ảnh...")
        count = 0
        for p in image_files:
            item = cv2.imread(p)
            if item is not None:
                ih, iw, _ = item.shape
                bx1, by1, bx2, by2 = get_crop_coords(ih, iw)
                cropped_item = item[by1:by2, bx1:bx2]
                cv2.imwrite(p, cropped_item)
                count += 1
        print(f"✅ Hoàn tất! Đã cắt thành công {count} ảnh.\n")
    # Nút Q hoặc ESC -> Thoát
    elif key in [ord('q'), 27]:
        break

cv2.destroyAllWindows()
