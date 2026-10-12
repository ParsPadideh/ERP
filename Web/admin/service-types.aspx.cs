using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_service_types : System.Web.UI.Page
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

        litPageTitle.Text = "کاتالوگ خدمات";
        litPageSubtitle.Text = "انواع خدمات قابل ارائه به مشتریان (بوجاری، خشک‌کن، آسیاب و ...) را مدیریت کنید.";

        LoadStats();
        LoadTypes();
    }

    private void ShowFormView(int typeId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        if (typeId > 0)
        {
            EditId = typeId;
            LoadTypeForEdit(typeId);
            litFormTitle.Text = "ویرایش خدمت";
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن خدمت جدید";
            pnlEditWarning.Visible = false;
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

                string sql1 =
                    "SELECT " +
                    "  COUNT(*) AS Total, " +
                    "  SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount, " +
                    "  SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount " +
                    "FROM ServiceTypes WHERE IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveCount"]).ToString("N0"));
                        litStatInactive.Text = HttpUtility.HtmlEncode(ToInt(dr["InactiveCount"]).ToString("N0"));
                    }
                }

                int orderCount = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders WHERE IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    orderCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litStatOrders.Text = HttpUtility.HtmlEncode(orderCount.ToString("N0"));
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
    // Load List
    // ─────────────────────────────────────────────────────
    private void LoadTypes()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string search = txtSearch.Text.Trim();
                string activeFilter = ddlFilterActive.SelectedValue;
                if (activeFilter == null) activeFilter = "";

                string sql =
                    "SELECT st.ServiceTypeId, st.ServiceCode, st.ServiceName, st.UnitName, " +
                    "       st.DefaultUnitPrice, st.DefaultWastePercent, st.Description, " +
                    "       st.IsActive, st.CreatedAt, " +
                    "       ISNULL((SELECT COUNT(*) FROM ServiceOrders so " +
                    "               WHERE so.ServiceTypeId = st.ServiceTypeId AND so.IsDeleted = 0), 0) AS OrderCount " +
                    "FROM ServiceTypes st " +
                    "WHERE st.IsDeleted = 0 ";

                if (search.Length > 0)
                    sql += "  AND (st.ServiceName LIKE @Search OR st.ServiceCode LIKE @Search) ";
                if (activeFilter.Length > 0)
                    sql += "  AND st.IsActive = @IsActive ";

                sql += "ORDER BY st.IsActive DESC, st.ServiceName";

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
                rptTypes.Visible = false;
                rptTypes.DataSource = null;
                rptTypes.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptTypes.Visible = true;
                rptTypes.DataSource = dt;
                rptTypes.DataBind();
            }

            litListInfo.Text = string.Format("{0} خدمت",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری خدمات: " + ex.Message);
        }
    }

    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        LoadTypes();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadTypes();
    }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadTypeForEdit(int typeId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT ServiceCode, ServiceName, UnitName, DefaultUnitPrice, " +
                    "       DefaultWastePercent, Description, IsActive, " +
                    "       ISNULL((SELECT COUNT(*) FROM ServiceOrders so " +
                    "               WHERE so.ServiceTypeId = ServiceTypes.ServiceTypeId " +
                    "                 AND so.IsDeleted = 0), 0) AS OrderCount " +
                    "FROM ServiceTypes WHERE ServiceTypeId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", typeId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtServiceCode.Text = Convert.ToString(dr["ServiceCode"]);
                            txtServiceName.Text = Convert.ToString(dr["ServiceName"]);

                            string unit = dr["UnitName"] == DBNull.Value
                                ? "" : Convert.ToString(dr["UnitName"]);
                            ddlUnitName.ClearSelection();
                            ListItem unitItem = ddlUnitName.Items.FindByValue(unit);
                            if (unitItem != null) unitItem.Selected = true;

                            txtDefaultUnitPrice.Text = dr["DefaultUnitPrice"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["DefaultUnitPrice"]).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                            txtDefaultWastePercent.Text = dr["DefaultWastePercent"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["DefaultWastePercent"]).ToString("N2", CultureInfo.InvariantCulture);
                            txtDescription.Text = dr["Description"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Description"]);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);

                            int orderCount = dr["OrderCount"] == DBNull.Value
                                ? 0 : Convert.ToInt32(dr["OrderCount"]);

                            if (orderCount > 0)
                            {
                                pnlEditWarning.Visible = true;
                                litEditOrderCount.Text = HttpUtility.HtmlEncode(orderCount.ToString("N0"));
                            }
                            else
                            {
                                pnlEditWarning.Visible = false;
                            }
                        }
                        else
                        {
                            ShowMessage("خدمت مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری خدمت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;

        string serviceCode = txtServiceCode.Text.Trim().ToUpper();
        string serviceName = txtServiceName.Text.Trim();
        string unitName = ddlUnitName.SelectedValue;
        string description = txtDescription.Text.Trim();
        bool isActive = chkIsActive.Checked;

        // Validation
        if (serviceCode.Length == 0)
        {
            ShowMessage("کد خدمت را وارد کنید.");
            return;
        }
        if (serviceCode.Length > 30)
        {
            ShowMessage("کد خدمت نمی‌تواند بیش از ۳۰ کاراکتر باشد.");
            return;
        }
        if (serviceName.Length == 0)
        {
            ShowMessage("نام خدمت را وارد کنید.");
            return;
        }
        if (serviceName.Length > 150)
        {
            ShowMessage("نام خدمت نمی‌تواند بیش از ۱۵۰ کاراکتر باشد.");
            return;
        }
        if (unitName == null) unitName = "کیلوگرم";

        decimal unitPrice = 0m;
        if (txtDefaultUnitPrice.Text.Trim().Length > 0)
        {
            decimal? p = ParseDecimal(txtDefaultUnitPrice.Text);
            if (!p.HasValue || p.Value < 0)
            {
                ShowMessage("نرخ پیش‌فرض باید عددی نامنفی باشد.");
                return;
            }
            unitPrice = p.Value;
        }

        decimal wastePercent = 0m;
        if (txtDefaultWastePercent.Text.Trim().Length > 0)
        {
            decimal? w = ParseDecimal(txtDefaultWastePercent.Text);
            if (!w.HasValue || w.Value < 0 || w.Value > 100)
            {
                ShowMessage("ضایعات باید بین ۰ تا ۱۰۰ باشد.");
                return;
            }
            wastePercent = w.Value;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Check duplicate code
                string dupSql =
                    "SELECT COUNT(*) FROM ServiceTypes " +
                    "WHERE ServiceCode = @Code AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND ServiceTypeId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Code", serviceCode);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("خدمتی با این کد قبلاً ثبت شده است.");
                        return;
                    }
                }

                // Check duplicate name
                string dupNameSql =
                    "SELECT COUNT(*) FROM ServiceTypes " +
                    "WHERE ServiceName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupNameSql += " AND ServiceTypeId <> @Id";

                using (SqlCommand dupName = new SqlCommand(dupNameSql, conn))
                {
                    dupName.Parameters.AddWithValue("@Name", serviceName);
                    if (currentId > 0) dupName.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dupName.ExecuteScalar()) > 0)
                    {
                        ShowMessage("خدمتی با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    // UPDATE
                    string updateSql =
                        "UPDATE ServiceTypes SET " +
                        "  ServiceCode = @Code, ServiceName = @Name, UnitName = @Unit, " +
                        "  DefaultUnitPrice = @Price, DefaultWastePercent = @Waste, " +
                        "  Description = @Desc, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE ServiceTypeId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", serviceCode);
                        cmd.Parameters.AddWithValue("@Name", serviceName);
                        cmd.Parameters.AddWithValue("@Unit", unitName);
                        cmd.Parameters.AddWithValue("@Price", unitPrice);
                        cmd.Parameters.AddWithValue("@Waste", wastePercent);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "ServiceTypes", currentId, serviceName, "ویرایش خدمت");
                    ShowMessage("تغییرات با موفقیت ذخیره شد.");
                }
                else
                {
                    // INSERT
                    string insertSql =
                        "INSERT INTO ServiceTypes " +
                        "  (ServiceCode, ServiceName, UnitName, DefaultUnitPrice, " +
                        "   DefaultWastePercent, Description, IsActive, IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@Code, @Name, @Unit, @Price, " +
                        "   @Waste, @Desc, @IsActive, 0, GETDATE(), @By); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", serviceCode);
                        cmd.Parameters.AddWithValue("@Name", serviceName);
                        cmd.Parameters.AddWithValue("@Unit", unitName);
                        cmd.Parameters.AddWithValue("@Price", unitPrice);
                        cmd.Parameters.AddWithValue("@Waste", wastePercent);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "ServiceTypes", newId, serviceName, "ایجاد خدمت");

                    ShowMessage("خدمت جدید با موفقیت افزوده شد.");
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
    protected void rptTypes_ItemCommand(object source, RepeaterCommandEventArgs e)
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

                string name = "";
                bool isActive = false;
                using (SqlCommand get = new SqlCommand(
                    "SELECT ServiceName, IsActive FROM ServiceTypes " +
                    "WHERE ServiceTypeId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["ServiceName"]);
                            isActive = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("خدمت مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ServiceTypes SET IsActive = @IsActive WHERE ServiceTypeId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "ServiceTypes", id, name,
                    newState ? "فعال‌سازی خدمت" : "غیرفعال‌سازی خدمت");
            }

            ShowMessage("وضعیت خدمت تغییر یافت.");
            LoadStats();
            LoadTypes();
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
                    "SELECT ServiceName FROM ServiceTypes WHERE ServiceTypeId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("خدمت مورد نظر یافت نشد.");
                    return;
                }

                // Check usage in orders
                int orderCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders " +
                    "WHERE ServiceTypeId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    orderCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (orderCount > 0)
                {
                    ShowMessage(string.Format(
                        "این خدمت در {0} سفارش استفاده شده است و قابل حذف نیست. " +
                        "به‌جای حذف، آن را غیرفعال کنید.", orderCount));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ServiceTypes SET IsDeleted = 1 WHERE ServiceTypeId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "ServiceTypes", id, name, "حذف نرم خدمت");
            }

            ShowMessage("خدمت با موفقیت حذف شد.");
            LoadStats();
            LoadTypes();
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

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
        catch { return "۰"; }
    }

    protected string GetWasteBadge(object wasteObj)
    {
        if (wasteObj == null || wasteObj == DBNull.Value)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        decimal w = Convert.ToDecimal(wasteObj);
        if (w <= 0m) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        string css = "badge bg-surface-muted text-ink-700";
        if (w >= 10m) css = "badge bg-danger-50 text-danger-600";
        else if (w >= 5m) css = "badge bg-amber-50 text-amber-700";
        else if (w > 0m) css = "badge bg-brand-50 text-brand-700";

        return "<span class=\"" + css + " font-mono\">" +
            w.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪</span>";
    }

    protected string GetDescriptionHint(object descObj)
    {
        if (descObj == null || descObj == DBNull.Value) return "";
        string desc = Convert.ToString(descObj).Trim();
        if (desc.Length == 0) return "";
        string preview = desc.Length > 70 ? desc.Substring(0, 67) + "..." : desc;
        return "<p class=\"mt-0.5 truncate text-[10.5px] text-ink-400\">" +
               HttpUtility.HtmlEncode(preview) + "</p>";
    }

    protected string GetServiceAvatarCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        return baseC + " bg-brand-50 text-brand-600";
    }

    protected string GetStatusBadgeClass(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        return isActive
            ? "badge bg-success-50 text-success-600"
            : "badge bg-warning-50 text-warning-600";
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
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('خدمت «" + name + "» غیرفعال شود؟');";
        return "return confirm('خدمت «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف خدمت «" + name + "» مطمئن هستید؟');";
    }

    protected bool CanDelete(object countObj)
    {
        int count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
        return count == 0;
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

    private void ClearForm()
    {
        txtServiceCode.Text = "";
        txtServiceName.Text = "";
        if (ddlUnitName.Items.Count > 0)
        {
            ddlUnitName.ClearSelection();
            ListItem item = ddlUnitName.Items.FindByValue("کیلوگرم");
            if (item != null) item.Selected = true;
        }
        txtDefaultUnitPrice.Text = "0";
        txtDefaultWastePercent.Text = "0";
        txtDescription.Text = "";
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