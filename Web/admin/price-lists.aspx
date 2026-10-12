<%@ Page Title="لیست‌های قیمت" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="price-lists.aspx.cs" Inherits="admin_price_lists" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditPriceListId" runat="server" Value="0" />
    <asp:HiddenField ID="hfCurrentPriceListId" runat="server" Value="0" />
    <asp:HiddenField ID="hfEditItemId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">لیست‌های قیمت</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">
                <asp:Literal ID="litPageTitle" runat="server" Text="لیست‌های قیمت" />
            </h1>
            <p class="mt-1 text-[14px] text-ink-500">
                <asp:Literal ID="litPageSubtitle" runat="server"
                    Text="لیست‌های قیمت تک‌فروشی، عمده و نمایندگی را مدیریت کنید." />
            </p>
        </div>
        <div class="flex items-center gap-2">
            <asp:PlaceHolder ID="phBackToList" runat="server" Visible="false">
                <asp:LinkButton ID="btnBackToList" runat="server" OnClick="btnBackToList_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                    <i class="h-4 w-4" data-lucide="arrow-right"></i>
                    بازگشت به لیست‌ها
                </asp:LinkButton>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddList" runat="server">
                <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن لیست قیمت"
                    OnClick="btnShowAddForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phAddItem" runat="server" Visible="false">
                <asp:Button ID="btnShowAddItemForm" runat="server" Text="افزودن محصول به لیست"
                    OnClick="btnShowAddItemForm_Click"
                    CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- ═══════════ VIEW 1: لیست لیست‌های قیمت ═══════════ -->
    <asp:Panel ID="pnlListView" runat="server">

        <!-- Stats -->
        <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <div class="flex items-center gap-3">
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                        <i class="h-5 w-5" data-lucide="tags"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل لیست‌ها</p>
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
                    <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                        <i class="h-5 w-5" data-lucide="list-checks"></i>
                    </span>
                    <div>
                        <p class="text-[12px] text-ink-500">کل اقلام قیمت</p>
                        <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                            <asp:Literal ID="litStatItems" runat="server" Text="۰" />
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

        <!-- Filters + List -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
                <h2 class="text-[15px] font-bold text-ink-900">لیست‌های قیمت</h2>
                <p class="text-[12.5px] text-ink-500">
                    <asp:Literal ID="litListInfo" runat="server" />
                </p>
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

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <asp:Repeater ID="rptPriceLists" runat="server" OnItemCommand="rptPriceLists_ItemCommand">
                    <ItemTemplate>
                        <article class='<%# GetListCardCss(Eval("IsActive"), Eval("IsDefault"), Eval("IsDeleted")) %>'>
                            <!-- Card Header -->
                            <div class="mb-3 flex items-start justify-between">
                                <div class="flex items-center gap-3">
                                    <span class='<%# GetListAvatarCss(Eval("PriceListType"), Eval("IsDeleted")) %>'>
                                        <i class="h-5 w-5" data-lucide='<%# GetListIcon(Eval("PriceListType")) %>'></i>
                                    </span>
                                    <div class="min-w-0">
                                        <p class="truncate text-[14px] font-bold text-ink-900">
                                            <%# Enc(Eval("PriceListName")) %>
                                            <%# GetDeletedBadge(Eval("IsDeleted")) %>
                                        </p>
                                        <div class="mt-1 flex items-center gap-1.5">
                                            <span class='<%# GetTypeBadgeCss(Eval("PriceListType")) %>'>
                                                <%# GetTypeLabel(Eval("PriceListType")) %>
                                            </span>
                                            <%# GetDefaultBadge(Eval("IsDefault")) %>
                                        </div>
                                    </div>
                                </div>
                                <%# GetStatusDot(Eval("IsActive"), Eval("IsDeleted")) %>
                            </div>

                            <!-- Card Body -->
                            <div class="mb-4 grid grid-cols-2 gap-3 border-t border-surface-line/50 pt-3">
                                <div>
                                    <p class="text-[11px] font-semibold text-ink-500">اقلام تعریف‌شده</p>
                                    <p class="mt-1 text-[16px] font-bold text-ink-800">
                                        <%# Eval("ItemCount") %>
                                    </p>
                                </div>
                                <div>
                                    <p class="text-[11px] font-semibold text-ink-500">محصولات متمایز</p>
                                    <p class="mt-1 text-[16px] font-bold text-ink-800">
                                        <%# Eval("ProductCount") %>
                                    </p>
                                </div>
                            </div>

                            <!-- Card Actions -->
                            <div class="flex items-center justify-between border-t border-surface-line/50 pt-3">
                                <asp:PlaceHolder ID="phActiveActions" runat="server"
                                    Visible='<%# CanEditList(Eval("IsDeleted")) %>'>
                                    <asp:LinkButton ID="btnManageItems" runat="server"
                                        CommandName="ManageItems"
                                        CommandArgument='<%# Eval("PriceListId") %>'
                                        CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-brand-600 px-3 text-[12px] font-bold text-white hover:bg-brand-700">
                                        <i class="h-3.5 w-3.5" data-lucide="list-checks"></i>
                                        مدیریت اقلام
                                    </asp:LinkButton>

                                    <div class="inline-flex items-center gap-1">
                                        <asp:LinkButton ID="btnSetDefault" runat="server"
                                            CommandName="SetDefault"
                                            CommandArgument='<%# Eval("PriceListId") %>'
                                            CssClass='<%# GetDefaultToggleCss(Eval("IsDefault")) %>'
                                            ToolTip='<%# GetDefaultToggleTooltip(Eval("IsDefault")) %>'
                                            Visible='<%# !IsTrue(Eval("IsDefault")) %>'
                                            OnClientClick="return confirm('این لیست به‌عنوان پیش‌فرض انتخاب شود؟');">
                                            <i class="h-3.5 w-3.5" data-lucide="star"></i>
                                        </asp:LinkButton>

                                        <asp:LinkButton ID="btnEdit" runat="server"
                                            CommandName="Edit"
                                            CommandArgument='<%# Eval("PriceListId") %>'
                                            CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                            ToolTip="ویرایش">
                                            <i class="h-4 w-4" data-lucide="pencil"></i>
                                        </asp:LinkButton>

                                        <asp:LinkButton ID="btnToggle" runat="server"
                                            CommandName="ToggleActive"
                                            CommandArgument='<%# Eval("PriceListId") %>'
                                            CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                            ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                            OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("PriceListName")) %>'>
                                            <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                        </asp:LinkButton>

                                        <asp:PlaceHolder ID="phDelete" runat="server"
                                            Visible='<%# CanDeleteList(Eval("ItemCount"), Eval("IsDefault")) %>'>
                                            <asp:LinkButton ID="btnDelete" runat="server"
                                                CommandName="SoftDelete"
                                                CommandArgument='<%# Eval("PriceListId") %>'
                                                CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                ToolTip="حذف"
                                                OnClientClick='<%# GetDeleteConfirm(Eval("PriceListName")) %>'>
                                                <i class="h-4 w-4" data-lucide="trash-2"></i>
                                            </asp:LinkButton>
                                        </asp:PlaceHolder>

                                        <asp:PlaceHolder ID="phBlockDelete" runat="server"
                                            Visible='<%# !CanDeleteList(Eval("ItemCount"), Eval("IsDefault")) %>'>
                                            <span class="icon-button text-ink-300 cursor-not-allowed"
                                                title='<%# GetBlockDeleteReason(Eval("ItemCount"), Eval("IsDefault")) %>'>
                                                <i class="h-4 w-4" data-lucide="lock"></i>
                                            </span>
                                        </asp:PlaceHolder>
                                    </div>
                                </asp:PlaceHolder>

                                <asp:PlaceHolder ID="phRestore" runat="server"
                                    Visible='<%# IsTrue(Eval("IsDeleted")) %>'>
                                    <asp:LinkButton ID="btnRestore" runat="server"
                                        CommandName="Restore"
                                        CommandArgument='<%# Eval("PriceListId") %>'
                                        CssClass="inline-flex h-8 items-center gap-1.5 rounded-base bg-emerald-600 px-3 text-[12px] font-bold text-white hover:bg-emerald-700"
                                        OnClientClick='<%# GetRestoreConfirm(Eval("PriceListName")) %>'>
                                        <i class="h-3.5 w-3.5" data-lucide="rotate-ccw"></i>
                                        بازیابی
                                    </asp:LinkButton>
                                </asp:PlaceHolder>
                            </div>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>

            </div>

            <asp:Panel ID="pnlListEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="tags"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">لیست قیمتی وجود ندارد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">برای شروع، اولین لیست قیمت را اضافه کنید.</p>
            </asp:Panel>

        </section>
    </asp:Panel>

    <!-- ═══════════ VIEW 2: فرم لیست قیمت ═══════════ -->
    <asp:Panel ID="pnlFormView" runat="server" Visible="false">
        <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="tags"></i>
                </span>
                <div>
                    <h2 class="text-[16px] font-semibold text-ink-900">
                        <asp:Literal ID="litFormTitle" runat="server" Text="افزودن لیست قیمت" />
                    </h2>
                    <p class="mt-0.5 text-[12px] text-ink-500">
                        نام، نوع و وضعیت لیست قیمت را مشخص کنید.
                    </p>
                </div>
            </div>

            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام لیست <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtPriceListName" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: لیست قیمت عمده پاییز ۱۴۰۳" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نوع لیست <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlPriceListType" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="1" Text="تک‌فروشی" />
                        <asp:ListItem Value="2" Text="عمده" />
                        <asp:ListItem Value="3" Text="نمایندگی" />
                    </asp:DropDownList>
                </label>

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer lg:col-span-2">
                    <asp:CheckBox ID="chkIsDefault" runat="server"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">لیست پیش‌فرض سیستم</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            اگر فعال باشد، لیست‌های پیش‌فرض دیگر به‌صورت خودکار غیرفعال می‌شوند.
                        </p>
                    </div>
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

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancelForm" runat="server" Text="انصراف" OnClick="btnCancelForm_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSavePriceList" runat="server" Text="ذخیره" OnClick="btnSavePriceList_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════ VIEW 3: مدیریت اقلام لیست ═══════════ -->
    <asp:Panel ID="pnlItemsView" runat="server" Visible="false">

        <!-- Header info -->
        <div class="mb-5 rounded-card border border-brand-100 bg-gradient-to-l from-brand-50/70 to-transparent p-5">
            <div class="flex items-center gap-3">
                <span class="grid h-12 w-12 place-items-center rounded-xl bg-brand-600 text-white shadow-md">
                    <i class="h-6 w-6" data-lucide="tags"></i>
                </span>
                <div class="min-w-0 flex-1">
                    <p class="text-[12px] font-semibold text-brand-700">در حال ویرایش اقلام لیست</p>
                    <p class="mt-0.5 text-[17px] font-extrabold text-ink-900">
                        <asp:Literal ID="litCurrentListName" runat="server" />
                    </p>
                    <div class="mt-1 flex items-center gap-2">
                        <asp:Literal ID="litCurrentListType" runat="server" />
                        <asp:Literal ID="litCurrentListDefault" runat="server" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Item Form (Inline) -->
        <asp:Panel ID="pnlItemForm" runat="server" Visible="false" CssClass="mb-5">
            <article class="rounded-card border border-brand-200 bg-brand-50/30 p-5 shadow-card">
                <div class="mb-4 flex items-center justify-between border-b border-brand-200/60 pb-3">
                    <h3 class="flex items-center gap-2 text-[14px] font-bold text-ink-800">
                        <span class="grid h-7 w-7 place-items-center rounded-base bg-brand-600 text-white">
                            <i class="h-3.5 w-3.5" data-lucide="plus-circle"></i>
                        </span>
                        <asp:Literal ID="litItemFormTitle" runat="server" Text="افزودن محصول به لیست" />
                    </h3>
                    <asp:LinkButton ID="btnCancelItemForm" runat="server" OnClick="btnCancelItemForm_Click"
                        CssClass="text-[12px] font-semibold text-ink-500 hover:text-ink-700">
                        <i class="inline h-3 w-3" data-lucide="x"></i>
                        انصراف
                    </asp:LinkButton>
                </div>

                <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

                    <label class="block lg:col-span-2">
                        <span class="text-[13px] font-semibold text-ink-700">
                            محصول <span class="text-danger-500">*</span>
                        </span>
                        <asp:DropDownList ID="ddlItemProduct" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        </asp:DropDownList>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            حداقل تعداد <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtItemMinQuantity" runat="server" MaxLength="20" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1" />
                        <p class="mt-1 text-[11px] text-ink-400">شروع اعمال این قیمت</p>
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">
                            قیمت واحد (تومان) <span class="text-danger-500">*</span>
                        </span>
                        <asp:TextBox ID="txtItemUnitPrice" runat="server" MaxLength="25" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="100000" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">درصد تخفیف</span>
                        <asp:TextBox ID="txtItemDiscount" runat="server" MaxLength="5" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="0" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تاریخ شروع اعتبار</span>
                        <asp:TextBox ID="txtItemEffectiveFrom" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="1403/01/01" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">تاریخ پایان اعتبار</span>
                        <asp:TextBox ID="txtItemEffectiveTo" runat="server" MaxLength="10" dir="ltr"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600"
                            placeholder="اختیاری" />
                    </label>

                    <label class="block">
                        <span class="text-[13px] font-semibold text-ink-700">وضعیت</span>
                        <asp:DropDownList ID="ddlItemIsActive" runat="server"
                            CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                            <asp:ListItem Value="1" Text="فعال" />
                            <asp:ListItem Value="0" Text="غیرفعال" />
                        </asp:DropDownList>
                    </label>

                </div>

                <div class="mt-4 flex items-center justify-end gap-3 border-t border-brand-200/60 pt-4">
                    <asp:Button ID="btnSaveItem" runat="server" Text="ذخیره محصول" OnClick="btnSaveItem_Click"
                        CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
                </div>
            </article>
        </asp:Panel>

        <!-- Items List -->
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجو</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearchItems" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearchItems_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو بر اساس نام یا کد محصول" />
                </label>

                <asp:DropDownList ID="ddlItemsFilterActive" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlItemsFilterActive_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه وضعیت‌ها" />
                    <asp:ListItem Value="1" Text="فعال" />
                    <asp:ListItem Value="0" Text="غیرفعال" />
                </asp:DropDownList>
            </div>

            <div class="dashboard-scrollbar overflow-x-auto">
                <table class="w-full min-w-[1100px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">محصول</th>
                            <th class="pb-3 pr-4 text-center font-semibold">حداقل تعداد</th>
                            <th class="pb-3 pr-4 text-center font-semibold">قیمت واحد</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تخفیف</th>
                            <th class="pb-3 pr-4 text-center font-semibold">اعتبار</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدام</th>
                        </tr>
                    </thead>
                    <tbody class="text-[14px]">
                        <asp:Repeater ID="rptItems" runat="server" OnItemCommand="rptItems_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="py-3 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetProductTypeAvatarCss(Eval("ProductType"), Eval("IsActive")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetProductTypeIcon(Eval("ProductType")) %>'></i>
                                            </span>
                                            <div class="min-w-0 text-right">
                                                <p class="truncate font-semibold text-ink-900 text-[13px]">
                                                    <%# Enc(Eval("ProductName")) %>
                                                </p>
                                                <p class="mt-0.5 truncate font-mono text-[11px] text-ink-400" dir="ltr">
                                                    <%# Enc(Eval("ProductCode")) %>
                                                </p>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700 font-mono">
                                            <%# FormatQuantity(Eval("MinQuantity")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class="badge bg-emerald-50 text-emerald-700 font-mono font-bold">
                                            <%# FormatMoney(Eval("UnitPrice")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <%# GetDiscountBadge(Eval("DiscountPercent")) %>
                                    </td>
                                    <td class="py-3 pr-4 text-center text-[11.5px] text-ink-600">
                                        <p>از: <%# FormatShamsiDate(Eval("EffectiveFrom")) %></p>
                                        <p class="mt-0.5 text-ink-400">تا: <%# FormatShamsiDateOrDash(Eval("EffectiveTo")) %></p>
                                    </td>
                                    <td class="py-3 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeClass(Eval("IsActive")) %>'>
                                            <%# GetStatusText(Eval("IsActive")) %>
                                        </span>
                                    </td>
                                    <td class="py-3 text-left">
                                        <div class="inline-flex items-center gap-1">
                                            <asp:LinkButton ID="btnEditItem" runat="server"
                                                CommandName="EditItem"
                                                CommandArgument='<%# Eval("PriceListItemId") %>'
                                                CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                ToolTip="ویرایش">
                                                <i class="h-4 w-4" data-lucide="pencil"></i>
                                            </asp:LinkButton>

                                            <asp:LinkButton ID="btnToggleItem" runat="server"
                                                CommandName="ToggleItem"
                                                CommandArgument='<%# Eval("PriceListItemId") %>'
                                                CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                OnClientClick='<%# GetToggleItemConfirm(Eval("IsActive"), Eval("ProductName")) %>'>
                                                <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                            </asp:LinkButton>

                                            <asp:LinkButton ID="btnDeleteItem" runat="server"
                                                CommandName="DeleteItem"
                                                CommandArgument='<%# Eval("PriceListItemId") %>'
                                                CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                ToolTip="حذف"
                                                OnClientClick='<%# GetDeleteItemConfirm(Eval("ProductName")) %>'>
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

            <asp:Panel ID="pnlItemsEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="package-search"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">هنوز محصولی به این لیست اضافه نشده.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">برای شروع، محصولی به این لیست اضافه کنید.</p>
            </asp:Panel>

            <!-- Pagination -->
            <div class="mt-5 flex flex-wrap items-center justify-between gap-3">
                <p class="text-[13px] text-ink-500">
                    <asp:Literal ID="litItemsInfo" runat="server" />
                </p>
                <div class="flex items-center gap-2">
                    <asp:LinkButton ID="btnItemsPrev" runat="server" OnClick="btnItemsPrev_Click"
                        CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                        <i class="h-3.5 w-3.5" data-lucide="chevron-right"></i>
                        قبلی
                    </asp:LinkButton>
                    <asp:LinkButton ID="btnItemsNext" runat="server" OnClick="btnItemsNext_Click"
                        CssClass="inline-flex h-9 items-center gap-1.5 rounded-base border border-surface-line px-3 text-[13px] font-semibold text-ink-700 hover:bg-surface-muted">
                        بعدی
                        <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                    </asp:LinkButton>
                </div>
            </div>
        </section>
    </asp:Panel>

</asp:Content>