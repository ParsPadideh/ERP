<%@ Page Title="گزارش سود و زیان" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="profit-loss.aspx.cs" Inherits="admin_profit_loss" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    /* ═══════════ Screen Styles ═══════════ */
    .statement-table { width: 100%; border-collapse: collapse; }
    .statement-table td { padding: 9px 14px; font-size: 13.5px; vertical-align: middle; }
    .statement-table td.amount { text-align: left; font-family: 'Vazirmatn', monospace; direction: ltr; font-weight: 600; }
    .statement-table td.percent { text-align: center; width: 90px; font-size: 12px; color: #64748b; }
    .statement-table tr.row-main { border-bottom: 1px solid #eef2f6; }
    .statement-table tr.row-main:hover { background: #f8fafc; }
    .statement-table tr.row-section-header { background: #f1f5f9; font-weight: 700; color: #0f172a; }
    .statement-table tr.row-section-header td { padding: 10px 14px; font-size: 13px; }
    .statement-table tr.row-sub { border-bottom: 1px solid #eef2f6; }
    .statement-table tr.row-sub td.title { padding-right: 30px; color: #475569; }
    .statement-table tr.row-total {
        background: linear-gradient(90deg, #f1f5f9, #f8fafc);
        border-top: 2px solid #334155;
        border-bottom: 2px solid #334155;
        font-weight: 800; font-size: 14.5px;
    }
    .statement-table tr.row-grand-total {
        background: linear-gradient(90deg, #eff6ff, #dbeafe);
        border-top: 3px double #1e40af;
        border-bottom: 3px double #1e40af;
        font-weight: 900; font-size: 15.5px;
        color: #1e3a8a;
    }
    .amount-negative { color: #dc2626; }
    .amount-positive { color: #059669; }
    .amount-zero { color: #94a3b8; }

    /* ═══════════ Print Styles ═══════════ */
    .print-header, .print-footer-print-only { display: none; }

    @media print {
        @page {
            size: A4 portrait;
            margin: 12mm 10mm 15mm 10mm;
        }

        html, body {
            background: #fff !important;
            font-size: 10.5px !important;
            color: #000 !important;
        }

        .admin-sidebar,
        .admin-topbar,
        footer,
        .no-print,
        .sidebar-overlay { display: none !important; }

        [class*="lg:mr-"] { margin-right: 0 !important; }
        .lg\:mr-\[272px\] { margin-right: 0 !important; }
        main.admin-content { padding: 0 !important; margin: 0 !important; }

        .print-header { display: block !important; }
        .print-footer-print-only { display: block !important; }

        .print-card,
        .rounded-card {
            border: 1px solid #cbd5e1 !important;
            border-radius: 0 !important;
            box-shadow: none !important;
            background: #fff !important;
            margin-bottom: 8px !important;
            page-break-inside: avoid;
        }

        .shadow-card, .shadow-card-lg { box-shadow: none !important; }

        table { page-break-inside: auto; border-collapse: collapse; }
        tr { page-break-inside: avoid; page-break-after: auto; }
        thead { display: table-header-group; }
        tfoot { display: table-footer-group; }

        #reportArea { width: 100% !important; max-width: 100% !important; }

        h1, h2, h3 { color: #000 !important; }
        .text-ink-900, .text-ink-800 { color: #000 !important; }
        .text-ink-500, .text-ink-400 { color: #555 !important; }
        .text-brand-600, .text-brand-700 { color: #1e40af !important; }

        .stat-card { page-break-inside: avoid; }
        .stat-value { font-size: 13px !important; }

        .statement-table td { padding: 5px 8px !important; font-size: 10px !important; }
        .statement-table tr.row-grand-total { font-size: 11.5px !important; }

        .page-break-before { page-break-before: always; }
        .avoid-break { page-break-inside: avoid; }

        .print-header-inner {
            border-bottom: 3px double #1e40af;
            padding-bottom: 10px;
            margin-bottom: 14px;
        }
        .print-header-inner .company-name {
            font-size: 18px;
            font-weight: 800;
            color: #1e3a8a;
        }
        .print-header-inner .company-meta {
            font-size: 10px;
            color: #555;
            margin-top: 3px;
        }
        .print-header-inner .report-title {
            font-size: 15px;
            font-weight: 800;
            color: #0f172a;
            text-align: center;
            margin-top: 8px;
        }
        .print-header-inner .report-period {
            font-size: 11px;
            text-align: center;
            color: #475569;
            margin-top: 3px;
        }

        .print-footer-print-only {
            margin-top: 24px;
            padding-top: 14px;
            border-top: 1px solid #cbd5e1;
            page-break-inside: avoid;
        }
        .signature-grid {
            display: flex;
            justify-content: space-between;
            gap: 20px;
        }
        .signature-box {
            flex: 1;
            text-align: center;
            font-size: 10.5px;
            color: #475569;
            padding-top: 40px;
            border-top: 1px dotted #94a3b8;
        }
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════════════════════════════════════════════════
         PRINT-ONLY HEADER (Company info)
         ═══════════════════════════════════════════════════════ -->
    <div class="print-header">
        <div class="print-header-inner">
            <table style="width:100%; border:0; border-collapse:collapse;">
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
            <div class="report-title">گزارش صورت سود و زیان</div>
            <div class="report-period">
                <asp:Literal ID="litPrintPeriod" runat="server" />
            </div>
        </div>
    </div>

    <!-- ═══════════ PAGE HEADER (no-print) ═══════════ -->
    <div class="no-print mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">گزارش سود و زیان</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">گزارش صورت سود و زیان</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                تحلیل جامع درآمد (فروش + خدمات)، بهای تمام شده، هزینه‌ها و سودآوری.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:LinkButton ID="btnRefresh" runat="server" OnClick="btnRefresh_Click"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="refresh-cw"></i>
                بروزرسانی
            </asp:LinkButton>
            <asp:LinkButton ID="btnPrint" runat="server" OnClientClick="window.print(); return false;"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="printer"></i>
                چاپ گزارش
            </asp:LinkButton>
        </div>
    </div>

    <!-- ═══════════ FILTERS (no-print) ═══════════ -->
    <section class="no-print mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

        <div class="mb-3 flex flex-wrap items-center gap-2">
            <span class="text-[12.5px] font-bold text-ink-600 ml-1">بازه سریع:</span>

            <asp:LinkButton ID="btnRangeToday" runat="server" CommandArgument="today"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">امروز</asp:LinkButton>

            <asp:LinkButton ID="btnRangeWeek" runat="server" CommandArgument="week"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">این هفته</asp:LinkButton>

            <asp:LinkButton ID="btnRangeMonth" runat="server" CommandArgument="month"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">این ماه</asp:LinkButton>

            <asp:LinkButton ID="btnRangeQuarter" runat="server" CommandArgument="quarter"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">این فصل</asp:LinkButton>

            <asp:LinkButton ID="btnRangeYear" runat="server" CommandArgument="year"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">امسال</asp:LinkButton>

            <asp:LinkButton ID="btnRange30" runat="server" CommandArgument="30days"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">۳۰ روز اخیر</asp:LinkButton>

            <asp:LinkButton ID="btnRangeLastYear" runat="server" CommandArgument="lastyear"
                OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">سال گذشته</asp:LinkButton>
        </div>

        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-5">

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">از تاریخ (شمسی)</span>
                <asp:TextBox ID="txtFrom" runat="server" MaxLength="10" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                    placeholder="1403/01/01" />
            </label>

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">تا تاریخ (شمسی)</span>
                <asp:TextBox ID="txtTo" runat="server" MaxLength="10" dir="ltr"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                    placeholder="1403/12/29" />
            </label>

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">کانال فروش</span>
                <asp:DropDownList ID="ddlFilterChannel" runat="server"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه کانال‌ها" />
                    <asp:ListItem Value="1" Text="بازاریاب" />
                    <asp:ListItem Value="2" Text="آنلاین" />
                    <asp:ListItem Value="3" Text="ادمین/دفتری" />
                </asp:DropDownList>
            </label>

            <label class="block">
                <span class="text-[12.5px] font-semibold text-ink-700">نوع مشتری</span>
                <asp:DropDownList ID="ddlFilterCustomerType" runat="server"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه انواع" />
                    <asp:ListItem Value="1" Text="تک‌فروشی" />
                    <asp:ListItem Value="2" Text="عمده" />
                    <asp:ListItem Value="3" Text="نمایندگی" />
                </asp:DropDownList>
            </label>

            <div class="flex items-end">
                <asp:Button ID="btnApply" runat="server" Text="اعمال فیلتر" OnClick="btnApply_Click"
                    CssClass="h-10 w-full rounded-base bg-brand-600 px-4 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
            </div>

        </div>

        <div class="mt-3 rounded-base bg-amber-50/60 border border-amber-200 px-3 py-2 text-[11px] text-amber-800 leading-relaxed">
            <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="info"></i>
            فیلتر کانال و نوع مشتری فقط روی فروش محصولات اثر دارد. درآمد خدمات به‌طور کامل در گزارش لحاظ می‌شود.
        </div>

    </section>

    <!-- ═══════════════════════════════════════════════════════
         REPORT AREA
         ═══════════════════════════════════════════════════════ -->
    <div id="reportArea">

        <!-- ── Report meta ── -->
        <div class="mb-4 flex flex-wrap items-center justify-between gap-2 rounded-card border border-surface-line bg-surface-body px-4 py-2.5 text-[12px] text-ink-600 print-card">
            <div class="flex flex-wrap items-center gap-4">
                <span>
                    <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="calendar-range"></i>
                    بازه: <strong class="text-ink-800">
                        <asp:Literal ID="litPeriodLabel" runat="server" Text="—" />
                    </strong>
                </span>
                <span>
                    <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="receipt"></i>
                    اسناد: <strong class="text-ink-800">
                        <asp:Literal ID="litInvoiceCount" runat="server" Text="۰" />
                    </strong>
                </span>
                <span>
                    <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="users"></i>
                    مشتریان: <strong class="text-ink-800">
                        <asp:Literal ID="litCustomerCount" runat="server" Text="۰" />
                    </strong>
                </span>
            </div>
            <div class="no-print">
                <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="user"></i>
                کاربر: <strong class="text-ink-800">
                    <asp:Literal ID="litCurrentUser" runat="server" Text="—" />
                </strong>
            </div>
        </div>

        <!-- ═══════════ 1. EXECUTIVE SUMMARY ═══════════ -->
        <section class="mb-5">
            <h2 class="mb-3 flex items-center gap-2 text-[15px] font-bold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="gauge"></i>
                </span>
                خلاصه مدیریتی
            </h2>

            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">

                <!-- KPI: Total Revenue with breakdown -->
                <article class="stat-card rounded-card border border-surface-line bg-surface-card p-4 shadow-card print-card avoid-break">
                    <div class="mb-1 flex items-center justify-between">
                        <span class="text-[11.5px] font-semibold text-ink-500">درآمد کل</span>
                        <span class="grid h-8 w-8 place-items-center rounded-base bg-brand-50 text-brand-600">
                            <i class="h-4 w-4" data-lucide="trending-up"></i>
                        </span>
                    </div>
                    <p class="stat-value mt-2 text-[16px] font-black text-ink-900 font-mono">
                        <asp:Literal ID="litKpiRevenue" runat="server" Text="۰" />
                    </p>
                    <div class="mt-1.5 space-y-0.5 text-[10.5px] font-mono text-ink-500">
                        <div class="flex items-center justify-between">
                            <span>فروش:</span>
                            <span class="text-ink-700"><asp:Literal ID="litKpiProductRevenue" runat="server" Text="۰" /></span>
                        </div>
                        <div class="flex items-center justify-between">
                            <span class="text-brand-600">خدمات:</span>
                            <span class="text-brand-700 font-bold"><asp:Literal ID="litKpiServiceRevenue" runat="server" Text="۰" /></span>
                        </div>
                    </div>
                </article>

                <!-- KPI: COGS -->
                <article class="stat-card rounded-card border border-surface-line bg-surface-card p-4 shadow-card print-card avoid-break">
                    <div class="mb-1 flex items-center justify-between">
                        <span class="text-[11.5px] font-semibold text-ink-500">بهای تمام شده (COGS)</span>
                        <span class="grid h-8 w-8 place-items-center rounded-base bg-rose-50 text-rose-600">
                            <i class="h-4 w-4" data-lucide="trending-down"></i>
                        </span>
                    </div>
                    <p class="stat-value mt-2 text-[16px] font-black text-rose-600 font-mono">
                        <asp:Literal ID="litKpiCogs" runat="server" Text="۰" />
                    </p>
                    <p class="mt-0.5 text-[11px] text-ink-400">
                        حاشیه سود ناخالص:
                        <strong class="text-ink-600"><asp:Literal ID="litKpiGrossMargin" runat="server" Text="—" /></strong>
                    </p>
                </article>

                <!-- KPI: Expenses -->
                <article class="stat-card rounded-card border border-surface-line bg-surface-card p-4 shadow-card print-card avoid-break">
                    <div class="mb-1 flex items-center justify-between">
                        <span class="text-[11.5px] font-semibold text-ink-500">هزینه‌های عملیاتی</span>
                        <span class="grid h-8 w-8 place-items-center rounded-base bg-amber-50 text-amber-600">
                            <i class="h-4 w-4" data-lucide="wallet"></i>
                        </span>
                    </div>
                    <p class="stat-value mt-2 text-[16px] font-black text-amber-700 font-mono">
                        <asp:Literal ID="litKpiExpenses" runat="server" Text="۰" />
                    </p>
                    <p class="mt-0.5 text-[11px] text-ink-400">
                        <asp:Literal ID="litKpiExpensesHint" runat="server" Text="—" />
                    </p>
                </article>

                <!-- KPI: Net Profit (dynamic coloring) -->
                <article id="cardNetProfit" runat="server"
                    class="stat-card rounded-card border-2 bg-surface-card p-4 shadow-card print-card avoid-break">
                    <div class="mb-1 flex items-center justify-between">
                        <span class="text-[11.5px] font-semibold text-ink-500">سود (زیان) خالص</span>
                        <span class="grid h-8 w-8 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                            <i class="h-4 w-4" data-lucide="badge-dollar-sign"></i>
                        </span>
                    </div>
                    <p id="pNetProfit" runat="server"
                        class="stat-value mt-2 text-[16px] font-black font-mono">
                        <asp:Literal ID="litKpiNetProfit" runat="server" Text="۰" />
                    </p>
                    <p class="mt-0.5 text-[11px] text-ink-400">
                        حاشیه سود خالص:
                        <strong class="text-ink-600"><asp:Literal ID="litKpiNetMargin" runat="server" Text="—" /></strong>
                    </p>
                </article>

            </div>
        </section>

        <!-- ═══════════ 2. FORMAL P&L STATEMENT ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card shadow-card print-card avoid-break">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-ink-800 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="file-spreadsheet"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">صورت سود و زیان</h2>
                <span class="badge bg-surface-muted text-ink-500 mr-auto">ارقام به تومان</span>
            </div>

            <table class="statement-table">
                <tbody>

                    <!-- ═══ درآمد فروش محصولات ═══ -->
                    <tr class="row-main">
                        <td class="title font-semibold text-ink-800">درآمد فروش محصولات</td>
                        <td class="amount">
                            <asp:Literal ID="litStRevenue" runat="server" Text="۰" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStRevenuePct" runat="server" Text="—" />
                        </td>
                    </tr>

                    <!-- ═══ درآمد خدمات ═══ -->
                    <tr class="row-main">
                        <td class="title font-semibold text-ink-800">
                            <span class="text-brand-600">درآمد خدمات</span>
                            <span class="text-[10.5px] text-ink-400 mr-1">(بوجاری، خشک‌کن، آسیاب و ...)</span>
                        </td>
                        <td class="amount" style="color: #2563eb;">
                            <asp:Literal ID="litStServiceRevenue" runat="server" Text="۰" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStServicePct" runat="server" Text="—" />
                        </td>
                    </tr>

                    <!-- ═══ جمع درآمد کل ═══ -->
                    <tr class="row-total" style="background: linear-gradient(90deg, #eff6ff, #dbeafe); border-top: 2px solid #1e40af; border-bottom: 2px solid #1e40af;">
                        <td class="title" style="color: #1e3a8a;">جمع درآمد کل</td>
                        <td class="amount" style="color: #1e3a8a; font-size: 15px;">
                            <asp:Literal ID="litStTotalRevenue" runat="server" Text="۰" />
                        </td>
                        <td class="percent" style="color: #1e3a8a; font-weight: 700;">۱۰۰٪</td>
                    </tr>

                    <!-- ═══ COGS ═══ -->
                    <tr class="row-main">
                        <td class="title font-semibold text-ink-800">بهای تمام شده کالای فروش رفته</td>
                        <td class="amount amount-negative">
                            <asp:Literal ID="litStCogs" runat="server" Text="(۰)" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStCogsPct" runat="server" Text="—" />
                        </td>
                    </tr>

                    <!-- ═══ سود ناخالص ═══ -->
                    <tr class="row-total">
                        <td class="title">سود ناخالص</td>
                        <td class="amount">
                            <asp:Literal ID="litStGrossProfit" runat="server" Text="۰" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStGrossPct" runat="server" Text="—" />
                        </td>
                    </tr>

                    <!-- ═══ هزینه‌های عملیاتی ═══ -->
                    <tr class="row-section-header">
                        <td colspan="3">هزینه‌های عملیاتی دوره</td>
                    </tr>

                    <asp:Repeater ID="rptStatementExpenses" runat="server">
                        <ItemTemplate>
                            <tr class="row-sub">
                                <td class="title">
                                    <span class="text-ink-400 ml-1">—</span>
                                    <%# Enc(Eval("CategoryName")) %>
                                </td>
                                <td class="amount">
                                    <%# FormatAmountNegative(Eval("Amount")) %>
                                </td>
                                <td class="percent">
                                    <%# FormatPercentOf(Eval("Amount"), Eval("TotalRevenue")) %>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>

                    <asp:Panel ID="pnlNoExpenses" runat="server" Visible="false">
                        <tr class="row-sub">
                            <td class="title text-ink-400" colspan="3" style="text-align:center; padding:14px;">
                                در این بازه هزینه‌ای ثبت نشده است.
                            </td>
                        </tr>
                    </asp:Panel>

                    <!-- ═══ جمع هزینه‌ها ═══ -->
                    <tr class="row-total">
                        <td class="title">جمع هزینه‌های عملیاتی</td>
                        <td class="amount amount-negative">
                            <asp:Literal ID="litStTotalExpenses" runat="server" Text="(۰)" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStExpensesPct" runat="server" Text="—" />
                        </td>
                    </tr>

                    <!-- ═══ سود خالص ═══ -->
                    <tr class="row-grand-total">
                        <td class="title">سود (زیان) خالص</td>
                        <td class="amount">
                            <asp:Literal ID="litStNetProfit" runat="server" Text="۰" />
                        </td>
                        <td class="percent">
                            <asp:Literal ID="litStNetPct" runat="server" Text="—" />
                        </td>
                    </tr>

                </tbody>
            </table>
        </section>

        <!-- ═══════════ 3. REVENUE BY CHANNEL & TYPE ═══════════ -->
        <div class="grid gap-5 mb-5 lg:grid-cols-2">

            <!-- By Channel -->
            <section class="rounded-card border border-surface-line bg-surface-card shadow-card print-card avoid-break">
                <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-3.5 w-3.5" data-lucide="radio-tower"></i>
                    </span>
                    <h2 class="text-[14px] font-bold text-ink-900">فروش به تفکیک کانال</h2>
                </div>

                <div class="overflow-x-auto">
                    <table class="w-full text-right">
                        <thead>
                            <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                                <th class="px-4 py-2.5 text-right font-semibold">کانال</th>
                                <th class="px-4 py-2.5 text-center font-semibold">تعداد فاکتور</th>
                                <th class="px-4 py-2.5 text-center font-semibold">درآمد</th>
                                <th class="px-4 py-2.5 text-center font-semibold">سود ناخالص</th>
                            </tr>
                        </thead>
                        <tbody class="text-[13px]">
                            <asp:Repeater ID="rptByChannel" runat="server">
                                <ItemTemplate>
                                    <tr class="border-b border-surface-line/60">
                                        <td class="px-4 py-2.5">
                                            <span class="badge <%# GetChannelBadgeCss(Eval("Channel")) %>">
                                                <%# GetChannelLabel(Eval("Channel")) %>
                                            </span>
                                        </td>
                                        <td class="px-4 py-2.5 text-center font-mono text-ink-700">
                                            <%# FormatCount(Eval("InvoiceCount")) %>
                                        </td>
                                        <td class="px-4 py-2.5 text-center font-mono font-bold text-ink-900">
                                            <%# FormatMoney(Eval("Revenue")) %>
                                        </td>
                                        <td class='<%# GetGrossProfitCss(Eval("GrossProfit")) %> font-mono font-bold px-4 py-2.5 text-center'>
                                            <%# FormatMoney(Eval("GrossProfit")) %>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>

                <asp:Panel ID="pnlNoChannel" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">داده‌ای برای این بازه وجود ندارد.</p>
                </asp:Panel>
            </section>

            <!-- By Customer Type -->
            <section class="rounded-card border border-surface-line bg-surface-card shadow-card print-card avoid-break">
                <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-3.5 w-3.5" data-lucide="users"></i>
                    </span>
                    <h2 class="text-[14px] font-bold text-ink-900">فروش به تفکیک نوع مشتری</h2>
                </div>

                <div class="overflow-x-auto">
                    <table class="w-full text-right">
                        <thead>
                            <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                                <th class="px-4 py-2.5 text-right font-semibold">نوع مشتری</th>
                                <th class="px-4 py-2.5 text-center font-semibold">تعداد فاکتور</th>
                                <th class="px-4 py-2.5 text-center font-semibold">درآمد</th>
                                <th class="px-4 py-2.5 text-center font-semibold">سهم</th>
                            </tr>
                        </thead>
                        <tbody class="text-[13px]">
                            <asp:Repeater ID="rptByCustomerType" runat="server">
                                <ItemTemplate>
                                    <tr class="border-b border-surface-line/60">
                                        <td class="px-4 py-2.5">
                                            <span class="badge <%# GetCustomerTypeBadgeCss(Eval("OrderType")) %>">
                                                <%# GetCustomerTypeLabel(Eval("OrderType")) %>
                                            </span>
                                        </td>
                                        <td class="px-4 py-2.5 text-center font-mono text-ink-700">
                                            <%# FormatCount(Eval("InvoiceCount")) %>
                                        </td>
                                        <td class="px-4 py-2.5 text-center font-mono font-bold text-ink-900">
                                            <%# FormatMoney(Eval("Revenue")) %>
                                        </td>
                                        <td class="px-4 py-2.5 text-center">
                                            <%# FormatShareBar(Eval("Revenue"), Eval("TotalRevenue")) %>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>

                <asp:Panel ID="pnlNoCustomerType" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">داده‌ای برای این بازه وجود ندارد.</p>
                </asp:Panel>
            </section>

        </div>

        <!-- ═══════════ 4. SERVICE REVENUE BREAKDOWN (NEW) ═══════════ -->
        <section class="mb-5 rounded-card border-2 border-brand-200 bg-gradient-to-l from-brand-50/50 to-transparent shadow-card print-card avoid-break">
            <div class="flex items-center gap-2 border-b border-brand-200 px-5 py-3">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="wrench"></i>
                </span>
                <h2 class="text-[14px] font-bold text-ink-900">تحلیل درآمد خدمات</h2>
                <span class="badge bg-brand-100 text-brand-700 mr-auto">
                    جمع:
                    <asp:Literal ID="litServiceGrandTotal" runat="server" Text="۰" />
                    تومان
                </span>
            </div>

            <div class="overflow-x-auto">
                <table class="w-full min-w-[700px] text-right">
                    <thead>
                        <tr class="border-b border-brand-200 text-[11.5px] uppercase text-ink-500">
                            <th class="px-4 py-2.5 text-right font-semibold">نوع خدمت</th>
                            <th class="px-4 py-2.5 text-center font-semibold">تعداد سفارش</th>
                            <th class="px-4 py-2.5 text-center font-semibold">مجموع ورودی</th>
                            <th class="px-4 py-2.5 text-center font-semibold">درآمد</th>
                            <th class="px-4 py-2.5 text-center font-semibold">سهم</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13px]">
                        <asp:Repeater ID="rptByServiceType" runat="server">
                            <ItemTemplate>
                                <tr class="border-b border-brand-100 hover:bg-brand-50/40">
                                    <td class="px-4 py-2.5">
                                        <span class="badge bg-brand-50 text-brand-700">
                                            <i class="h-3 w-3" data-lucide="settings"></i>
                                            <%# Enc(Eval("ServiceName")) %>
                                        </span>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-ink-700">
                                        <%# FormatCount(Eval("OrderCount")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-ink-600">
                                        <%# FormatQty(Eval("TotalInputWeight")) %>
                                        <span class="text-[10px] text-ink-400">kg</span>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono font-bold text-brand-700">
                                        <%# FormatMoney(Eval("Revenue")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center">
                                        <%# FormatShareBar(Eval("Revenue"), Eval("TotalRevenue")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlNoServices" runat="server" Visible="false" CssClass="py-10 text-center">
                <p class="text-[13px] text-ink-400">در این بازه سفارش خدمت تکمیل‌شده‌ای وجود ندارد.</p>
            </asp:Panel>
        </section>

        <!-- ═══════════ 5. TOP PRODUCTS BY PROFIT ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card shadow-card print-card avoid-break">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-3.5 w-3.5" data-lucide="award"></i>
                </span>
                <h2 class="text-[14px] font-bold text-ink-900">پرفروش‌ترین محصولات (بر اساس سود ناخالص)</h2>
                <span class="badge bg-surface-muted text-ink-500 mr-auto">۱۰ محصول برتر</span>
            </div>

            <div class="overflow-x-auto">
                <table class="w-full min-w-[720px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                            <th class="px-4 py-2.5 text-center font-semibold" style="width:40px;">#</th>
                            <th class="px-4 py-2.5 text-right font-semibold">محصول</th>
                            <th class="px-4 py-2.5 text-center font-semibold">مقدار فروش</th>
                            <th class="px-4 py-2.5 text-center font-semibold">درآمد</th>
                            <th class="px-4 py-2.5 text-center font-semibold">COGS</th>
                            <th class="px-4 py-2.5 text-center font-semibold">سود ناخالص</th>
                            <th class="px-4 py-2.5 text-center font-semibold">حاشیه</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13px]">
                        <asp:Repeater ID="rptTopProducts" runat="server">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line/60 hover:bg-surface-body/60">
                                    <td class="px-4 py-2.5 text-center">
                                        <span class="grid h-6 w-6 place-items-center rounded-full bg-ink-800 text-white text-[11px] font-bold">
                                            <%# Eval("Rank") %>
                                        </span>
                                    </td>
                                    <td class="px-4 py-2.5">
                                        <p class="font-semibold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("ProductName")) %>
                                        </p>
                                        <p class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr">
                                            <%# Enc(Eval("ProductCode")) %>
                                        </p>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-ink-700">
                                        <%# FormatQuantity(Eval("TotalQty")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-ink-900">
                                        <%# FormatMoney(Eval("Revenue")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-rose-600">
                                        <%# FormatMoney(Eval("Cogs")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono font-bold text-emerald-700">
                                        <%# FormatMoney(Eval("GrossProfit")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center">
                                        <%# GetMarginBadge(Eval("GrossProfit"), Eval("Revenue")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlNoProducts" runat="server" Visible="false" CssClass="py-10 text-center">
                <p class="text-[13px] text-ink-400">محصولی در این بازه فروخته نشده است.</p>
            </asp:Panel>
        </section>

        <!-- ═══════════ 6. EXPENSES BREAKDOWN ═══════════ -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card shadow-card print-card avoid-break">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-3.5 w-3.5" data-lucide="wallet"></i>
                </span>
                <h2 class="text-[14px] font-bold text-ink-900">تحلیل هزینه‌های عملیاتی</h2>
                <span class="badge bg-surface-muted text-ink-500 mr-auto">
                    جمع:
                    <asp:Literal ID="litExpensesGrandTotal" runat="server" Text="۰" />
                    تومان
                </span>
            </div>

            <div class="overflow-x-auto">
                <table class="w-full text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                            <th class="px-4 py-2.5 text-right font-semibold">دسته هزینه</th>
                            <th class="px-4 py-2.5 text-center font-semibold">تعداد</th>
                            <th class="px-4 py-2.5 text-center font-semibold">مبلغ</th>
                            <th class="px-4 py-2.5 text-center font-semibold">سهم</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13px]">
                        <asp:Repeater ID="rptExpenses" runat="server">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line/60">
                                    <td class="px-4 py-2.5 font-semibold text-ink-800">
                                        <span class="badge bg-amber-50 text-amber-700 mr-1">
                                            <i class="h-3 w-3" data-lucide="tag"></i>
                                        </span>
                                        <%# Enc(Eval("CategoryName")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono text-ink-600">
                                        <%# FormatCount(Eval("ExpenseCount")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center font-mono font-bold text-amber-700">
                                        <%# FormatMoney(Eval("Amount")) %>
                                    </td>
                                    <td class="px-4 py-2.5 text-center">
                                        <%# FormatShareBar(Eval("Amount"), Eval("TotalExpenses")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>

            <asp:Panel ID="pnlNoExpensesBreakdown" runat="server" Visible="false" CssClass="py-10 text-center">
                <p class="text-[13px] text-ink-400">هزینه‌ای در این بازه ثبت نشده است.</p>
            </asp:Panel>
        </section>

        <!-- ═══════════ 7. PRINT-ONLY FOOTER (Signatures) ═══════════ -->
        <div class="print-footer-print-only">
            <table style="width:100%; border:0;">
                <tr>
                    <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                        <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:80%; margin:0 auto;">
                            تهیه‌کننده / حسابدار
                        </div>
                    </td>
                    <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                        <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:80%; margin:0 auto;">
                            مدیر مالی
                        </div>
                    </td>
                    <td style="text-align:center; padding-top:50px; font-size:11px; color:#475569; border:0;">
                        <div style="border-top:1px dotted #94a3b8; padding-top:6px; width:80%; margin:0 auto;">
                            مدیرعامل
                        </div>
                    </td>
                </tr>
            </table>
            <p style="text-align:center; font-size:9.5px; color:#94a3b8; margin-top:14px;">
                این گزارش به‌صورت خودکار توسط سامانه ERP تولید شده است.
            </p>
        </div>

    </div><!-- /reportArea -->

</asp:Content>