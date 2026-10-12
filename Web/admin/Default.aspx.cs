using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_Default : System.Web.UI.Page
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
            LoadWelcome();
            LoadKpis();
            LoadOperationsSummary();
            LoadChartData();
            LoadTopProducts();
            LoadTopServices();
            LoadRecentInvoices();
            LoadRecentServices();
        }
    }

    // ─────────────────────────────────────────────────────
    // Welcome
    // ─────────────────────────────────────────────────────
    private void LoadWelcome()
    {
        string name = "";
        if (Session["FullName"] != null)
            name = Convert.ToString(Session["FullName"]).Trim();
        if (name.Length == 0 && Session["Username"] != null)
            name = Convert.ToString(Session["Username"]).Trim();
        if (name.Length == 0) name = "کاربر";

        litWelcomeName.Text = HttpUtility.HtmlEncode(name);
        litTodayShamsi.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
    }

    // ─────────────────────────────────────────────────────
    // KPIs (current month)
    // ─────────────────────────────────────────────────────
    private void LoadKpis()
    {
        try
        {
            DateTime monthStart = GetCurrentPersianMonthStart();
            DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);
            DateTime now = DateTime.Now;

            decimal productRev = 0m;
            decimal serviceRev = 0m;
            decimal cogs = 0m;
            decimal expenses = 0m;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Product revenue + COGS
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(fii.Total), 0) AS Revenue, " +
                    "       ISNULL(SUM(fii.COGS), 0) AS Cogs " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            productRev = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            cogs = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                        }
                    }
                }

                // Service revenue
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Total), 0) FROM ServiceOrders " +
                    "WHERE IsDeleted = 0 AND Status = 3 " +
                    "  AND ServiceDate >= @From AND ServiceDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    serviceRev = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Expenses
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    expenses = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Previous month (for comparison)
                DateTime prevStart = new PersianCalendar().AddMonths(monthStart, -1);
                DateTime prevEnd = monthStart;

                decimal prevTotal = 0m;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(fii.Total), 0) FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", prevStart);
                    cmd.Parameters.AddWithValue("@To", prevEnd);
                    object o = cmd.ExecuteScalar();
                    prevTotal = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Total), 0) FROM ServiceOrders " +
                    "WHERE IsDeleted = 0 AND Status = 3 " +
                    "  AND ServiceDate >= @From AND ServiceDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", prevStart);
                    cmd.Parameters.AddWithValue("@To", prevEnd);
                    object o = cmd.ExecuteScalar();
                    prevTotal += (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Pending services count
                int pendingServices = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders WHERE IsDeleted = 0 AND Status = 1", conn))
                {
                    object o = cmd.ExecuteScalar();
                    pendingServices = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litKpiPendingServices.Text = HttpUtility.HtmlEncode(pendingServices.ToString("N0"));

                // Calculate totals
                decimal totalRevenue = productRev + serviceRev;
                decimal grossProfit = totalRevenue - cogs;
                decimal netProfit = grossProfit - expenses;

                litKpiTotalRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue));
                litKpiProductRev.Text = HttpUtility.HtmlEncode(FormatMoneyValue(productRev));
                litKpiServiceRev.Text = HttpUtility.HtmlEncode(FormatMoneyValue(serviceRev));
                litKpiGrossProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(grossProfit));
                litKpiNetProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(netProfit));
                litKpiExpenses.Text = HttpUtility.HtmlEncode(FormatMoneyValue(expenses));

                // Margins
                if (totalRevenue > 0m)
                {
                    litKpiGrossMargin.Text = HttpUtility.HtmlEncode(
                        ((grossProfit / totalRevenue) * 100m).ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪");
                }
                else
                {
                    litKpiGrossMargin.Text = "—";
                }

                // Compare with previous month
                if (prevTotal > 0m && totalRevenue > 0m)
                {
                    decimal change = ((totalRevenue - prevTotal) / prevTotal) * 100m;
                    string arrow = change >= 0 ? "▲" : "▼";
                    string color = change >= 0 ? "text-emerald-600" : "text-danger-500";
                    litRevenueCompare.Text =
                        "<span class=\"" + color + " font-bold\">" + arrow + " " +
                        Math.Abs(change).ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪</span>" +
                        " نسبت به ماه قبل";
                }
                else if (totalRevenue > 0m)
                {
                    litRevenueCompare.Text = "<span class=\"text-ink-400\">— ماه قبل فروشی نبود</span>";
                }
                else
                {
                    litRevenueCompare.Text = "";
                }

                // Net profit card coloring
                if (netProfit > 0)
                {
                    cardNetProfitKpi.Attributes["class"] =
                        "kpi-card kpi-card--emerald rounded-card border-2 border-emerald-300 bg-emerald-50/40 p-5 shadow-card";
                    pNetProfitKpi.Attributes["class"] =
                        "font-mono text-[20px] font-black text-emerald-700";
                }
                else if (netProfit < 0)
                {
                    cardNetProfitKpi.Attributes["class"] =
                        "kpi-card kpi-card--rose rounded-card border-2 border-danger-300 bg-danger-50/40 p-5 shadow-card";
                    pNetProfitKpi.Attributes["class"] =
                        "font-mono text-[20px] font-black text-danger-600";
                }
                else
                {
                    cardNetProfitKpi.Attributes["class"] =
                        "kpi-card kpi-card--violet rounded-card border border-surface-line bg-surface-card p-5 shadow-card";
                    pNetProfitKpi.Attributes["class"] =
                        "font-mono text-[20px] font-black text-ink-500";
                }

                // Pending services hint
                if (pendingServices == 0)
                    litKpiPendingServicesHint.Text = "همه تسویه شده ✓";
                else if (pendingServices == 1)
                    litKpiPendingServicesHint.Text = "۱ سفارش منتظر";
                else
                    litKpiPendingServicesHint.Text = pendingServices.ToString("N0") + " سفارش منتظر";
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری KPI: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Operations Summary
    // ─────────────────────────────────────────────────────
    private void LoadOperationsSummary()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Open proformas
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProformaInvoices " +
                    "WHERE Status IN (2, 3) AND ValidUntil >= GETDATE()", conn))
                {
                    object o = cmd.ExecuteScalar();
                    litOpOpenProformas.Text = HttpUtility.HtmlEncode(
                        ((o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o)).ToString("N0"));
                }

                // Running production orders
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductionOrders WHERE Status = 2", conn))
                {
                    object o = cmd.ExecuteScalar();
                    litOpRunningProd.Text = HttpUtility.HtmlEncode(
                        ((o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o)).ToString("N0"));
                }

                // Low stock
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Products p " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                    "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock", conn))
                {
                    object o = cmd.ExecuteScalar();
                    litOpLowStock.Text = HttpUtility.HtmlEncode(
                        ((o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o)).ToString("N0"));
                }

                // Pending distribution periods
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProfitPeriods WHERE Status = 2", conn))
                {
                    object o = cmd.ExecuteScalar();
                    litOpPendingPeriods.Text = HttpUtility.HtmlEncode(
                        ((o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o)).ToString("N0"));
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری خلاصه عملیات: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Chart Data (last 30 days)
    // ─────────────────────────────────────────────────────
    private void LoadChartData()
    {
        try
        {
            DateTime now = DateTime.Now;
            DateTime start = now.Date.AddDays(-29);

            // Build date buckets
            var labels = new List<string>();
            var productData = new List<decimal>();
            var serviceData = new List<decimal>();

            var productMap = new Dictionary<string, decimal>();
            var serviceMap = new Dictionary<string, decimal>();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Product revenue by day
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CAST(fi.IssueDate AS DATE) AS D, ISNULL(SUM(fii.Total), 0) AS Total " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From " +
                    "GROUP BY CAST(fi.IssueDate AS DATE)", conn))
                {
                    cmd.Parameters.AddWithValue("@From", start);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            DateTime d = Convert.ToDateTime(dr["D"]);
                            decimal total = dr["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Total"]);
                            productMap[d.ToString("yyyy-MM-dd")] = total;
                        }
                    }
                }

                // Service revenue by day
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CAST(ServiceDate AS DATE) AS D, ISNULL(SUM(Total), 0) AS Total " +
                    "FROM ServiceOrders " +
                    "WHERE IsDeleted = 0 AND Status = 3 " +
                    "  AND ServiceDate >= @From " +
                    "GROUP BY CAST(ServiceDate AS DATE)", conn))
                {
                    cmd.Parameters.AddWithValue("@From", start);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            DateTime d = Convert.ToDateTime(dr["D"]);
                            decimal total = dr["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Total"]);
                            serviceMap[d.ToString("yyyy-MM-dd")] = total;
                        }
                    }
                }
            }

            // Build series
            PersianCalendar pc = new PersianCalendar();
            for (int i = 0; i < 30; i++)
            {
                DateTime d = start.AddDays(i);
                string key = d.ToString("yyyy-MM-dd");

                labels.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0}/{1:D2}",
                    pc.GetMonth(d),
                    pc.GetDayOfMonth(d)));

                productData.Add(productMap.ContainsKey(key) ? productMap[key] : 0m);
                serviceData.Add(serviceMap.ContainsKey(key) ? serviceMap[key] : 0m);
            }

            hfChartLabels.Value = BuildJsonArray(labels);
            hfChartProductData.Value = BuildJsonDecimalArray(productData);
            hfChartServiceData.Value = BuildJsonDecimalArray(serviceData);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری نمودار: " + ex.Message);
        }
    }

    private string BuildJsonArray(List<string> items)
    {
        StringBuilder sb = new StringBuilder("[");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("\"");
            sb.Append(items[i].Replace("\\", "\\\\").Replace("\"", "\\\""));
            sb.Append("\"");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private string BuildJsonDecimalArray(List<decimal> items)
    {
        StringBuilder sb = new StringBuilder("[");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(items[i].ToString(CultureInfo.InvariantCulture));
        }
        sb.Append("]");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────
    // Top Products
    // ─────────────────────────────────────────────────────
    private void LoadTopProducts()
    {
        try
        {
            DateTime monthStart = GetCurrentPersianMonthStart();
            DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

            DataTable dt = new DataTable();
            dt.Columns.Add("Rank", typeof(int));
            dt.Columns.Add("ProductName", typeof(string));
            dt.Columns.Add("TotalQty", typeof(decimal));
            dt.Columns.Add("Revenue", typeof(decimal));

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT TOP 5 p.ProductName, " +
                    "  ISNULL(SUM(fii.Quantity), 0) AS TotalQty, " +
                    "  ISNULL(SUM(fii.Total), 0) AS Revenue " +
                    "FROM FinalInvoiceItems fii " +
                    "INNER JOIN FinalInvoices fi ON fi.FinalInvoiceId = fii.FinalInvoiceId " +
                    "INNER JOIN Products p ON p.ProductId = fii.ProductId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate < @To " +
                    "GROUP BY p.ProductName " +
                    "ORDER BY SUM(fii.Total) DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (dr.Read())
                        {
                            DataRow r = dt.NewRow();
                            r["Rank"] = rank++;
                            r["ProductName"] = Convert.ToString(dr["ProductName"]);
                            r["TotalQty"] = dr["TotalQty"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TotalQty"]);
                            r["Revenue"] = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            dt.Rows.Add(r);
                        }
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlNoTopProducts.Visible = true;
                rptTopProducts.Visible = false;
                rptTopProducts.DataSource = null;
                rptTopProducts.DataBind();
            }
            else
            {
                pnlNoTopProducts.Visible = false;
                rptTopProducts.Visible = true;
                rptTopProducts.DataSource = dt;
                rptTopProducts.DataBind();
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Top Services
    // ─────────────────────────────────────────────────────
    private void LoadTopServices()
    {
        try
        {
            DateTime monthStart = GetCurrentPersianMonthStart();
            DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

            DataTable dt = new DataTable();
            dt.Columns.Add("Rank", typeof(int));
            dt.Columns.Add("ServiceName", typeof(string));
            dt.Columns.Add("OrderCount", typeof(int));
            dt.Columns.Add("Revenue", typeof(decimal));

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT TOP 5 st.ServiceName, " +
                    "  COUNT(so.ServiceOrderId) AS OrderCount, " +
                    "  ISNULL(SUM(so.Total), 0) AS Revenue " +
                    "FROM ServiceOrders so " +
                    "INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId " +
                    "WHERE so.IsDeleted = 0 AND so.Status = 3 " +
                    "  AND so.ServiceDate >= @From AND so.ServiceDate < @To " +
                    "GROUP BY st.ServiceName " +
                    "ORDER BY SUM(so.Total) DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (dr.Read())
                        {
                            DataRow r = dt.NewRow();
                            r["Rank"] = rank++;
                            r["ServiceName"] = Convert.ToString(dr["ServiceName"]);
                            r["OrderCount"] = dr["OrderCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderCount"]);
                            r["Revenue"] = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            dt.Rows.Add(r);
                        }
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlNoTopServices.Visible = true;
                rptTopServices.Visible = false;
                rptTopServices.DataSource = null;
                rptTopServices.DataBind();
            }
            else
            {
                pnlNoTopServices.Visible = false;
                rptTopServices.Visible = true;
                rptTopServices.DataSource = dt;
                rptTopServices.DataBind();
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Recent Invoices
    // ─────────────────────────────────────────────────────
    private void LoadRecentInvoices()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT TOP 5 fi.InvoiceNumber, fi.IssueDate, fi.Total, c.CustomerName " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN Customers c ON c.CustomerId = fi.CustomerId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "ORDER BY fi.FinalInvoiceId DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
            {
                pnlNoRecentInvoices.Visible = true;
                rptRecentInvoices.Visible = false;
                rptRecentInvoices.DataSource = null;
                rptRecentInvoices.DataBind();
            }
            else
            {
                pnlNoRecentInvoices.Visible = false;
                rptRecentInvoices.Visible = true;
                rptRecentInvoices.DataSource = dt;
                rptRecentInvoices.DataBind();
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Recent Service Orders
    // ─────────────────────────────────────────────────────
    private void LoadRecentServices()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT TOP 5 so.OrderNumber, so.ServiceDate, so.Status, so.Total, " +
                    "  c.CustomerName, st.ServiceName " +
                    "FROM ServiceOrders so " +
                    "INNER JOIN Customers c ON c.CustomerId = so.CustomerId " +
                    "INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId " +
                    "WHERE so.IsDeleted = 0 " +
                    "ORDER BY so.ServiceOrderId DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
            {
                pnlNoRecentServices.Visible = true;
                rptRecentServices.Visible = false;
                rptRecentServices.DataSource = null;
                rptRecentServices.DataBind();
            }
            else
            {
                pnlNoRecentServices.Visible = false;
                rptRecentServices.Visible = true;
                rptRecentServices.DataSource = dt;
                rptRecentServices.DataBind();
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Helper: Current Persian Month Start
    // ─────────────────────────────────────────────────────
    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
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

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
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

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    // ── Service Status Icons ──
    protected string GetServiceStatusIconCss(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        string baseC = "grid h-9 w-9 shrink-0 place-items-center rounded-lg";
        if (s == 1) return baseC + " bg-amber-50 text-amber-600";
        if (s == 3) return baseC + " bg-emerald-50 text-emerald-600";
        return baseC + " bg-surface-muted text-ink-400";
    }

    protected string GetServiceStatusIcon(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1) return "clock";
        if (s == 3) return "check-circle-2";
        return "x-circle";
    }

    protected string GetServiceStatusBadge(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-amber-50 text-amber-700\">در انتظار</span>";
        if (s == 3) return "<span class=\"badge bg-emerald-50 text-emerald-700\">تکمیل</span>";
        return "<span class=\"badge bg-surface-muted text-ink-500\">لغو</span>";
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