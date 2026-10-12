using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_service_orders : System.Web.UI.Page
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

    private int ExistingMovementId
    {
        get
        {
            int id;
            if (int.TryParse(hfExistingMovementId.Value, out id)) return id;
            return 0;
        }
        set { hfExistingMovementId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            LoadServiceTypeFilter();
            LoadServiceTypesFormDropdown();
            LoadCustomersDropdown();
            LoadWarehousesDropdown();
            LoadInKindProductsDropdown();
            ShowListView();
        }
    }

    // ─────────────────────────────────────────────────────
    // View Management
    // ─────────────────────────────────────────────────────
    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlReceiveView.Visible = false;
        pnlCompleteView.Visible = false;
        phBackToList.Visible = false;
        phQuickActions.Visible = true;

        litPageTitle.Text = "سفارش‌های خدمات";
        litPageSubtitle.Text = "پذیرش سریع محصول، انجام عملیات، و تسویه نهایی در سه مرحله ساده.";

        LoadStats();
        LoadOrders();
    }

    private void ShowReceiveView(int orderId)
    {
        pnlListView.Visible = false;
        pnlReceiveView.Visible = true;
        pnlCompleteView.Visible = false;
        phBackToList.Visible = true;
        phQuickActions.Visible = false;

        if (orderId > 0)
        {
            EditId = orderId;
            litReceiveTitle.Text = "ویرایش پذیرش";
            LoadReceiveForEdit(orderId);
        }
        else
        {
            EditId = 0;
            ClearReceiveForm();
            litReceiveTitle.Text = "پذیرش سریع محصول";
        }
    }

    private void ShowCompleteView(int orderId)
    {
        pnlListView.Visible = false;
        pnlReceiveView.Visible = false;
        pnlCompleteView.Visible = true;
        phBackToList.Visible = true;
        phQuickActions.Visible = false;

        EditId = orderId;
        LoadCompleteView(orderId);
    }

    // ─────────────────────────────────────────────────────
    // Load Dropdowns
    // ─────────────────────────────────────────────────────
    private void LoadServiceTypeFilter()
    {
        ddlFilterServiceType.Items.Clear();
        ddlFilterServiceType.Items.Add(new ListItem("همه خدمات", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ServiceTypeId, ServiceName FROM ServiceTypes " +
                    "WHERE IsDeleted = 0 ORDER BY ServiceName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ServiceTypeId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["ServiceName"]);
                        ddlFilterServiceType.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadServiceTypesFormDropdown()
    {
        ddlServiceType.Items.Clear();
        ddlServiceType.Items.Add(new ListItem("— انتخاب خدمت —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT ServiceTypeId, ServiceCode, ServiceName, UnitName, DefaultUnitPrice " +
                    "FROM ServiceTypes WHERE IsDeleted = 0 AND IsActive = 1 " +
                    "ORDER BY ServiceName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ServiceTypeId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["ServiceCode"]);
                        string name = Convert.ToString(dr["ServiceName"]);
                        string unit = dr["UnitName"] == DBNull.Value ? "" : Convert.ToString(dr["UnitName"]);

                        string label = name + " (" + code + ")";
                        if (unit.Length > 0) label += " — " + unit;

                        ddlServiceType.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch { }
    }

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
                    "SELECT CustomerId, CustomerCode, CustomerName, CustomerType " +
                    "FROM Customers WHERE IsDeleted = 0 AND IsActive = 1 " +
                    "ORDER BY CustomerName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["CustomerCode"]);
                        string name = Convert.ToString(dr["CustomerName"]);
                        int type = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);

                        string typeLabel = GetCustomerTypeLabelStatic(type);
                        string label = name + " (" + code + ") — " + typeLabel;

                        ddlCustomer.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadWarehousesDropdown()
    {
        ddlInKindWarehouse.Items.Clear();
        ddlInKindWarehouse.Items.Add(new ListItem("— انتخاب انبار —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT WarehouseId, WarehouseName FROM Warehouses " +
                    "WHERE IsActive = 1 AND WarehouseType = 2 ORDER BY WarehouseName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["WarehouseId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["WarehouseName"]);
                        ddlInKindWarehouse.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadInKindProductsDropdown()
    {
        ddlInKindProduct.Items.Clear();
        ddlInKindProduct.Items.Add(new ListItem("— انتخاب محصول —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, ProductCode, ProductName FROM Products " +
                    "WHERE IsActive = 1 AND IsDeleted = 0 AND ProductType = 2 " +
                    "ORDER BY ProductName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["ProductCode"]);
                        string name = Convert.ToString(dr["ProductName"]);
                        ddlInKindProduct.Items.Add(new ListItem(name + " (" + code + ")", id));
                    }
                }
            }
        }
        catch { }
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

                int total = 0, pending = 0, completed = 0;
                decimal revenue = 0m;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders WHERE IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    total = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders WHERE Status = 1 AND IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    pending = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                DateTime monthStart = GetCurrentPersianMonthStart();
                DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders " +
                    "WHERE Status = 3 AND IsDeleted = 0 AND ServiceDate >= @From AND ServiceDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    completed = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Total), 0) FROM ServiceOrders " +
                    "WHERE Status = 3 AND IsDeleted = 0 AND ServiceDate >= @From AND ServiceDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    revenue = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                litStatTotal.Text = HttpUtility.HtmlEncode(total.ToString("N0"));
                litStatPending.Text = HttpUtility.HtmlEncode(pending.ToString("N0"));
                litStatCompleted.Text = HttpUtility.HtmlEncode(completed.ToString("N0"));
                litStatRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(revenue));
                litTilePendingCount.Text = HttpUtility.HtmlEncode(pending.ToString("N0"));
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری آمار: " + ex.Message);
        }
    }

    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
    }

    // ─────────────────────────────────────────────────────
    // Load Orders List
    // ─────────────────────────────────────────────────────
    private void LoadOrders()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT so.ServiceOrderId, so.OrderNumber, so.ServiceDate, ");
                sb.Append("  so.InputWeight, so.OutputWeight, so.WasteWeight, so.WastePercent, ");
                sb.Append("  so.UnitPrice, so.Total, so.PaymentMode, so.PaidAmount, so.PaymentStatus, ");
                sb.Append("  so.Status, so.CustomerId, c.CustomerName, c.CustomerType, ");
                sb.Append("  so.ServiceTypeId, st.ServiceName, ");
                sb.Append("  so.DeliveredBy, so.DeliveredByMobile ");
                sb.Append("FROM ServiceOrders so ");
                sb.Append("INNER JOIN Customers c ON c.CustomerId = so.CustomerId ");
                sb.Append("INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId ");
                sb.Append("WHERE so.IsDeleted = 0 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append("  AND (so.OrderNumber LIKE @Search OR c.CustomerName LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    DateTime? fromDate = ParsePersianDate(txtFilterFrom.Text.Trim());
                    if (fromDate.HasValue)
                    {
                        sb.Append("  AND so.ServiceDate >= @FromDate ");
                        cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                    }

                    DateTime? toDate = ParsePersianDate(txtFilterTo.Text.Trim());
                    if (toDate.HasValue)
                    {
                        sb.Append("  AND so.ServiceDate <= @ToDate ");
                        cmd.Parameters.AddWithValue("@ToDate", toDate.Value.AddDays(1).AddSeconds(-1));
                    }

                    string statusFilter = ddlFilterStatus.SelectedValue;
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        int st;
                        if (int.TryParse(statusFilter, out st) && st > 0)
                        {
                            sb.Append("  AND so.Status = @Status ");
                            cmd.Parameters.AddWithValue("@Status", st);
                        }
                    }

                    string svcFilter = ddlFilterServiceType.SelectedValue;
                    if (!string.IsNullOrEmpty(svcFilter))
                    {
                        int sv;
                        if (int.TryParse(svcFilter, out sv) && sv > 0)
                        {
                            sb.Append("  AND so.ServiceTypeId = @SvcId ");
                            cmd.Parameters.AddWithValue("@SvcId", sv);
                        }
                    }

                    string payFilter = ddlFilterPaymentMode.SelectedValue;
                    if (!string.IsNullOrEmpty(payFilter))
                    {
                        int pm;
                        if (int.TryParse(payFilter, out pm) && pm > 0)
                        {
                            sb.Append("  AND so.PaymentMode = @PayMode ");
                            cmd.Parameters.AddWithValue("@PayMode", pm);
                        }
                    }

                    sb.Append("ORDER BY so.Status ASC, so.ServiceDate DESC, so.ServiceOrderId DESC");

                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlListEmpty.Visible = true;
                rptOrders.Visible = false;
                rptOrders.DataSource = null;
                rptOrders.DataBind();
            }
            else
            {
                pnlListEmpty.Visible = false;
                rptOrders.Visible = true;
                rptOrders.DataSource = dt;
                rptOrders.DataBind();
            }

            litListInfo.Text = string.Format("{0} سفارش",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Filter events
    // ─────────────────────────────────────────────────────
    protected void FilterChanged(object sender, EventArgs e) { LoadOrders(); }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        txtFilterFrom.Text = "";
        txtFilterTo.Text = "";
        if (ddlFilterStatus.Items.Count > 0) ddlFilterStatus.SelectedIndex = 0;
        if (ddlFilterServiceType.Items.Count > 0) ddlFilterServiceType.SelectedIndex = 0;
        if (ddlFilterPaymentMode.Items.Count > 0) ddlFilterPaymentMode.SelectedIndex = 0;
        LoadOrders();
    }

    protected void btnTilePending_Click(object sender, EventArgs e)
    {
        ddlFilterStatus.ClearSelection();
        ListItem item = ddlFilterStatus.Items.FindByValue("1");
        if (item != null) item.Selected = true;
        LoadOrders();
    }

    protected void QuickRange_Click(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        if (btn == null) return;

        string arg = btn.CommandArgument;
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        DateTime from;
        DateTime to = now;

        if (arg == "today") from = now.Date;
        else if (arg == "week")
        {
            int dayOfWeek = (int)now.DayOfWeek;
            int daysSinceSaturday = (dayOfWeek + 1) % 7;
            from = now.Date.AddDays(-daysSinceSaturday);
        }
        else if (arg == "month")
            from = pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
        else if (arg == "30days") from = now.Date.AddDays(-29);
        else if (arg == "clear")
        {
            txtFilterFrom.Text = "";
            txtFilterTo.Text = "";
            LoadOrders();
            return;
        }
        else return;

        txtFilterFrom.Text = ToShamsiDateString(from);
        txtFilterTo.Text = ToShamsiDateString(to);
        LoadOrders();
    }

    // ─────────────────────────────────────────────────────
    // Receive View
    // ─────────────────────────────────────────────────────
    protected void btnQuickReceive_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowReceiveView(0);
    }

    protected void btnCancelReceive_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowListView();
    }

    protected void btnSaveReceiveOnly_Click(object sender, EventArgs e)
    {
        int orderId = SaveReceive(false);
        if (orderId > 0)
        {
            ShowMessage("پذیرش با موفقیت ثبت شد. شماره: " + GetOrderNumber(orderId));
            ShowListView();
        }
    }

    protected void btnSaveReceiveAndComplete_Click(object sender, EventArgs e)
    {
        int orderId = SaveReceive(true);
        if (orderId > 0)
        {
            ShowMessage("پذیرش ثبت شد. اکنون اطلاعات تسویه را وارد کنید.");
            ShowCompleteView(orderId);
        }
    }

    private int SaveReceive(bool redirectToComplete)
    {
        int currentId = EditId;

        int customerId;
        if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
            || !int.TryParse(ddlCustomer.SelectedValue, out customerId) || customerId <= 0)
        {
            ShowMessage("مشتری را انتخاب کنید.");
            return 0;
        }

        int serviceTypeId;
        if (string.IsNullOrEmpty(ddlServiceType.SelectedValue)
            || !int.TryParse(ddlServiceType.SelectedValue, out serviceTypeId) || serviceTypeId <= 0)
        {
            ShowMessage("نوع خدمت را انتخاب کنید.");
            return 0;
        }

        DateTime? svcDate = ParsePersianDate(txtReceiveDate.Text.Trim());
        if (!svcDate.HasValue)
        {
            ShowMessage("تاریخ پذیرش معتبر نیست.");
            return 0;
        }

        decimal inputWeight = ParseDecimalOrZero(txtInputWeight.Text);
        if (inputWeight <= 0)
        {
            ShowMessage("وزن ورودی باید عددی بزرگ‌تر از صفر باشد.");
            return 0;
        }

        // Load default unit price from service
        decimal unitPrice = 0m;
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT DefaultUnitPrice FROM ServiceTypes WHERE ServiceTypeId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", serviceTypeId);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) unitPrice = Convert.ToDecimal(o);
                }
            }
        }
        catch { }

        string deliveredBy = txtDeliveredBy.Text.Trim();
        string deliveredByMobile = txtDeliveredByMobile.Text.Trim();
        string notes = txtReceiveNotes.Text.Trim();

        // Estimated delivery
        int minutes = 0;
        int.TryParse(ddlEstimatedDelivery.SelectedValue, out minutes);
        DateTime? estimatedDelivery = null;
        if (minutes > 0)
            estimatedDelivery = DateTime.Now.AddMinutes(minutes);

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (currentId > 0)
                {
                    // UPDATE
                    string updateSql =
                        "UPDATE ServiceOrders SET " +
                        "  CustomerId = @CustomerId, ServiceTypeId = @SvcId, ServiceDate = @SvcDate, " +
                        "  InputWeight = @InputW, UnitPrice = @UnitPrice, " +
                        "  DeliveredBy = @DelBy, DeliveredByMobile = @DelByMobile, " +
                        "  EstimatedDelivery = @EstDel, Notes = @Notes, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE ServiceOrderId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        cmd.Parameters.AddWithValue("@SvcId", serviceTypeId);
                        cmd.Parameters.AddWithValue("@SvcDate", svcDate.Value);
                        cmd.Parameters.AddWithValue("@InputW", inputWeight);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                        cmd.Parameters.AddWithValue("@DelBy", NullIfEmpty(deliveredBy));
                        cmd.Parameters.AddWithValue("@DelByMobile", NullIfEmpty(deliveredByMobile));
                        cmd.Parameters.AddWithValue("@EstDel",
                            estimatedDelivery.HasValue ? (object)estimatedDelivery.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "ServiceOrders", currentId, "", "ویرایش پذیرش");
                    return currentId;
                }
                else
                {
                    // INSERT
                    string orderNumber = GenerateOrderNumber(conn);

                    string insertSql =
                        "INSERT INTO ServiceOrders " +
                        "  (OrderNumber, CustomerId, ServiceTypeId, ServiceDate, " +
                        "   InputWeight, OutputWeight, WasteWeight, WastePercent, " +
                        "   UnitPrice, Subtotal, Discount, Tax, Total, " +
                        "   PaymentMode, CashAmount, PaidAmount, PaymentStatus, Status, " +
                        "   DeliveredBy, DeliveredByMobile, EstimatedDelivery, Notes, " +
                        "   IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@Number, @CustomerId, @SvcId, @SvcDate, " +
                        "   @InputW, 0, 0, 0, " +
                        "   @UnitPrice, 0, 0, 0, 0, " +
                        "   1, 0, 0, 1, 1, " +
                        "   @DelBy, @DelByMobile, @EstDel, @Notes, " +
                        "   0, GETDATE(), @By); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Number", orderNumber);
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        cmd.Parameters.AddWithValue("@SvcId", serviceTypeId);
                        cmd.Parameters.AddWithValue("@SvcDate", svcDate.Value);
                        cmd.Parameters.AddWithValue("@InputW", inputWeight);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                        cmd.Parameters.AddWithValue("@DelBy", NullIfEmpty(deliveredBy));
                        cmd.Parameters.AddWithValue("@DelByMobile", NullIfEmpty(deliveredByMobile));
                        cmd.Parameters.AddWithValue("@EstDel",
                            estimatedDelivery.HasValue ? (object)estimatedDelivery.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "ServiceOrders", newId, orderNumber, "پذیرش جدید");

                    return newId;
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره: " + ex.Message);
            return 0;
        }
    }

    private string GenerateOrderNumber(SqlConnection conn)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "SVC-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(OrderNumber, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
            "FROM ServiceOrders WHERE OrderNumber LIKE @Prefix + '%'", conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + seq.ToString("D4");
        }
    }

    private string GetOrderNumber(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT OrderNumber FROM ServiceOrders WHERE ServiceOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
                }
            }
        }
        catch { return ""; }
    }

    private void LoadReceiveForEdit(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT CustomerId, ServiceTypeId, ServiceDate, InputWeight, " +
                    "       DeliveredBy, DeliveredByMobile, EstimatedDelivery, Notes " +
                    "FROM ServiceOrders WHERE ServiceOrderId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ddlCustomer.ClearSelection();
                            if (dr["CustomerId"] != DBNull.Value)
                            {
                                string cid = Convert.ToInt32(dr["CustomerId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem ci = ddlCustomer.Items.FindByValue(cid);
                                if (ci != null) ci.Selected = true;
                            }

                            ddlServiceType.ClearSelection();
                            if (dr["ServiceTypeId"] != DBNull.Value)
                            {
                                string sid = Convert.ToInt32(dr["ServiceTypeId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem si = ddlServiceType.Items.FindByValue(sid);
                                if (si != null) si.Selected = true;
                            }

                            txtReceiveDate.Text = dr["ServiceDate"] == DBNull.Value
                                ? "" : ToShamsiDateString(Convert.ToDateTime(dr["ServiceDate"]));
                            txtInputWeight.Text = dr["InputWeight"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["InputWeight"]).ToString(CultureInfo.InvariantCulture);
                            txtDeliveredBy.Text = dr["DeliveredBy"] == DBNull.Value
                                ? "" : Convert.ToString(dr["DeliveredBy"]);
                            txtDeliveredByMobile.Text = dr["DeliveredByMobile"] == DBNull.Value
                                ? "" : Convert.ToString(dr["DeliveredByMobile"]);
                            txtReceiveNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);

                            UpdateCustomerHint();
                            UpdateServiceDefaultPrice();
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

    protected void ddlCustomer_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateCustomerHint();
    }

    protected void ddlServiceType_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateServiceDefaultPrice();
    }

    private void UpdateCustomerHint()
    {
        int custId;
        if (string.IsNullOrEmpty(ddlCustomer.SelectedValue)
            || !int.TryParse(ddlCustomer.SelectedValue, out custId) || custId <= 0)
        {
            litCustomerHint.Text = "مشتری را از لیست انتخاب کنید.";
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CustomerType, Mobile, City FROM Customers WHERE CustomerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", custId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int type = dr["CustomerType"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CustomerType"]);
                            string mobile = dr["Mobile"] == DBNull.Value ? "" : Convert.ToString(dr["Mobile"]);
                            string city = dr["City"] == DBNull.Value ? "" : Convert.ToString(dr["City"]);

                            StringBuilder sb = new StringBuilder();
                            sb.Append("نوع: ").Append(GetCustomerTypeLabelStatic(type));
                            if (mobile.Length > 0) sb.Append(" — موبایل: ").Append(mobile);
                            if (city.Length > 0) sb.Append(" — ").Append(city);

                            litCustomerHint.Text = HttpUtility.HtmlEncode(sb.ToString());
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void UpdateServiceDefaultPrice()
    {
        int svcId;
        if (string.IsNullOrEmpty(ddlServiceType.SelectedValue)
            || !int.TryParse(ddlServiceType.SelectedValue, out svcId) || svcId <= 0)
        {
            litServiceDefaultPrice.Text = "—";
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT DefaultUnitPrice FROM ServiceTypes WHERE ServiceTypeId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", svcId);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value)
                    {
                        litServiceDefaultPrice.Text = HttpUtility.HtmlEncode(
                            FormatMoneyValue(Convert.ToDecimal(o)));
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Complete View
    // ─────────────────────────────────────────────────────
    private void LoadCompleteView(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT so.OrderNumber, so.CustomerId, so.ServiceTypeId, so.ServiceDate, " +
                    "  so.InputWeight, so.OutputWeight, so.InputDescription, so.OutputDescription, " +
                    "  so.UnitPrice, so.Discount, so.Tax, so.Total, " +
                    "  so.PaymentMode, so.CashAmount, " +
                    "  so.InKindWeight, so.InKindProductId, so.InKindUnitPrice, so.InKindWarehouseId, " +
                    "  ISNULL(so.StockMovementId, 0) AS StockMovementId, " +
                    "  so.Notes, " +
                    "  c.CustomerName, c.Mobile AS CustomerMobile, " +
                    "  st.ServiceName, st.DefaultWastePercent " +
                    "FROM ServiceOrders so " +
                    "INNER JOIN Customers c ON c.CustomerId = so.CustomerId " +
                    "INNER JOIN ServiceTypes st ON st.ServiceTypeId = so.ServiceTypeId " +
                    "WHERE so.ServiceOrderId = @Id AND so.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string orderNumber = Convert.ToString(dr["OrderNumber"]);
                            string customerName = Convert.ToString(dr["CustomerName"]);
                            string customerMobile = dr["CustomerMobile"] == DBNull.Value
                                ? "" : Convert.ToString(dr["CustomerMobile"]);
                            string serviceName = Convert.ToString(dr["ServiceName"]);

                            decimal inputWeight = dr["InputWeight"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["InputWeight"]);
                            decimal defaultWaste = dr["DefaultWastePercent"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["DefaultWastePercent"]);

                            litCompleteOrderNumber.Text = HttpUtility.HtmlEncode(orderNumber);
                            litCompleteCustomerInfo.Text = HttpUtility.HtmlEncode(
                                customerName + " — " + serviceName +
                                (customerMobile.Length > 0 ? " — " + customerMobile : ""));
                            litCompleteInputWeight.Text = HttpUtility.HtmlEncode(FormatQtyValue(inputWeight));
                            litCompleteDefaultWaste.Text = HttpUtility.HtmlEncode(
                                defaultWaste.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪");

                            // Pre-fill form
                            txtOutputWeight.Text = dr["OutputWeight"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["OutputWeight"]).ToString(CultureInfo.InvariantCulture);
                            txtInputDesc.Text = dr["InputDescription"] == DBNull.Value
                                ? "" : Convert.ToString(dr["InputDescription"]);
                            txtOutputDesc.Text = dr["OutputDescription"] == DBNull.Value
                                ? "" : Convert.ToString(dr["OutputDescription"]);

                            decimal unitPrice = dr["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["UnitPrice"]);
                            txtUnitPrice.Text = unitPrice.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                            decimal discount = dr["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Discount"]);
                            decimal tax = dr["Tax"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Tax"]);
                            txtDiscount.Text = discount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
                            txtTax.Text = tax.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                            int payMode = dr["PaymentMode"] == DBNull.Value ? 1 : Convert.ToInt32(dr["PaymentMode"]);
                            ddlPaymentMode.ClearSelection();
                            ListItem pmi = ddlPaymentMode.Items.FindByValue(payMode.ToString(CultureInfo.InvariantCulture));
                            if (pmi != null) pmi.Selected = true;

                            decimal cashAmount = dr["CashAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CashAmount"]);
                            txtCashAmount.Text = cashAmount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                            decimal ikW = dr["InKindWeight"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["InKindWeight"]);
                            decimal ikP = dr["InKindUnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["InKindUnitPrice"]);
                            txtInKindWeight.Text = ikW.ToString(CultureInfo.InvariantCulture);
                            txtInKindUnitPrice.Text = ikP.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");

                            ddlInKindProduct.ClearSelection();
                            if (dr["InKindProductId"] != DBNull.Value)
                            {
                                string pid = Convert.ToInt32(dr["InKindProductId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem pi = ddlInKindProduct.Items.FindByValue(pid);
                                if (pi != null) pi.Selected = true;
                            }

                            ddlInKindWarehouse.ClearSelection();
                            if (dr["InKindWarehouseId"] != DBNull.Value)
                            {
                                string wid = Convert.ToInt32(dr["InKindWarehouseId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem wi = ddlInKindWarehouse.Items.FindByValue(wid);
                                if (wi != null) wi.Selected = true;
                            }

                            ExistingMovementId = Convert.ToInt32(dr["StockMovementId"]);
                            txtCompleteNotes.Text = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);
                        }
                        else
                        {
                            ShowMessage("سفارش یافت نشد.");
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

    protected void btnCancelComplete_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowListView();
    }

    protected void btnSaveComplete_Click(object sender, EventArgs e)
    {
        int orderId = EditId;
        if (orderId <= 0)
        {
            ShowMessage("سفارش معتبر نیست.");
            return;
        }

        decimal outputWeight = ParseDecimalOrZero(txtOutputWeight.Text);
        if (outputWeight <= 0)
        {
            ShowMessage("وزن خروجی باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        // Get input weight from DB
        decimal inputWeight = 0m;
        int serviceTypeId = 0;
        string orderNumber = "";
        DateTime serviceDate = DateTime.Now;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT OrderNumber, ServiceTypeId, ServiceDate, InputWeight " +
                    "FROM ServiceOrders WHERE ServiceOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            orderNumber = Convert.ToString(dr["OrderNumber"]);
                            serviceTypeId = Convert.ToInt32(dr["ServiceTypeId"]);
                            serviceDate = Convert.ToDateTime(dr["ServiceDate"]);
                            inputWeight = dr["InputWeight"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["InputWeight"]);
                        }
                    }
                }
            }
        }
        catch { }

        if (outputWeight > inputWeight)
        {
            ShowMessage("وزن خروجی نمی‌تواند بیشتر از وزن ورودی باشد.");
            return;
        }

        decimal wasteWeight = inputWeight - outputWeight;
        decimal wastePercent = inputWeight > 0
            ? Math.Round((wasteWeight / inputWeight) * 100m, 2) : 0m;

        // Pricing
        decimal unitPrice = ParseDecimalOrZero(txtUnitPrice.Text);
        decimal subtotal = Math.Round(inputWeight * unitPrice, 2);
        decimal discount = ParseDecimalOrZero(txtDiscount.Text);
        decimal tax = ParseDecimalOrZero(txtTax.Text);
        decimal total = subtotal - discount + tax;
        if (total < 0) total = 0;

        // Payment
        int paymentMode = 1;
        int.TryParse(ddlPaymentMode.SelectedValue, out paymentMode);
        if (paymentMode < 1 || paymentMode > 3) paymentMode = 1;

        decimal cashAmount = ParseDecimalOrZero(txtCashAmount.Text);
        decimal inKindWeight = ParseDecimalOrZero(txtInKindWeight.Text);
        decimal inKindUnitPrice = ParseDecimalOrZero(txtInKindUnitPrice.Text);
        decimal inKindValue = Math.Round(inKindWeight * inKindUnitPrice, 2);

        int inKindProductId = 0;
        int inKindWarehouseId = 0;
        if (paymentMode == 2 || paymentMode == 3)
        {
            if (string.IsNullOrEmpty(ddlInKindProduct.SelectedValue)
                || !int.TryParse(ddlInKindProduct.SelectedValue, out inKindProductId)
                || inKindProductId <= 0)
            { ShowMessage("محصول دریافتی را انتخاب کنید."); return; }

            if (inKindWeight <= 0)
            { ShowMessage("وزن محصول دریافتی باید بزرگ‌تر از صفر باشد."); return; }

            if (inKindUnitPrice <= 0)
            { ShowMessage("ارزش واحد محصول دریافتی باید بزرگ‌تر از صفر باشد."); return; }

            if (string.IsNullOrEmpty(ddlInKindWarehouse.SelectedValue)
                || !int.TryParse(ddlInKindWarehouse.SelectedValue, out inKindWarehouseId)
                || inKindWarehouseId <= 0)
            { ShowMessage("انبار ورود را انتخاب کنید."); return; }
        }

        decimal paidAmount = (paymentMode == 1) ? cashAmount
                            : (paymentMode == 2) ? inKindValue
                            : cashAmount + inKindValue;

        int paymentStatus = 1;
        if (paidAmount >= total - 0.01m) paymentStatus = 3;
        else if (paidAmount > 0m) paymentStatus = 2;

        string cashMethod = ddlCashMethod.SelectedValue;
        if (cashMethod == null) cashMethod = "";
        string cashRef = txtCashRef.Text.Trim();
        string inputDesc = txtInputDesc.Text.Trim();
        string outputDesc = txtOutputDesc.Text.Trim();
        string notes = txtCompleteNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Update order to completed
                        string updateSql =
                            "UPDATE ServiceOrders SET " +
                            "  OutputWeight = @OutputW, WasteWeight = @WasteW, WastePercent = @WasteP, " +
                            "  InputDescription = @InputDesc, OutputDescription = @OutputDesc, " +
                            "  UnitPrice = @UnitPrice, Subtotal = @Subtotal, Discount = @Discount, Tax = @Tax, Total = @Total, " +
                            "  PaymentMode = @PayMode, CashAmount = @CashAmount, " +
                            "  InKindWeight = @InKindW, InKindProductId = @InKindPId, " +
                            "  InKindUnitPrice = @InKindPrice, InKindValue = @InKindValue, InKindWarehouseId = @InKindWId, " +
                            "  PaidAmount = @PaidAmount, PaymentStatus = @PayStatus, " +
                            "  Status = 3, EndTime = GETDATE(), Notes = @Notes, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ServiceOrderId = @Id";

                        using (SqlCommand cmd = new SqlCommand(updateSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@OutputW", outputWeight);
                            cmd.Parameters.AddWithValue("@WasteW", wasteWeight);
                            cmd.Parameters.AddWithValue("@WasteP", wastePercent);
                            cmd.Parameters.AddWithValue("@InputDesc", NullIfEmpty(inputDesc));
                            cmd.Parameters.AddWithValue("@OutputDesc", NullIfEmpty(outputDesc));
                            cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                            cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                            cmd.Parameters.AddWithValue("@Discount", discount);
                            cmd.Parameters.AddWithValue("@Tax", tax);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@PayMode", paymentMode);
                            cmd.Parameters.AddWithValue("@CashAmount", cashAmount);
                            cmd.Parameters.AddWithValue("@InKindW", inKindWeight);
                            cmd.Parameters.AddWithValue("@InKindPId",
                                inKindProductId > 0 ? (object)inKindProductId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@InKindPrice", inKindUnitPrice);
                            cmd.Parameters.AddWithValue("@InKindValue", inKindValue);
                            cmd.Parameters.AddWithValue("@InKindWId",
                                inKindWarehouseId > 0 ? (object)inKindWarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@PaidAmount", paidAmount);
                            cmd.Parameters.AddWithValue("@PayStatus", paymentStatus);
                            cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                            cmd.Parameters.AddWithValue("@By",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        // Cash payment record
                        if (cashAmount > 0 && (paymentMode == 1 || paymentMode == 3))
                        {
                            InsertCashPayment(conn, tran, orderId, cashAmount, serviceDate, cashMethod, cashRef);
                        }

                        // Create StockMovement for in-kind
                        if ((paymentMode == 2 || paymentMode == 3) && inKindWeight > 0
                            && inKindProductId > 0 && inKindWarehouseId > 0 && ExistingMovementId == 0)
                        {
                            int newMovementId = CreateInKindStockMovement(
                                conn, tran, orderId, inKindProductId, inKindWarehouseId,
                                inKindWeight, inKindUnitPrice, orderNumber);

                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE ServiceOrders SET StockMovementId = @MovId WHERE ServiceOrderId = @Id",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@MovId", newMovementId);
                                cmd.Parameters.AddWithValue("@Id", orderId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        WriteAuditTran(conn, tran, "Complete", "ServiceOrders", orderId,
                            orderNumber, "تکمیل و تسویه");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }

            ShowMessage("سفارش با موفقیت تکمیل و تسویه شد.");
            EditId = 0;
            ShowListView();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تسویه: " + ex.Message);
        }
    }

    private void InsertCashPayment(SqlConnection conn, SqlTransaction tran,
        int orderId, decimal amount, DateTime payDate, string method, string refNum)
    {
        string sql =
            "INSERT INTO ServiceOrderPayments " +
            "  (ServiceOrderId, Amount, PaymentDate, PaymentMethod, ReferenceNumber, CreatedAt, CreatedBy) " +
            "VALUES (@OrderId, @Amount, @PayDate, @Method, @Ref, GETDATE(), @By)";

        using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
        {
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@PayDate", payDate);
            cmd.Parameters.AddWithValue("@Method", NullIfEmpty(method));
            cmd.Parameters.AddWithValue("@Ref", NullIfEmpty(refNum));
            cmd.Parameters.AddWithValue("@By",
                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private int CreateInKindStockMovement(SqlConnection conn, SqlTransaction tran,
        int orderId, int productId, int warehouseId, decimal weight, decimal unitPrice, string orderNumber)
    {
        string sql1 =
            "INSERT INTO StockMovements " +
            "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
            "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
            "VALUES (@WhId, @PId, 1, @Qty, @UnitCost, @TotalCost, " +
            "   GETDATE(), 'ServiceOrder', @RefId, @Notes, GETDATE(), @By); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);";

        int movementId = 0;
        using (SqlCommand cmd = new SqlCommand(sql1, conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            cmd.Parameters.AddWithValue("@Qty", weight);
            cmd.Parameters.AddWithValue("@UnitCost", unitPrice);
            cmd.Parameters.AddWithValue("@TotalCost", Math.Round(weight * unitPrice, 2));
            cmd.Parameters.AddWithValue("@RefId", orderId);
            cmd.Parameters.AddWithValue("@Notes",
                "محصول دریافتی بابت دستمزد خدمت " + orderNumber);
            cmd.Parameters.AddWithValue("@By",
                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

            object o = cmd.ExecuteScalar();
            if (o != null && o != DBNull.Value) movementId = Convert.ToInt32(o);
        }

        if (movementId <= 0)
            throw new Exception("خطا در ایجاد حرکت انبار");

        string sql2 =
            "INSERT INTO StockLayers " +
            "  (WarehouseId, ProductId, StockMovementId, QuantityReceived, QuantityRemaining, " +
            "   UnitCost, ReceivedDate, IsClosed) " +
            "VALUES (@WhId, @PId, @MovId, @Qty, @Qty, @UnitCost, GETDATE(), 0)";

        using (SqlCommand cmd = new SqlCommand(sql2, conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            cmd.Parameters.AddWithValue("@MovId", movementId);
            cmd.Parameters.AddWithValue("@Qty", weight);
            cmd.Parameters.AddWithValue("@UnitCost", unitPrice);
            cmd.ExecuteNonQuery();
        }

        return movementId;
    }

    // ─────────────────────────────────────────────────────
    // Row commands
    // ─────────────────────────────────────────────────────
    protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Complete") ShowCompleteView(id);
        else if (e.CommandName == "EditReceive") ShowReceiveView(id);
        else if (e.CommandName == "View") ShowCompleteView(id);
        else if (e.CommandName == "CancelOrder") CancelOrder(id);
    }

    private void CancelOrder(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string orderNumber = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT OrderNumber FROM ServiceOrders WHERE ServiceOrderId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", orderId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) orderNumber = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(orderNumber))
                {
                    ShowMessage("سفارش یافت نشد.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ServiceOrders SET Status = 4 WHERE ServiceOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Cancel", "ServiceOrders", orderId, orderNumber, "لغو سفارش");
            }

            ShowMessage("سفارش لغو شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
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
        WriteAuditTran(conn, null, action, entityName, entityId, targetName, note);
    }

    private void WriteAuditTran(SqlConnection conn, SqlTransaction tran, string action,
        string entityName, int entityId, string targetName, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
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

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    protected string FormatQty(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return FormatQtyValue(Convert.ToDecimal(value)); }
        catch { return "۰"; }
    }

    private string FormatQtyValue(decimal d)
    {
        if (d == 0m) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
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
        catch { return "—"; }
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    protected string GetRowCss(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1)
            return "border-b border-surface-line bg-amber-50/40 hover:bg-amber-50/70 transition";
        if (s == 3)
            return "border-b border-surface-line transition hover:bg-surface-body/70";
        return "border-b border-surface-line opacity-60";
    }

    protected string GetStatusBadge(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1)
            return "<span class=\"badge bg-amber-100 text-amber-800\"><i class=\"h-3 w-3\" data-lucide=\"clock\"></i> در انتظار تسویه</span>";
        if (s == 3)
            return "<span class=\"badge bg-emerald-100 text-emerald-800\"><i class=\"h-3 w-3\" data-lucide=\"check-circle-2\"></i> تکمیل‌شده</span>";
        if (s == 4)
            return "<span class=\"badge bg-danger-50 text-danger-600\">لغو‌شده</span>";
        return "";
    }

    protected bool IsPending(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1;
    }

    protected bool IsCompleted(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 3;
    }

    protected string GetAmountCell(object statusObj, object totalObj, object paidObj, object modeObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 1)
            return "<span class=\"text-[11.5px] text-ink-400\">پس از تسویه</span>";

        decimal total = (totalObj == null || totalObj == DBNull.Value) ? 0m : Convert.ToDecimal(totalObj);
        decimal paid = (paidObj == null || paidObj == DBNull.Value) ? 0m : Convert.ToDecimal(paidObj);
        int mode = (modeObj == null || modeObj == DBNull.Value) ? 1 : Convert.ToInt32(modeObj);

        string modeBadge = "";
        if (mode == 1) modeBadge = "<span class=\"badge bg-emerald-50 text-emerald-700 text-[10px] mr-1\">نقدی</span>";
        else if (mode == 2) modeBadge = "<span class=\"badge bg-violet-50 text-violet-700 text-[10px] mr-1\">جنسی</span>";
        else modeBadge = "<span class=\"badge bg-amber-50 text-amber-700 text-[10px] mr-1\">ترکیبی</span>";

        decimal remaining = total - paid;
        string sub = "";
        if (remaining > 0.01m)
            sub = "<p class=\"mt-0.5 text-[10.5px] text-danger-500 font-mono\">" +
                  FormatMoneyValue(remaining) + " مانده</p>";
        else
            sub = "<p class=\"mt-0.5 text-[10.5px] text-emerald-600 font-bold\">تسویه شد ✓</p>";

        return "<span class=\"font-mono font-bold text-ink-900 text-[13.5px]\">" +
               FormatMoneyValue(total) + "</span>" +
               "<div class=\"mt-0.5\">" + modeBadge + "</div>" + sub;
    }

    protected string GetOutputHint(object outputObj)
    {
        if (outputObj == null || outputObj == DBNull.Value) return "";
        decimal d = Convert.ToDecimal(outputObj);
        if (d == 0m) return " <span class=\"text-ink-300\">→ ?</span>";
        return " <span class=\"text-ink-400\">→</span> " + FormatQtyValue(d);
    }

    protected string GetWasteHint(object pctObj, object weightObj)
    {
        if (pctObj == null || pctObj == DBNull.Value) return "";
        decimal pct = Convert.ToDecimal(pctObj);
        if (pct <= 0m) return "";
        string cls = pct >= 10m ? "text-danger-500" : (pct >= 5m ? "text-amber-600" : "text-brand-600");
        return "<div class=\"mt-0.5 text-[10.5px] " + cls + " font-mono\">" +
               pct.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪ ضایعات</div>";
    }

    protected string GetDeliveredByHint(object nameObj, object mobileObj)
    {
        string name = nameObj == null || nameObj == DBNull.Value ? "" : Convert.ToString(nameObj).Trim();
        if (name.Length == 0) return "";

        string mobile = mobileObj == null || mobileObj == DBNull.Value ? "" : Convert.ToString(mobileObj).Trim();
        string s = "تحویل‌دهنده: " + name;
        if (mobile.Length > 0) s += " — " + mobile;

        return "<p class=\"mt-0.5 text-[10.5px] text-violet-600\">" +
               HttpUtility.HtmlEncode(s) + "</p>";
    }

    protected string GetCustomerTypeHint(object typeObj)
    {
        int t = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (t == 4)
            return "<p class=\"mt-0.5 text-[10.5px] text-amber-600\">کشاورز</p>";
        return "";
    }

    private string GetCustomerTypeLabelStatic(int type)
    {
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        if (type == 3) return "نمایندگی";
        if (type == 4) return "کشاورز";
        return "—";
    }

    protected string GetCancelConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value
            ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('آیا از لغو سفارش «" + num + "» مطمئن هستید؟');";
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

    private decimal ParseDecimalOrZero(string input)
    {
        decimal? d = ParseDecimal(input);
        return d.HasValue ? d.Value : 0m;
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

    private void ClearReceiveForm()
    {
        if (ddlCustomer.Items.Count > 0) ddlCustomer.SelectedIndex = 0;
        if (ddlServiceType.Items.Count > 0) ddlServiceType.SelectedIndex = 0;
        if (ddlEstimatedDelivery.Items.Count > 0)
        {
            ddlEstimatedDelivery.ClearSelection();
            ListItem item = ddlEstimatedDelivery.Items.FindByValue("180");
            if (item != null) item.Selected = true;
        }

        txtReceiveDate.Text = ToShamsiDateString(DateTime.Now);
        txtInputWeight.Text = "";
        txtDeliveredBy.Text = "";
        txtDeliveredByMobile.Text = "";
        txtReceiveNotes.Text = "";

        litCustomerHint.Text = "مشتری را از لیست انتخاب کنید.";
        litServiceDefaultPrice.Text = "—";
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