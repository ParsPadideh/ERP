using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_expense_categories : System.Web.UI.Page
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
        LoadStats();
        LoadCategories();
    }

    private void ShowFormView(int categoryId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;

        if (categoryId > 0)
        {
            LoadCategoryForEdit(categoryId);
            litFormTitle.Text = "ویرایش دسته هزینه";
        }
        else
        {
            ClearForm();
            EditId = 0;
            litFormTitle.Text = "افزودن دسته هزینه جدید";
        }
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
                    "  SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount, " +
                    "  SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount, " +
                    "  (SELECT COUNT(*) FROM Expenses WHERE IsDeleted = 0) AS ExpenseCount " +
                    "FROM ExpenseCategories WHERE IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveCount"]).ToString("N0"));
                        litStatInactive.Text = HttpUtility.HtmlEncode(ToInt(dr["InactiveCount"]).ToString("N0"));
                        litStatExpenses.Text = HttpUtility.HtmlEncode(ToInt(dr["ExpenseCount"]).ToString("N0"));
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
    // Load Categories
    // ─────────────────────────────────────────────────────
    private void LoadCategories()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string search = txtSearch.Text.Trim();
                string activeFilter = ddlFilterActive.SelectedValue;

                string sql =
                    "SELECT ec.ExpenseCategoryId, ec.CategoryName, ec.IsActive, " +
                    "  ISNULL((SELECT COUNT(*) FROM Expenses e " +
                    "          WHERE e.ExpenseCategoryId = ec.ExpenseCategoryId " +
                    "            AND e.IsDeleted = 0), 0) AS ExpenseCount " +
                    "FROM ExpenseCategories ec " +
                    "WHERE ec.IsDeleted = 0 ";

                if (search.Length > 0)
                    sql += "  AND ec.CategoryName LIKE @Search ";
                if (activeFilter.Length > 0)
                    sql += "  AND ec.IsActive = @IsActive ";

                sql += "ORDER BY ec.IsActive DESC, ec.CategoryName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (search.Length > 0)
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    if (activeFilter.Length > 0)
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptCategories.Visible = false;
                rptCategories.DataSource = null;
                rptCategories.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptCategories.Visible = true;
                rptCategories.DataSource = dt;
                rptCategories.DataBind();
            }

            litListInfo.Text = string.Format("{0} دسته",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری دسته‌ها: " + ex.Message);
        }
    }

    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        LoadCategories();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadCategories();
    }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadCategoryForEdit(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CategoryName, IsActive FROM ExpenseCategories " +
                    "WHERE ExpenseCategoryId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditId = id;
                            txtCategoryName.Text = Convert.ToString(dr["CategoryName"]);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                        }
                        else
                        {
                            ShowMessage("دسته مورد نظر یافت نشد.");
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
        string name = txtCategoryName.Text.Trim();
        bool isActive = chkIsActive.Checked;

        if (name.Length == 0)
        {
            ShowMessage("نام دسته را وارد کنید.");
            return;
        }
        if (name.Length > 100)
        {
            ShowMessage("نام دسته نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری (در رکوردهای غیرحذف‌شده)
                string dupSql =
                    "SELECT COUNT(*) FROM ExpenseCategories " +
                    "WHERE CategoryName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND ExpenseCategoryId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", name);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("دسته‌ای با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE ExpenseCategories SET CategoryName = @Name, IsActive = @IsActive " +
                        "WHERE ExpenseCategoryId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "ExpenseCategories", currentId, name, "ویرایش دسته هزینه");
                    ShowMessage("تغییرات با موفقیت ذخیره شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO ExpenseCategories (CategoryName, IsActive, IsDeleted) " +
                        "VALUES (@Name, @IsActive, 0); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "ExpenseCategories", newId, name, "ایجاد دسته هزینه");

                    ShowMessage("دسته جدید با موفقیت افزوده شد.");
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
    protected void rptCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "Delete") SoftDelete(id);
    }

    private void ToggleActive(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                bool isActive = false;
                string name = "";

                using (SqlCommand get = new SqlCommand(
                    "SELECT CategoryName, IsActive FROM ExpenseCategories " +
                    "WHERE ExpenseCategoryId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["CategoryName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("دسته مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ExpenseCategories SET IsActive = @IsActive WHERE ExpenseCategoryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "ExpenseCategories", id, name,
                    newState ? "فعال‌سازی دسته" : "غیرفعال‌سازی دسته");
            }

            ShowMessage("وضعیت دسته تغییر یافت.");
            LoadStats();
            LoadCategories();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
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
                    "SELECT CategoryName FROM ExpenseCategories WHERE ExpenseCategoryId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("دسته مورد نظر یافت نشد.");
                    return;
                }

                // چک وابستگی
                int count = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Expenses WHERE ExpenseCategoryId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    count = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (count > 0)
                {
                    ShowMessage(string.Format(
                        "این دسته دارای {0} هزینه ثبت‌شده است و قابل حذف نیست. ابتدا هزینه‌ها را حذف کنید.", count));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ExpenseCategories SET IsDeleted = 1 WHERE ExpenseCategoryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "ExpenseCategories", id, name, "حذف نرم دسته هزینه");
            }

            ShowMessage("دسته با موفقیت حذف شد.");
            LoadStats();
            LoadCategories();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف: " + ex.Message);
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

    protected string FormatNumber(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            return Convert.ToInt32(value).ToString("N0").Replace(",", "،");
        }
        catch { return "۰"; }
    }

    protected string GetCategoryAvatarCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        return baseC + " bg-brand-50 text-brand-600";
    }

    protected string GetStatusBadgeClass(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        return isActive ? "badge bg-success-50 text-success-600" : "badge bg-warning-50 text-warning-600";
    }

    protected string GetStatusText(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "فعال" : "غیرفعال";
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
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('دسته «" + name + "» غیرفعال شود؟');";
        return "return confirm('دسته «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف دسته «" + name + "» مطمئن هستید؟');";
    }

    protected bool CanDelete(object countObj)
    {
        int count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
        return count == 0;
    }

    // ─────────────────────────────────────────────────────
    // Clear
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        txtCategoryName.Text = "";
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