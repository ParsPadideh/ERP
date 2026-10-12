<%@ Page Language="C#" AutoEventWireup="true" CodeFile="final-invoice-print.aspx.cs" Inherits="admin_final_invoice_print" %>

<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>
        <asp:Literal ID="litPageTitle" runat="server" Text="فاکتور نهایی" />
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
        .action-bar-controls {
            display: flex;
            align-items: center;
            gap: 12px;
        }
        .toggle-cost {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 10px;
            background: #fef3c7;
            border: 1px solid #fcd34d;
            border-radius: 8px;
            font-size: 12px;
            font-weight: 700;
            color: #78350f;
            cursor: pointer;
            user-select: none;
        }
        .toggle-cost input { cursor: pointer; }
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

        /* ─── Invoice ─── */
        .invoice {
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

        .invoice-watermark {
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
        .invoice > *:not(.invoice-watermark) {
            position: relative;
            z-index: 1;
        }

        /* ─── Header ─── */
        .invoice-header {
            display: flex;
            align-items: flex-start;
            gap: 16px;
            padding-bottom: 14px;
            border-bottom: 2px solid #1e40af;
            margin-bottom: 18px;
        }
        .invoice-logo {
            width: 62px;
            height: 62px;
            object-fit: contain;
            flex-shrink: 0;
        }
        .invoice-logo-placeholder {
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
        .invoice-header-info { flex: 1; min-width: 0; }
        .invoice-company-name {
            font-size: 17px;
            font-weight: 800;
            color: #1e3a8a;
            line-height: 1.3;
        }
        .invoice-company-tagline {
            font-size: 11px;
            color: #64748b;
            margin-top: 2px;
        }
        .invoice-company-meta {
            font-size: 10.5px;
            color: #475569;
            margin-top: 5px;
            line-height: 1.7;
        }

        /* ─── Title strip ─── */
        .invoice-title {
            background: linear-gradient(90deg, #eff6ff, #dbeafe);
            border: 1px solid #bfdbfe;
            border-radius: 10px;
            padding: 12px 16px;
            margin-bottom: 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
        }
        .invoice-title-text {
            font-size: 16px;
            font-weight: 800;
            color: #1e3a8a;
        }
        .invoice-title-badge {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 4px 10px;
            border-radius: 8px;
            font-size: 11.5px;
            font-weight: 800;
        }
        .badge-issued { background: #dbeafe; color: #1e40af; border: 1px solid #93c5fd; }
        .badge-paid { background: #d1fae5; color: #047857; border: 1px solid #6ee7b7; }
        .badge-cancelled { background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5; }

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
        .items-table th.cost-col { background: #fef3c7; color: #78350f; }
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
        .items-table td.cost-col {
            background: #fffbeb;
            font-family: 'Vazirmatn', monospace;
            direction: ltr;
            text-align: left;
            font-weight: 700;
        }
        .items-table tr:hover td { background: #f8fafc; }
        .items-table tr:hover td.cost-col { background: #fef3c7; }
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
        .totals-wrap {
            display: flex;
            justify-content: flex-start;
            gap: 16px;
            margin-bottom: 14px;
        }
        .totals-table {
            flex: 1;
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
        .totals-table tr.cost-row td {
            background: #fffbeb;
            color: #78350f;
        }
        .totals-table tr.profit-row td {
            background: #f0fdf4;
            color: #166534;
            font-weight: 800;
        }
        .totals-table tr.loss-row td {
            background: #fef2f2;
            color: #991b1b;
            font-weight: 800;
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
        .terms-box .term-label { color: #64748b; font-weight: 700; min-width: 110px; }
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
        .invoice-footer {
            margin-top: 20px;
            padding-top: 10px;
            border-top: 1px solid #e2e8f0;
            text-align: center;
            font-size: 10px;
            color: #94a3b8;
            line-height: 1.8;
        }

        /* ─── Cost visibility toggle ─── */
        .cost-col-hidden { display: none !important; }

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
            .invoice {
                width: 100%;
                max-width: 100%;
                padding: 0;
                border: none;
                border-radius: 0;
                box-shadow: none;
            }
            .invoice-watermark {
                color: rgba(15, 23, 42, 0.06);
                font-size: 70px;
            }
            .invoice-company-name { font-size: 14px; }
            .invoice-title-text { font-size: 13px; }
            .items-table th { font-size: 9.5px; padding: 5px 8px; }
            .items-table td { font-size: 10px; padding: 5px 8px; }
            .totals-table td { font-size: 10px; padding: 4px 8px; }
            .totals-table tr.grand td { font-size: 12px; padding: 8px; }
            .signature-row { margin-top: 20px; padding-top: 12px; }
            .signature-box { padding-top: 30px; font-size: 10px; }
        }

        /* ─── Error ─── */
        .error-state { text-align: center; padding: 60px 20px; }
        .error-icon {
            width: 60px; height: 60px; margin: 0 auto 16px;
            background: #fef2f2; border-radius: 50%;
            display: grid; place-items: center; color: #dc2626;
            font-size: 28px;
        }
        .error-title { font-size: 18px; font-weight: 800; color: #0f172a; margin-bottom: 6px; }
        .error-msg { font-size: 13px; color: #64748b; }
    </style>
</head>

<body>
<form id="mainForm" runat="server">

    <!-- ═══ Action Bar ═══ -->
    <div class="screen-wrap">
        <asp:Panel ID="pnlActionBar" runat="server">
            <div class="action-bar">
                <div class="action-bar-info">
                    <span>🧾</span>
                    <span>
                        فاکتور نهایی
                        <strong><asp:Literal ID="litBarNumber" runat="server" /></strong>
                    </span>
                </div>

                <div class="action-bar-controls">
                    <label class="toggle-cost" title="نمایش بهای تمام‌شده و سود — فقط برای استفاده داخلی">
                        <input type="checkbox" id="chkShowCost" onchange="toggleCostView(this);" />
                        <span>نمایش بهای تمام‌شده و سود (داخلی)</span>
                    </label>

                    <div class="action-buttons">
                        <a href="javascript:window.close();" class="btn btn-secondary">
                            ✕ بستن
                        </a>
                        <a href="javascript:window.print();" class="btn btn-primary">
                            🖨 چاپ فاکتور
                        </a>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </div>

    <!-- ═══ Invoice ═══ -->
    <div class="screen-wrap">

        <asp:Panel ID="pnlInvoice" runat="server">
            <div class="invoice">
                <div class="invoice-watermark">
                    <asp:Literal ID="litWatermark" runat="server" Text="فاکتور" />
                </div>

                <!-- ═══ Header ═══ -->
                <div class="invoice-header">
                    <asp:Image ID="imgLogo" runat="server" CssClass="invoice-logo" Visible="false" />
                    <asp:Panel ID="pnlLogoPlaceholder" runat="server" CssClass="invoice-logo-placeholder">
                        <asp:Literal ID="litLogoInitial" runat="server" Text="FF" />
                    </asp:Panel>

                    <div class="invoice-header-info">
                        <div class="invoice-company-name">
                            <asp:Literal ID="litCompanyName" runat="server" Text="کارخانه تولید و بسته‌بندی مواد غذایی" />
                        </div>
                        <div class="invoice-company-tagline">
                            <asp:Literal ID="litCompanyTagline" runat="server" />
                        </div>
                        <div class="invoice-company-meta">
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
                <div class="invoice-title">
                    <div class="invoice-title-text">
                        <asp:Literal ID="litReceiptTitle" runat="server" Text="فاکتور فروش" />
                    </div>
                    <div>
                        <asp:Literal ID="litStatusBadge" runat="server" />
                    </div>
                </div>

                <!-- ═══ Meta grid ═══ -->
                <div class="meta-grid">
                    <div class="meta-cell">
                        <span class="meta-label">شماره فاکتور</span>
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
                        <span class="meta-label">کد اقتصادی</span>
                        <span class="meta-value" style="direction:ltr; font-family:monospace;">
                            <asp:Literal ID="litEconomicCode" runat="server" Text="—" />
                        </span>
                    </div>
                    <div class="meta-cell last-row">
                        <span class="meta-label">شماره پیش‌فاکتور</span>
                        <span class="meta-value" style="direction:ltr; font-family:monospace;">
                            <asp:Literal ID="litProformaNumber" runat="server" Text="—" />
                        </span>
                    </div>
                </div>

                <!-- ═══ Items ═══ -->
                <div class="section-title">اقلام فاکتور</div>

                <table class="items-table" id="tblItems">
                    <thead>
                        <tr>
                            <th style="width:32px;">#</th>
                            <th>محصول</th>
                            <th class="center" style="width:70px;">تعداد</th>
                            <th class="center" style="width:100px;">قیمت واحد</th>
                            <th class="center" style="width:80px;">تخفیف</th>
                            <th class="center" style="width:80px;">مالیات</th>
                            <th class="center" style="width:110px;">جمع</th>
                            <th class="center cost-col cost-col-hidden" style="width:100px;">بهای تمام‌شده</th>
                            <th class="center cost-col cost-col-hidden" style="width:100px;">سود</th>
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
                                    <td class="cost-col cost-col-hidden amount" style="color:#b45309;">
                                        <%# FormatMoney(Eval("COGS")) %>
                                    </td>
                                    <td class="cost-col cost-col-hidden amount">
                                        <%# GetItemProfitCell(Eval("Total"), Eval("COGS")) %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>

                <asp:Panel ID="pnlNoItems" runat="server" Visible="false" style="text-align:center; padding:20px; color:#94a3b8; font-size:12px;">
                    اقلامی برای این فاکتور ثبت نشده است.
                </asp:Panel>

                <!-- ═══ Totals ═══ -->
                <asp:Panel ID="pnlTotals" runat="server" Visible="false">
                    <div class="totals-wrap">
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
                            <tr class="cost-row cost-col-hidden">
                                <td class="label">بهای تمام‌شده (COGS)</td>
                                <td class="value">
                                    <asp:Literal ID="litTotalCogs" runat="server" Text="۰" />
                                    <span style="font-weight:500;"> تومان</span>
                                </td>
                            </tr>
                            <tr id="trTotalProfit" runat="server" class="profit-row cost-col-hidden">
                                <td class="label">سود ناخالص</td>
                                <td class="value">
                                    <asp:Literal ID="litTotalProfit" runat="server" Text="۰" />
                                    <span style="font-weight:500;"> تومان</span>
                                </td>
                            </tr>
                        </table>
                    </div>
                </asp:Panel>

                <!-- ═══ Terms ═══ -->
                <asp:Panel ID="pnlTerms" runat="server" Visible="false">
                    <div class="terms-box">
                        <asp:Panel ID="pnlTermNotes" runat="server" Visible="false" CssClass="term-row">
                            <span class="term-label">یادداشت:</span>
                            <span class="term-value"><asp:Literal ID="litNotes" runat="server" /></span>
                        </asp:Panel>
                    </div>
                </asp:Panel>

                <!-- ═══ Signatures ═══ -->
                <div class="signature-row">
                    <div class="signature-box">مهر و امضای فروشنده</div>
                    <div class="signature-box">امضای خریدار</div>
                    <div class="signature-box">تاریخ و مهر شرکت</div>
                </div>

                <!-- ═══ Footer ═══ -->
                <div class="invoice-footer">
                    <div>
                        این فاکتور به‌عنوان سند رسمی فروش صادر شده است. در صورت هرگونه مغایرت، لطفاً با شماره تماس کارخانه هماهنگ فرمایید.
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
            <div class="invoice">
                <div class="error-state">
                    <div class="error-icon">⚠</div>
                    <div class="error-title">
                        <asp:Literal ID="litErrorTitle" runat="server" Text="فاکتور یافت نشد" />
                    </div>
                    <div class="error-msg">
                        <asp:Literal ID="litErrorMessage" runat="server" Text="فاکتور مورد نظر یافت نشد یا حذف شده است." />
                    </div>
                </div>
            </div>
        </asp:Panel>

    </div>

    <!-- ═══ JavaScript: Cost Toggle + Auto-print ═══ -->
    <script type="text/javascript">
        function toggleCostView(chk) {
            var costCols = document.querySelectorAll('.cost-col-hidden');
            for (var i = 0; i < costCols.length; i++) {
                if (chk.checked) {
                    costCols[i].classList.remove('cost-col-hidden');
                } else {
                    costCols[i].classList.add('cost-col-hidden');
                }
            }
        }

        // اگر ?showCost=1 باشد، خودکار چک‌باکس را تیک بزن
        document.addEventListener('DOMContentLoaded', function () {
            var params = window.location.search;
            if (params.indexOf('showCost=1') >= 0) {
                var chk = document.getElementById('chkShowCost');
                if (chk) { chk.checked = true; toggleCostView(chk); }
            }

            // اگر ?print=1 باشد، خودکار چاپ
            if (params.indexOf('print=1') >= 0) {
                setTimeout(function () { window.print(); }, 600);
            }
        });
    </script>

</form>
</body>
</html>