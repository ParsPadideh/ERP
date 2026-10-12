using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_price_lists : System.Web.UI.Page
{
    private const int ItemsPageSize = 20;

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

    private int EditPriceListId
    {
        get { int id; if (int.TryParse(hfEditPriceListId.Value, out id)) return id; return 0; }
        set { hfEditPriceListId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int CurrentPriceListId
    {
        get { int id; if (int.TryParse(hfCurrentPriceListId.Value, out id)) return id; return 0; }
        set { hfCurrentPriceListId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int EditItemId
    {
        get { int id; if (int.TryParse(hfEditItemId.Value, out id)) return id; return 0; }
        set { hfEditItemId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int ItemsCurrentPage
    {
        get { return ViewState["ItemsPage"] == null ? 1 : (int)ViewState["ItemsPage"]; }
        set { ViewState["ItemsPage"] = value; }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ItemsCurrentPage = 1;
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
        phAddList.Visible = true;
        phAddItem.Visible = false;

        litPageTitle.Text = "لیست‌های قیمت";
        litPageSubtitle.Text = "لیست‌های قیمت تک‌فروشی، عمده و نمایندگی را مدیریت کنید.";

        LoadStats();
        LoadPriceLists();
    }

    private void ShowFormView(int priceListId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        pnlItemsView.Visible = false;

        phBackToList.Visible = true;
        phAddList.Visible = false;
        phAddItem.Visible = false;

        litPageTitle.Text = priceListId > 0 ? "ویرایش لیست قیمت" : "افزودن لیست قیمت جدید";
        litPageSubtitle.Text = "اطلاعات پایه لیست قیمت را وارد کنید.";

        if (priceListId > 0)
            LoadPriceListForEdit(priceListId);
        else
            ClearListForm();
    }

    private void ShowItemsView(int priceListId)
    {
        CurrentPriceListId = priceListId;
        ItemsCurrentPage = 1;

        pnlListView.Visible = false;
        pnlFormView.Visible = false;
        pnlItemsView.Visible = true;

        phBackToList.Visible = true;
        phAddList.Visible = false;
        phAddItem.Visible = true;

        litPageTitle.Text = "مدیریت اقلام لیست قیمت";
        litPageSubtitle.Text = "محصولات و قیمت‌های پله‌ای این لیست را تعریف کنید.";

        LoadCurrentListInfo();
        LoadProductDropdown();
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
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS TotalLists, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveLists, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedLists, " +
                    "  (SELECT COUNT(*) FROM PriceListItems) AS TotalItems " +
                    "FROM PriceLists";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["TotalLists"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveLists"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedLists"]).ToString("N0"));
                        litStatItems.Text = HttpUtility.HtmlEncode(ToInt(dr["TotalItems"]).ToString("N0"));
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
    // Load Price Lists
    // ─────────────────────────────────────────────────────
    private void LoadPriceLists()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append("  pl.PriceListId, pl.PriceListName, pl.PriceListType, ");
                sb.Append("  pl.IsDefault, pl.IsActive, pl.IsDeleted, pl.CreatedAt, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM PriceListItems pli ");
                sb.Append("          WHERE pli.PriceListId = pl.PriceListId), 0) AS ItemCount, ");
                sb.Append("  ISNULL((SELECT COUNT(DISTINCT pli.ProductId) FROM PriceListItems pli ");
                sb.Append("          WHERE pli.PriceListId = pl.PriceListId), 0) AS ProductCount ");
                sb.Append("FROM PriceLists pl WHERE 1 = 1 ");

                if (!chkShowDeleted.Checked)
                    sb.Append(" AND pl.IsDeleted = 0 ");

                sb.Append(" ORDER BY pl.IsDeleted ASC, pl.IsDefault DESC, pl.PriceListType ASC, pl.PriceListId ASC");

                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlListEmpty.Visible = true;
                rptPriceLists.Visible = false;
                rptPriceLists.DataSource = null;
                rptPriceLists.DataBind();
            }
            else
            {
                pnlListEmpty.Visible = false;
                rptPriceLists.Visible = true;
                rptPriceLists.DataSource = dt;
                rptPriceLists.DataBind();
            }

            litListInfo.Text = string.Format("{0} لیست قیمت",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری لیست‌ها: " + ex.Message); }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadPriceLists(); }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadPriceListForEdit(int priceListId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT PriceListId, PriceListName, PriceListType, IsDefault, IsActive " +
                    "FROM PriceLists WHERE PriceListId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", priceListId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditPriceListId = priceListId;
                            txtPriceListName.Text = Convert.ToString(dr["PriceListName"]);

                            int type = dr["PriceListType"] == DBNull.Value
                                ? 1 : Convert.ToInt32(dr["PriceListType"]);
                            ddlPriceListType.ClearSelection();
                            ListItem tItem = ddlPriceListType.Items.FindByValue(
                                type.ToString(CultureInfo.InvariantCulture));
                            if (tItem != null) tItem.Selected = true;

                            chkIsDefault.Checked = dr["IsDefault"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsDefault"]);

                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem aItem = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (aItem != null) aItem.Selected = true;

                            litFormTitle.Text = "ویرایش لیست — " + Convert.ToString(dr["PriceListName"]);
                        }
                        else
                        {
                            ShowMessage("لیست قیمت مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری لیست: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Save Price List
    // ─────────────────────────────────────────────────────
    protected void btnSavePriceList_Click(object sender, EventArgs e)
    {
        int currentId = EditPriceListId;
        string name = txtPriceListName.Text.Trim();

        int type = 1;
        if (!int.TryParse(ddlPriceListType.SelectedValue, out type)) type = 1;

        bool isDefault = chkIsDefault.Checked;
        bool isActive = (ddlIsActive.SelectedValue == "1");

        if (name.Length == 0) { ShowMessage("نام لیست را وارد کنید."); return; }
        if (name.Length > 100) { ShowMessage("نام حداکثر ۱۰۰ کاراکتر."); return; }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری
                string dupSql = "SELECT COUNT(*) FROM PriceLists " +
                    "WHERE PriceListName = @Name AND IsDeleted = 0";
                if (currentId > 0) dupSql += " AND PriceListId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", name);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("لیستی با این نام قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    if (isDefault) UnsetOtherDefaults(conn, currentId);

                    string updateSql =
                        "UPDATE PriceLists SET " +
                        "  PriceListName = @Name, PriceListType = @Type, " +
                        "  IsDefault = @IsDefault, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE PriceListId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@IsDefault", isDefault);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "PriceLists", currentId, name, "ویرایش لیست قیمت");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    if (isDefault) UnsetOtherDefaults(conn, 0);

                    string insertSql =
                        "INSERT INTO PriceLists " +
                        "  (PriceListName, PriceListType, IsDefault, IsActive, IsDeleted, CreatedAt) " +
                        "VALUES (@Name, @Type, @IsDefault, @IsActive, 0, GETDATE()); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@IsDefault", isDefault);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "PriceLists", newId, name, "ایجاد لیست قیمت");

                    ShowMessage("لیست جدید افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا در ذخیره: " + ex.Message); }
    }

    private void UnsetOtherDefaults(SqlConnection conn, int excludeId)
    {
        string sql = "UPDATE PriceLists SET IsDefault = 0";
        if (excludeId > 0) sql += " WHERE PriceListId <> @Id";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            if (excludeId > 0) cmd.Parameters.AddWithValue("@Id", excludeId);
            cmd.ExecuteNonQuery();
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Items
    // ─────────────────────────────────────────────────────
    private void LoadCurrentListInfo()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT PriceListName, PriceListType, IsDefault FROM PriceLists " +
                    "WHERE PriceListId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentPriceListId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            litCurrentListName.Text = HttpUtility.HtmlEncode(Convert.ToString(dr["PriceListName"]));

                            int type = dr["PriceListType"] == DBNull.Value ? 1 : Convert.ToInt32(dr["PriceListType"]);
                            litCurrentListType.Text = "<span class=\"" + GetTypeBadgeCss(type) + "\">" +
                                HttpUtility.HtmlEncode(GetTypeLabel(type)) + "</span>";

                            bool isDef = dr["IsDefault"] != DBNull.Value && Convert.ToBoolean(dr["IsDefault"]);
                            litCurrentListDefault.Text = isDef
                                ? "<span class=\"badge bg-violet-50 text-violet-700\"><i class=\"h-3 w-3\" data-lucide=\"star\"></i> پیش‌فرض سیستم</span>"
                                : "";
                        }
                        else
                        {
                            ShowMessage("لیست یافت نشد یا حذف شده است.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void LoadProductDropdown()
    {
        ddlItemProduct.Items.Clear();
        ddlItemProduct.Items.Add(new ListItem("— انتخاب محصول —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, u.UnitName " +
                    "FROM Products p " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND u.IsDeleted = 0 " +
                    "  AND p.ProductType = 2 " +
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

                if (ddlItemProduct.Items.Count <= 1)
                {
                    ddlItemProduct.Items.Add(new ListItem(
                        "— هیچ محصول نهایی فعالی وجود ندارد —", ""));
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری محصولات: " + ex.Message); }
    }

    private void LoadItems()
    {
        try
        {
            string search = txtSearchItems.Text.Trim();
            string activeFilter = ddlItemsFilterActive.SelectedValue;
            if (activeFilter == null) activeFilter = "";

            int totalCount = GetTotalItemsCount(search, activeFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / ItemsPageSize);
            if (totalPages < 1) totalPages = 1;
            if (ItemsCurrentPage > totalPages) ItemsCurrentPage = totalPages;
            if (ItemsCurrentPage < 1) ItemsCurrentPage = 1;

            DataTable dt = GetPagedItems(search, activeFilter, ItemsCurrentPage, ItemsPageSize);

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

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (ItemsCurrentPage - 1) * ItemsPageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litItemsInfo.Text = string.Format("نمایش {0} تا {1} از {2} قلم",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnItemsPrev, ItemsCurrentPage > 1);
            SetPagerEnabled(btnItemsNext, ItemsCurrentPage < totalPages);
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری اقلام: " + ex.Message); }
    }

    private string BuildItemsFilterSql(string search, string activeFilter, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE pli.PriceListId = @PriceListId ");
        cmd.Parameters.AddWithValue("@PriceListId", CurrentPriceListId);

        if (search.Length > 0)
        {
            sb.Append(" AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (activeFilter.Length > 0)
        {
            sb.Append(" AND pli.IsActive = @IsActive ");
            cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
        }

        return sb.ToString();
    }

    private int GetTotalItemsCount(string search, string activeFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildItemsFilterSql(search, activeFilter, cmd);
                cmd.CommandText =
                    "SELECT COUNT(*) FROM PriceListItems pli " +
                    "INNER JOIN Products p ON p.ProductId = pli.ProductId " + filter;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPagedItems(string search, string activeFilter, int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildItemsFilterSql(search, activeFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY p.ProductName, pli.MinQuantity) AS RowNum, " +
                    "    pli.PriceListItemId, pli.ProductId, pli.MinQuantity, pli.UnitPrice, " +
                    "    pli.DiscountPercent, pli.EffectiveFrom, pli.EffectiveTo, pli.IsActive, " +
                    "    p.ProductCode, p.ProductName, p.ProductType " +
                    "  FROM PriceListItems pli " +
                    "  INNER JOIN Products p ON p.ProductId = pli.ProductId " +
                    filter +
                    " ) x WHERE x.RowNum BETWEEN @Start AND @End ORDER BY x.RowNum";

                cmd.CommandText = sql;

                int start = (page - 1) * pageSize + 1;
                int end = start + pageSize - 1;
                cmd.Parameters.AddWithValue("@Start", start);
                cmd.Parameters.AddWithValue("@End", end);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
        }

        return dt;
    }

    protected void txtSearchItems_TextChanged(object sender, EventArgs e) { ItemsCurrentPage = 1; LoadItems(); }
    protected void ddlItemsFilterActive_SelectedIndexChanged(object sender, EventArgs e) { ItemsCurrentPage = 1; LoadItems(); }

    protected void btnItemsPrev_Click(object sender, EventArgs e)
    {
        if (ItemsCurrentPage > 1) { ItemsCurrentPage = ItemsCurrentPage - 1; LoadItems(); }
    }

    protected void btnItemsNext_Click(object sender, EventArgs e)
    {
        ItemsCurrentPage = ItemsCurrentPage + 1;
        LoadItems();
    }

    // ─────────────────────────────────────────────────────
    // Item Form
    // ─────────────────────────────────────────────────────
    protected void btnShowAddItemForm_Click(object sender, EventArgs e)
    {
        ClearItemForm();
        EditItemId = 0;
        litItemFormTitle.Text = "افزودن محصول به لیست";
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

        int productId;
        if (string.IsNullOrEmpty(ddlItemProduct.SelectedValue)
            || !int.TryParse(ddlItemProduct.SelectedValue, out productId)
            || productId <= 0)
        {
            ShowMessage("محصول را انتخاب کنید.");
            return;
        }

        decimal minQty = 1m;
        if (txtItemMinQuantity.Text.Trim().Length > 0)
        {
            decimal? q = ParseDecimal(txtItemMinQuantity.Text);
            if (!q.HasValue || q.Value <= 0)
            {
                ShowMessage("حداقل تعداد باید عددی بزرگ‌تر از صفر باشد.");
                return;
            }
            minQty = q.Value;
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
            if (!d.HasValue || d.Value < 0 || d.Value > 100)
            {
                ShowMessage("درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.");
                return;
            }
            discount = d.Value;
        }

        DateTime effFrom = DateTime.Now.Date;
        if (txtItemEffectiveFrom.Text.Trim().Length > 0)
        {
            DateTime? dt = ParsePersianDate(txtItemEffectiveFrom.Text.Trim());
            if (!dt.HasValue) { ShowMessage("تاریخ شروع معتبر نیست."); return; }
            effFrom = dt.Value;
        }

        DateTime? effTo = null;
        if (txtItemEffectiveTo.Text.Trim().Length > 0)
        {
            DateTime? dt = ParsePersianDate(txtItemEffectiveTo.Text.Trim());
            if (!dt.HasValue) { ShowMessage("تاریخ پایان معتبر نیست."); return; }
            effTo = dt.Value;
            if (effTo.Value < effFrom) { ShowMessage("تاریخ پایان قبل از شروع است."); return; }
        }

        bool isActive = (ddlItemIsActive.SelectedValue == "1");

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE PriceListItems SET " +
                        "  ProductId = @ProductId, MinQuantity = @MinQty, UnitPrice = @UnitPrice, " +
                        "  DiscountPercent = @Discount, EffectiveFrom = @EffFrom, " +
                        "  EffectiveTo = @EffTo, IsActive = @IsActive " +
                        "WHERE PriceListItemId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@MinQty", minQty);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice.Value);
                        cmd.Parameters.AddWithValue("@Discount", discount);
                        cmd.Parameters.AddWithValue("@EffFrom", effFrom);
                        cmd.Parameters.AddWithValue("@EffTo", effTo.HasValue ? (object)effTo.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "PriceListItems", currentId,
                        "قلم لیست #" + CurrentPriceListId, "ویرایش قلم لیست قیمت");
                    ShowMessage("قلم ویرایش شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO PriceListItems " +
                        "  (PriceListId, ProductId, MinQuantity, UnitPrice, DiscountPercent, " +
                        "   EffectiveFrom, EffectiveTo, IsActive) " +
                        "VALUES (@PriceListId, @ProductId, @MinQty, @UnitPrice, @Discount, " +
                        "   @EffFrom, @EffTo, @IsActive); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@PriceListId", CurrentPriceListId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@MinQty", minQty);
                        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice.Value);
                        cmd.Parameters.AddWithValue("@Discount", discount);
                        cmd.Parameters.AddWithValue("@EffFrom", effFrom);
                        cmd.Parameters.AddWithValue("@EffTo", effTo.HasValue ? (object)effTo.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "PriceListItems", newId,
                            "قلم جدید در لیست #" + CurrentPriceListId, "افزودن قلم");

                    ShowMessage("محصول به لیست اضافه شد.");
                }
            }

            ClearItemForm();
            EditItemId = 0;
            pnlItemForm.Visible = false;
            LoadItems();
        }
        catch (Exception ex) { ShowMessage("خطا در ذخیره قلم: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Commands — List
    // ─────────────────────────────────────────────────────
    protected void rptPriceLists_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int priceListId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out priceListId) || priceListId <= 0)
            return;

        if (e.CommandName == "ManageItems") ShowItemsView(priceListId);
        else if (e.CommandName == "Edit") ShowFormView(priceListId);
        else if (e.CommandName == "SetDefault") SetDefaultList(priceListId);
        else if (e.CommandName == "ToggleActive") ToggleListActive(priceListId);
        else if (e.CommandName == "SoftDelete") SoftDeleteList(priceListId);
        else if (e.CommandName == "Restore") RestoreList(priceListId);
    }

    private void SetDefaultList(int priceListId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string listName = "";
                bool isDeleted = false;
                using (SqlCommand get = new SqlCommand(
                    "SELECT PriceListName, IsDeleted FROM PriceLists WHERE PriceListId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", priceListId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            listName = Convert.ToString(dr["PriceListName"]);
                            isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(listName)) { ShowMessage("لیست یافت نشد."); return; }
                if (isDeleted) { ShowMessage("لیست حذف‌شده قابل تنظیم به‌عنوان پیش‌فرض نیست."); return; }

                UnsetOtherDefaults(conn, priceListId);

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PriceLists SET IsDefault = 1 WHERE PriceListId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", priceListId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SetDefault", "PriceLists", priceListId, listName, "تعیین لیست پیش‌فرض");
            }

            ShowMessage("لیست به‌عنوان پیش‌فرض تعیین شد.");
            LoadStats();
            LoadPriceLists();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void ToggleListActive(int priceListId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string listName = "";
                bool isActive = false;
                bool isDefault = false;
                bool isDeleted = false;

                using (SqlCommand get = new SqlCommand(
                    "SELECT PriceListName, IsActive, IsDefault, IsDeleted " +
                    "FROM PriceLists WHERE PriceListId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", priceListId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            listName = Convert.ToString(dr["PriceListName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            isDefault = dr["IsDefault"] != DBNull.Value && Convert.ToBoolean(dr["IsDefault"]);
                            isDeleted = dr["IsDeleted"] != DBNull.Value && Convert.ToBoolean(dr["IsDeleted"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(listName)) { ShowMessage("لیست یافت نشد."); return; }
                if (isDeleted) { ShowMessage("لیست حذف‌شده قابل تغییر وضعیت نیست."); return; }

                if (isDefault && isActive)
                {
                    ShowMessage("لیست پیش‌فرض را نمی‌توان غیرفعال کرد. ابتدا لیست دیگری را پیش‌فرض کنید.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PriceLists SET IsActive = @IsActive WHERE PriceListId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", priceListId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "PriceLists", priceListId, listName,
                    newState ? "فعال‌سازی لیست" : "غیرفعال‌سازی لیست");
            }

            ShowMessage("وضعیت لیست تغییر یافت.");
            LoadStats();
            LoadPriceLists();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete List
    // ═══════════════════════════════════════════════════════
    private void SoftDeleteList(int priceListId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string listName = "";
                bool isDefault = false;
                using (SqlCommand get = new SqlCommand(
                    "SELECT PriceListName, IsDefault FROM PriceLists " +
                    "WHERE PriceListId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", priceListId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            listName = Convert.ToString(dr["PriceListName"]);
                            isDefault = dr["IsDefault"] != DBNull.Value && Convert.ToBoolean(dr["IsDefault"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(listName)) { ShowMessage("لیست یافت نشد."); return; }

                if (isDefault)
                {
                    ShowMessage("لیست پیش‌فرض قابل حذف نیست. ابتدا لیست دیگری را پیش‌فرض کنید.");
                    return;
                }

                int itemCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM PriceListItems WHERE PriceListId = @Id", conn))
                {
                    check.Parameters.AddWithValue("@Id", priceListId);
                    object o = check.ExecuteScalar();
                    itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (itemCount > 0)
                {
                    ShowMessage(string.Format(
                        "این لیست {0} قلم قیمت دارد و قابل حذف نیست. ابتدا اقلام را حذف کنید.", itemCount));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PriceLists SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE PriceListId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", priceListId);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "PriceLists", priceListId, listName, "حذف نرم لیست قیمت");
            }

            ShowMessage("لیست حذف شد (قابل بازیابی).");
            LoadStats();
            LoadPriceLists();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Restore List
    // ═══════════════════════════════════════════════════════
    private void RestoreList(int priceListId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string listName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT PriceListName FROM PriceLists " +
                    "WHERE PriceListId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", priceListId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) listName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(listName)) { ShowMessage("لیست حذف‌شده یافت نشد."); return; }

                int dup = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM PriceLists WHERE PriceListName = @Name AND IsDeleted = 0 AND PriceListId <> @Id", conn))
                {
                    check.Parameters.AddWithValue("@Name", listName);
                    check.Parameters.AddWithValue("@Id", priceListId);
                    object o = check.ExecuteScalar();
                    dup = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dup > 0)
                {
                    ShowMessage("لیست دیگری با همین نام وجود دارد. ابتدا آن را تغییر دهید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PriceLists SET IsDeleted = 0, IsActive = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE PriceListId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", priceListId);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "PriceLists", priceListId, listName, "بازیابی لیست قیمت");
            }

            ShowMessage("لیست بازیابی شد.");
            LoadStats();
            LoadPriceLists();
        }
        catch (Exception ex) { ShowMessage("خطا در بازیابی: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Commands — Items (Hard Delete — خط اقلام)
    // ─────────────────────────────────────────────────────
    protected void rptItems_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int itemId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out itemId) || itemId <= 0) return;

        if (e.CommandName == "EditItem") LoadItemForEdit(itemId);
        else if (e.CommandName == "ToggleItem") ToggleItemActive(itemId);
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
                    "SELECT PriceListItemId, ProductId, MinQuantity, UnitPrice, " +
                    "       DiscountPercent, EffectiveFrom, EffectiveTo, IsActive " +
                    "FROM PriceListItems WHERE PriceListItemId = @Id", conn))
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
                                ListItem pItem = ddlItemProduct.Items.FindByValue(pid);
                                if (pItem != null) pItem.Selected = true;
                            }

                            txtItemMinQuantity.Text = dr["MinQuantity"] == DBNull.Value
                                ? "1" : Convert.ToDecimal(dr["MinQuantity"]).ToString(CultureInfo.InvariantCulture);
                            txtItemUnitPrice.Text = dr["UnitPrice"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["UnitPrice"]).ToString(CultureInfo.InvariantCulture);
                            txtItemDiscount.Text = dr["DiscountPercent"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["DiscountPercent"]).ToString(CultureInfo.InvariantCulture);
                            txtItemEffectiveFrom.Text = dr["EffectiveFrom"] == DBNull.Value
                                ? "" : ToShamsiDateString(Convert.ToDateTime(dr["EffectiveFrom"]));
                            txtItemEffectiveTo.Text = dr["EffectiveTo"] == DBNull.Value
                                ? "" : ToShamsiDateString(Convert.ToDateTime(dr["EffectiveTo"]));

                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlItemIsActive.ClearSelection();
                            ListItem aItem = ddlItemIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (aItem != null) aItem.Selected = true;

                            litItemFormTitle.Text = "ویرایش قلم لیست";
                            pnlItemForm.Visible = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void ToggleItemActive(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                bool isActive = false;
                using (SqlCommand get = new SqlCommand(
                    "SELECT IsActive FROM PriceListItems WHERE PriceListItemId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", itemId);
                    object o = get.ExecuteScalar();
                    if (o == null || o == DBNull.Value) { ShowMessage("قلم یافت نشد."); return; }
                    isActive = Convert.ToBoolean(o);
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PriceListItems SET IsActive = @IsActive WHERE PriceListItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "PriceListItems", itemId,
                    "قلم #" + itemId, newState ? "فعال‌سازی قلم" : "غیرفعال‌سازی قلم");
            }

            ShowMessage("وضعیت قلم تغییر یافت.");
            LoadItems();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ⚠️ Hard Delete — چون PriceListItems جدول خط اقلام است
    private void DeleteItem(int itemId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM PriceListItems WHERE PriceListItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "PriceListItems", itemId, "قلم #" + itemId, "حذف قلم لیست");
            }

            ShowMessage("قلم حذف شد.");
            LoadItems();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditPriceListId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditPriceListId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        CurrentPriceListId = 0;
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

    // ── Card CSS ──
    protected string GetListCardCss(object isActiveObj, object isDefaultObj, object isDeletedObj)
    {
        bool isActive = IsTrue(isActiveObj);
        bool isDefault = IsTrue(isDefaultObj);
        bool isDeleted = IsTrue(isDeletedObj);

        string baseC = "relative overflow-hidden rounded-card border-2 bg-surface-card p-4 shadow-card transition hover:shadow-card-lg";

        if (isDeleted) return baseC + " border-danger-300 bg-danger-50/30 opacity-70";
        if (!isActive) return baseC + " border-surface-line opacity-60";
        if (isDefault) return baseC + " border-violet-300";
        return baseC + " border-surface-line";
    }

    protected string GetListAvatarCss(object typeObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj))
            return "grid h-10 w-10 shrink-0 place-items-center rounded-base bg-danger-50 text-danger-500";

        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-base";

        if (type == 1) return baseC + " bg-emerald-100 text-emerald-700";
        if (type == 2) return baseC + " bg-amber-100 text-amber-700";
        return baseC + " bg-violet-100 text-violet-700";
    }

    protected string GetListIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "store";
        if (type == 2) return "building-2";
        return "briefcase";
    }

    protected string GetTypeLabel(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "تک‌فروشی";
        if (type == 2) return "عمده";
        return "نمایندگی";
    }

    private string GetTypeLabel(int type)
    {
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

    private string GetTypeBadgeCss(int type)
    {
        if (type == 1) return "badge bg-emerald-50 text-emerald-700";
        if (type == 2) return "badge bg-amber-50 text-amber-700";
        return "badge bg-violet-50 text-violet-700";
    }

    protected string GetDefaultBadge(object isDefaultObj)
    {
        if (!IsTrue(isDefaultObj)) return "";
        return " <span class=\"badge bg-violet-100 text-violet-700\"><i class=\"h-3 w-3\" data-lucide=\"star\"></i> پیش‌فرض</span>";
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

    protected bool CanEditList(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected bool CanDeleteList(object itemCountObj, object isDefaultObj)
    {
        int count = (itemCountObj == null || itemCountObj == DBNull.Value) ? 0 : Convert.ToInt32(itemCountObj);
        bool isDefault = IsTrue(isDefaultObj);
        return count == 0 && !isDefault;
    }

    protected string GetBlockDeleteReason(object itemCountObj, object isDefaultObj)
    {
        int count = (itemCountObj == null || itemCountObj == DBNull.Value) ? 0 : Convert.ToInt32(itemCountObj);
        bool isDefault = IsTrue(isDefaultObj);
        if (isDefault) return "لیست پیش‌فرض قابل حذف نیست";
        if (count > 0) return "این لیست اقلام قیمت دارد";
        return "قابل حذف نیست";
    }

    protected string GetDefaultToggleCss(object isDefaultObj)
    {
        return IsTrue(isDefaultObj) ? "icon-button text-violet-600" : "icon-button hover:bg-violet-50 hover:text-violet-600";
    }

    protected string GetDefaultToggleTooltip(object isDefaultObj)
    {
        return IsTrue(isDefaultObj) ? "پیش‌فرض فعلی" : "تعیین به‌عنوان پیش‌فرض";
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
        if (isActive) return "return confirm('لیست «" + name + "» غیرفعال شود؟');";
        return "return confirm('لیست «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف لیست «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('لیست «" + name + "» بازیابی شود؟');";
    }

    // ── Item helpers ──
    protected string GetProductTypeIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "pickaxe";
        if (type == 2) return "package-check";
        return "box";
    }

    protected string GetProductTypeAvatarCss(object typeObj, object isActiveObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value) ? true : Convert.ToBoolean(isActiveObj);
        string baseC = "grid h-8 w-8 shrink-0 place-items-center rounded-lg";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        if (type == 1) return baseC + " bg-amber-100 text-amber-700";
        if (type == 2) return baseC + " bg-violet-100 text-violet-700";
        return baseC + " bg-emerald-100 text-emerald-700";
    }

    protected string GetDiscountBadge(object discountObj)
    {
        decimal d = (discountObj == null || discountObj == DBNull.Value) ? 0m : Convert.ToDecimal(discountObj);
        if (d <= 0) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";
        return "<span class=\"badge bg-rose-50 text-rose-700 font-bold\">٪" +
            d.ToString("N0", CultureInfo.InvariantCulture) + "</span>";
    }

    protected string GetStatusBadgeClass(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "badge bg-success-50 text-success-600" : "badge bg-warning-50 text-warning-600";
    }

    protected string GetStatusText(object isActiveObj) { return IsTrue(isActiveObj) ? "فعال" : "غیرفعال"; }

    protected string FormatQuantity(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0) return "۰";
            if (d == Math.Floor(d)) return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
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

    protected string FormatShamsiDateOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        return FormatShamsiDate(value);
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    protected string GetToggleItemConfirm(object isActiveObj, object productNameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (productNameObj == null || productNameObj == DBNull.Value)
            ? "" : productNameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('قیمت «" + name + "» غیرفعال شود؟');";
        return "return confirm('قیمت «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteItemConfirm(object productNameObj)
    {
        string name = (productNameObj == null || productNameObj == DBNull.Value)
            ? "" : productNameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف قیمت «" + name + "» مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // Date / Number Helpers
    // ─────────────────────────────────────────────────────
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

    private decimal? ParseDecimal(string input)
    {
        if (input == null) return null;
        input = input.Trim();
        if (input.Length == 0) return null;
        input = input.Replace(",", "").Replace("،", "").Replace("٪", "");
        decimal d;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    // ─────────────────────────────────────────────────────
    // Clear / Set helpers
    // ─────────────────────────────────────────────────────
    private void ClearListForm()
    {
        txtPriceListName.Text = "";
        chkIsDefault.Checked = false;

        if (ddlPriceListType.Items.Count > 0)
        {
            ddlPriceListType.ClearSelection();
            ListItem t = ddlPriceListType.Items.FindByValue("1");
            if (t != null) t.Selected = true;
        }

        if (ddlIsActive.Items.Count > 0)
        {
            ddlIsActive.ClearSelection();
            ListItem a = ddlIsActive.Items.FindByValue("1");
            if (a != null) a.Selected = true;
        }
    }

    private void ClearItemForm()
    {
        if (ddlItemProduct.Items.Count > 0)
        {
            ddlItemProduct.ClearSelection();
            ddlItemProduct.SelectedIndex = 0;
        }

        txtItemMinQuantity.Text = "1";
        txtItemUnitPrice.Text = "";
        txtItemDiscount.Text = "0";
        txtItemEffectiveFrom.Text = "";
        txtItemEffectiveTo.Text = "";

        if (ddlItemIsActive.Items.Count > 0)
        {
            ddlItemIsActive.ClearSelection();
            ListItem a = ddlItemIsActive.Items.FindByValue("1");
            if (a != null) a.Selected = true;
        }
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