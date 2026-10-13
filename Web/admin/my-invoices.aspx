<%@ Page Title="فاکتورهای من" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="my-invoices.aspx.cs" Inherits="admin_my_invoices" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .status-chip {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 7px 14px;
        border-radius: 20px;
        background: #fff;
        border: 1.5px solid #eef2f6;
        font-size: 12.5px;
        font-weight: 700;
        color: #475569;
        cursor: pointer;
        transition: all .2s;
        user-select: none;
        white-space: nowrap;
    }
    .status-chip:hover { border-color: #93c5fd; color: #2563eb; }
    .status-chip.active {
        background: #2563eb;
        border-color: #2563eb;
        color: #fff;
        box-shadow: 0 4px 12px -4px rgba(37, 99, 235, 0.5);
    }
    .status-chip .count {
        display: inline-grid;
        place-items: center;
        min-width: 20px;
        height: 20px;
        padding: 0 6px;
        border-radius: 10px;
        background: rgba(0, 0, 0, 0.08);
        font-size: 10.5px;
        font-weight: 800;
    }
    .status-chip.active .count { background: rgba(255, 255, 255, 0.25); }

    .inv-row { transition: background .15s; }
    .inv-row:hover { background: #f8fafc; }

    .edit-btn {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        padding: 6px 12px;
        border-radius: 8px;
        background: #2563eb;
        color: #fff;
        font-size: 12px;
        font-weight: 700;
        text-decoration: none;
        transition: all .2s;
        border: none;
        cursor: pointer;
    }
    .edit-btn:hover { background: #1d4ed8; }

    .empty-state { text-align: center; padding: 60px 20px; }
    .empty-state-icon {
        width: 80px; height: 80px; border-radius: 50%;
        background: #f1f5f9; margin: 0 auto 16px;
        display: grid; place-items: center; color: #94a3b8;
    }

    /* ═══ Details Modal ═══ */
    .details-modal {
        position: fixed;
        inset: 0;
        z-index: 90;
        display: none;
        align-items: center;
        justify-content: center;
        padding: 16px;
    }
    .details-modal.open { display: flex; }

    .details-backdrop {
        position: absolute;
        inset: 0;
        background: rgba(15, 23, 42, 0.6);
        backdrop-filter: blur(4px);
        animation: fadeIn .2s ease;
    }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

    .details-content {
        position: relative;
        width: 100%;
        max-width: 720px;
        max-height: 90vh;
        background: #fff;
        border-radius: 16px;
        box-shadow: 0 30px 80px -20px rgba(0, 0, 0, 0.4);
        display: flex;
        flex-direction: column;
        overflow: hidden;
        animation: modalIn .25s cubic-bezier(.4, 0, .2, 1);
    }
    @keyframes modalIn {
        from { transform: scale(.96); opacity: 0; }
        to { transform: scale(1); opacity: 1; }
    }

    .details-header {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 16px 20px;
        border-bottom: 1px solid #eef2f6;
        background: linear-gradient(90deg, #f8fafc, transparent);
    }

    .details-body {
        flex: 1;
        overflow-y: auto;
        padding: 20px;
    }
    .details-body::-webkit-scrollbar { width: 6px; }
    .details-body::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 3px; }

    .details-footer {
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        padding: 12px 20px;
        border-top: 1px solid #eef2f6;
        background: #f8fafc;
    }

    .detail-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 8px 0;
        border-bottom: 1px dashed #eef2f6;
        font-size: 13px;
    }
    .detail-row:last-child { border-bottom: none; }
    .detail-label { color: #64748b; font-weight: 600; }
    .detail-value { color: #0f172a; font-weight: 700; font-family: 'Vazirmatn', monospace; }

    .items-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 12.5px;
    }
    .items-table th {
        padding: 8px 6px;
        border-bottom: 2px solid #eef2f6;
        color: #64748b;
        font-weight: 700;
        text-align: right;
        font-size: 11.5px;
    }
    .items-table td {
        padding: 10px 6px;
        border-bottom: 1px solid #f1f5f9;
    }

    .spinner-modal {
        width: 40px;
        height: 40px;
        border: 3px solid #e2e8f0;
        border-top-color: #2563eb;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
        margin: 40px auto;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="sales-presentation.aspx">کاتالوگ فروش</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">فاکتورهای من</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">فاکتورهای من</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                وضعیت پیش‌فاکتورها و فاکتورهای ثبت‌شده توسط شما
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="sales-presentation.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="plus-circle"></i>
                ثبت پیش‌فاکتور جدید
            </a>
        </div>
    </div>

    <!-- ═══════════ راهنما ═══════════ -->
    <div class="mb-4 flex items-start gap-3 rounded-base border border-brand-100 bg-brand-50/50 p-3">
        <span class="grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-brand-100 text-brand-600">
            <i class="h-4 w-4" data-lucide="info"></i>
        </span>
        <p class="text-[12.5px] leading-relaxed text-ink-700">
            شما فقط می‌توانید <strong>پیش‌نویس‌های خودتان</strong> را ویرایش کنید.
            تبدیل پیش‌نویس به پیش‌فاکتور و صدور فاکتور نهایی فقط توسط <strong>مدیر سیستم</strong> انجام می‌شود.
        </p>
    </div>

    <!-- ═══════════ کارت‌های آماری ═══════════ -->
    <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="file-text"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل فاکتورها</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-warning-50 text-warning-600">
                    <i class="h-5 w-5" data-lucide="clock"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">در انتظار</p>
                    <p class="mt-0.5 text-[20px] font-bold text-warning-600">
                        <asp:Literal ID="litStatPending" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-success-50 text-success-600">
                    <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">پرداخت‌شده</p>
                    <p class="mt-0.5 text-[20px] font-bold text-success-600">
                        <asp:Literal ID="litStatPaid" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                    <i class="h-5 w-5" data-lucide="banknote"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">مجموع فروش (تومان)</p>
                    <p class="mt-0.5 text-[15px] font-bold text-violet-600">
                        <asp:Literal ID="litStatTotalAmount" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

    </div>

    <!-- ═══════════ Filters ═══════════ -->
    <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">

        <div class="mb-3 flex flex-wrap items-center gap-2">
            <span class="text-[12px] font-semibold text-ink-500 ml-1">وضعیت:</span>

            <div class="status-chip active" data-status="" onclick="selectStatus('')">
                همه <span class="count"><asp:Literal ID="litCountAll" runat="server" Text="0" /></span>
            </div>
            <div class="status-chip" data-status="1" onclick="selectStatus('1')">
                صادرشده <span class="count"><asp:Literal ID="litCountIssued" runat="server" Text="0" /></span>
            </div>
            <div class="status-chip" data-status="2" onclick="selectStatus('2')">
                پیش‌نویس <span class="count"><asp:Literal ID="litCountDraft" runat="server" Text="0" /></span>
            </div>
            <div class="status-chip" data-status="4" onclick="selectStatus('4')">
                پرداخت‌شده <span class="count"><asp:Literal ID="litCountPaid" runat="server" Text="0" /></span>
            </div>
            <div class="status-chip" data-status="5" onclick="selectStatus('5')">
                لغوشده <span class="count"><asp:Literal ID="litCountCancelled" runat="server" Text="0" /></span>
            </div>
        </div>

        <div class="flex flex-wrap items-center gap-2">
            <div class="status-chip active" data-range="30" onclick="selectRange('30')">۳۰ روز اخیر</div>
            <div class="status-chip" data-range="90" onclick="selectRange('90')">۹۰ روز اخیر</div>
            <div class="status-chip" data-range="365" onclick="selectRange('365')">یک سال اخیر</div>
            <div class="status-chip" data-range="all" onclick="selectRange('all')">همه</div>

            <label class="relative block mr-auto min-w-[220px]">
                <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                    data-lucide="search"></i>
                <input type="text" id="searchBox" placeholder="جستجوی شماره فاکتور یا نام مشتری..."
                    oninput="filterTable();"
                    class="h-10 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-500 focus:outline-none" />
            </label>
        </div>

        <asp:HiddenField ID="hfStatusFilter" runat="server" Value="" />
        <asp:HiddenField ID="hfRangeFilter" runat="server" Value="30" />
    </section>

    <!-- ═══════════ Table ═══════════ -->
    <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="overflow-x-auto">
            <table class="w-full min-w-[900px] text-right">
                <thead>
                    <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                        <th class="pb-3 pr-4 text-right font-semibold">شماره فاکتور</th>
                        <th class="pb-3 pr-4 text-right font-semibold">مشتری</th>
                        <th class="pb-3 pr-4 text-center font-semibold">تاریخ صدور</th>
                        <th class="pb-3 pr-4 text-center font-semibold">مبلغ (تومان)</th>
                        <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                        <th class="pb-3 text-left font-semibold">اقدام</th>
                    </tr>
                </thead>
                <tbody id="invoicesBody" class="text-[13.5px]">

                    <asp:Repeater ID="rptInvoices" runat="server">
                        <ItemTemplate>
                            <tr class="inv-row border-b border-surface-line"
                                data-invoice-number='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("ProformaNumber"))) %>'
                                data-customer-name='<%# HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("CustomerName"))) %>'
                                data-status='<%# Eval("Status") %>'
                                data-issue-date-ticks='<%# GetIssueDateTicks(Eval("IssueDate")) %>'>

                                <td class="py-4 pr-4">
                                    <p class="font-mono font-bold text-ink-900" dir="ltr">
                                        <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("ProformaNumber"))) %>
                                    </p>
                                    <p class="mt-0.5 text-[11px] text-ink-400">
                                        پیش‌فاکتور
                                        <%# GetFinalInvoiceBadge(Eval("FinalInvoiceNumber")) %>
                                    </p>
                                </td>

                                <td class="py-4 pr-4">
                                    <p class="font-semibold text-ink-800">
                                        <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("CustomerName"))) %>
                                    </p>
                                    <p class="mt-0.5 text-[11px] text-ink-400">
                                        <%# HttpUtility.HtmlEncode(Convert.ToString(Eval("City"))) %>
                                    </p>
                                </td>

                                <td class="py-4 pr-4 text-center">
                                    <p class="text-ink-700"><%# GetShamsiDate(Eval("IssueDate")) %></p>
                                    <p class="mt-0.5 text-[11px] text-ink-400">
                                        اعتبار: <%# GetShamsiDate(Eval("ValidUntil")) %>
                                    </p>
                                </td>

                                <td class="py-4 pr-4 text-center">
                                    <p class="font-mono font-bold text-emerald-700">
                                        <%# FormatMoney(Eval("Total")) %>
                                    </p>
                                </td>

                                <td class="py-4 pr-4 text-center">
                                    <span class='<%# GetStatusBadgeCss(Eval("Status")) %>'>
                                        <%# GetStatusLabel(Eval("Status")) %>
                                    </span>
                                </td>

                                <td class="py-4 text-left">
                                    <div class="inline-flex items-center gap-1 flex-wrap">

                                        <%-- ═══ ویرایش: فقط پیش‌نویسِ خودم ═══ --%>
                                        <asp:PlaceHolder ID="phEdit" runat="server"
                                            Visible='<%# CanEdit(Eval("Status"), Eval("MarketerId")) %>'>
                                            <a href='<%# "my-invoice-edit.aspx?id=" + Eval("ProformaInvoiceId") %>'
                                                class="edit-btn" title="ویرایش پیش‌نویس">
                                                <i class="h-3.5 w-3.5" data-lucide="pencil"></i>
                                                ویرایش
                                            </a>
                                        </asp:PlaceHolder>

                                        <%-- ═══ مشاهده جزئیات (پاپ‌آپ) ═══ --%>
                                        <button type="button"
                                            onclick='openDetailsModal(<%# Eval("ProformaInvoiceId") %>); return false;'
                                            class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            title="مشاهده جزئیات">
                                            <i class="h-4 w-4" data-lucide="eye"></i>
                                        </button>

                                        <%-- ═══ چاپ ═══ --%>
                                        <a href='<%# "proforma-print.aspx?id=" + Eval("ProformaInvoiceId") %>'
                                            target="_blank"
                                            class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            title="چاپ">
                                            <i class="h-4 w-4" data-lucide="printer"></i>
                                        </a>

                                    </div>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>

                </tbody>
            </table>
        </div>

        <div id="emptyState" class="empty-state" style="display:none;">
            <div class="empty-state-icon">
                <i class="h-8 w-8" data-lucide="file-x"></i>
            </div>
            <p class="text-[15px] font-bold text-ink-700">فاکتوری با این فیلتر یافت نشد</p>
            <p class="mt-1 text-[12.5px] text-ink-400">فیلترها یا بازه‌ی زمانی را تغییر دهید</p>
        </div>

        <div class="mt-5 flex flex-wrap items-center justify-between gap-3">
            <p class="text-[12.5px] text-ink-500">
                نمایش <span id="visibleCount">0</span> از
                <asp:Literal ID="litTotalCount" runat="server" Text="0" /> فاکتور
            </p>
            <a href="sales-presentation.aspx"
                class="text-[12.5px] font-bold text-brand-600 hover:text-brand-700">
                ثبت پیش‌فاکتور جدید →
            </a>
        </div>

    </section>

    <!-- ═══════════ Details Modal ═══════════ -->
    <div id="detailsModal" class="details-modal" onclick="closeDetailsModal();">
        <div class="details-backdrop"></div>

        <div class="details-content" onclick="event.stopPropagation();">

            <div class="details-header">
                <span class="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="receipt"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p id="dtlNumber" class="text-[15px] font-extrabold text-ink-900" dir="ltr">—</p>
                    <p id="dtlStatusLabel" class="mt-0.5 text-[11.5px] font-semibold text-ink-500">در حال بارگذاری...</p>
                </div>
                <button type="button" onclick="closeDetailsModal(); return false;"
                    class="grid h-9 w-9 place-items-center rounded-lg text-ink-500 hover:bg-surface-muted transition">
                    <i class="h-5 w-5" data-lucide="x"></i>
                </button>
            </div>

            <div id="detailsBody" class="details-body">
                <div id="dtlLoading" class="spinner-modal"></div>

                <div id="dtlContent" style="display:none;">

                    <!-- وضعیت + مبلغ کل -->
                    <div class="mb-5 rounded-card border border-surface-line bg-gradient-to-l from-brand-50/60 to-transparent p-4">
                        <div class="flex items-center justify-between flex-wrap gap-2">
                            <div>
                                <p class="text-[11px] font-semibold text-ink-500">مبلغ کل</p>
                                <p class="mt-0.5 font-mono text-[20px] font-black text-emerald-700">
                                    <span id="dtlTotal">۰</span>
                                    <span class="text-[11px] font-normal text-ink-400">تومان</span>
                                </p>
                            </div>
                            <span id="dtlStatusBadge" class="badge bg-surface-muted text-ink-700">—</span>
                        </div>
                    </div>

                    <!-- اطلاعات مشتری -->
                    <h3 class="mb-2 text-[13px] font-bold text-ink-700 flex items-center gap-2">
                        <i class="h-4 w-4 text-brand-500" data-lucide="user-round"></i>
                        مشتری
                    </h3>
                    <div class="mb-5 rounded-base border border-surface-line bg-surface-body p-3">
                        <div class="detail-row">
                            <span class="detail-label">نام مشتری</span>
                            <span class="detail-value" id="dtlCustomerName">—</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">شهر</span>
                            <span class="detail-value" id="dtlCity">—</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">موبایل</span>
                            <span class="detail-value" id="dtlMobile" dir="ltr">—</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">آدرس</span>
                            <span class="detail-value" id="dtlAddress" style="font-family: inherit; font-weight: 500;">—</span>
                        </div>
                    </div>

                    <!-- اطلاعات فاکتور -->
                    <h3 class="mb-2 text-[13px] font-bold text-ink-700 flex items-center gap-2">
                        <i class="h-4 w-4 text-brand-500" data-lucide="calendar"></i>
                        اطلاعات فاکتور
                    </h3>
                    <div class="mb-5 rounded-base border border-surface-line bg-surface-body p-3">
                        <div class="detail-row">
                            <span class="detail-label">تاریخ صدور</span>
                            <span class="detail-value" id="dtlIssueDate">—</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">معتبر تا</span>
                            <span class="detail-value" id="dtlValidUntil">—</span>
                        </div>
                        <div class="detail-row" id="dtlFinalRow" style="display:none;">
                            <span class="detail-label">شماره فاکتور نهایی</span>
                            <span class="detail-value text-success-700" id="dtlFinalNumber" dir="ltr">—</span>
                        </div>
                    </div>

                    <!-- اقلام -->
                    <h3 class="mb-2 text-[13px] font-bold text-ink-700 flex items-center gap-2">
                        <i class="h-4 w-4 text-brand-500" data-lucide="list"></i>
                        اقلام (<span id="dtlItemsCount">0</span>)
                    </h3>
                    <div class="mb-5 rounded-base border border-surface-line overflow-hidden">
                        <table class="items-table w-full">
                            <thead>
                                <tr class="bg-surface-body">
                                    <th>محصول</th>
                                    <th class="text-center">تعداد</th>
                                    <th class="text-center">قیمت واحد</th>
                                    <th class="text-left">جمع</th>
                                </tr>
                            </thead>
                            <tbody id="dtlItemsBody"></tbody>
                        </table>
                    </div>

                    <!-- جمع‌ها -->
                    <div class="rounded-base border border-surface-line bg-surface-body p-3">
                        <div class="detail-row">
                            <span class="detail-label">جمع جزء</span>
                            <span class="detail-value" id="dtlSubtotal">۰</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">تخفیف</span>
                            <span class="detail-value text-danger-600" id="dtlDiscount">۰</span>
                        </div>
                        <div class="detail-row" style="border-top: 2px solid #eef2f6; margin-top: 4px; padding-top: 10px;">
                            <span class="text-[14px] font-bold text-ink-900">مبلغ کل</span>
                            <span class="font-mono text-[16px] font-black text-emerald-700" id="dtlTotalFooter">۰</span>
                        </div>
                    </div>

                    <!-- یادداشت -->
                    <div id="dtlNotesBox" class="mt-4" style="display:none;">
                        <h3 class="mb-2 text-[13px] font-bold text-ink-700 flex items-center gap-2">
                            <i class="h-4 w-4 text-brand-500" data-lucide="message-square"></i>
                            یادداشت
                        </h3>
                        <div class="rounded-base border border-surface-line bg-surface-body p-3 text-[13px] text-ink-700 leading-relaxed" id="dtlNotes"></div>
                    </div>

                </div>

                <div id="dtlError" class="py-12 text-center" style="display:none;">
                    <div class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-danger-50 text-danger-500">
                        <i class="h-6 w-6" data-lucide="alert-triangle"></i>
                    </div>
                    <p class="text-[15px] font-bold text-danger-700">خطا در بارگذاری</p>
                    <p class="mt-1 text-[12.5px] text-ink-500" id="dtlErrorText">—</p>
                </div>
            </div>

            <div class="details-footer">
                <a id="dtlPrintLink" href="#" target="_blank"
                    class="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line bg-white px-4 text-[13px] font-bold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="printer"></i>
                    چاپ
                </a>
                <button type="button" onclick="closeDetailsModal(); return false;"
                    class="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[13px] font-bold text-white hover:bg-brand-700">
                    بستن
                </button>
            </div>
        </div>
    </div>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    var currentStatus = '';
    var currentRange = '30';
    var detailsCache = {};

    document.addEventListener('DOMContentLoaded', function () {
        applyFilters();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });

    // ═══════════════════════════════════════════════════════
    // FILTERS
    // ═══════════════════════════════════════════════════════
    function selectStatus(status) {
        currentStatus = status;
        document.getElementById('<%= hfStatusFilter.ClientID %>').value = status;

        var chips = document.querySelectorAll('.status-chip[data-status]');
        for (var i = 0; i < chips.length; i++) {
            if (chips[i].getAttribute('data-status') === status) chips[i].classList.add('active');
            else chips[i].classList.remove('active');
        }

        applyFilters();
    }

    function selectRange(range) {
        currentRange = range;
        document.getElementById('<%= hfRangeFilter.ClientID %>').value = range;

        var chips = document.querySelectorAll('.status-chip[data-range]');
        for (var i = 0; i < chips.length; i++) {
            if (chips[i].getAttribute('data-range') === range) chips[i].classList.add('active');
            else chips[i].classList.remove('active');
        }

        applyFilters();
    }

    function filterTable() { applyFilters(); }

    function applyFilters() {
        var rows = document.querySelectorAll('.inv-row');
        var search = (document.getElementById('searchBox').value || '').trim().toLowerCase();
        var nowTicks = new Date().getTime();
        var rangeMs = 0;

        if (currentRange === '30') rangeMs = 30 * 24 * 60 * 60 * 1000;
        else if (currentRange === '90') rangeMs = 90 * 24 * 60 * 60 * 1000;
        else if (currentRange === '365') rangeMs = 365 * 24 * 60 * 60 * 1000;

        var visible = 0;

        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            var status = row.getAttribute('data-status');
            var invNum = (row.getAttribute('data-invoice-number') || '').toLowerCase();
            var custName = (row.getAttribute('data-customer-name') || '').toLowerCase();
            var dateTicks = parseInt(row.getAttribute('data-issue-date-ticks') || '0', 10);

            var matchStatus = (currentStatus === '' || status === currentStatus);

            var matchRange = true;
            if (rangeMs > 0 && dateTicks > 0) {
                var dateMs = (dateTicks / 10000);
                matchRange = (nowTicks - dateMs) <= rangeMs;
            }

            var matchSearch = (search === '' ||
                invNum.indexOf(search) > -1 ||
                custName.indexOf(search) > -1);

            if (matchStatus && matchRange && matchSearch) {
                row.style.display = '';
                visible++;
            } else {
                row.style.display = 'none';
            }
        }

        document.getElementById('visibleCount').textContent = visible;
        document.getElementById('emptyState').style.display = visible === 0 ? 'block' : 'none';

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    // ═══════════════════════════════════════════════════════
    // DETAILS MODAL
    // ═══════════════════════════════════════════════════════
    function openDetailsModal(invoiceId) {
        if (!invoiceId || invoiceId <= 0) return;

        var modal = document.getElementById('detailsModal');
        modal.classList.add('open');
        document.body.style.overflow = 'hidden';

        // Reset UI
        document.getElementById('dtlLoading').style.display = 'block';
        document.getElementById('dtlContent').style.display = 'none';
        document.getElementById('dtlError').style.display = 'none';
        document.getElementById('dtlNumber').textContent = '—';
        document.getElementById('dtlStatusLabel').textContent = 'در حال بارگذاری...';

        if (typeof lucide !== 'undefined') lucide.createIcons();

        // از کش استفاده کن
        if (detailsCache[invoiceId]) {
            renderDetails(detailsCache[invoiceId]);
            return;
        }

        // Fetch
        var url = 'my-invoice-details.ashx?id=' + encodeURIComponent(invoiceId);

        fetch(url, { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.error) {
                    showDetailsError(getErrorMessage(data.error));
                    return;
                }
                detailsCache[invoiceId] = data;
                renderDetails(data);
            })
            .catch(function (err) {
                showDetailsError('ارتباط با سرور برقرار نشد.');
            });
    }

    function closeDetailsModal() {
        var modal = document.getElementById('detailsModal');
        if (modal) modal.classList.remove('open');
        document.body.style.overflow = '';
    }

    function showDetailsError(msg) {
        document.getElementById('dtlLoading').style.display = 'none';
        document.getElementById('dtlContent').style.display = 'none';
        document.getElementById('dtlError').style.display = 'block';
        document.getElementById('dtlErrorText').textContent = msg || 'خطای نامشخص';
        document.getElementById('dtlStatusLabel').textContent = 'خطا';
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function getErrorMessage(code) {
        if (code === 'not_authenticated') return 'شما وارد نشده‌اید. لطفاً دوباره وارد شوید.';
        if (code === 'not_authorized') return 'شما مجاز به مشاهده این فاکتور نیستید.';
        if (code === 'not_owned') return 'این فاکتور به شما تعلق ندارد.';
        if (code === 'not_found') return 'فاکتور یافت نشد.';
        if (code === 'invalid_id') return 'شناسه فاکتور نامعتبر است.';
        return 'خطا: ' + code;
    }

    function renderDetails(d) {
        document.getElementById('dtlLoading').style.display = 'none';
        document.getElementById('dtlError').style.display = 'none';
        document.getElementById('dtlContent').style.display = 'block';

        // Header
        document.getElementById('dtlNumber').textContent = d.number || '—';
        document.getElementById('dtlStatusLabel').textContent = d.statusLabel || '—';

        // Status badge
        var badge = document.getElementById('dtlStatusBadge');
        badge.textContent = d.statusLabel || '—';
        badge.className = 'badge ' + (d.statusCss || 'bg-surface-muted text-ink-700');

        // Total
        document.getElementById('dtlTotal').textContent = formatNum(d.total);
        document.getElementById('dtlTotalFooter').textContent = formatNum(d.total) + ' تومان';

        // Customer
        document.getElementById('dtlCustomerName').textContent = d.customerName || '—';
        document.getElementById('dtlCity').textContent = d.city || '—';
        document.getElementById('dtlMobile').textContent = d.mobile || '—';
        document.getElementById('dtlAddress').textContent = d.address || '—';

        // Invoice info
        document.getElementById('dtlIssueDate').textContent = d.issueDate || '—';
        document.getElementById('dtlValidUntil').textContent = d.validUntil || '—';

        // Final invoice number
        var finalRow = document.getElementById('dtlFinalRow');
        if (d.finalInvoiceNumber && d.finalInvoiceNumber.length > 0) {
            finalRow.style.display = 'flex';
            document.getElementById('dtlFinalNumber').textContent = d.finalInvoiceNumber;
        } else {
            finalRow.style.display = 'none';
        }

        // Items
        var itemsBody = document.getElementById('dtlItemsBody');
        itemsBody.innerHTML = '';
        var items = d.items || [];
        document.getElementById('dtlItemsCount').textContent = items.length;

        if (items.length === 0) {
            itemsBody.innerHTML = '<tr><td colspan="4" style="text-align:center;padding:20px;color:#94a3b8;">هیچ قلمی ثبت نشده است.</td></tr>';
        } else {
            for (var i = 0; i < items.length; i++) {
                var it = items[i];
                var tr = document.createElement('tr');
                tr.innerHTML =
                    '<td>' +
                        '<p style="font-weight:700;color:#0f172a;">' + escapeHtml(it.name) + '</p>' +
                        '<p style="font-size:10.5px;color:#94a3b8;font-family:monospace;" dir="ltr">' + escapeHtml(it.code) + '</p>' +
                    '</td>' +
                    '<td style="text-align:center;font-family:monospace;font-weight:700;">' +
                        formatNum(it.qty) + ' <span style="font-size:10px;color:#94a3b8;font-weight:400;">' + escapeHtml(it.unit || '') + '</span>' +
                    '</td>' +
                    '<td style="text-align:center;font-family:monospace;color:#475569;">' + formatNum(it.unitPrice) + '</td>' +
                    '<td style="text-align:left;font-family:monospace;font-weight:800;color:#047857;">' + formatNum(it.total) + '</td>';
                itemsBody.appendChild(tr);
            }
        }

        // Totals
        document.getElementById('dtlSubtotal').textContent = formatNum(d.subtotal) + ' تومان';
        document.getElementById('dtlDiscount').textContent = formatNum(d.discount) + ' تومان';

        // Notes
        var notesBox = document.getElementById('dtlNotesBox');
        if (d.notes && d.notes.length > 0) {
            notesBox.style.display = 'block';
            document.getElementById('dtlNotes').textContent = d.notes;
        } else {
            notesBox.style.display = 'none';
        }

        // Print link
        document.getElementById('dtlPrintLink').href = 'proforma-print.aspx?id=' + d.id;

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════
    function formatNum(n) {
        if (n === null || n === undefined || n === 0) return '۰';
        var num = parseFloat(n);
        if (isNaN(num)) return '۰';
        var parts = num.toFixed(2).replace(/\.00$/, '').split('.');
        var intPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, '،');
        return intPart;
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

    // Esc برای بستن مودال
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var modal = document.getElementById('detailsModal');
            if (modal && modal.classList.contains('open')) {
                closeDetailsModal();
            }
        }
    });
</script>
</asp:Content>