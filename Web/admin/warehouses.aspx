<%@ Page Title="انبارها" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="warehouses.aspx.cs" Inherits="admin_warehouses" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">انبارها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="انبارها" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="انبارهای مواد اولیه و محصولات نهایی را مدیریت کنید." />
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
                <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن انبار"
                    OnClick="btnShowAddForm_Click"
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
                        <i class="h-5 w-5" data-lucide="warehouse"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل انبارها</p>
                        <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="pickaxe"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">انبار مواد اولیه</p>
                        <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                            <asp:Literal ID="litStatRM" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="package-check"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">انبار محصول نهایی</p>
                        <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                            <asp:Literal ID="litStatFG" runat="server" Text="۰" />
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
            <div class="grid gap-3 md:grid-cols-4">

                <label class="relative block md:col-span-2">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-600"
                        placeholder="جستجو در نام یا موقعیت انبار..." />
                </label>

                <asp:DropDownList ID="ddlFilterType" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه انواع" />
                    <asp:ListItem Value="1" Text="مواد اولیه (RM)" />
                    <asp:ListItem Value="2" Text="محصول نهایی (FG)" />
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterActive" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="فعال" />
                    <asp:ListItem Value="0" Text="غیرفعال" />
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
                <table class="w-full min-w-[900px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">نام انبار</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                            <th class="pb-3 pr-4 text-right font-semibold">موقعیت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">محصولات موجود</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptWarehouses" runat="server" OnItemCommand="rptWarehouses_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("IsDeleted")) %>'>
                                    <td class="py-3 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetWarehouseAvatarCss(Eval("WarehouseType"), Eval("IsDeleted")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetWarehouseIcon(Eval("WarehouseType")) %>'></i>
                                            </span>
                                            <div>
                                                <p class="font-semibold text-ink-900 text-[13.5px]">
                                                    <%# Enc(Eval("WarehouseName")) %>
                                                    <%# GetDeletedBadge(Eval("IsDeleted")) %>
                                                </p>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetTypeBadge(Eval("WarehouseType")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-[12.5px] text-ink-600">
                                        <%# FormatOrDash(Eval("Location")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-brand-50 text-brand-700 font-mono">
                                            <%# FormatCount(Eval("ProductCount")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetStatusBadge(Eval("IsActive"), Eval("IsDeleted")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phActiveActions" runat="server"
                                                Visible='<%# CanEdit(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit"
                                                    CommandArgument='<%# Eval("WarehouseId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnToggle" runat="server"
                                                    CommandName="ToggleActive"
                                                    CommandArgument='<%# Eval("WarehouseId") %>'
                                                    CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                    ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                    OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("WarehouseName")) %>'>
                                                    <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                                </asp:LinkButton>

                                                <asp:PlaceHolder ID="phDeleteBtn" runat="server"
                                                    Visible='<%# CanDelete(Eval("ProductCount")) %>'>
                                                    <asp:LinkButton ID="btnDelete" runat="server"
                                                        CommandName="SoftDelete"
                                                        CommandArgument='<%# Eval("WarehouseId") %>'
                                                        CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                        ToolTip="حذف"
                                                        OnClientClick='<%# GetDeleteConfirm(Eval("WarehouseName")) %>'>
                                                        <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                    </asp:LinkButton>
                                                </asp:PlaceHolder>

                                                <asp:PlaceHolder ID="phBlockDelete" runat="server"
                                                    Visible='<%# !CanDelete(Eval("ProductCount")) %>'>
                                                    <span class="icon-button text-ink-300 cursor-not-allowed"
                                                        title="قابل حذف نیست — موجودی دارد">
                                                        <i class="h-4 w-4" data-lucide="lock"></i>
                                                    </span>
                                                </asp:PlaceHolder>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore"
                                                    CommandArgument='<%# Eval("WarehouseId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                                    OnClientClick='<%# GetRestoreConfirm(Eval("WarehouseName")) %>'>
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
                    <i class="h-6 w-6" data-lucide="warehouse"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">انباری یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین انبار را اضافه کنید.</p>
            </asp:Panel>

            <div class="mt-5">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>
        </section>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 2: FORM
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="warehouse"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="افزودن انبار جدید" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        اطلاعات انبار را وارد کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام انبار <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtWarehouseName" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: انبار مواد اولیه مرکزی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نوع انبار <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlWarehouseType" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="1" Text="مواد اولیه (RM)" />
                        <asp:ListItem Value="2" Text="محصول نهایی (FG)" />
                    </asp:DropDownList>
                </label>

                <label class="block md:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">موقعیت فیزیکی</span>
                    <asp:TextBox ID="txtLocation" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600"
                        placeholder="مثال: سالن ۲ — ردیف ۵" />
                </label>

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer md:col-span-2">
                    <asp:CheckBox ID="chkIsActive" runat="server" Checked="true"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">انبار فعال</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            انبار غیرفعال در فرم‌های ثبت موجودی نمایش داده نمی‌شود.
                        </p>
                    </div>
                </label>

            </div>

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>

        </article>
    </asp:Panel>

</asp:Content>