<%@ Page Title="شرکا" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="partners.aspx.cs" Inherits="admin_partners" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<style>
    .tab-btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 10px 18px;
        border-bottom: 3px solid transparent;
        font-size: 13.5px;
        font-weight: 700;
        color: #64748b;
        cursor: pointer;
        background: none;
        border-top: none;
        border-left: none;
        border-right: none;
        transition: all .2s;
    }
    .tab-btn:hover { color: #0f172a; background: #f8fafc; }
    .tab-btn.active {
        color: #2563eb;
        border-bottom-color: #2563eb;
    }
</style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- Hidden fields -->
    <asp:HiddenField ID="hfEditId" runat="server" Value="0" />
    <asp:HiddenField ID="hfDetailPartnerId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditShareId" runat="server" Value="0" />
    <asp:HiddenField ID="hfActiveTab" runat="server" Value="info" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">شرکا</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="مدیریت شرکا" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="اطلاعات شرکا، درصد سهام و برداشت‌های علی‌الحساب را مدیریت کنید." />
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:PlaceHolder ID="phBackToList" runat="server" Visible="false">
                <asp:LinkButton ID="btnBackToList" runat="server" OnClick="btnBackToList_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="arrow-right"></i>
                    بازگشت به لیست شرکا
                </asp:LinkButton>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddPartner" runat="server">
                <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن شریک"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 1: LIST OF PARTNERS
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats -->
        <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="users"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">تعداد کل شرکا</p>
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
                        <p class="text-[12px] text-ink-500">شرکای فعال</p>
                        <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                            <asp:Literal ID="litStatActive" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="pie-chart"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">جمع درصد سهام فعال</p>
                        <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                            <asp:Literal ID="litStatTotalShare" runat="server" Text="۰٪" />
                        </p>
                    </div>
                </div>
            </article>

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                        <i class="h-5 w-5" data-lucide="hand-coins"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">جمع برداشت‌های باز</p>
                        <p class="mt-0.5 text-[16px] font-bold text-amber-700 font-mono">
                            <asp:Literal ID="litStatOpenDrawings" runat="server" Text="۰" />
                        </p>
                    </div>
                </div>
            </article>

        </div>

        <!-- Partners list -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
            <div class="mb-5 flex items-center justify-between">
                <h2 class="text-[15px] font-bold text-ink-900">لیست شرکا</h2>
                <p class="text-[12.5px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                <asp:Repeater ID="rptPartners" runat="server" OnItemCommand="rptPartners_ItemCommand">
                    <ItemTemplate>
                        <article class='<%# GetPartnerCardCss(Eval("IsActive")) %>'>

                            <!-- Header -->
                            <div class="mb-3 flex items-start justify-between">
                                <div class="flex items-center gap-3 min-w-0">
                                    <span class='<%# GetPartnerAvatarCss(Eval("IsActive")) %>'>
                                        <i class="h-5 w-5" data-lucide="user-round"></i>
                                    </span>
                                    <div class="min-w-0">
                                        <p class="truncate text-[14px] font-bold text-ink-900">
                                            <%# Enc(Eval("FullName")) %>
                                        </p>
                                        <p class="mt-0.5 text-[11.5px] text-ink-500">
                                            کد: #<span class="font-mono"><%# Eval("PartnerId") %></span>
                                        </p>

                                    </div>
                                </div>
                                <%# GetStatusDot(Eval("IsActive")) %>
                            </div>

                            <!-- Share badge -->
                            <div class="mb-3 flex items-center gap-2">
                                <%# GetShareBadge(Eval("CurrentSharePercent")) %>
                                <%# GetLinkedUserBadge(Eval("LinkedUsername")) %>
                            </div>

                            <!-- Contact info -->
                            <div class="mb-3 grid grid-cols-2 gap-2 border-t border-surface-line/50 pt-3 text-[11.5px]">
                                <div>
                                    <p class="text-ink-400">موبایل</p>
                                    <p class="mt-0.5 font-mono text-ink-700" dir="ltr">
                                        <%# FormatOrDash(Eval("Mobile")) %>
                                    </p>
                                </div>
                                <div>
                                    <p class="text-ink-400">کد ملی</p>
                                    <p class="mt-0.5 font-mono text-ink-700" dir="ltr">
                                        <%# FormatOrDash(Eval("NationalId")) %>
                                    </p>
                                </div>
                            </div>

                            <!-- Drawings -->
                            <div class="mb-4 rounded-base bg-surface-muted p-3">
                                <div class="flex items-center justify-between text-[11.5px]">
                                    <span class="text-ink-500">برداشت‌های باز</span>
                                    <span class="font-mono font-bold text-amber-700">
                                        <%# FormatMoney(Eval("OpenDrawings")) %>
                                        <span class="text-[10px] text-ink-400">تومان</span>
                                    </span>
                                </div>
                            </div>

                            <!-- Actions -->
                            <div class="flex items-center justify-between border-t border-surface-line/50 pt-3">
                                <asp:LinkButton ID="btnManage" runat="server"
                                    CommandName="Manage"
                                    CommandArgument='<%# Eval("PartnerId") %>'
                                    CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700">
                                    <i class="h-3.5 w-3.5" data-lucide="settings-2"></i>
                                    مدیریت
                                </asp:LinkButton>
 

                                <div class="inline-flex items-center gap-1">
                                    <asp:LinkButton ID="btnEdit" runat="server"
                                        CommandName="Edit"
                                        CommandArgument='<%# Eval("PartnerId") %>'
                                        CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                        ToolTip="ویرایش">
                                        <i class="h-4 w-4" data-lucide="pencil"></i>
                                    </asp:LinkButton>

                                    <asp:LinkButton ID="btnToggle" runat="server"
                                        CommandName="ToggleActive"
                                        CommandArgument='<%# Eval("PartnerId") %>'
                                        CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                        ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                        OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("FullName")) %>'>
                                        <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                    </asp:LinkButton>
                                                                   <a href='<%# "partner-statement.aspx?id=" + Eval("PartnerId") %>'
    class="icon-button hover:bg-violet-50 hover:text-violet-600"
    title="صورت‌حساب">
    <i class="h-4 w-4" data-lucide="scroll-text"></i>
