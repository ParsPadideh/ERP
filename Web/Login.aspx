<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Login" %>

<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no" />
    <title>ورود به سامانه | ERP کارخانه</title>

    <link rel="preconnect" href="https://cdn.jsdelivr.net" />
    <link href="https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css" rel="stylesheet" />
    <script src="https://cdn.tailwindcss.com"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    fontFamily: { sans: ['Vazirmatn', 'Tahoma', 'sans-serif'] },
                    colors: {
                        brand: {
                            50: '#eff6ff', 100: '#dbeafe', 200: '#bfdbfe', 300: '#93c5fd',
                            400: '#60a5fa', 500: '#3b82f6', 600: '#2563eb', 700: '#1d4ed8',
                            800: '#1e40af', 900: '#1e3a8a'
                        },
                        ink: {
                            300: '#cbd5e1', 400: '#94a3b8', 500: '#64748b',
                            600: '#475569', 700: '#334155', 800: '#1e293b', 900: '#0f172a'
                        }
                    },
                    borderRadius: { 'card': '20px', 'base': '12px' },
                    boxShadow: {
                        'card-lg': '0 30px 60px -20px rgba(15, 23, 42, 0.25), 0 8px 24px -8px rgba(15, 23, 42, 0.1)',
                        'glow': '0 20px 40px -16px rgba(37, 99, 235, 0.5)'
                    }
                }
            }
        }
    </script>
    <script src="https://unpkg.com/lucide@latest/dist/umd/lucide.min.js"></script>

    <style>
        html, body { font-family: 'Vazirmatn', Tahoma, sans-serif; }
        body {
            min-height: 100vh;
            background: #0b1220;
            overflow-x: hidden;
        }
        .bg-hero {
            position: fixed;
            inset: 0;
            z-index: 0;
            background:
                radial-gradient(ellipse at top right, rgba(37, 99, 235, 0.35), transparent 55%),
                radial-gradient(ellipse at bottom left, rgba(16, 185, 129, 0.25), transparent 55%),
                linear-gradient(135deg, #0b1220 0%, #0f172a 50%, #0b1220 100%);
        }
        .bg-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background-image:
                linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px);
            background-size: 48px 48px;
            mask-image: radial-gradient(ellipse at center, black 40%, transparent 75%);
            -webkit-mask-image: radial-gradient(ellipse at center, black 40%, transparent 75%);
        }

        .login-shell { position: relative; z-index: 1; }

        .glass-card {
            background: rgba(255, 255, 255, 0.98);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.4);
        }

        .input-field {
            width: 100%;
            height: 50px;
            padding: 0 14px 0 46px;
            border-radius: 12px;
            border: 1.5px solid #e2e8f0;
            background: #f8fafc;
            font-size: 14px;
            color: #0f172a;
            transition: all .2s;
            outline: none;
        }
        .input-field::placeholder { color: #94a3b8; }
        .input-field:focus {
            border-color: #2563eb;
            background: #ffffff;
            box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.12);
        }

        .field-icon {
            position: absolute;
            left: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: #94a3b8;
            pointer-events: none;
        }

        .btn-primary {
            width: 100%;
            height: 50px;
            border-radius: 12px;
            background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
            color: #fff;
            font-size: 14.5px;
            font-weight: 700;
            letter-spacing: .02em;
            border: none;
            cursor: pointer;
            transition: all .2s cubic-bezier(.4, 0, .2, 1);
            box-shadow: 0 10px 25px -8px rgba(37, 99, 235, 0.55);
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }
        .btn-primary:hover {
            transform: translateY(-1px);
            box-shadow: 0 15px 30px -8px rgba(37, 99, 235, 0.7);
        }
        .btn-primary:active { transform: translateY(0); }
        .btn-primary:disabled { opacity: .7; cursor: not-allowed; transform: none; }

        .err-box {
            background: #fef2f2;
            border: 1px solid #fecaca;
            color: #b91c1c;
            padding: 12px 14px;
            border-radius: 12px;
            font-size: 13px;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 8px;
            animation: shake .4s ease;
        }
        @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-6px); }
            75% { transform: translateX(6px); }
        }

        .success-box {
            background: #ecfdf5;
            border: 1px solid #a7f3d0;
            color: #047857;
            padding: 12px 14px;
            border-radius: 12px;
            font-size: 13px;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .brand-badge {
            width: 64px;
            height: 64px;
            border-radius: 20px;
            background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
            display: grid;
            place-items: center;
            color: #fff;
            box-shadow: 0 15px 30px -10px rgba(37, 99, 235, 0.6);
        }

        .float-slow { animation: floatY 6s ease-in-out infinite; }
        @keyframes floatY {
            0%, 100% { transform: translateY(0); }
            50% { transform: translateY(-14px); }
        }

        .feature-pill {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 8px 14px;
            background: rgba(255, 255, 255, 0.06);
            border: 1px solid rgba(255, 255, 255, 0.08);
            border-radius: 999px;
            color: rgba(255, 255, 255, 0.85);
            font-size: 12.5px;
            font-weight: 600;
            backdrop-filter: blur(8px);
        }
        .feature-pill .dot {
            width: 6px; height: 6px; border-radius: 50%;
            background: #34d399;
            box-shadow: 0 0 10px #34d399;
        }
    </style>
</head>
<body>
<div class="bg-hero"></div>

<form id="form1" runat="server">
    <div class="login-shell flex min-h-screen items-center justify-center p-4 sm:p-6 lg:p-8">
        <div class="grid w-full max-w-5xl overflow-hidden rounded-card shadow-card-lg lg:grid-cols-2 glass-card">

            <!-- ستون فرم -->
            <div class="p-8 sm:p-10 lg:p-12">

                <div class="mb-8 flex flex-col items-center text-center lg:items-start lg:text-right">
                    <span class="brand-badge mb-4">
                        <i class="h-7 w-7" data-lucide="factory"></i>
                    </span>
                    <h1 class="text-[24px] font-extrabold text-ink-900">ورود به سامانه ERP</h1>
                    <p class="mt-1 text-[13.5px] text-ink-500">
                        کارخانه تولید و بسته‌بندی مواد غذایی
                    </p>
                </div>

                <!-- پیام‌ها -->
                <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-5">
                    <div class="err-box">
                        <i class="h-4 w-4" data-lucide="alert-circle"></i>
                        <asp:Literal ID="litError" runat="server" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="mb-5">
                    <div class="success-box">
                        <i class="h-4 w-4" data-lucide="check-circle-2"></i>
                        <asp:Literal ID="litSuccess" runat="server" />
                    </div>
                </asp:Panel>

                <!-- فرم -->
                <div class="space-y-5">
                    <div>
                        <label class="mb-2 block text-[13px] font-bold text-ink-700">نام کاربری</label>
                        <div class="relative">
                            <span class="field-icon"><i class="h-4 w-4" data-lucide="user"></i></span>
                            <asp:TextBox ID="txtUsername" runat="server" MaxLength="50"
                                CssClass="input-field" placeholder="نام کاربری خود را وارد کنید"
                                autocomplete="username" />
                        </div>
                    </div>

                    <div>
                        <label class="mb-2 block text-[13px] font-bold text-ink-700">رمز عبور</label>
                        <div class="relative">
                            <span class="field-icon"><i class="h-4 w-4" data-lucide="lock"></i></span>
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="200"
                                CssClass="input-field" placeholder="رمز عبور خود را وارد کنید"
                                autocomplete="current-password" />
                        </div>
                    </div>

                    <div class="flex items-center justify-between pt-1">
                        <label class="inline-flex cursor-pointer items-center gap-2 select-none">
                            <asp:CheckBox ID="chkRemember" runat="server"
                                CssClass="h-4 w-4 rounded border-ink-300 text-brand-600 focus:ring-brand-500" />
                            <span class="text-[12.5px] font-semibold text-ink-600">مرا به خاطر بسپار</span>
                        </label>
                        <a href="javascript:void(0);" onclick="showForgot(); return false;"
                           class="text-[12.5px] font-bold text-brand-600 hover:text-brand-700">
                            فراموشی رمز؟
                        </a>
                    </div>

                    <asp:Button ID="btnLogin" runat="server" Text="ورود به سامانه"
                        OnClick="btnLogin_Click"
                        CssClass="btn-primary" />

                    <p class="pt-2 text-center text-[11.5px] text-ink-400">
                        با ورود، شما <a href="javascript:void(0);" class="font-bold text-brand-600 hover:underline">قوانین سامانه</a> را می‌پذیرید.
                    </p>
                </div>
            </div>

            <!-- ستون معرفی -->
            <div class="relative hidden overflow-hidden bg-gradient-to-br from-[#0f172a] via-[#1e293b] to-[#0b1220] p-10 lg:flex lg:flex-col lg:justify-between">

                <div class="pointer-events-none absolute inset-0 opacity-40">
                    <div class="absolute -right-20 -top-20 h-72 w-72 rounded-full bg-brand-600/40 blur-3xl float-slow"></div>
                    <div class="absolute -bottom-24 -left-16 h-80 w-80 rounded-full bg-emerald-500/30 blur-3xl float-slow" style="animation-delay:-2s;"></div>
                </div>

                <div class="relative z-10">
                    <span class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-1 text-[11px] font-bold text-white/70 backdrop-blur">
                        <span class="dot" style="width:6px;height:6px;border-radius:50%;background:#34d399;box-shadow:0 0 8px #34d399;display:inline-block;"></span>
                        نسخه ۱.۰ — فاز MVP
                    </span>
                    <h2 class="mt-6 text-[26px] font-extrabold leading-tight text-white">
                        یکپارچه‌سازی کامل<br />
                        <span class="text-brand-300">چرخه کسب‌وکار</span> کارخانه
                    </h2>
                    <p class="mt-4 max-w-md text-[13.5px] leading-relaxed text-white/60">
                        از خرید مواد اولیه تا فروش نهایی، همه در یک سیستم. با موتور FIFO، محاسبه دقیق بهای تمام‌شده و رزرو هوشمند موجودی.
                    </p>
                </div>

                <div class="relative z-10 mt-10 grid grid-cols-1 gap-2.5">
                    <span class="feature-pill"><span class="dot"></span> مدیریت دو انبار مستقل RM / FG</span>
                    <span class="feature-pill"><span class="dot"></span> موتور FIFO برای بهای تمام‌شده واقعی</span>
                    <span class="feature-pill"><span class="dot"></span> پیش‌فاکتور با رزرو خودکار موجودی</span>
                    <span class="feature-pill"><span class="dot"></span> سه کانال فروش از یک موتور موجودی</span>
                </div>

                <div class="relative z-10 mt-8 border-t border-white/10 pt-5">
                    <p class="text-[11.5px] font-semibold text-white/40">
                        © <asp:Literal ID="litYear" runat="server" /> — Food Factory ERP
                    </p>
                </div>
            </div>
        </div>
    </div>
</form>

<script>
    function showForgot() {
        alert('برای بازیابی رمز عبور با مدیر سیستم تماس بگیرید.');
    }

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons({ attrs: { 'stroke-width': 2, 'class': 'lucide' } });
        }
        var u = document.getElementById('<%= txtUsername.ClientID %>');
        if (u && !u.value) u.focus();
    });

    // Enter روی پسورد
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            var p = document.getElementById('<%= txtPassword.ClientID %>');
            if (document.activeElement === p) {
                e.preventDefault();
                var b = document.getElementById('<%= btnLogin.ClientID %>');
                if (b) b.click();
            }
        }
    });
</script>
</body>
</html>