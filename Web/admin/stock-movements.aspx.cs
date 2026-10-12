using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_stock_movements : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
    }

    private int PageSize
    {
        get
        {
            int size = 25;
            if (int.TryParse(ddlPageSize.SelectedValue, out size))
            {
                if (size <= 0) size = 25;
                if (size > 500) size = 500;
            }
            return size;
        }
    }

    private int DateRangeDays
    {
        get
        {
            int days = 7;
            if (int.TryParse(hfDateRange.Value, out days))
            {
                if (days < 0) days = 0;
            }
            return days;
        }
        set { hfDateRange.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private string Direction
    {
        get
        {
            if (hfDirection.Value == null) return "";
            return hfDirection.Value.Trim();
        }
        set { hfDirection.Value = value; }
    }

    private long SelectedMovementId
    {
        get
        {
            long id;
            if (long.TryParse(hfSelectedMovementId.Value, out id)) return id;
            return 0;
        }
        set { hfSelectedMovementId.Value = value.ToString(CultureInfo.InvariantCulture); }
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
        if (CurrentUserId <= 0)
        {
            Response.Redirect(ResolveUrl("~/Login.aspx"), false);
            return;
        }

        HandlePostbackRequests();

        if (!IsPostBack)
        {
            CurrentPage = 1;
            DateRangeDays = 7;
            Direction = "";

            // پارامترهای URL
            string prodId = Request.QueryString["productId"];
            string whId = Request.QueryString["warehouseId"];

            LoadWarehouseFilter();

            if (!string.IsNullOrEmpty(prodId))
            {
                string code = GetProductCodeById(prodId);
                if (!string.IsNullOrEmpty(code)) txtSearch.Text = code;
            }

            if (!string.IsNullOrEmpty(whId))
            {
                ListItem item = ddlFilterWarehouse.Items.FindByValue(whId);
                if (item != null)
                {
                    ddlFilterWarehouse.ClearSelection();
                    item.Selected = true;
                }
            }

            LoadStats();
            LoadMovements();
        }
    }

    private void HandlePostbackRequests()
    {
        string target = Request.Form["__EVENTTARGET"];
        string arg = Request.Form["__EVENTARGUMENT"];

        if (target != "__PAGE" || string.IsNullOrEmpty(arg)) return;

        if (arg.StartsWith("setRange:"))
        {
            string range = arg.Substring("setRange:".Length);
            int days;
            if (int.TryParse(range, out days))
            {
                DateRangeDays = days;
                CurrentPage = 1;
                LoadStats();
                LoadMovements();
            }
        }
        else if (arg.StartsWith("setDir:"))
        {
            string dir = arg.Substring("setDir:".Length);
            Direction = dir;
            CurrentPage = 1;
            LoadStats();
            LoadMovements();
        }
    }

    private string GetProductCodeById(string productId)
    {
        try
        {
            int pid;
            if (!int.TryParse(productId, out pid) || pid <= 0) return "";

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductCode FROM Products WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", pid);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return Convert.ToString(o);
                }
            }
        }
        catch { }
        return "";
    }

    // ─────────────────────────────────────────────────────
    // Load: Warehouses
    // ─────────────────────────────────────────────────────
    private void LoadWarehouseFilter()
    {
        ddlFilterWarehouse.Items.Clear();
        ddlFilterWarehouse.Items.Add(new ListItem("همه انبارها", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT WarehouseId, WarehouseName, WarehouseType FROM Warehouses " +
                    "WHERE IsActive = 1 ORDER BY WarehouseType, WarehouseId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["WarehouseId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["WarehouseName"]);
                        int type = dr["WarehouseType"] == DBNull.Value ? 1 : Convert.ToInt32(dr["WarehouseType"]);
                        string typeLabel = type == 1 ? "RM" : "FG";

                        ddlFilterWarehouse.Items.Add(new ListItem(name + " (" + typeLabel + ")", id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری انبارها: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Stats
    // ─────────────────────────────────────────────────────
    private void LoadStats()
    {
        try
        {
            // فیلتر جهت
            string dirFilter = "";
            if (Direction == "IN") dirFilter = " AND sm.MovementType IN (1,2,5) ";
            else if (Direction == "OUT") dirFilter = " AND sm.MovementType IN (3,4,6) ";
            else if (Direction == "TRANSFER") dirFilter = " AND sm.MovementType = 7 ";

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // IN count
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM StockMovements sm WHERE sm.MovementType IN (1,2,5) " + dirFilter, conn))
                {
                    object o = cmd.ExecuteScalar();
                    litStatIn.Text = HttpUtility.HtmlEncode(
                        ToInt(o).ToString("N0", CultureInfo.InvariantCulture));
                }

                // OUT count
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM StockMovements sm WHERE sm.MovementType IN (3,4,6) " + dirFilter, conn))
                {
                    object o = cmd.ExecuteScalar();
                    litStatOut.Text = HttpUtility.HtmlEncode(
                        ToInt(o).ToString("N0", CultureInfo.InvariantCulture));
                }

                // Today's movements
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM StockMovements sm " +
                    "WHERE CAST(sm.MovementDate AS DATE) = CAST(GETDATE() AS DATE) " + dirFilter, conn))
                {
                    object o = cmd.ExecuteScalar();
                    litStatToday.Text = HttpUtility.HtmlEncode(
                        ToInt(o).ToString("N0", CultureInfo.InvariantCulture));
                }

                // Total filtered count
                int total = GetTotalCount(
                    txtSearch.Text.Trim(),
                    ddlFilterMovementType.SelectedValue,
                    ddlFilterWarehouse.SelectedValue,
                    ddlFilterProductType.SelectedValue,
                    ddlFilterReferenceType.SelectedValue);

                litStatTotal.Text = HttpUtility.HtmlEncode(
                    total.ToString("N0", CultureInfo.InvariantCulture));
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
    // Load: Movements
    // ─────────────────────────────────────────────────────
    private void LoadMovements()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string mtFilter = ddlFilterMovementType.SelectedValue;
            string whFilter = ddlFilterWarehouse.SelectedValue;
            string ptFilter = ddlFilterProductType.SelectedValue;
            string refFilter = ddlFilterReferenceType.SelectedValue;
            if (mtFilter == null) mtFilter = "";
            if (whFilter == null) whFilter = "";
            if (ptFilter == null) ptFilter = "";
            if (refFilter == null) refFilter = "";

            int totalCount = GetTotalCount(search, mtFilter, whFilter, ptFilter, refFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, mtFilter, whFilter, ptFilter, refFilter, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptMovements.Visible = false;
                rptMovements.DataSource = null;
                rptMovements.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptMovements.Visible = true;
                rptMovements.DataSource = dt;
                rptMovements.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} گردش",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            litPageInfo.Text = string.Format("صفحه {0} از {1}",
                CurrentPage.ToString(CultureInfo.InvariantCulture),
                totalPages.ToString(CultureInfo.InvariantCulture));

            litCurrentPage.Text = HttpUtility.HtmlEncode(ToPersianNumber(CurrentPage.ToString(CultureInfo.InvariantCulture)));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);

            BuildCsvData(dt);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری گردش‌ها: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string mtFilter, string whFilter, string ptFilter,
        string refFilter, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        // ── بازه زمانی ──
        int days = DateRangeDays;
        if (days > 0)
        {
            sb.Append(" AND sm.MovementDate >= DATEADD(DAY, -@Days, CAST(GETDATE() AS DATE)) ");
            cmd.Parameters.AddWithValue("@Days", days - 1);
        }

        // ── جهت ──
        if (Direction == "IN")
        {
            sb.Append(" AND sm.MovementType IN (1,2,5) ");
        }
        else if (Direction == "OUT")
        {
            sb.Append(" AND sm.MovementType IN (3,4,6) ");
        }
        else if (Direction == "TRANSFER")
        {
            sb.Append(" AND sm.MovementType = 7 ");
        }

        // ── نوع گردش ──
        if (mtFilter.Length > 0)
        {
            int mt;
            if (int.TryParse(mtFilter, out mt))
            {
                sb.Append(" AND sm.MovementType = @MovementType ");
                cmd.Parameters.AddWithValue("@MovementType", mt);
            }
        }

        // ── انبار ──
        if (whFilter.Length > 0)
        {
            int wh;
            if (int.TryParse(whFilter, out wh))
            {
                sb.Append(" AND sm.WarehouseId = @WarehouseId ");
                cmd.Parameters.AddWithValue("@WarehouseId", wh);
            }
        }

        // ── نوع محصول ──
        if (ptFilter.Length > 0)
        {
            int pt;
            if (int.TryParse(ptFilter, out pt))
            {
                sb.Append(" AND p.ProductType = @ProductType ");
                cmd.Parameters.AddWithValue("@ProductType", pt);
            }
        }

        // ── سند مرجع ──
        if (refFilter.Length > 0)
        {
            sb.Append(" AND sm.ReferenceType = @ReferenceType ");
            cmd.Parameters.AddWithValue("@ReferenceType", refFilter);
        }

        // ── جستجو ──
        if (search.Length > 0)
        {
            sb.Append(" AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search " +
                      " OR sm.BatchNumber LIKE @Search OR sm.Notes LIKE @Search " +
                      " OR sm.ReferenceType LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string mtFilter, string whFilter, string ptFilter, string refFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, mtFilter, whFilter, ptFilter, refFilter, cmd);
                cmd.CommandText =
                    "SELECT COUNT(*) FROM StockMovements sm " +
                    "INNER JOIN Products p ON p.ProductId = sm.ProductId " + filter;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string mtFilter, string whFilter, string ptFilter,
        string refFilter, int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, mtFilter, whFilter, ptFilter, refFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY sm.StockMovementId DESC) AS RowNum, " +
                    "    sm.StockMovementId, sm.WarehouseId, sm.ProductId, sm.MovementType, " +
                    "    sm.Quantity, sm.UnitCost, sm.TotalCost, sm.MovementDate, " +
                    "    sm.ReferenceType, sm.ReferenceId, sm.BatchNumber, sm.ExpiryDate, " +
                    "    sm.Notes, sm.CreatedAt, " +
                    "    w.WarehouseName, w.WarehouseType, " +
                    "    p.ProductCode, p.ProductName, p.ProductType, " +
                    "    u.UnitName, u.Symbol " +
                    "  FROM StockMovements sm " +
                    "  INNER JOIN Warehouses w ON w.WarehouseId = sm.WarehouseId " +
                    "  INNER JOIN Products p ON p.ProductId = sm.ProductId " +
                    "  INNER JOIN Units u ON u.UnitId = p.UnitId " +
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
        LoadStats();
        LoadMovements();
    }

    protected void ddlFilterMovementType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStats();
        LoadMovements();
    }

    protected void ddlFilterWarehouse_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStats();
        LoadMovements();
    }

    protected void ddlFilterProductType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStats();
        LoadMovements();
    }

    protected void ddlFilterReferenceType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStats();
        LoadMovements();
    }

    protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadMovements();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        ddlFilterMovementType.ClearSelection();
        ddlFilterWarehouse.ClearSelection();
        ddlFilterProductType.ClearSelection();
        ddlFilterReferenceType.ClearSelection();
        DateRangeDays = 7;
        Direction = "";
        CurrentPage = 1;
        LoadStats();
        LoadMovements();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadMovements(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadMovements();
    }

    protected void rptMovements_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName == "Details")
        {
            long id;
            if (long.TryParse(Convert.ToString(e.CommandArgument), out id) && id > 0)
            {
                SelectedMovementId = id;
                LoadMovementDetails(id);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Movement Details
    // ─────────────────────────────────────────────────────
    private void LoadMovementDetails(long movementId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int movementType = 0;

                string sql =
                    "SELECT sm.StockMovementId, sm.MovementType, sm.Quantity, sm.UnitCost, sm.TotalCost, " +
                    "       sm.MovementDate, sm.ReferenceType, sm.ReferenceId, " +
                    "       sm.BatchNumber, sm.ExpiryDate, sm.Notes, " +
                    "       w.WarehouseName, w.WarehouseType, " +
                    "       p.ProductCode, p.ProductName, p.ProductType, " +
                    "       u.UnitName " +
                    "FROM StockMovements sm " +
                    "INNER JOIN Warehouses w ON w.WarehouseId = sm.WarehouseId " +
                    "INNER JOIN Products p ON p.ProductId = sm.ProductId " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE sm.StockMovementId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", movementId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            movementType = Convert.ToInt32(dr["MovementType"]);

                            litModalMovementId.Text = HttpUtility.HtmlEncode(
                                movementId.ToString(CultureInfo.InvariantCulture));

                            string pname = Convert.ToString(dr["ProductName"]);
                            string pcode = Convert.ToString(dr["ProductCode"]);
                            litModalProduct.Text =
                                HttpUtility.HtmlEncode(pname) +
                                " <span class=\"font-mono text-[11px] text-ink-400\" dir=\"ltr\">(" +
                                HttpUtility.HtmlEncode(pcode) + ")</span>";

                            int whType = dr["WarehouseType"] == DBNull.Value ? 1 : Convert.ToInt32(dr["WarehouseType"]);
                            string whName = Convert.ToString(dr["WarehouseName"]);
                            litModalWarehouse.Text =
                                "<span class=\"" + GetWarehouseBadgeCss(whType) + "\">" +
                                HttpUtility.HtmlEncode(whName) +
                                "</span>";

                            litModalType.Text =
                                "<span class=\"" + GetMovementTypeBadgeCss(movementType) + "\">" +
                                HttpUtility.HtmlEncode(GetMovementTypeLabel(movementType)) +
                                "</span>";

                            string sign = GetQuantitySign(movementType);
                            string signClass = GetQuantitySignClass(movementType);
                            string unitName = dr["UnitName"] == DBNull.Value ? "" : Convert.ToString(dr["UnitName"]);

                            litModalQuantity.Text =
                                "<span class=\"" + signClass + "\">" +
                                HttpUtility.HtmlEncode(sign + " " + FormatQuantityValue(
                                    dr["Quantity"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Quantity"])) +
                                    " " + unitName) +
                                "</span>";

                            litModalUnitCost.Text = HttpUtility.HtmlEncode(
                                FormatMoneyValue(dr["UnitCost"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["UnitCost"])));

                            litModalTotalCost.Text = HttpUtility.HtmlEncode(
                                FormatMoneyValue(dr["TotalCost"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TotalCost"])));

                            string refType = dr["ReferenceType"] == DBNull.Value ? "" : Convert.ToString(dr["ReferenceType"]);
                            int refId = dr["ReferenceId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ReferenceId"]);
                            litModalReference.Text = RenderReferenceText(refType, refId);

                            string batch = dr["BatchNumber"] == DBNull.Value ? "" : Convert.ToString(dr["BatchNumber"]);
                            litModalBatch.Text = batch.Length > 0 ? HttpUtility.HtmlEncode(batch) : "—";

                            litModalExpiry.Text = FormatShamsiDateValue(dr["ExpiryDate"]);

                            string notes = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);
                            if (notes.Length > 0)
                            {
                                litModalNotes.Text = HttpUtility.HtmlEncode(notes);
                                pnlModalNotes.Visible = true;
                            }
                            else
                            {
                                pnlModalNotes.Visible = false;
                            }

                            // Modal icon
                            SetModalIcon(movementType);
                        }
                        else
                        {
                            ShowMessage("گردش مورد نظر یافت نشد.");
                            return;
                        }
                    }
                }

                // ── Load Layers ──
                LoadLayersForMovement(conn, movementId, movementType);
            }

            pnlDetailsModal.Visible = true;
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری جزئیات: " + ex.Message);
        }
    }

    private void SetModalIcon(int movementType)
    {
        // از JS استفاده می‌کنیم تا آیکن را تغییر دهیم
        string iconName = "arrow-left-right";
        string cssClass = "grid h-11 w-11 place-items-center rounded-base ";

        if (movementType == 1 || movementType == 2)
        {
            iconName = "arrow-down-to-line";
            cssClass += "bg-emerald-50 text-emerald-600";
        }
        else if (movementType == 3 || movementType == 4)
        {
            iconName = "arrow-up-from-line";
            cssClass += "bg-rose-50 text-rose-600";
        }
        else if (movementType == 5)
        {
            iconName = "plus-circle";
            cssClass += "bg-emerald-50 text-emerald-600";
        }
        else if (movementType == 6)
        {
            iconName = "minus-circle";
            cssClass += "bg-rose-50 text-rose-600";
        }
        else if (movementType == 7)
        {
            iconName = "arrow-right-left";
            cssClass += "bg-brand-50 text-brand-600";
        }

        string script =
            "(function(){" +
            "  var wrap = document.getElementById('modalIconWrap');" +
            "  if (wrap) wrap.className = '" + cssClass.Replace("'", "\\'") + "';" +
            "  var icon = document.getElementById('modalIcon');" +
            "  if (icon) {" +
            "    icon.setAttribute('data-lucide', '" + iconName + "');" +
            "    if (typeof lucide !== 'undefined') lucide.createIcons();" +
            "  }" +
            "})();";

        if (ScriptManager.GetCurrent(this) != null)
        {
            ScriptManager.RegisterStartupScript(this, GetType(),
                "modalIcon_" + Guid.NewGuid().ToString("N"), script, true);
        }
        else
        {
            ClientScript.RegisterStartupScript(GetType(),
                "modalIcon_" + Guid.NewGuid().ToString("N"), script, true);
        }
    }

    private void LoadLayersForMovement(SqlConnection conn, long movementId, int movementType)
    {
        // برای IN (1,2,5): لایه‌ای که این حرکت ساخته
        // برای OUT (3,4,6): لایه‌هایی که مصرف شده
        // برای TRANSFER (7): هر دو
        DataTable dt = new DataTable();
        dt.Columns.Add("StockLayerId", typeof(long));
        dt.Columns.Add("QuantityReceived", typeof(decimal));
        dt.Columns.Add("QuantityRemaining", typeof(decimal));
        dt.Columns.Add("QuantityUsed", typeof(decimal));
        dt.Columns.Add("UnitCost", typeof(decimal));
        dt.Columns.Add("ReceivedDate", typeof(DateTime));
        dt.Columns.Add("IsClosed", typeof(bool));

        string layersTitle = "لایه‌های مرتبط (FIFO)";

        try
        {
            if (movementType == 1 || movementType == 2 || movementType == 5)
            {
                // IN: یک لایه ساخته شده
                layersTitle = "لایه ایجادشده توسط این ورود";

                string sql =
                    "SELECT StockLayerId, QuantityReceived, QuantityRemaining, UnitCost, " +
                    "       ReceivedDate, IsClosed " +
                    "FROM StockLayers WHERE StockMovementId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", movementId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            DataRow row = dt.NewRow();
                            row["StockLayerId"] = Convert.ToInt64(dr["StockLayerId"]);
                            row["QuantityReceived"] = dr["QuantityReceived"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityReceived"]);
                            row["QuantityRemaining"] = dr["QuantityRemaining"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityRemaining"]);
                            row["QuantityUsed"] = DBNull.Value;
                            row["UnitCost"] = dr["UnitCost"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["UnitCost"]);
                            row["ReceivedDate"] = dr["ReceivedDate"] == DBNull.Value
                                ? DateTime.Now : Convert.ToDateTime(dr["ReceivedDate"]);
                            row["IsClosed"] = dr["IsClosed"] == DBNull.Value
                                ? false : Convert.ToBoolean(dr["IsClosed"]);
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            else if (movementType == 3 || movementType == 4 || movementType == 6)
            {
                // OUT: از چند لایه مصرف شده
                layersTitle = "لایه‌های مصرف‌شده (FIFO)";

                string sql =
                    "SELECT sil.StockIssueLayerId, sil.QuantityUsed, sil.UnitCost, " +
                    "       sl.StockLayerId, sl.QuantityReceived, sl.QuantityRemaining, " +
                    "       sl.ReceivedDate, sl.IsClosed " +
                    "FROM StockIssueLayers sil " +
                    "INNER JOIN StockLayers sl ON sl.StockLayerId = sil.StockLayerId " +
                    "WHERE sil.StockMovementId = @Id " +
                    "ORDER BY sl.ReceivedDate";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", movementId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            DataRow row = dt.NewRow();
                            row["StockLayerId"] = Convert.ToInt64(dr["StockLayerId"]);
                            row["QuantityReceived"] = dr["QuantityReceived"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityReceived"]);
                            row["QuantityRemaining"] = dr["QuantityRemaining"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityRemaining"]);
                            row["QuantityUsed"] = dr["QuantityUsed"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityUsed"]);
                            row["UnitCost"] = dr["UnitCost"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["UnitCost"]);
                            row["ReceivedDate"] = dr["ReceivedDate"] == DBNull.Value
                                ? DateTime.Now : Convert.ToDateTime(dr["ReceivedDate"]);
                            row["IsClosed"] = dr["IsClosed"] == DBNull.Value
                                ? false : Convert.ToBoolean(dr["IsClosed"]);
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            else if (movementType == 7)
            {
                // TRANSFER: ترکیب هر دو
                layersTitle = "لایه‌های مرتبط با انتقال";

                // OUT از انبار مبدأ
                string sqlOut =
                    "SELECT sil.QuantityUsed, sil.UnitCost, " +
                    "       sl.StockLayerId, sl.QuantityReceived, sl.QuantityRemaining, " +
                    "       sl.ReceivedDate, sl.IsClosed " +
                    "FROM StockIssueLayers sil " +
                    "INNER JOIN StockLayers sl ON sl.StockLayerId = sil.StockLayerId " +
                    "WHERE sil.StockMovementId = @Id";

                using (SqlCommand cmd = new SqlCommand(sqlOut, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", movementId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            DataRow row = dt.NewRow();
                            row["StockLayerId"] = Convert.ToInt64(dr["StockLayerId"]);
                            row["QuantityReceived"] = dr["QuantityReceived"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityReceived"]);
                            row["QuantityRemaining"] = dr["QuantityRemaining"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityRemaining"]);
                            row["QuantityUsed"] = dr["QuantityUsed"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["QuantityUsed"]);
                            row["UnitCost"] = dr["UnitCost"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["UnitCost"]);
                            row["ReceivedDate"] = dr["ReceivedDate"] == DBNull.Value
                                ? DateTime.Now : Convert.ToDateTime(dr["ReceivedDate"]);
                            row["IsClosed"] = dr["IsClosed"] == DBNull.Value
                                ? false : Convert.ToBoolean(dr["IsClosed"]);
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری لایه‌ها: " + ex.Message);
        }

        litLayersTitle.Text = layersTitle;

        if (dt.Rows.Count == 0)
        {
            pnlNoLayers.Visible = true;
            rptLayers.Visible = false;
            rptLayers.DataSource = null;
            rptLayers.DataBind();
        }
        else
        {
            pnlNoLayers.Visible = false;
            rptLayers.Visible = true;
            rptLayers.DataSource = dt;
            rptLayers.DataBind();
        }
    }

    protected void btnCloseModal_Click(object sender, EventArgs e)
    {
        pnlDetailsModal.Visible = false;
        SelectedMovementId = 0;
    }

    // ─────────────────────────────────────────────────────
    // CSV
    // ─────────────────────────────────────────────────────
    private void BuildCsvData(DataTable dt)
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            litCsvData.Text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("شناسه,محصول,کد,انبار,نوع گردش,مقدار,واحد,قیمت واحد,ارزش,مرجع,شماره بچ,تاریخ");

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            DataRow r = dt.Rows[i];
            int mt = r["MovementType"] == DBNull.Value ? 0 : Convert.ToInt32(r["MovementType"]);

            sb.Append(CsvEscape(Convert.ToString(r["StockMovementId"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["ProductName"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["ProductCode"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["WarehouseName"]))).Append(",");
            sb.Append(CsvEscape(GetMovementTypeLabel(mt))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["Quantity"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["UnitName"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["UnitCost"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["TotalCost"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["ReferenceType"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["BatchNumber"]))).Append(",");
            sb.Append(CsvEscape(FormatShamsiDateValue(r["MovementDate"])));
            sb.AppendLine();
        }

        litCsvData.Text = sb.ToString();
    }

    private string CsvEscape(string value)
    {
        if (value == null) return "";
        value = value.Replace("\"", "\"\"");
        if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 ||
            value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
        {
            return "\"" + value + "\"";
        }
        return value;
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

    protected string GetProductIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "pickaxe";
        if (type == 2) return "package-check";
        return "box";
    }

    protected string GetProductAvatarCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";

        if (type == 1) return baseC + " bg-amber-100 text-amber-700";
        if (type == 2) return baseC + " bg-violet-100 text-violet-700";
        return baseC + " bg-emerald-100 text-emerald-700";
    }

    protected string GetMovementTypeLabel(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (type == 1) return "ورود خرید";
        if (type == 2) return "ورود تولید";
        if (type == 3) return "خروج فروش";
        if (type == 4) return "خروج مصرف";
        if (type == 5) return "تعدیل +";
        if (type == 6) return "تعدیل -";
        if (type == 7) return "انتقال";
        return "—";
    }

    protected string GetMovementTypeIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (type == 1) return "shopping-cart";
        if (type == 2) return "hammer";
        if (type == 3) return "shopping-bag";
        if (type == 4) return "flask-conical";
        if (type == 5) return "plus-circle";
        if (type == 6) return "minus-circle";
        if (type == 7) return "arrow-right-left";
        return "activity";
    }

    protected string GetMovementTypeBadgeCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (type == 1) return "badge bg-emerald-50 text-emerald-700";
        if (type == 2) return "badge bg-violet-50 text-violet-700";
        if (type == 3) return "badge bg-brand-50 text-brand-700";
        if (type == 4) return "badge bg-amber-50 text-amber-700";
        if (type == 5) return "badge bg-sky-50 text-sky-700";
        if (type == 6) return "badge bg-rose-50 text-rose-700";
        if (type == 7) return "badge bg-ink-100 text-ink-700";
        return "badge bg-surface-muted text-ink-600";
    }

    protected string GetQuantitySign(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (type == 1 || type == 2 || type == 5) return "+";
        if (type == 3 || type == 4 || type == 6) return "−";
        return "↔";
    }

    protected string GetQuantitySignClass(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 0 : Convert.ToInt32(typeObj);
        if (type == 1 || type == 2 || type == 5) return "font-mono text-[13px] font-bold text-emerald-600";
        if (type == 3 || type == 4 || type == 6) return "font-mono text-[13px] font-bold text-rose-600";
        return "font-mono text-[13px] font-bold text-ink-700";
    }

    protected string GetWarehouseBadgeCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "badge bg-amber-50 text-amber-700";
        return "badge bg-violet-50 text-violet-700";
    }

    protected string RenderReference(object refTypeObj, object refIdObj)
    {
        return RenderReferenceText(
            refTypeObj == null || refTypeObj == DBNull.Value ? "" : Convert.ToString(refTypeObj),
            refIdObj == null || refIdObj == DBNull.Value ? 0 : Convert.ToInt32(refIdObj));
    }

    private string RenderReferenceText(string refType, int refId)
    {
        if (string.IsNullOrEmpty(refType)) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        string label;
        string color;
        if (refType == "PurchaseOrder") { label = "سفارش خرید"; color = "brand"; }
        else if (refType == "ProductionOrder") { label = "دستور تولید"; color = "violet"; }
        else if (refType == "FinalInvoice") { label = "فاکتور نهایی"; color = "emerald"; }
        else if (refType == "Adjustment") { label = "تعدیل دستی"; color = "amber"; }
        else if (refType == "Transfer") { label = "انتقال"; color = "sky"; }
        else { label = refType; color = "ink"; }

        string output = "<span class=\"badge bg-" + color + "-50 text-" + color + "-700\" style=\"font-size:10.5px;\">" +
            HttpUtility.HtmlEncode(label);

        if (refId > 0)
        {
            output += " <span class=\"font-mono text-[10px] opacity-70\">#" + refId.ToString(CultureInfo.InvariantCulture) + "</span>";
        }

        output += "</span>";
        return output;
    }

    protected string GetLayerStatusBadge(object isClosedObj)
    {
        bool isClosed = isClosedObj == null || isClosedObj == DBNull.Value ? false : Convert.ToBoolean(isClosedObj);
        if (isClosed)
        {
            return "<span class=\"badge bg-surface-muted text-ink-500\">بسته‌شده</span>";
        }
        return "<span class=\"badge bg-emerald-50 text-emerald-700\">باز</span>";
    }

    protected string FormatQuantity(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            return HttpUtility.HtmlEncode(FormatQuantityValue(d));
        }
        catch { return "۰"; }
    }

    protected string FormatQuantityOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        return FormatQuantity(value);
    }

    private string FormatQuantityValue(decimal d)
    {
        if (d == 0) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
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

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
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

    private string FormatShamsiDateValue(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(value);
            PersianCalendar pc = new PersianCalendar();
            return string.Format(CultureInfo.InvariantCulture,
                "{0}/{1:D2}/{2:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
        }
        catch { return "—"; }
    }

    protected string FormatTime(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(value);
            return HttpUtility.HtmlEncode(string.Format(CultureInfo.InvariantCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                dt.Hour, dt.Minute, dt.Second));
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