<%@ Page Title="گردش انبار" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="stock-movements.aspx.cs" Inherits="admin_stock_movements" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfSelectedMovementId" runat="server" Value="0" />
    <asp:HiddenField ID="hfDateRange" runat="server" Value="7" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">گردش انبار</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">گردش انبار (تاریخچه کامل)</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                تمام ورود، خروج، تعدیل و انتقال‌های انبار با جزئیات کامل لایه‌های FIFO.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="stock-onhand.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="boxes"></i>
                موجودی لحظه‌ای
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
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-emerald-500 to-emerald-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">ورود (IN)</p>
                    <p class="mt-2 text-[22px] font-extrabold text-emerald-600">
                        <asp:Literal ID="litStatIn" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">خرید + تولید + تعدیل مثبت</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-emerald-50 text-emerald-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="arrow-down-to-line"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-rose-500 to-rose-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">خروج (OUT)</p>
                    <p class="mt-2 text-[22px] font-extrabold text-rose-600">
                        <asp:Literal ID="litStatOut" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">فروش + مصرف + تعدیل منفی</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-rose-50 text-rose-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="arrow-up-from-line"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-brand-500 to-brand-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">کل گردش‌های امروز</p>
                    <p class="mt-2 text-[22px] font-extrabold text-brand-600">
                        <asp:Literal ID="litStatToday" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">رویداد ثبت‌شده</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-brand-50 text-brand-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="activity"></i>
                </span>
            </div>
        </article>

        <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
            <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-violet-500 to-violet-400"></div>
            <div class="flex items-start justify-between">
                <div>
                    <p class="text-[12px] font-semibold text-ink-500">کل گردش‌های دوره</p>
                    <p class="mt-2 text-[22px] font-extrabold text-violet-600">
                        <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                    </p>
                    <p class="mt-1 text-[11px] font-semibold text-ink-400">بر اساس فیلتر فعلی</p>
                </div>
                <span class="grid h-11 w-11 place-items-center rounded-base bg-violet-50 text-violet-600 transition group-hover:scale-110">
                    <i class="h-5 w-5" data-lucide="arrow-left-right"></i>
                </span>
            </div>
        </article>

    </asp:Panel>

    <!-- ═══════════ فیلترها ═══════════ -->
    <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

        <!-- Quick date range -->
        <div class="mb-4 flex flex-wrap items-center gap-2">
            <span class="text-[12.5px] font-bold text-ink-700 ml-2">بازه زمانی:</span>

            <button type="button" onclick="setDateRange('1'); return false;" id="btnRange1"
                class="date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                امروز
            </button>
            <button type="button" onclick="setDateRange('7'); return false;" id="btnRange7"
                class="date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                ۷ روز
            </button>
            <button type="button" onclick="setDateRange('30'); return false;" id="btnRange30"
                class="date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                ۳۰ روز
            </button>
            <button type="button" onclick="setDateRange('90'); return false;" id="btnRange90"
                class="date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                ۹۰ روز
            </button>
            <button type="button" onclick="setDateRange('0'); return false;" id="btnRange0"
                class="date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                همه
            </button>
        </div>

        <!-- Direction quick filter -->
        <div class="mb-4 flex flex-wrap items-center gap-2">
            <span class="text-[12.5px] font-bold text-ink-700 ml-2">جهت گردش:</span>

            <button type="button" onclick="setDirection(''); return false;" id="btnDirAll"
                class="dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="layers"></i>
                همه
            </button>
            <button type="button" onclick="setDirection('IN'); return false;" id="btnDirIn"
                class="dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="arrow-down-to-line"></i>
                ورود
            </button>
            <button type="button" onclick="setDirection('OUT'); return false;" id="btnDirOut"
                class="dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="arrow-up-from-line"></i>
                خروج
            </button>
            <button type="button" onclick="setDirection('TRANSFER'); return false;" id="btnDirTransfer"
                class="dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                <i class="h-3 w-3" data-lucide="arrow-right-left"></i>
                انتقال
            </button>
        </div>

        <!-- Filters -->
        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-4">

            <label class="relative block lg:col-span-2">
                <span class="sr-only">جستجو</span>
                <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                    data-lucide="search"></i>
                <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                    OnTextChanged="txtSearch_TextChanged"
                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                    placeholder="جستجو در نام/کد محصول، شماره بچ، مرجع یا یادداشت..." />
            </label>

            <asp:DropDownList ID="ddlFilterMovementType" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterMovementType_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه انواع گردش" />
                <asp:ListItem Value="1" Text="ورود خرید" />
                <asp:ListItem Value="2" Text="ورود تولید" />
                <asp:ListItem Value="3" Text="خروج فروش" />
                <asp:ListItem Value="4" Text="خروج مصرف تولید" />
                <asp:ListItem Value="5" Text="تعدیل افزایشی" />
                <asp:ListItem Value="6" Text="تعدیل کاهشی" />
                <asp:ListItem Value="7" Text="انتقال" />
            </asp:DropDownList>

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
                <asp:ListItem Value="3" Text="بسته‌بندی" />
            </asp:DropDownList>

            <asp:DropDownList ID="ddlFilterReferenceType" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterReferenceType_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه اسناد مرجع" />
                <asp:ListItem Value="PurchaseOrder" Text="سفارش خرید" />
                <asp:ListItem Value="ProductionOrder" Text="دستور تولید" />
                <asp:ListItem Value="FinalInvoice" Text="فاکتور نهایی" />
                <asp:ListItem Value="Adjustment" Text="تعدیل دستی" />
                <asp:ListItem Value="Transfer" Text="انتقال بین انبار" />
            </asp:DropDownList>

            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="25" Text="۲۵ ردیف" />
                <asp:ListItem Value="50" Text="۵۰ ردیف" />
                <asp:ListItem Value="100" Text="۱۰۰ ردیف" />
            </asp:DropDownList>

            <div class="flex items-center gap-2">
                <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن"
                    OnClick="btnClearFilters_Click"
                    CssClass="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <a href="javascript:void(0);" onclick="exportToCsv(); return false;"
                    class="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="download"></i>
                    CSV
                </a>
            </div>

        </div>
    </section>

    <!-- ═══════════ جدول گردش ═══════════ -->
    <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="mb-4 flex items-center justify-between">
            <h2 class="text-[15px] font-bold text-ink-900">تاریخچه گردش</h2>
            <p class="text-[12.5px] text-ink-500">
                <asp:Literal ID="litInfo" runat="server" />
            </p>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <table class="w-full min-w-[1250px] text-right">
                <thead>
                    <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                        <th class="pb-3 pr-4 text-right font-semibold">محصول</th>
                        <th class="pb-3 pr-4 text-center font-semibold">نوع گردش</th>
                        <th class="pb-3 pr-4 text-center font-semibold">انبار</th>
                        <th class="pb-3 pr-4 text-center font-semibold">مقدار</th>
                        <th class="pb-3 pr-4 text-center font-semibold">قیمت واحد</th>
                        <th class="pb-3 pr-4 text-center font-semibold">ارزش</th>
                        <th class="pb-3 pr-4 text-center font-semibold">سند مرجع</th>
                        <th class="pb-3 pr-4 text-center font-semibold">تاریخ</th>
                        <th class="pb-3 text-left font-semibold">جزئیات</th>
                    </tr>
                </thead>
                <tbody class="text-[13.5px]">
                    <asp:Repeater ID="rptMovements" runat="server" OnItemCommand="rptMovements_ItemCommand">
                        <ItemTemplate>
                            <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
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
                                    <span class='<%# GetMovementTypeBadgeCss(Eval("MovementType")) %>'>
                                        <i class="h-3 w-3" data-lucide='<%# GetMovementTypeIcon(Eval("MovementType")) %>'></i>
                                        <%# GetMovementTypeLabel(Eval("MovementType")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class='<%# GetWarehouseBadgeCss(Eval("WarehouseType")) %>'>
                                        <%# Enc(Eval("WarehouseName")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class='<%# GetQuantitySignClass(Eval("MovementType")) %>'>
                                        <%# GetQuantitySign(Eval("MovementType")) %>
                                        <%# FormatQuantity(Eval("Quantity")) %>
                                    </span>
                                    <span class="ml-1 text-[10.5px] text-ink-400">
                                        <%# EncOrDash(Eval("UnitName")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-center font-mono text-[12.5px] text-ink-700">
                                    <%# FormatMoney(Eval("UnitCost")) %>
                                </td>
                                <td class="py-3 pr-4 text-center font-mono text-[12.5px] font-bold text-emerald-700">
                                    <%# FormatMoney(Eval("TotalCost")) %>
                                </td>
                                <td class="py-3 pr-4 text-center text-[12px]">
                                    <%# RenderReference(Eval("ReferenceType"), Eval("ReferenceId")) %>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <p class="text-[12px] font-semibold text-ink-700">
                                        <%# FormatShamsiDate(Eval("MovementDate")) %>
                                    </p>
                                    <p class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr">
                                        <%# FormatTime(Eval("MovementDate")) %>
                                    </p>
                                </td>
                                <td class="py-3 text-left">
                                    <asp:LinkButton ID="btnDetails" runat="server"
                                        CommandName="Details" CommandArgument='<%# Eval("StockMovementId") %>'
                                        CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                        ToolTip="مشاهده جزئیات و لایه‌ها">
                                        <i class="h-4 w-4" data-lucide="eye"></i>
                                    </asp:LinkButton>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>

        <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
            <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                <i class="h-6 w-6" data-lucide="arrow-left-right"></i>
            </span>
            <p class="text-[14px] font-semibold text-ink-600">گردشی با این فیلتر یافت نشد.</p>
            <p class="mt-1 text-[12.5px] text-ink-400">بازه زمانی یا فیلترهای دیگری را امتحان کنید.</p>
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

    <!-- ═══════════ Modal جزئیات ═══════════ -->
    <asp:Panel ID="pnlDetailsModal" runat="server" Visible="false">
        <div class="fixed inset-0 z-[100] flex items-center justify-center p-4" style="background: rgba(15, 23, 42, 0.55); backdrop-filter: blur(4px);">
            <div class="relative w-full max-w-4xl rounded-card bg-surface-card shadow-card-lg" style="max-height: 92vh; overflow-y: auto;">

                <!-- Header -->
                <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-line bg-surface-card px-6 py-4">
                    <div class="flex items-center gap-3">
                        <span id="modalIconWrap" class="grid h-11 w-11 place-items-center rounded-base bg-brand-50 text-brand-600">
                            <i id="modalIcon" class="h-5 w-5" data-lucide="arrow-left-right"></i>
                        </span>
                        <div>
                            <h3 class="text-[15px] font-bold text-ink-900">جزئیات گردش انبار</h3>
                            <p class="mt-0.5 font-mono text-[11.5px] text-ink-400" dir="ltr">
                                #<asp:Literal ID="litModalMovementId" runat="server" />
                            </p>
                        </div>
                    </div>
                    <asp:LinkButton ID="btnCloseModal" runat="server" OnClick="btnCloseModal_Click"
                        CssClass="icon-button hover:bg-rose-50 hover:text-rose-500" ToolTip="بستن">
                        <i class="h-4 w-4" data-lucide="x"></i>
                    </asp:LinkButton>
                </div>

                <!-- Body -->
                <div class="p-6">

                    <!-- Meta grid -->
                    <div class="mb-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">محصول</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalProduct" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">انبار</p>
                            <p class="mt-1">
                                <asp:Literal ID="litModalWarehouse" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">نوع گردش</p>
                            <p class="mt-1">
                                <asp:Literal ID="litModalType" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">مقدار</p>
                            <p class="mt-1 text-[14px] font-bold">
                                <asp:Literal ID="litModalQuantity" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">قیمت واحد</p>
                            <p class="mt-1 font-mono text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalUnitCost" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">ارزش کل</p>
                            <p class="mt-1 font-mono text-[13px] font-bold text-emerald-700">
                                <asp:Literal ID="litModalTotalCost" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">سند مرجع</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalReference" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">شماره بچ</p>
                            <p class="mt-1 font-mono text-[13px] font-semibold text-ink-800" dir="ltr">
                                <asp:Literal ID="litModalBatch" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">تاریخ انقضا</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalExpiry" runat="server" Text="—" />
                            </p>
                        </div>

                    </div>

                    <!-- Notes -->
                    <asp:Panel ID="pnlModalNotes" runat="server" Visible="false" CssClass="mb-5">
                        <div class="rounded-base border border-amber-200 bg-amber-50/50 p-4">
                            <p class="mb-1 flex items-center gap-1.5 text-[12px] font-bold text-amber-800">
                                <i class="h-3.5 w-3.5" data-lucide="sticky-note"></i>
                                یادداشت
                            </p>
                            <p class="text-[12.5px] leading-relaxed text-ink-700">
                                <asp:Literal ID="litModalNotes" runat="server" />
                            </p>
                        </div>
                    </asp:Panel>

                    <!-- Layers Section -->
                    <div class="mb-2 flex items-center gap-2">
                        <span class="grid h-7 w-7 place-items-center rounded-lg bg-brand-100 text-brand-700">
                            <i class="h-3.5 w-3.5" data-lucide="layers"></i>
                        </span>
                        <h4 class="text-[13.5px] font-bold text-ink-800">
                            <asp:Literal ID="litLayersTitle" runat="server" Text="لایه‌های مرتبط (FIFO)" />
                        </h4>
                    </div>

                    <div class="dashboard-scrollbar overflow-x-auto rounded-base border border-surface-line">
                        <asp:Repeater ID="rptLayers" runat="server">
                            <HeaderTemplate>
                                <table class="w-full min-w-[700px] text-right">
                                    <thead>
                                        <tr class="bg-surface-body text-[11px] uppercase text-ink-500">
                                            <th class="px-3 py-2 text-right font-semibold">لایه</th>
                                            <th class="px-3 py-2 text-center font-semibold">دریافت اولیه</th>
                                            <th class="px-3 py-2 text-center font-semibold">باقی‌مانده</th>
                                            <th class="px-3 py-2 text-center font-semibold">مقدار مصرف</th>
                                            <th class="px-3 py-2 text-center font-semibold">قیمت واحد</th>
                                            <th class="px-3 py-2 text-center font-semibold">تاریخ دریافت</th>
                                            <th class="px-3 py-2 text-center font-semibold">وضعیت</th>
                                        </tr>
                                    </thead>
                                    <tbody class="text-[12.5px]">
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class="border-t border-surface-line">
                                    <td class="px-3 py-2 font-mono text-[12px] font-bold text-ink-800">
                                        #<%# Eval("StockLayerId") %>
                                    </td>
                                    <td class="px-3 py-2 text-center font-mono">
                                        <%# FormatQuantity(Eval("QuantityReceived")) %>
                                    </td>
                                    <td class="px-3 py-2 text-center font-mono">
                                        <%# FormatQuantity(Eval("QuantityRemaining")) %>
                                    </td>
                                    <td class="px-3 py-2 text-center font-mono font-bold text-rose-600">
                                        <%# FormatQuantityOrDash(Eval("QuantityUsed")) %>
                                    </td>
                                    <td class="px-3 py-2 text-center font-mono text-emerald-700">
                                        <%# FormatMoney(Eval("UnitCost")) %>
                                    </td>
                                    <td class="px-3 py-2 text-center text-[11.5px] text-ink-600">
                                        <%# FormatShamsiDate(Eval("ReceivedDate")) %>
                                    </td>
                                    <td class="px-3 py-2 text-center">
                                        <%# GetLayerStatusBadge(Eval("IsClosed")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate>
                                    </tbody>
                                </table>
                            </FooterTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:Panel ID="pnlNoLayers" runat="server" Visible="false"
                        CssClass="rounded-base border border-surface-line bg-surface-body py-8 text-center">
                        <p class="text-[13px] font-semibold text-ink-500">
                            اطلاعات لایه‌ای برای این گردش ثبت نشده است.
                        </p>
                    </asp:Panel>

                </div>

                <!-- Footer -->
                <div class="sticky bottom-0 border-t border-surface-line bg-surface-card px-6 py-3 text-left">
                    <asp:Button ID="btnCloseFooter" runat="server" Text="بستن" OnClick="btnCloseModal_Click"
                        CssClass="inline-flex h-9 items-center rounded-base bg-ink-800 px-5 text-[13px] font-semibold text-white hover:bg-ink-900" />
                </div>

            </div>
        </div>
    </asp:Panel>

    <asp:Literal ID="litCsvData" runat="server" Visible="false" />
    <asp:HiddenField ID="hfDirection" runat="server" Value="" />

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ Date Range ═══
    function setDateRange(days) {
        var field = document.getElementById('<%= hfDateRange.ClientID %>');
        if (field) field.value = days;
        __doPostBack('__PAGE', 'setRange:' + days);
    }

    function highlightRangeButton() {
        var field = document.getElementById('<%= hfDateRange.ClientID %>');
        var current = field ? field.value : '7';

        var buttons = document.querySelectorAll('.date-range-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].className = 'date-range-btn inline-flex h-8 items-center rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted';
        }

        var active = document.getElementById('btnRange' + current);
        if (active) {
            active.className = 'date-range-btn inline-flex h-8 items-center rounded-lg border border-brand-300 bg-brand-50 px-3 text-[12px] font-bold text-brand-700';
        }
    }

    // ═══ Direction ═══
    function setDirection(dir) {
        var field = document.getElementById('<%= hfDirection.ClientID %>');
        if (field) field.value = dir;
        __doPostBack('__PAGE', 'setDir:' + dir);
    }

    function highlightDirectionButton() {
        var field = document.getElementById('<%= hfDirection.ClientID %>');
        var current = field ? field.value : '';

        var buttons = document.querySelectorAll('.dir-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].className = 'dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted';
        }

        var active = null;
        if (current === 'IN') active = document.getElementById('btnDirIn');
        else if (current === 'OUT') active = document.getElementById('btnDirOut');
        else if (current === 'TRANSFER') active = document.getElementById('btnDirTransfer');
        else active = document.getElementById('btnDirAll');

        if (active) {
            active.className = 'dir-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-brand-300 bg-brand-50 px-3 text-[12px] font-bold text-brand-700';
        }
    }

    // ═══ CSV ═══
    function exportToCsv() {
        var csvField = document.getElementById('<%= litCsvData.ClientID %>');
        if (!csvField) { alert('داده‌ای برای خروجی یافت نشد.'); return; }

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
        link.setAttribute('download', 'stock-movements_' + stamp + '.csv');
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // ═══ Escape to close modal ═══
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var closeBtn = document.getElementById('<%= btnCloseModal.ClientID %>');
            if (closeBtn) closeBtn.click();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        highlightRangeButton();
        highlightDirectionButton();
    });
</script>
</asp:Content>