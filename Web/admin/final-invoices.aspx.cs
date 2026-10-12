using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_final_invoices : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private long CurrentUserId
    {
        get
        {
            if (Session["UserId"] == null) return 0;
            return Convert.ToInt64(Session["UserId"]);
        }
    }

    private int CurrentInvoiceId
    {
        get { int id; if (int.TryParse(hfCurrentInvoiceId.Value, out id)) return id; return 0; }
        set { hfCurrentInvoiceId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            LoadCustomerFilter();

            string idParam = Request.QueryString["id"];
            int viewId = 0;
            if (!string.IsNullOrEmpty(idParam)) int.TryParse(idParam, out viewId);

            if (viewId > 0)
                ShowDetailView(viewId);
            else
                ShowListView();
        }
    }

    // ─────────────────────────────────────────────────────
    // View Management
    // ─────────────────────────────────────────────────────
    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlDetailView.Visible = false;
        phBackToList.Visible = false;

        litPageTitle.Text = "فاکتورهای نهایی";
        litPageSubtitle.Text = "فاکتورهای نهایی فروش، بهای تمام‌شده، و سود را مدیریت کنید.";

        LoadStats();
        LoadInvoices();
    }

    private void ShowDetailView(int invoiceId)
    {
        CurrentInvoiceId = invoiceId;

        pnlListView.Visible = false;
        pnlDetailView.Visible = true;
        phBackToList.Visible = true;

        litPageTitle.Text = "جزئیات فاکتور نهایی";
        litPageSubtitle.Text = "مشاهده، چاپ، و مدیریت فاکتور نهایی.";

        LoadDetailHeader(invoiceId);
        LoadDetailItems(invoiceId);
        LoadDetailTotals(invoiceId);
        ApplyActionVisibility(invoiceId);
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        CurrentInvoiceId = 0;
        ShowListView();
    }

    // ─────────────────────────────────────────────────────
    // Dropdowns
    // ─────────────────────────────────────────────────────
    private void LoadCustomerFilter()
    {
        ddlFilterCustomer.Items.Clear();
        ddlFilterCustomer.Items.Add(new ListItem("همه مشتریان", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerId, CustomerName FROM Customers " +
                    "WHERE IsDeleted = 0 ORDER BY CustomerName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["CustomerName"]);
                        ddlFilterCustomer.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Stats
    // ─────────────────────────────────────────────────────
    private void LoadStats()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                DateTime monthStart = GetCurrentPersianMonthStart();
                DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

                string sql =
                    "SELECT " +
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Total, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND Status = 1 THEN 1 ELSE 0 END) AS IssuedCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM FinalInvoices";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatIssued.Text = HttpUtility.HtmlEncode(ToInt(dr["IssuedCount"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
                    }
                }

                // Paid this month
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM FinalInvoices " +
                    "WHERE Status = 2 AND IsDeleted = 0 " +
                    "  AND IssueDate >= @From AND IssueDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    int cnt = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatPaid.Text = HttpUtility.HtmlEncode(cnt.ToString("N0"));
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری آمار: " + ex.Message); }
    }

    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        return Convert.ToInt32(o);
    }

    // ─────────────────────────────────────────────────────
    // Load List
    // ─────────────────────────────────────────────────────
    private void LoadInvoices()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT fi.FinalInvoiceId, fi.InvoiceNumber, fi.IssueDate, fi.Status, ");
                sb.Append("  fi.Total, fi.PaidAmount, fi.OrderType, fi.Channel, fi.IsDeleted, ");
                sb.Append("  c.CustomerName, c.CustomerType, ");
                sb.Append("  ISNULL((SELECT SUM(fii.COGS) FROM FinalInvoiceItems fii ");
                sb.Append("          WHERE fii.FinalInvoiceId = fi.FinalInvoiceId), 0) AS TotalCogs ");
                sb.Append("FROM FinalInvoices fi ");
                sb.Append("INNER JOIN Customers c ON c.CustomerId = fi.CustomerId ");
                sb.Append("WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND fi.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (fi.InvoiceNumber LIKE @Search OR c.CustomerName LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string statusFilter = ddlFilterStatus.SelectedValue;
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        int st;
                        if (int.TryParse(statusFilter, out st) && st > 0)
                        {
                            sb.Append(" AND fi.Status = @Status ");
                            cmd.Parameters.AddWithValue("@Status", st);
                        }
                    }

                    string custFilter = ddlFilterCustomer.SelectedValue;
                    if (!string.IsNullOrEmpty(custFilter))
                    {
                        int cid;
                        if (int.TryParse(custFilter, out cid) && cid > 0)
                        {
                            sb.Append(" AND fi.CustomerId = @CId ");
                            cmd.Parameters.AddWithValue("@CId", cid);
                        }
                    }

                    sb.Append(" ORDER BY fi.IsDeleted ASC, fi.Status ASC, fi.FinalInvoiceId DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptInvoices.Visible = false;
                rptInvoices.DataSource = null;
                rptInvoices.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptInvoices.Visible = true;
                rptInvoices.DataSource = dt;
                rptInvoices.DataBind();
            }

            litListInfo.Text = string.Format("{0} فاکتور",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadInvoices(); }

    // ─────────────────────────────────────────────────────
    // Detail Header
    // ─────────────────────────────────────────────────────
    private void LoadDetailHeader(int invoiceId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT fi.InvoiceNumber, fi.IssueDate, fi.Status, fi.IsDeleted, " +
                    "  fi.Total, fi.PaidAmount, fi.OrderType, fi.Channel, fi.Notes, " +
                    "  fi.ProformaInvoiceId, " +
                    "  c.CustomerName, c.CustomerType, c.Mobile AS CustomerMobile, " +
                    "  pi.ProformaNumber, " +
                    "  ISNULL((SELECT SUM(fii.COGS) FROM FinalInvoiceItems fii " +
                    "          WHERE fii.FinalInvoiceId = fi.FinalInvoiceId), 0) AS TotalCogs " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN Customers c ON c.CustomerId = fi.CustomerId " +
                    "LEFT JOIN ProformaInvoices pi ON pi.ProformaInvoiceId = fi.ProformaInvoiceId " +
                    "WHERE fi.FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowMessage("فاکتور یافت نشد.");
                            ShowListView();
                            return;
                        }

                        int status = Convert.ToInt32(dr["Status"]);
                        bool isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                        string number = Convert.ToString(dr["InvoiceNumber"]);
                        string customerName = Convert.ToString(dr["CustomerName"]);
                        int customerType = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);
                        string mobile = dr["CustomerMobile"] == DBNull.Value ? "" : Convert.ToString(dr["CustomerMobile"]);

                        decimal total = ToDecimal(dr["Total"]);
                        decimal paidAmount = ToDecimal(dr["PaidAmount"]);
                        decimal totalCogs = ToDecimal(dr["TotalCogs"]);
                        decimal profit = total - totalCogs;

                        litDetailNumber.Text = HttpUtility.HtmlEncode(number);
                        litDetailCustomerInfo.Text = HttpUtility.HtmlEncode(
                            customerName + " — " + GetCustomerTypeLabelStatic(customerType) +
                            (mobile.Length > 0 ? " — " + mobile : ""));
                        litDetailTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));
                        litDetailIssueDate.Text = HttpUtility.HtmlEncode(FormatShamsiDateTime(ToDateTime(dr["IssueDate"])));
                        litDetailStatusBadge.Text = GetStatusBadge(status, isDeleted);
                        litDetailCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalCogs));
                        litDetailProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(profit));

                        // Banner style
                        string bannerClass = "state-banner state-banner--issued";
                        string iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-brand-100 text-brand-700";
                        string stateText = "در انتظار پرداخت";
                        string iconName = "receipt";

                        if (isDeleted)
                        {
                            bannerClass = "state-banner state-banner--cancelled";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-danger-50 text-danger-500";
                            stateText = "حذف‌شده";
                            iconName = "trash-2";
                        }
                        else if (status == 1)
                        {
                            bannerClass = "state-banner state-banner--issued";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-brand-100 text-brand-700";
                            stateText = "در انتظار پرداخت";
                            iconName = "clock";
                        }
                        else if (status == 2)
                        {
                            bannerClass = "state-banner state-banner--paid";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-emerald-100 text-emerald-700";
                            stateText = "پرداخت‌شده";
                            iconName = "check-circle-2";
                        }
                        else if (status == 3)
                        {
                            bannerClass = "state-banner state-banner--cancelled";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-danger-50 text-danger-500";
                            stateText = "لغو‌شده";
                            iconName = "x-circle";
                        }

                        divStateBanner.Attributes["class"] = bannerClass +
                            " mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card";
                        divStateIcon.Attributes["class"] = iconClass;
                        divStateIcon.InnerHtml = "<i class=\"h-6 w-6\" data-lucide=\"" + iconName + "\"></i>";
                        litDetailStateText.Text = stateText;

                        // Profit box color
                        if (profit < 0)
                        {
                            divProfitBox.Attributes["class"] = "rounded-base bg-danger-50 px-3 py-2";
                            pDetailProfit.Attributes["class"] = "mt-0.5 font-mono text-[13px] font-bold text-danger-700";
                        }
                        else
                        {
                            divProfitBox.Attributes["class"] = "rounded-base bg-emerald-50 px-3 py-2";
                            pDetailProfit.Attributes["class"] = "mt-0.5 font-mono text-[13px] font-bold text-emerald-700";
                        }

                        // Info
                        litInfoChannel.Text = HttpUtility.HtmlEncode(GetChannelLabelStatic(
                            dr["Channel"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Channel"])));
                        litInfoOrderType.Text = HttpUtility.HtmlEncode(GetCustomerTypeLabelStatic(customerType));

                        litInfoPaidAmount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(paidAmount) + " تومان");

                        decimal remaining = total - paidAmount;
                        if (remaining < 0) remaining = 0;
                        litInfoRemaining.Text = HttpUtility.HtmlEncode(FormatMoneyValue(remaining) + " تومان");

                        // Proforma link
                        if (dr["ProformaInvoiceId"] != DBNull.Value && dr["ProformaNumber"] != DBNull.Value)
                        {
                            int pfId = Convert.ToInt32(dr["ProformaInvoiceId"]);
                            string pfNum = Convert.ToString(dr["ProformaNumber"]);

                            pnlInfoProforma.Visible = true;
                            litInfoProformaNumber.Text = HttpUtility.HtmlEncode(pfNum);
                            aProformaLink.HRef = "proforma-invoices.aspx?id=" + pfId;

                            phProformaLink.Visible = true;
                            aProformaBack.HRef = "proforma-invoices.aspx?id=" + pfId;
                        }
                        else
                        {
                            pnlInfoProforma.Visible = false;
                            phProformaLink.Visible = false;
                        }

                        // Notes
                        string notes = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]).Trim();
                        if (notes.Length > 0)
                        {
                            pnlNotes.Visible = true;
                            litNotes.Text = HttpUtility.HtmlEncode(notes).Replace("\n", "<br />");
                        }
                        else
                        {
                            pnlNotes.Visible = false;
                        }

                        // Print link
                        lnkPrint.NavigateUrl = "final-invoice-print.aspx?id=" +
                            invoiceId.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Detail Items
    // ─────────────────────────────────────────────────────
    private void LoadDetailItems(int invoiceId)
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT fii.FinalInvoiceItemId, fii.ProductId, fii.Quantity, fii.UnitPrice, " +
                    "  fii.Discount, fii.Tax, fii.Total, fii.COGS, " +
                    "  p.ProductCode, p.ProductName " +
                    "FROM FinalInvoiceItems fii " +
                    "INNER JOIN Products p ON p.ProductId = fii.ProductId " +
                    "WHERE fii.FinalInvoiceId = @Id " +
                    "ORDER BY fii.FinalInvoiceItemId", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlDetailItemsEmpty.Visible = true;
                rptDetailItems.Visible = false;
                rptDetailItems.DataSource = null;
                rptDetailItems.DataBind();
            }
            else
            {
                pnlDetailItemsEmpty.Visible = false;
                rptDetailItems.Visible = true;
                rptDetailItems.DataSource = dt;
                rptDetailItems.DataBind();
            }

            litItemsInfo.Text = string.Format("{0} قلم",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری اقلام: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Detail Totals
    // ─────────────────────────────────────────────────────
    private void LoadDetailTotals(int invoiceId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Subtotal, Discount, Tax, Total " +
                    "FROM FinalInvoices WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            decimal subtotal = ToDecimal(dr["Subtotal"]);
                            decimal discount = ToDecimal(dr["Discount"]);
                            decimal tax = ToDecimal(dr["Tax"]);
                            decimal total = ToDecimal(dr["Total"]);

                            litSubtotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(subtotal));
                            litDiscount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(discount));
                            litTax.Text = HttpUtility.HtmlEncode(FormatMoneyValue(tax));
                            litTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));
                        }
                    }
                }

                // Total COGS
                decimal totalCogs = 0m;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(COGS), 0) FROM FinalInvoiceItems " +
                    "WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    object o = cmd.ExecuteScalar();
                    totalCogs = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Total
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

                // Color
                if (totalProfit < 0)
                {
                    divTotalProfitBox.Attributes["class"] =
                        "rounded-base bg-danger-100 border-2 border-danger-300 px-4 py-2 text-right";
                    pTotalProfit.Attributes["class"] =
                        "mt-0.5 font-mono text-[18px] font-black text-danger-800";
                }
                else
                {
                    divTotalProfitBox.Attributes["class"] =
                        "rounded-base bg-emerald-100 border-2 border-emerald-300 px-4 py-2 text-right";
                    pTotalProfit.Attributes["class"] =
                        "mt-0.5 font-mono text-[18px] font-black text-emerald-800";
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Apply Actions
    // ─────────────────────────────────────────────────────
    private void ApplyActionVisibility(int invoiceId)
    {
        try
        {
            int status = 0;
            bool isDeleted = false;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Status, IsDeleted FROM FinalInvoices WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            status = Convert.ToInt32(dr["Status"]);
                            isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                        }
                    }
                }
            }

            // Reset
            phActionMarkPaid.Visible = false;
            phActionCancel.Visible = false;
            phActionPrint.Visible = false;
            phActionDelete.Visible = false;

            if (isDeleted)
            {
                phActionPrint.Visible = true;
                return;
            }

            // Issued (1)
            if (status == 1)
            {
                phActionMarkPaid.Visible = true;
                phActionCancel.Visible = true;
                phActionPrint.Visible = true;
            }
            // Paid (2)
            else if (status == 2)
            {
                phActionPrint.Visible = true;
            }
            // Cancelled (3)
            else if (status == 3)
            {
                phActionPrint.Visible = true;
                phActionDelete.Visible = true;
            }
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════════
    // Action: Mark Paid
    // ═══════════════════════════════════════════════════════
    protected void btnMarkPaid_Click(object sender, EventArgs e)
    {
        try
        {
            int invoiceId = CurrentInvoiceId;
            if (invoiceId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                decimal total = 0m;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT InvoiceNumber, Status, Total FROM FinalInvoices " +
                    "WHERE FinalInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("فاکتور یافت نشد."); return; }
                        number = Convert.ToString(dr["InvoiceNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                        total = ToDecimal(dr["Total"]);
                    }
                }

                if (status != 1)
                {
                    ShowMessage("فقط فاکتورهای «در انتظار پرداخت» قابل تأیید هستند.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FinalInvoices SET Status = 2, PaidAmount = @Paid, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Paid", total);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "MarkPaid", "FinalInvoices", invoiceId, number, "تأیید پرداخت فاکتور");
            }

            ShowMessage("پرداخت تأیید شد.");
            ShowDetailView(invoiceId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Action: Cancel (with Stock Reversal)
    // ═══════════════════════════════════════════════════════
    protected void btnCancelInvoice_Click(object sender, EventArgs e)
    {
        try
        {
            int invoiceId = CurrentInvoiceId;
            if (invoiceId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT InvoiceNumber, Status FROM FinalInvoices " +
                    "WHERE FinalInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("فاکتور یافت نشد."); return; }
                        number = Convert.ToString(dr["InvoiceNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                    }
                }

                if (status != 1)
                {
                    ShowMessage("فقط فاکتورهای «در انتظار پرداخت» قابل لغو هستند.");
                    return;
                }

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ═══ بازگشت کالا به انبار ═══
                        // پیدا کردن StockMovements اصلی
                        DataTable movements = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT StockMovementId, WarehouseId, ProductId, Quantity " +
                            "FROM StockMovements " +
                            "WHERE ReferenceType = 'FinalInvoice' AND ReferenceId = @Id " +
                            "  AND MovementType = 3", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", invoiceId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(movements);
                        }

                        decimal totalReturned = 0m;

                        foreach (DataRow mv in movements.Rows)
                        {
                            int originalMovementId = Convert.ToInt32(mv["StockMovementId"]);
                            int whId = Convert.ToInt32(mv["WarehouseId"]);
                            int pid = Convert.ToInt32(mv["ProductId"]);
                            decimal qty = Convert.ToDecimal(mv["Quantity"]);

                            // بازگرداندن لایه‌های مصرف‌شده
                            DataTable issueLayers = new DataTable();
                            using (SqlCommand cmd = new SqlCommand(
                                "SELECT StockIssueLayerId, StockLayerId, QuantityUsed " +
                                "FROM StockIssueLayers WHERE StockMovementId = @MvId", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@MvId", originalMovementId);
                                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(issueLayers);
                            }

                            foreach (DataRow il in issueLayers.Rows)
                            {
                                int layerId = Convert.ToInt32(il["StockLayerId"]);
                                decimal usedQty = Convert.ToDecimal(il["QuantityUsed"]);

                                // بازگرداندن به لایه
                                using (SqlCommand cmd = new SqlCommand(
                                    "UPDATE StockLayers SET " +
                                    "  QuantityRemaining = QuantityRemaining + @Qty, " +
                                    "  IsClosed = 0 " +
                                    "WHERE StockLayerId = @Id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Qty", usedQty);
                                    cmd.Parameters.AddWithValue("@Id", layerId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            totalReturned += qty;
                        }

                        // ═══ درج StockMovement معکوس ═══
                        if (movements.Rows.Count > 0)
                        {
                            // میانگین قیمت‌ها را از اقلام می‌گیریم
                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO StockMovements " +
                                "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
                                "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
                                "SELECT sl.WarehouseId, sl.ProductId, 5, " +
                                "  (SELECT ISNULL(SUM(sl2.QuantityReceived), 0) FROM StockLayers sl2 " +
                                "   WHERE sl2.StockMovementId = sl.StockMovementId), " +
                                "  sl.UnitCost, sl.QuantityReceived * sl.UnitCost, " +
                                "  GETDATE(), 'FinalInvoiceReversal', @InvId, " +
                                "  @Notes, GETDATE(), @By " +
                                "FROM StockLayers sl " +
                                "INNER JOIN StockMovements sm ON sm.StockMovementId = sl.StockMovementId " +
                                "WHERE sm.ReferenceType = 'FinalInvoice' " +
                                "  AND sm.ReferenceId = @InvId " +
                                "  AND sm.MovementType = 3", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@InvId", invoiceId);
                                cmd.Parameters.AddWithValue("@Notes", "بازگشت کالا بابت لغو فاکتور " + number);
                                cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ═══ تغییر وضعیت ═══
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE FinalInvoices SET Status = 3, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE FinalInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", invoiceId);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Cancel", "FinalInvoices", invoiceId,
                            number, "لغو فاکتور و بازگشت کالا به انبار");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }

            ShowMessage("فاکتور لغو شد. کالاها به انبار بازگردانده شدند.");
            ShowDetailView(CurrentInvoiceId);
        }
        catch (Exception ex) { ShowMessage("خطا در لغو: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Action: Soft Delete (فقط Cancelled)
    // ═══════════════════════════════════════════════════════
    protected void btnSoftDelete_Click(object sender, EventArgs e)
    {
        try
        {
            int invoiceId = CurrentInvoiceId;
            if (invoiceId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT InvoiceNumber, Status FROM FinalInvoices " +
                    "WHERE FinalInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("فاکتور یافت نشد."); return; }
                        number = Convert.ToString(dr["InvoiceNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                    }
                }

                if (status != 3)
                {
                    ShowMessage("فقط فاکتورهای «لغو‌شده» قابل حذف هستند.\n\nابتدا فاکتور را لغو کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FinalInvoices SET IsDeleted = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "FinalInvoices", invoiceId, number, "حذف نرم فاکتور نهایی");
            }

            ShowMessage("فاکتور حذف شد (قابل بازیابی).");
            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Action: Restore
    // ═══════════════════════════════════════════════════════
    protected void rptInvoices_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "View") ShowDetailView(id);
        else if (e.CommandName == "Restore") RestoreInvoice(id);
    }

    private void RestoreInvoice(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string number = "";
                int status = 0;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT InvoiceNumber, Status FROM FinalInvoices " +
                    "WHERE FinalInvoiceId = @Id AND IsDeleted = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("فاکتور حذف‌شده یافت نشد."); return; }
                        number = Convert.ToString(dr["InvoiceNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                    }
                }

                // فقط اگر Cancelled باشد می‌توان بازیابی کرد
                if (status != 3)
                {
                    ShowMessage("فقط فاکتورهای «لغو‌شده» قابل بازیابی هستند.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FinalInvoices SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE FinalInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "FinalInvoices", id, number, "بازیابی فاکتور نهایی");
            }

            ShowMessage("فاکتور بازیابی شد.");
            LoadStats();
            LoadInvoices();
        }
        catch (Exception ex) { ShowMessage("خطا در بازیابی: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Audit
    // ─────────────────────────────────────────────────────
    private void WriteAudit(SqlConnection conn, string action, string entityName, int entityId,
        string targetName, string note)
    {
        WriteAuditTran(conn, null, action, entityName, entityId, targetName, note);
    }

    private void WriteAuditTran(SqlConnection conn, SqlTransaction tran, string action,
        string entityName, int entityId, string targetName, string note)
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())",
                conn, tran))
            {
                cmd.Parameters.AddWithValue("@UserId", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityName", entityName);
                cmd.Parameters.AddWithValue("@EntityId", entityId);
                string json = "{\"target\":\"" + Safe(targetName) + "\",\"note\":\"" + Safe(note) + "\"}";
                cmd.Parameters.AddWithValue("@NewValues", json);
                cmd.Parameters.AddWithValue("@IPAddress", GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
    }

    private string Safe(string s) { if (s == null) return ""; return s.Replace("\\", "\\\\").Replace("\"", "'"); }

    private string GetClientIP()
    {
        try
        {
            string fwd = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(fwd))
            {
                int comma = fwd.IndexOf(',');
                if (comma > 0) return fwd.Substring(0, comma).Trim();
                return fwd.Trim();
            }
            if (Request.UserHostAddress != null) return Request.UserHostAddress;
        }
        catch { }
        return "";
    }

    // ─────────────────────────────────────────────────────
    // Markup Helpers
    // ─────────────────────────────────────────────────────
    protected string Enc(object value)
    {
        if (value == null || value == DBNull.Value) return "";
        return HttpUtility.HtmlEncode(value.ToString());
    }

    protected bool IsTrue(object boolObj)
    {
        if (boolObj == null || boolObj == DBNull.Value) return false;
        return Convert.ToBoolean(boolObj);
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

    protected string FormatShamsiDate(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(value);
            PersianCalendar pc = new PersianCalendar();
            return HttpUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture,
                "{0}/{1:D2}/{2:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt)));
        }
        catch { return "—"; }
    }

    private string FormatShamsiDateTime(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2} — {3:D2}:{4:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt),
            dt.Hour, dt.Minute);
    }

    private string GetCustomerTypeLabelStatic(int type)
    {
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        if (type == 3) return "نمایندگی";
        if (type == 4) return "کشاورز";
        return "—";
    }

    private string GetChannelLabelStatic(int channel)
    {
        if (channel == 1) return "بازاریاب";
        if (channel == 2) return "آنلاین";
        if (channel == 3) return "ادمین / دفتری";
        return "—";
    }

    protected string GetCustomerTypeHint(object typeObj)
    {
        int t = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        string label = GetCustomerTypeLabelStatic(t);
        string css = "text-ink-400";
        if (t == 4) css = "text-amber-600";
        else if (t == 2) css = "text-brand-600";
        else if (t == 3) css = "text-violet-600";

        return "<p class=\"mt-0.5 text-[10.5px] " + css + "\">" +
            HttpUtility.HtmlEncode(label) + "</p>";
    }

    protected string GetRowCss(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1) return "border-b border-surface-line bg-brand-50/30 hover:bg-brand-50/50 transition";
        if (s == 2) return "border-b border-surface-line bg-emerald-50/30 transition";
        if (s == 3) return "border-b border-surface-line bg-danger-50/20 opacity-70";
        return "border-b border-surface-line transition hover:bg-surface-body/70";
    }

    protected string GetStatusBadge(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-brand-100 text-brand-800\"><i class=\"h-3 w-3\" data-lucide=\"clock\"></i> در انتظار پرداخت</span>";
        if (s == 2) return "<span class=\"badge bg-emerald-100 text-emerald-800\"><i class=\"h-3 w-3\" data-lucide=\"check-circle-2\"></i> پرداخت‌شده</span>";
        if (s == 3) return "<span class=\"badge bg-danger-50 text-danger-600\"><i class=\"h-3 w-3\" data-lucide=\"x-circle\"></i> لغو‌شده</span>";
        return "";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetProfitCell(object totalObj, object cogsObj)
    {
        try
        {
            decimal total = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
            decimal cogs = (cogsObj == null || cogsObj == DBNull.Value) ? 0m : Convert.ToDecimal(cogsObj);
            decimal profit = total - cogs;

            string css = profit >= 0 ? "text-emerald-700" : "text-danger-600";
            return "<span class=\"font-mono font-bold " + css + "\">" +
                HttpUtility.HtmlEncode(FormatMoneyValue(profit)) + "</span>";
        }
        catch { return "—"; }
    }

    protected string GetItemProfitBadge(object totalObj, object cogsObj)
    {
        try
        {
            decimal total = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
            decimal cogs = (cogsObj == null || cogsObj == DBNull.Value) ? 0m : Convert.ToDecimal(cogsObj);
            decimal profit = total - cogs;

            string css = "badge bg-emerald-50 text-emerald-700";
            if (profit < 0m) css = "badge bg-danger-50 text-danger-600";
            else if (total > 0m)
            {
                decimal margin = (profit / total) * 100m;
                if (margin >= 30m) css = "badge bg-emerald-100 text-emerald-800";
                else if (margin >= 15m) css = "badge bg-emerald-50 text-emerald-700";
                else if (margin >= 5m) css = "badge bg-amber-50 text-amber-700";
                else css = "badge bg-surface-muted text-ink-600";
            }

            return "<span class=\"" + css + " font-mono font-bold text-[11px]\">" +
                HttpUtility.HtmlEncode(FormatMoneyValue(profit)) + "</span>";
        }
        catch { return "—"; }
    }

    protected string GetRestoreConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('فاکتور «" + num + "» بازیابی شود؟');";
    }

    private decimal ToDecimal(object o)
    {
        if (o == null || o == DBNull.Value) return 0m;
        return Convert.ToDecimal(o);
    }

    private DateTime ToDateTime(object o)
    {
        if (o == null || o == DBNull.Value) return DateTime.Now;
        return Convert.ToDateTime(o);
    }

    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", "\\n");
        string script = "alert('" + safe + "');";
        if (ScriptManager.GetCurrent(this) != null)
            ScriptManager.RegisterStartupScript(this, GetType(), "msg_" + Guid.NewGuid().ToString("N"), script, true);
        else
            ClientScript.RegisterStartupScript(GetType(), "msg_" + Guid.NewGuid().ToString("N"), script, true);
    }
}