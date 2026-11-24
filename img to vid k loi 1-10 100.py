import os
import re
import cv2
import tkinter as tk
from tkinter import filedialog


def natural_keys(text):
  """Hàm tách chữ và số để sắp xếp tên file theo thứ tự số tự nhiên (Natural Sort)

  Ví dụ: 'a (1).jpg' -> ['a (', 1, ').jpg']
  """
  return [int(c) if c.isdigit() else c.lower() for c in re.split(r'(\d+)', text)]


def create_video():
  # Ẩn cửa sổ chính của tkinter
  root = tk.Tk()
  root.withdraw()

  # 1. Hộp thoại chọn thư mục chứa ảnh
  print("Đang mở hộp thoại chọn thư mục ảnh...")
  image_folder = filedialog.askdirectory(title="Chọn thư mục chứa ảnh")

  if not image_folder:
    print("Bạn chưa chọn thư mục ảnh. Đã hủy!")
    return

  # 2. Lấy danh sách ảnh và sắp xếp theo số tự nhiên
  images = [
      img
      for img in os.listdir(image_folder)
      if img.lower().endswith((".png", ".jpg", ".jpeg"))
  ]

  # Sắp xếp lại theo thứ tự số tự nhiên: (1), (2), ..., (10), (100)
  images.sort(key=natural_keys)

  if not images:
    print("Không tìm thấy ảnh (.png, .jpg, .jpeg) trong thư mục này.")
    return

  # 3. Hộp thoại chọn nơi lưu video
  print("Đang mở hộp thoại chọn nơi lưu video...")
  video_path = filedialog.asksaveasfilename(
      title="Chọn nơi lưu video",
      defaultextension=".mp4",
      filetypes=[("MP4 Video", "*.mp4")],
  )

  if not video_path:
    print("Bạn chưa chọn nơi lưu. Đã hủy!")
    return

  # 4. Đọc ảnh đầu tiên để lấy kích thước khung hình
  first_image = cv2.imread(os.path.join(image_folder, images[0]))
  height, width, layers = first_image.shape

  # 5. Cấu hình video (2 FPS, codec MP4)
  fps = 6
  fourcc = cv2.VideoWriter_fourcc(*"mp4v")
  video = cv2.VideoWriter(video_path, fourcc, fps, (width, height))

  print(f"Đang tiến hành ghép {len(images)} ảnh thành video ({fps} FPS)...")

  # 6. Ghi từng ảnh vào video
  for image in images:
    img_path = os.path.join(image_folder, image)
    frame = cv2.imread(img_path)

    # Nếu ảnh không cùng kích thước với ảnh đầu, ép về cùng kích thước
    if frame.shape[:2] != (height, width):
      frame = cv2.resize(frame, (width, height))

    video.write(frame)
    print(f"Đã xử lý: {image}")

  # 7. Hoàn tất
  video.release()
  print(f"\n✅ Hoàn tất! Video đã được lưu tại: {video_path}")


if __name__ == "__main__":
  create_video()
