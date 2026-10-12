<%@ Page Title="دسته‌بندی هزینه‌ها" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="expense-categories.aspx.cs" Inherits="admin_expense_categories" %>

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
                <span class="text-ink-700">دسته‌بندی هزینه‌ها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">دسته‌بندی هزینه‌ها</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                دسته‌بندی‌های هزینه‌های عملیاتی کارخانه را مدیریت کنید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن دسته هزینه"
                OnClick="btnShowAddForm_Click"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
        </div>
    </div>

    <!-- ═══════════ Stats ═══════════ -->
    <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="layers"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل دسته‌ها</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
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
                    <p class="text-[12px] text-ink-500">فعال</p>
                    <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                        <asp:Literal ID="litStatActive" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-5 w-5" data-lucide="pause-circle"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">غیرفعال</p>
                    <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                        <asp:Literal ID="litStatInactive" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                    <i class="h-5 w-5" data-lucide="receipt"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل هزینه‌های ثبت‌شده</p>
                    <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                        <asp:Literal ID="litStatExpenses" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

    </div>

    <!-- ═══════════ VIEW 1: List ═══════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <!-- Search + Filter -->
            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو در نام دسته..." />
                </label>

                <asp:DropDownList ID="ddlFilterActive" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterActive_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="فعال" />
                    <asp:ListItem Value="0" Text="غیرفعال" />
                </asp:DropDownList>
            </div>

            <!-- Table -->
            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[760px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">نام دسته</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تعداد هزینه‌ها</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[14px]">
                        <asp:Repeater ID="rptCategories" runat="server" OnItemCommand="rptCategories_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line transition hover:bg-surface-body/70">
                                    <td class="py-3 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetCategoryAvatarCss(Eval("IsActive")) %>'>
                                                <i class="h-4 w-4" data-lucide="tag"></i>
                                            </span>
                                            <p class="font-semibold text-ink-900 text-[13.5px]">
                                                <%# Enc(Eval("CategoryName")) %>
                                            </p>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700 font-mono">
                                            <%# FormatNumber(Eval("ExpenseCount")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeClass(Eval("IsActive")) %>'>
                                            <%# GetStatusText(Eval("IsActive")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:LinkButton ID="btnEdit" runat="server"
                                                CommandName="Edit"
                                                CommandArgument='<%# Eval("ExpenseCategoryId") %>'
                                                CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                ToolTip="ویرایش">
                                                <i class="h-4 w-4" data-lucide="pencil"></i>
                                            </asp:LinkButton>

                                            <asp:LinkButton ID="btnToggle" runat="server"
                                                CommandName="ToggleActive"
                                                CommandArgument='<%# Eval("ExpenseCategoryId") %>'
                                                CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("CategoryName")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                            </asp:LinkButton>

                                            <asp:LinkButton ID="btnDelete" runat="server"
                                                CommandName="Delete"
                                                CommandArgument='<%# Eval("ExpenseCategoryId") %>'
                                                CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                ToolTip="حذف"
                                                Visible='<%# CanDelete(Eval("ExpenseCount")) %>'
                                                OnClientClick='<%# GetDeleteConfirm(Eval("CategoryName")) %>'>
                                                <i class="h-4 w-4" data-lucide="trash-2"></i>
                                            </asp:LinkButton>
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
                    <i class="h-6 w-6" data-lucide="layers"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">دسته‌ای یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین دسته هزینه را اضافه کنید.</p>
            </asp:Panel>

            <div class="mt-5 flex items-center justify-between">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>
        </section>
    </asp:Panel>

    <!-- ═══════════ VIEW 2: Form ═══════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="layers"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="افزودن دسته هزینه" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        نام و وضعیت دسته را مشخص کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2">

                <label class="block md:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام دسته <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtCategoryName" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: حمل و نقل، تبلیغات، تعمیر و نگهداری" />
                </label>

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer">
                    <asp:CheckBox ID="chkIsActive" runat="server" Checked="true"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">فعال</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            دسته‌های غیرفعال در فرم ثبت هزینه نمایش داده نمی‌شوند.
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