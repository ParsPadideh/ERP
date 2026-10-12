<%@ Page Title="موجودی لحظه‌ای" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="stock-onhand.aspx.cs" Inherits="admin_stock_onhand" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">موجودی لحظه‌ای</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">موجودی لحظه‌ای انبار</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                موجودی فعلی انبارها بر اساس موتور FIFO و لایه‌های فعال.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="stock-movements.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="arrow-left-right"></i>
                گردش انبار
            </a>
            <a href="javascript:void(0);" onclick="location.reload(); return false;"
                class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="refresh-cw"></i>
                بروزرسانی
            </a>
        </div>
    </div>

    <!-- ═══════════ کارت‌های آماری ═══════════ -->
    <asp:Panel ID="pnlStats" runat="server" CssClass="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-amber-500 to-amber-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">ارزش کل موجودی</p>
                    <p class="mt-2 text-[22px] font-extrabold text-ink-900">
                        <asp:Literal ID="litStatTotalValue" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">تومان</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-amber-50 text-amber-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="coins"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-brand-500 to-brand-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">اقلام موجود</p>
                    <p class="mt-2 text-[22px] font-extrabold text-ink-900">
                        <asp:Literal ID="litStatItems" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">قلم کالا در انبار</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-brand-50 text-brand-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="package"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-violet-500 to-violet-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">موجودی رزروشده</p>
                    <p class="mt-2 text-[22px] font-extrabold text-violet-600">
                        <asp:Literal ID="litStatReserved" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">قلم فعال</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-violet-50 text-violet-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="lock"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-rose-500 to-rose-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">هشدار کمبود</p>
                    <p class="mt-2 text-[22px] font-extrabold text-danger-600">
                        <asp:Literal ID="litStatLowStock" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">کالای نیازمند سفارش</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-rose-50 text-rose-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="alert-triangle"></i>
                </span>
            </div>
        </article>

    </asp:Panel>

    <!-- ═══════════ فیلترها ═══════════ -->
    <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

        <div class="mb-4 flex flex-wrap items-center gap-2">
            <span class="text-[12.5px] font-bold text-ink-700 ml-2">نمای انبار:</span>

            <button type="button" onclick="setWarehouseView('0'); return false;" id="btnViewAll"
                class="wh-view-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="layers"></i>
                همه
            </button>
            <button type="button" onclick="setWarehouseView('RM'); return false;" id="btnViewRM"
                class="wh-view-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="package"></i>
                مواد اولیه (RM)
            </button>
            <button type="button" onclick="setWarehouseView('FG'); return false;" id="btnViewFG"
                class="wh-view-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="package-check"></i>
                محصول نهایی (FG)
            </button>

            <span class="mr-auto text-[12px] text-ink-500">
                <asp:Literal ID="litFilterInfo" runat="server" />
            </span>
        </div>

        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-4">

            <label class="relative block lg:col-span-2">
                <span class="sr-only">جستجو</span>
                <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                    data-lucide="search"></i>
                <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                    OnTextChanged="txtSearch_TextChanged"
                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                    placeholder="جستجو بر اساس نام یا کد محصول" />
            </label>

            <asp:DropDownList ID="ddlFilterWarehouse" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterWarehouse_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
            </asp:DropDownList>

            <asp:DropDownList ID="ddlFilterProductType" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterProductType_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه انواع محصول" />
                <asp:ListItem Value="1" Text="مواد اولیه" />
                <asp:ListItem Value="2" Text="محصول نهایی" />
                <asp:ListItem Value="3" Text="مواد بسته‌بندی" />
            </asp:DropDownList>

            <asp:DropDownList ID="ddlFilterStockState" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterStockState_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                <asp:ListItem Value="low" Text="⚠️ فقط کمبود" />
                <asp:ListItem Value="available" Text="✓ دارای موجودی" />
                <asp:ListItem Value="reserved" Text="🔒 دارای رزرو" />
            </asp:DropDownList>

            <div class="lg:col-span-2 flex items-center gap-2">
                <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن فیلترها"
                    OnClick="btnClearFilters_Click"
                    CssClass="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <a href="javascript:void(0);" onclick="exportToCsv(); return false;"
                    class="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="download"></i>
                    خروجی CSV
                </a>
            </div>

        </div>
    </section>

    <!-- ═══════════ جدول موجودی ═══════════ -->
    <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="mb-4 flex items-center justify-between">
            <h2 class="text-[15px] font-bold text-ink-900">موجودی به تفکیک انبار و محصول</h2>
            <p class="text-[12.5px] text-ink-500">
                <asp:Literal ID="litInfo" runat="server" />
            </p>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <table class="w-full min-w-[1200px] text-right">
                <thead>
                    <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                        <th class="pb-3 pr-4 text-right font-semibold">محصول</th>
                        <th class="pb-3 pr-4 text-center font-semibold">انبار</th>
                        <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                        <th class="pb-3 pr-4 text-center font-semibold">موجودی فیزیکی</th>
                        <th class="pb-3 pr-4 text-center font-semibold">رزروشده</th>
                        <th class="pb-3 pr-4 text-center font-semibold">قابل فروش</th>
                        <th class="pb-3 pr-4 text-center font-semibold">حداقل</th>
                        <th class="pb-3 pr-4 text-center font-semibold">ارزش</th>
                        <th class="pb-3 text-left font-semibold">اقدام</th>
                    </tr>
                </thead>
                <tbody class="text-[13.5px]">
                    <asp:Repeater ID="rptStock" runat="server" OnItemCommand="rptStock_ItemCommand">
                        <ItemTemplate>
                            <tr class='<%# GetRowCss(Eval("OnHand"), Eval("MinStock"), Eval("Available")) %>'>
                                <td class="py-3 pr-4">
                                    <div class="flex items-center gap-3">
                                        <span class='<%# GetProductAvatarCss(Eval("ProductType")) %>'>
                                            <i class="h-4 w-4" data-lucide='<%# GetProductIcon(Eval("ProductType")) %>'></i>
                                        </span>
                                        <div class="min-w-0 text-right">
                                            <p class="truncate font-semibold text-ink-900 text-[13px]">
                                                <%# Enc(Eval("ProductName")) %>
                                            </p>
                                            <p class="mt-0.5 truncate font-mono text-[11px] text-ink-400" dir="ltr">
                                                <%# Enc(Eval("ProductCode")) %>
                                            </p>
                                        </div>
                                    </div>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class='<%# GetWarehouseBadgeCss(Eval("WarehouseType")) %>'>
                                        <i class="h-3 w-3" data-lucide='<%# GetWarehouseIcon(Eval("WarehouseType")) %>'></i>
                                        <%# Enc(Eval("WarehouseName")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class='<%# GetTypeBadgeCss(Eval("ProductType")) %>'>
                                        <%# GetTypeLabel(Eval("ProductType")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class="inline-flex items-center gap-1.5 font-mono text-[13px] font-bold text-ink-800">
                                        <%# FormatQuantity(Eval("OnHand")) %>
                                        <span class="text-[10.5px] font-semibold text-ink-400">
                                            <%# EncOrDash(Eval("UnitName")) %>
                                        </span>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <%# RenderReserved(Eval("Reserved")) %>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <%# RenderAvailable(Eval("Available"), Eval("OnHand")) %>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class="font-mono text-[12px] text-ink-600">
                                        <%# FormatQuantity(Eval("MinStock")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class="font-mono text-[12.5px] font-bold text-emerald-700">
                                        <%# FormatMoney(Eval("StockValue")) %>
                                    </span>
                                </td>
                                <td class="py-3 text-left">
                                    <div class="inline-flex items-center gap-1">
                                        <a href='<%# "stock-movements.aspx?productId=" + Eval("ProductId") + "&warehouseId=" + Eval("WarehouseId") %>'
                                            class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            title="مشاهده گردش این محصول">
                                            <i class="h-4 w-4" data-lucide="history"></i>
                                        </a>
                                        <a href='<%# "products.aspx?q=" + HttpUtility.UrlEncode(Convert.ToString(Eval("ProductCode"))) %>'
                                            class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            title="مشاهده محصول">
                                            <i class="h-4 w-4" data-lucide="external-link"></i>
                                        </a>
                                    </div>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>

        <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
            <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                <i class="h-6 w-6" data-lucide="package-x"></i>
            </span>
            <p class="text-[14px] font-semibold text-ink-600">موردی با این فیلتر یافت نشد.</p>
            <p class="mt-1 text-[12.5px] text-ink-400">فیلترها را تغییر دهید یا بعداً مراجعه کنید.</p>
        </asp:Panel>

        <!-- Pagination -->
        <div class="mt-5 flex flex-wrap items-center justify-between gap-3">
            <p class="text-[12.5px] text-ink-500">
                <asp:Literal ID="litPageInfo" runat="server" />
            </p>
            <div class="flex items-center gap-2">
                <asp:LinkButton ID="btnPrev" runat="server" OnClick="btnPrev_Click"
                    CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-3.5 w-3.5" data-lucide="chevron-right"></i>
                    قبلی
                </asp:LinkButton>
                <span class="text-[13px] font-bold text-ink-700 px-2">
                    <asp:Literal ID="litCurrentPage" runat="server" Text="۱" />
                </span>
                <asp:LinkButton ID="btnNext" runat="server" OnClick="btnNext_Click"
                    CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                    بعدی
                    <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                </asp:LinkButton>
            </div>
        </div>

    </section>

    <!-- Hidden -->
    <asp:HiddenField ID="hfWarehouseView" runat="server" Value="0" />
    <asp:Literal ID="litCsvData" runat="server" Visible="false" />

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ Warehouse View Buttons ═══
    function setWarehouseView(view) {
        var field = document.getElementById('<%= hfWarehouseView.ClientID %>');
        if (field) field.value = view;
        __doPostBack('__PAGE', 'setWhView:' + view);
    }

    function highlightWarehouseViewButton() {
        var field = document.getElementById('<%= hfWarehouseView.ClientID %>');
        var current = field ? field.value : '0';

        var buttons = document.querySelectorAll('.wh-view-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].className = 'wh-view-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted';
        }

        var active = null;
        if (current === 'RM') active = document.getElementById('btnViewRM');
        else if (current === 'FG') active = document.getElementById('btnViewFG');
        else active = document.getElementById('btnViewAll');

        if (active) {
            active.className = 'wh-view-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-brand-300 bg-brand-50 px-3 text-[12px] font-bold text-brand-700';
        }
    }

    // ═══ CSV Export ═══
    function exportToCsv() {
        var csvField = document.getElementById('<%= litCsvData.ClientID %>');
        if (!csvField) {
            alert('داده‌ای برای خروجی یافت نشد.');
            return;
        }

        var csv = csvField.textContent || csvField.innerText || '';
        if (!csv || csv.trim().length === 0) {
            alert('ابتدا فیلترها را اعمال کنید و سپس خروجی بگیرید.');
            return;
        }

        var blob = new Blob(["\uFEFF" + csv], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        var url = URL.createObjectURL(blob);

        var now = new Date();
        var stamp = now.getFullYear() + '-' +
                    String(now.getMonth() + 1).padStart(2, '0') + '-' +
                    String(now.getDate()).padStart(2, '0') + '_' +
                    String(now.getHours()).padStart(2, '0') +
                    String(now.getMinutes()).padStart(2, '0');

        link.setAttribute('href', url);
        link.setAttribute('download', 'stock-onhand_' + stamp + '.csv');
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // ═══ Init ═══
    document.addEventListener('DOMContentLoaded', function () {
        highlightWarehouseViewButton();
    });
</script>
</asp:Content>