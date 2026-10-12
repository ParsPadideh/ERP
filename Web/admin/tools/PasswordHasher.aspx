<%@ Page Language="C#" AutoEventWireup="true" CodeFile="PasswordHasher.aspx.cs" Inherits="tools_PasswordHasher" %>

<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <title>تولید هش رمز عبور</title>
    <link href="https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css" rel="stylesheet" />
    <script src="https://cdn.tailwindcss.com"></script>
    <style>
        body { font-family: 'Vazirmatn', Tahoma, sans-serif; background: #0f172a; color: #e2e8f0; }
        .card { background: #1e293b; border: 1px solid #334155; border-radius: 16px; padding: 24px; }
        .field { background: #0f172a; border: 1.5px solid #334155; color: #e2e8f0;
                 padding: 12px 14px; border-radius: 10px; width: 100%; font-size: 14px; outline: none; }
        .field:focus { border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.2); }
        .btn { background: linear-gradient(135deg,#2563eb,#1d4ed8); color: #fff; padding: 12px 20px;
               border-radius: 10px; font-weight: 700; border: none; cursor: pointer;
               width: 100%; font-size: 14.5px; transition: all .2s; }
        .btn:hover { transform: translateY(-1px); box-shadow: 0 10px 25px -8px rgba(37,99,235,.6); }
        .output { background: #0f172a; border: 1px solid #334155; border-radius: 10px;
                  padding: 14px; font-family: Consolas, monospace; font-size: 12px;
                  color: #a5f3fc; word-break: break-all; direction: ltr; }
        label { font-size: 13px; font-weight: 700; color: #94a3b8; display: block; margin-bottom: 8px; }
        .warn { background: #7f1d1d; border: 1px solid #b91c1c; color: #fecaca;
                padding: 12px 14px; border-radius: 10px; font-size: 13px; font-weight: 600; }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <div class="min-h-screen flex items-center justify-center p-6">
        <div class="w-full max-w-2xl">
            <div class="mb-4 warn">
                ⚠️ این ابزار فقط برای راه‌اندازی اولیه یا بازیابی رمز است.
                بعد از استفاده، فایل آن را از سرور پاک کن.
            </div>

            <div class="card">
                <h1 class="text-2xl font-extrabold mb-6 text-white">🔐 تولید هش رمز عبور</h1>

                <div class="mb-5">
                    <label>رمز عبور خام</label>
                    <asp:TextBox ID="txtPassword" runat="server" CssClass="field" TextMode="SingleLine" />
                </div>

                <asp:Button ID="btnGenerate" runat="server" Text="تولید هش"
                    OnClick="btnGenerate_Click" CssClass="btn" />

                <asp:Panel ID="pnlResult" runat="server" Visible="false" CssClass="mt-6 space-y-4">
                    <div>
                        <label>Salt (Base64)</label>
                        <div class="output">
                            <asp:Literal ID="litSalt" runat="server" />
                        </div>
                    </div>
                    <div>
                        <label>Hash (SHA-256 + Salt)</label>
                        <div class="output">
                            <asp:Literal ID="litHash" runat="server" />
                        </div>
                    </div>

                    <div>
                        <label>اسکریپت SQL آماده (کپی و اجرا کن)</label>
                        <div class="output" style="color:#86efac; white-space:pre-wrap;">
<asp:Literal ID="litSql" runat="server" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>
</form>
</body>
</html>