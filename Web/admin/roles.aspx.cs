using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_roles : System.Web.UI.Page
{
    // نقش‌های سیستمی که حذف نمی‌شوند
    private static readonly string[] SystemRoles = new string[] {
        "مدیر سیستم", "مدیر بهره‌برداری", "مالک تجهیزات",
        "بازاریاب", "انباردار", "مسئول تولید", "حسابدار"
    };

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

    private int EditId
    {
        get { int id; if (int.TryParse(hfEditId.Value, out id)) return id; return 0; }
        set { hfEditId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack) ShowListView();
    }

    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlFormView.Visible = false;
        phBackToList.Visible = false;
        phAddNew.Visible = true;

        LoadStats();
        LoadRoles();
    }

    private void ShowFormView(int roleId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        if (roleId > 0)
        {
            EditId = roleId;
            LoadRoleForEdit(roleId);
            litFormTitle.Text = "ویرایش نقش";
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن نقش جدید";
        }
    }

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
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM Roles";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveCount"]).ToString("N0"));
                        litStatInactive.Text = HttpUtility.HtmlEncode(ToInt(dr["InactiveCount"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری آمار: " + ex.Message); }
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        return Convert.ToInt32(o);
    }

    private void LoadRoles()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT r.RoleId, r.RoleName, r.Description, r.IsActive, r.IsDeleted, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM Users u ");
                sb.Append("          WHERE u.RoleId = r.RoleId AND u.IsDeleted = 0), 0) AS UserCount ");
                sb.Append("FROM Roles r WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND r.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (r.RoleName LIKE @Search OR r.Description LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string activeFilter = ddlFilterActive.SelectedValue;
                    if (!string.IsNullOrEmpty(activeFilter))
                    {
                        sb.Append(" AND r.IsActive = @IsActive ");
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
                    }

                    sb.Append(" ORDER BY r.IsDeleted ASC, r.IsActive DESC, r.RoleName");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            // ═══════════════════════════════════════════════════════
            // ✅ اضافه‌کردن ستون IsSystemRole برای Eval در ASPX
            // ═══════════════════════════════════════════════════════
            if (!dt.Columns.Contains("IsSystemRole"))
                dt.Columns.Add("IsSystemRole", typeof(bool));

            foreach (DataRow row in dt.Rows)
            {
                string roleName = Convert.ToString(row["RoleName"]);
                bool isSys = false;
                for (int i = 0; i < SystemRoles.Length; i++)
                {
                    if (string.Equals(SystemRoles[i], roleName, StringComparison.OrdinalIgnoreCase))
                    {
                        isSys = true;
                        break;
                    }
                }
                row["IsSystemRole"] = isSys;
            }

            // ═══════════════════════════════════════════════════════
            // Bind
            // ═══════════════════════════════════════════════════════
            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptRoles.Visible = false;
                rptRoles.DataSource = null;
                rptRoles.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptRoles.Visible = true;
                rptRoles.DataSource = dt;
                rptRoles.DataBind();
            }

            litListInfo.Text = string.Format("{0} نقش",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری: " + ex.Message);
        }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadRoles(); }

    private void LoadRoleForEdit(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT RoleName, Description, IsActive FROM Roles " +
                    "WHERE RoleId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtRoleName.Text = Convert.ToString(dr["RoleName"]);
                            txtDescription.Text = dr["Description"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Description"]);

                            bool isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem item = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (item != null) item.Selected = true;
                        }
                        else { ShowMessage("نقش یافت نشد."); ShowListView(); }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;
        string name = txtRoleName.Text.Trim();
        string description = txtDescription.Text.Trim();
        bool isActive = (ddlIsActive.SelectedValue == "1");

        if (name.Length == 0) { ShowMessage("نام نقش را وارد کنید."); return; }
        if (name.Length > 50) { ShowMessage("نام حداکثر ۵۰ کاراکتر."); return; }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string dupSql = "SELECT COUNT(*) FROM Roles " +
                    "WHERE RoleName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND RoleId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", name);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("نقشی با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Roles SET RoleName = @Name, Description = @Desc, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE RoleId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "Roles", currentId, name, "ویرایش نقش");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string sql =
                        "INSERT INTO Roles (RoleName, Description, IsActive, IsDeleted, CreatedAt) " +
                        "VALUES (@Name, @Desc, @IsActive, 0, GETDATE()); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0) WriteAudit(conn, "Insert", "Roles", newId, name, "ایجاد نقش");
                    ShowMessage("نقش جدید افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void rptRoles_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreRole(id);
    }

    private void ToggleActive(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                bool isActive = false;
                using (SqlCommand get = new SqlCommand(
                    "SELECT RoleName, IsActive FROM Roles WHERE RoleId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["RoleName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("نقش یافت نشد."); return; }

                bool newState = !isActive;
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Roles SET IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE RoleId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "Roles", id, name,
                    newState ? "فعال‌سازی نقش" : "غیرفعال‌سازی نقش");
            }

            ShowMessage("وضعیت تغییر یافت.");
            LoadStats();
            LoadRoles();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void SoftDelete(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT RoleName FROM Roles WHERE RoleId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("نقش یافت نشد."); return; }

                // چک نقش سیستمی
                bool isSystem = false;
                foreach (string sr in SystemRoles)
                {
                    if (string.Equals(sr, name, StringComparison.OrdinalIgnoreCase))
                    { isSystem = true; break; }
                }
                if (isSystem)
                {
                    ShowMessage("نقش‌های سیستمی قابل حذف نیستند.");
                    return;
                }

                // چک کاربران
                int userCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    userCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (userCount > 0)
                {
                    ShowMessage(string.Format("این نقش به {0} کاربر اختصاص یافته و قابل حذف نیست.", userCount));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Roles SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE RoleId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "Roles", id, name, "حذف نرم نقش");
            }

            ShowMessage("نقش حذف شد (قابل بازیابی).");
            LoadStats();
            LoadRoles();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void RestoreRole(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT RoleName FROM Roles WHERE RoleId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("نقش حذف‌شده یافت نشد."); return; }

                int dup = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Roles WHERE RoleName = @Name AND IsDeleted = 0 AND RoleId <> @Id", conn))
                {
                    check.Parameters.AddWithValue("@Name", name);
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    dup = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dup > 0) { ShowMessage("نقش دیگری با همین نام وجود دارد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Roles SET IsDeleted = 0, IsActive = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE RoleId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "Roles", id, name, "بازیابی نقش");
            }

            ShowMessage("نقش بازیابی شد.");
            LoadStats();
            LoadRoles();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }

    private void WriteAudit(SqlConnection conn, string action, string entityName, int entityId,
        string targetName, string note)
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())", conn))
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

    // ─── Helpers ───
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

    protected string FormatOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        string s = Convert.ToString(value).Trim();
        if (s.Length == 0) return "—";
        return HttpUtility.HtmlEncode(s);
    }

    protected bool IsSystemRole(object roleNameObj)
    {
        if (roleNameObj == null || roleNameObj == DBNull.Value) return false;
        string name = Convert.ToString(roleNameObj);
        foreach (string sr in SystemRoles)
        {
            if (string.Equals(sr, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    protected string GetRowCss(object isDeletedObj, object isSystemRoleObj)
    {
        if (IsTrue(isDeletedObj))
            return "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition";
        if (IsTrue(isSystemRoleObj))
            return "border-b border-surface-line bg-brand-50/20 hover:bg-brand-50/40 transition";
        return "border-b border-surface-line hover:bg-surface-body/70 transition";
    }

    protected string GetRoleIcon(object roleNameObj)
    {
        if (roleNameObj == null || roleNameObj == DBNull.Value) return "shield";
        string name = Convert.ToString(roleNameObj);
        if (name.Contains("مدیر سیستم")) return "crown";
        if (name.Contains("مدیر")) return "user-cog";
        if (name.Contains("مالک")) return "gem";
        if (name.Contains("بازاریاب")) return "megaphone";
        if (name.Contains("انبار")) return "warehouse";
        if (name.Contains("تولید")) return "hammer";
        if (name.Contains("حساب")) return "calculator";
        return "shield";
    }

    protected string GetRoleAvatarCss(object isActiveObj, object isDeletedObj)
    {
        bool isDeleted = IsTrue(isDeletedObj);
        bool isActive = IsTrue(isActiveObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (isDeleted) return baseC + " bg-danger-50 text-danger-500";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        return baseC + " bg-brand-50 text-brand-600";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetSystemBadge(object isSystemObj)
    {
        if (!IsTrue(isSystemObj)) return "";
        return " <span class=\"badge bg-brand-100 text-brand-700 text-[10px]\" title=\"نقش سیستمی — قابل حذف نیست\"><i class=\"h-3 w-3\" data-lucide=\"lock\"></i></span>";
    }

    protected string GetStatusBadge(object isActiveObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj)) return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";
        if (IsTrue(isActiveObj)) return "<span class=\"badge bg-success-50 text-success-600\">فعال</span>";
        return "<span class=\"badge bg-warning-50 text-warning-600\">غیرفعال</span>";
    }

    protected bool CanEdit(object isDeletedObj, object isSystemRoleObj)
    {
        // نقش سیستمی قابل ویرایش است (ولی حذف نمی‌شود)
        return !IsTrue(isDeletedObj);
    }

    protected bool CanDelete(object userCountObj, object isSystemRoleObj)
    {
        if (IsTrue(isSystemRoleObj)) return false;
        int count = (userCountObj == null || userCountObj == DBNull.Value) ? 0 : Convert.ToInt32(userCountObj);
        return count == 0;
    }

    protected string GetBlockReason(object userCountObj, object isSystemRoleObj)
    {
        if (IsTrue(isSystemRoleObj)) return "نقش سیستمی — قابل حذف نیست";
        int count = (userCountObj == null || userCountObj == DBNull.Value) ? 0 : Convert.ToInt32(userCountObj);
        if (count > 0) return "این نقش به کاربران اختصاص یافته";
        return "قابل حذف نیست";
    }

    protected string GetToggleCss(object isActiveObj)
    {
        return IsTrue(isActiveObj)
            ? "icon-button hover:bg-warning-50 hover:text-warning-600"
            : "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj) { return IsTrue(isActiveObj) ? "غیرفعال کردن" : "فعال کردن"; }
    protected string GetToggleIcon(object isActiveObj) { return IsTrue(isActiveObj) ? "toggle-right" : "toggle-left"; }

    protected string GetToggleConfirm(object isActiveObj, object nameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('نقش «" + name + "» غیرفعال شود؟');";
        return "return confirm('نقش «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف نقش «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('نقش «" + name + "» بازیابی شود؟');";
    }

    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
    }

    private void ClearForm()
    {
        txtRoleName.Text = "";
        txtDescription.Text = "";
        if (ddlIsActive.Items.Count > 0)
        {
            ddlIsActive.ClearSelection();
            ListItem item = ddlIsActive.Items.FindByValue("1");
            if (item != null) item.Selected = true;
        }
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