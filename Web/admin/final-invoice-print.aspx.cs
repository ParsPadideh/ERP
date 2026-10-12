using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_final_invoice_print : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litPrintDate.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
            litPrintDateFooter.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
            litPrintTime.Text = HttpUtility.HtmlEncode(DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));

            LoadCompanyInfo();

            int id = 0;
            string idParam = Request.QueryString["id"];
            if (!string.IsNullOrEmpty(idParam))
                int.TryParse(idParam, out id);

            if (id <= 0)
            {
                ShowError("شناسه نامعتبر", "شناسه فاکتور معتبر نیست.");
                return;
            }

            LoadInvoice(id);
        }
    }

    // ─────────────────────────────────────────────────────
    // Company
    // ─────────────────────────────────────────────────────
    private void LoadCompanyInfo()
    {
        string companyName = ConfigurationManager.AppSettings["CompanyName"];
        if (string.IsNullOrEmpty(companyName))
            companyName = "کارخانه تولید و بسته‌بندی مواد غذایی";
        litCompanyName.Text = HttpUtility.HtmlEncode(companyName);

        // Initial for placeholder
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

        StringBuilder meta = new StringBuilder();
        string address = ConfigurationManager.AppSettings["CompanyAddress"];
        string phone = ConfigurationManager.AppSettings["CompanyPhone"];
        string email = ConfigurationManager.AppSettings["CompanyEmail"];

        if (!string.IsNullOrEmpty(address)) meta.Append("📍 ").Append(address);
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
    // Load Invoice
    // ─────────────────────────────────────────────────────
    private void LoadInvoice(int invoiceId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ─── Header ───
                string sql =
                    "SELECT fi.FinalInvoiceId, fi.InvoiceNumber, fi.IssueDate, fi.Status, fi.IsDeleted, " +
                    "  fi.OrderType, fi.Channel, fi.Subtotal, fi.Discount, fi.Tax, fi.Total, " +
                    "  fi.PaidAmount, fi.Notes, fi.ProformaInvoiceId, " +
                    "  c.CustomerName, c.Mobile AS CustomerMobile, c.EconomicCode, " +
                    "  c.CustomerType, " +
                    "  pi.ProformaNumber " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN Customers c ON c.CustomerId = fi.CustomerId " +
                    "LEFT JOIN ProformaInvoices pi ON pi.ProformaInvoiceId = fi.ProformaInvoiceId " +
                    "WHERE fi.FinalInvoiceId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowError("فاکتور یافت نشد", "فاکتور مورد نظر یافت نشد.");
                            return;
                        }

                        string number = Convert.ToString(dr["InvoiceNumber"]);
                        int status = Convert.ToInt32(dr["Status"]);
                        bool isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);

                        litBarNumber.Text = HttpUtility.HtmlEncode(number);
                        litNumber.Text = HttpUtility.HtmlEncode(number);
                        litPageTitle.Text = "فاکتور نهایی " + HttpUtility.HtmlEncode(number);

                        // Watermark
                        litWatermark.Text = GetWatermarkText(status, isDeleted);

                        // Status badge
                        litStatusBadge.Text = GetStatusBadgeHtml(status, isDeleted);

                        // Meta
                        litIssueDate.Text = HttpUtility.HtmlEncode(
                            ToShamsiDateTime(Convert.ToDateTime(dr["IssueDate"])));

                        litCustomerName.Text = HttpUtility.HtmlEncode(Convert.ToString(dr["CustomerName"]));

                        string mobile = dr["CustomerMobile"] == DBNull.Value
                            ? "" : Convert.ToString(dr["CustomerMobile"]).Trim();
                        litCustomerMobile.Text = mobile.Length > 0
                            ? HttpUtility.HtmlEncode(mobile) : "—";

                        int customerType = dr["CustomerType"] == DBNull.Value
                            ? 0 : Convert.ToInt32(dr["CustomerType"]);
                        litCustomerType.Text = HttpUtility.HtmlEncode(GetCustomerTypeLabel(customerType));

                        int channel = dr["Channel"] == DBNull.Value
                            ? 1 : Convert.ToInt32(dr["Channel"]);
                        litChannel.Text = HttpUtility.HtmlEncode(GetChannelLabel(channel));

                        string econCode = dr["EconomicCode"] == DBNull.Value
                            ? "" : Convert.ToString(dr["EconomicCode"]).Trim();
                        litEconomicCode.Text = econCode.Length > 0
                            ? HttpUtility.HtmlEncode(econCode) : "—";

                        string proformaNum = dr["ProformaNumber"] == DBNull.Value
                            ? "" : Convert.ToString(dr["ProformaNumber"]).Trim();
                        litProformaNumber.Text = proformaNum.Length > 0
                            ? HttpUtility.HtmlEncode(proformaNum) : "—";

                        // Totals
                        decimal subtotal = dr["Subtotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Subtotal"]);
                        decimal discount = dr["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Discount"]);
                        decimal tax = dr["Tax"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Tax"]);
                        decimal total = dr["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Total"]);

                        if (subtotal > 0m || total > 0m)
                        {
                            pnlTotals.Visible = true;
                            litSubtotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(subtotal));
                            litDiscount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(discount));
                            litTax.Text = HttpUtility.HtmlEncode(FormatMoneyValue(tax));
                            litTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));
                        }
                        else
                        {
                            pnlTotals.Visible = false;
                        }

                        // Notes
                        string notes = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]).Trim();
                        if (notes.Length > 0)
                        {
                            pnlTerms.Visible = true;
                            pnlTermNotes.Visible = true;
                            litNotes.Text = HttpUtility.HtmlEncode(notes).Replace("\n", "<br />");
                        }
                        else
                        {
                            pnlTerms.Visible = false;
                            pnlTermNotes.Visible = false;
                        }
                    }
                }

                // ─── Items ───
                DataTable items = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT fii.FinalInvoiceItemId, fii.ProductId, fii.Quantity, " +
                    "       fii.UnitPrice, fii.Discount, fii.Tax, fii.Total, fii.COGS, " +
                    "       p.ProductCode, p.ProductName " +
                    "FROM FinalInvoiceItems fii " +
                    "INNER JOIN Products p ON p.ProductId = fii.ProductId " +
                    "WHERE fii.FinalInvoiceId = @Id " +
                    "ORDER BY fii.FinalInvoiceItemId", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(items);
                }

                // اضافه‌کردن شماره ردیف
                if (!items.Columns.Contains("RowNumber"))
                    items.Columns.Add("RowNumber", typeof(int));

                for (int i = 0; i < items.Rows.Count; i++)
                    items.Rows[i]["RowNumber"] = i + 1;

                if (items.Rows.Count == 0)
                {
                    pnlNoItems.Visible = true;
                    rptItems.Visible = false;
                    rptItems.DataSource = null;
                    rptItems.DataBind();
                }
                else
                {
                    pnlNoItems.Visible = false;
                    rptItems.Visible = true;
                    rptItems.DataSource = items;
                    rptItems.DataBind();
                }

                // ─── COGS + Profit ───
                decimal totalCogs = 0m;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(COGS), 0) FROM FinalInvoiceItems " +
                    "WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    object o = cmd.ExecuteScalar();
                    totalCogs = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                decimal totalAmount = 0m;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(Total, 0) FROM FinalInvoices WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    object o = cmd.ExecuteScalar();
                    totalAmount = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                decimal totalProfit = totalAmount - totalCogs;

                litTotalCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalCogs));
                litTotalProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalProfit));

                // رنگ سود
                if (totalProfit < 0m)
                {
                    trTotalProfit.Attributes["class"] = "loss-row cost-col-hidden";
                }
                else
                {
                    trTotalProfit.Attributes["class"] = "profit-row cost-col-hidden";
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("خطا", ex.Message);
        }
    }

    private string GetWatermarkText(int status, bool isDeleted)
    {
        if (isDeleted) return "حذف‌شده";
        if (status == 1) return "در انتظار پرداخت";
        if (status == 2) return "پرداخت‌شده";
        if (status == 3) return "لغو‌شده";
        return "فاکتور";
    }

    private string GetStatusBadgeHtml(int status, bool isDeleted)
    {
        if (isDeleted)
            return "<span class=\"invoice-title-badge badge-cancelled\">✕ حذف‌شده</span>";

        if (status == 1)
            return "<span class=\"invoice-title-badge badge-issued\">⏱ در انتظار پرداخت</span>";
        if (status == 2)
            return "<span class=\"invoice-title-badge badge-paid\">✓ پرداخت‌شده</span>";
        if (status == 3)
            return "<span class=\"invoice-title-badge badge-cancelled\">✕ لغو‌شده</span>";

        return "";
    }

    private string GetCustomerTypeLabel(int type)
    {
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        if (type == 3) return "نمایندگی";
        if (type == 4) return "کشاورز";
        return "—";
    }

    private string GetChannelLabel(int channel)
    {
        if (channel == 1) return "بازاریاب";
        if (channel == 2) return "آنلاین";
        if (channel == 3) return "ادمین / دفتری";
        return "—";
    }

    private void ShowError(string title, string message)
    {
        pnlInvoice.Visible = false;
        pnlError.Visible = true;
        pnlActionBar.Visible = false;

        litErrorTitle.Text = HttpUtility.HtmlEncode(title);
        litErrorMessage.Text = HttpUtility.HtmlEncode(message);
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    protected string Enc(object value)
    {
        if (value == null || value == DBNull.Value) return "";
        return HttpUtility.HtmlEncode(value.ToString());
    }

    protected string FormatQty(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "۰";
            if (d == Math.Floor(d))
                return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
            return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "۰"; }
    }

    protected string FormatMoney(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "۰";
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "۰"; }
    }

    protected string FormatMoneyOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "—";
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "—"; }
    }

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        bool neg = d < 0m;
        decimal abs = Math.Abs(d);
        string s = abs.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return neg ? "(" + s + ")" : s;
    }

    protected string GetItemProfitCell(object totalObj, object cogsObj)
    {
        try
        {
            decimal total = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
            decimal cogs = (cogsObj == null || cogsObj == DBNull.Value) ? 0m : Convert.ToDecimal(cogsObj);
            decimal profit = total - cogs;

            string color = profit >= 0m ? "#047857" : "#b91c1c";
            return "<span style=\"color:" + color + "; font-weight:800;\">" +
                HttpUtility.HtmlEncode(FormatMoneyValue(profit)) + "</span>";
        }
        catch { return "—"; }
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    private string ToShamsiDateTime(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2} — {3:D2}:{4:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt),
            dt.Hour, dt.Minute);
    }
}