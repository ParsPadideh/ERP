<%@ Page Title="تنظیمات تقسیم سود" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="profit-settings.aspx.cs" Inherits="admin_profit_settings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- Hidden fields -->
    <asp:HiddenField ID="hfFilterYear" runat="server" Value="1403" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">تنظیمات تقسیم سود</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">تنظیمات تقسیم سود شرکا</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                دوره‌های تقسیم سود را تعریف کنید و پارامترهای پیش‌فرض را تنظیم نمایید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="partners.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="users-round"></i>
                مدیریت شرکا
            </a>
            <a href="profit-periods.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="calendar-check"></i>
                مدیریت دوره‌ها
            </a>
        </div>
    </div>

    <!-- ═══════════ Stats ═══════════ -->
    <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="calendar-range"></i>
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
                    <p class="text-[12px] text-ink-500">دوره‌های باز</p>
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

    <!-- ═══════════ Settings form ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
            <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                <i class="h-5 w-5" data-lucide="settings-2"></i>
            </span>
            <div>
                <h2 class="text-[16px] font-semibold text-ink-900">پارامترهای پیش‌فرض تقسیم سود</h2>
                <p class="mt-0.5 text-[12px] text-ink-500">
                    این تنظیمات روی محاسبه سهم شرکا و اجازه‌ی برداشت اثر می‌گذارد.
                </p>
            </div>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">
                    شروع سال مالی (شمسی) <span class="text-danger-500">*</span>
                </span>
                <asp:TextBox ID="txtFiscalYearStart" runat="server" MaxLength="10" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                    placeholder="1403/01/01" />
                <p class="mt-1 text-[11px] text-ink-400">معمولاً اول فروردین. اگر متفاوت است وارد کنید.</p>
            </label>

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">
                    سال مالی جاری <span class="text-danger-500">*</span>
                </span>
                <asp:TextBox ID="txtCurrentFiscalYear" runat="server" MaxLength="4" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                    placeholder="1403" />
                <p class="mt-1 text-[11px] text-ink-400">سال شمسی که در حال حاضر در آن هستیم.</p>
            </label>

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">
                    نوع دوره پیش‌فرض <span class="text-danger-500">*</span>
                </span>
                <asp:DropDownList ID="ddlDefaultPeriodType" runat="server"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="1" Text="ماهانه (۱۲ دوره در سال)" />
                    <asp:ListItem Value="2" Text="سه‌ماهه (۴ دوره در سال)" />
                    <asp:ListItem Value="3" Text="شش‌ماهه (۲ دوره در سال)" />
                    <asp:ListItem Value="4" Text="سالانه (۱ دوره در سال)" />
                </asp:DropDownList>
            </label>

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">حداکثر درصد قابل برداشت</span>
                <div class="relative mt-1.5">
                    <asp:TextBox ID="txtMaxAdvancePercent" runat="server" MaxLength="6" dir="ltr"
                        CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-10 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="80" />
                    <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                </div>
                <p class="mt-1 text-[11px] text-ink-400">سقف امنیتی برداشت از سهم برآوردی (۰ تا ۱۰۰).</p>
            </label>

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">ذخیره احتیاطی پیش‌فرض</span>
                <div class="relative mt-1.5">
                    <asp:TextBox ID="txtMinReservePercent" runat="server" MaxLength="6" dir="ltr"
                        CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-10 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="0" />
                    <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                </div>
                <p class="mt-1 text-[11px] text-ink-400">درصدی از سود که قبل از تقسیم کنار گذاشته می‌شود.</p>
            </label>

            <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer">
                <asp:CheckBox ID="chkEnableLossCarryForward" runat="server" Checked="true"
                    CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                <div>
                    <p class="text-[13px] font-bold text-ink-800">انتقال زیان به دوره بعد</p>
                    <p class="mt-0.5 text-[11px] text-ink-500 leading-relaxed">
                        اگر دوره‌ای زیان داشته باشد، زیان از سود دوره بعد کسر می‌شود.
                    </p>
                </div>
            </label>

        </div>

        <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
            <asp:LinkButton ID="btnResetSettings" runat="server" OnClick="btnResetSettings_Click"
                OnClientClick="return confirm('تنظیمات به حالت پیش‌فرض بازگردانده شود؟');"
                CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="rotate-ccw"></i>
                پیش‌فرض‌ها
            </asp:LinkButton>

            <asp:Button ID="btnSaveSettings" runat="server" Text="ذخیره تنظیمات"
                OnClick="btnSaveSettings_Click"
                CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
        </div>

    </section>

    <!-- ═══════════ Period generator ═══════════ -->
    <section class="mb-5 rounded-card border-2 border-dashed border-brand-200 bg-brand-50/30 p-6">

        <div class="mb-5 flex items-center gap-3">
            <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-600 text-white">
                <i class="h-5 w-5" data-lucide="wand-2"></i>
            </span>
            <div>
                <h2 class="text-[16px] font-semibold text-ink-900">تولید خودکار دوره‌ها</h2>
                <p class="mt-0.5 text-[12px] text-ink-500">
                    برای یک سال شمسی، دوره‌های تقسیم سود را خودکار بسازید.
                </p>
            </div>
        </div>

        <div class="grid gap-4 md:grid-cols-3">

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">
                    سال مالی (شمسی) <span class="text-danger-500">*</span>
                </span>
                <asp:TextBox ID="txtGenerateYear" runat="server" MaxLength="4" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                    placeholder="1404" />
            </label>

            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">
                    نوع دوره <span class="text-danger-500">*</span>
                </span>
                <asp:DropDownList ID="ddlGenerateType" runat="server"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="1" Text="ماهانه (۱۲ دوره)" />
                    <asp:ListItem Value="2" Text="سه‌ماهه (۴ دوره)" Selected="True" />
                    <asp:ListItem Value="3" Text="شش‌ماهه (۲ دوره)" />
                    <asp:ListItem Value="4" Text="سالانه (۱ دوره)" />
                </asp:DropDownList>
            </label>

            <div class="flex items-end">
                <asp:Button ID="btnGenerate" runat="server" Text="تولید دوره‌ها"
                    OnClick="btnGenerate_Click"
                    CssClass="h-10 w-full rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>

        </div>

        <asp:Panel ID="pnlGenerateHint" runat="server" Visible="true"
            CssClass="mt-4 rounded-base bg-surface-card border border-brand-200 p-3 text-[12px] text-ink-600 leading-relaxed">
            <i class="inline h-3.5 w-3.5 align-middle ml-1 text-brand-600" data-lucide="info"></i>
            اگر برای سال انتخابی قبلاً دوره‌ای وجود داشته باشد:
            <ul class="mt-1.5 list-disc pr-5 space-y-0.5">
                <li>دوره‌های <strong>باز (Open)</strong> یا <strong>بسته‌شده</strong> حذف و از نو ساخته می‌شوند.</li>
                <li>دوره‌های <strong>تقسیم‌شده</strong> حفظ می‌شوند و عملیات متوقف می‌شود.</li>
            </ul>
        </asp:Panel>

    </section>

    <!-- ═══════════ Existing periods ═══════════ -->
    <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

        <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
            <h2 class="text-[15px] font-bold text-ink-900">دوره‌های تعریف‌شده</h2>

            <div class="flex items-center gap-2">
                <label class="text-[12.5px] text-ink-500">سال:</label>
                <asp:TextBox ID="txtFilterYear" runat="server" MaxLength="4" dir="ltr"
                    AutoPostBack="true" OnTextChanged="txtFilterYear_TextChanged"
                    CssClass="h-9 w-[90px] rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                    placeholder="1403" />
                <asp:LinkButton ID="btnClearFilter" runat="server" OnClick="btnClearFilter_Click"
                    CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line bg-surface-card px-3 text-[12.5px] font-semibold text-ink-700 hover:bg-surface-muted">
                    همه سال‌ها
                </asp:LinkButton>
            </div>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <table class="w-full min-w-[900px] text-right">
                <thead>
                    <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                        <th class="pb-3 pr-4 text-right font-semibold">شماره دوره</th>
                        <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                        <th class="pb-3 pr-4 text-center font-semibold">از تاریخ</th>
                        <th class="pb-3 pr-4 text-center font-semibold">تا تاریخ</th>
                        <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                        <th class="pb-3 pr-4 text-center font-semibold">تقسیم مرتبط</th>
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
                                        <div>
                                            <p class="font-mono font-bold text-ink-900 text-[13.5px]">
                                                <%# Enc(Eval("PeriodNumber")) %>
                                            </p>
                                            <%# GetPeriodNotes(Eval("Notes")) %>
                                        </div>
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
                                    <%# GetDistributionLink(Eval("DistributionId"), Eval("DistributionNumber")) %>
                                </td>
                                <td class="py-3 text-left">
                                    <div class="inline-flex items-center gap-1">
                                        <asp:PlaceHolder ID="phDelete" runat="server"
                                            Visible='<%# CanDeletePeriod(Eval("Status")) %>'>
                                            <asp:LinkButton ID="btnDeletePeriod" runat="server"
                                                CommandName="DeletePeriod"
                                                CommandArgument='<%# Eval("PeriodId") %>'
                                                CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                ToolTip="حذف دوره"
                                                OnClientClick='<%# GetDeletePeriodConfirm(Eval("PeriodNumber")) %>'>
                                                <i class="h-4 w-4" data-lucide="trash-2"></i>
                                            </asp:LinkButton>
                                        </asp:PlaceHolder>

                                        <asp:PlaceHolder ID="phLocked" runat="server"
                                            Visible='<%# IsLocked(Eval("Status")) %>'>
                                            <span class="icon-button text-ink-300 cursor-not-allowed"
                                                title="دوره تقسیم‌شده قابل حذف نیست">
                                                <i class="h-4 w-4" data-lucide="lock"></i>
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

        <asp:Panel ID="pnlPeriodsEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
            <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                <i class="h-6 w-6" data-lucide="calendar-x"></i>
            </span>
            <p class="text-[14px] font-semibold text-ink-600">هیچ دوره‌ای تعریف نشده است.</p>
            <p class="mt-1 text-[12.5px] text-ink-400">
                از بخش «تولید خودکار دوره‌ها» استفاده کنید.
            </p>
        </asp:Panel>

        <div class="mt-5 flex items-center justify-between">
            <p class="text-[13px] text-ink-500">
                <asp:Literal ID="litPeriodsInfo" runat="server" />
            </p>
        </div>

    </section>

</asp:Content>