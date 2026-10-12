<%@ Page Language="C#" AutoEventWireup="true" CodeFile="proforma-print.aspx.cs" Inherits="admin_proforma_print" %>

<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>
        <asp:Literal ID="litPageTitle" runat="server" Text="پیش‌فاکتور" />
    </title>

    <link rel="preconnect" href="https://cdn.jsdelivr.net" />
    <link href="https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css" rel="stylesheet" />

    <style>
        * { box-sizing: border-box; }
        html, body {
            margin: 0;
            padding: 0;
            font-family: 'Vazirmatn', Tahoma, sans-serif;
            background: #f1f5f9;
            color: #0f172a;
            direction: rtl;
        }

        .screen-wrap {
            max-width: 820px;
            margin: 20px auto;
            padding: 0 12px;
        }

        /* ─── Action bar ─── */
        .action-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            margin-bottom: 16px;
            padding: 12px 16px;
            background: #fff;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            box-shadow: 0 2px 8px -2px rgba(15, 23, 42, 0.05);
        }
        .action-bar-info {
            font-size: 13px;
            color: #64748b;
            display: flex;
            align-items: center;
            gap: 6px;
        }
        .action-bar-info strong { color: #0f172a; }
        .action-buttons { display: flex; gap: 8px; }
        .btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            height: 40px;
            padding: 0 18px;
            border-radius: 10px;
            font-size: 13.5px;
            font-weight: 700;
            cursor: pointer;
            border: none;
            text-decoration: none;
            transition: all .15s;
        }
        .btn-primary { background: #2563eb; color: #fff; }
        .btn-primary:hover { background: #1d4ed8; }
        .btn-secondary { background: #fff; color: #334155; border: 1px solid #e2e8f0; }
        .btn-secondary:hover { background: #f8fafc; }

        /* ─── Receipt ─── */
        .receipt {
            background: #fff;
            width: 100%;
            max-width: 820px;
            margin: 0 auto;
            padding: 30px 36px;
            border: 1px solid #e2e8f0;
            border-radius: 14px;
            box-shadow: 0 8px 32px -12px rgba(15, 23, 42, 0.12);
            position: relative;
            overflow: hidden;
        }

        .receipt-watermark {
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-30deg);
            font-size: 90px;
            font-weight: 900;
            color: rgba(15, 23, 42, 0.035);
            white-space: nowrap;
            pointer-events: none;
            user-select: none;
            letter-spacing: 8px;
            z-index: 0;
        }
        .receipt > *:not(.receipt-watermark) {
            position: relative;
            z-index: 1;
        }

        /* ─── Header ─── */
        .receipt-header {
            display: flex;
            align-items: flex-start;
            gap: 16px;
            padding-bottom: 14px;
            border-bottom: 2px solid #1e40af;
            margin-bottom: 18px;
        }
        .receipt-logo {
            width: 62px;
            height: 62px;
            object-fit: contain;
            flex-shrink: 0;
        }
        .receipt-logo-placeholder {
            width: 62px;
            height: 62px;
            display: grid;
            place-items: center;
            background: linear-gradient(135deg, #2563eb, #1e40af);
            border-radius: 14px;
            color: #fff;
            font-size: 26px;
            font-weight: 900;
            flex-shrink: 0;
        }
        .receipt-header-info { flex: 1; min-width: 0; }
        .receipt-company-name {
            font-size: 17px;
            font-weight: 800;
            color: #1e3a8a;
            line-height: 1.3;
        }
        .receipt-company-tagline {
            font-size: 11px;
            color: #64748b;
            margin-top: 2px;
        }
        .receipt-company-meta {
            font-size: 10.5px;
            color: #475569;
            margin-top: 5px;
            line-height: 1.7;
        }

        /* ─── Title strip ─── */
        .receipt-title {
            background: linear-gradient(90deg, #eff6ff, #dbeafe);
            border: 1px solid #bfdbfe;
            border-radius: 10px;
            padding: 10px 16px;
            margin-bottom: 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
        }
        .receipt-title-text {
            font-size: 15px;
            font-weight: 800;
            color: #1e3a8a;
        }
        .receipt-title-badge {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 4px 10px;
            border-radius: 8px;
            font-size: 11.5px;
            font-weight: 800;
        }
        .badge-draft { background: #f1f5f9; color: #475569; border: 1px solid #cbd5e1; }
        .badge-issued { background: #fef3c7; color: #b45309; border: 1px solid #fcd34d; }
        .badge-paid { background: #dbeafe; color: #1e40af; border: 1px solid #93c5fd; }
        .badge-converted { background: #d1fae5; color: #047857; border: 1px solid #6ee7b7; }
        .badge-cancelled { background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5; }
        .badge-expired { background: #f3e8ff; color: #7e22ce; border: 1px solid #d8b4fe; }

        /* ─── Meta grid ─── */
        .meta-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 0;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            overflow: hidden;
            margin-bottom: 14px;
        }
        .meta-cell {
            padding: 8px 12px;
            border-bottom: 1px solid #e2e8f0;
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 8px;
            font-size: 12px;
        }
        .meta-cell:nth-child(odd) { border-left: 1px solid #e2e8f0; }
        .meta-cell.last-row { border-bottom: none; }
        .meta-label { color: #64748b; font-weight: 600; }
        .meta-value { color: #0f172a; font-weight: 700; }

        /* ─── Section title ─── */
        .section-title {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 12px;
            font-weight: 800;
            color: #1e3a8a;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
            padding-bottom: 5px;
            border-bottom: 1px dashed #cbd5e1;
        }
        .section-title::before {
            content: '';
            width: 4px;
            height: 14px;
            background: #2563eb;
            border-radius: 2px;
        }

        /* ─── Items table ─── */
        .items-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12px;
            margin-bottom: 14px;
        }
        .items-table th {
            background: #f8fafc;
            padding: 8px 10px;
            text-align: right;
            font-size: 11px;
            font-weight: 700;
            color: #475569;
            text-transform: uppercase;
            border-bottom: 2px solid #cbd5e1;
        }
        .items-table th.center { text-align: center; }
        .items-table td {
            padding: 8px 10px;
            border-bottom: 1px solid #f1f5f9;
            vertical-align: middle;
        }
        .items-table td.center { text-align: center; }
        .items-table td.amount {
            font-family: 'Vazirmatn', monospace;
            direction: ltr;
            text-align: left;
            font-weight: 700;
        }
        .items-table tr:hover td { background: #f8fafc; }
        .item-name { font-weight: 600; color: #0f172a; }
        .item-code {
            font-family: monospace;
            font-size: 10px;
            color: #94a3b8;
            direction: ltr;
            display: block;
            margin-top: 2px;
        }

        /* ─── Totals ─── */
        .totals-box {
            display: flex;
            justify-content: flex-start;
            gap: 0;
            margin-bottom: 14px;
        }
        .totals-table {
            width: 100%;
            max-width: 400px;
            border-collapse: collapse;
            font-size: 12.5px;
        }
        .totals-table td {
            padding: 7px 12px;
            border-bottom: 1px solid #f1f5f9;
        }
        .totals-table td.label {
            color: #64748b;
            font-weight: 600;
        }
        .totals-table td.value {
            font-family: 'Vazirmatn', monospace;
            direction: ltr;
            text-align: left;
            font-weight: 700;
            color: #0f172a;
        }
        .totals-table tr.grand {
            background: linear-gradient(90deg, #ecfdf5, #d1fae5);
            border-top: 2px solid #10b981;
            border-bottom: 2px solid #10b981;
        }
        .totals-table tr.grand td {
            font-size: 15px;
            font-weight: 900;
            color: #065f46;
            padding: 12px;
        }

        /* ─── Terms ─── */
        .terms-box {
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            padding: 10px 14px;
            font-size: 11.5px;
            color: #475569;
            margin-bottom: 14px;
            line-height: 1.9;
        }
        .terms-box .term-row { display: flex; gap: 8px; }
        .terms-box .term-label { color: #64748b; font-weight: 700; min-width: 100px; }
        .terms-box .term-value { color: #0f172a; font-weight: 600; }

        /* ─── Signature footer ─── */
        .signature-row {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 20px;
            margin-top: 30px;
            padding-top: 16px;
            border-top: 1px solid #cbd5e1;
        }
        .signature-box {
            text-align: center;
            padding-top: 40px;
            border-top: 1px dotted #94a3b8;
            font-size: 11px;
            color: #64748b;
            font-weight: 700;
        }

        /* ─── Footer ─── */
        .receipt-footer {
            margin-top: 20px;
            padding-top: 10px;
            border-top: 1px solid #e2e8f0;
            text-align: center;
            font-size: 10px;
            color: #94a3b8;
            line-height: 1.8;
        }

        /* ═══ Print ═══ */
        @media print {
            @page {
                size: A4 portrait;
                margin: 12mm 10mm 15mm 10mm;
            }
            html, body {
                background: #fff !important;
                font-size: 10.5px !important;
            }
            .screen-wrap {
                max-width: 100%;
                margin: 0;
                padding: 0;
            }
            .action-bar { display: none !important; }
            .receipt {
                width: 100%;
                max-width: 100%;
                padding: 0;
                border: none;
                border-radius: 0;
                box-shadow: none;
            }
            .receipt-watermark {
                color: rgba(15, 23, 42, 0.06);
                font-size: 70px;
            }
            .receipt-company-name { font-size: 14px; }
            .receipt-title-text { font-size: 13px; }
            .items-table th { font-size: 9.5px; padding: 5px 8px; }
            .items-table td { font-size: 10px; padding: 5px 8px; }
            .totals-table td { font-size: 10px; padding: 4px 8px; }
            .totals-table tr.grand td { font-size: 12px; padding: 8px; }
            .signature-row { margin-top: 20px; padding-top: 12px; }
            .signature-box { padding-top: 30px; font-size: 10px; }
        }

        /* ─── Error ─── */
        .error-state {
            text-align: center;
            padding: 60px 20px;
        }
        .error-icon {
            width: 60px;
            height: 60px;
            margin: 0 auto 16px;
            background: #fef2f2;
            border-radius: 50%;
            display: grid;
            place-items: center;
            color: #dc2626;
            font-size: 28px;
        }
        .error-title {
            font-size: 18px;
            font-weight: 800;
            color: #0f172a;
            margin-bottom: 6px;
        }
        .error-msg {
            font-size: 13px;
            color: #64748b;
        }
    </style>
</head>

<body>
<form id="mainForm" runat="server">

    <!-- ═══ Action Bar ═══ -->
    <div class="screen-wrap">
        <asp:Panel ID="pnlActionBar" runat="server">
            <div class="action-bar">
                <div class="action-bar-info">
                    <span>📄</span>
                    <span>
                        پیش‌فاکتور
                        <strong><asp:Literal ID="litBarNumber" runat="server" /></strong>
                    </span>
                </div>
                <div class="action-buttons">
                    <a href="javascript:window.close();" class="btn btn-secondary">
                        ✕ بستن
                    </a>
                    <a href="javascript:window.print();" class="btn btn-primary">
                        🖨 چاپ
                    </a>
                </div>
            </div>
        </asp:Panel>
    </div>

    <!-- ═══ Receipt ═══ -->
    <div class="screen-wrap">

        <asp:Panel ID="pnlReceipt" runat="server">
            <div class="receipt">
                <div class="receipt-watermark">
                    <asp:Literal ID="litWatermark" runat="server" Text="پیش‌فاکتور" />
                </div>

                <!-- ═══ Header ═══ -->
                <div class="receipt-header">
                    <asp:Image ID="imgLogo" runat="server" CssClass="receipt-logo" Visible="false" />
                    <asp:Panel ID="pnlLogoPlaceholder" runat="server" CssClass="receipt-logo-placeholder">
                        <asp:Literal ID="litLogoInitial" runat="server" Text="FF" />
                    </asp:Panel>

                    <div class="receipt-header-info">
                        <div class="receipt-company-name">
                            <asp:Literal ID="litCompanyName" runat="server" Text="کارخانه تولید و بسته‌بندی مواد غذایی" />
                        </div>
                        <div class="receipt-company-tagline">
                            <asp:Literal ID="litCompanyTagline" runat="server" />
                        </div>
                        <div class="receipt-company-meta">
                            <asp:Literal ID="litCompanyMeta" runat="server" />
                        </div>
                    </div>

                    <div style="text-align:left; font-size:10px; color:#555;">
                        <div>تاریخ چاپ:</div>
                        <div style="font-weight:700; margin-top:2px;">
                            <asp:Literal ID="litPrintDate" runat="server" />
                        </div>
                    </div>
                </div>

                <!-- ═══ Title strip ═══ -->
                <div class="receipt-title">
                    <div class="receipt-title-text">
                        <asp:Literal ID="litReceiptTitle" runat="server" Text="پیش‌فاکتور فروش" />
                    </div>
                    <div>
                        <asp:Literal ID="litStatusBadge" runat="server" />
                    </div>
                </div>

                <!-- ═══ Meta grid ═══ -->
                <div class="meta-grid">
                    <div class="meta-cell">
                        <span class="meta-label">شماره پیش‌فاکتور</span>
                        <span class="meta-value" style="direction:ltr; font-family:monospace;">
                            <asp:Literal ID="litNumber" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">تاریخ صدور</span>
                        <span class="meta-value">
                            <asp:Literal ID="litIssueDate" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">مشتری</span>
                        <span class="meta-value">
                            <asp:Literal ID="litCustomerName" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">موبایل</span>
                        <span class="meta-value" style="direction:ltr; font-family:monospace;">
                            <asp:Literal ID="litCustomerMobile" runat="server" Text="—" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">نوع مشتری</span>
                        <span class="meta-value">
                            <asp:Literal ID="litCustomerType" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">کانال فروش</span>
                        <span class="meta-value">
                            <asp:Literal ID="litChannel" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell last-row">
                        <span class="meta-label">معتبر تا</span>
                        <span class="meta-value" style="color:#b45309;">
                            <asp:Literal ID="litValidUntil" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell last-row">
                        <span class="meta-label">کد اقتصادی مشتری</span>
                        <span class="meta-value" style="direction:ltr; font-family:monospace;">
                            <asp:Literal ID="litEconomicCode" runat="server" Text="—" />
                        </span>
                    </div>
                </div>

                <!-- ═══ Items ═══ -->
                <div class="section-title">اقلام پیش‌فاکتور</div>

                <table class="items-table">
                    <thead>
                        <tr>
                            <th style="width:32px;">#</th>
                            <th>محصول</th>
                            <th class="center" style="width:70px;">تعداد</th>
                            <th class="center" style="width:100px;">قیمت واحد</th>
                            <th class="center" style="width:90px;">تخفیف</th>
                            <th class="center" style="width:90px;">مالیات</th>
                            <th class="center" style="width:110px;">جمع</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="rptItems" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td class="center" style="color:#94a3b8; font-weight:700;">
                                        <%# Eval("RowNumber") %>
                                    </td>
                                    <td>
                                        <span class="item-name"><%# Enc(Eval("ProductName")) %></span>
                                        <span class="item-code"><%# Enc(Eval("ProductCode")) %></span>
                                    </td>
                                    <td class="center" style="font-family:monospace; font-weight:700;">
                                        <%# FormatQty(Eval("Quantity")) %>
                                    </td>
                                    <td class="amount"><%# FormatMoney(Eval("UnitPrice")) %></td>
                                    <td class="amount" style="color:#dc2626;">
                                        <%# FormatMoneyOrDash(Eval("Discount")) %>
                                    </td>
                                    <td class="amount" style="color:#64748b;">
                                        <%# FormatMoneyOrDash(Eval("Tax")) %>
                                    </td>
                                    <td class="amount" style="color:#047857; font-weight:800;">
                                        <%# FormatMoney(Eval("Total")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>

                <asp:Panel ID="pnlNoItems" runat="server" Visible="false" CssClass="py-6 text-center" style="text-align:center; padding:20px; color:#94a3b8; font-size:12px;">
                    اقلامی برای این پیش‌فاکتور ثبت نشده است.
                </asp:Panel>

                <!-- ═══ Totals ═══ -->
                <asp:Panel ID="pnlTotals" runat="server" Visible="false">
                    <div class="totals-box">
                        <table class="totals-table">
                            <tr>
                                <td class="label">جمع جزء</td>
                                <td class="value">
                                    <asp:Literal ID="litSubtotal" runat="server" Text="۰" />
                                    <span style="font-weight:500; color:#94a3b8;"> تومان</span>
                                </td>
                            </tr>
                            <tr>
                                <td class="label">تخفیف</td>
                                <td class="value" style="color:#dc2626;">
                                    (<asp:Literal ID="litDiscount" runat="server" Text="۰" />)
                                    <span style="font-weight:500; color:#94a3b8;"> تومان</span>
                                </td>
                            </tr>
                            <tr>
                                <td class="label">مالیات</td>
                                <td class="value">
                                    <asp:Literal ID="litTax" runat="server" Text="۰" />
                                    <span style="font-weight:500; color:#94a3b8;"> تومان</span>
                                </td>
                            </tr>
                            <tr class="grand">
                                <td class="label" style="color:#065f46 !important;">مبلغ کل قابل پرداخت</td>
                                <td class="value" style="color:#065f46 !important;">
                                    <asp:Literal ID="litTotal" runat="server" Text="۰" />
                                    <span style="font-weight:500;"> تومان</span>
                                </td>
                            </tr>
                        </table>
                    </div>
                </asp:Panel>

                <!-- ═══ Terms ═══ -->
                <asp:Panel ID="pnlTerms" runat="server" Visible="false">
                    <div class="terms-box">
                        <asp:Panel ID="pnlTermPayment" runat="server" Visible="false" CssClass="term-row">
                            <span class="term-label">شرایط پرداخت:</span>
                            <span class="term-value"><asp:Literal ID="litPaymentTerms" runat="server" /></span>
                        </asp:Panel>
                        <asp:Panel ID="pnlTermDelivery" runat="server" Visible="false" CssClass="term-row">
                            <span class="term-label">شرایط تحویل:</span>
                            <span class="term-value"><asp:Literal ID="litDeliveryTerms" runat="server" /></span>
                        </asp:Panel>
                        <asp:Panel ID="pnlTermNotes" runat="server" Visible="false" CssClass="term-row">
                            <span class="term-label">یادداشت:</span>
                            <span class="term-value"><asp:Literal ID="litNotes" runat="server" /></span>
                        </asp:Panel>
                    </div>
                </asp:Panel>

                <!-- ═══ Signatures ═══ -->
                <div class="signature-row">
                    <div class="signature-box">مهر و امضای فروشنده</div>
                    <div class="signature-box">امضای مشتری</div>
                    <div class="signature-box">تاریخ و مهر شرکت</div>
                </div>

                <!-- ═══ Footer ═══ -->
                <div class="receipt-footer">
                    <div>
                        این پیش‌فاکتور به‌منزله سند قطعی فروش نیست و تا زمان تبدیل به فاکتور نهایی، اعتبار دارد.
                    </div>
                    <div style="margin-top:4px;">
                        تاریخ چاپ:
                        <span style="font-family:monospace;"><asp:Literal ID="litPrintDateFooter" runat="server" /></span>
                        —
                        ساعت:
                        <span style="font-family:monospace;"><asp:Literal ID="litPrintTime" runat="server" /></span>
                    </div>
                </div>

            </div>
        </asp:Panel>

        <!-- ═══ Error ═══ -->
        <asp:Panel ID="pnlError" runat="server" Visible="false">
            <div class="receipt">
                <div class="error-state">
                    <div class="error-icon">⚠</div>
                    <div class="error-title">
                        <asp:Literal ID="litErrorTitle" runat="server" Text="پیش‌فاکتور یافت نشد" />
                    </div>
                    <div class="error-msg">
                        <asp:Literal ID="litErrorMessage" runat="server" Text="پیش‌فاکتور مورد نظر یافت نشد یا حذف شده است." />
                    </div>
                </div>
            </div>
        </asp:Panel>

    </div>

    <!-- ═══ Auto-print ═══ -->
    <asp:Panel ID="pnlAutoPrint" runat="server" Visible="false">
        <script type="text/javascript">
            if (window.location.search.indexOf('print=1') >= 0) {
                window.addEventListener('load', function () {
                    setTimeout(function () { window.print(); }, 600);
                });
            }
        </script>
    </asp:Panel>

</form>
</body>
</html>