// ==UserScript==
// @name         Tải Audio Riki (Theo bài & Thư mục)
// @namespace    http://tampermonkey.net/
// @version      1.0
// @description  Tự động tải audio trên riki.edu.vn, đặt tên theo thứ tự và lưu vào thư mục riêng.
// @author       Long
// @match        https://riki.edu.vn/online/my-course/1*
// @grant        GM_download
// @grant        GM_addStyle
// ==/UserScript==

(function() {
    'use strict';

    // Tạo nút bấm lơ lửng ở góc màn hình cho tiện thao tác
    let btn = document.createElement('button');
    btn.innerHTML = '⬇️';
    btn.id = 'riki-download-btn';

    // CSS cho nút bấm đẹp và dễ nhìn
    GM_addStyle(`
        #riki-download-btn {
            position: fixed;
            bottom: 5px;
            right: 5px;
            z-index: 999999;
            padding: 5px 5px;
            background-color: #007bff;
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 9px;
            font-weight: bold;
            cursor: pointer;
            box-shadow: 0 4px 6px rgba(0,0,0,0.3);
            transition: 0.3s;
        }
        #riki-download-btn:hover {
            background-color: #0056b3;
            transform: translateY(-2px);
        }
    `);

    document.body.appendChild(btn);

    // Sự kiện khi bấm nút
    btn.onclick = function() {
        // 1. Lấy tên bài học (Ví dụ: "Bài tập bài 26") từ thẻ h5
        let titleElement = document.querySelector('h5.mt-3.mb-3 strong') || document.querySelector('h5.mt-3.mb-3');
        let rawTitle = titleElement ? titleElement.innerText.trim() : "Audio_Riki";

        // Loại bỏ các ký tự đặc biệt không được phép dùng làm tên file/thư mục
        let safeTitle = rawTitle.replace(/[\\/:*?"<>|]/g, "");

        // 2. HỎI NGƯỜI DÙNG: Tên thư mục muốn lưu
        // (Nếu bạn tạo nhiều folder, mỗi lần bấm nó sẽ hỏi, bạn chỉ việc gõ tên Bài 26, Bài 27...)
        let folderName = prompt("Nhập tên thư mục muốn lưu (File sẽ nằm trong thư mục Downloads của máy):\n\nVí dụ: Bài tập bài 26", safeTitle);

        // Nếu người dùng bấm "Hủy" (Cancel) thì dừng lại
        if (folderName === null) {
            return;
        }

        let safeFolder = folderName.trim().replace(/[\\/:*?"<>|]/g, "");

        // 3. Tìm toàn bộ thẻ audio trên trang
        let audios = document.querySelectorAll('audio');

        if (audios.length === 0) {
            alert("Không tìm thấy file audio nào trên trang này!");
            return;
        }

        // 4. Tiến hành tải lần lượt
        let count = 1;
        let downloadedCount = 0;

        audios.forEach((audio, index) => {
            // Lấy link audio
            let src = audio.src || (audio.querySelector('source') && audio.querySelector('source').src);

            if (src) {
                // Đặt tên file: "Bài tập bài 26 - 1.mp3"
                let fileName = `${safeTitle} - ${count}.mp3`;

                // Cấu trúc đường dẫn: "Thu_Muc/Ten_File.mp3"
                // Tampermonkey sẽ tự động tạo thư mục này bên trong thư mục Downloads mặc định
                let savePath = safeFolder !== "" ? `${safeFolder}/${fileName}` : fileName;

                // Gọi lệnh tải của Tampermonkey
                GM_download({
                    url: src,
                    name: savePath,
                    saveAs: false, // Để false để không hiện bảng "Save as..." cho từng file một
                    onload: function() {
                        downloadedCount++;
                        console.log(`Đã tải thành công: ${fileName}`);
                    },
                    onerror: function(e) {
                        console.error(`Lỗi khi tải ${fileName}:`, e);
                    }
                });
                count++;
            }
        });

        alert(`Đã ra lệnh tải ${count - 1} file audio.\nĐường dẫn: Downloads/${safeFolder}/...\n\nVui lòng không tắt trang web cho đến khi tải xong hết nhé!`);
    };
})();