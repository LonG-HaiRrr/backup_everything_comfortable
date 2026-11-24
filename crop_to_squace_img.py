import os
import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk

# --- CẤU HÌNH MÀU SẮC GIAO DIỆN (MICROSOFT PHOTOS DARK MODE) ---
BG_COLOR = "#191919"
HEADER_BG = "#202020"
PANEL_BG = "#2B2B2B"
TEXT_COLOR = "#FFFFFF"
ACCENT_COLOR = "#C778DD" # Màu tím tím giống nút Save/Options trong app Photos
BTN_BG = "#333333"
BTN_HOVER = "#404040"

class SquareCropApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Microsoft Photos Style - Square Image Cropper")
        self.root.geometry("1100x750")
        self.root.configure(bg=BG_COLOR)

        # Quản lý file
        self.image_paths = []
        self.current_idx = -1
        self.current_pil_img = None
        self.save_dir = None

        # Biến tọa độ hiển thị & crop
        self.scale_factor = 1.0
        self.img_x0 = 0
        self.img_y0 = 0
        self.img_w = 0
        self.img_h = 0

        # Khung vuông Crop (trong hệ tọa độ Canvas)
        self.crop_x = 0
        self.crop_y = 0
        self.crop_size = 100

        # Trạng thái kéo chuột
        self.drag_mode = None  # 'move', 'nw', 'ne', 'sw', 'se'
        self.drag_start_x = 0
        self.drag_start_y = 0
        self.start_crop_x = 0
        self.start_crop_y = 0
        self.start_crop_size = 0

        self.setup_ui()
        self.bind_events()

    def setup_ui(self):
        # 1. HEADER BAR
        header = tk.Frame(self.root, bg=HEADER_BG, height=50)
        header.pack(fill="x", side="top")

        self.lbl_filename = tk.Label(header, text="Chưa mở thư mục nào", fg=TEXT_COLOR, bg=HEADER_BG, font=("Segoe UI", 11, "bold"))
        self.lbl_filename.pack(side="left", padx=20)

        # Nút Lưu & Chuyển tiếp (Save Options / Save)
        self.btn_save = tk.Button(header, text="💾 Save & Next (Enter)", bg=ACCENT_COLOR, fg="white", 
                                  font=("Segoe UI", 10, "bold"), relief="flat", padx=15, pady=5, 
                                  cursor="hand2", command=self.save_and_next)
        self.btn_save.pack(side="right", padx=20, pady=10)

        self.btn_open = tk.Button(header, text="📁 Chọn Thư Mục Ảnh", bg=BTN_BG, fg=TEXT_COLOR, 
                                  font=("Segoe UI", 10), relief="flat", padx=12, pady=5, 
                                  cursor="hand2", command=self.open_folder)
        self.btn_open.pack(side="right", padx=10, pady=10)

        # 2. CANVAS HIỂN THỊ ÁNH & KHUNG CROP
        self.canvas_frame = tk.Frame(self.root, bg=BG_COLOR)
        self.canvas_frame.pack(fill="both", expand=True)

        self.canvas = tk.Canvas(self.canvas_frame, bg=BG_COLOR, highlightthickness=0)
        self.canvas.pack(fill="both", expand=True)

        # 3. BOTTOM CONTROL BAR
        bottom_bar = tk.Frame(self.root, bg=HEADER_BG, height=60)
        bottom_bar.pack(fill="x", side="bottom")

        self.btn_prev = tk.Button(bottom_bar, text="◀ Ảnh trước (←)", bg=BTN_BG, fg=TEXT_COLOR, 
                                  font=("Segoe UI", 10), relief="flat", padx=15, pady=5, 
                                  cursor="hand2", command=self.prev_image)
        self.btn_prev.pack(side="left", padx=20, pady=12)

        self.lbl_counter = tk.Label(bottom_bar, text="0 / 0", fg="#888888", bg=HEADER_BG, font=("Segoe UI", 10))
        self.lbl_counter.pack(side="left", expand=True)

        self.btn_next = tk.Button(bottom_bar, text="Ảnh sau (→) ▶", bg=BTN_BG, fg=TEXT_COLOR, 
                                  font=("Segoe UI", 10), relief="flat", padx=15, pady=5, 
                                  cursor="hand2", command=self.next_image)
        self.btn_next.pack(side="right", padx=20, pady=12)

    def bind_events(self):
        # Sự kiện chuột trên Canvas
        self.canvas.bind("<ButtonPress-1>", self.on_mouse_down)
        self.canvas.bind("<B1-Motion>", self.on_mouse_drag)
        self.canvas.bind("<ButtonRelease-1>", self.on_mouse_up)
        self.canvas.bind("<MouseWheel>", self.on_mouse_wheel)

        # Phím tắt thao tác siêu nhanh
        self.root.bind("<Return>", lambda e: self.save_and_next())
        self.root.bind("<space>", lambda e: self.save_and_next())
        self.root.bind("<Left>", lambda e: self.prev_image())
        self.root.bind("<Right>", lambda e: self.next_image())
        self.root.bind("<Configure>", lambda e: self.redraw_canvas())

    def open_folder(self):
        folder = filedialog.askdirectory(title="Chọn thư mục chứa ảnh")
        if not folder:
            return
        
        # Lọc danh sách các file ảnh
        valid_exts = ('.jpg', '.jpeg', '.png', '.bmp', '.webp')
        self.image_paths = [os.path.join(folder, f) for f in os.listdir(folder) if f.lower().endswith(valid_exts)]
        
        if not self.image_paths:
            messagebox.showwarning("Thông báo", "Không tìm thấy file ảnh hợp lệ trong thư mục này!")
            return

        # Tạo thư mục đầu ra 'cropped_squares'
        self.save_dir = os.path.join(folder, "cropped_squares")
        os.makedirs(self.save_dir, exist_ok=True)

        self.current_idx = 0
        self.load_image()

    def load_image(self):
        if 0 <= self.current_idx < len(self.image_paths):
            path = self.image_paths[self.current_idx]
            self.lbl_filename.config(text=f"📄 {os.path.basename(path)}")
            self.lbl_counter.config(text=f"Ảnh {self.current_idx + 1} / {len(self.image_paths)}")
            
            self.current_pil_img = Image.open(path)
            
            # Reset lại khung vuông ở trung tâm ảnh
            self.reset_crop_box()
            self.redraw_canvas()

    def reset_crop_box(self):
        # Mặc định tạo khung vuông lấy theo cạnh ngắn nhất của ảnh
        if self.current_pil_img:
            w, h = self.current_pil_img.size
            min_side = min(w, h)
            # Khởi tạo mặc định chiếm 80% chiều rộng/cao nhỏ nhất
            self.initial_crop_size_img = min_side * 0.95
            self.initial_crop_x_img = (w - self.initial_crop_size_img) / 2
            self.initial_crop_y_img = (h - self.initial_crop_size_img) / 2

    def redraw_canvas(self):
        if not self.current_pil_img:
            return

        self.canvas.delete("all")
        cw = self.canvas.winfo_width()
        ch = self.canvas.winfo_height()

        if cw <= 10 or ch <= 10:
            return

        img_w, img_h = self.current_pil_img.size
        # Tính tỷ lệ fit ảnh vào Canvas
        scale = min(cw / img_w, ch / img_h) * 0.9
        self.scale_factor = scale

        display_w = int(img_w * scale)
        display_h = int(img_h * scale)

        self.img_x0 = (cw - display_w) // 2
        self.img_y0 = (ch - display_h) // 2
        self.img_w = display_w
        self.img_h = display_h

        # Resize ảnh hiển thị
        resized = self.current_pil_img.resize((display_w, display_h), Image.Resampling.LANCZOS)
        self.tk_img = ImageTk.PhotoImage(resized)
        self.canvas.create_image(self.img_x0, self.img_y0, anchor="nw", image=self.tk_img)

        # Chuyển đổi tọa độ Crop Box từ hệ ảnh gốc sang hệ Canvas
        if hasattr(self, 'initial_crop_x_img'):
            self.crop_x = self.img_x0 + self.initial_crop_x_img * scale
            self.crop_y = self.img_y0 + self.initial_crop_y_img * scale
            self.crop_size = self.initial_crop_size_img * scale
            del self.initial_crop_x_img # Chỉ dùng 1 lần khi load

        # Clamp khung crop để luôn nằm trong viền ảnh
        self.clamp_crop_box()

        # 1. Vẽ lớp phủ tối bên ngoài vùng chọn (Dark Overlay)
        x1, y1 = self.crop_x, self.crop_y
        x2, y2 = x1 + self.crop_size, y1 + self.crop_size

        # 4 vùng tối xung quanh
        self.canvas.create_rectangle(self.img_x0, self.img_y0, self.img_x0 + display_w, y1, fill="#000000", stipple="gray50", width=0)
        self.canvas.create_rectangle(self.img_x0, y2, self.img_x0 + display_w, self.img_y0 + display_h, fill="#000000", stipple="gray50", width=0)
        self.canvas.create_rectangle(self.img_x0, y1, x1, y2, fill="#000000", stipple="gray50", width=0)
        self.canvas.create_rectangle(x2, y1, self.img_x0 + display_w, y2, fill="#000000", stipple="gray50", width=0)

        # 2. Viền khung Vuông
        self.canvas.create_rectangle(x1, y1, x2, y2, outline="white", width=2)

        # 3. Vẽ 4 góc kéo (Microsoft Photos Handles)
        h_len = 15
        h_thick = 4
        # Tây Bắc
        self.canvas.create_line(x1, y1, x1 + h_len, y1, fill="red", width=h_thick)
        self.canvas.create_line(x1, y1, x1, y1 + h_len, fill="red", width=h_thick)
        # Đông Bắc
        self.canvas.create_line(x2, y1, x2 - h_len, y1, fill="red", width=h_thick)
        self.canvas.create_line(x2, y1, x2, y1 + h_len, fill="red", width=h_thick)
        # Tây Nam
        self.canvas.create_line(x1, y2, x1 + h_len, y2, fill="red", width=h_thick)
        self.canvas.create_line(x1, y2, x1, y2 - h_len, fill="red", width=h_thick)
        # Đông Nam
        self.canvas.create_line(x2, y2, x2 - h_len, y2, fill="red", width=h_thick)
        self.canvas.create_line(x2, y2, x2, y2 - h_len, fill="red", width=h_thick)

    def clamp_crop_box(self):
        """ THUẬT TOÁN KHÓA BIÊN: Đảm bảo khung crop 100% nằm bên trong ảnh """
        min_border = min(self.img_w, self.img_h)
        
        # Giới hạn kích thước tối đa không lớn hơn cạnh ngắn nhất của ảnh
        if self.crop_size > min_border:
            self.crop_size = min_border
        if self.crop_size < 30: # Kích thước tối thiểu
            self.crop_size = 30

        # Khóa biên X
        if self.crop_x < self.img_x0:
            self.crop_x = self.img_x0
        if self.crop_x + self.crop_size > self.img_x0 + self.img_w:
            self.crop_x = self.img_x0 + self.img_w - self.crop_size

        # Khóa biên Y
        if self.crop_y < self.img_y0:
            self.crop_y = self.img_y0
        if self.crop_y + self.crop_size > self.img_y0 + self.img_h:
            self.crop_y = self.img_y0 + self.img_h - self.crop_size

    def on_mouse_down(self, event):
        mx, my = event.x, event.y
        x1, y1 = self.crop_x, self.crop_y
        x2, y2 = x1 + self.crop_size, y1 + self.crop_size
        margin = 15

        # Kiểm tra click vào góc kéo
        if abs(mx - x1) < margin and abs(my - y1) < margin:
            self.drag_mode = 'nw'
        elif abs(mx - x2) < margin and abs(my - y1) < margin:
            self.drag_mode = 'ne'
        elif abs(mx - x1) < margin and abs(my - y2) < margin:
            self.drag_mode = 'sw'
        elif abs(mx - x2) < margin and abs(my - y2) < margin:
            self.drag_mode = 'se'
        elif x1 <= mx <= x2 and y1 <= my <= y2:
            self.drag_mode = 'move'
        else:
            self.drag_mode = None

        self.drag_start_x = mx
        self.drag_start_y = my
        self.start_crop_x = self.crop_x
        self.start_crop_y = self.crop_y
        self.start_crop_size = self.crop_size

    def on_mouse_drag(self, event):
        if not self.drag_mode:
            return

        dx = event.x - self.drag_start_x
        dy = event.y - self.drag_start_y

        if self.drag_mode == 'move':
            self.crop_x = self.start_crop_x + dx
            self.crop_y = self.start_crop_y + dy
        else:
            # Thay đổi kích thước giữ tỷ lệ 1:1
            if self.drag_mode == 'se':
                delta = max(dx, dy)
                self.crop_size = self.start_crop_size + delta
            elif self.drag_mode == 'nw':
                delta = min(dx, dy)
                new_size = self.start_crop_size - delta
                if new_size > 30:
                    self.crop_size = new_size
                    self.crop_x = self.start_crop_x + (self.start_crop_size - new_size)
                    self.crop_y = self.start_crop_y + (self.start_crop_size - new_size)
            elif self.drag_mode == 'ne':
                delta = max(dx, -dy)
                new_size = self.start_crop_size + delta
                if new_size > 30:
                    self.crop_size = new_size
                    self.crop_y = self.start_crop_y - (new_size - self.start_crop_size)
            elif self.drag_mode == 'sw':
                delta = max(-dx, dy)
                new_size = self.start_crop_size + delta
                if new_size > 30:
                    self.crop_size = new_size
                    self.crop_x = self.start_crop_x - (new_size - self.start_crop_size)

        self.redraw_canvas()

    def on_mouse_up(self, event):
        self.drag_mode = None

    def on_mouse_wheel(self, event):
        # Cuộn chuột để phóng to / thu nhỏ khung crop
        step = 15 if event.delta > 0 else -15
        new_size = self.crop_size + step
        if 30 <= new_size <= min(self.img_w, self.img_h):
            # Thu phóng từ tâm khung
            center_x = self.crop_x + self.crop_size / 2
            center_y = self.crop_y + self.crop_size / 2
            self.crop_size = new_size
            self.crop_x = center_x - new_size / 2
            self.crop_y = center_y - new_size / 2
            self.redraw_canvas()

    def save_and_next(self):
        if not self.current_pil_img or self.current_idx < 0:
            return

        # Tính toán chính xác tọa độ cắt trên ảnh gốc (Pixel gốc)
        crop_x_img = (self.crop_x - self.img_x0) / self.scale_factor
        crop_y_img = (self.crop_y - self.img_y0) / self.scale_factor
        crop_size_img = self.crop_size / self.scale_factor

        # Cắt ảnh vuông bằng Pillow
        box = (
            int(round(crop_x_img)),
            int(round(crop_y_img)),
            int(round(crop_x_img + crop_size_img)),
            int(round(crop_y_img + crop_size_img))
        )
        cropped_img = self.current_pil_img.crop(box)

        # Lưu ảnh vào thư mục 'cropped_squares'
        orig_filename = os.path.basename(self.image_paths[self.current_idx])
        save_path = os.path.join(self.save_dir, f"square_{orig_filename}")
        cropped_img.save(save_path, quality=95)

        # Tự động chuyển sang ảnh tiếp theo
        self.next_image()

    def prev_image(self):
        if self.current_idx > 0:
            self.current_idx -= 1
            self.load_image()

    def next_image(self):
        if self.current_idx < len(self.image_paths) - 1:
            self.current_idx += 1
            self.load_image()
        else:
            messagebox.showinfo("Hoàn tất", "Đã xử lý xong toàn bộ ảnh trong thư mục!")

if __name__ == "__main__":
    root = tk.Tk()
    app = SquareCropApp(root)
    root.mainloop()
