<%@ Page Title="مرکز گزارش‌ها" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="reports-hub.aspx.cs" Inherits="admin_reports_hub" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .report-card {
        position: relative;
        overflow: hidden;
        transition: all .25s cubic-bezier(.4, 0, .2, 1);
        text-decoration: none;
    }
    .report-card:hover {
        transform: translateY(-3px);
        box-shadow: 0 20px 40px -16px rgba(15, 23, 42, 0.18);
    }
    .report-card::after {
        content: '';
        position: absolute;
        top: -30px;
        left: -30px;
        width: 100px;
        height: 100px;
        border-radius: 50%;
        opacity: 0.05;
        pointer-events: none;
        transition: all .3s;
    }
    .report-card:hover::after {
        opacity: 0.10;
        transform: scale(1.2);
    }
    .report-card--brand::after { background: #2563eb; }
    .report-card--emerald::after { background: #10b981; }
    .report-card--amber::after { background: #f59e0b; }
    .report-card--violet::after { background: #8b5cf6; }
    .report-card--rose::after { background: #ef4444; }
    .report-card--sky::after { background: #0ea5e9; }
    .report-card--slate::after { background: #475569; }

    .report-icon {
        display: grid;
        place-items: center;
        width: 48px;
        height: 48px;
        border-radius: 14px;
        transition: all .2s;
    }
    .report-card:hover .report-icon {
        transform: scale(1.08);
    }

    .hub-stat {
        transition: all .2s;
    }
    .hub-stat:hover {
        transform: translateY(-2px);
    }
    .hub-stat::before {
        content: '';
        position: absolute;
        right: 0;
        top: 0;
        bottom: 0;
        width: 3px;
        border-radius: 0 3px 3px 0;
    }
    .hub-stat--brand::before { background: #2563eb; }
    .hub-stat--emerald::before { background: #10b981; }
    .hub-stat--amber::before { background: #f59e0b; }
    .hub-stat--violet::before { background: #8b5cf6; }

    /* ─── Print ─── */
    @media print {
        @page { size: A4 portrait; margin: 12mm 10mm; }
        .admin-sidebar, .admin-topbar, footer, .no-print { display: none !important; }
        [class*="lg:mr-"] { margin-right: 0 !important; }
        main.admin-content { padding: 0 !important; }
        .report-card { box-shadow: none !important; border: 1px solid #cbd5e1 !important; }
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">مرکز گزارش‌ها</span>
            </nav>
            <h1 class="text-[24px] font-semibold text-ink-900">مرکز گزارش‌ها</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                دسترسی سریع به تمام گزارش‌ها و تحلیل‌های کسب‌وکار — امروز،
                <asp:Literal ID="litTodayShamsi" runat="server" Text="—" />
            </p>
        </div>
        <div class="flex items-center gap-2 no-print">
            <a href="Default.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="layout-dashboard"></i>
                داشبورد
            </a>
        </div>
    </div>

    <!-- ═══════════ Live Stats ═══════════ -->
    <section class="mb-6">
        <h2 class="mb-3 flex items-center gap-2 text-[14px] font-bold text-ink-900">
            <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                <i class="h-3.5 w-3.5" data-lucide="gauge"></i>
            </span>
            آمار زنده
            <span class="text-[11px] font-normal text-ink-400 mr-1">
                (این ماه)
            </span>
        </h2>

        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <a href="profit-loss.aspx" class="hub-stat hub-stat--brand relative rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="trending-up"></i>
                    </span>
                    <div class="min-w-0">
                        <p class="text-[11.5px] text-ink-500">درآمد کل</p>
                        <p class="mt-0.5 font-mono text-[16px] font-bold text-ink-900">
                            <asp:Literal ID="litStatRevenue" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">تومان</p>
                    </div>
                </div>
            </a>

            <a href="profit-loss.aspx" class="hub-stat hub-stat--emerald relative rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-5 w-5" data-lucide="badge-dollar-sign"></i>
                    </span>
                    <div class="min-w-0">
                        <p class="text-[11.5px] text-ink-500">سود خالص</p>
                        <p id="pStatProfit" runat="server"
                            class="mt-0.5 font-mono text-[16px] font-bold text-emerald-700">
                            <asp:Literal ID="litStatProfit" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">تومان</p>
                    </div>
                </div>
            </a>

            <a href="stock-onhand.aspx" class="hub-stat hub-stat--violet relative rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="boxes"></i>
                    </span>
                    <div class="min-w-0">
                        <p class="text-[11.5px] text-ink-500">ارزش موجودی انبار</p>
                        <p class="mt-0.5 font-mono text-[16px] font-bold text-violet-700">
                            <asp:Literal ID="litStatStockValue" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">تومان</p>
                    </div>
                </div>
            </a>

            <a href="service-orders.aspx" class="hub-stat hub-stat--amber relative rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="wrench"></i>
                    </span>
                    <div class="min-w-0">
                        <p class="text-[11.5px] text-ink-500">خدمات در جریان</p>
                        <p class="mt-0.5 font-mono text-[16px] font-bold text-amber-700">
                            <asp:Literal ID="litStatPendingServices" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">سفارش</p>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ FINANCIAL ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-emerald-100 text-emerald-700">
                <i class="h-4 w-4" data-lucide="wallet"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">مالی و سود</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="profit-loss.aspx" class="report-card report-card--emerald rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-emerald-50 text-emerald-600">
                        <i class="h-6 w-6" data-lucide="bar-chart-3"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">گزارش سود و زیان</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            تحلیل کامل درآمد (فروش + خدمات)، COGS، هزینه‌ها، و سود خالص در بازه دلخواه.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-emerald-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="expenses.aspx" class="report-card report-card--rose rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-rose-50 text-rose-600">
                        <i class="h-6 w-6" data-lucide="receipt"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">گزارش هزینه‌ها</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست هزینه‌ها با فیلتر تاریخ، دسته، و روش پرداخت. قابل چاپ و تحلیل.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-rose-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="expense-categories.aspx" class="report-card report-card--slate rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-surface-muted text-ink-600">
                        <i class="h-6 w-6" data-lucide="layers"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">تحلیل دسته‌های هزینه</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            مجموع هزینه‌ها به تفکیک دسته با درصد سهم هر دسته از کل.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-ink-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ SALES ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-brand-100 text-brand-700">
                <i class="h-4 w-4" data-lucide="shopping-bag"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">فروش و خدمات</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="final-invoices.aspx" class="report-card report-card--brand rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-brand-50 text-brand-600">
                        <i class="h-6 w-6" data-lucide="receipt"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">فاکتورهای نهایی فروش</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست فاکتورهای صادرشده با فیلتر مشتری، تاریخ، و وضعیت پرداخت.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-brand-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="proforma-invoices.aspx" class="report-card report-card--amber rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-amber-50 text-amber-600">
                        <i class="h-6 w-6" data-lucide="file-text"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">پیش‌فاکتورهای در جریان</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            پیش‌فاکتورهای صادرشده، پرداخت‌شده، و تبدیل‌شده با تحلیل رزرو موجودی.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-amber-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="sales-orders.aspx" class="report-card report-card--sky rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-sky-50 text-sky-600">
                        <i class="h-6 w-6" data-lucide="clipboard-list"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">سفارش‌های فروش</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            سفارش‌های مشتریان به تفکیک کانال فروش (بازاریاب، آنلاین، ادمین).
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-sky-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="service-orders.aspx" class="report-card report-card--violet rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-violet-50 text-violet-600">
                        <i class="h-6 w-6" data-lucide="wrench"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">سفارش‌های خدمات</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            خدمات جانبی (بوجاری، خشک‌کن، آسیاب) با فیلتر مشتری و بازه تاریخ.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-violet-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="service-types.aspx" class="report-card report-card--violet rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-violet-50 text-violet-600">
                        <i class="h-6 w-6" data-lucide="settings"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">کاتالوگ خدمات</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست خدمات قابل ارائه با نرخ پیش‌فرض، ضایعات، و تعداد سفارش‌ها.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-violet-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="customers.aspx" class="report-card report-card--brand rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-brand-50 text-brand-600">
                        <i class="h-6 w-6" data-lucide="users"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">تحلیل مشتریان</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            مشتریان به تفکیک نوع (تک‌فروشی، عمده، نمایندگی، کشاورز) و شهر.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-brand-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ INVENTORY ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-amber-100 text-amber-700">
                <i class="h-4 w-4" data-lucide="warehouse"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">انبار و موجودی</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="stock-onhand.aspx" class="report-card report-card--amber rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-amber-50 text-amber-600">
                        <i class="h-6 w-6" data-lucide="boxes"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">موجودی لحظه‌ای انبار</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            موجودی همه محصولات به تفکیک انبار با نمایش لایه‌های FIFO و ارزش ریالی.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-amber-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="stock-movements.aspx" class="report-card report-card--sky rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-sky-50 text-sky-600">
                        <i class="h-6 w-6" data-lucide="arrow-left-right"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">گردش انبار</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            تاریخچه همه ورود/خروج کالا با نوع حرکت، مرجع سند، و مقدار.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-sky-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="products.aspx" class="report-card report-card--slate rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-surface-muted text-ink-600">
                        <i class="h-6 w-6" data-lucide="package"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">کاتالوگ محصولات</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست محصولات به تفکیک نوع (RM / FG / PKG) با حداقل موجودی.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-ink-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ OPERATIONS ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-violet-100 text-violet-700">
                <i class="h-4 w-4" data-lucide="hammer"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">خرید و تولید</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="purchase-orders.aspx" class="report-card report-card--violet rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-violet-50 text-violet-600">
                        <i class="h-6 w-6" data-lucide="shopping-cart"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">سفارش‌های خرید</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            خرید مواد اولیه از تأمین‌کنندگان با فیلتر تاریخ و وضعیت.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-violet-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="production-orders.aspx" class="report-card report-card--amber rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-amber-50 text-amber-600">
                        <i class="h-6 w-6" data-lucide="hammer"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">دستورهای تولید</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            دستورهای تولید با مصرف FIFO، بهای تمام‌شده، و وضعیت اجرا.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-amber-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="boms.aspx" class="report-card report-card--sky rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-sky-50 text-sky-600">
                        <i class="h-6 w-6" data-lucide="git-branch"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">فرمول‌های ساخت (BOM)</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست BOM ها با نسخه، تعداد مواد اولیه، و استفاده در تولید.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-sky-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ PARTNERS ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-emerald-100 text-emerald-700">
                <i class="h-4 w-4" data-lucide="users-round"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">شرکا و تقسیم سود</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="partners.aspx" class="report-card report-card--emerald rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-emerald-50 text-emerald-600">
                        <i class="h-6 w-6" data-lucide="users-round"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">شرکا و سهام</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            لیست شرکا با درصد سهام فعلی، تاریخچه تغییرات، و برداشت‌ها.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-emerald-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="profit-periods.aspx" class="report-card report-card--emerald rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-emerald-50 text-emerald-600">
                        <i class="h-6 w-6" data-lucide="calendar-check"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">دوره‌های تقسیم سود</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            دوره‌های ماهانه/سه‌ماهه با سود خالص، سهم شرکا، و پرداخت‌ها.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-emerald-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="profit-settings.aspx" class="report-card report-card--slate rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-surface-muted text-ink-600">
                        <i class="h-6 w-6" data-lucide="settings-2"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">تنظیمات تقسیم سود</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            تنظیمات دوره‌ها، سقف برداشت، ذخیره احتیاطی، و تولید خودکار دوره.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-ink-600">
                            مشاهده تنظیمات
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ SYSTEM & AUDIT ═══════════ -->
    <section class="mb-6">
        <div class="mb-3 flex items-center gap-2">
            <span class="grid h-7 w-7 place-items-center rounded-base bg-surface-muted text-ink-700">
                <i class="h-4 w-4" data-lucide="shield"></i>
            </span>
            <h2 class="text-[15px] font-bold text-ink-900">سیستم و امنیت</h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

            <a href="audit-logs.aspx" class="report-card report-card--slate rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-surface-muted text-ink-600">
                        <i class="h-6 w-6" data-lucide="scroll-text"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">لاگ فعالیت‌ها</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            تمام عملیات حساس سیستم با کاربر، زمان، و تغییرات.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-ink-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="users.aspx" class="report-card report-card--brand rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-brand-50 text-brand-600">
                        <i class="h-6 w-6" data-lucide="user-cog"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">کاربران و نقش‌ها</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            کاربران سیستم به تفکیک نقش با آخرین ورود و وضعیت.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-brand-600">
                            مشاهده گزارش
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

            <a href="Default.aspx" class="report-card report-card--brand rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-start gap-4">
                    <span class="report-icon bg-brand-50 text-brand-600">
                        <i class="h-6 w-6" data-lucide="layout-dashboard"></i>
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="text-[15px] font-bold text-ink-900">داشبورد مدیریتی</p>
                        <p class="mt-1 text-[12.5px] text-ink-500 leading-relaxed">
                            نمای کلی KPI ها، نمودار روند درآمد، و Top Products / Services.
                        </p>
                        <span class="mt-3 inline-flex items-center gap-1 text-[12px] font-bold text-brand-600">
                            مشاهده داشبورد
                            <i class="h-3.5 w-3.5" data-lucide="arrow-left"></i>
                        </span>
                    </div>
                </div>
            </a>

        </div>
    </section>

    <!-- ═══════════ QUICK LINKS FOOTER ═══════════ -->
    <section class="rounded-card border border-dashed border-brand-200 bg-brand-50/30 p-5">
        <div class="flex flex-wrap items-center justify-between gap-3">
            <div>
                <p class="text-[13.5px] font-bold text-ink-800">
                    <i class="inline h-4 w-4 align-middle ml-1 text-brand-600" data-lucide="info"></i>
                    راهنمای سریع
                </p>
                <p class="mt-1 text-[12px] text-ink-600 leading-relaxed">
                    برای دیدن گزارش دقیق‌تر، روی هر کارت کلیک کنید. اکثر گزارش‌ها فیلتر تاریخ و خروجی چاپ دارند.
                </p>
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <a href="profit-loss.aspx"
                    class="inline-flex h-9 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700">
                    <i class="h-3.5 w-3.5" data-lucide="bar-chart-3"></i>
                    سود و زیان
                </a>
                <a href="stock-onhand.aspx"
                    class="inline-flex h-9 items-center gap-1.5 rounded-base bg-amber-600 px-3 text-[12px] font-bold text-white hover:bg-amber-700">
                    <i class="h-3.5 w-3.5" data-lucide="boxes"></i>
                    موجودی
                </a>
                <a href="audit-logs.aspx"
                    class="inline-flex h-9 items-center gap-1.5 rounded-base bg-ink-700 px-3 text-[12px] font-bold text-white hover:bg-ink-800">
                    <i class="h-3.5 w-3.5" data-lucide="scroll-text"></i>
                    لاگ فعالیت‌ها
                </a>
            </div>
        </div>
    </section>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
</script>
</asp:Content>