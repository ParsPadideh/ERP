<%@ Page Title="کاتالوگ فروش" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="sales-presentation.aspx.cs" Inherits="admin_sales_presentation" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    /* ═══════════════════════════════════════════════════════
       1. BASE
       ═══════════════════════════════════════════════════════ */
    body { background: #f8fafc; }

    /* ═══════════════════════════════════════════════════════
       2. HEADER
       ═══════════════════════════════════════════════════════ */
    .pres-header {
        background: linear-gradient(135deg, #1e40af 0%, #2563eb 100%);
        color: #fff;
        box-shadow: 0 4px 20px -8px rgba(30, 64, 175, 0.4);
    }

    .cart-badge {
        position: absolute;
        top: -6px;
        left: -6px;
        min-width: 22px;
        height: 22px;
        padding: 0 6px;
        border-radius: 11px;
        background: #ef4444;
        color: #fff;
        font-size: 11px;
        font-weight: 800;
        display: grid;
        place-items: center;
        box-shadow: 0 2px 8px rgba(239, 68, 68, 0.5);
        border: 2px solid #fff;
    }

    /* ═══════════════════════════════════════════════════════
       3. CATEGORY CHIPS
       ═══════════════════════════════════════════════════════ */
    .category-chips {
        display: flex;
        gap: 8px;
        overflow-x: auto;
        padding: 4px 0;
        scrollbar-width: none;
        -webkit-overflow-scrolling: touch;
    }
    .category-chips::-webkit-scrollbar { display: none; }

    .category-chip {
        flex-shrink: 0;
        padding: 8px 14px;
        border-radius: 20px;
        background: #fff;
        border: 1.5px solid #eef2f6;
        font-size: 13px;
        font-weight: 600;
        color: #475569;
        cursor: pointer;
        transition: all .2s;
        white-space: nowrap;
        user-select: none;
    }
    .category-chip:hover { border-color: #93c5fd; color: #2563eb; }
    .category-chip.active {
        background: #2563eb;
        border-color: #2563eb;
        color: #fff;
        box-shadow: 0 4px 12px -4px rgba(37, 99, 235, 0.5);
    }

    /* ═══════════════════════════════════════════════════════
       4. PRODUCT CARD
       ═══════════════════════════════════════════════════════ */
    .product-card {
        background: #fff;
        border-radius: 16px;
        overflow: hidden;
        border: 1.5px solid #eef2f6;
        transition: all .25s cubic-bezier(.4, 0, .2, 1);
        cursor: pointer;
        display: flex;
        flex-direction: column;
        position: relative;
        user-select: none;
    }
    .product-card:hover {
        transform: translateY(-3px);
        box-shadow: 0 16px 32px -12px rgba(15, 23, 42, 0.15);
        border-color: #bfdbfe;
    }
    .product-card.in-cart {
        border-color: #10b981;
        box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.15);
    }
    .product-card.out-of-stock { opacity: 0.55; cursor: not-allowed; }
    .product-card.out-of-stock:hover { transform: none; box-shadow: none; }

    /* ─── تصویر کارت ─── */
    .product-img {
        aspect-ratio: 1 / 1;
        display: grid;
        place-items: center;
        position: relative;
        overflow: hidden;
        background: #f1f5f9;
    }
    .product-img-real {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
    }
    .product-img-icon {
        font-size: 56px;
        filter: drop-shadow(0 4px 8px rgba(0,0,0,0.15));
    }
    .product-img-gradient-1 { background: linear-gradient(135deg, #fbbf24, #f59e0b); }
    .product-img-gradient-2 { background: linear-gradient(135deg, #a78bfa, #8b5cf6); }
    .product-img-gradient-3 { background: linear-gradient(135deg, #34d399, #10b981); }
    .product-img-gradient-4 { background: linear-gradient(135deg, #fb7185, #ef4444); }
    .product-img-gradient-5 { background: linear-gradient(135deg, #60a5fa, #3b82f6); }
    .product-img-gradient-6 { background: linear-gradient(135deg, #f472b6, #ec4899); }

    /* ─── بج سبد ─── */
    .product-cart-badge {
        position: absolute;
        top: 10px;
        right: 10px;
        min-width: 28px;
        height: 28px;
        padding: 0 8px;
        border-radius: 14px;
        background: #10b981;
        color: #fff;
        font-size: 13px;
        font-weight: 800;
        display: grid;
        place-items: center;
        box-shadow: 0 4px 12px rgba(16, 185, 129, 0.5);
        border: 2px solid #fff;
        z-index: 2;
    }

    /* ─── بج موجودی ─── */
    .product-stock-badge {
        position: absolute;
        top: 10px;
        left: 10px;
        padding: 3px 9px;
        border-radius: 10px;
        font-size: 10.5px;
        font-weight: 800;
        background: rgba(255,255,255,0.95);
        color: #059669;
        backdrop-filter: blur(4px);
        z-index: 2;
    }
    .product-stock-badge.low { color: #d97706; background: rgba(255,251,235,0.95); }
    .product-stock-badge.out { color: #dc2626; background: rgba(254,242,242,0.95); }

    /* ─── دکمه گالری روی کارت ─── */
    .gallery-btn {
        position: absolute;
        bottom: 10px;
        left: 50%;
        transform: translateX(-50%) translateY(6px);
        z-index: 3;
        display: inline-flex;
        align-items: center;
        gap: 5px;
        padding: 6px 13px;
        border-radius: 20px;
        background: rgba(15, 23, 42, 0.82);
        color: #fff;
        font-size: 11.5px;
        font-weight: 800;
        border: 1px solid rgba(255,255,255,0.2);
        cursor: pointer;
        backdrop-filter: blur(8px);
        box-shadow: 0 6px 16px rgba(0,0,0,0.3);
        opacity: 0;
        transition: all .25s cubic-bezier(.4, 0, .2, 1);
        font-family: inherit;
    }
    .product-card:hover .gallery-btn {
        opacity: 1;
        transform: translateX(-50%) translateY(0);
    }
    .gallery-btn:hover {
        background: rgba(37, 99, 235, 0.95);
        border-color: #60a5fa;
    }
    .gallery-btn:active { transform: translateX(-50%) translateY(1px) scale(0.97); }

    @media (max-width: 640px) {
        .gallery-btn { opacity: 1; transform: translateX(-50%) translateY(0); }
    }

    .product-name {
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        line-height: 1.3;
        min-height: 34px;
    }

    /* ═══════════════════════════════════════════════════════
       5. CART DRAWER
       ═══════════════════════════════════════════════════════ */
    .cart-drawer {
        position: fixed;
        top: 0;
        left: 0;
        bottom: 0;
        width: 100%;
        max-width: 440px;
        background: #fff;
        z-index: 60;
        transform: translateX(-100%);
        transition: transform .3s cubic-bezier(.4, 0, .2, 1);
        display: flex;
        flex-direction: column;
        box-shadow: 20px 0 60px -20px rgba(15, 23, 42, 0.3);
    }
    .cart-drawer.open { transform: translateX(0); }

    .cart-overlay {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.5);
        backdrop-filter: blur(2px);
        z-index: 55;
        opacity: 0;
        visibility: hidden;
        transition: all .3s;
    }
    .cart-overlay.open { opacity: 1; visibility: visible; }

    /* ═══════════════════════════════════════════════════════
       6. MODALS (QTY, CUSTOMER PICKER, etc.)
       ═══════════════════════════════════════════════════════ */
    .qty-modal {
        position: fixed;
        inset: 0;
        z-index: 70;
        display: none;
        align-items: flex-end;
        justify-content: center;
    }
    .qty-modal.open { display: flex; }

    .qty-modal-content {
        background: #fff;
        width: 100%;
        max-width: 420px;
        border-radius: 20px 20px 0 0;
        padding: 20px;
        animation: slideUp .25s cubic-bezier(.4, 0, .2, 1);
        max-height: 90vh;
        overflow-y: auto;
    }

    @media (min-width: 768px) {
        .qty-modal { align-items: center; padding: 20px; }
        .qty-modal-content { border-radius: 20px; max-width: 500px; }
    }

    @keyframes slideUp {
        from { transform: translateY(20px); opacity: 0; }
        to { transform: translateY(0); opacity: 1; }
    }

    .qty-btn {
        width: 44px;
        height: 44px;
        border-radius: 12px;
        display: grid;
        place-items: center;
        background: #f1f5f9;
        color: #334155;
        font-size: 20px;
        font-weight: 800;
        border: none;
        cursor: pointer;
        transition: all .15s;
        user-select: none;
    }
    .qty-btn:hover { background: #e2e8f0; }
    .qty-btn:active { transform: scale(0.95); }

    .qty-input {
        width: 80px;
        height: 44px;
        text-align: center;
        font-family: 'Vazirmatn', monospace;
        font-size: 18px;
        font-weight: 800;
        border: 2px solid #eef2f6;
        border-radius: 12px;
        outline: none;
        transition: all .15s;
    }
    .qty-input:focus { border-color: #2563eb; }

    /* ═══════════════════════════════════════════════════════
       7. EMPTY & LOADING
       ═══════════════════════════════════════════════════════ */
    .empty-products {
        text-align: center;
        padding: 60px 20px;
    }
    .empty-products-icon {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: #f1f5f9;
        margin: 0 auto 16px;
        display: grid;
        place-items: center;
        color: #94a3b8;
    }

    .spinner {
        width: 20px;
        height: 20px;
        border: 2.5px solid rgba(255,255,255,0.3);
        border-top-color: #fff;
        border-radius: 50%;
        animation: spin 0.6s linear infinite;
        display: inline-block;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* ═══════════════════════════════════════════════════════
       8. TOAST
       ═══════════════════════════════════════════════════════ */
    .toast-container {
        position: fixed;
        top: 20px;
        left: 50%;
        transform: translateX(-50%);
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 8px;
        pointer-events: none;
    }
    .toast {
        padding: 12px 20px;
        border-radius: 12px;
        font-size: 13.5px;
        font-weight: 700;
        box-shadow: 0 10px 30px -10px rgba(15, 23, 42, 0.3);
        animation: toastIn .3s cubic-bezier(.4, 0, .2, 1);
        pointer-events: auto;
        max-width: 90vw;
    }
    @keyframes toastIn {
        from { opacity: 0; transform: translateY(-10px); }
        to { opacity: 1; transform: translateY(0); }
    }
    .toast--success { background: #10b981; color: #fff; }
    .toast--error { background: #ef4444; color: #fff; }
    .toast--info { background: #3b82f6; color: #fff; }

    /* ═══════════════════════════════════════════════════════
       9. GALLERY MODAL
       ═══════════════════════════════════════════════════════ */
    .gallery-modal {
        position: fixed;
        inset: 0;
        z-index: 90;
        display: none;
        align-items: center;
        justify-content: center;
        padding: 16px;
    }
    .gallery-modal.open { display: flex; }

    .gallery-backdrop {
        position: absolute;
        inset: 0;
        background: rgba(8, 12, 25, 0.85);
        backdrop-filter: blur(6px);
        animation: fadeIn .25s ease;
    }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

    .gallery-content {
        position: relative;
        width: 100%;
        max-width: 720px;
        max-height: 94vh;
        background: #0f172a;
        border-radius: 20px;
        overflow: hidden;
        display: flex;
        flex-direction: column;
        box-shadow: 0 30px 80px -20px rgba(0, 0, 0, 0.8);
        animation: zoomIn .3s cubic-bezier(.4, 0, .2, 1);
        border: 1px solid rgba(255, 255, 255, 0.08);
    }
    @keyframes zoomIn {
        from { transform: scale(0.94); opacity: 0; }
        to { transform: scale(1); opacity: 1; }
    }

    /* ─── دکمه بستن گالری ─── */
    .gallery-close {
        position: absolute;
        top: 12px;
        left: 12px;
        z-index: 5;
        width: 38px;
        height: 38px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        background: rgba(255, 255, 255, 0.12);
        color: #fff;
        border: 1px solid rgba(255, 255, 255, 0.2);
        cursor: pointer;
        transition: all .2s;
        backdrop-filter: blur(6px);
    }
    .gallery-close:hover {
        background: rgba(239, 68, 68, 0.9);
        border-color: rgba(239, 68, 68, 1);
        transform: rotate(90deg);
    }
    .gallery-close i { width: 20px; height: 20px; }

    /* ─── هدر گالری ─── */
    .gallery-header {
        padding: 16px 60px 12px;
        border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        background: rgba(255, 255, 255, 0.03);
        text-align: center;
    }

    /* ─── محل نمایش عکس اصلی ─── */
    .gallery-viewer {
        position: relative;
        flex: 1 1 auto;
        min-height: 0;
        height: 55vh;
        background: #0a0f1a;
        padding: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
    }

    .gallery-main-img {
        display: block;
        max-width: 100%;
        max-height: 100%;
        width: auto;
        height: auto;
        object-fit: contain;
        object-position: center;
        border-radius: 12px;
        user-select: none;
        -webkit-user-drag: none;
        background: transparent;
        cursor: zoom-in;
    }

    /* ─── دکمه زوم روی عکس ─── */
    .gallery-zoom-btn {
        position: absolute;
        top: 16px;
        right: 16px;
        z-index: 5;
        width: 42px;
        height: 42px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        background: rgba(255, 255, 255, 0.15);
        color: #fff;
        border: 1px solid rgba(255, 255, 255, 0.28);
        cursor: pointer;
        backdrop-filter: blur(8px);
        transition: all .2s;
        opacity: 0;
        transform: scale(0.9);
    }
    .gallery-viewer:hover .gallery-zoom-btn {
        opacity: 1;
        transform: scale(1);
    }
    .gallery-zoom-btn:hover {
        background: rgba(37, 99, 235, 0.95);
        border-color: #60a5fa;
        transform: scale(1.1);
    }
    .gallery-zoom-btn i { width: 18px; height: 18px; }

    /* ─── فلش‌های ناوبری ─── */
    .gallery-nav {
        position: absolute;
        top: 50%;
        transform: translateY(-50%);
        width: 46px;
        height: 46px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        background: rgba(255, 255, 255, 0.13);
        color: #fff;
        border: 1px solid rgba(255, 255, 255, 0.18);
        cursor: pointer;
        transition: all .2s;
        backdrop-filter: blur(6px);
        z-index: 4;
        opacity: 0;
    }
    .gallery-viewer:hover .gallery-nav { opacity: 1; }
    .gallery-nav:hover {
        background: rgba(37, 99, 235, 0.9);
        transform: translateY(-50%) scale(1.08);
    }
    .gallery-nav:disabled { opacity: 0.25; cursor: not-allowed; }
    .gallery-nav:disabled:hover {
        background: rgba(255, 255, 255, 0.13);
        transform: translateY(-50%);
    }
    .gallery-nav i { width: 22px; height: 22px; }

    .gallery-nav-prev { right: 12px; }
    .gallery-nav-next { left: 12px; }

    /* ─── شمارنده ─── */
    .gallery-counter {
        position: absolute;
        bottom: 12px;
        left: 50%;
        transform: translateX(-50%);
        padding: 5px 14px;
        border-radius: 20px;
        background: rgba(0, 0, 0, 0.55);
        color: #fff;
        font-size: 12px;
        font-weight: 700;
        font-family: 'Vazirmatn', monospace;
        backdrop-filter: blur(6px);
        border: 1px solid rgba(255, 255, 255, 0.1);
        z-index: 3;
    }
    .gallery-counter span { color: #93c5fd; }

    /* ─── ایموجی fallback ─── */
    .gallery-emoji {
        font-size: 140px;
        filter: drop-shadow(0 8px 24px rgba(0,0,0,0.5));
        user-select: none;
    }

    /* ─── Thumbnails ─── */
    .gallery-thumbnails {
        display: flex;
        gap: 8px;
        padding: 12px 16px;
        overflow-x: auto;
        background: rgba(0, 0, 0, 0.25);
        border-top: 1px solid rgba(255, 255, 255, 0.08);
        scrollbar-width: thin;
        scrollbar-color: #334155 transparent;
    }
    .gallery-thumbnails::-webkit-scrollbar { height: 4px; }
    .gallery-thumbnails::-webkit-scrollbar-thumb {
        background: #334155;
        border-radius: 2px;
    }

    .gallery-thumb {
        flex-shrink: 0;
        width: 62px;
        height: 62px;
        border-radius: 10px;
        overflow: hidden;
        border: 2px solid rgba(255, 255, 255, 0.12);
        cursor: pointer;
        transition: all .2s;
        background: rgba(255, 255, 255, 0.05);
        display: grid;
        place-items: center;
        position: relative;
        padding: 0;
    }
    .gallery-thumb img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
    }
    .gallery-thumb:hover {
        border-color: rgba(96, 165, 250, 0.7);
        transform: translateY(-2px);
    }
    .gallery-thumb.active {
        border-color: #2563eb;
        box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.35);
    }
    .gallery-thumb.is-primary::after {
        content: "★";
        position: absolute;
        top: 2px;
        right: 2px;
        font-size: 10px;
        color: #fbbf24;
        text-shadow: 0 0 4px rgba(0,0,0,0.8);
    }

    /* ═══════════════════════════════════════════════════════
       10. ZOOM OVERLAY (تمام‌صفحه)
       ═══════════════════════════════════════════════════════ */
    .zoom-overlay {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.97);
        z-index: 99999;
        display: none;
        align-items: center;
        justify-content: center;
        padding: 24px;
        cursor: zoom-out;
        animation: fadeIn .2s ease;
    }
    .zoom-overlay.open { display: flex; }

    .zoom-overlay img {
        max-width: 100%;
        max-height: 100%;
        width: auto;
        height: auto;
        object-fit: contain;
        display: block;
        border-radius: 8px;
        cursor: default;
        user-select: none;
        -webkit-user-drag: none;
        animation: zoomIn .25s cubic-bezier(.4, 0, .2, 1);
    }

    .zoom-close {
        position: absolute;
        top: 20px;
        right: 20px;
        width: 46px;
        height: 46px;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.15);
        color: #fff;
        border: 1px solid rgba(255, 255, 255, 0.28);
        cursor: pointer;
        display: grid;
        place-items: center;
        backdrop-filter: blur(8px);
        transition: all .2s;
        z-index: 2;
    }
    .zoom-close:hover {
        background: rgba(239, 68, 68, 0.95);
        border-color: rgba(239, 68, 68, 1);
        transform: rotate(90deg);
    }
    .zoom-close i { width: 22px; height: 22px; }

    .zoom-hint {
        position: absolute;
        bottom: 20px;
        left: 50%;
        transform: translateX(-50%);
        padding: 6px 16px;
        border-radius: 20px;
        background: rgba(255, 255, 255, 0.1);
        color: rgba(255, 255, 255, 0.75);
        font-size: 12px;
        font-weight: 600;
        backdrop-filter: blur(6px);
        border: 1px solid rgba(255, 255, 255, 0.12);
    }

    /* ═══════════════════════════════════════════════════════
       11. RESPONSIVE
       ═══════════════════════════════════════════════════════ */
    @media (max-width: 640px) {
        .product-img-icon { font-size: 42px; }
        .gallery-content { max-height: 98vh; border-radius: 16px; }
        .gallery-thumb { width: 52px; height: 52px; }
        .gallery-nav { width: 40px; height: 40px; }
        .gallery-emoji { font-size: 96px; }
        .gallery-viewer { height: 45vh; }
        .zoom-overlay { padding: 12px; }
        .zoom-close { top: 14px; right: 14px; width: 40px; height: 40px; }
        .zoom-hint { font-size: 11px; padding: 5px 12px; }
    }

    @media (max-width: 768px) {
        .gallery-nav { opacity: 1; }
        .gallery-zoom-btn { opacity: 1; transform: scale(1); width: 38px; height: 38px; }
    }

    @media (min-height: 900px) {
        .gallery-viewer { height: 62vh; }
    }

    /* ═══════════════════════════════════════════════════════
       12. PRINT
       ═══════════════════════════════════════════════════════ */
    @media print {
        .cart-drawer,
        .cart-overlay,
        .qty-modal,
        .toast-container,
        .gallery-modal,
        .zoom-overlay {
            display: none !important;
        }
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfCartJson" runat="server" Value="[]" />
    <asp:HiddenField ID="hfSelectedCustomerId" runat="server" Value="0" />
    <asp:HiddenField ID="hfSelectedCustomerType" runat="server" Value="0" />
    <asp:HiddenField ID="hfNewCustomerData" runat="server" Value="" />

    <!-- Toast Container -->
    <div id="toastContainer" class="toast-container"></div>

    <!-- ═══════════ HEADER ═══════════ -->
    <div class="pres-header -mx-4 sm:-mx-6 -mt-4 sm:-mt-6 mb-4 px-4 sm:px-6 py-4">
        <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-3 min-w-0">
                <span class="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-white/20 backdrop-blur">
                    <i class="h-5 w-5 text-white" data-lucide="store"></i>
                </span>
                <div class="min-w-0">
                    <p class="truncate text-[15px] font-extrabold text-white">کاتالوگ فروش</p>
                    <p class="mt-0.5 truncate text-[11.5px] font-medium text-white/75">
                        <asp:Literal ID="litMarketerName" runat="server" Text="بازاریاب" />
                        —
                        <asp:Literal ID="litTodayShamsi" runat="server" />
                    </p>
                </div>
            </div>

            <button type="button" onclick="openCart(); return false;"
                class="relative grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-white/15 backdrop-blur hover:bg-white/25 transition">
                <i class="h-5 w-5 text-white" data-lucide="shopping-cart"></i>
                <span id="cartBadgeHeader" class="cart-badge" style="display:none;">0</span>
            </button>
        </div>
    </div>

    <!-- ═══════════ CUSTOMER SELECTOR ═══════════ -->
    <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
        <div class="flex items-center gap-3">
            <span id="customerAvatar" class="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600">
                <i class="h-5 w-5" data-lucide="user-plus"></i>
            </span>
            <div class="min-w-0 flex-1">
                <p class="text-[11px] font-semibold text-ink-500">مشتری انتخاب‌شده</p>
                <p id="customerNameDisplay" class="mt-0.5 truncate text-[14px] font-bold text-ink-900">
                    هنوز مشتری انتخاب نشده
                </p>
                <p id="customerHint" class="mt-0.5 truncate text-[11px] text-ink-400">
                    برای نمایش قیمت‌ها، مشتری را انتخاب کنید
                </p>
            </div>
            <button type="button" onclick="openCustomerPicker(); return false;"
                class="inline-flex h-10 shrink-0 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[13px] font-bold text-white hover:bg-brand-700 transition">
                <i class="h-4 w-4" data-lucide="user-search"></i>
                <span id="customerBtnText">انتخاب مشتری</span>
            </button>
        </div>
    </section>

    <!-- ═══════════ FILTERS ═══════════ -->
    <section class="mb-4">
        <div id="categoryChips" class="category-chips mb-3"></div>

        <label class="relative block">
            <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                data-lucide="search"></i>
            <input type="text" id="productSearch" placeholder="جستجو در نام یا کد محصول..."
                oninput="filterProducts();"
                class="h-12 w-full rounded-card border border-surface-line bg-surface-card pl-12 pr-4 text-[14px] font-medium placeholder:text-ink-400 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-100/50 transition" />
        </label>
    </section>

    <!-- ═══════════ PRODUCT GRID ═══════════ -->
    <section class="pb-24">
        <div id="productGrid"
            class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">

            <asp:Repeater ID="rptProducts" runat="server">
                <ItemTemplate>
                    <div class="product-card"
                        data-product-id='<%# Eval("ProductId") %>'
                        data-product-name='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("ProductName"))) %>'
                        data-product-code='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("ProductCode"))) %>'
                        data-category-id='<%# Eval("CategoryId") %>'
                        data-category-name='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("CategoryName"))) %>'
                        data-stock='<%# Eval("AvailableStock") %>'
                        data-unit-name='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("UnitName"))) %>'
                        data-price-retail='<%# Eval("PriceRetail") %>'
                        data-price-wholesale='<%# Eval("PriceWholesale") %>'
                        data-price-agent='<%# Eval("PriceAgent") %>'
                        data-price-farmer='<%# Eval("PriceFarmer") %>'
                        data-emoji='<%# GetProductEmoji(Eval("CategoryName")) %>'
                        onclick='handleProductClick(this);'>

                        <div class='<%# GetProductImageWrapperCss(Eval("ImageUrl"), Eval("ProductId"), Eval("CategoryId")) %>'>

                            <asp:PlaceHolder ID="phRealImage" runat="server"
                                Visible='<%# HasProductImage(Eval("ImageUrl")) %>'>
                                <img src='<%# ImageHelper.GetProductImageUrl(Convert.ToString(Eval("ImageUrl")), Request.ApplicationPath) %>'
                                    alt='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("ProductName"))) %>'
                                    class="product-img-real"
                                    loading="lazy"
                                    onerror="this.style.display='none';" />
                            </asp:PlaceHolder>

                            <asp:PlaceHolder ID="phEmojiFallback" runat="server"
                                Visible='<%# !HasProductImage(Eval("ImageUrl")) %>'>
                                <span class="product-img-icon"><%# GetProductEmoji(Eval("CategoryName")) %></span>
                            </asp:PlaceHolder>

                            <span class='<%# GetStockBadgeCss(Eval("AvailableStock")) %>'>
                                <%# GetStockBadgeText(Eval("AvailableStock"), Eval("UnitName")) %>
                            </span>
                            <span class="product-cart-badge" data-cart-badge style="display:none;">0</span>

                            <!-- دکمه گالری تصاویر -->
                            <button type="button" class="gallery-btn"
                                onclick='event.stopPropagation(); openGallery(<%# Eval("ProductId") %>); return false;'
                                title="مشاهده گالری تصاویر">
                                <i class="h-3.5 w-3.5" data-lucide="images"></i>
                                <span>گالری</span>
                            </button>
                        </div>

                        <div class="p-3 flex-1 flex flex-col">
                            <p class="product-name text-[13.5px] font-bold text-ink-900">
                                <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("ProductName"))) %>
                            </p>
                            <p class="mt-1 font-mono text-[10.5px] text-ink-400" dir="ltr">
                                <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("ProductCode"))) %>
                            </p>

                            <div class="mt-auto pt-2">
                                <p class="font-mono text-[16px] font-black text-emerald-700">
                                    <span data-price-display>—</span>
                                    <span class="text-[10.5px] font-normal text-ink-400">تومان</span>
                                </p>
                                <p class="mt-0.5 text-[10.5px] text-ink-400">
                                    در هر <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("UnitName"))) %>
                                </p>
                            </div>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

        </div>

        <div id="emptyProducts" class="empty-products" style="display:none;">
            <div class="empty-products-icon">
                <i class="h-8 w-8" data-lucide="package-search"></i>
            </div>
            <p class="text-[15px] font-bold text-ink-700">محصولی یافت نشد</p>
            <p class="mt-1 text-[13px] text-ink-400">فیلتر یا جستجو را تغییر دهید</p>
        </div>
    </section>

    <!-- ═══════════ BOTTOM CART BAR ═══════════ -->
    <div id="bottomCartBar"
        class="fixed bottom-0 left-0 right-0 lg:left-[272px] z-40 border-t border-surface-line bg-white/95 backdrop-blur px-4 py-3 shadow-2xl"
        style="display:none;">
        <div class="mx-auto flex max-w-7xl items-center gap-3">
            <div class="flex-1 min-w-0">
                <p class="text-[11px] font-semibold text-ink-500">
                    <span id="cartItemCountBottom">0</span> قلم در سبد
                </p>
                <p class="mt-0.5 font-mono text-[15px] font-black text-emerald-700">
                    <span id="cartTotalBottom">۰</span>
                    <span class="text-[10px] font-normal text-ink-400">تومان</span>
                </p>
            </div>
            <button type="button" onclick="openCart(); return false;"
                class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-5 text-[13.5px] font-bold text-white hover:bg-brand-700 transition">
                <i class="h-4 w-4" data-lucide="shopping-cart"></i>
                مشاهده سبد
            </button>
        </div>
    </div>

    <!-- ═══════════ CART DRAWER ═══════════ -->
    <div id="cartOverlay" class="cart-overlay" onclick="closeCart();"></div>

    <aside id="cartDrawer" class="cart-drawer">
        <div class="flex items-center gap-3 border-b border-surface-line p-4">
            <span class="grid h-10 w-10 place-items-center rounded-xl bg-brand-50 text-brand-600">
                <i class="h-5 w-5" data-lucide="shopping-cart"></i>
            </span>
            <div class="flex-1 min-w-0">
                <p class="text-[15px] font-extrabold text-ink-900">سبد خرید</p>
                <p class="mt-0.5 text-[11.5px] text-ink-500">
                    <span id="cartItemCount">0</span> قلم
                </p>
            </div>
            <button type="button" onclick="closeCart(); return false;"
                class="grid h-10 w-10 place-items-center rounded-xl text-ink-500 hover:bg-surface-muted transition">
                <i class="h-5 w-5" data-lucide="x"></i>
            </button>
        </div>

        <div id="cartItemsContainer" class="flex-1 overflow-y-auto p-4 space-y-2"></div>

        <div id="cartEmptyState" class="flex-1 flex flex-col items-center justify-center p-6 text-center">
            <div class="empty-products-icon">
                <i class="h-8 w-8" data-lucide="shopping-bag"></i>
            </div>
            <p class="text-[15px] font-bold text-ink-700">سبد خالی است</p>
            <p class="mt-1 text-[12.5px] text-ink-400">برای شروع، محصولات را از کاتالوگ اضافه کنید</p>
        </div>

        <div id="cartFooter" class="border-t border-surface-line p-4 space-y-3" style="display:none;">
            <div class="space-y-2 rounded-base bg-surface-muted p-3">
                <div class="flex items-center justify-between text-[12.5px]">
                    <span class="text-ink-500">جمع جزء</span>
                    <span class="font-mono font-bold text-ink-800">
                        <span id="cartSubtotal">۰</span> تومان
                    </span>
                </div>
                <div class="flex items-center justify-between gap-3 text-[12.5px]">
                    <span class="text-ink-500 shrink-0">تخفیف (تومان)</span>
                    <input type="text" id="cartDiscount" placeholder="0" value="0"
                        oninput="recalcCart();"
                        class="h-9 w-32 rounded-base border border-surface-line bg-white px-3 text-left font-mono text-[13px] focus:border-brand-500 focus:outline-none" dir="ltr" />
                </div>
                <div class="flex items-center justify-between border-t border-surface-line pt-2">
                    <span class="text-[13px] font-bold text-ink-900">مبلغ کل</span>
                    <span class="font-mono text-[18px] font-black text-emerald-700">
                        <span id="cartTotal">۰</span> تومان
                    </span>
                </div>
            </div>

            <button type="button" id="btnSubmitCart" onclick="submitCart(); return false;"
                class="flex h-12 w-full items-center justify-center gap-2 rounded-base bg-emerald-600 text-[14px] font-extrabold text-white hover:bg-emerald-700 transition">
                <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                ثبت پیش‌فاکتور
            </button>
        </div>
    </aside>

    <!-- ═══════════ GALLERY MODAL ═══════════ -->
    <div id="galleryModal" class="gallery-modal" role="dialog" aria-modal="true" aria-label="گالری تصاویر محصول">
        <div class="gallery-backdrop" onclick="closeGallery();"></div>

        <div class="gallery-content">
            <button type="button" class="gallery-close" onclick="closeGallery(); return false;"
                title="بستن (Esc)">
                <i data-lucide="x"></i>
            </button>

            <div class="gallery-header">
                <p id="galleryProductName" class="text-[15px] font-extrabold text-white truncate"></p>
                <p id="galleryProductCode" class="mt-0.5 font-mono text-[11px] text-white/55" dir="ltr"></p>
            </div>

            <div class="gallery-viewer">
    <button type="button" class="gallery-nav gallery-nav-prev" id="galleryPrevBtn"
        onclick="galleryPrev(); return false;" title="قبلی">
        <i data-lucide="chevron-right"></i>
    </button>

    <img id="galleryMainImg" src="" alt="" class="gallery-main-img" style="display:none;"
        onclick="openZoom();"
        onerror="this.style.display='none'; document.getElementById('galleryEmoji').style.display='grid';" />

    <div id="galleryEmoji" class="gallery-emoji" style="display:none;">🌰</div>

    <button type="button" class="gallery-nav gallery-nav-next" id="galleryNextBtn"
        onclick="galleryNext(); return false;" title="بعدی">
        <i data-lucide="chevron-left"></i>
    </button>

    <button type="button" class="gallery-zoom-btn" onclick="openZoom(); return false;"
        title="نمایش تمام‌صفحه">
        <i data-lucide="maximize-2"></i>
    </button>

    <div class="gallery-counter">
        <span id="galleryCurrent">1</span> / <span id="galleryTotal">1</span>
    </div>
</div>

            <div id="galleryThumbs" class="gallery-thumbnails"></div>
        </div>
    </div>

    <!-- ═══════════ CUSTOMER PICKER MODAL ═══════════ -->
    <div id="customerPickerModal" class="qty-modal" style="z-index:65;">
        <div class="qty-modal-content" style="max-width: 560px; max-height: 85vh;">
            <div class="mb-4 flex items-center justify-between border-b border-surface-line pb-3">
                <h3 class="text-[15px] font-extrabold text-ink-900">انتخاب مشتری</h3>
                <button type="button" onclick="closeCustomerPicker(); return false;"
                    class="grid h-9 w-9 place-items-center rounded-lg text-ink-500 hover:bg-surface-muted transition">
                    <i class="h-5 w-5" data-lucide="x"></i>
                </button>
            </div>

            <button type="button" onclick="openNewCustomerModal(); return false;"
                class="mb-3 flex h-11 w-full items-center justify-center gap-2 rounded-base border-2 border-dashed border-brand-300 bg-brand-50 text-[13.5px] font-bold text-brand-700 hover:bg-brand-100 transition">
                <i class="h-4 w-4" data-lucide="user-plus"></i>
                مشتری در لیست نیست؟ افزودن مشتری جدید
            </button>

            <label class="relative mb-3 block">
                <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                    data-lucide="search"></i>
                <input type="text" id="customerSearchInput" placeholder="جستجو در نام، کد، شهر یا موبایل..."
                    oninput="filterCustomers();"
                    class="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-500 focus:outline-none" />
            </label>

            <div id="customersList" class="max-h-[55vh] overflow-y-auto -mx-1 space-y-1"></div>
        </div>
    </div>

    <!-- ═══════════ NEW CUSTOMER MODAL ═══════════ -->
    <div id="newCustomerModal" class="qty-modal" style="z-index:68;">
        <div class="qty-modal-content" style="max-width: 520px;">
            <div class="mb-4 flex items-center justify-between border-b border-surface-line pb-3">
                <div class="flex items-center gap-2">
                    <span class="grid h-9 w-9 place-items-center rounded-lg bg-emerald-100 text-emerald-700">
                        <i class="h-4 w-4" data-lucide="user-plus"></i>
                    </span>
                    <div>
                        <h3 class="text-[15px] font-extrabold text-ink-900">افزودن مشتری جدید</h3>
                        <p class="mt-0.5 text-[11px] text-ink-500">شما به‌عنوان بازاریاب مسئول ثبت می‌شوید</p>
                    </div>
                </div>
                <button type="button" onclick="closeNewCustomerModal(); return false;"
                    class="grid h-9 w-9 place-items-center rounded-lg text-ink-500 hover:bg-surface-muted transition">
                    <i class="h-5 w-5" data-lucide="x"></i>
                </button>
            </div>

            <div class="space-y-3">
                <label class="block">
                    <span class="text-[12.5px] font-semibold text-ink-700">
                        نام کامل <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtNewCustomerName" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-500 focus:outline-none"
                        placeholder="مثال: آقای محمد رضایی" />
                </label>

                <div class="grid grid-cols-2 gap-3">
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">
                            موبایل <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtNewCustomerMobile" runat="server" MaxLength="15" dir="ltr"
                            CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-500 focus:outline-none"
                            placeholder="09121234567" />
                    </label>

                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">شهر</span>
                        <asp:TextBox ID="txtNewCustomerCity" runat="server" MaxLength="100"
                            CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-500 focus:outline-none"
                            placeholder="مثال: تهران" />
                    </label>
                </div>

                <div class="grid grid-cols-2 gap-3">
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">کد مشتری</span>
                        <asp:TextBox ID="txtNewCustomerCode" runat="server" MaxLength="50" dir="ltr"
                            CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-500 focus:outline-none"
                            placeholder="خودکار" />
                        <p class="mt-1 text-[10.5px] text-ink-400">اگر خالی بگذاری، خودکار تولید می‌شود</p>
                    </label>

                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">کد اقتصادی</span>
                        <asp:TextBox ID="txtNewCustomerEconomicCode" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-500 focus:outline-none"
                            placeholder="اختیاری" />
                    </label>
                </div>

                <div>
                    <p class="mb-2 text-[12.5px] font-semibold text-ink-700">
                        نوع مشتری <span class="text-danger-500">*</span>
                    </p>
                    <div id="customerTypeChips" class="grid grid-cols-4 gap-2">
                        <button type="button" class="cust-type-chip rounded-base border-2 border-surface-line bg-white py-2.5 text-[12px] font-bold text-ink-600 transition"
                            data-type="1">تک‌فروشی</button>
                        <button type="button" class="cust-type-chip active rounded-base border-2 border-brand-500 bg-brand-50 py-2.5 text-[12px] font-bold text-brand-700 transition"
                            data-type="2">عمده</button>
                        <button type="button" class="cust-type-chip rounded-base border-2 border-surface-line bg-white py-2.5 text-[12px] font-bold text-ink-600 transition"
                            data-type="3">نمایندگی</button>
                        <button type="button" class="cust-type-chip rounded-base border-2 border-surface-line bg-white py-2.5 text-[12px] font-bold text-ink-600 transition"
                            data-type="4">کشاورز</button>
                    </div>
                    <asp:HiddenField ID="hfNewCustomerType" runat="server" Value="2" />
                </div>

                <label class="block">
                    <span class="text-[12.5px] font-semibold text-ink-700">آدرس</span>
                    <asp:TextBox ID="txtNewCustomerAddress" runat="server" TextMode="MultiLine" Rows="2"
                        MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[13px] focus:border-brand-500 focus:outline-none"
                        placeholder="اختیاری" />
                </label>
            </div>

            <div class="mt-5 flex gap-2">
                <button type="button" onclick="closeNewCustomerModal(); return false;"
                    class="flex h-12 flex-1 items-center justify-center rounded-base border border-surface-line bg-white text-[14px] font-bold text-ink-700 hover:bg-surface-muted transition">
                    انصراف
                </button>
                <asp:Button ID="btnSaveNewCustomer" runat="server"
                    Text="ثبت و انتخاب"
                    OnClick="btnSaveNewCustomer_Click"
                    CssClass="flex h-12 flex-[2] items-center justify-center gap-2 rounded-base bg-emerald-600 text-[14px] font-extrabold text-white hover:bg-emerald-700 transition" />
            </div>
        </div>
    </div>

    <!-- ═══════════ QTY MODAL ═══════════ -->
    <div id="qtyModal" class="qty-modal">
        <div class="qty-modal-content">
            <div class="mb-4 flex items-start gap-3 border-b border-surface-line pb-3">
                <span id="qtyProductEmoji" class="grid h-12 w-12 shrink-0 place-items-center rounded-xl bg-amber-100 text-2xl">🌰</span>
                <div class="min-w-0 flex-1">
                    <p id="qtyProductName" class="text-[15px] font-extrabold text-ink-900 leading-tight">محصول</p>
                    <p id="qtyProductCode" class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr"></p>
                </div>
                <button type="button" onclick="closeQtyModal(); return false;"
                    class="grid h-9 w-9 place-items-center rounded-lg text-ink-500 hover:bg-surface-muted transition shrink-0">
                    <i class="h-5 w-5" data-lucide="x"></i>
                </button>
            </div>

            <div class="mb-4 grid grid-cols-2 gap-3">
                <div class="rounded-base bg-emerald-50 border border-emerald-200 p-3">
                    <p class="text-[10.5px] font-semibold text-emerald-700">قیمت واحد</p>
                    <p class="mt-1 font-mono text-[15px] font-black text-emerald-800">
                        <span id="qtyUnitPrice">۰</span>
                        <span class="text-[10px] font-normal">تومان</span>
                    </p>
                </div>
                <div class="rounded-base bg-surface-muted p-3">
                    <p class="text-[10.5px] font-semibold text-ink-500">موجودی</p>
                    <p id="qtyAvailable" class="mt-1 font-mono text-[15px] font-black text-ink-800">۰</p>
                </div>
            </div>

            <div class="mb-4">
                <p class="mb-2 text-center text-[12.5px] font-semibold text-ink-600">
                    تعداد (<span id="qtyUnitName">کیلوگرم</span>)
                </p>
                <div class="flex items-center justify-center gap-3">
                    <button type="button" onclick="changeQty(-1); return false;" class="qty-btn">
                        <i class="h-5 w-5" data-lucide="minus"></i>
                    </button>
                    <input type="number" id="qtyInput" value="1" min="1" step="any"
                        oninput="onQtyInputChange();" class="qty-input" dir="ltr" />
                    <button type="button" onclick="changeQty(1); return false;" class="qty-btn">
                        <i class="h-5 w-5" data-lucide="plus"></i>
                    </button>
                </div>
                <div class="mt-3 flex justify-center gap-2">
                    <button type="button" onclick="setQty(5); return false;" class="rounded-lg border border-surface-line bg-white px-3 py-1.5 text-[11.5px] font-semibold text-ink-700 hover:bg-surface-muted">۵</button>
                    <button type="button" onclick="setQty(10); return false;" class="rounded-lg border border-surface-line bg-white px-3 py-1.5 text-[11.5px] font-semibold text-ink-700 hover:bg-surface-muted">۱۰</button>
                    <button type="button" onclick="setQty(20); return false;" class="rounded-lg border border-surface-line bg-white px-3 py-1.5 text-[11.5px] font-semibold text-ink-700 hover:bg-surface-muted">۲۰</button>
                    <button type="button" onclick="setQty(50); return false;" class="rounded-lg border border-surface-line bg-white px-3 py-1.5 text-[11.5px] font-semibold text-ink-700 hover:bg-surface-muted">۵۰</button>
                </div>
            </div>

            <div class="mb-4 rounded-base border-2 border-emerald-300 bg-emerald-50 p-3 text-center">
                <p class="text-[11.5px] font-semibold text-emerald-700">جمع این قلم</p>
                <p class="mt-1 font-mono text-[22px] font-black text-emerald-800">
                    <span id="qtyTotalPrice">۰</span>
                    <span class="text-[11px] font-normal">تومان</span>
                </p>
            </div>

            <div class="flex gap-2">
                <button type="button" onclick="closeQtyModal(); return false;"
                    class="flex h-12 flex-1 items-center justify-center rounded-base border border-surface-line bg-white text-[14px] font-bold text-ink-700 hover:bg-surface-muted transition">
                    انصراف
                </button>
                <button type="button" onclick="addToCartFromModal(); return false;"
                    class="flex h-12 flex-[2] items-center justify-center gap-2 rounded-base bg-brand-600 text-[14px] font-extrabold text-white hover:bg-brand-700 transition">
                    <i class="h-5 w-5" data-lucide="plus-circle"></i>
                    افزودن به سبد
                </button>
            </div>
        </div>
    </div>

    <!-- ═══════════ SUCCESS MODAL ═══════════ -->
    <div id="successModal" class="qty-modal" style="z-index:80;">
        <div class="qty-modal-content" style="max-width: 420px; text-align:center;">
            <div class="mx-auto mb-4 grid h-20 w-20 place-items-center rounded-full bg-emerald-100">
                <i class="h-10 w-10 text-emerald-600" data-lucide="check-circle-2"></i>
            </div>
            <p class="text-[18px] font-extrabold text-ink-900">پیش‌فاکتور ثبت شد!</p>
            <p class="mt-2 text-[13px] text-ink-500 leading-relaxed">
                شماره پیش‌فاکتور:
                <strong id="successProformaNumber" class="font-mono text-[14px] text-ink-900"></strong>
            </p>

            <div class="mt-5 space-y-2">
                <button type="button" onclick="printProforma(); return false;"
                    class="flex h-11 w-full items-center justify-center gap-2 rounded-base bg-brand-600 text-[13.5px] font-bold text-white hover:bg-brand-700 transition">
                    <i class="h-4 w-4" data-lucide="printer"></i>
                    چاپ پیش‌فاکتور
                </button>
                <a id="lnkViewProforma" href="#"
                    class="flex h-11 w-full items-center justify-center gap-2 rounded-base border border-surface-line bg-white text-[13.5px] font-bold text-ink-700 hover:bg-surface-muted transition">
                    <i class="h-4 w-4" data-lucide="external-link"></i>
                    مشاهده در سامانه
                </a>
                <button type="button" onclick="resetForNextCustomer(); return false;"
                    class="flex h-11 w-full items-center justify-center gap-2 rounded-base border border-emerald-300 bg-emerald-50 text-[13.5px] font-bold text-emerald-700 hover:bg-emerald-100 transition">
                    <i class="h-4 w-4" data-lucide="user-plus"></i>
                    سفارش جدید برای مشتری بعدی
                </button>
            </div>
        </div>
    </div>

    <!-- Hidden JSONs -->
    <asp:Literal ID="litCustomersJson" runat="server" />
    <asp:Literal ID="litProductImagesJson" runat="server" />
    <!-- ═══════════ ZOOM OVERLAY ═══════════ -->
<div id="zoomOverlay" class="zoom-overlay" onclick="closeZoom();">
    <button type="button" class="zoom-close" onclick="event.stopPropagation(); closeZoom(); return false;"
        title="بستن (Esc)">
        <i data-lucide="x"></i>
    </button>
    <img id="zoomImg" src="" alt=""
        onclick="event.stopPropagation();"
        onerror="closeZoom();" />
    <div class="zoom-hint">برای بستن، کلیک کن یا Esc بزن</div>
</div>
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════
    var cart = [];
    var selectedCustomer = null;
    var currentQtyProduct = null;
    var currentCategoryFilter = 'all';
    var allCustomers = [];
    var lastCreatedProformaId = 0;

    // Gallery state
    var allProductImages = {};   // { productId: [ {imageId, url, isPrimary}, ... ] }
    var currentGalleryProduct = null;
    var currentGalleryIndex = 0;

    // ═══════════════════════════════════════════════════════
    // INIT
    // ═══════════════════════════════════════════════════════
    document.addEventListener('DOMContentLoaded', function () {
        loadCustomersFromJson();
        loadProductImagesFromJson();
        buildCategoryChips();
        updateAllProductPrices();
        renderCart();
        bindCustomerTypeChips();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });

    window.addEventListener('load', function () {
        loadCustomersFromJson();
        loadProductImagesFromJson();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });

    function loadCustomersFromJson() {
        try {
            var jsonEl = document.getElementById('<%= litCustomersJson.ClientID %>');
            if (jsonEl && jsonEl.innerHTML) {
                var raw = jsonEl.innerHTML.trim();
                if (raw.length > 0) {
                    allCustomers = JSON.parse(raw);
                }
            }
        } catch (e) { allCustomers = []; }
    }

    function loadProductImagesFromJson() {
        try {
            var jsonEl = document.getElementById('<%= litProductImagesJson.ClientID %>');
            if (jsonEl && jsonEl.innerHTML) {
                var raw = jsonEl.innerHTML.trim();
                if (raw.length > 0) {
                    allProductImages = JSON.parse(raw) || {};
                }
            }
        } catch (e) { allProductImages = {}; }
    }

    // ═══════════════════════════════════════════════════════
    // GALLERY
    // ═══════════════════════════════════════════════════════
    function openGallery(productId) {
        productId = parseInt(productId, 10);
        if (!productId) return;

        var card = document.querySelector('.product-card[data-product-id="' + productId + '"]');
        if (!card) return;

        var name = card.getAttribute('data-product-name') || 'محصول';
        var code = card.getAttribute('data-product-code') || '';
        var emoji = card.getAttribute('data-emoji') || '🌰';

        var images = allProductImages[String(productId)] || [];

        currentGalleryProduct = {
            id: productId,
            name: name,
            code: code,
            emoji: emoji,
            images: images
        };
        currentGalleryIndex = 0;

        document.getElementById('galleryProductName').textContent = name;
        document.getElementById('galleryProductCode').textContent = code;
        document.getElementById('galleryEmoji').textContent = emoji;

        renderGallery();

        document.getElementById('galleryModal').classList.add('open');
        document.body.style.overflow = 'hidden';

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function closeGallery() {
        closeZoom(); // بستن زوم در صورت باز بودن
        document.getElementById('galleryModal').classList.remove('open');
        var anyOpen = document.querySelector('.cart-drawer.open, .qty-modal.open');
        if (!anyOpen) document.body.style.overflow = '';
        currentGalleryProduct = null;
        currentGalleryIndex = 0;
    }

    function renderGallery() {
        if (!currentGalleryProduct) return;

        var images = currentGalleryProduct.images || [];
        var mainImg = document.getElementById('galleryMainImg');
        var emojiEl = document.getElementById('galleryEmoji');
        var thumbsBox = document.getElementById('galleryThumbs');
        var counter = document.getElementById('galleryCurrent');
        var total = document.getElementById('galleryTotal');
        var prevBtn = document.getElementById('galleryPrevBtn');
        var nextBtn = document.getElementById('galleryNextBtn');
        var zoomBtn = document.querySelector('.gallery-zoom-btn');

        // حالت بدون عکس
        if (images.length === 0) {
            mainImg.style.display = 'none';
            mainImg.removeAttribute('src');
            emojiEl.style.display = 'grid';
            thumbsBox.innerHTML = '';
            counter.textContent = '0';
            total.textContent = '0';
            prevBtn.disabled = true;
            nextBtn.disabled = true;
            if (zoomBtn) zoomBtn.style.display = 'none';
            return;
        }

        // اگر فقط یک عکس داریم، فلش‌ها را مخفی کن
        if (images.length === 1) {
            prevBtn.style.display = 'none';
            nextBtn.style.display = 'none';
        } else {
            prevBtn.style.display = 'grid';
            nextBtn.style.display = 'grid';
        }

        if (zoomBtn) zoomBtn.style.display = 'grid';

        // عکس فعلی
        var img = images[currentGalleryIndex];
        mainImg.style.display = 'block';
        mainImg.src = img.url;
        mainImg.alt = currentGalleryProduct.name;
        emojiEl.style.display = 'none';

        counter.textContent = (currentGalleryIndex + 1);
        total.textContent = images.length;

        prevBtn.disabled = false;
        nextBtn.disabled = false;

        // Thumbnails
        thumbsBox.innerHTML = '';
        for (var i = 0; i < images.length; i++) {
            (function (idx) {
                var thumb = document.createElement('button');
                thumb.type = 'button';
                thumb.className = 'gallery-thumb'
                    + (idx === currentGalleryIndex ? ' active' : '')
                    + (images[idx].isPrimary ? ' is-primary' : '');
                thumb.onclick = function () { gotoGalleryImage(idx); };
                thumb.title = 'تصویر ' + (idx + 1);

                var tImg = document.createElement('img');
                tImg.src = images[idx].url;
                tImg.alt = '';
                tImg.loading = 'lazy';
                tImg.onerror = function () {
                    tImg.style.display = 'none';
                    thumb.textContent = '🌰';
                };
                thumb.appendChild(tImg);
                thumbsBox.appendChild(thumb);
            })(i);
        }

        // اسکرول thumbnail فعال به دید
        setTimeout(function () {
            var active = thumbsBox.querySelector('.gallery-thumb.active');
            if (active && active.scrollIntoView) {
                active.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
            }
        }, 50);
    }

    function gotoGalleryImage(idx) {
        if (!currentGalleryProduct) return;
        var images = currentGalleryProduct.images || [];
        if (idx < 0 || idx >= images.length) return;
        currentGalleryIndex = idx;
        renderGallery();
    }

    function galleryPrev() {
        if (!currentGalleryProduct) return;
        var images = currentGalleryProduct.images || [];
        if (images.length === 0) return;
        currentGalleryIndex = (currentGalleryIndex - 1 + images.length) % images.length;
        renderGallery();
    }

    function galleryNext() {
        if (!currentGalleryProduct) return;
        var images = currentGalleryProduct.images || [];
        if (images.length === 0) return;
        currentGalleryIndex = (currentGalleryIndex + 1) % images.length;
        renderGallery();
    }

    // ═══════════════════════════════════════════════════════
    // ZOOM FULLSCREEN
    // ═══════════════════════════════════════════════════════
    function openZoom() {
        var mainImg = document.getElementById('galleryMainImg');
        var overlay = document.getElementById('zoomOverlay');
        var zoomImg = document.getElementById('zoomImg');

        if (!mainImg || !overlay || !zoomImg) return;

        // اگر عکس اصلی مخفی است، زوم نکن
        if (mainImg.style.display === 'none') return;
        if (!mainImg.src) return;

        zoomImg.src = mainImg.src;
        zoomImg.alt = mainImg.alt || '';
        overlay.classList.add('open');

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function closeZoom() {
        var overlay = document.getElementById('zoomOverlay');
        var zoomImg = document.getElementById('zoomImg');
        if (overlay) overlay.classList.remove('open');
        if (zoomImg) zoomImg.removeAttribute('src');
    }

    // ═══════════════════════════════════════════════════════
    // CUSTOMER TYPE CHIPS
    // ═══════════════════════════════════════════════════════
    function bindCustomerTypeChips() {
        var chips = document.querySelectorAll('.cust-type-chip');
        for (var i = 0; i < chips.length; i++) {
            chips[i].addEventListener('click', function () {
                var type = this.getAttribute('data-type');
                document.getElementById('<%= hfNewCustomerType.ClientID %>').value = type;

                var all = document.querySelectorAll('.cust-type-chip');
                for (var j = 0; j < all.length; j++) {
                    all[j].classList.remove('active', 'border-brand-500', 'bg-brand-50', 'text-brand-700');
                    all[j].classList.add('border-surface-line', 'bg-white', 'text-ink-600');
                }
                this.classList.add('active', 'border-brand-500', 'bg-brand-50', 'text-brand-700');
                this.classList.remove('border-surface-line', 'bg-white', 'text-ink-600');
            });
        }
    }

    // ═══════════════════════════════════════════════════════
    // CATEGORY CHIPS
    // ═══════════════════════════════════════════════════════
    function buildCategoryChips() {
        var cards = document.querySelectorAll('.product-card');
        var chips = document.getElementById('categoryChips');
        if (!chips) return;

        chips.innerHTML = '';

        var allChip = document.createElement('div');
        allChip.className = 'category-chip active';
        allChip.setAttribute('data-category', 'all');
        allChip.textContent = 'همه';
        allChip.onclick = function () { selectCategory('all'); };
        chips.appendChild(allChip);

        var catMap = {};
        for (var i = 0; i < cards.length; i++) {
            var id = cards[i].getAttribute('data-category-id');
            var name = cards[i].getAttribute('data-category-name');
            if (id && name && id !== '0') {
                if (!catMap[id]) catMap[id] = name;
            }
        }

        var keys = Object.keys(catMap);
        for (var k = 0; k < keys.length; k++) {
            var chip = document.createElement('div');
            chip.className = 'category-chip';
            chip.setAttribute('data-category', keys[k]);
            chip.textContent = catMap[keys[k]];
            chip.onclick = (function (catId) {
                return function () { selectCategory(catId); };
            })(keys[k]);
            chips.appendChild(chip);
        }
    }

    function selectCategory(catId) {
        currentCategoryFilter = catId;
        var chips = document.querySelectorAll('.category-chip');
        for (var i = 0; i < chips.length; i++) {
            if (chips[i].getAttribute('data-category') === catId) {
                chips[i].classList.add('active');
            } else {
                chips[i].classList.remove('active');
            }
        }
        filterProducts();
    }

    function filterProducts() {
        var search = (document.getElementById('productSearch').value || '').trim().toLowerCase();
        var cards = document.querySelectorAll('.product-card');
        var visibleCount = 0;

        for (var i = 0; i < cards.length; i++) {
            var card = cards[i];
            var name = (card.getAttribute('data-product-name') || '').toLowerCase();
            var code = (card.getAttribute('data-product-code') || '').toLowerCase();
            var catId = card.getAttribute('data-category-id') || '';

            var matchSearch = (search === '' || name.indexOf(search) > -1 || code.indexOf(search) > -1);
            var matchCat = (currentCategoryFilter === 'all' || catId === currentCategoryFilter);

            if (matchSearch && matchCat) {
                card.style.display = '';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        }

        document.getElementById('emptyProducts').style.display = visibleCount === 0 ? 'block' : 'none';
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    // ═══════════════════════════════════════════════════════
    // PRICE UPDATE
    // ═══════════════════════════════════════════════════════
    function updateAllProductPrices() {
        var cards = document.querySelectorAll('.product-card');
        var priceKey = getPriceKeyForCustomer();

        for (var i = 0; i < cards.length; i++) {
            var price = cards[i].getAttribute('data-price-' + priceKey)
                || cards[i].getAttribute('data-price-retail') || '0';
            var priceEl = cards[i].querySelector('[data-price-display]');
            if (priceEl) priceEl.textContent = formatNumber(price);
        }
    }

    function getPriceKeyForCustomer() {
        if (!selectedCustomer) return 'retail';
        var type = parseInt(selectedCustomer.type, 10);
        if (type === 1) return 'retail';
        if (type === 2) return 'wholesale';
        if (type === 3) return 'agent';
        if (type === 4) return 'farmer';
        return 'retail';
    }

    // ═══════════════════════════════════════════════════════
    // CUSTOMER PICKER
    // ═══════════════════════════════════════════════════════
    function openCustomerPicker() {
        renderCustomers('');
        document.getElementById('customerPickerModal').classList.add('open');
        document.body.style.overflow = 'hidden';
        setTimeout(function () {
            document.getElementById('customerSearchInput').focus();
        }, 200);
    }

    function closeCustomerPicker() {
        document.getElementById('customerPickerModal').classList.remove('open');
        document.body.style.overflow = '';
    }

    function filterCustomers() {
        var term = document.getElementById('customerSearchInput').value || '';
        renderCustomers(term);
    }

    function renderCustomers(term) {
        var list = document.getElementById('customersList');
        if (!list) return;

        term = (term || '').trim().toLowerCase();
        list.innerHTML = '';

        var filtered = allCustomers.filter(function (c) {
            if (term === '') return true;
            return (c.name || '').toLowerCase().indexOf(term) > -1
                || (c.code || '').toLowerCase().indexOf(term) > -1
                || (c.city || '').toLowerCase().indexOf(term) > -1
                || (c.mobile || '').toLowerCase().indexOf(term) > -1;
        });

        if (filtered.length === 0) {
            list.innerHTML = '<div style="padding:40px 20px;text-align:center;color:#94a3b8;font-size:13px;">مشتری‌ای یافت نشد</div>';
            return;
        }

        filtered.slice(0, 100).forEach(function (c) {
            var item = document.createElement('div');
            item.className = 'flex items-center gap-3 rounded-lg border border-transparent p-3 hover:border-brand-200 hover:bg-brand-50/40 cursor-pointer transition';
            item.onclick = function () { chooseCustomer(c); };

            item.innerHTML =
                '<span class="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-brand-100 text-brand-700 font-bold text-[13px]">' +
                    escapeHtml((c.name || '?').substring(0, 1)) +
                '</span>' +
                '<div class="min-w-0 flex-1">' +
                    '<p class="truncate text-[13.5px] font-bold text-ink-900">' + escapeHtml(c.name) + '</p>' +
                    '<p class="mt-0.5 truncate text-[11px] text-ink-500">' +
                        '<span class="font-mono" dir="ltr">' + escapeHtml(c.code) + '</span>' +
                        ' · ' + escapeHtml(c.city || '') +
                        (c.mobile ? ' · ' + escapeHtml(c.mobile) : '') +
                    '</p>' +
                '</div>' +
                '<span class="badge ' + getCustomerTypeColor(c.type) + ' text-[10.5px]">' + getCustomerTypeLabel(c.type) + '</span>';
            list.appendChild(item);
        });

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function chooseCustomer(c) {
        selectedCustomer = c;
        document.getElementById('<%= hfSelectedCustomerId.ClientID %>').value = c.id;
        document.getElementById('<%= hfSelectedCustomerType.ClientID %>').value = c.type;
        document.getElementById('customerNameDisplay').textContent = c.name;
        document.getElementById('customerHint').textContent =
            getCustomerTypeLabel(c.type) + (c.city ? ' · ' + c.city : '');
        document.getElementById('customerBtnText').textContent = 'تغییر';
        document.getElementById('customerAvatar').className =
            'grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-emerald-50 text-emerald-600';
        document.getElementById('customerAvatar').innerHTML = '<i class="h-5 w-5" data-lucide="user-check"></i>';

        if (typeof lucide !== 'undefined') lucide.createIcons();

        closeCustomerPicker();
        updateAllProductPrices();
    }

    // ═══════════════════════════════════════════════════════
    // NEW CUSTOMER MODAL
    // ═══════════════════════════════════════════════════════
    function openNewCustomerModal() {
        closeCustomerPicker();
        clearNewCustomerForm();
        document.getElementById('newCustomerModal').classList.add('open');
        document.body.style.overflow = 'hidden';
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function closeNewCustomerModal() {
        document.getElementById('newCustomerModal').classList.remove('open');
        document.body.style.overflow = '';
    }

    function clearNewCustomerForm() {
        document.getElementById('<%= txtNewCustomerName.ClientID %>').value = '';
        document.getElementById('<%= txtNewCustomerMobile.ClientID %>').value = '';
        document.getElementById('<%= txtNewCustomerCity.ClientID %>').value = '';
        document.getElementById('<%= txtNewCustomerCode.ClientID %>').value = '';
        document.getElementById('<%= txtNewCustomerEconomicCode.ClientID %>').value = '';
        document.getElementById('<%= txtNewCustomerAddress.ClientID %>').value = '';
        document.getElementById('<%= hfNewCustomerType.ClientID %>').value = '2';

        var chips = document.querySelectorAll('.cust-type-chip');
        for (var i = 0; i < chips.length; i++) {
            var t = chips[i].getAttribute('data-type');
            if (t === '2') {
                chips[i].classList.add('active', 'border-brand-500', 'bg-brand-50', 'text-brand-700');
                chips[i].classList.remove('border-surface-line', 'bg-white', 'text-ink-600');
            } else {
                chips[i].classList.remove('active', 'border-brand-500', 'bg-brand-50', 'text-brand-700');
                chips[i].classList.add('border-surface-line', 'bg-white', 'text-ink-600');
            }
        }
    }

    function handleNewCustomerSaved() {
        try {
            var hf = document.getElementById('<%= hfNewCustomerData.ClientID %>');
            if (!hf || !hf.value) return;

            var newCust = JSON.parse(hf.value);
            if (!newCust || !newCust.id) return;

            allCustomers.push(newCust);
            hf.value = '';

            chooseCustomer(newCust);
            closeNewCustomerModal();
            closeCustomerPicker();

            showToast('مشتری جدید ثبت شد: ' + newCust.name, 'success');
        } catch (e) { }
    }

    // ═══════════════════════════════════════════════════════
    // PRODUCT CLICK (افزودن به سبد)
    // ═══════════════════════════════════════════════════════
    function handleProductClick(el) {
        if (!selectedCustomer) {
            showToast('ابتدا مشتری را انتخاب کنید', 'info');
            openCustomerPicker();
            return;
        }

        var stock = parseFloat(el.getAttribute('data-stock') || '0');
        if (stock <= 0) {
            showToast('موجودی این محصول تمام شده است', 'error');
            return;
        }

        var priceKey = getPriceKeyForCustomer();
        var price = parseFloat(el.getAttribute('data-price-' + priceKey)
            || el.getAttribute('data-price-retail') || '0');

        currentQtyProduct = {
            id: parseInt(el.getAttribute('data-product-id'), 10),
            name: el.getAttribute('data-product-name'),
            code: el.getAttribute('data-product-code'),
            price: price,
            stock: stock,
            unit: el.getAttribute('data-unit-name') || 'عدد',
            emoji: el.getAttribute('data-emoji') || '🌰'
        };

        var existing = findCartItem(currentQtyProduct.id);
        var initialQty = existing ? existing.qty : 1;

        document.getElementById('qtyProductName').textContent = currentQtyProduct.name;
        document.getElementById('qtyProductCode').textContent = currentQtyProduct.code;
        document.getElementById('qtyProductEmoji').textContent = currentQtyProduct.emoji;
        document.getElementById('qtyUnitPrice').textContent = formatNumber(price);
        document.getElementById('qtyAvailable').textContent = formatNumber(stock) + ' ' + currentQtyProduct.unit;
        document.getElementById('qtyUnitName').textContent = currentQtyProduct.unit;
        document.getElementById('qtyInput').value = initialQty;
        document.getElementById('qtyInput').max = stock;

        recalcQtyModal();

        document.getElementById('qtyModal').classList.add('open');
        document.body.style.overflow = 'hidden';

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function closeQtyModal() {
        document.getElementById('qtyModal').classList.remove('open');
        document.body.style.overflow = '';
        currentQtyProduct = null;
    }

    function changeQty(delta) {
        var input = document.getElementById('qtyInput');
        var val = parseFloat(input.value || '1');
        if (isNaN(val)) val = 1;
        val += delta;
        if (val < 1) val = 1;
        if (currentQtyProduct && val > currentQtyProduct.stock) val = currentQtyProduct.stock;
        input.value = val;
        recalcQtyModal();
    }

    function setQty(n) {
        var input = document.getElementById('qtyInput');
        if (currentQtyProduct && n > currentQtyProduct.stock) n = currentQtyProduct.stock;
        input.value = n;
        recalcQtyModal();
    }

    function onQtyInputChange() {
        var input = document.getElementById('qtyInput');
        var val = parseFloat(input.value || '1');
        if (isNaN(val) || val < 1) val = 1;
        if (currentQtyProduct && val > currentQtyProduct.stock) val = currentQtyProduct.stock;
        input.value = val;
        recalcQtyModal();
    }

    function recalcQtyModal() {
        if (!currentQtyProduct) return;
        var qty = parseFloat(document.getElementById('qtyInput').value || '1');
        if (isNaN(qty)) qty = 1;
        var total = qty * currentQtyProduct.price;
        document.getElementById('qtyTotalPrice').textContent = formatNumber(total);
    }

    function addToCartFromModal() {
        if (!currentQtyProduct) return;

        var qty = parseFloat(document.getElementById('qtyInput').value || '1');
        if (isNaN(qty) || qty <= 0) { showToast('تعداد معتبر وارد کنید', 'error'); return; }
        if (qty > currentQtyProduct.stock) { showToast('تعداد بیشتر از موجودی است', 'error'); return; }

        var existing = findCartItem(currentQtyProduct.id);
        if (existing) {
            existing.qty = qty;
        } else {
            cart.push({
                productId: currentQtyProduct.id,
                name: currentQtyProduct.name,
                code: currentQtyProduct.code,
                price: currentQtyProduct.price,
                qty: qty,
                unit: currentQtyProduct.unit,
                emoji: currentQtyProduct.emoji
            });
        }

        closeQtyModal();
        updateProductBadges();
        renderCart();
        syncCartToHiddenField();
        showToast('به سبد اضافه شد', 'success');
    }

    // ═══════════════════════════════════════════════════════
    // CART
    // ═══════════════════════════════════════════════════════
    function findCartItem(productId) {
        for (var i = 0; i < cart.length; i++) {
            if (cart[i].productId === productId) return cart[i];
        }
        return null;
    }

    function removeFromCart(productId) {
        cart = cart.filter(function (it) { return it.productId !== productId; });
        updateProductBadges();
        renderCart();
        syncCartToHiddenField();
    }

    function changeCartQty(productId, delta) {
        var item = findCartItem(productId);
        if (!item) return;
        item.qty += delta;
        if (item.qty < 1) item.qty = 1;
        updateProductBadges();
        renderCart();
        syncCartToHiddenField();
    }

    function setCartQty(productId, qty) {
        var item = findCartItem(productId);
        if (!item) return;
        qty = parseFloat(qty || '1');
        if (isNaN(qty) || qty < 1) qty = 1;
        item.qty = qty;
        updateProductBadges();
        renderCart();
        syncCartToHiddenField();
    }

    function renderCart() {
        var container = document.getElementById('cartItemsContainer');
        var emptyState = document.getElementById('cartEmptyState');
        var footer = document.getElementById('cartFooter');
        var bottomBar = document.getElementById('bottomCartBar');

        if (cart.length === 0) {
            container.innerHTML = '';
            emptyState.style.display = 'flex';
            footer.style.display = 'none';
            bottomBar.style.display = 'none';
        } else {
            emptyState.style.display = 'none';
            footer.style.display = 'block';
            bottomBar.style.display = 'block';

            container.innerHTML = '';
            cart.forEach(function (item) {
                var row = document.createElement('div');
                row.className = 'rounded-base border border-surface-line bg-white p-3';
                row.innerHTML =
                    '<div class="flex items-start gap-2 mb-2">' +
                        '<span class="text-2xl shrink-0">' + escapeHtml(item.emoji || '🌰') + '</span>' +
                        '<div class="flex-1 min-w-0">' +
                            '<p class="truncate text-[13px] font-bold text-ink-900">' + escapeHtml(item.name) + '</p>' +
                            '<p class="mt-0.5 font-mono text-[10.5px] text-ink-400" dir="ltr">' + escapeHtml(item.code) + '</p>' +
                        '</div>' +
                        '<button type="button" onclick="removeFromCart(' + item.productId + '); return false;" ' +
                            'class="grid h-7 w-7 shrink-0 place-items-center rounded-lg text-danger-500 hover:bg-danger-50 transition">' +
                            '<i class="h-3.5 w-3.5" data-lucide="trash-2"></i>' +
                        '</button>' +
                    '</div>' +
                    '<div class="flex items-center justify-between gap-2">' +
                        '<div class="flex items-center gap-2">' +
                            '<button type="button" onclick="changeCartQty(' + item.productId + ', -1); return false;" ' +
                                'class="grid h-8 w-8 place-items-center rounded-lg bg-surface-muted text-ink-700 hover:bg-ink-200 transition">' +
                                '<i class="h-3.5 w-3.5" data-lucide="minus"></i>' +
                            '</button>' +
                            '<input type="number" value="' + item.qty + '" min="1" step="any" ' +
                                'onchange="setCartQty(' + item.productId + ', this.value);" ' +
                                'class="h-8 w-16 rounded-lg border border-surface-line bg-white text-center font-mono text-[13px] font-bold focus:border-brand-500 focus:outline-none" dir="ltr" />' +
                            '<button type="button" onclick="changeCartQty(' + item.productId + ', 1); return false;" ' +
                                'class="grid h-8 w-8 place-items-center rounded-lg bg-surface-muted text-ink-700 hover:bg-ink-200 transition">' +
                                '<i class="h-3.5 w-3.5" data-lucide="plus"></i>' +
                            '</button>' +
                            '<span class="text-[11px] text-ink-400">' + escapeHtml(item.unit) + '</span>' +
                        '</div>' +
                        '<p class="font-mono text-[13px] font-black text-emerald-700">' +
                            formatNumber(item.qty * item.price) +
                            ' <span class="text-[10px] font-normal text-ink-400">تومان</span>' +
                        '</p>' +
                    '</div>';
                container.appendChild(row);
            });
        }

        var totalItems = cart.length;
        document.getElementById('cartItemCount').textContent = totalItems;
        document.getElementById('cartItemCountBottom').textContent = totalItems;

        var badge = document.getElementById('cartBadgeHeader');
        if (totalItems > 0) {
            badge.style.display = 'grid';
            badge.textContent = totalItems;
        } else {
            badge.style.display = 'none';
        }

        recalcCart();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function recalcCart() {
        var subtotal = cart.reduce(function (s, i) { return s + (i.qty * i.price); }, 0);
        var discount = parseFloat((document.getElementById('cartDiscount').value || '0').replace(/[,\s]/g, '')) || 0;
        if (discount < 0) discount = 0;
        if (discount > subtotal) discount = subtotal;

        var total = subtotal - discount;

        document.getElementById('cartSubtotal').textContent = formatNumber(subtotal);
        document.getElementById('cartTotal').textContent = formatNumber(total);
        document.getElementById('cartTotalBottom').textContent = formatNumber(total);
    }

    function updateProductBadges() {
        var cards = document.querySelectorAll('.product-card');
        for (var i = 0; i < cards.length; i++) {
            var card = cards[i];
            var pid = parseInt(card.getAttribute('data-product-id'), 10);
            var item = findCartItem(pid);
            var badge = card.querySelector('[data-cart-badge]');

            if (item && item.qty > 0) {
                card.classList.add('in-cart');
                if (badge) {
                    badge.style.display = 'grid';
                    badge.textContent = item.qty;
                }
            } else {
                card.classList.remove('in-cart');
                if (badge) badge.style.display = 'none';
            }
        }
    }

    function openCart() {
        document.getElementById('cartDrawer').classList.add('open');
        document.getElementById('cartOverlay').classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeCart() {
        document.getElementById('cartDrawer').classList.remove('open');
        document.getElementById('cartOverlay').classList.remove('open');
        document.body.style.overflow = '';
    }

    // ═══════════════════════════════════════════════════════
    // SUBMIT CART
    // ═══════════════════════════════════════════════════════
    function submitCart() {
        if (!selectedCustomer) {
            showToast('ابتدا مشتری را انتخاب کنید', 'error');
            closeCart();
            openCustomerPicker();
            return;
        }
        if (cart.length === 0) {
            showToast('سبد خرید خالی است', 'error');
            return;
        }

        var discount = parseFloat((document.getElementById('cartDiscount').value || '0').replace(/[,\s]/g, '')) || 0;

        var payload = {
            customerId: selectedCustomer.id,
            customerType: selectedCustomer.type,
            discount: discount,
            items: cart.map(function (i) {
                return {
                    productId: i.productId,
                    qty: i.qty,
                    price: i.price,
                    name: i.name,
                    code: i.code
                };
            })
        };

        document.getElementById('<%= hfCartJson.ClientID %>').value = JSON.stringify(payload);

        var btn = document.getElementById('btnSubmitCart');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> در حال ثبت...';

        if (typeof __doPostBack === 'function') {
            __doPostBack('submitCartAction', '');
        }
    }

    // ═══════════════════════════════════════════════════════
    // SUCCESS CALLBACK
    // ═══════════════════════════════════════════════════════
    function showSuccessModal(proformaId, proformaNumber) {
        lastCreatedProformaId = proformaId;
        document.getElementById('successProformaNumber').textContent = proformaNumber;
        document.getElementById('lnkViewProforma').href = 'proforma-invoices.aspx?id=' + proformaId;
        document.getElementById('successModal').classList.add('open');
        document.body.style.overflow = 'hidden';

        var btn = document.getElementById('btnSubmitCart');
        btn.disabled = false;
        btn.innerHTML = '<i class="h-5 w-5" data-lucide="check-circle-2"></i> ثبت پیش‌فاکتور';

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function printProforma() {
        if (lastCreatedProformaId > 0) {
            window.open('proforma-print.aspx?id=' + lastCreatedProformaId, '_blank');
        }
    }

    function resetForNextCustomer() {
        cart = [];
        selectedCustomer = null;
        lastCreatedProformaId = 0;

        document.getElementById('<%= hfCartJson.ClientID %>').value = '[]';
        document.getElementById('<%= hfSelectedCustomerId.ClientID %>').value = '0';

        document.getElementById('customerNameDisplay').textContent = 'هنوز مشتری انتخاب نشده';
        document.getElementById('customerHint').textContent = 'برای نمایش قیمت‌ها، مشتری را انتخاب کنید';
        document.getElementById('customerBtnText').textContent = 'انتخاب مشتری';
        document.getElementById('customerAvatar').className =
            'grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600';
        document.getElementById('customerAvatar').innerHTML = '<i class="h-5 w-5" data-lucide="user-plus"></i>';

        document.getElementById('cartDiscount').value = '0';
        document.getElementById('productSearch').value = '';

        currentCategoryFilter = 'all';
        var chips = document.querySelectorAll('.category-chip');
        for (var i = 0; i < chips.length; i++) {
            if (chips[i].getAttribute('data-category') === 'all') chips[i].classList.add('active');
            else chips[i].classList.remove('active');
        }

        document.getElementById('successModal').classList.remove('open');
        document.body.style.overflow = '';

        updateAllProductPrices();
        updateProductBadges();
        renderCart();
        filterProducts();

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════
    function formatNumber(n) {
        if (!n || n === 0) return '۰';
        var num = Math.round(parseFloat(n));
        if (isNaN(num)) return '۰';
        return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '،');
    }

    function escapeHtml(s) {
        if (!s) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function getCustomerTypeLabel(type) {
        type = parseInt(type, 10);
        if (type === 1) return 'تک‌فروشی';
        if (type === 2) return 'عمده';
        if (type === 3) return 'نمایندگی';
        if (type === 4) return 'کشاورز';
        return '—';
    }

    function getCustomerTypeColor(type) {
        type = parseInt(type, 10);
        if (type === 1) return 'bg-emerald-50 text-emerald-700';
        if (type === 2) return 'bg-brand-50 text-brand-700';
        if (type === 3) return 'bg-violet-50 text-violet-700';
        if (type === 4) return 'bg-amber-50 text-amber-700';
        return 'bg-surface-muted text-ink-600';
    }

    function showToast(message, type) {
        if (!type) type = 'info';
        var container = document.getElementById('toastContainer');
        if (!container) return;

        var toast = document.createElement('div');
        toast.className = 'toast toast--' + type;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-10px)';
            toast.style.transition = 'all .3s';
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 300);
        }, 2500);
    }

    function syncCartToHiddenField() {
        var payload = {
            customerId: selectedCustomer ? selectedCustomer.id : 0,
            customerType: selectedCustomer ? selectedCustomer.type : 0,
            items: cart.map(function (i) {
                return { productId: i.productId, qty: i.qty, price: i.price };
            })
        };
        document.getElementById('<%= hfCartJson.ClientID %>').value = JSON.stringify(payload);
    }

    // ═══════════════════════════════════════════════════════
    // KEYBOARD HANDLING — اولویت‌بندی درست
    // ═══════════════════════════════════════════════════════
    document.addEventListener('keydown', function (e) {

        // ── ۱) اگر زوم باز است، فقط Esc کار می‌کند ──
        var zoomOverlay = document.getElementById('zoomOverlay');
        if (zoomOverlay && zoomOverlay.classList.contains('open')) {
            if (e.key === 'Escape') {
                closeZoom();
                e.preventDefault();
                e.stopPropagation();
            }
            return;
        }

        // ── ۲) اگر گالری باز است ──
        var galleryOpen = document.getElementById('galleryModal').classList.contains('open');
        if (galleryOpen) {
            if (e.key === 'Escape') { closeGallery(); e.preventDefault(); return; }
            if (e.key === 'ArrowRight') { galleryPrev(); e.preventDefault(); return; }
            if (e.key === 'ArrowLeft') { galleryNext(); e.preventDefault(); return; }
            return;
        }

        // ── ۳) بقیه مودال‌ها ──
        if (e.key === 'Escape') {
            closeQtyModal();
            closeCart();
            closeCustomerPicker();
            closeNewCustomerModal();
        }
    });
</script>
</asp:Content>