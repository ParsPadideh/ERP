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

public partial class admin_production_orders : System.Web.UI.Page
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
        get { int id; if (int.TryParse(hfEditId.Value, out id)) return id; return 0; }
        set { hfEditId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditMaterialId
    {
        get { int id; if (int.TryParse(hfEditMaterialId.Value, out id)) return id; return 0; }
        set { hfEditMaterialId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int CurrentOrderId
    {
        get { int id; if (int.TryParse(hfCurrentOrderId.Value, out id)) return id; return 0; }
        set { hfCurrentOrderId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            LoadProductDropdown();
            LoadProductFilter();
            LoadMaterialDropdown();
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
        phAddMaterial.Visible = false;

        litPageTitle.Text = "دستورهای تولید";
        litPageSubtitle.Text = "دستورهای تولید محصولات را مدیریت کنید. با تکمیل، انبار خودکار به‌روز می‌شود.";

        LoadStats();
        LoadOrders();
    }

    private void ShowFormView(int orderId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        if (orderId > 0)
        {
            EditId = orderId;
            CurrentOrderId = orderId;
            LoadOrderForEdit(orderId);
            LoadMaterials(orderId);
            pnlMaterialsInline.Visible = true;
            phAddMaterial.Visible = true;
            litFormTitle.Text = "ویرایش دستور تولید";
        }
        else
        {
            EditId = 0;
            CurrentOrderId = 0;
            ClearForm();
            pnlMaterialsInline.Visible = false;
            phAddMaterial.Visible = false;
            litFormTitle.Text = "دستور تولید جدید";
        }
    }

    // ─────────────────────────────────────────────────────
    // Dropdowns
    // ─────────────────────────────────────────────────────
    private void LoadProductDropdown()
    {
        ddlProduct.Items.Clear();
        ddlProduct.Items.Add(new ListItem("— انتخاب محصول نهایی —", ""));

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
                        ddlProduct.Items.Add(new ListItem(name + " (" + code + ")", id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadProductFilter()
    {
        ddlFilterProduct.Items.Clear();
        ddlFilterProduct.Items.Add(new ListItem("همه محصولات", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, ProductName FROM Products " +
                    "WHERE IsDeleted = 0 AND ProductType = 2 " +
                    "ORDER BY ProductName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["ProductName"]);
                        ddlFilterProduct.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch { }
    }

    private void LoadMaterialDropdown()
    {
        ddlMaterial.Items.Clear();
        ddlMaterial.Items.Add(new ListItem("— انتخاب ماده اولیه —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, u.UnitName " +
                    "FROM Products p " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND u.IsDeleted = 0 " +
                    "  AND p.ProductType = 1 ORDER BY p.ProductName", conn))
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

                        ddlMaterial.Items.Add(new ListItem(label, id));
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

                string sql =
                    "SELECT " +
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Total, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND Status = 2 THEN 1 ELSE 0 END) AS InProgress, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM ProductionOrders";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatInProgress.Text = HttpUtility.HtmlEncode(ToInt(dr["InProgress"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
                    }
                }

                DateTime monthStart = GetCurrentPersianMonthStart();
                DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductionOrders " +
                    "WHERE Status = 3 AND IsDeleted = 0 " +
                    "  AND EndDate >= @From AND EndDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    int cnt = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatCompleted.Text = HttpUtility.HtmlEncode(cnt.ToString("N0"));
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری آمار: " + ex.Message); }
    }

    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
    }

    private int ToInt(object o)
    {
        if (o == null || o == DBNull.Value) return 0;
        return Convert.ToInt32(o);
    }

    // ─────────────────────────────────────────────────────
    // Load Orders
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
                sb.Append("SELECT po.ProductionOrderId, po.OrderNumber, po.PlannedQuantity, ");
                sb.Append("       po.ProducedQuantity, po.WasteQuantity, po.Status, ");
                sb.Append("       po.StartDate, po.EndDate, po.CreatedAt, po.IsDeleted, ");
                sb.Append("       po.BOMId, po.ProductId, ");
                sb.Append("       p.ProductCode, p.ProductName, ");
                sb.Append("       b.Version AS BOMVersion ");
                sb.Append("FROM ProductionOrders po ");
                sb.Append("INNER JOIN Products p ON p.ProductId = po.ProductId ");
                sb.Append("LEFT JOIN BOMs b ON b.BOMId = po.BOMId ");
                sb.Append("WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND po.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (po.OrderNumber LIKE @Search OR p.ProductName LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string statusFilter = ddlFilterStatus.SelectedValue;
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        int st;
                        if (int.TryParse(statusFilter, out st) && st > 0)
                        {
                            sb.Append(" AND po.Status = @Status ");
                            cmd.Parameters.AddWithValue("@Status", st);
                        }
                    }

                    string productFilter = ddlFilterProduct.SelectedValue;
                    if (!string.IsNullOrEmpty(productFilter))
                    {
                        int pid;
                        if (int.TryParse(productFilter, out pid) && pid > 0)
                        {
                            sb.Append(" AND po.ProductId = @PId ");
                            cmd.Parameters.AddWithValue("@PId", pid);
                        }
                    }

                    sb.Append(" ORDER BY po.IsDeleted ASC, po.Status ASC, po.ProductionOrderId DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

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

            litListInfo.Text = string.Format("{0} دستور",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadOrders(); }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadOrderForEdit(int orderId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, BOMId, PlannedQuantity, ProducedQuantity, " +
                    "       WasteQuantity, Notes, Status FROM ProductionOrders " +
                    "WHERE ProductionOrderId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ddlProduct.ClearSelection();
                            if (dr["ProductId"] != DBNull.Value)
                            {
                                string pid = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem pItem = ddlProduct.Items.FindByValue(pid);
                                if (pItem != null) pItem.Selected = true;
                            }

                            // Load BOMs for this product
                            int productId = Convert.ToInt32(dr["ProductId"]);
                            LoadBomDropdown(productId);

                            ddlBom.ClearSelection();
                            if (dr["BOMId"] != DBNull.Value)
                            {
                                string bid = Convert.ToInt32(dr["BOMId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem bItem = ddlBom.Items.FindByValue(bid);
                                if (bItem != null) bItem.Selected = true;
                            }

                            txtPlannedQty.Text = dr["PlannedQuantity"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["PlannedQuantity"]).ToString(CultureInfo.InvariantCulture);
                            txtProducedQty.Text = dr["ProducedQuantity"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["ProducedQuantity"]).ToString(CultureInfo.InvariantCulture);
                            txtWasteQty.Text = dr["WasteQuantity"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["WasteQuantity"]).ToString(CultureInfo.InvariantCulture);
                            txtNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);
                        }
                        else { ShowMessage("دستور یافت نشد."); ShowListView(); }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void ddlProduct_SelectedIndexChanged(object sender, EventArgs e)
    {
        int productId;
        if (!string.IsNullOrEmpty(ddlProduct.SelectedValue)
            && int.TryParse(ddlProduct.SelectedValue, out productId) && productId > 0)
        {
            LoadBomDropdown(productId);
        }
    }

    private void LoadBomDropdown(int productId)
    {
        ddlBom.Items.Clear();
        ddlBom.Items.Add(new ListItem("— انتخاب BOM —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT BOMId, Version FROM BOMs " +
                    "WHERE ProductId = @PId AND IsActive = 1 AND IsDeleted = 0 " +
                    "ORDER BY Version DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@PId", productId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string id = Convert.ToInt32(dr["BOMId"]).ToString(CultureInfo.InvariantCulture);
                            int v = Convert.ToInt32(dr["Version"]);
                            ddlBom.Items.Add(new ListItem("نسخه " + v.ToString(CultureInfo.InvariantCulture), id));
                        }
                    }
                }

                if (ddlBom.Items.Count <= 1)
                    ddlBom.Items.Add(new ListItem("— BOM فعالی برای این محصول نیست —", ""));
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Save Order
    // ─────────────────────────────────────────────────────
    protected void btnSaveOrder_Click(object sender, EventArgs e)
    {
        int currentId = EditId;

        int productId;
        if (string.IsNullOrEmpty(ddlProduct.SelectedValue)
            || !int.TryParse(ddlProduct.SelectedValue, out productId) || productId <= 0)
        {
            ShowMessage("محصول نهایی را انتخاب کنید.");
            return;
        }

        int bomId = 0;
        int.TryParse(ddlBom.SelectedValue, out bomId);

        decimal plannedQty = ParseDecimalOrZero(txtPlannedQty.Text);
        if (plannedQty <= 0)
        {
            ShowMessage("مقدار برنامه‌ریزی‌شده باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        decimal producedQty = ParseDecimalOrZero(txtProducedQty.Text);
        decimal wasteQty = ParseDecimalOrZero(txtWasteQty.Text);
        string notes = txtNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE ProductionOrders SET " +
                        "  ProductId = @PId, BOMId = @BId, PlannedQuantity = @Planned, " +
                        "  ProducedQuantity = @Produced, WasteQuantity = @Waste, " +
                        "  Notes = @Notes, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE ProductionOrderId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@PId", productId);
                        cmd.Parameters.AddWithValue("@BId", bomId > 0 ? (object)bomId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Planned", plannedQty);
                        cmd.Parameters.AddWithValue("@Produced", producedQty);
                        cmd.Parameters.AddWithValue("@Waste", wasteQty);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "ProductionOrders", currentId,
                        "Order #" + currentId, "ویرایش دستور تولید");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string orderNumber = GenerateOrderNumber(conn);

                    string insertSql =
                        "INSERT INTO ProductionOrders " +
                        "  (OrderNumber, ProductId, BOMId, PlannedQuantity, ProducedQuantity, " +
                        "   WasteQuantity, Status, TotalMaterialCost, TotalLaborCost, " +
                        "   TotalOverheadCost, TotalCost, UnitCost, Notes, " +
                        "   IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@Number, @PId, @BId, @Planned, @Produced, " +
                        "   @Waste, 1, 0, 0, 0, 0, 0, @Notes, " +
                        "   0, GETDATE(), @By); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Number", orderNumber);
                        cmd.Parameters.AddWithValue("@PId", productId);
                        cmd.Parameters.AddWithValue("@BId", bomId > 0 ? (object)bomId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Planned", plannedQty);
                        cmd.Parameters.AddWithValue("@Produced", producedQty);
                        cmd.Parameters.AddWithValue("@Waste", wasteQty);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "ProductionOrders", newId, orderNumber, "ایجاد دستور تولید");

                    // اگر BOM انتخاب شده، به Materials منتقل کن
                    if (bomId > 0 && newId > 0)
                    {
                        AutoFillMaterialsFromBom(conn, newId, bomId, plannedQty);
                    }

                    ShowMessage("دستور تولید ایجاد شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private string GenerateOrderNumber(SqlConnection conn)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "PRD-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(OrderNumber, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
            "FROM ProductionOrders WHERE OrderNumber LIKE @Prefix + '%'", conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + seq.ToString("D4");
        }
    }

    private void AutoFillMaterialsFromBom(SqlConnection conn, int orderId, int bomId, decimal multiplier)
    {
        try
        {
            string sql =
                "INSERT INTO ProductionOrderMaterials " +
                "  (ProductionOrderId, MaterialId, PlannedQuantity, ActualQuantity, UnitCost, TotalCost) " +
                "SELECT @OrderId, bi.MaterialId, " +
                "       bi.Quantity * @Mult * (1 + bi.WastePercent / 100.0), " +
                "       0, 0, 0 " +
                "FROM BOMItems bi " +
                "WHERE bi.BOMId = @BomId " +
                "  AND NOT EXISTS (SELECT 1 FROM ProductionOrderMaterials pom " +
                "                  WHERE pom.ProductionOrderId = @OrderId " +
                "                    AND pom.MaterialId = bi.MaterialId)";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@OrderId", orderId);
                cmd.Parameters.AddWithValue("@BomId", bomId);
                cmd.Parameters.AddWithValue("@Mult", multiplier);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Materials
    // ─────────────────────────────────────────────────────
    private void LoadMaterials(int orderId)
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT pom.ProductionOrderMaterialId, pom.MaterialId, " +
                    "  pom.PlannedQuantity, pom.ActualQuantity, pom.UnitCost, pom.TotalCost, " +
                    "  p.ProductCode AS MaterialCode, p.ProductName AS MaterialName, " +
                    "  po.Status AS OrderStatus " +
                    "FROM ProductionOrderMaterials pom " +
                    "INNER JOIN Products p ON p.ProductId = pom.MaterialId " +
                    "INNER JOIN ProductionOrders po ON po.ProductionOrderId = pom.ProductionOrderId " +
                    "WHERE pom.ProductionOrderId = @Id " +
                    "ORDER BY p.ProductName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlMaterialsEmpty.Visible = true;
                rptMaterials.Visible = false;
                rptMaterials.DataSource = null;
                rptMaterials.DataBind();
            }
            else
            {
                pnlMaterialsEmpty.Visible = false;
                rptMaterials.Visible = true;
                rptMaterials.DataSource = dt;
                rptMaterials.DataBind();
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری مواد: " + ex.Message); }
    }

    protected void btnShowAddMaterialForm_Click(object sender, EventArgs e)
    {
        ClearMaterialForm();
        EditMaterialId = 0;
        litMaterialFormTitle.Text = "افزودن ماده اولیه";
        pnlMaterialForm.Visible = true;
    }

    protected void btnCancelMaterialForm_Click(object sender, EventArgs e)
    {
        ClearMaterialForm();
        EditMaterialId = 0;
        pnlMaterialForm.Visible = false;
    }

    protected void btnSaveMaterial_Click(object sender, EventArgs e)
    {
        int orderId = CurrentOrderId;
        if (orderId <= 0) { ShowMessage("دستور معتبر نیست."); return; }

        int materialId;
        if (string.IsNullOrEmpty(ddlMaterial.SelectedValue)
            || !int.TryParse(ddlMaterial.SelectedValue, out materialId) || materialId <= 0)
        {
            ShowMessage("ماده اولیه را انتخاب کنید.");
            return;
        }

        decimal plannedQty = ParseDecimalOrZero(txtMaterialPlannedQty.Text);
        if (plannedQty <= 0)
        {
            ShowMessage("مقدار برنامه‌ریزی‌شده باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int currentId = EditMaterialId;

                // چک تکراری
                string dupSql =
                    "SELECT COUNT(*) FROM ProductionOrderMaterials " +
                    "WHERE ProductionOrderId = @OrderId AND MaterialId = @MaterialId";
                if (currentId > 0) dupSql += " AND ProductionOrderMaterialId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@OrderId", orderId);
                    dup.Parameters.AddWithValue("@MaterialId", materialId);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("این ماده قبلاً اضافه شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE ProductionOrderMaterials SET " +
                        "  MaterialId = @MaterialId, PlannedQuantity = @Qty " +
                        "WHERE ProductionOrderMaterialId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@MaterialId", materialId);
                        cmd.Parameters.AddWithValue("@Qty", plannedQty);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }
                    ShowMessage("ماده ویرایش شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO ProductionOrderMaterials " +
                        "  (ProductionOrderId, MaterialId, PlannedQuantity, ActualQuantity, UnitCost, TotalCost) " +
                        "VALUES (@OrderId, @MaterialId, @Qty, 0, 0, 0)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        cmd.Parameters.AddWithValue("@MaterialId", materialId);
                        cmd.Parameters.AddWithValue("@Qty", plannedQty);
                        cmd.ExecuteNonQuery();
                    }
                    ShowMessage("ماده اضافه شد.");
                }
            }

            ClearMaterialForm();
            EditMaterialId = 0;
            pnlMaterialForm.Visible = false;
            LoadMaterials(orderId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnAutoFillFromBom_Click(object sender, EventArgs e)
    {
        int orderId = CurrentOrderId;
        if (orderId <= 0) return;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int bomId = 0;
                decimal plannedQty = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(BOMId, 0) AS BOMId, PlannedQuantity " +
                    "FROM ProductionOrders WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            bomId = Convert.ToInt32(dr["BOMId"]);
                            plannedQty = dr["PlannedQuantity"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["PlannedQuantity"]);
                        }
                    }
                }

                if (bomId <= 0 || plannedQty <= 0)
                {
                    ShowMessage("BOM یا مقدار برنامه‌ریزی‌شده تعریف نشده است.");
                    return;
                }

                AutoFillMaterialsFromBom(conn, orderId, bomId, plannedQty);
                ShowMessage("مواد از BOM پر شد.");
            }

            LoadMaterials(orderId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void rptMaterials_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0) return;

        if (e.CommandName == "EditMaterial") LoadMaterialForEdit(itemId);
        else if (e.CommandName == "DeleteMaterial") DeleteMaterial(itemId);
    }

    private void LoadMaterialForEdit(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MaterialId, PlannedQuantity FROM ProductionOrderMaterials " +
                    "WHERE ProductionOrderMaterialId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditMaterialId = itemId;

                            ddlMaterial.ClearSelection();
                            string mid = Convert.ToInt32(dr["MaterialId"]).ToString(CultureInfo.InvariantCulture);
                            ListItem item = ddlMaterial.Items.FindByValue(mid);
                            if (item != null) item.Selected = true;

                            txtMaterialPlannedQty.Text = dr["PlannedQuantity"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["PlannedQuantity"]).ToString(CultureInfo.InvariantCulture);

                            litMaterialFormTitle.Text = "ویرایش ماده اولیه";
                            pnlMaterialForm.Visible = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void DeleteMaterial(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ProductionOrderMaterials WHERE ProductionOrderMaterialId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }
            }

            ShowMessage("ماده حذف شد.");
            LoadMaterials(CurrentOrderId);
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Order Commands
    // ─────────────────────────────────────────────────────
    protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "Start") StartOrder(id);
        else if (e.CommandName == "Complete") CompleteOrder(id);
        else if (e.CommandName == "CancelOrder") CancelOrder(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreOrder(id);
    }

    private void StartOrder(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT OrderNumber, Status FROM ProductionOrders " +
                    "WHERE ProductionOrderId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["OrderNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                        }
                    }
                }

                if (status != 1) { ShowMessage("این دستور قابل شروع نیست."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductionOrders SET Status = 2, StartDate = GETDATE(), " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Start", "ProductionOrders", id, number, "شروع دستور تولید");
            }

            ShowMessage("دستور به حالت «در حال اجرا» درآمد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void CancelOrder(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = 0;
                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT OrderNumber, Status FROM ProductionOrders " +
                    "WHERE ProductionOrderId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["OrderNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                        }
                    }
                }

                if (status == 3) { ShowMessage("دستور تکمیل‌شده قابل لغو نیست."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductionOrders SET Status = 4, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Cancel", "ProductionOrders", id, number, "لغو دستور تولید");
            }

            ShowMessage("دستور لغو شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Complete Order — FIFO consumption + FG production
    // ═══════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════
    // Complete Order — FIFO consumption + FG production
    // ═══════════════════════════════════════════════════════
    private void CompleteOrder(int orderId)
    {
        try
        {
            int productId = 0;
            decimal plannedQty = 0m;
            decimal producedQty = 0m;
            int status = 0;
            string orderNumber = "";
            int rmWarehouseId = 0;
            int fgWarehouseId = 0;
            decimal totalMaterialCost = 0m;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ═══ Load order ═══
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT OrderNumber, ProductId, PlannedQuantity, ProducedQuantity, Status " +
                    "FROM ProductionOrders WHERE ProductionOrderId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("دستور یافت نشد."); return; }
                        orderNumber = Convert.ToString(dr["OrderNumber"]);
                        productId = Convert.ToInt32(dr["ProductId"]);
                        plannedQty = dr["PlannedQuantity"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["PlannedQuantity"]);
                        producedQty = dr["ProducedQuantity"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ProducedQuantity"]);
                        status = Convert.ToInt32(dr["Status"]);
                    }
                }

                if (status != 2) { ShowMessage("فقط دستور در حال اجرا قابل تکمیل است."); return; }

                // اگر ProducedQty = 0 → معادل Planned استفاده کن
                if (producedQty <= 0m) producedQty = plannedQty;
                if (producedQty <= 0m) { ShowMessage("مقدار تولیدشده معتبر نیست."); return; }

                // ═══ پیدا کردن انبار RM و FG ═══
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 WarehouseId FROM Warehouses " +
                    "WHERE WarehouseType = 1 AND IsActive = 1 AND IsDeleted = 0 " +
                    "ORDER BY WarehouseId", conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) { ShowMessage("انبار مواد اولیه یافت نشد."); return; }
                    rmWarehouseId = Convert.ToInt32(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 WarehouseId FROM Warehouses " +
                    "WHERE WarehouseType = 2 AND IsActive = 1 AND IsDeleted = 0 " +
                    "ORDER BY WarehouseId", conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) { ShowMessage("انبار محصول نهایی یافت نشد."); return; }
                    fgWarehouseId = Convert.ToInt32(o);
                }

                // ═══ Load materials ═══
                DataTable dtMaterials = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductionOrderMaterialId, MaterialId, PlannedQuantity " +
                    "FROM ProductionOrderMaterials WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dtMaterials);
                }

                if (dtMaterials.Rows.Count == 0)
                {
                    ShowMessage("هیچ ماده اولیه‌ای برای این دستور ثبت نشده است.");
                    return;
                }

                // ═══ Transaction ═══
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ═══ 1) مصرف مواد اولیه از RM با FIFO ═══
                        foreach (DataRow m in dtMaterials.Rows)
                        {
                            int pomId = Convert.ToInt32(m["ProductionOrderMaterialId"]);
                            int materialId = Convert.ToInt32(m["MaterialId"]);
                            decimal requiredQty = Convert.ToDecimal(m["PlannedQuantity"]);

                            decimal consumedCost = ConsumeFifo(
                                conn, tran, rmWarehouseId, materialId, requiredQty, orderId, orderNumber);

                            // Update material row
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE ProductionOrderMaterials SET " +
                                "  ActualQuantity = @ActualQty, UnitCost = @UnitCost, TotalCost = @TotalCost " +
                                "WHERE ProductionOrderMaterialId = @Id", conn, tran))
                            {
                                decimal materialUnitCost = requiredQty > 0
                                    ? Math.Round(consumedCost / requiredQty, 2)
                                    : 0m;
                                cmd.Parameters.AddWithValue("@ActualQty", requiredQty);
                                cmd.Parameters.AddWithValue("@UnitCost", materialUnitCost);
                                cmd.Parameters.AddWithValue("@TotalCost", consumedCost);
                                cmd.Parameters.AddWithValue("@Id", pomId);
                                cmd.ExecuteNonQuery();
                            }

                            totalMaterialCost += consumedCost;
                        }

                        // ═══ 2) تولید FG (IN) ═══
                        decimal finishedUnitCost = producedQty > 0
                            ? Math.Round(totalMaterialCost / producedQty, 2)
                            : 0m;

                        int movementId = 0;
                        using (SqlCommand cmd = new SqlCommand(
                            "INSERT INTO StockMovements " +
                            "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
                            "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
                            "VALUES (@WhId, @PId, 2, @Qty, @UnitCost, @TotalCost, " +
                            "   GETDATE(), 'ProductionOrder', @RefId, @Notes, GETDATE(), @By); " +
                            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@WhId", fgWarehouseId);
                            cmd.Parameters.AddWithValue("@PId", productId);
                            cmd.Parameters.AddWithValue("@Qty", producedQty);
                            cmd.Parameters.AddWithValue("@UnitCost", finishedUnitCost);
                            cmd.Parameters.AddWithValue("@TotalCost", totalMaterialCost);
                            cmd.Parameters.AddWithValue("@RefId", orderId);
                            cmd.Parameters.AddWithValue("@Notes", "تولید محصول " + orderNumber);
                            cmd.Parameters.AddWithValue("@By",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                            object r = cmd.ExecuteScalar();
                            if (r != null && r != DBNull.Value)
                                movementId = Convert.ToInt32(r);
                        }

                        if (movementId > 0)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO StockLayers " +
                                "  (WarehouseId, ProductId, StockMovementId, QuantityReceived, QuantityRemaining, " +
                                "   UnitCost, ReceivedDate, IsClosed) " +
                                "VALUES (@WhId, @PId, @MovId, @Qty, @Qty, @UnitCost, GETDATE(), 0)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@WhId", fgWarehouseId);
                                cmd.Parameters.AddWithValue("@PId", productId);
                                cmd.Parameters.AddWithValue("@MovId", movementId);
                                cmd.Parameters.AddWithValue("@Qty", producedQty);
                                cmd.Parameters.AddWithValue("@UnitCost", finishedUnitCost);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ═══ 3) Update Order ═══
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProductionOrders SET " +
                            "  Status = 3, ProducedQuantity = @Produced, EndDate = GETDATE(), " +
                            "  TotalMaterialCost = @MatCost, TotalCost = @TotalCost, UnitCost = @UnitCost, " +
                            "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ProductionOrderId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Produced", producedQty);
                            cmd.Parameters.AddWithValue("@MatCost", totalMaterialCost);
                            cmd.Parameters.AddWithValue("@TotalCost", totalMaterialCost);
                            cmd.Parameters.AddWithValue("@UnitCost", finishedUnitCost);
                            cmd.Parameters.AddWithValue("@By",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Complete", "ProductionOrders", orderId,
                            orderNumber, "تکمیل دستور تولید");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }

            ShowMessage("دستور با موفقیت تکمیل شد. انبار به‌روز شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تکمیل: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════
    // FIFO consumption helper
    // ═══════════════════════════════════════════════════════
    private decimal ConsumeFifo(SqlConnection conn, SqlTransaction tran,
        int warehouseId, int productId, decimal quantity, int orderId, string orderNumber)
    {
        decimal totalCost = 0m;
        decimal remaining = quantity;

        // خواندن لایه‌های FIFO
        DataTable layers = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            "SELECT StockLayerId, QuantityRemaining, UnitCost " +
            "FROM StockLayers " +
            "WHERE WarehouseId = @WhId AND ProductId = @PId " +
            "  AND IsClosed = 0 AND QuantityRemaining > 0 " +
            "ORDER BY ReceivedDate ASC, StockLayerId ASC", conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(layers);
        }

        decimal availableTotal = 0m;
        foreach (DataRow r in layers.Rows)
            availableTotal += Convert.ToDecimal(r["QuantityRemaining"]);

        if (availableTotal < quantity)
            throw new Exception("موجودی ماده کافی نیست. نیاز: " + quantity.ToString("N2") +
                " — موجود: " + availableTotal.ToString("N2"));

        // ایجاد StockMovement OUT
        int movementId = 0;
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO StockMovements " +
            "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
            "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
            "VALUES (@WhId, @PId, 4, @Qty, 0, 0, " +
            "   GETDATE(), 'ProductionOrder', @RefId, @Notes, GETDATE(), @By); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
        {
            cmd.Parameters.AddWithValue("@WhId", warehouseId);
            cmd.Parameters.AddWithValue("@PId", productId);
            cmd.Parameters.AddWithValue("@Qty", quantity);
            cmd.Parameters.AddWithValue("@RefId", orderId);
            cmd.Parameters.AddWithValue("@Notes", "مصرف در تولید " + orderNumber);
            cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
            object r = cmd.ExecuteScalar();
            if (r != null && r != DBNull.Value) movementId = Convert.ToInt32(r);
        }

        // مصرف لایه‌ها
        foreach (DataRow layer in layers.Rows)
        {
            if (remaining <= 0m) break;

            int layerId = Convert.ToInt32(layer["StockLayerId"]);
            decimal layerRemaining = Convert.ToDecimal(layer["QuantityRemaining"]);
            decimal layerUnitCost = Convert.ToDecimal(layer["UnitCost"]);

            decimal useQty = Math.Min(remaining, layerRemaining);
            decimal layerCost = useQty * layerUnitCost;
            totalCost += layerCost;

            // Update layer
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE StockLayers SET QuantityRemaining = QuantityRemaining - @Qty, " +
                "  IsClosed = CASE WHEN QuantityRemaining - @Qty <= 0 THEN 1 ELSE 0 END " +
                "WHERE StockLayerId = @Id", conn, tran))
            {
                cmd.Parameters.AddWithValue("@Qty", useQty);
                cmd.Parameters.AddWithValue("@Id", layerId);
                cmd.ExecuteNonQuery();
            }

            // Insert StockIssueLayer
            if (movementId > 0)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO StockIssueLayers " +
                    "  (StockMovementId, StockLayerId, QuantityUsed, UnitCost, TotalCost) " +
                    "VALUES (@MovId, @LayerId, @Qty, @UnitCost, @TotalCost)", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MovId", movementId);
                    cmd.Parameters.AddWithValue("@LayerId", layerId);
                    cmd.Parameters.AddWithValue("@Qty", useQty);
                    cmd.Parameters.AddWithValue("@UnitCost", layerUnitCost);
                    cmd.Parameters.AddWithValue("@TotalCost", layerCost);
                    cmd.ExecuteNonQuery();
                }
            }

            remaining -= useQty;
        }

        // Update movement UnitCost + TotalCost
        if (movementId > 0)
        {
            decimal avgCost = quantity > 0 ? Math.Round(totalCost / quantity, 2) : 0m;
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE StockMovements SET UnitCost = @UnitCost, TotalCost = @TotalCost " +
                "WHERE StockMovementId = @Id", conn, tran))
            {
                cmd.Parameters.AddWithValue("@UnitCost", avgCost);
                cmd.Parameters.AddWithValue("@TotalCost", totalCost);
                cmd.Parameters.AddWithValue("@Id", movementId);
                cmd.ExecuteNonQuery();
            }
        }

        return totalCost;
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete — فقط پیش‌نویس و لغو‌شده
    // ═══════════════════════════════════════════════════════
    private void SoftDelete(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string number = "";
                int status = 0;
                using (SqlCommand get = new SqlCommand(
                    "SELECT OrderNumber, Status FROM ProductionOrders " +
                    "WHERE ProductionOrderId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            number = Convert.ToString(dr["OrderNumber"]);
                            status = Convert.ToInt32(dr["Status"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(number)) { ShowMessage("دستور یافت نشد."); return; }

                if (status == 3)
                {
                    ShowMessage("دستور تکمیل‌شده روی انبار اثر داشته و قابل حذف نیست.");
                    return;
                }

                if (status == 2)
                {
                    ShowMessage("دستور در حال اجرا قابل حذف نیست. ابتدا آن را لغو یا تکمیل کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductionOrders SET IsDeleted = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "ProductionOrders", id, number, "حذف نرم دستور تولید");
            }

            ShowMessage("دستور حذف شد (قابل بازیابی).");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    private void RestoreOrder(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string number = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT OrderNumber FROM ProductionOrders " +
                    "WHERE ProductionOrderId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) number = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(number)) { ShowMessage("دستور حذف‌شده یافت نشد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductionOrders SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE ProductionOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "ProductionOrders", id, number, "بازیابی دستور تولید");
            }

            ShowMessage("دستور بازیابی شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex) { ShowMessage("خطا در بازیابی: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Form Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        EditId = 0;
        CurrentOrderId = 0;
        EditMaterialId = 0;
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
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())",
                conn, tran))
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

    protected bool IsStatus(object statusObj, int target)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == target;
    }

    protected string FormatCount(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try { return Convert.ToInt32(value).ToString("N0"); }
        catch { return "۰"; }
    }

    protected string FormatQty(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0m) return "۰";
            if (d == Math.Floor(d))
                return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
            return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "۰"; }
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

    protected string GetRowCss(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        if (s == 2)
            return "border-b border-surface-line bg-amber-50/40 hover:bg-amber-50/70 transition";
        if (s == 4)
            return "border-b border-surface-line opacity-60";
        return "border-b border-surface-line transition hover:bg-surface-body/70";
    }

    protected string GetStatusBadge(object statusObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";

        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-surface-muted text-ink-600\">پیش‌نویس</span>";
        if (s == 2) return "<span class=\"badge bg-amber-100 text-amber-800\"><i class=\"h-3 w-3\" data-lucide=\"play-circle\"></i> در حال اجرا</span>";
        if (s == 3) return "<span class=\"badge bg-emerald-100 text-emerald-800\"><i class=\"h-3 w-3\" data-lucide=\"check-circle-2\"></i> تکمیل‌شده</span>";
        if (s == 4) return "<span class=\"badge bg-danger-50 text-danger-600\">لغو‌شده</span>";
        return "";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetBomBadge(object versionObj, object bomIdObj)
    {
        if (bomIdObj == null || bomIdObj == DBNull.Value)
            return "<span class=\"text-[11px] text-ink-400\">—</span>";

        if (versionObj == null || versionObj == DBNull.Value)
            return "<span class=\"text-[11px] text-ink-400\">—</span>";

        int v = Convert.ToInt32(versionObj);
        return "<span class=\"badge bg-violet-50 text-violet-700 font-mono\">v" +
            v.ToString(CultureInfo.InvariantCulture) + "</span>";
    }

    protected string GetActualQtyHint(object producedObj)
    {
        if (producedObj == null || producedObj == DBNull.Value) return "";
        decimal d = Convert.ToDecimal(producedObj);
        if (d <= 0m) return " <span class=\"text-ink-300\">→ ?</span>";
        return " <span class=\"text-ink-400\">→</span> <span class=\"text-emerald-700 font-bold\">" +
            FormatQty(d) + "</span>";
    }

    protected string GetDateRangeHint(object startObj, object endObj)
    {
        if (startObj == null || startObj == DBNull.Value) return "";

        StringBuilder sb = new StringBuilder();
        sb.Append("<p class=\"mt-0.5 text-[10.5px] text-ink-400\">");

        DateTime start = Convert.ToDateTime(startObj);
        PersianCalendar pc = new PersianCalendar();
        sb.Append("شروع: ").Append(pc.GetYear(start).ToString(CultureInfo.InvariantCulture))
          .Append("/").Append(pc.GetMonth(start).ToString("D2", CultureInfo.InvariantCulture))
          .Append("/").Append(pc.GetDayOfMonth(start).ToString("D2", CultureInfo.InvariantCulture));

        if (endObj != null && endObj != DBNull.Value)
        {
            DateTime end = Convert.ToDateTime(endObj);
            sb.Append(" — پایان: ").Append(pc.GetYear(end).ToString(CultureInfo.InvariantCulture))
              .Append("/").Append(pc.GetMonth(end).ToString("D2", CultureInfo.InvariantCulture))
              .Append("/").Append(pc.GetDayOfMonth(end).ToString("D2", CultureInfo.InvariantCulture));
        }

        sb.Append("</p>");
        return sb.ToString();
    }

    protected bool CanEdit(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected bool CanEditOrder(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2;
    }

    protected bool CanCancel(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2;
    }

    protected bool CanSoftDelete(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1 || s == 4;
    }

    protected bool CanEditMaterial(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 1 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2;
    }

    protected string GetStartConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دستور «" + num + "» شروع شود؟');";
    }

    protected string GetCompleteConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دستور «" + num + "» تکمیل شود؟\\n\\nتوجه: مواد اولیه از انبار مصرف و محصول به انبار اضافه می‌شود. این عملیات قابل بازگشت نیست.');";
    }

    protected string GetCancelConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دستور «" + num + "» لغو شود؟');";
    }

    protected string GetDeleteConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف دستور «" + num + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object numberObj)
    {
        string num = numberObj == null || numberObj == DBNull.Value ? "" : numberObj.ToString().Replace("'", "");
        return "return confirm('دستور «" + num + "» بازیابی شود؟');";
    }

    protected string GetDeleteMaterialConfirm(object materialNameObj)
    {
        string name = (materialNameObj == null || materialNameObj == DBNull.Value)
            ? "" : materialNameObj.ToString().Replace("'", "");
        return "return confirm('ماده «" + name + "» حذف شود؟');";
    }

    // ─────────────────────────────────────────────────────
    // Clear Forms
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        if (ddlProduct.Items.Count > 0) ddlProduct.SelectedIndex = 0;
        if (ddlBom.Items.Count > 0) ddlBom.Items.Clear();

        txtPlannedQty.Text = "";
        txtProducedQty.Text = "0";
        txtWasteQty.Text = "0";
        txtNotes.Text = "";
    }

    private void ClearMaterialForm()
    {
        if (ddlMaterial.Items.Count > 0) ddlMaterial.SelectedIndex = 0;
        txtMaterialPlannedQty.Text = "";
    }

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