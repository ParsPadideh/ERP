using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;

public partial class admin_service_receipt : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litPrintDate.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
            litPrintTime.Text = HttpUtility.HtmlEncode(DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));

            LoadCompanyInfo();

            int orderId = 0;
            string idParam = Request.QueryString["id"];
            if (!string.IsNullOrEmpty(idParam))
                int.TryParse(idParam, out orderId);

            if (orderId <= 0)
            {
                ShowError("شناسه سفارش معتبر نیست.", "لطفاً از طریق لیست سفارش‌ها اقدام کنید.");
                return;
            }

            LoadReceipt(orderId);
        }
    }

    // ─────────────────────────────────────────────────────
    // Company Info
    // ─────────────────────────────────────────────────────
    private void LoadCompanyInfo()
    {
        string companyName = ConfigurationManager.AppSettings["CompanyName"];
        if (string.IsNullOrEmpty(companyName))
            companyName = "کارخانه تولید و بسته‌بندی مواد غذایی";
        litCompanyName.Text = HttpUtility.HtmlEncode(companyName);

        // Initial for placeholder logo
        string initial = "";
        string[] parts = companyName.Split(' ');
        for (int i = 0; i < parts.Length && initial.Length < 2; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
                initial += parts[i].Substring(0, 1);
        }
        if (initial.Length == 0) initial = "FF";
        litLogoInitial.Text = HttpUtility.HtmlEncode(initial);

        litCompanyTagline.Text = HttpUtility.HtmlEncode(
            ConfigurationManager.AppSettings["CompanyTagline"] ?? "");

        // Meta line
        StringBuilder meta = new StringBuilder();
        string address = ConfigurationManager.AppSettings["CompanyAddress"];
        string phone = ConfigurationManager.AppSettings["CompanyPhone"];
        string email = ConfigurationManager.AppSettings["CompanyEmail"];

        if (!string.IsNullOrEmpty(address))
            meta.Append("📍 ").Append(address);
        if (!string.IsNullOrEmpty(phone))
        {
            if (meta.Length > 0) meta.Append(" &nbsp;|&nbsp; ");
            meta.Append("☎ ").Append(phone);
        }
        if (!string.IsNullOrEmpty(email))
        {
            if (meta.Length > 0) meta.Append(" &nbsp;|&nbsp; ");
            meta.Append("✉ ").Append(email);
        }
        litCompanyMeta.Text = meta.ToString();

        // Logo
        string logoUrl = ConfigurationManager.AppSettings["CompanyLogoUrl"];
        if (!string.IsNullOrEmpty(logoUrl))
        {
            try
            {
                string physicalPath = Server.MapPath(logoUrl);
                if (System.IO.File.Exists(physicalPath))
                {
                    imgLogo.ImageUrl = ResolveUrl(logoUrl);
                    imgLogo.Visible = true;
                    pnlLogoPlaceholder.Visible = false;
                }
            }
            catch { }
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Receipt
    // ─────────────────────────────────────────────────────
    private void LoadReceipt(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT so.ServiceOrderId, so.OrderNumber, so.ServiceDate, " +
                    "  so.InputWeight, so.OutputWeight, so.WasteWeight, so.WastePercent, " +
                    "  so.InputDescription, so.OutputDescription, " +
                    "  so.UnitPrice, so.Subtotal, so.Discount, so.Tax, so.Total, " +
                    "  so.PaymentMode, so.CashAmount, so.PaidAmount, so.PaymentStatus, " +
                    "  so.InKindWeight, so.InKindUnitPrice, so.InKindValue, " +
                    "  so.InKindProductId, so.InKindWarehouseId, " +
                    "  so.DeliveredBy, so.DeliveredByMobile, so.EstimatedDelivery, " +
                    "  so.Status, so.Notes, " +
                    "  c.CustomerName, c.Mobile AS CustomerMobile, c.CustomerType, " +
                    "  st.ServiceName, st.ServiceCode " +
                    "FROM ServiceOrders so " +
                    "INNER JOIN Customers c ON c.CustomerId = so.CustomerId " +
                    "INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId " +
                    "WHERE so.ServiceOrderId = @Id AND so.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowError("سفارش یافت نشد", "سفارش مورد نظر یافت نشد یا حذف شده است.");
                            return;
                        }

                        int status = dr["Status"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Status"]);
                        string orderNumber = Convert.ToString(dr["OrderNumber"]);

                        // Set page title
                        litPageTitle.Text = "رسید " + HttpUtility.HtmlEncode(orderNumber);
                        litBarOrderNumber.Text = HttpUtility.HtmlEncode(orderNumber);

                        // Watermark
                        litWatermark.Text = (status == 1)
                            ? "پذیرش موقت"
                            : (status == 3 ? "تکمیل‌شده" : "لغو‌شده");

                        // Title
                        if (status == 1)
                        {
                            litReceiptTitle.Text = "رسید پذیرش موقت";
                            litStatusBadge.Text = "<span class=\"receipt-title-badge badge-acceptance\">⏱ در انتظار تسویه</span>";
                        }
                        else if (status == 3)
                        {
                            litReceiptTitle.Text = "رسید ارائه خدمات";
                            litStatusBadge.Text = "<span class=\"receipt-title-badge badge-final\">✓ تسویه‌شده</span>";
                        }
                        else
                        {
                            litReceiptTitle.Text = "رسید خدمات";
                            litStatusBadge.Text = "<span class=\"receipt-title-badge badge-acceptance\">لغو‌شده</span>";
                        }

                        // Meta
                        litOrderNumber.Text = HttpUtility.HtmlEncode(orderNumber);
                        litServiceDate.Text = dr["ServiceDate"] == DBNull.Value
                            ? "—" : HttpUtility.HtmlEncode(ToShamsiDateString(Convert.ToDateTime(dr["ServiceDate"])));
                        litCustomerName.Text = HttpUtility.HtmlEncode(Convert.ToString(dr["CustomerName"]));

                        string mobile = dr["CustomerMobile"] == DBNull.Value
                            ? "" : Convert.ToString(dr["CustomerMobile"]);
                        litCustomerMobile.Text = mobile.Length > 0
                            ? HttpUtility.HtmlEncode(mobile)
                            : "—";

                        // Service
                        litServiceName.Text = HttpUtility.HtmlEncode(Convert.ToString(dr["ServiceName"]));

                        string inputDesc = dr["InputDescription"] == DBNull.Value
                            ? "" : Convert.ToString(dr["InputDescription"]).Trim();
                        if (inputDesc.Length > 0)
                        {
                            pnlInputDesc.Visible = true;
                            litInputDesc.Text = HttpUtility.HtmlEncode(inputDesc);
                        }

                        decimal inputWeight = dr["InputWeight"] == DBNull.Value
                            ? 0m : Convert.ToDecimal(dr["InputWeight"]);
                        litInputWeight.Text = HttpUtility.HtmlEncode(FormatQtyValue(inputWeight));

                        // Output section (only if final or has output)
                        decimal outputWeight = dr["OutputWeight"] == DBNull.Value
                            ? 0m : Convert.ToDecimal(dr["OutputWeight"]);
                        decimal wasteWeight = dr["WasteWeight"] == DBNull.Value
                            ? 0m : Convert.ToDecimal(dr["WasteWeight"]);
                        decimal wastePercent = dr["WastePercent"] == DBNull.Value
                            ? 0m : Convert.ToDecimal(dr["WastePercent"]);

                        if (status == 3 || outputWeight > 0)
                        {
                            pnlOutputSection.Visible = true;
                            litOutputWeight.Text = HttpUtility.HtmlEncode(FormatQtyValue(outputWeight));
                            litWasteWeight.Text = HttpUtility.HtmlEncode(FormatQtyValue(wasteWeight));
                            litWastePercent.Text = HttpUtility.HtmlEncode(
                                wastePercent.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪");

                            string outputDesc = dr["OutputDescription"] == DBNull.Value
                                ? "" : Convert.ToString(dr["OutputDescription"]).Trim();
                            if (outputDesc.Length > 0)
                            {
                                pnlOutputDesc.Visible = true;
                                litOutputDesc.Text = HttpUtility.HtmlEncode(outputDesc);
                            }
                        }

                        // Pricing section (only for final)
                        if (status == 3)
                        {
                            pnlPricingSection.Visible = true;

                            decimal unitPrice = dr["UnitPrice"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["UnitPrice"]);
                            decimal subtotal = dr["Subtotal"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["Subtotal"]);
                            decimal discount = dr["Discount"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["Discount"]);
                            decimal tax = dr["Tax"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["Tax"]);
                            decimal total = dr["Total"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["Total"]);

                            litUnitPrice.Text = HttpUtility.HtmlEncode(FormatMoneyValue(unitPrice));
                            litSubtotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(subtotal));

                            if (discount > 0m)
                            {
                                pnlDiscount.Visible = true;
                                litDiscount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(discount));
                            }
                            if (tax > 0m)
                            {
                                pnlTax.Visible = true;
                                litTax.Text = HttpUtility.HtmlEncode(FormatMoneyValue(tax));
                            }

                            pnlTotalBox.Visible = true;
                            litTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));

                            // Payment section
                            pnlPaymentSection.Visible = true;

                            int payMode = dr["PaymentMode"] == DBNull.Value
                                ? 1 : Convert.ToInt32(dr["PaymentMode"]);
                            decimal cashAmount = dr["CashAmount"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["CashAmount"]);
                            decimal paidAmount = dr["PaidAmount"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["PaidAmount"]);

                            if (payMode == 1)
                                litPaymentMode.Text = "نقدی";
                            else if (payMode == 2)
                                litPaymentMode.Text = "جنسی (محصول به‌جای دستمزد)";
                            else
                                litPaymentMode.Text = "ترکیبی (نقدی + جنسی)";

                            if (payMode == 1 || payMode == 3)
                            {
                                pnlCashDetails.Visible = true;
                                litCashAmount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(cashAmount));

                                string cashMethod = GetLastCashMethod(orderId);
                                if (!string.IsNullOrEmpty(cashMethod))
                                {
                                    pnlCashMethod.Visible = true;
                                    litCashMethod.Text = HttpUtility.HtmlEncode(cashMethod);
                                }
                            }

                            if (payMode == 2 || payMode == 3)
                            {
                                pnlInKindDetails.Visible = true;

                                decimal ikW = dr["InKindWeight"] == DBNull.Value
                                    ? 0m : Convert.ToDecimal(dr["InKindWeight"]);
                                decimal ikValue = dr["InKindValue"] == DBNull.Value
                                    ? 0m : Convert.ToDecimal(dr["InKindValue"]);

                                litInKindWeight.Text = HttpUtility.HtmlEncode(FormatQtyValue(ikW));
                                litInKindValue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ikValue));

                                // Product name
                                if (dr["InKindProductId"] != DBNull.Value)
                                {
                                    int pid = Convert.ToInt32(dr["InKindProductId"]);
                                    string pname = GetProductName(pid);
                                    litInKindProduct.Text = HttpUtility.HtmlEncode(pname);
                                }
                                else
                                {
                                    litInKindProduct.Text = "—";
                                }
                            }

                            // Settlement status
                            if (paidAmount >= total - 0.01m)
                            {
                                litSettlementStatus.Text = "✓ کاملاً تسویه شده";
                            }
                            else
                            {
                                decimal remaining = total - paidAmount;
                                if (remaining < 0) remaining = 0;
                                litSettlementStatus.Text = "مانده قابل پرداخت: " +
                                    HttpUtility.HtmlEncode(FormatMoneyValue(remaining)) + " تومان";
                            }
                        }

                        // Acceptance info box (only for pending)
                        if (status == 1)
                        {
                            pnlAcceptanceInfo.Visible = true;

                            if (dr["EstimatedDelivery"] != DBNull.Value)
                            {
                                DateTime est = Convert.ToDateTime(dr["EstimatedDelivery"]);
                                pnlEstimatedTime.Visible = true;
                                litEstimatedDelivery.Text = HttpUtility.HtmlEncode(
                                    ToShamsiDateString(est) + " — ساعت " +
                                    est.ToString("HH:mm", CultureInfo.InvariantCulture));
                            }
                        }

                        // Delivered by
                        string deliveredBy = dr["DeliveredBy"] == DBNull.Value
                            ? "" : Convert.ToString(dr["DeliveredBy"]).Trim();
                        if (deliveredBy.Length > 0)
                        {
                            pnlDeliveredBy.Visible = true;
                            litDeliveredBy.Text = HttpUtility.HtmlEncode(deliveredBy);

                            string dbMobile = dr["DeliveredByMobile"] == DBNull.Value
                                ? "" : Convert.ToString(dr["DeliveredByMobile"]).Trim();
                            if (dbMobile.Length > 0)
                            {
                                pnlDeliveredByMobile.Visible = true;
                                litDeliveredByMobile.Text = HttpUtility.HtmlEncode(dbMobile);
                            }
                        }

                        // Notes
                        string notes = dr["Notes"] == DBNull.Value
                            ? "" : Convert.ToString(dr["Notes"]).Trim();
                        if (notes.Length > 0)
                        {
                            pnlNotes.Visible = true;
                            litNotes.Text = HttpUtility.HtmlEncode(notes).Replace("\n", "<br />");
                        }

                        // QR data
                        pnlQrScript.Visible = true;
                        litQrData.Text = HttpUtility.HtmlEncode(orderNumber);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("خطا در بارگذاری رسید", ex.Message);
        }
    }

    private string GetLastCashMethod(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 PaymentMethod FROM ServiceOrderPayments " +
                    "WHERE ServiceOrderId = @Id AND PaymentMethod IS NOT NULL " +
                    "ORDER BY PaymentId DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
                }
            }
        }
        catch { return ""; }
    }

    private string GetProductName(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductName FROM Products WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? "—" : Convert.ToString(o);
                }
            }
        }
        catch { return "—"; }
    }

    // ─────────────────────────────────────────────────────
    // Error state
    // ─────────────────────────────────────────────────────
    private void ShowError(string title, string message)
    {
        pnlReceipt.Visible = false;
        pnlError.Visible = true;
        pnlActionBar.Visible = false;
        pnlQrScript.Visible = false;

        litErrorTitle.Text = HttpUtility.HtmlEncode(title);
        litErrorMessage.Text = HttpUtility.HtmlEncode(message);
    }

    // ─────────────────────────────────────────────────────
    // Format helpers
    // ─────────────────────────────────────────────────────
    private string FormatQtyValue(decimal d)
    {
        if (d == 0m) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        bool neg = d < 0m;
        decimal abs = Math.Abs(d);
        string s = abs.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return neg ? "(" + s + ")" : s;
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }
}