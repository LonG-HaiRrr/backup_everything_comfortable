import cv2
import numpy as np

# === CẤU HÌNH ===
START_TIME = "13:40:00"      # Thời điểm bắt đầu (HH:MM:SS)
DURATION = "00:20:00"        # Khoảng thời gian chạy (HH:MM:SS hoặc số giây, vd: 3600)
OUTPUT_FILE = "clock_1fps__luc13h.mp4"
WIDTH, HEIGHT = 1280, 720
FPS = 1

def to_seconds(val):
    if isinstance(val, int):
        return val
    parts = [int(p) for p in str(val).strip().split(":")]
    if len(parts) == 3:
        return parts[0] * 3600 + parts[1] * 60 + parts[2]
    if len(parts) == 2:
        return parts[0] * 60 + parts[1]
    return parts[0]

start_sec = to_seconds(START_TIME)
total_sec = to_seconds(DURATION)

fourcc = cv2.VideoWriter_fourcc(*"mp4v")
out = cv2.VideoWriter(OUTPUT_FILE, fourcc, FPS, (WIDTH, HEIGHT))

font = cv2.FONT_HERSHEY_SIMPLEX
scale, thickness = 4.0, 8
(tw, th), _ = cv2.getTextSize("00:00:00", font, scale, thickness)
x = (WIDTH - tw) // 2
y = (HEIGHT + th) // 2

for i in range(total_sec):
    current = (start_sec + i) % 86400  # Tự quay vòng về 00:00:00 nếu qua 23:59:59
    h, rem = divmod(current, 3600)
    m, s = divmod(rem, 60)
    text = f"{h:02d}:{m:02d}:{s:02d}"

    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    cv2.putText(frame, text, (x, y), font, scale, (255, 255, 255), thickness, cv2.LINE_AA)
    out.write(frame)

out.release()
print(f"Xong: {OUTPUT_FILE} ({total_sec} khung hình)")
