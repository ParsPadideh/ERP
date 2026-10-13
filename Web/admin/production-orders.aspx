<%@ Page Title="دستورهای تولید" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="production-orders.aspx.cs" Inherits="admin_production_orders" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .po-items-grid {
        display: grid;
        grid-template-columns: 1.5fr 100px 100px 100px 80px 40px;
        gap: 8px;
        align-items: center;
        padding: 8px 10px;
        border-radius: 8px;
        font-size: 12.5px;
        transition: all .15s;
    }
    .po-items-grid:hover { background: #f8fafc; }
    .po-items-header {
        background: #f1f5f9;
        font-weight: 700;
        color: #475569;
        font-size: 11px;
        text-transform: uppercase;
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditMaterialId" runat="server" Value="0" />
    <asp:HiddenField ID="hfCurrentOrderId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">دستورهای تولید</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="دستورهای تولید" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="دستورهای تولید محصولات را مدیریت کنید. با تکمیل، انبار خودکار به‌روز می‌شود." />
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
                <asp:Button ID="btnShowAddForm" runat="server" Text="دستور تولید جدید"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddMaterial" runat="server" Visible="false">
                <asp:Button ID="btnShowAddMaterialForm" runat="server" Text="افزودن ماده اولیه"
                    OnClick="btnShowAddMaterialForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
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
                        <i class="h-5 w-5" data-lucide="clipboard-list"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل دستورها</p>
                        <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="play-circle"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">در حال اجرا</p>
                        <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                            <asp:Literal ID="litStatInProgress" runat="server" Text="۰" />
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
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-danger-50 text-danger-500">
                        <i class="h-5 w-5" data-lucide="trash-2"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">حذف‌شده</p>
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
                        placeholder="جستجو در شماره دستور یا نام محصول..." />
                </label>

                <asp:DropDownList ID="ddlFilterStatus" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="پیش‌نویس" />
                    <asp:ListItem Value="2" Text="در حال اجرا" />
                    <asp:ListItem Value="3" Text="تکمیل‌شده" />
                    <asp:ListItem Value="4" Text="لغو‌شده" />
                </asp:DropDownList>
                 
                <asp:DropDownList ID="ddlFilterProduct" runat="server" AutoPostBack="true" data-placeholder="- انتخاب محصول-"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="select2-search h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
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
                            <th class="pb-3 pr-4 text-right font-semibold">دستور</th>
                            <th class="pb-3 pr-4 text-right font-semibold">محصول</th>
                            <th class="pb-3 pr-4 text-center font-semibold">برنامه / واقعی</th>
                            <th class="pb-3 pr-4 text-center font-semibold">BOM</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تاریخ</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptOrders" runat="server" OnItemCommand="rptOrders_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("Status"), Eval("IsDeleted")) %>'>
                                    <td class="py-3 pr-4">
                                        <p class="font-mono font-bold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("OrderNumber")) %>
                                            <%# GetDeletedBadge(Eval("IsDeleted")) %>
                                        </p>
                                    </td>
                                    <td class="py-3 pr-4">
                                        <p class="font-semibold text-ink-900 text-[13px]">
                                            <%# Enc(Eval("ProductName")) %>
                                        </p>
                                        <p class="mt-0.5 font-mono text-[11px] text-ink-400" dir="ltr">
                                            <%# Enc(Eval("ProductCode")) %>
                                        </p>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[12.5px] font-mono">
                                        <div class="text-ink-700">
                                            <%# FormatQty(Eval("PlannedQuantity")) %>
                                            <%# GetActualQtyHint(Eval("ProducedQuantity")) %>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetBomBadge(Eval("BOMVersion"), Eval("BOMId")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetStatusBadge(Eval("Status"), Eval("IsDeleted")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center font-mono text-[12px] text-ink-600">
                                        <p><%# FormatShamsiDate(Eval("CreatedAt")) %></p>
                                        <%# GetDateRangeHint(Eval("StartDate"), Eval("EndDate")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phActiveActions" runat="server"
                                                Visible='<%# CanEdit(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit"
                                                    CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="مشاهده / ویرایش"
                                                    Visible='<%# CanEditOrder(Eval("Status")) %>'>
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>

                                                <asp:PlaceHolder ID="phStart" runat="server"
                                                    Visible='<%# IsStatus(Eval("Status"), 1) %>'>
                                                    <asp:LinkButton ID="btnStart" runat="server"
                                                        CommandName="Start"
                                                        CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                        CssClass="inline-flex h-7 items-center gap-1 rounded-base bg-amber-500 px-2.5 text-[11px] font-bold text-white hover:bg-amber-600"
                                                        OnClientClick='<%# GetStartConfirm(Eval("OrderNumber")) %>'>
                                                        <i class="h-3 w-3" data-lucide="play"></i>
                                                        شروع
                                                    </asp:LinkButton>
                                                </asp:PlaceHolder>

                                                <asp:PlaceHolder ID="phComplete" runat="server"
                                                    Visible='<%# IsStatus(Eval("Status"), 2) %>'>
                                                    <asp:LinkButton ID="btnComplete" runat="server"
                                                        CommandName="Complete"
                                                        CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                        CssClass="inline-flex h-7 items-center gap-1 rounded-base bg-emerald-600 px-2.5 text-[11px] font-bold text-white hover:bg-emerald-700"
                                                        OnClientClick='<%# GetCompleteConfirm(Eval("OrderNumber")) %>'>
                                                        <i class="h-3 w-3" data-lucide="check"></i>
                                                        تکمیل
                                                    </asp:LinkButton>
                                                </asp:PlaceHolder>

                                                <asp:PlaceHolder ID="phCancel" runat="server"
                                                    Visible='<%# CanCancel(Eval("Status")) %>'>
                                                    <asp:LinkButton ID="btnCancel" runat="server"
                                                        CommandName="CancelOrder"
                                                        CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                        CssClass="icon-button hover:bg-warning-50 hover:text-warning-600"
                                                        ToolTip="لغو"
                                                        OnClientClick='<%# GetCancelConfirm(Eval("OrderNumber")) %>'>
                                                        <i class="h-4 w-4" data-lucide="x-circle"></i>
                                                    </asp:LinkButton>
                                                </asp:PlaceHolder>

                                                <asp:PlaceHolder ID="phDelete" runat="server"
                                                    Visible='<%# CanSoftDelete(Eval("Status")) %>'>
                                                    <asp:LinkButton ID="btnDelete" runat="server"
                                                        CommandName="SoftDelete"
                                                        CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                        CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                        ToolTip="حذف"
                                                        OnClientClick='<%# GetDeleteConfirm(Eval("OrderNumber")) %>'>
                                                        <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                    </asp:LinkButton>
                                                </asp:PlaceHolder>

                                                <asp:PlaceHolder ID="phBlockDelete" runat="server"
                                                    Visible='<%# IsStatus(Eval("Status"), 3) %>'>
                                                    <span class="icon-button text-ink-300 cursor-not-allowed"
                                                        title="دستور تکمیل‌شده روی انبار اثر داشته و قابل حذف نیست">
                                                        <i class="h-4 w-4" data-lucide="lock"></i>
                                                    </span>
                                                </asp:PlaceHolder>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore"
                                                    CommandArgument='<%# Eval("ProductionOrderId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                                    OnClientClick='<%# GetRestoreConfirm(Eval("OrderNumber")) %>'>
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
                    <i class="h-6 w-6" data-lucide="clipboard-list"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">دستور تولیدی یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین دستور تولید را ثبت کنید.</p>
            </asp:Panel>

            <div class="mt-5">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>
        </section>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 2: FORM (Create/Edit Order)
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">

        <!-- Order info -->
        <article class="mb-5 rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="clipboard-list"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="دستور تولید جدید" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        محصول، BOM و مقدار برنامه‌ریزی‌شده را وارد کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        محصول نهایی <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlProduct" runat="server" data-placeholder="-انتخاب محصول-"
                        AutoPostBack="true" OnSelectedIndexChanged="ddlProduct_SelectedIndexChanged"
                        CssClass="select2-search mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        BOM <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlBom" runat="server" data-placeholder="- انتخاب بوم-"
                        CssClass="select2-search mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                    <p class="mt-1 text-[11px] text-ink-400">فقط BOM فعال</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        مقدار برنامه‌ریزی‌شده <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtPlannedQty" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="100" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">مقدار تولیدشده (واقعی)</span>
                    <asp:TextBox ID="txtProducedQty" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="0" />
                    <p class="mt-1 text-[11px] text-ink-400">در زمان تکمیل پر می‌شود</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">مقدار ضایعات</span>
                    <asp:TextBox ID="txtWasteQty" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="0" />
                </label>

                <label class="block md:col-span-2 lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                    <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600"
                        placeholder="توضیحات اختیاری" />
                </label>

            </div>

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSaveOrder" runat="server" Text="ذخیره" OnClick="btnSaveOrder_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>

        <!-- Materials inline (فقط برای دستورهای موجود) -->
        <asp:Panel ID="pnlMaterialsInline" runat="server" Visible="false">
            <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

                <div class="mb-4 flex items-center justify-between border-b border-surface-line pb-3">
                    <h3 class="flex items-center gap-2 text-[14px] font-bold text-ink-900">
                        <span class="grid h-7 w-7 place-items-center rounded-base bg-violet-100 text-violet-700">
                            <i class="h-3.5 w-3.5" data-lucide="layers"></i>
                        </span>
                        مواد اولیه مورد نیاز
                    </h3>
                    <asp:Button ID="btnAutoFillFromBom" runat="server" Text="پر کردن خودکار از BOM"
                        OnClick="btnAutoFillFromBom_Click"
                        CssClass="inline-flex h-8 items-center gap-1.5 rounded-base border border-violet-300 bg-violet-50 px-3 text-[12px] font-bold text-violet-700 hover:bg-violet-100" />
                </div>

                <!-- Item form inline -->
                <asp:Panel ID="pnlMaterialForm" runat="server" Visible="false" CssClass="mb-4">
                    <div class="rounded-base border-2 border-dashed border-brand-200 bg-brand-50/30 p-4">
                        <div class="mb-3 flex items-center justify-between">
                            <h4 class="text-[13px] font-bold text-ink-800">
                                <asp:Literal ID="litMaterialFormTitle" runat="server" Text="افزودن ماده اولیه" />
                            </h4>
                            <asp:LinkButton ID="btnCancelMaterialForm" runat="server" OnClick="btnCancelMaterialForm_Click"
                                CssClass="text-[11.5px] font-semibold text-ink-500 hover:text-ink-700">
                                انصراف
                            </asp:LinkButton>
                        </div>

                        <div class="grid gap-3 md:grid-cols-4">
                            <label class="block md:col-span-2">
                                <span class="text-[12.5px] font-semibold text-ink-700">ماده اولیه</span>
                                <asp:DropDownList ID="ddlMaterial" runat="server" data-placeholder="- جستجو در مواد اولیه -"
                                    CssClass="select2-search mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                                </asp:DropDownList>
                            </label>

                            <label class="block">
                                <span class="text-[12.5px] font-semibold text-ink-700">مقدار برنامه‌ریزی‌شده</span>
                                <asp:TextBox ID="txtMaterialPlannedQty" runat="server" MaxLength="15" dir="ltr"
                                    CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                                    placeholder="10" />
                            </label>

                            <div class="flex items-end">
                                <asp:Button ID="btnSaveMaterial" runat="server" Text="ذخیره"
                                    OnClick="btnSaveMaterial_Click"
                                    CssClass="h-10 w-full rounded-base bg-brand-600 text-[13px] font-semibold text-white hover:bg-brand-700" />
                            </div>
                        </div>
                    </div>
                </asp:Panel>

                <!-- Items grid -->
                <div class="po-items-grid po-items-header">
                    <div>ماده اولیه</div>
                    <div class="text-center">برنامه‌ریزی</div>
                    <div class="text-center">واقعی</div>
                    <div class="text-center">قیمت واحد</div>
                    <div class="text-center">هزینه</div>
                    <div></div>
                </div>

                <asp:Repeater ID="rptMaterials" runat="server" OnItemCommand="rptMaterials_ItemCommand">
                    <ItemTemplate>
                        <div class="po-items-grid border-b border-surface-line/50">
                            <div class="flex items-center gap-2 min-w-0">
                                <span class="grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-amber-50 text-amber-600">
                                    <i class="h-3.5 w-3.5" data-lucide="pickaxe"></i>
                                </span>
                                <div class="min-w-0">
                                    <p class="truncate font-semibold text-ink-900 text-[12.5px]">
                                        <%# Enc(Eval("MaterialName")) %>
                                    </p>
                                    <p class="truncate font-mono text-[10.5px] text-ink-400" dir="ltr">
                                        <%# Enc(Eval("MaterialCode")) %>
                                    </p>
                                </div>
                            </div>
                            <div class="text-center font-mono text-ink-800">
                                <%# FormatQty(Eval("PlannedQuantity")) %>
                            </div>
                            <div class="text-center font-mono text-ink-700">
                                <%# FormatQty(Eval("ActualQuantity")) %>
                            </div>
                            <div class="text-center font-mono text-[12px] text-ink-600">
                                <%# FormatMoney(Eval("UnitCost")) %>
                            </div>
                            <div class="text-center font-mono font-bold text-danger-600 text-[12px]">
                                <%# FormatMoney(Eval("TotalCost")) %>
                            </div>
                            <div class="text-left">
                                <asp:PlaceHolder ID="phMaterialActions" runat="server"
                                    Visible='<%# CanEditMaterial(Eval("OrderStatus")) %>'>
                                    <div class="inline-flex items-center gap-1">
                                        <asp:LinkButton ID="btnEditMaterial" runat="server"
                                            CommandName="EditMaterial"
                                            CommandArgument='<%# Eval("ProductionOrderMaterialId") %>'
                                            CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            ToolTip="ویرایش">
                                            <i class="h-3.5 w-3.5" data-lucide="pencil"></i>
                                        </asp:LinkButton>

                                        <asp:LinkButton ID="btnDeleteMaterial" runat="server"
                                            CommandName="DeleteMaterial"
                                            CommandArgument='<%# Eval("ProductionOrderMaterialId") %>'
                                            CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                            ToolTip="حذف"
                                            OnClientClick='<%# GetDeleteMaterialConfirm(Eval("MaterialName")) %>'>
                                            <i class="h-3.5 w-3.5" data-lucide="trash-2"></i>
                                        </asp:LinkButton>
                                    </div>
                                </asp:PlaceHolder>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlMaterialsEmpty" runat="server" Visible="false" CssClass="py-8 text-center">
                    <p class="text-[13px] text-ink-400">مواد اولیه‌ای ثبت نشده.</p>
                    <p class="mt-1 text-[12px] text-ink-400">از دکمه «پر کردن خودکار از BOM» استفاده کنید.</p>
                </asp:Panel>

            </article>
        </asp:Panel>

    </asp:Panel>

</asp:Content>