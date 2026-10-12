<%@ Page Title="دوره‌های تقسیم سود" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="profit-periods.aspx.cs" Inherits="admin_profit_periods" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .fin-row { display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; border-bottom: 1px solid #eef2f6; }
    .fin-row:last-child { border-bottom: none; }
    .fin-label { font-size: 13.5px; color: #475569; font-weight: 600; }
    .fin-value { font-family: 'Vazirmatn', monospace; font-size: 14px; font-weight: 700; direction: ltr; }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfFilterYear" runat="server" Value="0" />
    <asp:HiddenField ID="hfActivePaymentItemId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <a class="hover:text-brand-600" href="profit-settings.aspx">تنظیمات تقسیم سود</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">دوره‌ها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="دوره‌های تقسیم سود" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="دوره‌ها را ببندید و سود شرکا را محاسبه و تقسیم کنید." />
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:PlaceHolder ID="phBackToList" runat="server" Visible="false">
                <asp:LinkButton ID="btnBackToList" runat="server" OnClick="btnBackToList_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="arrow-right"></i>
                    بازگشت به لیست دوره‌ها
                </asp:LinkButton>
            </asp:PlaceHolder>

            <a href="profit-settings.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="settings-2"></i>
                تنظیمات دوره‌ها
            </a>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 1: LIST
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats -->
        <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="calendar-days"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل دوره‌ها</p>
                        <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="circle-dot"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">باز</p>
                        <p class="mt-0.5 text-[20px] font-bold text-brand-600">
                            <asp:Literal ID="litStatOpen" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="lock"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">بسته‌شده (منتظر تقسیم)</p>
                        <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                            <asp:Literal ID="litStatClosed" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">تقسیم‌شده</p>
                        <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatDistributed" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>
        </div>

        <!-- Periods table -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
                <h2 class="text-[15px] font-bold text-ink-900">لیست دوره‌ها</h2>

                <div class="flex items-center gap-2">
                    <label class="text-[12.5px] text-ink-500">سال:</label>
                    <asp:TextBox ID="txtFilterYear" runat="server" MaxLength="4" dir="ltr"
                        AutoPostBack="true" OnTextChanged="txtFilterYear_TextChanged"
                        CssClass="h-9 w-[90px] rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                        placeholder="همه" />
                    <asp:LinkButton ID="btnClearFilter" runat="server" OnClick="btnClearFilter_Click"
                        CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line bg-surface-card px-3 text-[12.5px] font-semibold text-ink-700 hover:bg-surface-muted">
                        <i class="h-3.5 w-3.5" data-lucide="x"></i>
                        پاک کردن
                    </asp:LinkButton>
                </div>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1000px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">دوره</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                            <th class="pb-3 pr-4 text-center font-semibold">از تاریخ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تا تاریخ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">سود خالص</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptPeriods" runat="server" OnItemCommand="rptPeriods_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line transition hover:bg-surface-body/70">
                                    <td class="py-3 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetPeriodIconCss(Eval("Status")) %>'>
                                                <i class="h-4 w-4" data-lucide="calendar-days"></i>
                                            </span>
                                            <p class="font-mono font-bold text-ink-900 text-[13.5px]">
                                                <%# Enc(Eval("PeriodNumber")) %>
                                            </p>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetPeriodTypeBadge(Eval("PeriodType")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center font-mono text-ink-700">
                                        <%# FormatShamsiDate(Eval("PeriodFrom")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center font-mono text-ink-700">
                                        <%# FormatShamsiDate(Eval("PeriodTo")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetPeriodStatusBadge(Eval("Status")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetNetProfitCell(Eval("Status"), Eval("DistributionId"), Eval("NetProfit")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phClose" runat="server"
                                                Visible='<%# IsOpen(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnClose" runat="server"
                                                    CommandName="Close"
                                                    CommandArgument='<%# Eval("PeriodId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700"
                                                    OnClientClick='<%# GetCloseConfirm(Eval("PeriodNumber")) %>'>
                                                    <i class="h-3.5 w-3.5" data-lucide="lock"></i>
                                                    بستن دوره
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phDistribute" runat="server"
                                                Visible='<%# IsClosed(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnDistribute" runat="server"
                                                    CommandName="Distribute"
                                                    CommandArgument='<%# Eval("PeriodId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700">
                                                    <i class="h-3.5 w-3.5" data-lucide="calculator"></i>
                                                    محاسبه سود
                                                </asp:LinkButton>
                                                <asp:LinkButton ID="btnReopen" runat="server"
                                                    CommandName="Reopen"
                                                    CommandArgument='<%# Eval("PeriodId") %>'
                                                    CssClass="icon-button hover:bg-warning-50 hover:text-warning-600"
                                                    ToolTip="بازگشایی دوره"
                                                    OnClientClick='<%# GetReopenConfirm(Eval("PeriodNumber")) %>'>
                                                    <i class="h-4 w-4" data-lucide="lock-open"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phViewDist" runat="server"
                                                Visible='<%# IsDistributed(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnViewDist" runat="server"
                                                    CommandName="ViewDistribution"
                                                    CommandArgument='<%# Eval("DistributionId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base border border-surface-line bg-surface-card px-3 text-[12px] font-bold text-ink-700 hover:bg-surface-muted">
                                                    <i class="h-3.5 w-3.5" data-lucide="eye"></i>
                                                    مشاهده تقسیم
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phDelete" runat="server"
                                                Visible='<%# CanDeletePeriod(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="DeletePeriod"
                                                    CommandArgument='<%# Eval("PeriodId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick='<%# GetDeleteConfirm(Eval("PeriodNumber")) %>'>
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>
                                        </div>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlListEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="calendar-x"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">دوره‌ای یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">
                    از <a href="profit-settings.aspx" class="text-brand-600 hover:underline">تنظیمات تقسیم سود</a> دوره‌ها را تولید کنید.
                </p>
            </asp:Panel>

            <div class="mt-5">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>
        </section>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 2: PREVIEW DISTRIBUTION
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlPreviewView" runat="server" Visible="false">

        <!-- Period banner -->
        <div class="mb-5 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
            <div class="flex items-center gap-4">
                <span class="grid h-12 w-12 place-items-center rounded-xl bg-brand-600 text-white shadow-md">
                    <i class="h-6 w-6" data-lucide="calculator"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-semibold text-brand-700">پیش‌نمایش تقسیم سود</p>
                    <p class="mt-0.5 text-[18px] font-extrabold text-ink-900">
                        <asp:Literal ID="litPrevPeriodNumber" runat="server" />
                    </p>
                    <p class="mt-0.5 text-[12.5px] text-ink-600">
                        <asp:Literal ID="litPrevPeriodDates" runat="server" />
                    </p>
                </div>
                <span class="badge bg-brand-100 text-brand-700 text-[12px]">
                    <i class="h-3.5 w-3.5" data-lucide="eye"></i>
                    حالت پیش‌نمایش
                </span>
            </div>
        </div>

        <div class="grid gap-5 lg:grid-cols-3">

            <!-- Financial summary -->
            <section class="rounded-card border border-surface-line bg-surface-card shadow-card lg:col-span-2">

                <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-ink-800 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="file-spreadsheet"></i>
                    </span>
                    <h2 class="text-[14.5px] font-bold text-ink-900">صورت سود و زیان دوره</h2>
                    <span class="badge bg-surface-muted text-ink-500 mr-auto">ارقام به تومان</span>
                </div>

                <div class="py-1">
                    <div class="fin-row">
                        <span class="fin-label">درآمد فروش محصولات</span>
                        <span class="fin-value text-ink-900">
                            <asp:Literal ID="litPrevProductRevenue" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="fin-row">
                        <span class="fin-label" style="color: #2563eb;">درآمد خدمات</span>
                        <span class="fin-value" style="color: #2563eb; font-weight: 700;">
                            <asp:Literal ID="litPrevServiceRevenue" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="fin-row bg-brand-50/40 border-y border-brand-100">
                        <span class="fin-label font-bold text-ink-900">جمع درآمد کل</span>
                        <span class="fin-value text-ink-900 font-bold">
                            <asp:Literal ID="litPrevRevenue" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="fin-row">
                        <span class="fin-label">بهای تمام شده کالای فروش رفته</span>
                        <span class="fin-value text-danger-600">
                            (<asp:Literal ID="litPrevCogs" runat="server" Text="۰" />)
                        </span>
                    </div>
                    <div class="fin-row bg-surface-muted/60">
                        <span class="fin-label font-bold text-ink-900">سود ناخالص</span>
                        <span class="fin-value text-ink-900">
                            <asp:Literal ID="litPrevGrossProfit" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="fin-row">
                        <span class="fin-label">هزینه‌های عملیاتی</span>
                        <span class="fin-value text-danger-600">
                            (<asp:Literal ID="litPrevExpenses" runat="server" Text="۰" />)
                        </span>
                    </div>
                    <div class="fin-row bg-ink-800/5 border-y-2 border-ink-300">
                        <span class="fin-label font-bold text-ink-900">سود (زیان) خالص دوره</span>
                        <span class="fin-value text-ink-900">
                            <asp:Literal ID="litPrevNetProfit" runat="server" Text="۰" />
                        </span>
                    </div>
                    <asp:Panel ID="pnlLossCarry" runat="server" Visible="false">
                        <div class="fin-row">
                            <span class="fin-label text-amber-700">زیان منتقل‌شده از دوره‌های قبل</span>
                            <span class="fin-value text-amber-700">
                                (<asp:Literal ID="litPrevLossCarry" runat="server" Text="۰" />)
                            </span>
                        </div>
                    </asp:Panel>
                    <div class="fin-row bg-emerald-50/60 border-t-2 border-emerald-300">
                        <span class="fin-label font-bold text-emerald-800">سود قابل تقسیم (بعد از زیان)</span>
                        <span class="fin-value text-emerald-800">
                            <asp:Literal ID="litPrevAdjustedProfit" runat="server" Text="۰" />
                        </span>
                    </div>
                </div>

            </section>

            <!-- Reserve & distribution -->
            <section class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

                <h3 class="mb-4 flex items-center gap-2 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-amber-100 text-amber-700">
                        <i class="h-3.5 w-3.5" data-lucide="shield"></i>
                    </span>
                    ذخیره احتیاطی
                </h3>

                <label class="block">
                    <span class="text-[12.5px] font-semibold text-ink-700">درصد ذخیره</span>
                    <div class="relative mt-1.5">
                        <asp:TextBox ID="txtReservePercent" runat="server" MaxLength="6" dir="ltr"
                            AutoPostBack="true" OnTextChanged="txtReservePercent_TextChanged"
                            CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-10 text-[14px] font-mono focus:border-brand-600"
                            placeholder="0" />
                        <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                    </div>
                </label>

                <div class="mt-4 space-y-3">
                    <div class="flex items-center justify-between rounded-base bg-surface-muted px-3 py-2.5">
                        <span class="text-[12.5px] text-ink-600">مبلغ ذخیره</span>
                        <span class="font-mono text-[13.5px] font-bold text-amber-700">
                            <asp:Literal ID="litPrevReserveAmount" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between rounded-base bg-emerald-50 border border-emerald-200 px-3 py-2.5">
                        <span class="text-[12.5px] font-bold text-emerald-700">سود قابل توزیع</span>
                        <span class="font-mono text-[13.5px] font-bold text-emerald-700">
                            <asp:Literal ID="litPrevDistributable" runat="server" Text="۰" />
                        </span>
                    </div>
                </div>

                <div class="mt-4 rounded-base border border-surface-line bg-surface-body p-3 text-[11px] leading-relaxed text-ink-500">
                    <i class="inline h-3 w-3 align-middle ml-1" data-lucide="info"></i>
                    با تغییر درصد ذخیره، اعداد به‌روز می‌شوند.
                </div>

            </section>

        </div>

        <!-- Partner shares preview -->
        <section class="mt-5 rounded-card border border-surface-line bg-surface-card shadow-card">

            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="users"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">سهم شرکا</h2>
                <span class="badge bg-surface-muted text-ink-500 mr-auto">
                    <asp:Literal ID="litPrevPartnerCount" runat="server" Text="۰" />
                    شریک
                </span>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[900px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="px-4 py-3 text-right font-semibold">شریک</th>
                            <th class="px-4 py-3 text-center font-semibold">درصد مؤثر</th>
                            <th class="px-4 py-3 text-center font-semibold">سهم از سود</th>
                            <th class="px-4 py-3 text-center font-semibold">برداشت‌های باز</th>
                            <th class="px-4 py-3 text-center font-semibold">قابل پرداخت</th>
                            <th class="px-4 py-3 text-center font-semibold">برداشت باقی‌مانده</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptPreviewItems" runat="server">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line/60">
                                    <td class="px-4 py-3 font-semibold text-ink-900">
                                        <%# Enc(Eval("FullName")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center">
                                        <span class="badge bg-brand-50 text-brand-700 font-mono font-bold">
                                            <%# FormatPercent(Eval("EffectiveShare")) %>
                                        </span>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono font-bold text-emerald-700">
                                        <%# FormatMoney(Eval("ShareAmount")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono text-amber-700">
                                        <%# FormatMoney(Eval("OpenDrawings")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono font-bold text-ink-900 bg-emerald-50/40">
                                        <%# FormatMoney(Eval("NetPayable")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono text-ink-500 text-[12.5px]">
                                        <%# FormatMoneyOrDash(Eval("RemainingOpen")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlPreviewNoPartners" runat="server" Visible="false" CysClass="py-10 text-center">
                <p class="text-[13px] text-ink-400 text-center py-8">هیچ شریک فعالی با سهم در این دوره وجود ندارد.</p>
            </asp:Panel>

            <asp:Panel ID="pnlPreviewNoProfit" runat="server" Visible="false" CysClass="py-10 text-center">
                <div class="mx-auto max-w-lg rounded-base border border-amber-300 bg-amber-50 p-5 my-4">
                    <i class="h-10 w-10 text-amber-600 mx-auto mb-2" data-lucide="alert-triangle"></i>
                    <p class="text-[14px] font-bold text-amber-800">این دوره سود قابل تقسیمی ندارد</p>
                    <p class="mt-1 text-[12.5px] text-amber-700 leading-relaxed">
                        سود خالص دوره منفی یا کمتر از زیان منتقل‌شده است. با تأیید، زیان این دوره به دوره بعد منتقل می‌شود.
                    </p>
                </div>
            </asp:Panel>

            <!-- Company retained -->
            <asp:Panel ID="pnlCompanyRetained" runat="server" Visible="false"
                CssClass="border-t border-surface-line px-5 py-3 bg-violet-50/50">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-2">
                        <span class="grid h-6 w-6 place-items-center rounded-base bg-violet-100 text-violet-700">
                            <i class="h-3.5 w-3.5" data-lucide="building-2"></i>
                        </span>
                        <span class="text-[13px] font-bold text-violet-800">سود انباشته شرکت</span>
                    </div>
                    <span class="font-mono text-[14px] font-bold text-violet-800">
                        <asp:Literal ID="litPrevCompanyRetained" runat="server" Text="۰" />
                        <span class="text-[10.5px] text-violet-600">تومان</span>
                    </span>
                </div>
                <p class="mt-1 text-[11px] text-violet-600 leading-relaxed">
                    این مبلغ معادل سهمی است که در شرکت باقی می‌ماند (جمع سهام شرکا کمتر از ۱۰۰٪).
                </p>
            </asp:Panel>

        </section>

        <!-- Actions -->
        <div class="mt-5 flex flex-wrap items-center justify-between gap-3 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <p class="text-[12.5px] text-ink-500">
                <i class="inline h-4 w-4 align-middle ml-1" data-lucide="info"></i>
                با تأیید، سود بین شرکا تقسیم می‌شود و برداشت‌های علی‌الحساب به‌صورت خودکار کسر می‌شوند.
            </p>
            <div class="flex items-center gap-2">
                <asp:Button ID="btnCancelPreview" runat="server" Text="انصراف"
                    OnClick="btnCancelPreview_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnConfirmDistribute" runat="server"
                    Text="تأیید و ایجاد تقسیم"
                    OnClick="btnConfirmDistribute_Click"
                    OnClientClick="return confirm('آیا از ایجاد تقسیم سود مطمئن هستید؟ پس از تأیید، این عملیات قابل بازگشت نیست.');"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-emerald-600 px-5 text-[14px] font-semibold text-white hover:bg-emerald-700" />
            </div>
        </div>

    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 3: DISTRIBUTION DETAIL
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlDetailView" runat="server" Visible="false">

        <!-- Distribution header -->
        <div class="mb-5 rounded-card border border-emerald-100 bg-gradient-to-l from-emerald-50/70 to-transparent p-5">
            <div class="flex items-center gap-4">
                <span class="grid h-12 w-12 place-items-center rounded-xl bg-emerald-600 text-white shadow-md">
                    <i class="h-6 w-6" data-lucide="file-check-2"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-semibold text-emerald-700">سند تقسیم سود</p>
                    <p class="mt-0.5 text-[18px] font-extrabold text-ink-900 font-mono">
                        <asp:Literal ID="litDistNumber" runat="server" />
                    </p>
                    <p class="mt-0.5 text-[12.5px] text-ink-600">
                        <asp:Literal ID="litDistDates" runat="server" />
                    </p>
                </div>
                <asp:Literal ID="litDistStatus" runat="server" />
            </div>
        </div>

        <div class="grid gap-5 lg:grid-cols-3">

            <!-- Summary -->
            <section class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card lg:col-span-2">
                <h3 class="mb-4 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                    خلاصه مالی
                </h3>

                <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
                    <div class="rounded-base bg-surface-muted p-3">
                        <p class="text-[11.5px] text-ink-500">درآمد کل</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-ink-900">
                            <asp:Literal ID="litDistRevenue" runat="server" Text="۰" />
                        </p>
                    </div>
                    <div class="rounded-base bg-rose-50 p-3">
                        <p class="text-[11.5px] text-rose-700">COGS</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-rose-700">
                            <asp:Literal ID="litDistCogs" runat="server" Text="۰" />
                        </p>
                    </div>
                    <div class="rounded-base bg-surface-muted p-3">
                        <p class="text-[11.5px] text-ink-500">سود ناخالص</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-ink-900">
                            <asp:Literal ID="litDistGross" runat="server" Text="۰" />
                        </p>
                    </div>
                    <div class="rounded-base bg-amber-50 p-3">
                        <p class="text-[11.5px] text-amber-700">هزینه‌ها</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-amber-700">
                            <asp:Literal ID="litDistExpenses" runat="server" Text="۰" />
                        </p>
                    </div>
                    <div class="rounded-base bg-surface-muted p-3">
                        <p class="text-[11.5px] text-ink-500">سود خالص دوره</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-ink-900">
                            <asp:Literal ID="litDistNetProfit" runat="server" Text="۰" />
                        </p>
                    </div>
                    <div class="rounded-base bg-emerald-50 p-3">
                        <p class="text-[11.5px] text-emerald-700">سود قابل توزیع</p>
                        <p class="mt-1 font-mono text-[14px] font-bold text-emerald-700">
                            <asp:Literal ID="litDistDistributable" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>

                <!-- Extras -->
                <div class="mt-4 grid grid-cols-1 gap-2 sm:grid-cols-3 text-[12px]">
                    <div class="flex items-center justify-between rounded-base border border-surface-line px-3 py-2">
                        <span class="text-ink-500">ذخیره احتیاطی</span>
                        <span class="font-mono font-bold text-amber-700">
                            <asp:Literal ID="litDistReserve" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between rounded-base border border-surface-line px-3 py-2">
                        <span class="text-ink-500">برداشت‌های کسر‌شده</span>
                        <span class="font-mono font-bold text-brand-700">
                            <asp:Literal ID="litDistDrawingsApplied" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between rounded-base border border-surface-line px-3 py-2">
                        <span class="text-ink-500">سود انباشته شرکت</span>
                        <span class="font-mono font-bold text-violet-700">
                            <asp:Literal ID="litDistCompanyRetained" runat="server" Text="۰" />
                        </span>
                    </div>
                </div>

                <div class="mt-4 flex items-center gap-3 text-[11.5px] text-ink-500">
                    <span>
                        <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="calendar"></i>
                        تاریخ ایجاد:
                        <strong class="text-ink-700">
                            <asp:Literal ID="litDistCreatedAt" runat="server" />
                        </strong>
                    </span>
                    <asp:Literal ID="litDistFinalizedInfo" runat="server" />
                </div>
            </section>

            <!-- Payments summary -->
            <section class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <h3 class="mb-4 flex items-center gap-2 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-3.5 w-3.5" data-lucide="hand-coins"></i>
                    </span>
                    وضعیت پرداخت‌ها
                </h3>

                <div class="space-y-3">
                    <div class="flex items-center justify-between rounded-base bg-surface-muted px-3 py-2.5">
                        <span class="text-[12.5px] text-ink-600">جمع سهم شرکا</span>
                        <span class="font-mono text-[13.5px] font-bold text-ink-800">
                            <asp:Literal ID="litDistTotalShares" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between rounded-base bg-emerald-50 px-3 py-2.5 border border-emerald-200">
                        <span class="text-[12.5px] font-bold text-emerald-700">پرداخت‌شده</span>
                        <span class="font-mono text-[13.5px] font-bold text-emerald-700">
                            <asp:Literal ID="litDistTotalPaid" runat="server" Text="۰" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between rounded-base bg-amber-50 px-3 py-2.5 border border-amber-200">
                        <span class="text-[12.5px] font-bold text-amber-700">مانده قابل پرداخت</span>
                        <span class="font-mono text-[13.5px] font-bold text-amber-700">
                            <asp:Literal ID="litDistTotalRemaining" runat="server" Text="۰" />
                        </span>
                    </div>
                </div>
            </section>

        </div>

        <!-- Payment form (hidden by default) -->
        <asp:Panel ID="pnlPaymentForm" runat="server" Visible="false"
            CssClass="mt-5 rounded-card border-2 border-dashed border-brand-200 bg-brand-50/30 p-5">

            <div class="mb-4 flex items-center justify-between border-b border-brand-200/60 pb-3">
                <h3 class="flex items-center gap-2 text-[14px] font-bold text-ink-800">
                    <span class="grid h-7 w-7 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="hand-coins"></i>
                    </span>
                    ثبت پرداخت به شریک
                </h3>
                <asp:LinkButton ID="btnCancelPayment" runat="server" OnClick="btnCancelPayment_Click"
                    CssClass="text-[12px] font-semibold text-ink-500 hover:text-ink-700">
                    <i class="inline h-3 w-3" data-lucide="x"></i>
                    بستن
                </asp:LinkButton>
            </div>

            <div class="mb-4 rounded-base bg-surface-card border border-brand-200 p-3">
                <div class="grid grid-cols-2 gap-3 text-[12.5px]">
                    <div>
                        <span class="text-ink-500">شریک: </span>
                        <strong class="text-ink-800">
                            <asp:Literal ID="litPayPartnerName" runat="server" />
                        </strong>
                    </div>
                    <div>
                        <span class="text-ink-500">سهم کل: </span>
                        <strong class="font-mono text-ink-800">
                            <asp:Literal ID="litPayShareAmount" runat="server" />
                        </strong>
                    </div>
                    <div>
                        <span class="text-ink-500">پرداخت‌شده تاکنون: </span>
                        <strong class="font-mono text-emerald-700">
                            <asp:Literal ID="litPayAlreadyPaid" runat="server" />
                        </strong>
                    </div>
                    <div>
                        <span class="text-ink-500">مانده: </span>
                        <strong class="font-mono text-amber-700">
                            <asp:Literal ID="litPayRemaining" runat="server" />
                        </strong>
                    </div>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-4">
                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        مبلغ پرداخت (تومان) <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtPayAmount" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="0" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        تاریخ پرداخت <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtPayDate" runat="server" MaxLength="10" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="1403/04/15" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">روش پرداخت</span>
                    <asp:DropDownList ID="ddlPayMethod" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="" Text="— انتخاب —" />
                        <asp:ListItem Value="نقدی" Text="نقدی" />
                        <asp:ListItem Value="کارت بانکی" Text="کارت بانکی" />
                        <asp:ListItem Value="انتقال بانکی" Text="انتقال بانکی" />
                        <asp:ListItem Value="چک" Text="چک" />
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">شماره سند</span>
                    <asp:TextBox ID="txtPayRef" runat="server" MaxLength="100" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="اختیاری" />
                </label>
            </div>

            <div class="mt-4 flex items-center justify-end gap-3">
                <asp:Button ID="btnSavePayment" runat="server" Text="ثبت پرداخت"
                    OnClick="btnSavePayment_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </asp:Panel>

        <!-- Items table -->
        <section class="mt-5 rounded-card border border-surface-line bg-surface-card shadow-card">

            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="users"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">سهم شرکا و پرداخت‌ها</h2>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1000px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="px-4 py-3 text-right font-semibold">شریک</th>
                            <th class="px-4 py-3 text-center font-semibold">درصد سهم</th>
                            <th class="px-4 py-3 text-center font-semibold">سهم از سود</th>
                            <th class="px-4 py-3 text-center font-semibold">پرداخت‌شده</th>
                            <th class="px-4 py-3 text-center font-semibold">مانده</th>
                            <th class="px-4 py-3 text-center font-semibold">وضعیت</th>
                            <th class="px-4 py-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptItems" runat="server" OnItemCommand="rptItems_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="px-4 py-3">
                                        <p class="font-semibold text-ink-900">
                                            <%# Enc(Eval("FullName")) %>
                                        </p>
                                        <%# GetPaymentDetailHint(Eval("PaymentMethod"), Eval("ReferenceNumber"), Eval("PaidAt")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center">
                                        <span class="badge bg-brand-50 text-brand-700 font-mono font-bold">
                                            <%# FormatPercent(Eval("SharePercent")) %>
                                        </span>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono font-bold text-ink-900">
                                        <%# FormatMoney(Eval("ShareAmount")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono text-emerald-700">
                                        <%# FormatMoney(Eval("PaidAmount")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center font-mono font-bold text-amber-700">
                                        <%# FormatMoney(Eval("Remaining")) %>
                                    </td>
                                    <td class="px-4 py-3 text-center">
                                        <%# GetItemStatusBadge(Eval("ShareAmount"), Eval("PaidAmount")) %>
                                    </td>
                                    <td class="px-4 py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phPay" runat="server"
                                                Visible='<%# CanRecordPayment(Eval("ShareAmount"), Eval("PaidAmount")) %>'>
                                                <asp:LinkButton ID="btnPay" runat="server"
                                                    CommandName="Pay"
                                                    CommandArgument='<%# Eval("ItemId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700">
                                                    <i class="h-3.5 w-3.5" data-lucide="hand-coins"></i>
                                                    ثبت پرداخت
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phPaid" runat="server"
                                                Visible='<%# IsFullyPaid(Eval("ShareAmount"), Eval("PaidAmount")) %>'>
                                                <span class="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-50 px-3 text-[12px] font-bold text-emerald-700">
                                                    <i class="h-3.5 w-3.5" data-lucide="check-circle-2"></i>
                                                    تسویه‌شده
                                                </span>
                                            </asp:PlaceHolder>
                                        </div>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlItemsEmpty" runat="server" Visible="false" CysClass="py-10 text-center">
                <p class="text-[13px] text-ink-400 text-center py-8">در این تقسیم، سهمی برای شرکا ثبت نشده است.</p>
            </asp:Panel>
        </section>

        <!-- Action bar -->
        <div class="mt-5 flex flex-wrap items-center justify-between gap-3 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
            <p class="text-[12.5px] text-ink-500">
                <i class="inline h-4 w-4 align-middle ml-1" data-lucide="shield-check"></i>
                این سند نهایی شده و قابل ویرایش نیست. پرداخت‌ها در این صفحه ثبت می‌شوند.
            </p>
            <asp:LinkButton ID="btnBackFromDetail" runat="server" OnClick="btnBackToList_Click"
                CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="arrow-right"></i>
                بازگشت به لیست دوره‌ها
            </asp:LinkButton>
        </div>

    </asp:Panel>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
</script>
</asp:Content>