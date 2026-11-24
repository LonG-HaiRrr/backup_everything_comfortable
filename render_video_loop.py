import os
import subprocess
import imageio_ffmpeg

input_video = "1112.mp4"
output_video = "1112_lap10lan.mp4"
so_lan_lap = 10

# Lấy đường dẫn file ffmpeg có sẵn từ thư viện
ffmpeg_exe = imageio_ffmpeg.get_ffmpeg_exe()
list_file = "danh_sach_tam.txt"

# Tạo file danh sách chứa đường dẫn video lặp lại 10 lần
duong_dan_chuan = os.path.abspath(input_video).replace("\\", "/")
with open(list_file, "w", encoding="utf-8") as f:
  for _ in range(so_lan_lap):
    f.write(f"file '{duong_dan_chuan}'\n")

# Ghép nối siêu tốc bằng -c copy (không render lại)
lenh_ffmpeg = [
    ffmpeg_exe,
    "-y",  # Ghi đè nếu file đầu ra đã tồn tại
    "-f",
    "concat",
    "-safe",
    "0",
    "-i",
    list_file,
    "-c",
    "copy",
    output_video,
]

subprocess.run(lenh_ffmpeg, check=True)

# Xóa file danh sách tạm
os.remove(list_file)
print(f"Xong! Đã xuất file: {output_video}")