<%@ Page Title="داشبورد" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="admin_Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<style>
    .kpi-card {
        position: relative;
        overflow: hidden;
    }
    .kpi-card::after {
        content: '';
        position: absolute;
        top: -20px;
        left: -20px;
        width: 80px;
        height: 80px;
        border-radius: 50%;
        opacity: 0.06;
        pointer-events: none;
    }
    .kpi-card--brand::after { background: #2563eb; }
    .kpi-card--emerald::after { background: #10b981; }
    .kpi-card--amber::after { background: #f59e0b; }
    .kpi-card--violet::after { background: #8b5cf6; }
    .kpi-card--rose::after { background: #ef4444; }

    .chart-container {
        position: relative;
        height: 300px;
    }

    .quick-action {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 14px 16px;
        border-radius: 14px;
        background: #fff;
        border: 1px solid #eef2f6;
        transition: all .2s;
        text-decoration: none;
        color: #334155;
    }
    .quick-action:hover {
        transform: translateY(-2px);
        box-shadow: 0 10px 24px -8px rgba(15, 23, 42, 0.12);
        border-color: #bfdbfe;
        color: #1e40af;
    }
    .quick-action-icon {
        display: grid;
        place-items: center;
        width: 44px;
        height: 44px;
        border-radius: 12px;
        flex-shrink: 0;
    }

    .activity-item {
        display: flex;
        align-items: flex-start;
        gap: 12px;
        padding: 12px 0;
        border-bottom: 1px solid #f1f5f9;
    }
    .activity-item:last-child { border-bottom: none; }

    .rank-badge {
        display: grid;
        place-items: center;
        width: 24px;
        height: 24px;
        border-radius: 50%;
        background: #0f172a;
        color: #fff;
        font-size: 11px;
        font-weight: 800;
        flex-shrink: 0;
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════ WELCOME HEADER ═══════════ -->
    <div class="mb-6">
        <h1 class="text-[24px] font-semibold text-ink-900">
            خوش آمدید، <asp:Literal ID="litWelcomeName" runat="server" Text="کاربر" />
        </h1>
        <p class="mt-1 text-[14px] text-ink-500">
            نمای کلی کسب‌وکار — امروز،
            <asp:Literal ID="litTodayShamsi" runat="server" Text="—" />
        </p>
    </div>

    <!-- ═══════════ QUICK ACTIONS ═══════════ -->
    <section class="mb-6">
        <h2 class="mb-3 flex items-center gap-2 text-[14px] font-bold text-ink-900">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                <i class="h-3.5 w-3.5" data-lucide="zap"></i>
            </span>
            دسترسی سریع
        </h2>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">

            <a href="service-orders.aspx" class="quick-action">
                <span class="quick-action-icon bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="package-plus"></i>
                </span>
                <div class="min-w-0">
                    <p class="text-[13.5px] font-bold text-ink-900">پذیرش خدمت</p>
                    <p class="mt-0.5 text-[11px] text-ink-500">ثبت سریع بوجاری / خشک‌کن</p>
                </div>
            </a>

            <a href="purchase-orders.aspx" class="quick-action">
                <span class="quick-action-icon bg-violet-50 text-violet-600">
                    <i class="h-5 w-5" data-lucide="shopping-cart"></i>
                </span>
                <div class="min-w-0">
                    <p class="text-[13.5px] font-bold text-ink-900">سفارش خرید</p>
                    <p class="mt-0.5 text-[11px] text-ink-500">خرید مواد اولیه</p>
                </div>
            </a>

            <a href="final-invoices.aspx" class="quick-action">
                <span class="quick-action-icon bg-emerald-50 text-emerald-600">
                    <i class="h-5 w-5" data-lucide="receipt"></i>
                </span>
                <div class="min-w-0">
                    <p class="text-[13.5px] font-bold text-ink-900">فاکتور فروش</p>
                    <p class="mt-0.5 text-[11px] text-ink-500">صدور فاکتور رسمی</p>
                </div>
            </a>

            <a href="profit-loss.aspx" class="quick-action">
                <span class="quick-action-icon bg-amber-50 text-amber-600">
                    <i class="h-5 w-5" data-lucide="bar-chart-3"></i>
                </span>
                <div class="min-w-0">
                    <p class="text-[13.5px] font-bold text-ink-900">گزارش سود و زیان</p>
                    <p class="mt-0.5 text-[11px] text-ink-500">تحلیل سوددهی</p>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ PRIMARY KPIs ═══════════ -->
    <section class="mb-6">
        <h2 class="mb-3 flex items-center gap-2 text-[14px] font-bold text-ink-900">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-ink-800 text-white">
                <i class="h-3.5 w-3.5" data-lucide="gauge"></i>
            </span>
            عملکرد این ماه
        </h2>

        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <!-- Total Revenue -->
            <article class="kpi-card kpi-card--brand rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center justify-between mb-2">
                    <span class="text-[11.5px] font-semibold text-ink-500">درآمد کل این ماه</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-4 w-4" data-lucide="trending-up"></i>
                    </span>
                </div>
                <p class="font-mono text-[20px] font-black text-ink-900">
                    <asp:Literal ID="litKpiTotalRevenue" runat="server" Text="۰" />
                </p>
                <div class="mt-2 space-y-0.5 text-[10.5px] font-mono text-ink-500">
                    <div class="flex justify-between">
                        <span>فروش:</span>
                        <span class="text-ink-700"><asp:Literal ID="litKpiProductRev" runat="server" Text="۰" /></span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-brand-600">خدمات:</span>
                        <span class="text-brand-700 font-bold"><asp:Literal ID="litKpiServiceRev" runat="server" Text="۰" /></span>
                    </div>
                </div>
                <p class="mt-2 text-[10.5px] text-ink-400">
                    <asp:Literal ID="litRevenueCompare" runat="server" />
                </p>
            </article>

            <!-- Gross Profit -->
            <article class="kpi-card kpi-card--emerald rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center justify-between mb-2">
                    <span class="text-[11.5px] font-semibold text-ink-500">سود ناخالص</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-4 w-4" data-lucide="coins"></i>
                    </span>
                </div>
                <p class="font-mono text-[20px] font-black text-emerald-700">
                    <asp:Literal ID="litKpiGrossProfit" runat="server" Text="۰" />
                </p>
                <p class="mt-2 text-[10.5px] text-ink-400">
                    حاشیه: <strong class="text-emerald-700"><asp:Literal ID="litKpiGrossMargin" runat="server" Text="—" /></strong>
                </p>
            </article>

            <!-- Net Profit -->
            <article id="cardNetProfitKpi" runat="server"
                class="kpi-card kpi-card--violet rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center justify-between mb-2">
                    <span class="text-[11.5px] font-semibold text-ink-500">سود خالص</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-4 w-4" data-lucide="badge-dollar-sign"></i>
                    </span>
                </div>
                <p id="pNetProfitKpi" runat="server"
                    class="font-mono text-[20px] font-black text-ink-900">
                    <asp:Literal ID="litKpiNetProfit" runat="server" Text="۰" />
                </p>
                <p class="mt-2 text-[10.5px] text-ink-400">
                    هزینه: <strong class="text-amber-700"><asp:Literal ID="litKpiExpenses" runat="server" Text="۰" /></strong>
                </p>
            </article>

            <!-- Pending Services -->
            <article class="kpi-card kpi-card--amber rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center justify-between mb-2">
                    <span class="text-[11.5px] font-semibold text-ink-500">خدمات در انتظار</span>
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-4 w-4" data-lucide="clock"></i>
                    </span>
                </div>
                <p class="font-mono text-[20px] font-black text-amber-700">
                    <asp:Literal ID="litKpiPendingServices" runat="server" Text="۰" />
                </p>
                <p class="mt-2 text-[10.5px] text-ink-400">
                    <asp:Literal ID="litKpiPendingServicesHint" runat="server" Text="سفارش در انتظار تسویه" />
                </p>
            </article>

        </div>
    </section>

    <!-- ═══════════ OPERATIONS SUMMARY ═══════════ -->
    <section class="mb-6">
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="file-text"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">پیش‌فاکتور باز</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-900">
                            <asp:Literal ID="litOpOpenProformas" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="hammer"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">دستور تولید جاری</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-900">
                            <asp:Literal ID="litOpRunningProd" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-danger-50 text-danger-500">
                        <i class="h-5 w-5" data-lucide="package-x"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">موجودی زیر حداقل</p>
                        <p class="mt-0.5 text-[18px] font-bold text-danger-600">
                            <asp:Literal ID="litOpLowStock" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="calendar-check"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">دوره منتظر تقسیم</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-900">
                            <asp:Literal ID="litOpPendingPeriods" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

        </div>
    </section>

    <!-- ═══════════ REVENUE CHART ═══════════ -->
    <section class="mb-6 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
            <h2 class="flex items-center gap-2 text-[15px] font-bold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="line-chart"></i>
                </span>
                روند درآمد ۳۰ روز اخیر
            </h2>
            <div class="flex items-center gap-3 text-[12px]">
                <span class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-3 rounded-sm" style="background: #2563eb;"></span>
                    فروش محصولات
                </span>
                <span class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-3 rounded-sm" style="background: #8b5cf6;"></span>
                    خدمات
                </span>
            </div>
        </div>

        <div class="chart-container">
            <canvas id="revenueChart"></canvas>
        </div>

        <asp:HiddenField ID="hfChartLabels" runat="server" />
        <asp:HiddenField ID="hfChartProductData" runat="server" />
        <asp:HiddenField ID="hfChartServiceData" runat="server" />
    </section>

    <!-- ═══════════ TOP LISTS ═══════════ -->
    <div class="grid gap-5 mb-6 lg:grid-cols-2">

        <!-- Top Products -->
        <section class="rounded-card border border-surface-line bg-surface-card shadow-card">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-3.5 w-3.5" data-lucide="package"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">پرفروش‌ترین محصولات (این ماه)</h2>
                <a href="final-invoices.aspx" class="mr-auto text-[11.5px] text-brand-600 hover:underline">
                    مشاهده همه ←
                </a>
            </div>

            <div class="p-3">
                <asp:Repeater ID="rptTopProducts" runat="server">
                    <ItemTemplate>
                        <div class="activity-item">
                            <span class="rank-badge"><%# Eval("Rank") %></span>
                            <div class="min-w-0 flex-1">
                                <p class="truncate text-[13px] font-semibold text-ink-900">
                                    <%# Enc(Eval("ProductName")) %>
                                </p>
                                <p class="mt-0.5 text-[11px] text-ink-400">
                                    مقدار: <span class="font-mono"><%# FormatQty(Eval("TotalQty")) %></span>
                                    — درآمد: <span class="font-mono font-bold text-emerald-700"><%# FormatMoney(Eval("Revenue")) %></span>
                                </p>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlNoTopProducts" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">در این ماه فروشی ثبت نشده است.</p>
                </asp:Panel>
            </div>
        </section>

        <!-- Top Services -->
        <section class="rounded-card border-2 border-brand-200 bg-gradient-to-l from-brand-50/50 to-transparent shadow-card">
            <div class="flex items-center gap-2 border-b border-brand-200 px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="wrench"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">پرتقاضاترین خدمات (این ماه)</h2>
                <a href="service-orders.aspx" class="mr-auto text-[11.5px] text-brand-600 hover:underline">
                    مشاهده همه ←
                </a>
            </div>

            <div class="p-3">
                <asp:Repeater ID="rptTopServices" runat="server">
                    <ItemTemplate>
                        <div class="activity-item">
                            <span class="rank-badge" style="background: #2563eb;"><%# Eval("Rank") %></span>
                            <div class="min-w-0 flex-1">
                                <p class="truncate text-[13px] font-semibold text-ink-900">
                                    <%# Enc(Eval("ServiceName")) %>
                                </p>
                                <p class="mt-0.5 text-[11px] text-ink-400">
                                    <span class="font-mono"><%# FormatCount(Eval("OrderCount")) %></span> سفارش
                                    — درآمد: <span class="font-mono font-bold text-brand-700"><%# FormatMoney(Eval("Revenue")) %></span>
                                </p>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlNoTopServices" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">در این ماه خدمتی ارائه نشده است.</p>
                </asp:Panel>
            </div>
        </section>

    </div>

    <!-- ═══════════ RECENT ACTIVITY ═══════════ -->
    <div class="grid gap-5 lg:grid-cols-2">

        <!-- Recent Invoices -->
        <section class="rounded-card border border-surface-line bg-surface-card shadow-card">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-3.5 w-3.5" data-lucide="receipt"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">آخرین فاکتورها</h2>
                <a href="final-invoices.aspx" class="mr-auto text-[11.5px] text-brand-600 hover:underline">
                    مشاهده همه ←
                </a>
            </div>

            <div class="p-3">
                <asp:Repeater ID="rptRecentInvoices" runat="server">
                    <ItemTemplate>
                        <div class="activity-item">
                            <span class="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-emerald-50 text-emerald-600">
                                <i class="h-4 w-4" data-lucide="file-check-2"></i>
                            </span>
                            <div class="min-w-0 flex-1">
                                <div class="flex items-center justify-between gap-2">
                                    <p class="truncate text-[12.5px] font-semibold text-ink-900">
                                        <%# Enc(Eval("CustomerName")) %>
                                    </p>
                                    <span class="font-mono text-[12px] font-bold text-emerald-700">
                                        <%# FormatMoney(Eval("Total")) %>
                                    </span>
                                </div>
                                <p class="mt-0.5 flex items-center gap-2 text-[10.5px] text-ink-400 font-mono">
                                    <span><%# Enc(Eval("InvoiceNumber")) %></span>
                                    <span>—</span>
                                    <span><%# FormatShamsiDate(Eval("IssueDate")) %></span>
                                </p>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlNoRecentInvoices" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">فاکتوری ثبت نشده است.</p>
                </asp:Panel>
            </div>
        </section>

        <!-- Recent Service Orders -->
        <section class="rounded-card border border-surface-line bg-surface-card shadow-card">
            <div class="flex items-center gap-2 border-b border-surface-line px-5 py-3.5">
                <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                    <i class="h-3.5 w-3.5" data-lucide="wrench"></i>
                </span>
                <h2 class="text-[14.5px] font-bold text-ink-900">آخرین سفارش‌های خدمات</h2>
                <a href="service-orders.aspx" class="mr-auto text-[11.5px] text-brand-600 hover:underline">
                    مشاهده همه ←
                </a>
            </div>

            <div class="p-3">
                <asp:Repeater ID="rptRecentServices" runat="server">
                    <ItemTemplate>
                        <div class="activity-item">
                            <span class='<%# GetServiceStatusIconCss(Eval("Status")) %>'>
                                <i class="h-4 w-4" data-lucide='<%# GetServiceStatusIcon(Eval("Status")) %>'></i>
                            </span>
                            <div class="min-w-0 flex-1">
                                <div class="flex items-center justify-between gap-2">
                                    <p class="truncate text-[12.5px] font-semibold text-ink-900">
                                        <%# Enc(Eval("CustomerName")) %>
                                    </p>
                                    <%# GetServiceStatusBadge(Eval("Status")) %>
                                </div>
                                <p class="mt-0.5 flex items-center gap-2 text-[10.5px] text-ink-400 font-mono">
                                    <span><%# Enc(Eval("OrderNumber")) %></span>
                                    <span>—</span>
                                    <span><%# Enc(Eval("ServiceName")) %></span>
                                    <span>—</span>
                                    <span><%# FormatShamsiDate(Eval("ServiceDate")) %></span>
                                </p>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlNoRecentServices" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">سفارش خدمتی ثبت نشده است.</p>
                </asp:Panel>
            </div>
        </section>

    </div>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();

        // Revenue chart
        var labels = readHidden('<%= hfChartLabels.ClientID %>');
        var productData = readHidden('<%= hfChartProductData.ClientID %>');
        var serviceData = readHidden('<%= hfChartServiceData.ClientID %>');

        var ctx = document.getElementById('revenueChart');
        if (ctx && labels.length > 0) {
            new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'فروش محصولات',
                            data: productData,
                            borderColor: '#2563eb',
                            backgroundColor: 'rgba(37, 99, 235, 0.08)',
                            borderWidth: 2.5,
                            tension: 0.35,
                            fill: true,
                            pointRadius: 3,
                            pointHoverRadius: 6,
                            pointBackgroundColor: '#2563eb',
                            pointBorderColor: '#fff',
                            pointBorderWidth: 2
                        },
                        {
                            label: 'خدمات',
                            data: serviceData,
                            borderColor: '#8b5cf6',
                            backgroundColor: 'rgba(139, 92, 246, 0.08)',
                            borderWidth: 2.5,
                            tension: 0.35,
                            fill: true,
                            pointRadius: 3,
                            pointHoverRadius: 6,
                            pointBackgroundColor: '#8b5cf6',
                            pointBorderColor: '#fff',
                            pointBorderWidth: 2
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: {
                        mode: 'index',
                        intersect: false
                    },
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
                                font: { family: 'Vazirmatn', size: 10 },
                                color: '#94a3b8',
                                maxRotation: 0,
                                autoSkip: true,
                                maxTicksLimit: 12
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
        try {
            return JSON.parse(el.value);
        } catch (e) {
            return [];
        }
    }

    function formatNumber(n) {
        if (!n || n === 0) return '۰';
        return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '،');
    }
</script>
</asp:Content>