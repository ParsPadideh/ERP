<%@ Page Title="مدیریت کاربران | سامانه ERP" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="users.aspx.cs" Inherits="admin_users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditUserId" runat="server" Value="0" />
    <asp:HiddenField ID="hfOriginalHash" runat="server" Value="" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">کاربران</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">مدیریت کاربران</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                کاربران سامانه را ثبت، ویرایش، فعال/غیرفعال و به نقش‌ها تخصیص دهید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="roles.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="shield"></i>
                مدیریت نقش‌ها
            </a>
            <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن کاربر جدید"
                OnClick="btnShowAddForm_Click"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
        </div>
    </div>

    <!-- ═══════════ کارت‌های آماری ═══════════ -->
    <asp:Panel ID="pnlStats" runat="server" CssClass="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="users"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل کاربران</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatTotal" runat="server" Text="0" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-success-50 text-success-600">
                    <i class="h-5 w-5" data-lucide="user-check"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">فعال</p>
                    <p class="mt-0.5 text-[20px] font-bold text-success-600">
                        <asp:Literal ID="litStatActive" runat="server" Text="0" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-warning-50 text-warning-600">
                    <i class="h-5 w-5" data-lucide="user-x"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">غیرفعال</p>
                    <p class="mt-0.5 text-[20px] font-bold text-warning-600">
                        <asp:Literal ID="litStatInactive" runat="server" Text="0" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-danger-50 text-danger-500">
                    <i class="h-5 w-5" data-lucide="user-minus"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">حذف‌شده</p>
                    <p class="mt-0.5 text-[20px] font-bold text-danger-500">
                        <asp:Literal ID="litStatDeleted" runat="server" Text="0" />
                    </p>
                </div>
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════ فرم افزودن / ویرایش ═══════════ -->
    <asp:Panel ID="pnlForm" runat="server" Visible="false" CssClass="mb-5">
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center justify-between border-b border-surface-line pb-4">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="user-plus"></i>
                    </span>
                    <div>
                        <h2 class="text-[16px] font-semibold text-ink-900">
                            <asp:Literal ID="litFormTitle" runat="server" Text="افزودن کاربر جدید" />
                        </h2>
                        <p class="mt-0.5 text-[12px] text-ink-500">
                            اطلاعات حساب کاربری و نقش دسترسی را تعیین کنید.
                        </p>
                    </div>
                </div>
                <asp:LinkButton ID="btnCancelTop" runat="server" OnClick="btnCancel_Click"
                    CssClass="text-[13px] font-semibold text-ink-500 hover:text-ink-700">
                    <i class="inline h-3.5 w-3.5" data-lucide="x"></i>
                    انصراف
                </asp:LinkButton>
            </div>

            <!-- ── بخش ۱: اطلاعات حساب ── -->
            <h3 class="mb-3 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="user-circle-2"></i>
                اطلاعات حساب کاربری
            </h3>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام کاربری <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="admin" />
                    <p class="mt-1 text-[11px] text-ink-400">حروف انگلیسی، عدد، نقطه، خط تیره و آندرلاین</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام و نام خانوادگی <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtFullName" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: علی رضایی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نقش <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlRole" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        رمز عبور
                        <asp:Literal ID="litPwdRequired" runat="server" Text="<span class='text-danger-500'>*</span>" />
                    </span>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="200" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="حداقل ۶ کاراکتر" />
                    <p class="mt-1 text-[11px] text-ink-400">
                        <asp:Literal ID="litPwdHint" runat="server" Text="حداقل ۶ کاراکتر وارد کنید." />
                    </p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">تکرار رمز عبور</span>
                    <asp:TextBox ID="txtPassword2" runat="server" TextMode="Password" MaxLength="200" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="تکرار رمز عبور" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">وضعیت</span>
                    <asp:DropDownList ID="ddlIsActive" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="1" Text="فعال" />
                        <asp:ListItem Value="0" Text="غیرفعال" />
                    </asp:DropDownList>
                </label>

            </div>

            <!-- ── بخش ۲: اطلاعات تماس ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="contact"></i>
                اطلاعات تماس
            </h3>
            <div class="grid gap-4 md:grid-cols-2">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">موبایل</span>
                    <asp:TextBox ID="txtMobile" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="09123456789" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">ایمیل</span>
                    <asp:TextBox ID="txtEmail" runat="server" MaxLength="100" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="user@example.com" />
                </label>

            </div>

            <!-- ── Action buttons ── -->
            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancel" runat="server" Text="انصراف" OnClick="btnCancel_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════ لیست کاربران ═══════════ -->
    <asp:Panel ID="pnlList" runat="server">
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجوی کاربر</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو بر اساس نام، نام کاربری، موبایل یا ایمیل" />
                </label>

                <asp:DropDownList ID="ddlFilterRole" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterRole_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterActive" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterActive_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="فعال" />
                    <asp:ListItem Value="0" Text="غیرفعال" />
                </asp:DropDownList>

                <asp:DropDownList ID="ddlFilterDeleted" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterDeleted_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="0" Text="حذف‌نشده‌ها" />
                    <asp:ListItem Value="1" Text="فقط حذف‌شده‌ها" />
                    <asp:ListItem Value="" Text="همه" />
                </asp:DropDownList>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1100px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">کاربر</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تماس</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نقش</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">آخرین ورود</th>
                            <th class="pb-3 text-left font-semibold">اقدام</th>
                        </tr>
                    </thead>
                    <tbody class="text-[14px]">
                        <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="py-4 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetAvatarCss(Eval("IsActive"), Eval("IsDeleted")) %>'>
                                                <%# GetInitial(Eval("FullName"), Eval("Username")) %>
                                            </span>
                                            <div class="min-w-0 text-right">
                                                <p class="truncate font-semibold text-ink-900">
                                                    <%# Enc(Eval("FullName")) %>
                                                    <%# GetCurrentUserBadge(Eval("UserId")) %>
                                                </p>
                                                <p class="mt-0.5 truncate font-mono text-[12px] text-ink-400" dir="ltr">
                                                    @<%# Enc(Eval("Username")) %>
                                                </p>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <p class="font-mono text-[13px] text-ink-700" dir="ltr">
                                            <%# EncOrDash(Eval("Mobile")) %>
                                        </p>
                                        <p class="mt-0.5 truncate text-[11px] text-ink-400" dir="ltr">
                                            <%# EncOrDash(Eval("Email")) %>
                                        </p>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class="badge bg-brand-50 text-brand-700">
                                            <i class="h-3 w-3" data-lucide="shield"></i>
                                            <%# Enc(Eval("RoleName")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeClass(Eval("IsActive"), Eval("IsDeleted")) %>'>
                                            <%# GetStatusText(Eval("IsActive"), Eval("IsDeleted")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center text-[12.5px] text-ink-600">
                                        <%# FormatShamsiDateTime(Eval("LastLoginAt")) %>
                                    </td>
                                    <td class="py-4 text-left">
                                        <div class="inline-flex items-center gap-1">

                                            <%-- ── ویرایش و ریست رمز (فقط غیرحذف‌شده‌ها) ── --%>
                                            <asp:PlaceHolder ID="phEditActions" runat="server"
                                                Visible='<%# !IsUserDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit" CommandArgument='<%# Eval("UserId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnResetPwd" runat="server"
                                                    CommandName="ResetPassword" CommandArgument='<%# Eval("UserId") %>'
                                                    CssClass="icon-button hover:bg-warning-50 hover:text-warning-600"
                                                    ToolTip="ریست رمز عبور"
                                                    OnClientClick="return confirm('رمز عبور این کاربر به مقدار پیش‌فرض Admin@123 ریست شود؟');">
                                                    <i class="h-4 w-4" data-lucide="key-round"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <%-- ── فعال/غیرفعال و حذف (اگر قابل تغییر باشد) ── --%>
                                            <asp:PlaceHolder ID="phCanModify" runat="server"
                                                Visible='<%# CanModifyUser(Eval("UserId"), Eval("Username"), Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnToggle" runat="server"
                                                    CommandName="ToggleActive" CommandArgument='<%# Eval("UserId") %>'
                                                    CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                    ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                    OnClientClick='<%# GetToggleConfirm(Eval("IsActive")) %>'>
                                                    <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="SoftDelete" CommandArgument='<%# Eval("UserId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick="return confirm('آیا از حذف این کاربر مطمئن هستید؟');">
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <%-- ── بازگردانی (فقط حذف‌شده‌ها) ── --%>
                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsUserDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore" CommandArgument='<%# Eval("UserId") %>'
                                                    CssClass="icon-button hover:bg-success-50 hover:text-success-600"
                                                    ToolTip="بازگردانی"
                                                    OnClientClick="return confirm('این کاربر بازگردانی شود؟');">
                                                    <i class="h-4 w-4" data-lucide="rotate-ccw"></i>
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

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false"
                CssClass="py-12 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="user-x"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">کاربری مطابق با این فیلتر یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">فیلترها را تغییر دهید یا کاربر جدید اضافه کنید.</p>
            </asp:Panel>

            <div class="mt-5 flex flex-wrap items-center justify-between gap-3">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litInfo" runat="server" />
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

</asp:Content>