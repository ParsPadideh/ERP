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

public partial class admin_reports_profit_channel : System.Web.UI.Page
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
            SetDefaultRange();
            LoadMarketersFilter();
            LoadCompanyInfo();
            LoadReport();
        }
    }

    // ─────────────────────────────────────────────────────
    // Set Default Range — از خود دیتا
    // ─────────────────────────────────────────────────────
    private void SetDefaultRange()
    {
        DateTime from = DateTime.Now.Date.AddDays(-30);
        DateTime to = DateTime.Now;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MIN(IssueDate) AS MinDate, MAX(IssueDate) AS MaxDate " +
                    "FROM FinalInvoices WHERE IsDeleted = 0 AND Status IN (1, 2)", conn))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            if (dr["MinDate"] != DBNull.Value)
                                from = Convert.ToDateTime(dr["MinDate"]).Date;
                            if (dr["MaxDate"] != DBNull.Value)
                                to = Convert.ToDateTime(dr["MaxDate"]).Date;
                        }
                    }
                }
            }
        }
        catch { }

        txtFrom.Text = ToShamsiDateString(from);
        txtTo.Text = ToShamsiDateString(to);
    }

    // ─────────────────────────────────────────────────────
    // Load Filter — فقط بازاریابان با مشتری
    // ─────────────────────────────────────────────────────
    private void LoadMarketersFilter()
    {
        ddlFilterMarketer.Items.Clear();
        ddlFilterMarketer.Items.Add(new ListItem("همه بازاریابان", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT u.UserId, ISNULL(u.FullName, u.Username) AS DisplayName, " +
                    "  (SELECT COUNT(*) FROM Customers c " +
                    "   WHERE c.MarketerId = u.UserId AND c.IsDeleted = 0) AS CustomerCount " +
                    "FROM Users u " +
                    "INNER JOIN Roles r ON r.RoleId = u.RoleId " +
                    "WHERE u.IsDeleted = 0 AND r.IsDeleted = 0 " +
                    "  AND r.RoleName = N'بازاریاب' " +
                    "  AND EXISTS (SELECT 1 FROM Customers c " +
                    "              WHERE c.MarketerId = u.UserId AND c.IsDeleted = 0) " +
                    "ORDER BY u.FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["UserId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["DisplayName"]);
                        int custCount = dr["CustomerCount"] == DBNull.Value
                            ? 0 : Convert.ToInt32(dr["CustomerCount"]);

                        string label = name;
                        if (custCount > 0)
                            label += " (" + custCount.ToString(CultureInfo.InvariantCulture) + " مشتری)";

                        ddlFilterMarketer.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Company Info
    // ─────────────────────────────────────────────────────
    private void LoadCompanyInfo()
    {
        string companyName = ConfigurationManager.AppSettings["CompanyName"];
        if (string.IsNullOrEmpty(companyName)) companyName = "کارخانه تولید و بسته‌بندی مواد غذایی";
        litCompanyName.Text = HttpUtility.HtmlEncode(companyName);

        StringBuilder meta = new StringBuilder();
        string tagline = ConfigurationManager.AppSettings["CompanyTagline"];
        string address = ConfigurationManager.AppSettings["CompanyAddress"];
        string phone = ConfigurationManager.AppSettings["CompanyPhone"];

        if (!string.IsNullOrEmpty(tagline)) meta.Append(HttpUtility.HtmlEncode(tagline));
        if (!string.IsNullOrEmpty(address)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("آدرس: " + HttpUtility.HtmlEncode(address)); }
        if (!string.IsNullOrEmpty(phone)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("تلفن: " + HttpUtility.HtmlEncode(phone)); }
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
    }

    // ─────────────────────────────────────────────────────
    // Main Report
    // ─────────────────────────────────────────────────────
    private void LoadReport()
    {
        try
        {
            DateTime? fromNullable = ParsePersianDate(txtFrom.Text.Trim());
            DateTime? toNullable = ParsePersianDate(txtTo.Text.Trim());

            if (!fromNullable.HasValue) { ShowMessage("تاریخ شروع معتبر نیست."); return; }
            if (!toNullable.HasValue) { ShowMessage("تاریخ پایان معتبر نیست."); return; }
            if (toNullable.Value < fromNullable.Value) { ShowMessage("تاریخ پایان قبل از شروع است."); return; }

            DateTime from = fromNullable.Value.Date;
            DateTime to = toNullable.Value.Date.AddDays(1).AddSeconds(-1);

            string channelFilter = ddlFilterChannel.SelectedValue;
            if (channelFilter == null) channelFilter = "";

            string marketerFilter = ddlFilterMarketer.SelectedValue;
            if (marketerFilter == null) marketerFilter = "";

            litPrintPeriod.Text = "بازه: " + HttpUtility.HtmlEncode(
                ToShamsiDateString(from) + " تا " + ToShamsiDateString(toNullable.Value));

            // ۱) KPIs
            LoadKpis(from, to, channelFilter, marketerFilter);

            // ۲) By Channel
            DataTable dtChannel = LoadByChannel(from, to, channelFilter, marketerFilter);
            BindChannel(dtChannel);

            // ۳) By Marketer
            DataTable dtMarketer = LoadByMarketer(from, to, channelFilter, marketerFilter);
            BindMarketers(dtMarketer);

            // ۴) Top 5
            LoadTopMarketers(dtMarketer);

            // ۵) Chart
            LoadChartData(dtChannel);

            // ۶) پیام راهنما اگر خالی بود
            if (dtMarketer.Rows.Count == 0 && !string.IsNullOrEmpty(marketerFilter))
            {
                ShowMessage("این بازاریاب در بازه انتخابی هیچ فروشی ثبت نکرده است.\n\n" +
                    "راهنما:\n" +
                    "• بازه تاریخ را گسترده‌تر کنید\n" +
                    "• کانال فروش را روی «همه کانال‌ها» بگذارید\n" +
                    "• مطمئن شوید مشتریان به این بازاریاب اختصاص یافته‌اند");
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری گزارش: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // KPIs
    // ─────────────────────────────────────────────────────
    private void LoadKpis(DateTime from, DateTime to, string channelFilter, string marketerFilter)
    {
        decimal revenue = 0m;
        decimal cogs = 0m;
        int invoiceCount = 0;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
                sb.Append("  ISNULL(SUM(fii.COGS), 0) AS Cogs, ");
                sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount ");
                sb.Append("FROM FinalInvoices fi ");
                sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
                sb.Append("WHERE fi.Status IN (1, 2) AND fi.IsDeleted = 0 ");
                sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);

                    // ═══ منطق فیلتر: اگر بازاریاب انتخاب شده، کانال نادیده گرفته می‌شود ═══
                    int mk = 0;
                    bool hasMarketer = marketerFilter.Length > 0
                        && int.TryParse(marketerFilter, out mk) && mk > 0;

                    if (hasMarketer)
                    {
                        sb.Append(" AND fi.MarketerId = @Mk ");
                        cmd.Parameters.AddWithValue("@Mk", mk);
                    }
                    else
                    {
                        int ch;
                        if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                        {
                            sb.Append(" AND fi.Channel = @Ch ");
                            cmd.Parameters.AddWithValue("@Ch", ch);
                        }
                    }

                    cmd.CommandText = sb.ToString();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            revenue = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            cogs = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                            invoiceCount = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                        }
                    }
                }
            }
        }
        catch { }

        decimal profit = revenue - cogs;
        decimal margin = revenue > 0m ? (profit / revenue) * 100m : 0m;
        decimal avgInvoice = invoiceCount > 0 ? Math.Round(revenue / invoiceCount, 0) : 0m;
        decimal cogsPct = revenue > 0m ? (cogs / revenue) * 100m : 0m;

        litKpiRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(revenue));
        litKpiCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(cogs));
        litKpiProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(profit));
        litKpiInvoiceCount.Text = HttpUtility.HtmlEncode(invoiceCount.ToString("N0"));
        litKpiAvgInvoice.Text = HttpUtility.HtmlEncode(FormatMoneyValue(avgInvoice));
        litKpiMargin.Text = HttpUtility.HtmlEncode(
            margin.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪");
        litKpiCogsPct.Text = HttpUtility.HtmlEncode(
            cogsPct.ToString("N1", CultureInfo.InvariantCulture).Replace(",", "،") + "٪");

        // رنگ کارت سود
        if (profit > 0m)
        {
            cardProfit.Attributes["class"] =
                "rounded-card border-2 border-emerald-300 bg-emerald-50/40 p-5 shadow-card print-card";
            pKpiProfit.Attributes["class"] =
                "stat-value font-mono text-[20px] font-black text-emerald-700";
        }
        else if (profit < 0m)
        {
            cardProfit.Attributes["class"] =
                "rounded-card border-2 border-danger-300 bg-danger-50/40 p-5 shadow-card print-card";
            pKpiProfit.Attributes["class"] =
                "stat-value font-mono text-[20px] font-black text-danger-600";
        }
        else
        {
            cardProfit.Attributes["class"] =
                "rounded-card border border-surface-line bg-surface-card p-5 shadow-card print-card";
            pKpiProfit.Attributes["class"] =
                "stat-value font-mono text-[20px] font-black text-ink-500";
        }
    }

    // ─────────────────────────────────────────────────────
    // By Channel
    // ─────────────────────────────────────────────────────
    private DataTable LoadByChannel(DateTime from, DateTime to, string channelFilter, string marketerFilter)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Channel", typeof(int));
        dt.Columns.Add("ChannelName", typeof(string));
        dt.Columns.Add("InvoiceCount", typeof(int));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("Cogs", typeof(decimal));
        dt.Columns.Add("Profit", typeof(decimal));
        dt.Columns.Add("TotalRevenue", typeof(decimal));

        decimal grandTotal = 0m;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT fi.Channel, ");
                sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount, ");
                sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
                sb.Append("  ISNULL(SUM(fii.COGS), 0) AS Cogs ");
                sb.Append("FROM FinalInvoices fi ");
                sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
                sb.Append("WHERE fi.Status IN (1, 2) AND fi.IsDeleted = 0 ");
                sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);

                    // ═══ منطق فیلتر ═══
                    int mk = 0;
                    bool hasMarketer = marketerFilter.Length > 0
                        && int.TryParse(marketerFilter, out mk) && mk > 0;

                    if (hasMarketer)
                    {
                        sb.Append(" AND fi.MarketerId = @Mk ");
                        cmd.Parameters.AddWithValue("@Mk", mk);
                    }
                    else
                    {
                        int ch;
                        if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                        {
                            sb.Append(" AND fi.Channel = @Ch ");
                            cmd.Parameters.AddWithValue("@Ch", ch);
                        }
                    }

                    sb.Append(" GROUP BY fi.Channel ORDER BY SUM(fii.Total) DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int channel = Convert.ToInt32(dr["Channel"]);
                            decimal rev = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            decimal cg = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                            grandTotal += rev;

                            DataRow r = dt.NewRow();
                            r["Channel"] = channel;
                            r["ChannelName"] = GetChannelLabelStatic(channel);
                            r["InvoiceCount"] = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                            r["Revenue"] = rev;
                            r["Cogs"] = cg;
                            r["Profit"] = rev - cg;
                            r["TotalRevenue"] = 0m;
                            dt.Rows.Add(r);
                        }
                    }
                }
            }

            foreach (DataRow row in dt.Rows)
                row["TotalRevenue"] = grandTotal;
        }
        catch { }

        return dt;
    }

    private void BindChannel(DataTable dt)
    {
        if (dt.Rows.Count == 0)
        {
            pnlChannelEmpty.Visible = true;
            pnlChannelTotal.Visible = false;
            rptByChannel.Visible = false;
            rptByChannel.DataSource = null;
            rptByChannel.DataBind();
            return;
        }

        pnlChannelEmpty.Visible = false;
        pnlChannelTotal.Visible = true;
        rptByChannel.Visible = true;
        rptByChannel.DataSource = dt;
        rptByChannel.DataBind();

        int totalCount = 0;
        decimal totalRev = 0m;
        decimal totalCogs = 0m;

        foreach (DataRow r in dt.Rows)
        {
            totalCount += Convert.ToInt32(r["InvoiceCount"]);
            totalRev += Convert.ToDecimal(r["Revenue"]);
            totalCogs += Convert.ToDecimal(r["Cogs"]);
        }

        litChannelTotalCount.Text = HttpUtility.HtmlEncode(totalCount.ToString("N0"));
        litChannelTotalRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRev));
        litChannelTotalCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalCogs));
        litChannelTotalProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRev - totalCogs));
    }

    // ─────────────────────────────────────────────────────
    // By Marketer
    // ─────────────────────────────────────────────────────
    private DataTable LoadByMarketer(DateTime from, DateTime to, string channelFilter, string marketerFilter)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Rank", typeof(int));
        dt.Columns.Add("MarketerId", typeof(int));
        dt.Columns.Add("MarketerName", typeof(string));
        dt.Columns.Add("Channel", typeof(int));
        dt.Columns.Add("ChannelName", typeof(string));
        dt.Columns.Add("InvoiceCount", typeof(int));
        dt.Columns.Add("Revenue", typeof(decimal));
        dt.Columns.Add("Cogs", typeof(decimal));
        dt.Columns.Add("Profit", typeof(decimal));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append("  ISNULL(fi.MarketerId, 0) AS MarketerId, ");
                sb.Append("  ISNULL(u.FullName, ISNULL(u.Username, N'—')) AS MarketerName, ");
                sb.Append("  fi.Channel, ");
                sb.Append("  COUNT(DISTINCT fi.FinalInvoiceId) AS InvoiceCount, ");
                sb.Append("  ISNULL(SUM(fii.Total), 0) AS Revenue, ");
                sb.Append("  ISNULL(SUM(fii.COGS), 0) AS Cogs ");
                sb.Append("FROM FinalInvoices fi ");
                sb.Append("INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId ");
                sb.Append("LEFT JOIN Users u ON u.UserId = fi.MarketerId ");
                sb.Append("WHERE fi.Status IN (1, 2) AND fi.IsDeleted = 0 ");
                sb.Append("  AND fi.IssueDate >= @From AND fi.IssueDate <= @To ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);

                    // ═══ منطق فیلتر ═══
                    int mk = 0;
                    bool hasMarketer = marketerFilter.Length > 0
                        && int.TryParse(marketerFilter, out mk) && mk > 0;

                    if (hasMarketer)
                    {
                        sb.Append(" AND fi.MarketerId = @Mk ");
                        cmd.Parameters.AddWithValue("@Mk", mk);
                    }
                    else
                    {
                        int ch;
                        if (channelFilter.Length > 0 && int.TryParse(channelFilter, out ch) && ch > 0)
                        {
                            sb.Append(" AND fi.Channel = @Ch ");
                            cmd.Parameters.AddWithValue("@Ch", ch);
                        }
                    }

                    sb.Append(" GROUP BY ISNULL(fi.MarketerId, 0), ");
                    sb.Append("  ISNULL(u.FullName, ISNULL(u.Username, N'—')), fi.Channel ");
                    sb.Append(" ORDER BY SUM(fii.Total) DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (dr.Read())
                        {
                            int mkId = Convert.ToInt32(dr["MarketerId"]);
                            string mkName = Convert.ToString(dr["MarketerName"]);
                            int channel = Convert.ToInt32(dr["Channel"]);
                            decimal rev = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            decimal cg = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);

                            string displayName = mkName;
                            if (mkId == 0 || string.IsNullOrEmpty(mkName) || mkName == "—")
                                displayName = "بدون بازاریاب (فروش مستقیم)";

                            DataRow r = dt.NewRow();
                            r["Rank"] = rank++;
                            r["MarketerId"] = mkId;
                            r["MarketerName"] = displayName;
                            r["Channel"] = channel;
                            r["ChannelName"] = GetChannelLabelStatic(channel);
                            r["InvoiceCount"] = dr["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["InvoiceCount"]);
                            r["Revenue"] = rev;
                            r["Cogs"] = cg;
                            r["Profit"] = rev - cg;
                            dt.Rows.Add(r);
                        }
                    }
                }
            }
        }
        catch { }

        return dt;
    }

    private void BindMarketers(DataTable dt)
    {
        litMarketerCount.Text = HttpUtility.HtmlEncode(dt.Rows.Count.ToString("N0"));

        if (dt.Rows.Count == 0)
        {
            pnlMarketerEmpty.Visible = true;
            rptByMarketer.Visible = false;
            rptByMarketer.DataSource = null;
            rptByMarketer.DataBind();
            return;
        }

        pnlMarketerEmpty.Visible = false;
        rptByMarketer.Visible = true;
        rptByMarketer.DataSource = dt;
        rptByMarketer.DataBind();
    }

    // ─────────────────────────────────────────────────────
    // Top 5 Marketers
    // ─────────────────────────────────────────────────────
    private void LoadTopMarketers(DataTable dt)
    {
        if (dt.Rows.Count == 0)
        {
            pnlTopMarketers.Visible = false;
            return;
        }

        DataTable dtSorted = dt.Clone();
        DataView dv = dt.DefaultView;
        dv.Sort = "Profit DESC";

        int count = 0;
        foreach (DataRowView rv in dv)
        {
            if (count >= 5) break;
            dtSorted.ImportRow(rv.Row);
            count++;
        }

        pnlTopMarketers.Visible = dtSorted.Rows.Count > 0;
        rptTopMarketers.DataSource = dtSorted;
        rptTopMarketers.DataBind();
    }

    // ─────────────────────────────────────────────────────
    // Chart Data
    // ─────────────────────────────────────────────────────
    private void LoadChartData(DataTable dtChannel)
    {
        List<string> labels = new List<string>();
        List<decimal> revenue = new List<decimal>();
        List<decimal> cogs = new List<decimal>();
        List<decimal> profit = new List<decimal>();

        foreach (DataRow r in dtChannel.Rows)
        {
            labels.Add(Convert.ToString(r["ChannelName"]));
            revenue.Add(Convert.ToDecimal(r["Revenue"]));
            cogs.Add(Convert.ToDecimal(r["Cogs"]));
            profit.Add(Convert.ToDecimal(r["Profit"]));
        }

        hfChartLabels.Value = BuildJsonStringArray(labels);
        hfChartRevenue.Value = BuildJsonDecimalArray(revenue);
        hfChartCogs.Value = BuildJsonDecimalArray(cogs);
        hfChartProfit.Value = BuildJsonDecimalArray(profit);
    }

    private string BuildJsonStringArray(List<string> items)
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
    // Filter events
    // ─────────────────────────────────────────────────────
    protected void btnApply_Click(object sender, EventArgs e) { LoadReport(); }

    protected void btnClear_Click(object sender, EventArgs e)
    {
        SetDefaultRange();
        if (ddlFilterChannel.Items.Count > 0) ddlFilterChannel.SelectedIndex = 0;
        if (ddlFilterMarketer.Items.Count > 0) ddlFilterMarketer.SelectedIndex = 0;
        LoadReport();
    }

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
        else if (arg == "all")
        {
            from = GetMinInvoiceDate();
            to = GetMaxInvoiceDate();
        }
        else return;

        txtFrom.Text = ToShamsiDateString(from);
        txtTo.Text = ToShamsiDateString(to);
        LoadReport();
    }

    // ─────────────────────────────────────────────────────
    // Helpers — Min/Max dates from data
    // ─────────────────────────────────────────────────────
    private DateTime GetMinInvoiceDate()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MIN(IssueDate) FROM FinalInvoices WHERE IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value)
                        ? DateTime.Now.Date.AddYears(-5)
                        : Convert.ToDateTime(o).Date;
                }
            }
        }
        catch { return DateTime.Now.Date.AddYears(-5); }
    }

    private DateTime GetMaxInvoiceDate()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MAX(IssueDate) FROM FinalInvoices WHERE IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value)
                        ? DateTime.Now.Date
                        : Convert.ToDateTime(o).Date;
                }
            }
        }
        catch { return DateTime.Now.Date; }
    }

    // ─────────────────────────────────────────────────────
    // Markup Helpers
    // ─────────────────────────────────────────────────────
    protected string Enc(object value)
    {
        if (value == null || value == DBNull.Value) return "";
        return HttpUtility.HtmlEncode(value.ToString());
    }

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
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
        bool neg = d < 0m;
        decimal abs = Math.Abs(d);
        string s = abs.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return neg ? "(" + s + ")" : s;
    }

    protected string FormatPercent(object profitObj, object revenueObj)
    {
        try
        {
            decimal p = (profitObj == null || profitObj == DBNull.Value) ? 0m : Convert.ToDecimal(profitObj);
            decimal r = (revenueObj == null || revenueObj == DBNull.Value) ? 0m : Convert.ToDecimal(revenueObj);
            if (r == 0m) return "—";
            decimal pct = (p / r) * 100m;
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

    private string GetChannelLabelStatic(int channel)
    {
        if (channel == 1) return "بازاریاب";
        if (channel == 2) return "آنلاین";
        if (channel == 3) return "ادمین / دفتری";
        return "نامشخص";
    }

    protected string GetChannelBadgeCss(object channelObj)
    {
        int ch = (channelObj == null || channelObj == DBNull.Value) ? 0 : Convert.ToInt32(channelObj);
        if (ch == 1) return "badge bg-brand-50 text-brand-700";
        if (ch == 2) return "badge bg-violet-50 text-violet-700";
        if (ch == 3) return "badge bg-surface-muted text-ink-600";
        return "badge bg-surface-muted text-ink-500";
    }

    protected string GetChannelIcon(object channelObj)
    {
        int ch = (channelObj == null || channelObj == DBNull.Value) ? 0 : Convert.ToInt32(channelObj);
        if (ch == 1) return "megaphone";
        if (ch == 2) return "globe";
        if (ch == 3) return "briefcase";
        return "help-circle";
    }

    protected string GetRankBadgeCss(object rankObj)
    {
        int rank = (rankObj == null || rankObj == DBNull.Value) ? 0 : Convert.ToInt32(rankObj);
        if (rank == 1) return "rank-badge rank-1";
        if (rank == 2) return "rank-badge rank-2";
        if (rank == 3) return "rank-badge rank-3";
        return "rank-badge rank-other";
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
        string safe = message.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", "\\n");
        string script = "alert('" + safe + "');";
        if (ScriptManager.GetCurrent(this) != null)
            ScriptManager.RegisterStartupScript(this, GetType(), "msg_" + Guid.NewGuid().ToString("N"), script, true);
        else
            ClientScript.RegisterStartupScript(GetType(), "msg_" + Guid.NewGuid().ToString("N"), script, true);
    }
}