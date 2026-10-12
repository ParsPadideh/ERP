<%@ Page Title="کاتالوگ خدمات" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="service-types.aspx.cs" Inherits="admin_service_types" %>

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
                <span class="text-ink-700">کاتالوگ خدمات</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="کاتالوگ خدمات" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="انواع خدمات قابل ارائه به مشتریان (بوجاری، خشک‌کن، آسیاب و ...) را مدیریت کنید." />
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
                <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن خدمت جدید"
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
        <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="layers"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل خدمات</p>
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
                        <p class="text-[12px] text-ink-500">سفارش‌های ثبت‌شده</p>
                        <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                            <asp:Literal ID="litStatOrders" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>
        </div>

        <!-- List -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو در نام یا کد خدمت..." />
                </label>

                <asp:DropDownList ID="ddlFilterActive" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterActive_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="فعال" />
                    <asp:ListItem Value="0" Text="غیرفعال" />
                </asp:DropDownList>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[900px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">خدمت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">واحد</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نرخ پیش‌فرض</th>
                            <th class="pb-3 pr-4 text-center font-semibold">ضایعات پیش‌فرض</th>
                            <th class="pb-3 pr-4 text-center font-semibold">سفارش‌ها</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptTypes" runat="server" OnItemCommand="rptTypes_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line transition hover:bg-surface-body/70">
                                    <td class="py-3 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetServiceAvatarCss(Eval("IsActive")) %>'>
                                                <i class="h-4 w-4" data-lucide="settings"></i>
                                            </span>
                                            <div class="min-w-0">
                                                <p class="truncate font-semibold text-ink-900 text-[13.5px]">
                                                    <%# Enc(Eval("ServiceName")) %>
                                                </p>
                                                <p class="mt-0.5 truncate font-mono text-[11px] text-ink-400" dir="ltr">
                                                    <%# Enc(Eval("ServiceCode")) %>
                                                </p>
                                                <%# GetDescriptionHint(Eval("Description")) %>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700">
                                            <%# Enc(Eval("UnitName")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono font-bold text-emerald-700">
                                            <%# FormatMoney(Eval("DefaultUnitPrice")) %>
                                        </span>
                                        <span class="text-[10.5px] text-ink-400"> تومان</span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetWasteBadge(Eval("DefaultWastePercent")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700 font-mono">
                                            <%# FormatCount(Eval("OrderCount")) %>
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
                                                CommandArgument='<%# Eval("ServiceTypeId") %>'
                                                CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                ToolTip="ویرایش">
                                                <i class="h-4 w-4" data-lucide="pencil"></i>
                                            </asp:LinkButton>

                                            <asp:LinkButton ID="btnToggle" runat="server"
                                                CommandName="ToggleActive"
                                                CommandArgument='<%# Eval("ServiceTypeId") %>'
                                                CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("ServiceName")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                            </asp:LinkButton>

                                            <asp:PlaceHolder ID="phDelete" runat="server"
                                                Visible='<%# CanDelete(Eval("OrderCount")) %>'>
                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="Delete"
                                                    CommandArgument='<%# Eval("ServiceTypeId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick='<%# GetDeleteConfirm(Eval("ServiceName")) %>'>
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
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
                    <i class="h-6 w-6" data-lucide="layers"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">خدمتی ثبت نشده است.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین خدمت را اضافه کنید.</p>
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
                    <i class="h-5 w-5" data-lucide="layers"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="افزودن خدمت جدید" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        مشخصات خدمت و نرخ پیش‌فرض آن را وارد کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        کد خدمت <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtServiceCode" runat="server" MaxLength="30" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="SRV-005" />
                    <p class="mt-1 text-[11px] text-ink-400">کد یکتا برای این خدمت</p>
                </label>

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام خدمت <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtServiceName" runat="server" MaxLength="150"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: بوجاری غلات" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        واحد اندازه‌گیری <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlUnitName" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="کیلوگرم" Text="کیلوگرم" />
                        <asp:ListItem Value="تن" Text="تن" />
                        <asp:ListItem Value="گرم" Text="گرم" />
                        <asp:ListItem Value="عدد" Text="عدد" />
                        <asp:ListItem Value="بسته" Text="بسته" />
                        <asp:ListItem Value="کارتن" Text="کارتن" />
                        <asp:ListItem Value="ساعت" Text="ساعت" />
                        <asp:ListItem Value="متر" Text="متر" />
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نرخ پیش‌فرض (تومان)
                    </span>
                    <asp:TextBox ID="txtDefaultUnitPrice" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="500" />
                    <p class="mt-1 text-[11px] text-ink-400">قابل تغییر در هر سفارش</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">ضایعات پیش‌فرض</span>
                    <div class="relative mt-1.5">
                        <asp:TextBox ID="txtDefaultWastePercent" runat="server" MaxLength="6" dir="ltr"
                            CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-10 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                            placeholder="3" />
                        <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                    </div>
                    <p class="mt-1 text-[11px] text-ink-400">درصد افت مواد در حین خدمت</p>
                </label>

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer lg:col-span-2">
                    <asp:CheckBox ID="chkIsActive" runat="server" Checked="true"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">خدمت فعال</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            خدمت غیرفعال در فرم ثبت سفارش جدید نمایش داده نمی‌شود.
                        </p>
                    </div>
                </label>

                <label class="block lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">توضیحات</span>
                    <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="3" MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="توضیحات کامل خدمت (اختیاری)" />
                </label>

            </div>

            <!-- Existing service warning (edit mode) -->
            <asp:Panel ID="pnlEditWarning" runat="server" Visible="false"
                CssClass="mt-4 rounded-base border border-amber-300 bg-amber-50 p-3 text-[12.5px] text-amber-800">
                <i class="inline h-4 w-4 align-middle ml-1" data-lucide="alert-triangle"></i>
                این خدمت در <asp:Literal ID="litEditOrderCount" runat="server" Text="۰" /> سفارش استفاده شده است.
                تغییر نرخ پیش‌فرض روی سفارش‌های قبلی اثری ندارد.
            </asp:Panel>

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>

        </article>
    </asp:Panel>

</asp:Content>