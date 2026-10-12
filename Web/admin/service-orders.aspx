<%@ Page Title="سفارش‌های خدمات" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="service-orders.aspx.cs" Inherits="admin_service_orders" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .receive-hero {
        background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
        border: 2px solid #bfdbfe;
    }
    .complete-hero {
        background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%);
        border: 2px solid #a7f3d0;
    }
    .big-number {
        font-size: 32px;
        font-weight: 900;
        line-height: 1.2;
        letter-spacing: -0.5px;
    }
    .action-tile {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 24px 16px;
        border-radius: 16px;
        cursor: pointer;
        transition: all .2s;
        border: 2px solid transparent;
        text-decoration: none;
    }
    .action-tile:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 24px -8px rgba(15,23,42,0.15);
    }
    .action-tile-primary {
        background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
        color: #fff;
    }
    .action-tile-primary:hover { color: #fff; }
    .action-tile-secondary {
        background: #ffffff;
        border-color: #e2e8f0;
        color: #334155;
    }
    .action-tile-secondary:hover { color: #2563eb; border-color: #93c5fd; }
    .status-pill-lg {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 6px 12px;
        border-radius: 10px;
        font-size: 12px;
        font-weight: 800;
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditId" runat="server" Value="0" />
    <asp:HiddenField ID="hfExistingMovementId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">سفارش‌های خدمات</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="سفارش‌های خدمات" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="پذیرش سریع محصول، انجام عملیات، و تسویه نهایی در سه مرحله ساده." />
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

            <asp:PlaceHolder ID="phQuickActions" runat="server">
                <a href="service-types.aspx"
                    class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="settings"></i>
                    کاتالوگ خدمات
                </a>
                <asp:Button ID="btnQuickReceive" runat="server" Text="پذیرش جدید"
                    OnClick="btnQuickReceive_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 1: LIST
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Big action tiles -->
        <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <asp:LinkButton ID="btnTileQuickReceive" runat="server" OnClick="btnQuickReceive_Click"
                CssClass="action-tile action-tile-primary">
                <i class="h-8 w-8" data-lucide="package-plus"></i>
                <span class="text-[15px] font-bold">پذیرش سریع محصول</span>
                <span class="text-[11.5px] opacity-80">ثبت ورودی مشتری در ۵ ثانیه</span>
            </asp:LinkButton>

            <asp:LinkButton ID="btnTilePending" runat="server" OnClick="btnTilePending_Click"
                CssClass="action-tile action-tile-secondary">
                <i class="h-8 w-8" data-lucide="clock"></i>
                <span class="text-[15px] font-bold">در انتظار تسویه</span>
                <span class="text-[11.5px] text-ink-500">
                    <asp:Literal ID="litTilePendingCount" runat="server" Text="۰" />
                    سفارش
                </span>
            </asp:LinkButton>

            <a href="service-types.aspx" class="action-tile action-tile-secondary">
                <i class="h-8 w-8" data-lucide="settings-2"></i>
                <span class="text-[15px] font-bold">مدیریت خدمات</span>
                <span class="text-[11.5px] text-ink-500">افزودن یا ویرایش خدمات</span>
            </a>
        </div>

        <!-- Stats -->
        <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="wrench"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل سفارش‌ها</p>
                        <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="clock"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">در انتظار تسویه</p>
                        <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                            <asp:Literal ID="litStatPending" runat="server" Text="۰" />
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
                        <p class="text-[12px] text-ink-500">تکمیل‌شده (این ماه)</p>
                        <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatCompleted" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="banknote"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">درآمد این ماه</p>
                        <p class="mt-0.5 text-[16px] font-bold text-violet-600 font-mono">
                            <asp:Literal ID="litStatRevenue" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>
        </div>

        <!-- Filters -->
        <section class="mb-4 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

            <div class="mb-3 flex flex-wrap items-center gap-2">
                <span class="text-[12.5px] font-bold text-ink-600 ml-1">بازه سریع:</span>
                <asp:LinkButton ID="btnRangeToday" runat="server" CommandArgument="today"
                    OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">امروز</asp:LinkButton>
                <asp:LinkButton ID="btnRangeWeek" runat="server" CommandArgument="week"
                    OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">این هفته</asp:LinkButton>
                <asp:LinkButton ID="btnRangeMonth" runat="server" CommandArgument="month"
                    OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">این ماه</asp:LinkButton>
                <asp:LinkButton ID="btnRange30" runat="server" CommandArgument="30days"
                    OnClick="QuickRange_Click" CssClass="badge bg-surface-muted text-ink-700 hover:bg-brand-50 hover:text-brand-700 cursor-pointer">۳۰ روز اخیر</asp:LinkButton>
                <asp:LinkButton ID="btnRangeClear" runat="server" CommandArgument="clear"
                    OnClick="QuickRange_Click" CssClass="badge bg-danger-50 text-danger-600 hover:bg-danger-100 cursor-pointer">پاک کردن</asp:LinkButton>
            </div>

            <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-5">

                <label class="relative block lg:col-span-2">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-600"
                        placeholder="جستجو در شماره یا نام مشتری..." />
                </label>

                <label class="block">
                    <asp:TextBox ID="txtFilterFrom" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged" MaxLength="10" dir="ltr"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                        placeholder="از تاریخ" />
                </label>

                <label class="block">
                    <asp:TextBox ID="txtFilterTo" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged" MaxLength="10" dir="ltr"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                        placeholder="تا تاریخ" />
                </label>

                <asp:DropDownList ID="ddlFilterStatus" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="در انتظار تسویه" />
                    <asp:ListItem Value="3" Text="تکمیل‌شده" />
                    <asp:ListItem Value="4" Text="لغو‌شده" />
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterServiceType" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600 lg:col-span-2">
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterPaymentMode" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه حالت‌های پرداخت" />
                    <asp:ListItem Value="1" Text="نقدی" />
                    <asp:ListItem Value="2" Text="جنسی" />
                    <asp:ListItem Value="3" Text="ترکیبی" />
                </asp:DropDownList>

                <div class="flex items-end lg:col-span-2">
                    <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن فیلترها"
                        OnClick="btnClearFilters_Click"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-card px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
                </div>

            </div>
        </section>

        <!-- Table -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1200px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">سفارش</th>
                            <th class="pb-3 pr-4 text-right font-semibold">مشتری / تحویل‌دهنده</th>
                            <th class="pb-3 pr-4 text-center font-semibold">خدمت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">ورودی / خروجی</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مبلغ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptOrders" runat="server" OnItemCommand="rptOrders_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("Status")) %>'>
                                    <td class="py-3 pr-4">
                                        <p class="font-mono font-bold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("OrderNumber")) %>
                                        </p>
                                        <p class="mt-0.5 text-[11px] text-ink-400 font-mono">
                                            <%# FormatShamsiDate(Eval("ServiceDate")) %>
                                        </p>
                                    </td>
                                    <td class="py-3 pr-4">
                                        <p class="font-semibold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("CustomerName")) %>
                                        </p>
                                        <%# GetDeliveredByHint(Eval("DeliveredBy"), Eval("DeliveredByMobile")) %>
                                        <%# GetCustomerTypeHint(Eval("CustomerType")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-brand-50 text-brand-700">
                                            <%# Enc(Eval("ServiceName")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[12.5px] font-mono">
                                        <div class="text-ink-700">
                                            <%# FormatQty(Eval("InputWeight")) %>
                                            <%# GetOutputHint(Eval("OutputWeight")) %>
                                        </div>
                                        <%# GetWasteHint(Eval("WastePercent"), Eval("WasteWeight")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetAmountCell(Eval("Status"), Eval("Total"), Eval("PaidAmount"), Eval("PaymentMode")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetStatusBadge(Eval("Status")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phComplete" runat="server"
                                                Visible='<%# IsPending(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnComplete" runat="server"
                                                    CommandName="Complete"
                                                    CommandArgument='<%# Eval("ServiceOrderId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700">
                                                    <i class="h-3.5 w-3.5" data-lucide="calculator"></i>
                                                    تکمیل و تسویه
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <a href='<%# "service-receipt.aspx?id=" + Eval("ServiceOrderId") %>'
                                                target="_blank"
                                                class="icon-button hover:bg-violet-50 hover:text-violet-600"
                                                title="چاپ رسید">
                                                <i class="h-4 w-4" data-lucide="printer"></i>
                                            </a>

                                            <asp:PlaceHolder ID="phView" runat="server"
                                                Visible='<%# IsCompleted(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnView" runat="server"
                                                    CommandName="View"
                                                    CommandArgument='<%# Eval("ServiceOrderId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="مشاهده">
                                                    <i class="h-4 w-4" data-lucide="eye"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phEditReceive" runat="server"
                                                Visible='<%# IsPending(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="EditReceive"
                                                    CommandArgument='<%# Eval("ServiceOrderId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش پذیرش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phCancel" runat="server"
                                                Visible='<%# IsPending(Eval("Status")) %>'>
                                                <asp:LinkButton ID="btnCancel" runat="server"
                                                    CommandName="CancelOrder"
                                                    CommandArgument='<%# Eval("ServiceOrderId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="لغو"
                                                    OnClientClick='<%# GetCancelConfirm(Eval("OrderNumber")) %>'>
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

            <asp:Panel ID="pnlListEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="wrench"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">سفارشی یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین سفارش خدمت را با دکمه «پذیرش جدید» ثبت کنید.</p>
            </asp:Panel>

            <div class="mt-5">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>
        </section>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 2: QUICK RECEIVE
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlReceiveView" runat="server" Visible="false">

        <div class="mx-auto max-w-2xl">

            <section class="receive-hero mb-5 rounded-card p-6">
                <div class="flex items-center gap-4">
                    <span class="grid h-14 w-14 place-items-center rounded-2xl bg-brand-600 text-white shadow-md">
                        <i class="h-7 w-7" data-lucide="package-plus"></i>
                    </span>
                    <div class="flex-1">
                        <h2 class="text-[18px] font-bold text-ink-900">
                            <asp:Literal ID="litReceiveTitle" runat="server" Text="پذیرش سریع محصول" />
                        </h2>
                        <p class="mt-1 text-[12.5px] text-ink-600">
                            فقط مشتری، خدمت و وزن ورودی. مابقی بعد از اتمام کار.
                        </p>
                    </div>
                </div>
            </section>

            <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

                <div class="grid gap-5 md:grid-cols-2">

                    <label class="block md:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مشتری <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlCustomer" runat="server"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlCustomer_SelectedIndexChanged"
                            CssClass="mt-1.5 h-12 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[15px] text-ink-800 focus:border-brand-600">
                        </asp:DropDownList>
                        <p class="mt-1.5 text-[11.5px] text-ink-500">
                            <asp:Literal ID="litCustomerHint" runat="server" Text="مشتری را از لیست انتخاب کنید." />
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            نوع خدمت <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlServiceType" runat="server"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlServiceType_SelectedIndexChanged"
                            CssClass="mt-1.5 h-12 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                        <p class="mt-1.5 text-[11.5px] text-ink-500">
                            نرخ پیش‌فرض:
                            <strong class="font-mono text-emerald-700">
                                <asp:Literal ID="litServiceDefaultPrice" runat="server" Text="—" />
                            </strong>
                            تومان
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            وزن ورودی (کیلوگرم) <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtInputWeight" runat="server" MaxLength="15" dir="ltr"
                            CssClass="mt-1.5 h-12 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[16px] font-mono font-bold focus:border-brand-600"
                            placeholder="1000" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تاریخ پذیرش</span>
                        <asp:TextBox ID="txtReceiveDate" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-12 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1403/07/21" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">زمان تخمینی تحویل</span>
                        <asp:DropDownList ID="ddlEstimatedDelivery" runat="server"
                            CssClass="mt-1.5 h-12 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="30" Text="۳۰ دقیقه دیگر" />
                            <asp:ListItem Value="60" Text="۱ ساعت دیگر" />
                            <asp:ListItem Value="120" Text="۲ ساعت دیگر" />
                            <asp:ListItem Value="180" Text="۳ ساعت دیگر" Selected="True" />
                            <asp:ListItem Value="360" Text="۶ ساعت دیگر" />
                            <asp:ListItem Value="720" Text="۱۲ ساعت دیگر (فردا)" />
                            <asp:ListItem Value="0" Text="— اعلام نمی‌کنم —" />
                        </asp:DropDownList>
                    </label>

                </div>

                <!-- Delivered by (optional) -->
                <div class="mt-5 rounded-base border border-dashed border-surface-line bg-surface-body p-4">
                    <p class="mb-3 text-[12.5px] font-bold text-ink-700">
                        <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="users"></i>
                        اگر تحویل‌دهنده نماینده است (اختیاری)
                    </p>
                    <div class="grid gap-4 md:grid-cols-2">
                        <label class="block">
                            <span class="text-[12.5px] font-semibold text-ink-700">نام تحویل‌دهنده</span>
                            <asp:TextBox ID="txtDeliveredBy" runat="server" MaxLength="150"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] focus:border-brand-600"
                                placeholder="مثال: آقای رضایی" />
                        </label>
                        <label class="block">
                            <span class="text-[12.5px] font-semibold text-ink-700">موبایل تحویل‌دهنده</span>
                            <asp:TextBox ID="txtDeliveredByMobile" runat="server" MaxLength="15" dir="ltr"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                                placeholder="09xxxxxxxxx" />
                        </label>
                    </div>
                </div>

                <label class="mt-4 block">
                    <span class="text-[13px] font-semibold text-ink-700">یادداشت پذیرش</span>
                    <asp:TextBox ID="txtReceiveNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600"
                        placeholder="مثال: گندم مرطوب، نیاز به خشک‌کن قبل از بوجاری (اختیاری)" />
                </label>

                <!-- Buttons -->
                <div class="mt-6 flex flex-wrap items-center justify-end gap-3 border-t border-surface-line pt-4">
                    <asp:Button ID="btnCancelReceive" runat="server" Text="انصراف" OnClick="btnCancelReceive_Click"
                        CssClass="inline-flex h-11 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />

                    <asp:Button ID="btnSaveReceiveOnly" runat="server"
                        Text="ثبت پذیرش"
                        OnClick="btnSaveReceiveOnly_Click"
                        CssClass="inline-flex h-11 items-center gap-2 rounded-base border-2 border-brand-600 bg-white px-5 text-[14px] font-bold text-brand-700 hover:bg-brand-50" />

                    <asp:Button ID="btnSaveReceiveAndComplete" runat="server"
                        Text="ثبت و تسویه فوری"
                        OnClick="btnSaveReceiveAndComplete_Click"
                        CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-bold text-white hover:bg-brand-700" />
                </div>

            </section>
        </div>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 3: COMPLETE / SETTLE
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlCompleteView" runat="server" Visible="false">

        <!-- Order banner -->
        <div class="complete-hero mb-5 rounded-card p-5">
            <div class="flex flex-wrap items-center gap-4">
                <span class="grid h-14 w-14 place-items-center rounded-2xl bg-emerald-600 text-white shadow-md">
                    <i class="h-7 w-7" data-lucide="calculator"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-bold text-emerald-700">
                        <asp:Literal ID="litCompleteHeaderStatus" runat="server" Text="تکمیل و تسویه" />
                    </p>
                    <p class="mt-0.5 text-[18px] font-extrabold text-ink-900 font-mono">
                        <asp:Literal ID="litCompleteOrderNumber" runat="server" />
                    </p>
                    <p class="mt-0.5 text-[12.5px] text-ink-600">
                        <asp:Literal ID="litCompleteCustomerInfo" runat="server" />
                    </p>
                </div>
                <div class="text-left">
                    <p class="text-[11.5px] text-ink-500">وزن ورودی</p>
                    <p class="mt-1 font-mono text-[18px] font-black text-ink-900">
                        <asp:Literal ID="litCompleteInputWeight" runat="server" Text="۰" />
                        <span class="text-[12px] font-normal text-ink-500">kg</span>
                    </p>
                </div>
            </div>
        </div>

        <!-- Section 1: Output weight -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="mb-4 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                    <i class="h-5 w-5" data-lucide="scale"></i>
                </span>
                <div>
                    <h2 class="text-[15px] font-semibold text-ink-900">توزین خروجی</h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        وزن محصول پاک‌شده را وارد کنید. ضایعات خودکار محاسبه می‌شود.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-3">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        وزن خروجی (کیلوگرم) <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtOutputWeight" runat="server" MaxLength="15" dir="ltr"
                        CssClass="js-output-weight mt-1.5 h-12 w-full rounded-base border-2 border-brand-300 bg-brand-50/40 px-3 text-[18px] font-mono font-bold focus:border-brand-600 focus:bg-white"
                        placeholder="970" />
                </label>

                <div class="rounded-base border border-surface-line bg-surface-muted p-3 flex flex-col justify-center">
                    <p class="text-[11.5px] text-ink-500">ضایعات محاسبه‌شده</p>
                    <p class="mt-1 font-mono text-[20px] font-bold text-ink-900" id="lblWasteWeight">—</p>
                    <p class="mt-0.5 text-[11px] text-ink-400" id="lblWastePercent">—</p>
                </div>

                <div class="rounded-base border border-amber-200 bg-amber-50 p-3 flex flex-col justify-center">
                    <p class="text-[11.5px] text-amber-700">ضایعات استاندارد این خدمت</p>
                    <p class="mt-1 font-mono text-[16px] font-bold text-amber-800">
                        <asp:Literal ID="litCompleteDefaultWaste" runat="server" Text="—" />
                    </p>
                    <p class="mt-0.5 text-[11px] text-amber-600">
                        بیشتر از این مقدار = هشدار
                    </p>
                </div>

            </div>

            <div id="wasteWarning" class="mt-3 hidden rounded-base border border-amber-300 bg-amber-50 p-3 text-[12.5px] text-amber-800">
                <i class="inline h-4 w-4 align-middle ml-1" data-lucide="alert-triangle"></i>
                <span id="wasteWarningText">ضایعات از حد استاندارد بیشتر است.</span>
            </div>

            <div class="mt-4 grid gap-4 md:grid-cols-2">
                <label class="block">
                    <span class="text-[12.5px] font-semibold text-ink-700">توضیح ورودی</span>
                    <asp:TextBox ID="txtInputDesc" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13.5px] focus:border-brand-600"
                        placeholder="مثال: گندم مرطوب" />
                </label>
                <label class="block">
                    <span class="text-[12.5px] font-semibold text-ink-700">توضیح خروجی</span>
                    <asp:TextBox ID="txtOutputDesc" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13.5px] focus:border-brand-600"
                        placeholder="مثال: گندم پاک" />
                </label>
            </div>
        </section>

        <!-- Section 2: Pricing -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="mb-4 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-5 w-5" data-lucide="banknote"></i>
                </span>
                <div>
                    <h2 class="text-[15px] font-semibold text-ink-900">محاسبه هزینه</h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        مبلغ بر اساس وزن ورودی × نرخ محاسبه می‌شود.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-4">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">نرخ واحد (تومان)</span>
                    <asp:TextBox ID="txtUnitPrice" runat="server" MaxLength="20" dir="ltr"
                        CssClass="js-unit-price mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[15px] font-mono font-bold focus:border-brand-600"
                        placeholder="500" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">جمع جزء</span>
                    <p class="mt-1.5 h-11 rounded-base border border-surface-line bg-surface-muted px-3 py-3 text-[15px] font-mono font-bold text-ink-800" id="lblSubtotal">—</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">تخفیف (تومان)</span>
                    <asp:TextBox ID="txtDiscount" runat="server" MaxLength="20" dir="ltr"
                        CssClass="js-discount mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="0" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">مالیات (تومان)</span>
                    <asp:TextBox ID="txtTax" runat="server" MaxLength="20" dir="ltr"
                        CssClass="js-tax mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="0" />
                </label>

            </div>

            <div class="mt-4 rounded-base bg-emerald-50 border-2 border-emerald-300 p-4 flex items-center justify-between">
                <span class="text-[14px] font-bold text-emerald-800">مبلغ کل قابل پرداخت</span>
                <span class="font-mono text-[24px] font-black text-emerald-800" id="lblTotal">۰ <span class="text-[12px] font-normal">تومان</span></span>
            </div>

        </section>

        <!-- Section 3: Payment -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="mb-4 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-5 w-5" data-lucide="hand-coins"></i>
                </span>
                <div>
                    <h2 class="text-[15px] font-semibold text-ink-900">حالت پرداخت</h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        نقدی، جنسی (محصول به‌جای پول)، یا ترکیبی.
                    </p>
                </div>
            </div>

            <div class="grid gap-3 md:grid-cols-3 mb-4">
                <label class="block md:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">حالت پرداخت</span>
                    <asp:DropDownList ID="ddlPaymentMode" runat="server"
                        onchange="onPaymentModeChange(this); return false;"
                        CssClass="mt-1.5 h-11 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="1" Text="نقدی (پرداخت با پول)" />
                        <asp:ListItem Value="2" Text="جنسی (محصول به‌جای پول)" />
                        <asp:ListItem Value="3" Text="ترکیبی (نقدی + جنسی)" />
                    </asp:DropDownList>
                </label>
                <div class="rounded-base border border-amber-200 bg-amber-50 p-3 flex flex-col justify-center">
                    <p class="text-[11.5px] text-amber-700">جمع دریافتی</p>
                    <p class="mt-1 font-mono text-[18px] font-bold text-amber-800" id="lblPaidTotal">۰ <span class="text-[10px] font-normal">تومان</span></p>
                </div>
            </div>

            <!-- Cash Section -->
            <div id="cashSection" class="rounded-base border border-emerald-200 bg-emerald-50/40 p-4">
                <h3 class="mb-3 text-[13px] font-bold text-emerald-800 flex items-center gap-2">
                    <i class="h-4 w-4" data-lucide="banknote"></i>
                    پرداخت نقدی
                </h3>
                <div class="grid gap-4 md:grid-cols-3">
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">مبلغ نقدی دریافتی (تومان)</span>
                        <asp:TextBox ID="txtCashAmount" runat="server" MaxLength="20" dir="ltr"
                            CssClass="js-cash-amount mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono font-bold focus:border-brand-600"
                            placeholder="0" />
                    </label>
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">روش پرداخت</span>
                        <asp:DropDownList ID="ddlCashMethod" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="" Text="— انتخاب —" />
                            <asp:ListItem Value="نقدی" Text="نقدی" />
                            <asp:ListItem Value="کارت بانکی" Text="کارت بانکی" />
                            <asp:ListItem Value="انتقال بانکی" Text="انتقال بانکی" />
                            <asp:ListItem Value="چک" Text="چک" />
                        </asp:DropDownList>
                    </label>
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">شماره سند</span>
                        <asp:TextBox ID="txtCashRef" runat="server" MaxLength="100" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="اختیاری" />
                    </label>
                </div>
            </div>

            <!-- In-Kind Section -->
            <div id="inKindSection" class="mt-4 rounded-base border border-violet-200 bg-violet-50/40 p-4" style="display:none;">
                <h3 class="mb-3 text-[13px] font-bold text-violet-800 flex items-center gap-2">
                    <i class="h-4 w-4" data-lucide="package"></i>
                    محصول دریافتی به‌جای دستمزد
                </h3>
                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
                    <label class="block lg:col-span-2">
                        <span class="text-[12.5px] font-semibold text-ink-700">
                            محصول <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlInKindProduct" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                    </label>
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">وزن (کیلوگرم)</span>
                        <asp:TextBox ID="txtInKindWeight" runat="server" MaxLength="15" dir="ltr"
                            CssClass="js-inkind-weight mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="0" />
                    </label>
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">ارزش واحد (تومان)</span>
                        <asp:TextBox ID="txtInKindUnitPrice" runat="server" MaxLength="20" dir="ltr"
                            CssClass="js-inkind-price mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="5000" />
                    </label>
                    <label class="block">
                        <span class="text-[12.5px] font-semibold text-ink-700">
                            انبار ورود <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlInKindWarehouse" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                    </label>
                </div>
                <div class="mt-3 grid gap-3 md:grid-cols-2">
                    <div class="rounded-base bg-surface-card border border-violet-200 p-3">
                        <p class="text-[11.5px] text-violet-700">ارزش کل محصول دریافتی</p>
                        <p class="mt-1 font-mono text-[16px] font-bold text-violet-800" id="lblInKindValue">۰ <span class="text-[10px] font-normal">تومان</span></p>
                    </div>
                    <div class="rounded-base bg-surface-card border border-violet-200 p-3">
                        <p class="text-[11.5px] text-violet-700">مانده قابل پرداخت (بعد از جنسی)</p>
                        <p class="mt-1 font-mono text-[16px] font-bold text-ink-800" id="lblRemaining">—</p>
                    </div>
                </div>
                <p class="mt-3 text-[11px] text-violet-700 leading-relaxed">
                    <i class="inline h-3 w-3 align-middle ml-1" data-lucide="info"></i>
                    محصول دریافتی هنگام ذخیره به‌طور خودکار به انبار انتخاب‌شده وارد می‌شود.
                </p>
            </div>

        </section>

        <!-- Section 4: Notes -->
        <section class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <label class="block">
                <span class="text-[13px] font-semibold text-ink-700">یادداشت تسویه</span>
                <asp:TextBox ID="txtCompleteNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                    CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600"
                    placeholder="هر توضیح اضافه‌ای (اختیاری)" />
            </label>
        </section>

        <!-- Action bar -->
        <div class="flex flex-wrap items-center justify-between gap-3 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
            <p class="text-[12px] text-ink-500">
                <i class="inline h-4 w-4 align-middle ml-1" data-lucide="shield-check"></i>
                پس از تأیید، سفارش تکمیل‌شده و قابل ویرایش نخواهد بود (مگر با نقش مدیر).
            </p>
            <div class="flex items-center gap-3">
                <asp:Button ID="btnCancelComplete" runat="server" Text="انصراف" OnClick="btnCancelComplete_Click"
                    CssClass="inline-flex h-11 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSaveComplete" runat="server" Text="تکمیل و تسویه"
                    OnClick="btnSaveComplete_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-emerald-600 px-6 text-[15px] font-bold text-white hover:bg-emerald-700" />
            </div>
        </div>

    </asp:Panel>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══════════════════════════════════════════════
    // Auto-calc
    // ═══════════════════════════════════════════════

    function parseNum(el) {
        if (!el) return 0;
        var v = (el.value || '').toString().replace(/[,\s،]/g, '').replace(/[۰-۹]/g, function (d) {
            return '۰۱۲۳۴۵۶۷۸۹'.indexOf(d);
        });
        var n = parseFloat(v);
        return isNaN(n) ? 0 : n;
    }

    function fmtNum(n) {
        if (!n || n === 0) return '۰';
        return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '،');
    }

    function fmtNum2(n) {
        if (!n || n === 0) return '۰';
        return n.toFixed(2).replace(/\.00$/, '').replace(/\B(?=(\d{3})+(?!\d))/g, '،');
    }

    function byClass(cls) {
        return document.querySelector('.' + cls);
    }

    function setText(id, html) {
        var el = document.getElementById(id);
        if (el) el.innerHTML = html;
    }

    // Read-only input weight (from server-rendered literal)
    function getInputWeight() {
        var el = document.getElementById('<%= litCompleteInputWeight.ClientID %>');
        if (!el) return 0;
        var v = el.innerHTML.replace(/[,\s،]/g, '').replace(/[۰-۹]/g, function (d) {
            return '۰۱۲۳۴۵۶۷۸۹'.indexOf(d);
        });
        var n = parseFloat(v);
        return isNaN(n) ? 0 : n;
    }

    function recalcAll() {
        var inputW = getInputWeight();
        var outputEl = byClass('js-output-weight');
        var priceEl = byClass('js-unit-price');
        var discEl = byClass('js-discount');
        var taxEl = byClass('js-tax');
        var cashEl = byClass('js-cash-amount');
        var ikWEl = byClass('js-inkind-weight');
        var ikPEl = byClass('js-inkind-price');

        var outputVal = parseNum(outputEl);
        var priceVal = parseNum(priceEl);
        var discVal = parseNum(discEl);
        var taxVal = parseNum(taxEl);
        var cashVal = parseNum(cashEl);
        var ikW = parseNum(ikWEl);
        var ikP = parseNum(ikPEl);

        // Waste
        var waste = inputW - outputVal;
        if (waste < 0) waste = 0;
        var wastePct = inputW > 0 ? (waste / inputW) * 100 : 0;

        setText('lblWasteWeight', waste > 0 ? fmtNum(waste) + ' kg' : '—');
        setText('lblWastePercent', inputW > 0 && outputVal > 0 ? wastePct.toFixed(2) + '٪' : '—');

        // Waste warning
        var warningDiv = document.getElementById('wasteWarning');
        if (warningDiv) {
            var defWasteEl = document.getElementById('<%= litCompleteDefaultWaste.ClientID %>');
            var defWaste = 0;
            if (defWasteEl) {
                var v = defWasteEl.innerHTML.replace(/[٪,\s،]/g, '');
                var n = parseFloat(v);
                if (!isNaN(n)) defWaste = n;
            }
            if (defWaste > 0 && wastePct > defWaste) {
                warningDiv.classList.remove('hidden');
                var warnText = document.getElementById('wasteWarningText');
                if (warnText) {
                    warnText.innerHTML = 'ضایعات محاسبه‌شده (' + wastePct.toFixed(2) +
                        '٪) از حد استاندارد (' + defWaste + '٪) بیشتر است. لطفاً بررسی کنید.';
                }
            } else {
                warningDiv.classList.add('hidden');
            }
        }

        // Subtotal (input × unit price)
        var subtotal = inputW * priceVal;
        setText('lblSubtotal', fmtNum(subtotal) + ' تومان');

        // Total
        var total = subtotal - discVal + taxVal;
        if (total < 0) total = 0;
        setText('lblTotal', fmtNum(total) + ' <span class="text-[12px] font-normal">تومان</span>');

        // In-kind value
        var inKindValue = ikW * ikP;
        setText('lblInKindValue', fmtNum(inKindValue) + ' <span class="text-[10px] font-normal">تومان</span>');

        // Paid total
        var paidTotal = cashVal + inKindValue;
        setText('lblPaidTotal', fmtNum(paidTotal) + ' <span class="text-[10px] font-normal">تومان</span>');

        // Remaining
        var remaining = total - paidTotal;
        if (remaining < 0) remaining = 0;
        var remainEl = document.getElementById('lblRemaining');
        if (remainEl) {
            if (total === 0) remainEl.innerHTML = '—';
            else if (remaining === 0) remainEl.innerHTML = '<span class="text-emerald-700">تسویه شد ✓</span>';
            else remainEl.innerHTML = fmtNum(remaining) + ' <span class="text-[10px] font-normal">تومان</span>';
        }
    }

    // ═══════════════════════════════════════════════
    // Payment mode toggle
    // ═══════════════════════════════════════════════

    function onPaymentModeChange(ddl) {
        var mode = parseInt(ddl.value, 10);
        var cashSection = document.getElementById('cashSection');
        var inKindSection = document.getElementById('inKindSection');

        if (mode === 1) {
            cashSection.style.display = '';
            inKindSection.style.display = 'none';
        } else if (mode === 2) {
            cashSection.style.display = 'none';
            inKindSection.style.display = '';
        } else {
            cashSection.style.display = '';
            inKindSection.style.display = '';
        }
        recalcAll();
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    // ═══════════════════════════════════════════════
    // Bind
    // ═══════════════════════════════════════════════

    document.addEventListener('DOMContentLoaded', function () {
        var classes = ['js-output-weight', 'js-unit-price', 'js-discount', 'js-tax',
                       'js-cash-amount', 'js-inkind-weight', 'js-inkind-price'];
        for (var i = 0; i < classes.length; i++) {
            var el = byClass(classes[i]);
            if (el) {
                el.addEventListener('input', recalcAll);
                el.addEventListener('change', recalcAll);
            }
        }

        var ddl = document.getElementById('<%= ddlPaymentMode.ClientID %>');
        if (ddl) onPaymentModeChange(ddl);

        recalcAll();

        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
</script>
</asp:Content>