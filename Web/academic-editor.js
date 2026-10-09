/**
 * ACADEMIC RICH TEXT EDITOR (WYSIWYG)
 * Chuyên biệt cho Tạp chí Khoa học & Xuất bản Học thuật HUIT
 * Hỗ trợ: Kiểu chữ, cỡ chữ, phông chữ, in nghiêng danh pháp, ký hiệu toán & hóa học, căn đều hai bên
 */

(function () {
  'use strict';

  // Danh mục ký hiệu học thuật & toán học hay dùng trong bài báo khoa học
  const ACADEMIC_SYMBOLS = [
    '±', '×', '÷', '≤', '≥', '≠', '≈', '≡',
    '∞', '∝', '∫', '∑', '∏', '√', '∂', '∇',
    'α', 'β', 'γ', 'δ', 'ε', 'ζ', 'η', 'θ',
    'λ', 'μ', 'ν', 'ξ', 'π', 'ρ', 'σ', 'τ',
    'φ', 'χ', 'ψ', 'ω', 'Δ', 'Γ', 'Θ', 'Λ',
    'Ξ', 'Π', 'Σ', 'Φ', 'Ψ', 'Ω', '℃', '‰',
    'Å', 'ħ', '→', '↔', '⇒', '⇔', '∈', '∉'
  ];

  // SVG Icons sắc nét cho thanh công cụ
  const ICONS = {
    bold: '<svg viewBox="0 0 24 24"><path d="M6 4h8a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z"></path><path d="M6 12h9a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z"></path></svg>',
    italic: '<svg viewBox="0 0 24 24"><line x1="19" y1="4" x2="10" y2="4"></line><line x1="14" y1="20" x2="5" y2="20"></line><line x1="15" y1="4" x2="9" y2="20"></line></svg>',
    underline: '<svg viewBox="0 0 24 24"><path d="M6 3v7a6 6 0 0 0 12 0V3"></path><line x1="4" y1="21" x2="20" y2="21"></line></svg>',
    strike: '<svg viewBox="0 0 24 24"><path d="M16 4H9a3 3 0 0 0-2.83 4"></path><path d="M14 12a4 4 0 0 1 0 8H6"></path><line x1="4" y1="12" x2="20" y2="12"></line></svg>',
    sub: '<span style="font-size:12px;font-weight:700;">x<sub style="font-size:9px;">2</sub></span>',
    sup: '<span style="font-size:12px;font-weight:700;">x<sup style="font-size:9px;">2</sup></span>',
    alignLeft: '<svg viewBox="0 0 24 24"><line x1="17" y1="10" x2="3" y2="10"></line><line x1="21" y1="6" x2="3" y2="6"></line><line x1="21" y1="14" x2="3" y2="14"></line><line x1="17" y1="18" x2="3" y2="18"></line></svg>',
    alignCenter: '<svg viewBox="0 0 24 24"><line x1="18" y1="10" x2="6" y2="10"></line><line x1="21" y1="6" x2="3" y2="6"></line><line x1="21" y1="14" x2="3" y2="14"></line><line x1="18" y1="18" x2="6" y2="18"></line></svg>',
    alignRight: '<svg viewBox="0 0 24 24"><line x1="21" y1="10" x2="7" y2="10"></line><line x1="21" y1="6" x2="3" y2="6"></line><line x1="21" y1="14" x2="3" y2="14"></line><line x1="21" y1="18" x2="7" y2="18"></line></svg>',
    alignJustify: '<svg viewBox="0 0 24 24"><line x1="21" y1="6" x2="3" y2="6"></line><line x1="21" y1="10" x2="3" y2="10"></line><line x1="21" y1="14" x2="3" y2="14"></line><line x1="21" y1="18" x2="3" y2="18"></line></svg>',
    numList: '<svg viewBox="0 0 24 24"><line x1="10" y1="6" x2="21" y2="6"></line><line x1="10" y1="12" x2="21" y2="12"></line><line x1="10" y1="18" x2="21" y2="18"></line><path d="M4 6h1v4"></path><path d="M4 10h2"></path><path d="M6 18H4c0-1 2-2 2-3s-1-1.5-2-1"></path></svg>',
    bulletList: '<svg viewBox="0 0 24 24"><line x1="8" y1="6" x2="21" y2="6"></line><line x1="8" y1="12" x2="21" y2="12"></line><line x1="8" y1="18" x2="21" y2="18"></line><line x1="3" y1="6" x2="3.01" y2="6"></line><line x1="3" y1="12" x2="3.01" y2="12"></line><line x1="3" y1="18" x2="3.01" y2="18"></line></svg>',
    outdent: '<svg viewBox="0 0 24 24"><polyline points="7 8 3 12 7 16"></polyline><line x1="21" y1="12" x2="11" y2="12"></line><line x1="21" y1="6" x2="11" y2="6"></line><line x1="21" y1="18" x2="11" y2="18"></line></svg>',
    indent: '<svg viewBox="0 0 24 24"><polyline points="3 8 7 12 3 16"></polyline><line x1="21" y1="12" x2="11" y2="12"></line><line x1="21" y1="6" x2="11" y2="6"></line><line x1="21" y1="18" x2="11" y2="18"></line></svg>',
    symbol: '<span style="font-family:serif;font-size:14px;font-weight:700;">Ω</span>',
    clear: '<span style="font-size:12px;font-weight:700;">T<span style="font-size:9px;vertical-align:sub;">x</span></span>',
    source: '<svg viewBox="0 0 24 24"><polyline points="16 18 22 12 16 6"></polyline><polyline points="8 6 2 12 8 18"></polyline></svg>',
    undo: '<svg viewBox="0 0 24 24"><path d="M3 7v6h6"></path><path d="M21 17a9 9 0 0 0-9-9 9 9 0 0 0-6 2.3L3 13"></path></svg>',
    redo: '<svg viewBox="0 0 24 24"><path d="M21 7v6h-6"></path><path d="M3 17a9 9 0 0 1 9-9 9 9 0 0 1 6 2.3l3 2.7"></path></svg>'
  };

  /**
   * Khởi tạo Academic Rich Text Editor cho một textarea
   */
  function initAcademicEditor(textarea, options = {}) {
    if (typeof textarea === 'string') {
      textarea = document.getElementById(textarea);
    }
    if (!textarea || textarea.dataset.hasAcademicEditor) return;

    textarea.dataset.hasAcademicEditor = 'true';
    textarea.classList.add('has-academic-editor');

    const placeholder = textarea.getAttribute('placeholder') || 'Nhập nội dung học thuật tại đây...';

    // Tạo khung chứa Editor
    const container = document.createElement('div');
    container.className = 'academic-editor-container';

    // 1. Tạo Toolbar
    const toolbar = document.createElement('div');
    toolbar.className = 'academic-editor-toolbar';
    toolbar.setAttribute('role', 'toolbar');
    toolbar.setAttribute('aria-label', 'Thanh công cụ định dạng học thuật');

    // Nhóm 1: Kiểu văn bản (Paragraph / Heading)
    const styleSelect = document.createElement('select');
    styleSelect.className = 'rte-select rte-style';
    styleSelect.title = 'Kiểu đoạn văn';
    styleSelect.innerHTML = `
      <option value="p">Đoạn văn (Paragraph)</option>
      <option value="h3">Tiêu đề phụ 3</option>
      <option value="h4">Tiêu đề phụ 4</option>
      <option value="blockquote">Khung trích dẫn</option>
    `;
    styleSelect.onchange = () => {
      formatDoc('formatBlock', styleSelect.value);
    };

    // Nhóm 2: Phông chữ học thuật (Font family)
    const fontSelect = document.createElement('select');
    fontSelect.className = 'rte-select rte-font';
    fontSelect.title = 'Phông chữ học thuật';
    fontSelect.innerHTML = `
      <option value="Inter, sans-serif">Mặc định (Inter)</option>
      <option value="'Times New Roman', Times, serif" selected>Times New Roman</option>
      <option value="Arial, sans-serif">Arial</option>
      <option value="Georgia, serif">Georgia</option>
      <option value="'Courier New', monospace">Courier New</option>
    `;
    fontSelect.onchange = () => {
      formatDoc('fontName', fontSelect.value);
    };

    // Nhóm 3: Cỡ chữ (Font size)
    const sizeSelect = document.createElement('select');
    sizeSelect.className = 'rte-select rte-size';
    sizeSelect.title = 'Cỡ chữ';
    sizeSelect.innerHTML = `
      <option value="12px">12px (Nhỏ)</option>
      <option value="13px" selected>13px (Chuẩn bản thảo)</option>
      <option value="14px">14px (Vừa)</option>
      <option value="16px">16px (Lớn)</option>
      <option value="18px">18px (Đề mục)</option>
    `;
    sizeSelect.onchange = () => {
      applyCustomFontSize(sizeSelect.value);
    };

    // Màu chữ & Màu nền (Color Pickers)
    const textColorBox = createColorControl('Màu chữ', '#0f172a', (color) => formatDoc('foreColor', color));
    const bgColorBox = createColorControl('Màu nền / Dạ quang', '#fef08a', (color) => formatDoc('hiliteColor', color));

    toolbar.append(
      styleSelect,
      fontSelect,
      sizeSelect,
      textColorBox,
      bgColorBox,
      createDivider()
    );

    // Nhóm 4: Định dạng inline (B, I, U, S, Sub, Sup)
    toolbar.append(
      createButton('bold', 'In đậm (Ctrl+B)', () => formatDoc('bold')),
      createButton('italic', 'In nghiêng danh pháp/thuật ngữ (Ctrl+I)', () => formatDoc('italic')),
      createButton('underline', 'Gạch chân (Ctrl+U)', () => formatDoc('underline')),
      createButton('strike', 'Gạch ngang', () => formatDoc('strikeThrough')),
      createButton('sub', 'Chỉ số dưới (Ví dụ H₂O, x₁)', () => formatDoc('subscript')),
      createButton('sup', 'Chỉ số trên / Số mũ (Ví dụ m², R²)', () => formatDoc('superscript')),
      createDivider()
    );

    // Nhóm 5: Căn lề
    toolbar.append(
      createButton('alignLeft', 'Căn trái', () => formatDoc('justifyLeft')),
      createButton('alignCenter', 'Căn giữa', () => formatDoc('justifyCenter')),
      createButton('alignRight', 'Căn phải', () => formatDoc('justifyRight')),
      createButton('alignJustify', 'Căn đều 2 bên (Chuẩn xuất bản)', () => formatDoc('justifyFull')),
      createDivider()
    );

    // Nhóm 6: Danh sách & Thụt lề
    toolbar.append(
      createButton('numList', 'Danh sách đánh số (1, 2, 3)', () => formatDoc('insertOrderedList')),
      createButton('bulletList', 'Danh sách gạch đầu dòng', () => formatDoc('insertUnorderedList')),
      createButton('outdent', 'Giảm thụt lề', () => formatDoc('outdent')),
      createButton('indent', 'Tăng thụt lề', () => formatDoc('indent')),
      createDivider()
    );

    // Nhóm 7: Ký hiệu học thuật (Omega Popover)
    const symbolWrap = document.createElement('div');
    symbolWrap.style.position = 'relative';
    const symbolBtn = createButton('symbol', 'Chèn ký hiệu khoa học & toán học (Ω)', (e) => {
      e.stopPropagation();
      symbolPopover.classList.toggle('is-open');
    });
    
    const symbolPopover = document.createElement('div');
    symbolPopover.className = 'rte-popover';
    const symbolGrid = document.createElement('div');
    symbolGrid.className = 'rte-symbol-grid';

    ACADEMIC_SYMBOLS.forEach(sym => {
      const sBtn = document.createElement('button');
      sBtn.type = 'button';
      sBtn.className = 'rte-symbol-btn';
      sBtn.textContent = sym;
      sBtn.title = `Chèn ký hiệu ${sym}`;
      sBtn.onclick = (e) => {
        e.preventDefault();
        e.stopPropagation();
        insertTextAtCursor(sym);
        symbolPopover.classList.remove('is-open');
      };
      symbolGrid.append(sBtn);
    });

    symbolPopover.append(symbolGrid);
    symbolWrap.append(symbolBtn, symbolPopover);

    document.addEventListener('click', (e) => {
      if (!symbolWrap.contains(e.target)) {
        symbolPopover.classList.remove('is-open');
      }
    });

    toolbar.append(symbolWrap);

    // Nhóm 8: Tiện ích: Tẩy định dạng, Xem mã HTML, Undo, Redo
    toolbar.append(
      createButton('clear', 'Tẩy định dạng văn bản', () => formatDoc('removeFormat')),
      createDivider(),
      createButton('undo', 'Hoàn tác (Ctrl+Z)', () => formatDoc('undo')),
      createButton('redo', 'Làm lại (Ctrl+Y)', () => formatDoc('redo')),
      createButton('source', 'Xem / Chỉnh sửa mã nguồn HTML', () => toggleSourceMode())
    );

    // 2. Tạo Visual Contenteditable Body
    const editorBody = document.createElement('div');
    editorBody.className = 'academic-editor-body';
    editorBody.contentEditable = 'true';
    editorBody.setAttribute('role', 'textbox');
    editorBody.setAttribute('aria-multiline', 'true');
    editorBody.setAttribute('data-placeholder', placeholder);
    editorBody.style.fontFamily = "'Times New Roman', Times, serif";

    // Khởi tạo nội dung ban đầu từ textarea
    if (textarea.value) {
      editorBody.innerHTML = textarea.value.trim();
    }

    // 3. Tạo Source View (Textarea xem mã HTML)
    const sourceView = document.createElement('textarea');
    sourceView.className = 'academic-editor-source';
    sourceView.placeholder = '<p>Nhập mã HTML trực tiếp...</p>';

    container.append(toolbar, editorBody, sourceView);
    textarea.parentNode.insertBefore(container, textarea);

    // ==========================================
    // LOGIC ĐIỀU HÀNH VÀ ĐỒNG BỘ 2 CHIỀU
    // ==========================================

    let isSourceMode = false;
    let isInternalSyncing = false;

    function formatDoc(command, value = null) {
      if (isSourceMode) return;
      editorBody.focus();
      document.execCommand(command, false, value);
      syncToTextarea();
      updateToolbarState();
    }

    function applyCustomFontSize(size) {
      if (isSourceMode) return;
      editorBody.focus();
      const sel = window.getSelection();
      if (!sel.rangeCount || sel.isCollapsed) return;

      const span = document.createElement('span');
      span.style.fontSize = size;
      const range = sel.getRangeAt(0);
      try {
        range.surroundContents(span);
      } catch {
        document.execCommand('fontSize', false, '3');
      }
      syncToTextarea();
    }

    function insertTextAtCursor(text) {
      if (isSourceMode) {
        sourceView.setRangeText(text, sourceView.selectionStart, sourceView.selectionEnd, 'end');
        syncFromSource();
        return;
      }
      editorBody.focus();
      document.execCommand('insertText', false, text);
      syncToTextarea();
    }

    function toggleSourceMode() {
      isSourceMode = !isSourceMode;
      if (isSourceMode) {
        sourceView.value = editorBody.innerHTML;
        sourceView.style.display = 'block';
        editorBody.style.display = 'none';
        sourceView.focus();
      } else {
        editorBody.innerHTML = sourceView.value;
        sourceView.style.display = 'none';
        editorBody.style.display = 'block';
        editorBody.focus();
        syncToTextarea();
      }
    }

    const originalValueSetter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value')?.set;
    const originalValueGetter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value')?.get;

    function syncToTextarea() {
      if (isInternalSyncing) return;
      isInternalSyncing = true;
      try {
        const html = isSourceMode ? sourceView.value : editorBody.innerHTML;
        const clean = (editorBody.textContent.trim() === '') ? '' : html;
        if (originalValueSetter) {
          originalValueSetter.call(textarea, clean);
        } else {
          textarea.value = clean;
        }
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
        textarea.dispatchEvent(new Event('change', { bubbles: true }));
      } finally {
        isInternalSyncing = false;
      }
    }

    function syncFromSource() {
      if (isInternalSyncing) return;
      isInternalSyncing = true;
      try {
        if (originalValueSetter) {
          originalValueSetter.call(textarea, sourceView.value);
        } else {
          textarea.value = sourceView.value;
        }
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
      } finally {
        isInternalSyncing = false;
      }
    }

    // Lắng nghe sự kiện người dùng gõ
    editorBody.addEventListener('input', () => {
      syncToTextarea();
      updateToolbarState();
    });

    editorBody.addEventListener('keyup', updateToolbarState);
    editorBody.addEventListener('mouseup', updateToolbarState);

    sourceView.addEventListener('input', syncFromSource);

    // Chặn dán HTML bẩn (lọc từ Word sang HTML gọn)
    editorBody.addEventListener('paste', () => {
      setTimeout(() => {
        syncToTextarea();
      }, 0);
    });

    function updateToolbarState() {
      // Cập nhật trạng thái active cho các nút B, I, U...
      if (isSourceMode) return;
      const commands = ['bold', 'italic', 'underline', 'strikeThrough', 'subscript', 'superscript'];
      commands.forEach(cmd => {
        const btn = toolbar.querySelector(`[data-cmd="${cmd}"]`);
        if (btn) {
          try {
            const active = document.queryCommandState(cmd);
            btn.classList.toggle('is-active', active);
          } catch {}
        }
      });
    }

    // Hỗ trợ cập nhật từ ngoài (ví dụ insertIMRaDTemplate)
    container._academicEditor = {
      setContent(content) {
        if (isInternalSyncing) return;
        isInternalSyncing = true;
        try {
          if (isSourceMode) {
            sourceView.value = content || '';
          } else {
            editorBody.innerHTML = content || '';
          }
          if (originalValueSetter) {
            originalValueSetter.call(textarea, content || '');
          } else {
            textarea.value = content || '';
          }
          textarea.dispatchEvent(new Event('input', { bubbles: true }));
        } finally {
          isInternalSyncing = false;
        }
      },
      getContent() {
        return isSourceMode ? sourceView.value : editorBody.innerHTML;
      },
      getText() {
        return editorBody.innerText.trim();
      },
      focus() {
        if (isSourceMode) sourceView.focus();
        else editorBody.focus();
      }
    };

    textarea._academicEditorContainer = container;

    // Quan sát nếu textarea bị gán value từ bên ngoài
    if (originalValueSetter) {
      Object.defineProperty(textarea, 'value', {
        set(val) {
          originalValueSetter.call(this, val);
          if (!isInternalSyncing && this._academicEditorContainer && this._academicEditorContainer._academicEditor) {
            isInternalSyncing = true;
            try {
              if (isSourceMode) {
                sourceView.value = val || '';
              } else {
                editorBody.innerHTML = val || '';
              }
            } finally {
              isInternalSyncing = false;
            }
          }
        },
        get() {
          return originalValueGetter ? originalValueGetter.call(this) : '';
        }
      });
    }

    return container;
  }

  // Helper tạo nút trên toolbar
  function createButton(iconKey, tooltip, onClick) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'rte-btn';
    btn.title = tooltip;
    btn.innerHTML = ICONS[iconKey] || iconKey;
    btn.setAttribute('data-cmd', iconKey);
    btn.onclick = (e) => {
      e.preventDefault();
      onClick(e);
    };
    return btn;
  }

  function createDivider() {
    const d = document.createElement('div');
    d.className = 'rte-divider';
    return d;
  }

  function createColorControl(title, defaultColor, onColorSelect) {
    const wrap = document.createElement('div');
    wrap.className = 'rte-color-picker';

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'rte-btn';
    btn.title = title;
    btn.innerHTML = `<span style="font-weight:700;font-size:12px;">A</span>`;
    
    const bar = document.createElement('div');
    bar.className = 'rte-color-bar';
    bar.style.background = defaultColor;
    btn.append(bar);

    const input = document.createElement('input');
    input.type = 'color';
    input.className = 'rte-hidden-color';
    input.value = defaultColor;
    input.onchange = () => {
      bar.style.background = input.value;
      onColorSelect(input.value);
    };

    btn.onclick = (e) => {
      e.preventDefault();
      input.click();
    };

    wrap.append(btn, input);
    return wrap;
  }

  // Tự động quét và khởi tạo cho tất cả textarea có class 'academic-rich-editor'
  function initAllAcademicEditors() {
    document.querySelectorAll('textarea.academic-rich-editor').forEach(el => {
      initAcademicEditor(el);
    });
  }

  // Đăng ký toàn cục
  window.initAcademicEditor = initAcademicEditor;
  window.initAllAcademicEditors = initAllAcademicEditors;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initAllAcademicEditors);
  } else {
    initAllAcademicEditors();
  }
})();
