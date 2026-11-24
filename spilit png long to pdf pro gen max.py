import os
import tkinter as tk
from tkinter import filedialog, messagebox
import numpy as np
import cv2
from PIL import Image, ImageTk, ImageOps

# Cho phép mở ảnh chụp màn hình siêu dài (như 1824 x 41617 px) mà không bị giới hạn pixel
Image.MAX_IMAGE_PIXELS = None

# Chuẩn kích thước A4 thật ở 300 DPI (210mm x 297mm)
A4_DPI = 300
A4_WIDTH_PX = 2480
A4_HEIGHT_PX = 2508


class SmartA4SplitterApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Smart A4 Image Splitter & PDF Exporter")
        self.root.geometry("1280x840")
        self.root.minsize(1050, 700)

        # Bảng màu Dark Mode
        self.bg_main = "#18191c"
        self.bg_panel = "#222428"
        self.bg_card = "#2b2e33"
        self.fg_text = "#f1f3f5"
        self.fg_muted = "#adb5bd"
        self.accent_blue = "#3b82f6"
        self.accent_green = "#10b981"
        self.accent_purple = "#8b5cf6"
        self.accent_amber = "#f59e0b"

        self.root.configure(bg=self.bg_main)

        # Trạng thái dữ liệu
        self.image_paths = []
        self.current_img_idx = 0
        self.raw_image_rgb = None       # Ảnh gốc RGB
        self.scaled_image_rgb = None    # Ảnh đã scale về chiều rộng A4 (và tăng cường nếu bật)
        self.is_blank_rows = None       # Cache mảng dòng trống để tính lại trang cực nhanh
        self.enhance_enabled = False
        self.pad_full_a4 = tk.BooleanVar(value=True)

        self.cut_points = []            # Danh sách tọa độ Y các điểm cắt [0, y1, y2, ..., H]
        self.pages_pil = []             # Danh sách các trang PIL Image đã cắt
        self.current_page_idx = 0
        self.zoom_mode = "fit"          # "fit" hoặc float scale
        self.tk_preview_img = None

        self._build_ui()

    def _build_ui(self):
        # ================= THANH CÔNG CỤ TRÊN =================
        toolbar = tk.Frame(self.root, bg=self.bg_panel, height=64, padx=14, pady=10)
        toolbar.pack(side=tk.TOP, fill=tk.X)

        btn_style = {
            "font": ("Segoe UI", 10, "bold"),
            "fg": "#ffffff",
            "bd": 0,
            "padx": 14,
            "pady": 8,
            "cursor": "hand2",
            "activeforeground": "#ffffff",
        }

        self.btn_open = tk.Button(
            toolbar, text="📂 Mở ảnh (Open)", bg=self.accent_blue,
            activebackground="#2563eb", command=self.open_images, **btn_style
        )
        self.btn_open.pack(side=tk.LEFT, padx=(0, 8))

        self.btn_enhance = tk.Button(
            toolbar, text="✨ Tăng cường ảnh: TẮT", bg="#4b5563",
            activebackground="#6d28d9", command=self.toggle_enhance, **btn_style
        )
        self.btn_enhance.pack(side=tk.LEFT, padx=8)

        self.btn_process = tk.Button(
            toolbar, text="⚙️ Tiến hành & Xem trước", bg=self.accent_amber,
            activebackground="#d97706", command=self.process_and_preview, **btn_style
        )
        self.btn_process.pack(side=tk.LEFT, padx=8)

        self.btn_export = tk.Button(
            toolbar, text="📄 Xuất PDF", bg=self.accent_green,
            activebackground="#059669", command=self.export_pdf, **btn_style
        )
        self.btn_export.pack(side=tk.LEFT, padx=8)

        # Tùy chọn bù nền đủ chiều cao A4
        chk_pad = tk.Checkbutton(
            toolbar,
            text="Bù nền đủ khổ A4 chuẩn (210×297mm)",
            variable=self.pad_full_a4,
            command=self._on_pad_option_changed,
            bg=self.bg_panel,
            fg=self.fg_text,
            selectcolor=self.bg_card,
            activebackground=self.bg_panel,
            activeforeground=self.fg_text,
            font=("Segoe UI", 9)
        )
        chk_pad.pack(side=tk.RIGHT, padx=6)

        # ================= KHU VỰC CHÍNH =================
        main_body = tk.Frame(self.root, bg=self.bg_main)
        main_body.pack(side=tk.TOP, fill=tk.BOTH, expand=True, padx=10, pady=10)

        # --- CỘT TRÁI: DANH SÁCH FILE & DANH SÁCH TRANG CẮT ---
        left_panel = tk.Frame(main_body, bg=self.bg_panel, width=320, padx=10, pady=10)
        left_panel.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 10))
        left_panel.pack_propagate(False)

        tk.Label(
            left_panel, text="DANH SÁCH ẢNH ĐÃ CHỌN", bg=self.bg_panel,
            fg=self.fg_muted, font=("Segoe UI", 9, "bold")
        ).pack(anchor="w", pady=(0, 4))

        self.file_listbox = tk.Listbox(
            left_panel, height=4, bg=self.bg_card, fg=self.fg_text,
            selectbackground=self.accent_blue, bd=0, highlightthickness=0,
            font=("Segoe UI", 9), exportselection=False
        )
        self.file_listbox.pack(fill=tk.X, pady=(0, 10))
        self.file_listbox.bind("<<ListboxSelect>>", self.on_select_file)

        tk.Label(
            left_panel, text="CÁC TRANG A4 SAU KHI CẮT", bg=self.bg_panel,
            fg=self.fg_muted, font=("Segoe UI", 9, "bold")
        ).pack(anchor="w", pady=(0, 4))

        page_list_frame = tk.Frame(left_panel, bg=self.bg_card)
        page_list_frame.pack(fill=tk.BOTH, expand=True)

        scrollbar_pages = tk.Scrollbar(page_list_frame)
        scrollbar_pages.pack(side=tk.RIGHT, fill=tk.Y)

        self.page_listbox = tk.Listbox(
            page_list_frame, bg=self.bg_card, fg=self.fg_text,
            selectbackground=self.accent_green, bd=0, highlightthickness=0,
            font=("Segoe UI", 9), yscrollcommand=scrollbar_pages.set,
            exportselection=False
        )
        self.page_listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar_pages.config(command=self.page_listbox.yview)
        self.page_listbox.bind("<<ListboxSelect>>", self.on_select_page)

        # Khung tinh chỉnh thủ công điểm cắt cuối của trang đang xem
        adjust_frame = tk.LabelFrame(
            left_panel, text="Tinh chỉnh mép cắt dưới trang này",
            bg=self.bg_panel, fg=self.fg_text, font=("Segoe UI", 9, "bold"),
            padx=8, pady=8
        )
        adjust_frame.pack(fill=tk.X, pady=(10, 0))

        # Thông báo trực tiếp ngay phía trên cụm nút bấm
        self.lbl_cut_notice = tk.Label(
            adjust_frame,
            text="⚠️ Lưu ý: Lùi lên nhiều sẽ tự đổi kích thước & chia lại toàn bộ các trang phía dưới.",
            bg="#2d2618",
            fg="#fbbf24",
            font=("Segoe UI", 8, "bold"),
            wraplength=275,
            justify="left",
            padx=8,
            pady=6
        )
        self.lbl_cut_notice.pack(fill=tk.X, pady=(0, 8))

        small_btn = {
            "bg": self.bg_card, "fg": self.fg_text, "bd": 0,
            "font": ("Segoe UI", 9, "bold"), "pady": 5, "cursor": "hand2",
            "activebackground": "#3b3f46", "activeforeground": "#ffffff"
        }

        # Hàng 1: Tinh chỉnh bước nhỏ ±10px
        btn_adj_row1 = tk.Frame(adjust_frame, bg=self.bg_panel)
        btn_adj_row1.pack(fill=tk.X, pady=(0, 4))

        tk.Button(
            btn_adj_row1, text="▲ Lùi lên 10px",
            command=lambda: self.adjust_current_cut(-10), **small_btn
        ).pack(side=tk.LEFT, expand=True, fill=tk.X, padx=(0, 3))

        tk.Button(
            btn_adj_row1, text="▼ Hạ xuống 10px",
            command=lambda: self.adjust_current_cut(10), **small_btn
        ).pack(side=tk.LEFT, expand=True, fill=tk.X, padx=(3, 0))

        # Hàng 2: Tinh chỉnh bước nhanh ±50px (tiện khi cần né khối hình/công thức cao)
        btn_adj_row2 = tk.Frame(adjust_frame, bg=self.bg_panel)
        btn_adj_row2.pack(fill=tk.X)

        tk.Button(
            btn_adj_row2, text="⏫ Lùi nhanh 50px",
            command=lambda: self.adjust_current_cut(-50), **small_btn
        ).pack(side=tk.LEFT, expand=True, fill=tk.X, padx=(0, 3))

        tk.Button(
            btn_adj_row2, text="⏬ Hạ nhanh 50px",
            command=lambda: self.adjust_current_cut(50), **small_btn
        ).pack(side=tk.LEFT, expand=True, fill=tk.X, padx=(3, 0))

        # --- CỘT PHẢI: KHUNG XEM TRƯỚC (PREVIEW) ---
        right_panel = tk.Frame(main_body, bg=self.bg_panel)
        right_panel.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        # Thanh điều hướng trang preview
        nav_bar = tk.Frame(right_panel, bg=self.bg_card, height=42, padx=10, pady=6)
        nav_bar.pack(side=tk.TOP, fill=tk.X)

        nav_btn_style = {
            "bg": "#374151", "fg": self.fg_text, "bd": 0,
            "font": ("Segoe UI", 9, "bold"), "padx": 10, "pady": 4, "cursor": "hand2"
        }
        tk.Button(nav_bar, text="◀ Trang trước", command=self.prev_page, **nav_btn_style).pack(side=tk.LEFT)
        self.lbl_page_info = tk.Label(
            nav_bar, text="Chưa có dữ liệu xem trước", bg=self.bg_card,
            fg=self.fg_text, font=("Segoe UI", 10, "bold"), padx=14
        )
        self.lbl_page_info.pack(side=tk.LEFT)
        tk.Button(nav_bar, text="Trang sau ▶", command=self.next_page, **nav_btn_style).pack(side=tk.LEFT)

        tk.Button(nav_bar, text="Vừa khung", command=lambda: self.set_zoom("fit"), **nav_btn_style).pack(side=tk.RIGHT, padx=3)
        tk.Button(nav_bar, text="Zoom +", command=lambda: self.change_zoom(1.25), **nav_btn_style).pack(side=tk.RIGHT, padx=3)
        tk.Button(nav_bar, text="Zoom -", command=lambda: self.change_zoom(0.8), **nav_btn_style).pack(side=tk.RIGHT, padx=3)

        # Canvas hiển thị ảnh xem trước có thanh cuộn
        canvas_container = tk.Frame(right_panel, bg="#111214")
        canvas_container.pack(side=tk.TOP, fill=tk.BOTH, expand=True)

        self.v_scroll = tk.Scrollbar(canvas_container, orient=tk.VERTICAL)
        self.v_scroll.pack(side=tk.RIGHT, fill=tk.Y)
        self.h_scroll = tk.Scrollbar(canvas_container, orient=tk.HORIZONTAL)
        self.h_scroll.pack(side=tk.BOTTOM, fill=tk.X)

        self.canvas = tk.Canvas(
            canvas_container, bg="#111214", highlightthickness=0,
            yscrollcommand=self.v_scroll.set, xscrollcommand=self.h_scroll.set
        )
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        self.v_scroll.config(command=self.canvas.yview)
        self.h_scroll.config(command=self.canvas.xview)

        self.canvas.bind("<MouseWheel>", self._on_mousewheel)
        self.canvas.bind("<Configure>", lambda e: self.render_preview() if self.zoom_mode == "fit" else None)

        # Thanh trạng thái dưới cùng
        self.status_var = tk.StringVar(value="Sẵn sàng. Bấm 'Mở ảnh (Open)' để chọn file JPG, PNG, WEBP...")
        status_bar = tk.Label(
            self.root, textvariable=self.status_var, bg=self.bg_panel,
            fg=self.fg_muted, anchor="w", padx=12, pady=6, font=("Segoe UI", 9)
        )
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)

    # ================= XỬ LÝ MỞ FILE =================
    def open_images(self):
        filetypes = [
            ("Tất cả file ảnh", "*.jpg *.jpeg *.png *.webp *.bmp *.tif *.tiff"),
            ("PNG files", "*.png"),
            ("JPEG files", "*.jpg *.jpeg"),
            ("Tất cả file", "*.*"),
        ]
        paths = filedialog.askopenfilenames(
            title="Chọn ảnh cần cắt sang khổ A4",
            filetypes=filetypes
        )
        if not paths:
            return

        self.image_paths = list(paths)
        self.file_listbox.delete(0, tk.END)
        for p in self.image_paths:
            self.file_listbox.insert(tk.END, os.path.basename(p))

        self.file_listbox.selection_set(0)
        self.load_image_at_index(0)

    def on_select_file(self, event):
        sel = self.file_listbox.curselection()
        if not sel:
            return
        idx = sel[0]
        if idx != self.current_img_idx:
            self.load_image_at_index(idx)

    def load_image_at_index(self, idx: int):
        self.current_img_idx = idx
        path = self.image_paths[idx]
        try:
            pil_img = Image.open(path)
            pil_img = ImageOps.exif_transpose(pil_img)

            if pil_img.mode in ("RGBA", "LA") or (pil_img.mode == "P" and "transparency" in pil_img.info):
                rgba = pil_img.convert("RGBA")
                bg = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
                pil_img = Image.alpha_composite(bg, rgba).convert("RGB")
            else:
                pil_img = pil_img.convert("RGB")

            self.raw_image_rgb = np.array(pil_img)
            self.is_blank_rows = None
            h, w, _ = self.raw_image_rgb.shape
            self.pages_pil = []
            self.cut_points = []
            self.page_listbox.delete(0, tk.END)

            self.tk_preview_img = None
            self.lbl_page_info.config(text=f"Ảnh gốc: {w} × {h} px (Bấm 'Tiến hành & Xem trước')")
            self.status_var.set(f"Đã tải: {os.path.basename(path)} ({w}×{h} px). Bấm 'Tiến hành & Xem trước' để cắt trang.")
            self._show_raw_preview(pil_img)

        except Exception as e:
            messagebox.showerror("Lỗi mở ảnh", f"Không thể mở file ảnh:\n{e}")

    def _show_raw_preview(self, pil_img: Image.Image):
        self.canvas.delete("all")
        cw = max(self.canvas.winfo_width(), 600)
        scale = min(1.0, (cw - 40) / pil_img.width)
        new_w = max(1, int(pil_img.width * scale))
        new_h = max(1, int(pil_img.height * scale))
        disp = pil_img.resize((new_w, new_h), Image.Resampling.BILINEAR)
        self.tk_preview_img = ImageTk.PhotoImage(disp)
        self.canvas.create_image(20, 20, anchor="nw", image=self.tk_preview_img)
        self.canvas.config(scrollregion=(0, 0, new_w + 40, new_h + 40))

    # ================= TĂNG CƯỜNG ẢNH =================
    def toggle_enhance(self):
        self.enhance_enabled = not self.enhance_enabled
        if self.enhance_enabled:
            self.btn_enhance.config(text="✨ Tăng cường ảnh: BẬT", bg=self.accent_purple)
            self.status_var.set("Đã BẬT chế độ tăng cường độ nét & khử vỡ chữ.")
        else:
            self.btn_enhance.config(text="✨ Tăng cường ảnh: TẮT", bg="#4b5563")
            self.status_var.set("Đã TẮT chế độ tăng cường ảnh.")

        if self.raw_image_rgb is not None and self.pages_pil:
            self.process_and_preview()

    def _prepare_a4_width_image(self) -> np.ndarray:
        """Đưa chiều rộng ảnh về đúng chiều rộng A4 thật (2480 px) và tăng cường nét nếu bật."""
        h, w, _ = self.raw_image_rgb.shape
        scale = A4_WIDTH_PX / float(w)
        new_h = max(1, int(round(h * scale)))

        resized = cv2.resize(
            self.raw_image_rgb,
            (A4_WIDTH_PX, new_h),
            interpolation=cv2.INTER_LANCZOS4
        )

        if self.enhance_enabled:
            denoised = cv2.bilateralFilter(resized, d=5, sigmaColor=35, sigmaSpace=35)
            gaussian = cv2.GaussianBlur(denoised, (0, 0), sigmaX=1.4)
            sharpened = cv2.addWeighted(denoised, 1.55, gaussian, -0.55, 0)
            lab = cv2.cvtColor(sharpened, cv2.COLOR_RGB2LAB)
            l_ch, a_ch, b_ch = cv2.split(lab)
            clahe = cv2.createCLAHE(clipLimit=1.5, tileGridSize=(8, 8))
            l_ch = clahe.apply(l_ch)
            enhanced = cv2.cvtColor(cv2.merge([l_ch, a_ch, b_ch]), cv2.COLOR_LAB2RGB)
            return enhanced

        return resized

    # ================= THUẬT TOÁN DÒ KHOẢNG TRỐNG & CẮT TRANG =================
    def _detect_blank_rows(self, img_rgb: np.ndarray) -> np.ndarray:
        """Trả về mảng boolean is_blank[y]: True nếu hàng y là khoảng trống (bất kể nền đen hay trắng)."""
        gray = cv2.cvtColor(img_rgb, cv2.COLOR_RGB2GRAY).astype(np.float32)
        h, w = gray.shape

        x1 = int(w * 0.02)
        x2 = int(w * 0.97)
        roi = gray[:, x1:x2]

        diff_x = np.abs(np.diff(roi, axis=1))
        text_pixels_x = np.sum(diff_x > 10.0, axis=1)

        diff_y = np.zeros_like(roi)
        diff_y[1:, :] = np.abs(roi[1:, :] - roi[:-1, :])
        line_pixels_y = np.sum(diff_y > 12.0, axis=1)

        is_blank = (text_pixels_x <= 2) & (line_pixels_y <= 6)
        return is_blank

    def _compute_smart_cuts(self, img_rgb: np.ndarray, start_y: int = 0) -> list:
        """Tính danh sách tọa độ Y cắt trang bắt đầu từ start_y sao cho mỗi lát <= A4_HEIGHT_PX."""
        total_h = img_rgb.shape[0]
        if self.is_blank_rows is None or len(self.is_blank_rows) != total_h:
            self.is_blank_rows = self._detect_blank_rows(img_rgb)
        is_blank = self.is_blank_rows

        cuts = [int(start_y)]
        y_start = int(start_y)
        safe_margin = 14
        min_page_h = int(A4_HEIGHT_PX * 0.45)

        while y_start < total_h:
            y_target = y_start + A4_HEIGHT_PX
            if y_target >= total_h:
                cuts.append(total_h)
                break

            y_search_min = max(y_start + min_page_h, y_start + 100)
            chosen_cut = None

            # Bước 1: Tìm dải khoảng trống an toàn (>= 14px liên tục trống) lùi từ y_target lên
            half = safe_margin // 2
            for y in range(y_target - half, y_search_min, -1):
                if np.all(is_blank[y - half : y + half + 1]):
                    top_gap = y - half
                    while top_gap > y_search_min and (y + half - top_gap) < 40 and is_blank[top_gap - 1]:
                        top_gap -= 1
                    chosen_cut = (top_gap + (y + half)) // 2
                    break

            # Bước 2 (Dự phòng): Giảm khe trống xuống 4px nếu đoạn chứa hình/bảng dày đặc
            if chosen_cut is None:
                for y in range(y_target - 2, y_search_min, -1):
                    if np.all(is_blank[y - 2 : y + 3]):
                        chosen_cut = y
                        break

            # Bước 3 (Dự phòng cuối cùng): Chọn hàng ít biến thiên pixel nhất
            if chosen_cut is None:
                gray_sub = cv2.cvtColor(
                    img_rgb[y_search_min:y_target, int(A4_WIDTH_PX * 0.02):int(A4_WIDTH_PX * 0.97)],
                    cv2.COLOR_RGB2GRAY
                ).astype(np.float32)
                row_energy = np.sum(np.abs(np.diff(gray_sub, axis=1)), axis=1)
                best_offset = int(np.argmin(row_energy))
                chosen_cut = y_search_min + best_offset

            if chosen_cut <= y_start:
                chosen_cut = min(y_target, total_h)

            cuts.append(int(chosen_cut))
            y_start = int(chosen_cut)

        return cuts

    def _build_pages_from_cuts(self):
        """Tạo danh sách ảnh PIL cho từng trang từ self.scaled_image_rgb và self.cut_points."""
        if self.scaled_image_rgb is None or len(self.cut_points) < 2:
            return

        self.pages_pil = []
        self.page_listbox.delete(0, tk.END)
        w = self.scaled_image_rgb.shape[1]

        for idx in range(len(self.cut_points) - 1):
            y1 = self.cut_points[idx]
            y2 = self.cut_points[idx + 1]
            slice_arr = self.scaled_image_rgb[y1:y2, :, :]
            slice_h = y2 - y1

            if self.pad_full_a4.get():
                ref_row = slice_arr[-1, int(w * 0.05):int(w * 0.95), :]
                bg_color = tuple(np.median(ref_row, axis=0).astype(np.uint8).tolist())

                canvas_arr = np.full((A4_HEIGHT_PX, A4_WIDTH_PX, 3), bg_color, dtype=np.uint8)
                canvas_arr[:slice_h, :, :] = slice_arr
                page_img = Image.fromarray(canvas_arr)
            else:
                page_img = Image.fromarray(slice_arr)

            self.pages_pil.append(page_img)
            self.page_listbox.insert(
                tk.END,
                f"Trang {idx + 1}: cao {slice_h} px (Y: {y1} → {y2})"
            )

    def process_and_preview(self):
        if self.raw_image_rgb is None:
            messagebox.showwarning("Chưa có ảnh", "Vui lòng bấm 'Mở ảnh (Open)' để chọn ảnh trước.")
            return

        self.status_var.set("Đang xử lý co kích thước A4 và dò khoảng trống ngắt trang...")
        self.root.update_idletasks()

        self.scaled_image_rgb = self._prepare_a4_width_image()
        self.is_blank_rows = self._detect_blank_rows(self.scaled_image_rgb)
        self.cut_points = self._compute_smart_cuts(self.scaled_image_rgb, start_y=0)
        self._build_pages_from_cuts()

        self.current_page_idx = 0
        self.page_listbox.selection_clear(0, tk.END)
        self.page_listbox.selection_set(0)
        self._refresh_cut_notice()
        self.render_preview()

        num_pages = len(self.pages_pil)
        self.status_var.set(
            f"Hoàn tất! Đã ngắt thành {num_pages} trang A4 (rộng {A4_WIDTH_PX} px, cao mỗi lát ≤ {A4_HEIGHT_PX} px)."
        )

    def _on_pad_option_changed(self):
        if self.scaled_image_rgb is not None and self.cut_points:
            self._build_pages_from_cuts()
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.render_preview()

    # ================= TINH CHỈNH THỦ CÔNG & TỰ ĐỘNG TÍNH LẠI TRANG SAU =================
    def _refresh_cut_notice(self, just_recalculated: bool = False):
        """Cập nhật nội dung thông báo ngay phía trên các nút lùi/hạ mép cắt."""
        if not self.pages_pil or not self.cut_points:
            self.lbl_cut_notice.config(
                text="⚠️ Lưu ý: Lùi lên nhiều sẽ tự đổi kích thước & chia lại toàn bộ các trang phía dưới.",
                bg="#2d2618", fg="#fbbf24"
            )
            return

        idx = self.current_page_idx + 1
        if idx >= len(self.cut_points) - 1:
            self.lbl_cut_notice.config(
                text="ℹ️ Trang cuối kết thúc ở đáy ảnh. Bấm 'Lùi lên' nếu muốn tách thêm trang mới.",
                bg="#1f2937", fg="#93c5fd"
            )
            return

        curr_cut = self.cut_points[idx]
        next_cut = self.cut_points[idx + 1]
        remaining_room = A4_HEIGHT_PX - (next_cut - curr_cut)

        if just_recalculated:
            self.lbl_cut_notice.config(
                text="⚡ Đã ưu tiên mép cắt trang này & tự động chia lại toàn bộ các trang phía dưới!",
                bg="#3b2611", fg="#fdba74"
            )
        elif remaining_room <= 60:
            self.lbl_cut_notice.config(
                text=f"⚠️ Sắp chạm ngưỡng trang dưới (còn {max(0, remaining_room)} px): Lùi thêm sẽ tự tính lại toàn bộ các trang dưới!",
                bg="#3b2611", fg="#fbbf24"
            )
        else:
            self.lbl_cut_notice.config(
                text=f"⚠️ Lùi lên quá {remaining_room} px nữa sẽ đổi kích thước & tự tính lại toàn bộ các trang dưới.",
                bg="#2d2618", fg="#fbbf24"
            )

    def adjust_current_cut(self, delta_y: int):
        """
        Cho phép lùi lên / hạ xuống tự do.
        Ưu tiên tuyệt đối điểm ngắt của trang đang chỉnh; nếu làm trang dưới vượt khổ A4
        thì tự động tính lại toàn bộ kích thước và điểm cắt của tất cả các trang phía sau.
        """
        if not self.pages_pil or self.scaled_image_rgb is None:
            return

        total_h = self.scaled_image_rgb.shape[0]
        idx = self.current_page_idx + 1
        y_prev = self.cut_points[idx - 1]
        curr_cut = self.cut_points[idx]
        new_y = curr_cut + delta_y

        # Trường hợp đang ở trang cuối cùng (curr_cut == total_h)
        if idx == len(self.cut_points) - 1:
            if delta_y >= 0:
                self.lbl_cut_notice.config(
                    text="ℹ️ Mép dưới đã chạm đáy ảnh gốc, không thể hạ thêm.",
                    bg="#1f2937", fg="#93c5fd"
                )
                return
            if new_y < y_prev + 80:
                return
            tail_cuts = self._compute_smart_cuts(self.scaled_image_rgb, start_y=new_y)
            self.cut_points = self.cut_points[:idx] + tail_cuts
            self._build_pages_from_cuts()
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self._refresh_cut_notice(just_recalculated=True)
            self.render_preview()
            return

        # Ràng buộc trang hiện tại: không cao quá 1 tờ A4 (3508 px) và không nhỏ hơn 80 px
        if new_y - y_prev > A4_HEIGHT_PX:
            new_y = y_prev + A4_HEIGHT_PX
            if new_y == curr_cut:
                self.lbl_cut_notice.config(
                    text=f"ℹ️ Trang này đã đạt chiều cao tối đa 1 tờ A4 ({A4_HEIGHT_PX} px).",
                    bg="#1f2937", fg="#93c5fd"
                )
                return
        if new_y < y_prev + 80:
            self.lbl_cut_notice.config(
                text="ℹ️ Trang hiện tại đã đạt chiều cao tối thiểu (80 px).",
                bg="#1f2937", fg="#93c5fd"
            )
            return

        y_next = self.cut_points[idx + 1]
        recalculated = False

        if delta_y < 0:
            # Nếu lùi lên làm trang kế tiếp dài hơn A4 -> khóa điểm ngắt trang này & tính lại toàn bộ các trang sau
            if (y_next - new_y) > A4_HEIGHT_PX:
                tail_cuts = self._compute_smart_cuts(self.scaled_image_rgb, start_y=new_y)
                self.cut_points = self.cut_points[:idx] + tail_cuts
                recalculated = True
            else:
                self.cut_points[idx] = new_y
        else:
            # Hạ xuống: nếu vượt sát hoặc qua điểm cắt trang kế tiếp -> tính lại các trang sau từ new_y
            if new_y >= total_h:
                self.cut_points = self.cut_points[:idx] + [total_h]
                recalculated = True
            elif new_y >= y_next - 80:
                tail_cuts = self._compute_smart_cuts(self.scaled_image_rgb, start_y=new_y)
                self.cut_points = self.cut_points[:idx] + tail_cuts
                recalculated = True
            else:
                self.cut_points[idx] = new_y

        self._build_pages_from_cuts()
        self.page_listbox.selection_clear(0, tk.END)
        self.page_listbox.selection_set(self.current_page_idx)
        self.page_listbox.see(self.current_page_idx)
        self._refresh_cut_notice(just_recalculated=recalculated)
        self.render_preview()

        if recalculated:
            self.status_var.set(
                f"Đã giữ mép cắt Trang {self.current_page_idx + 1} tại Y = {new_y} px và chia lại toàn bộ {len(self.pages_pil)} trang."
            )

    def on_select_page(self, event):
        sel = self.page_listbox.curselection()
        if not sel:
            return
        self.current_page_idx = sel[0]
        self._refresh_cut_notice()
        self.render_preview()

    def prev_page(self):
        if not self.pages_pil:
            return
        if self.current_page_idx > 0:
            self.current_page_idx -= 1
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.page_listbox.see(self.current_page_idx)
            self._refresh_cut_notice()
            self.render_preview()

    def next_page(self):
        if not self.pages_pil:
            return
        if self.current_page_idx < len(self.pages_pil) - 1:
            self.current_page_idx += 1
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.page_listbox.see(self.current_page_idx)
            self._refresh_cut_notice()
            self.render_preview()

    def set_zoom(self, mode):
        self.zoom_mode = mode
        self.render_preview()

    def change_zoom(self, factor: float):
        if not self.pages_pil:
            return
        if self.zoom_mode == "fit":
            cw = max(self.canvas.winfo_width(), 600)
            self.zoom_mode = (cw - 60) / A4_WIDTH_PX
        self.zoom_mode = max(0.15, min(2.0, self.zoom_mode * factor))
        self.render_preview()

    def render_preview(self):
        if not self.pages_pil:
            return

        page_img = self.pages_pil[self.current_page_idx]
        y1 = self.cut_points[self.current_page_idx]
        y2 = self.cut_points[self.current_page_idx + 1]
        slice_h = y2 - y1

        self.lbl_page_info.config(
            text=f"Trang {self.current_page_idx + 1} / {len(self.pages_pil)}  |  "
                 f"Nội dung: {A4_WIDTH_PX} × {slice_h} px (Tối đa A4: {A4_HEIGHT_PX} px)"
        )

        cw = max(self.canvas.winfo_width(), 600)
        if self.zoom_mode == "fit":
            scale = (cw - 60) / float(page_img.width)
        else:
            scale = float(self.zoom_mode)

        disp_w = max(1, int(page_img.width * scale))
        disp_h = max(1, int(page_img.height * scale))
        disp_img = page_img.resize((disp_w, disp_h), Image.Resampling.BILINEAR)

        self.tk_preview_img = ImageTk.PhotoImage(disp_img)
        self.canvas.delete("all")

        offset_x = max(20, (cw - disp_w) // 2)
        offset_y = 20
        self.canvas.create_image(offset_x, offset_y, anchor="nw", image=self.tk_preview_img)

        # Vẽ đường đứt nét đỏ đánh dấu ranh giới kết thúc nội dung
        cut_line_y = offset_y + int(slice_h * scale)
        self.canvas.create_line(
            offset_x, cut_line_y, offset_x + disp_w, cut_line_y,
            fill="#ef4444", width=2, dash=(6, 4)
        )
        self.canvas.create_text(
            offset_x + disp_w - 8, cut_line_y - 10,
            text="▲ Ngắt trang",
            fill="#ef4444", anchor="e", font=("Segoe UI", 6, "bold")
        )

        # Viền khung tờ giấy A4
        self.canvas.create_rectangle(
            offset_x, offset_y, offset_x + disp_w, offset_y + disp_h,
            outline="#6b7280", width=1
        )
        self.canvas.config(scrollregion=(0, 0, disp_w + offset_x * 2, disp_h + 40))

    def _on_mousewheel(self, event):
        self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

    # ================= XUẤT FILE PDF =================
    def export_pdf(self):
        if not self.pages_pil:
            if self.raw_image_rgb is not None:
                self.process_and_preview()
            else:
                messagebox.showwarning("Chưa có dữ liệu", "Vui lòng mở ảnh và bấm 'Tiến hành & Xem trước' trước khi xuất PDF.")
                return

        src_path = self.image_paths[self.current_img_idx]
        default_name = os.path.splitext(os.path.basename(src_path))[0] + "_A4.pdf"

        save_path = filedialog.asksaveasfilename(
            title="Lưu file PDF khổ A4",
            initialfile=default_name,
            defaultextension=".pdf",
            filetypes=[("PDF Document", "*.pdf")]
        )
        if not save_path:
            return

        try:
            self.status_var.set("Đang xuất file PDF chất lượng cao...")
            self.root.update_idletasks()

            first_page = self.pages_pil[0]
            rest_pages = self.pages_pil[1:]

            first_page.save(
                save_path,
                "PDF",
                resolution=float(A4_DPI),
                save_all=True,
                append_images=rest_pages
            )

            self.status_var.set(f"Xuất PDF thành công: {save_path}")
            messagebox.showinfo(
                "Thành công",
                f"Đã xuất {len(self.pages_pil)} trang khổ A4 ra file:\n{save_path}"
            )
        except Exception as e:
            messagebox.showerror("Lỗi xuất PDF", f"Không thể lưu file PDF:\n{e}")


if __name__ == "__main__":
    root = tk.Tk()
    app = SmartA4SplitterApp(root)
    root.mainloop()
