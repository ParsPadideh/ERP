<%@ Page Title="تغییر رمز عبور" Language="C#" MasterPageFile="~/admin/AdminMaster.master"
    AutoEventWireup="true" CodeFile="change-password.aspx.cs" Inherits="admin_change_password" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="mainContent" runat="server">

    <!-- ═══════════ Page header ═══════════ -->
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
            <nav class="mb-1 flex items-center gap-1.5 text-[13px] text-ink-400">
                <a class="hover:text-brand-600" href="Default.aspx">صفحه اصلی</a>
                <i class="h-3.5 w-3.5" data-lucide="chevron-left"></i>
                <span class="text-ink-700">تغییر رمز عبور</span>
            </nav>
            <h1 class="text-[22px] font-semibold text-ink-900">تغییر رمز عبور</h1>
            <p class="mt-1 text-[14px] text-ink-500">
                برای حفظ امنیت حساب کاربری، رمز عبور خود را به‌صورت دوره‌ای تغییر دهید.
            </p>
        </div>
    </div>

    <!-- ═══════════ Layout دو ستونه ═══════════ -->
    <div class="grid gap-5 lg:grid-cols-3">

        <!-- ═══════════ ستون اصلی: فرم ═══════════ -->
        <div class="lg:col-span-2">
            <asp:Panel ID="pnlForm" runat="server">
                <article class="rounded-card border border-surface-line bg-surface-card p-6 shadow-card">

                    <div class="mb-5 flex items-center gap-3 border-b border-surface-line pb-4">
                        <span class="grid h-10 w-10 place-items-center rounded-base bg-brand-50 text-brand-600">
                            <i class="h-5 w-5" data-lucide="key-round"></i>
                        </span>
                        <div>
                            <h2 class="text-[16px] font-semibold text-ink-900">تغییر رمز عبور</h2>
                            <p class="mt-0.5 text-[12px] text-ink-500">
                                رمز جدید باید حداقل ۶ کاراکتر و شامل حرف و عدد باشد.
                            </p>
                        </div>
                    </div>

                    <div class="space-y-5 max-w-xl">

                        <!-- رمز فعلی -->
                        <label class="block">
                            <span class="text-[13px] font-semibold text-ink-700">
                                رمز عبور فعلی <span class="text-danger-500">*</span>
                            </span>
                            <div class="relative mt-1.5">
                                <asp:TextBox ID="txtCurrentPassword" runat="server" TextMode="Password"
                                    MaxLength="200" dir="ltr"
                                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-11 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                                    placeholder="رمز فعلی خود را وارد کنید" />
                                <button type="button" onclick="togglePwd('<%= txtCurrentPassword.ClientID %>', this); return false;"
                                    class="absolute left-2 top-1/2 -translate-y-1/2 grid h-8 w-8 place-items-center rounded-lg text-ink-400 hover:bg-surface-muted hover:text-ink-700 transition"
                                    title="نمایش / مخفی کردن">
                                    <i class="h-4 w-4" data-lucide="eye"></i>
                                </button>
                            </div>
                        </label>

                        <!-- رمز جدید -->
                        <label class="block">
                            <span class="text-[13px] font-semibold text-ink-700">
                                رمز عبور جدید <span class="text-danger-500">*</span>
                            </span>
                            <div class="relative mt-1.5">
                                <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password"
                                    MaxLength="200" dir="ltr"
                                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-11 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                                    placeholder="حداقل ۶ کاراکتر"
                                    onkeyup="checkPasswordStrength(this.value);" />
                                <button type="button" onclick="togglePwd('<%= txtNewPassword.ClientID %>', this); return false;"
                                    class="absolute left-2 top-1/2 -translate-y-1/2 grid h-8 w-8 place-items-center rounded-lg text-ink-400 hover:bg-surface-muted hover:text-ink-700 transition"
                                    title="نمایش / مخفی کردن">
                                    <i class="h-4 w-4" data-lucide="eye"></i>
                                </button>
                            </div>

                            <!-- نشانگر قدرت رمز -->
                            <div class="mt-2.5">
                                <div class="flex items-center gap-1.5">
                                    <span class="h-1.5 flex-1 rounded-full bg-surface-muted transition-all" id="strengthBar1"></span>
                                    <span class="h-1.5 flex-1 rounded-full bg-surface-muted transition-all" id="strengthBar2"></span>
                                    <span class="h-1.5 flex-1 rounded-full bg-surface-muted transition-all" id="strengthBar3"></span>
                                    <span class="h-1.5 flex-1 rounded-full bg-surface-muted transition-all" id="strengthBar4"></span>
                                </div>
                                <p class="mt-1.5 text-[11.5px] font-semibold text-ink-500" id="strengthText">
                                    رمز عبور خود را وارد کنید
                                </p>
                            </div>
                        </label>

                        <!-- تکرار رمز جدید -->
                        <label class="block">
                            <span class="text-[13px] font-semibold text-ink-700">
                                تکرار رمز عبور جدید <span class="text-danger-500">*</span>
                            </span>
                            <div class="relative mt-1.5">
                                <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password"
                                    MaxLength="200" dir="ltr"
                                    CssClass="h-11 w-full rounded-base border border-surface-line bg-surface-body px-3 pl-11 text-[14px] focus:border-brand-600 focus:ring-4 focus:ring-brand-100/50 focus:outline-none transition"
                                    placeholder="تکرار رمز جدید"
                                    onkeyup="checkMatch();" />
                                <button type="button" onclick="togglePwd('<%= txtConfirmPassword.ClientID %>', this); return false;"
                                    class="absolute left-2 top-1/2 -translate-y-1/2 grid h-8 w-8 place-items-center rounded-lg text-ink-400 hover:bg-surface-muted hover:text-ink-700 transition"
                                    title="نمایش / مخفی کردن">
                                    <i class="h-4 w-4" data-lucide="eye"></i>
                                </button>
                            </div>
                            <p class="mt-1.5 text-[11.5px] font-semibold text-ink-500 hidden" id="matchText">
                                <i class="inline h-3 w-3" data-lucide="check-circle-2"></i>
                                رمزها یکسان هستند
                            </p>
                        </label>

                        <!-- دکمه‌ها -->
                        <div class="flex items-center justify-end gap-3 border-t border-surface-line pt-5">
                            <a href="Default.aspx"
                                class="inline-flex h-10 items-center rounded-base border border-surface-line px-5 text-[14px] font-semibold text-ink-700 hover:bg-surface-muted">
                                انصراف
                            </a>
                            <asp:Button ID="btnChangePassword" runat="server" Text="تغییر رمز عبور"
                                OnClick="btnChangePassword_Click"
                                CssClass="inline-flex h-10 items-center gap-2 rounded-base bg-brand-600 px-5 text-[14px] font-semibold text-white hover:bg-brand-700" />
                        </div>

                    </div>
                </article>
            </asp:Panel>
        </div>

        <!-- ═══════════ ستون کناری: راهنما و اطلاعات ═══════════ -->
        <div class="space-y-5">

            <!-- اطلاعات کاربر -->
            <article class="rounded-card border border-surface-line bg-surface-card p-5 shadow-card">
                <h3 class="mb-4 flex items-center gap-2 text-[13px] font-bold text-ink-700">
                    <i class="h-4 w-4 text-brand-500" data-lucide="user-circle-2"></i>
                    حساب کاربری شما
                </h3>

                <div class="flex items-center gap-3">
                    <span class="grid h-12 w-12 shrink-0 place-items-center rounded-full text-[16px] font-bold"
                        style="background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; box-shadow: 0 4px 12px -4px rgba(37,99,235,0.5);">
                        <asp:Literal ID="litUserInitial" runat="server" Text="U" />
                    </span>
                    <div class="min-w-0 flex-1">
                        <p class="truncate text-[13.5px] font-bold text-ink-900">
                            <asp:Literal ID="litUserFullName" runat="server" Text="کاربر" />
                        </p>
                        <p class="mt-0.5 truncate font-mono text-[11.5px] text-ink-500" dir="ltr">
                            @<asp:Literal ID="litUsername" runat="server" Text="user" />
                        </p>
                        <p class="mt-0.5 text-[11px] font-semibold text-brand-600">
                            <asp:Literal ID="litUserRole" runat="server" Text="—" />
                        </p>
                    </div>
                </div>

                <div class="mt-4 space-y-2 border-t border-surface-line pt-4 text-[12px]">
                    <div class="flex items-center justify-between">
                        <span class="text-ink-500">آخرین تغییر رمز</span>
                        <span class="font-semibold text-ink-800">
                            <asp:Literal ID="litLastPasswordChange" runat="server" Text="—" />
                        </span>
                    </div>
                    <div class="flex items-center justify-between">
                        <span class="text-ink-500">آخرین ورود</span>
                        <span class="font-semibold text-ink-800">
                            <asp:Literal ID="litLastLogin" runat="server" Text="—" />
                        </span>
                    </div>
                </div>
            </article>

            <!-- نکات امنیتی -->
            <article class="rounded-card border border-brand-100 bg-brand-50/50 p-5 shadow-card">
                <h3 class="mb-3 flex items-center gap-2 text-[13px] font-bold text-brand-800">
                    <i class="h-4 w-4 text-brand-600" data-lucide="shield-check"></i>
                    نکات امنیتی رمز عبور
                </h3>
                <ul class="space-y-2.5 text-[12px] leading-relaxed text-ink-700">
                    <li class="flex items-start gap-2">
                        <i class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" data-lucide="check"></i>
                        <span>حداقل ۶ کاراکتر — ترجیحاً ۸ کاراکتر یا بیشتر</span>
                    </li>
                    <li class="flex items-start gap-2">
                        <i class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" data-lucide="check"></i>
                        <span>ترکیبی از حروف بزرگ و کوچک انگلیسی</span>
                    </li>
                    <li class="flex items-start gap-2">
                        <i class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" data-lucide="check"></i>
                        <span>حداقل یک عدد و یک کاراکتر خاص (@ # $ ! %)</span>
                    </li>
                    <li class="flex items-start gap-2">
                        <i class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" data-lucide="check"></i>
                        <span>از نام، تاریخ تولد یا کلمات ساده استفاده نکنید</span>
                    </li>
                    <li class="flex items-start gap-2">
                        <i class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-500" data-lucide="check"></i>
                        <span>رمز را با هیچ‌کس به اشتراک نگذارید</span>
                    </li>
                </ul>
            </article>

            <!-- هشدار -->
            <article class="rounded-card border border-warning-100 bg-warning-50 p-5 shadow-card">
                <div class="flex items-start gap-3">
                    <span class="grid h-9 w-9 shrink-0 place-items-center rounded-base bg-warning-100 text-warning-700">
                        <i class="h-4.5 w-4.5" data-lucide="alert-triangle"></i>
                    </span>
                    <div>
                        <p class="text-[12.5px] font-bold text-warning-800">هشدار امنیتی</p>
                        <p class="mt-1 text-[11.5px] leading-relaxed text-warning-700">
                            پس از تغییر رمز، در صورت استفاده از دستگاه‌های دیگر، از آن‌ها خارج شده و دوباره وارد شوید.
                        </p>
                    </div>
                </div>
            </article>

        </div>
    </div>

</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<script type="text/javascript">
    // ═══ نمایش / مخفی کردن رمز ═══
    function togglePwd(inputId, btn) {
        var input = document.getElementById(inputId);
        if (!input) return;

        if (input.type === 'password') {
            input.type = 'text';
            if (btn) {
                var icon = btn.querySelector('i');
                if (icon) {
                    icon.setAttribute('data-lucide', 'eye-off');
                    if (typeof lucide !== 'undefined') lucide.createIcons();
                }
            }
        } else {
            input.type = 'password';
            if (btn) {
                var icon2 = btn.querySelector('i');
                if (icon2) {
                    icon2.setAttribute('data-lucide', 'eye');
                    if (typeof lucide !== 'undefined') lucide.createIcons();
                }
            }
        }
    }

    // ═══ قدرت رمز ═══
    function checkPasswordStrength(pwd) {
        var bars = [
            document.getElementById('strengthBar1'),
            document.getElementById('strengthBar2'),
            document.getElementById('strengthBar3'),
            document.getElementById('strengthBar4')
        ];
        var txt = document.getElementById('strengthText');

        var score = 0;
        if (!pwd || pwd.length === 0) {
            resetBars(bars);
            if (txt) { txt.textContent = 'رمز عبور خود را وارد کنید'; txt.style.color = '#64748b'; }
            return;
        }

        if (pwd.length >= 6) score++;
        if (pwd.length >= 8) score++;
        if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd)) score++;
        if (/\d/.test(pwd)) score++;
        if (/[^A-Za-z0-9]/.test(pwd)) score++;

        // حداکثر 4
        if (score > 4) score = 4;

        var colors = ['', '#ef4444', '#f59e0b', '#3b82f6', '#10b981'];
        var labels = ['', 'ضعیف', 'متوسط', 'خوب', 'قوی'];

        resetBars(bars);

        for (var i = 0; i < score; i++) {
            if (bars[i]) {
                bars[i].style.background = colors[score];
            }
        }

        if (txt) {
            txt.textContent = 'قدرت رمز: ' + labels[score];
            txt.style.color = colors[score];
        }
    }

    function resetBars(bars) {
        for (var i = 0; i < bars.length; i++) {
            if (bars[i]) bars[i].style.background = '#f1f5f9';
        }
    }

    // ═══ تطابق رمز ═══
    function checkMatch() {
        var newPwd = document.getElementById('<%= txtNewPassword.ClientID %>');
        var confirmPwd = document.getElementById('<%= txtConfirmPassword.ClientID %>');
        var matchText = document.getElementById('matchText');

        if (!newPwd || !confirmPwd || !matchText) return;

        if (confirmPwd.value.length === 0) {
            matchText.classList.add('hidden');
            return;
        }

        if (newPwd.value === confirmPwd.value) {
            matchText.classList.remove('hidden');
            matchText.className = 'mt-1.5 text-[11.5px] font-semibold text-success-600';
            matchText.innerHTML = '<i class="inline h-3 w-3" data-lucide="check-circle-2"></i> رمزها یکسان هستند';
        } else {
            matchText.classList.remove('hidden');
            matchText.className = 'mt-1.5 text-[11.5px] font-semibold text-danger-500';
            matchText.innerHTML = '<i class="inline h-3 w-3" data-lucide="x-circle"></i> رمزها یکسان نیستند';
        }

        if (typeof lucide !== 'undefined') lucide.createIcons();
    }
</script>
</asp:Content>