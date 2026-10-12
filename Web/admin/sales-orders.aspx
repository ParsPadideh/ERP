<%@ Page Title="سفارش‌های فروش" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="sales-orders.aspx.cs" Inherits="admin_sales_orders" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditSOId" runat="server" Value="0" />
    <asp:HiddenField ID="hfCurrentSOId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditItemId" runat="server" Value="0" />
    <asp:HiddenField ID="hfStatusFilter" runat="server" Value="" />
    <asp:HiddenField ID="hfChannelFilter" runat="server" Value="" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">سفارش‌های فروش</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="سفارش‌های فروش" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="ثبت و پیگیری سفارش‌های فروش از سه کانال بازاریاب، آنلاین و ادمین." />
            </p>
        </div>
        <div class="flex items-center gap-2">

            <asp:PlaceHolder ID="phBackToList" runat="server" Visible="false">
                <asp:LinkButton ID="btnBackToList" runat="server" OnClick="btnBackToList_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="arrow-right"></i>
                    بازگشت به لیست
                </asp:LinkButton>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddNew" runat="server">
                <asp:Button ID="btnShowAddForm" runat="server" Text="سفارش فروش جدید"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>

        </div>
    </div>

    <!-- ═══════════ VIEW 1: List ═══════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats -->
        <asp:Panel ID="pnlStats" runat="server" CssClass="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
                <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-brand-500 to-brand-400"></div>
                <div class="flex items-start justify-between">
                    <div>
                        <p class="text-[12px] font-semibold text-ink-500">سفارش‌های باز</p>
                        <p class="mt-2 text-[22px] font-extrabold text-ink-900">
                            <asp:Literal ID="litStatOpen" runat="server" Text="۰" />
                        </p>
                        <p class="mt-1 text-[11px] font-semibold text-ink-400">پیش‌نویس / تأیید / آماده</p>
                    </div>
                    <span class="grid h-11 w-11 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="clipboard-list"></i>
                    </span>
                </div>
            </article>

            <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
                <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-violet-500 to-violet-400"></div>
                <div class="flex items-start justify-between">
                    <div>
                        <p class="text-[12px] font-semibold text-ink-500">ارسال‌شده</p>
                        <p class="mt-2 text-[22px] font-extrabold text-violet-600">
                            <asp:Literal ID="litStatShipped" runat="server" Text="۰" />
                        </p>
                        <p class="mt-1 text-[11px] font-semibold text-ink-400">در مسیر مشتری</p>
                    </div>
                    <span class="grid h-11 w-11 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="truck"></i>
                    </span>
                </div>
            </article>

            <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
                <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-emerald-500 to-emerald-400"></div>
                <div class="flex items-start justify-between">
                    <div>
                        <p class="text-[12px] font-semibold text-ink-500">تحویل‌شده (ماه)</p>
                        <p class="mt-2 text-[22px] font-extrabold text-emerald-600">
                            <asp:Literal ID="litStatDelivered" runat="server" Text="۰" />
                        </p>
                        <p class="mt-1 text-[11px] font-semibold text-ink-400">۳۰ روز اخیر</p>
                    </div>
                    <span class="grid h-11 w-11 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                    </span>
                </div>
            </article>

            <article class="group relative overflow-hidden rounded-card border border-surface-line bg-surface-card p-5 shadow-card transition hover:shadow-card-lg">
                <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-l from-amber-500 to-amber-400"></div>
                <div class="flex items-start justify-between">
                    <div>
                        <p class="text-[12px] font-semibold text-ink-500">ارزش سفارش‌های باز</p>
                        <p class="mt-2 text-[22px] font-extrabold text-amber-600">
                            <asp:Literal ID="litStatOpenValue" runat="server" Text="۰" />
                        </p>
                        <p class="mt-1 text-[11px] font-semibold text-ink-400">تومان</p>
                    </div>
                    <span class="grid h-11 w-11 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="wallet"></i>
                    </span>
                </div>
            </article>

        </asp:Panel>

        <!-- Filter -->
        <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

            <!-- Status quick filter -->
            <div class="mb-3 flex flex-wrap items-center gap-2">
                <span class="text-[12.5px] font-bold text-ink-700 ml-2">وضعیت:</span>

                <button type="button" onclick="setStatusFilter(''); return false;" id="btnStAll"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    همه
                </button>
                <button type="button" onclick="setStatusFilter('1'); return false;" id="btnStDraft"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="edit"></i>
                    پیش‌نویس
                </button>
                <button type="button" onclick="setStatusFilter('2'); return false;" id="btnStApproved"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="check"></i>
                    تأییدشده
                </button>
                <button type="button" onclick="setStatusFilter('3'); return false;" id="btnStReady"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="package"></i>
                    آماده‌سازی
                </button>
                <button type="button" onclick="setStatusFilter('4'); return false;" id="btnStShipped"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="truck"></i>
                    ارسال‌شده
                </button>
                <button type="button" onclick="setStatusFilter('5'); return false;" id="btnStDelivered"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="check-circle-2"></i>
                    تحویل‌شده
                </button>
                <button type="button" onclick="setStatusFilter('6'); return false;" id="btnStCancelled"
                    class="st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="x-circle"></i>
                    لغوشده
                </button>
            </div>

            <!-- Channel quick filter -->
            <div class="mb-4 flex flex-wrap items-center gap-2">
                <span class="text-[12.5px] font-bold text-ink-700 ml-2">کانال:</span>

                <button type="button" onclick="setChannelFilter(''); return false;" id="btnChAll"
                    class="ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    همه
                </button>
                <button type="button" onclick="setChannelFilter('1'); return false;" id="btnChMarketer"
                    class="ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="users"></i>
                    بازاریاب
                </button>
                <button type="button" onclick="setChannelFilter('2'); return false;" id="btnChOnline"
                    class="ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="globe"></i>
                    آنلاین
                </button>
                <button type="button" onclick="setChannelFilter('3'); return false;" id="btnChAdmin"
                    class="ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted">
                    <i class="h-3 w-3" data-lucide="user-cog"></i>
                    ادمین
                </button>
            </div>

            <!-- Search + dropdowns -->
            <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-4">

                <label class="relative block lg:col-span-2">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو در شماره سفارش، نام مشتری یا یادداشت..." />
                </label>

                <asp:DropDownList ID="ddlFilterCustomer" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterCustomer_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                </asp:DropDownList>

                <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن فیلترها"
                    OnClick="btnClearFilters_Click"
                    CssClass="inline-flex h-11 items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />

            </div>
        </section>

        <!-- Table -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-4 flex items-center justify-between">
                <h2 class="text-[15px] font-bold text-ink-900">لیست سفارش‌های فروش</h2>
                <p class="text-[12.5px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1200px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[11.5px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">شماره سفارش</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مشتری</th>
                            <th class="pb-3 pr-4 text-center font-semibold">کانال</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تاریخ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">اقلام</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مبلغ کل</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدام</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptOrders" runat="server" OnItemCommand="rptOrders_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="py-3 pr-4">
                                        <a href='<%# "sales-orders.aspx?id=" + Eval("SalesOrderId") %>'
                                            class="flex items-center gap-2">
                                            <span class='<%# GetStatusAvatarCss(Eval("Status")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetStatusIcon(Eval("Status")) %>'></i>
                                            </span>
                                            <div class="min-w-0">
                                                <p class="font-mono font-bold text-ink-900 text-[13px]" dir="ltr">
                                                    <%# Enc(Eval("OrderNumber")) %>
                                                </p>
                                                <p class="mt-0.5 text-[11px] text-ink-400">
                                                    <%# FormatShamsiShort(Eval("CreatedAt")) %>
                                                </p>
                                            </div>
                                        </a>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <p class="font-semibold text-ink-800 text-[12.5px]">
                                            <%# Enc(Eval("CustomerName")) %>
                                        </p>
                                        <p class="mt-0.5 font-mono text-[10.5px] text-ink-400" dir="ltr">
                                            <%# Enc(Eval("CustomerCode")) %>
                                        </p>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class='<%# GetChannelBadgeCss(Eval("Channel")) %>'>
                                            <i class="h-3 w-3" data-lucide='<%# GetChannelIcon(Eval("Channel")) %>'></i>
                                            <%# GetChannelLabel(Eval("Channel")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[12px] text-ink-700">
                                        <%# FormatShamsiShort(Eval("OrderDate")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700">
                                            <%# Eval("ItemCount") %> قلم
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono font-bold text-emerald-700 text-[12.5px]">
                                            <%# FormatMoney(Eval("Total")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeCss(Eval("Status")) %>'>
                                            <%# GetStatusLabel(Eval("Status")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">

                                            <a href='<%# "sales-orders.aspx?id=" + Eval("SalesOrderId") %>'
                                                class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                title="مشاهده جزئیات">
                                                <i class="h-4 w-4" data-lucide="eye"></i>
                                            </a>

                                            <%-- پیش‌نویس: ویرایش + تأیید --%>
                                            <asp:PlaceHolder ID="phDraftActions" runat="server"
                                                Visible='<%# IsDraft(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit" CommandArgument='<%# Eval("SalesOrderId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>
                                                <asp:LinkButton ID="btnApprove" runat="server"
                                                    CommandName="Approve" CommandArgument='<%# Eval("SalesOrderId") %>'
                                                    CssClass="icon-button hover:bg-emerald-50 hover:text-emerald-600"
                                                    ToolTip="تأیید سفارش"
                                                    OnClientClick="return confirm('سفارش تأیید شود؟ پس از تأیید قابل ویرایش نیست.');">
                                                    <i class="h-4 w-4" data-lucide="check"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <%-- تأییدشده: تبدیل به پیش‌فاکتور --%>
                                            <asp:PlaceHolder ID="phApprovedActions" runat="server"
                                                Visible='<%# IsApproved(Eval("Status")) %>'>
                                                <a href='<%# "proforma-invoices.aspx?orderId=" + Eval("SalesOrderId") %>'
                                                    class="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[11.5px] font-bold text-white hover:bg-brand-700"
                                                    title="صدور پیش‌فاکتور">
                                                    <i class="h-3.5 w-3.5" data-lucide="file-text"></i>
                                                    پیش‌فاکتور
                                                </a>
                                            </asp:PlaceHolder>

                                            <%-- آماده‌سازی: ارسال --%>
                                            <asp:PlaceHolder ID="phReadyActions" runat="server"
                                                Visible='<%# IsReady(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnShip" runat="server"
                                                    CommandName="Ship" CommandArgument='<%# Eval("SalesOrderId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-violet-600 px-3 text-[11.5px] font-bold text-white hover:bg-violet-700"
                                                    ToolTip="ارسال"
                                                    OnClientClick="return confirm('سفارش ارسال شود؟');">
                                                    <i class="h-3.5 w-3.5" data-lucide="truck"></i>
                                                    ارسال
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <%-- ارسال‌شده: تحویل --%>
                                            <asp:PlaceHolder ID="phShippedActions" runat="server"
                                                Visible='<%# IsShipped(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnDeliver" runat="server"
                                                    CommandName="Deliver" CommandArgument='<%# Eval("SalesOrderId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[11.5px] font-bold text-white hover:bg-emerald-700"
                                                    ToolTip="ثبت تحویل"
                                                    OnClientClick="return confirm('تحویل مشتری ثبت شود؟');">
                                                    <i class="h-3.5 w-3.5" data-lucide="check-circle-2"></i>
                                                    تحویل
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <%-- لغو --%>
                                            <asp:PlaceHolder ID="phCancel" runat="server"
                                                Visible='<%# CanCancel(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnCancel" runat="server"
                                                    CommandName="Cancel" CommandArgument='<%# Eval("SalesOrderId") %>'
                                                    CssClass="icon-button hover:bg-rose-50 hover:text-rose-600"
                                                    ToolTip="لغو سفارش"
                                                    OnClientClick="return confirm('این سفارش لغو شود؟');">
                                                    <i class="h-4 w-4" data-lucide="x-circle"></i>
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
                    <i class="h-6 w-6" data-lucide="clipboard-list"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">سفارشی با این فیلتر یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">فیلترها را تغییر دهید یا سفارش جدید ثبت کنید.</p>
            </asp:Panel>

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
    </asp:Panel>

    <!-- ═══════════ VIEW 2: Form ═══════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">

        <!-- Header Info -->
        <div class="mb-4 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
            <div class="flex items-center gap-3">
                <span class="grid h-12 w-12 place-items-center rounded-xl bg-brand-600 text-white shadow-md">
                    <i class="h-6 w-6" data-lucide="clipboard-list"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-semibold text-brand-700">
                        <asp:Literal ID="litFormMode" runat="server" Text="سفارش فروش جدید" />
                    </p>
                    <p class="mt-0.5 text-[17px] font-extrabold text-ink-900">
                        <asp:Literal ID="litFormNumber" runat="server" Text="—" />
                    </p>
                </div>
                <asp:Literal ID="litFormStatusBadge" runat="server" />
            </div>
        </div>

        <!-- Order Header Info -->
        <asp:Panel ID="pnlOrderHeader" runat="server">
            <article class="mb-4 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

                <h3 class="mb-4 flex items-center gap-2 text-[14px] font-bold text-ink-800">
                    <span class="grid h-7 w-7 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="file-text"></i>
                    </span>
                    اطلاعات پایه سفارش
                </h3>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                    <label class="block lg:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مشتری <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlCustomer" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="ddlCustomer_SelectedIndexChanged"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                        <p class="mt-1 text-[11px] text-ink-400">
                            <asp:Literal ID="litCustomerInfo" runat="server" />
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">نوع سفارش</span>
                        <asp:DropDownList ID="ddlOrderType" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="1" Text="تک‌فروشی" />
                            <asp:ListItem Value="2" Text="عمده" />
                            <asp:ListItem Value="3" Text="نمایندگی" />
                        </asp:DropDownList>
                        <p class="mt-1 text-[11px] text-ink-400">از مشتری اتخاذ می‌شود</p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">کانال فروش</span>
                        <asp:DropDownList ID="ddlChannel" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="ddlChannel_SelectedIndexChanged"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="1" Text="بازاریاب" />
                            <asp:ListItem Value="2" Text="آنلاین" />
                            <asp:ListItem Value="3" Text="ادمین (حضوری/تلفنی)" />
                        </asp:DropDownList>
                    </label>

                    <asp:Panel ID="pnlMarketer" runat="server" Visible="false">
                        <label class="block">
                            <span class="text-[13px] font-semibold text-ink-700">بازاریاب مسئول</span>
                            <asp:DropDownList ID="ddlMarketer" runat="server"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            </asp:DropDownList>
                        </label>
                    </asp:Panel>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تاریخ تحویل مورد انتظار</span>
                        <asp:TextBox ID="txtDeliveryDate" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="1403/05/15" />
                        <p class="mt-1 text-[11px] text-ink-400">اختیاری — شمسی</p>
                    </label>

                    <label class="block lg:col-span-3">
                        <span class="text-[13px] font-semibold text-ink-700">آدرس تحویل</span>
                        <asp:TextBox ID="txtDeliveryAddress" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2"
                            CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="آدرس تحویل کالا" />
                        <p class="mt-1 text-[11px] text-ink-400">خالی = از آدرس مشتری استفاده می‌شود</p>
                    </label>

                    <label class="block lg:col-span-3">
                        <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                        <asp:TextBox ID="txtNotes" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2"
                            CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="یادداشت داخلی (اختیاری)" />
                    </label>

                </div>
            </article>
        </asp:Panel>

        <!-- Items Section -->
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h3 class="flex items-center gap-2 text-[14px] font-bold text-ink-800">
                    <span class="grid h-7 w-7 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="package"></i>
                    </span>
                    اقلام سفارش
                </h3>
                <asp:PlaceHolder ID="phAddItem" runat="server">
                    <asp:Button ID="btnAddItem" runat="server" Text="افزودن قلم"
                        OnClick="btnAddItem_Click"
                        CssClass="inline-flex h-9 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12.5px] font-bold text-white hover:bg-brand-700" />
                </asp:PlaceHolder>
            </div>

            <!-- Inline Item Form -->
            <asp:Panel ID="pnlItemForm" runat="server" Visible="false"
                CssClass="mb-4 rounded-base border border-brand-200 bg-brand-50/30 p-5">
                <div class="mb-4 flex items-center justify-between border-b border-brand-200/60 pb-3">
                    <h4 class="flex items-center gap-2 text-[13px] font-bold text-ink-800">
                        <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                            <i class="h-3 w-3" data-lucide="plus-circle"></i>
                        </span>
                        <asp:Literal ID="litItemFormTitle" runat="server" Text="افزودن قلم جدید" />
                    </h4>
                    <asp:LinkButton ID="btnCancelItemForm" runat="server" OnClick="btnCancelItemForm_Click"
                        CssClass="text-[12px] font-semibold text-ink-500 hover:text-ink-700">
                        <i class="inline h-3 w-3" data-lucide="x"></i>
                        انصراف
                    </asp:LinkButton>
                </div>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

                    <label class="block lg:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">
                            محصول نهایی <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlItemProduct" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="ddlItemProduct_SelectedIndexChanged"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                        <p class="mt-1 text-[11px] text-ink-400">
                            قیمت خودکار از لیست قیمت مشتری
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مقدار <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtItemQuantity" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="10" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            قیمت واحد (تومان) <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtItemUnitPrice" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="100000" />
                        <p class="mt-1 text-[11px] text-ink-400">
                            <asp:Literal ID="litPriceInfo" runat="server" />
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تخفیف (تومان)</span>
                        <asp:TextBox ID="txtItemDiscount" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="0" />
                    </label>

                    <label class="block lg:col-span-3">
                        <span class="text-[13px] font-semibold text-ink-700">مالیات (تومان)</span>
                        <asp:TextBox ID="txtItemTax" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="0" />
                    </label>

                </div>

                <div class="mt-4 flex items-center justify-end gap-3 border-t border-brand-200/60 pt-4">
                    <asp:Button ID="btnSaveItem" runat="server" Text="ذخیره قلم" OnClick="btnSaveItem_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
                </div>
            </asp:Panel>

            <!-- Items Table -->
            <div class="dashboard-scrollbar overflow-x-auto rounded-base border border-surface-line">
                <asp:Repeater ID="rptItems" runat="server" OnItemCommand="rptItems_ItemCommand">
                    <HeaderTemplate>
                        <table class="w-full min-w-[900px] text-right">
                            <thead>
                                <tr class="bg-surface-body text-[11px] uppercase text-ink-500">
                                    <th class="px-3 py-3 text-right font-semibold">محصول</th>
                                    <th class="px-3 py-3 text-center font-semibold">مقدار</th>
                                    <th class="px-3 py-3 text-center font-semibold">قیمت واحد</th>
                                    <th class="px-3 py-3 text-center font-semibold">تخفیف</th>
                                    <th class="px-3 py-3 text-center font-semibold">مالیات</th>
                                    <th class="px-3 py-3 text-center font-semibold">جمع</th>
                                    <th class="px-3 py-3 text-left font-semibold">اقدام</th>
                                </tr>
                            </thead>
                            <tbody class="text-[13px]">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-t border-surface-line">
                            <td class="px-3 py-3">
                                <div class="flex items-center gap-2">
                                    <span class="grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-violet-100 text-violet-700">
                                        <i class="h-3.5 w-3.5" data-lucide="package-check"></i>
                                    </span>
                                    <div class="min-w-0">
                                        <p class="truncate font-semibold text-ink-900 text-[12.5px]">
                                            <%# Enc(Eval("ProductName")) %>
                                        </p>
                                        <p class="mt-0.5 truncate font-mono text-[10.5px] text-ink-400" dir="ltr">
                                            <%# Enc(Eval("ProductCode")) %>
                                        </p>
                                    </div>
                                </div>
                            </td>
                            <td class="px-3 py-3 text-center">
                                <span class="font-mono font-bold text-[12.5px]">
                                    <%# FormatQuantity(Eval("Quantity")) %>
                                </span>
                                <span class="text-[10.5px] text-ink-400">
                                    <%# EncOrDash(Eval("UnitName")) %>
                                </span>
                            </td>
                            <td class="px-3 py-3 text-center font-mono text-[12px] text-ink-700">
                                <%# FormatMoney(Eval("UnitPrice")) %>
                            </td>
                            <td class="px-3 py-3 text-center font-mono text-[12px] text-ink-600">
                                <%# FormatMoneyOrDash(Eval("Discount")) %>
                            </td>
                            <td class="px-3 py-3 text-center font-mono text-[12px] text-ink-600">
                                <%# FormatMoneyOrDash(Eval("Tax")) %>
                            </td>
                            <td class="px-3 py-3 text-center font-mono text-[12.5px] font-bold text-emerald-700">
                                <%# FormatMoney(Eval("Total")) %>
                            </td>
                            <td class="px-3 py-3 text-left">
                                <asp:PlaceHolder ID="phItemActions" runat="server"
                                    Visible='<%# IsEditableItem() %>'>
                                    <asp:LinkButton ID="btnEditItem" runat="server"
                                        CommandName="EditItem" CommandArgument='<%# Eval("ItemId") %>'
                                        CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                        ToolTip="ویرایش قلم">
                                        <i class="h-3.5 w-3.5" data-lucide="pencil"></i>
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnDeleteItem" runat="server"
                                        CommandName="DeleteItem" CommandArgument='<%# Eval("ItemId") %>'
                                        CssClass="icon-button hover:bg-rose-50 hover:text-rose-600"
                                        ToolTip="حذف قلم"
                                        OnClientClick="return confirm('این قلم حذف شود؟');">
                                        <i class="h-3.5 w-3.5" data-lucide="trash-2"></i>
                                    </asp:LinkButton>
                                </asp:PlaceHolder>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>

            <asp:Panel ID="pnlItemsEmpty" runat="server" Visible="false"
                CssClass="rounded-base border border-surface-line bg-surface-body py-8 text-center">
                <p class="text-[13px] font-semibold text-ink-500">
                    هنوز قلمی به این سفارش اضافه نشده است.
                </p>
                <p class="mt-1 text-[11.5px] text-ink-400">
                    روی دکمه «افزودن قلم» کلیک کنید.
                </p>
            </asp:Panel>

            <!-- Totals -->
            <div class="mt-5 flex justify-end">
                <div class="w-full max-w-md rounded-base border border-surface-line bg-surface-body p-4">
                    <div class="space-y-2 text-[13px]">
                        <div class="flex items-center justify-between">
                            <span class="text-ink-600">جمع اقلام</span>
                            <span class="font-mono font-bold text-ink-800">
                                <asp:Literal ID="litSubtotal" runat="server" Text="۰" />
                            </span>
                        </div>
                        <div class="flex items-center justify-between">
                            <span class="text-ink-600">مجموع تخفیف</span>
                            <span class="font-mono font-bold text-rose-600">
                                <asp:Literal ID="litDiscountTotal" runat="server" Text="۰" />
                            </span>
                        </div>
                        <div class="flex items-center justify-between">
                            <span class="text-ink-600">مجموع مالیات</span>
                            <span class="font-mono font-bold text-amber-600">
                                <asp:Literal ID="litTaxTotal" runat="server" Text="۰" />
                            </span>
                        </div>
                        <div class="flex items-center justify-between border-t border-surface-line pt-2.5">
                            <span class="font-bold text-ink-800">جمع کل</span>
                            <span class="font-mono text-[15px] font-extrabold text-emerald-700">
                                <asp:Literal ID="litGrandTotal" runat="server" Text="۰" />
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Completed Info (Read Only) -->
            <asp:Panel ID="pnlCompletedInfo" runat="server" Visible="false" CssClass="mt-5">
                <div class="rounded-base border border-emerald-200 bg-emerald-50/40 p-4">
                    <div class="flex items-start gap-3">
                        <span class="grid h-9 w-9 shrink-0 place-items-center rounded-base bg-emerald-100 text-emerald-700">
                            <i class="h-4.5 w-4.5" data-lucide="check-circle-2"></i>
                        </span>
                        <div class="flex-1">
                            <p class="text-[13px] font-bold text-emerald-800">
                                <asp:Literal ID="litCompletedStatusLabel" runat="server" />
                            </p>
                            <div class="mt-2 grid gap-3 sm:grid-cols-2">
                                <div>
                                    <p class="text-[11px] font-semibold text-emerald-700">تاریخ تحویل ثبت‌شده</p>
                                    <p class="mt-0.5 text-[12.5px] font-bold text-ink-800">
                                        <asp:Literal ID="litCompletedDeliveryDate" runat="server" Text="—" />
                                    </p>
                                </div>
                                <div>
                                    <p class="text-[11px] font-semibold text-emerald-700">پیش‌فاکتور مرتبط</p>
                                    <p class="mt-0.5 text-[12.5px] font-bold text-ink-800">
                                        <asp:Literal ID="litCompletedProformaLink" runat="server" Text="—" />
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <!-- Form Actions -->
            <div class="mt-6 flex flex-wrap items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[13.5px] font-semibold text-ink-700 hover:bg-surface-muted" />

                <asp:PlaceHolder ID="phSaveActions" runat="server">
                    <asp:Button ID="btnSaveDraft" runat="server" Text="ذخیره پیش‌نویس"
                        OnClick="btnSaveDraft_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-brand-600 px-5 text-[13.5px] font-semibold text-brand-700 hover:bg-brand-50" />

                    <asp:Button ID="btnSaveAndApprove" runat="server" Text="ذخیره و تأیید"
                        OnClick="btnSaveAndApprove_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
                </asp:PlaceHolder>
            </div>

        </article>
    </asp:Panel>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ Status Filter ═══
    function setStatusFilter(status) {
        var field = document.getElementById('<%= hfStatusFilter.ClientID %>');
        if (field) field.value = status;
        __doPostBack('__PAGE', 'setStatus:' + status);
    }

    function highlightStatusButton() {
        var field = document.getElementById('<%= hfStatusFilter.ClientID %>');
        var current = field ? field.value : '';

        var buttons = document.querySelectorAll('.st-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].className = 'st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted';
        }

        var active = null;
        if (current === '1') active = document.getElementById('btnStDraft');
        else if (current === '2') active = document.getElementById('btnStApproved');
        else if (current === '3') active = document.getElementById('btnStReady');
        else if (current === '4') active = document.getElementById('btnStShipped');
        else if (current === '5') active = document.getElementById('btnStDelivered');
        else if (current === '6') active = document.getElementById('btnStCancelled');
        else active = document.getElementById('btnStAll');

        if (active) {
            active.className = 'st-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-brand-300 bg-brand-50 px-3 text-[12px] font-bold text-brand-700';
        }
    }

    // ═══ Channel Filter ═══
    function setChannelFilter(channel) {
        var field = document.getElementById('<%= hfChannelFilter.ClientID %>');
        if (field) field.value = channel;
        __doPostBack('__PAGE', 'setChannel:' + channel);
    }

    function highlightChannelButton() {
        var field = document.getElementById('<%= hfChannelFilter.ClientID %>');
        var current = field ? field.value : '';

        var buttons = document.querySelectorAll('.ch-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].className = 'ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-surface-line bg-surface-card px-3 text-[12px] font-semibold text-ink-600 hover:bg-surface-muted';
        }

        var active = null;
        if (current === '1') active = document.getElementById('btnChMarketer');
        else if (current === '2') active = document.getElementById('btnChOnline');
        else if (current === '3') active = document.getElementById('btnChAdmin');
        else active = document.getElementById('btnChAll');

        if (active) {
            active.className = 'ch-btn inline-flex h-8 items-center gap-1.5 rounded-lg border border-brand-300 bg-brand-50 px-3 text-[12px] font-bold text-brand-700';
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        highlightStatusButton();
        highlightChannelButton();
    });
</script>
</asp:Content>