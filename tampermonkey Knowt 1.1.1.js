// ==UserScript==
// @name         Knowt 1.1.1
// @namespace    http://tampermonkey.net/
// @version      1.1
// @description  try to take over the world!
// @author       You
// @match        https://knowt.com/*
// @icon         https://www.google.com/s2/favicons?sz=64&domain=knowt.com
// @grant        none
// ==/UserScript==

(function () {
    'use strict';

    const KEY_VOL = 'knowt_tts_volume';
    const KEY_DELAY = 'knowt_tts_delay';

    let volume = parseInt(localStorage.getItem(KEY_VOL)) ?? 100;
    let delaySec = parseFloat(localStorage.getItem(KEY_DELAY)) ?? 0;
    let lastQuestionText = '';
    let speechTimer = null;

    function getVoice(lang) {
        const voices = window.speechSynthesis.getVoices();
        if (lang === 'ja') return voices.find(v => v.lang.startsWith('ja')) || null;
        return voices.find(v => v.lang.startsWith('en')) || null;
    }

    function speakText(text) {
        if (!text) return;
        window.speechSynthesis.cancel();
        if (speechTimer) clearTimeout(speechTimer);

        speechTimer = setTimeout(() => {
            const utterance = new SpeechSynthesisUtterance(text);
            const isJapanese = /[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff]/.test(text);

            utterance.lang = isJapanese ? 'ja-JP' : 'en-US';
            const voice = getVoice(isJapanese ? 'ja' : 'en');
            if (voice) utterance.voice = voice;
            utterance.volume = Math.min(1.0, volume / 100);

            window.speechSynthesis.speak(utterance);
        }, delaySec * 1000);
    }

    function speakText2(text) {
        if (!text) return;
        window.speechSynthesis.cancel();
        if (speechTimer) clearTimeout(speechTimer);

        speechTimer = setTimeout(() => {
            const utterance = new SpeechSynthesisUtterance(text);
            const isJapanese = /[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff]/.test(text);

            utterance.lang = isJapanese ? 'ja-JP' : 'en-US';
            const voice = getVoice(isJapanese ? 'ja' : 'en');
            if (voice) utterance.voice = voice;
            utterance.volume = Math.min(1.0, volume / 100);

            window.speechSynthesis.speak(utterance);
        }, delaySec * 1);
    }

    function injectHeaderControls() {
        const navbar = document.querySelector('.studyNavbar-module__ci9FBa__container');
        if (!navbar || document.querySelector('#knowt-custom-controls')) return;

        const controlsWrapper = document.createElement('div');
        controlsWrapper.id = 'knowt-custom-controls';
        controlsWrapper.style.cssText = `
            display: flex; align-items: center; gap: 12px;
            background: rgba(255, 255, 255, 0.1); padding: 4px 10px;
            border-radius: 6px; font-size: 12px; color: #fff;
            margin-left: auto; user-select: none; z-index: 999;
        `;

        controlsWrapper.innerHTML = `
            <div style="display:flex; align-items:center; gap:5px;">
                <span>Vol:</span>
                <input type="range" id="knowt-vol-slider" min="0" max="300" value="${volume}" style="width:165px; cursor:pointer;">
                <span id="knowt-vol-val" style="text-align:right;">${volume}%</span>
            </div>
            <div style="display:flex; align-items:center; gap:5px;">
                <span>Delay:</span>
                <input type="range" id="knowt-delay-slider" min="0" max="3.95" step="3.95" value="${delaySec}" style="width:25px; cursor:pointer;">
                <span id="knowt-delay-val" style="text-align:right;">${delaySec}s</span>
            </div>
        `;

        navbar.appendChild(controlsWrapper);

        controlsWrapper.querySelector('#knowt-vol-slider').addEventListener('input', (e) => {
            volume = parseInt(e.target.value);
            controlsWrapper.querySelector('#knowt-vol-val').textContent = `${volume}%`;
            localStorage.setItem(KEY_VOL, volume);
        });

        controlsWrapper.querySelector('#knowt-delay-slider').addEventListener('input', (e) => {
            delaySec = parseFloat(e.target.value);
            controlsWrapper.querySelector('#knowt-delay-val').textContent = `${delaySec}s`;
            localStorage.setItem(KEY_DELAY, delaySec);
        });
    }

    function updateState() {
        // CHỐT CHẶN: Nếu đường dẫn hiện tại KHÔNG PHẢI là trang flashcards
        // lập tức hủy lệnh đọc cũ và dừng việc update DOM lại.
        if (!window.location.href.includes('/study/flashcards')) {
            window.speechSynthesis.cancel();
            return;
        }

        const setTitleEl = document.querySelector('.ellipsisText.MuiBox-root.knowt-6o8n9d') || document.querySelector('.studyNavbar-module__ci9FBa__container .ellipsisText');
        if (setTitleEl && setTitleEl.textContent.trim()) {
            if (document.title !== setTitleEl.textContent.trim()) document.title = setTitleEl.textContent.trim();
        }

        injectHeaderControls();

        const quesEl = document.querySelector('.breakpoints-module__zbexiG__mdDownColumnReverse');
        if (quesEl) {
            const currentFullText = quesEl.innerText.trim();
            if (!currentFullText) return;

            const firstLine = currentFullText.split('\n')[0].trim();

            const termBox = document.querySelector('.studyPage-module__2Yqjuq__skeletonHeader .breakpoints-module__zbexiG__smDownDisplayNone');
            if (termBox && termBox.textContent !== firstLine) {
                termBox.textContent = firstLine;
            }

            if (firstLine !== lastQuestionText && !firstLine.toLowerCase().includes('copyright')) {
                lastQuestionText = firstLine;
                speakText(firstLine);
            }
        }
    }

    window.addEventListener('keydown', (e) => {
        // Thêm điều kiện: Phím tắt cũng chỉ hoạt động trong trang flashcards
        if (!window.location.href.includes('/study/flashcards')) return;

        const isNumpad0 = e.code === 'Numpad0' || (e.key === '0' && e.location === 3);
        const isNumpadDot = e.code === 'NumpadDecimal' || e.key === '.';
        if ((isNumpad0 || isNumpadDot) && lastQuestionText) {
            speakText2(lastQuestionText);
        }
    });

    const observer = new MutationObserver(() => updateState());
    observer.observe(document.body, { childList: true, subtree: true, characterData: true });

    if (speechSynthesis.onvoiceschanged !== undefined) speechSynthesis.onvoiceschanged = () => {};
})();