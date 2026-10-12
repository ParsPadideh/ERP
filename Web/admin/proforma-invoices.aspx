<%@ Page Title="پیش‌فاکتورها" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="proforma-invoices.aspx.cs" Inherits="admin_proforma_invoices" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .pf-item-row {
        display: grid;
        grid-template-columns: 1.5fr 90px 90px 110px 80px 80px 120px 40px;
        gap: 8px;
        align-items: center;
        padding: 10px 12px;
        border-radius: 8px;
        font-size: 12.5px;
        transition: all .15s;
    }
    .pf-item-row:hover { background: #f8fafc; }
    .pf-item-header {
        background: #f1f5f9;
        font-weight: 700;
        color: #475569;
        font-size: 11px;
        text-transform: uppercase;
        border-radius: 8px;
    }
    .state-banner {
        position: relative;
        overflow: hidden;
    }
    .state-banner::before {
        content: '';
        position: absolute;
        right: 0; top: 0; bottom: 0;
        width: 4px;
    }
    .state-banner--draft::before { background: #94a3b8; }
    .state-banner--issued::before { background: #f59e0b; }
    .state-banner--paid::before { background: #3b82f6; }
    .state-banner--converted::before { background: #10b981; }
    .state-banner--cancelled::before { background: #ef4444; }
    .state-banner--expired::before { background: #a855f7; }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfCurrentProformaId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditItemId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditHeaderId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">پیش‌فاکتورها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="پیش‌فاکتورها" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="پیش‌فاکتورها را ثبت، صادر، و مدیریت کنید. صدور، موجودی را رزرو می‌کند." />
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

            <asp:PlaceHolder ID="phAddNew" runat="server">
                <asp:Button ID="btnShowAddForm" runat="server" Text="پیش‌فاکتور جدید"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddItem" runat="server" Visible="false">
                <asp:Button ID="btnShowAddItemForm" runat="server" Text="افزودن محصول"
                    OnClick="btnShowAddItemForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 1: LIST
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats (5 cards) -->
        <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-9 w-9 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-4 w-4" data-lucide="file-text"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">کل</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-9 w-9 place-items-center rounded-base bg-surface-muted text-ink-600">
                        <i class="h-4 w-4" data-lucide="file-edit"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">پیش‌نویس</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-600">
                            <asp:Literal ID="litStatDraft" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-9 w-9 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-4 w-4" data-lucide="clock"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">صادرشده</p>
                        <p class="mt-0.5 text-[18px] font-bold text-amber-600">
                            <asp:Literal ID="litStatIssued" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-9 w-9 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-4 w-4" data-lucide="check-circle-2"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">تبدیل‌شده</p>
                        <p class="mt-0.5 text-[18px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatConverted" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-4 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-9 w-9 place-items-center rounded-base bg-danger-50 text-danger-500">
                        <i class="h-4 w-4" data-lucide="trash-2"></i>
                    </span>
                    <div>
                        <p class="text-[11.5px] text-ink-500">حذف‌شده</p>
                        <p class="mt-0.5 text-[18px] font-bold text-danger-600">
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
                        placeholder="جستجو در شماره یا نام مشتری..." />
                </label>

                <asp:DropDownList ID="ddlFilterStatus" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="پیش‌نویس" />
                    <asp:ListItem Value="2" Text="صادرشده" />
                    <asp:ListItem Value="3" Text="پرداخت‌شده" />
                    <asp:ListItem Value="4" Text="تبدیل‌شده" />
                    <asp:ListItem Value="5" Text="لغو‌شده" />
                    <asp:ListItem Value="6" Text="منقضی" />
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
                <table class="w-full min-w-[1200px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">شماره</th>
                            <th class="pb-3 pr-4 text-right font-semibold">مشتری</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تعداد اقلام</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مبلغ کل</th>
                            <th class="pb-3 pr-4 text-center font-semibold">اعتبار</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptProformas" runat="server" OnItemCommand="rptProformas_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("Status"), Eval("IsDeleted")) %>'>
                                    <td class="py-3 pr-4">
                                        <p class="font-mono font-bold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("ProformaNumber")) %>
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
                                        <%# GetCustomerTypeHint(Eval("CustomerType")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700 font-mono">
                                            <%# FormatCount(Eval("ItemCount")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono font-bold text-emerald-700">
                                            <%# FormatMoney(Eval("Total")) %>
                                        </span>
                                        <span class="text-[10.5px] text-ink-400"> تومان</span>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[11.5px] font-mono">
                                        <%# GetValidityCell(Eval("Status"), Eval("ValidUntil"), Eval("IsDeleted")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetStatusBadge(Eval("Status"), Eval("IsDeleted")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:LinkButton ID="btnView" runat="server"
                                                CommandName="View"
                                                CommandArgument='<%# Eval("ProformaInvoiceId") %>'
                                                CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700"
                                                Visible='<%# !IsTrue(Eval("IsDeleted")) %>'>
                                                <i class="h-3.5 w-3.5" data-lucide="eye"></i>
                                                مشاهده
                                            </asp:LinkButton>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore"
                                                    CommandArgument='<%# Eval("ProformaInvoiceId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                                    OnClientClick='<%# GetRestoreConfirm(Eval("ProformaNumber")) %>'>
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
                    <i class="h-6 w-6" data-lucide="file-text"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">پیش‌فاکتوری یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین پیش‌فاکتور را ثبت کنید.</p>
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
            class="state-banner state-banner--draft mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card">

            <div class="flex flex-wrap items-center gap-4">
                <span id="divStateIcon" runat="server"
                    class="grid h-12 w-12 place-items-center rounded-xl bg-surface-muted text-ink-600">
                    <i class="h-6 w-6" data-lucide="file-edit"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[11.5px] font-semibold text-ink-500">
                        <asp:Literal ID="litDetailStateText" runat="server" Text="پیش‌فاکتور" />
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

            <div class="mt-4 grid gap-3 sm:grid-cols-3">
                <div class="rounded-base bg-surface-muted px-3 py-2">
                    <p class="text-[11px] text-ink-500">تاریخ صدور</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-ink-800">
                        <asp:Literal ID="litDetailIssueDate" runat="server" />
                    </p>
                </div>
                <div class="rounded-base bg-surface-muted px-3 py-2">
                    <p class="text-[11px] text-ink-500">معتبر تا</p>
                    <p class="mt-0.5 font-mono text-[13px] font-bold text-ink-800">
                        <asp:Literal ID="litDetailValidUntil" runat="server" />
                    </p>
                </div>
                <div class="rounded-base bg-surface-muted px-3 py-2">
                    <p class="text-[11px] text-ink-500">وضعیت</p>
                    <div class="mt-0.5">
                        <asp:Literal ID="litDetailStatusBadge" runat="server" />
                    </div>
                </div>
            </div>

        </div>

        <!-- ═══ Header form (editable in Draft) ═══ -->
        <asp:Panel ID="pnlHeaderForm" runat="server" Visible="false">
            <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
                <div class="mb-4 flex items-center gap-3 border-b border-surface-line pb-3">
                    <h2 class="text-[15px] font-bold text-ink-900">اطلاعات پیش‌فاکتور</h2>
                </div>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                    <label class="block lg:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مشتری <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlCustomer" runat="server"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlCustomer_SelectedIndexChanged"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                        <p class="mt-1 text-[11px] text-ink-400">
                            <asp:Literal ID="litCustomerHint" runat="server" />
                        </p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">کانال فروش</span>
                        <asp:DropDownList ID="ddlChannel" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="1" Text="بازاریاب" />
                            <asp:ListItem Value="2" Text="آنلاین" />
                            <asp:ListItem Value="3" Text="ادمین / دفتری" />
                        </asp:DropDownList>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مدت اعتبار (روز) <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtValidityDays" runat="server" MaxLength="3" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="3" />
                        <p class="mt-1 text-[11px] text-ink-400">پیش‌فرض: ۳ روز</p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">شرایط پرداخت</span>
                        <asp:TextBox ID="txtPaymentTerms" runat="server" MaxLength="200"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600"
                            placeholder="مثال: ۵۰٪ پیش‌پرداخت" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">شرایط تحویل</span>
                        <asp:TextBox ID="txtDeliveryTerms" runat="server" MaxLength="200"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600"
                            placeholder="مثال: تحویل درب کارخانه" />
                    </label>

                    <label class="block md:col-span-2 lg:col-span-3">
                        <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                        <asp:TextBox ID="txtHeaderNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                            CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600"
                            placeholder="توضیحات اختیاری" />
                    </label>

                </div>
            </article>
        </asp:Panel>

        <!-- ═══ Read-only header summary (non-Draft) ═══ -->
        <asp:Panel ID="pnlHeaderSummary" runat="server" Visible="false">
            <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
                <h2 class="mb-4 border-b border-surface-line pb-3 text-[15px] font-bold text-ink-900">اطلاعات پیش‌فاکتور</h2>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                    <div>
                        <p class="text-[11.5px] text-ink-500">کانال فروش</p>
                        <p class="mt-0.5 text-[13px] font-semibold text-ink-800">
                            <asp:Literal ID="litSumChannel" runat="server" />
                        </p>
                    </div>
                    <div>
                        <p class="text-[11.5px] text-ink-500">نوع مشتری</p>
                        <p class="mt-0.5 text-[13px] font-semibold text-ink-800">
                            <asp:Literal ID="litSumOrderType" runat="server" />
                        </p>
                    </div>
                    <div>
                        <p class="text-[11.5px] text-ink-500">مدت اعتبار</p>
                        <p class="mt-0.5 text-[13px] font-semibold text-ink-800">
                            <asp:Literal ID="litSumValidity" runat="server" />
                        </p>
                    </div>
                    <div>
                        <p class="text-[11.5px] text-ink-500">شرایط پرداخت</p>
                        <p class="mt-0.5 text-[13px] text-ink-700">
                            <asp:Literal ID="litSumPaymentTerms" runat="server" />
                        </p>
                    </div>
                    <div>
                        <p class="text-[11.5px] text-ink-500">شرایط تحویل</p>
                        <p class="mt-0.5 text-[13px] text-ink-700">
                            <asp:Literal ID="litSumDeliveryTerms" runat="server" />
                        </p>
                    </div>
                    <div>
                        <p class="text-[11.5px] text-ink-500">تاریخ ایجاد</p>
                        <p class="mt-0.5 font-mono text-[13px] text-ink-700">
                            <asp:Literal ID="litSumCreatedAt" runat="server" />
                        </p>
                    </div>
                </div>

                <asp:Panel ID="pnlSumNotes" runat="server" Visible="false" CssClass="mt-4 border-t border-surface-line pt-3">
                    <p class="text-[11.5px] text-ink-500">یادداشت</p>
                    <p class="mt-1 text-[12.5px] text-ink-700 leading-relaxed">
                        <asp:Literal ID="litSumNotes" runat="server" />
                    </p>
                </asp:Panel>
            </article>
        </asp:Panel>

        <!-- ═══ Items section ═══ -->
        <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-4 flex flex-wrap items-center justify-between gap-2">
                <h2 class="flex items-center gap-2 text-[15px] font-bold text-ink-900">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="package"></i>
                    </span>
                    اقلام پیش‌فاکتور
                </h2>
                <div class="flex items-center gap-2">
                    <p class="text-[12.5px] text-ink-500">
                        <asp:Literal ID="litItemsInfo" runat="server" />
                    </p>
                    <asp:Button ID="btnAutoFillAllPrices" runat="server"
                        Text="پر کردن خودکار قیمت‌ها از لیست"
                        OnClick="btnAutoFillAllPrices_Click"
                        Visible="false"
                        CssClass="inline-flex h-8 items-center gap-1.5 rounded-base border border-violet-300 bg-violet-50 px-3 text-[11.5px] font-bold text-violet-700 hover:bg-violet-100" />
                </div>
            </div>

            <!-- Item form (only in Draft) -->
            <asp:Panel ID="pnlItemForm" runat="server" Visible="false" CssClass="mb-4">
                <div class="rounded-base border-2 border-dashed border-brand-200 bg-brand-50/30 p-4">
                    <div class="mb-3 flex items-center justify-between">
                        <h4 class="text-[13px] font-bold text-ink-800">
                            <asp:Literal ID="litItemFormTitle" runat="server" Text="افزودن محصول" />
                        </h4>
                        <asp:LinkButton ID="btnCancelItemForm" runat="server" OnClick="btnCancelItemForm_Click"
                            CssClass="text-[11.5px] font-semibold text-ink-500 hover:text-ink-700">
                            <i class="inline h-3 w-3" data-lucide="x"></i>
                            انصراف
                        </asp:LinkButton>
                    </div>

                    <div class="grid gap-3 md:grid-cols-4 lg:grid-cols-6">

                        <label class="block md:col-span-2 lg:col-span-3">
                            <span class="text-[12.5px] font-semibold text-ink-700">
                                محصول <span class="text-danger-500">*</span>
                            </span>
                            <asp:DropDownList ID="ddlItemProduct" runat="server"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlItemProduct_SelectedIndexChanged"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                            </asp:DropDownList>
                            <p class="mt-1 text-[11px] text-ink-400">
                                برای جستجوی سریع، کد یا نام محصول را تایپ کنید
                            </p>
                        </label>

                        <label class="block">
                            <span class="text-[12.5px] font-semibold text-ink-700">
                                تعداد <span class="text-danger-500">*</span>
                            </span>
                            <asp:TextBox ID="txtItemQty" runat="server" MaxLength="15" dir="ltr"
                                AutoPostBack="true" OnTextChanged="txtItemQty_TextChanged"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                                placeholder="1" />
                        </label>

                        <label class="block">
                            <span class="text-[12.5px] font-semibold text-ink-700">قیمت واحد</span>
                            <asp:TextBox ID="txtItemUnitPrice" runat="server" MaxLength="15" dir="ltr"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                                placeholder="خودکار" />
                        </label>

                        <label class="block">
                            <span class="text-[12.5px] font-semibold text-ink-700">تخفیف</span>
                            <asp:TextBox ID="txtItemDiscount" runat="server" MaxLength="15" dir="ltr"
                                CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                                placeholder="0" />
                        </label>

                        <div class="lg:col-span-6 flex items-center justify-end border-t border-brand-200/60 pt-3">
                            <asp:Button ID="btnSaveItem" runat="server" Text="ذخیره محصول"
                                OnClick="btnSaveItem_Click"
                                CssClass="inline-flex h-9 items-center rounded-base bg-brand-600 px-4 text-[13px] font-semibold text-white hover:bg-brand-700" />
                        </div>

                    </div>
                </div>
            </asp:Panel>

            <!-- Items grid -->
            <div class="pf-item-row pf-item-header">
                <div>محصول</div>
                <div class="text-center">تعداد</div>
                <div class="text-center">موجودی</div>
                <div class="text-center">قیمت واحد</div>
                <div class="text-center">تخفیف</div>
                <div class="text-center">مالیات</div>
                <div class="text-center">جمع</div>
                <div></div>
            </div>

            <asp:Repeater ID="rptItems" runat="server" OnItemCommand="rptItems_ItemCommand">
                <ItemTemplate>
                    <div class="pf-item-row border-b border-surface-line/50">
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
                        <div class="text-center">
                            <%# GetAvailabilityBadge(Eval("Available"), Eval("Quantity")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-ink-600">
                            <%# FormatMoney(Eval("UnitPrice")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-rose-600">
                            <%# FormatMoney(Eval("Discount")) %>
                        </div>
                        <div class="text-center font-mono text-[12px] text-ink-500">
                            <%# FormatMoney(Eval("Tax")) %>
                        </div>
                        <div class="text-center font-mono font-bold text-emerald-700">
                            <%# FormatMoney(Eval("Total")) %>
                        </div>
                        <div class="text-left">
                            <asp:PlaceHolder ID="phItemActions" runat="server"
                                Visible='<%# CanEditItems(Eval("InvoiceStatus"), Eval("IsDeleted")) %>'>
                                <div class="inline-flex items-center gap-1">
                                    <asp:LinkButton ID="btnEditItem" runat="server"
                                        CommandName="EditItem"
                                        CommandArgument='<%# Eval("ProformaInvoiceItemId") %>'
                                        CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                        ToolTip="ویرایش">
                                        <i class="h-3.5 w-3.5" data-lucide="pencil"></i>
                                    </asp:LinkButton>

                                    <asp:LinkButton ID="btnDeleteItem" runat="server"
                                        CommandName="DeleteItem"
                                        CommandArgument='<%# Eval("ProformaInvoiceItemId") %>'
                                        CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                        ToolTip="حذف"
                                        OnClientClick='<%# GetDeleteItemConfirm(Eval("ProductName")) %>'>
                                        <i class="h-3.5 w-3.5" data-lucide="trash-2"></i>
                                    </asp:LinkButton>
                                </div>
                            </asp:PlaceHolder>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlItemsEmpty" runat="server" Visible="false" CssClass="py-10 text-center">
                <span class="mx-auto mb-3 grid h-12 w-12 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-5 w-5" data-lucide="package-search"></i>
                </span>
                <p class="text-[13px] font-semibold text-ink-600">هنوز محصولی اضافه نشده.</p>
                <p class="mt-1 text-[12px] text-ink-400">با دکمه «افزودن محصول» شروع کنید.</p>
            </asp:Panel>

            <!-- Totals -->
            <asp:Panel ID="pnlTotals" runat="server" Visible="false" CssClass="mt-5 border-t border-surface-line pt-4">
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
                            <span class="text-[11px] font-normal text-ink-500">تومان</span>
                        </p>
                    </div>
                    <div class="text-right">
                        <p class="text-[11.5px] text-ink-500">مالیات</p>
                        <p class="mt-0.5 font-mono font-bold text-ink-700">
                            <asp:Literal ID="litTax" runat="server" Text="۰" />
                            <span class="text-[11px] font-normal text-ink-500">تومان</span>
                        </p>
                    </div>
                    <div class="rounded-base bg-emerald-50 border border-emerald-200 px-4 py-2 text-right">
                        <p class="text-[11.5px] font-bold text-emerald-700">مبلغ کل</p>
                        <p class="mt-0.5 font-mono text-[18px] font-black text-emerald-800">
                            <asp:Literal ID="litTotal" runat="server" Text="۰" />
                            <span class="text-[11px] font-normal">تومان</span>
                        </p>
                    </div>
                </div>
            </asp:Panel>

        </article>

        <!-- ═══ Actions bar ═══ -->
        <div class="flex flex-wrap items-center justify-between gap-3 rounded-card border border-surface-line bg-surface-card p-4 shadow-card">

            <div class="flex flex-wrap items-center gap-2">
                <asp:PlaceHolder ID="phActionSaveHeader" runat="server" Visible="false">
                    <asp:Button ID="btnSaveHeader" runat="server" Text="ذخیره پیش‌نویس"
                        OnClick="btnSaveHeader_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-4 text-[13.5px] font-semibold text-white hover:bg-brand-700" />
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionIssue" runat="server" Visible="false">
                    <asp:Button ID="btnIssue" runat="server" Text="صدور پیش‌فاکتور"
                        OnClick="btnIssue_Click"
                        OnClientClick="return confirm('پیش‌فاکتور صادر شود؟\n\nتوجه: موجودی محصولات رزرو خواهد شد.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-amber-600 px-4 text-[13.5px] font-semibold text-white hover:bg-amber-700" />
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionConvert" runat="server" Visible="false">
                    <asp:Button ID="btnConvert" runat="server" Text="تبدیل به فاکتور نهایی"
                        OnClick="btnConvert_Click"
                        OnClientClick="return confirm('پیش‌فاکتور به فاکتور نهایی تبدیل شود؟\n\nتوجه: این عملیات قابل بازگشت نیست.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-emerald-600 px-4 text-[13.5px] font-semibold text-white hover:bg-emerald-700" />
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionCancel" runat="server" Visible="false">
                    <asp:LinkButton ID="btnCancelProforma" runat="server" OnClick="btnCancelProforma_Click"
                        OnClientClick="return confirm('پیش‌فاکتور لغو شود؟\n\nتوجه: رزروها آزاد می‌شوند.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border-2 border-danger-500 bg-white px-4 text-[13.5px] font-semibold text-danger-600 hover:bg-danger-50">
                        <i class="h-4 w-4" data-lucide="x-circle"></i>
                        لغو پیش‌فاکتور
                    </asp:LinkButton>
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionPrint" runat="server" Visible="false">
                    <asp:HyperLink ID="lnkPrint" runat="server" Target="_blank"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-surface-line bg-white px-4 text-[13.5px] font-semibold text-ink-700 hover:bg-surface-muted">
                        <i class="h-4 w-4" data-lucide="printer"></i>
                        چاپ
                    </asp:HyperLink>
                </asp:PlaceHolder>

                <asp:PlaceHolder ID="phActionDelete" runat="server" Visible="false">
                    <asp:LinkButton ID="btnSoftDelete" runat="server" OnClick="btnSoftDelete_Click"
                        OnClientClick="return confirm('پیش‌فاکتور حذف شود؟\n\nتوجه: قابل بازیابی است.');"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base border border-danger-300 bg-white px-4 text-[13.5px] font-semibold text-danger-600 hover:bg-danger-50">
                        <i class="h-4 w-4" data-lucide="trash-2"></i>
                        حذف
                    </asp:LinkButton>
                </asp:PlaceHolder>
            </div>

            <asp:PlaceHolder ID="phFinalInvoiceLink" runat="server" Visible="false">
                <a id="aFinalInvoiceLink" runat="server"
                    class="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-4 text-[13.5px] font-semibold text-white hover:bg-brand-700">
                    <i class="h-4 w-4" data-lucide="external-link"></i>
                    مشاهده فاکتور نهایی
                </a>
            </asp:PlaceHolder>

        </div>

    </asp:Panel>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══════════════════════════════════════════════
    // Select2 — Searchable Dropdowns
    // ═══════════════════════════════════════════════
    function initSelect2() {
        if (typeof jQuery === 'undefined' || typeof jQuery.fn.select2 === 'undefined') {
            return;
        }

        var $ = jQuery;

        // ═══ Dropdown مشتری (فرم) ═══
        var $customer = $('#<%= ddlCustomer.ClientID %>');
        if ($customer.length > 0 && !$customer.hasClass('select2-hidden-accessible')) {
            $customer.select2({
                placeholder: '— جستجو در مشتریان (نام، کد، شهر، موبایل) —',
                allowClear: false,
                width: '100%',
                dir: 'rtl',
                language: {
                    noResults: function () { return 'مشتری‌ای یافت نشد'; },
                    searching: function () { return 'در حال جستجو...'; }
                }
            });

            // وقتی کاربر مشتری را انتخاب می‌کند، PostBack
            $customer.on('select2:select', function (e) {
                if (typeof __doPostBack === 'function') {
                    __doPostBack('<%= ddlCustomer.UniqueID %>', '');
                }
            });
        }

        // ═══ Dropdown فیلتر مشتری (لیست) ═══
        var $filter = $('#<%= ddlFilterCustomer.ClientID %>');
        if ($filter.length > 0 && !$filter.hasClass('select2-hidden-accessible')) {
            $filter.select2({
                placeholder: '— جستجو —',
                allowClear: true,
                width: '100%',
                dir: 'rtl',
                language: {
                    noResults: function () { return 'مشتری‌ای یافت نشد'; }
                }
            });
        }

        // ═══ Dropdown محصول (فرم اقلام) ═══
        var $product = $('#<%= ddlItemProduct.ClientID %>');
        if ($product.length > 0 && !$product.hasClass('select2-hidden-accessible')) {
            $product.select2({
                placeholder: '— جستجو در محصولات (کد، نام، دسته) —',
                allowClear: true,
                width: '100%',
                dir: 'rtl',
                language: {
                    noResults: function () { return 'محصولی یافت نشد'; },
                    searching: function () { return 'در حال جستجو...'; }
                }
            });

            $product.on('select2:select', function (e) {
                if (typeof __doPostBack === 'function') {
                    __doPostBack('<%= ddlItemProduct.UniqueID %>', '');
                }
            });
        }
    }

    // ═══════════════════════════════════════════════
    // اجرا در بارگذاری + بعد از هر PostBack
    // ═══════════════════════════════════════════════
    if (typeof jQuery !== 'undefined') {
        jQuery(document).ready(function () {
            initSelect2();
            if (typeof lucide !== 'undefined') lucide.createIcons();
        });
    }

    if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            setTimeout(initSelect2, 100);
        });
    }

    window.addEventListener('load', function () {
        setTimeout(initSelect2, 100);
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
</script>
</asp:Content>