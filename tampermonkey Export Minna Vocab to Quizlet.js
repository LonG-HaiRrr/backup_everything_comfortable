// ==UserScript==
// @name         Export Minna Vocab to Quizlet
// @namespace    http://tampermonkey.net/
// @version      1.0
// @description  Lấy dữ liệu từ vựng từ bảng (Hira \n Kanji ++ Nghĩa) và copy vào Clipboard
// @author       Long
// @match        *://riki.edu.vn/minna-no-nihongo/*
// @grant        GM_setClipboard
// ==/UserScript==

(function() {
    'use strict';

    // 1. Tạo nút bấm "Copy to Quizlet" nổi trên màn hình
    const btn = document.createElement('button');
    btn.innerText = 'Copy to Quizlet';
    btn.style.position = 'fixed';
    btn.style.bottom = '30px';
    btn.style.right = '30px';
    btn.style.zIndex = '999999';
    btn.style.padding = '12px 20px';
    btn.style.background = '#4CAF50';
    btn.style.color = 'white';
    btn.style.border = 'none';
    btn.style.borderRadius = '8px';
    btn.style.cursor = 'pointer';
    btn.style.fontWeight = 'bold';
    btn.style.boxShadow = '0 4px 6px rgba(0,0,0,0.3)';

    // Hiệu ứng hover
    btn.onmouseover = () => btn.style.background = '#45a049';
    btn.onmouseout = () => btn.style.background = '#4CAF50';

    // 2. Xử lý logic khi click
    btn.onclick = function() {
        // Trỏ vào các hàng tr trong bảng nằm trong class pageDetailContent_post
        const rows = document.querySelectorAll('.pageDetailContent_post table tr');
        let results = [];

        rows.forEach(row => {
            const cols = row.querySelectorAll('td');

            // Bảng cần có ít nhất 4 cột: STT, Từ Vựng, Kanji, Nghĩa
            if (cols.length >= 4) {
                const stt = cols[0].innerText.trim();

                // Lọc bỏ hàng Tiêu đề (STT phải là số 1, 2, 3...)
                if (!isNaN(parseInt(stt))) {
                    const hira = cols[1].innerText.trim();
                    const kanji = cols[2].innerText.trim();
                    const nghia = cols[3].innerText.trim();

                    // Format: Hira + \n + Kanji + ++ + Nghĩa
                    // VD: うんどうします \n 運動します ++ vận động, tập thể thao
                    let cardStr = `${hira}\n${kanji}++${nghia}`;
                    results.push(cardStr);
                }
            }
        });

        if (results.length > 0) {
            // Nối mảng bằng ** (thẻ cuối tự động không bị thừa **)
            const finalOutput = results.join('**');

            // Lệnh API của Tampermonkey để copy thẳng vào bộ nhớ tạm
            GM_setClipboard(finalOutput);

            alert(`Đã copy thành công ${results.length} từ vựng!\n\nHướng dẫn Import vào Quizlet:\n1. Chọn "Import from Word, Excel, Google Docs, etc."\n2. Between term and definition (Giữa thuật ngữ & định nghĩa) nhập: ++\n3. Between rows (Giữa các hàng) nhập: **`);
        } else {
            alert('Lỗi: Không tìm thấy bảng từ vựng nào hoặc bảng không đúng định dạng!');
        }
    };

    // 3. Gắn nút vào body
    document.body.appendChild(btn);
})();