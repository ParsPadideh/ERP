<%@ Page Title="هزینه‌ها" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="expenses.aspx.cs" Inherits="admin_expenses" %>

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
                <span class="text-ink-700">هزینه‌ها</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">هزینه‌های عملیاتی</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                هزینه‌های کارخانه را ثبت و پیگیری کنید تا سود خالص دقیق محاسبه شود.
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

            <asp:PlaceHolder ID="phAddExpense" runat="server">
                <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن هزینه"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- ═══════════ VIEW 1: List ═══════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats -->
        <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="wallet"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل هزینه‌ها (تومان)</p>
                        <p class="mt-0.5 text-[18px] font-bold text-ink-900 font-mono">
                            <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="calendar-days"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">هزینه‌های این ماه</p>
                        <p class="mt-0.5 text-[18px] font-bold text-violet-600 font-mono">
                            <asp:Literal ID="litStatMonth" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                        <i class="h-5 w-5" data-lucide="receipt"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">تعداد رکوردهای این ماه</p>
                        <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatCount" runat="server" Text="۰" />
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

        <!-- List Section -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <!-- Filters -->
            <div class="mb-5 grid gap-3 lg:grid-cols-12">

                <label class="relative block lg:col-span-3">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-10 pr-4 text-[13px] focus:border-brand-600"
                        placeholder="جستجو در عنوان یا شماره سند..." />
                </label>

                <asp:DropDownList ID="ddlFilterCategory" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="FilterChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] text-ink-700 focus:border-brand-600 lg:col-span-3">
                </asp:DropDownList>

                <label class="block lg:col-span-2">
                    <asp:TextBox ID="txtFilterFrom" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                        placeholder="از تاریخ: 1403/01/01" dir="ltr" />
                </label>

                <label class="block lg:col-span-2">
                    <asp:TextBox ID="txtFilterTo" runat="server" AutoPostBack="true"
                        OnTextChanged="FilterChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[13px] font-mono focus:border-brand-600"
                        placeholder="تا تاریخ: 1403/12/29" dir="ltr" />
                </label>

                <div class="lg:col-span-2">
                    <asp:Button ID="btnClearFilters" runat="server" Text="پاک کردن فیلترها"
                        OnClick="btnClearFilters_Click"
                        CssClass="inline-flex h-11 w-full items-center justify-center gap-2 rounded-base border border-surface-line bg-surface-card px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
                </div>

            </div>

            <div class="mb-4 flex items-center gap-2">
                <asp:CheckBox ID="chkShowDeleted" runat="server" AutoPostBack="true"
                    OnCheckedChanged="FilterChanged"
                    CssClass="h-4 w-4 rounded border-ink-300 text-brand-600" />
                <label class="text-[12.5px] text-ink-600 cursor-pointer"
                    onclick="document.getElementById('<%= chkShowDeleted.ClientID %>').click();">
                    نمایش موارد حذف‌شده
                </label>
            </div>

            <!-- Table -->
            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1100px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">عنوان</th>
                            <th class="pb-3 pr-4 text-right font-semibold">دسته</th>
                            <th class="pb-3 pr-4 text-center font-semibold">مبلغ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تاریخ</th>
                            <th class="pb-3 pr-4 text-center font-semibold">روش پرداخت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">شماره سند</th>
                            <th class="pb-3 text-left font-semibold">اقدامات</th>
                        </tr>
                    </thead>
                    <tbody class="text-[13.5px]">
                        <asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand">
                            <ItemTemplate>
                                <tr class='<%# GetRowCss(Eval("IsDeleted")) %>'>
                                    <td class="py-3 pr-4">
                                        <p class="font-semibold text-ink-900">
                                            <%# Enc(Eval("Title")) %>
                                            <%# GetDeletedBadge(Eval("IsDeleted")) %>
                                        </p>
                                        <%# GetNotesHint(Eval("Notes")) %>
                                    </td>
                                    <td class="py-3 pr-4">
                                        <span class="badge bg-brand-50 text-brand-700">
                                            <i class="h-3 w-3" data-lucide="tag"></i>
                                            <%# Enc(Eval("CategoryName")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="font-mono font-bold text-danger-600">
                                            <%# FormatMoney(Eval("Amount")) %>
                                        </span>
                                        <span class="text-[10.5px] text-ink-400"> تومان</span>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[12.5px] font-mono text-ink-600">
                                        <%# FormatShamsiDate(Eval("ExpenseDate")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetPaymentBadge(Eval("PaymentMethod")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[12px] font-mono text-ink-500" dir="ltr">
                                        <%# FormatRef(Eval("ReferenceNumber")) %>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:PlaceHolder ID="phActiveActions" runat="server"
                                                Visible='<%# CanEdit(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit"
                                                    CommandArgument='<%# Eval("ExpenseId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="SoftDelete"
                                                    CommandArgument='<%# Eval("ExpenseId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick='<%# GetDeleteConfirm(Eval("Title")) %>'>
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore"
                                                    CommandArgument='<%# Eval("ExpenseId") %>'
                                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                                    OnClientClick='<%# GetRestoreConfirm(Eval("Title")) %>'>
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
                    <i class="h-6 w-6" data-lucide="wallet"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">هزینه‌ای یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین هزینه را ثبت کنید یا فیلترها را تغییر دهید.</p>
            </asp:Panel>

            <!-- Pagination -->
            <div class="mt-5 flex flex-wrap items-center justify-between gap-3">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
                <div class="flex items-center gap-2">
                    <asp:LinkButton ID="btnPrev" runat="server" OnClick="btnPrev_Click"
                        CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                        <i class="h-3.5 w-3.5" data-lucide="chevron-right"></i>
                        قبلی
                    </asp:LinkButton>
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
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="wallet"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="ثبت هزینه جدید" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        اطلاعات هزینه را دقیق وارد کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        عنوان هزینه <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtTitle" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: خرید کارتن بسته‌بندی — آبان ۱۴۰۳" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        دسته <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlCategory" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        مبلغ (تومان) <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtAmount" runat="server" MaxLength="25" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="1000000" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        تاریخ هزینه <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtExpenseDate" runat="server" MaxLength="10" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="1403/01/15" />
                    <p class="mt-1 text-[11px] text-ink-400">فرمت: yyyy/MM/dd (شمسی)</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">روش پرداخت</span>
                    <asp:DropDownList ID="ddlPaymentMethod" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="" Text="— انتخاب —" />
                        <asp:ListItem Value="نقدی" Text="نقدی" />
                        <asp:ListItem Value="کارت بانکی" Text="کارت بانکی" />
                        <asp:ListItem Value="انتقال بانکی" Text="انتقال بانکی" />
                        <asp:ListItem Value="چک" Text="چک" />
                        <asp:ListItem Value="سایر" Text="سایر" />
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">شماره سند / فاکتور</span>
                    <asp:TextBox ID="txtReferenceNumber" runat="server" MaxLength="100" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="اختیاری" />
                </label>

                <label class="block lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                    <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="3" MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="توضیحات تکمیلی (اختیاری)" />
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