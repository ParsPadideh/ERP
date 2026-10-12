<%@ Page Title="صورت‌حساب شریک" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="partner-statement.aspx.cs" Inherits="admin_partner_statement" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .ledger-table { width: 100%; border-collapse: collapse; }
    .ledger-table th {
        padding: 10px 12px;
        font-size: 11.5px;
        font-weight: 700;
        text-transform: uppercase;
        color: #64748b;
        text-align: center;
        border-bottom: 2px solid #e2e8f0;
        background: #f8fafc;
    }
    .ledger-table th.right { text-align: right; }
    .ledger-table td {
        padding: 10px 12px;
        font-size: 13px;
        border-bottom: 1px solid #f1f5f9;
        vertical-align: middle;
    }
    .ledger-table tr:hover td { background: #f8fafc; }
    .ledger-table tr.opening-row td {
        background: #fffbeb;
        border-top: 2px solid #fcd34d;
        border-bottom: 2px solid #fcd34d;
        font-weight: 700;
        color: #78350f;
    }
    .ledger-table tr.closing-row td {
        background: #eff6ff;
        border-top: 2px solid #93c5fd;
        border-bottom: 3px double #1e40af;
        font-weight: 800;
        font-size: 14px;
        color: #1e3a8a;
    }
    .ledger-table .amount {
        font-family: 'Vazirmatn', monospace;
        text-align: left;
        direction: ltr;
        font-weight: 700;
    }
    .ledger-table .amount.debit { color: #dc2626; }
    .ledger-table .amount.credit { color: #059669; }
    .ledger-table .amount.balance-neg { color: #dc2626; }
    .ledger-table .amount.balance-pos { color: #1e40af; }
    .ledger-table .amount.balance-zero { color: #94a3b8; }

    /* ═════ Print ═════ */
    .print-header, .print-footer-print-only { display: none; }

    @media print {
        @page { size: A4 portrait; margin: 12mm 10mm 15mm 10mm; }
        html, body { background: #fff !important; font-size: 10.5px !important; }
        .admin-sidebar, .admin-topbar, footer, .no-print, .sidebar-overlay { display: none !important; }
        [class*="lg:mr-"] { margin-right: 0 !important; }
        main.admin-content { padding: 0 !important; margin: 0 !important; }
        .print-header, .print-footer-print-only { display: block !important; }
        .print-card, .rounded-card {
            border: 1px solid #cbd5e1 !important;
            border-radius: 0 !important;
            box-shadow: none !important;
            background: #fff !important;
            margin-bottom: 8px !important;
        }
        .shadow-card { box-shadow: none !important; }
        table { page-break-inside: auto; }
        tr { page-break-inside: avoid; }
        thead { display: table-header-group; }
        .ledger-table th { background: #f1f5f9 !important; }
        .ledger-table td { padding: 5px 8px !important; font-size: 10px !important; }
        .ledger-table th { padding: 5px 8px !important; font-size: 9.5px !important; }
        .stat-card { page-break-inside: avoid; }
        .print-header-inner {
            border-bottom: 3px double #1e40af;
            padding-bottom: 10px;
            margin-bottom: 14px;
        }
        .print-header-inner .company-name { font-size: 18px; font-weight: 800; color: #1e3a8a; }
        .print-header-inner .company-meta { font-size: 10px; color: #555; margin-top: 3px; }
        .print-header-inner .report-title { font-size: 15px; font-weight: 800; color: #0f172a; text-align: center; margin-top: 8px; }
        .print-header-inner .report-period { font-size: 11px; text-align: center; color: #475569; margin-top: 3px; }
        .print-footer-print-only {
            margin-top: 24px;
            padding-top: 14px;
            border-top: 1px solid #cbd5e1;
            page-break-inside: avoid;
        }
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfPartnerId" runat="server" Value="0" />

    <!-- ═══════════════════════════════════════════════════════
         PRINT HEADER
         ═══════════════════════════════════════════════════════ -->
    <div class="print-header">
        <div class="print-header-inner">
            <table style="width:100%; border:0;">
                <tr>
                    <td style="width:70px; vertical-align:top; border:0;">
                        <asp:Image ID="imgPrintLogo" runat="server" Visible="false"
                            style="height:60px; max-width:70px; object-fit:contain;" />
                    </td>
                    <td style="vertical-align:top; border:0;">
                        <div class="company-name">
                            <asp:Literal ID="litCompanyName" runat="server" Text="کارخانه تولید و بسته‌بندی مواد غذایی" />
                        </div>
                        <div class="company-meta">
                            <asp:Literal ID="litCompanyMeta" runat="server" />
                        </div>
                    </td>
                    <td style="vertical-align:top; text-align:left; border:0; font-size:10px; color:#555;">
                        <div>تاریخ چاپ:</div>
                        <div style="font-weight:700; margin-top:2px;">
                            <asp:Literal ID="litPrintDate" runat="server" />
                        </div>
                    </td>
                </tr>
            </table>
            <div class="report-title">صورت‌حساب شریک</div>
            <div class="report-period">
                <asp:Literal ID="litPrintPeriod" runat="server" />
            </div>
        </div>
    </div>

    <!-- ═══════════ PAGE HEADER ═══════════ -->
    <div class="no-print mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <a class="hover:text-brand-600" href="partners.aspx">شرکا</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">صورت‌حساب شریک</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">صورت‌حساب مالی شریک</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                سابقه کامل سهم از سود، برداشت‌ها، و پرداخت‌های نقدی.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="partners.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="users-round"></i>
                مدیریت شرکا
            </a>
            <asp:LinkButton ID="btnPrint" runat="server" OnClientClick="window.print(); return false;"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="printer"></i>
                چاپ صورت‌حساب
            </asp:LinkButton>
        </div>
    </div>

    <!-- ═══════════ FILTERS ═══════════ -->
    <section class="no-print mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

        <div class="mb-3 flex flex-wrap items-center gap-2">
            <span class="text-[12.5px] font-bold text-ink-600 ml-1">بازه سریع:</span>
            <asp:LinkButton ID="btnRangeYear" runat="server" CommandArgument="year"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">امسال</asp:LinkButton>
            <asp:LinkButton ID="btnRange6M" runat="server" CommandArgument="6m"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">۶ ماه اخیر</asp:LinkButton>
            <asp:LinkButton ID="btnRange30" runat="server" CommandArgument="30d"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">۳۰ روز اخیر</asp:LinkButton>
            <asp:LinkButton ID="btnRangeAll" runat="server" CommandArgument="all"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">همه</asp:LinkButton>
        </div>

        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-5">

            <label class="block lg:col-span-2">
                <span class="text-[12.5px] font-semibold text-ink-700">شریک</span>
                <asp:DropDownList ID="ddlPartner" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlPartner_SelectedIndexChanged"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                </asp:DropDownList>
            </label>

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">از تاریخ</span>
                <asp:TextBox ID="txtFrom" runat="server" MaxLength="10" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                    placeholder="1403/01/01" />
            </label>

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">تا تاریخ</span>
                <asp:TextBox ID="txtTo" runat="server" MaxLength="10" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                    placeholder="1403/12/29" />
            </label>

            <div class="flex items-end">
                <asp:Button ID="btnApply" runat="server" Text="اعمال" OnClick="btnApply_Click"
                    CssClass="h-10 w-full rounded-base bg-brand-600 px-4 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
            </div>

        </div>

    </section>

    <!-- ═══════════ PARTNER BANNER ═══════════ -->
    <asp:Panel ID="pnlPartnerBanner" runat="server" Visible="false"
        CssClass="mb-5 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
        <div class="flex flex-wrap items-center gap-4">
            <span class="grid h-14 w-14 place-items-center rounded-xl bg-brand-600 text-white shadow-md">
                <i class="h-7 w-7" data-lucide="user-round"></i>
            </span>
            <div class="min-w-0 flex-1">
                <p class="text-[12px] font-semibold text-brand-700">صورت‌حساب شریک</p>
                <p class="mt-0.5 text-[18px] font-extrabold text-ink-900">
                    <asp:Literal ID="litPartnerName" runat="server" />
                </p>
                <div class="mt-1 flex flex-wrap items-center gap-2">
                    <asp:Literal ID="litPartnerShare" runat="server" />
                    <asp:Literal ID="litPartnerStatus" runat="server" />
                    <asp:Literal ID="litPartnerContact" runat="server" />
                </div>
            </div>
            <div class="text-left">
                <p class="text-[11.5px] text-ink-500">درصد سهام فعلی</p>
                <p class="mt-1 font-mono text-[22px] font-black text-brand-700">
                    <asp:Literal ID="litCurrentSharePercent" runat="server" Text="—" />
                </p>
            </div>
        </div>
    </asp:Panel>

    <!-- ═══════════ SUMMARY CARDS ═══════════ -->
    <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
            <div class="flex items-center gap-3 mb-2">
                <span class="grid h-9 w-9 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-4 w-4" data-lucide="trending-up"></i>
                </span>
                <p class="text-[12px] font-semibold text-ink-500">جمع سهم از تقسیم‌ها</p>
            </div>
            <p class="font-mono text-[18px] font-black text-emerald-700">
                <asp:Literal ID="litSumEarned" runat="server" Text="۰" />
            </p>
            <p class="mt-1 text-[11px] text-ink-400">
                <asp:Literal ID="litSumEarnedHint" runat="server" Text="—" />
            </p>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
            <div class="flex items-center gap-3 mb-2">
                <span class="grid h-9 w-9 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-4 w-4" data-lucide="hand-coins"></i>
                </span>
                <p class="text-[12px] font-semibold text-ink-500">جمع برداشت علی‌الحساب</p>
            </div>
            <p class="font-mono text-[18px] font-black text-amber-700">
                <asp:Literal ID="litSumDrawings" runat="server" Text="۰" />
            </p>
            <p class="mt-1 text-[11px] text-ink-400">
                <asp:Literal ID="litSumDrawingsHint" runat="server" Text="—" />
            </p>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
            <div class="flex items-center gap-3 mb-2">
                <span class="grid h-9 w-9 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-4 w-4" data-lucide="banknote"></i>
                </span>
                <p class="text-[12px] font-semibold text-ink-500">جمع پرداخت نقدی</p>
            </div>
            <p class="font-mono text-[18px] font-black text-brand-700">
                <asp:Literal ID="litSumPaid" runat="server" Text="۰" />
            </p>
            <p class="mt-1 text-[11px] text-ink-400">
                <asp:Literal ID="litSumPaidHint" runat="server" Text="—" />
            </p>
        </article>

        <article id="cardBalance" runat="server"
            class="rounded-card border-2 border-surface-line bg-surface-card p-5 shadow-card print-card">
            <div class="flex items-center gap-3 mb-2">
                <span class="grid h-9 w-9 place-items-center rounded-base bg-violet-50 text-violet-600">
                    <i class="h-4 w-4" data-lucide="scale"></i>
                </span>
                <p class="text-[12px] font-semibold text-ink-500">مانده فعلی</p>
            </div>
            <p id="pBalance" runat="server" class="font-mono text-[18px] font-black">
                <asp:Literal ID="litBalance" runat="server" Text="۰" />
            </p>
            <p class="mt-1 text-[11px] text-ink-400">
                <asp:Literal ID="litBalanceHint" runat="server" Text="—" />
            </p>
        </article>

    </div>

    <!-- ═══════════ LEDGER ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card shadow-card print-card">

        <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-ink-800 text-white">
                <i class="h-3.5 w-3.5" data-lucide="list"></i>
            </span>
            <h2 class="text-[14.5px] font-bold text-ink-900">دفتر گردش مالی</h2>
            <span class="badge bg-surface-muted text-ink-500 mr-auto">ارقام به تومان</span>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <table class="ledger-table">
                <thead>
                    <tr>
                        <th class="right" style="width:110px;">تاریخ</th>
                        <th class="right">شرح</th>
                        <th style="width:110px;">مرجع</th>
                        <th style="width:140px;">بدهکار (دریافت)</th>
                        <th style="width:140px;">بستانکار (سهم)</th>
                        <th style="width:140px;">مانده</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Repeater ID="rptLedger" runat="server">
                        <ItemTemplate>
                            <tr class='<%# Eval("RowCss") %>'>
                                <td class="font-mono text-[12px] text-ink-700" style="text-align:center;">
                                    <%# Eval("EventDate") %>
                                </td>
                                <td>
                                    <div class="flex items-center gap-2">
                                        <%# Eval("IconHtml") %>
                                        <div class="min-w-0">
                                            <p class="text-[12.5px] font-semibold text-ink-800">
                                                <%# Eval("Description") %>
                                            </p>
                                            <%# Eval("SubHintHtml") %>
                                        </div>
                                    </div>
                                </td>
                                <td class="font-mono text-[11.5px] text-ink-500" style="text-align:center;" dir="ltr">
                                    <%# Eval("Reference") %>
                                </td>
                                <td class="amount debit">
                                    <%# Eval("DebitHtml") %>
                                </td>
                                <td class="amount credit">
                                    <%# Eval("CreditHtml") %>
                                </td>
                                <td class='<%# "amount " + Eval("BalanceCss") %>'>
                                    <%# Eval("BalanceHtml") %>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>

        <asp:Panel ID="pnlLedgerEmpty" runat="server" Visible="false" CysClass="py-14 text-center">
            <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                <i class="h-6 w-6" data-lucide="inbox"></i>
            </span>
            <p class="text-[14px] font-semibold text-ink-600">هیچ گردشی در این بازه ثبت نشده است.</p>
            <p class="mt-1 text-[12.5px] text-ink-400">بازه را تغییر دهید یا از «همه» استفاده کنید.</p>
        </asp:Panel>

        <div class="border-t border-surface-line bg-surface-body px-5 py-3 flex items-center justify-between">
            <p class="text-[12.5px] text-ink-500">
                <asp:Literal ID="litLedgerCount" runat="server" />
            </p>
         <p class="text-[12.5px] text-ink-500">
    مانده نهایی:
    <strong id="litClosingBalance" runat="server"
        class="font-mono font-bold text-ink-900 mr-1">۰</strong>
</p>
        </div>

    </section>

    <!-- ═══════════ PRINT SIGNATURES ═══════════ -->
    <div class="print-footer-print-only">
        <table style="width:100%; border:0;">
            <tr>
                <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                    <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:70%; margin:0 auto;">
                        امضای شریک
                    </div>
                </td>
                <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                    <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:70%; margin:0 auto;">
                        مدیر مالی
                    </div>
                </td>
                <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                    <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:70%; margin:0 auto;">
                        مهر و امضای شرکت
                    </div>
                </td>
            </tr>
        </table>
        <p style="text-align:center; font-size:9.5px; color:#94a3b8; margin-top:14px;">
            این صورت‌حساب توسط سامانه ERP تولید شده است.
        </p>
    </div>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
</script>
</asp:Content>