<%@ Page Title="گزارش سود به تفکیک کانال" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="reports-profit-channel.aspx.cs" Inherits="admin_reports_profit_channel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<style>
    .channel-row {
        display: grid;
        grid-template-columns: 130px 80px 130px 130px 130px 90px 1fr;
        gap: 10px;
        align-items: center;
        padding: 10px 12px;
        font-size: 12.5px;
    }
    .marketer-row {
        display: grid;
        grid-template-columns: 36px 1.5fr 110px 70px 120px 110px 110px 80px;
        gap: 10px;
        align-items: center;
        padding: 10px 12px;
        font-size: 12.5px;
        border-bottom: 1px solid #f1f5f9;
        transition: background .15s;
    }
    .marketer-row:hover { background: #f8fafc; }
    .marketer-header {
        background: #f1f5f9;
        font-weight: 700;
        color: #475569;
        font-size: 11px;
        text-transform: uppercase;
        border-radius: 8px;
    }

    .rank-badge {
        display: grid;
        place-items: center;
        width: 28px;
        height: 28px;
        border-radius: 50%;
        font-size: 12px;
        font-weight: 800;
        flex-shrink: 0;
    }
    .rank-1 { background: linear-gradient(135deg, #fbbf24, #f59e0b); color: #fff; box-shadow: 0 2px 8px rgba(251, 191, 36, 0.4); }
    .rank-2 { background: linear-gradient(135deg, #cbd5e1, #94a3b8); color: #fff; }
    .rank-3 { background: linear-gradient(135deg, #f59e0b, #d97706); color: #fff; }
    .rank-other { background: #f1f5f9; color: #64748b; }

    .chart-container {
        position: relative;
        height: 280px;
    }

    /* ─── Print ─── */
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
            page-break-inside: avoid;
        }
        .shadow-card, .shadow-card-lg { box-shadow: none !important; }
        table { page-break-inside: auto; border-collapse: collapse; }
        tr { page-break-inside: avoid; }
        thead { display: table-header-group; }
        .marketer-row { padding: 5px 8px !important; font-size: 10px !important; grid-template-columns: 30px 1.5fr 90px 60px 100px 90px 90px 70px !important; }
        .channel-row { padding: 5px 8px !important; font-size: 10px !important; }
        .rank-badge { width: 22px !important; height: 22px !important; font-size: 10px !important; }
        .stat-value { font-size: 14px !important; }
        .chart-container { height: 200px !important; }

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

    <!-- ═══════════════════════════════════════════════════════
         PRINT HEADER
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
            <div class="report-title">گزارش سود به تفکیک کانال فروش و بازاریاب</div>
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
                <a class="hover:text-brand-600" href="reports-hub.aspx">مرکز گزارش‌ها</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">سود به تفکیک کانال</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                سود به تفکیک کانال و بازاریاب
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                تحلیل درآمد، بهای تمام‌شده، و سود ناخالص به تفکیک کانال فروش و بازاریاب.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="reports-hub.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="layout-grid"></i>
                مرکز گزارش‌ها
            </a>
            <asp:LinkButton ID="btnPrint" runat="server" OnClientClick="window.print(); return false;"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700">
                <i class="h-4 w-4" data-lucide="printer"></i>
                چاپ گزارش
            </asp:LinkButton>
        </div>
    </div>

    <!-- ═══════════ FILTERS ═══════════ -->
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
        
        <asp:LinkButton ID="btnRangeAll" runat="server" CommandArgument="all"
    OnClick="QuickRange_Click" 
    CssClass="badge bg-brand-50 text-brand-700 hover:bg-brand-100 cursor-pointer">
    <i class="inline h-3 w-3" data-lucide="infinity"></i>
    همه
</asp:LinkButton>
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
                <span class="text-[12.5px] font-semibold text-ink-700">بازاریاب</span>
                <asp:DropDownList ID="ddlFilterMarketer" runat="server"
                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                </asp:DropDownList>
            </label>

            <div class="flex items-end gap-2">
                <asp:Button ID="btnApply" runat="server" Text="اعمال" OnClick="btnApply_Click"
                    CssClass="h-10 flex-1 rounded-base bg-brand-600 px-4 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
                <asp:Button ID="btnClear" runat="server" Text="پاک" OnClick="btnClear_Click"
                    CssClass="h-10 rounded-base border border-surface-line bg-white px-4 text-[13.5px] font-semibold text-ink-700 hover:bg-surface-muted" />
            </div>

        </div>

    </section>

    <!-- ═══════════ KPI CARDS ═══════════ -->
    <section class="mb-5">
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
                <div class="mb-2 flex items-center justify-between">
                    <span class="text-[11.5px] font-semibold text-ink-500">درآمد کل</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-4 w-4" data-lucide="trending-up"></i>
                    </span>
                </div>
                <p class="stat-value font-mono text-[20px] font-black text-ink-900">
                    <asp:Literal ID="litKpiRevenue" runat="server" Text="۰" />
                </p>
                <p class="mt-0.5 text-[11px] text-ink-400">
                    از <asp:Literal ID="litKpiInvoiceCount" runat="server" Text="۰" /> فاکتور
                </p>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
                <div class="mb-2 flex items-center justify-between">
                    <span class="text-[11.5px] font-semibold text-ink-500">بهای تمام‌شده (COGS)</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-rose-50 text-rose-600">
                        <i class="h-4 w-4" data-lucide="trending-down"></i>
                    </span>
                </div>
                <p class="stat-value font-mono text-[20px] font-black text-rose-600">
                    <asp:Literal ID="litKpiCogs" runat="server" Text="۰" />
                </p>
                <p class="mt-0.5 text-[11px] text-ink-400">
                    سهم از درآمد: <asp:Literal ID="litKpiCogsPct" runat="server" Text="—" />
                </p>
            </article>

            <article id="cardProfit" runat="server"
                class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
                <div class="mb-2 flex items-center justify-between">
                    <span class="text-[11.5px] font-semibold text-ink-500">سود ناخالص</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-4 w-4" data-lucide="badge-dollar-sign"></i>
                    </span>
                </div>
                <p id="pKpiProfit" runat="server" class="stat-value font-mono text-[20px] font-black text-emerald-700">
                    <asp:Literal ID="litKpiProfit" runat="server" Text="۰" />
                </p>
                <p class="mt-0.5 text-[11px] text-ink-400">
                    حاشیه سود: <asp:Literal ID="litKpiMargin" runat="server" Text="—" />
                </p>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
                <div class="mb-2 flex items-center justify-between">
                    <span class="text-[11.5px] font-semibold text-ink-500">میانگین فاکتور</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-4 w-4" data-lucide="receipt"></i>
                    </span>
                </div>
                <p class="stat-value font-mono text-[20px] font-black text-violet-700">
                    <asp:Literal ID="litKpiAvgInvoice" runat="server" Text="۰" />
                </p>
                <p class="mt-0.5 text-[11px] text-ink-400">
                    میانگین مبلغ هر فاکتور
                </p>
            </article>

        </div>
    </section>

    <!-- ═══════════ CHART ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <h2 class="flex items-center gap-2 text-[15px] font-bold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="bar-chart-3"></i>
                </span>
                مقایسه کانال‌های فروش
            </h2>
            <div class="flex items-center gap-3 text-[12px]">
                <span class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-3 rounded-sm" style="background: #2563eb;"></span>
                    درآمد
                </span>
                <span class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-3 rounded-sm" style="background: #ef4444;"></span>
                    COGS
                </span>
                <span class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-3 rounded-sm" style="background: #10b981;"></span>
                    سود
                </span>
            </div>
        </div>

        <div class="chart-container">
            <canvas id="channelChart"></canvas>
        </div>

        <asp:HiddenField ID="hfChartLabels" runat="server" />
        <asp:HiddenField ID="hfChartRevenue" runat="server" />
        <asp:HiddenField ID="hfChartCogs" runat="server" />
        <asp:HiddenField ID="hfChartProfit" runat="server" />
    </section>

    <!-- ═══════════ BY CHANNEL ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card print-card">

        <div class="mb-4 flex items-center gap-2 border-b border-surface-line pb-3">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                <i class="h-3.5 w-3.5" data-lucide="radio-tower"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">تفکیک کانال فروش</h2>
            <span class="badge bg-surface-muted text-ink-500 mr-auto">ارقام به تومان</span>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <div class="channel-row marketer-header rounded-lg">
                <div>کانال</div>
                <div class="text-center">تعداد فاکتور</div>
                <div class="text-center">درآمد</div>
                <div class="text-center">COGS</div>
                <div class="text-center">سود ناخالص</div>
                <div class="text-center">حاشیه</div>
                <div class="text-center">سهم از درآمد</div>
            </div>

            <asp:Repeater ID="rptByChannel" runat="server">
                <ItemTemplate>
                    <div class="channel-row border-b border-surface-line/50">
                        <div>
                            <span class='<%# GetChannelBadgeCss(Eval("Channel")) %>'>
                                <i class="h-3 w-3" data-lucide='<%# GetChannelIcon(Eval("Channel")) %>'></i>
                                <%# Enc(Eval("ChannelName")) %>
                            </span>
                        </div>
                        <div class="text-center font-mono font-bold text-ink-800">
                            <%# FormatCount(Eval("InvoiceCount")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-ink-900">
                            <%# FormatMoney(Eval("Revenue")) %>
                        </div>
                        <div class="text-center font-mono text-rose-600">
                            <%# FormatMoney(Eval("Cogs")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-emerald-700">
                            <%# FormatMoney(Eval("Profit")) %>
                        </div>
                        <div class="text-center">
                            <%# GetMarginBadge(Eval("Profit"), Eval("Revenue")) %>
                        </div>
                        <div>
                            <%# FormatShareBar(Eval("Revenue"), Eval("TotalRevenue")) %>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <!-- Total row -->
            <asp:Panel ID="pnlChannelTotal" runat="server" Visible="false">
                <div class="channel-row rounded-lg border-t-2 border-b-2 border-ink-300 bg-ink-800/5 font-bold">
                    <div class="text-ink-900">جمع کل</div>
                    <div class="text-center font-mono text-ink-900">
                        <asp:Literal ID="litChannelTotalCount" runat="server" Text="۰" />
                    </div>
                    <div class="text-center font-mono text-ink-900">
                        <asp:Literal ID="litChannelTotalRevenue" runat="server" Text="۰" />
                    </div>
                    <div class="text-center font-mono text-rose-700">
                        <asp:Literal ID="litChannelTotalCogs" runat="server" Text="۰" />
                    </div>
                    <div class="text-center font-mono text-emerald-800">
                        <asp:Literal ID="litChannelTotalProfit" runat="server" Text="۰" />
                    </div>
                    <div class="text-center font-mono text-ink-700">—</div>
                    <div></div>
                </div>
            </asp:Panel>
        </div>

        <asp:Panel ID="pnlChannelEmpty" runat="server" Visible="false" CssClass="py-10 text-center">
            <p class="text-[13px] text-ink-400">داده‌ای در این بازه وجود ندارد.</p>
        </asp:Panel>

    </section>

    <!-- ═══════════ BY MARKETER ═══════════ -->
    <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card print-card">

        <div class="mb-4 flex items-center gap-2 border-b border-surface-line pb-3">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-violet-600 text-white">
                <i class="h-3.5 w-3.5" data-lucide="users"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">تفکیک بازاریاب</h2>
            <span class="badge bg-surface-muted text-ink-500 mr-auto">
                <asp:Literal ID="litMarketerCount" runat="server" Text="۰" /> بازاریاب
            </span>
        </div>

        <div class="dashboard-scrollbar overflow-x-auto">
            <div class="marketer-row marketer-header rounded-lg">
                <div></div>
                <div>بازاریاب</div>
                <div class="text-center">کانال</div>
                <div class="text-center">تعداد</div>
                <div class="text-center">درآمد</div>
                <div class="text-center">COGS</div>
                <div class="text-center">سود</div>
                <div class="text-center">حاشیه</div>
            </div>

            <asp:Repeater ID="rptByMarketer" runat="server">
                <ItemTemplate>
                    <div class="marketer-row">
                        <div class='<%# GetRankBadgeCss(Eval("Rank")) %>'>
                            <%# Eval("Rank") %>
                        </div>
                        <div class="min-w-0">
                            <p class="truncate font-semibold text-ink-900 text-[13px]">
                                <%# Enc(Eval("MarketerName")) %>
                            </p>
                        </div>
                        <div class="text-center">
                            <span class='<%# GetChannelBadgeCss(Eval("Channel")) %>'>
                                <%# Enc(Eval("ChannelName")) %>
                            </span>
                        </div>
                        <div class="text-center font-mono text-ink-700">
                            <%# FormatCount(Eval("InvoiceCount")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-ink-900">
                            <%# FormatMoney(Eval("Revenue")) %>
                        </div>
                        <div class="text-center font-mono text-rose-600">
                            <%# FormatMoney(Eval("Cogs")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-emerald-700">
                            <%# FormatMoney(Eval("Profit")) %>
                        </div>
                        <div class="text-center">
                            <%# GetMarginBadge(Eval("Profit"), Eval("Revenue")) %>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>

        <asp:Panel ID="pnlMarketerEmpty" runat="server" Visible="false" CssClass="py-10 text-center">
            <p class="text-[13px] text-ink-400">بازاریابی در این بازه فروش نداشته است.</p>
        </asp:Panel>

    </section>

    <!-- ═══════════ TOP 5 LEADERBOARD ═══════════ -->
    <asp:Panel ID="pnlTopMarketers" runat="server" Visible="false"
        CssClass="mb-5 rounded-card border-2 border-amber-200 bg-gradient-to-l from-amber-50/60 to-transparent p-6 shadow-card print-card">

        <div class="mb-4 flex items-center gap-2 border-b border-amber-200 pb-3">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-amber-500 text-white">
                <i class="h-3.5 w-3.5" data-lucide="award"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">۵ بازاریاب برتر</h2>
            <span class="badge bg-amber-100 text-amber-700 mr-auto">بر اساس سود ناخالص</span>
        </div>

        <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-5">
            <asp:Repeater ID="rptTopMarketers" runat="server">
                <ItemTemplate>
                    <div class="relative rounded-card border border-amber-200 bg-white p-4 shadow-sm">
                        <div class="flex items-center gap-3 mb-3">
                            <span class='<%# GetRankBadgeCss(Eval("Rank")) %>'>
                                <%# Eval("Rank") %>
                            </span>
                            <p class="truncate text-[13px] font-bold text-ink-900 min-w-0 flex-1">
                                <%# Enc(Eval("MarketerName")) %>
                            </p>
                        </div>
                        <p class="font-mono text-[16px] font-black text-emerald-700">
                            <%# FormatMoney(Eval("Profit")) %>
                            <span class="text-[10px] font-normal text-ink-400">تومان</span>
                        </p>
                        <div class="mt-2 flex items-center justify-between text-[10.5px] text-ink-400">
                            <span>درآمد: <span class="font-mono"><%# FormatMoney(Eval("Revenue")) %></span></span>
                            <span>حاشیه: <span class="font-mono font-bold"><%# FormatPercent(Eval("Profit"), Eval("Revenue")) %></span></span>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>

    <!-- ═══════════ PRINT FOOTER ═══════════ -->
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
                        مدیر فروش
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

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();

        var labels = readHidden('<%= hfChartLabels.ClientID %>');
        var revenueData = readHidden('<%= hfChartRevenue.ClientID %>');
        var cogsData = readHidden('<%= hfChartCogs.ClientID %>');
        var profitData = readHidden('<%= hfChartProfit.ClientID %>');

        var ctx = document.getElementById('channelChart');
        if (ctx && labels.length > 0) {
            new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'درآمد',
                            data: revenueData,
                            backgroundColor: 'rgba(37, 99, 235, 0.75)',
                            borderColor: '#2563eb',
                            borderWidth: 1.5,
                            borderRadius: 6
                        },
                        {
                            label: 'COGS',
                            data: cogsData,
                            backgroundColor: 'rgba(239, 68, 68, 0.7)',
                            borderColor: '#ef4444',
                            borderWidth: 1.5,
                            borderRadius: 6
                        },
                        {
                            label: 'سود',
                            data: profitData,
                            backgroundColor: 'rgba(16, 185, 129, 0.75)',
                            borderColor: '#10b981',
                            borderWidth: 1.5,
                            borderRadius: 6
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            rtl: true,
                            textDirection: 'rtl',
                            backgroundColor: '#0f172a',
                            titleFont: { family: 'Vazirmatn', size: 12, weight: '700' },
                            bodyFont: { family: 'Vazirmatn', size: 12 },
                            padding: 10,
                            callbacks: {
                                label: function (context) {
                                    return context.dataset.label + ': ' +
                                        formatNumber(context.parsed.y) + ' تومان';
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                font: { family: 'Vazirmatn', size: 11 },
                                color: '#94a3b8',
                                callback: function (value) {
                                    if (value >= 1000000) return (value / 1000000).toFixed(1) + 'M';
                                    if (value >= 1000) return (value / 1000).toFixed(0) + 'K';
                                    return value;
                                }
                            },
                            grid: { color: '#f1f5f9', drawBorder: false }
                        },
                        x: {
                            ticks: {
                                font: { family: 'Vazirmatn', size: 11, weight: '600' },
                                color: '#475569'
                            },
                            grid: { display: false }
                        }
                    }
                }
            });
        }
    });

    function readHidden(id) {
        var el = document.getElementById(id);
        if (!el || !el.value) return [];
        try { return JSON.parse(el.value); }
        catch (e) { return []; }
    }

    function formatNumber(n) {
        if (!n || n === 0) return '۰';
        return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '،');
    }
</script>
</asp:Content>