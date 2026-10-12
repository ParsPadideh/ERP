using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_warehouses : System.Web.UI.Page
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
        if (!IsPostBack) ShowListView();
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

        litPageTitle.Text = "انبارها";
        litPageSubtitle.Text = "انبارهای مواد اولیه و محصولات نهایی را مدیریت کنید.";

        LoadStats();
        LoadWarehouses();
    }

    private void ShowFormView(int warehouseId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        if (warehouseId > 0)
        {
            EditId = warehouseId;
            LoadWarehouseForEdit(warehouseId);
            litFormTitle.Text = "ویرایش انبار";
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن انبار جدید";
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
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Total, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND WarehouseType = 1 THEN 1 ELSE 0 END) AS RMCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND WarehouseType = 2 THEN 1 ELSE 0 END) AS FGCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM Warehouses";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatRM.Text = HttpUtility.HtmlEncode(ToInt(dr["RMCount"]).ToString("N0"));
                        litStatFG.Text = HttpUtility.HtmlEncode(ToInt(dr["FGCount"]).ToString("N0"));
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

    // ─────────────────────────────────────────────────────
    // Load Warehouses
    // ─────────────────────────────────────────────────────
    private void LoadWarehouses()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT w.WarehouseId, w.WarehouseName, w.WarehouseType, ");
                sb.Append("       w.Location, w.IsActive, w.IsDeleted, ");
                sb.Append("  ISNULL((SELECT COUNT(DISTINCT sl.ProductId) FROM StockLayers sl ");
                sb.Append("          WHERE sl.WarehouseId = w.WarehouseId AND sl.IsClosed = 0 ");
                sb.Append("            AND sl.QuantityRemaining > 0), 0) AS ProductCount ");
                sb.Append("FROM Warehouses w WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked) sb.Append(" AND w.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (w.WarehouseName LIKE @Search OR w.Location LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string typeFilter = ddlFilterType.SelectedValue;
                    if (!string.IsNullOrEmpty(typeFilter))
                    {
                        int t;
                        if (int.TryParse(typeFilter, out t) && t > 0)
                        {
                            sb.Append(" AND w.WarehouseType = @Type ");
                            cmd.Parameters.AddWithValue("@Type", t);
                        }
                    }

                    string activeFilter = ddlFilterActive.SelectedValue;
                    if (!string.IsNullOrEmpty(activeFilter))
                    {
                        sb.Append(" AND w.IsActive = @IsActive ");
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
                    }

                    sb.Append(" ORDER BY w.IsDeleted ASC, w.WarehouseType, w.WarehouseName");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptWarehouses.Visible = false;
                rptWarehouses.DataSource = null;
                rptWarehouses.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptWarehouses.Visible = true;
                rptWarehouses.DataSource = dt;
                rptWarehouses.DataBind();
            }

            litListInfo.Text = string.Format("{0} انبار",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadWarehouses(); }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadWarehouseForEdit(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT WarehouseName, WarehouseType, Location, IsActive " +
                    "FROM Warehouses WHERE WarehouseId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtWarehouseName.Text = Convert.ToString(dr["WarehouseName"]);
                            int type = Convert.ToInt32(dr["WarehouseType"]);
                            ddlWarehouseType.ClearSelection();
                            ListItem tItem = ddlWarehouseType.Items.FindByValue(
                                type.ToString(CultureInfo.InvariantCulture));
                            if (tItem != null) tItem.Selected = true;

                            txtLocation.Text = dr["Location"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Location"]);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                        }
                        else
                        {
                            ShowMessage("انبار مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;
        string name = txtWarehouseName.Text.Trim();
        string location = txtLocation.Text.Trim();
        bool isActive = chkIsActive.Checked;

        int wtype = 1;
        int.TryParse(ddlWarehouseType.SelectedValue, out wtype);
        if (wtype < 1 || wtype > 2) wtype = 1;

        if (name.Length == 0) { ShowMessage("نام انبار را وارد کنید."); return; }
        if (name.Length > 100) { ShowMessage("نام نمی‌تواند بیش از ۱۰۰ کاراکتر باشد."); return; }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری
                string dupSql = "SELECT COUNT(*) FROM Warehouses " +
                    "WHERE WarehouseName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND WarehouseId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", name);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("انباری با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    string sql =
                        "UPDATE Warehouses SET WarehouseName = @Name, WarehouseType = @Type, " +
                        "  Location = @Location, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE WarehouseId = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Type", wtype);
                        cmd.Parameters.AddWithValue("@Location", NullIfEmpty(location));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "Warehouses", currentId, name, "ویرایش انبار");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string sql =
                        "INSERT INTO Warehouses (WarehouseName, WarehouseType, Location, IsActive, IsDeleted, CreatedAt) " +
                        "VALUES (@Name, @Type, @Location, @IsActive, 0, GETDATE()); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Type", wtype);
                        cmd.Parameters.AddWithValue("@Location", NullIfEmpty(location));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "Warehouses", newId, name, "ایجاد انبار");

                    ShowMessage("انبار جدید افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────
    protected void rptWarehouses_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreWarehouse(id);
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
                    "SELECT WarehouseName, IsActive FROM Warehouses " +
                    "WHERE WarehouseId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["WarehouseName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("انبار یافت نشد."); return; }

                bool newState = !isActive;
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Warehouses SET IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE WarehouseId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "Warehouses", id, name,
                    newState ? "فعال‌سازی انبار" : "غیرفعال‌سازی انبار");
            }

            ShowMessage("وضعیت تغییر یافت.");
            LoadStats();
            LoadWarehouses();
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
                    "SELECT WarehouseName FROM Warehouses WHERE WarehouseId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("انبار یافت نشد."); return; }

                // چک موجودی
                int count = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM StockLayers " +
                    "WHERE WarehouseId = @Id AND IsClosed = 0 AND QuantityRemaining > 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    count = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (count > 0)
                {
                    ShowMessage(string.Format("این انبار {0} قلم موجودی دارد و قابل حذف نیست.", count));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Warehouses SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE WarehouseId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "Warehouses", id, name, "حذف نرم انبار");
            }

            ShowMessage("انبار حذف شد (قابل بازیابی).");
            LoadStats();
            LoadWarehouses();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void RestoreWarehouse(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT WarehouseName FROM Warehouses WHERE WarehouseId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("انبار حذف‌شده یافت نشد."); return; }

                // چک تکراری
                int dup = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Warehouses WHERE WarehouseName = @Name AND IsDeleted = 0 AND WarehouseId <> @Id", conn))
                {
                    check.Parameters.AddWithValue("@Name", name);
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    dup = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dup > 0) { ShowMessage("انبار دیگری با همین نام وجود دارد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Warehouses SET IsDeleted = 0, IsActive = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE WarehouseId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "Warehouses", id, name, "بازیابی انبار");
            }

            ShowMessage("انبار بازیابی شد.");
            LoadStats();
            LoadWarehouses();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Form Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }

    // ─────────────────────────────────────────────────────
    // Audit
    // ─────────────────────────────────────────────────────
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
        return IsTrue(isDeletedObj)
            ? "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition"
            : "border-b border-surface-line hover:bg-surface-body/70 transition";
    }

    protected string GetWarehouseIcon(object typeObj)
    {
        int t = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        return t == 1 ? "pickaxe" : "package-check";
    }

    protected string GetWarehouseAvatarCss(object typeObj, object isDeletedObj)
    {
        bool isDeleted = IsTrue(isDeletedObj);
        int t = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (isDeleted) return baseC + " bg-danger-50 text-danger-500";
        if (t == 1) return baseC + " bg-amber-50 text-amber-700";
        return baseC + " bg-violet-50 text-violet-700";
    }

    protected string GetTypeBadge(object typeObj)
    {
        int t = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (t == 1) return "<span class=\"badge bg-amber-50 text-amber-700\">مواد اولیه</span>";
        return "<span class=\"badge bg-violet-50 text-violet-700\">محصول نهایی</span>";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetStatusBadge(object isActiveObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";
        if (IsTrue(isActiveObj))
            return "<span class=\"badge bg-success-50 text-success-600\">فعال</span>";
        return "<span class=\"badge bg-warning-50 text-warning-600\">غیرفعال</span>";
    }

    protected bool CanEdit(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected bool CanDelete(object countObj)
    {
        int count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
        return count == 0;
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
        if (isActive) return "return confirm('انبار «" + name + "» غیرفعال شود؟');";
        return "return confirm('انبار «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف انبار «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('انبار «" + name + "» بازیابی شود؟');";
    }

    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
    }

    private void ClearForm()
    {
        txtWarehouseName.Text = "";
        txtLocation.Text = "";
        chkIsActive.Checked = true;
        if (ddlWarehouseType.Items.Count > 0)
        {
            ddlWarehouseType.ClearSelection();
            ListItem item = ddlWarehouseType.Items.FindByValue("1");
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