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

public partial class admin_profit_periods : System.Web.UI.Page
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

    private int FilterYear
    {
        get
        {
            int y;
            if (int.TryParse(hfFilterYear.Value, out y)) return y;
            return 0;
        }
        set { hfFilterYear.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int PreviewPeriodId
    {
        get { return ViewState["PrevPeriodId"] == null ? 0 : (int)ViewState["PrevPeriodId"]; }
        set { ViewState["PrevPeriodId"] = value; }
    }

    private int ActiveDistributionId
    {
        get { return ViewState["ActiveDistId"] == null ? 0 : (int)ViewState["ActiveDistId"]; }
        set { ViewState["ActiveDistId"] = value; }
    }

    private int ActivePaymentItemId
    {
        get
        {
            int id;
            if (int.TryParse(hfActivePaymentItemId.Value, out id)) return id;
            return 0;
        }
        set { hfActivePaymentItemId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            string distParam = Request.QueryString["dist"];
            int distId;
            if (!string.IsNullOrEmpty(distParam) && int.TryParse(distParam, out distId) && distId > 0)
            {
                ShowDetailView(distId);
            }
            else
            {
                ShowListView();
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // View Management
    // ─────────────────────────────────────────────────────
    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlPreviewView.Visible = false;
        pnlDetailView.Visible = false;

        phBackToList.Visible = false;
        PreviewPeriodId = 0;
        ActiveDistributionId = 0;
        ActivePaymentItemId = 0;

        litPageTitle.Text = "دوره‌های تقسیم سود";
        litPageSubtitle.Text = "دوره‌ها را ببندید و سود شرکا را محاسبه و تقسیم کنید.";

        LoadStats();
        LoadPeriods();
    }

    private void ShowPreviewView(int periodId)
    {
        DataRow period = GetPeriodRow(periodId);
        if (period == null)
        {
            ShowMessage("دوره مورد نظر یافت نشد.");
            ShowListView();
            return;
        }

        int status = Convert.ToInt32(period["Status"]);
        if (status != 2)
        {
            ShowMessage("فقط دوره‌های بسته‌شده قابل تقسیم هستند.");
            ShowListView();
            return;
        }

        PreviewPeriodId = periodId;

        pnlListView.Visible = false;
        pnlPreviewView.Visible = true;
        pnlDetailView.Visible = false;
        phBackToList.Visible = false;

        litPageTitle.Text = "پیش‌نمایش تقسیم سود";
        litPageSubtitle.Text = "اعداد را بررسی کنید و در صورت تأیید، تقسیم را ایجاد نمایید.";

        LoadPreviewData(period);
    }

    private void ShowDetailView(int distributionId)
    {
        DataRow dist = GetDistributionRow(distributionId);
        if (dist == null)
        {
            ShowMessage("سند تقسیم مورد نظر یافت نشد.");
            ShowListView();
            return;
        }

        ActiveDistributionId = distributionId;

        pnlListView.Visible = false;
        pnlPreviewView.Visible = false;
        pnlDetailView.Visible = true;
        phBackToList.Visible = true;

        litPageTitle.Text = "سند تقسیم سود";
        litPageSubtitle.Text = "جزئیات تقسیم سود و وضعیت پرداخت به شرکا.";

        LoadDistributionDetail(dist);
    }

    // ─────────────────────────────────────────────────────
    // Get Rows
    // ─────────────────────────────────────────────────────
    private DataRow GetPeriodRow(int periodId)
    {
        try
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT PeriodId, PeriodNumber, PeriodType, PeriodFrom, PeriodTo, Status, DistributionId, Notes " +
                    "FROM ProfitPeriods WHERE PeriodId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", periodId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
        catch { return null; }
    }

    private DataRow GetDistributionRow(int distributionId)
    {
        try
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT d.*, pp.PeriodNumber " +
                    "FROM ProfitDistributions d " +
                    "LEFT JOIN ProfitPeriods pp ON pp.DistributionId = d.DistributionId " +
                    "WHERE d.DistributionId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", distributionId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
        catch { return null; }
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
                    "  COUNT(*) AS Total, " +
                    "  SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS OpenCount, " +
                    "  SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS ClosedCount, " +
                    "  SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS DistributedCount " +
                    "FROM ProfitPeriods";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatOpen.Text = HttpUtility.HtmlEncode(ToInt(dr["OpenCount"]).ToString("N0"));
                        litStatClosed.Text = HttpUtility.HtmlEncode(ToInt(dr["ClosedCount"]).ToString("N0"));
                        litStatDistributed.Text = HttpUtility.HtmlEncode(ToInt(dr["DistributedCount"]).ToString("N0"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری آمار: " + ex.Message);
        }
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        return Convert.ToInt32(o);
    }

    // ─────────────────────────────────────────────────────
    // Periods List
    // ─────────────────────────────────────────────────────
    private void LoadPeriods()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT pp.PeriodId, pp.PeriodNumber, pp.PeriodType, " +
                    "       pp.PeriodFrom, pp.PeriodTo, pp.Status, pp.Notes, " +
                    "       pp.DistributionId, " +
                    "       pd.DistributionNumber, pd.NetProfit " +
                    "FROM ProfitPeriods pp " +
                    "LEFT JOIN ProfitDistributions pd ON pd.DistributionId = pp.DistributionId ";

                if (FilterYear > 0)
                    sql += "WHERE pp.PeriodNumber LIKE '%' + @Suffix ";

                sql += "ORDER BY pp.PeriodFrom DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (FilterYear > 0)
                        cmd.Parameters.AddWithValue("@Suffix", "-" + FilterYear.ToString(CultureInfo.InvariantCulture));

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlListEmpty.Visible = true;
                rptPeriods.Visible = false;
                rptPeriods.DataSource = null;
                rptPeriods.DataBind();
            }
            else
            {
                pnlListEmpty.Visible = false;
                rptPeriods.Visible = true;
                rptPeriods.DataSource = dt;
                rptPeriods.DataBind();
            }

            litListInfo.Text = string.Format("{0} دوره{1}",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture),
                FilterYear > 0 ? " در سال " + FilterYear.ToString(CultureInfo.InvariantCulture) : "");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری دوره‌ها: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Preview Data
    // ─────────────────────────────────────────────────────
    private void LoadPreviewData(DataRow period)
    {
        DateTime periodFrom = Convert.ToDateTime(period["PeriodFrom"]);
        DateTime periodTo = Convert.ToDateTime(period["PeriodTo"]);
        string periodNumber = Convert.ToString(period["PeriodNumber"]);

        litPrevPeriodNumber.Text = HttpUtility.HtmlEncode(periodNumber);
        litPrevPeriodDates.Text = HttpUtility.HtmlEncode(
            "از " + ToShamsiDateString(periodFrom) + " تا " + ToShamsiDateString(periodTo));

        // Compute P&L (product + service)
        decimal productRevenue, serviceRevenue, cogs, expenses, netProfit;
        ComputePeriodPnLDetailed(periodFrom, periodTo,
            out productRevenue, out serviceRevenue, out cogs, out expenses, out netProfit);

        decimal totalRevenue = productRevenue + serviceRevenue;

        litPrevProductRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(productRevenue));
        litPrevServiceRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(serviceRevenue));
        litPrevRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue));

        litPrevCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(cogs));
        litPrevGrossProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue - cogs));
        litPrevExpenses.Text = HttpUtility.HtmlEncode(FormatMoneyValue(expenses));
        litPrevNetProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(netProfit));

        // Loss carry forward
        decimal lossFromBefore = GetRunningLossCarryForward(periodFrom);
        decimal adjustedProfit = netProfit - lossFromBefore;

        if (lossFromBefore > 0)
        {
            pnlLossCarry.Visible = true;
            litPrevLossCarry.Text = HttpUtility.HtmlEncode(FormatMoneyValue(lossFromBefore));
        }
        else
        {
            pnlLossCarry.Visible = false;
            litPrevLossCarry.Text = "۰";
        }

        litPrevAdjustedProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(adjustedProfit));

        // Reserve
        if (string.IsNullOrEmpty(txtReservePercent.Text))
        {
            decimal defaultReserve = GetSettingDecimal("MinReservePercent", 0m);
            txtReservePercent.Text = defaultReserve.ToString("N0", CultureInfo.InvariantCulture);
        }

        decimal reservePct = 0m;
        decimal? rp = ParseDecimal(txtReservePercent.Text);
        if (rp.HasValue && rp.Value >= 0 && rp.Value <= 100)
            reservePct = rp.Value;

        decimal reserveAmount = 0m;
        decimal distributable = 0m;
        if (adjustedProfit > 0)
        {
            reserveAmount = Math.Round(adjustedProfit * reservePct / 100m, 2);
            distributable = adjustedProfit - reserveAmount;
        }

        litPrevReserveAmount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(reserveAmount));
        litPrevDistributable.Text = HttpUtility.HtmlEncode(FormatMoneyValue(distributable));

        if (distributable <= 0)
        {
            pnlPreviewNoProfit.Visible = true;
            pnlPreviewNoPartners.Visible = false;
            rptPreviewItems.Visible = false;
            rptPreviewItems.DataSource = null;
            rptPreviewItems.DataBind();
            litPrevPartnerCount.Text = "۰";
            pnlCompanyRetained.Visible = false;
            return;
        }

        pnlPreviewNoProfit.Visible = false;

        // Partners
        DataTable dtPartners = ComputePreviewItems(periodFrom, periodTo, distributable);

        if (dtPartners.Rows.Count == 0)
        {
            pnlPreviewNoPartners.Visible = true;
            rptPreviewItems.Visible = false;
            rptPreviewItems.DataSource = null;
            rptPreviewItems.DataBind();
            litPrevPartnerCount.Text = "۰";
            pnlCompanyRetained.Visible = false;
        }
        else
        {
            pnlPreviewNoPartners.Visible = false;
            rptPreviewItems.Visible = true;
            rptPreviewItems.DataSource = dtPartners;
            rptPreviewItems.DataBind();
            litPrevPartnerCount.Text = HttpUtility.HtmlEncode(dtPartners.Rows.Count.ToString("N0"));

            decimal totalShares = 0m;
            foreach (DataRow r in dtPartners.Rows)
                totalShares += Convert.ToDecimal(r["ShareAmount"]);

            decimal companyRetained = distributable - totalShares;

            if (companyRetained > 0.01m)
            {
                pnlCompanyRetained.Visible = true;
                litPrevCompanyRetained.Text = HttpUtility.HtmlEncode(FormatMoneyValue(companyRetained));
            }
            else
            {
                pnlCompanyRetained.Visible = false;
            }
        }
    }

    private DataTable ComputePreviewItems(DateTime periodFrom, DateTime periodTo, decimal distributable)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("PartnerId", typeof(int));
        dt.Columns.Add("FullName", typeof(string));
        dt.Columns.Add("EffectiveShare", typeof(decimal));
        dt.Columns.Add("ShareAmount", typeof(decimal));
        dt.Columns.Add("OpenDrawings", typeof(decimal));
        dt.Columns.Add("AppliedAmount", typeof(decimal));
        dt.Columns.Add("NetPayable", typeof(decimal));
        dt.Columns.Add("RemainingOpen", typeof(decimal));

        List<int> partnerIds = new List<int>();
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT PartnerId FROM Partners WHERE IsActive = 1 AND IsDeleted = 0", conn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                    partnerIds.Add(Convert.ToInt32(dr["PartnerId"]));
            }
        }

        List<decimal> shares = new List<decimal>();
        List<string> names = new List<string>();

        foreach (int pid in partnerIds)
        {
            decimal eff = ComputeEffectiveSharePercent(pid, periodFrom, periodTo);
            shares.Add(eff);
            names.Add(GetPartnerName(pid));
        }

        for (int i = 0; i < partnerIds.Count; i++)
        {
            decimal eff = shares[i];
            if (eff <= 0m) continue;

            decimal shareAmount = Math.Round(distributable * eff / 100m, 2);

            decimal openDrawings = GetPartnerOpenDrawings(partnerIds[i]);
            decimal applied = Math.Min(openDrawings, shareAmount);
            decimal netPayable = shareAmount - applied;
            decimal remaining = openDrawings - applied;

            DataRow row = dt.NewRow();
            row["PartnerId"] = partnerIds[i];
            row["FullName"] = names[i];
            row["EffectiveShare"] = Math.Round(eff, 2);
            row["ShareAmount"] = shareAmount;
            row["OpenDrawings"] = openDrawings;
            row["AppliedAmount"] = applied;
            row["NetPayable"] = netPayable;
            row["RemainingOpen"] = remaining;
            dt.Rows.Add(row);
        }

        return dt;
    }

    private decimal ComputeEffectiveSharePercent(int partnerId, DateTime periodFrom, DateTime periodTo)
    {
        decimal totalDays = (decimal)(periodTo.Date - periodFrom.Date).Days + 1m;
        if (totalDays <= 0m) return 0m;

        decimal weightedSum = 0m;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT SharePercent, EffectiveFrom, EffectiveTo " +
                    "FROM PartnerShareHistory " +
                    "WHERE PartnerId = @PartnerId " +
                    "  AND (EffectiveTo IS NULL OR EffectiveTo >= @From) " +
                    "  AND EffectiveFrom <= @To";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                    cmd.Parameters.AddWithValue("@From", periodFrom);
                    cmd.Parameters.AddWithValue("@To", periodTo);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            decimal pct = Convert.ToDecimal(dr["SharePercent"]);
                            DateTime effFrom = Convert.ToDateTime(dr["EffectiveFrom"]);
                            DateTime effTo = dr["EffectiveTo"] == DBNull.Value
                                ? periodTo : Convert.ToDateTime(dr["EffectiveTo"]);

                            DateTime overlapFrom = effFrom > periodFrom ? effFrom : periodFrom;
                            DateTime overlapTo = effTo < periodTo ? effTo : periodTo;

                            if (overlapTo.Date < overlapFrom.Date) continue;

                            decimal overlapDays = (decimal)(overlapTo.Date - overlapFrom.Date).Days + 1m;
                            weightedSum += pct * overlapDays;
                        }
                    }
                }
            }
        }
        catch { return 0m; }

        return weightedSum / totalDays;
    }

    private string GetPartnerName(int partnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT FullName FROM Partners WHERE PartnerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? "—" : Convert.ToString(o);
                }
            }
        }
        catch { return "—"; }
    }

    private decimal GetPartnerOpenDrawings(int partnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM PartnerDrawings " +
                    "WHERE PartnerId = @Id AND Status = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }
            }
        }
        catch { return 0m; }
    }

    // ─────────────────────────────────────────────────────
    // P&L for Period (Detailed — product + service)
    // ─────────────────────────────────────────────────────
    private void ComputePeriodPnLDetailed(DateTime from, DateTime to,
        out decimal productRevenue, out decimal serviceRevenue,
        out decimal cogs, out decimal expenses, out decimal netProfit)
    {
        productRevenue = 0m;
        serviceRevenue = 0m;
        cogs = 0m;
        expenses = 0m;
        netProfit = 0m;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Product revenue + COGS
                string sql1 =
                    "SELECT " +
                    "  ISNULL(SUM(fii.Total), 0) AS Revenue, " +
                    "  ISNULL(SUM(fii.COGS), 0) AS Cogs " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate <= @To";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            productRevenue = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            cogs = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                        }
                    }
                }

                // Service revenue
                string sql2 =
                    "SELECT ISNULL(SUM(Total), 0) FROM ServiceOrders " +
                    "WHERE IsDeleted = 0 AND Status = 3 " +
                    "  AND ServiceDate >= @From AND ServiceDate <= @To";

                using (SqlCommand cmd = new SqlCommand(sql2, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    object o = cmd.ExecuteScalar();
                    serviceRevenue = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Expenses
                string sql3 =
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate <= @To";

                using (SqlCommand cmd = new SqlCommand(sql3, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    object o = cmd.ExecuteScalar();
                    expenses = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                netProfit = (productRevenue + serviceRevenue) - cogs - expenses;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("خطا در محاسبه P&L: " + ex.Message);
        }
    }

    // Simple version — kept for CreateDistribution
    private void ComputePeriodPnL(DateTime from, DateTime to,
        out decimal revenue, out decimal cogs, out decimal expenses, out decimal netProfit)
    {
        decimal productRev = 0m;
        decimal serviceRev = 0m;
        ComputePeriodPnLDetailed(from, to, out productRev, out serviceRev, out cogs, out expenses, out netProfit);
        revenue = productRev + serviceRev;
    }

    // ─────────────────────────────────────────────────────
    // Loss Carry Forward
    // ─────────────────────────────────────────────────────
    private decimal GetRunningLossCarryForward(DateTime beforeDate)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT TOP 1 LossCarriedForward FROM ProfitDistributions " +
                    "WHERE PeriodTo < @BeforeDate " +
                    "  AND Status IN (2, 3) " +
                    "ORDER BY PeriodTo DESC, DistributionId DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BeforeDate", beforeDate);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return 0m;
                    return Convert.ToDecimal(o);
                }
            }
        }
        catch { return 0m; }
    }

    // ─────────────────────────────────────────────────────
    // Period Commands
    // ─────────────────────────────────────────────────────
    protected void rptPeriods_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Close") ClosePeriod(id);
        else if (e.CommandName == "Distribute") ShowPreviewView(id);
        else if (e.CommandName == "ViewDistribution") ShowDetailView(id);
        else if (e.CommandName == "Reopen") ReopenPeriod(id);
        else if (e.CommandName == "DeletePeriod") DeletePeriod(id);
    }

    private void ClosePeriod(int periodId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT PeriodNumber, Status FROM ProfitPeriods WHERE PeriodId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", periodId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["PeriodNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                        }
                    }
                }

                if (status != 1)
                {
                    ShowMessage("این دوره در وضعیت قابل بستن نیست.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProfitPeriods SET Status = 2, ClosedAt = GETDATE(), ClosedBy = @By " +
                    "WHERE PeriodId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", periodId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Close", "ProfitPeriods", periodId, number, "بستن دوره");
            }

            ShowMessage("دوره با موفقیت بسته شد.");
            LoadStats();
            LoadPeriods();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    private void ReopenPeriod(int periodId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                int distId = 0;
                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT PeriodNumber, Status, ISNULL(DistributionId, 0) AS DistId " +
                    "FROM ProfitPeriods WHERE PeriodId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", periodId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["PeriodNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                            distId = Convert.ToInt32(dr["DistId"]);
                        }
                    }
                }

                if (status != 2 || distId > 0)
                {
                    ShowMessage("این دوره قابل بازگشایی نیست.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProfitPeriods SET Status = 1, ClosedAt = NULL, ClosedBy = NULL " +
                    "WHERE PeriodId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", periodId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Reopen", "ProfitPeriods", periodId, number, "بازگشایی دوره");
            }

            ShowMessage("دوره با موفقیت بازگشایی شد.");
            LoadStats();
            LoadPeriods();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    private void DeletePeriod(int periodId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT PeriodNumber, Status FROM ProfitPeriods WHERE PeriodId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", periodId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["PeriodNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                        }
                    }
                }

                if (status == 3)
                {
                    ShowMessage("این دوره تقسیم‌شده است و قابل حذف نیست.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ProfitPeriods WHERE PeriodId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", periodId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "ProfitPeriods", periodId, number, "حذف دوره");
            }

            ShowMessage("دوره با موفقیت حذف شد.");
            LoadStats();
            LoadPeriods();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Preview Actions
    // ─────────────────────────────────────────────────────
    protected void txtReservePercent_TextChanged(object sender, EventArgs e)
    {
        if (PreviewPeriodId > 0)
        {
            DataRow period = GetPeriodRow(PreviewPeriodId);
            if (period != null)
                LoadPreviewData(period);
        }
    }

    protected void btnCancelPreview_Click(object sender, EventArgs e)
    {
        PreviewPeriodId = 0;
        ShowListView();
    }

    protected void btnConfirmDistribute_Click(object sender, EventArgs e)
    {
        int periodId = PreviewPeriodId;
        if (periodId <= 0)
        {
            ShowMessage("دوره‌ای برای تقسیم انتخاب نشده است.");
            return;
        }

        DataRow period = GetPeriodRow(periodId);
        if (period == null || Convert.ToInt32(period["Status"]) != 2)
        {
            ShowMessage("دوره معتبر نیست یا قابل تقسیم نمی‌باشد.");
            ShowListView();
            return;
        }

        decimal reservePct = 0m;
        decimal? rp = ParseDecimal(txtReservePercent.Text);
        if (rp.HasValue && rp.Value >= 0 && rp.Value <= 100)
            reservePct = rp.Value;

        try
        {
            int newDistId = CreateDistribution(period, reservePct);
            ShowMessage("تقسیم سود با موفقیت ایجاد شد.");
            ShowDetailView(newDistId);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ایجاد تقسیم: " + ex.Message);
        }
    }

    private int CreateDistribution(DataRow period, decimal reservePercent)
    {
        int periodId = Convert.ToInt32(period["PeriodId"]);
        DateTime periodFrom = Convert.ToDateTime(period["PeriodFrom"]);
        DateTime periodTo = Convert.ToDateTime(period["PeriodTo"]);
        string periodNumber = Convert.ToString(period["PeriodNumber"]);

        decimal productRev, serviceRev, cogs, expenses, netProfit;
        ComputePeriodPnLDetailed(periodFrom, periodTo,
            out productRev, out serviceRev, out cogs, out expenses, out netProfit);
        decimal revenue = productRev + serviceRev;

        decimal lossFromBefore = GetRunningLossCarryForward(periodFrom);
        decimal adjustedProfit = netProfit - lossFromBefore;

        decimal reserveAmount = 0m;
        decimal distributable = 0m;
        decimal lossCarriedForward = 0m;

        if (adjustedProfit > 0)
        {
            reserveAmount = Math.Round(adjustedProfit * reservePercent / 100m, 2);
            distributable = adjustedProfit - reserveAmount;
            lossCarriedForward = 0m;
        }
        else
        {
            lossCarriedForward = lossFromBefore + Math.Abs(netProfit);
            distributable = 0m;
        }

        DataTable dtPartners = distributable > 0
            ? ComputePreviewItems(periodFrom, periodTo, distributable)
            : new DataTable();

        decimal totalShares = 0m;
        foreach (DataRow r in dtPartners.Rows)
            totalShares += Convert.ToDecimal(r["ShareAmount"]);

        decimal companyRetained = distributable - totalShares;
        if (companyRetained < 0) companyRetained = 0m;

        decimal totalDrawingsApplied = 0m;
        foreach (DataRow r in dtPartners.Rows)
            totalDrawingsApplied += Convert.ToDecimal(r["AppliedAmount"]);

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    string distNumber = GenerateDistributionNumber(conn, tran, periodTo);

                    string insertDist =
                        "INSERT INTO ProfitDistributions " +
                        "  (DistributionNumber, PeriodFrom, PeriodTo, TotalRevenue, TotalCogs, " +
                        "   TotalExpenses, GrossProfit, NetProfit, ReservedPercent, ReservedAmount, " +
                        "   DistributableAmount, Status, FinalizedAt, FinalizedBy, " +
                        "   AccumulatedLossFromBefore, LossCarriedForward, RetainedByCompany, " +
                        "   PeriodId, TotalDrawingsApplied, Notes, CreatedAt, CreatedBy) " +
                        "VALUES (@Number, @From, @To, @Rev, @Cogs, " +
                        "   @Exp, @Gross, @Net, @ResPct, @ResAmt, " +
                        "   @DistAmt, 2, GETDATE(), @FinalizedBy, " +
                        "   @LossFromBefore, @LossCarryFwd, @CompanyRetained, " +
                        "   @PeriodId, @DrawingsApplied, @Notes, GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newDistId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertDist, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Number", distNumber);
                        cmd.Parameters.AddWithValue("@From", periodFrom);
                        cmd.Parameters.AddWithValue("@To", periodTo);
                        cmd.Parameters.AddWithValue("@Rev", revenue);
                        cmd.Parameters.AddWithValue("@Cogs", cogs);
                        cmd.Parameters.AddWithValue("@Exp", expenses);
                        cmd.Parameters.AddWithValue("@Gross", revenue - cogs);
                        cmd.Parameters.AddWithValue("@Net", netProfit);
                        cmd.Parameters.AddWithValue("@ResPct", reservePercent);
                        cmd.Parameters.AddWithValue("@ResAmt", reserveAmount);
                        cmd.Parameters.AddWithValue("@DistAmt", distributable);
                        cmd.Parameters.AddWithValue("@FinalizedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@LossFromBefore", lossFromBefore);
                        cmd.Parameters.AddWithValue("@LossCarryFwd", lossCarriedForward);
                        cmd.Parameters.AddWithValue("@CompanyRetained", companyRetained);
                        cmd.Parameters.AddWithValue("@PeriodId", periodId);
                        cmd.Parameters.AddWithValue("@DrawingsApplied", totalDrawingsApplied);
                        cmd.Parameters.AddWithValue("@Notes",
                            "تقسیم سود دوره " + periodNumber);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value)
                            newDistId = Convert.ToInt32(r);
                    }

                    if (newDistId <= 0)
                        throw new Exception("خطا در ایجاد سند تقسیم.");

                    foreach (DataRow r in dtPartners.Rows)
                    {
                        int partnerId = Convert.ToInt32(r["PartnerId"]);
                        decimal effShare = Convert.ToDecimal(r["EffectiveShare"]);
                        decimal shareAmount = Convert.ToDecimal(r["ShareAmount"]);
                        decimal appliedAmount = Convert.ToDecimal(r["AppliedAmount"]);

                        string insertItem =
                            "INSERT INTO ProfitDistributionItems " +
                            "  (DistributionId, PartnerId, SharePercent, ShareAmount, PaidAmount) " +
                            "VALUES (@DistId, @PartnerId, @SharePercent, @ShareAmount, @PaidAmount); " +
                            "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int newItemId = 0;
                        using (SqlCommand cmd = new SqlCommand(insertItem, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@DistId", newDistId);
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.Parameters.AddWithValue("@SharePercent", effShare);
                            cmd.Parameters.AddWithValue("@ShareAmount", shareAmount);
                            cmd.Parameters.AddWithValue("@PaidAmount", appliedAmount);
                            object r2 = cmd.ExecuteScalar();
                            if (r2 != null && r2 != DBNull.Value)
                                newItemId = Convert.ToInt32(r2);
                        }

                        if (appliedAmount > 0m && newItemId > 0)
                        {
                            ApplyDrawings(conn, tran, partnerId, newDistId, newItemId, appliedAmount);
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE ProfitPeriods SET Status = 3, DistributionId = @DistId WHERE PeriodId = @Id",
                        conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", periodId);
                        cmd.Parameters.AddWithValue("@DistId", newDistId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAuditTran(conn, tran, "Create", "ProfitDistributions", newDistId,
                        distNumber, "ایجاد تقسیم سود دوره " + periodNumber);

                    tran.Commit();
                    return newDistId;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
    }

    private void ApplyDrawings(SqlConnection conn, SqlTransaction tran,
        int partnerId, int distributionId, int itemId, decimal maxAmount)
    {
        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            "SELECT DrawingId, Amount FROM PartnerDrawings " +
            "WHERE PartnerId = @PartnerId AND Status = 1 " +
            "ORDER BY DrawingDate, DrawingId", conn, tran))
        {
            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
        }

        decimal remaining = maxAmount;

        foreach (DataRow r in dt.Rows)
        {
            if (remaining <= 0m) break;

            int drawingId = Convert.ToInt32(r["DrawingId"]);
            decimal amount = Convert.ToDecimal(r["Amount"]);

            if (amount <= remaining)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PartnerDrawings SET Status = 2, " +
                    "  AppliedDistributionId = @DistId, AppliedItemId = @ItemId " +
                    "WHERE DrawingId = @Id", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Id", drawingId);
                    cmd.Parameters.AddWithValue("@DistId", distributionId);
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.ExecuteNonQuery();
                }
                remaining -= amount;
            }
            else
            {
                break;
            }
        }
    }

    private string GenerateDistributionNumber(SqlConnection conn, SqlTransaction tran, DateTime periodTo)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(periodTo);

        string sql =
            "SELECT ISNULL(MAX(CAST(SUBSTRING(DistributionNumber, 9, 4) AS INT)), 0) + 1 " +
            "FROM ProfitDistributions WHERE DistributionNumber LIKE 'PD-' + @YearPrefix + '-%'";

        using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
        {
            cmd.Parameters.AddWithValue("@YearPrefix", year.ToString(CultureInfo.InvariantCulture));
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return string.Format(CultureInfo.InvariantCulture, "PD-{0}-{1:D4}", year, seq);
        }
    }

    // ─────────────────────────────────────────────────────
    // Distribution Detail
    // ─────────────────────────────────────────────────────
    private void LoadDistributionDetail(DataRow dist)
    {
        int distId = Convert.ToInt32(dist["DistributionId"]);
        string number = Convert.ToString(dist["DistributionNumber"]);
        string periodNumber = dist["PeriodNumber"] == DBNull.Value
            ? "" : Convert.ToString(dist["PeriodNumber"]);

        DateTime periodFrom = Convert.ToDateTime(dist["PeriodFrom"]);
        DateTime periodTo = Convert.ToDateTime(dist["PeriodTo"]);

        litDistNumber.Text = HttpUtility.HtmlEncode(number);
        litDistDates.Text = HttpUtility.HtmlEncode(
            "دوره " + periodNumber + " — از " + ToShamsiDateString(periodFrom) +
            " تا " + ToShamsiDateString(periodTo));

        int status = Convert.ToInt32(dist["Status"]);
        if (status == 1)
            litDistStatus.Text = "<span class=\"badge bg-surface-muted text-ink-600\">پیش‌نویس</span>";
        else if (status == 2)
            litDistStatus.Text = "<span class=\"badge bg-emerald-100 text-emerald-700\">نهایی‌شده</span>";
        else
            litDistStatus.Text = "<span class=\"badge bg-danger-50 text-danger-600\">لغو‌شده</span>";

        litDistRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["TotalRevenue"])));
        litDistCogs.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["TotalCogs"])));
        litDistGross.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["GrossProfit"])));
        litDistExpenses.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["TotalExpenses"])));
        litDistNetProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["NetProfit"])));
        litDistDistributable.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["DistributableAmount"])));

        litDistReserve.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["ReservedAmount"])));
        litDistDrawingsApplied.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["TotalDrawingsApplied"])));
        litDistCompanyRetained.Text = HttpUtility.HtmlEncode(FormatMoneyValue(ToDecimal(dist["RetainedByCompany"])));

        litDistCreatedAt.Text = HttpUtility.HtmlEncode(FormatShamsiDateTime(Convert.ToDateTime(dist["CreatedAt"])));
        litDistFinalizedInfo.Text = dist["FinalizedAt"] == DBNull.Value
            ? ""
            : "<span><i class=\"inline h-3.5 w-3.5 align-middle ml-1\" data-lucide=\"check-circle-2\"></i>نهایی‌سازی: <strong class=\"text-ink-700\">" +
              HttpUtility.HtmlEncode(FormatShamsiDateTime(Convert.ToDateTime(dist["FinalizedAt"]))) + "</strong></span>";

        LoadDistributionItems(distId);
    }

    private decimal ToDecimal(object o)
    {
        if (o == null || o == DBNull.Value) return 0m;
        return Convert.ToDecimal(o);
    }

    private void LoadDistributionItems(int distId)
    {
        DataTable dt = new DataTable();
        decimal totalShares = 0m;
        decimal totalPaid = 0m;

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            string sql =
                "SELECT i.ItemId, i.PartnerId, i.SharePercent, i.ShareAmount, i.PaidAmount, " +
                "       i.PaidAt, i.PaymentMethod, i.ReferenceNumber, i.Notes, " +
                "       p.FullName " +
                "FROM ProfitDistributionItems i " +
                "INNER JOIN Partners p ON p.PartnerId = i.PartnerId " +
                "WHERE i.DistributionId = @Id " +
                "ORDER BY i.ShareAmount DESC";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", distId);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
        }

        DataTable dtDisplay = new DataTable();
        dtDisplay.Columns.Add("ItemId", typeof(int));
        dtDisplay.Columns.Add("PartnerId", typeof(int));
        dtDisplay.Columns.Add("FullName", typeof(string));
        dtDisplay.Columns.Add("SharePercent", typeof(decimal));
        dtDisplay.Columns.Add("ShareAmount", typeof(decimal));
        dtDisplay.Columns.Add("PaidAmount", typeof(decimal));
        dtDisplay.Columns.Add("Remaining", typeof(decimal));
        dtDisplay.Columns.Add("PaymentMethod", typeof(string));
        dtDisplay.Columns.Add("ReferenceNumber", typeof(string));
        dtDisplay.Columns.Add("PaidAt", typeof(object));

        foreach (DataRow r in dt.Rows)
        {
            decimal shareAmount = ToDecimal(r["ShareAmount"]);
            decimal paid = ToDecimal(r["PaidAmount"]);
            decimal remaining = shareAmount - paid;
            if (remaining < 0) remaining = 0;

            totalShares += shareAmount;
            totalPaid += paid;

            DataRow nr = dtDisplay.NewRow();
            nr["ItemId"] = r["ItemId"];
            nr["PartnerId"] = r["PartnerId"];
            nr["FullName"] = r["FullName"];
            nr["SharePercent"] = r["SharePercent"];
            nr["ShareAmount"] = shareAmount;
            nr["PaidAmount"] = paid;
            nr["Remaining"] = remaining;
            nr["PaymentMethod"] = r["PaymentMethod"];
            nr["ReferenceNumber"] = r["ReferenceNumber"];
            nr["PaidAt"] = r["PaidAt"];
            dtDisplay.Rows.Add(nr);
        }

        if (dtDisplay.Rows.Count == 0)
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
            rptItems.DataSource = dtDisplay;
            rptItems.DataBind();
        }

        litDistTotalShares.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalShares));
        litDistTotalPaid.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalPaid));
        litDistTotalRemaining.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalShares - totalPaid));
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        ActiveDistributionId = 0;
        ActivePaymentItemId = 0;
        pnlPaymentForm.Visible = false;
        ShowListView();
    }

    // ─────────────────────────────────────────────────────
    // Payment
    // ─────────────────────────────────────────────────────
    protected void rptItems_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "Pay") return;

        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0) return;

        ActivePaymentItemId = itemId;
        LoadPaymentForm(itemId);
        pnlPaymentForm.Visible = true;
    }

    private void LoadPaymentForm(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT i.ItemId, i.ShareAmount, i.PaidAmount, p.FullName " +
                    "FROM ProfitDistributionItems i " +
                    "INNER JOIN Partners p ON p.PartnerId = i.PartnerId " +
                    "WHERE i.ItemId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string name = Convert.ToString(dr["FullName"]);
                            decimal shareAmount = ToDecimal(dr["ShareAmount"]);
                            decimal paid = ToDecimal(dr["PaidAmount"]);
                            decimal remaining = shareAmount - paid;
                            if (remaining < 0) remaining = 0;

                            litPayPartnerName.Text = HttpUtility.HtmlEncode(name);
                            litPayShareAmount.Text = HttpUtility.HtmlEncode(FormatMoneyValue(shareAmount) + " تومان");
                            litPayAlreadyPaid.Text = HttpUtility.HtmlEncode(FormatMoneyValue(paid) + " تومان");
                            litPayRemaining.Text = HttpUtility.HtmlEncode(FormatMoneyValue(remaining) + " تومان");

                            txtPayAmount.Text = remaining.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                            txtPayDate.Text = ToShamsiDateString(DateTime.Now);
                            ddlPayMethod.ClearSelection();
                            txtPayRef.Text = "";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    protected void btnCancelPayment_Click(object sender, EventArgs e)
    {
        ActivePaymentItemId = 0;
        pnlPaymentForm.Visible = false;
    }

    protected void btnSavePayment_Click(object sender, EventArgs e)
    {
        int itemId = ActivePaymentItemId;
        if (itemId <= 0)
        {
            ShowMessage("ابتدا یک قلم را انتخاب کنید.");
            return;
        }

        decimal? amount = ParseDecimal(txtPayAmount.Text);
        if (!amount.HasValue || amount.Value <= 0)
        {
            ShowMessage("مبلغ پرداخت باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        DateTime? payDate = ParsePersianDate(txtPayDate.Text.Trim());
        if (!payDate.HasValue)
        {
            ShowMessage("تاریخ پرداخت معتبر نیست. فرمت: 1403/04/15");
            return;
        }

        string method = ddlPayMethod.SelectedValue;
        if (method == null) method = "";
        string refNum = txtPayRef.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                decimal shareAmount = 0m;
                decimal currentPaid = 0m;
                string partnerName = "";
                string distNumber = "";

                string selectSql =
                    "SELECT i.ShareAmount, i.PaidAmount, p.FullName, d.DistributionNumber " +
                    "FROM ProfitDistributionItems i " +
                    "INNER JOIN Partners p ON p.PartnerId = i.PartnerId " +
                    "INNER JOIN ProfitDistributions d ON d.DistributionId = i.DistributionId " +
                    "WHERE i.ItemId = @Id";

                using (SqlCommand cmd = new SqlCommand(selectSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            shareAmount = ToDecimal(dr["ShareAmount"]);
                            currentPaid = ToDecimal(dr["PaidAmount"]);
                            partnerName = Convert.ToString(dr["FullName"]);
                            distNumber = Convert.ToString(dr["DistributionNumber"]);
                        }
                    }
                }

                decimal remaining = shareAmount - currentPaid;
                if (amount.Value > remaining)
                {
                    ShowMessage(string.Format("مبلغ پرداخت نمی‌تواند بیشتر از مانده ({0} تومان) باشد.",
                        FormatMoneyValue(remaining)));
                    return;
                }

                decimal newPaid = currentPaid + amount.Value;

                string updateSql =
                    "UPDATE ProfitDistributionItems SET " +
                    "  PaidAmount = @PaidAmount, PaidAt = @PaidAt, " +
                    "  PaymentMethod = @Method, ReferenceNumber = @Ref " +
                    "WHERE ItemId = @Id";

                using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.Parameters.AddWithValue("@PaidAmount", newPaid);
                    cmd.Parameters.AddWithValue("@PaidAt", payDate.Value);
                    cmd.Parameters.AddWithValue("@Method", NullIfEmpty(method));
                    cmd.Parameters.AddWithValue("@Ref", NullIfEmpty(refNum));
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Payment", "ProfitDistributionItems", itemId, partnerName,
                    string.Format("پرداخت {0} تومان به {1} — سند {2}",
                        FormatMoneyValue(amount.Value), partnerName, distNumber));
            }

            ShowMessage("پرداخت با موفقیت ثبت شد.");
            ActivePaymentItemId = 0;
            pnlPaymentForm.Visible = false;

            DataRow dist = GetDistributionRow(ActiveDistributionId);
            if (dist != null) LoadDistributionDetail(dist);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت پرداخت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Filter Events
    // ─────────────────────────────────────────────────────
    protected void txtFilterYear_TextChanged(object sender, EventArgs e)
    {
        int y;
        if (int.TryParse(txtFilterYear.Text.Trim(), out y) && y >= 1300 && y <= 1500)
            FilterYear = y;
        else
            FilterYear = 0;
        LoadPeriods();
    }

    protected void btnClearFilter_Click(object sender, EventArgs e)
    {
        FilterYear = 0;
        txtFilterYear.Text = "";
        LoadPeriods();
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
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@UserId",
                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityName", entityName);
                cmd.Parameters.AddWithValue("@EntityId", entityId);

                string json = "{\"target\":\"" + Safe(targetName) + "\",\"note\":\"" + Safe(note) + "\"}";
                cmd.Parameters.AddWithValue("@NewValues", json);
                cmd.Parameters.AddWithValue("@IPAddress",
                    GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
    }

    private string Safe(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "'");
    }

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

    protected string FormatPercent(object value)
    {
        if (value == null || value == DBNull.Value) return "۰٪";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == Math.Floor(d))
                return d.ToString("N0", CultureInfo.InvariantCulture) + "٪";
            return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪";
        }
        catch { return "۰٪"; }
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

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    // ── Status helpers ──
    protected bool IsOpen(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 1;
    }

    protected bool IsClosed(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 2;
    }

    protected bool IsDistributed(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 3;
    }

    protected bool CanDeletePeriod(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2 || s == 4;
    }

    protected string GetPeriodIconCss(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (s == 1) return baseC + " bg-brand-50 text-brand-600";
        if (s == 2) return baseC + " bg-amber-50 text-amber-600";
        if (s == 3) return baseC + " bg-emerald-50 text-emerald-600";
        return baseC + " bg-surface-muted text-ink-400";
    }

    protected string GetPeriodTypeBadge(object typeObj)
    {
        int t = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        string label = "—";
        string css = "badge bg-surface-muted text-ink-600";
        if (t == 1) { label = "ماهانه"; css = "badge bg-violet-50 text-violet-700"; }
        else if (t == 2) { label = "سه‌ماهه"; css = "badge bg-brand-50 text-brand-700"; }
        else if (t == 3) { label = "شش‌ماهه"; css = "badge bg-amber-50 text-amber-700"; }
        else if (t == 4) { label = "سالانه"; css = "badge bg-emerald-50 text-emerald-700"; }
        return "<span class=\"" + css + "\">" + HttpUtility.HtmlEncode(label) + "</span>";
    }

    protected string GetPeriodStatusBadge(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-brand-50 text-brand-700\">باز</span>";
        if (s == 2) return "<span class=\"badge bg-amber-50 text-amber-700\">بسته‌شده</span>";
        if (s == 3) return "<span class=\"badge bg-emerald-50 text-emerald-700\">تقسیم‌شده</span>";
        if (s == 4) return "<span class=\"badge bg-surface-muted text-ink-500\">لغو‌شده</span>";
        return "<span class=\"badge bg-surface-muted text-ink-500\">—</span>";
    }

    protected string GetNetProfitCell(object statusObj, object distIdObj, object netProfitObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s != 3 || distIdObj == null || distIdObj == DBNull.Value)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        if (netProfitObj == null || netProfitObj == DBNull.Value)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        decimal net = Convert.ToDecimal(netProfitObj);
        string css = net >= 0 ? "text-emerald-700" : "text-danger-600";
        return "<span class=\"font-mono font-bold " + css + "\">" +
            HttpUtility.HtmlEncode(FormatMoneyValue(net)) + "</span>";
    }

    protected string GetCloseConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دوره «" + num + "» بسته شود؟ پس از بستن، امکان ثبت فاکتور/هزینه در این بازه توصیه نمی‌شود.');";
    }

    protected string GetReopenConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دوره «" + num + "» بازگشایی شود؟');";
    }

    protected string GetDeleteConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف دوره «" + num + "» مطمئن هستید؟');";
    }

    // ── Payment helpers ──
    protected bool CanRecordPayment(object shareObj, object paidObj)
    {
        decimal share = (shareObj == null || shareObj == DBNull.Value) ? 0m : Convert.ToDecimal(shareObj);
        decimal paid = (paidObj == null || paidObj == DBNull.Value) ? 0m : Convert.ToDecimal(paidObj);
        return paid < share - 0.01m;
    }

    protected bool IsFullyPaid(object shareObj, object paidObj)
    {
        decimal share = (shareObj == null || shareObj == DBNull.Value) ? 0m : Convert.ToDecimal(shareObj);
        decimal paid = (paidObj == null || paidObj == DBNull.Value) ? 0m : Convert.ToDecimal(paidObj);
        return paid >= share - 0.01m;
    }

    protected string GetItemStatusBadge(object shareObj, object paidObj)
    {
        decimal share = (shareObj == null || shareObj == DBNull.Value) ? 0m : Convert.ToDecimal(shareObj);
        decimal paid = (paidObj == null || paidObj == DBNull.Value) ? 0m : Convert.ToDecimal(paidObj);

        if (paid >= share - 0.01m)
            return "<span class=\"badge bg-emerald-50 text-emerald-700\">تسویه‌شده</span>";
        if (paid > 0m)
            return "<span class=\"badge bg-amber-50 text-amber-700\">پرداخت جزئی</span>";
        return "<span class=\"badge bg-danger-50 text-danger-600\">در انتظار</span>";
    }

    protected string GetPaymentDetailHint(object methodObj, object refObj, object paidAtObj)
    {
        string method = methodObj == null || methodObj == DBNull.Value ? "" : Convert.ToString(methodObj).Trim();
        string refNum = refObj == null || refObj == DBNull.Value ? "" : Convert.ToString(refObj).Trim();

        if (method.Length == 0 && refNum.Length == 0) return "";

        StringBuilder sb = new StringBuilder();
        sb.Append("<p class=\"mt-0.5 text-[10.5px] text-ink-400\">");
        if (method.Length > 0) sb.Append("روش: ").Append(HttpUtility.HtmlEncode(method));
        if (refNum.Length > 0)
        {
            if (method.Length > 0) sb.Append(" — ");
            sb.Append("سند: ").Append(HttpUtility.HtmlEncode(refNum));
        }
        sb.Append("</p>");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────
    // Settings Helper
    // ─────────────────────────────────────────────────────
    private decimal GetSettingDecimal(string key, decimal defaultValue)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT SettingValue FROM AppSettings WHERE SettingKey = @Key", conn))
                {
                    cmd.Parameters.AddWithValue("@Key", key);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return defaultValue;
                    decimal d;
                    if (decimal.TryParse(Convert.ToString(o), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out d)) return d;
                    return defaultValue;
                }
            }
        }
        catch { return defaultValue; }
    }

    // ─────────────────────────────────────────────────────
    // Misc Helpers
    // ─────────────────────────────────────────────────────
    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
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
            .Replace("\n", "\\n");

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