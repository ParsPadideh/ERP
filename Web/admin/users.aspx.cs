using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_users : System.Web.UI.Page
{
    private const int PageSize = 10;
    private const string PlaceholderHash = "CHANGE_ME_ON_FIRST_LOGIN";

    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
    }

    private long EditUserId
    {
        get
        {
            long id;
            if (long.TryParse(hfEditUserId.Value, out id)) return id;
            return 0;
        }
        set { hfEditUserId.Value = value.ToString(CultureInfo.InvariantCulture); }
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
        if (!IsPostBack)
        {
            CurrentPage = 1;
            LoadRoles(ddlFilterRole, "همه نقش‌ها", true);
            LoadRoles(ddlRole, null, false);
            LoadStats();
            LoadUsers();
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری نقش‌ها
    // ─────────────────────────────────────────────────────
    private void LoadRoles(DropDownList ddl, string firstItemText, bool includeEmpty)
    {
        ddl.Items.Clear();
        if (includeEmpty)
        {
            ddl.Items.Add(new ListItem(firstItemText != null ? firstItemText : "همه نقش‌ها", ""));
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT RoleId, RoleName FROM Roles " +
                    "WHERE IsActive = 1 ORDER BY RoleId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string roleId = Convert.ToInt32(dr["RoleId"]).ToString(CultureInfo.InvariantCulture);
                        string roleName = Convert.ToString(dr["RoleName"]);
                        ddl.Items.Add(new ListItem(roleName, roleId));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری نقش‌ها: " + ex.Message);
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
                string sql =
                    "SELECT " +
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS TotalUsers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveUsers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 0 THEN 1 ELSE 0 END) AS InactiveUsers, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedUsers " +
                    "FROM Users";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["TotalUsers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatActive.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["ActiveUsers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatInactive.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["InactiveUsers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["DeletedUsers"]).ToString("N0", CultureInfo.InvariantCulture));
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
    // نمایش فرم
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditUserId = 0;
        litFormTitle.Text = "افزودن کاربر جدید";
        litPwdRequired.Text = "<span class='text-danger-500'>*</span>";
        litPwdHint.Text = "حداقل ۶ کاراکتر وارد کنید.";
        txtPassword.Attributes["placeholder"] = "حداقل ۶ کاراکتر";

        pnlForm.Visible = true;
        pnlList.Visible = false;
        pnlStats.Visible = false;
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditUserId = 0;
        pnlForm.Visible = false;
        pnlList.Visible = true;
        pnlStats.Visible = true;
        LoadStats();
        LoadUsers();
    }

    // ─────────────────────────────────────────────────────
    // لیست کاربران
    // ─────────────────────────────────────────────────────
    private void LoadUsers()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string roleFilter = ddlFilterRole.SelectedValue;
            string activeFilter = ddlFilterActive.SelectedValue;
            string deletedFilter = ddlFilterDeleted.SelectedValue;
            if (roleFilter == null) roleFilter = "";
            if (activeFilter == null) activeFilter = "";
            if (deletedFilter == null) deletedFilter = "";

            int totalCount = GetTotalUserCount(search, roleFilter, activeFilter, deletedFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPagedUsers(search, roleFilter, activeFilter, deletedFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptUsers.Visible = false;
                rptUsers.DataSource = null;
                rptUsers.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptUsers.Visible = true;
                rptUsers.DataSource = dt;
                rptUsers.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }
            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} کاربر",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری کاربران: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string roleFilter, string activeFilter, string deletedFilter,
        SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        if (search.Length > 0)
        {
            sb.Append(" AND (u.FullName LIKE @Search OR u.Username LIKE @Search " +
                      " OR u.Mobile LIKE @Search OR u.Email LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (roleFilter.Length > 0)
        {
            int roleId;
            if (int.TryParse(roleFilter, out roleId))
            {
                sb.Append(" AND u.RoleId = @RoleId ");
                cmd.Parameters.AddWithValue("@RoleId", roleId);
            }
        }

        if (activeFilter.Length > 0)
        {
            sb.Append(" AND u.IsActive = @IsActive ");
            cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
        }

        if (deletedFilter.Length > 0)
        {
            sb.Append(" AND u.IsDeleted = @IsDeleted ");
            cmd.Parameters.AddWithValue("@IsDeleted", deletedFilter == "1" ? 1 : 0);
        }

        return sb.ToString();
    }

    private int GetTotalUserCount(string search, string roleFilter, string activeFilter, string deletedFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            string baseSql = "SELECT COUNT(*) FROM Users u ";
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = baseSql + BuildFilterSql(search, roleFilter, activeFilter, deletedFilter, cmd);

                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPagedUsers(string search, string roleFilter, string activeFilter, string deletedFilter,
        int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, roleFilter, activeFilter, deletedFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY u.UserId DESC) AS RowNum, " +
                    "    u.UserId, u.Username, u.FullName, u.Mobile, u.Email, " +
                    "    u.IsActive, u.IsDeleted, u.LastLoginAt, u.CreatedAt, " +
                    "    r.RoleName " +
                    "  FROM Users u " +
                    "  INNER JOIN Roles r ON r.RoleId = u.RoleId " +
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

    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadUsers();
    }

    protected void ddlFilterRole_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadUsers();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadUsers();
    }

    protected void ddlFilterDeleted_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadUsers();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadUsers(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadUsers();
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
    protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        long userId;
        if (!long.TryParse(Convert.ToString(e.CommandArgument), out userId) || userId <= 0)
            return;

        if (e.CommandName == "Edit") LoadUserForEdit(userId);
        else if (e.CommandName == "SoftDelete") SoftDeleteUser(userId);
        else if (e.CommandName == "Restore") RestoreUser(userId);
        else if (e.CommandName == "ToggleActive") ToggleActiveUser(userId);
        else if (e.CommandName == "ResetPassword") ResetUserPassword(userId);
    }

    private void LoadUserForEdit(long userId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT UserId, Username, FullName, Mobile, Email, RoleId, IsActive, " +
                    "       PasswordHash " +
                    "FROM Users WHERE UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditUserId = userId;

                            txtUsername.Text = Convert.ToString(dr["Username"]);
                            txtFullName.Text = Convert.ToString(dr["FullName"]);
                            txtMobile.Text = dr["Mobile"] == DBNull.Value ? "" : Convert.ToString(dr["Mobile"]);
                            txtEmail.Text = dr["Email"] == DBNull.Value ? "" : Convert.ToString(dr["Email"]);

                            string roleIdStr = dr["RoleId"] == DBNull.Value
                                ? "" : Convert.ToInt32(dr["RoleId"]).ToString(CultureInfo.InvariantCulture);

                            ddlRole.ClearSelection();
                            ListItem roleItem = ddlRole.Items.FindByValue(roleIdStr);
                            if (roleItem != null) roleItem.Selected = true;

                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem actItem = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (actItem != null) actItem.Selected = true;

                            string dbHash = dr["PasswordHash"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PasswordHash"]);
                            hfOriginalHash.Value = dbHash;

                            txtPassword.Text = "";
                            txtPassword2.Text = "";
                            litPwdRequired.Text = "";
                            litPwdHint.Text = "برای تغییر رمز، هر دو فیلد را پر کنید. در غیر این صورت خالی بگذارید.";
                            txtPassword.Attributes["placeholder"] = "خالی بگذارید تا رمز حفظ شود";

                            litFormTitle.Text = "ویرایش کاربر — " + Convert.ToString(dr["FullName"]);

                            pnlForm.Visible = true;
                            pnlList.Visible = false;
                            pnlStats.Visible = false;
                        }
                        else
                        {
                            ShowMessage("کاربر مورد نظر یافت نشد.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری کاربر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // ذخیره (افزودن / ویرایش)
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        long currentId = EditUserId;

        string username = txtUsername.Text.Trim();
        string fullName = txtFullName.Text.Trim();
        string mobile = txtMobile.Text.Trim();
        string email = txtEmail.Text.Trim();
        string password = txtPassword.Text;
        string password2 = txtPassword2.Text;
        bool isActive = (ddlIsActive.SelectedValue == "1");

        if (username.Length == 0)
        {
            ShowMessage("نام کاربری را وارد کنید.");
            return;
        }
        if (!IsValidUsername(username))
        {
            ShowMessage("نام کاربری فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و آندرلاین باشد (۳ تا ۵۰ کاراکتر).");
            return;
        }
        if (fullName.Length == 0)
        {
            ShowMessage("نام و نام خانوادگی را وارد کنید.");
            return;
        }
        if (ddlRole.SelectedValue == null || ddlRole.SelectedValue.Length == 0)
        {
            ShowMessage("نقش کاربر را انتخاب کنید.");
            return;
        }

        int roleId;
        if (!int.TryParse(ddlRole.SelectedValue, out roleId))
        {
            ShowMessage("نقش انتخاب‌شده معتبر نیست.");
            return;
        }

        bool passwordProvided = (password.Length > 0 || password2.Length > 0);
        if (currentId == 0)
        {
            if (password.Length == 0)
            {
                ShowMessage("رمز عبور را وارد کنید.");
                return;
            }
        }

        if (passwordProvided)
        {
            if (password.Length < 6)
            {
                ShowMessage("رمز عبور باید حداقل ۶ کاراکتر باشد.");
                return;
            }
            if (password != password2)
            {
                ShowMessage("رمز عبور و تکرار آن یکسان نیستند.");
                return;
            }
        }

        if (mobile.Length > 15)
        {
            ShowMessage("شماره موبایل نمی‌تواند بیش از ۱۵ کاراکتر باشد.");
            return;
        }
        if (email.Length > 100)
        {
            ShowMessage("ایمیل نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── چک تکراری بودن نام کاربری ──
                string dupSql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                if (currentId > 0) dupSql += " AND UserId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Username", username);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);

                    int cnt = Convert.ToInt32(dup.ExecuteScalar());
                    if (cnt > 0)
                    {
                        ShowMessage("این نام کاربری قبلاً استفاده شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    if (currentId == CurrentUserId && !isActive)
                    {
                        ShowMessage("نمی‌توانید حساب خودتان را غیرفعال کنید.");
                        return;
                    }

                    string updateSql =
                        "UPDATE Users SET " +
                        "  Username = @Username, FullName = @FullName, Mobile = @Mobile, Email = @Email, " +
                        "  RoleId = @RoleId, IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy ";

                    if (passwordProvided)
                    {
                        updateSql += ", PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt ";
                    }

                    updateSql += " WHERE UserId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Mobile", mobile.Length > 0 ? (object)mobile : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", email.Length > 0 ? (object)email : DBNull.Value);
                        cmd.Parameters.AddWithValue("@RoleId", roleId);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UpdatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);

                        if (passwordProvided)
                        {
                            string newSalt = GenerateSalt();
                            string newHash = HashPassword(password, newSalt);
                            cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                            cmd.Parameters.AddWithValue("@PasswordSalt", newSalt);
                        }

                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", currentId, username,
                        passwordProvided ? "ویرایش کاربر + تغییر رمز" : "ویرایش کاربر");

                    ShowMessage("تغییرات کاربر با موفقیت ذخیره شد.");
                }
                else
                {
                    string newSalt = GenerateSalt();
                    string newHash = HashPassword(password, newSalt);

                    string insertSql =
                        "INSERT INTO Users " +
                        "  (Username, PasswordHash, PasswordSalt, FullName, Mobile, Email, " +
                        "   RoleId, IsActive, IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES " +
                        "  (@Username, @PasswordHash, @PasswordSalt, @FullName, @Mobile, @Email, " +
                        "   @RoleId, @IsActive, 0, GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                    long newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                        cmd.Parameters.AddWithValue("@PasswordSalt", newSalt);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Mobile", mobile.Length > 0 ? (object)mobile : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", email.Length > 0 ? (object)email : DBNull.Value);
                        cmd.Parameters.AddWithValue("@RoleId", roleId);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            newId = Convert.ToInt64(result);
                    }

                    if (newId > 0)
                    {
                        WriteAudit(conn, "Insert", newId, username, "ایجاد کاربر جدید");
                    }

                    ShowMessage("کاربر جدید با موفقیت افزوده شد.");
                }
            }

            ClearForm();
            EditUserId = 0;
            pnlForm.Visible = false;
            pnlList.Visible = true;
            pnlStats.Visible = true;
            LoadStats();
            LoadUsers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره کاربر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Soft Delete / Restore
    // ─────────────────────────────────────────────────────
    private void SoftDeleteUser(long userId)
    {
        if (userId == CurrentUserId)
        {
            ShowMessage("نمی‌توانید حساب خودتان را حذف کنید.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string uname = "";
                using (SqlCommand get = new SqlCommand("SELECT Username FROM Users WHERE UserId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", userId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) uname = Convert.ToString(o);
                }

                if (string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    ShowMessage("کاربر admin قابل حذف نیست.");
                    return;
                }

                string sql =
                    "UPDATE Users SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                    "WHERE UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", userId, uname, "حذف نرم کاربر");
            }

            ShowMessage("کاربر با موفقیت حذف شد.");
            LoadStats();
            LoadUsers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف کاربر: " + ex.Message);
        }
    }

    private void RestoreUser(long userId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string uname = "";
                using (SqlCommand get = new SqlCommand("SELECT Username FROM Users WHERE UserId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", userId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) uname = Convert.ToString(o);
                }

                string sql =
                    "UPDATE Users SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                    "WHERE UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", userId, uname, "بازگردانی کاربر حذف‌شده");
            }

            ShowMessage("کاربر با موفقیت بازگردانده شد.");
            LoadStats();
            LoadUsers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بازگردانی کاربر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // فعال / غیرفعال
    // ─────────────────────────────────────────────────────
    private void ToggleActiveUser(long userId)
    {
        if (userId == CurrentUserId)
        {
            ShowMessage("نمی‌توانید وضعیت فعال/غیرفعال حساب خودتان را تغییر دهید.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string uname = "";
                bool isActive = false;

                using (SqlCommand get = new SqlCommand(
                    "SELECT Username, IsActive FROM Users WHERE UserId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", userId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            uname = Convert.ToString(dr["Username"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) && isActive)
                {
                    ShowMessage("کاربر admin قابل غیرفعال کردن نیست.");
                    return;
                }

                bool newState = !isActive;

                string sql =
                    "UPDATE Users SET IsActive = @IsActive, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                    "WHERE UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", userId, uname,
                    newState ? "فعال‌سازی کاربر" : "غیرفعال‌سازی کاربر");
            }

            ShowMessage("وضعیت کاربر تغییر یافت.");
            LoadStats();
            LoadUsers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تغییر وضعیت کاربر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // ریست رمز عبور
    // ─────────────────────────────────────────────────────
    private void ResetUserPassword(long userId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string uname = "";
                using (SqlCommand get = new SqlCommand("SELECT Username FROM Users WHERE UserId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", userId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) uname = Convert.ToString(o);
                }

                string sql =
                    "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = NULL, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                    "WHERE UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Hash", PlaceholderHash);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ResetPassword", userId, uname, "ریست رمز عبور به حالت پیش‌فرض");
            }

            ShowMessage("رمز عبور ریست شد. رمز جدید: Admin@123 (در اولین ورود تغییر دهید)");
            LoadUsers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ریست رمز: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // AuditLog
    // ─────────────────────────────────────────────────────
    private void WriteAudit(SqlConnection conn, string action, long entityId, string targetUsername, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, 'Users', @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId",
                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityId", entityId);

                string json = "{\"target\":\"" + (targetUsername != null ? targetUsername.Replace("\"", "'") : "")
                    + "\",\"note\":\"" + (note != null ? note.Replace("\"", "'") : "") + "\"}";
                cmd.Parameters.AddWithValue("@NewValues", json);
                cmd.Parameters.AddWithValue("@IPAddress",
                    GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
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
    // Hash
    // ─────────────────────────────────────────────────────
    private string GenerateSalt()
    {
        byte[] saltBytes = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    private string HashPassword(string password, string salt)
    {
        if (salt == null) salt = "";
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        byte[] passBytes = Encoding.UTF8.GetBytes(password);

        byte[] combined = new byte[saltBytes.Length + passBytes.Length];
        System.Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        System.Buffer.BlockCopy(passBytes, 0, combined, saltBytes.Length, passBytes.Length);

        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(combined);
            StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }

    // ─────────────────────────────────────────────────────
    // Markup helpers — داده‌های پایه
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

    protected string GetInitial(object fullNameObj, object usernameObj)
    {
        string fullName = (fullNameObj == null || fullNameObj == DBNull.Value)
            ? "" : fullNameObj.ToString().Trim();
        if (fullName.Length > 0)
            return HttpUtility.HtmlEncode(fullName.Substring(0, 1));

        string username = (usernameObj == null || usernameObj == DBNull.Value)
            ? "" : usernameObj.ToString().Trim();
        if (username.Length > 0)
            return HttpUtility.HtmlEncode(username.Substring(0, 1).ToUpper());

        return "U";
    }

    protected string GetAvatarCss(object isActiveObj, object isDeletedObj)
    {
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);

        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-full text-[14px] font-bold";

        if (isDeleted)
            return baseC + " bg-danger-50 text-danger-500 line-through";
        if (isActive)
            return baseC + " bg-brand-600 text-white";

        return baseC + " bg-surface-muted text-ink-400";
    }

    protected string GetCurrentUserBadge(object userIdObj)
    {
        long uid = (userIdObj == null || userIdObj == DBNull.Value)
            ? 0 : Convert.ToInt64(userIdObj);
        if (uid == CurrentUserId)
        {
            return " <span class=\"mr-1.5 badge bg-emerald-50 text-emerald-700\" style=\"font-size:10px;\">شما</span>";
        }
        return "";
    }

    protected string GetStatusBadgeClass(object isActiveObj, object isDeletedObj)
    {
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);

        if (isDeleted) return "badge bg-danger-50 text-danger-500";
        if (isActive) return "badge bg-success-50 text-success-600";
        return "badge bg-warning-50 text-warning-600";
    }

    protected string GetStatusText(object isActiveObj, object isDeletedObj)
    {
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);

        if (isDeleted) return "حذف‌شده";
        if (isActive) return "فعال";
        return "غیرفعال";
    }

    protected string FormatShamsiDateTime(object value)
    {
        if (value == null || value == DBNull.Value) return "—";

        try
        {
            DateTime dt = Convert.ToDateTime(value);
            PersianCalendar pc = new PersianCalendar();
            return HttpUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture,
                "{0}/{1:D2}/{2:D2} - {3:D2}:{4:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt),
                dt.Hour, dt.Minute));
        }
        catch
        {
            return "—";
        }
    }

    // ─────────────────────────────────────────────────────
    // Markup helpers — منطق نمایش دکمه‌های اقدام
    // ─────────────────────────────────────────────────────
    protected bool IsUserDeleted(object isDeletedObj)
    {
        if (isDeletedObj == null || isDeletedObj == DBNull.Value) return false;
        return Convert.ToBoolean(isDeletedObj);
    }

    protected bool CanModifyUser(object userIdObj, object usernameObj, object isDeletedObj)
    {
        // اگر کاربر حذف شده باشد → هیچ اقدامی نباید انجام شود
        if (IsUserDeleted(isDeletedObj)) return false;

        long uid = (userIdObj == null || userIdObj == DBNull.Value)
            ? 0 : Convert.ToInt64(userIdObj);

        // نمی‌توان خود کاربر را حذف/غیرفعال کرد
        if (uid == CurrentUserId) return false;

        string username = (usernameObj == null || usernameObj == DBNull.Value)
            ? "" : usernameObj.ToString();

        // کاربر admin اصلی محافظت‌شده است
        if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    protected string GetToggleCss(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);

        if (isActive)
            return "icon-button hover:bg-warning-50 hover:text-warning-600";
        return "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        return isActive ? "غیرفعال کردن" : "فعال کردن";
    }

    protected string GetToggleConfirm(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        return isActive
            ? "return confirm('این کاربر غیرفعال شود؟');"
            : "return confirm('این کاربر فعال شود؟');";
    }

    protected string GetToggleIcon(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        return isActive ? "user-x" : "user-check";
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private bool IsValidUsername(string u)
    {
        if (u == null) return false;
        if (u.Length < 3 || u.Length > 50) return false;

        for (int i = 0; i < u.Length; i++)
        {
            char c = u[i];
            bool ok = (c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == '.' || c == '_' || c == '-';
            if (!ok) return false;
        }
        return true;
    }

    private void ClearForm()
    {
        txtUsername.Text = "";
        txtFullName.Text = "";
        txtMobile.Text = "";
        txtEmail.Text = "";
        txtPassword.Text = "";
        txtPassword2.Text = "";
        hfOriginalHash.Value = "";

        txtPassword.Attributes["placeholder"] = "حداقل ۶ کاراکتر";

        if (ddlIsActive.Items.Count > 0)
        {
            ddlIsActive.ClearSelection();
            ListItem activeItem = ddlIsActive.Items.FindByValue("1");
            if (activeItem != null) activeItem.Selected = true;
        }

        if (ddlRole.Items.Count > 0)
        {
            ddlRole.ClearSelection();
            ddlRole.SelectedIndex = 0;
        }
    }

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