<%@ Page Title="فاکتورهای نهایی" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="final-invoices.aspx.cs" Inherits="admin_final_invoices" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .fi-items-grid {
        display: grid;
        grid-template-columns: 1.5fr 80px 110px 100px 100px 120px 120px;
        gap: 8px;
        align-items: center;
        padding: 10px 12px;
        border-radius: 8px;
        font-size: 12.5px;
        transition: all .15s;
    }
    .fi-items-grid:hover { background: #f8fafc; }
    .fi-items-header {
        background: #f1f5f9;
        font-weight: 700;
        color: #475569;
        font-size: 11px;
        text-transform: uppercase;
        border-radius: 8px;
    }
    .state-banner { position: relative; overflow: hidden; }
    .state-banner::before {
        content: '';
        position: absolute;
        right: 0; top: 0; bottom: 0;
        width: 4px;
    }
    .state-banner--issued::before { background: #3b82f6; }
    .state-banner--paid::before { background: #10b981; }
    .state-banner--cancelled::before { background: #ef4444; }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfCurrentInvoiceId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">فاکتورهای نهایی</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="فاکتورهای نهایی" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="فاکتورهای نهایی فروش، بهای تمام‌شده، و سود را مدیریت کنید." />
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:PlaceHolder ID="phBackToList" runat="server" Visible="false">
                <asp:LinkButton ID="btnBackToList" runat="server" OnClick="btnBackToList_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="arrow-right"></i>
                    بازگشت
                </asp:LinkButton>
            </asp:PlaceHolder>

            <a href="proforma-invoices.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="file-text"></i>
                پیش‌فاکتورها
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
                        <i class="h-5 w-5" data-lucide="receipt"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">کل فاکتورها</p>
                        <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-100 text-brand-700">
                        <i class="h-5 w-5" data-lucide="clock"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">در انتظار پرداخت</p>
                        <p class="mt-0.5 text-[20px] font-bold text-brand-700">
                            <asp:Literal ID="litStatIssued" runat="server" Text="۰" />
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
                        <p class="text-[11.5px] text-ink-500">پرداخت‌شده (این ماه)</p>
                        <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatPaid" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-danger-50 text-danger-500">
                        <i class="h-5 w-5" data-lucide="trash-2"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">حذف‌شده</p>
                        <p class="mt-0.5 text-[20px] font-bold text-danger-600">
                            <asp:Literal ID="litStatDeleted" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

        </div>

        <!-- Filters -->
        <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-4">

                <label class="relative block lg:col-span-2">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-600"
                        placeholder="جستجو در شماره فاکتور یا نام مشتری..." />
                </label>

                <asp:DropDownList ID="ddlFilterStatus" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="در انتظار پرداخت" />
                    <asp:ListItem Value="2" Text="پرداخت‌شده" />
                    <asp:ListItem Value="3" Text="لغو‌شده" />
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterCustomer" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                </asp:DropDownList>

            </div>

            <div class="mt-3 flex items-center gap-2">
                <asp:CheckBox ID="chkShowDeleted" runat="server" AutoPostBack="true"
                    OnCheckedChanged="FilterChanged"
                    CssClass="h-4 w-4 rounded border-ink-300 text-brand-600" />
                <label class="text-[12.5px] text-ink-600 cursor-pointer"
                    onclick="document.getElementById('<%= chkShowDeleted.ClientID %>').click();">
                    نمایش موارد حذف‌شده
                </label>
            </div>
        </section>

        <!-- Table -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1100px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">شماره</th>
                            <th class="pb-3 pr-4 text-right font-semibold">مشتری</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مبلغ کل</th>
                            <th class="pb-3 pr-4 text-center font-semibold">COGS</th>
                            <th class="pb-3 pr-4 text-center font-semibold">سود</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptInvoices" runat="server" OnItemCommand="rptInvoices_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("Status"), Eval("IsDeleted")) %>'>
                                    <td class="py-3 pr-4">
                                        <p class="font-mono font-bold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("InvoiceNumber")) %>
                                            <%# GetDeletedBadge(Eval("IsDeleted")) %>
                                        </p>
                                        <p class="mt-0.5 font-mono text-[10.5px] text-ink-400">
                                            <%# FormatShamsiDate(Eval("IssueDate")) %>
                                        </p>
                                    </td>
                                    <td class="py-3 pr-4">
                                        <p class="font-semibold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("CustomerName")) %>
                                        </p>
                                        <%# GetCustomerTypeHint(Eval("OrderType")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono font-bold text-emerald-700">
                                            <%# FormatMoney(Eval("Total")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono text-[12.5px] text-rose-600">
                                            <%# FormatMoney(Eval("TotalCogs")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetProfitCell(Eval("Total"), Eval("TotalCogs")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetStatusBadge(Eval("Status"), Eval("IsDeleted")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:LinkButton ID="btnView" runat="server"
                                                CommandName="View"
                                                CommandArgument='<%# Eval("FinalInvoiceId") %>'
                                                CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700"
                                                Visible='<%# !IsTrue(Eval("IsDeleted")) %>'>
                                                <i class="h-3.5 w-3.5" data-lucide="eye"></i>
                                                مشاهده
                                            </asp:LinkButton>

                                            <a href='<%# "final-invoice-print.aspx?id=" + Eval("FinalInvoiceId") %>'
                                                target="_blank"
                                                class="icon-button hover:bg-violet-50 hover:text-violet-600"
                                                title="چاپ">
                                                <i class="h-4 w-4" data-lucide="printer"></i>
                                            </a>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore"
                                                    CommandArgument='<%# Eval("FinalInvoiceId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                                    OnClientClick='<%# GetRestoreConfirm(Eval("InvoiceNumber")) %>'>
                                                    <i class="h-3.5 w-3.5" data-lucide="rotate-ccw"></i>
                                                    بازیابی
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

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="receipt"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">فاکتوری یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">
                    فاکتورهای نهایی از تبدیل پیش‌فاکتورهای «پرداخت‌شده» ساخته می‌شوند.
                    <a href="proforma-invoices.aspx" class="text-brand-600 hover:underline">رفتن به پیش‌فاکتورها</a>
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
         VIEW 2: DETAIL
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlDetailView" runat="server" Visible="false">

        <!-- ═══ State banner ═══ -->
        <div id="divStateBanner" runat="server"
            class="state-banner state-banner--issued mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

            <div class="flex flex-wrap items-center gap-4">
                <span id="divStateIcon" runat="server"
                    class="grid h-12 w-12 place-items-center rounded-xl bg-brand-100 text-brand-700">
                    <i class="h-6 w-6" data-lucide="receipt"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[11.5px] font-semibold text-ink-500">
                        <asp:Literal ID="litDetailStateText" runat="server" Text="فاکتور نهایی" />
                    </p>
                    <p class="mt-0.5 text-[20px] font-extrabold text-ink-900 font-mono">
                        <asp:Literal ID="litDetailNumber" runat="server" />
                    </p>
                    <p class="mt-1 text-[12.5px] text-ink-600">
                        <asp:Literal ID="litDetailCustomerInfo" runat="server" />
                    </p>
                </div>
                <div class="text-left">
                    <p class="text-[11.5px] text-ink-500">مبلغ کل</p>
                    <p class="mt-1 font-mono text-[20px] font-black text-emerald-700">
                        <asp:Literal ID="litDetailTotal" runat="server" Text="۰" />
                        <span class="text-[11px] font-normal text-ink-500">تومان</span>
                    </p>
                </div>
            </div>

            <div class="mt-4 grid gap-3 sm:grid-cols-4">
                <div class="rounded-base bg-surface-muted px-3 py-2">
                    <p class="text-[11px] text-ink-500">تاریخ صدور</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-ink-800">
                        <asp:Literal ID="litDetailIssueDate" runat="server" />
                    </p>
                </div>
                <div class="rounded-base bg-surface-muted px-3 py-2">
                    <p class="text-[11px] text-ink-500">وضعیت</p>
                    <div class="mt-0.5">
                        <asp:Literal ID="litDetailStatusBadge" runat="server" />
                    </div>
                </div>
                <div class="rounded-base bg-rose-50 px-3 py-2">
                    <p class="text-[11px] text-rose-700">بهای تمام شده (COGS)</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-rose-700">
                        <asp:Literal ID="litDetailCogs" runat="server" Text="۰" />
                    </p>
                </div>
                <div id="divProfitBox" runat="server"
                    class="rounded-base bg-emerald-50 px-3 py-2">
                    <p class="text-[11px] text-emerald-700">سود ناخالص</p>
                    <p id="pDetailProfit" runat="server"
                        class="mt-0.5 font-mono text-[13px] font-bold text-emerald-700">
                        <asp:Literal ID="litDetailProfit" runat="server" Text="۰" />
                    </p>
                </div>
            </div>

        </div>

        <!-- ═══ Info ═══ -->
        <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <h2 class="mb-4 border-b border-surface-line pb-3 text-[15px] font-bold text-ink-900">اطلاعات فاکتور</h2>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

                <div>
                    <p class="text-[11.5px] text-ink-500">کانال فروش</p>
                    <p class="mt-0.5 text-[13px] font-semibold text-ink-800">
                        <asp:Literal ID="litInfoChannel" runat="server" />
                    </p>
                </div>

                <div>
                    <p class="text-[11.5px] text-ink-500">نوع مشتری</p>
                    <p class="mt-0.5 text-[13px] font-semibold text-ink-800">
                        <asp:Literal ID="litInfoOrderType" runat="server" />
                    </p>
                </div>

                <div>
                    <p class="text-[11.5px] text-ink-500">مبلغ پرداخت‌شده</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-emerald-700">
                        <asp:Literal ID="litInfoPaidAmount" runat="server" />
                    </p>
                </div>

                <div>
                    <p class="text-[11.5px] text-ink-500">مانده قابل پرداخت</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-amber-700">
                        <asp:Literal ID="litInfoRemaining" runat="server" />
                    </p>
                </div>

                <asp:Panel ID="pnlInfoProforma" runat="server" Visible="false">
                    <div class="lg:col-span-4">
                        <p class="text-[11.5px] text-ink-500">پیش‌فاکتور مرجع</p>
                        <p class="mt-0.5">
                            <a id="aProformaLink" runat="server" class="text-[13px] font-semibold text-brand-600 hover:underline font-mono">
                                <asp:Literal ID="litInfoProformaNumber" runat="server" />
                            </a>
                        </p>
                    </div>
                </asp:Panel>

            </div>
        </article>

        <!-- ═══ Items ═══ -->
        <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-4 flex items-center justify-between">
                <h2 class="flex items-center gap-2 text-[15px] font-bold text-ink-900">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="package"></i>
                    </span>
                    اقلام فاکتور و بهای تمام‌شده
                </h2>
                <p class="text-[12.5px] text-ink-500">
                    <asp:Literal ID="litItemsInfo" runat="server" />
                </p>
            </div>

            <div class="fi-items-grid fi-items-header">
                <div>محصول</div>
                <div class="text-center">تعداد</div>
                <div class="text-center">قیمت واحد</div>
                <div class="text-center">تخفیف</div>
                <div class="text-center">جمع قلم</div>
                <div class="text-center">COGS</div>
                <div class="text-center">سود قلم</div>
            </div>

            <asp:Repeater ID="rptDetailItems" runat="server">
                <ItemTemplate>
                    <div class="fi-items-grid border-b border-surface-line/50">
                        <div class="flex items-center gap-2 min-w-0">
                            <span class="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-violet-50 text-violet-600">
                                <i class="h-4 w-4" data-lucide="package"></i>
                            </span>
                            <div class="min-w-0">
                                <p class="truncate font-semibold text-ink-900 text-[12.5px]">
                                    <%# Enc(Eval("ProductName")) %>
                                </p>
                                <p class="truncate font-mono text-[10.5px] text-ink-400" dir="ltr">
                                    <%# Enc(Eval("ProductCode")) %>
                                </p>
                            </div>
                        </div>
                        <div class="text-center font-mono font-bold text-ink-800">
                            <%# FormatQty(Eval("Quantity")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-ink-600">
                            <%# FormatMoney(Eval("UnitPrice")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-rose-600">
                            <%# FormatMoneyOrDash(Eval("Discount")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-emerald-700">
                            <%# FormatMoney(Eval("Total")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-rose-600">
                            <%# FormatMoney(Eval("COGS")) %>
                        </div>
                        <div class="text-center">
                            <%# GetItemProfitBadge(Eval("Total"), Eval("COGS")) %>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlDetailItemsEmpty" runat="server" Visible="false" CssClass="py-10 text-center">
                <p class="text-[13px] text-ink-400">قلمی در این فاکتور ثبت نشده.</p>
            </asp:Panel>

        </article>

        <!-- ═══ Totals ═══ -->
        <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <h2 class="mb-4 border-b border-surface-line pb-3 text-[15px] font-bold text-ink-900">جمع‌بندی</h2>

            <div class="flex flex-wrap items-center justify-end gap-6">
                <div class="text-right">
                    <p class="text-[11.5px] text-ink-500">جمع جزء</p>
                    <p class="mt-0.5 font-mono font-bold text-ink-800">
                        <asp:Literal ID="litSubtotal" runat="server" Text="۰" />
                        <span class="text-[11px] font-normal text-ink-500">تومان</span>
                    </p>
                </div>
                <div class="text-right">
                    <p class="text-[11.5px] text-ink-500">تخفیف</p>
                    <p class="mt-0.5 font-mono font-bold text-rose-600">
                        (<asp:Literal ID="litDiscount" runat="server" Text="۰" />)
                    </p>
                </div>
                <div class="text-right">
                    <p class="text-[11.5px] text-ink-500">مالیات</p>
                    <p class="mt-0.5 font-mono font-bold text-ink-700">
                        <asp:Literal ID="litTax" runat="server" Text="۰" />
                    </p>
                </div>
                <div class="rounded-base bg-emerald-50 border border-emerald-200 px-4 py-2 text-right">
                    <p class="text-[11.5px] font-bold text-emerald-700">مبلغ کل</p>
                    <p class="mt-0.5 font-mono text-[18px] font-black text-emerald-800">
                        <asp:Literal ID="litTotal" runat="server" Text="۰" />
                    </p>
                </div>
                <div class="rounded-base bg-rose-50 border border-rose-200 px-4 py-2 text-right">
                    <p class="text-[11.5px] font-bold text-rose-700">COGS</p>
                    <p class="mt-0.5 font-mono text-[16px] font-black text-rose-800">
                        <asp:Literal ID="litTotalCogs" runat="server" Text="۰" />
                    </p>
                </div>
                <div id="divTotalProfitBox" runat="server"
                    class="rounded-base bg-emerald-100 border-2 border-emerald-300 px-4 py-2 text-right">
                    <p class="text-[11.5px] font-bold text-emerald-800">سود ناخالص</p>
                    <p id="pTotalProfit" runat="server"
                        class="mt-0.5 font-mono text-[18px] font-black text-emerald-800">
                        <asp:Literal ID="litTotalProfit" runat="server" Text="۰" />
                    </p>
                </div>
            </div>

            <asp:Panel ID="pnlNotes" runat="server" Visible="false" CssClass="mt-4 border-t border-surface-line pt-3">
                <p class="text-[11.5px] text-ink-500">یادداشت</p>
                <p class="mt-1 text-[12.5px] text-ink-700 leading-relaxed">
                    <asp:Literal ID="litNotes" runat="server" />
                </p>
            </asp:Panel>
        </article>

        <!-- ═══ Actions bar ═══ -->
        <div class="flex flex-wrap items-center justify-between gap-3 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">

            <div class="flex flex-wrap items-center gap-2">
                <asp:PlaceHolder ID="phActionMarkPaid" runat="server" Visible="false">
                    <asp:Button ID="btnMarkPaid" runat="server" Text="تأیید پرداخت"
                        OnClick="btnMarkPaid_Click"
                        OnClientClick="return confirm('پرداخت این فاکتور تأیید شود؟');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-emerald-600 px-4 text-[13.5px] font-semibold text-white hover:bg-emerald-700" />
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionCancel" runat="server" Visible="false">
                    <asp:LinkButton ID="btnCancelInvoice" runat="server" OnClick="btnCancelInvoice_Click"
                        OnClientClick="return confirm('فاکتور لغو شود؟\n\nتوجه: کالاها به انبار بازگردانده می‌شوند و موجودی اصلاح می‌شود.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border-2 border-danger-500 bg-white px-4 text-[13.5px] font-semibold text-danger-600 hover:bg-danger-50">
                        <i class="h-4 w-4" data-lucide="x-circle"></i>
                        لغو فاکتور
                    </asp:LinkButton>
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionPrint" runat="server" Visible="false">
                    <asp:HyperLink ID="lnkPrint" runat="server" Target="_blank"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line bg-white px-4 text-[13.5px] font-semibold text-ink-700 hover:bg-surface-muted">
                        <i class="h-4 w-4" data-lucide="printer"></i>
                        چاپ فاکتور
                    </asp:HyperLink>
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionDelete" runat="server" Visible="false">
                    <asp:LinkButton ID="btnSoftDelete" runat="server" OnClick="btnSoftDelete_Click"
                        OnClientClick="return confirm('فاکتور حذف شود؟\n\nتوجه: قابل بازیابی است.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-danger-300 bg-white px-4 text-[13.5px] font-semibold text-danger-600 hover:bg-danger-50">
                        <i class="h-4 w-4" data-lucide="trash-2"></i>
                        حذف
                    </asp:LinkButton>
                </asp:PlaceHolder>
            </div>

            <asp:PlaceHolder ID="phProformaLink" runat="server" Visible="false">
                <a id="aProformaBack" runat="server"
                    class="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line bg-white px-4 text-[13.5px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="external-link"></i>
                    مشاهده پیش‌فاکتور مرجع
                </a>
            </asp:PlaceHolder>

        </div>

    </asp:Panel>

</asp:Content>