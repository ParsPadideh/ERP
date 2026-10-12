<%@ Page Title="محصولات" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="products.aspx.cs" Inherits="admin_products" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <asp:HiddenField ID="hfEditProductId" runat="server" Value="0" />

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">محصولات</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">مدیریت محصولات</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                مواد اولیه، محصولات نهایی و مواد بسته‌بندی را تعریف و مدیریت کنید.
            </p>
        </div>
        <div class="flex items-center gap-2">
            <a href="stock-onhand.aspx"
                class="inline-flex h-11 items-center gap-2 rounded-base border border-surface-line bg-surface-card px-4 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                <i class="h-4 w-4" data-lucide="boxes"></i>
                موجودی
            </a>
            <asp:Button ID="btnShowAddForm" runat="server" Text="افزودن محصول جدید"
                OnClick="btnShowAddForm_Click"
                CssClass="inline-flex h-11 items-center gap-2 rounded-base bg-brand-600 px-4 text-[14px] font-semibold text-white hover:bg-brand-700" />
        </div>
    </div>

    <!-- ═══════════ کارت‌های آماری ═══════════ -->
    <asp:Panel ID="pnlStats" runat="server" CssClass="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                    <i class="h-5 w-5" data-lucide="package"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">کل محصولات</p>
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
                    <p class="text-[12px] text-ink-500">مواد اولیه (RM)</p>
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
                    <p class="text-[12px] text-ink-500">محصول نهایی (FG)</p>
                    <p class="mt-0.5 text-[20px] font-bold text-violet-600">
                        <asp:Literal ID="litStatFG" runat="server" Text="۰" />
                    </p>
                </div>
            </div>
        </article>

        <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
            <div class="flex items-center gap-3">
                <span class="grid h-10 w-10 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                    <i class="h-5 w-5" data-lucide="box"></i>
                </span>
                <div>
                    <p class="text-[12px] text-ink-500">بسته‌بندی</p>
                    <p class="mt-0.5 text-[20px] font-bold text-emerald-600">
                        <asp:Literal ID="litStatPackaging" runat="server" Text="۰" />
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
                        <i class="h-5 w-5" data-lucide="package-plus"></i>
                    </span>
                    <div>
                        <h2 class="text-[16px] font-semibold text-ink-900">
                            <asp:Literal ID="litFormTitle" runat="server" Text="افزودن محصول جدید" />
                        </h2>
                        <p class="mt-0.5 text-[12px] text-ink-500">
                            ابتدا نوع محصول را انتخاب کنید تا فیلدهای مرتبط نمایش داده شوند.
                        </p>
                    </div>
                </div>
                <asp:LinkButton ID="btnCancelTop" runat="server" OnClick="btnCancel_Click"
                    CssClass="text-[13px] font-semibold text-ink-500 hover:text-ink-700">
                    <i class="inline h-3.5 w-3.5" data-lucide="x"></i>
                    انصراف
                </asp:LinkButton>
            </div>

            <!-- ── بخش ۱: نوع محصول (اولویت اول) ── -->
            <div class="mb-6">
                <h3 class="mb-3 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                    <i class="h-4 w-4 text-brand-500" data-lucide="layers"></i>
                    نوع محصول <span class="text-danger-500">*</span>
                </h3>

                <div class="grid gap-3 sm:grid-cols-3">
                    <label class="product-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="1">
                        <asp:RadioButton ID="rbTypeRM" runat="server" GroupName="ProductType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-amber-50 text-amber-600">
                            <i class="h-5 w-5" data-lucide="pickaxe"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">مواد اولیه</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">RM — برای تولید مصرف می‌شود</p>
                        </div>
                        <span class="check-mark hidden text-brand-600">
                            <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                        </span>
                    </label>

                    <label class="product-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="2">
                        <asp:RadioButton ID="rbTypeFG" runat="server" GroupName="ProductType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-violet-50 text-violet-600">
                            <i class="h-5 w-5" data-lucide="package-check"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">محصول نهایی</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">FG — برای فروش آماده است</p>
                        </div>
                        <span class="check-mark hidden text-brand-600">
                            <i class="h-5 w-5" data-lucide="check-circle-2"></i>
                        </span>
                    </label>

                    <label class="product-type-card relative flex cursor-pointer items-center gap-3 rounded-card border-2 border-surface-line bg-surface-card p-4 transition hover:border-brand-300"
                        data-type="3">
                        <asp:RadioButton ID="rbTypePackaging" runat="server" GroupName="ProductType"
                            CssClass="peer hidden" />
                        <span class="grid h-11 w-11 shrink-0 place-items-center rounded-base bg-emerald-50 text-emerald-600">
                            <i class="h-5 w-5" data-lucide="box"></i>
                        </span>
                        <div class="min-w-0 flex-1 text-right">
                            <p class="text-[13.5px] font-bold text-ink-900">مواد بسته‌بندی</p>
                            <p class="mt-0.5 text-[11px] text-ink-500">شیشه، برچسب، کارتن، ...</p>
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
                        کد محصول <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtProductCode" runat="server" MaxLength="50" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] font-mono focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="RM-001 / FG-101" />
                    <p class="mt-1 text-[11px] text-ink-400">یکتا — حروف انگلیسی، عدد، `-` `_` `.`</p>
                </label>

                <label class="block lg:col-span-2">
                    <span class="text-[13px] font-semibold text-ink-700">
                        نام محصول <span class="text-danger-500">*</span>
                    </span>
                    <asp:TextBox ID="txtProductName" runat="server" MaxLength="200"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: زعفران سرگل ۱ گرمی" />
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">دسته‌بندی</span>
                    <asp:DropDownList ID="ddlCategory" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                    <p class="mt-1 text-[11px] text-ink-400">اختیاری — برای گزارش‌گیری بهتر</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">
                        واحد اندازه‌گیری <span class="text-danger-500">*</span>
                    </span>
                    <asp:DropDownList ID="ddlUnit" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    </asp:DropDownList>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">وضعیت</span>
                    <asp:DropDownList ID="ddlIsActive" runat="server"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                        <asp:ListItem Value="1" Text="فعال" />
                        <asp:ListItem Value="0" Text="غیرفعال" />
                    </asp:DropDownList>
                </label>

                <label class="block lg:col-span-3">
                    <span class="text-[13px] font-semibold text-ink-700">توضیحات</span>
                    <asp:TextBox ID="txtDescription" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2"
                        CssClass="mt-1.5 w-full rounded-base border border-surface-line bg-surface-body px-3 py-2 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="توضیحات کوتاه (اختیاری)" />
                </label>

            </div>

            <!-- ── بخش ۳: مشخصات و کنترل موجودی ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="scale"></i>
                کنترل موجودی
            </h3>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">

                <!-- وزن: فقط برای FG -->
                <label class="block" id="weightField" style="display:none;">
                    <span class="text-[13px] font-semibold text-ink-700">
                        وزن (گرم)
                        <span class="badge bg-violet-50 text-violet-700" style="font-size:10px;">فقط FG</span>
                    </span>
                    <asp:TextBox ID="txtWeightGram" runat="server" MaxLength="15" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: 1" />
                    <p class="mt-1 text-[11px] text-ink-400">وزن بسته‌بندی محصول نهایی</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">حداقل موجودی</span>
                    <asp:TextBox ID="txtMinStock" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="0" />
                    <p class="mt-1 text-[11px] text-ink-400">هشدار کمبود</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">حداکثر موجودی</span>
                    <asp:TextBox ID="txtMaxStock" runat="server" MaxLength="20" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="اختیاری" />
                    <p class="mt-1 text-[11px] text-ink-400">سقف ظرفیت</p>
                </label>

                <label class="block">
                    <span class="text-[13px] font-semibold text-ink-700">ماندگاری (روز)</span>
                    <asp:TextBox ID="txtShelfLifeDays" runat="server" MaxLength="10" dir="ltr"
                        CssClass="mt-1.5 h-10 w-full rounded-base border border-surface-line bg-surface-body px-3 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                        placeholder="مثال: 365" />
                    <p class="mt-1 text-[11px] text-ink-400">تاریخ انقضا</p>
                </label>

            </div>

            <!-- ── بخش ۴: تنظیمات ── -->
            <h3 class="mt-6 mb-3 border-t border-surface-line pt-5 flex items-center gap-2 text-[14px] font-semibold text-ink-700">
                <i class="h-4 w-4 text-brand-500" data-lucide="settings-2"></i>
                تنظیمات پیشرفته
            </h3>
            <div class="grid gap-4 md:grid-cols-2">

                <label class="flex items-start gap-3 rounded-base border border-surface-line bg-surface-body p-4 cursor-pointer hover:border-brand-300 transition">
                    <asp:CheckBox ID="chkRequiresBatch" runat="server"
                        CssClass="mt-0.5 h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                    <div>
                        <p class="text-[13px] font-bold text-ink-800">نیاز به شماره بچ</p>
                        <p class="mt-0.5 text-[11px] text-ink-500">
                            برای ردیابی دقیق دسته خرید یا تولید — مناسب خشکبار، ادویه، مواد حساس
                        </p>
                    </div>
                </label>

                <div class="rounded-base border border-brand-100 bg-brand-50/50 p-4 flex items-start gap-3">
                    <span class="grid h-8 w-8 shrink-0 place-items-center rounded-base bg-brand-100 text-brand-600">
                        <i class="h-4 w-4" data-lucide="lightbulb"></i>
                    </span>
                    <div class="text-[11.5px] leading-relaxed text-ink-700">
                        <p class="font-bold text-ink-800">راهنمای سریع</p>
                        <p class="mt-1" id="typeHintText">
                            نوع محصول را انتخاب کنید تا راهنمای مرتبط نمایش داده شود.
                        </p>
                    </div>
                </div>

            </div>

            <div class="mt-6 flex items-center justify-end gap-3 border-t border-surface-line pt-4">
                <asp:Button ID="btnCancel" runat="server" Text="انصراف" OnClick="btnCancel_Click"
                    CssClass="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted" />
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" OnClick="btnSave_Click"
                    CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
            </div>
        </article>
    </asp:Panel>

    <!-- ═══════════ لیست محصولات ═══════════ -->
    <asp:Panel ID="pnlList" runat="server">
        <section class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

            <div class="mb-5 flex flex-wrap items-center gap-3">
                <label class="relative block min-w-[220px] flex-1">
                    <span class="sr-only">جستجوی محصول</span>
                    <i class="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400"
                        data-lucide="search"></i>
                    <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                        OnTextChanged="txtSearch_TextChanged"
                        CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body pl-11 pr-4 text-[14px] focus:border-brand-600"
                        placeholder="جستجو بر اساس کد یا نام محصول" />
                </label>

                <asp:DropDownList ID="ddlFilterType" runat="server" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlFilterType_SelectedIndexChanged"
                    CssClass="h-11 rounded-base border border-surface-line bg-surface-card px-3 text-[14px] text-ink-700 focus:border-brand-600">
                    <asp:ListItem Value="" Text="همه انواع" />
                    <asp:ListItem Value="1" Text="مواد اولیه" />
                    <asp:ListItem Value="2" Text="محصول نهایی" />
                    <asp:ListItem Value="3" Text="بسته‌بندی" />
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
                            <th class="pb-3 pr-4 text-right font-semibold">محصول</th>
                            <th class="pb-3 pr-4 text-center font-semibold">نوع</th>
                            <th class="pb-3 pr-4 text-center font-semibold">دسته‌بندی</th>
                            <th class="pb-3 pr-4 text-center font-semibold">واحد</th>
                            <th class="pb-3 pr-4 text-center font-semibold">موجودی</th>
                            <th class="pb-3 pr-4 text-center font-semibold">وضعیت</th>
                            <th class="pb-3 text-left font-semibold">اقدام</th>
                        </tr>
                    </thead>
                    <tbody class="text-[14px]">
                        <asp:Repeater ID="rptProducts" runat="server" OnItemCommand="rptProducts_ItemCommand">
                            <ItemTemplate>
                                <tr class="border-b border-surface-line hover:bg-surface-body/70 transition">
                                    <td class="py-4 pr-4">
                                        <div class="flex items-center gap-3">
                                            <span class='<%# GetProductAvatarCss(Eval("ProductType"), Eval("IsActive"), Eval("IsDeleted")) %>'>
                                                <i class="h-5 w-5" data-lucide='<%# GetProductIcon(Eval("ProductType")) %>'></i>
                                            </span>
                                            <div class="min-w-0 text-right">
                                                <p class="truncate font-semibold text-ink-900">
                                                    <%# Enc(Eval("ProductName")) %>
                                                    <%# GetBatchBadge(Eval("RequiresBatch")) %>
                                                </p>
                                                <p class="mt-0.5 truncate font-mono text-[11.5px] text-ink-400" dir="ltr">
                                                    <%# Enc(Eval("ProductCode")) %>
                                                </p>
                                            </div>
                                        </div>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetTypeBadgeCss(Eval("ProductType")) %>'>
                                            <%# GetTypeLabel(Eval("ProductType")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center text-[12.5px] text-ink-600">
                                        <%# EncOrDash(Eval("CategoryName")) %>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class="badge bg-surface-muted text-ink-700">
                                            <%# Enc(Eval("UnitName")) %>
                                            <span class="font-mono text-[10.5px] text-ink-400" dir="ltr">
                                                <%# EncOrDash(Eval("Symbol")) %>
                                            </span>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetStockBadgeCss(Eval("OnHand"), Eval("MinStock")) %>'>
                                            <%# FormatQuantity(Eval("OnHand")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 pr-4 text-center">
                                        <span class='<%# GetStatusBadgeClass(Eval("IsActive"), Eval("IsDeleted")) %>'>
                                            <%# GetStatusText(Eval("IsActive"), Eval("IsDeleted")) %>
                                        </span>
                                    </td>
                                    <td class="py-4 text-left">
                                        <div class="inline-flex items-center gap-1">

                                            <a href='<%# "stock-onhand.aspx?productId=" + Eval("ProductId") %>'
                                                class="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                title="مشاهده موجودی">
                                                <i class="h-4 w-4" data-lucide="eye"></i>
                                            </a>

                                            <asp:PlaceHolder ID="phEditActions" runat="server"
                                                Visible='<%# !IsProductDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnEdit" runat="server"
                                                    CommandName="Edit" CommandArgument='<%# Eval("ProductId") %>'
                                                    CssClass="icon-button hover:bg-brand-50 hover:text-brand-600"
                                                    ToolTip="ویرایش">
                                                    <i class="h-4 w-4" data-lucide="pencil"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phCanModify" runat="server"
                                                Visible='<%# !IsProductDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnToggle" runat="server"
                                                    CommandName="ToggleActive" CommandArgument='<%# Eval("ProductId") %>'
                                                    CssClass='<%# GetToggleCss(Eval("IsActive")) %>'
                                                    ToolTip='<%# GetToggleTooltip(Eval("IsActive")) %>'
                                                    OnClientClick='<%# GetToggleConfirm(Eval("IsActive"), Eval("ProductName")) %>'>
                                                    <i class="h-4 w-4" data-lucide='<%# GetToggleIcon(Eval("IsActive")) %>'></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phDelete" runat="server"
                                                Visible='<%# CanDeleteProduct(Eval("IsDeleted"), Eval("HasActivity")) %>'>
                                                <asp:LinkButton ID="btnDelete" runat="server"
                                                    CommandName="SoftDelete" CommandArgument='<%# Eval("ProductId") %>'
                                                    CssClass="icon-button hover:bg-danger-50 hover:text-danger-500"
                                                    ToolTip="حذف"
                                                    OnClientClick='<%# GetDeleteConfirm(Eval("ProductName")) %>'>
                                                    <i class="h-4 w-4" data-lucide="trash-2"></i>
                                                </asp:LinkButton>
                                            </asp:PlaceHolder>

                                            <asp:PlaceHolder ID="phRestore" runat="server"
                                                Visible='<%# IsProductDeleted(Eval("IsDeleted")) %>'>
                                                <asp:LinkButton ID="btnRestore" runat="server"
                                                    CommandName="Restore" CommandArgument='<%# Eval("ProductId") %>'
                                                    CssClass="icon-button hover:bg-success-50 hover:text-success-600"
                                                    ToolTip="بازگردانی"
                                                    OnClientClick="return confirm('این محصول بازگردانی شود؟');">
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
                    <i class="h-6 w-6" data-lucide="package-x"></i>
                </span>
                <p class="text-[14px] font-semibold text-ink-600">محصولی با این فیلتر یافت نشد.</p>
                <p class="mt-1 text-[12.5px] text-ink-400">فیلترها را تغییر دهید یا محصول جدید اضافه کنید.</p>
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

    <!-- CSS اختصاصی برای کارت‌های نوع محصول -->
    <style>
        .product-type-card.selected {
            border-color: #2563eb !important;
            background: linear-gradient(135deg, rgba(37, 99, 235, 0.04), rgba(37, 99, 235, 0.02)) !important;
            box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.08);
        }
        .product-type-card.selected .check-mark {
            display: block !important;
        }
    </style>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ نمایش/مخفی کردن فیلدها بر اساس نوع محصول ═══
    function getSelectedProductType() {
        var rm = document.getElementById('<%= rbTypeRM.ClientID %>');
        var fg = document.getElementById('<%= rbTypeFG.ClientID %>');
        var pk = document.getElementById('<%= rbTypePackaging.ClientID %>');

        if (fg && fg.checked) return '2';
        if (pk && pk.checked) return '3';
        if (rm && rm.checked) return '1';
        return '1'; // پیش‌فرض: مواد اولیه
    }

    function applyProductTypeUI() {
        var type = getSelectedProductType();

        // نمایش/مخفی کردن فیلد وزن
        var weightField = document.getElementById('weightField');
        if (weightField) {
            weightField.style.display = (type === '2') ? '' : 'none';
        }

        // برجسته‌سازی کارت انتخاب‌شده
        var cards = document.querySelectorAll('.product-type-card');
        for (var i = 0; i < cards.length; i++) {
            var cardType = cards[i].getAttribute('data-type');
            if (cardType === type) {
                cards[i].classList.add('selected');
            } else {
                cards[i].classList.remove('selected');
            }
        }

        // راهنمای سریع
        var hint = document.getElementById('typeHintText');
        if (hint) {
            if (type === '1') {
                hint.innerHTML = '<strong>مواد اولیه (RM):</strong> از تأمین‌کننده خریداری می‌شود و در خط تولید مصرف می‌گردد. واحد معمولاً «کیلوگرم» است.';
            } else if (type === '2') {
                hint.innerHTML = '<strong>محصول نهایی (FG):</strong> پس از تولید، آماده فروش است. وزن بسته‌بندی، تاریخ انقضا و MinStock اهمیت بالایی دارند.';
            } else {
                hint.innerHTML = '<strong>مواد بسته‌بندی:</strong> شامل شیشه، برچسب، کارتن، ... . این اقلام در BOM محصول نهایی مصرف می‌شوند.';
            }
        }

        // آیکن‌های Lucide دوباره رندر شوند
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    function bindProductTypeEvents() {
        var rm = document.getElementById('<%= rbTypeRM.ClientID %>');
        var fg = document.getElementById('<%= rbTypeFG.ClientID %>');
        var pk = document.getElementById('<%= rbTypePackaging.ClientID %>');

        if (rm) rm.addEventListener('change', applyProductTypeUI);
        if (fg) fg.addEventListener('change', applyProductTypeUI);
        if (pk) pk.addEventListener('change', applyProductTypeUI);
    }

    // در بارگذاری اولیه
    document.addEventListener('DOMContentLoaded', function () {
        bindProductTypeEvents();
        applyProductTypeUI();
    });

    // در PostBack های AJAX
    if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            bindProductTypeEvents();
            applyProductTypeUI();
        });
    }
</script>
</asp:Content>