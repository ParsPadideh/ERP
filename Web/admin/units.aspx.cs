using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_units : System.Web.UI.Page
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

    private int EditId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditId.Value, out id)) return id;
            return 0;
        }
        set { hfEditId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ShowListView();
        }
    }

    // ─────────────────────────────────────────────────────
    // View Management
    // ─────────────────────────────────────────────────────
    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlFormView.Visible = false;
        phBackToList.Visible = false;
        phAddNew.Visible = true;

        litPageTitle.Text = "واحدهای اندازه‌گیری";
        litPageSubtitle.Text = "واحدهای اندازه‌گیری محصولات را مدیریت کنید.";

        LoadStats();
        LoadUnits();
    }

    private void ShowFormView(int unitId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        if (unitId > 0)
        {
            EditId = unitId;
            LoadUnitForEdit(unitId);
            litFormTitle.Text = "ویرایش واحد";
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن واحد جدید";
        }
    }

    // ─────────────────────────────────────────────────────
    // Stats — جدا برای Soft Delete
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
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM Units";

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
    // Load Units — با فیلتر Soft Delete
    // ─────────────────────────────────────────────────────
    private void LoadUnits()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT u.UnitId, u.UnitName, u.Symbol, u.IsActive, u.IsDeleted, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM Products p ");
                sb.Append("          WHERE p.UnitId = u.UnitId AND p.IsDeleted = 0), 0) AS ProductCount ");
                sb.Append("FROM Units u WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    // فیلتر حذف‌شده‌ها
                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND u.IsDeleted = 0 ");

                    // جستجو
                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (u.UnitName LIKE @Search OR u.Symbol LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    // فیلتر فعال/غیرفعال
                    string activeFilter = ddlFilterActive.SelectedValue;
                    if (!string.IsNullOrEmpty(activeFilter))
                    {
                        sb.Append(" AND u.IsActive = @IsActive ");
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
                    }

                    // مرتب‌سازی: حذف‌شده‌ها آخر
                    sb.Append(" ORDER BY u.IsDeleted ASC, u.IsActive DESC, u.UnitName");

                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptUnits.Visible = false;
                rptUnits.DataSource = null;
                rptUnits.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptUnits.Visible = true;
                rptUnits.DataSource = dt;
                rptUnits.DataBind();
            }

            litListInfo.Text = string.Format("{0} واحد",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری واحدها: " + ex.Message);
        }
    }

    protected void FilterChanged(object sender, EventArgs e)
    {
        LoadUnits();
    }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadUnitForEdit(int unitId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT UnitName, Symbol, IsActive FROM Units " +
                    "WHERE UnitId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", unitId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtUnitName.Text = Convert.ToString(dr["UnitName"]);
                            txtSymbol.Text = dr["Symbol"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Symbol"]);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                        }
                        else
                        {
                            ShowMessage("واحد مورد نظر یافت نشد یا حذف شده است.");
                            ShowListView();
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

    // ─────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;
        string unitName = txtUnitName.Text.Trim();
        string symbol = txtSymbol.Text.Trim();
        bool isActive = chkIsActive.Checked;

        if (unitName.Length == 0)
        {
            ShowMessage("نام واحد را وارد کنید.");
            return;
        }
        if (unitName.Length > 30)
        {
            ShowMessage("نام واحد نمی‌تواند بیش از ۳۰ کاراکتر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری — فقط در بین رکوردهای حذف‌نشده
                string dupSql =
                    "SELECT COUNT(*) FROM Units " +
                    "WHERE UnitName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND UnitId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", unitName);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("واحدی با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    // UPDATE
                    string updateSql =
                        "UPDATE Units SET " +
                        "  UnitName = @Name, Symbol = @Symbol, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE UnitId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", unitName);
                        cmd.Parameters.AddWithValue("@Symbol", NullIfEmpty(symbol));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "Units", currentId, unitName, "ویرایش واحد");
                    ShowMessage("تغییرات با موفقیت ذخیره شد.");
                }
                else
                {
                    // INSERT
                    string insertSql =
                        "INSERT INTO Units (UnitName, Symbol, IsActive, IsDeleted) " +
                        "VALUES (@Name, @Symbol, @IsActive, 0); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", unitName);
                        cmd.Parameters.AddWithValue("@Symbol", NullIfEmpty(symbol));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "Units", newId, unitName, "ایجاد واحد");

                    ShowMessage("واحد جدید با موفقیت افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────
    protected void rptUnits_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreUnit(id);
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
                    "SELECT UnitName, IsActive FROM Units WHERE UnitId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["UnitName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("واحد مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Units SET IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE UnitId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "Units", id, name,
                    newState ? "فعال‌سازی واحد" : "غیرفعال‌سازی واحد");
            }

            ShowMessage("وضعیت واحد تغییر یافت.");
            LoadStats();
            LoadUnits();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete — جایگزین DELETE
    // ═══════════════════════════════════════════════════════
    private void SoftDelete(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT UnitName FROM Units WHERE UnitId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("واحد مورد نظر یافت نشد.");
                    return;
                }

                // چک محصولات مرتبط
                int productCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Products WHERE UnitId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    productCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (productCount > 0)
                {
                    ShowMessage(string.Format(
                        "این واحد در {0} محصول استفاده شده و قابل حذف نیست.", productCount));
                    return;
                }

                // Soft Delete
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Units SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE UnitId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "Units", id, name, "حذف نرم واحد");
            }

            ShowMessage("واحد با موفقیت حذف شد (قابل بازیابی).");
            LoadStats();
            LoadUnits();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Restore — بازیابی رکورد حذف‌شده
    // ═══════════════════════════════════════════════════════
    private void RestoreUnit(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT UnitName FROM Units WHERE UnitId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("واحد حذف‌شده یافت نشد.");
                    return;
                }

                // چک تکراری در بین فعال‌ها
                int dupCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Units WHERE UnitName = @Name AND IsDeleted = 0 AND UnitId <> @Id", conn))
                {
                    check.Parameters.AddWithValue("@Name", name);
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    dupCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dupCount > 0)
                {
                    ShowMessage("واحد دیگری با همین نام وجود دارد. ابتدا آن را حذف یا تغییر نام دهید.");
                    return;
                }

                // Restore
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Units SET IsDeleted = 0, IsActive = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE UnitId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "Units", id, name, "بازیابی واحد");
            }

            ShowMessage("واحد با موفقیت بازیابی شد.");
            LoadStats();
            LoadUnits();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بازیابی: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Form Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowFormView(0);
    }

    protected void btnCancelForm_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowListView();
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowListView();
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

    protected string GetRowCss(object isDeletedObj)
    {
        bool isDeleted = IsTrue(isDeletedObj);
        if (isDeleted)
            return "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition";
        return "border-b border-surface-line hover:bg-surface-body/70 transition";
    }

    protected string GetUnitAvatarCss(object isActiveObj, object isDeletedObj)
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

    protected string GetStatusBadge(object isActiveObj, object isDeletedObj)
    {
        bool isDeleted = IsTrue(isDeletedObj);
        bool isActive = IsTrue(isActiveObj);

        if (isDeleted)
            return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";
        if (isActive)
            return "<span class=\"badge bg-success-50 text-success-600\">فعال</span>";
        return "<span class=\"badge bg-warning-50 text-warning-600\">غیرفعال</span>";
    }

    protected bool CanEdit(object isDeletedObj)
    {
        return !IsTrue(isDeletedObj);
    }

    protected bool CanDelete(object productCountObj)
    {
        int count = (productCountObj == null || productCountObj == DBNull.Value) ? 0 : Convert.ToInt32(productCountObj);
        return count == 0;
    }

    protected string GetToggleCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        if (isActive) return "icon-button hover:bg-warning-50 hover:text-warning-600";
        return "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "غیرفعال کردن" : "فعال کردن";
    }

    protected string GetToggleIcon(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "toggle-right" : "toggle-left";
    }

    protected string GetToggleConfirm(object isActiveObj, object nameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('واحد «" + name + "» غیرفعال شود؟');";
        return "return confirm('واحد «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف واحد «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('واحد «" + name + "» بازیابی شود؟');";
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
    }

    private void ClearForm()
    {
        txtUnitName.Text = "";
        txtSymbol.Text = "";
        chkIsActive.Checked = true;
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