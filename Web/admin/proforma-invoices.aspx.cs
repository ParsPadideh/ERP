using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_proforma_invoices : System.Web.UI.Page
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

    private int CurrentProformaId
    {
        get { int id; if (int.TryParse(hfCurrentProformaId.Value, out id)) return id; return 0; }
        set { hfCurrentProformaId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditItemId
    {
        get { int id; if (int.TryParse(hfEditItemId.Value, out id)) return id; return 0; }
        set { hfEditItemId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditHeaderId
    {
        get { int id; if (int.TryParse(hfEditHeaderId.Value, out id)) return id; return 0; }
        set { hfEditHeaderId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            AutoExpireProformas();
            LoadCustomersDropdown();
            LoadCustomerFilter();
            LoadProductsDropdown();
            ShowListView();
        }
    }

    // ═══════════════════════════════════════════════════════
    // Auto-Expire
    // ═══════════════════════════════════════════════════════
    private void AutoExpireProformas()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        DataTable expired = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT ProformaInvoiceId FROM ProformaInvoices " +
                            "WHERE Status IN (2, 3) AND ValidUntil < GETDATE() AND IsDeleted = 0",
                            conn, tran))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(expired);
                        }

                        foreach (DataRow r in expired.Rows)
                        {
                            int pid = Convert.ToInt32(r["ProformaInvoiceId"]);

                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE StockReservations SET Status = 3 " +
                                "WHERE ReservedForType = 'ProformaInvoice' " +
                                "  AND ReservedForId = @Id AND Status = 1", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@Id", pid);
                                cmd.ExecuteNonQuery();
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE ProformaInvoices SET Status = 6, UpdatedAt = GETDATE() " +
                                "WHERE ProformaInvoiceId = @Id", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@Id", pid);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                    catch { tran.Rollback(); }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // View Management
    // ─────────────────────────────────────────────────────
    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlDetailView.Visible = false;
        phBackToList.Visible = false;
        phAddNew.Visible = true;
        phAddItem.Visible = false;

        litPageTitle.Text = "پیش‌فاکتورها";
        litPageSubtitle.Text = "پیش‌فاکتورها را ثبت، صادر، و مدیریت کنید. صدور، موجودی را رزرو می‌کند.";

        LoadStats();
        LoadProformas();
    }

    private void ShowDetailView(int proformaId)
    {
        CurrentProformaId = proformaId;

        pnlListView.Visible = false;
        pnlDetailView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        litPageTitle.Text = "جزئیات پیش‌فاکتور";
        litPageSubtitle.Text = "مشاهده، ویرایش، و مدیریت پیش‌فاکتور.";

        LoadDetailHeader(proformaId);
        LoadItems(proformaId);
        LoadTotals(proformaId);
        ApplyActionVisibility(proformaId);
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
                string sql =
                    "SELECT " +
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Total, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND Status = 1 THEN 1 ELSE 0 END) AS DraftCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND Status = 2 THEN 1 ELSE 0 END) AS IssuedCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND Status = 4 THEN 1 ELSE 0 END) AS ConvertedCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM ProformaInvoices";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatDraft.Text = HttpUtility.HtmlEncode(ToInt(dr["DraftCount"]).ToString("N0"));
                        litStatIssued.Text = HttpUtility.HtmlEncode(ToInt(dr["IssuedCount"]).ToString("N0"));
                        litStatConverted.Text = HttpUtility.HtmlEncode(ToInt(dr["ConvertedCount"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
                    }
                }
            }
        }
        catch { }
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        return Convert.ToInt32(o);
    }

    // ─────────────────────────────────────────────────────
    // Dropdowns
    // ─────────────────────────────────────────────────────
    private void LoadCustomersDropdown()
    {
        ddlCustomer.Items.Clear();
        ddlCustomer.Items.Add(new ListItem("", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerId, CustomerCode, CustomerName, CustomerType, " +
                    "       ISNULL(City, '') AS City, ISNULL(Mobile, '') AS Mobile " +
                    "FROM Customers WHERE IsDeleted = 0 AND IsActive = 1 " +
                    "ORDER BY CustomerName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["CustomerCode"]);
                        string name = Convert.ToString(dr["CustomerName"]);
                        int type = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);
                        string city = Convert.ToString(dr["City"]);
                        string mobile = Convert.ToString(dr["Mobile"]);

                        StringBuilder label = new StringBuilder();
                        label.Append(name);
                        if (code.Length > 0) label.Append(" — ").Append(code);
                        label.Append(" — ").Append(GetCustomerTypeLabelStatic(type));
                        if (city.Length > 0) label.Append(" — ").Append(city);
                        if (mobile.Length > 0) label.Append(" — ").Append(mobile);

                        ddlCustomer.Items.Add(new ListItem(label.ToString(), id));
                    }
                }
            }
        }
        catch { }
    }

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
                    "SELECT CustomerId, CustomerName, CustomerCode FROM Customers " +
                    "WHERE IsDeleted = 0 ORDER BY CustomerName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["CustomerName"]);
                        string code = Convert.ToString(dr["CustomerCode"]);
                        ddlFilterCustomer.Items.Add(new ListItem(name + " (" + code + ")", id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadProductsDropdown()
    {
        ddlItemProduct.Items.Clear();
        ddlItemProduct.Items.Add(new ListItem("", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, " +
                    "       u.UnitName, ISNULL(pc.CategoryName, '') AS CategoryName " +
                    "FROM Products p " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "LEFT JOIN ProductCategories pc ON pc.CategoryId = p.CategoryId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND u.IsDeleted = 0 " +
                    "  AND p.ProductType = 2 " +
                    "ORDER BY p.ProductName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["ProductCode"]);
                        string name = Convert.ToString(dr["ProductName"]);
                        string unit = dr["UnitName"] == DBNull.Value ? "" : Convert.ToString(dr["UnitName"]);
                        string category = dr["CategoryName"] == DBNull.Value ? "" : Convert.ToString(dr["CategoryName"]);

                        // ═══ Label: کد — نام — دسته — واحد ═══
                        StringBuilder label = new StringBuilder();
                        label.Append(code);
                        label.Append(" — ");
                        label.Append(name);
                        if (category.Length > 0) label.Append(" — ").Append(category);
                        if (unit.Length > 0) label.Append(" (").Append(unit).Append(")");

                        ddlItemProduct.Items.Add(new ListItem(label.ToString(), id));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Load List
    // ─────────────────────────────────────────────────────
    private void LoadProformas()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT p.ProformaInvoiceId, p.ProformaNumber, p.CustomerId, ");
                sb.Append("  p.OrderType, p.Channel, p.IssueDate, p.ValidUntil, p.Status, ");
                sb.Append("  p.Total, p.IsDeleted, ");
                sb.Append("  c.CustomerName, c.CustomerType, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM ProformaInvoiceItems pii ");
                sb.Append("          WHERE pii.ProformaInvoiceId = p.ProformaInvoiceId), 0) AS ItemCount ");
                sb.Append("FROM ProformaInvoices p ");
                sb.Append("INNER JOIN Customers c ON c.CustomerId = p.CustomerId ");
                sb.Append("WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND p.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (p.ProformaNumber LIKE @Search OR c.CustomerName LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string statusFilter = ddlFilterStatus.SelectedValue;
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        int st;
                        if (int.TryParse(statusFilter, out st) && st > 0)
                        {
                            sb.Append(" AND p.Status = @Status ");
                            cmd.Parameters.AddWithValue("@Status", st);
                        }
                    }

                    string custFilter = ddlFilterCustomer.SelectedValue;
                    if (!string.IsNullOrEmpty(custFilter))
                    {
                        int cid;
                        if (int.TryParse(custFilter, out cid) && cid > 0)
                        {
                            sb.Append(" AND p.CustomerId = @CId ");
                            cmd.Parameters.AddWithValue("@CId", cid);
                        }
                    }

                    sb.Append(" ORDER BY p.IsDeleted ASC, p.Status ASC, p.ProformaInvoiceId DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptProformas.Visible = false;
                rptProformas.DataSource = null;
                rptProformas.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptProformas.Visible = true;
                rptProformas.DataSource = dt;
                rptProformas.DataBind();
            }

            litListInfo.Text = string.Format("{0} پیش‌فاکتور",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadProformas(); }

    // ─────────────────────────────────────────────────────
    // Load Detail Header
    // ─────────────────────────────────────────────────────
    private void LoadDetailHeader(int proformaId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT p.ProformaNumber, p.CustomerId, p.OrderType, p.Channel, " +
                    "  p.IssueDate, p.ValidUntil, p.Status, p.Subtotal, p.Discount, p.Tax, p.Total, " +
                    "  p.PaymentTerms, p.DeliveryTerms, p.Notes, p.CreatedAt, p.IsDeleted, " +
                    "  c.CustomerName, c.CustomerType, c.Mobile AS CustomerMobile " +
                    "FROM ProformaInvoices p " +
                    "INNER JOIN Customers c ON c.CustomerId = p.CustomerId " +
                    "WHERE p.ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowMessage("پیش‌فاکتور یافت نشد.");
                            ShowListView();
                            return;
                        }

                        int status = Convert.ToInt32(dr["Status"]);
                        bool isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                        string number = Convert.ToString(dr["ProformaNumber"]);
                        string customerName = Convert.ToString(dr["CustomerName"]);
                        int customerType = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);
                        string customerMobile = dr["CustomerMobile"] == DBNull.Value ? "" : Convert.ToString(dr["CustomerMobile"]);

                        litDetailNumber.Text = HttpUtility.HtmlEncode(number);
                        litDetailCustomerInfo.Text = HttpUtility.HtmlEncode(
                            customerName + " — " + GetCustomerTypeLabelStatic(customerType) +
                            (customerMobile.Length > 0 ? " — " + customerMobile : ""));

                        litDetailTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dr["Total"])));
                        litDetailIssueDate.Text = HttpUtility.HtmlEncode(FormatShamsiDateTime(ToDateTime(dr["IssueDate"])));
                        litDetailValidUntil.Text = HttpUtility.HtmlEncode(FormatShamsiDateTime(ToDateTime(dr["ValidUntil"])));
                        litDetailStatusBadge.Text = GetStatusBadge(status, isDeleted);

                        // Banner
                        string bannerClass = "state-banner state-banner--draft";
                        string iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-surface-muted text-ink-600";
                        string stateText = "پیش‌نویس";
                        string iconName = "file-edit";

                        if (isDeleted)
                        {
                            bannerClass = "state-banner state-banner--cancelled";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-danger-50 text-danger-500";
                            stateText = "حذف‌شده"; iconName = "trash-2";
                        }
                        else if (status == 1) { stateText = "پیش‌نویس — قابل ویرایش"; iconName = "file-edit"; }
                        else if (status == 2)
                        {
                            bannerClass = "state-banner state-banner--issued";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-amber-100 text-amber-700";
                            stateText = "صادرشده — رزرو فعال"; iconName = "clock";
                        }
                        else if (status == 3)
                        {
                            bannerClass = "state-banner state-banner--paid";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-brand-100 text-brand-700";
                            stateText = "پرداخت‌شده — آماده تبدیل"; iconName = "badge-check";
                        }
                        else if (status == 4)
                        {
                            bannerClass = "state-banner state-banner--converted";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-emerald-100 text-emerald-700";
                            stateText = "تبدیل‌شده به فاکتور نهایی"; iconName = "check-circle-2";
                        }
                        else if (status == 5)
                        {
                            bannerClass = "state-banner state-banner--cancelled";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-danger-50 text-danger-500";
                            stateText = "لغو‌شده"; iconName = "x-circle";
                        }
                        else if (status == 6)
                        {
                            bannerClass = "state-banner state-banner--expired";
                            iconClass = "grid h-12 w-12 place-items-center rounded-xl bg-violet-100 text-violet-700";
                            stateText = "منقضی‌شده"; iconName = "clock-alert";
                        }

                        divStateBanner.Attributes["class"] = bannerClass +
                            " mb-5 rounded-card border border-surface-line bg-surface-card p-5 shadow-card";
                        divStateIcon.Attributes["class"] = iconClass;
                        divStateIcon.InnerHtml = "<i class=\"h-6 w-6\" data-lucide=\"" + iconName + "\"></i>";
                        litDetailStateText.Text = stateText;

                        bool canEditHeader = (status == 1 && !isDeleted);
                        pnlHeaderForm.Visible = canEditHeader;
                        pnlHeaderSummary.Visible = !canEditHeader;

                        if (canEditHeader)
                        {
                            ddlCustomer.ClearSelection();
                            if (dr["CustomerId"] != DBNull.Value)
                            {
                                string cid = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem ci = ddlCustomer.Items.FindByValue(cid);
                                if (ci != null) ci.Selected = true;
                            }

                            int channel = dr["Channel"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Channel"]);
                            ddlChannel.ClearSelection();
                            ListItem chItem = ddlChannel.Items.FindByValue(channel.ToString(CultureInfo.InvariantCulture));
                            if (chItem != null) chItem.Selected = true;

                            DateTime issueDt = ToDateTime(dr["IssueDate"]);
                            DateTime validDt = ToDateTime(dr["ValidUntil"]);
                            int days = Math.Max(1, (int)Math.Round((validDt - issueDt).TotalDays));
                            txtValidityDays.Text = days.ToString(CultureInfo.InvariantCulture);

                            txtPaymentTerms.Text = dr["PaymentTerms"] == DBNull.Value ? "" : Convert.ToString(dr["PaymentTerms"]);
                            txtDeliveryTerms.Text = dr["DeliveryTerms"] == DBNull.Value ? "" : Convert.ToString(dr["DeliveryTerms"]);
                            txtHeaderNotes.Text = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);

                            UpdateCustomerHint();
                        }
                        else
                        {
                            int channel = dr["Channel"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Channel"]);
                            litSumChannel.Text = HttpUtility.HtmlEncode(GetChannelLabelStatic(channel));
                            litSumOrderType.Text = HttpUtility.HtmlEncode(GetCustomerTypeLabelStatic(customerType));

                            DateTime issueDt = ToDateTime(dr["IssueDate"]);
                            DateTime validDt = ToDateTime(dr["ValidUntil"]);
                            int days = Math.Max(1, (int)Math.Round((validDt - issueDt).TotalDays));
                            litSumValidity.Text = HttpUtility.HtmlEncode(days.ToString(CultureInfo.InvariantCulture) + " روز");

                            litSumPaymentTerms.Text = dr["PaymentTerms"] == DBNull.Value ? "—" : HttpUtility.HtmlEncode(Convert.ToString(dr["PaymentTerms"]));
                            litSumDeliveryTerms.Text = dr["DeliveryTerms"] == DBNull.Value ? "—" : HttpUtility.HtmlEncode(Convert.ToString(dr["DeliveryTerms"]));
                            litSumCreatedAt.Text = HttpUtility.HtmlEncode(FormatShamsiDateTime(ToDateTime(dr["CreatedAt"])));

                            string notes = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]).Trim();
                            if (notes.Length > 0)
                            {
                                pnlSumNotes.Visible = true;
                                litSumNotes.Text = HttpUtility.HtmlEncode(notes).Replace("\n", "<br />");
                            }
                            else pnlSumNotes.Visible = false;
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Apply Action Visibility
    // ─────────────────────────────────────────────────────
    private void ApplyActionVisibility(int proformaId)
    {
        try
        {
            int status = 0;
            bool isDeleted = false;
            int finalInvoiceId = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Status, IsDeleted, ISNULL(FinalInvoiceId, 0) AS FId " +
                    "FROM ProformaInvoices WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            status = Convert.ToInt32(dr["Status"]);
                            isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                            finalInvoiceId = Convert.ToInt32(dr["FId"]);
                        }
                    }
                }
            }

            phActionSaveHeader.Visible = false;
            phActionIssue.Visible = false;
            phActionConvert.Visible = false;
            phActionCancel.Visible = false;
            phActionPrint.Visible = false;
            phActionDelete.Visible = false;
            phFinalInvoiceLink.Visible = false;

            if (isDeleted) { phActionPrint.Visible = true; return; }

            if (status == 1)
            {
                phActionSaveHeader.Visible = true;
                phActionIssue.Visible = true;
                phActionCancel.Visible = true;
                phActionDelete.Visible = true;
                phAddItem.Visible = true;
            }
            else if (status == 2)
            {
                phActionCancel.Visible = true;
                phActionPrint.Visible = true;
            }
            else if (status == 3)
            {
                phActionConvert.Visible = true;
                phActionCancel.Visible = true;
                phActionPrint.Visible = true;
            }
            else if (status == 4)
            {
                phActionPrint.Visible = true;
                if (finalInvoiceId > 0)
                {
                    phFinalInvoiceLink.Visible = true;
                    aFinalInvoiceLink.HRef = "final-invoices.aspx?id=" + finalInvoiceId;
                }
            }
            else if (status == 5 || status == 6)
            {
                phActionPrint.Visible = true;
                phActionDelete.Visible = true;
            }

            // Print link
            lnkPrint.NavigateUrl = "proforma-print.aspx?id=" + proformaId.ToString(CultureInfo.InvariantCulture);
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Load Items
    // ─────────────────────────────────────────────────────
    private void LoadItems(int proformaId)
    {
        try
        {
            int status = GetCurrentStatus();
            bool isDeleted = IsCurrentDeleted();

            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT pii.ProformaInvoiceItemId, pii.ProductId, pii.Quantity, " +
                    "       pii.UnitPrice, pii.Discount, pii.Tax, pii.Total, " +
                    "       p.ProductCode, p.ProductName, " +
                    "       ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "               WHERE sl.ProductId = pii.ProductId AND sl.IsClosed = 0 " +
                    "                 AND sl.QuantityRemaining > 0), 0) AS Available " +
                    "FROM ProformaInvoiceItems pii " +
                    "INNER JOIN Products p ON p.ProductId = pii.ProductId " +
                    "WHERE pii.ProformaInvoiceId = @Id " +
                    "ORDER BY pii.ProformaInvoiceItemId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (!dt.Columns.Contains("InvoiceStatus")) dt.Columns.Add("InvoiceStatus", typeof(int));
            if (!dt.Columns.Contains("IsDeleted")) dt.Columns.Add("IsDeleted", typeof(bool));

            foreach (DataRow row in dt.Rows)
            {
                row["InvoiceStatus"] = status;
                row["IsDeleted"] = isDeleted;
            }

            if (dt.Rows.Count == 0)
            {
                pnlItemsEmpty.Visible = true;
                rptItems.Visible = false;
                rptItems.DataSource = null;
                rptItems.DataBind();
            }
            else
            {
                pnlItemsEmpty.Visible = false;
                rptItems.Visible = true;
                rptItems.DataSource = dt;
                rptItems.DataBind();
            }

            litItemsInfo.Text = string.Format("{0} قلم", dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری اقلام: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Load Totals
    // ─────────────────────────────────────────────────────
    private void LoadTotals(int proformaId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Subtotal, Discount, Tax, Total FROM ProformaInvoices " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            decimal subtotal = ToDecimal(dr["Subtotal"]);
                            decimal discount = ToDecimal(dr["Discount"]);
                            decimal tax = ToDecimal(dr["Tax"]);
                            decimal total = ToDecimal(dr["Total"]);

                            if (subtotal > 0m || total > 0m)
                            {
                                pnlTotals.Visible = true;
                                litSubtotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(subtotal));
                                litDiscount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(discount));
                                litTax.Text = HttpUtility.HtmlEncode(FormatMoneyValue(tax));
                                litTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));
                            }
                            else pnlTotals.Visible = false;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private int GetCurrentStatus()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Status FROM ProformaInvoices WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentProformaId);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return 0;
                    return Convert.ToInt32(o);
                }
            }
        }
        catch { return 0; }
    }

    private bool IsCurrentDeleted()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT IsDeleted FROM ProformaInvoices WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentProformaId);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return false;
                    return Convert.ToBoolean(o);
                }
            }
        }
        catch { return false; }
    }

    // ═══════════════════════════════════════════════════════
    // BUTTONS
    // ═══════════════════════════════════════════════════════

    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        try
        {
            int firstCustomerId = GetFirstCustomerId();
            if (firstCustomerId <= 0)
            {
                ShowMessage("هیچ مشتری فعالی وجود ندارد. ابتدا از بخش مشتریان اضافه کنید.");
                return;
            }

            string proformaNumber = GenerateProformaNumber();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int orderType = GetCustomerType(firstCustomerId);
                int marketerId = GetCustomerMarketerId(conn, null, firstCustomerId);

                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO ProformaInvoices " +
                    "  (ProformaNumber, CustomerId, OrderType, Channel, MarketerId, IssueDate, ValidUntil, " +
                    "   Status, Subtotal, Discount, Tax, Total, IsDeleted, CreatedAt, CreatedBy) " +
                    "VALUES (@Number, @CustomerId, @OrderType, 1, @Mk, GETDATE(), DATEADD(DAY, 3, GETDATE()), " +
                    "   1, 0, 0, 0, 0, 0, GETDATE(), @By); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    cmd.Parameters.AddWithValue("@Number", proformaNumber);
                    cmd.Parameters.AddWithValue("@CustomerId", firstCustomerId);
                    cmd.Parameters.AddWithValue("@OrderType", orderType);
                    cmd.Parameters.AddWithValue("@Mk", marketerId > 0 ? (object)marketerId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                    object r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value)
                    {
                        int newId = Convert.ToInt32(r);
                        WriteAudit(conn, "Insert", "ProformaInvoices", newId, proformaNumber, "ایجاد پیش‌فاکتور");
                        ShowDetailView(newId);
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private string GenerateProformaNumber()
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "PF-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(CAST(SUBSTRING(ProformaNumber, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
                    "FROM ProformaInvoices WHERE ProformaNumber LIKE @Prefix + '%'", conn))
                {
                    cmd.Parameters.AddWithValue("@Prefix", prefix);
                    object o = cmd.ExecuteScalar();
                    int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
                    return prefix + seq.ToString("D4");
                }
            }
        }
        catch { return prefix + "0001"; }
    }

    private int GetFirstCustomerId()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 CustomerId FROM Customers " +
                    "WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY CustomerId", conn))
                {
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
            }
        }
        catch { return 0; }
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        CurrentProformaId = 0;
        EditItemId = 0;
        ShowListView();
    }

    // ─────────────────────────────────────────────────────
    // Repeater — List
    // ─────────────────────────────────────────────────────
    protected void rptProformas_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "View") ShowDetailView(id);
        else if (e.CommandName == "Restore") RestoreProforma(id);
    }

    // ═══════════════════════════════════════════════════════
    // Customer change — ذخیره فوری
    // ═══════════════════════════════════════════════════════
    protected void ddlCustomer_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            int customerId;
            if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
                || !int.TryParse(ddlCustomer.SelectedValue, out customerId) || customerId <= 0)
            {
                litCustomerHint.Text = "مشتری را از لیست انتخاب کنید.";
                return;
            }

            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            int status = GetCurrentStatus();
            if (status != 1)
            {
                UpdateCustomerHint();
                return;
            }

            // ✅ ذخیره فوری در DB
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int orderType = GetCustomerType(customerId);
                int marketerId = GetCustomerMarketerId(conn, null, customerId);

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProformaInvoices SET " +
                    "  CustomerId = @CId, OrderType = @OT, MarketerId = @Mk, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@CId", customerId);
                    cmd.Parameters.AddWithValue("@OT", orderType);
                    cmd.Parameters.AddWithValue("@Mk", marketerId > 0 ? (object)marketerId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    cmd.ExecuteNonQuery();
                }
            }

            UpdateCustomerHint();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void UpdateCustomerHint()
    {
        int custId;
        if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
            || !int.TryParse(ddlCustomer.SelectedValue, out custId) || custId <= 0)
        {
            litCustomerHint.Text = "مشتری را از لیست انتخاب کنید.";
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerType, Mobile, City, CreditLimit FROM Customers WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", custId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int type = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);
                            string mobile = dr["Mobile"] == DBNull.Value ? "" : Convert.ToString(dr["Mobile"]);
                            string city = dr["City"] == DBNull.Value ? "" : Convert.ToString(dr["City"]);
                            decimal creditLimit = dr["CreditLimit"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CreditLimit"]);

                            StringBuilder sb = new StringBuilder();
                            sb.Append("نوع: ").Append(GetCustomerTypeLabelStatic(type));
                            if (mobile.Length > 0) sb.Append(" — موبایل: ").Append(mobile);
                            if (city.Length > 0) sb.Append(" — ").Append(city);
                            if (creditLimit > 0m)
                                sb.Append(" — سقف اعتبار: ").Append(FormatMoneyValue(creditLimit)).Append(" تومان");

                            litCustomerHint.Text = HttpUtility.HtmlEncode(sb.ToString());
                        }
                    }
                }
            }
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════════
    // Save Header
    // ═══════════════════════════════════════════════════════
    protected void btnSaveHeader_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) { ShowMessage("پیش‌فاکتور معتبر نیست."); return; }

            int status = GetCurrentStatus();
            if (status != 1) { ShowMessage("فقط پیش‌نویس قابل ویرایش است."); return; }

            int customerId;
            if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
                || !int.TryParse(ddlCustomer.SelectedValue, out customerId) || customerId <= 0)
            {
                ShowMessage("مشتری را انتخاب کنید.");
                return;
            }

            int channel = 1;
            int.TryParse(ddlChannel.SelectedValue, out channel);

            int validityDays = 3;
            if (!int.TryParse(txtValidityDays.Text.Trim(), out validityDays) || validityDays < 1)
                validityDays = 3;
            if (validityDays > 365) validityDays = 365;

            string paymentTerms = txtPaymentTerms.Text.Trim();
            string deliveryTerms = txtDeliveryTerms.Text.Trim();
            string notes = txtHeaderNotes.Text.Trim();

            int orderType = GetCustomerType(customerId);

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int marketerId = GetCustomerMarketerId(conn, null, customerId);

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProformaInvoices SET " +
                    "  CustomerId = @CId, OrderType = @OT, Channel = @Ch, MarketerId = @Mk, " +
                    "  ValidUntil = DATEADD(DAY, @Days, IssueDate), " +
                    "  PaymentTerms = @PT, DeliveryTerms = @DT, Notes = @Notes, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@CId", customerId);
                    cmd.Parameters.AddWithValue("@OT", orderType);
                    cmd.Parameters.AddWithValue("@Ch", channel);
                    cmd.Parameters.AddWithValue("@Mk", marketerId > 0 ? (object)marketerId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Days", validityDays);
                    cmd.Parameters.AddWithValue("@PT", NullIfEmpty(paymentTerms));
                    cmd.Parameters.AddWithValue("@DT", NullIfEmpty(deliveryTerms));
                    cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Update", "ProformaInvoices", proformaId, "PF", "ویرایش هدر");
            }

            ShowMessage("اطلاعات ذخیره شد.");
            ShowDetailView(proformaId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private int GetCustomerType(int customerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerType FROM Customers WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return 2;
                    return Convert.ToInt32(o);
                }
            }
        }
        catch { return 2; }
    }

    private int GetCustomerMarketerId(SqlConnection conn, SqlTransaction tran, int customerId)
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(MarketerId, 0) FROM Customers WHERE CustomerId = @Id",
                conn, tran))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
        catch { return 0; }
    }

    // ═══════════════════════════════════════════════════════
    // Items
    // ═══════════════════════════════════════════════════════
    protected void btnShowAddItemForm_Click(object sender, EventArgs e)
    {
        int status = GetCurrentStatus();
        if (status != 1) { ShowMessage("فقط در پیش‌نویس می‌توان اقلام اضافه کرد."); return; }

        ClearItemForm();
        EditItemId = 0;
        litItemFormTitle.Text = "افزودن محصول";
        pnlItemForm.Visible = true;
    }

    protected void btnCancelItemForm_Click(object sender, EventArgs e)
    {
        ClearItemForm();
        EditItemId = 0;
        pnlItemForm.Visible = false;
    }

    // ═══════════════════════════════════════════════════════
    // Auto-Fill Price
    // ═══════════════════════════════════════════════════════
    protected void ddlItemProduct_SelectedIndexChanged(object sender, EventArgs e)
    {
        AutoFillItemPrice();
    }

    protected void txtItemQty_TextChanged(object sender, EventArgs e)
    {
        AutoFillItemPrice();
    }

    private void AutoFillItemPrice()
    {
        try
        {
            int productId;
            if (string.IsNullOrEmpty(ddlItemProduct.SelectedValue)
                || !int.TryParse(ddlItemProduct.SelectedValue, out productId) || productId <= 0)
                return;

            // ✅ مشتری را از Dropdown بخوان
            int customerId = 0;
            if (!string.IsNullOrEmpty(ddlCustomer.SelectedValue))
                int.TryParse(ddlCustomer.SelectedValue, out customerId);

            if (customerId <= 0)
            {
                int proformaId = CurrentProformaId;
                if (proformaId > 0)
                {
                    using (SqlConnection conn = new SqlConnection(ConnStr))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT CustomerId FROM ProformaInvoices WHERE ProformaInvoiceId = @Id", conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            object o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value)
                                customerId = Convert.ToInt32(o);
                        }
                    }
                }
            }

            if (customerId <= 0) return;

            decimal qty = ParseDecimalOrZero(txtItemQty.Text);
            if (qty <= 0) qty = 1;

            int orderType = 2;
            int customerPriceListId = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerType, ISNULL(PriceListId, 0) AS PriceListId " +
                    "FROM Customers WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            orderType = dr["CustomerType"] == DBNull.Value ? 2 : Convert.ToInt32(dr["CustomerType"]);
                            customerPriceListId = dr["PriceListId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PriceListId"]);
                        }
                    }
                }

                int priceListId = customerPriceListId;

                // لیست قیمت بر اساس نوع مشتری
                if (priceListId <= 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 PriceListId FROM PriceLists " +
                        "WHERE IsActive = 1 AND IsDeleted = 0 AND PriceListType = @Type " +
                        "ORDER BY IsDefault DESC, PriceListId", conn))
                    {
                        cmd.Parameters.AddWithValue("@Type", orderType);
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            priceListId = Convert.ToInt32(o);
                    }
                }

                // لیست پیش‌فرض
                if (priceListId <= 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 PriceListId FROM PriceLists " +
                        "WHERE IsActive = 1 AND IsDeleted = 0 " +
                        "ORDER BY IsDefault DESC, PriceListId", conn))
                    {
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            priceListId = Convert.ToInt32(o);
                    }
                }

                if (priceListId <= 0) return;

                decimal unitPrice = 0m;
                decimal discountPercent = 0m;
                bool found = false;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 UnitPrice, DiscountPercent " +
                    "FROM PriceListItems " +
                    "WHERE PriceListId = @PLId AND ProductId = @PId " +
                    "  AND IsActive = 1 " +
                    "  AND EffectiveFrom <= GETDATE() " +
                    "  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE()) " +
                    "  AND MinQuantity <= @Qty " +
                    "ORDER BY MinQuantity DESC, PriceListItemId DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@PLId", priceListId);
                    cmd.Parameters.AddWithValue("@PId", productId);
                    cmd.Parameters.AddWithValue("@Qty", qty);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            unitPrice = dr["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["UnitPrice"]);
                            discountPercent = dr["DiscountPercent"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["DiscountPercent"]);
                            found = true;
                        }
                    }
                }

                if (found && unitPrice > 0m)
                {
                    txtItemUnitPrice.Text = unitPrice.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                    if (discountPercent > 0m)
                    {
                        decimal discountAmount = Math.Round(unitPrice * qty * discountPercent / 100m, 2);
                        txtItemDiscount.Text = discountAmount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                    }
                }
            }
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════════
    // Auto-Fill All Items
    // ═══════════════════════════════════════════════════════
    protected void btnAutoFillAllPrices_Click(object sender, EventArgs e)
    {
        int updatedCount = 0;   // ✅ تعریف بیرون از using

        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            int customerId = 0;
            int orderType = 2;
            int priceListId = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT p.CustomerId, p.OrderType, ISNULL(c.PriceListId, 0) AS PLId " +
                    "FROM ProformaInvoices p " +
                    "INNER JOIN Customers c ON c.CustomerId = p.CustomerId " +
                    "WHERE p.ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            customerId = Convert.ToInt32(dr["CustomerId"]);
                            orderType = dr["OrderType"] == DBNull.Value ? 2 : Convert.ToInt32(dr["OrderType"]);
                            priceListId = dr["PLId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PLId"]);
                        }
                    }
                }

                if (customerId <= 0) { ShowMessage("مشتری مشخص نیست."); return; }

                if (priceListId <= 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 PriceListId FROM PriceLists " +
                        "WHERE IsActive = 1 AND IsDeleted = 0 AND PriceListType = @Type " +
                        "ORDER BY IsDefault DESC, PriceListId", conn))
                    {
                        cmd.Parameters.AddWithValue("@Type", orderType);
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            priceListId = Convert.ToInt32(o);
                    }
                }

                if (priceListId <= 0)
                {
                    ShowMessage("هیچ لیست قیمت فعالی برای این مشتری وجود ندارد.");
                    return;
                }

                DataTable items = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProformaInvoiceItemId, ProductId, Quantity " +
                    "FROM ProformaInvoiceItems WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(items);
                }

                if (items.Rows.Count == 0)
                {
                    ShowMessage("پیش‌فاکتور هیچ قلمی ندارد.");
                    return;
                }

                foreach (DataRow r in items.Rows)
                {
                    int itemId = Convert.ToInt32(r["ProformaInvoiceItemId"]);
                    int productId = Convert.ToInt32(r["ProductId"]);
                    decimal qty = Convert.ToDecimal(r["Quantity"]);

                    decimal unitPrice = 0m;
                    decimal discountPercent = 0m;

                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 UnitPrice, DiscountPercent " +
                        "FROM PriceListItems " +
                        "WHERE PriceListId = @PLId AND ProductId = @PId " +
                        "  AND IsActive = 1 " +
                        "  AND EffectiveFrom <= GETDATE() " +
                        "  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE()) " +
                        "  AND MinQuantity <= @Qty " +
                        "ORDER BY MinQuantity DESC, PriceListItemId DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@PLId", priceListId);
                        cmd.Parameters.AddWithValue("@PId", productId);
                        cmd.Parameters.AddWithValue("@Qty", qty);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                unitPrice = dr["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["UnitPrice"]);
                                discountPercent = dr["DiscountPercent"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["DiscountPercent"]);
                            }
                        }
                    }

                    if (unitPrice <= 0m) continue;

                    decimal discountAmount = discountPercent > 0m
                        ? Math.Round(unitPrice * qty * discountPercent / 100m, 2)
                        : 0m;

                    decimal itemTotal = Math.Round(qty * unitPrice, 2) - discountAmount;

                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE ProformaInvoiceItems SET " +
                        "  UnitPrice = @UP, Discount = @Disc, Total = @Total " +
                        "WHERE ProformaInvoiceItemId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@UP", unitPrice);
                        cmd.Parameters.AddWithValue("@Disc", discountAmount);
                        cmd.Parameters.AddWithValue("@Total", itemTotal);
                        cmd.Parameters.AddWithValue("@Id", itemId);
                        cmd.ExecuteNonQuery();
                    }

                    updatedCount++;
                }

                RecalcTotals(conn, proformaId);
            }

            if (updatedCount > 0)
                ShowMessage(updatedCount.ToString(CultureInfo.InvariantCulture) + " قلم از لیست قیمت پر شد.");
            else
                ShowMessage("هیچ قیمتی در لیست قیمت برای این محصولات یافت نشد.");

            ShowDetailView(CurrentProformaId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnSaveItem_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) { ShowMessage("پیش‌فاکتور معتبر نیست."); return; }

            int status = GetCurrentStatus();
            if (status != 1) { ShowMessage("فقط در پیش‌نویس می‌توان اقلام اضافه کرد."); return; }

            int currentId = EditItemId;

            int productId;
            if (string.IsNullOrEmpty(ddlItemProduct.SelectedValue)
                || !int.TryParse(ddlItemProduct.SelectedValue, out productId) || productId <= 0)
            {
                ShowMessage("محصول را انتخاب کنید.");
                return;
            }

            decimal qty = ParseDecimalOrZero(txtItemQty.Text);
            if (qty <= 0) { ShowMessage("تعداد باید بزرگ‌تر از صفر باشد."); return; }

            decimal unitPrice = ParseDecimalOrZero(txtItemUnitPrice.Text);
            if (unitPrice < 0) unitPrice = 0;

            decimal discount = ParseDecimalOrZero(txtItemDiscount.Text);
            if (discount < 0) discount = 0;

            decimal tax = 0m;

            decimal itemSubtotal = Math.Round(qty * unitPrice, 2);
            decimal itemTotal = itemSubtotal - discount + tax;
            if (itemTotal < 0) itemTotal = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string dupSql = "SELECT COUNT(*) FROM ProformaInvoiceItems " +
                    "WHERE ProformaInvoiceId = @PId AND ProductId = @ProdId";
                if (currentId > 0) dupSql += " AND ProformaInvoiceItemId <> @ItemId";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@PId", proformaId);
                    dup.Parameters.AddWithValue("@ProdId", productId);
                    if (currentId > 0) dup.Parameters.AddWithValue("@ItemId", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("این محصول قبلاً اضافه شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE ProformaInvoiceItems SET " +
                        "  ProductId = @ProdId, Quantity = @Qty, UnitPrice = @UP, " +
                        "  Discount = @Disc, Tax = @Tax, Total = @Total " +
                        "WHERE ProformaInvoiceItemId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        cmd.Parameters.AddWithValue("@Qty", qty);
                        cmd.Parameters.AddWithValue("@UP", unitPrice);
                        cmd.Parameters.AddWithValue("@Disc", discount);
                        cmd.Parameters.AddWithValue("@Tax", tax);
                        cmd.Parameters.AddWithValue("@Total", itemTotal);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO ProformaInvoiceItems " +
                        "  (ProformaInvoiceId, ProductId, Quantity, UnitPrice, Discount, Tax, Total) " +
                        "VALUES (@PId, @ProdId, @Qty, @UP, @Disc, @Tax, @Total)", conn))
                    {
                        cmd.Parameters.AddWithValue("@PId", proformaId);
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        cmd.Parameters.AddWithValue("@Qty", qty);
                        cmd.Parameters.AddWithValue("@UP", unitPrice);
                        cmd.Parameters.AddWithValue("@Disc", discount);
                        cmd.Parameters.AddWithValue("@Tax", tax);
                        cmd.Parameters.AddWithValue("@Total", itemTotal);
                        cmd.ExecuteNonQuery();
                    }
                }

                RecalcTotals(conn, proformaId);
                WriteAudit(conn, currentId > 0 ? "Update" : "Insert",
                    "ProformaInvoiceItems", currentId > 0 ? currentId : proformaId,
                    "قلم #" + proformaId, "ذخیره قلم پیش‌فاکتور");
            }

            ClearItemForm();
            EditItemId = 0;
            pnlItemForm.Visible = false;
            ShowDetailView(proformaId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void RecalcTotals(SqlConnection conn, int proformaId)
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE ProformaInvoices SET " +
                "  Subtotal = ISNULL((SELECT SUM(Quantity * UnitPrice) FROM ProformaInvoiceItems " +
                "                     WHERE ProformaInvoiceId = @Id), 0), " +
                "  Discount = ISNULL((SELECT SUM(Discount) FROM ProformaInvoiceItems " +
                "                     WHERE ProformaInvoiceId = @Id), 0), " +
                "  Tax = ISNULL((SELECT SUM(Tax) FROM ProformaInvoiceItems " +
                "                WHERE ProformaInvoiceId = @Id), 0), " +
                "  Total = ISNULL((SELECT SUM(Total) FROM ProformaInvoiceItems " +
                "                  WHERE ProformaInvoiceId = @Id), 0) " +
                "WHERE ProformaInvoiceId = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", proformaId);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
    }

    protected void rptItems_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0) return;

        if (e.CommandName == "EditItem") LoadItemForEdit(itemId);
        else if (e.CommandName == "DeleteItem") DeleteItem(itemId);
    }

    private void LoadItemForEdit(int itemId)
    {
        try
        {
            int status = GetCurrentStatus();
            if (status != 1) { ShowMessage("فقط در پیش‌نویس قابل ویرایش است."); return; }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, Quantity, UnitPrice, Discount " +
                    "FROM ProformaInvoiceItems WHERE ProformaInvoiceItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditItemId = itemId;

                            ddlItemProduct.ClearSelection();
                            string pid = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                            ListItem pItem = ddlItemProduct.Items.FindByValue(pid);
                            if (pItem != null) pItem.Selected = true;

                            txtItemQty.Text = dr["Quantity"] == DBNull.Value
                                ? "1" : Convert.ToDecimal(dr["Quantity"]).ToString(CultureInfo.InvariantCulture);
                            txtItemUnitPrice.Text = dr["UnitPrice"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["UnitPrice"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                            txtItemDiscount.Text = dr["Discount"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["Discount"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                            litItemFormTitle.Text = "ویرایش محصول";
                            pnlItemForm.Visible = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void DeleteItem(int itemId)
    {
        try
        {
            int status = GetCurrentStatus();
            if (status != 1) { ShowMessage("فقط در پیش‌نویس قابل حذف است."); return; }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                int proformaId = CurrentProformaId;

                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ProformaInvoiceItems WHERE ProformaInvoiceItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }

                RecalcTotals(conn, proformaId);
                WriteAudit(conn, "Delete", "ProformaInvoiceItems", itemId, "قلم #" + proformaId, "حذف قلم");
            }

            ShowMessage("قلم حذف شد.");
            ShowDetailView(CurrentProformaId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Issue
    // ═══════════════════════════════════════════════════════
    protected void btnIssue_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                DateTime validUntil = DateTime.Now;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProformaNumber, Status, ValidUntil " +
                    "FROM ProformaInvoices WHERE ProformaInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("یافت نشد."); return; }
                        number = Convert.ToString(dr["ProformaNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                        validUntil = dr["ValidUntil"] == DBNull.Value
                            ? DateTime.Now.AddDays(3) : Convert.ToDateTime(dr["ValidUntil"]);
                    }
                }

                if (status != 1) { ShowMessage("فقط پیش‌نویس قابل صدور است."); return; }
                if (validUntil < DateTime.Now) { ShowMessage("تاریخ اعتبار گذشته است."); return; }

                DataTable items = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, Quantity FROM ProformaInvoiceItems " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(items);
                }

                if (items.Rows.Count == 0) { ShowMessage("پیش‌فاکتور هیچ قلمی ندارد."); return; }

                int fgWarehouseId = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 WarehouseId FROM Warehouses " +
                    "WHERE WarehouseType = 2 AND IsActive = 1 AND IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) { ShowMessage("انبار محصول نهایی یافت نشد."); return; }
                    fgWarehouseId = Convert.ToInt32(o);
                }

                StringBuilder errMsg = new StringBuilder();
                foreach (DataRow r in items.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductId"]);
                    decimal qty = Convert.ToDecimal(r["Quantity"]);

                    decimal available = GetAvailableStock(fgWarehouseId, pid, proformaId);

                    if (available < qty)
                    {
                        string pname = GetProductName(pid);
                        errMsg.Append("• ").Append(pname)
                              .Append(" — نیاز: ").Append(qty.ToString("N2"))
                              .Append(" / موجود: ").Append(available.ToString("N2"))
                              .Append("\n");
                    }
                }

                if (errMsg.Length > 0)
                {
                    ShowMessage("موجودی کافی نیست:\n\n" + errMsg.ToString());
                    return;
                }

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (DataRow r in items.Rows)
                        {
                            int pid = Convert.ToInt32(r["ProductId"]);
                            decimal qty = Convert.ToDecimal(r["Quantity"]);

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO StockReservations " +
                                "  (WarehouseId, ProductId, Quantity, ReservedForType, ReservedForId, " +
                                "   Status, ExpiresAt, CreatedAt, CreatedBy) " +
                                "VALUES (@WhId, @PId, @Qty, 'ProformaInvoice', @RefId, " +
                                "   1, @Expires, GETDATE(), @By)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@WhId", fgWarehouseId);
                                cmd.Parameters.AddWithValue("@PId", pid);
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@RefId", proformaId);
                                cmd.Parameters.AddWithValue("@Expires", validUntil);
                                cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProformaInvoices SET Status = 2, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Issue", "ProformaInvoices", proformaId, number,
                            "صدور پیش‌فاکتور + رزرو موجودی");

                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                }
            }

            ShowMessage("پیش‌فاکتور صادر شد و موجودی رزرو شد.");
            ShowDetailView(CurrentProformaId);
        }
        catch (Exception ex) { ShowMessage("خطا در صدور: " + ex.Message); }
    }

    private decimal GetAvailableStock(int warehouseId, int productId, int excludeProformaId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                decimal onHand = 0m;
                decimal reserved = 0m;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(QuantityRemaining), 0) FROM StockLayers " +
                    "WHERE WarehouseId = @WhId AND ProductId = @PId " +
                    "  AND IsClosed = 0 AND QuantityRemaining > 0", conn))
                {
                    cmd.Parameters.AddWithValue("@WhId", warehouseId);
                    cmd.Parameters.AddWithValue("@PId", productId);
                    object o = cmd.ExecuteScalar();
                    onHand = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Quantity), 0) FROM StockReservations " +
                    "WHERE WarehouseId = @WhId AND ProductId = @PId AND Status = 1 " +
                    "  AND NOT (ReservedForType = 'ProformaInvoice' AND ReservedForId = @ExclId)", conn))
                {
                    cmd.Parameters.AddWithValue("@WhId", warehouseId);
                    cmd.Parameters.AddWithValue("@PId", productId);
                    cmd.Parameters.AddWithValue("@ExclId", excludeProformaId);
                    object o = cmd.ExecuteScalar();
                    reserved = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                return onHand - reserved;
            }
        }
        catch { return 0m; }
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

    // ═══════════════════════════════════════════════════════
    // Cancel
    // ═══════════════════════════════════════════════════════
    protected void btnCancelProforma_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProformaNumber, Status FROM ProformaInvoices " +
                    "WHERE ProformaInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("یافت نشد."); return; }
                        number = Convert.ToString(dr["ProformaNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                    }
                }

                if (status != 1 && status != 2 && status != 3)
                {
                    ShowMessage("این پیش‌فاکتور قابل لغو نیست.");
                    return;
                }

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE StockReservations SET Status = 3 " +
                            "WHERE ReservedForType = 'ProformaInvoice' " +
                            "  AND ReservedForId = @Id AND Status = 1", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProformaInvoices SET Status = 5, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Cancel", "ProformaInvoices", proformaId, number,
                            "لغو پیش‌فاکتور + آزادسازی رزروها");

                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                }
            }

            ShowMessage("پیش‌فاکتور لغو شد و رزروها آزاد شدند.");
            ShowDetailView(CurrentProformaId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Convert
    // ═══════════════════════════════════════════════════════
    protected void btnConvert_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                int customerId = 0;
                int orderType = 2;
                int channel = 1;
                int proformaMarketerId = 0;
                decimal subtotal = 0m, discount = 0m, tax = 0m, total = 0m;
                string number = "";

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProformaNumber, Status, CustomerId, OrderType, Channel, " +
                    "  ISNULL(MarketerId, 0) AS MarketerId, " +
                    "  Subtotal, Discount, Tax, Total " +
                    "FROM ProformaInvoices WHERE ProformaInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("یافت نشد."); return; }
                        number = Convert.ToString(dr["ProformaNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                        customerId = Convert.ToInt32(dr["CustomerId"]);
                        orderType = dr["OrderType"] == DBNull.Value ? 2 : Convert.ToInt32(dr["OrderType"]);
                        channel = dr["Channel"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Channel"]);
                        proformaMarketerId = Convert.ToInt32(dr["MarketerId"]);
                        subtotal = ToDecimal(dr["Subtotal"]);
                        discount = ToDecimal(dr["Discount"]);
                        tax = ToDecimal(dr["Tax"]);
                        total = ToDecimal(dr["Total"]);
                    }
                }

                if (status != 3) { ShowMessage("فقط پیش‌فاکتور «پرداخت‌شده» قابل تبدیل است."); return; }

                int fgWarehouseId = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 WarehouseId FROM Warehouses " +
                    "WHERE WarehouseType = 2 AND IsActive = 1 AND IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) { ShowMessage("انبار محصول نهایی یافت نشد."); return; }
                    fgWarehouseId = Convert.ToInt32(o);
                }

                DataTable items = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, Quantity, UnitPrice, Discount, Tax, Total " +
                    "FROM ProformaInvoiceItems WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(items);
                }

                if (items.Rows.Count == 0) { ShowMessage("پیش‌فاکتور هیچ قلمی ندارد."); return; }

                string invoiceNumber = GenerateFinalInvoiceNumber(conn);

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int finalMarketerId = proformaMarketerId;
                        if (finalMarketerId <= 0)
                            finalMarketerId = GetCustomerMarketerId(conn, tran, customerId);

                        int finalInvoiceId = 0;
                        using (SqlCommand cmd = new SqlCommand(
                            "INSERT INTO FinalInvoices " +
                            "  (InvoiceNumber, ProformaInvoiceId, CustomerId, OrderType, Channel, MarketerId, " +
                            "   IssueDate, Status, Subtotal, Discount, Tax, Total, PaidAmount, " +
                            "   IsDeleted, CreatedAt, CreatedBy) " +
                            "VALUES (@Number, @PId, @CId, @OT, @Ch, @Mk, " +
                            "   GETDATE(), 1, @Sub, @Disc, @Tax, @Total, @Total, " +
                            "   0, GETDATE(), @By); " +
                            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Number", invoiceNumber);
                            cmd.Parameters.AddWithValue("@PId", proformaId);
                            cmd.Parameters.AddWithValue("@CId", customerId);
                            cmd.Parameters.AddWithValue("@OT", orderType);
                            cmd.Parameters.AddWithValue("@Ch", channel);
                            cmd.Parameters.AddWithValue("@Mk", finalMarketerId > 0 ? (object)finalMarketerId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Sub", subtotal);
                            cmd.Parameters.AddWithValue("@Disc", discount);
                            cmd.Parameters.AddWithValue("@Tax", tax);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                            object r = cmd.ExecuteScalar();
                            if (r != null && r != DBNull.Value)
                                finalInvoiceId = Convert.ToInt32(r);
                        }

                        if (finalInvoiceId <= 0) throw new Exception("خطا در ایجاد فاکتور نهایی.");

                        decimal totalCogs = 0m;

                        foreach (DataRow r in items.Rows)
                        {
                            int pid = Convert.ToInt32(r["ProductId"]);
                            decimal qty = Convert.ToDecimal(r["Quantity"]);
                            decimal up = ToDecimal(r["UnitPrice"]);
                            decimal disc = ToDecimal(r["Discount"]);
                            decimal tx = ToDecimal(r["Tax"]);
                            decimal tot = ToDecimal(r["Total"]);

                            decimal cogs = ConsumeFifoForSale(conn, tran, fgWarehouseId, pid, qty,
                                finalInvoiceId, invoiceNumber);

                            totalCogs += cogs;

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO FinalInvoiceItems " +
                                "  (FinalInvoiceId, ProductId, Quantity, UnitPrice, Discount, Tax, Total, COGS) " +
                                "VALUES (@FId, @PId, @Qty, @UP, @Disc, @Tax, @Total, @Cogs)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@FId", finalInvoiceId);
                                cmd.Parameters.AddWithValue("@PId", pid);
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@UP", up);
                                cmd.Parameters.AddWithValue("@Disc", disc);
                                cmd.Parameters.AddWithValue("@Tax", tx);
                                cmd.Parameters.AddWithValue("@Total", tot);
                                cmd.Parameters.AddWithValue("@Cogs", cogs);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE StockReservations SET Status = 2 " +
                            "WHERE ReservedForType = 'ProformaInvoice' " +
                            "  AND ReservedForId = @Id AND Status = 1", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProformaInvoices SET Status = 4, FinalInvoiceId = @FId, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@FId", finalInvoiceId);
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Convert", "ProformaInvoices", proformaId, number,
                            "تبدیل به " + invoiceNumber + " (MarketerId=" + finalMarketerId + ")");

                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                }
            }

            ShowMessage("پیش‌فاکتور به فاکتور نهایی تبدیل شد.");
            ShowDetailView(CurrentProformaId);
        }
        catch (Exception ex) { ShowMessage("خطا در تبدیل: " + ex.Message); }
    }

    private string GenerateFinalInvoiceNumber(SqlConnection conn)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "INV-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(InvoiceNumber, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
            "FROM FinalInvoices WHERE InvoiceNumber LIKE @Prefix + '%'", conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + seq.ToString("D4");
        }
    }

    private decimal ConsumeFifoForSale(SqlConnection conn, SqlTransaction tran,
        int warehouseId, int productId, decimal quantity, int finalInvoiceId, string invoiceNumber)
    {
        decimal totalCost = 0m;
        decimal remaining = quantity;

        DataTable layers = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            "SELECT StockLayerId, QuantityRemaining, UnitCost " +
            "FROM StockLayers " +
            "WHERE WarehouseId = @WhId AND ProductId = @PId " +
            "  AND IsClosed = 0 AND QuantityRemaining > 0 " +
            "ORDER BY ReceivedDate ASC, StockLayerId ASC", conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(layers);
        }

        decimal available = 0m;
        foreach (DataRow r in layers.Rows)
            available += Convert.ToDecimal(r["QuantityRemaining"]);

        if (available < quantity)
            throw new Exception("موجودی کافی نیست. موجود: " + available.ToString("N2") +
                " — نیاز: " + quantity.ToString("N2"));

        int movementId = 0;
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO StockMovements " +
            "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
            "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
            "VALUES (@WhId, @PId, 3, @Qty, 0, 0, " +
            "   GETDATE(), 'FinalInvoice', @RefId, @Notes, GETDATE(), @By); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            cmd.Parameters.AddWithValue("@Qty", quantity);
            cmd.Parameters.AddWithValue("@RefId", finalInvoiceId);
            cmd.Parameters.AddWithValue("@Notes", "فروش در فاکتور " + invoiceNumber);
            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

            object r = cmd.ExecuteScalar();
            if (r != null && r != DBNull.Value) movementId = Convert.ToInt32(r);
        }

        foreach (DataRow layer in layers.Rows)
        {
            if (remaining <= 0m) break;

            int layerId = Convert.ToInt32(layer["StockLayerId"]);
            decimal layerRemaining = Convert.ToDecimal(layer["QuantityRemaining"]);
            decimal layerUnitCost = Convert.ToDecimal(layer["UnitCost"]);

            decimal useQty = Math.Min(remaining, layerRemaining);
            decimal layerCost = useQty * layerUnitCost;
            totalCost += layerCost;

            using (SqlCommand cmd = new SqlCommand(
                "UPDATE StockLayers SET QuantityRemaining = QuantityRemaining - @Qty, " +
                "  IsClosed = CASE WHEN QuantityRemaining - @Qty <= 0 THEN 1 ELSE 0 END " +
                "WHERE StockLayerId = @Id", conn, tran))
            {
                cmd.Parameters.AddWithValue("@Qty", useQty);
                cmd.Parameters.AddWithValue("@Id", layerId);
                cmd.ExecuteNonQuery();
            }

            if (movementId > 0)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO StockIssueLayers " +
                    "  (StockMovementId, StockLayerId, QuantityUsed, UnitCost, TotalCost) " +
                    "VALUES (@MovId, @LayerId, @Qty, @UnitCost, @TotalCost)", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MovId", movementId);
                    cmd.Parameters.AddWithValue("@LayerId", layerId);
                    cmd.Parameters.AddWithValue("@Qty", useQty);
                    cmd.Parameters.AddWithValue("@UnitCost", layerUnitCost);
                    cmd.Parameters.AddWithValue("@TotalCost", layerCost);
                    cmd.ExecuteNonQuery();
                }
            }

            remaining -= useQty;
        }

        if (movementId > 0)
        {
            decimal avgCost = quantity > 0 ? Math.Round(totalCost / quantity, 2) : 0m;
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE StockMovements SET UnitCost = @UnitCost, TotalCost = @TotalCost " +
                "WHERE StockMovementId = @Id", conn, tran))
            {
                cmd.Parameters.AddWithValue("@UnitCost", avgCost);
                cmd.Parameters.AddWithValue("@TotalCost", totalCost);
                cmd.Parameters.AddWithValue("@Id", movementId);
                cmd.ExecuteNonQuery();
            }
        }

        return totalCost;
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete
    // ═══════════════════════════════════════════════════════
    protected void btnSoftDelete_Click(object sender, EventArgs e)
    {
        try
        {
            int proformaId = CurrentProformaId;
            if (proformaId <= 0) return;

            int status = GetCurrentStatus();
            if (status != 1 && status != 5 && status != 6)
            {
                ShowMessage("فقط پیش‌نویس، لغو‌شده، یا منقضی قابل حذف است.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (status == 1)
                {
                    int itemCount = 0;
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM ProformaInvoiceItems WHERE ProformaInvoiceId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", proformaId);
                        object o = cmd.ExecuteScalar();
                        itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    }

                    if (itemCount > 0) { ShowMessage("ابتدا اقلام را حذف کنید."); return; }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProformaInvoices SET IsDeleted = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", proformaId);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "ProformaInvoices", proformaId, "PF", "حذف نرم");
            }

            ShowMessage("پیش‌فاکتور حذف شد (قابل بازیابی).");
            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void RestoreProforma(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string number = "";
                int status = 0;
                int finalInvoiceId = 0;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProformaNumber, Status, ISNULL(FinalInvoiceId, 0) AS FId " +
                    "FROM ProformaInvoices WHERE ProformaInvoiceId = @Id AND IsDeleted = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("یافت نشد."); return; }
                        number = Convert.ToString(dr["ProformaNumber"]);
                        status = Convert.ToInt32(dr["Status"]);
                        finalInvoiceId = Convert.ToInt32(dr["FId"]);
                    }
                }

                if (status == 4 || finalInvoiceId > 0)
                {
                    ShowMessage("این پیش‌فاکتور تبدیل شده و قابل بازیابی نیست.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProformaInvoices SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE ProformaInvoiceId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "ProformaInvoices", id, number, "بازیابی");
            }

            ShowMessage("پیش‌فاکتور بازیابی شد.");
            LoadStats();
            LoadProformas();
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

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
        catch { return "۰"; }
    }

    protected string FormatQty(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "۰";
            if (d == Math.Floor(d)) return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
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

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
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

        return "<p class=\"mt-0.5 text-[10.5px] " + css + "\">" + HttpUtility.HtmlEncode(label) + "</p>";
    }

    protected string GetRowCss(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 2) return "border-b border-surface-line bg-amber-50/40 hover:bg-amber-50/70 transition";
        if (s == 4) return "border-b border-surface-line bg-emerald-50/30";
        if (s == 5) return "border-b border-surface-line bg-danger-50/20 opacity-70";
        if (s == 6) return "border-b border-surface-line bg-violet-50/30 opacity-70";
        return "border-b border-surface-line transition hover:bg-surface-body/70";
    }

    protected string GetStatusBadge(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-surface-muted text-ink-600\">پیش‌نویس</span>";
        if (s == 2) return "<span class=\"badge bg-amber-100 text-amber-800\">صادرشده</span>";
        if (s == 3) return "<span class=\"badge bg-brand-100 text-brand-800\">پرداخت‌شده</span>";
        if (s == 4) return "<span class=\"badge bg-emerald-100 text-emerald-800\">تبدیل‌شده</span>";
        if (s == 5) return "<span class=\"badge bg-danger-50 text-danger-600\">لغو‌شده</span>";
        if (s == 6) return "<span class=\"badge bg-violet-100 text-violet-800\">منقضی</span>";
        return "";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetValidityCell(object statusObj, object validUntilObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj)) return "<span class=\"text-ink-400\">—</span>";
        if (validUntilObj == null || validUntilObj == DBNull.Value) return "<span class=\"text-ink-400\">—</span>";

        DateTime valid = Convert.ToDateTime(validUntilObj);
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);

        if (s == 4 || s == 5) return "<span class=\"text-ink-400\">—</span>";

        PersianCalendar pc = new PersianCalendar();
        string dateStr = string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(valid), pc.GetMonth(valid), pc.GetDayOfMonth(valid));

        TimeSpan span = valid - DateTime.Now;
        if (span.TotalSeconds < 0)
            return "<span class=\"text-danger-600\">" + dateStr + "</span>";

        string relative = "";
        if (span.TotalDays < 1) relative = "امروز";
        else if (span.TotalDays < 2) relative = "فردا";
        else relative = ((int)span.TotalDays).ToString(CultureInfo.InvariantCulture) + " روز";

        string color = span.TotalHours < 24 ? "text-amber-600" : "text-ink-600";

        return "<span class=\"" + color + " font-bold\">" + dateStr + "</span>" +
               "<div class=\"mt-0.5 text-[10px] text-ink-400\">" + relative + "</div>";
    }

    protected string GetAvailabilityBadge(object availableObj, object qtyObj)
    {
        try
        {
            decimal avail = (availableObj == null || availableObj == DBNull.Value) ? 0m : Convert.ToDecimal(availableObj);
            decimal qty = (qtyObj == null || qtyObj == DBNull.Value) ? 0m : Convert.ToDecimal(qtyObj);

            string css = avail >= qty ? "badge bg-emerald-50 text-emerald-700" : "badge bg-danger-50 text-danger-600";

            return "<span class=\"" + css + " font-mono text-[11px]\">" + FormatQty(avail) + "</span>";
        }
        catch { return "—"; }
    }

    protected bool CanEditItems(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj)) return false;
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1;
    }

    protected string GetDeleteItemConfirm(object productNameObj)
    {
        string name = (productNameObj == null || productNameObj == DBNull.Value) ? "" : productNameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف «" + name + "» مطمئن هستید؟');";
    }

    protected string GetRestoreConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('پیش‌فاکتور «" + num + "» بازیابی شود؟');";
    }

    // ─────────────────────────────────────────────────────
    // Misc
    // ─────────────────────────────────────────────────────
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

    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
    }

    private decimal ParseDecimalOrZero(string input)
    {
        decimal? d = ParseDecimal(input);
        return d.HasValue ? d.Value : 0m;
    }

    private decimal? ParseDecimal(string input)
    {
        if (input == null) return null;
        input = input.Trim();
        if (input.Length == 0) return null;
        input = input.Replace(",", "").Replace("،", "").Replace("٪", "").Replace(" ", "");
        decimal d;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    private void ClearItemForm()
    {
        if (ddlItemProduct.Items.Count > 0) ddlItemProduct.SelectedIndex = 0;
        txtItemQty.Text = "1";
        txtItemUnitPrice.Text = "";
        txtItemDiscount.Text = "0";
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