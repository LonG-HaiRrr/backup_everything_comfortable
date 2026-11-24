import os
import copy
import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk, ImageDraw

try:
    import fitz  # PyMuPDF
except ImportError:
    raise ImportError(
        "Thiếu thư viện PyMuPDF. Vui lòng cài đặt bằng lệnh:\n"
        "pip install pymupdf"
    )


class SmartPDFOrganizerApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Smart PDF Page Organizer & Exporter")
        self.root.geometry("1360x860")
        self.root.minsize(1120, 720)

        # Bảng màu Dark Mode giữ nguyên từ giao diện gốc
        self.bg_main = "#18191c"
        self.bg_panel = "#222428"
        self.bg_card = "#2b2e33"
        self.bg_canvas = "#141518"
        self.fg_text = "#f1f3f5"
        self.fg_muted = "#adb5bd"
        self.fg_disabled = "#5c6370"
        self.accent_blue = "#3b82f6"
        self.accent_green = "#10b981"
        self.accent_purple = "#8b5cf6"
        self.accent_amber = "#f59e0b"
        self.accent_red = "#ef4444"

        self.root.configure(bg=self.bg_main)

        # Trạng thái tài liệu PDF
        self.source_docs = {}           # {doc_id: fitz.Document}
        self.source_paths = {}          # {doc_id: file_path}
        self.next_doc_id = 0
        self.main_pdf_path = None
        self.main_total_pages = 0

        # Danh sách trang hiện tại còn lại:
        # Mỗi phần tử: {"uid": int, "doc_id": int, "orig_idx": int, "orig_num": int, "source_name": str, "rotation": int}
        self.pages = []
        self.next_uid = 1
        self.selected_indices = set()   # Tập index (0-based) đang chọn trong self.pages
        self.last_clicked_idx = None

        # Lịch sử Undo / Redo
        self.undo_stack = []
        self.redo_stack = []

        # Cache ảnh thumbnail: {(doc_id, orig_idx, rotation, thumb_w): ImageTk.PhotoImage}
        self.thumb_cache = {}
        self.thumb_width = 185          # Chiều rộng mặc định mỗi trang thu nhỏ (px)
        self.min_thumb_w = 110
        self.max_thumb_w = 340

        # Quản lý vùng click từng card trên Canvas
        self.card_bboxes = []           # [(x1, y1, x2, y2, page_idx), ...]
        self._syncing_listbox = False
        self._resize_after_id = None

        self._build_ui()
        self._bind_shortcuts()
        self._update_toolbar_states()

        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    # ================= XÂY DỰNG GIAO DIỆN =================
    def _build_ui(self):
        # ================= THANH CÔNG CỤ CHÍNH TRÊN CÙNG =================
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
            toolbar, text="📂 Mở PDF (Open)", bg=self.accent_blue,
            activebackground="#2563eb", command=self.open_pdf, **btn_style
        )
        self.btn_open.pack(side=tk.LEFT, padx=(0, 8))

        self.btn_export = tk.Button(
            toolbar, text="📄 Xuất PDF", bg=self.accent_green,
            activebackground="#059669", command=self.export_pdf, **btn_style
        )
        self.btn_export.pack(side=tk.LEFT, padx=8)

        # Nhãn tóm tắt nhanh trạng thái file ở góc phải thanh trên
        self.lbl_doc_summary = tk.Label(
            toolbar,
            text="Chưa mở file PDF nào",
            bg=self.bg_card,
            fg=self.fg_text,
            font=("Segoe UI", 9, "bold"),
            padx=12,
            pady=7
        )
        self.lbl_doc_summary.pack(side=tk.RIGHT, padx=4)

        # ================= KHU VỰC CHÍNH =================
        main_body = tk.Frame(self.root, bg=self.bg_main)
        main_body.pack(side=tk.TOP, fill=tk.BOTH, expand=True, padx=10, pady=10)

        # --- CỘT TRÁI: DANH SÁCH TRANG CÒN LẠI & BOX THỐNG KÊ TRANG ĐÃ BỊ BỎ ---
        left_panel = tk.Frame(main_body, bg=self.bg_panel, width=320, padx=10, pady=10)
        left_panel.pack(side=tk.LEFT, fill=tk.Y, padx=(0, 10))
        left_panel.pack_propagate(False)

        # 1. Danh sách các trang còn lại
        header_remain = tk.Frame(left_panel, bg=self.bg_panel)
        header_remain.pack(fill=tk.X, pady=(0, 4))

        tk.Label(
            header_remain, text="CÁC TRANG PDF CÒN LẠI", bg=self.bg_panel,
            fg=self.fg_muted, font=("Segoe UI", 9, "bold")
        ).pack(side=tk.LEFT)

        self.lbl_remain_count = tk.Label(
            header_remain, text="0 trang", bg=self.bg_panel,
            fg=self.accent_green, font=("Segoe UI", 9, "bold")
        )
        self.lbl_remain_count.pack(side=tk.RIGHT)

        remain_frame = tk.Frame(left_panel, bg=self.bg_card)
        remain_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 10))

        scroll_remain = tk.Scrollbar(remain_frame)
        scroll_remain.pack(side=tk.RIGHT, fill=tk.Y)

        self.remain_listbox = tk.Listbox(
            remain_frame,
            bg=self.bg_card,
            fg=self.fg_text,
            selectbackground=self.accent_blue,
            selectforeground="#ffffff",
            selectmode=tk.EXTENDED,
            bd=0,
            highlightthickness=0,
            font=("Segoe UI", 9),
            yscrollcommand=scroll_remain.set,
            exportselection=False
        )
        self.remain_listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scroll_remain.config(command=self.remain_listbox.yview)
        self.remain_listbox.bind("<<ListboxSelect>>", self.on_remain_listbox_select)

        # 2. Box thống kê các trang đã bị bỏ
        deleted_box = tk.LabelFrame(
            left_panel,
            text="Thống kê các trang đã bị bỏ",
            bg=self.bg_panel,
            fg=self.fg_text,
            font=("Segoe UI", 9, "bold"),
            padx=8,
            pady=8
        )
        deleted_box.pack(fill=tk.X, side=tk.BOTTOM)

        self.lbl_deleted_summary = tk.Label(
            deleted_box,
            text="Chưa có trang nào bị bỏ (0 trang).",
            bg="#2d2618",
            fg="#fbbf24",
            font=("Segoe UI", 8, "bold"),
            wraplength=275,
            justify="left",
            padx=8,
            pady=6
        )
        self.lbl_deleted_summary.pack(fill=tk.X, pady=(0, 6))

        del_list_frame = tk.Frame(deleted_box, bg=self.bg_card, height=135)
        del_list_frame.pack(fill=tk.X)
        del_list_frame.pack_propagate(False)

        scroll_del = tk.Scrollbar(del_list_frame)
        scroll_del.pack(side=tk.RIGHT, fill=tk.Y)

        self.deleted_listbox = tk.Listbox(
            del_list_frame,
            bg=self.bg_card,
            fg="#fca5a5",
            selectbackground=self.accent_red,
            selectforeground="#ffffff",
            selectmode=tk.EXTENDED,
            bd=0,
            highlightthickness=0,
            font=("Segoe UI", 9),
            yscrollcommand=scroll_del.set,
            exportselection=False
        )
        self.deleted_listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scroll_del.config(command=self.deleted_listbox.yview)
        self.deleted_listbox.bind("<Double-Button-1>", lambda e: self.restore_selected_deleted())

        self.btn_restore = tk.Button(
            deleted_box,
            text="↩ Khôi phục trang đã bỏ",
            bg=self.bg_card,
            fg=self.fg_text,
            activebackground="#3b3f46",
            activeforeground="#ffffff",
            bd=0,
            pady=5,
            font=("Segoe UI", 9, "bold"),
            cursor="hand2",
            command=self.restore_selected_deleted
        )
        self.btn_restore.pack(fill=tk.X, pady=(6, 0))

        # --- CỘT PHẢI: THANH CÔNG CỤ TỔ CHỨC TRANG & LƯỚI XEM TRƯỚC THU NHỎ ---
        right_panel = tk.Frame(main_body, bg=self.bg_panel)
        right_panel.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        # Thanh công cụ thao tác trang (theo đúng bố cục ảnh mẫu)
        org_bar = tk.Frame(right_panel, bg=self.bg_card, padx=6, pady=4)
        org_bar.pack(side=tk.TOP, fill=tk.X)

        self.org_buttons = {}

        # Định nghĩa danh sách nút theo đúng thứ tự trong hình mẫu
        toolbar_specs = [
            ("delete", "Delete Pages", "🗑", self.delete_selected_pages, True),
            ("duplicate", "Duplicate", "❐", self.duplicate_selected_pages, True),
            ("sep1", None, None, None, False),
            ("rot_left", "Rotate Left", "↺", lambda: self.rotate_selected_pages(-90), True),
            ("rot_right", "Rotate Right", "↻", lambda: self.rotate_selected_pages(90), True),
            ("sep2", None, None, None, False),
            ("move_before", "Move Before", "⇤", self.move_selected_before, True),
            ("move_after", "Move After", "⇥", self.move_selected_after, True),
            ("sep3", None, None, None, False),
            ("import_doc", "Import Document", "📥", self.import_document, False),
            ("sep4", None, None, None, False),
            ("undo", "Undo", "↶", self.undo, False),
            ("redo", "Redo", "↷", self.redo, False),
            ("sep5", None, None, None, False),
            ("select_all", "Select All", "☑", self.select_all, False),
            ("select_none", "Select None", "☐", self.select_none, True),
            ("sep6", None, None, None, False),
            ("zoom_out", "Zoom Out", "⊖", lambda: self.change_zoom(-25), False),
            ("zoom_in", "Zoom In", "⊕", lambda: self.change_zoom(25), False),
        ]

        for key, label, icon, cmd, needs_sel in toolbar_specs:
            if label is None:
                sep = tk.Frame(org_bar, bg="#3e4249", width=1, height=40)
                sep.pack(side=tk.LEFT, padx=4, pady=4)
                continue

            btn = tk.Button(
                org_bar,
                text=f"{icon}\n{label}",
                command=cmd,
                bg=self.bg_card,
                fg=self.fg_text,
                disabledforeground=self.fg_disabled,
                activebackground="#3b3f46",
                activeforeground="#ffffff",
                bd=0,
                padx=8,
                pady=4,
                font=("Segoe UI", 8, "bold"),
                justify="center",
                cursor="hand2"
            )
            btn.pack(side=tk.LEFT, padx=1)
            btn.bind("<Enter>", lambda e, b=btn: b.config(bg="#353941") if b["state"] == tk.NORMAL else None)
            btn.bind("<Leave>", lambda e, b=btn: b.config(bg=self.bg_card))
            self.org_buttons[key] = (btn, needs_sel)

        # Canvas hiển thị lưới trang thu nhỏ có thanh cuộn dọc
        canvas_container = tk.Frame(right_panel, bg=self.bg_canvas)
        canvas_container.pack(side=tk.TOP, fill=tk.BOTH, expand=True)

        self.v_scroll = tk.Scrollbar(canvas_container, orient=tk.VERTICAL)
        self.v_scroll.pack(side=tk.RIGHT, fill=tk.Y)

        self.canvas = tk.Canvas(
            canvas_container,
            bg=self.bg_canvas,
            highlightthickness=0,
            yscrollcommand=self.v_scroll.set
        )
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        self.v_scroll.config(command=self.canvas.yview)

        self.canvas.bind("<MouseWheel>", self._on_mousewheel)
        self.canvas.bind("<Button-1>", self._on_canvas_click)
        self.canvas.bind("<Configure>", self._on_canvas_configure)

        # Thanh trạng thái dưới cùng
        self.status_var = tk.StringVar(
            value="Sẵn sàng. Bấm 'Mở PDF (Open)' để chọn file PDF cần cắt bớt hoặc sắp xếp lại trang."
        )
        status_bar = tk.Label(
            self.root,
            textvariable=self.status_var,
            bg=self.bg_panel,
            fg=self.fg_muted,
            anchor="w",
            padx=12,
            pady=6,
            font=("Segoe UI", 9)
        )
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)

    def _bind_shortcuts(self):
        self.root.bind("<Control-o>", lambda e: self.open_pdf())
        self.root.bind("<Control-s>", lambda e: self.export_pdf())
        self.root.bind("<Control-z>", lambda e: self.undo())
        self.root.bind("<Control-y>", lambda e: self.redo())
        self.root.bind("<Control-a>", lambda e: self.select_all())
        self.root.bind("<Escape>", lambda e: self.select_none())
        self.root.bind("<Delete>", lambda e: self.delete_selected_pages())

    # ================= MỞ FILE & CHÈN THÊM PDF =================
    def open_pdf(self):
        path = filedialog.askopenfilename(
            title="Chọn file PDF cần chỉnh sửa / xóa bớt trang",
            filetypes=[("PDF Document", "*.pdf"), ("Tất cả file", "*.*")]
        )
        if not path:
            return

        try:
            doc = fitz.open(path)
            if doc.is_encrypted:
                messagebox.showerror("PDF có mật khẩu", "File PDF này đang được đặt mật khẩu bảo vệ.")
                doc.close()
                return
            if len(doc) == 0:
                messagebox.showerror("File rỗng", "File PDF không có trang nào.")
                doc.close()
                return

            # Đóng các tài liệu cũ
            self._close_all_docs()

            doc_id = self.next_doc_id
            self.next_doc_id += 1
            self.source_docs[doc_id] = doc
            self.source_paths[doc_id] = path
            self.main_pdf_path = path
            self.main_total_pages = len(doc)

            self.pages = []
            self.next_uid = 1
            base_name = os.path.basename(path)
            for i in range(len(doc)):
                self.pages.append({
                    "uid": self.next_uid,
                    "doc_id": doc_id,
                    "orig_idx": i,
                    "orig_num": i + 1,
                    "source_name": base_name,
                    "rotation": 0,
                })
                self.next_uid += 1

            self.selected_indices.clear()
            self.last_clicked_idx = None
            self.undo_stack.clear()
            self.redo_stack.clear()
            self.thumb_cache.clear()

            self._refresh_all_views()
            self.canvas.yview_moveto(0)
            self.status_var.set(
                f"Đã mở: {base_name} ({self.main_total_pages} trang). "
                "Click chọn trang (giữ Ctrl hoặc Shift để chọn nhiều trang) rồi bấm 'Delete Pages'."
            )
        except Exception as e:
            messagebox.showerror("Lỗi mở PDF", f"Không thể mở file PDF:\n{e}")

    def import_document(self):
        if not self.pages:
            self.open_pdf()
            return

        path = filedialog.askopenfilename(
            title="Chọn file PDF muốn chèn thêm vào cuối tài liệu",
            filetypes=[("PDF Document", "*.pdf"), ("Tất cả file", "*.*")]
        )
        if not path:
            return

        try:
            doc = fitz.open(path)
            if doc.is_encrypted or len(doc) == 0:
                messagebox.showerror("Không thể chèn", "File PDF được chọn bị khóa mật khẩu hoặc rỗng.")
                doc.close()
                return

            self._push_undo()
            doc_id = self.next_doc_id
            self.next_doc_id += 1
            self.source_docs[doc_id] = doc
            self.source_paths[doc_id] = path

            base_name = os.path.basename(path)
            start_new_idx = len(self.pages)
            for i in range(len(doc)):
                self.pages.append({
                    "uid": self.next_uid,
                    "doc_id": doc_id,
                    "orig_idx": i,
                    "orig_num": i + 1,
                    "source_name": base_name,
                    "rotation": 0,
                })
                self.next_uid += 1

            self.selected_indices = set(range(start_new_idx, len(self.pages)))
            self._refresh_all_views()
            self.status_var.set(f"Đã chèn thêm {len(doc)} trang từ '{base_name}'.")
        except Exception as e:
            messagebox.showerror("Lỗi chèn PDF", f"Không thể chèn file PDF:\n{e}")

    # ================= QUẢN LÝ UNDO / REDO =================
    def _push_undo(self):
        snapshot = (copy.deepcopy(self.pages), set(self.selected_indices))
        self.undo_stack.append(snapshot)
        if len(self.undo_stack) > 50:
            self.undo_stack.pop(0)
        self.redo_stack.clear()

    def undo(self):
        if not self.undo_stack:
            return
        self.redo_stack.append((copy.deepcopy(self.pages), set(self.selected_indices)))
        self.pages, self.selected_indices = self.undo_stack.pop()
        self._refresh_all_views()
        self.status_var.set("Đã hoàn tác (Undo) thao tác trước đó.")

    def redo(self):
        if not self.redo_stack:
            return
        self.undo_stack.append((copy.deepcopy(self.pages), set(self.selected_indices)))
        self.pages, self.selected_indices = self.redo_stack.pop()
        self._refresh_all_views()
        self.status_var.set("Đã làm lại (Redo) thao tác.")

    # ================= CÁC THAO TÁC TRÊN THANH CÔNG CỤ =================
    def delete_selected_pages(self):
        if not self.selected_indices or not self.pages:
            return

        if len(self.selected_indices) >= len(self.pages):
            messagebox.showwarning(
                "Không thể xóa hết",
                "Bạn đang chọn toàn bộ số trang còn lại.\nFile PDF xuất ra phải giữ lại ít nhất 1 trang!"
            )
            return

        self._push_undo()
        deleted_count = len(self.selected_indices)
        first_removed = min(self.selected_indices)

        self.pages = [
            p for idx, p in enumerate(self.pages)
            if idx not in self.selected_indices
        ]
        self.selected_indices.clear()
        self.last_clicked_idx = min(first_removed, len(self.pages) - 1)

        self._refresh_all_views()
        self.status_var.set(
            f"Đã xóa {deleted_count} trang. Còn lại {len(self.pages)} trang."
        )

    def restore_selected_deleted(self):
        """Khôi phục các trang gốc đã bị xóa quay lại đúng vị trí ban đầu."""
        sel = self.deleted_listbox.curselection()
        if not sel or 0 not in self.source_docs:
            return

        deleted_orig_nums = self._get_deleted_main_pages()
        to_restore = [deleted_orig_nums[i] for i in sel if i < len(deleted_orig_nums)]
        if not to_restore:
            return

        self._push_undo()
        base_name = os.path.basename(self.main_pdf_path)

        for orig_num in sorted(to_restore):
            new_item = {
                "uid": self.next_uid,
                "doc_id": 0,
                "orig_idx": orig_num - 1,
                "orig_num": orig_num,
                "source_name": base_name,
                "rotation": 0,
            }
            self.next_uid += 1

            # Tìm vị trí chèn hợp lý dựa theo thứ tự trang gốc của file chính
            insert_pos = len(self.pages)
            for idx, p in enumerate(self.pages):
                if p["doc_id"] == 0 and p["orig_num"] > orig_num:
                    insert_pos = idx
                    break
            self.pages.insert(insert_pos, new_item)

        self.selected_indices.clear()
        self._refresh_all_views()
        self.status_var.set(
            f"Đã khôi phục trang gốc: {', '.join(map(str, sorted(to_restore)))}. Còn lại {len(self.pages)} trang."
        )

    def duplicate_selected_pages(self):
        if not self.selected_indices or not self.pages:
            return

        self._push_undo()
        new_pages = []
        new_selection = set()

        for idx, p in enumerate(self.pages):
            new_pages.append(p)
            if idx in self.selected_indices:
                dup = copy.deepcopy(p)
                dup["uid"] = self.next_uid
                self.next_uid += 1
                new_pages.append(dup)
                new_selection.add(len(new_pages) - 1)

        self.pages = new_pages
        self.selected_indices = new_selection
        self._refresh_all_views()
        self.status_var.set(f"Đã nhân bản {len(new_selection)} trang. Tổng cộng: {len(self.pages)} trang.")

    def rotate_selected_pages(self, delta_deg: int):
        if not self.selected_indices or not self.pages:
            return

        self._push_undo()
        for idx in self.selected_indices:
            self.pages[idx]["rotation"] = (self.pages[idx]["rotation"] + delta_deg) % 360

        self._refresh_all_views()
        direction = "phải 90°" if delta_deg > 0 else "trái 90°"
        self.status_var.set(f"Đã xoay {direction} cho {len(self.selected_indices)} trang đang chọn.")

    def move_selected_before(self):
        if not self.selected_indices or min(self.selected_indices) == 0:
            return

        self._push_undo()
        sorted_sel = sorted(self.selected_indices)
        new_sel = set()

        for idx in sorted_sel:
            if idx > 0 and (idx - 1) not in new_sel:
                self.pages[idx - 1], self.pages[idx] = self.pages[idx], self.pages[idx - 1]
                new_sel.add(idx - 1)
            else:
                new_sel.add(idx)

        self.selected_indices = new_sel
        self._refresh_all_views()
        self.status_var.set("Đã di chuyển các trang được chọn lên trước 1 vị trí.")

    def move_selected_after(self):
        if not self.selected_indices or max(self.selected_indices) >= len(self.pages) - 1:
            return

        self._push_undo()
        sorted_sel = sorted(self.selected_indices, reverse=True)
        new_sel = set()

        for idx in sorted_sel:
            if idx < len(self.pages) - 1 and (idx + 1) not in new_sel:
                self.pages[idx + 1], self.pages[idx] = self.pages[idx], self.pages[idx + 1]
                new_sel.add(idx + 1)
            else:
                new_sel.add(idx)

        self.selected_indices = new_sel
        self._refresh_all_views()
        self.status_var.set("Đã di chuyển các trang được chọn ra sau 1 vị trí.")

    def select_all(self):
        if not self.pages:
            return
        self.selected_indices = set(range(len(self.pages)))
        self._sync_side_lists()
        self.render_thumbnails()
        self._update_toolbar_states()

    def select_none(self):
        if not self.selected_indices:
            return
        self.selected_indices.clear()
        self._sync_side_lists()
        self.render_thumbnails()
        self._update_toolbar_states()

    def change_zoom(self, delta: int):
        if not self.pages:
            return
        new_w = max(self.min_thumb_w, min(self.max_thumb_w, self.thumb_width + delta))
        if new_w != self.thumb_width:
            self.thumb_width = new_w
            self.render_thumbnails()
            self.status_var.set(f"Kích thước thu nhỏ: {self.thumb_width} px / trang.")

    # ================= ĐỒNG BỘ DANH SÁCH TRÁI & THỐNG KÊ TRANG BỊ BỎ =================
    def _get_deleted_main_pages(self) -> list:
        """Trả về danh sách số thứ tự (1-based) các trang của file PDF gốc đã bị xóa hết."""
        if self.main_total_pages == 0:
            return []
        present_orig_nums = {
            p["orig_num"] for p in self.pages if p["doc_id"] == 0
        }
        return [n for n in range(1, self.main_total_pages + 1) if n not in present_orig_nums]

    def _sync_side_lists(self):
        self._syncing_listbox = True
        try:
            # 1. Cập nhật Listbox các trang còn lại
            self.remain_listbox.delete(0, tk.END)
            for idx, p in enumerate(self.pages):
                rot_tag = f" • Xoay {p['rotation']}°" if p["rotation"] != 0 else ""
                if p["doc_id"] == 0:
                    label = f"Trang {idx + 1}  (Gốc #{p['orig_num']}{rot_tag})"
                else:
                    label = f"Trang {idx + 1}  (+{p['source_name']} #{p['orig_num']}{rot_tag})"
                self.remain_listbox.insert(tk.END, label)

            for idx in self.selected_indices:
                if 0 <= idx < len(self.pages):
                    self.remain_listbox.selection_set(idx)

            if self.selected_indices:
                first_idx = min(self.selected_indices)
                self.remain_listbox.see(first_idx)

            self.lbl_remain_count.config(text=f"{len(self.pages)} trang")

            # 2. Cập nhật Box thống kê các trang đã bị bỏ
            deleted_nums = self._get_deleted_main_pages()
            self.deleted_listbox.delete(0, tk.END)

            if not self.pages and self.main_total_pages == 0:
                self.lbl_deleted_summary.config(
                    text="Chưa có trang nào bị bỏ (0 trang).",
                    bg="#2d2618", fg="#fbbf24"
                )
            elif not deleted_nums:
                self.lbl_deleted_summary.config(
                    text=f"Giữ nguyên toàn bộ {self.main_total_pages} trang gốc (0 trang bị bỏ).",
                    bg="#1f2937", fg="#93c5fd"
                )
            else:
                nums_str = ", ".join(map(str, deleted_nums))
                self.lbl_deleted_summary.config(
                    text=f"Đã bỏ: {len(deleted_nums)} trang (Trang gốc: {nums_str})",
                    bg="#3b1d1d", fg="#fca5a5"
                )
                for n in deleted_nums:
                    self.deleted_listbox.insert(tk.END, f"✕  Trang gốc #{n}  (đã loại bỏ)")

            # 3. Nhãn tóm tắt trên thanh trên cùng
            if self.main_pdf_path:
                fname = os.path.basename(self.main_pdf_path)
                self.lbl_doc_summary.config(
                    text=f"{fname}  |  Gốc: {self.main_total_pages} → Còn: {len(self.pages)} trang  |  Đang chọn: {len(self.selected_indices)}"
                )
        finally:
            self._syncing_listbox = False

    def on_remain_listbox_select(self, event):
        if self._syncing_listbox or not self.pages:
            return
        sel = self.remain_listbox.curselection()
        self.selected_indices = set(sel)
        if sel:
            self.last_clicked_idx = sel[-1]
        self._update_toolbar_states()
        self.render_thumbnails()
        self._scroll_to_page(min(sel)) if sel else None

    def _update_toolbar_states(self):
        has_doc = len(self.pages) > 0
        has_sel = len(self.selected_indices) > 0

        for key, (btn, needs_sel) in self.org_buttons.items():
            if key == "undo":
                state = tk.NORMAL if self.undo_stack else tk.DISABLED
            elif key == "redo":
                state = tk.NORMAL if self.redo_stack else tk.DISABLED
            elif key == "import_doc":
                state = tk.NORMAL
            elif key in ("select_all", "zoom_in", "zoom_out"):
                state = tk.NORMAL if has_doc else tk.DISABLED
            elif key == "move_before":
                state = tk.NORMAL if (has_sel and min(self.selected_indices) > 0) else tk.DISABLED
            elif key == "move_after":
                state = tk.NORMAL if (has_sel and max(self.selected_indices) < len(self.pages) - 1) else tk.DISABLED
            elif needs_sel:
                state = tk.NORMAL if has_sel else tk.DISABLED
            else:
                state = tk.NORMAL if has_doc else tk.DISABLED

            btn.config(state=state)

        # Cập nhật nhãn tóm tắt số trang chọn
        if self.main_pdf_path:
            fname = os.path.basename(self.main_pdf_path)
            self.lbl_doc_summary.config(
                text=f"{fname}  |  Gốc: {self.main_total_pages} → Còn: {len(self.pages)} trang  |  Đang chọn: {len(self.selected_indices)}"
            )

    def _refresh_all_views(self):
        self._sync_side_lists()
        self._update_toolbar_states()
        self.render_thumbnails()

    # ================= RENDER LƯỚI TRANG THU NHỎ & PILL SỐ TRANG =================
    def _get_thumbnail_photo(self, page_info: dict, target_w: int) -> tuple:
        """Render & cache ảnh PIL/PhotoImage cho một trang PDF tại kích thước target_w."""
        key = (page_info["doc_id"], page_info["orig_idx"], page_info["rotation"], target_w)
        if key in self.thumb_cache:
            return self.thumb_cache[key]

        doc = self.source_docs[page_info["doc_id"]]
        page = doc[page_info["orig_idx"]]
        rect = page.rect

        # Tính kích thước sau khi xoay
        if page_info["rotation"] in (90, 270):
            src_w, src_h = rect.height, rect.width
        else:
            src_w, src_h = rect.width, rect.height

        zoom = target_w / max(src_w, 1.0)
        mat = fitz.Matrix(zoom, zoom).prerotate(page_info["rotation"])
        pix = page.get_pixmap(matrix=mat, alpha=False)
        pil_img = Image.frombytes("RGB", [pix.width, pix.height], pix.samples)

        photo = ImageTk.PhotoImage(pil_img)
        self.thumb_cache[key] = (photo, pix.width, pix.height)

        # Giới hạn kích thước cache tránh tràn RAM nếu zoom nhiều lần
        if len(self.thumb_cache) > 600:
            oldest_key = next(iter(self.thumb_cache))
            del self.thumb_cache[oldest_key]

        return self.thumb_cache[key]

    def _draw_pill_badge(self, cx: int, top_y: int, text: str, selected: bool):
        """Vẽ huy hiệu số trang bo tròn (pill) ngay bên dưới trang thu nhỏ giống ảnh mẫu."""
        pad_x = 16
        pill_h = 22
        text_w = max(22, len(text) * 8)
        pill_w = text_w + pad_x * 2
        r = pill_h // 2

        x1 = cx - pill_w // 2
        x2 = cx + pill_w // 2
        y1 = top_y
        y2 = top_y + pill_h

        fill_col = self.accent_blue if selected else "#343841"
        outline_col = "#60a5fa" if selected else "#4b5563"
        txt_col = "#ffffff" if selected else "#e5e7eb"

        # Vẽ hình viên thuốc bo tròn 2 đầu
        self.canvas.create_oval(x1, y1, x1 + pill_h, y2, fill=fill_col, outline=outline_col, width=1)
        self.canvas.create_oval(x2 - pill_h, y1, x2, y2, fill=fill_col, outline=outline_col, width=1)
        self.canvas.create_rectangle(x1 + r, y1, x2 - r, y2, fill=fill_col, outline="")
        self.canvas.create_line(x1 + r, y1, x2 - r, y1, fill=outline_col, width=1)
        self.canvas.create_line(x1 + r, y2, x2 - r, y2, fill=outline_col, width=1)

        self.canvas.create_text(
            cx, (y1 + y2) // 2,
            text=text,
            fill=txt_col,
            font=("Segoe UI", 9, "bold")
        )

    def render_thumbnails(self):
        self.canvas.delete("all")
        self.card_bboxes.clear()

        cw = max(self.canvas.winfo_width(), 650)
        ch = max(self.canvas.winfo_height(), 500)

        if not self.pages:
            self.canvas.create_text(
                cw // 2,
                ch // 2,
                text="Bấm '📂 Mở PDF (Open)' ở góc trên bên trái để tải tài liệu PDF",
                fill=self.fg_muted,
                font=("Segoe UI", 12, "bold")
            )
            self.canvas.config(scrollregion=(0, 0, cw, ch))
            return

        gap_x = 34
        gap_y = 32
        pad_outer = 28
        pill_area_h = 34

        cell_w = self.thumb_width + gap_x
        cols = max(1, (cw - pad_outer * 2 + gap_x) // cell_w)
        total_grid_w = cols * self.thumb_width + (cols - 1) * gap_x
        start_x = max(pad_outer, (cw - total_grid_w) // 2)

        # Tính chiều cao lớn nhất của từng hàng để căn chỉnh thẳng hàng ngang đẹp mắt
        rows = (len(self.pages) + cols - 1) // cols
        cur_y = pad_outer

        for r in range(rows):
            row_items = []
            max_row_img_h = 0
            for c in range(cols):
                idx = r * cols + c
                if idx >= len(self.pages):
                    break
                photo, pw, ph = self._get_thumbnail_photo(self.pages[idx], self.thumb_width)
                max_row_img_h = max(max_row_img_h, ph)
                row_items.append((idx, c, photo, pw, ph))

            for idx, c, photo, pw, ph in row_items:
                is_selected = idx in self.selected_indices
                slot_x = start_x + c * (self.thumb_width + gap_x)
                img_x = slot_x + (self.thumb_width - pw) // 2
                img_y = cur_y + (max_row_img_h - ph) // 2

                # Đổ bóng nhẹ phía sau trang giấy
                self.canvas.create_rectangle(
                    img_x + 4, img_y + 4, img_x + pw + 4, img_y + ph + 4,
                    fill="#0b0c0e", outline=""
                )

                # Ảnh nội dung trang PDF
                self.canvas.create_image(img_x, img_y, anchor="nw", image=photo)

                # Viền trang (xanh đậm khi đang được chọn)
                if is_selected:
                    self.canvas.create_rectangle(
                        img_x - 3, img_y - 3, img_x + pw + 3, img_y + ph + 3,
                        outline=self.accent_blue, width=3
                    )
                    # Dấu tick xanh ở góc trên phải trang được chọn
                    bx, by = img_x + pw - 14, img_y + 14
                    self.canvas.create_oval(
                        bx - 10, by - 10, bx + 10, by + 10,
                        fill=self.accent_blue, outline="#ffffff", width=1
                    )
                    self.canvas.create_text(
                        bx, by, text="✓", fill="#ffffff", font=("Segoe UI", 9, "bold")
                    )
                else:
                    self.canvas.create_rectangle(
                        img_x, img_y, img_x + pw, img_y + ph,
                        outline="#4b5563", width=1
                    )

                # Pill số trang đặt ngay chính giữa bên dưới trang (giống hình mẫu)
                pill_top_y = cur_y + max_row_img_h + 10
                center_x = slot_x + self.thumb_width // 2
                self._draw_pill_badge(center_x, pill_top_y, str(idx + 1), is_selected)

                # Ghi nhận vùng click (bao gồm cả trang lẫn pill số trang)
                self.card_bboxes.append((
                    slot_x - 6,
                    cur_y - 6,
                    slot_x + self.thumb_width + 6,
                    pill_top_y + 26,
                    idx
                ))

            cur_y += max_row_img_h + pill_area_h + gap_y

        total_h = max(ch, cur_y + 10)
        self.canvas.config(scrollregion=(0, 0, cw, total_h))

    def _scroll_to_page(self, page_idx: int):
        for x1, y1, x2, y2, idx in self.card_bboxes:
            if idx == page_idx:
                sr = self.canvas.cget("scrollregion")
                if not sr:
                    return
                total_h = float(sr.split()[3])
                if total_h > 0:
                    target_frac = max(0.0, (y1 - 24) / total_h)
                    self.canvas.yview_moveto(target_frac)
                break

    # ================= TƯƠNG TÁC CHUỘT TRÊN LƯỚI =================
    def _on_canvas_click(self, event):
        if not self.pages:
            return

        cx = self.canvas.canvasx(event.x)
        cy = self.canvas.canvasy(event.y)

        clicked_idx = None
        for x1, y1, x2, y2, idx in self.card_bboxes:
            if x1 <= cx <= x2 and y1 <= cy <= y2:
                clicked_idx = idx
                break

        ctrl_pressed = (event.state & 0x0004) != 0
        shift_pressed = (event.state & 0x0001) != 0

        if clicked_idx is None:
            if not ctrl_pressed and not shift_pressed:
                self.select_none()
            return

        if shift_pressed and self.last_clicked_idx is not None:
            start = min(self.last_clicked_idx, clicked_idx)
            end = max(self.last_clicked_idx, clicked_idx)
            if not ctrl_pressed:
                self.selected_indices.clear()
            self.selected_indices.update(range(start, end + 1))
        elif ctrl_pressed:
            if clicked_idx in self.selected_indices:
                self.selected_indices.remove(clicked_idx)
            else:
                self.selected_indices.add(clicked_idx)
            self.last_clicked_idx = clicked_idx
        else:
            self.selected_indices = {clicked_idx}
            self.last_clicked_idx = clicked_idx

        self._sync_side_lists()
        self._update_toolbar_states()
        self.render_thumbnails()

    def _on_mousewheel(self, event):
        self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

    def _on_canvas_configure(self, event):
        if self._resize_after_id is not None:
            self.root.after_cancel(self._resize_after_id)
        self._resize_after_id = self.root.after(60, self.render_thumbnails)

    # ================= XUẤT FILE PDF =================
    def export_pdf(self):
        if not self.pages:
            messagebox.showwarning("Chưa có dữ liệu", "Vui lòng mở file PDF trước khi xuất.")
            return

        base_name = os.path.splitext(os.path.basename(self.main_pdf_path))[0]
        default_name = f"{base_name}_da_cat_{len(self.pages)}trang.pdf"

        save_path = filedialog.asksaveasfilename(
            title="Chọn nơi lưu file PDF sau khi chỉnh sửa",
            initialfile=default_name,
            defaultextension=".pdf",
            filetypes=[("PDF Document", "*.pdf")]
        )
        if not save_path:
            return

        # Tránh ghi đè trực tiếp lên file đang được PyMuPDF mở đọc
        norm_save = os.path.normcase(os.path.abspath(save_path))
        for open_path in self.source_paths.values():
            if os.path.normcase(os.path.abspath(open_path)) == norm_save:
                messagebox.showwarning(
                    "Trùng tên file gốc",
                    "Không thể ghi đè trực tiếp lên file PDF đang mở.\nVui lòng đặt tên file khác để lưu!"
                )
                return

        try:
            self.status_var.set("Đang đóng gói và xuất file PDF mới...")
            self.root.update_idletasks()

            out_doc = fitz.open()
            for p in self.pages:
                src_doc = self.source_docs[p["doc_id"]]
                orig_idx = p["orig_idx"]
                out_doc.insert_pdf(src_doc, from_page=orig_idx, to_page=orig_idx)
                if p["rotation"] != 0:
                    out_doc[-1].set_rotation(p["rotation"])

            out_doc.save(save_path, garbage=4, deflate=True)
            out_doc.close()

            deleted_nums = self._get_deleted_main_pages()
            del_info = (
                f"\nCác trang gốc đã loại bỏ: {', '.join(map(str, deleted_nums))}"
                if deleted_nums else ""
            )

            self.status_var.set(f"Xuất PDF thành công ({len(self.pages)} trang): {save_path}")
            messagebox.showinfo(
                "Xuất PDF thành công",
                f"Đã xuất {len(self.pages)} trang ra file:\n{save_path}{del_info}"
            )
        except Exception as e:
            messagebox.showerror("Lỗi xuất PDF", f"Không thể lưu file PDF:\n{e}")

    # ================= DỌN DẸP TÀI NGUYÊN =================
    def _close_all_docs(self):
        for doc in self.source_docs.values():
            try:
                doc.close()
            except Exception:
                pass
        self.source_docs.clear()
        self.source_paths.clear()

    def _on_close(self):
        self._close_all_docs()
        self.root.destroy()


if __name__ == "__main__":
    root = tk.Tk()
    app = SmartPDFOrganizerApp(root)
    root.mainloop()