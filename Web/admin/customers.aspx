<%@ Page Title="مشتریان" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="customers.aspx.cs" Inherits="admin_customers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditCustomerId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">مشتریان</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">مدیریت مشتریان</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                مشتریان تک‌فروشی، عمده و نمایندگی را ثبت، ویرایش و مدیریت کنید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="price-lists.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="tags"></i>
                لیست‌های قیمت
            </a>
            <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن مشتری جدید"
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
                    <p class="text-[12px] text-ink-500">کل مشتریان</p>
                    <p class="mt-0.5 text-[20px] font-bold text-ink-900">
                        <asp:Literal ID="litStatTotal" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-5 w-5" data-lucide="store"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">تک‌فروشی</p>
                    <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                        <asp:Literal ID="litStatRetail" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-amber-50 text-amber-600">
                    <i class="h-5 w-5" data-lucide="building-2"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">عمده</p>
                    <p class="mt-0.5 text-[20px] font-bold text-amber-600">
                        <asp:Literal ID="litStatWholesale" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-violet-50 text-violet-600">
                    <i class="h-5 w-5" data-lucide="briefcase"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">نمایندگی</p>
                    <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                        <asp:Literal ID="litStatAgent" runat="server" Text="۰" />
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
                            <asp:Literal ID="litFormTitle" runat="server" Text="افزودن مشتری جدید" />
                        </h2>
                        <p class="mt-0.5 text-[12px] text-ink-500">
                            اطلاعات پایه و اطلاعات تجاری مشتری را وارد کنید.
                        </p>
                    </div>
                </div>
                <asp:LinkButton ID="btnCancelTop" runat="server" OnClick="btnCancel_Click"
                    CssClass="text-[13px] font-semibold text-ink-500 hover:text-ink-700">
                    <i class="inline h-3.5 w-3.5" data-lucide="x"></i>
                    انصراف
                </asp:LinkButton>
            </div>

            <!-- ── بخش ۱: نوع مشتری ── -->
            <div class="mb-6">
                <h3 class="mb-3 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                    <i class="h-4 w-4 text-brand-500" data-lucide="layers"></i>
                    نوع مشتری <span class="text-danger-500">*</span>
                </h3>

                <div class="grid gap-3 sm:grid-cols-3">
                    <label class="customer-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="1">
                        <asp:RadioButton ID="rbTypeRetail" runat="server" GroupName="CustomerType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                            <i class="h-5 w-5" data-lucide="store"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">تک‌فروشی</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">مشتری خرد، خرید با قیمت مصرف‌کننده</p>
                        </div>
                        <span class="check-mark hidden text-brand-600">
                            <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                        </span>
                    </label>

                    <label class="customer-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="2">
                        <asp:RadioButton ID="rbTypeWholesale" runat="server" GroupName="CustomerType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-amber-50 text-amber-600">
                            <i class="h-5 w-5" data-lucide="building-2"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">عمده</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">فروشگاه، سوپرمارکت — قیمت پله‌ای</p>
                        </div>
                        <span class="check-mark hidden text-brand-600">
                            <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                        </span>
                    </label>

                    <label class="customer-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="3">
                        <asp:RadioButton ID="rbTypeAgent" runat="server" GroupName="CustomerType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-violet-50 text-violet-600">
                            <i class="h-5 w-5" data-lucide="briefcase"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">نمایندگی</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">پخش استانی یا منطقه‌ای — بهترین قیمت</p>
                        </div>
                        <span class="check-mark hidden text-brand-600">
                            <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                        </span>
                    </label>
                </div>
            </div>

            <!-- ── بخش ۲: اطلاعات پایه ── -->
            <h3 class="mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="info"></i>
                اطلاعات پایه
            </h3>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        کد مشتری <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtCustomerCode" runat="server" MaxLength="50" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="C-1001" />
                    <p class="mt-1 text-[11px] text-ink-400">یکتا — حروف انگلیسی، عدد، `-` `_`</p>
                </label>

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام مشتری / فروشگاه <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtCustomerName" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: سوپرمارکت رضایی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">نام شخص رابط</span>
                    <asp:TextBox ID="txtContactPerson" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: علی رضایی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">موبایل</span>
                    <asp:TextBox ID="txtMobile" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="09123456789" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">تلفن ثابت</span>
                    <asp:TextBox ID="txtPhone" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="02188888888" />
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

            <!-- ── بخش ۳: آدرس ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="map-pin"></i>
                آدرس و موقعیت
            </h3>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">شهر</span>
                    <asp:TextBox ID="txtCity" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: تهران" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">منطقه / محله</span>
                    <asp:TextBox ID="txtRegion" runat="server" MaxLength="100"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: سعادت‌آباد" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">کد پستی</span>
                    <asp:TextBox ID="txtPostalCode" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="1234567890" />
                </label>

                <label class="block lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">آدرس</span>
                    <asp:TextBox ID="txtAddress" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="آدرس کامل پستی" />
                </label>

            </div>

            <!-- ── بخش ۴: اطلاعات مالی و تجاری ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="wallet"></i>
                اطلاعات مالی و تجاری
            </h3>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">کد ملی / شناسه ملی</span>
                    <asp:TextBox ID="txtNationalId" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="1234567890" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">کد اقتصادی</span>
                    <asp:TextBox ID="txtEconomicCode" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="برای فاکتور رسمی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">سقف اعتبار (تومان)</span>
                    <asp:TextBox ID="txtCreditLimit" runat="server" MaxLength="25" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="0" />
                    <p class="mt-1 text-[11px] text-ink-400">برای فروش اعتباری</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">لیست قیمت پیش‌فرض</span>
                    <asp:DropDownList ID="ddlPriceList" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                </label>

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">بازاریاب مسئول</span>
                    <asp:DropDownList ID="ddlMarketer" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                    <p class="mt-1 text-[11px] text-ink-400">
                        کاربرانی که در سیستم نقش «بازاریاب» دارند
                    </p>
                </label>

            </div>

            <!-- ── بخش ۵: یادداشت ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="sticky-note"></i>
                یادداشت
            </h3>

            <label class="block">
                <asp:TextBox ID="txtNotes" runat="server" MaxLength="1000" TextMode="MultiLine" Rows="2"
                    CssClass="w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                    placeholder="یادداشت‌های داخلی (اختیاری)" />
            </label>

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancel" runat="server" Text="انصراف" OnClick="btnCancel_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════ لیست مشتریان ═══════════ -->
    <asp:Panel ID="pnlList" runat="server">
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجوی مشتری</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو بر اساس نام، کد، موبایل یا شهر" />
                </label>

                <asp:DropDownList ID="ddlFilterType" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterType_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه انواع" />
                    <asp:ListItem Value="1" Text="تک‌فروشی" />
                    <asp:ListItem Value="2" Text="عمده" />
                    <asp:ListItem Value="3" Text="نمایندگی" />
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
                <table class="w-full min-w-[1200px] text-right">
                    <thead>
                        <tr class="border-b border-surface-line text-[12px] uppercase text-ink-400">
                            <th class="pb-3 pr-4 text-right font-semibold">مشتری</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                            <th class="pb-3 pr-4 text-center font-semibold">تماس</th>
                            <th class="pb-3 pr-4 text-center font-semibold">موقعیت</th>
                            <th class="pb-3 pr-4 text-center font-semibold">سقف اعتبار</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدام</th>
                        </tr>
                    </thead>
                    <tbody class="text-[14px]">
                        <asp:Repeater ID="rptCustomers" runat="server" OnItemCommand="rptCustomers_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="py-4 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetCustomerAvatarCss(Eval("CustomerType"), Eval("IsActive"), Eval("IsDeleted")) %>'>
                                                <i class="h-5 w-5" data-lucide='<%# GetCustomerIcon(Eval("CustomerType")) %>'></i>
                                            </span>
                                            <div class="min-w-0 text-right">
                                                <p class="truncate font-semibold text-ink-900">
                                                    <%# Enc(Eval("CustomerName")) %>
                                                </p>
                                                <p class="mt-0.5 truncate font-mono text-[11.5px] text-ink-400" dir="ltr">
                                                    <%# Enc(Eval("CustomerCode")) %>
                                                </p>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetTypeBadgeCss(Eval("CustomerType")) %>'>
                                            <%# GetTypeLabel(Eval("CustomerType")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <p class="font-mono text-[12.5px] text-ink-700" dir="ltr">
                                            <%# EncOrDash(Eval("Mobile")) %>
                                        </p>
                                        <p class="mt-0.5 text-[11px] text-ink-400">
                                            <%# EncOrDash(Eval("ContactPerson")) %>
                                        </p>
                                    </td>
                                    <td class="py-4 pr-4 text-center text-[12.5px] text-ink-600">
                                        <%# EncOrDash(Eval("City")) %>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetCreditBadgeCss(Eval("CreditLimit")) %>'>
                                            <%# FormatMoney(Eval("CreditLimit")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeClass(Eval("IsActive"), Eval("IsDeleted")) %>'>
                                            <%# GetStatusText(Eval("IsActive"), Eval("IsDeleted")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 text-left">
                                        <div class="inline-flex items-center gap-1">

                                            <a href='<%# "sales-orders.aspx?customerId=" + Eval("CustomerId") %>'
                                                class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                title="سفارش‌های مشتری">
                                                <i class="h-4 w-4" data-lucide="shopping-bag"></i>
                                            </a>

                                            <asp:PlaceHolder ID="phEditActions" runat="server"
                                                Visible='<%# !IsCustomerDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit" CommandArgument='<%# Eval("CustomerId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phCanModify" runat="server"
                                                Visible='<%# !IsCustomerDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnToggle" runat="server"
                                                    CommandName="ToggleActive" CommandArgument='<%# Eval("CustomerId") %>'
                                                    CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                    ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                    OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("CustomerName")) %>'>
                                                    <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phDelete" runat="server"
                                                Visible='<%# CanDeleteCustomer(Eval("IsDeleted"), Eval("HasActivity")) %>'>
                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="SoftDelete" CommandArgument='<%# Eval("CustomerId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick='<%# GetDeleteConfirm(Eval("CustomerName")) %>'>
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phLocked" runat="server"
                                                Visible='<%# ShouldShowLock(Eval("IsDeleted"), Eval("HasActivity")) %>'>
                                                <span class="inline-flex h-8 items-center gap-1 rounded-base bg-surface-muted px-2 text-[11px] font-bold text-ink-500"
                                                    title="این مشتری دارای سابقه سفارش است و قابل حذف نیست">
                                                    <i class="h-3 w-3" data-lucide="lock"></i>
                                                    در استفاده
                                                </span>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsCustomerDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore" CommandArgument='<%# Eval("CustomerId") %>'
                                                    CssClass="icon-button hover:bg-success-50 hover:text-success-600"
                                                    ToolTip="بازگردانی"
                                                    OnClientClick="return confirm('این مشتری بازگردانی شود؟');">
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

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="py-14 text-center">
                <span class="mx-auto mb-3 grid h-14 w-14 place-items-center rounded-full bg-surface-muted text-ink-400">
                    <i class="h-6 w-6" data-lucide="users"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">مشتری‌ای با این فیلتر یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">فیلترها را تغییر دهید یا مشتری جدید اضافه کنید.</p>
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

    <style>
        .customer-type-card.selected {
            border-color: #2563eb !important;
            background: linear-gradient(135deg, rgba(37, 99, 235, 0.04), rgba(37, 99, 235, 0.02)) !important;
            box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.08);
        }
        .customer-type-card.selected .check-mark {
            display: block !important;
        }
    </style>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ نوع مشتری ═══
    function getSelectedCustomerType() {
        var r = document.getElementById('<%= rbTypeRetail.ClientID %>');
        var w = document.getElementById('<%= rbTypeWholesale.ClientID %>');
        var a = document.getElementById('<%= rbTypeAgent.ClientID %>');

        if (w && w.checked) return '2';
        if (a && a.checked) return '3';
        if (r && r.checked) return '1';
        return '1';
    }

    function applyCustomerTypeUI() {
        var type = getSelectedCustomerType();

        var cards = document.querySelectorAll('.customer-type-card');
        for (var i = 0; i < cards.length; i++) {
            var t = cards[i].getAttribute('data-type');
            if (t === type) cards[i].classList.add('selected');
            else cards[i].classList.remove('selected');
        }

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }

    function bindCustomerTypeEvents() {
        var r = document.getElementById('<%= rbTypeRetail.ClientID %>');
        var w = document.getElementById('<%= rbTypeWholesale.ClientID %>');
        var a = document.getElementById('<%= rbTypeAgent.ClientID %>');

        if (r) r.addEventListener('change', applyCustomerTypeUI);
        if (w) w.addEventListener('change', applyCustomerTypeUI);
        if (a) a.addEventListener('change', applyCustomerTypeUI);
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindCustomerTypeEvents();
        applyCustomerTypeUI();
    });

    if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            bindCustomerTypeEvents();
            applyCustomerTypeUI();
        });
    }
</script>
</asp:Content>