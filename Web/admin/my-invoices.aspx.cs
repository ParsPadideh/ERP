using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;

public partial class admin_my_invoices : System.Web.UI.Page
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

    protected void Page_Load(object sender, EventArgs e)
    {
        // فقط بازاریاب‌ها
        string role = Session["PrimaryRole"] == null ? "" : Convert.ToString(Session["PrimaryRole"]);
        if (!string.Equals(role, "بازاریاب", StringComparison.OrdinalIgnoreCase))
        {
            Response.Redirect(ResolveUrl("~/admin/Default.aspx"), false);
            return;
        }

        if (!IsPostBack)
        {
            LoadStats();
            LoadInvoices();
            LoadCounters();
        }
    }

    // ═══════════════════════════════════════════════════════
    // CanEdit — فقط پیش‌نویسِ خودِ بازاریاب
    // ═══════════════════════════════════════════════════════
    protected bool CanEdit(object statusObj, object marketerIdObj)
    {
        try
        {
            int status = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
            long marketerId = (marketerIdObj == null || marketerIdObj == DBNull.Value) ? 0 : Convert.ToInt64(marketerIdObj);

            if (status != 2) return false;
            if (marketerId != CurrentUserId) return false;

            return true;
        }
        catch { return false; }
    }

    // ─────────────────────────────────────────────────────
    // آمار
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
                    "  COUNT(*) AS TotalCount, " +
                    "  SUM(CASE WHEN Status IN (1, 2) THEN 1 ELSE 0 END) AS PendingCount, " +
                    "  SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS PaidCount, " +
                    "  ISNULL(SUM(Total), 0) AS TotalAmount " +
                    "FROM ProformaInvoices " +
                    "WHERE IsDeleted = 0 AND MarketerId = @Mid";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Mid", CurrentUserId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            litStatTotal.Text = HttpUtility.HtmlEncode(
                                ToInt(dr["TotalCount"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
                            litStatPending.Text = HttpUtility.HtmlEncode(
                                ToInt(dr["PendingCount"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
                            litStatPaid.Text = HttpUtility.HtmlEncode(
                                ToInt(dr["PaidCount"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
                            litStatTotalAmount.Text = HttpUtility.HtmlEncode(
                                FormatMoney(ToDecimal(dr["TotalAmount"])));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری آمار: " + ex.Message);
        }
    }

    private void LoadCounters()
    {
        try
        {
            int cAll = 0, cIssued = 0, cDraft = 0, cPaid = 0, cCancelled = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT Status, COUNT(*) AS Cnt FROM ProformaInvoices " +
                    "WHERE IsDeleted = 0 AND MarketerId = @Mid " +
                    "GROUP BY Status";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Mid", CurrentUserId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int status = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
                            int cnt = Convert.ToInt32(dr["Cnt"]);
                            cAll += cnt;

                            if (status == 1) cIssued += cnt;
                            else if (status == 2) cDraft += cnt;
                            else if (status == 4) cPaid += cnt;
                            else if (status == 5) cCancelled += cnt;
                        }
                    }
                }
            }

            litCountAll.Text = cAll.ToString(CultureInfo.InvariantCulture);
            litCountIssued.Text = cIssued.ToString(CultureInfo.InvariantCulture);
            litCountDraft.Text = cDraft.ToString(CultureInfo.InvariantCulture);
            litCountPaid.Text = cPaid.ToString(CultureInfo.InvariantCulture);
            litCountCancelled.Text = cCancelled.ToString(CultureInfo.InvariantCulture);
        }
        catch { }
    }

    private void LoadInvoices()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT " +
                    "  pi.ProformaInvoiceId, " +
                    "  pi.ProformaNumber, " +
                    "  pi.IssueDate, " +
                    "  pi.ValidUntil, " +
                    "  pi.Status, " +
                    "  pi.Total, " +
                    "  ISNULL(pi.MarketerId, 0) AS MarketerId, " +
                    "  ISNULL(c.CustomerName, N'—') AS CustomerName, " +
                    "  ISNULL(c.City, N'') AS City, " +
                    "  fi.InvoiceNumber AS FinalInvoiceNumber " +
                    "FROM ProformaInvoices pi " +
                    "LEFT JOIN Customers c ON c.CustomerId = pi.CustomerId " +
                    "LEFT JOIN FinalInvoices fi ON fi.ProformaInvoiceId = pi.ProformaInvoiceId AND fi.IsDeleted = 0 " +
                    "WHERE pi.IsDeleted = 0 AND pi.MarketerId = @Mid " +
                    "ORDER BY pi.ProformaInvoiceId DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Mid", CurrentUserId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            rptInvoices.DataSource = dt;
            rptInvoices.DataBind();

            litTotalCount.Text = HttpUtility.HtmlEncode(
                dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری فاکتورها: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    protected string GetShamsiDate(object dateObj)
    {
        if (dateObj == null || dateObj == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(dateObj);
            PersianCalendar pc = new PersianCalendar();
            return string.Format(CultureInfo.InvariantCulture,
                "{0}/{1:D2}/{2:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
        }
        catch { return "—"; }
    }

    protected string GetIssueDateTicks(object dateObj)
    {
        if (dateObj == null || dateObj == DBNull.Value) return "0";
        try
        {
            DateTime dt = Convert.ToDateTime(dateObj);
            return dt.Ticks.ToString(CultureInfo.InvariantCulture);
        }
        catch { return "0"; }
    }

    protected string FormatMoney(object valueObj)
    {
        if (valueObj == null || valueObj == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(valueObj);
            if (d == 0) return "۰";
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "۰"; }
    }

    protected string GetStatusLabel(object statusObj)
    {
        int status = ToInt(statusObj);
        switch (status)
        {
            case 1: return "صادرشده";
            case 2: return "پیش‌نویس";
            case 3: return "تبدیل‌شده";
            case 4: return "پرداخت‌شده";
            case 5: return "لغو شده";
            case 6: return "منقضی‌شده";
            default: return "نامشخص";
        }
    }

    protected string GetStatusBadgeCss(object statusObj)
    {
        int status = ToInt(statusObj);
        switch (status)
        {
            case 1: return "badge bg-brand-50 text-brand-700";
            case 2: return "badge bg-surface-muted text-ink-700";
            case 3: return "badge bg-violet-50 text-violet-700";
            case 4: return "badge bg-success-50 text-success-700";
            case 5: return "badge bg-danger-50 text-danger-600";
            case 6: return "badge bg-warning-50 text-warning-700";
            default: return "badge bg-surface-muted text-ink-500";
        }
    }

    protected string GetFinalInvoiceBadge(object finalInvObj)
    {
        if (finalInvObj == null || finalInvObj == DBNull.Value) return "";
        string fNum = Convert.ToString(finalInvObj).Trim();
        if (fNum.Length == 0) return "";
        return " <span class=\"badge bg-success-50 text-success-700\" style=\"font-size:10px;\">→ " +
            HttpUtility.HtmlEncode(fNum) + "</span>";
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        try { return Convert.ToInt32(o); }
        catch { return 0; }
    }

    private decimal ToDecimal(object o)
    {
        if (o == null || o == DBNull.Value) return 0m;
        try { return Convert.ToDecimal(o); }
        catch { return 0m; }
    }

    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message.Replace("\\", "\\\\").Replace("'", "\\'")
            .Replace("\r", " ").Replace("\n", " ");
        string script = "alert('" + safe + "');";
        if (ScriptManager.GetCurrent(this) != null)
            ScriptManager.RegisterStartupScript(this, GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
        else
            ClientScript.RegisterStartupScript(GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
    }
}