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

public partial class admin_customers : System.Web.UI.Page
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

    private int EditCustomerId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditCustomerId.Value, out id)) return id;
            return 0;
        }
        set { hfEditCustomerId.Value = value.ToString(CultureInfo.InvariantCulture); }
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
            LoadPriceLists();
            LoadMarketers();
            LoadStats();
            LoadCustomers();

            // پیش‌فرض: تک‌فروشی
            rbTypeRetail.Checked = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری لیست‌های قیمت
    // ─────────────────────────────────────────────────────
    private void LoadPriceLists()
    {
        ddlPriceList.Items.Clear();
        ddlPriceList.Items.Add(new ListItem("— انتخاب کنید —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT PriceListId, PriceListName FROM PriceLists " +
                    "WHERE IsActive = 1 ORDER BY IsDefault DESC, PriceListId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["PriceListId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["PriceListName"]);
                        ddlPriceList.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری لیست‌های قیمت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری بازاریاب‌ها (نقش «بازاریاب»)
    // ─────────────────────────────────────────────────────
    private void LoadMarketers()
    {
        ddlMarketer.Items.Clear();
        ddlMarketer.Items.Add(new ListItem("— بدون بازاریاب —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT u.UserId, u.FullName, u.Username " +
                    "FROM Users u " +
                    "INNER JOIN Roles r ON r.RoleId = u.RoleId " +
                    "WHERE u.IsActive = 1 AND u.IsDeleted = 0 " +
                    "  AND r.RoleName = N'بازاریاب' " +
                    "ORDER BY u.FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt64(dr["UserId"]).ToString(CultureInfo.InvariantCulture);
                        string fullName = dr["FullName"] == DBNull.Value
                            ? "" : Convert.ToString(dr["FullName"]).Trim();
                        string username = dr["Username"] == DBNull.Value
                            ? "" : Convert.ToString(dr["Username"]);

                        string label = fullName.Length > 0 ? fullName : username;
                        if (label.Length == 0) label = "کاربر #" + id;

                        ddlMarketer.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری بازاریابان: " + ex.Message);
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
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS TotalCustomers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND CustomerType = 1 THEN 1 ELSE 0 END) AS RetailCustomers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND CustomerType = 2 THEN 1 ELSE 0 END) AS WholesaleCustomers, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND CustomerType = 3 THEN 1 ELSE 0 END) AS AgentCustomers " +
                    "FROM Customers";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["TotalCustomers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatRetail.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["RetailCustomers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatWholesale.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["WholesaleCustomers"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatAgent.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["AgentCustomers"]).ToString("N0", CultureInfo.InvariantCulture));
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
        EditCustomerId = 0;
        litFormTitle.Text = "افزودن مشتری جدید";
        rbTypeRetail.Checked = true;

        pnlForm.Visible = true;
        pnlList.Visible = false;
        pnlStats.Visible = false;
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditCustomerId = 0;
        pnlForm.Visible = false;
        pnlList.Visible = true;
        pnlStats.Visible = true;
        LoadStats();
        LoadCustomers();
    }

    // ─────────────────────────────────────────────────────
    // لیست مشتریان
    // ─────────────────────────────────────────────────────
    private void LoadCustomers()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string typeFilter = ddlFilterType.SelectedValue;
            string activeFilter = ddlFilterActive.SelectedValue;
            string deletedFilter = ddlFilterDeleted.SelectedValue;
            if (typeFilter == null) typeFilter = "";
            if (activeFilter == null) activeFilter = "";
            if (deletedFilter == null) deletedFilter = "";

            int totalCount = GetTotalCount(search, typeFilter, activeFilter, deletedFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, typeFilter, activeFilter, deletedFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptCustomers.Visible = false;
                rptCustomers.DataSource = null;
                rptCustomers.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptCustomers.Visible = true;
                rptCustomers.DataSource = dt;
                rptCustomers.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }
            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} مشتری",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری مشتریان: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string typeFilter, string activeFilter, string deletedFilter,
        SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        if (search.Length > 0)
        {
            sb.Append(" AND (c.CustomerName LIKE @Search OR c.CustomerCode LIKE @Search " +
                      " OR c.Mobile LIKE @Search OR c.City LIKE @Search " +
                      " OR c.ContactPerson LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (typeFilter.Length > 0)
        {
            int t;
            if (int.TryParse(typeFilter, out t))
            {
                sb.Append(" AND c.CustomerType = @Type ");
                cmd.Parameters.AddWithValue("@Type", t);
            }
        }

        if (activeFilter.Length > 0)
        {
            sb.Append(" AND c.IsActive = @IsActive ");
            cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
        }

        if (deletedFilter.Length > 0)
        {
            sb.Append(" AND c.IsDeleted = @IsDeleted ");
            cmd.Parameters.AddWithValue("@IsDeleted", deletedFilter == "1" ? 1 : 0);
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string typeFilter, string activeFilter, string deletedFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "SELECT COUNT(*) FROM Customers c " +
                    BuildFilterSql(search, typeFilter, activeFilter, deletedFilter, cmd);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string typeFilter, string activeFilter, string deletedFilter,
        int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, typeFilter, activeFilter, deletedFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY c.CustomerId DESC) AS RowNum, " +
                    "    c.CustomerId, c.CustomerCode, c.CustomerName, c.CustomerType, " +
                    "    c.ContactPerson, c.Mobile, c.Phone, c.City, c.Region, " +
                    "    c.CreditLimit, c.IsActive, c.IsDeleted, " +
                    "    CASE WHEN EXISTS (SELECT 1 FROM SalesOrders so " +
                    "                      WHERE so.CustomerId = c.CustomerId) THEN 1 ELSE 0 END AS HasActivity " +
                    "  FROM Customers c " +
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
        LoadCustomers();
    }

    protected void ddlFilterType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadCustomers();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadCustomers();
    }

    protected void ddlFilterDeleted_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadCustomers();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadCustomers(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadCustomers();
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
    protected void rptCustomers_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int customerId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out customerId) || customerId <= 0)
            return;

        if (e.CommandName == "Edit") LoadCustomerForEdit(customerId);
        else if (e.CommandName == "SoftDelete") SoftDeleteCustomer(customerId);
        else if (e.CommandName == "Restore") RestoreCustomer(customerId);
        else if (e.CommandName == "ToggleActive") ToggleActiveCustomer(customerId);
    }

    private void LoadCustomerForEdit(int customerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CustomerId, CustomerCode, CustomerName, CustomerType, " +
                    "       ContactPerson, Mobile, Phone, Address, City, Region, PostalCode, " +
                    "       NationalId, EconomicCode, CreditLimit, PriceListId, MarketerId, " +
                    "       Notes, IsActive " +
                    "FROM Customers WHERE CustomerId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditCustomerId = customerId;

                            txtCustomerCode.Text = Convert.ToString(dr["CustomerCode"]);
                            txtCustomerName.Text = Convert.ToString(dr["CustomerName"]);
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
                            txtRegion.Text = dr["Region"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Region"]);
                            txtPostalCode.Text = dr["PostalCode"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PostalCode"]);
                            txtNationalId.Text = dr["NationalId"] == DBNull.Value
                                ? "" : Convert.ToString(dr["NationalId"]);
                            txtEconomicCode.Text = dr["EconomicCode"] == DBNull.Value
                                ? "" : Convert.ToString(dr["EconomicCode"]);
                            txtCreditLimit.Text = dr["CreditLimit"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["CreditLimit"]).ToString(CultureInfo.InvariantCulture);
                            txtNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);

                            // نوع مشتری
                            int type = dr["CustomerType"] == DBNull.Value
                                ? 1 : Convert.ToInt32(dr["CustomerType"]);
                            rbTypeRetail.Checked = (type == 1);
                            rbTypeWholesale.Checked = (type == 2);
                            rbTypeAgent.Checked = (type == 3);

                            // لیست قیمت
                            ddlPriceList.ClearSelection();
                            if (dr["PriceListId"] == DBNull.Value)
                            {
                                ListItem emptyItem = ddlPriceList.Items.FindByValue("");
                                if (emptyItem != null) emptyItem.Selected = true;
                            }
                            else
                            {
                                string plVal = Convert.ToInt32(dr["PriceListId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem plItem = ddlPriceList.Items.FindByValue(plVal);
                                if (plItem != null) plItem.Selected = true;
                            }

                            // بازاریاب
                            ddlMarketer.ClearSelection();
                            if (dr["MarketerId"] == DBNull.Value)
                            {
                                ListItem emptyItem = ddlMarketer.Items.FindByValue("");
                                if (emptyItem != null) emptyItem.Selected = true;
                            }
                            else
                            {
                                string mVal = Convert.ToInt64(dr["MarketerId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem mItem = ddlMarketer.Items.FindByValue(mVal);
                                if (mItem != null) mItem.Selected = true;
                            }

                            // وضعیت
                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem activeItem = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (activeItem != null) activeItem.Selected = true;

                            litFormTitle.Text = "ویرایش مشتری — " + Convert.ToString(dr["CustomerName"]);

                            pnlForm.Visible = true;
                            pnlList.Visible = false;
                            pnlStats.Visible = false;
                        }
                        else
                        {
                            ShowMessage("مشتری مورد نظر یافت نشد.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری مشتری: " + ex.Message);
        }
    }

    private int GetSelectedCustomerType()
    {
        if (rbTypeWholesale.Checked) return 2;
        if (rbTypeAgent.Checked) return 3;
        return 1;
    }

    // ─────────────────────────────────────────────────────
    // ذخیره
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditCustomerId;

        string customerCode = txtCustomerCode.Text.Trim();
        string customerName = txtCustomerName.Text.Trim();
        string contactPerson = txtContactPerson.Text.Trim();
        string mobile = txtMobile.Text.Trim();
        string phone = txtPhone.Text.Trim();
        string address = txtAddress.Text.Trim();
        string city = txtCity.Text.Trim();
        string region = txtRegion.Text.Trim();
        string postalCode = txtPostalCode.Text.Trim();
        string nationalId = txtNationalId.Text.Trim();
        string economicCode = txtEconomicCode.Text.Trim();
        string notes = txtNotes.Text.Trim();
        bool isActive = (ddlIsActive.SelectedValue == "1");

        int customerType = GetSelectedCustomerType();

        // اعتبارسنجی
        if (customerCode.Length == 0)
        {
            ShowMessage("کد مشتری را وارد کنید.");
            return;
        }
        if (customerCode.Length > 50)
        {
            ShowMessage("کد مشتری نمی‌تواند بیش از ۵۰ کاراکتر باشد.");
            return;
        }
        if (!IsValidCode(customerCode))
        {
            ShowMessage("کد مشتری فقط می‌تواند شامل حروف انگلیسی، عدد، خط تیره و آندرلاین باشد.");
            return;
        }
        if (customerName.Length == 0)
        {
            ShowMessage("نام مشتری را وارد کنید.");
            return;
        }
        if (customerName.Length > 200)
        {
            ShowMessage("نام مشتری نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
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
        if (region.Length > 100)
        {
            ShowMessage("نام منطقه بیش از حد طولانی است.");
            return;
        }
        if (postalCode.Length > 20)
        {
            ShowMessage("کد پستی معتبر نیست.");
            return;
        }
        if (nationalId.Length > 20)
        {
            ShowMessage("کد ملی معتبر نیست.");
            return;
        }
        if (economicCode.Length > 20)
        {
            ShowMessage("کد اقتصادی معتبر نیست.");
            return;
        }
        if (notes.Length > 1000)
        {
            ShowMessage("یادداشت نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
            return;
        }

        // سقف اعتبار
        decimal creditLimit = 0m;
        if (txtCreditLimit.Text.Trim().Length > 0)
        {
            decimal? cl = ParseDecimal(txtCreditLimit.Text);
            if (!cl.HasValue)
            {
                ShowMessage("سقف اعتبار معتبر نیست.");
                return;
            }
            if (cl.Value < 0)
            {
                ShowMessage("سقف اعتبار نمی‌تواند منفی باشد.");
                return;
            }
            creditLimit = cl.Value;
        }

        // PriceListId (اختیاری)
        int? priceListId = null;
        if (!string.IsNullOrEmpty(ddlPriceList.SelectedValue))
        {
            int pl;
            if (int.TryParse(ddlPriceList.SelectedValue, out pl) && pl > 0)
                priceListId = pl;
        }

        // MarketerId (اختیاری)
        int? marketerId = null;
        if (!string.IsNullOrEmpty(ddlMarketer.SelectedValue))
        {
            int m;
            if (int.TryParse(ddlMarketer.SelectedValue, out m) && m > 0)
                marketerId = m;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── چک تکراری بودن کد ──
                string dupSql = "SELECT COUNT(*) FROM Customers WHERE CustomerCode = @Code";
                if (currentId > 0) dupSql += " AND CustomerId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Code", customerCode);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);

                    int cnt = Convert.ToInt32(dup.ExecuteScalar());
                    if (cnt > 0)
                    {
                        ShowMessage("مشتری‌ای با این کد قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE Customers SET " +
                        "  CustomerCode = @Code, CustomerName = @Name, CustomerType = @Type, " +
                        "  ContactPerson = @Contact, Mobile = @Mobile, Phone = @Phone, " +
                        "  Address = @Address, City = @City, Region = @Region, PostalCode = @Postal, " +
                        "  NationalId = @NationalId, EconomicCode = @EconomicCode, " +
                        "  CreditLimit = @CreditLimit, PriceListId = @PriceListId, MarketerId = @MarketerId, " +
                        "  Notes = @Notes, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                        "WHERE CustomerId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", customerCode);
                        cmd.Parameters.AddWithValue("@Name", customerName);
                        cmd.Parameters.AddWithValue("@Type", customerType);
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
                        cmd.Parameters.AddWithValue("@Region",
                            region.Length > 0 ? (object)region : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Postal",
                            postalCode.Length > 0 ? (object)postalCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NationalId",
                            nationalId.Length > 0 ? (object)nationalId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@EconomicCode",
                            economicCode.Length > 0 ? (object)economicCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreditLimit", creditLimit);
                        cmd.Parameters.AddWithValue("@PriceListId",
                            priceListId.HasValue ? (object)priceListId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MarketerId",
                            marketerId.HasValue ? (object)marketerId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UpdatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", currentId, customerName, "ویرایش مشتری");
                    ShowMessage("تغییرات مشتری با موفقیت ذخیره شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO Customers " +
                        "  (CustomerCode, CustomerName, CustomerType, ContactPerson, Mobile, Phone, " +
                        "   Address, City, Region, PostalCode, NationalId, EconomicCode, " +
                        "   CreditLimit, PriceListId, MarketerId, Notes, IsActive, IsDeleted, " +
                        "   CreatedAt, CreatedBy) " +
                        "VALUES (@Code, @Name, @Type, @Contact, @Mobile, @Phone, " +
                        "   @Address, @City, @Region, @Postal, @NationalId, @EconomicCode, " +
                        "   @CreditLimit, @PriceListId, @MarketerId, @Notes, @IsActive, 0, " +
                        "   GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", customerCode);
                        cmd.Parameters.AddWithValue("@Name", customerName);
                        cmd.Parameters.AddWithValue("@Type", customerType);
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
                        cmd.Parameters.AddWithValue("@Region",
                            region.Length > 0 ? (object)region : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Postal",
                            postalCode.Length > 0 ? (object)postalCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NationalId",
                            nationalId.Length > 0 ? (object)nationalId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@EconomicCode",
                            economicCode.Length > 0 ? (object)economicCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreditLimit", creditLimit);
                        cmd.Parameters.AddWithValue("@PriceListId",
                            priceListId.HasValue ? (object)priceListId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MarketerId",
                            marketerId.HasValue ? (object)marketerId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            newId = Convert.ToInt32(result);
                    }

                    if (newId > 0)
                    {
                        WriteAudit(conn, "Insert", newId, customerName, "ایجاد مشتری جدید");
                    }

                    ShowMessage("مشتری جدید با موفقیت افزوده شد.");
                }
            }

            ClearForm();
            EditCustomerId = 0;
            pnlForm.Visible = false;
            pnlList.Visible = true;
            pnlStats.Visible = true;
            LoadStats();
            LoadCustomers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره مشتری: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Soft Delete / Restore
    // ─────────────────────────────────────────────────────
    private void SoftDeleteCustomer(int customerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string customerName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT CustomerName FROM Customers WHERE CustomerId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", customerId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) customerName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(customerName))
                {
                    ShowMessage("مشتری مورد نظر یافت نشد.");
                    return;
                }

                if (HasActivity(conn, customerId))
                {
                    ShowMessage("این مشتری دارای سفارش یا فاکتور است و قابل حذف نیست. برای حفظ سوابق، آن را غیرفعال کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Customers SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", customerId, customerName, "حذف نرم مشتری");
            }

            ShowMessage("مشتری با موفقیت حذف شد.");
            LoadStats();
            LoadCustomers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف مشتری: " + ex.Message);
        }
    }

    private void RestoreCustomer(int customerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string customerName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT CustomerName FROM Customers WHERE CustomerId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", customerId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) customerName = Convert.ToString(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Customers SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", customerId, customerName, "بازگردانی مشتری");
            }

            ShowMessage("مشتری با موفقیت بازگردانده شد.");
            LoadStats();
            LoadCustomers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بازگردانی مشتری: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // فعال/غیرفعال
    // ─────────────────────────────────────────────────────
    private void ToggleActiveCustomer(int customerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string customerName = "";
                bool isActive = false;

                using (SqlCommand get = new SqlCommand(
                    "SELECT CustomerName, IsActive FROM Customers WHERE CustomerId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", customerId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            customerName = Convert.ToString(dr["CustomerName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(customerName))
                {
                    ShowMessage("مشتری مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Customers SET IsActive = @IsActive, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", customerId, customerName,
                    newState ? "فعال‌سازی مشتری" : "غیرفعال‌سازی مشتری");
            }

            ShowMessage("وضعیت مشتری تغییر یافت.");
            LoadStats();
            LoadCustomers();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تغییر وضعیت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // کمک‌کننده‌ها
    // ─────────────────────────────────────────────────────
    private bool HasActivity(SqlConnection conn, int customerId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM SalesOrders WHERE CustomerId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", customerId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM ProformaInvoices WHERE CustomerId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", customerId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM FinalInvoices WHERE CustomerId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", customerId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        return false;
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

    private decimal? ParseDecimal(string input)
    {
        if (input == null) return null;
        input = input.Trim();
        if (input.Length == 0) return null;

        input = input.Replace(",", "").Replace("،", "");
        decimal d;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            return d;

        return null;
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
                "VALUES (@UserId, @Action, 'Customers', @EntityId, @NewValues, @IPAddress, GETDATE())";

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

    protected string GetCustomerIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "store";
        if (type == 2) return "building-2";
        return "briefcase";
    }

    protected string GetCustomerAvatarCss(object typeObj, object isActiveObj, object isDeletedObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);

        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-full";

        if (isDeleted) return baseC + " bg-surface-muted text-ink-400 line-through";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";

        if (type == 1) return baseC + " bg-emerald-500 text-white";
        if (type == 2) return baseC + " bg-amber-500 text-white";
        return baseC + " bg-violet-500 text-white";
    }

    protected string GetTypeLabel(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        return "نمایندگی";
    }

    protected string GetTypeBadgeCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "badge bg-emerald-50 text-emerald-700";
        if (type == 2) return "badge bg-amber-50 text-amber-700";
        return "badge bg-violet-50 text-violet-700";
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

    protected string GetCreditBadgeCss(object creditObj)
    {
        decimal credit = (creditObj == null || creditObj == DBNull.Value) ? 0m : Convert.ToDecimal(creditObj);
        if (credit <= 0) return "badge bg-surface-muted text-ink-500";
        return "badge bg-brand-50 text-brand-700";
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

    protected bool IsCustomerDeleted(object isDeletedObj)
    {
        if (isDeletedObj == null || isDeletedObj == DBNull.Value) return false;
        return Convert.ToBoolean(isDeletedObj);
    }

    protected bool CanDeleteCustomer(object isDeletedObj, object hasActivityObj)
    {
        if (IsCustomerDeleted(isDeletedObj)) return false;
        bool hasActivity = (hasActivityObj == null || hasActivityObj == DBNull.Value)
            ? false : Convert.ToBoolean(hasActivityObj);
        return !hasActivity;
    }

    protected bool ShouldShowLock(object isDeletedObj, object hasActivityObj)
    {
        if (IsCustomerDeleted(isDeletedObj)) return false;
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

        if (isActive) return "return confirm('مشتری «" + name + "» غیرفعال شود؟');";
        return "return confirm('مشتری «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف مشتری «" + name + "» مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // ClearForm
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        txtCustomerCode.Text = "";
        txtCustomerName.Text = "";
        txtContactPerson.Text = "";
        txtMobile.Text = "";
        txtPhone.Text = "";
        txtAddress.Text = "";
        txtCity.Text = "";
        txtRegion.Text = "";
        txtPostalCode.Text = "";
        txtNationalId.Text = "";
        txtEconomicCode.Text = "";
        txtCreditLimit.Text = "0";
        txtNotes.Text = "";

        rbTypeRetail.Checked = true;
        rbTypeWholesale.Checked = false;
        rbTypeAgent.Checked = false;

        if (ddlPriceList.Items.Count > 0)
        {
            ddlPriceList.ClearSelection();
            ddlPriceList.SelectedIndex = 0;
        }

        if (ddlMarketer.Items.Count > 0)
        {
            ddlMarketer.ClearSelection();
            ddlMarketer.SelectedIndex = 0;
        }

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