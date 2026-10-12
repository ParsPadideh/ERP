using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_profit_settings : System.Web.UI.Page
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

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Default filter year = current
            int currentYear = GetSettingInt("CurrentFiscalYear", GetCurrentPersianYear());
            FilterYear = currentYear;

            LoadSettings();
            LoadStats();
            LoadPeriods();
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Settings
    // ─────────────────────────────────────────────────────
    private void LoadSettings()
    {
        try
        {
            txtFiscalYearStart.Text = GetSettingString("FiscalYearStartShamsi", "1403/01/01");
            txtCurrentFiscalYear.Text = GetSettingString("CurrentFiscalYear",
                GetCurrentPersianYear().ToString(CultureInfo.InvariantCulture));

            string ptStr = GetSettingString("DefaultPeriodType", "2");
            ddlDefaultPeriodType.ClearSelection();
            ListItem ptItem = ddlDefaultPeriodType.Items.FindByValue(ptStr);
            if (ptItem != null) ptItem.Selected = true;

            string maxAdv = GetSettingString("MaxAdvancePercent", "80");
            txtMaxAdvancePercent.Text = maxAdv;

            string minRes = GetSettingString("MinReservePercent", "0");
            txtMinReservePercent.Text = minRes;

            string carryFwd = GetSettingString("EnableLossCarryForward", "true");
            chkEnableLossCarryForward.Checked = (carryFwd == "true" || carryFwd == "1");

            // Set default generate year = current + 1
            txtGenerateYear.Text = (GetCurrentPersianYear() + 1).ToString(CultureInfo.InvariantCulture);

            // Set filter textbox
            txtFilterYear.Text = FilterYear.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تنظیمات: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Save Settings
    // ─────────────────────────────────────────────────────
    protected void btnSaveSettings_Click(object sender, EventArgs e)
    {
        string fiscalStart = txtFiscalYearStart.Text.Trim();
        string currentYear = txtCurrentFiscalYear.Text.Trim();
        string periodType = ddlDefaultPeriodType.SelectedValue;
        string maxAdv = txtMaxAdvancePercent.Text.Trim();
        string minRes = txtMinReservePercent.Text.Trim();
        bool carryFwd = chkEnableLossCarryForward.Checked;

        // Validate fiscal year start
        DateTime? fyStart = ParsePersianDate(fiscalStart);
        if (!fyStart.HasValue)
        {
            ShowMessage("شروع سال مالی معتبر نیست. فرمت: 1403/01/01");
            return;
        }

        // Validate current year
        int cy;
        if (!int.TryParse(currentYear, out cy) || cy < 1300 || cy > 1500)
        {
            ShowMessage("سال مالی جاری معتبر نیست (عددی بین ۱۳۰۰ تا ۱۵۰۰).");
            return;
        }

        // Validate max advance
        decimal? maxAdvVal = ParseDecimal(maxAdv);
        if (!maxAdvVal.HasValue || maxAdvVal.Value < 0 || maxAdvVal.Value > 100)
        {
            ShowMessage("حداکثر درصد قابل برداشت باید بین ۰ تا ۱۰۰ باشد.");
            return;
        }

        // Validate min reserve
        decimal? minResVal = ParseDecimal(minRes);
        if (!minResVal.HasValue || minResVal.Value < 0 || minResVal.Value > 100)
        {
            ShowMessage("ذخیره احتیاطی باید بین ۰ تا ۱۰۰ باشد.");
            return;
        }

        if (maxAdvVal.Value + minResVal.Value > 100m)
        {
            ShowMessage("مجموع سقف برداشت و ذخیره احتیاطی نمی‌تواند از ۱۰۰٪ بیشتر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                SaveSetting(conn, "FiscalYearStartShamsi", fiscalStart);
                SaveSetting(conn, "CurrentFiscalYear", cy.ToString(CultureInfo.InvariantCulture));
                SaveSetting(conn, "DefaultPeriodType", periodType);
                SaveSetting(conn, "MaxAdvancePercent",
                    maxAdvVal.Value.ToString(CultureInfo.InvariantCulture));
                SaveSetting(conn, "MinReservePercent",
                    minResVal.Value.ToString(CultureInfo.InvariantCulture));
                SaveSetting(conn, "EnableLossCarryForward", carryFwd ? "true" : "false");

                WriteAudit(conn, "Update", "AppSettings", 0, "ProfitSettings", "به‌روزرسانی تنظیمات تقسیم سود");
            }

            ShowMessage("تنظیمات با موفقیت ذخیره شد.");
            LoadSettings();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره تنظیمات: " + ex.Message);
        }
    }

    protected void btnResetSettings_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                SaveSetting(conn, "FiscalYearStartShamsi", "1403/01/01");
                SaveSetting(conn, "DefaultPeriodType", "2");
                SaveSetting(conn, "MaxAdvancePercent", "80");
                SaveSetting(conn, "MinReservePercent", "0");
                SaveSetting(conn, "EnableLossCarryForward", "true");

                WriteAudit(conn, "Reset", "AppSettings", 0, "ProfitSettings", "بازنشانی تنظیمات به پیش‌فرض");
            }

            ShowMessage("تنظیمات به مقادیر پیش‌فرض بازگشت.");
            LoadSettings();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    private void SaveSetting(SqlConnection conn, string key, string value)
    {
        string sql =
            "IF EXISTS (SELECT 1 FROM AppSettings WHERE SettingKey = @Key) " +
            "  UPDATE AppSettings SET SettingValue = @Value, UpdatedAt = GETDATE() " +
            "  WHERE SettingKey = @Key " +
            "ELSE " +
            "  INSERT INTO AppSettings (SettingKey, SettingValue, UpdatedAt) " +
            "  VALUES (@Key, @Value, GETDATE())";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.ExecuteNonQuery();
        }
    }

    // ─────────────────────────────────────────────────────
    // Generate Periods
    // ─────────────────────────────────────────────────────
    protected void btnGenerate_Click(object sender, EventArgs e)
    {
        string yearText = txtGenerateYear.Text.Trim();
        int year;
        if (!int.TryParse(yearText, out year) || year < 1300 || year > 1500)
        {
            ShowMessage("سال معتبر نیست (عددی بین ۱۳۰۰ تا ۱۵۰۰).");
            return;
        }

        int periodType;
        if (!int.TryParse(ddlGenerateType.SelectedValue, out periodType) || periodType < 1 || periodType > 4)
        {
            ShowMessage("نوع دوره معتبر نیست.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Check if any distributed periods exist for this year
                int distributedCount = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProfitPeriods " +
                    "WHERE Year(PeriodFrom) >= @MinDate AND Year(PeriodFrom) <= @MaxDate " +
                    "  AND Status = 3", conn))
                {
                    // We'll filter by our period number prefix instead
                    // Better: use a custom column or parse the periodNumber
                    // Simpler: use date range for the year
                }

                // Simpler approach: check by PeriodNumber suffix
                string suffix = "-" + year.ToString(CultureInfo.InvariantCulture);
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProfitPeriods " +
                    "WHERE PeriodNumber LIKE '%' + @Suffix AND Status = 3", conn))
                {
                    cmd.Parameters.AddWithValue("@Suffix", suffix);
                    object o = cmd.ExecuteScalar();
                    distributedCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (distributedCount > 0)
                {
                    ShowMessage(string.Format(
                        "برای سال {0} تعداد {1} دوره تقسیم‌شده وجود دارد. " +
                        "ابتدا آن‌ها را بررسی کنید. امکان تولید مجدد وجود ندارد.",
                        year.ToString(CultureInfo.InvariantCulture), distributedCount));
                    return;
                }

                // Check existing non-distributed periods for this year
                int existingCount = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProfitPeriods " +
                    "WHERE PeriodNumber LIKE '%' + @Suffix AND Status IN (1, 2, 4)", conn))
                {
                    cmd.Parameters.AddWithValue("@Suffix", suffix);
                    object o = cmd.ExecuteScalar();
                    existingCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (existingCount > 0)
                {
                    // Delete them (with transaction)
                    using (SqlCommand cmd = new SqlCommand(
                        "DELETE FROM ProfitPeriods " +
                        "WHERE PeriodNumber LIKE '%' + @Suffix AND Status IN (1, 2, 4)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Suffix", suffix);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Generate new periods
                int created = GeneratePeriodsForYear(conn, year, periodType);

                WriteAudit(conn, "Generate", "ProfitPeriods", 0,
                    "Year " + year.ToString(CultureInfo.InvariantCulture),
                    string.Format("تولید {0} دوره از نوع {1}", created, GetPeriodTypeLabel(periodType)));
            }

            ShowMessage(string.Format("{0} دوره با موفقیت ساخته شد.",
                GetPeriodTypeCount(periodType).ToString(CultureInfo.InvariantCulture)));

            // Update filter
            FilterYear = year;
            txtFilterYear.Text = year.ToString(CultureInfo.InvariantCulture);

            LoadStats();
            LoadPeriods();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تولید دوره‌ها: " + ex.Message);
        }
    }

    private int GeneratePeriodsForYear(SqlConnection conn, int year, int periodType)
    {
        PersianCalendar pc = new PersianCalendar();
        int created = 0;

        if (periodType == 1) // Monthly
        {
            for (int m = 1; m <= 12; m++)
            {
                DateTime from = pc.ToDateTime(year, m, 1, 0, 0, 0, 0);
                DateTime nextMonth = pc.AddMonths(from, 1);
                DateTime to = nextMonth.AddSeconds(-1);
                string number = string.Format(CultureInfo.InvariantCulture,
                    "M{0:D2}-{1}", m, year);
                InsertPeriod(conn, number, periodType, from, to, year, m);
                created++;
            }
        }
        else if (periodType == 2) // Quarterly
        {
            for (int q = 1; q <= 4; q++)
            {
                int startMonth = (q - 1) * 3 + 1;
                DateTime from = pc.ToDateTime(year, startMonth, 1, 0, 0, 0, 0);
                DateTime nextQuarter = pc.AddMonths(from, 3);
                DateTime to = nextQuarter.AddSeconds(-1);
                string number = string.Format(CultureInfo.InvariantCulture,
                    "Q{0}-{1}", q, year);
                InsertPeriod(conn, number, periodType, from, to, year, q);
                created++;
            }
        }
        else if (periodType == 3) // Semi-annual
        {
            for (int h = 1; h <= 2; h++)
            {
                int startMonth = (h - 1) * 6 + 1;
                DateTime from = pc.ToDateTime(year, startMonth, 1, 0, 0, 0, 0);
                DateTime nextHalf = pc.AddMonths(from, 6);
                DateTime to = nextHalf.AddSeconds(-1);
                string number = string.Format(CultureInfo.InvariantCulture,
                    "H{0}-{1}", h, year);
                InsertPeriod(conn, number, periodType, from, to, year, h);
                created++;
            }
        }
        else if (periodType == 4) // Annual
        {
            DateTime from = pc.ToDateTime(year, 1, 1, 0, 0, 0, 0);
            DateTime nextYear = pc.AddYears(from, 1);
            DateTime to = nextYear.AddSeconds(-1);
            string number = string.Format(CultureInfo.InvariantCulture, "Y-{0}", year);
            InsertPeriod(conn, number, periodType, from, to, year, 1);
            created++;
        }

        return created;
    }

    private void InsertPeriod(SqlConnection conn, string number, int periodType,
        DateTime from, DateTime to, int year, int sequence)
    {
        string sql =
            "INSERT INTO ProfitPeriods " +
            "  (PeriodNumber, PeriodType, PeriodFrom, PeriodTo, Status, Notes, CreatedAt) " +
            "VALUES (@Number, @Type, @From, @To, 1, @Notes, GETDATE())";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Number", number);
            cmd.Parameters.AddWithValue("@Type", periodType);
            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters.AddWithValue("@To", to);
            cmd.Parameters.AddWithValue("@Notes",
                string.Format(CultureInfo.InvariantCulture,
                    "سال {0} — دوره {1}", year, sequence));
            cmd.ExecuteNonQuery();
        }
    }

    private int GetPeriodTypeCount(int periodType)
    {
        if (periodType == 1) return 12;
        if (periodType == 2) return 4;
        if (periodType == 3) return 2;
        if (periodType == 4) return 1;
        return 0;
    }

    private string GetPeriodTypeLabel(int periodType)
    {
        if (periodType == 1) return "ماهانه";
        if (periodType == 2) return "سه‌ماهه";
        if (periodType == 3) return "شش‌ماهه";
        if (periodType == 4) return "سالانه";
        return "سفارشی";
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
    // Load Periods
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
                    "       pp.DistributionId, pp.ClosedAt, " +
                    "       pd.DistributionNumber " +
                    "FROM ProfitPeriods pp " +
                    "LEFT JOIN ProfitDistributions pd ON pd.DistributionId = pp.DistributionId ";

                if (FilterYear > 0)
                {
                    sql += "WHERE pp.PeriodNumber LIKE '%' + @Suffix ";
                }

                sql += "ORDER BY pp.PeriodFrom DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (FilterYear > 0)
                        cmd.Parameters.AddWithValue("@Suffix", "-" + FilterYear.ToString(CultureInfo.InvariantCulture));

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlPeriodsEmpty.Visible = true;
                rptPeriods.Visible = false;
                rptPeriods.DataSource = null;
                rptPeriods.DataBind();
            }
            else
            {
                pnlPeriodsEmpty.Visible = false;
                rptPeriods.Visible = true;
                rptPeriods.DataSource = dt;
                rptPeriods.DataBind();
            }

            litPeriodsInfo.Text = string.Format("{0} دوره{1}",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture),
                FilterYear > 0
                    ? " در سال " + FilterYear.ToString(CultureInfo.InvariantCulture)
                    : "");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری دوره‌ها: " + ex.Message);
        }
    }

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
    // Period commands
    // ─────────────────────────────────────────────────────
    protected void rptPeriods_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "DeletePeriod") return;

        int periodId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out periodId) || periodId <= 0) return;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Verify status
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
                            status = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
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
            ShowMessage("خطا در حذف دوره: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Audit
    // ─────────────────────────────────────────────────────
    private void WriteAudit(SqlConnection conn, string action, string entityName, int entityId,
        string targetName, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
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

    protected bool IsTrue(object boolObj)
    {
        if (boolObj == null || boolObj == DBNull.Value) return false;
        return Convert.ToBoolean(boolObj);
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

    protected string GetPeriodNotes(object notesObj)
    {
        if (notesObj == null || notesObj == DBNull.Value) return "";
        string notes = Convert.ToString(notesObj).Trim();
        if (notes.Length == 0) return "";
        string preview = notes.Length > 50 ? notes.Substring(0, 47) + "..." : notes;
        return "<p class=\"mt-0.5 text-[10.5px] text-ink-400\">" +
               HttpUtility.HtmlEncode(preview) + "</p>";
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
        string label = GetPeriodTypeLabel(t);
        string css = "badge bg-surface-muted text-ink-600";
        if (t == 1) css = "badge bg-violet-50 text-violet-700";
        else if (t == 2) css = "badge bg-brand-50 text-brand-700";
        else if (t == 3) css = "badge bg-amber-50 text-amber-700";
        else if (t == 4) css = "badge bg-emerald-50 text-emerald-700";

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

    protected string GetDistributionLink(object distIdObj, object distNumObj)
    {
        if (distIdObj == null || distIdObj == DBNull.Value)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        string num = distNumObj == null || distNumObj == DBNull.Value
            ? "" : Convert.ToString(distNumObj);
        string id = Convert.ToInt32(distIdObj).ToString(CultureInfo.InvariantCulture);

        return "<a href=\"profit-periods.aspx?dist=" + id + "\" class=\"inline-flex items-center gap-1 text-[12px] font-bold text-brand-600 hover:underline font-mono\">" +
               "<i class=\"h-3 w-3\" data-lucide=\"external-link\"></i>" +
               HttpUtility.HtmlEncode(num) +
               "</a>";
    }

    protected bool CanDeletePeriod(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2 || s == 4;
    }

    protected bool IsLocked(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 3;
    }

    protected string GetDeletePeriodConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value
            ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف دوره «" + num + "» مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // Settings helpers
    // ─────────────────────────────────────────────────────
    private string GetSettingString(string key, string defaultValue)
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
                    string s = Convert.ToString(o);
                    return string.IsNullOrEmpty(s) ? defaultValue : s;
                }
            }
        }
        catch { return defaultValue; }
    }

    private int GetSettingInt(string key, int defaultValue)
    {
        string s = GetSettingString(key, null);
        if (string.IsNullOrEmpty(s)) return defaultValue;
        int v;
        if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
        return defaultValue;
    }

    private int GetCurrentPersianYear()
    {
        return new PersianCalendar().GetYear(DateTime.Now);
    }

    // ─────────────────────────────────────────────────────
    // Date helpers
    // ─────────────────────────────────────────────────────
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