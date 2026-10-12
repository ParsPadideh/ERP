<%@ Page Language="C#" AutoEventWireup="true" CodeFile="service-receipt.aspx.cs" Inherits="admin_service_receipt" %>

<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no" />
    <meta name="robots" content="noindex, nofollow" />
    <title>
        <asp:Literal ID="litPageTitle" runat="server" Text="رسید خدمات" />
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

        /* ─── Screen wrapper ─── */
        .screen-wrap {
            max-width: 720px;
            margin: 20px auto;
            padding: 0 12px;
        }

        /* ─── Action bar (no-print) ─── */
        .action-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            margin-bottom: 16px;
            padding: 12px 16px;
            background: #ffffff;
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
        .action-bar-buttons {
            display: flex;
            gap: 8px;
        }
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
        .btn-secondary {
            background: #fff;
            color: #334155;
            border: 1px solid #e2e8f0;
        }
        .btn-secondary:hover { background: #f8fafc; }

        /* ─── Receipt page (A5) ─── */
        .receipt {
            background: #ffffff;
            width: 100%;
            max-width: 720px;
            margin: 0 auto;
            padding: 28px 32px;
            border: 1px solid #e2e8f0;
            border-radius: 14px;
            box-shadow: 0 8px 32px -12px rgba(15, 23, 42, 0.12);
            position: relative;
            overflow: hidden;
        }

        /* Diagonal watermark */
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
        .receipt-header-info {
            flex: 1;
            min-width: 0;
        }
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
        .badge-acceptance {
            background: #fef3c7;
            color: #b45309;
            border: 1px solid #fcd34d;
        }
        .badge-final {
            background: #d1fae5;
            color: #047857;
            border: 1px solid #6ee7b7;
        }

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

        /* ─── Section ─── */
        .section {
            margin-bottom: 14px;
        }
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

        /* ─── Detail table ─── */
        .detail-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12.5px;
        }
        .detail-table td {
            padding: 7px 10px;
            border-bottom: 1px solid #f1f5f9;
        }
        .detail-table tr:last-child td { border-bottom: none; }
        .detail-table .lbl {
            color: #64748b;
            font-weight: 600;
            width: 45%;
        }
        .detail-table .val {
            color: #0f172a;
            font-weight: 700;
            text-align: left;
            font-family: 'Vazirmatn', monospace;
            direction: ltr;
        }
        .detail-table .val-fa {
            font-family: 'Vazirmatn', sans-serif;
            direction: rtl;
        }
        .amount-highlight {
            font-size: 15px;
            font-weight: 900;
            color: #059669;
        }
        .waste-highlight {
            color: #b45309;
            font-weight: 800;
        }

        /* ─── Summary box ─── */
        .summary-box {
            background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%);
            border: 2px solid #6ee7b7;
            border-radius: 12px;
            padding: 12px 16px;
            margin-bottom: 14px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
        }
        .summary-label {
            font-size: 12.5px;
            font-weight: 700;
            color: #047857;
        }
        .summary-value {
            font-size: 22px;
            font-weight: 900;
            color: #065f46;
            font-family: 'Vazirmatn', monospace;
            direction: ltr;
        }
        .summary-value small {
            font-size: 11px;
            font-weight: 400;
            color: #047857;
            direction: rtl;
        }

        /* ─── Acceptance info box ─── */
        .info-box {
            background: #fffbeb;
            border: 1.5px solid #fcd34d;
            border-radius: 10px;
            padding: 10px 14px;
            font-size: 11.5px;
            color: #78350f;
            margin-bottom: 14px;
            line-height: 1.8;
        }
        .info-box-title {
            font-weight: 800;
            color: #b45309;
            margin-bottom: 4px;
            font-size: 12px;
        }

        /* ─── QR + signature row ─── */
        .bottom-row {
            display: grid;
            grid-template-columns: 130px 1fr 1fr;
            gap: 12px;
            align-items: end;
            margin-top: 18px;
            padding-top: 14px;
            border-top: 1.5px solid #cbd5e1;
        }
        .qr-box {
            text-align: center;
            padding: 8px;
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
        }
        .qr-code {
            width: 100px;
            height: 100px;
            margin: 0 auto;
            background: #fff;
            display: grid;
            place-items: center;
        }
        .qr-caption {
            font-size: 9.5px;
            color: #64748b;
            margin-top: 5px;
            font-weight: 700;
        }

        .sign-box {
            text-align: center;
            padding-top: 42px;
            border-top: 1px dotted #94a3b8;
            font-size: 11px;
            color: #64748b;
            font-weight: 700;
        }

        /* ─── Footer ─── */
        .receipt-footer {
            margin-top: 16px;
            padding-top: 10px;
            border-top: 1px solid #e2e8f0;
            text-align: center;
            font-size: 10px;
            color: #94a3b8;
            line-height: 1.8;
        }
        .receipt-footer a {
            color: #2563eb;
            text-decoration: none;
        }

        /* ─── Print styles ─── */
        @media print {
            @page {
                size: A5 portrait;
                margin: 8mm 8mm 10mm 8mm;
            }
            html, body {
                background: #fff !important;
                font-size: 9.5px !important;
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
            .summary-value { font-size: 18px; }
            .amount-highlight { font-size: 13px; }
            .receipt-header { padding-bottom: 10px; margin-bottom: 12px; }
            .meta-cell { padding: 6px 10px; font-size: 11px; }
            .detail-table td { padding: 5px 8px; font-size: 11px; }
            .bottom-row { grid-template-columns: 120px 1fr 1fr; }
            .qr-code { width: 90px; height: 90px; }
        }

        @media (max-width: 640px) {
            .receipt { padding: 20px 18px; }
            .meta-grid { grid-template-columns: 1fr; }
            .meta-cell:nth-child(odd) { border-left: none; }
            .bottom-row { grid-template-columns: 1fr; }
            .sign-box { padding-top: 30px; }
        }

        /* ─── Error state ─── */
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

    <script src="https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js"></script>
</head>

<body>
<form id="mainForm" runat="server">

    <!-- ═══════ Action bar (screen only) ═══════ -->
    <div class="screen-wrap">
        <asp:Panel ID="pnlActionBar" runat="server">
            <div class="action-bar">
                <div class="action-bar-info">
                    <span>📄</span>
                    <span>
                        رسید
                        <strong><asp:Literal ID="litBarOrderNumber" runat="server" /></strong>
                    </span>
                </div>
                <div class="action-bar-buttons">
                    <a href="javascript:window.close();" class="btn btn-secondary">
                        ✕ بستن
                    </a>
                    <a href="javascript:window.print();" class="btn btn-primary">
                        🖨 چاپ رسید
                    </a>
                </div>
            </div>
        </asp:Panel>
    </div>

    <!-- ═══════ Receipt ═══════ -->
    <div class="screen-wrap">

        <asp:Panel ID="pnlReceipt" runat="server">
            <div class="receipt">
                <div class="receipt-watermark">
                    <asp:Literal ID="litWatermark" runat="server" Text="پذیرش" />
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
                </div>

                <!-- ═══ Title strip ═══ -->
                <div class="receipt-title">
                    <div class="receipt-title-text">
                        <asp:Literal ID="litReceiptTitle" runat="server" Text="رسید خدمات" />
                    </div>
                    <div>
                        <asp:Literal ID="litStatusBadge" runat="server" />
                    </div>
                </div>

                <!-- ═══ Meta grid ═══ -->
                <div class="meta-grid">
                    <div class="meta-cell">
                        <span class="meta-label">شماره رسید</span>
                        <span class="meta-value" style="direction: ltr; font-family: monospace;">
                            <asp:Literal ID="litOrderNumber" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell">
                        <span class="meta-label">تاریخ</span>
                        <span class="meta-value">
                            <asp:Literal ID="litServiceDate" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell last-row">
                        <span class="meta-label">مشتری</span>
                        <span class="meta-value">
                            <asp:Literal ID="litCustomerName" runat="server" />
                        </span>
                    </div>
                    <div class="meta-cell last-row">
                        <span class="meta-label">موبایل</span>
                        <span class="meta-value" style="direction: ltr; font-family: monospace;">
                            <asp:Literal ID="litCustomerMobile" runat="server" Text="—" />
                        </span>
                    </div>
                </div>

                <!-- ═══ Service section ═══ -->
                <div class="section">
                    <div class="section-title">مشخصات خدمت</div>
                    <table class="detail-table">
                        <tr>
                            <td class="lbl">نوع خدمت</td>
                            <td class="val val-fa">
                                <asp:Literal ID="litServiceName" runat="server" />
                            </td>
                        </tr>
                        <asp:Panel ID="pnlInputDesc" runat="server" Visible="false">
                            <tr>
                                <td class="lbl">مشخصات ورودی</td>
                                <td class="val val-fa">
                                    <asp:Literal ID="litInputDesc" runat="server" />
                                </td>
                            </tr>
                        </asp:Panel>
                        <tr>
                            <td class="lbl">وزن ورودی</td>
                            <td class="val">
                                <asp:Literal ID="litInputWeight" runat="server" />
                                <span style="direction: rtl; font-weight: 600; color: #64748b;"> کیلوگرم</span>
                            </td>
                        </tr>
                        <asp:Panel ID="pnlOutputSection" runat="server" Visible="false">
                            <asp:Panel ID="pnlOutputDesc" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">مشخصات خروجی</td>
                                    <td class="val val-fa">
                                        <asp:Literal ID="litOutputDesc" runat="server" />
                                    </td>
                                </tr>
                            </asp:Panel>
                            <tr>
                                <td class="lbl">وزن خروجی</td>
                                <td class="val">
                                    <asp:Literal ID="litOutputWeight" runat="server" />
                                    <span style="direction: rtl; font-weight: 600; color: #64748b;"> کیلوگرم</span>
                                </td>
                            </tr>
                            <tr>
                                <td class="lbl">ضایعات</td>
                                <td class="val">
                                    <span class="waste-highlight">
                                        <asp:Literal ID="litWasteWeight" runat="server" />
                                        <span style="direction: rtl; font-weight: 600;"> کیلوگرم</span>
                                        (<asp:Literal ID="litWastePercent" runat="server" />)
                                    </span>
                                </td>
                            </tr>
                        </asp:Panel>
                    </table>
                </div>

                <!-- ═══ Pricing (only for final) ═══ -->
                <asp:Panel ID="pnlPricingSection" runat="server" Visible="false">
                    <div class="section">
                        <div class="section-title">محاسبه هزینه</div>
                        <table class="detail-table">
                            <tr>
                                <td class="lbl">نرخ واحد</td>
                                <td class="val">
                                    <asp:Literal ID="litUnitPrice" runat="server" />
                                    <span style="direction: rtl; font-weight: 600; color: #64748b;"> تومان</span>
                                </td>
                            </tr>
                            <tr>
                                <td class="lbl">جمع جزء</td>
                                <td class="val">
                                    <asp:Literal ID="litSubtotal" runat="server" />
                                    <span style="direction: rtl; font-weight: 600; color: #64748b;"> تومان</span>
                                </td>
                            </tr>
                            <asp:Panel ID="pnlDiscount" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">تخفیف</td>
                                    <td class="val" style="color: #dc2626;">
                                        (<asp:Literal ID="litDiscount" runat="server" />)
                                        <span style="direction: rtl; font-weight: 600;"> تومان</span>
                                    </td>
                                </tr>
                            </asp:Panel>
                            <asp:Panel ID="pnlTax" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">مالیات</td>
                                    <td class="val">
                                        <asp:Literal ID="litTax" runat="server" />
                                        <span style="direction: rtl; font-weight: 600; color: #64748b;"> تومان</span>
                                    </td>
                                </tr>
                            </asp:Panel>
                        </table>
                    </div>
                </asp:Panel>

                <!-- ═══ Final total ═══ -->
                <asp:Panel ID="pnlTotalBox" runat="server" Visible="false">
                    <div class="summary-box">
                        <div class="summary-label">مبلغ قابل پرداخت</div>
                        <div class="summary-value">
                            <asp:Literal ID="litTotal" runat="server" />
                            <small>تومان</small>
                        </div>
                    </div>
                </asp:Panel>

                <!-- ═══ Payment details (only for final) ═══ -->
                <asp:Panel ID="pnlPaymentSection" runat="server" Visible="false">
                    <div class="section">
                        <div class="section-title">جزئیات پرداخت</div>
                        <table class="detail-table">
                            <tr>
                                <td class="lbl">حالت پرداخت</td>
                                <td class="val val-fa">
                                    <asp:Literal ID="litPaymentMode" runat="server" />
                                </td>
                            </tr>
                            <asp:Panel ID="pnlCashDetails" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">مبلغ نقدی دریافتی</td>
                                    <td class="val">
                                        <asp:Literal ID="litCashAmount" runat="server" />
                                        <span style="direction: rtl; font-weight: 600; color: #64748b;"> تومان</span>
                                    </td>
                                </tr>
                                <asp:Panel ID="pnlCashMethod" runat="server" Visible="false">
                                    <tr>
                                        <td class="lbl">روش پرداخت</td>
                                        <td class="val val-fa">
                                            <asp:Literal ID="litCashMethod" runat="server" />
                                        </td>
                                    </tr>
                                </asp:Panel>
                            </asp:Panel>
                            <asp:Panel ID="pnlInKindDetails" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">محصول دریافتی</td>
                                    <td class="val val-fa">
                                        <asp:Literal ID="litInKindProduct" runat="server" />
                                    </td>
                                </tr>
                                <tr>
                                    <td class="lbl">وزن محصول دریافتی</td>
                                    <td class="val">
                                        <asp:Literal ID="litInKindWeight" runat="server" />
                                        <span style="direction: rtl; font-weight: 600; color: #64748b;"> کیلوگرم</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="lbl">ارزش محصول دریافتی</td>
                                    <td class="val">
                                        <asp:Literal ID="litInKindValue" runat="server" />
                                        <span style="direction: rtl; font-weight: 600; color: #64748b;"> تومان</span>
                                    </td>
                                </tr>
                            </asp:Panel>
                        </table>
                    </div>

                    <!-- Settlement status -->
                    <asp:Panel ID="pnlSettlementStatus" runat="server">
                        <div class="summary-box" style="background: linear-gradient(135deg, #f0fdf4, #dcfce7); border-color: #86efac;">
                            <div class="summary-label" style="color: #166534;">وضعیت تسویه</div>
                            <div class="summary-value" style="font-size: 14px; color: #166534; font-family: 'Vazirmatn', sans-serif; direction: rtl;">
                                <asp:Literal ID="litSettlementStatus" runat="server" />
                            </div>
                        </div>
                    </asp:Panel>
                </asp:Panel>

                <!-- ═══ Acceptance info box (only for pending) ═══ -->
                <asp:Panel ID="pnlAcceptanceInfo" runat="server" Visible="false">
                    <div class="info-box">
                        <div class="info-box-title">⚠️ این رسید موقت است</div>
                        <div>
                            • مبلغ نهایی و وزن خروجی پس از اتمام عملیات مشخص می‌شود.
                        </div>
                        <asp:Panel ID="pnlEstimatedTime" runat="server" Visible="false">
                            <div>
                                • زمان تخمینی تحویل:
                                <strong style="font-family: monospace;">
                                    <asp:Literal ID="litEstimatedDelivery" runat="server" />
                                </strong>
                            </div>
                        </asp:Panel>
                        <div>
                            • لطفاً این رسید را تا زمان تسویه نهایی نزد خود نگه دارید.
                        </div>
                    </div>
                </asp:Panel>

                <!-- ═══ Delivered by (if exists) ═══ -->
                <asp:Panel ID="pnlDeliveredBy" runat="server" Visible="false">
                    <div class="section">
                        <div class="section-title">تحویل‌دهنده</div>
                        <table class="detail-table">
                            <tr>
                                <td class="lbl">نام تحویل‌دهنده</td>
                                <td class="val val-fa">
                                    <asp:Literal ID="litDeliveredBy" runat="server" />
                                </td>
                            </tr>
                            <asp:Panel ID="pnlDeliveredByMobile" runat="server" Visible="false">
                                <tr>
                                    <td class="lbl">موبایل</td>
                                    <td class="val" style="font-family: monospace;">
                                        <asp:Literal ID="litDeliveredByMobile" runat="server" />
                                    </td>
                                </tr>
                            </asp:Panel>
                        </table>
                    </div>
                </asp:Panel>

                <!-- ═══ Notes ═══ -->
                <asp:Panel ID="pnlNotes" runat="server" Visible="false">
                    <div class="section">
                        <div class="section-title">یادداشت</div>
                        <div style="font-size: 11.5px; color: #475569; line-height: 1.8; padding: 6px 4px;">
                            <asp:Literal ID="litNotes" runat="server" />
                        </div>
                    </div>
                </asp:Panel>

                <!-- ═══ Bottom row: QR + Signatures ═══ -->
                <div class="bottom-row">
                    <div class="qr-box">
                        <div id="qrcode" class="qr-code"></div>
                        <div class="qr-caption">اسکن برای پیگیری</div>
                    </div>

                    <div class="sign-box">
                        امضای مشتری
                    </div>

                    <div class="sign-box">
                        مهر و امضای کارخانه
                    </div>
                </div>

                <!-- ═══ Footer ═══ -->
                <div class="receipt-footer">
                    <div>
                        این رسید توسط سامانه ERP تولید شده است. در صورت هرگونه مغایرت، لطفاً با شماره تماس کارخانه هماهنگ فرمایید.
                    </div>
                    <div style="margin-top: 4px;">
                        تاریخ چاپ:
                        <span style="font-family: monospace;"><asp:Literal ID="litPrintDate" runat="server" /></span>
                        —
                        ساعت:
                        <span style="font-family: monospace;"><asp:Literal ID="litPrintTime" runat="server" /></span>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- ═══ Error state ═══ -->
        <asp:Panel ID="pnlError" runat="server" Visible="false">
            <div class="receipt">
                <div class="error-state">
                    <div class="error-icon">⚠</div>
                    <div class="error-title">
                        <asp:Literal ID="litErrorTitle" runat="server" Text="رسید یافت نشد" />
                    </div>
                    <div class="error-msg">
                        <asp:Literal ID="litErrorMessage" runat="server" Text="سفارش مورد نظر یافت نشد یا حذف شده است." />
                    </div>
                </div>
            </div>
        </asp:Panel>

    </div>

    <!-- ═══ QR Code Generation (client-side) ═══ -->
    <asp:Panel ID="pnlQrScript" runat="server" Visible="false">
        <script type="text/javascript">
            document.addEventListener('DOMContentLoaded', function () {
                var qrData = '<asp:Literal ID="litQrData" runat="server" />';
                var qrEl = document.getElementById('qrcode');
                if (qrEl && qrData) {
                    try {
                        new QRCode(qrEl, {
                            text: qrData,
                            width: 100,
                            height: 100,
                            colorDark: '#0f172a',
                            colorLight: '#ffffff',
                            correctLevel: QRCode.CorrectLevel.M
                        });
                    } catch (e) {
                        qrEl.innerHTML = '<div style="font-size:9px;color:#94a3b8;">QR</div>';
                    }
                }
            });

            // Auto-print option (add ?print=1 to URL)
            if (window.location.search.indexOf('print=1') >= 0) {
                window.addEventListener('load', function () {
                    setTimeout(function () { window.print(); }, 500);
                });
            }
        </script>
    </asp:Panel>

</form>
</body>
</html>