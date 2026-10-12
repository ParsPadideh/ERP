using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_suppliers : System.Web.UI.Page
{
    private const int PageSize = 10;

    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
    }

    private int EditSupplierId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditSupplierId.Value, out id)) return id;
            return 0;
        }
        set { hfEditSupplierId.Value = value.ToString(CultureInfo.InvariantCulture); }
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
            LoadStats();
            LoadSuppliers();
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

                // ── استفاده از Derived Table برای محاسبه HasOrders قبل از SUM ──
                string sql =
                    "SELECT " +
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS TotalSuppliers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveSuppliers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 0 THEN 1 ELSE 0 END) AS InactiveSuppliers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND HasOrders = 1 THEN 1 ELSE 0 END) AS WithOrders " +
                    "FROM ( " +
                    "  SELECT s.SupplierId, s.IsDeleted, s.IsActive, " +
                    "    CASE WHEN EXISTS (SELECT 1 FROM PurchaseOrders po " +
                    "                      WHERE po.SupplierId = s.SupplierId) " +
                    "         THEN 1 ELSE 0 END AS HasOrders " +
                    "  FROM Suppliers s " +
                    ") x";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["TotalSuppliers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatActive.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["ActiveSuppliers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatInactive.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["InactiveSuppliers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatWithOrders.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["WithOrders"]).ToString("N0", CultureInfo.InvariantCulture));
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
    // فرم
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditSupplierId = 0;
        litFormTitle.Text = "افزودن تأمین‌کننده جدید";

        pnlForm.Visible = true;
        pnlList.Visible = false;
        pnlStats.Visible = false;
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditSupplierId = 0;
        pnlForm.Visible = false;
        pnlList.Visible = true;
        pnlStats.Visible = true;
        LoadStats();
        LoadSuppliers();
    }

    // ─────────────────────────────────────────────────────
    // لیست
    // ─────────────────────────────────────────────────────
    private void LoadSuppliers()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string activeFilter = ddlFilterActive.SelectedValue;
            string deletedFilter = ddlFilterDeleted.SelectedValue;
            if (activeFilter == null) activeFilter = "";
            if (deletedFilter == null) deletedFilter = "";

            int totalCount = GetTotalCount(search, activeFilter, deletedFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, activeFilter, deletedFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptSuppliers.Visible = false;
                rptSuppliers.DataSource = null;
                rptSuppliers.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptSuppliers.Visible = true;
                rptSuppliers.DataSource = dt;
                rptSuppliers.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }
            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} تأمین‌کننده",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تأمین‌کنندگان: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string activeFilter, string deletedFilter, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        if (search.Length > 0)
        {
            sb.Append(" AND (s.SupplierName LIKE @Search OR s.SupplierCode LIKE @Search " +
                      " OR s.Mobile LIKE @Search OR s.City LIKE @Search " +
                      " OR s.ContactPerson LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (activeFilter.Length > 0)
        {
            sb.Append(" AND s.IsActive = @IsActive ");
            cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
        }

        if (deletedFilter.Length > 0)
        {
            sb.Append(" AND s.IsDeleted = @IsDeleted ");
            cmd.Parameters.AddWithValue("@IsDeleted", deletedFilter == "1" ? 1 : 0);
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string activeFilter, string deletedFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "SELECT COUNT(*) FROM Suppliers s " +
                    BuildFilterSql(search, activeFilter, deletedFilter, cmd);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string activeFilter, string deletedFilter,
        int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, activeFilter, deletedFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY s.SupplierId DESC) AS RowNum, " +
                    "    s.SupplierId, s.SupplierCode, s.SupplierName, s.ContactPerson, " +
                    "    s.Mobile, s.Phone, s.City, s.IsActive, s.IsDeleted, " +
                    "    ISNULL((SELECT COUNT(*) FROM PurchaseOrders po " +
                    "            WHERE po.SupplierId = s.SupplierId), 0) AS OrderCount, " +
                    "    CASE WHEN EXISTS (SELECT 1 FROM PurchaseOrders po " +
                    "                      WHERE po.SupplierId = s.SupplierId) THEN 1 ELSE 0 END AS HasActivity " +
                    "  FROM Suppliers s " +
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
        LoadSuppliers();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadSuppliers();
    }

    protected void ddlFilterDeleted_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadSuppliers();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadSuppliers(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadSuppliers();
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
    protected void rptSuppliers_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int supplierId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out supplierId) || supplierId <= 0)
            return;

        if (e.CommandName == "Edit") LoadSupplierForEdit(supplierId);
        else if (e.CommandName == "SoftDelete") SoftDeleteSupplier(supplierId);
        else if (e.CommandName == "Restore") RestoreSupplier(supplierId);
        else if (e.CommandName == "ToggleActive") ToggleActiveSupplier(supplierId);
    }

    private void LoadSupplierForEdit(int supplierId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT SupplierId, SupplierCode, SupplierName, ContactPerson, " +
                    "       Mobile, Phone, Address, City, NationalId, PaymentTerms, " +
                    "       Notes, IsActive " +
                    "FROM Suppliers WHERE SupplierId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", supplierId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditSupplierId = supplierId;

                            txtSupplierCode.Text = Convert.ToString(dr["SupplierCode"]);
                            txtSupplierName.Text = Convert.ToString(dr["SupplierName"]);
                            txtContactPerson.Text = dr["ContactPerson"] == DBNull.Value
                                ? "" : Convert.ToString(dr["ContactPerson"]);
                            txtMobile.Text = dr["Mobile"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Mobile"]);
                            txtPhone.Text = dr["Phone"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Phone"]);
                            txtAddress.Text = dr["Address"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Address"]);
                            txtCity.Text = dr["City"] == DBNull.Value
                                ? "" : Convert.ToString(dr["City"]);
                            txtNationalId.Text = dr["NationalId"] == DBNull.Value
                                ? "" : Convert.ToString(dr["NationalId"]);
                            txtPaymentTerms.Text = dr["PaymentTerms"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PaymentTerms"]);
                            txtNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);

                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem activeItem = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (activeItem != null) activeItem.Selected = true;

                            litFormTitle.Text = "ویرایش تأمین‌کننده — " + Convert.ToString(dr["SupplierName"]);

                            pnlForm.Visible = true;
                            pnlList.Visible = false;
                            pnlStats.Visible = false;
                        }
                        else
                        {
                            ShowMessage("تأمین‌کننده مورد نظر یافت نشد.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تأمین‌کننده: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // ذخیره
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditSupplierId;

        string supplierCode = txtSupplierCode.Text.Trim();
        string supplierName = txtSupplierName.Text.Trim();
        string contactPerson = txtContactPerson.Text.Trim();
        string mobile = txtMobile.Text.Trim();
        string phone = txtPhone.Text.Trim();
        string address = txtAddress.Text.Trim();
        string city = txtCity.Text.Trim();
        string nationalId = txtNationalId.Text.Trim();
        string paymentTerms = txtPaymentTerms.Text.Trim();
        string notes = txtNotes.Text.Trim();
        bool isActive = (ddlIsActive.SelectedValue == "1");

        // اعتبارسنجی
        if (supplierCode.Length == 0)
        {
            ShowMessage("کد تأمین‌کننده را وارد کنید.");
            return;
        }
        if (supplierCode.Length > 50)
        {
            ShowMessage("کد تأمین‌کننده نمی‌تواند بیش از ۵۰ کاراکتر باشد.");
            return;
        }
        if (!IsValidCode(supplierCode))
        {
            ShowMessage("کد تأمین‌کننده فقط می‌تواند شامل حروف انگلیسی، عدد، خط تیره و آندرلاین باشد.");
            return;
        }
        if (supplierName.Length == 0)
        {
            ShowMessage("نام تأمین‌کننده را وارد کنید.");
            return;
        }
        if (supplierName.Length > 200)
        {
            ShowMessage("نام تأمین‌کننده نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            return;
        }
        if (contactPerson.Length > 100)
        {
            ShowMessage("نام شخص رابط بیش از حد طولانی است.");
            return;
        }
        if (mobile.Length > 15)
        {
            ShowMessage("شماره موبایل معتبر نیست.");
            return;
        }
        if (phone.Length > 15)
        {
            ShowMessage("شماره تلفن معتبر نیست.");
            return;
        }
        if (address.Length > 500)
        {
            ShowMessage("آدرس نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
            return;
        }
        if (city.Length > 100)
        {
            ShowMessage("نام شهر بیش از حد طولانی است.");
            return;
        }
        if (nationalId.Length > 20)
        {
            ShowMessage("کد ملی معتبر نیست.");
            return;
        }
        if (paymentTerms.Length > 200)
        {
            ShowMessage("شرایط پرداخت نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            return;
        }
        if (notes.Length > 1000)
        {
            ShowMessage("یادداشت نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── چک تکراری بودن کد ──
                string dupSql = "SELECT COUNT(*) FROM Suppliers WHERE SupplierCode = @Code";
                if (currentId > 0) dupSql += " AND SupplierId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Code", supplierCode);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);

                    int cnt = Convert.ToInt32(dup.ExecuteScalar());
                    if (cnt > 0)
                    {
                        ShowMessage("تأمین‌کننده‌ای با این کد قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE Suppliers SET " +
                        "  SupplierCode = @Code, SupplierName = @Name, ContactPerson = @Contact, " +
                        "  Mobile = @Mobile, Phone = @Phone, Address = @Address, City = @City, " +
                        "  NationalId = @NationalId, PaymentTerms = @PaymentTerms, Notes = @Notes, " +
                        "  IsActive = @IsActive " +
                        "WHERE SupplierId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", supplierCode);
                        cmd.Parameters.AddWithValue("@Name", supplierName);
                        cmd.Parameters.AddWithValue("@Contact",
                            contactPerson.Length > 0 ? (object)contactPerson : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Mobile",
                            mobile.Length > 0 ? (object)mobile : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone",
                            phone.Length > 0 ? (object)phone : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address",
                            address.Length > 0 ? (object)address : DBNull.Value);
                        cmd.Parameters.AddWithValue("@City",
                            city.Length > 0 ? (object)city : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NationalId",
                            nationalId.Length > 0 ? (object)nationalId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@PaymentTerms",
                            paymentTerms.Length > 0 ? (object)paymentTerms : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", currentId, supplierName, "ویرایش تأمین‌کننده");
                    ShowMessage("تغییرات تأمین‌کننده با موفقیت ذخیره شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO Suppliers " +
                        "  (SupplierCode, SupplierName, ContactPerson, Mobile, Phone, " +
                        "   Address, City, NationalId, PaymentTerms, Notes, " +
                        "   IsActive, IsDeleted, CreatedAt) " +
                        "VALUES (@Code, @Name, @Contact, @Mobile, @Phone, " +
                        "   @Address, @City, @NationalId, @PaymentTerms, @Notes, " +
                        "   @IsActive, 0, GETDATE()); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", supplierCode);
                        cmd.Parameters.AddWithValue("@Name", supplierName);
                        cmd.Parameters.AddWithValue("@Contact",
                            contactPerson.Length > 0 ? (object)contactPerson : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Mobile",
                            mobile.Length > 0 ? (object)mobile : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone",
                            phone.Length > 0 ? (object)phone : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address",
                            address.Length > 0 ? (object)address : DBNull.Value);
                        cmd.Parameters.AddWithValue("@City",
                            city.Length > 0 ? (object)city : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NationalId",
                            nationalId.Length > 0 ? (object)nationalId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@PaymentTerms",
                            paymentTerms.Length > 0 ? (object)paymentTerms : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            newId = Convert.ToInt32(result);
                    }

                    if (newId > 0)
                    {
                        WriteAudit(conn, "Insert", newId, supplierName, "ایجاد تأمین‌کننده جدید");
                    }

                    ShowMessage("تأمین‌کننده جدید با موفقیت افزوده شد.");
                }
            }

            ClearForm();
            EditSupplierId = 0;
            pnlForm.Visible = false;
            pnlList.Visible = true;
            pnlStats.Visible = true;
            LoadStats();
            LoadSuppliers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره تأمین‌کننده: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Soft Delete / Restore
    // ─────────────────────────────────────────────────────
    private void SoftDeleteSupplier(int supplierId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string supplierName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT SupplierName FROM Suppliers WHERE SupplierId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", supplierId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) supplierName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(supplierName))
                {
                    ShowMessage("تأمین‌کننده مورد نظر یافت نشد.");
                    return;
                }

                if (HasActivity(conn, supplierId))
                {
                    ShowMessage("این تأمین‌کننده دارای سفارش خرید است و قابل حذف نیست. برای حفظ سوابق، آن را غیرفعال کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Suppliers SET IsDeleted = 1, IsActive = 0 WHERE SupplierId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", supplierId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", supplierId, supplierName, "حذف نرم تأمین‌کننده");
            }

            ShowMessage("تأمین‌کننده با موفقیت حذف شد.");
            LoadStats();
            LoadSuppliers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف تأمین‌کننده: " + ex.Message);
        }
    }

    private void RestoreSupplier(int supplierId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string supplierName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT SupplierName FROM Suppliers WHERE SupplierId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", supplierId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) supplierName = Convert.ToString(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Suppliers SET IsDeleted = 0 WHERE SupplierId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", supplierId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", supplierId, supplierName, "بازگردانی تأمین‌کننده");
            }

            ShowMessage("تأمین‌کننده با موفقیت بازگردانده شد.");
            LoadStats();
            LoadSuppliers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بازگردانی تأمین‌کننده: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // فعال/غیرفعال
    // ─────────────────────────────────────────────────────
    private void ToggleActiveSupplier(int supplierId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string supplierName = "";
                bool isActive = false;

                using (SqlCommand get = new SqlCommand(
                    "SELECT SupplierName, IsActive FROM Suppliers WHERE SupplierId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", supplierId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            supplierName = Convert.ToString(dr["SupplierName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(supplierName))
                {
                    ShowMessage("تأمین‌کننده مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Suppliers SET IsActive = @IsActive WHERE SupplierId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", supplierId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", supplierId, supplierName,
                    newState ? "فعال‌سازی تأمین‌کننده" : "غیرفعال‌سازی تأمین‌کننده");
            }

            ShowMessage("وضعیت تأمین‌کننده تغییر یافت.");
            LoadStats();
            LoadSuppliers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تغییر وضعیت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // کمک‌کننده‌ها
    // ─────────────────────────────────────────────────────
    private bool HasActivity(SqlConnection conn, int supplierId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM PurchaseOrders WHERE SupplierId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", supplierId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            return c > 0;
        }
    }

    private bool IsValidCode(string code)
    {
        if (code == null) return false;
        if (code.Length < 1 || code.Length > 50) return false;

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            bool ok = (c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == '-' || c == '_' || c == '.';
            if (!ok) return false;
        }
        return true;
    }

    // ─────────────────────────────────────────────────────
    // AuditLog
    // ─────────────────────────────────────────────────────
    private void WriteAudit(SqlConnection conn, string action, int entityId, string targetName, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, 'Suppliers', @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId",
                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityId", entityId);

                string json = "{\"target\":\"" + (targetName != null ? targetName.Replace("\"", "'") : "")
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

    protected string GetSupplierAvatarCss(object isActiveObj, object isDeletedObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);

        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-full";

        if (isDeleted) return baseC + " bg-surface-muted text-ink-400 line-through";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        return baseC + " bg-sky-500 text-white";
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

    protected string GetOrderCountBadgeCss(object countObj)
    {
        int count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
        if (count == 0) return "badge bg-surface-muted text-ink-500";
        return "badge bg-brand-50 text-brand-700";
    }

    protected bool IsSupplierDeleted(object isDeletedObj)
    {
        if (isDeletedObj == null || isDeletedObj == DBNull.Value) return false;
        return Convert.ToBoolean(isDeletedObj);
    }

    protected bool CanDeleteSupplier(object isDeletedObj, object hasActivityObj)
    {
        if (IsSupplierDeleted(isDeletedObj)) return false;
        bool hasActivity = (hasActivityObj == null || hasActivityObj == DBNull.Value)
            ? false : Convert.ToBoolean(hasActivityObj);
        return !hasActivity;
    }

    protected bool ShouldShowLock(object isDeletedObj, object hasActivityObj)
    {
        if (IsSupplierDeleted(isDeletedObj)) return false;
        bool hasActivity = (hasActivityObj == null || hasActivityObj == DBNull.Value)
            ? false : Convert.ToBoolean(hasActivityObj);
        return hasActivity;
    }

    protected string GetToggleCss(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        if (isActive) return "icon-button hover:bg-warning-50 hover:text-warning-600";
        return "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        return isActive ? "غیرفعال کردن" : "فعال کردن";
    }

    protected string GetToggleIcon(object isActiveObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        return isActive ? "toggle-right" : "toggle-left";
    }

    protected string GetToggleConfirm(object isActiveObj, object nameObj)
    {
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? false : Convert.ToBoolean(isActiveObj);
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");

        if (isActive) return "return confirm('تأمین‌کننده «" + name + "» غیرفعال شود؟');";
        return "return confirm('تأمین‌کننده «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف تأمین‌کننده «" + name + "» مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // ClearForm
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        txtSupplierCode.Text = "";
        txtSupplierName.Text = "";
        txtContactPerson.Text = "";
        txtMobile.Text = "";
        txtPhone.Text = "";
        txtAddress.Text = "";
        txtCity.Text = "";
        txtNationalId.Text = "";
        txtPaymentTerms.Text = "";
        txtNotes.Text = "";

        if (ddlIsActive.Items.Count > 0)
        {
            ddlIsActive.ClearSelection();
            ListItem activeItem = ddlIsActive.Items.FindByValue("1");
            if (activeItem != null) activeItem.Selected = true;
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