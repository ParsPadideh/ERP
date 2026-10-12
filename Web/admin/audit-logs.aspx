<%@ Page Title="لاگ فعالیت‌ها | سامانه ERP" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="audit-logs.aspx.cs" Inherits="admin_audit_logs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfSelectedLogId" runat="server" Value="0" />
    <asp:HiddenField ID="hfDateRange" runat="server" Value="7" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">لاگ فعالیت‌ها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">لاگ فعالیت‌های سامانه</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                تمام عملیات کاربران در سامانه ثبت و قابل ردیابی است.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <button type="button" onclick="exportToCsv(); return false;"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="download"></i>
                خروجی CSV
            </button>
            <a href="javascript:void(0);" onclick="location.reload(); return false;"
                class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="refresh-cw"></i>
                بروزرسانی
            </a>
        </div>
    </div>

    <!-- ═══════════ کارت‌های آماری ═══════════ -->
    <asp:Panel ID="pnlStats" runat="server" CssClass="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="scroll-text"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل رویدادهای امروز</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatToday" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-5 w-5" data-lucide="log-in"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">ورود موفق امروز</p>
                    <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                        <asp:Literal ID="litStatLogins" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-rose-50 text-rose-600">
                    <i class="h-5 w-5" data-lucide="shield-x"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">ورود ناموفق (۲۴ ساعت)</p>
                    <p class="mt-0.5 text-[20px] font-bold text-rose-600">
                        <asp:Literal ID="litStatFailed" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-5 w-5" data-lucide="activity"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل رویدادهای ثبت‌شده</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

    </asp:Panel>

    <!-- ═══════════ فیلترها ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

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

        <!-- Filters grid -->
        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-4">

            <label class="relative block lg:col-span-2">
                <span class="sr-only">جستجو</span>
                <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                    data-lucide="search"></i>
                <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                    OnTextChanged="txtSearch_TextChanged"
                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                    placeholder="جستجو در نام کاربر، IP، موجودیت یا شناسه..." />
            </label>

            <asp:DropDownList ID="ddlFilterUser" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterUser_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
            </asp:DropDownList>

            <asp:DropDownList ID="ddlFilterAction" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterAction_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه عملیات‌ها" />
                <asp:ListItem Value="Login" Text="ورود موفق" />
                <asp:ListItem Value="LoginFailed" Text="ورود ناموفق" />
                <asp:ListItem Value="Logout" Text="خروج" />
                <asp:ListItem Value="Insert" Text="ایجاد" />
                <asp:ListItem Value="Update" Text="ویرایش" />
                <asp:ListItem Value="Delete" Text="حذف" />
                <asp:ListItem Value="SoftDelete" Text="حذف نرم" />
                <asp:ListItem Value="Restore" Text="بازگردانی" />
                <asp:ListItem Value="ChangePassword" Text="تغییر رمز" />
                <asp:ListItem Value="ChangePasswordFailed" Text="خطای تغییر رمز" />
                <asp:ListItem Value="ResetPassword" Text="ریست رمز" />
                <asp:ListItem Value="ToggleActive" Text="تغییر وضعیت" />
            </asp:DropDownList>

        </div>

        <div class="mt-3 grid gap-3 md:grid-cols-2 lg:grid-cols-4">

            <asp:DropDownList ID="ddlFilterEntity" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilterEntity_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="" Text="همه موجودیت‌ها" />
                <asp:ListItem Value="Users" Text="کاربران" />
                <asp:ListItem Value="Roles" Text="نقش‌ها" />
                <asp:ListItem Value="Products" Text="محصولات" />
                <asp:ListItem Value="ProductCategories" Text="دسته‌بندی محصولات" />
                <asp:ListItem Value="Units" Text="واحدها" />
                <asp:ListItem Value="Warehouses" Text="انبارها" />
                <asp:ListItem Value="Customers" Text="مشتریان" />
                <asp:ListItem Value="Suppliers" Text="تأمین‌کنندگان" />
                <asp:ListItem Value="PriceLists" Text="لیست‌های قیمت" />
                <asp:ListItem Value="PurchaseOrders" Text="سفارش‌های خرید" />
                <asp:ListItem Value="ProductionOrders" Text="دستورهای تولید" />
                <asp:ListItem Value="SalesOrders" Text="سفارش‌های فروش" />
                <asp:ListItem Value="ProformaInvoices" Text="پیش‌فاکتورها" />
                <asp:ListItem Value="FinalInvoices" Text="فاکتورهای نهایی" />
                <asp:ListItem Value="Expenses" Text="هزینه‌ها" />
                <asp:ListItem Value="StockMovements" Text="گردش انبار" />
            </asp:DropDownList>

            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged"
                CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                <asp:ListItem Value="25" Text="۲۵ ردیف در صفحه" />
                <asp:ListItem Value="50" Text="۵۰ ردیف در صفحه" />
                <asp:ListItem Value="100" Text="۱۰۰ ردیف در صفحه" />
            </asp:DropDownList>

            <div class="flex items-center gap-2">
                <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن فیلترها"
                    OnClick="btnClearFilters_Click"
                    CssClass="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
            </div>

        </div>
    </section>

    <!-- ═══════════ جدول لاگ‌ها ═══════════ -->
    <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="mb-4 flex items-center justify-between">
            <h2 class="text-[15px] font-bold text-ink-900">
                رویدادهای ثبت‌شده
            </h2>
            <p class="text-[12.5px] text-ink-500">
                <asp:Literal ID="litInfo" runat="server" />
            </p>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <table class="w-full min-w-[1000px] text-right" id="logsTable">
                <thead>
                    <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                        <th class="pb-3 pr-4 text-right font-semibold">کاربر</th>
                        <th class="pb-3 pr-4 text-center font-semibold">عملیات</th>
                        <th class="pb-3 pr-4 text-right font-semibold">موجودیت</th>
                        <th class="pb-3 pr-4 text-center font-semibold">تاریخ و زمان</th>
                        <th class="pb-3 pr-4 text-center font-semibold">IP</th>
                        <th class="pb-3 text-left font-semibold">جزئیات</th>
                    </tr>
                </thead>
                <tbody class="text-[13.5px]">
                    <asp:Repeater ID="rptLogs" runat="server" OnItemCommand="rptLogs_ItemCommand">
                        <ItemTemplate>
                            <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                <td class="py-3 pr-4">
                                    <div class="flex items-center gap-2.5">
                                        <span class='<%# GetUserAvatarCss(Eval("Action")) %>'>
                                            <i class="h-3.5 w-3.5" data-lucide='<%# GetActionIcon(Eval("Action")) %>'></i>
                                        </span>
                                        <div class="min-w-0 text-right">
                                            <p class="truncate font-semibold text-ink-800">
                                                <%# Enc(Eval("UserDisplay")) %>
                                            </p>
                                            <p class="mt-0.5 truncate font-mono text-[11px] text-ink-400" dir="ltr">
                                                <%# EncOrDash(Eval("Username")) %>
                                            </p>
                                        </div>
                                    </div>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <span class='<%# GetActionBadgeCss(Eval("Action")) %>'>
                                        <%# GetActionLabel(Eval("Action")) %>
                                    </span>
                                </td>
                                <td class="py-3 pr-4 text-right">
                                    <p class="font-semibold text-[12.5px] text-ink-700">
                                        <%# GetEntityLabel(Eval("EntityName")) %>
                                    </p>
                                    <p class="mt-0.5 font-mono text-[10.5px] text-ink-400">
                                        <%# FormatEntityId(Eval("EntityId")) %>
                                    </p>
                                </td>
                                <td class="py-3 pr-4 text-center">
                                    <p class="text-[12px] font-semibold text-ink-700">
                                        <%# FormatShamsiDate(Eval("CreatedAt")) %>
                                    </p>
                                    <p class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr">
                                        <%# FormatTime(Eval("CreatedAt")) %>
                                    </p>
                                </td>
                                <td class="py-3 pr-4 text-center font-mono text-[11.5px] text-ink-500" dir="ltr">
                                    <%# EncOrDash(Eval("IPAddress")) %>
                                </td>
                                <td class="py-3 text-left">
                                    <asp:LinkButton ID="btnDetails" runat="server"
                                        CommandName="Details" CommandArgument='<%# Eval("AuditLogId") %>'
                                        CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                        ToolTip="مشاهده جزئیات">
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
                <i class="h-6 w-6" data-lucide="scroll-text"></i>
            </span>
            <p class="text-[14px] font-semibold text-ink-600">رویدادی با این فیلتر یافت نشد.</p>
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
            <div class="relative w-full max-w-3xl rounded-card bg-surface-card shadow-card-lg" style="max-height: 90vh; overflow-y: auto;">

                <!-- Header -->
                <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-line bg-surface-card px-6 py-4">
                    <div class="flex items-center gap-3">
                        <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                            <i class="h-5 w-5" data-lucide="file-search"></i>
                        </span>
                        <div>
                            <h3 class="text-[15px] font-bold text-ink-900">جزئیات رویداد</h3>
                            <p class="mt-0.5 font-mono text-[11.5px] text-ink-400" dir="ltr">
                                #<asp:Literal ID="litModalLogId" runat="server" />
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

                    <!-- Meta -->
                    <div class="mb-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">کاربر</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalUser" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">عملیات</p>
                            <p class="mt-1">
                                <asp:Literal ID="litModalAction" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">موجودیت</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalEntity" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">تاریخ و زمان</p>
                            <p class="mt-1 text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalDate" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">IP</p>
                            <p class="mt-1 font-mono text-[13px] font-semibold text-ink-800" dir="ltr">
                                <asp:Literal ID="litModalIP" runat="server" Text="—" />
                            </p>
                        </div>

                        <div class="rounded-base border border-surface-line bg-surface-body p-3">
                            <p class="text-[11px] font-bold text-ink-500 uppercase tracking-wide">شناسه رکورد</p>
                            <p class="mt-1 font-mono text-[13px] font-semibold text-ink-800">
                                <asp:Literal ID="litModalEntityId" runat="server" Text="—" />
                            </p>
                        </div>

                    </div>

                    <!-- JSON viewers -->
                    <asp:Panel ID="pnlOldValues" runat="server" Visible="false" CssClass="mb-5">
                        <div class="mb-2 flex items-center justify-between">
                            <h4 class="flex items-center gap-2 text-[13px] font-bold text-ink-700">
                                <span class="grid h-6 w-6 place-items-center rounded-base bg-rose-50 text-rose-500">
                                    <i class="h-3.5 w-3.5" data-lucide="minus-circle"></i>
                                </span>
                                مقادیر قبلی
                            </h4>
                            <button type="button" onclick="copyToClipboard('oldValuesBlock', this); return false;"
                                class="inline-flex h-7 items-center gap-1 rounded-lg bg-surface-muted px-2.5 text-[11px] font-bold text-ink-600 hover:bg-ink-100">
                                <i class="h-3 w-3" data-lucide="copy"></i>
                                کپی
                            </button>
                        </div>
                        <pre id="oldValuesBlock" class="dashboard-scrollbar overflow-x-auto rounded-base border border-rose-100 bg-rose-50/30 p-4 font-mono text-[12px] leading-relaxed text-ink-700" style="direction: ltr; text-align: left; white-space: pre-wrap; word-break: break-word;"><asp:Literal ID="litOldValues" runat="server" /></pre>
                    </asp:Panel>

                    <asp:Panel ID="pnlNewValues" runat="server" Visible="false" CssClass="mb-5">
                        <div class="mb-2 flex items-center justify-between">
                            <h4 class="flex items-center gap-2 text-[13px] font-bold text-ink-700">
                                <span class="grid h-6 w-6 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                                    <i class="h-3.5 w-3.5" data-lucide="plus-circle"></i>
                                </span>
                                مقادیر جدید
                            </h4>
                            <button type="button" onclick="copyToClipboard('newValuesBlock', this); return false;"
                                class="inline-flex h-7 items-center gap-1 rounded-lg bg-surface-muted px-2.5 text-[11px] font-bold text-ink-600 hover:bg-ink-100">
                                <i class="h-3 w-3" data-lucide="copy"></i>
                                کپی
                            </button>
                        </div>
                        <pre id="newValuesBlock" class="dashboard-scrollbar overflow-x-auto rounded-base border border-emerald-100 bg-emerald-50/30 p-4 font-mono text-[12px] leading-relaxed text-ink-700" style="direction: ltr; text-align: left; white-space: pre-wrap; word-break: break-word;"><asp:Literal ID="litNewValues" runat="server" /></pre>
                    </asp:Panel>

                    <asp:Panel ID="pnlNoValues" runat="server" Visible="false"
                        CssClass="rounded-base border border-surface-line bg-surface-body py-8 text-center">
                        <p class="text-[13px] font-semibold text-ink-500">اطلاعات تکمیلی برای این رویداد ثبت نشده است.</p>
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

    <!-- Hidden field برای JSON خروجی CSV -->
    <asp:Literal ID="litCsvData" runat="server" Visible="false" />

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ Date range buttons ═══
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

    // ═══ Copy to clipboard ═══
    function copyToClipboard(elementId, btn) {
        var el = document.getElementById(elementId);
        if (!el) return;

        var text = el.textContent || el.innerText || '';

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function () {
                showCopyFeedback(btn, 'کپی شد ✓');
            }, function () {
                fallbackCopy(text, btn);
            });
        } else {
            fallbackCopy(text, btn);
        }
    }

    function fallbackCopy(text, btn) {
        var ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.select();
        try {
            document.execCommand('copy');
            showCopyFeedback(btn, 'کپی شد ✓');
        } catch (e) {
            showCopyFeedback(btn, 'خطا');
        }
        document.body.removeChild(ta);
    }

    function showCopyFeedback(btn, msg) {
        if (!btn) return;
        var original = btn.innerHTML;
        btn.innerHTML = msg;
        btn.classList.add('bg-emerald-50', 'text-emerald-700');
        setTimeout(function () {
            btn.innerHTML = original;
            btn.classList.remove('bg-emerald-50', 'text-emerald-700');
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }, 1500);
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
        link.setAttribute('download', 'audit-logs_' + stamp + '.csv');
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // ═══ Escape to close modal ═══
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var modal = document.querySelector('.fixed.inset-0.z-\\[100\\]');
            if (modal) {
                var closeBtn = document.getElementById('<%= btnCloseModal.ClientID %>');
                if (closeBtn) closeBtn.click();
            }
        }
    });

    // ═══ Init ═══
    document.addEventListener('DOMContentLoaded', function () {
        highlightRangeButton();
    });
</script>
</asp:Content>