import os
import re
import tkinter as tk
from tkinter import filedialog, messagebox
from pypdf import PdfWriter

def extract_first_number(filepath):
    """
    Ưu tiên lọc lấy con số đầu tiên trong tên file để sắp xếp.
    Ví dụ: 'vở AI 1' -> lấy số 1, 'AI2' -> lấy số 2, 'AI3' -> lấy số 3.
    """
    filename = os.path.basename(filepath)
    numbers = re.findall(r'\d+', filename)
    if numbers:
        return (0, int(numbers[0]), filename.lower())
    return (1, 0, filename.lower())

class PDFMergerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Công Cụ Gộp File PDF")
        self.root.geometry("620x480")
        self.files = []

        # Thanh công cụ chọn & sắp xếp
        btn_frame = tk.Frame(root)
        btn_frame.pack(fill=tk.X, padx=10, pady=10)

        tk.Button(btn_frame, text="📁 Chọn Các File PDF", command=self.select_files, bg="#2196F3", fg="white", font=("Arial", 10, "bold")).pack(side=tk.LEFT, padx=5)
        tk.Button(btn_frame, text="🔢 Xếp Theo Số (1, 2, 3...)", command=self.sort_by_number, bg="#4CAF50", fg="white", font=("Arial", 10)).pack(side=tk.LEFT, padx=5)

        # Khung hiển thị danh sách thứ tự file
        frame_list = tk.Frame(root)
        frame_list.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        self.listbox = tk.Listbox(frame_list, selectmode=tk.SINGLE, font=("Consolas", 11))
        self.listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        scrollbar = tk.Scrollbar(frame_list, orient=tk.VERTICAL, command=self.listbox.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.listbox.config(yscrollcommand=scrollbar.set)

        # Thanh nút điều chỉnh vị trí thủ công
        ctrl_frame = tk.Frame(root)
        ctrl_frame.pack(fill=tk.X, padx=10, pady=5)

        tk.Button(ctrl_frame, text="⬆️ Đẩy lên", command=self.move_up, width=12).pack(side=tk.LEFT, padx=5)
        tk.Button(ctrl_frame, text="⬇️ Đẩy xuống", command=self.move_down, width=12).pack(side=tk.LEFT, padx=5)
        tk.Button(ctrl_frame, text="❌ Xóa file chọn", command=self.remove_selected, width=12).pack(side=tk.LEFT, padx=5)

        # Nút thực thi gộp file
        tk.Button(root, text="⚡ BẮT ĐẦU GỘP FILE PDF", command=self.merge_pdfs, bg="#FF5722", fg="white", font=("Arial", 11, "bold"), height=2).pack(fill=tk.X, padx=10, pady=10)

    def select_files(self):
        selected = filedialog.askopenfilenames(
            title="Chọn các file PDF cần gộp",
            filetypes=[("PDF files", "*.pdf"), ("All files", "*.*")]
        )
        if selected:
            self.files = list(selected)
            self.sort_by_number()

    def update_listbox(self):
        self.listbox.delete(0, tk.END)
        for idx, f in enumerate(self.files, 1):
            self.listbox.insert(tk.END, f"{idx}. {os.path.basename(f)}")

    def sort_by_number(self):
        self.files.sort(key=extract_first_number)
        self.update_listbox()

    def move_up(self):
        idx = self.listbox.curselection()
        if idx and idx[0] > 0:
            i = idx[0]
            self.files[i], self.files[i-1] = self.files[i-1], self.files[i]
            self.update_listbox()
            self.listbox.selection_set(i-1)

    def move_down(self):
        idx = self.listbox.curselection()
        if idx and idx[0] < len(self.files) - 1:
            i = idx[0]
            self.files[i], self.files[i+1] = self.files[i+1], self.files[i]
            self.update_listbox()
            self.listbox.selection_set(i+1)

    def remove_selected(self):
        idx = self.listbox.curselection()
        if idx:
            del self.files[idx[0]]
            self.update_listbox()

    def merge_pdfs(self):
        if not self.files:
            messagebox.showwarning("Cảnh báo", "Bạn chưa chọn file PDF nào!")
            return

        out_dir = os.path.dirname(self.files[0])
        output_path = os.path.join(out_dir, "File_Da_Gop.pdf")

        merger = PdfWriter()
        for pdf in self.files:
            merger.append(pdf)

        with open(output_path, "wb") as f_out:
            merger.write(f_out)
        merger.close()

        messagebox.showinfo("Thành công", f"Đã gộp file thành công!\nFile lưu tại:\n{output_path}")

if __name__ == "__main__":
    root = tk.Tk()
    app = PDFMergerApp(root)
    root.mainloop()
