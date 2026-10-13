<%@ Page Title="ویرایش پیش‌نویس" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="my-invoice-edit.aspx.cs" Inherits="admin_my_invoice_edit" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .qty-btn {
        width: 34px;
        height: 34px;
        border-radius: 8px;
        display: grid;
        place-items: center;
        background: #f1f5f9;
        color: #334155;
        border: none;
        cursor: pointer;
        transition: all .15s;
    }
    .qty-btn:hover { background: #e2e8f0; }
    .qty-btn:active { transform: scale(0.95); }

    .qty-input {
        width: 70px;
        height: 34px;
        text-align: center;
        font-family: 'Vazirmatn', monospace;
        font-size: 14px;
        font-weight: 800;
        border: 2px solid #eef2f6;
        border-radius: 8px;
        outline: none;
    }
    .qty-input:focus { border-color: #2563eb; }

    .item-row {
        transition: all .2s;
    }
    .item-row:hover {
        background: #f8fafc;
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfProformaId" runat="server" Value="0" />
    <asp:HiddenField ID="hfDraftData" runat="server" Value="{}" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="sales-presentation.aspx">کاتالوگ فروش</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <a class="hover:text-brand-600" href="my-invoices.aspx">فاکتورهای من</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">ویرایش پیش‌نویس</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                ویرایش پیش‌نویس
                <span class="font-mono text-[16px] text-ink-500 mr-2" dir="ltr">
                    <asp:Literal ID="litProformaNumber" runat="server" />
                </span>
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                می‌توانید اقلام، تعداد و تخفیف پیش‌نویس را تغییر دهید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="my-invoices.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="arrow-right"></i>
                بازگشت
            </a>
        </div>
    </div>

    <asp:Panel ID="pnlNotFound" runat="server" Visible="false" CssClass="rounded-card border border-danger-100 bg-danger-50 p-8 text-center">
        <i class="h-12 w-12 mx-auto text-danger-500" data-lucide="alert-triangle"></i>
        <p class="mt-3 text-[16px] font-bold text-danger-700">پیش‌نویس یافت نشد</p>
        <p class="mt-1 text-[13px] text-danger-600">
            این پیش‌نویس وجود ندارد، حذف شده، یا به شما تعلق ندارد.
        </p>
        <a href="my-invoices.aspx" class="mt-4 inline-flex items-center gap-2 rounded-base bg-brand-600 px-5 py-2.5 text-[13px] font-bold text-white hover:bg-brand-700">
            بازگشت به لیست
        </a>
    </asp:Panel>

    <asp:Panel ID="pnlEdit" runat="server" Visible="false">

        <!-- ═══════════ اطلاعات مشتری ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="user-round"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[11.5px] font-semibold text-ink-500">مشتری</p>
                    <p class="mt-0.5 text-[15px] font-bold text-ink-900">
                        <asp:Literal ID="litCustomerName" runat="server" />
                    </p>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        <asp:Literal ID="litCustomerInfo" runat="server" />
                    </p>
                </div>
                <span class="badge bg-warning-50 text-warning-700">
                    <i class="h-3.5 w-3.5" data-lucide="clock"></i>
                    پیش‌نویس
                </span>
            </div>
        </section>

        <!-- ═══════════ اقلام ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

            <div class="mb-4 flex items-center justify-between border-b border-surface-line pb-3">
                <h2 class="text-[15px] font-bold text-ink-800 flex items-center gap-2">
                    <i class="h-4 w-4 text-brand-500" data-lucide="list"></i>
                    اقلام پیش‌نویس
                </h2>
                <p class="text-[12.5px] text-ink-500">
                    <asp:Literal ID="litItemsCount" runat="server" Text="0" /> قلم
                </p>
            </div>

            <div id="itemsContainer"></div>

            <div id="emptyItems" class="py-10 text-center" style="display:none;">
                <p class="text-[14px] font-semibold text-ink-600">هیچ قلمی در این پیش‌نویس نیست.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">از بخش «افزودن محصول جدید» در پایین استفاده کنید.</p>
            </div>

        </section>

        <!-- ═══════════ افزودن محصول ═══════════ -->
        <section class="mb-5 rounded-card border border-brand-100 bg-brand-50/40 p-5 shadow-card">
            <h2 class="mb-3 text-[14px] font-bold text-ink-800 flex items-center gap-2">
                <i class="h-4 w-4 text-brand-600" data-lucide="plus-circle"></i>
                افزودن محصول جدید
            </h2>

            <div class="grid gap-3 sm:grid-cols-3">
                <label class="block sm:col-span-2">
                    <span class="text-[12.5px] font-semibold text-ink-700">انتخاب محصول</span>
                    <asp:DropDownList ID="ddlProductToAdd" runat="server"
                        CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-white px-3 text-[13.5px] select2-search"
                        data-placeholder="جستجوی محصول...">
                    </asp:DropDownList>
                </label>

                <div class="flex items-end gap-2">
                    <label class="block flex-1">
                        <span class="text-[12.5px] font-semibold text-ink-700">تعداد</span>
                        <input type="number" id="newItemQty" value="1" min="1" step="any"
                            class="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-white px-3 text-[13.5px] text-center font-mono font-bold focus:border-brand-500 focus:outline-none" dir="ltr" />
                    </label>
                    <button type="button" onclick="addNewItem(); return false;"
                        class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[13.5px] font-bold text-white hover:bg-brand-700">
                        <i class="h-4 w-4" data-lucide="plus"></i>
                        افزودن
                    </button>
                </div>
            </div>
        </section>

        <!-- ═══════════ تخفیف + جمع ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="space-y-3">
                <div class="flex items-center justify-between text-[13px]">
                    <span class="text-ink-600">جمع جزء</span>
                    <span class="font-mono font-bold text-ink-800">
                        <span id="subtotalDisplay">۰</span> تومان
                    </span>
                </div>

                <div class="flex items-center justify-between gap-3 text-[13px]">
                    <span class="text-ink-600">تخفیف (تومان)</span>
                    <input type="text" id="discountInput" value="0"
                        oninput="recalcTotals();"
                        class="h-10 w-40 rounded-base border border-surface-line bg-surface-body px-3 text-left font-mono text-[13px] focus:border-brand-500 focus:outline-none" dir="ltr" />
                </div>

                <div class="flex items-center justify-between border-t border-surface-line pt-3">
                    <span class="text-[15px] font-bold text-ink-900">مبلغ کل</span>
                    <span class="font-mono text-[20px] font-black text-emerald-700">
                        <span id="totalDisplay">۰</span> تومان
                    </span>
                </div>
            </div>
        </section>

        <!-- ═══════════ یادداشت ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                    CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[13.5px] focus:border-brand-500 focus:outline-none"
                    placeholder="توضیحات برای مدیر (اختیاری)" />
            </label>
        </section>

        <!-- ═══════════ Actions ═══════════ -->
        <div class="flex flex-wrap items-center justify-end gap-3 border-t border-surface-line pt-4">
            <a href="my-invoices.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-white px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                انصراف
            </a>
            <asp:Button ID="btnSave" runat="server" Text="ذخیره پیش‌نویس"
                OnClick="btnSave_Click"
                OnClientClick="submitSave();"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-emerald-600 px-5 text-[14px] font-bold text-white hover:bg-emerald-700" />
        </div>

    </asp:Panel>

    <!-- JSON محصولات -->
    <asp:Literal ID="litProductsJson" runat="server" />

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    var allProducts = {};
    var draftItems = [];
    var draftMeta = {};

    document.addEventListener('DOMContentLoaded', function () {
        loadProductsFromJson();
        loadDraftFromJson();
        renderItems();
        recalcTotals();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });

    function loadProductsFromJson() {
        try {
            var el = document.getElementById('<%= litProductsJson.ClientID %>');
            if (el && el.innerHTML) {
                var raw = el.innerHTML.trim();
                if (raw.length > 0) {
                    allProducts = JSON.parse(raw) || {};
                }
            }
        } catch (e) { allProducts = {}; }
    }

    function loadDraftFromJson() {
        try {
            var hf = document.getElementById('<%= hfDraftData.ClientID %>');
            if (hf && hf.value) {
                var data = JSON.parse(hf.value);
                draftMeta = data.meta || {};
                draftItems = data.items || [];

                var discountInput = document.getElementById('discountInput');
                if (discountInput) {
                    discountInput.value = draftMeta.discount || 0;
                }
            }
        } catch (e) {
            draftItems = [];
        }
    }

    function renderItems() {
        var container = document.getElementById('itemsContainer');
        var emptyBox = document.getElementById('emptyItems');
        if (!container) return;

        if (draftItems.length === 0) {
            container.innerHTML = '';
            emptyBox.style.display = 'block';
            updateItemsCount();
            return;
        }

        emptyBox.style.display = 'none';
        container.innerHTML = '';

        for (var i = 0; i < draftItems.length; i++) {
            (function (idx) {
                var it = draftItems[idx];
                var row = document.createElement('div');
                row.className = 'item-row flex flex-wrap items-center gap-3 rounded-base border border-surface-line bg-white p-3 mb-2';

                row.innerHTML =
                    '<div class="flex-1 min-w-[150px]">' +
                        '<p class="text-[13.5px] font-bold text-ink-900">' + escapeHtml(it.name) + '</p>' +
                        '<p class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr">' + escapeHtml(it.code) + '</p>' +
                    '</div>' +
                    '<div class="flex items-center gap-1">' +
                        '<button type="button" onclick="changeQty(' + idx + ', -1); return false;" class="qty-btn">' +
                            '<i class="h-4 w-4" data-lucide="minus"></i>' +
                        '</button>' +
                        '<input type="number" value="' + it.qty + '" min="0.001" step="any" ' +
                            'onchange="setQty(' + idx + ', this.value);" ' +
                            'class="qty-input" dir="ltr" />' +
                        '<button type="button" onclick="changeQty(' + idx + ', 1); return false;" class="qty-btn">' +
                            '<i class="h-4 w-4" data-lucide="plus"></i>' +
                        '</button>' +
                        '<span class="text-[11.5px] text-ink-400 mr-1">' + escapeHtml(it.unit || '') + '</span>' +
                    '</div>' +
                    '<div class="min-w-[120px] text-left">' +
                        '<p class="text-[10.5px] text-ink-400">قیمت واحد</p>' +
                        '<p class="font-mono text-[12.5px] font-bold text-ink-700">' + formatNumber(it.unitPrice) + '</p>' +
                    '</div>' +
                    '<div class="min-w-[130px] text-left">' +
                        '<p class="text-[10.5px] text-ink-400">جمع</p>' +
                        '<p class="font-mono text-[13px] font-black text-emerald-700">' +
                            formatNumber(it.qty * it.unitPrice) + ' <span class="text-[10px] font-normal">ت</span>' +
                        '</p>' +
                    '</div>' +
                    '<button type="button" onclick="removeItem(' + idx + '); return false;" ' +
                        'class="grid h-8 w-8 place-items-center rounded-lg text-danger-500 hover:bg-danger-50 transition" ' +
                        'title="حذف">' +
                        '<i class="h-4 w-4" data-lucide="trash-2"></i>' +
                    '</button>';

                container.appendChild(row);
            })(i);
        }

        updateItemsCount();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function updateItemsCount() {
        var litCount = document.getElementById('<%= litItemsCount.ClientID %>');
        if (litCount) litCount.textContent = draftItems.length;
    }

    function changeQty(index, delta) {
        if (index < 0 || index >= draftItems.length) return;
        var newQty = parseFloat(draftItems[index].qty) + delta;
        if (newQty < 0.001) newQty = 0.001;
        draftItems[index].qty = newQty;
        renderItems();
        recalcTotals();
    }

    function setQty(index, value) {
        if (index < 0 || index >= draftItems.length) return;
        var q = parseFloat(value);
        if (isNaN(q) || q <= 0) q = 1;
        draftItems[index].qty = q;
        renderItems();
        recalcTotals();
    }

    function removeItem(index) {
        if (index < 0 || index >= draftItems.length) return;
        if (!confirm('این قلم حذف شود؟')) return;
        draftItems.splice(index, 1);
        renderItems();
        recalcTotals();
    }

    function addNewItem() {
        var ddl = document.getElementById('<%= ddlProductToAdd.ClientID %>');
        var qtyInput = document.getElementById('newItemQty');

        if (!ddl || !ddl.value) {
            alert('ابتدا یک محصول انتخاب کنید.');
            return;
        }

        var productId = parseInt(ddl.value, 10);
        if (!productId) {
            alert('محصول معتبر نیست.');
            return;
        }

        var qty = parseFloat(qtyInput.value || '1');
        if (isNaN(qty) || qty <= 0) {
            alert('تعداد معتبر وارد کنید.');
            return;
        }

        var p = allProducts[String(productId)];
        if (!p) {
            alert('محصول یافت نشد.');
            return;
        }

        // اگر قبلاً اضافه شده، فقط تعداد رو زیاد کن
        for (var i = 0; i < draftItems.length; i++) {
            if (draftItems[i].productId === productId) {
                draftItems[i].qty = parseFloat(draftItems[i].qty) + qty;
                renderItems();
                recalcTotals();
                resetAddForm();
                return;
            }
        }

        draftItems.push({
            itemId: 0,
            productId: productId,
            name: p.name,
            code: p.code,
            unit: p.unit,
            qty: qty,
            unitPrice: p.price || 0
        });

        renderItems();
        recalcTotals();
        resetAddForm();
    }

    function resetAddForm() {
        var ddl = document.getElementById('<%= ddlProductToAdd.ClientID %>');
        var qtyInput = document.getElementById('newItemQty');
        if (qtyInput) qtyInput.value = '1';
        if (ddl) {
            ddl.value = '';
            if (typeof jQuery !== 'undefined' && jQuery(ddl).data('select2')) {
                jQuery(ddl).val(null).trigger('change');
            }
        }
    }

    function recalcTotals() {
        var subtotal = 0;
        for (var i = 0; i < draftItems.length; i++) {
            subtotal += parseFloat(draftItems[i].qty) * parseFloat(draftItems[i].unitPrice);
        }

        var discountInput = document.getElementById('discountInput');
        var discount = parseFloat((discountInput.value || '0').replace(/[,\s]/g, '')) || 0;
        if (discount < 0) discount = 0;
        if (discount > subtotal) discount = subtotal;

        var total = subtotal - discount;

        document.getElementById('subtotalDisplay').textContent = formatNumber(subtotal);
        document.getElementById('totalDisplay').textContent = formatNumber(total);
    }

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

    function submitSave() {
        var discountInput = document.getElementById('discountInput');
        var discount = parseFloat((discountInput.value || '0').replace(/[,\s]/g, '')) || 0;

        var payload = {
            meta: { discount: discount },
            items: draftItems.map(function (it) {
                return {
                    itemId: it.itemId,
                    productId: it.productId,
                    qty: it.qty,
                    unitPrice: it.unitPrice
                };
            })
        };

        var hf = document.getElementById('<%= hfDraftData.ClientID %>');
        hf.value = JSON.stringify(payload);
    }
</script>
</asp:Content>