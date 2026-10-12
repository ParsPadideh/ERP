using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_profit_loss : System.Web.UI.Page
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

    private string CurrentUserName
    {
        get
        {
            if (Session["FullName"] != null)
            {
                string s = Convert.ToString(Session["FullName"]).Trim();
                if (s.Length > 0) return s;
            }
            if (Session["Username"] != null)
                return Convert.ToString(Session["Username"]).Trim();
            return "—";
        }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            SetCurrentPersianMonth();
            LoadReport();
        }
    }

    private void SetCurrentPersianMonth()
    {
        DateTime from = GetCurrentPersianMonthStart();
        DateTime to = DateTime.Now;
        txtFrom.Text = ToShamsiDateString(from);
        txtTo.Text = ToShamsiDateString(to);
    }

    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
    }

    // ─────────────────────────────────────────────────────
    // Main Load
    // ─────────────────────────────────────────────────────
    private void LoadReport()
    {
        try
        {
            DateTime? fromNullable = ParsePersianDate(txtFrom.Text.Trim());
            DateTime? toNullable = ParsePersianDate(txtTo.Text.Trim());

            if (!fromNullable.HasValue)
            {
                ShowMessage("تاریخ شروع معتبر نیست. فرمت: 1403/01/01");
                return;
            }
            if (!toNullable.HasValue)
            {
                ShowMessage("تاریخ پایان معتبر نیست. فرمت: 1403/12/29");
                return;
            }
            if (toNullable.Value < fromNullable.Value)
            {
                ShowMessage("تاریخ پایان نمی‌تواند قبل از شروع باشد.");
                return;
            }

            DateTime from = fromNullable.Value.Date;
            DateTime to = toNullable.Value.Date.AddDays(1).AddSeconds(-1);

            string channelFilter = ddlFilterChannel.SelectedValue;
            if (channelFilter == null) channelFilter = "";

            string customerTypeFilter = ddlFilterCustomerType.SelectedValue;
            if (customerTypeFilter == null) customerTypeFilter = "";

            // Header meta
            litPeriodLabel.Text = HttpUtility.HtmlEncode(
                ToShamsiDateString(from) + " تا " + ToShamsiDateString(toNullable.Value));
            litCurrentUser.Text = HttpUtility.HtmlEncode(CurrentUserName);

            // Print header
            BuildPrintHeader(from, toNullable.Value);

            // Main sections
            LoadSummaryAndStatement(from, to, channelFilter, customerTypeFilter);
            LoadRevenueByChannel(from, to, customerTypeFilter);
            LoadRevenueByCustomerType(from, to, channelFilter);
            LoadServiceRevenueBreakdown(from, to);
            LoadTopProducts(from, to, channelFilter, customerTypeFilter);
            LoadExpensesByCategory(from, to);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری گزارش: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Print header
    // ─────────────────────────────────────────────────────
    private void BuildPrintHeader(DateTime from, DateTime to)
    {
        string companyName = ConfigurationManager.AppSettings["CompanyName"];
        if (string.IsNullOrEmpty(companyName)) companyName = "کارخانه تولید و بسته‌بندی مواد غذایی";
        litCompanyName.Text = HttpUtility.HtmlEncode(companyName);

        string tagline = ConfigurationManager.AppSettings["CompanyTagline"];
        string address = ConfigurationManager.AppSettings["CompanyAddress"];
        string phone = ConfigurationManager.AppSettings["CompanyPhone"];
        string email = ConfigurationManager.AppSettings["CompanyEmail"];
        string website = ConfigurationManager.AppSettings["CompanyWebsite"];

        StringBuilder meta = new StringBuilder();
        if (!string.IsNullOrEmpty(tagline)) meta.Append(HttpUtility.HtmlEncode(tagline));
        if (!string.IsNullOrEmpty(address)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("آدرس: " + HttpUtility.HtmlEncode(address)); }
        if (!string.IsNullOrEmpty(phone)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("تلفن: " + HttpUtility.HtmlEncode(phone)); }
        if (!string.IsNullOrEmpty(email)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("ایمیل: " + HttpUtility.HtmlEncode(email)); }
        if (!string.IsNullOrEmpty(website)) { if (meta.Length > 0) meta.Append(" — "); meta.Append(HttpUtility.HtmlEncode(website)); }
        litCompanyMeta.Text = meta.ToString();

        string logoUrl = ConfigurationManager.AppSettings["CompanyLogoUrl"];
        if (!string.IsNullOrEmpty(logoUrl))
        {
            try
            {
                string resolved = ResolveUrl(logoUrl);
                if (System.IO.File.Exists(Server.MapPath(logoUrl)))
                {
                    imgPrintLogo.ImageUrl = resolved;
                    imgPrintLogo.Visible = true;
                }
            }
            catch { }
        }

        litPrintDate.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now) + " — " +
            DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));

        litPrintPeriod.Text = "بازه گزارش: " + HttpUtility.HtmlEncode(
            ToShamsiDateString(from) + " تا " + ToShamsiDateString(to));
    }

    // ─────────────────────────────────────────────────────
    // Summary + Statement
    // ─────────────────────────────────────────────────────
    private void LoadSummaryAndStatement(DateTime from, DateTime to, string channelFilter, string customerTypeFilter)
    {
        decimal productRevenue = 0m;
        decimal serviceRevenue = 0m;
        decimal cogs = 0m;
        int invoiceCount = 0;
        int customerCount = 0;
        int serviceOrderCount = 0;

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            // ═══ 1) Product revenue + COGS ═══
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
            sb.Append("  ISNULL(SUM(fii.COGS), 0) AS Cogs, ");
            sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount, ");
            sb.Append("  COUNT(DISTINCT fi.CustomerId) AS CustomerCount ");
            sb.Append("FROM FinalInvoices fi ");
            sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
            sb.Append("WHERE fi.Status IN (1, 2) ");
            sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                int ch;
                if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                {
                    sb.Append(" AND fi.Channel = @Channel ");
                    cmd.Parameters.AddWithValue("@Channel", ch);
                }

                int ct;
                if (customerTypeFilter.Length > 0 && int.TryParse(customerTypeFilter, out ct) && ct > 0)
                {
                    sb.Append(" AND fi.OrderType = @OrderType ");
                    cmd.Parameters.AddWithValue("@OrderType", ct);
                }

                cmd.CommandText = sb.ToString();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        productRevenue = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                        cogs = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                        invoiceCount = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                        customerCount = dr["CustomerCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerCount"]);
                    }
                }
            }

            // ═══ 2) Service revenue ═══
            string svcSql =
                "SELECT ISNULL(SUM(Total), 0) AS ServiceRevenue, " +
                "       COUNT(*) AS ServiceOrderCount " +
                "FROM ServiceOrders " +
                "WHERE IsDeleted = 0 AND Status = 3 " +
                "  AND ServiceDate >= @From AND ServiceDate <= @To";

            using (SqlCommand cmd = new SqlCommand(svcSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        serviceRevenue = dr["ServiceRevenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ServiceRevenue"]);
                        serviceOrderCount = dr["ServiceOrderCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ServiceOrderCount"]);
                    }
                }
            }

            // ═══ 3) Expenses ═══
            decimal totalExpenses = 0m;
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate <= @To", conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                object o = cmd.ExecuteScalar();
                totalExpenses = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
            }

            // ═══ Calculate ═══
            decimal totalRevenue = productRevenue + serviceRevenue;
            decimal grossProfit = totalRevenue - cogs;
            decimal netProfit = grossProfit - totalExpenses;

            // ═══ KPIs ═══
            litKpiRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue));
            litKpiProductRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(productRevenue));
            litKpiServiceRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(serviceRevenue));

            litKpiCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(cogs));
            litKpiExpenses.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalExpenses));
            litKpiNetProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(netProfit));

            litKpiGrossMargin.Text = HttpUtility.HtmlEncode(FormatPercent(totalRevenue, grossProfit));
            litKpiNetMargin.Text = HttpUtility.HtmlEncode(FormatPercent(totalRevenue, netProfit));

            // Expenses count hint
            int expenseCount = 0;
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Expenses WHERE IsDeleted = 0 " +
                "AND ExpenseDate >= @From AND ExpenseDate <= @To", conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                object o = cmd.ExecuteScalar();
                expenseCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            litKpiExpensesHint.Text = expenseCount.ToString(CultureInfo.InvariantCulture) + " رکورد هزینه";

            // Net profit card coloring
            if (netProfit > 0)
            {
                cardNetProfit.Attributes["class"] =
                    "stat-card rounded-card border-2 border-emerald-300 bg-emerald-50/40 p-4 shadow-card print-card avoid-break";
                pNetProfit.Attributes["class"] =
                    "stat-value mt-2 text-[16px] font-black font-mono text-emerald-700";
            }
            else if (netProfit < 0)
            {
                cardNetProfit.Attributes["class"] =
                    "stat-card rounded-card border-2 border-danger-300 bg-danger-50/40 p-4 shadow-card print-card avoid-break";
                pNetProfit.Attributes["class"] =
                    "stat-value mt-2 text-[16px] font-black font-mono text-danger-600";
            }
            else
            {
                cardNetProfit.Attributes["class"] =
                    "stat-card rounded-card border-2 border-surface-line bg-surface-card p-4 shadow-card print-card avoid-break";
                pNetProfit.Attributes["class"] =
                    "stat-value mt-2 text-[16px] font-black font-mono text-ink-500";
            }

            // ═══ Statement ═══
            litStRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(productRevenue));
            litStServiceRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(serviceRevenue));
            litStTotalRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue));

            litStCogs.Text = HttpUtility.HtmlEncode("(" + FormatMoneyValue(cogs) + ")");
            litStGrossProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(grossProfit));
            litStTotalExpenses.Text = HttpUtility.HtmlEncode("(" + FormatMoneyValue(totalExpenses) + ")");
            litStNetProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(netProfit));

            litStRevenuePct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, productRevenue));
            litStServicePct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, serviceRevenue));
            litStCogsPct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, cogs));
            litStGrossPct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, grossProfit));
            litStExpensesPct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, totalExpenses));
            litStNetPct.Text = HttpUtility.HtmlEncode(FormatPercentRaw(totalRevenue, netProfit));

            // Meta
            string invoiceMeta = invoiceCount.ToString("N0");
            if (serviceOrderCount > 0)
                invoiceMeta += " + " + serviceOrderCount.ToString("N0") + " خدمت";
            litInvoiceCount.Text = HttpUtility.HtmlEncode(invoiceMeta);
            litCustomerCount.Text = HttpUtility.HtmlEncode(customerCount.ToString("N0"));

            // ═══ Statement expenses rows ═══
            DataTable dtExp = new DataTable();
            dtExp.Columns.Add("CategoryName", typeof(string));
            dtExp.Columns.Add("Amount", typeof(decimal));
            dtExp.Columns.Add("TotalRevenue", typeof(decimal));

            string expSql =
                "SELECT ec.CategoryName, SUM(e.Amount) AS Amount " +
                "FROM Expenses e " +
                "INNER JOIN ExpenseCategories ec ON ec.ExpenseCategoryId = e.ExpenseCategoryId " +
                "WHERE e.IsDeleted = 0 AND e.ExpenseDate >= @From AND e.ExpenseDate <= @To " +
                "GROUP BY ec.CategoryName " +
                "ORDER BY SUM(e.Amount) DESC";

            using (SqlCommand cmd = new SqlCommand(expSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        DataRow r = dtExp.NewRow();
                        r["CategoryName"] = Convert.ToString(dr["CategoryName"]);
                        r["Amount"] = dr["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Amount"]);
                        r["TotalRevenue"] = totalRevenue;
                        dtExp.Rows.Add(r);
                    }
                }
            }

            if (dtExp.Rows.Count == 0)
            {
                pnlNoExpenses.Visible = true;
                rptStatementExpenses.DataSource = null;
                rptStatementExpenses.DataBind();
            }
            else
            {
                pnlNoExpenses.Visible = false;
                rptStatementExpenses.DataSource = dtExp;
                rptStatementExpenses.DataBind();
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // By Channel
    // ─────────────────────────────────────────────────────
    private void LoadRevenueByChannel(DateTime from, DateTime to, string customerTypeFilter)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Channel", typeof(int));
        dt.Columns.Add("InvoiceCount", typeof(int));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("GrossProfit", typeof(decimal));

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT fi.Channel, ");
            sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount, ");
            sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
            sb.Append("  ISNULL(SUM(fii.Total) - SUM(fii.COGS), 0) AS GrossProfit ");
            sb.Append("FROM FinalInvoices fi ");
            sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
            sb.Append("WHERE fi.Status IN (1, 2) ");
            sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                int ct;
                if (customerTypeFilter.Length > 0 && int.TryParse(customerTypeFilter, out ct) && ct > 0)
                {
                    sb.Append(" AND fi.OrderType = @OrderType ");
                    cmd.Parameters.AddWithValue("@OrderType", ct);
                }

                sb.Append("GROUP BY fi.Channel ORDER BY SUM(fii.Total) DESC");
                cmd.CommandText = sb.ToString();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        DataRow r = dt.NewRow();
                        r["Channel"] = dr["Channel"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Channel"]);
                        r["InvoiceCount"] = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                        r["Revenue"] = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                        r["GrossProfit"] = dr["GrossProfit"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["GrossProfit"]);
                        dt.Rows.Add(r);
                    }
                }
            }
        }

        if (dt.Rows.Count == 0)
        {
            pnlNoChannel.Visible = true;
            rptByChannel.DataSource = null;
            rptByChannel.DataBind();
        }
        else
        {
            pnlNoChannel.Visible = false;
            rptByChannel.DataSource = dt;
            rptByChannel.DataBind();
        }
    }

    // ─────────────────────────────────────────────────────
    // By Customer Type
    // ─────────────────────────────────────────────────────
    private void LoadRevenueByCustomerType(DateTime from, DateTime to, string channelFilter)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("OrderType", typeof(int));
        dt.Columns.Add("InvoiceCount", typeof(int));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("TotalRevenue", typeof(decimal));

        decimal grandTotal = 0m;

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT fi.OrderType, ");
            sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount, ");
            sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue ");
            sb.Append("FROM FinalInvoices fi ");
            sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
            sb.Append("WHERE fi.Status IN (1, 2) ");
            sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                int ch;
                if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                {
                    sb.Append(" AND fi.Channel = @Channel ");
                    cmd.Parameters.AddWithValue("@Channel", ch);
                }

                sb.Append("GROUP BY fi.OrderType ORDER BY SUM(fii.Total) DESC");
                cmd.CommandText = sb.ToString();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        decimal rev = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                        grandTotal += rev;

                        DataRow r = dt.NewRow();
                        r["OrderType"] = dr["OrderType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderType"]);
                        r["InvoiceCount"] = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                        r["Revenue"] = rev;
                        r["TotalRevenue"] = 0m;
                        dt.Rows.Add(r);
                    }
                }
            }
        }

        foreach (DataRow row in dt.Rows)
            row["TotalRevenue"] = grandTotal;

        if (dt.Rows.Count == 0)
        {
            pnlNoCustomerType.Visible = true;
            rptByCustomerType.DataSource = null;
            rptByCustomerType.DataBind();
        }
        else
        {
            pnlNoCustomerType.Visible = false;
            rptByCustomerType.DataSource = dt;
            rptByCustomerType.DataBind();
        }
    }

    // ─────────────────────────────────────────────────────
    // Service Revenue Breakdown (NEW)
    // ─────────────────────────────────────────────────────
    private void LoadServiceRevenueBreakdown(DateTime from, DateTime to)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("ServiceTypeId", typeof(int));
        dt.Columns.Add("ServiceName", typeof(string));
        dt.Columns.Add("OrderCount", typeof(int));
        dt.Columns.Add("TotalInputWeight", typeof(decimal));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("TotalRevenue", typeof(decimal));

        decimal grandTotal = 0m;

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            string sql =
                "SELECT st.ServiceTypeId, st.ServiceName, " +
                "       COUNT(so.ServiceOrderId) AS OrderCount, " +
                "       ISNULL(SUM(so.InputWeight), 0) AS TotalInputWeight, " +
                "       ISNULL(SUM(so.Total), 0) AS Revenue " +
                "FROM ServiceOrders so " +
                "INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId " +
                "WHERE so.IsDeleted = 0 AND so.Status = 3 " +
                "  AND so.ServiceDate >= @From AND so.ServiceDate <= @To " +
                "GROUP BY st.ServiceTypeId, st.ServiceName " +
                "ORDER BY SUM(so.Total) DESC";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        decimal rev = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                        grandTotal += rev;

                        DataRow r = dt.NewRow();
                        r["ServiceTypeId"] = dr["ServiceTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ServiceTypeId"]);
                        r["ServiceName"] = Convert.ToString(dr["ServiceName"]);
                        r["OrderCount"] = dr["OrderCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderCount"]);
                        r["TotalInputWeight"] = dr["TotalInputWeight"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TotalInputWeight"]);
                        r["Revenue"] = rev;
                        r["TotalRevenue"] = 0m;
                        dt.Rows.Add(r);
                    }
                }
            }
        }

        foreach (DataRow row in dt.Rows)
            row["TotalRevenue"] = grandTotal;

        litServiceGrandTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(grandTotal));

        if (dt.Rows.Count == 0)
        {
            pnlNoServices.Visible = true;
            rptByServiceType.DataSource = null;
            rptByServiceType.DataBind();
        }
        else
        {
            pnlNoServices.Visible = false;
            rptByServiceType.DataSource = dt;
            rptByServiceType.DataBind();
        }
    }

    // ─────────────────────────────────────────────────────
    // Top Products
    // ─────────────────────────────────────────────────────
    private void LoadTopProducts(DateTime from, DateTime to, string channelFilter, string customerTypeFilter)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Rank", typeof(int));
        dt.Columns.Add("ProductName", typeof(string));
        dt.Columns.Add("ProductCode", typeof(string));
        dt.Columns.Add("TotalQty", typeof(decimal));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("Cogs", typeof(decimal));
        dt.Columns.Add("GrossProfit", typeof(decimal));

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TOP 10 ");
            sb.Append("  p.ProductId, p.ProductName, p.ProductCode, ");
            sb.Append("  ISNULL(SUM(fii.Quantity), 0) AS TotalQty, ");
            sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
            sb.Append("  ISNULL(SUM(fii.COGS), 0) AS Cogs, ");
            sb.Append("  ISNULL(SUM(fii.Total) - SUM(fii.COGS), 0) AS GrossProfit ");
            sb.Append("FROM FinalInvoiceItems fii ");
            sb.Append("INNER JOIN FinalInvoices fi ON fi.FinalInvoiceId = fii.FinalInvoiceId ");
            sb.Append("INNER JOIN Products p ON p.ProductId = fii.ProductId ");
            sb.Append("WHERE fi.Status IN (1, 2) ");
            sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                int ch;
                if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                {
                    sb.Append(" AND fi.Channel = @Channel ");
                    cmd.Parameters.AddWithValue("@Channel", ch);
                }

                int ct;
                if (customerTypeFilter.Length > 0 && int.TryParse(customerTypeFilter, out ct) && ct > 0)
                {
                    sb.Append(" AND fi.OrderType = @OrderType ");
                    cmd.Parameters.AddWithValue("@OrderType", ct);
                }

                sb.Append("GROUP BY p.ProductId, p.ProductName, p.ProductCode ");
                sb.Append("ORDER BY ISNULL(SUM(fii.Total) - SUM(fii.COGS), 0) DESC");
                cmd.CommandText = sb.ToString();

                int rank = 1;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        DataRow r = dt.NewRow();
                        r["Rank"] = rank++;
                        r["ProductName"] = Convert.ToString(dr["ProductName"]);
                        r["ProductCode"] = Convert.ToString(dr["ProductCode"]);
                        r["TotalQty"] = dr["TotalQty"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TotalQty"]);
                        r["Revenue"] = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                        r["Cogs"] = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                        r["GrossProfit"] = dr["GrossProfit"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["GrossProfit"]);
                        dt.Rows.Add(r);
                    }
                }
            }
        }

        if (dt.Rows.Count == 0)
        {
            pnlNoProducts.Visible = true;
            rptTopProducts.DataSource = null;
            rptTopProducts.DataBind();
        }
        else
        {
            pnlNoProducts.Visible = false;
            rptTopProducts.DataSource = dt;
            rptTopProducts.DataBind();
        }
    }

    // ─────────────────────────────────────────────────────
    // Expenses Breakdown
    // ─────────────────────────────────────────────────────
    private void LoadExpensesByCategory(DateTime from, DateTime to)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("CategoryName", typeof(string));
        dt.Columns.Add("ExpenseCount", typeof(int));
        dt.Columns.Add("Amount", typeof(decimal));
        dt.Columns.Add("TotalExpenses", typeof(decimal));

        decimal grandTotal = 0m;

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            string sql =
                "SELECT ec.CategoryName, COUNT(e.ExpenseId) AS ExpenseCount, " +
                "       ISNULL(SUM(e.Amount), 0) AS Amount " +
                "FROM Expenses e " +
                "INNER JOIN ExpenseCategories ec ON ec.ExpenseCategoryId = e.ExpenseCategoryId " +
                "WHERE e.IsDeleted = 0 AND e.ExpenseDate >= @From AND e.ExpenseDate <= @To " +
                "GROUP BY ec.CategoryName " +
                "ORDER BY SUM(e.Amount) DESC";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        decimal amt = dr["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Amount"]);
                        grandTotal += amt;

                        DataRow r = dt.NewRow();
                        r["CategoryName"] = Convert.ToString(dr["CategoryName"]);
                        r["ExpenseCount"] = dr["ExpenseCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ExpenseCount"]);
                        r["Amount"] = amt;
                        r["TotalExpenses"] = 0m;
                        dt.Rows.Add(r);
                    }
                }
            }
        }

        foreach (DataRow row in dt.Rows)
            row["TotalExpenses"] = grandTotal;

        litExpensesGrandTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(grandTotal));

        if (dt.Rows.Count == 0)
        {
            pnlNoExpensesBreakdown.Visible = true;
            rptExpenses.DataSource = null;
            rptExpenses.DataBind();
        }
        else
        {
            pnlNoExpensesBreakdown.Visible = false;
            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();
        }
    }

    // ─────────────────────────────────────────────────────
    // Filters
    // ─────────────────────────────────────────────────────
    protected void btnApply_Click(object sender, EventArgs e) { LoadReport(); }
    protected void btnRefresh_Click(object sender, EventArgs e) { LoadReport(); }

    protected void QuickRange_Click(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        if (btn == null) return;

        string arg = btn.CommandArgument;
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        DateTime from;
        DateTime to = now;

        if (arg == "today") { from = now.Date; to = now; }
        else if (arg == "week")
        {
            int dayOfWeek = (int)now.DayOfWeek;
            int daysSinceSaturday = (dayOfWeek + 1) % 7;
            from = now.Date.AddDays(-daysSinceSaturday);
            to = now;
        }
        else if (arg == "month")
        {
            from = pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
            to = now;
        }
        else if (arg == "quarter")
        {
            int m = pc.GetMonth(now);
            int qStartMonth = ((m - 1) / 3) * 3 + 1;
            from = pc.ToDateTime(pc.GetYear(now), qStartMonth, 1, 0, 0, 0, 0);
            to = now;
        }
        else if (arg == "year")
        {
            from = pc.ToDateTime(pc.GetYear(now), 1, 1, 0, 0, 0, 0);
            to = now;
        }
        else if (arg == "30days") { from = now.Date.AddDays(-29); to = now; }
        else if (arg == "lastyear")
        {
            int y = pc.GetYear(now) - 1;
            from = pc.ToDateTime(y, 1, 1, 0, 0, 0, 0);
            to = pc.ToDateTime(y, 12, 29, 23, 59, 59, 0);
        }
        else { return; }

        txtFrom.Text = ToShamsiDateString(from);
        txtTo.Text = ToShamsiDateString(to);
        LoadReport();
    }

    // ─────────────────────────────────────────────────────
    // Markup Helpers
    // ─────────────────────────────────────────────────────
    protected string Enc(object value)
    {
        if (value == null || value == DBNull.Value) return "";
        return HttpUtility.HtmlEncode(value.ToString());
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

    protected string FormatAmountNegative(object value)
    {
        if (value == null || value == DBNull.Value) return "(۰)";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "(۰)";
            return "(" + d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،") + ")";
        }
        catch { return "(۰)"; }
    }

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
        catch { return "۰"; }
    }

    protected string FormatQuantity(object value)
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

    protected string FormatQty(object value)
    {
        return FormatQuantity(value);
    }

    private string FormatPercent(decimal baseValue, decimal part)
    {
        if (baseValue == 0m) return "—";
        decimal pct = (part / baseValue) * 100m;
        return pct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪";
    }

    protected string FormatPercentRaw(object baseObj, object partObj)
    {
        try
        {
            decimal b = (baseObj == null || baseObj == DBNull.Value) ? 0m : Convert.ToDecimal(baseObj);
            decimal p = (partObj == null || partObj == DBNull.Value) ? 0m : Convert.ToDecimal(partObj);
            if (b == 0m) return "—";
            decimal pct = Math.Abs((p / b) * 100m);
            return pct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪";
        }
        catch { return "—"; }
    }

    protected string FormatPercentOf(object amountObj, object totalObj)
    {
        try
        {
            decimal a = (amountObj == null || amountObj == DBNull.Value) ? 0m : Convert.ToDecimal(amountObj);
            decimal t = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
            if (t == 0m) return "—";
            decimal pct = (a / t) * 100m;
            return pct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪";
        }
        catch { return "—"; }
    }

    protected string FormatShareBar(object valueObj, object totalObj)
    {
        try
        {
            decimal v = (valueObj == null || valueObj == DBNull.Value) ? 0m : Convert.ToDecimal(valueObj);
            decimal t = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
            if (t == 0m) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

            decimal pct = (v / t) * 100m;
            string pctText = pct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪";
            string barCss = "bg-brand-500";
            if (pct >= 50m) barCss = "bg-brand-600";
            else if (pct < 10m) barCss = "bg-brand-300";

            return "<div class=\"flex items-center gap-2\">" +
                "<div class=\"h-1.5 flex-1 overflow-hidden rounded-full bg-surface-muted\">" +
                "<div class=\"h-full rounded-full " + barCss + "\" style=\"width:" +
                Math.Round(pct, 0).ToString(CultureInfo.InvariantCulture) + "%\"></div>" +
                "</div>" +
                "<span class=\"font-mono text-[11.5px] font-bold text-ink-600\" style=\"min-width:50px;\">" +
                pctText + "</span>" +
                "</div>";
        }
        catch { return "—"; }
    }

    protected string GetMarginBadge(object profitObj, object revenueObj)
    {
        try
        {
            decimal p = (profitObj == null || profitObj == DBNull.Value) ? 0m : Convert.ToDecimal(profitObj);
            decimal r = (revenueObj == null || revenueObj == DBNull.Value) ? 0m : Convert.ToDecimal(revenueObj);
            if (r == 0m) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

            decimal pct = (p / r) * 100m;
            string css = "badge bg-surface-muted text-ink-600";
            if (pct >= 30m) css = "badge bg-emerald-100 text-emerald-700";
            else if (pct >= 15m) css = "badge bg-emerald-50 text-emerald-700";
            else if (pct >= 5m) css = "badge bg-amber-50 text-amber-700";
            else if (pct >= 0m) css = "badge bg-orange-50 text-orange-700";
            else css = "badge bg-danger-50 text-danger-600";

            return "<span class=\"" + css + " font-bold\">" +
                pct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪</span>";
        }
        catch { return "—"; }
    }

    protected string GetGrossProfitCss(object valueObj)
    {
        try
        {
            decimal v = (valueObj == null || valueObj == DBNull.Value) ? 0m : Convert.ToDecimal(valueObj);
            if (v > 0m) return "text-emerald-700";
            if (v < 0m) return "text-danger-600";
            return "text-ink-400";
        }
        catch { return "text-ink-400"; }
    }

    protected string GetChannelLabel(object channelObj)
    {
        int ch = (channelObj == null || channelObj == DBNull.Value) ? 0 : Convert.ToInt32(channelObj);
        if (ch == 1) return "بازاریاب";
        if (ch == 2) return "آنلاین";
        if (ch == 3) return "ادمین / دفتری";
        return "نامشخص";
    }

    protected string GetChannelBadgeCss(object channelObj)
    {
        int ch = (channelObj == null || channelObj == DBNull.Value) ? 0 : Convert.ToInt32(channelObj);
        if (ch == 1) return "bg-brand-50 text-brand-700";
        if (ch == 2) return "bg-violet-50 text-violet-700";
        if (ch == 3) return "bg-surface-muted text-ink-600";
        return "bg-surface-muted text-ink-500";
    }

    protected string GetCustomerTypeLabel(object orderTypeObj)
    {
        int t = (orderTypeObj == null || orderTypeObj == DBNull.Value) ? 0 : Convert.ToInt32(orderTypeObj);
        if (t == 1) return "تک‌فروشی";
        if (t == 2) return "عمده";
        if (t == 3) return "نمایندگی";
        return "نامشخص";
    }

    protected string GetCustomerTypeBadgeCss(object orderTypeObj)
    {
        int t = (orderTypeObj == null || orderTypeObj == DBNull.Value) ? 0 : Convert.ToInt32(orderTypeObj);
        if (t == 1) return "bg-emerald-50 text-emerald-700";
        if (t == 2) return "bg-amber-50 text-amber-700";
        if (t == 3) return "bg-violet-50 text-violet-700";
        return "bg-surface-muted text-ink-500";
    }

    // ─────────────────────────────────────────────────────
    // Date Helpers
    // ─────────────────────────────────────────────────────
    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    private DateTime? ParsePersianDate(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;
        input = input.Trim().Replace("-", "/");

        string[] parts = input.Split('/');
        if (parts.Length != 3) return null;

        int year, month, day;
        if (!int.TryParse(parts[0], out year)) return null;
        if (!int.TryParse(parts[1], out month)) return null;
        if (!int.TryParse(parts[2], out day)) return null;

        if (year < 1300 || year > 1500) return null;
        if (month < 1 || month > 12) return null;
        if (day < 1 || day > 31) return null;

        try
        {
            PersianCalendar pc = new PersianCalendar();
            return pc.ToDateTime(year, month, day, 0, 0, 0, 0);
        }
        catch { return null; }
    }

    // ─────────────────────────────────────────────────────
    // Message
    // ─────────────────────────────────────────────────────
    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", " ");

        string script = "alert('" + safe + "');";

        if (ScriptManager.GetCurrent(this) != null)
        {
            ScriptManager.RegisterStartupScript(this, GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
        }
        else
        {
            ClientScript.RegisterStartupScript(GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
        }
    }
}