// ==UserScript==

// @name         riki n4 n3 fgdgdf

// @namespace    http://tampermonkey.net/

// @version      1.3

// @description  Export Quizlet UI Fix & Mobile Menu Button Fix

// @author       You

// @match        https://riki.edu.vn/online/my-course/*

// @icon         https://www.google.com/s2/favicons?sz=64&domain=edu.vn

// @grant        none

// ==/UserScript==





(function () {

    'use strict';



    // Inject CSS cố định nút menu bài học màu trắng LÊN TRÊN nút Export xanh

    function injectMobileStyles() {

        if (document.getElementById('mobile-menu-fix-style')) return;



        const style = document.createElement('style');

        style.id = 'mobile-menu-fix-style';

        style.type = 'text/css';

        style.innerHTML = `

            @media screen and (max-width: 768px) {

                /* Nút Menu khóa học (màu trắng) ghim ở phía TRÊN nút Export xanh */

                .button_menu_course {

                    position: fixed !important;

                    bottom: 85px !important; /* Ghim lên trên nút xanh */

                    right: 5px !important;

                    top: auto !important;

                    left: auto !important;

                    z-index: 999998 !important;

                    display: flex !important;

                    visibility: visible !important;

                    opacity: 1 !important;

                    box-shadow: 0 3px 3px rgba(0, 0, 0, 0.3) !important;

                }



                .nav_mobile {

                    display: block !important;

                    position: static !important;

                }

            }

        `;

        document.head.appendChild(style);

    }



    // Hiển thị thông báo Toast tự động biến mất sau 2s

    function showToast(message, isError = false) {

        const oldToast = document.getElementById('quizlet-export-toast');

        if (oldToast) oldToast.remove();



        const toast = document.createElement('div');

        toast.id = 'quizlet-export-toast';

        toast.innerText = message;

        toast.style.cssText = `

            position: fixed;

            top: 20px;

            right: 20px;

            padding: 10px 18px;

            background-color: ${isError ? '#f5222d' : '#52c41a'};

            color: #ffffff;

            font-size: 14px;

            font-weight: 600;

            border-radius: 6px;

            box-shadow: 0 3px 12px rgba(0, 0, 0, 0.15);

            z-index: 999999;

            transition: opacity 0.3s ease;

        `;



        document.body.appendChild(toast);



        setTimeout(() => {

            toast.style.opacity = '0';

            setTimeout(() => toast.remove(), 300);

        }, 2000);

    }



    // Xử lý lấy và format dữ liệu

    function handleExport() {

        const items = document.querySelectorAll('[class*="vocabulary-lesson__block__item"], [class*="vocabulary-lesson__block_item"]');



        if (!items || items.length === 0) {

            showToast('Không tìm thấy danh sách từ vựng!', true);

            return;

        }



        const formattedList = [];



        items.forEach(item => {

            const frontEl = item.querySelector('[class*="front"]');

            const backEl = item.querySelector('[class*="parent-content"], [class*="content"]');



            if (frontEl && backEl) {

                const rawFront = frontEl.innerText.trim();

                const frontText = rawFront

                    .replace(/\s*[（\(](.*?)[）\)]/g, '\n$1')

                    .split('\n')

                    .map(line => line.trim())

                    .filter(line => line.length > 0)

                    .join('\n');



                const backText = backEl.innerText.trim().replace(/\r?\n|\r/g, ' ');



                if (frontText && backText) {

                    formattedList.push(`${frontText} ++ ${backText}`);

                }

            }

        });



        if (formattedList.length === 0) {

            showToast('Không trích xuất được dữ liệu!', true);

            return;

        }



        const resultText = formattedList.join('\n---\n');



        if (typeof GM_setClipboard !== 'undefined') {

            GM_setClipboard(resultText);

            showToast(`Đã chép ${formattedList.length} từ vựng vào bộ nhớ tạm!`);

        } else {

            navigator.clipboard.writeText(resultText)

                .then(() => showToast(`Đã chép ${formattedList.length} từ vựng vào bộ nhớ tạm!`))

                .catch(() => showToast('Lỗi khi sao chép dữ liệu!', true));

        }

    }



    // Nút Export Quizlet (màu xanh) GIỮ NGUYÊN VỊ TRÍ GỐC ở góc dưới (bottom: 30px)

    function injectFloatingButton() {

        if (document.getElementById('btn-quizlet-export')) return;



        const btn = document.createElement('button');

        btn.id = 'btn-quizlet-export';

        btn.innerText = '📥';



        btn.style.cssText = `

            position: fixed;

            bottom: 5px;

            right: 10px;

            z-index: 999999;

            padding: 10px 18px;

            background-color: #05ec3a;

            color: #ffffff;

            border: none;

            border-radius: 50px;

            font-size: 14px;

            font-weight: bold;

            cursor: pointer;

            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);

            transition: transform 0.2s ease, background-color 0.2s ease, box-shadow 0.2s ease;

            display: flex;

            align-items: center;

            gap: 6px;

        `;



        btn.addEventListener('mouseenter', () => {

            btn.style.backgroundColor = '#218838';

            btn.style.transform = 'translateY(-3px) scale(1.03)';

            btn.style.boxShadow = '0 6px 16px rgba(0, 0, 0, 0.35)';

        });



        btn.addEventListener('mouseleave', () => {

            btn.style.backgroundColor = '#28a745';

            btn.style.transform = 'translateY(0) scale(1)';

            btn.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.25)';

        });



        btn.addEventListener('mousedown', () => {

            btn.style.transform = 'translateY(1px) scale(0.97)';

        });



        btn.addEventListener('click', handleExport);

        document.body.appendChild(btn);

    }



    injectMobileStyles();

    setInterval(() => {

        injectMobileStyles();

        injectFloatingButton();

    }, 1000);

})();

