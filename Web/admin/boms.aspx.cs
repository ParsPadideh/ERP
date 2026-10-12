using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_boms : System.Web.UI.Page
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

    private int EditBomId
    {
        get { int id; if (int.TryParse(hfEditBomId.Value, out id)) return id; return 0; }
        set { hfEditBomId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditItemId
    {
        get { int id; if (int.TryParse(hfEditItemId.Value, out id)) return id; return 0; }
        set { hfEditItemId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int CurrentBomId
    {
        get { int id; if (int.TryParse(hfCurrentBomId.Value, out id)) return id; return 0; }
        set { hfCurrentBomId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            LoadProductDropdown();
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
        pnlItemsView.Visible = false;

        phBackToList.Visible = false;
        phAddBom.Visible = true;
        phAddItem.Visible = false;

        litPageTitle.Text = "فرمول ساخت محصولات";
        litPageSubtitle.Text = "فرمول ساخت (BOM) هر محصول نهایی را تعریف و مدیریت کنید.";

        LoadStats();
        LoadBoms();
    }

    private void ShowFormView(int bomId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        pnlItemsView.Visible = false;

        phBackToList.Visible = true;
        phAddBom.Visible = false;
        phAddItem.Visible = false;

        if (bomId > 0)
        {
            EditBomId = bomId;
            LoadBomForEdit(bomId);
            litFormTitle.Text = "ویرایش BOM";
        }
        else
        {
            EditBomId = 0;
            ClearBomForm();
            litFormTitle.Text = "افزودن BOM جدید";
        }
    }

    private void ShowItemsView(int bomId)
    {
        CurrentBomId = bomId;

        pnlListView.Visible = false;
        pnlFormView.Visible = false;
        pnlItemsView.Visible = true;

        phBackToList.Visible = true;
        phAddBom.Visible = false;
        phAddItem.Visible = true;

        litPageTitle.Text = "مدیریت اقلام BOM";
        litPageSubtitle.Text = "مواد اولیه و مقادیر مصرفی این BOM را مدیریت کنید.";

        LoadCurrentBomInfo();
        LoadItems();
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
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Total, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM BOMs";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveCount"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
                    }
                }

                // Total items
                int itemCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BOMItems", conn))
                {
                    object o = cmd.ExecuteScalar();
                    itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litStatItems.Text = HttpUtility.HtmlEncode(itemCount.ToString("N0"));
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
    // Load BOMs
    // ─────────────────────────────────────────────────────
    private void LoadBoms()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT b.BOMId, b.ProductId, b.Version, b.IsActive, b.IsDeleted, ");
                sb.Append("  b.Notes, b.CreatedAt, ");
                sb.Append("  p.ProductCode, p.ProductName, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM BOMItems bi ");
                sb.Append("          WHERE bi.BOMId = b.BOMId), 0) AS ItemCount, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM ProductionOrders po ");
                sb.Append("          WHERE po.BOMId = b.BOMId AND po.IsDeleted = 0), 0) AS UsageCount ");
                sb.Append("FROM BOMs b ");
                sb.Append("INNER JOIN Products p ON p.ProductId = b.ProductId ");
                sb.Append("WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND b.IsDeleted = 0 ");

                    string search = txtSearch.Text.Trim();
                    if (search.Length > 0)
                    {
                        sb.Append(" AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search) ");
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    }

                    string activeFilter = ddlFilterActive.SelectedValue;
                    if (!string.IsNullOrEmpty(activeFilter))
                    {
                        sb.Append(" AND b.IsActive = @IsActive ");
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
                    }

                    sb.Append(" ORDER BY b.IsDeleted ASC, b.IsActive DESC, p.ProductName, b.Version DESC");
                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptBoms.Visible = false;
                rptBoms.DataSource = null;
                rptBoms.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptBoms.Visible = true;
                rptBoms.DataSource = dt;
                rptBoms.DataBind();
            }

            litListInfo.Text = string.Format("{0} BOM",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadBoms(); }

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

                if (ddlProduct.Items.Count <= 1)
                {
                    ddlProduct.Items.Add(new ListItem(
                        "— هیچ محصول نهایی فعالی وجود ندارد —", ""));
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری محصولات: " + ex.Message); }
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
                    "  AND p.ProductType = 1 " +
                    "ORDER BY p.ProductName", conn))
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
    // Load BOM for Edit
    // ─────────────────────────────────────────────────────
    private void LoadBomForEdit(int bomId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId, Version, IsActive, Notes " +
                    "FROM BOMs WHERE BOMId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", bomId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ddlProduct.ClearSelection();
                            if (dr["ProductId"] != DBNull.Value)
                            {
                                string pid = Convert.ToInt32(dr["ProductId"])
                                    .ToString(CultureInfo.InvariantCulture);
                                ListItem item = ddlProduct.Items.FindByValue(pid);
                                if (item != null) item.Selected = true;
                            }

                            txtVersion.Text = dr["Version"] == DBNull.Value
                                ? "1" : Convert.ToInt32(dr["Version"]).ToString(CultureInfo.InvariantCulture);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                            txtBomNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);
                        }
                        else { ShowMessage("BOM یافت نشد."); ShowListView(); }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Save BOM
    // ─────────────────────────────────────────────────────
    protected void btnSaveBom_Click(object sender, EventArgs e)
    {
        int currentId = EditBomId;

        int productId;
        if (string.IsNullOrEmpty(ddlProduct.SelectedValue)
            || !int.TryParse(ddlProduct.SelectedValue, out productId)
            || productId <= 0)
        {
            ShowMessage("محصول نهایی را انتخاب کنید.");
            return;
        }

        int version = 1;
        if (!int.TryParse(txtVersion.Text.Trim(), out version) || version <= 0)
        {
            ShowMessage("نسخه باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        bool isActive = chkIsActive.Checked;
        string notes = txtBomNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری (همان محصول + همان نسخه)
                string dupSql = "SELECT COUNT(*) FROM BOMs " +
                    "WHERE ProductId = @ProductId AND Version = @Version AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND BOMId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@ProductId", productId);
                    dup.Parameters.AddWithValue("@Version", version);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("BOM با این محصول و نسخه قبلاً ثبت شده است.");
                        return;
                    }
                }

                // اگر BOM فعال است و می‌خواهیم فعال شود، بقیه BOM های این محصول غیرفعال شوند
                if (isActive)
                {
                    string unsetSql = "UPDATE BOMs SET IsActive = 0 " +
                        "WHERE ProductId = @ProductId AND IsDeleted = 0";
                    if (currentId > 0) unsetSql += " AND BOMId <> @Id";

                    using (SqlCommand unset = new SqlCommand(unsetSql, conn))
                    {
                        unset.Parameters.AddWithValue("@ProductId", productId);
                        if (currentId > 0) unset.Parameters.AddWithValue("@Id", currentId);
                        unset.ExecuteNonQuery();
                    }
                }

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE BOMs SET " +
                        "  ProductId = @ProductId, Version = @Version, " +
                        "  IsActive = @IsActive, Notes = @Notes, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE BOMId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Version", version);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "BOMs", currentId,
                        "BOM #" + currentId, "ویرایش BOM");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO BOMs " +
                        "  (ProductId, Version, IsActive, IsDeleted, Notes, CreatedAt, CreatedBy) " +
                        "VALUES (@ProductId, @Version, @IsActive, 0, @Notes, GETDATE(), @By); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Version", version);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "BOMs", newId, "BOM جدید", "ایجاد BOM");

                    ShowMessage("BOM جدید افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Load Current BOM Info
    // ─────────────────────────────────────────────────────
    private void LoadCurrentBomInfo()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT b.Version, b.IsActive, p.ProductName, p.ProductCode, " +
                    "  ISNULL((SELECT COUNT(*) FROM BOMItems bi WHERE bi.BOMId = b.BOMId), 0) AS ItemCount " +
                    "FROM BOMs b " +
                    "INNER JOIN Products p ON p.ProductId = b.ProductId " +
                    "WHERE b.BOMId = @Id AND b.IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentBomId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string productName = Convert.ToString(dr["ProductName"]);
                            string productCode = Convert.ToString(dr["ProductCode"]);
                            int version = Convert.ToInt32(dr["Version"]);
                            bool isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            int itemCount = Convert.ToInt32(dr["ItemCount"]);

                            litCurrentBomProduct.Text = HttpUtility.HtmlEncode(productName + " (" + productCode + ")");
                            litCurrentBomVersion.Text =
                                "<span class=\"badge bg-violet-50 text-violet-700 font-mono\">نسخه " +
                                version.ToString(CultureInfo.InvariantCulture) + "</span>" +
                                (isActive
                                    ? " <span class=\"badge bg-emerald-50 text-emerald-700\">فعال</span>"
                                    : " <span class=\"badge bg-surface-muted text-ink-500\">غیرفعال</span>");
                            litCurrentBomItems.Text =
                                "<span class=\"badge bg-brand-50 text-brand-700\"><i class=\"h-3 w-3\" data-lucide=\"layers\"></i> " +
                                itemCount.ToString(CultureInfo.InvariantCulture) + " ماده اولیه</span>";
                        }
                        else
                        {
                            ShowMessage("BOM یافت نشد یا حذف شده است.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Load Items
    // ─────────────────────────────────────────────────────
    private void LoadItems()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT bi.BOMItemId, bi.MaterialId, bi.Quantity, bi.WastePercent, bi.Notes, " +
                    "       p.ProductCode AS MaterialCode, p.ProductName AS MaterialName, " +
                    "       u.UnitName " +
                    "FROM BOMItems bi " +
                    "INNER JOIN Products p ON p.ProductId = bi.MaterialId " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE bi.BOMId = @Id " +
                    "ORDER BY p.ProductName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentBomId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
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

            litItemsInfo.Text = string.Format("{0} قلم",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری اقلام: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Item Form
    // ─────────────────────────────────────────────────────
    protected void btnShowAddItemForm_Click(object sender, EventArgs e)
    {
        ClearItemForm();
        EditItemId = 0;
        litItemFormTitle.Text = "افزودن ماده اولیه";
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
        int currentId = EditItemId;

        int materialId;
        if (string.IsNullOrEmpty(ddlMaterial.SelectedValue)
            || !int.TryParse(ddlMaterial.SelectedValue, out materialId)
            || materialId <= 0)
        {
            ShowMessage("ماده اولیه را انتخاب کنید.");
            return;
        }

        // چک: ماده نهایی خودش نباشد
        int productOfBom = 0;
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ProductId FROM BOMs WHERE BOMId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentBomId);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) productOfBom = Convert.ToInt32(o);
                }
            }
        }
        catch { }

        if (materialId == productOfBom)
        {
            ShowMessage("محصول نهایی نمی‌تواند ماده اولیه خودش باشد.");
            return;
        }

        decimal quantity = ParseDecimalOrZero(txtQuantity.Text);
        if (quantity <= 0)
        {
            ShowMessage("مقدار باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        decimal wastePercent = ParseDecimalOrZero(txtWastePercent.Text);
        if (wastePercent < 0 || wastePercent > 100)
        {
            ShowMessage("درصد ضایعات باید بین ۰ تا ۱۰۰ باشد.");
            return;
        }

        string notes = txtItemNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری
                string dupSql = "SELECT COUNT(*) FROM BOMItems " +
                    "WHERE BOMId = @BomId AND MaterialId = @MaterialId";
                if (currentId > 0) dupSql += " AND BOMItemId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@BomId", CurrentBomId);
                    dup.Parameters.AddWithValue("@MaterialId", materialId);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("این ماده قبلاً به BOM اضافه شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE BOMItems SET MaterialId = @MaterialId, Quantity = @Quantity, " +
                        "  WastePercent = @WastePercent, Notes = @Notes " +
                        "WHERE BOMItemId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@MaterialId", materialId);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@WastePercent", wastePercent);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "BOMItems", currentId,
                        "قلم BOM #" + CurrentBomId, "ویرایش قلم BOM");
                    ShowMessage("قلم ویرایش شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO BOMItems (BOMId, MaterialId, Quantity, WastePercent, Notes) " +
                        "VALUES (@BomId, @MaterialId, @Quantity, @WastePercent, @Notes); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@BomId", CurrentBomId);
                        cmd.Parameters.AddWithValue("@MaterialId", materialId);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@WastePercent", wastePercent);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "BOMItems", newId,
                            "قلم جدید BOM #" + CurrentBomId, "افزودن قلم BOM");

                    ShowMessage("ماده اولیه اضافه شد.");
                }
            }

            ClearItemForm();
            EditItemId = 0;
            pnlItemForm.Visible = false;
            LoadCurrentBomInfo();
            LoadItems();
        }
        catch (Exception ex) { ShowMessage("خطا در ذخیره قلم: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────
    protected void rptBoms_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "ManageItems") ShowItemsView(id);
        else if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreBom(id);
    }

    private void ToggleActive(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                bool isActive = false;
                int productId = 0;
                using (SqlCommand get = new SqlCommand(
                    "SELECT p.ProductName, b.IsActive, b.ProductId " +
                    "FROM BOMs b INNER JOIN Products p ON p.ProductId = b.ProductId " +
                    "WHERE b.BOMId = @Id AND b.IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            productName = Convert.ToString(dr["ProductName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            productId = Convert.ToInt32(dr["ProductId"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(productName)) { ShowMessage("BOM یافت نشد."); return; }

                bool newState = !isActive;

                // اگر فعال می‌شود، بقیه BOM های این محصول غیرفعال شوند
                if (newState)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE BOMs SET IsActive = 0 WHERE ProductId = @PId AND BOMId <> @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@PId", productId);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE BOMs SET IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE BOMId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "BOMs", id, productName,
                    newState ? "فعال‌سازی BOM" : "غیرفعال‌سازی BOM");
            }

            ShowMessage("وضعیت BOM تغییر یافت.");
            LoadStats();
            LoadBoms();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete BOM
    // ═══════════════════════════════════════════════════════
    private void SoftDelete(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                int itemCount = 0;
                int usageCount = 0;

                using (SqlCommand get = new SqlCommand(
                    "SELECT p.ProductName FROM BOMs b " +
                    "INNER JOIN Products p ON p.ProductId = b.ProductId " +
                    "WHERE b.BOMId = @Id AND b.IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) productName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(productName)) { ShowMessage("BOM یافت نشد."); return; }

                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM BOMItems WHERE BOMId = @Id", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (itemCount > 0)
                {
                    ShowMessage(string.Format(
                        "این BOM دارای {0} ماده اولیه است و قابل حذف نیست. ابتدا اقلام را حذف کنید.", itemCount));
                    return;
                }

                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductionOrders WHERE BOMId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    usageCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (usageCount > 0)
                {
                    ShowMessage(string.Format(
                        "این BOM در {0} دستور تولید استفاده شده و قابل حذف نیست.", usageCount));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE BOMs SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE BOMId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "BOMs", id, productName, "حذف نرم BOM");
            }

            ShowMessage("BOM حذف شد (قابل بازیابی).");
            LoadStats();
            LoadBoms();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Restore BOM
    // ═══════════════════════════════════════════════════════
    private void RestoreBom(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                int productId = 0;
                int version = 0;

                using (SqlCommand get = new SqlCommand(
                    "SELECT p.ProductName, b.ProductId, b.Version FROM BOMs b " +
                    "INNER JOIN Products p ON p.ProductId = b.ProductId " +
                    "WHERE b.BOMId = @Id AND b.IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            productName = Convert.ToString(dr["ProductName"]);
                            productId = Convert.ToInt32(dr["ProductId"]);
                            version = Convert.ToInt32(dr["Version"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(productName)) { ShowMessage("BOM حذف‌شده یافت نشد."); return; }

                // چک تکراری
                int dup = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM BOMs WHERE ProductId = @PId AND Version = @V " +
                    "AND IsDeleted = 0 AND BOMId <> @Id", conn))
                {
                    check.Parameters.AddWithValue("@PId", productId);
                    check.Parameters.AddWithValue("@V", version);
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    dup = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dup > 0)
                {
                    ShowMessage("BOM دیگری با همین محصول و نسخه وجود دارد.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE BOMs SET IsDeleted = 0, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE BOMId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "BOMs", id, productName, "بازیابی BOM");
            }

            ShowMessage("BOM بازیابی شد.");
            LoadStats();
            LoadBoms();
        }
        catch (Exception ex) { ShowMessage("خطا در بازیابی: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Item Commands (Hard Delete — خط اقلام)
    // ─────────────────────────────────────────────────────
    protected void rptItems_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0) return;

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
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT BOMItemId, MaterialId, Quantity, WastePercent, Notes " +
                    "FROM BOMItems WHERE BOMItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditItemId = itemId;

                            ddlMaterial.ClearSelection();
                            if (dr["MaterialId"] != DBNull.Value)
                            {
                                string mid = Convert.ToInt32(dr["MaterialId"])
                                    .ToString(CultureInfo.InvariantCulture);
                                ListItem item = ddlMaterial.Items.FindByValue(mid);
                                if (item != null) item.Selected = true;
                            }

                            txtQuantity.Text = dr["Quantity"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["Quantity"]).ToString(CultureInfo.InvariantCulture);
                            txtWastePercent.Text = dr["WastePercent"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["WastePercent"]).ToString("N2", CultureInfo.InvariantCulture);
                            txtItemNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);

                            litItemFormTitle.Text = "ویرایش ماده اولیه";
                            pnlItemForm.Visible = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ⚠️ Hard Delete — چون BOMItems خط اقلام است
    private void DeleteItem(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM BOMItems WHERE BOMItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "BOMItems", itemId,
                    "قلم BOM #" + CurrentBomId, "حذف قلم BOM");
            }

            ShowMessage("قلم حذف شد.");
            LoadCurrentBomInfo();
            LoadItems();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditBomId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditBomId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        CurrentBomId = 0;
        EditItemId = 0;
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

    // ── BOM Card ──
    protected string GetBomCardCss(object isActiveObj, object isDeletedObj)
    {
        bool isActive = IsTrue(isActiveObj);
        bool isDeleted = IsTrue(isDeletedObj);

        string baseC = "bom-card relative overflow-hidden rounded-card border-2 bg-surface-card p-4 shadow-card";

        if (isDeleted) return baseC + " border-danger-300 bg-danger-50/30 opacity-70";
        if (!isActive) return baseC + " border-surface-line opacity-60";
        return baseC + " border-surface-line hover:border-brand-300";
    }

    protected string GetBomAvatarCss(object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "grid h-10 w-10 shrink-0 place-items-center rounded-base bg-danger-50 text-danger-500";
        return "grid h-10 w-10 shrink-0 place-items-center rounded-base bg-brand-50 text-brand-600";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetStatusDot(object isActiveObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "<span class=\"inline-block h-2.5 w-2.5 rounded-full bg-danger-500\" title=\"حذف‌شده\"></span>";
        if (IsTrue(isActiveObj))
            return "<span class=\"inline-block h-2.5 w-2.5 rounded-full bg-emerald-500\" title=\"فعال\"></span>";
        return "<span class=\"inline-block h-2.5 w-2.5 rounded-full bg-ink-300\" title=\"غیرفعال\"></span>";
    }

    protected string GetNotesHint(object notesObj)
    {
        if (notesObj == null || notesObj == DBNull.Value) return "";
        string notes = Convert.ToString(notesObj).Trim();
        if (notes.Length == 0) return "";
        string preview = notes.Length > 80 ? notes.Substring(0, 77) + "..." : notes;
        return "<p class=\"mt-2 text-[11px] text-ink-400 leading-relaxed\">" +
               HttpUtility.HtmlEncode(preview) + "</p>";
    }

    protected string GetWasteBadge(object wasteObj)
    {
        if (wasteObj == null || wasteObj == DBNull.Value)
            return "<span class=\"text-[11px] text-ink-400\">—</span>";
        decimal w = Convert.ToDecimal(wasteObj);
        if (w <= 0m) return "<span class=\"text-[11px] text-ink-400\">—</span>";

        string css = "badge bg-surface-muted text-ink-700";
        if (w >= 10m) css = "badge bg-danger-50 text-danger-600";
        else if (w >= 5m) css = "badge bg-amber-50 text-amber-700";
        else css = "badge bg-brand-50 text-brand-700";

        return "<span class=\"" + css + " font-mono text-[11px]\">" +
            w.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،") + "٪</span>";
    }

    protected bool CanEditBom(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected bool CanDeleteBom(object itemCountObj, object usageCountObj)
    {
        int ic = (itemCountObj == null || itemCountObj == DBNull.Value) ? 0 : Convert.ToInt32(itemCountObj);
        int uc = (usageCountObj == null || usageCountObj == DBNull.Value) ? 0 : Convert.ToInt32(usageCountObj);
        return ic == 0 && uc == 0;
    }

    protected string GetBlockReason(object itemCountObj, object usageCountObj)
    {
        int ic = (itemCountObj == null || itemCountObj == DBNull.Value) ? 0 : Convert.ToInt32(itemCountObj);
        int uc = (usageCountObj == null || usageCountObj == DBNull.Value) ? 0 : Convert.ToInt32(usageCountObj);
        if (uc > 0) return "این BOM در دستور تولید استفاده شده";
        if (ic > 0) return "این BOM اقلام دارد";
        return "قابل حذف نیست";
    }

    protected string GetToggleCss(object isActiveObj)
    {
        return IsTrue(isActiveObj)
            ? "icon-button hover:bg-warning-50 hover:text-warning-600"
            : "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj) { return IsTrue(isActiveObj) ? "غیرفعال کردن" : "فعال کردن"; }
    protected string GetToggleIcon(object isActiveObj) { return IsTrue(isActiveObj) ? "toggle-right" : "toggle-left"; }

    protected string GetToggleConfirm(object isActiveObj, object productNameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (productNameObj == null || productNameObj == DBNull.Value)
            ? "" : productNameObj.ToString().Replace("'", "");
        if (isActive)
            return "return confirm('BOM «" + name + "» غیرفعال شود؟\\n\\nتوجه: با غیرفعال شدن، در تولید استفاده نمی‌شود.');";
        return "return confirm('BOM «" + name + "» فعال شود؟\\n\\nتوجه: BOM فعال دیگری برای این محصول (در صورت وجود) غیرفعال می‌شود.');";
    }

    protected string GetDeleteConfirm(object productNameObj)
    {
        string name = (productNameObj == null || productNameObj == DBNull.Value)
            ? "" : productNameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف BOM «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object productNameObj)
    {
        string name = (productNameObj == null || productNameObj == DBNull.Value)
            ? "" : productNameObj.ToString().Replace("'", "");
        return "return confirm('BOM «" + name + "» بازیابی شود؟');";
    }

    protected string GetDeleteItemConfirm(object materialNameObj)
    {
        string name = (materialNameObj == null || materialNameObj == DBNull.Value)
            ? "" : materialNameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف «" + name + "» از این BOM مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // Clear Forms
    // ─────────────────────────────────────────────────────
    private void ClearBomForm()
    {
        if (ddlProduct.Items.Count > 0) ddlProduct.SelectedIndex = 0;
        txtVersion.Text = "1";
        chkIsActive.Checked = true;
        txtBomNotes.Text = "";
    }

    private void ClearItemForm()
    {
        if (ddlMaterial.Items.Count > 0) ddlMaterial.SelectedIndex = 0;
        txtQuantity.Text = "";
        txtWastePercent.Text = "0";
        txtItemNotes.Text = "";
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

    // ─────────────────────────────────────────────────────
    // Message
    // ─────────────────────────────────────────────────────
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