</a>

                                    <asp:PlaceHolder ID="phDelete" runat="server"
                                        Visible='<%# CanDelete(Eval("OpenDrawings"), Eval("HasHistory")) %>'>
                                        <asp:LinkButton ID="btnDelete" runat="server"
                                            CommandName="Delete"
                                            CommandArgument='<%# Eval("PartnerId") %>'
                                            CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                            ToolTip="حذف"
                                            OnClientClick='<%# GetDeleteConfirm(Eval("FullName")) %>'>
                                            <i class="h-4 w-4" data-lucide="trash-2"></i>
                                        </asp:LinkButton>
                                    </asp:PlaceHolder>
                                </div>
                            </div>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>

            <asp:Panel ID="pnlListEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="users"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">شریکی ثبت نشده است.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">اولین شریک را اضافه کنید.</p>
            </asp:Panel>
        </section>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 2: ADD / EDIT PARTNER FORM
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="user-round"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="افزودن شریک جدید" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        اطلاعات پایه شریک و درصد سهام اولیه را وارد کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام کامل <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtFullName" runat="server" MaxLength="150"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: آقای محمد احمدی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">کد ملی</span>
                    <asp:TextBox ID="txtNationalId" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="10 رقم" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">شماره موبایل</span>
                    <asp:TextBox ID="txtMobile" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="09xxxxxxxxx" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">ایمیل</span>
                    <asp:TextBox ID="txtEmail" runat="server" MaxLength="100" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="user@example.com" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">شماره حساب / شبا</span>
                    <asp:TextBox ID="txtBankAccount" runat="server" MaxLength="50" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                        placeholder="IR..." />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">کاربر مرتبط (اختیاری)</span>
                    <asp:DropDownList ID="ddlUserId" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                    <p class="mt-1 text-[11px] text-ink-400">اگر شریک به‌عنوان کاربر وارد سیستم می‌شود.</p>
                </label>

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer lg:col-span-2">
                    <asp:CheckBox ID="chkIsActive" runat="server" Checked="true"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">شریک فعال</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            شرکای غیرفعال در محاسبه سود دوره‌های جدید لحاظ نمی‌شوند.
                        </p>
                    </div>
                </label>

                <label class="block lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                    <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600"
                        placeholder="توضیحات تکمیلی (اختیاری)" />
                </label>

            </div>

            <!-- ═══════ Initial Share (only for new) ═══════ -->
            <asp:Panel ID="pnlInitialShare" runat="server" Visible="false"
                CssClass="mt-5 rounded-base border-2 border-dashed border-brand-200 bg-brand-50/30 p-5">

                <div class="mb-4 flex items-center gap-3 border-b border-brand-200/60 pb-3">
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-4 w-4" data-lucide="pie-chart"></i>
                    </span>
                    <div>
                        <h3 class="text-[13.5px] font-bold text-ink-800">درصد سهام اولیه</h3>
                        <p class="mt-0.5 text-[11.5px] text-ink-500">
                            از این تاریخ به بعد، این شریک به این درصد از سود سهیم می‌شود.
                        </p>
                    </div>
                </div>

                <div class="grid gap-4 md:grid-cols-3">

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            درصد سهام <span class="text-danger-500">*</span>
                        </span>
                        <div class="relative mt-1.5">
                            <asp:TextBox ID="txtInitialSharePercent" runat="server" MaxLength="6" dir="ltr"
                                CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 pl-10 text-[14px] font-mono focus:border-brand-600"
                                placeholder="20" />
                            <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                        </div>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تاریخ شروع اعتبار</span>
                        <asp:TextBox ID="txtInitialShareFrom" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1403/01/01" />
                        <p class="mt-1 text-[11px] text-ink-400">خالی = از امروز</p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">یادداشت سهم</span>
                        <asp:TextBox ID="txtInitialShareNotes" runat="server" MaxLength="200"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] focus:border-brand-600"
                            placeholder="اختیاری" />
                    </label>

                </div>

                <div id="shareWarningBox" runat="server" visible="false"
                    class="mt-4 rounded-base border border-amber-300 bg-amber-50 p-3 text-[12.5px] text-amber-800">
                    <i class="inline h-4 w-4 align-middle ml-1" data-lucide="alert-triangle"></i>
                    <asp:Literal ID="litShareWarning" runat="server" />
                </div>

            </asp:Panel>

            <!-- Form actions -->
            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════════════════════════════════════════════════
         VIEW 3: PARTNER DETAIL (with Tabs)
         ═══════════════════════════════════════════════════════ -->
    <asp:Panel ID="pnlDetailView" runat="server" Visible="false">

        <!-- Partner header card -->
        <div class="mb-5 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
            <div class="flex items-center gap-4">
                <span class="grid h-14 w-14 place-items-center rounded-xl bg-brand-600 text-white shadow-md">
                    <i class="h-7 w-7" data-lucide="user-round"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-semibold text-brand-700">پرونده شریک</p>
                    <p class="mt-0.5 text-[18px] font-extrabold text-ink-900">
                        <asp:Literal ID="litDetailName" runat="server" />
                    </p>
                    <div class="mt-1 flex flex-wrap items-center gap-2">
                        <asp:Literal ID="litDetailShare" runat="server" />
                        <asp:Literal ID="litDetailStatus" runat="server" />
                        <asp:Literal ID="litDetailLinkedUser" runat="server" />
                    </div>
                </div>
                <div class="hidden text-left sm:block">
                    <p class="text-[11.5px] text-ink-500">برداشت‌های باز</p>
                    <p class="mt-1 font-mono text-[16px] font-bold text-amber-700">
                        <asp:Literal ID="litDetailOpenDrawings" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </div>

        <!-- Tabs -->
        <div class="mb-4 border-b border-surface-line bg-surface-card rounded-t-card overflow-hidden">
            <div class="flex flex-wrap">
                <button type="button" class='tab-btn <%# "" %>' data-tab="info" onclick="switchTab('info'); return false;">
                    <i class="h-4 w-4" data-lucide="id-card"></i>
                    اطلاعات شریک
                </button>
                <button type="button" class="tab-btn" data-tab="shares" onclick="switchTab('shares'); return false;">
                    <i class="h-4 w-4" data-lucide="pie-chart"></i>
                    تاریخچه سهام
                </button>
                <button type="button" class="tab-btn" data-tab="drawings" onclick="switchTab('drawings'); return false;">
                    <i class="h-4 w-4" data-lucide="hand-coins"></i>
                    برداشت‌ها
                </button>
            </div>
        </div>

        <!-- ═══════ Tab 1: Info ═══════ -->
        <asp:Panel ID="pnlTabInfo" runat="server" CssClass="tab-panel">
            <div class="grid gap-5 lg:grid-cols-3">

                <section class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card lg:col-span-2">
                    <h3 class="mb-4 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                        اطلاعات پایه
                    </h3>

                    <dl class="grid gap-4 sm:grid-cols-2">
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">نام کامل</dt>
                            <dd class="mt-1 text-[13.5px] font-bold text-ink-900">
                                <asp:Literal ID="litInfoFullName" runat="server" />
                            </dd>
                        </div>
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">کد ملی</dt>
                            <dd class="mt-1 font-mono text-[13.5px] text-ink-800" dir="ltr">
                                <asp:Literal ID="litInfoNationalId" runat="server" />
                            </dd>
                        </div>
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">موبایل</dt>
                            <dd class="mt-1 font-mono text-[13.5px] text-ink-800" dir="ltr">
                                <asp:Literal ID="litInfoMobile" runat="server" />
                            </dd>
                        </div>
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">ایمیل</dt>
                            <dd class="mt-1 font-mono text-[12.5px] text-ink-800" dir="ltr">
                                <asp:Literal ID="litInfoEmail" runat="server" />
                            </dd>
                        </div>
                        <div class="sm:col-span-2">
                            <dt class="text-[11.5px] font-semibold text-ink-500">شماره حساب / شبا</dt>
                            <dd class="mt-1 font-mono text-[13px] text-ink-800" dir="ltr">
                                <asp:Literal ID="litInfoBank" runat="server" />
                            </dd>
                        </div>
                        <div class="sm:col-span-2">
                            <dt class="text-[11.5px] font-semibold text-ink-500">یادداشت</dt>
                            <dd class="mt-1 text-[13px] text-ink-700 leading-relaxed">
                                <asp:Literal ID="litInfoNotes" runat="server" />
                            </dd>
                        </div>
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">تاریخ عضویت</dt>
                            <dd class="mt-1 text-[13px] text-ink-700">
                                <asp:Literal ID="litInfoCreatedAt" runat="server" />
                            </dd>
                        </div>
                        <div>
                            <dt class="text-[11.5px] font-semibold text-ink-500">آخرین ویرایش</dt>
                            <dd class="mt-1 text-[13px] text-ink-700">
                                <asp:Literal ID="litInfoUpdatedAt" runat="server" />
                            </dd>
                        </div>
                    </dl>

                    <div class="mt-5 flex justify-end border-t border-surface-line pt-4">
                        <asp:Button ID="btnEditFromDetail" runat="server" Text="ویرایش اطلاعات"
                            OnClick="btnEditFromDetail_Click"
                            CssClass="inline-flex h-9 items-center gap-2 rounded-base border border-surface-line px-4 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted" />
                    </div>
                </section>

                <!-- Summary card -->
                <section class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                    <h3 class="mb-4 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                        خلاصه مالی
                    </h3>

                    <div class="space-y-3">
                        <div class="flex items-center justify-between rounded-base bg-surface-muted px-3 py-2.5">
                            <span class="text-[12.5px] text-ink-600">درصد سهام فعلی</span>
                            <span class="font-mono text-[14px] font-bold text-brand-700">
                                <asp:Literal ID="litSumSharePercent" runat="server" Text="—" />
                            </span>
                        </div>
                        <div class="flex items-center justify-between rounded-base bg-surface-muted px-3 py-2.5">
                            <span class="text-[12.5px] text-ink-600">برداشت‌های باز</span>
                            <span class="font-mono text-[13.5px] font-bold text-amber-700">
                                <asp:Literal ID="litSumOpenDrawings" runat="server" Text="۰" />
                            </span>
                        </div>
                        <div class="flex items-center justify-between rounded-base bg-emerald-50 px-3 py-2.5">
                            <span class="text-[12.5px] text-emerald-700">جمع دریافتی (تسویه‌شده)</span>
                            <span class="font-mono text-[13.5px] font-bold text-emerald-700">
                                <asp:Literal ID="litSumPaidTotal" runat="server" Text="۰" />
                            </span>
                        </div>
                    </div>

                    <div class="mt-5 rounded-base border border-surface-line bg-surface-body p-3 text-[11.5px] leading-relaxed text-ink-500">
                        <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="info"></i>
                        محاسبه سهم برآوردی و برداشت جدید، در تب <strong>«برداشت‌ها»</strong> قابل انجام است.
                    </div>
                </section>

            </div>
        </asp:Panel>

        <!-- ═══════ Tab 2: Share History ═══════ -->
        <asp:Panel ID="pnlTabShares" runat="server" CssClass="tab-panel">

            <!-- Add new share -->
            <section class="mb-4 rounded-card border-2 border-dashed border-brand-200 bg-brand-50/30 p-5">
                <div class="mb-4 flex items-center gap-3">
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-4 w-4" data-lucide="plus-circle"></i>
                    </span>
                    <div>
                        <h3 class="text-[13.5px] font-bold text-ink-800">افزودن / تغییر درصد سهام</h3>
                        <p class="mt-0.5 text-[11.5px] text-ink-500">
                            درصد فعلی به‌طور خودکار بسته می‌شود و درصد جدید از تاریخ انتخابی اعمال می‌شود.
                        </p>
                    </div>
                </div>

                <div class="grid gap-4 md:grid-cols-4">

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            درصد جدید <span class="text-danger-500">*</span>
                        </span>
                        <div class="relative mt-1.5">
                            <asp:TextBox ID="txtNewSharePercent" runat="server" MaxLength="6" dir="ltr"
                                CssClass="h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 pl-10 text-[14px] font-mono focus:border-brand-600"
                                placeholder="25" />
                            <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-ink-400">٪</span>
                        </div>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            تاریخ اعمال <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtNewShareFrom" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1403/04/01" />
                    </label>

                    <label class="block md:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">یادداشت</span>
                        <asp:TextBox ID="txtNewShareNotes" runat="server" MaxLength="200"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] focus:border-brand-600"
                            placeholder="مثال: افزایش سرمایه — تیر ۱۴۰۳" />
                    </label>

                </div>

                <div class="mt-4 flex items-center justify-end gap-3">
                    <asp:Button ID="btnAddShare" runat="server" Text="ثبت درصد جدید"
                        OnClick="btnAddShare_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
                </div>
            </section>

            <!-- History table -->
            <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
                <h3 class="mb-4 border-b border-surface-line pb-3 text-[14px] font-bold text-ink-900">
                    تاریخچه سهام
                </h3>

                <div class="dashboard-scrollbar overflow-x-auto">
                    <table class="w-full min-w-[700px] text-right">
                        <thead>
                            <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                                <th class="pb-3 pr-4 text-right font-semibold">درصد</th>
                                <th class="pb-3 pr-4 text-center font-semibold">از تاریخ</th>
                                <th class="pb-3 pr-4 text-center font-semibold">تا تاریخ</th>
                                <th class="pb-3 pr-4 text-right font-semibold">یادداشت</th>
                                <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            </tr>
                        </thead>
                        <tbody class="text-[13.5px]">
                            <asp:Repeater ID="rptShares" runat="server">
                                <ItemTemplate>
                                    <tr class='<%# GetShareRowCss(Eval("EffectiveTo")) %>'>
                                        <td class="py-3 pr-4">
                                            <span class="badge bg-brand-50 text-brand-700 font-mono font-bold">
                                                <%# FormatPercent(Eval("SharePercent")) %>
                                            </span>
                                        </td>
                                        <td class="py-3 pr-4 text-center font-mono text-ink-700">
                                            <%# FormatShamsiDate(Eval("EffectiveFrom")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-center font-mono text-ink-700">
                                            <%# FormatShamsiDateOrOpen(Eval("EffectiveTo")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-[12.5px] text-ink-600">
                                            <%# FormatOrDash(Eval("Notes")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-center">
                                            <%# GetShareStatusBadge(Eval("EffectiveTo")) %>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>

                <asp:Panel ID="pnlSharesEmpty" runat="server" Visible="false" CysClass="py-10 text-center">
                    <p class="text-[13px] text-ink-400 text-center py-8">هنوز درصد سهامی ثبت نشده است.</p>
                </asp:Panel>
            </section>
        </asp:Panel>

        <!-- ═══════ Tab 3: Drawings ═══════ -->
        <asp:Panel ID="pnlTabDrawings" runat="server" CssClass="tab-panel">

            <!-- Estimate card -->
            <section class="mb-4 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
                <h3 class="mb-4 flex items-center gap-2 text-[14px] font-bold text-ink-900">
                    <span class="grid h-6 w-6 place-items-center rounded-base bg-brand-600 text-white">
                        <i class="h-3.5 w-3.5" data-lucide="calculator"></i>
                    </span>
                    سهم برآوردی از سود جاری
                </h3>

                <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

                    <div class="rounded-base bg-surface-card p-4 border border-surface-line">
                        <p class="text-[11.5px] text-ink-500">سود خالص دوره (تا امروز)</p>
                        <p class="mt-1 font-mono text-[15px] font-bold text-ink-900">
                            <asp:Literal ID="litEstPeriodProfit" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">
                            از <asp:Literal ID="litEstFromDate" runat="server" Text="—" />
                        </p>
                    </div>

                    <div class="rounded-base bg-surface-card p-4 border border-surface-line">
                        <p class="text-[11.5px] text-ink-500">سهم برآوردی شریک</p>
                        <p class="mt-1 font-mono text-[15px] font-bold text-brand-700">
                            <asp:Literal ID="litEstShare" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-ink-400">
                            با <asp:Literal ID="litEstSharePercent" runat="server" Text="—" />٪
                        </p>
                    </div>

                    <div class="rounded-base bg-surface-card p-4 border border-surface-line">
                        <p class="text-[11.5px] text-ink-500">برداشت‌های قبلی</p>
                        <p class="mt-1 font-mono text-[15px] font-bold text-amber-700">
                            <asp:Literal ID="litEstDrawings" runat="server" Text="۰" />
                        </p>
                    </div>

                    <div class="rounded-base bg-emerald-50 border border-emerald-200 p-4">
                        <p class="text-[11.5px] text-emerald-700">قابل برداشت</p>
                        <p class="mt-1 font-mono text-[15px] font-bold text-emerald-700">
                            <asp:Literal ID="litEstAvailable" runat="server" Text="۰" />
                        </p>
                        <p class="mt-0.5 text-[10.5px] text-emerald-600">
                            با سقف امنیتی <asp:Literal ID="litEstMaxPct" runat="server" Text="80" />٪
                        </p>
                    </div>

                </div>

                <p class="mt-4 text-[11.5px] text-ink-500 leading-relaxed">
                    <i class="inline h-3.5 w-3.5 align-middle ml-1" data-lucide="info"></i>
                    این مبالغ <strong>برآوردی</strong> هستند و بر اساس P&L جاری محاسبه می‌شوند. در زمان بستن دوره، سهم قطعی مشخص و برداشت‌ها به‌صورت خودکار کسر می‌شوند.
                </p>
            </section>

            <!-- Add drawing form -->
            <section class="mb-4 rounded-card border-2 border-dashed border-amber-200 bg-amber-50/30 p-5">
                <div class="mb-4 flex items-center gap-3">
                    <span class="grid h-8 w-8 place-items-center rounded-base bg-amber-500 text-white">
                        <i class="h-4 w-4" data-lucide="plus-circle"></i>
                    </span>
                    <div>
                        <h3 class="text-[13.5px] font-bold text-ink-800">ثبت برداشت جدید</h3>
                        <p class="mt-0.5 text-[11.5px] text-ink-500">
                            ثبت برداشت علی‌الحساب شریک از سهم سود جاری.
                        </p>
                    </div>
                </div>

                <div class="grid gap-4 md:grid-cols-3 lg:grid-cols-5">

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            مبلغ (تومان) <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtDrawingAmount" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="50000000" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            تاریخ <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtDrawingDate" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1403/02/15" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">روش پرداخت</span>
                        <asp:DropDownList ID="ddlDrawingPaymentMethod" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="" Text="— انتخاب —" />
                            <asp:ListItem Value="نقدی" Text="نقدی" />
                            <asp:ListItem Value="کارت بانکی" Text="کارت بانکی" />
                            <asp:ListItem Value="انتقال بانکی" Text="انتقال بانکی" />
                            <asp:ListItem Value="چک" Text="چک" />
                        </asp:DropDownList>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">شماره سند</span>
                        <asp:TextBox ID="txtDrawingRef" runat="server" MaxLength="100" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="اختیاری" />
                    </label>

                    <div class="flex items-end">
                        <asp:Button ID="btnAddDrawing" runat="server" Text="ثبت برداشت"
                            OnClick="btnAddDrawing_Click"
                            CssClass="h-10 w-full rounded-base bg-amber-500 px-4 text-[13.5px] font-semibold text-white hover:bg-amber-600" />
                    </div>

                    <label class="block md:col-span-3 lg:col-span-5">
                        <span class="text-[13px] font-semibold text-ink-700">دلیل برداشت</span>
                        <asp:TextBox ID="txtDrawingReason" runat="server" MaxLength="300"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] focus:border-brand-600"
                            placeholder="مثال: نیاز شخصی فوری — تسویه در دوره بعد" />
                    </label>

                </div>
            </section>

            <!-- Drawings table -->
            <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">
                <div class="mb-4 flex items-center justify-between">
                    <h3 class="text-[14px] font-bold text-ink-900">سابقه برداشت‌ها</h3>
                    <span class="badge bg-surface-muted text-ink-600">
                        جمع باز:
                        <asp:Literal ID="litDrawingsOpenTotal" runat="server" Text="۰" />
                        تومان
                    </span>
                </div>

                <div class="dashboard-scrollbar overflow-x-auto">
                    <table class="w-full min-w-[800px] text-right">
                        <thead>
                            <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                                <th class="pb-3 pr-4 text-center font-semibold">#</th>
                                <th class="pb-3 pr-4 text-center font-semibold">مبلغ</th>
                                <th class="pb-3 pr-4 text-center font-semibold">تاریخ</th>
                                <th class="pb-3 pr-4 text-center font-semibold">روش پرداخت</th>
                                <th class="pb-3 pr-4 text-right font-semibold">دلیل</th>
                                <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                                <th class="pb-3 text-left font-semibold">اقدامات</th>
                            </tr>
                        </thead>
                        <tbody class="text-[13.5px]">
                            <asp:Repeater ID="rptDrawings" runat="server" OnItemCommand="rptDrawings_ItemCommand">
                                <ItemTemplate>
                                    <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                        <td class="py-3 pr-4 text-center">
                                            <span class="grid h-6 w-6 place-items-center rounded-full bg-ink-800 text-white text-[11px] font-bold mx-auto">
                                                <%# Eval("DrawingId") %>
                                            </span>
                                        </td>
                                        <td class="py-3 pr-4 text-center font-mono font-bold text-amber-700">
                                            <%# FormatMoney(Eval("Amount")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-center font-mono text-ink-600">
                                            <%# FormatShamsiDate(Eval("DrawingDate")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-center">
                                            <%# GetPaymentBadge(Eval("PaymentMethod")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-[12.5px] text-ink-600">
                                            <%# FormatOrDash(Eval("Reason")) %>
                                        </td>
                                        <td class="py-3 pr-4 text-center">
                                            <%# GetDrawingStatusBadge(Eval("Status")) %>
                                        </td>
                                        <td class="py-3 text-left">
                                            <div class="inline-flex items-center gap-1">
                                                <asp:PlaceHolder ID="phCancel" runat="server"
                                                    Visible='<%# CanCancelDrawing(Eval("Status")) %>'>
                                                    <asp:LinkButton ID="btnCancelDrawing" runat="server"
                                                        CommandName="CancelDrawing"
                                                        CommandArgument='<%# Eval("DrawingId") %>'
                                                        CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                        ToolTip="لغو برداشت"
                                                        OnClientClick='<%# GetCancelDrawingConfirm() %>'>
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

                <asp:Panel ID="pnlDrawingsEmpty" runat="server" Visible="false" CysClass="py-10 text-center">
                    <p class="text-[13px] text-ink-400 text-center py-8">هنوز برداشتی ثبت نشده است.</p>
                </asp:Panel>
            </section>
        </asp:Panel>

    </asp:Panel>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    function switchTab(tabName) {
        // Store active tab in hidden field
        var hf = document.getElementById('<%= hfActiveTab.ClientID %>');
        if (hf) hf.value = tabName;

        // Toggle panels
        var panels = ['info', 'shares', 'drawings'];
        for (var i = 0; i < panels.length; i++) {
            var panel = document.getElementById('<%= pnlTabInfo.ClientID %>'.replace('pnlTabInfo', 'pnlTab' + capitalize(panels[i])));
            if (!panel) {
                // Fallback: use class
                var all = document.querySelectorAll('.tab-panel');
                for (var j = 0; j < all.length; j++) {
                    if (all[j].id && all[j].id.indexOf(capitalize(panels[i])) !== -1) {
                        panel = all[j];
                        break;
                    }
                }
            }
            if (panel) {
                if (panels[i] === tabName) panel.classList.remove('hidden');
                else panel.classList.add('hidden');
            }
        }

        // Update buttons
        var buttons = document.querySelectorAll('.tab-btn');
        for (var k = 0; k < buttons.length; k++) {
            var b = buttons[k];
            if (b.getAttribute('data-tab') === tabName) b.classList.add('active');
            else b.classList.remove('active');
        }

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function capitalize(s) {
        return s.charAt(0).toUpperCase() + s.slice(1);
    }

    // On page load, set active tab
    document.addEventListener('DOMContentLoaded', function () {
        var hf = document.getElementById('<%= hfActiveTab.ClientID %>');
        var initial = (hf && hf.value) ? hf.value : 'info';
        // Only run if detail view exists
        var detailView = document.getElementById('<%= pnlDetailView.ClientID %>');
        if (detailView) switchTab(initial);
    });
</script>
</asp:Content>