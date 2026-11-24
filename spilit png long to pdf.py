import os
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
import numpy as np
import cv2
from PIL import Image, ImageTk, ImageOps

# Cho phép mở ảnh chụp màn hình siêu dài (như 1824 x 41617 px) mà không bị giới hạn pixel
Image.MAX_IMAGE_PIXELS = None

# Chuẩn kích thước A4 thật ở 300 DPI (210mm x 297mm)
A4_DPI = 300
A4_WIDTH_PX = 2480
A4_HEIGHT_PX = 3508


class SmartA4SplitterApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Smart A4 Image Splitter & PDF Exporter")
        self.root.geometry("1280x820")
        self.root.minsize(1050, 680)

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
        left_panel = tk.Frame(main_body, bg=self.bg_panel, width=310, padx=10, pady=10)
        left_panel.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 10))
        left_panel.pack_propagate(False)

        tk.Label(
            left_panel, text="DANH SÁCH ẢNH ĐÃ CHỌN", bg=self.bg_panel,
            fg=self.fg_muted, font=("Segoe UI", 9, "bold")
        ).pack(anchor="w", pady=(0, 4))

        self.file_listbox = tk.Listbox(
            left_panel, height=5, bg=self.bg_card, fg=self.fg_text,
            selectbackground=self.accent_blue, bd=0, highlightthickness=0,
            font=("Segoe UI", 9), exportselection=False
        )
        self.file_listbox.pack(fill=tk.X, pady=(0, 12))
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
        adjust_frame.pack(fill=tk.X, pady=(12, 0))

        btn_adj_row = tk.Frame(adjust_frame, bg=self.bg_panel)
        btn_adj_row.pack(fill=tk.X)

        small_btn = {
            "bg": self.bg_card, "fg": self.fg_text, "bd": 0,
            "font": ("Segoe UI", 9, "bold"), "pady": 5, "cursor": "hand2"
        }
        tk.Button(
            btn_adj_row, text="▲ Lùi lên 10px",
            command=lambda: self.adjust_current_cut(-10), **small_btn
        ).pack(side=tk.LEFT, expand=True, fill=tk.X, padx=(0, 3))

        tk.Button(
            btn_adj_row, text="▼ Hạ xuống 10px",
            command=lambda: self.adjust_current_cut(10), **small_btn
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

        # Hỗ trợ cuộn chuột trên Canvas
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
            # Dùng Pillow đọc để hỗ trợ 100% đường dẫn tiếng Việt có dấu trên Windows
            pil_img = Image.open(path)
            pil_img = ImageOps.exif_transpose(pil_img)

            # Nếu ảnh PNG có kênh trong suốt (RGBA), ghép lên nền phù hợp
            if pil_img.mode in ("RGBA", "LA") or (pil_img.mode == "P" and "transparency" in pil_img.info):
                rgba = pil_img.convert("RGBA")
                bg = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
                pil_img = Image.alpha_composite(bg, rgba).convert("RGB")
            else:
                pil_img = pil_img.convert("RGB")

            self.raw_image_rgb = np.array(pil_img)
            h, w, _ = self.raw_image_rgb.shape
            self.pages_pil = []
            self.cut_points = []
            self.page_listbox.delete(0, tk.END)

            # Hiển thị nhanh ảnh gốc lên khung preview
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

        # Co giãn chất lượng cao bằng Lanczos4
        resized = cv2.resize(
            self.raw_image_rgb,
            (A4_WIDTH_PX, new_h),
            interpolation=cv2.INTER_LANCZOS4
        )

        if self.enhance_enabled:
            # 1. Khử nhiễu nén JPEG nhẹ mà vẫn giữ biên chữ sắc cạnh
            denoised = cv2.bilateralFilter(resized, d=5, sigmaColor=35, sigmaSpace=35)
            # 2. Unsharp Masking làm nét ký tự và công thức
            gaussian = cv2.GaussianBlur(denoised, (0, 0), sigmaX=1.4)
            sharpened = cv2.addWeighted(denoised, 1.55, gaussian, -0.55, 0)
            # 3. Tăng nhẹ độ tương phản kênh độ sáng (LAB) để chữ tách khỏi nền rõ ràng
            lab = cv2.cvtColor(sharpened, cv2.COLOR_RGB2LAB)
            l_ch, a_ch, b_ch = cv2.split(lab)
            clahe = cv2.createCLAHE(clipLimit=1.5, tileGridSize=(8, 8))
            l_ch = clahe.apply(l_ch)
            enhanced = cv2.cvtColor(cv2.merge([l_ch, a_ch, b_ch]), cv2.COLOR_LAB2RGB)
            return enhanced

        return resized

    # ================= THUẬT TOÁN DÒ KHOẢNG TRỐNG & CẮT TRANG =================
    def _detect_blank_rows(self, img_rgb: np.ndarray) -> np.ndarray:
        """
        Trả về mảng boolean is_blank[y] dài H:
        True nếu hàng y là khoảng trống (không chứa chữ, ký tự, đường kẻ),
        hoạt động tốt với cả nền đen, nền trắng, nền xám hoặc vùng chuyển màu.
        """
        gray = cv2.cvtColor(img_rgb, cv2.COLOR_RGB2GRAY).astype(np.float32)
        h, w = gray.shape

        # Bỏ qua 2% mép trái và 3% mép phải để tránh thanh cuộn (scrollbar) làm nhiễu
        x1 = int(w * 0.02)
        x2 = int(w * 0.97)
        roi = gray[:, x1:x2]

        # 1. Biên thiên theo phương ngang (phát hiện chữ, ký tự, nét thẳng đứng)
        diff_x = np.abs(np.diff(roi, axis=1))
        # Đếm số pixel trên hàng có độ chênh sáng > 10
        text_pixels_x = np.sum(diff_x > 10.0, axis=1)

        # 2. Biến thiên theo phương dọc (phát hiện đường kẻ ngang phân cách)
        diff_y = np.zeros_like(roi)
        diff_y[1:, :] = np.abs(roi[1:, :] - roi[:-1, :])
        line_pixels_y = np.sum(diff_y > 12.0, axis=1)

        # Một hàng được coi là trống khi hầu như không có cạnh ngang lẫn cạnh dọc
        is_blank = (text_pixels_x <= 2) & (line_pixels_y <= 6)
        return is_blank

    def _compute_smart_cuts(self, img_rgb: np.ndarray) -> list:
        """Tính danh sách tọa độ Y cắt trang sao cho chiều cao mỗi lát <= A4_HEIGHT_PX và không lẹm chữ."""
        total_h = img_rgb.shape[0]
        is_blank = self._detect_blank_rows(img_rgb)

        cuts = [0]
        y_start = 0
        safe_margin = 14          # Đệm an toàn tối thiểu 14px (7px trên + 7px dưới) để tránh chạm dấu chữ
        min_page_h = int(A4_HEIGHT_PX * 0.45)  # Không lùi quá 55% trang

        while y_start < total_h:
            y_target = y_start + A4_HEIGHT_PX
            if y_target >= total_h:
                cuts.append(total_h)
                break

            y_search_min = max(y_start + min_page_h, y_start + 100)
            chosen_cut = None

            # Bước 1: Tìm dải khoảng trống an toàn (>= safe_margin px liên tục đều trống) lùi dần từ y_target lên
            half = safe_margin // 2
            for y in range(y_target - half, y_search_min, -1):
                if np.all(is_blank[y - half : y + half + 1]):
                    # Mở rộng lên trên một chút để lấy đúng tâm khoảng trống giữa 2 dòng chữ
                    top_gap = y - half
                    while top_gap > y_search_min and (y + half - top_gap) < 40 and is_blank[top_gap - 1]:
                        top_gap -= 1
                    chosen_cut = (top_gap + (y + half)) // 2
                    break

            # Bước 2 (Dự phòng): Nếu gặp hình/bảng quá lớn không có khe trống 14px, giảm khe xuống 4px
            if chosen_cut is None:
                for y in range(y_target - 2, y_search_min, -1):
                    if np.all(is_blank[y - 2 : y + 3]):
                        chosen_cut = y
                        break

            # Bước 3 (Dự phòng cuối cùng): Chọn hàng ít biến thiên pixel nhất trong vùng cho phép
            if chosen_cut is None:
                gray_sub = cv2.cvtColor(
                    img_rgb[y_search_min:y_target, int(A4_WIDTH_PX * 0.02):int(A4_WIDTH_PX * 0.97)],
                    cv2.COLOR_RGB2GRAY
                ).astype(np.float32)
                row_energy = np.sum(np.abs(np.diff(gray_sub, axis=1)), axis=1)
                best_offset = int(np.argmin(row_energy))
                chosen_cut = y_search_min + best_offset

            # Đảm bảo lát cắt luôn tiến tới và <= A4_HEIGHT_PX
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
                # Lấy màu nền tại đúng mép cắt cuối để bù phần dưới cho liền mạch (đen ra đen, trắng ra trắng)
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
        self.cut_points = self._compute_smart_cuts(self.scaled_image_rgb)
        self._build_pages_from_cuts()

        self.current_page_idx = 0
        self.page_listbox.selection_clear(0, tk.END)
        self.page_listbox.selection_set(0)
        self.render_preview()

        num_pages = len(self.pages_pil)
        self.status_var.set(
            f"Hoàn tất! Đã ngắt thành {num_pages} trang A4 (rộng {A4_WIDTH_PX} px, cao mỗi lát ≤ {A4_HEIGHT_PX} px). "
            f"Hãy kiểm tra từng trang trước khi Xuất PDF."
        )

    def _on_pad_option_changed(self):
        if self.scaled_image_rgb is not None and self.cut_points:
            self._build_pages_from_cuts()
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.render_preview()

    # ================= TINH CHỈNH THỦ CÔNG & XEM TRƯỚC =================
    def adjust_current_cut(self, delta_y: int):
        """Cho phép người dùng dịch mép cắt dưới của trang đang xem lên/xuống nếu muốn."""
        if not self.pages_pil or self.current_page_idx >= len(self.cut_points) - 2:
            messagebox.showinfo("Thông báo", "Trang cuối cùng kết thúc ở đáy ảnh nên không cần dịch mép cắt.")
            return

        idx = self.current_page_idx + 1
        y_prev = self.cut_points[idx - 1]
        y_next = self.cut_points[idx + 1]
        new_y = self.cut_points[idx] + delta_y

        # Ràng buộc chiều cao trang hiện tại <= A4_HEIGHT_PX và trang kế tiếp cũng <= A4_HEIGHT_PX
        if new_y - y_prev > A4_HEIGHT_PX:
            messagebox.showwarning("Giới hạn A4", f"Chiều cao trang không được vượt quá {A4_HEIGHT_PX} px (khổ A4).")
            return
        if y_next - new_y > A4_HEIGHT_PX:
            messagebox.showwarning("Giới hạn A4", "Lùi lên quá nhiều sẽ làm trang kế tiếp vượt quá chiều cao A4.")
            return
        if new_y <= y_prev + 50 or new_y >= y_next - 50:
            return

        self.cut_points[idx] = new_y
        self._build_pages_from_cuts()
        self.page_listbox.selection_clear(0, tk.END)
        self.page_listbox.selection_set(self.current_page_idx)
        self.render_preview()

    def on_select_page(self, event):
        sel = self.page_listbox.curselection()
        if not sel:
            return
        self.current_page_idx = sel[0]
        self.render_preview()

    def prev_page(self):
        if not self.pages_pil:
            return
        if self.current_page_idx > 0:
            self.current_page_idx -= 1
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.page_listbox.see(self.current_page_idx)
            self.render_preview()

    def next_page(self):
        if not self.pages_pil:
            return
        if self.current_page_idx < len(self.pages_pil) - 1:
            self.current_page_idx += 1
            self.page_listbox.selection_clear(0, tk.END)
            self.page_listbox.selection_set(self.current_page_idx)
            self.page_listbox.see(self.current_page_idx)
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

        # Vẽ đường đứt nét đỏ đánh dấu đúng ranh giới kết thúc nội dung của lát cắt để dễ kiểm tra lẹm chữ
        cut_line_y = offset_y + int(slice_h * scale)
        self.canvas.create_line(
            offset_x, cut_line_y, offset_x + disp_w, cut_line_y,
            fill="#ef4444", width=2, dash=(6, 4)
        )
        self.canvas.create_text(
            offset_x + disp_w - 8, cut_line_y - 10,
            text="▲ Mép cắt nội dung (Kiểm tra không lẹm chữ)",
            fill="#ef4444", anchor="e", font=("Segoe UI", 9, "bold")
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

            # Xuất PDF chuẩn 300 DPI -> mỗi trang có kích thước vật lý chính xác 210mm x 297mm (A4)
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