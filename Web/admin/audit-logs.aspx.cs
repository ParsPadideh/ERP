using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_audit_logs : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
    }

    private int PageSize
    {
        get
        {
            int size = 25;
            if (int.TryParse(ddlPageSize.SelectedValue, out size))
            {
                if (size <= 0) size = 25;
                if (size > 500) size = 500;
            }
            return size;
        }
    }

    private int DateRangeDays
    {
        get
        {
            int days = 7;
            if (int.TryParse(hfDateRange.Value, out days))
            {
                if (days < 0) days = 0;
            }
            return days;
        }
        set { hfDateRange.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private long SelectedLogId
    {
        get
        {
            long id;
            if (long.TryParse(hfSelectedLogId.Value, out id)) return id;
            return 0;
        }
        set { hfSelectedLogId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private long CurrentUserId
    {
        get
        {
            if (Session["UserId"] == null) return 0;
            return Convert.ToInt64(Session["UserId"]);
        }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (CurrentUserId <= 0)
        {
            Response.Redirect(ResolveUrl("~/Login.aspx"), false);
            return;
        }

        // مدیریت درخواست تغییر بازه زمانی از طریق __doPostBack
        HandlePostbackRequests();

        if (!IsPostBack)
        {
            CurrentPage = 1;
            // مقدار پیش‌فرض: ۷ روز اخیر
            DateRangeDays = 7;

            LoadUsersFilter();
            LoadStats();
            LoadLogs();
        }
    }

    private void HandlePostbackRequests()
    {
        string target = Request.Form["__EVENTTARGET"];
        string arg = Request.Form["__EVENTARGUMENT"];

        if (target == "__PAGE" && !string.IsNullOrEmpty(arg) && arg.StartsWith("setRange:"))
        {
            string range = arg.Substring("setRange:".Length);
            int days;
            if (int.TryParse(range, out days))
            {
                DateRangeDays = days;
                CurrentPage = 1;
                LoadStats();
                LoadLogs();
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری فیلتر کاربران
    // ─────────────────────────────────────────────────────
    private void LoadUsersFilter()
    {
        ddlFilterUser.Items.Clear();
        ddlFilterUser.Items.Add(new ListItem("همه کاربران", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT DISTINCT u.UserId, u.FullName, u.Username " +
                    "FROM Users u " +
                    "INNER JOIN AuditLogs a ON a.UserId = u.UserId " +
                    "ORDER BY u.FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string uid = Convert.ToInt64(dr["UserId"]).ToString(CultureInfo.InvariantCulture);
                        string fullName = dr["FullName"] == DBNull.Value
                            ? "" : Convert.ToString(dr["FullName"]).Trim();
                        string username = dr["Username"] == DBNull.Value
                            ? "" : Convert.ToString(dr["Username"]);

                        string display = fullName.Length > 0 ? fullName : username;
                        if (display.Length == 0) display = "کاربر #" + uid;

                        ddlFilterUser.Items.Add(new ListItem(display, uid));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری لیست کاربران: " + ex.Message);
        }
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

                // ── کل رویدادهای امروز ──
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM AuditLogs WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    object o = cmd.ExecuteScalar();
                    int v = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatToday.Text = HttpUtility.HtmlEncode(FormatNumber(v));
                }

                // ── ورود موفق امروز ──
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM AuditLogs " +
                    "WHERE Action = 'Login' AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    object o = cmd.ExecuteScalar();
                    int v = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatLogins.Text = HttpUtility.HtmlEncode(FormatNumber(v));
                }

                // ── ورود ناموفق ۲۴ ساعت ──
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM AuditLogs " +
                    "WHERE Action IN ('LoginFailed','ChangePasswordFailed') " +
                    "  AND CreatedAt >= DATEADD(HOUR, -24, GETDATE())", conn))
                {
                    object o = cmd.ExecuteScalar();
                    int v = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatFailed.Text = HttpUtility.HtmlEncode(FormatNumber(v));
                }

                // ── کل رویدادها ──
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AuditLogs", conn))
                {
                    object o = cmd.ExecuteScalar();
                    int v = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatTotal.Text = HttpUtility.HtmlEncode(FormatNumber(v));
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری آمار: " + ex.Message);
        }
    }

    private string FormatNumber(int n)
    {
        if (n == 0) return "۰";
        return n.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    // ─────────────────────────────────────────────────────
    // لیست لاگ‌ها
    // ─────────────────────────────────────────────────────
    private void LoadLogs()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string userFilter = ddlFilterUser.SelectedValue;
            string actionFilter = ddlFilterAction.SelectedValue;
            string entityFilter = ddlFilterEntity.SelectedValue;
            if (userFilter == null) userFilter = "";
            if (actionFilter == null) actionFilter = "";
            if (entityFilter == null) entityFilter = "";

            int totalCount = GetTotalLogCount(search, userFilter, actionFilter, entityFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPagedLogs(search, userFilter, actionFilter, entityFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptLogs.Visible = false;
                rptLogs.DataSource = null;
                rptLogs.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptLogs.Visible = true;
                rptLogs.DataSource = dt;
                rptLogs.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} رویداد",
                HttpUtility.HtmlEncode(startIdx.ToString(CultureInfo.InvariantCulture)),
                HttpUtility.HtmlEncode(endIdx.ToString(CultureInfo.InvariantCulture)),
                HttpUtility.HtmlEncode(totalCount.ToString("N0", CultureInfo.InvariantCulture)));

            litPageInfo.Text = string.Format("صفحه {0} از {1}",
                HttpUtility.HtmlEncode(CurrentPage.ToString(CultureInfo.InvariantCulture)),
                HttpUtility.HtmlEncode(totalPages.ToString(CultureInfo.InvariantCulture)));

            litCurrentPage.Text = HttpUtility.HtmlEncode(
                ToPersianNumber(CurrentPage.ToString(CultureInfo.InvariantCulture)));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);

            // ── ساخت CSV ──
            BuildCsvData(dt);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری لاگ‌ها: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string userFilter, string actionFilter, string entityFilter,
        SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        // ── بازه زمانی ──
        int days = DateRangeDays;
        if (days > 0)
        {
            sb.Append(" AND a.CreatedAt >= DATEADD(DAY, -@Days, CAST(GETDATE() AS DATE)) ");
            cmd.Parameters.AddWithValue("@Days", days - 1); // امروز + N-1 روز قبل
        }

        if (search.Length > 0)
        {
            sb.Append(" AND (u.FullName LIKE @Search OR u.Username LIKE @Search " +
                      " OR a.IPAddress LIKE @Search OR a.EntityName LIKE @Search " +
                      " OR CAST(a.EntityId AS NVARCHAR(20)) LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (userFilter.Length > 0)
        {
            long uid;
            if (long.TryParse(userFilter, out uid))
            {
                sb.Append(" AND a.UserId = @UserId ");
                cmd.Parameters.AddWithValue("@UserId", uid);
            }
        }

        if (actionFilter.Length > 0)
        {
            sb.Append(" AND a.Action = @Action ");
            cmd.Parameters.AddWithValue("@Action", actionFilter);
        }

        if (entityFilter.Length > 0)
        {
            sb.Append(" AND a.EntityName = @EntityName ");
            cmd.Parameters.AddWithValue("@EntityName", entityFilter);
        }

        return sb.ToString();
    }

    private int GetTotalLogCount(string search, string userFilter, string actionFilter, string entityFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            string baseSql =
                "SELECT COUNT(*) " +
                "FROM AuditLogs a " +
                "LEFT JOIN Users u ON u.UserId = a.UserId ";

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = baseSql + BuildFilterSql(search, userFilter, actionFilter, entityFilter, cmd);

                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPagedLogs(string search, string userFilter, string actionFilter, string entityFilter,
      int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, userFilter, actionFilter, entityFilter, cmd);

                // ── اضافه کردن ستون محاسباتی UserDisplay در SQL ──
                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY a.AuditLogId DESC) AS RowNum, " +
                    "    a.AuditLogId, a.UserId, a.Action, a.EntityName, a.EntityId, " +
                    "    a.IPAddress, a.CreatedAt, " +
                    "    ISNULL(u.FullName, N'سیستم') AS UserFullName, " +
                    "    ISNULL(u.Username, N'system') AS Username, " +
                    "    CASE " +
                    "       WHEN u.FullName IS NOT NULL AND LTRIM(RTRIM(u.FullName)) <> N'' " +
                    "            THEN u.FullName " +
                    "       WHEN u.Username IS NOT NULL AND LTRIM(RTRIM(u.Username)) <> N'' " +
                    "            THEN u.Username " +
                    "       ELSE N'سیستم' " +
                    "    END AS UserDisplay " +
                    "  FROM AuditLogs a " +
                    "  LEFT JOIN Users u ON u.UserId = a.UserId " +
                    filter +
                    " ) x WHERE x.RowNum BETWEEN @Start AND @End ORDER BY x.RowNum";

                cmd.CommandText = sql;

                int start = (page - 1) * pageSize + 1;
                int end = start + pageSize - 1;
                cmd.Parameters.AddWithValue("@Start", start);
                cmd.Parameters.AddWithValue("@End", end);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
        }

        return dt;
    }

    // ─────────────────────────────────────────────────────
    // رویدادهای فیلتر
    // ─────────────────────────────────────────────────────
    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadLogs();
    }

    protected void ddlFilterUser_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadLogs();
    }

    protected void ddlFilterAction_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadLogs();
    }

    protected void ddlFilterEntity_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadLogs();
    }

    protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadLogs();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        ddlFilterUser.ClearSelection();
        ddlFilterAction.ClearSelection();
        ddlFilterEntity.ClearSelection();
        DateRangeDays = 7;
        CurrentPage = 1;
        LoadLogs();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadLogs(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadLogs();
    }

    private void SetPagerEnabled(LinkButton btn, bool enabled)
    {
        btn.Enabled = enabled;
        if (!enabled)
        {
            btn.Attributes["disabled"] = "disabled";
            btn.Attributes["style"] = "opacity:.5;pointer-events:none;";
        }
        else
        {
            btn.Attributes.Remove("disabled");
            btn.Attributes.Remove("style");
        }
    }

    // ─────────────────────────────────────────────────────
    // ItemCommand
    // ─────────────────────────────────────────────────────
    protected void rptLogs_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName == "Details")
        {
            long logId;
            if (long.TryParse(Convert.ToString(e.CommandArgument), out logId) && logId > 0)
            {
                SelectedLogId = logId;
                LoadLogDetails(logId);
            }
        }
    }

    private void LoadLogDetails(long logId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT a.AuditLogId, a.UserId, a.Action, a.EntityName, a.EntityId, " +
                    "       a.OldValues, a.NewValues, a.IPAddress, a.CreatedAt, " +
                    "       ISNULL(u.FullName, N'سیستم') AS UserFullName, " +
                    "       ISNULL(u.Username, N'system') AS Username " +
                    "FROM AuditLogs a " +
                    "LEFT JOIN Users u ON u.UserId = a.UserId " +
                    "WHERE a.AuditLogId = @Id";

                bool hasOld = false;
                bool hasNew = false;

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", logId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            litModalLogId.Text = HttpUtility.HtmlEncode(
                                Convert.ToInt64(dr["AuditLogId"]).ToString(CultureInfo.InvariantCulture));

                            string fullName = dr["UserFullName"] == DBNull.Value
                                ? "" : Convert.ToString(dr["UserFullName"]).Trim();
                            string username = dr["Username"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Username"]);

                            string display = fullName.Length > 0 ? fullName : username;
                            litModalUser.Text = HttpUtility.HtmlEncode(display);

                            string action = dr["Action"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Action"]);

                            litModalAction.Text =
                                "<span class=\"" + GetActionBadgeCss(action) + "\">" +
                                HttpUtility.HtmlEncode(GetActionLabel(action)) +
                                "</span>";

                            string entityName = dr["EntityName"] == DBNull.Value
                                ? "" : Convert.ToString(dr["EntityName"]);
                            litModalEntity.Text = HttpUtility.HtmlEncode(GetEntityLabel(entityName));

                            if (dr["EntityId"] != DBNull.Value)
                            {
                                litModalEntityId.Text = HttpUtility.HtmlEncode(
                                    Convert.ToInt32(dr["EntityId"]).ToString(CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                litModalEntityId.Text = "—";
                            }

                            if (dr["IPAddress"] != DBNull.Value)
                            {
                                string ip = Convert.ToString(dr["IPAddress"]);
                                litModalIP.Text = HttpUtility.HtmlEncode(ip);
                            }
                            else
                            {
                                litModalIP.Text = "—";
                            }

                            DateTime createdAt = Convert.ToDateTime(dr["CreatedAt"]);
                            litModalDate.Text = HttpUtility.HtmlEncode(FormatFullShamsiDateTime(createdAt));

                            // Old values
                            if (dr["OldValues"] != DBNull.Value)
                            {
                                string oldVal = Convert.ToString(dr["OldValues"]).Trim();
                                if (oldVal.Length > 0)
                                {
                                    litOldValues.Text = FormatJsonForDisplay(oldVal);
                                    hasOld = true;
                                }
                            }

                            // New values
                            if (dr["NewValues"] != DBNull.Value)
                            {
                                string newVal = Convert.ToString(dr["NewValues"]).Trim();
                                if (newVal.Length > 0)
                                {
                                    litNewValues.Text = FormatJsonForDisplay(newVal);
                                    hasNew = true;
                                }
                            }
                        }
                    }
                }

                pnlOldValues.Visible = hasOld;
                pnlNewValues.Visible = hasNew;
                pnlNoValues.Visible = (!hasOld && !hasNew);
                pnlDetailsModal.Visible = true;
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری جزئیات: " + ex.Message);
        }
    }

    protected void btnCloseModal_Click(object sender, EventArgs e)
    {
        pnlDetailsModal.Visible = false;
        SelectedLogId = 0;
    }

    // ─────────────────────────────────────────────────────
    // CSV Export
    // ─────────────────────────────────────────────────────
    private void BuildCsvData(DataTable dt)
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            litCsvData.Text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();

        // Header
        sb.AppendLine("شناسه,کاربر,نام کاربری,عملیات,موجودیت,شناسه رکورد,IP,تاریخ و زمان");

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            DataRow r = dt.Rows[i];

            string fullName = r["UserFullName"] == DBNull.Value ? "" : Convert.ToString(r["UserFullName"]);
            string username = r["Username"] == DBNull.Value ? "" : Convert.ToString(r["Username"]);
            string action = r["Action"] == DBNull.Value ? "" : Convert.ToString(r["Action"]);
            string entityName = r["EntityName"] == DBNull.Value ? "" : Convert.ToString(r["EntityName"]);
            string entityId = r["EntityId"] == DBNull.Value ? "" : Convert.ToString(r["EntityId"]);
            string ip = r["IPAddress"] == DBNull.Value ? "" : Convert.ToString(r["IPAddress"]);
            string createdAt = r["CreatedAt"] == DBNull.Value ? "" : FormatFullShamsiDateTime(Convert.ToDateTime(r["CreatedAt"]));

            sb.Append(CsvEscape(Convert.ToString(r["AuditLogId"]))).Append(",");
            sb.Append(CsvEscape(fullName)).Append(",");
            sb.Append(CsvEscape(username)).Append(",");
            sb.Append(CsvEscape(GetActionLabel(action))).Append(",");
            sb.Append(CsvEscape(GetEntityLabel(entityName))).Append(",");
            sb.Append(CsvEscape(entityId)).Append(",");
            sb.Append(CsvEscape(ip)).Append(",");
            sb.Append(CsvEscape(createdAt));
            sb.AppendLine();
        }

        litCsvData.Text = sb.ToString();
    }

    private string CsvEscape(string value)
    {
        if (value == null) return "";
        value = value.Replace("\"", "\"\"");
        if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 ||
            value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
        {
            return "\"" + value + "\"";
        }
        return value;
    }

    // ─────────────────────────────────────────────────────
    // Format helpers
    // ─────────────────────────────────────────────────────
    private string FormatJsonForDisplay(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";

        // Escape HTML
        string safe = HttpUtility.HtmlEncode(json);

        // Pretty-print با اضافه کردن خط جدید بعد از } و ,
        // چون ما در یک pre نمایش می‌دهیم، فقط escape کافیست
        return safe;
    }

    private string FormatFullShamsiDateTime(DateTime dt)
    {
        try
        {
            PersianCalendar pc = new PersianCalendar();

            string[] dayNames = new string[]
            {
                "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه"
            };
            string[] monthNames = new string[]
            {
                "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
                "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
            };

            int dow = (int)pc.GetDayOfWeek(dt);
            string dayName = dayNames[dow];
            string monthName = monthNames[pc.GetMonth(dt) - 1];

            return string.Format(CultureInfo.InvariantCulture,
                "{0} {1} {2} {3} — ساعت {4:D2}:{5:D2}:{6:D2}",
                dayName,
                pc.GetDayOfMonth(dt).ToString(CultureInfo.InvariantCulture),
                monthName,
                pc.GetYear(dt).ToString(CultureInfo.InvariantCulture),
                dt.Hour, dt.Minute, dt.Second);
        }
        catch
        {
            return "—";
        }
    }

    private string ToPersianNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        char[] persianDigits = new char[]
        {
            '۰','۱','۲','۳','۴','۵','۶','۷','۸','۹'
        };

        StringBuilder sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c >= '0' && c <= '9')
                sb.Append(persianDigits[c - '0']);
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────
    // Markup helpers
    // ─────────────────────────────────────────────────────
    protected string Enc(object value)
    {
        if (value == null || value == DBNull.Value) return "";
        return HttpUtility.HtmlEncode(value.ToString());
    }

    protected string EncOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        string s = value.ToString().Trim();
        return s.Length == 0 ? "—" : HttpUtility.HtmlEncode(s);
    }

    protected string FormatEntityId(object entityIdObj)
    {
        if (entityIdObj == null || entityIdObj == DBNull.Value) return "";
        try
        {
            int id = Convert.ToInt32(entityIdObj);
            if (id <= 0) return "";
            return "#" + id.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            return "";
        }
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
        catch
        {
            return "—";
        }
    }

    protected string FormatTime(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(value);
            return HttpUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                dt.Hour, dt.Minute, dt.Second));
        }
        catch
        {
            return "—";
        }
    }

    // ─────────────────────────────────────────────────────
    // Action helpers
    // ─────────────────────────────────────────────────────
    protected string GetActionLabel(object actionObj)
    {
        string action = (actionObj == null || actionObj == DBNull.Value) ? "" : actionObj.ToString();

        if (action == "Login") return "ورود موفق";
        if (action == "LoginFailed") return "ورود ناموفق";
        if (action == "Logout") return "خروج";
        if (action == "Insert") return "ایجاد";
        if (action == "Update") return "ویرایش";
        if (action == "Delete") return "حذف";
        if (action == "SoftDelete") return "حذف نرم";
        if (action == "Restore") return "بازگردانی";
        if (action == "ChangePassword") return "تغییر رمز";
        if (action == "ChangePasswordFailed") return "خطای رمز";
        if (action == "ResetPassword") return "ریست رمز";
        if (action == "ToggleActive") return "تغییر وضعیت";

        return action.Length > 0 ? action : "—";
    }

    protected string GetActionIcon(object actionObj)
    {
        string action = (actionObj == null || actionObj == DBNull.Value) ? "" : actionObj.ToString();

        if (action == "Login") return "log-in";
        if (action == "LoginFailed") return "shield-x";
        if (action == "Logout") return "log-out";
        if (action == "Insert") return "plus-circle";
        if (action == "Update") return "pencil";
        if (action == "Delete" || action == "SoftDelete") return "trash-2";
        if (action == "Restore") return "rotate-ccw";
        if (action == "ChangePassword" || action == "ResetPassword") return "key-round";
        if (action == "ChangePasswordFailed") return "shield-alert";
        if (action == "ToggleActive") return "toggle-right";

        return "activity";
    }

    protected string GetUserAvatarCss(object actionObj)
    {
        string action = (actionObj == null || actionObj == DBNull.Value) ? "" : actionObj.ToString();
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg ";

        if (action == "Login") return baseC + "bg-emerald-50 text-emerald-600";
        if (action == "LoginFailed") return baseC + "bg-rose-50 text-rose-500";
        if (action == "Logout") return baseC + "bg-surface-muted text-ink-500";
        if (action == "Insert") return baseC + "bg-emerald-50 text-emerald-600";
        if (action == "Update") return baseC + "bg-brand-50 text-brand-600";
        if (action == "Delete" || action == "SoftDelete") return baseC + "bg-rose-50 text-rose-500";
        if (action == "Restore") return baseC + "bg-violet-50 text-violet-600";
        if (action == "ChangePassword" || action == "ResetPassword") return baseC + "bg-amber-50 text-amber-600";
        if (action == "ChangePasswordFailed") return baseC + "bg-rose-50 text-rose-500";
        if (action == "ToggleActive") return baseC + "bg-brand-50 text-brand-600";

        return baseC + "bg-surface-muted text-ink-500";
    }

    protected string GetActionBadgeCss(object actionObj)
    {
        string action = (actionObj == null || actionObj == DBNull.Value) ? "" : actionObj.ToString();

        if (action == "Login") return "badge bg-emerald-50 text-emerald-700";
        if (action == "LoginFailed") return "badge bg-rose-50 text-rose-700";
        if (action == "Logout") return "badge bg-surface-muted text-ink-600";
        if (action == "Insert") return "badge bg-emerald-50 text-emerald-700";
        if (action == "Update") return "badge bg-brand-50 text-brand-700";
        if (action == "Delete" || action == "SoftDelete") return "badge bg-rose-50 text-rose-700";
        if (action == "Restore") return "badge bg-violet-50 text-violet-700";
        if (action == "ChangePassword" || action == "ResetPassword") return "badge bg-amber-50 text-amber-700";
        if (action == "ChangePasswordFailed") return "badge bg-rose-50 text-rose-700";
        if (action == "ToggleActive") return "badge bg-brand-50 text-brand-700";

        return "badge bg-surface-muted text-ink-600";
    }

    protected string GetEntityLabel(object entityObj)
    {
        string entity = (entityObj == null || entityObj == DBNull.Value) ? "" : entityObj.ToString();

        if (entity == "Users") return "کاربران";
        if (entity == "Roles") return "نقش‌ها";
        if (entity == "Products") return "محصولات";
        if (entity == "ProductCategories") return "دسته‌بندی محصولات";
        if (entity == "Units") return "واحدها";
        if (entity == "Warehouses") return "انبارها";
        if (entity == "Customers") return "مشتریان";
        if (entity == "Suppliers") return "تأمین‌کنندگان";
        if (entity == "PriceLists") return "لیست‌های قیمت";
        if (entity == "PurchaseOrders") return "سفارش‌های خرید";
        if (entity == "ProductionOrders") return "دستورهای تولید";
        if (entity == "SalesOrders") return "سفارش‌های فروش";
        if (entity == "ProformaInvoices") return "پیش‌فاکتورها";
        if (entity == "FinalInvoices") return "فاکتورهای نهایی";
        if (entity == "Expenses") return "هزینه‌ها";
        if (entity == "StockMovements") return "گردش انبار";

        return entity.Length > 0 ? entity : "—";
    }

    // ─────────────────────────────────────────────────────
    // ShowMessage
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