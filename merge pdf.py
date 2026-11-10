import os
import tkinter as tk
from tkinter import filedialog
from pypdf import PdfWriter  # Sử dụng PdfWriter thay cho PdfMerger

def main():
    root = tk.Tk()
    root.withdraw()

    print("Đang mở bảng chọn các file PDF cần gộp...")

    files_to_merge = filedialog.askopenfilenames(
        title="Chọn các file PDF bạn muốn gộp (chọn nhiều file)",
        filetypes=[("PDF files", "*.pdf"), ("All files", "*.*")]
    )

    if not files_to_merge:
        print("❌ Bạn đã hủy chọn file.")
        return

    merger = PdfWriter()

    print(f"Đang tiến hành gộp {len(files_to_merge)} file...")
    for pdf in files_to_merge:
        print(f" -> Đã thêm: {os.path.basename(pdf)}")
        merger.append(pdf)

    first_file_dir = os.path.dirname(files_to_merge[0])
    output_path = os.path.join(first_file_dir, "File_Da_Gop.pdf")

    with open(output_path, "wb") as f_out:
        merger.write(f_out)

    merger.close()

    print("\n✅ Gộp file thành công rực rỡ!")
    print(f"📄 File đầu ra: {output_path}")

if __name__ == "__main__":
    main()
