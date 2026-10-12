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

public partial class admin_sales_orders : System.Web.UI.Page
{
    private const int PageSize = 15;

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

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
    }

    private int EditSOId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditSOId.Value, out id)) return id;
            return 0;
        }
        set { hfEditSOId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int CurrentSOId
    {
        get
        {
            int id;
            if (int.TryParse(hfCurrentSOId.Value, out id)) return id;
            return 0;
        }
        set { hfCurrentSOId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditItemId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditItemId.Value, out id)) return id;
            return 0;
        }
        set { hfEditItemId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private string StatusFilter
    {
        get
        {
            if (hfStatusFilter.Value == null) return "";
            return hfStatusFilter.Value.Trim();
        }
        set { hfStatusFilter.Value = value; }
    }

    private string ChannelFilter
    {
        get
        {
            if (hfChannelFilter.Value == null) return "";
            return hfChannelFilter.Value.Trim();
        }
        set { hfChannelFilter.Value = value; }
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

        HandlePostbackRequests();

        if (!IsPostBack)
        {
            CurrentPage = 1;
            StatusFilter = "";
            ChannelFilter = "";

            LoadCustomerFilter();

            string idParam = Request.QueryString["id"];
            string custParam = Request.QueryString["customerId"];

            if (!string.IsNullOrEmpty(custParam))
            {
                ListItem item = ddlFilterCustomer.Items.FindByValue(custParam);
                if (item != null)
                {
                    ddlFilterCustomer.ClearSelection();
                    item.Selected = true;
                }
            }

            if (!string.IsNullOrEmpty(idParam))
            {
                int soId;
                if (int.TryParse(idParam, out soId) && soId > 0)
                {
                    ShowFormView(soId);
                    return;
                }
            }

            ShowListView();
        }
    }

    private void HandlePostbackRequests()
    {
        string target = Request.Form["__EVENTTARGET"];
        string arg = Request.Form["__EVENTARGUMENT"];

        if (target != "__PAGE" || string.IsNullOrEmpty(arg)) return;

        if (arg.StartsWith("setStatus:"))
        {
            StatusFilter = arg.Substring("setStatus:".Length);
            CurrentPage = 1;
            LoadStats();
            LoadOrders();
        }
        else if (arg.StartsWith("setChannel:"))
        {
            ChannelFilter = arg.Substring("setChannel:".Length);
            CurrentPage = 1;
            LoadStats();
            LoadOrders();
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

        litPageTitle.Text = "سفارش‌های فروش";
        litPageSubtitle.Text = "ثبت و پیگیری سفارش‌های فروش از سه کانال بازاریاب، آنلاین و ادمین.";

        LoadStats();
        LoadOrders();
    }

    private void ShowFormView(int soId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;

        phBackToList.Visible = true;
        phAddNew.Visible = false;

        LoadCustomersDropdown();
        LoadMarketersDropdown();

        if (soId > 0)
        {
            EditSOId = soId;
            CurrentSOId = soId;
            LoadOrderForView(soId);
        }
        else
        {
            EditSOId = 0;
            CurrentSOId = 0;
            PrepareNewOrder();
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Customer Filter (list view)
    // ─────────────────────────────────────────────────────
    private void LoadCustomerFilter()
    {
        ddlFilterCustomer.Items.Clear();
        ddlFilterCustomer.Items.Add(new ListItem("همه مشتریان", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CustomerId, CustomerName FROM Customers " +
                    "WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CustomerName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        ddlFilterCustomer.Items.Add(new ListItem(Convert.ToString(dr["CustomerName"]), id));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Load: Customers Dropdown (form)
    // ─────────────────────────────────────────────────────
    private void LoadCustomersDropdown()
    {
        ddlCustomer.Items.Clear();
        ddlCustomer.Items.Add(new ListItem("— انتخاب مشتری —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CustomerId, CustomerName, CustomerCode, CustomerType, " +
                    "       PriceListId, MarketerId, CreditLimit " +
                    "FROM Customers " +
                    "WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CustomerName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["CustomerName"]);
                        string code = Convert.ToString(dr["CustomerCode"]);
                        ddlCustomer.Items.Add(new ListItem(name + " (" + code + ")", id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری مشتریان: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Marketers Dropdown (users with role بازاریاب)
    // ─────────────────────────────────────────────────────
    private void LoadMarketersDropdown()
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
                    "WHERE u.IsActive = 1 AND u.IsDeleted = 0 AND r.RoleName = N'بازاریاب' " +
                    "ORDER BY u.FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt64(dr["UserId"]).ToString(CultureInfo.InvariantCulture);
                        string fullName = dr["FullName"] == DBNull.Value ? "" : Convert.ToString(dr["FullName"]).Trim();
                        string username = dr["Username"] == DBNull.Value ? "" : Convert.ToString(dr["Username"]);
                        string label = fullName.Length > 0 ? fullName : username;
                        if (label.Length == 0) label = "کاربر #" + id;
                        ddlMarketer.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Load: Stats
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
                    "  SUM(CASE WHEN Status IN (1,2,3) THEN 1 ELSE 0 END) AS OpenOrders, " +
                    "  SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS ShippedOrders, " +
                    "  SUM(CASE WHEN Status = 5 AND OrderDate >= DATEADD(DAY, -30, GETDATE()) THEN 1 ELSE 0 END) AS DeliveredRecent, " +
                    "  ISNULL(SUM(CASE WHEN Status IN (1,2,3) THEN Total ELSE 0 END), 0) AS OpenValue " +
                    "FROM SalesOrders WHERE IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatOpen.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["OpenOrders"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatShipped.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["ShippedOrders"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatDelivered.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["DeliveredRecent"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatOpenValue.Text = HttpUtility.HtmlEncode(
                            FormatMoneyValue(Convert.ToDecimal(dr["OpenValue"])));
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
    // Load: Orders List
    // ─────────────────────────────────────────────────────
    private void LoadOrders()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string custFilter = ddlFilterCustomer.SelectedValue;
            if (custFilter == null) custFilter = "";

            int totalCount = GetTotalCount(search, custFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, custFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptOrders.Visible = false;
                rptOrders.DataSource = null;
                rptOrders.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptOrders.Visible = true;
                rptOrders.DataSource = dt;
                rptOrders.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litListInfo.Text = string.Format("{0} سفارش",
                totalCount.ToString("N0", CultureInfo.InvariantCulture));

            litPageInfo.Text = string.Format("نمایش {0} تا {1} از {2}",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            litCurrentPage.Text = HttpUtility.HtmlEncode(ToPersianNumber(CurrentPage.ToString(CultureInfo.InvariantCulture)));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری سفارش‌ها: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string custFilter, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE so.IsDeleted = 0 ");

        if (StatusFilter.Length > 0)
        {
            int st;
            if (int.TryParse(StatusFilter, out st))
            {
                sb.Append(" AND so.Status = @Status ");
                cmd.Parameters.AddWithValue("@Status", st);
            }
        }

        if (ChannelFilter.Length > 0)
        {
            int ch;
            if (int.TryParse(ChannelFilter, out ch))
            {
                sb.Append(" AND so.Channel = @Channel ");
                cmd.Parameters.AddWithValue("@Channel", ch);
            }
        }

        if (custFilter.Length > 0)
        {
            int c;
            if (int.TryParse(custFilter, out c))
            {
                sb.Append(" AND so.CustomerId = @CustomerId ");
                cmd.Parameters.AddWithValue("@CustomerId", c);
            }
        }

        if (search.Length > 0)
        {
            sb.Append(" AND (so.OrderNumber LIKE @Search OR c.CustomerName LIKE @Search " +
                      " OR so.Notes LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string custFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, custFilter, cmd);
                cmd.CommandText =
                    "SELECT COUNT(*) FROM SalesOrders so " +
                    "INNER JOIN Customers c ON c.CustomerId = so.CustomerId " + filter;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string custFilter, int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, custFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY so.SalesOrderId DESC) AS RowNum, " +
                    "    so.SalesOrderId, so.OrderNumber, so.CustomerId, " +
                    "    so.OrderType, so.Channel, so.MarketerId, " +
                    "    so.OrderDate, so.Status, " +
                    "    so.Subtotal, so.Discount, so.Tax, so.Total, " +
                    "    so.DeliveryDate, so.CreatedAt, " +
                    "    c.CustomerName, c.CustomerCode, " +
                    "    ISNULL((SELECT COUNT(*) FROM SalesOrderItems soi " +
                    "            WHERE soi.SalesOrderId = so.SalesOrderId), 0) AS ItemCount " +
                    "  FROM SalesOrders so " +
                    "  INNER JOIN Customers c ON c.CustomerId = so.CustomerId " +
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
    // Filter Events
    // ─────────────────────────────────────────────────────
    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadOrders();
    }

    protected void ddlFilterCustomer_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadOrders();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        ddlFilterCustomer.ClearSelection();
        StatusFilter = "";
        ChannelFilter = "";
        CurrentPage = 1;
        LoadStats();
        LoadOrders();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadOrders(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadOrders();
    }

    // ─────────────────────────────────────────────────────
    // Form: New
    // ─────────────────────────────────────────────────────
    private void PrepareNewOrder()
    {
        litFormMode.Text = "سفارش فروش جدید";
        litFormNumber.Text = "— (پیش‌نویس)";
        litFormStatusBadge.Text =
            "<span class=\"badge bg-brand-50 text-brand-700\"><i class=\"h-3 w-3\" data-lucide=\"edit\"></i> پیش‌نویس</span>";

        if (ddlCustomer.Items.Count > 0)
        {
            ddlCustomer.ClearSelection();
            ddlCustomer.SelectedIndex = 0;
        }

        ddlOrderType.ClearSelection();
        ddlChannel.ClearSelection();

        if (ddlMarketer.Items.Count > 0)
        {
            ddlMarketer.ClearSelection();
            ddlMarketer.SelectedIndex = 0;
        }

        txtDeliveryDate.Text = "";
        txtDeliveryAddress.Text = "";
        txtNotes.Text = "";

        litCustomerInfo.Text = "";

        pnlItemForm.Visible = false;
        pnlCompletedInfo.Visible = false;
        pnlOrderHeader.Visible = true;
        phSaveActions.Visible = true;
        phAddItem.Visible = true;

        // Enable fields
        ddlCustomer.Enabled = true;
        ddlOrderType.Enabled = true;
        ddlChannel.Enabled = true;
        ddlMarketer.Enabled = true;
        txtDeliveryDate.Enabled = true;
        txtDeliveryAddress.Enabled = true;
        txtNotes.Enabled = true;

        LoadProductsDropdown();
        LoadItems();
        UpdateTotals();
    }

    // ─────────────────────────────────────────────────────
    // Customer Selected → auto-fill OrderType, Marketer, PriceList
    // ─────────────────────────────────────────────────────
    protected void ddlCustomer_SelectedIndexChanged(object sender, EventArgs e)
    {
        int customerId;
        if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
            || !int.TryParse(ddlCustomer.SelectedValue, out customerId)
            || customerId <= 0)
        {
            litCustomerInfo.Text = "";
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CustomerType, MarketerId, PriceListId, CreditLimit, City " +
                    "FROM Customers WHERE CustomerId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int custType = dr["CustomerType"] == DBNull.Value ? 1 : Convert.ToInt32(dr["CustomerType"]);
                            string city = dr["City"] == DBNull.Value ? "" : Convert.ToString(dr["City"]);
                            decimal credit = dr["CreditLimit"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CreditLimit"]);

                            // Auto-set OrderType
                            ddlOrderType.ClearSelection();
                            ListItem ot = ddlOrderType.Items.FindByValue(custType.ToString(CultureInfo.InvariantCulture));
                            if (ot != null) ot.Selected = true;

                            // Auto-set Marketer
                            if (dr["MarketerId"] != DBNull.Value)
                            {
                                string mid = Convert.ToInt64(dr["MarketerId"]).ToString(CultureInfo.InvariantCulture);
                                ddlMarketer.ClearSelection();
                                ListItem mi = ddlMarketer.Items.FindByValue(mid);
                                if (mi != null) mi.Selected = true;
                            }

                            // Info text
                            StringBuilder sb = new StringBuilder();
                            sb.Append("نوع: ").Append(GetCustomerTypeLabel(custType));
                            if (city.Length > 0) sb.Append(" — شهر: ").Append(city);
                            if (credit > 0) sb.Append(" — سقف اعتبار: ").Append(FormatMoneyValue(credit)).Append(" تومان");
                            litCustomerInfo.Text = HttpUtility.HtmlEncode(sb.ToString());
                        }
                    }
                }
            }
        }
        catch { }
    }

    private string GetCustomerTypeLabel(int type)
    {
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        return "نمایندگی";
    }

    // ─────────────────────────────────────────────────────
    // Channel Changed → Show/hide Marketer
    // ─────────────────────────────────────────────────────
    protected void ddlChannel_SelectedIndexChanged(object sender, EventArgs e)
    {
        int channel = 3;
        int.TryParse(ddlChannel.SelectedValue, out channel);
        pnlMarketer.Visible = (channel == 1);
    }

    // ─────────────────────────────────────────────────────
    // Load: Products Dropdown (only FG)
    // ─────────────────────────────────────────────────────
    private void LoadProductsDropdown()
    {
        ddlItemProduct.Items.Clear();
        ddlItemProduct.Items.Add(new ListItem("— انتخاب محصول —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                // فقط محصولات نهایی (FG)
                string sql =
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, u.UnitName " +
                    "FROM Products p " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.ProductType = 2 " +
                    "ORDER BY p.ProductName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["ProductCode"]);
                        string name = Convert.ToString(dr["ProductName"]);
                        string unit = dr["UnitName"] == DBNull.Value ? "" : Convert.ToString(dr["UnitName"]);

                        string label = name + " (" + code + ")";
                        if (unit.Length > 0) label += " — " + unit;

                        ddlItemProduct.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری محصولات: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Product Selected → Auto-load price from customer's price list
    // ─────────────────────────────────────────────────────
    protected void ddlItemProduct_SelectedIndexChanged(object sender, EventArgs e)
    {
        litPriceInfo.Text = "";

        int productId;
        if (string.IsNullOrEmpty(ddlItemProduct.SelectedValue)
            || !int.TryParse(ddlItemProduct.SelectedValue, out productId)
            || productId <= 0)
        {
            return;
        }

        // Find customer's price list
        int customerId = 0;
        if (!string.IsNullOrEmpty(ddlCustomer.SelectedValue))
        {
            int.TryParse(ddlCustomer.SelectedValue, out customerId);
        }

        // Find current quantity
        decimal minQty = 1m;
        if (txtItemQuantity.Text.Trim().Length > 0)
        {
            decimal? q = ParseDecimal(txtItemQuantity.Text);
            if (q.HasValue && q.Value > 0) minQty = q.Value;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int priceListId = 0;

                // 1) Try customer's default price list
                if (customerId > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT PriceListId FROM Customers WHERE CustomerId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", customerId);
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            priceListId = Convert.ToInt32(o);
                    }
                }

                // 2) If no customer or no price list, use default
                if (priceListId <= 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 PriceListId FROM PriceLists " +
                        "WHERE IsActive = 1 ORDER BY IsDefault DESC, PriceListId", conn))
                    {
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            priceListId = Convert.ToInt32(o);
                    }
                }

                if (priceListId <= 0)
                {
                    litPriceInfo.Text = "لیست قیمت مناسبی یافت نشد — قیمت را دستی وارد کنید.";
                    return;
                }

                // 3) Find best price (based on MinQuantity ≤ requested)
                string sql =
                    "SELECT TOP 1 UnitPrice, DiscountPercent, MinQuantity " +
                    "FROM PriceListItems " +
                    "WHERE PriceListId = @PriceListId AND ProductId = @ProductId " +
                    "  AND IsActive = 1 AND MinQuantity <= @MinQty " +
                    "ORDER BY MinQuantity DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PriceListId", priceListId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@MinQty", minQty);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            decimal unitPrice = dr["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["UnitPrice"]);
                            decimal discountPct = dr["DiscountPercent"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["DiscountPercent"]);
                            decimal minQ = dr["MinQuantity"] == DBNull.Value ? 1m : Convert.ToDecimal(dr["MinQuantity"]);

                            // Auto-fill unit price
                            txtItemUnitPrice.Text = unitPrice.ToString(CultureInfo.InvariantCulture);

                            // Discount amount based on percent
                            if (discountPct > 0 && minQty > 0)
                            {
                                decimal discountAmount = (unitPrice * minQty) * (discountPct / 100m);
                                txtItemDiscount.Text = discountAmount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                            }

                            string priceInfo = "لیست: " + priceListId.ToString(CultureInfo.InvariantCulture) +
                                " — از " + minQ.ToString("N0", CultureInfo.InvariantCulture) + " عدد";
                            if (discountPct > 0)
                                priceInfo += " — تخفیف: ٪" + discountPct.ToString("N0", CultureInfo.InvariantCulture);

                            litPriceInfo.Text = HttpUtility.HtmlEncode(priceInfo);
                        }
                        else
                        {
                            litPriceInfo.Text = "قیمتی برای این محصول در لیست مشتری تعریف نشده — دستی وارد کنید.";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری قیمت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Items
    // ─────────────────────────────────────────────────────
    private void LoadItems()
    {
        try
        {
            DataTable dt = new DataTable();

            if (CurrentSOId > 0)
            {
                using (SqlConnection conn = new SqlConnection(ConnStr))
                {
                    conn.Open();

                    string sql =
                        "SELECT soi.SalesOrderItemId AS ItemId, soi.ProductId, " +
                        "       soi.Quantity, soi.UnitPrice, soi.Discount, soi.Tax, soi.Total, " +
                        "       p.ProductCode, p.ProductName, " +
                        "       u.UnitName " +
                        "FROM SalesOrderItems soi " +
                        "INNER JOIN Products p ON p.ProductId = soi.ProductId " +
                        "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                        "WHERE soi.SalesOrderId = @Id " +
                        "ORDER BY soi.SalesOrderItemId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentSOId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlItemsEmpty.Visible = true;
                rptItems.Visible = false;
                rptItems.DataSource = null;
                rptItems.DataBind();
            }
            else
            {
                pnlItemsEmpty.Visible = false;
                rptItems.Visible = true;
                rptItems.DataSource = dt;
                rptItems.DataBind();
            }

            UpdateTotals();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری اقلام: " + ex.Message);
        }
    }

    private void UpdateTotals()
    {
        try
        {
            decimal subtotal = 0m, discountTotal = 0m, taxTotal = 0m, grand = 0m;

            if (CurrentSOId > 0)
            {
                using (SqlConnection conn = new SqlConnection(ConnStr))
                {
                    conn.Open();
                    string sql =
                        "SELECT " +
                        "  ISNULL(SUM(Quantity * UnitPrice), 0) AS Subtotal, " +
                        "  ISNULL(SUM(Discount), 0) AS DiscountTotal, " +
                        "  ISNULL(SUM(Tax), 0) AS TaxTotal, " +
                        "  ISNULL(SUM(Total), 0) AS GrandTotal " +
                        "FROM SalesOrderItems WHERE SalesOrderId = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentSOId);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                subtotal = dr["Subtotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Subtotal"]);
                                discountTotal = dr["DiscountTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["DiscountTotal"]);
                                taxTotal = dr["TaxTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TaxTotal"]);
                                grand = dr["GrandTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["GrandTotal"]);
                            }
                        }
                    }
                }
            }

            litSubtotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(subtotal));
            litDiscountTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(discountTotal));
            litTaxTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(taxTotal));
            litGrandTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(grand));
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Add/Edit Item
    // ─────────────────────────────────────────────────────
    protected void btnAddItem_Click(object sender, EventArgs e)
    {
        ClearItemForm();
        EditItemId = 0;
        litItemFormTitle.Text = "افزودن قلم جدید";
        pnlItemForm.Visible = true;
    }

    protected void btnCancelItemForm_Click(object sender, EventArgs e)
    {
        ClearItemForm();
        EditItemId = 0;
        pnlItemForm.Visible = false;
    }

    protected void btnSaveItem_Click(object sender, EventArgs e)
    {
        int productId;
        if (string.IsNullOrEmpty(ddlItemProduct.SelectedValue)
            || !int.TryParse(ddlItemProduct.SelectedValue, out productId)
            || productId <= 0)
        {
            ShowMessage("محصول را انتخاب کنید.");
            return;
        }

        decimal? qty = ParseDecimal(txtItemQuantity.Text);
        if (!qty.HasValue || qty.Value <= 0)
        {
            ShowMessage("مقدار باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        decimal? unitPrice = ParseDecimal(txtItemUnitPrice.Text);
        if (!unitPrice.HasValue || unitPrice.Value < 0)
        {
            ShowMessage("قیمت واحد را وارد کنید.");
            return;
        }

        decimal discount = 0m;
        if (txtItemDiscount.Text.Trim().Length > 0)
        {
            decimal? d = ParseDecimal(txtItemDiscount.Text);
            if (!d.HasValue || d.Value < 0)
            {
                ShowMessage("تخفیف معتبر نیست.");
                return;
            }
            discount = d.Value;
        }

        decimal tax = 0m;
        if (txtItemTax.Text.Trim().Length > 0)
        {
            decimal? t = ParseDecimal(txtItemTax.Text);
            if (!t.HasValue || t.Value < 0)
            {
                ShowMessage("مالیات معتبر نیست.");
                return;
            }
            tax = t.Value;
        }

        decimal lineTotal = (qty.Value * unitPrice.Value) - discount + tax;
        if (lineTotal < 0) lineTotal = 0;

        try
        {
            // اگر سفارش جدید است، ابتدا یک سفارش پیش‌نویس ایجاد می‌کنیم
            if (CurrentSOId <= 0)
            {
                int newId = CreateDraftOrder();
                if (newId <= 0)
                {
                    ShowMessage("خطا در ایجاد سفارش.");
                    return;
                }
                CurrentSOId = newId;
                EditSOId = newId;

                // Reload form
                LoadOrderForView(newId);
                pnlItemForm.Visible = true;
                EditItemId = 0;
                litItemFormTitle.Text = "افزودن قلم جدید";

                // Restore field values
                ddlItemProduct.ClearSelection();
                ListItem pit = ddlItemProduct.Items.FindByValue(productId.ToString(CultureInfo.InvariantCulture));
                if (pit != null) pit.Selected = true;
                txtItemQuantity.Text = qty.Value.ToString(CultureInfo.InvariantCulture);
                txtItemUnitPrice.Text = unitPrice.Value.ToString(CultureInfo.InvariantCulture);
                txtItemDiscount.Text = discount.ToString(CultureInfo.InvariantCulture);
                txtItemTax.Text = tax.ToString(CultureInfo.InvariantCulture);
            }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (EditItemId > 0)
                {
                    string updateSql =
                        "UPDATE SalesOrderItems SET " +
                        "  ProductId = @ProductId, Quantity = @Qty, UnitPrice = @UnitPrice, " +
                        "  Discount = @Discount, Tax = @Tax, Total = @Total " +
                        "WHERE SalesOrderItemId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Qty", qty.Value);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice.Value);
                        cmd.Parameters.AddWithValue("@Discount", discount);
                        cmd.Parameters.AddWithValue("@Tax", tax);
                        cmd.Parameters.AddWithValue("@Total", lineTotal);
                        cmd.Parameters.AddWithValue("@Id", EditItemId);
                        cmd.ExecuteNonQuery();
                    }

                    ShowMessage("قلم با موفقیت ویرایش شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO SalesOrderItems " +
                        "  (SalesOrderId, ProductId, Quantity, UnitPrice, Discount, Tax, Total) " +
                        "VALUES (@SOId, @ProductId, @Qty, @UnitPrice, @Discount, @Tax, @Total)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@SOId", CurrentSOId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Qty", qty.Value);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice.Value);
                        cmd.Parameters.AddWithValue("@Discount", discount);
                        cmd.Parameters.AddWithValue("@Tax", tax);
                        cmd.Parameters.AddWithValue("@Total", lineTotal);
                        cmd.ExecuteNonQuery();
                    }

                    ShowMessage("قلم با موفقیت اضافه شد.");
                }

                RecalculateOrderTotals(conn, CurrentSOId);
            }

            ClearItemForm();
            EditItemId = 0;
            pnlItemForm.Visible = false;
            LoadItems();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره قلم: " + ex.Message);
        }
    }

    private int CreateDraftOrder()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                return InsertNewOrderHeader(conn, 1);
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ایجاد سفارش: " + ex.Message);
            return 0;
        }
    }

    private int InsertNewOrderHeader(SqlConnection conn, int status)
    {
        string orderNumber = GenerateOrderNumber(conn);

        int customerId = 0;
        if (!string.IsNullOrEmpty(ddlCustomer.SelectedValue))
        {
            int.TryParse(ddlCustomer.SelectedValue, out customerId);
        }

        int orderType = 1;
        if (!int.TryParse(ddlOrderType.SelectedValue, out orderType)) orderType = 1;

        int channel = 3;
        if (!int.TryParse(ddlChannel.SelectedValue, out channel)) channel = 3;

        int? marketerId = null;
        if (channel == 1 && !string.IsNullOrEmpty(ddlMarketer.SelectedValue))
        {
            int mid;
            if (int.TryParse(ddlMarketer.SelectedValue, out mid) && mid > 0)
                marketerId = mid;
        }

        DateTime? deliveryDate = null;
        if (txtDeliveryDate.Text.Trim().Length > 0)
        {
            DateTime? d = ParsePersianDate(txtDeliveryDate.Text.Trim());
            if (d.HasValue) deliveryDate = d.Value;
        }

        string deliveryAddress = txtDeliveryAddress.Text.Trim();
        string notes = txtNotes.Text.Trim();

        string insertSql =
            "INSERT INTO SalesOrders " +
            "  (OrderNumber, CustomerId, OrderType, Channel, MarketerId, " +
            "   OrderDate, Status, Subtotal, Discount, Tax, Total, " +
            "   DeliveryAddress, DeliveryDate, Notes, " +
            "   CreatedAt, CreatedBy, IsDeleted) " +
            "VALUES (@Number, @CustomerId, @OrderType, @Channel, @MarketerId, " +
            "   GETDATE(), @Status, 0, 0, 0, 0, " +
            "   @DeliveryAddress, @DeliveryDate, @Notes, " +
            "   GETDATE(), @CreatedBy, 0); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);";

        int newId = 0;
        using (SqlCommand cmd = new SqlCommand(insertSql, conn))
        {
            cmd.Parameters.AddWithValue("@Number", orderNumber);
            cmd.Parameters.AddWithValue("@CustomerId", customerId > 0 ? (object)customerId : DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderType", orderType);
            cmd.Parameters.AddWithValue("@Channel", channel);
            cmd.Parameters.AddWithValue("@MarketerId", marketerId.HasValue ? (object)marketerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@DeliveryAddress",
                deliveryAddress.Length > 0 ? (object)deliveryAddress : DBNull.Value);
            cmd.Parameters.AddWithValue("@DeliveryDate",
                deliveryDate.HasValue ? (object)deliveryDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes",
                notes.Length > 0 ? (object)notes : DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy",
                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

            object r = cmd.ExecuteScalar();
            if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
        }

        return newId;
    }

    private string GenerateOrderNumber(SqlConnection conn)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "SO-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        string sql =
            "SELECT ISNULL(MAX(CAST(SUBSTRING(OrderNumber, LEN(@Prefix) + 1, 20) AS INT)), 0) + 1 " +
            "FROM SalesOrders WHERE OrderNumber LIKE @Prefix + '%'";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + next.ToString("D4", CultureInfo.InvariantCulture);
        }
    }

    private void RecalculateOrderTotals(SqlConnection conn, int soId)
    {
        string sql =
            "UPDATE SalesOrders SET " +
            "  Subtotal = ISNULL((SELECT SUM(Quantity * UnitPrice) FROM SalesOrderItems " +
            "                     WHERE SalesOrderId = @Id), 0), " +
            "  Discount = ISNULL((SELECT SUM(Discount) FROM SalesOrderItems " +
            "                     WHERE SalesOrderId = @Id), 0), " +
            "  Tax = ISNULL((SELECT SUM(Tax) FROM SalesOrderItems " +
            "                WHERE SalesOrderId = @Id), 0), " +
            "  Total = ISNULL((SELECT SUM(Total) FROM SalesOrderItems " +
            "                  WHERE SalesOrderId = @Id), 0) " +
            "WHERE SalesOrderId = @Id";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Id", soId);
            cmd.ExecuteNonQuery();
        }
    }

    protected void rptItems_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0)
            return;

        if (e.CommandName == "EditItem") LoadItemForEdit(itemId);
        else if (e.CommandName == "DeleteItem") DeleteItem(itemId);
    }

    private void LoadItemForEdit(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT SalesOrderItemId, ProductId, Quantity, UnitPrice, Discount, Tax " +
                    "FROM SalesOrderItems WHERE SalesOrderItemId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditItemId = itemId;

                            ddlItemProduct.ClearSelection();
                            if (dr["ProductId"] != DBNull.Value)
                            {
                                string pid = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem it = ddlItemProduct.Items.FindByValue(pid);
                                if (it != null) it.Selected = true;
                            }

                            txtItemQuantity.Text = dr["Quantity"] == DBNull.Value ? "" :
                                Convert.ToDecimal(dr["Quantity"]).ToString(CultureInfo.InvariantCulture);
                            txtItemUnitPrice.Text = dr["UnitPrice"] == DBNull.Value ? "" :
                                Convert.ToDecimal(dr["UnitPrice"]).ToString(CultureInfo.InvariantCulture);
                            txtItemDiscount.Text = dr["Discount"] == DBNull.Value ? "0" :
                                Convert.ToDecimal(dr["Discount"]).ToString(CultureInfo.InvariantCulture);
                            txtItemTax.Text = dr["Tax"] == DBNull.Value ? "0" :
                                Convert.ToDecimal(dr["Tax"]).ToString(CultureInfo.InvariantCulture);

                            litItemFormTitle.Text = "ویرایش قلم";
                            litPriceInfo.Text = "";
                            pnlItemForm.Visible = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری قلم: " + ex.Message);
        }
    }

    private void DeleteItem(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM SalesOrderItems WHERE SalesOrderItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }
                RecalculateOrderTotals(conn, CurrentSOId);
            }

            ShowMessage("قلم حذف شد.");
            LoadItems();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف قلم: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Save Draft / Save + Approve
    // ─────────────────────────────────────────────────────
    protected void btnSaveDraft_Click(object sender, EventArgs e)
    {
        SaveOrder(1);
    }

    protected void btnSaveAndApprove_Click(object sender, EventArgs e)
    {
        SaveOrder(2);
    }

    private void SaveOrder(int targetStatus)
    {
        // ── اعتبارسنجی ──
        int customerId = 0;
        if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
            || !int.TryParse(ddlCustomer.SelectedValue, out customerId)
            || customerId <= 0)
        {
            ShowMessage("مشتری را انتخاب کنید.");
            return;
        }

        if (CurrentSOId <= 0)
        {
            ShowMessage("ابتدا حداقل یک قلم به سفارش اضافه کنید.");
            return;
        }

        int itemCount = 0;
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM SalesOrderItems WHERE SalesOrderId = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", CurrentSOId);
                object o = cmd.ExecuteScalar();
                itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }

        if (itemCount == 0)
        {
            ShowMessage("ابتدا حداقل یک قلم به سفارش اضافه کنید.");
            return;
        }

        int orderType = 1;
        int.TryParse(ddlOrderType.SelectedValue, out orderType);

        int channel = 3;
        int.TryParse(ddlChannel.SelectedValue, out channel);

        int? marketerId = null;
        if (channel == 1 && !string.IsNullOrEmpty(ddlMarketer.SelectedValue))
        {
            int mid;
            if (int.TryParse(ddlMarketer.SelectedValue, out mid) && mid > 0)
                marketerId = mid;
        }

        DateTime? deliveryDate = null;
        if (txtDeliveryDate.Text.Trim().Length > 0)
        {
            DateTime? d = ParsePersianDate(txtDeliveryDate.Text.Trim());
            if (!d.HasValue)
            {
                ShowMessage("تاریخ تحویل معتبر نیست.");
                return;
            }
            deliveryDate = d.Value;
        }

        string deliveryAddress = txtDeliveryAddress.Text.Trim();
        string notes = txtNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string updateSql =
                    "UPDATE SalesOrders SET " +
                    "  CustomerId = @CustomerId, OrderType = @OrderType, Channel = @Channel, " +
                    "  MarketerId = @MarketerId, " +
                    "  DeliveryAddress = @DeliveryAddress, DeliveryDate = @DeliveryDate, " +
                    "  Notes = @Notes, Status = @Status, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE SalesOrderId = @Id";

                using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@OrderType", orderType);
                    cmd.Parameters.AddWithValue("@Channel", channel);
                    cmd.Parameters.AddWithValue("@MarketerId",
                        marketerId.HasValue ? (object)marketerId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeliveryAddress",
                        deliveryAddress.Length > 0 ? (object)deliveryAddress : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeliveryDate",
                        deliveryDate.HasValue ? (object)deliveryDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes",
                        notes.Length > 0 ? (object)notes : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", targetStatus);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", CurrentSOId);
                    cmd.ExecuteNonQuery();
                }

                RecalculateOrderTotals(conn, CurrentSOId);

                string num = GetOrderNumber(conn, CurrentSOId);
                WriteAudit(conn, "Update", "SalesOrders", CurrentSOId, num,
                    targetStatus == 2 ? "تأیید سفارش فروش" : "ذخیره پیش‌نویس فروش");
            }

            ShowMessage(targetStatus == 2
                ? "سفارش تأیید شد. برای صدور پیش‌فاکتور اقدام کنید."
                : "پیش‌نویس سفارش ذخیره شد.");

            ShowListView();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Order for View
    // ─────────────────────────────────────────────────────
    private void LoadOrderForView(int soId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT so.SalesOrderId, so.OrderNumber, so.CustomerId, " +
                    "       so.OrderType, so.Channel, so.MarketerId, so.Status, " +
                    "       so.DeliveryAddress, so.DeliveryDate, so.Notes, " +
                    "       so.Subtotal, so.Discount, so.Tax, so.Total, " +
                    "       so.CreatedAt, " +
                    "       (SELECT TOP 1 FinalInvoiceId FROM FinalInvoices fi " +
                    "        WHERE fi.SalesOrderId = so.SalesOrderId) AS FinalInvoiceId " +
                    "FROM SalesOrders so WHERE so.SalesOrderId = @Id AND so.IsDeleted = 0";

                int status = 0;

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowMessage("سفارش یافت نشد.");
                            ShowListView();
                            return;
                        }

                        CurrentSOId = soId;
                        EditSOId = soId;
                        status = Convert.ToInt32(dr["Status"]);

                        litFormMode.Text = "سفارش فروش — " + GetStatusLabel(status);
                        litFormNumber.Text = HttpUtility.HtmlEncode(Convert.ToString(dr["OrderNumber"]));
                        litFormStatusBadge.Text =
                            "<span class=\"" + GetStatusBadgeCss(status) + "\">" +
                            HttpUtility.HtmlEncode(GetStatusLabel(status)) + "</span>";

                        // Customer
                        if (dr["CustomerId"] != DBNull.Value)
                        {
                            string cid = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                            ddlCustomer.ClearSelection();
                            ListItem it = ddlCustomer.Items.FindByValue(cid);
                            if (it != null) it.Selected = true;
                        }

                        // Order Type
                        if (dr["OrderType"] != DBNull.Value)
                        {
                            ddlOrderType.ClearSelection();
                            ListItem ot = ddlOrderType.Items.FindByValue(Convert.ToInt32(dr["OrderType"]).ToString(CultureInfo.InvariantCulture));
                            if (ot != null) ot.Selected = true;
                        }

                        // Channel
                        int channel = 3;
                        if (dr["Channel"] != DBNull.Value)
                        {
                            channel = Convert.ToInt32(dr["Channel"]);
                            ddlChannel.ClearSelection();
                            ListItem ch = ddlChannel.Items.FindByValue(channel.ToString(CultureInfo.InvariantCulture));
                            if (ch != null) ch.Selected = true;
                        }
                        pnlMarketer.Visible = (channel == 1);

                        // Marketer
                        if (dr["MarketerId"] != DBNull.Value)
                        {
                            string mid = Convert.ToInt64(dr["MarketerId"]).ToString(CultureInfo.InvariantCulture);
                            ddlMarketer.ClearSelection();
                            ListItem mi = ddlMarketer.Items.FindByValue(mid);
                            if (mi != null) mi.Selected = true;
                        }

                        // Delivery
                        txtDeliveryDate.Text = dr["DeliveryDate"] == DBNull.Value
                            ? "" : ToShamsiDateString(Convert.ToDateTime(dr["DeliveryDate"]));
                        txtDeliveryAddress.Text = dr["DeliveryAddress"] == DBNull.Value
                            ? "" : Convert.ToString(dr["DeliveryAddress"]);
                        txtNotes.Text = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);

                        // Fill customer info
                        ddlCustomer_SelectedIndexChanged(null, null);

                        // Completed Info
                        if (status == 4 || status == 5)
                        {
                            litCompletedStatusLabel.Text = HttpUtility.HtmlEncode(
                                status == 4 ? "سفارش ارسال شد" : "سفارش تحویل مشتری شد");
                            litCompletedDeliveryDate.Text = dr["DeliveryDate"] == DBNull.Value
                                ? "—" : HttpUtility.HtmlEncode(FormatShamsiShort(Convert.ToDateTime(dr["DeliveryDate"])));

                            if (dr["FinalInvoiceId"] != DBNull.Value)
                            {
                                string fid = Convert.ToInt32(dr["FinalInvoiceId"]).ToString(CultureInfo.InvariantCulture);
                                litCompletedProformaLink.Text =
                                    "<a href=\"final-invoices.aspx?id=" + HttpUtility.HtmlAttributeEncode(fid) +
                                    "\" class=\"text-brand-600 font-bold hover:underline\">مشاهده فاکتور</a>";
                            }
                            else
                            {
                                litCompletedProformaLink.Text = "—";
                            }

                            pnlCompletedInfo.Visible = true;
                        }
                        else
                        {
                            pnlCompletedInfo.Visible = false;
                        }
                    }
                }

                // ── تنظیم نمایش بر اساس وضعیت ──
                bool isDraft = (status == 1);
                bool isFinal = (status == 4 || status == 5 || status == 6);

                ddlCustomer.Enabled = isDraft;
                ddlOrderType.Enabled = isDraft;
                ddlChannel.Enabled = isDraft;
                ddlMarketer.Enabled = isDraft;
                txtDeliveryDate.Enabled = isDraft;
                txtDeliveryAddress.Enabled = isDraft;
                txtNotes.Enabled = isDraft;

                phAddItem.Visible = isDraft;
                phSaveActions.Visible = isDraft;

                if (!isDraft) pnlItemForm.Visible = false;

                LoadProductsDropdown();
                LoadItems();
                UpdateTotals();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // List Actions
    // ─────────────────────────────────────────────────────
    protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int soId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out soId) || soId <= 0)
            return;

        if (e.CommandName == "Edit") ShowFormView(soId);
        else if (e.CommandName == "Approve") ApproveOrder(soId);
        else if (e.CommandName == "Ship") ShipOrder(soId);
        else if (e.CommandName == "Deliver") DeliverOrder(soId);
        else if (e.CommandName == "Cancel") CancelOrder(soId);
    }

    private void ApproveOrder(int soId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, soId);
                if (status != 1)
                {
                    ShowMessage("فقط سفارش‌های پیش‌نویس قابل تأیید هستند.");
                    return;
                }

                // Check items
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM SalesOrderItems WHERE SalesOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    object o = cmd.ExecuteScalar();
                    if ((o == null || o == DBNull.Value) || Convert.ToInt32(o) == 0)
                    {
                        ShowMessage("این سفارش هیچ قلمی ندارد.");
                        return;
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE SalesOrders SET Status = 2, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE SalesOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, soId);
                WriteAudit(conn, "Approve", "SalesOrders", soId, num, "تأیید سفارش فروش");
            }

            ShowMessage("سفارش تأیید شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تأیید: " + ex.Message);
        }
    }

    private void ShipOrder(int soId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, soId);
                if (status != 3)
                {
                    ShowMessage("فقط سفارش‌های «آماده‌سازی» قابل ارسال هستند. ابتدا پیش‌فاکتور تبدیل به فاکتور نهایی شود.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE SalesOrders SET Status = 4, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE SalesOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, soId);
                WriteAudit(conn, "Ship", "SalesOrders", soId, num, "ارسال سفارش");
            }

            ShowMessage("سفارش ارسال شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ارسال: " + ex.Message);
        }
    }

    private void DeliverOrder(int soId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, soId);
                if (status != 4)
                {
                    ShowMessage("فقط سفارش‌های «ارسال‌شده» قابل تحویل هستند.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE SalesOrders SET Status = 5, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE SalesOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, soId);
                WriteAudit(conn, "Deliver", "SalesOrders", soId, num, "تحویل سفارش به مشتری");
            }

            ShowMessage("تحویل ثبت شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت تحویل: " + ex.Message);
        }
    }

    private void CancelOrder(int soId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, soId);
                if (status == 5)
                {
                    ShowMessage("سفارش تحویل‌شده قابل لغو نیست.");
                    return;
                }
                if (status == 6)
                {
                    ShowMessage("این سفارش قبلاً لغو شده است.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE SalesOrders SET Status = 6, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE SalesOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", soId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, soId);
                WriteAudit(conn, "Cancel", "SalesOrders", soId, num, "لغو سفارش فروش");
            }

            ShowMessage("سفارش لغو شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در لغو: " + ex.Message);
        }
    }

    private int GetOrderStatus(SqlConnection conn, int soId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT Status FROM SalesOrders WHERE SalesOrderId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", soId);
            object o = cmd.ExecuteScalar();
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }
    }

    private string GetOrderNumber(SqlConnection conn, int soId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT OrderNumber FROM SalesOrders WHERE SalesOrderId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", soId);
            object o = cmd.ExecuteScalar();
            return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o);
        }
    }

    // ─────────────────────────────────────────────────────
    // Navigation
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        ShowFormView(0);
    }

    protected void btnCancelForm_Click(object sender, EventArgs e)
    {
        ShowListView();
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        CurrentSOId = 0;
        ShowListView();
    }

    // ─────────────────────────────────────────────────────
    // AuditLog
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
    // Markup Helpers
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

    protected bool IsDraft(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 1;
    }

    protected bool IsApproved(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 2;
    }

    protected bool IsReady(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 3;
    }

    protected bool IsShipped(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 4;
    }

    protected bool CanCancel(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2 || s == 3 || s == 4;
    }

    protected bool IsEditableItem()
    {
        return phSaveActions.Visible;
    }

    protected string GetStatusLabel(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "پیش‌نویس";
        if (s == 2) return "تأییدشده";
        if (s == 3) return "آماده‌سازی";
        if (s == 4) return "ارسال‌شده";
        if (s == 5) return "تحویل‌شده";
        if (s == 6) return "لغوشده";
        return "—";
    }

    protected string GetStatusIcon(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "edit";
        if (s == 2) return "check";
        if (s == 3) return "package";
        if (s == 4) return "truck";
        if (s == 5) return "check-circle-2";
        if (s == 6) return "x-circle";
        return "file";
    }

    protected string GetStatusBadgeCss(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "badge bg-brand-50 text-brand-700";
        if (s == 2) return "badge bg-sky-50 text-sky-700";
        if (s == 3) return "badge bg-amber-50 text-amber-700";
        if (s == 4) return "badge bg-violet-50 text-violet-700";
        if (s == 5) return "badge bg-emerald-50 text-emerald-700";
        if (s == 6) return "badge bg-rose-50 text-rose-700";
        return "badge bg-surface-muted text-ink-500";
    }

    protected string GetStatusAvatarCss(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        string baseC = "grid h-9 w-9 shrink-0 place-items-center rounded-lg";
        if (s == 1) return baseC + " bg-brand-100 text-brand-700";
        if (s == 2) return baseC + " bg-sky-100 text-sky-700";
        if (s == 3) return baseC + " bg-amber-100 text-amber-700";
        if (s == 4) return baseC + " bg-violet-100 text-violet-700";
        if (s == 5) return baseC + " bg-emerald-100 text-emerald-700";
        if (s == 6) return baseC + " bg-rose-100 text-rose-700";
        return baseC + " bg-surface-muted text-ink-500";
    }

    protected string GetChannelLabel(object channelObj)
    {
        int c = channelObj == null || channelObj == DBNull.Value ? 3 : Convert.ToInt32(channelObj);
        if (c == 1) return "بازاریاب";
        if (c == 2) return "آنلاین";
        return "ادمین";
    }

    protected string GetChannelIcon(object channelObj)
    {
        int c = channelObj == null || channelObj == DBNull.Value ? 3 : Convert.ToInt32(channelObj);
        if (c == 1) return "users";
        if (c == 2) return "globe";
        return "user-cog";
    }

    protected string GetChannelBadgeCss(object channelObj)
    {
        int c = channelObj == null || channelObj == DBNull.Value ? 3 : Convert.ToInt32(channelObj);
        if (c == 1) return "badge bg-brand-50 text-brand-700";
        if (c == 2) return "badge bg-sky-50 text-sky-700";
        return "badge bg-surface-muted text-ink-700";
    }

    protected string FormatQuantity(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0) return "۰";
            if (d == Math.Floor(d))
                return HttpUtility.HtmlEncode(d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
            return HttpUtility.HtmlEncode(d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،"));
        }
        catch { return "۰"; }
    }

    protected string FormatMoney(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            return HttpUtility.HtmlEncode(FormatMoneyValue(d));
        }
        catch { return "۰"; }
    }

    protected string FormatMoneyOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "—";
            return HttpUtility.HtmlEncode(FormatMoneyValue(d));
        }
        catch { return "—"; }
    }

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    protected string FormatShamsiShort(object value)
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

    private string ToPersianNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        char[] persianDigits = new char[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        StringBuilder sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c >= '0' && c <= '9') sb.Append(persianDigits[c - '0']);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────
    // Date Helpers
    // ─────────────────────────────────────────────────────
    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

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

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private decimal? ParseDecimal(string input)
    {
        if (input == null) return null;
        input = input.Trim();
        if (input.Length == 0) return null;
        input = input.Replace(",", "").Replace("،", "");
        decimal d;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    private void ClearItemForm()
    {
        if (ddlItemProduct.Items.Count > 0)
        {
            ddlItemProduct.ClearSelection();
            ddlItemProduct.SelectedIndex = 0;
        }
        txtItemQuantity.Text = "";
        txtItemUnitPrice.Text = "";
        txtItemDiscount.Text = "0";
        txtItemTax.Text = "0";
        litPriceInfo.Text = "";
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