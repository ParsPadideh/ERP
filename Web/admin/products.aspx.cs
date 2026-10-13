using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_products : System.Web.UI.Page
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

    private int EditProductId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditProductId.Value, out id)) return id;
            return 0;
        }
        set { hfEditProductId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private long CurrentUserId
    {
        get
        {
            if (Session["UserId"] == null) return 0;
            return Convert.ToInt64(Session["UserId"]);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Markup Helper — for ASPX
    // ═══════════════════════════════════════════════════════
    protected bool HasProductImage(object imageUrlObj)
    {
        if (imageUrlObj == null || imageUrlObj == DBNull.Value) return false;
        string url = Convert.ToString(imageUrlObj).Trim();
        return url.Length > 0;
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CurrentPage = 1;
            LoadCategories();
            LoadUnits();
            LoadStats();
            LoadProducts();

            rbTypeRM.Checked = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری دسته‌بندی (درختی)
    // ─────────────────────────────────────────────────────
    private void LoadCategories()
    {
        ddlCategory.Items.Clear();
        ddlCategory.Items.Add(new ListItem("— بدون دسته‌بندی —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT CategoryId, CategoryName, ParentCategoryId " +
                    "FROM ProductCategories WHERE IsActive = 1";

                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                Dictionary<int, DataRow> byId = new Dictionary<int, DataRow>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dt.Rows[i]["CategoryId"]);
                    byId[id] = dt.Rows[i];
                }

                List<DataRow> roots = new List<DataRow>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow r = dt.Rows[i];
                    if (r["ParentCategoryId"] == DBNull.Value)
                        roots.Add(r);
                    else
                    {
                        int pid = Convert.ToInt32(r["ParentCategoryId"]);
                        if (!byId.ContainsKey(pid)) roots.Add(r);
                    }
                }

                SortRows(roots);

                for (int i = 0; i < roots.Count; i++)
                {
                    AddCategoryNode(roots[i], dt, 0);
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری دسته‌بندی‌ها: " + ex.Message);
        }
    }

    private void SortRows(List<DataRow> rows)
    {
        rows.Sort(delegate(DataRow a, DataRow b)
        {
            return string.Compare(Convert.ToString(a["CategoryName"]),
                                  Convert.ToString(b["CategoryName"]),
                                  StringComparison.CurrentCulture);
        });
    }

    private void AddCategoryNode(DataRow row, DataTable dt, int depth)
    {
        int id = Convert.ToInt32(row["CategoryId"]);
        string name = Convert.ToString(row["CategoryName"]);

        string indent = "";
        for (int i = 0; i < depth; i++) indent += "— ";

        ddlCategory.Items.Add(new ListItem(indent + name, id.ToString(CultureInfo.InvariantCulture)));

        List<DataRow> children = new List<DataRow>();
        for (int i = 0; i < dt.Rows.Count; i++)
        {
            DataRow r = dt.Rows[i];
            if (r["ParentCategoryId"] != DBNull.Value &&
                Convert.ToInt32(r["ParentCategoryId"]) == id)
            {
                children.Add(r);
            }
        }

        SortRows(children);

        for (int i = 0; i < children.Count; i++)
        {
            AddCategoryNode(children[i], dt, depth + 1);
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری واحدها
    // ─────────────────────────────────────────────────────
    private void LoadUnits()
    {
        ddlUnit.Items.Clear();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT UnitId, UnitName, Symbol FROM Units " +
                    "WHERE IsActive = 1 ORDER BY UnitId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["UnitId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["UnitName"]);
                        string symbol = dr["Symbol"] == DBNull.Value
                            ? "" : Convert.ToString(dr["Symbol"]);

                        string label = symbol.Length > 0
                            ? name + " (" + symbol + ")"
                            : name;

                        ddlUnit.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری واحدها: " + ex.Message);
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
                    "  SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS TotalProducts, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND ProductType = 1 THEN 1 ELSE 0 END) AS RMProducts, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND ProductType = 2 THEN 1 ELSE 0 END) AS FGProducts, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND ProductType = 3 THEN 1 ELSE 0 END) AS PackagingProducts " +
                    "FROM Products";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["TotalProducts"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatRM.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["RMProducts"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatFG.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["FGProducts"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatPackaging.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["PackagingProducts"]).ToString("N0", CultureInfo.InvariantCulture));
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
        EditProductId = 0;
        litFormTitle.Text = "افزودن محصول جدید";
        rbTypeRM.Checked = true;

        pnlForm.Visible = true;
        pnlList.Visible = false;
        pnlStats.Visible = false;
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        ClearForm();
        EditProductId = 0;
        pnlForm.Visible = false;
        pnlList.Visible = true;
        pnlStats.Visible = true;
        LoadStats();
        LoadProducts();
    }

    // ─────────────────────────────────────────────────────
    // لیست محصولات
    // ─────────────────────────────────────────────────────
    private void LoadProducts()
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
                rptProducts.Visible = false;
                rptProducts.DataSource = null;
                rptProducts.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptProducts.Visible = true;
                rptProducts.DataSource = dt;
                rptProducts.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }
            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} محصول",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری محصولات: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string typeFilter, string activeFilter, string deletedFilter,
        SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1=1 ");

        if (search.Length > 0)
        {
            sb.Append(" AND (p.ProductCode LIKE @Search OR p.ProductName LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        if (typeFilter.Length > 0)
        {
            int t;
            if (int.TryParse(typeFilter, out t))
            {
                sb.Append(" AND p.ProductType = @Type ");
                cmd.Parameters.AddWithValue("@Type", t);
            }
        }

        if (activeFilter.Length > 0)
        {
            sb.Append(" AND p.IsActive = @IsActive ");
            cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
        }

        if (deletedFilter.Length > 0)
        {
            sb.Append(" AND p.IsDeleted = @IsDeleted ");
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
                cmd.CommandText = "SELECT COUNT(*) FROM Products p " +
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
                    "  SELECT ROW_NUMBER() OVER (ORDER BY p.ProductId DESC) AS RowNum, " +
                    "    p.ProductId, p.ProductCode, p.ProductName, p.ProductType, " +
                    "    p.WeightGram, p.MinStock, p.MaxStock, p.ShelfLifeDays, " +
                    "    p.RequiresBatch, p.IsActive, p.IsDeleted, p.ImageUrl, " +
                    "    ISNULL(c.CategoryName, N'—') AS CategoryName, " +
                    "    u.UnitName, u.Symbol, " +
                    "    ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "            WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) AS OnHand, " +
                    "    CASE WHEN EXISTS (SELECT 1 FROM StockMovements sm " +
                    "                      WHERE sm.ProductId = p.ProductId) THEN 1 ELSE 0 END AS HasActivity " +
                    "  FROM Products p " +
                    "  LEFT JOIN ProductCategories c ON c.CategoryId = p.CategoryId " +
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

    protected void txtSearch_TextChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadProducts();
    }

    protected void ddlFilterType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadProducts();
    }

    protected void ddlFilterActive_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadProducts();
    }

    protected void ddlFilterDeleted_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadProducts();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadProducts(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadProducts();
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
    protected void rptProducts_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int productId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out productId) || productId <= 0)
            return;

        if (e.CommandName == "Edit") LoadProductForEdit(productId);
        else if (e.CommandName == "SoftDelete") SoftDeleteProduct(productId);
        else if (e.CommandName == "Restore") RestoreProduct(productId);
        else if (e.CommandName == "ToggleActive") ToggleActiveProduct(productId);
    }

    // ─────────────────────────────────────────────────────
    // ItemCommand — گالری تصاویر
    // ─────────────────────────────────────────────────────
    protected void rptProductImages_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "DeleteImage") return;

        int imageId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out imageId) || imageId <= 0)
            return;

        DeleteProductImage(imageId);
    }

    private void LoadProductForEdit(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT ProductId, ProductCode, ProductName, CategoryId, ProductType, " +
                    "       UnitId, WeightGram, Description, MinStock, MaxStock, " +
                    "       ShelfLifeDays, RequiresBatch, IsActive, ImageUrl " +
                    "FROM Products WHERE ProductId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditProductId = productId;

                            txtProductCode.Text = Convert.ToString(dr["ProductCode"]);
                            txtProductName.Text = Convert.ToString(dr["ProductName"]);
                            txtDescription.Text = dr["Description"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Description"]);

                            int type = dr["ProductType"] == DBNull.Value
                                ? 1 : Convert.ToInt32(dr["ProductType"]);

                            rbTypeRM.Checked = (type == 1);
                            rbTypeFG.Checked = (type == 2);
                            rbTypePackaging.Checked = (type == 3);

                            ddlCategory.ClearSelection();
                            if (dr["CategoryId"] == DBNull.Value)
                            {
                                ListItem emptyItem = ddlCategory.Items.FindByValue("");
                                if (emptyItem != null) emptyItem.Selected = true;
                            }
                            else
                            {
                                string catVal = Convert.ToInt32(dr["CategoryId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem cItem = ddlCategory.Items.FindByValue(catVal);
                                if (cItem != null) cItem.Selected = true;
                            }

                            ddlUnit.ClearSelection();
                            if (dr["UnitId"] != DBNull.Value)
                            {
                                string uVal = Convert.ToInt32(dr["UnitId"]).ToString(CultureInfo.InvariantCulture);
                                ListItem uItem = ddlUnit.Items.FindByValue(uVal);
                                if (uItem != null) uItem.Selected = true;
                            }

                            txtWeightGram.Text = dr["WeightGram"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["WeightGram"]).ToString(CultureInfo.InvariantCulture);
                            txtMinStock.Text = dr["MinStock"] == DBNull.Value
                                ? "0" : Convert.ToDecimal(dr["MinStock"]).ToString(CultureInfo.InvariantCulture);
                            txtMaxStock.Text = dr["MaxStock"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["MaxStock"]).ToString(CultureInfo.InvariantCulture);
                            txtShelfLifeDays.Text = dr["ShelfLifeDays"] == DBNull.Value
                                ? "" : Convert.ToInt32(dr["ShelfLifeDays"]).ToString(CultureInfo.InvariantCulture);

                            chkRequiresBatch.Checked = dr["RequiresBatch"] != DBNull.Value
                                && Convert.ToBoolean(dr["RequiresBatch"]);

                            bool isActive = dr["IsActive"] == DBNull.Value
                                ? true : Convert.ToBoolean(dr["IsActive"]);
                            ddlIsActive.ClearSelection();
                            ListItem activeItem = ddlIsActive.Items.FindByValue(isActive ? "1" : "0");
                            if (activeItem != null) activeItem.Selected = true;

                            litFormTitle.Text = "ویرایش محصول — " + Convert.ToString(dr["ProductName"]);
                        }
                        else
                        {
                            ShowMessage("محصول مورد نظر یافت نشد.");
                            return;
                        }
                    }
                }

                // ✅ بارگذاری گالری تصاویر (بعد از بستن DataReader اصلی)
                LoadProductImages(productId);

                pnlForm.Visible = true;
                pnlList.Visible = false;
                pnlStats.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری محصول: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════
    // گالری تصاویر محصول
    // ═══════════════════════════════════════════════════════
    private void LoadProductImages(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                DataTable dt = new DataTable();
                string sql =
                    "SELECT ImageId, ImageUrl, IsPrimary, SortOrder " +
                    "FROM ProductImages " +
                    "WHERE ProductId = @Id AND IsDeleted = 0 " +
                    "ORDER BY IsPrimary DESC, SortOrder ASC, ImageId ASC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        da.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    pnlExistingImages.Visible = true;
                    litImageCount.Text = dt.Rows.Count.ToString(CultureInfo.InvariantCulture);
                    rptProductImages.DataSource = dt;
                    rptProductImages.DataBind();
                }
                else
                {
                    pnlExistingImages.Visible = false;
                    litImageCount.Text = "0";
                    rptProductImages.DataSource = null;
                    rptProductImages.DataBind();
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تصاویر: " + ex.Message);
        }
    }

    private void SaveNewProductImages(SqlConnection conn, int productId, string productCode)
    {
        if (productId <= 0) return;
        if (fuProductImages == null || fuProductImages.PostedFiles == null) return;
        if (fuProductImages.PostedFiles.Count == 0) return;

        // شماره ترتیب بعدی
        int nextSort = 0;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(SortOrder), -1) + 1 FROM ProductImages WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            if (o != null && o != DBNull.Value) nextSort = Convert.ToInt32(o);
        }

        // آیا محصول از قبل عکس اصلی دارد؟
        bool hasPrimary = false;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM ProductImages WHERE ProductId = @Id AND IsPrimary = 1 AND IsDeleted = 0", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            hasPrimary = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        int savedIndex = 0;
        foreach (HttpPostedFile posted in fuProductImages.PostedFiles)
        {
            if (posted == null || posted.ContentLength <= 0) continue;

            string err;
            string url = SavePostedImage(posted, productCode, out err);
            if (!string.IsNullOrEmpty(err))
            {
                ShowMessage(err);
                continue;
            }
            if (string.IsNullOrEmpty(url)) continue;

            bool isPrimary = (!hasPrimary && savedIndex == 0);

            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO ProductImages (ProductId, ImageUrl, SortOrder, IsPrimary, CreatedAt, CreatedBy) " +
                "VALUES (@Pid, @Url, @Sort, @Prim, GETDATE(), @By)", conn))
            {
                cmd.Parameters.AddWithValue("@Pid", productId);
                cmd.Parameters.AddWithValue("@Url", url);
                cmd.Parameters.AddWithValue("@Sort", nextSort + savedIndex);
                cmd.Parameters.AddWithValue("@Prim", isPrimary);
                cmd.Parameters.AddWithValue("@By",
                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            // همگام‌سازی Products.ImageUrl با عکس اصلی
            if (isPrimary)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Products SET ImageUrl = @Url WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Url", url);
                    cmd.Parameters.AddWithValue("@Id", productId);
                    cmd.ExecuteNonQuery();
                }
                hasPrimary = true;
            }

            savedIndex++;
        }
    }

    private string SavePostedImage(HttpPostedFile posted, string productCode, out string error)
    {
        error = null;
        try
        {
            if (posted == null || posted.ContentLength == 0)
            {
                error = "فایلی انتخاب نشده است.";
                return null;
            }

            const int maxSize = 3 * 1024 * 1024;
            if (posted.ContentLength > maxSize)
            {
                error = "حجم هر تصویر نمی‌تواند بیش از ۳ مگابایت باشد.";
                return null;
            }

            string ext = Path.GetExtension(posted.FileName);
            if (string.IsNullOrEmpty(ext)) ext = "";
            ext = ext.ToLowerInvariant();

            string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            bool ok = false;
            for (int i = 0; i < allowed.Length; i++)
                if (allowed[i] == ext) { ok = true; break; }

            if (!ok)
            {
                error = "فرمت تصویر مجاز نیست. فقط JPG، PNG، GIF و WebP.";
                return null;
            }

            string contentType = posted.ContentType != null
                ? posted.ContentType.ToLowerInvariant() : "";
            if (contentType.IndexOf("image/") != 0)
            {
                error = "فایل انتخاب‌شده یک تصویر معتبر نیست.";
                return null;
            }

            string safeCode = MakeSafeFileName(productCode);
            string uniqueName = safeCode
                + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                + "_" + Guid.NewGuid().ToString("N").Substring(0, 6)
                + ext;

            string folderVirtual = "~/Images/Products/";
            string folderPhysical = Server.MapPath(folderVirtual);
            if (!Directory.Exists(folderPhysical))
                Directory.CreateDirectory(folderPhysical);

            string physicalPath = Path.Combine(folderPhysical, uniqueName);
            posted.SaveAs(physicalPath);

            return folderVirtual + uniqueName;
        }
        catch (Exception ex)
        {
            error = "خطا در ذخیره تصویر: " + ex.Message;
            return null;
        }
    }

    private string MakeSafeFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "img";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            bool ok = (c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == '-' || c == '_';
            if (ok) sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "img";
    }

    private void DeleteProductImage(int imageId)
    {
        try
        {
            int productId = 0;
            bool wasPrimary = false;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // خواندن اطلاعات عکس
                using (SqlCommand get = new SqlCommand(
                    "SELECT ProductId, IsPrimary FROM ProductImages " +
                    "WHERE ImageId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", imageId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (!dr.Read()) { ShowMessage("تصویر یافت نشد."); return; }
                        productId = Convert.ToInt32(dr["ProductId"]);
                        wasPrimary = dr["IsPrimary"] != DBNull.Value
                            && Convert.ToBoolean(dr["IsPrimary"]);
                    }
                }

                // حذف نرم
                using (SqlCommand del = new SqlCommand(
                    "UPDATE ProductImages SET IsDeleted = 1 WHERE ImageId = @Id", conn))
                {
                    del.Parameters.AddWithValue("@Id", imageId);
                    del.ExecuteNonQuery();
                }

                // اگر عکس اصلی بود، جانشین انتخاب کن
                if (wasPrimary)
                {
                    int newPrimaryId = 0;
                    string newPrimaryUrl = null;

                    using (SqlCommand next = new SqlCommand(
                        "SELECT TOP 1 ImageId, ImageUrl FROM ProductImages " +
                        "WHERE ProductId = @Pid AND IsDeleted = 0 " +
                        "ORDER BY SortOrder ASC, ImageId ASC", conn))
                    {
                        next.Parameters.AddWithValue("@Pid", productId);
                        using (SqlDataReader dr = next.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                newPrimaryId = Convert.ToInt32(dr["ImageId"]);
                                newPrimaryUrl = Convert.ToString(dr["ImageUrl"]);
                            }
                        }
                    }

                    if (newPrimaryId > 0)
                    {
                        using (SqlCommand up = new SqlCommand(
                            "UPDATE ProductImages SET IsPrimary = 1 WHERE ImageId = @Id", conn))
                        {
                            up.Parameters.AddWithValue("@Id", newPrimaryId);
                            up.ExecuteNonQuery();
                        }
                    }

                    // همگام‌سازی Products.ImageUrl
                    using (SqlCommand sync = new SqlCommand(
                        "UPDATE Products SET ImageUrl = @Url WHERE ProductId = @Id", conn))
                    {
                        sync.Parameters.AddWithValue("@Url",
                            (object)newPrimaryUrl ?? DBNull.Value);
                        sync.Parameters.AddWithValue("@Id", productId);
                        sync.ExecuteNonQuery();
                    }
                }

                WriteAudit(conn, "DeleteImage", productId,
                    "Image #" + imageId.ToString(CultureInfo.InvariantCulture),
                    "حذف تصویر محصول");
            }

            ShowMessage("تصویر با موفقیت حذف شد.");
            if (EditProductId > 0) LoadProductImages(EditProductId);
            LoadProducts();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف تصویر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // تعیین نوع انتخاب‌شده
    // ─────────────────────────────────────────────────────
    private int GetSelectedProductType()
    {
        if (rbTypeFG.Checked) return 2;
        if (rbTypePackaging.Checked) return 3;
        return 1;
    }

    // ─────────────────────────────────────────────────────
    // ذخیره
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditProductId;

        string productCode = txtProductCode.Text.Trim();
        string productName = txtProductName.Text.Trim();
        string description = txtDescription.Text.Trim();
        bool isActive = (ddlIsActive.SelectedValue == "1");
        bool requiresBatch = chkRequiresBatch.Checked;

        int productType = GetSelectedProductType();

        int unitId;
        if (!int.TryParse(ddlUnit.SelectedValue, out unitId))
        {
            ShowMessage("واحد اندازه‌گیری را انتخاب کنید.");
            return;
        }

        int? categoryId = null;
        if (!string.IsNullOrEmpty(ddlCategory.SelectedValue))
        {
            int cid;
            if (int.TryParse(ddlCategory.SelectedValue, out cid) && cid > 0)
                categoryId = cid;
        }

        if (productCode.Length == 0)
        {
            ShowMessage("کد محصول را وارد کنید.");
            return;
        }
        if (productCode.Length > 50)
        {
            ShowMessage("کد محصول نمی‌تواند بیش از ۵۰ کاراکتر باشد.");
            return;
        }
        if (!IsValidProductCode(productCode))
        {
            ShowMessage("کد محصول فقط می‌تواند شامل حروف انگلیسی، عدد، خط تیره، آندرلاین و نقطه باشد.");
            return;
        }
        if (productName.Length == 0)
        {
            ShowMessage("نام محصول را وارد کنید.");
            return;
        }
        if (productName.Length > 200)
        {
            ShowMessage("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            return;
        }
        if (description.Length > 500)
        {
            ShowMessage("توضیحات نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
            return;
        }

        // ── وزن: فقط برای FG ──
        decimal? weightGram = null;
        if (productType == 2)
        {
            weightGram = ParseDecimal(txtWeightGram.Text);
            if (txtWeightGram.Text.Trim().Length > 0 && !weightGram.HasValue)
            {
                ShowMessage("وزن وارد شده معتبر نیست.");
                return;
            }
            if (weightGram.HasValue && weightGram.Value < 0)
            {
                ShowMessage("وزن نمی‌تواند منفی باشد.");
                return;
            }
        }

        decimal minStock = 0m;
        if (txtMinStock.Text.Trim().Length > 0)
        {
            decimal? m = ParseDecimal(txtMinStock.Text);
            if (!m.HasValue)
            {
                ShowMessage("حداقل موجودی معتبر نیست.");
                return;
            }
            if (m.Value < 0)
            {
                ShowMessage("حداقل موجودی نمی‌تواند منفی باشد.");
                return;
            }
            minStock = m.Value;
        }

        decimal? maxStock = ParseDecimal(txtMaxStock.Text);
        if (txtMaxStock.Text.Trim().Length > 0 && !maxStock.HasValue)
        {
            ShowMessage("حداکثر موجودی معتبر نیست.");
            return;
        }
        if (maxStock.HasValue && maxStock.Value < minStock)
        {
            ShowMessage("حداکثر موجودی نمی‌تواند کمتر از حداقل باشد.");
            return;
        }

        int? shelfLifeDays = ParseInt(txtShelfLifeDays.Text);
        if (txtShelfLifeDays.Text.Trim().Length > 0 && !shelfLifeDays.HasValue)
        {
            ShowMessage("مدت ماندگاری معتبر نیست.");
            return;
        }
        if (shelfLifeDays.HasValue && shelfLifeDays.Value <= 0)
        {
            ShowMessage("مدت ماندگاری باید بزرگ‌تر از صفر باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── چک تکراری بودن کد ──
                string dupSql = "SELECT COUNT(*) FROM Products WHERE ProductCode = @Code";
                if (currentId > 0) dupSql += " AND ProductId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Code", productCode);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);

                    int cnt = Convert.ToInt32(dup.ExecuteScalar());
                    if (cnt > 0)
                    {
                        ShowMessage("محصولی با این کد قبلاً ثبت شده است.");
                        return;
                    }
                }

                int savedProductId = 0;

                if (currentId > 0)
                {
                    // ── محافظت: تغییر نوع محصول ممنوع در صورت فعالیت ──
                    int oldType = GetProductType(conn, currentId);
                    if (oldType != productType && HasActivity(conn, currentId))
                    {
                        ShowMessage("این محصول دارای گردش انبار است و نوع آن قابل تغییر نیست.");
                        return;
                    }

                    string updateSql =
                        "UPDATE Products SET " +
                        "  ProductCode = @Code, ProductName = @Name, CategoryId = @CategoryId, " +
                        "  ProductType = @Type, UnitId = @UnitId, WeightGram = @Weight, " +
                        "  Description = @Description, MinStock = @MinStock, MaxStock = @MaxStock, " +
                        "  ShelfLifeDays = @ShelfLife, RequiresBatch = @RequiresBatch, " +
                        "  IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                        "WHERE ProductId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", productCode);
                        cmd.Parameters.AddWithValue("@Name", productName);
                        cmd.Parameters.AddWithValue("@CategoryId",
                            categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", productType);
                        cmd.Parameters.AddWithValue("@UnitId", unitId);
                        cmd.Parameters.AddWithValue("@Weight",
                            weightGram.HasValue ? (object)weightGram.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description",
                            description.Length > 0 ? (object)description : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MinStock", minStock);
                        cmd.Parameters.AddWithValue("@MaxStock",
                            maxStock.HasValue ? (object)maxStock.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@ShelfLife",
                            shelfLifeDays.HasValue ? (object)shelfLifeDays.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequiresBatch", requiresBatch);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UpdatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    savedProductId = currentId;

                    WriteAudit(conn, "Update", currentId, productName, "ویرایش محصول");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO Products " +
                        "  (ProductCode, ProductName, CategoryId, ProductType, UnitId, " +
                        "   WeightGram, Description, MinStock, MaxStock, ShelfLifeDays, " +
                        "   RequiresBatch, IsActive, IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@Code, @Name, @CategoryId, @Type, @UnitId, " +
                        "   @Weight, @Description, @MinStock, @MaxStock, @ShelfLife, " +
                        "   @RequiresBatch, @IsActive, 0, GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", productCode);
                        cmd.Parameters.AddWithValue("@Name", productName);
                        cmd.Parameters.AddWithValue("@CategoryId",
                            categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", productType);
                        cmd.Parameters.AddWithValue("@UnitId", unitId);
                        cmd.Parameters.AddWithValue("@Weight",
                            weightGram.HasValue ? (object)weightGram.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description",
                            description.Length > 0 ? (object)description : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MinStock", minStock);
                        cmd.Parameters.AddWithValue("@MaxStock",
                            maxStock.HasValue ? (object)maxStock.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@ShelfLife",
                            shelfLifeDays.HasValue ? (object)shelfLifeDays.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequiresBatch", requiresBatch);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            newId = Convert.ToInt32(result);
                    }

                    if (newId > 0)
                    {
                        WriteAudit(conn, "Insert", newId, productName, "ایجاد محصول جدید");
                    }

                    savedProductId = newId;
                }

                // ✅ ذخیره عکس‌های جدید (چندتایی)
                if (savedProductId > 0)
                {
                    SaveNewProductImages(conn, savedProductId, productCode);
                }
            }

            ShowMessage("اطلاعات محصول با موفقیت ذخیره شد.");

            ClearForm();
            EditProductId = 0;
            pnlForm.Visible = false;
            pnlList.Visible = true;
            pnlStats.Visible = true;
            LoadStats();
            LoadProducts();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره محصول: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Soft Delete / Restore
    // ─────────────────────────────────────────────────────
    private void SoftDeleteProduct(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT ProductName FROM Products WHERE ProductId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", productId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) productName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(productName))
                {
                    ShowMessage("محصول مورد نظر یافت نشد.");
                    return;
                }

                if (HasActivity(conn, productId))
                {
                    ShowMessage("این محصول دارای گردش انبار است و قابل حذف نیست. برای حفظ سوابق، آن را غیرفعال کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Products SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", productId, productName, "حذف نرم محصول");
            }

            ShowMessage("محصول با موفقیت حذف شد.");
            LoadStats();
            LoadProducts();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف محصول: " + ex.Message);
        }
    }

    private void RestoreProduct(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT ProductName FROM Products WHERE ProductId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", productId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) productName = Convert.ToString(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Products SET IsDeleted = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", productId, productName, "بازگردانی محصول");
            }

            ShowMessage("محصول با موفقیت بازگردانده شد.");
            LoadStats();
            LoadProducts();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بازگردانی محصول: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // فعال/غیرفعال
    // ─────────────────────────────────────────────────────
    private void ToggleActiveProduct(int productId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string productName = "";
                bool isActive = false;

                using (SqlCommand get = new SqlCommand(
                    "SELECT ProductName, IsActive FROM Products WHERE ProductId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", productId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            productName = Convert.ToString(dr["ProductName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(productName))
                {
                    ShowMessage("محصول مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Products SET IsActive = @IsActive, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE ProductId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", productId);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@UpdatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", productId, productName,
                    newState ? "فعال‌سازی محصول" : "غیرفعال‌سازی محصول");
            }

            ShowMessage("وضعیت محصول تغییر یافت.");
            LoadStats();
            LoadProducts();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تغییر وضعیت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // کمک‌کننده‌ها
    // ─────────────────────────────────────────────────────
    private bool HasActivity(SqlConnection conn, int productId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM StockMovements WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM PurchaseOrderItems WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM SalesOrderItems WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM BOMItems WHERE MaterialId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(*) FROM BOMs WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            int c = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            if (c > 0) return true;
        }

        return false;
    }

    private int GetProductType(SqlConnection conn, int productId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT ProductType FROM Products WHERE ProductId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", productId);
            object o = cmd.ExecuteScalar();
            if (o == null || o == DBNull.Value) return 0;
            return Convert.ToInt32(o);
        }
    }

    private bool IsValidProductCode(string code)
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

    private int? ParseInt(string input)
    {
        if (input == null) return null;
        input = input.Trim();
        if (input.Length == 0) return null;

        input = input.Replace(",", "").Replace("،", "");
        int i;
        if (int.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out i))
            return i;

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
                "VALUES (@UserId, @Action, 'Products', @EntityId, @NewValues, @IPAddress, GETDATE())";

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
        return s.Length == 0 || s == "—" ? "—" : HttpUtility.HtmlEncode(s);
    }

    protected string GetBatchBadge(object requiresBatchObj)
    {
        bool rb = (requiresBatchObj == null || requiresBatchObj == DBNull.Value)
            ? false : Convert.ToBoolean(requiresBatchObj);
        if (!rb) return "";
        return " <span class=\"badge bg-amber-50 text-amber-700\" style=\"font-size:10px;\">بچ</span>";
    }

    protected string GetProductIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "pickaxe";
        if (type == 2) return "package-check";
        return "box";
    }

    protected string GetProductAvatarCss(object typeObj, object isActiveObj, object isDeletedObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        bool isActive = (isActiveObj == null || isActiveObj == DBNull.Value)
            ? true : Convert.ToBoolean(isActiveObj);
        bool isDeleted = (isDeletedObj == null || isDeletedObj == DBNull.Value)
            ? false : Convert.ToBoolean(isDeletedObj);

        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-full";

        if (isDeleted) return baseC + " bg-surface-muted text-ink-400 line-through";

        string color;
        if (type == 1) color = " bg-amber-500 text-white";
        else if (type == 2) color = " bg-violet-500 text-white";
        else color = " bg-emerald-500 text-white";

        if (!isActive) return baseC + " bg-surface-muted text-ink-400";

        return baseC + color;
    }

    protected string GetTypeLabel(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "مواد اولیه";
        if (type == 2) return "محصول نهایی";
        return "بسته‌بندی";
    }

    protected string GetTypeBadgeCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "badge bg-amber-50 text-amber-700";
        if (type == 2) return "badge bg-violet-50 text-violet-700";
        return "badge bg-emerald-50 text-emerald-700";
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

    protected string GetStockBadgeCss(object onHandObj, object minStockObj)
    {
        decimal onHand = (onHandObj == null || onHandObj == DBNull.Value) ? 0m : Convert.ToDecimal(onHandObj);
        decimal minStock = (minStockObj == null || minStockObj == DBNull.Value) ? 0m : Convert.ToDecimal(minStockObj);

        if (minStock <= 0)
        {
            return onHand > 0
                ? "badge bg-brand-50 text-brand-700"
                : "badge bg-surface-muted text-ink-500";
        }

        if (onHand < minStock) return "badge bg-danger-50 text-danger-600";
        if (onHand < minStock * 1.5m) return "badge bg-warning-50 text-warning-600";
        return "badge bg-success-50 text-success-600";
    }

    protected string FormatQuantity(object value)
    {
        if (value == null || value == DBNull.Value) return "۰";
        try
        {
            decimal d = Convert.ToDecimal(value);
            if (d == 0) return "۰";
            if (d == Math.Floor(d))
                return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
            return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
        }
        catch { return "۰"; }
    }

    protected bool IsProductDeleted(object isDeletedObj)
    {
        if (isDeletedObj == null || isDeletedObj == DBNull.Value) return false;
        return Convert.ToBoolean(isDeletedObj);
    }

    protected bool CanDeleteProduct(object isDeletedObj, object hasActivityObj)
    {
        if (IsProductDeleted(isDeletedObj)) return false;
        bool hasActivity = (hasActivityObj == null || hasActivityObj == DBNull.Value)
            ? false : Convert.ToBoolean(hasActivityObj);
        return !hasActivity;
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

        if (isActive) return "return confirm('محصول «" + name + "» غیرفعال شود؟');";
        return "return confirm('محصول «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value)
            ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف محصول «" + name + "» مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // ClearForm
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        txtProductCode.Text = "";
        txtProductName.Text = "";
        txtDescription.Text = "";
        txtWeightGram.Text = "";
        txtMinStock.Text = "0";
        txtMaxStock.Text = "";
        txtShelfLifeDays.Text = "";
        chkRequiresBatch.Checked = false;

        rbTypeRM.Checked = true;
        rbTypeFG.Checked = false;
        rbTypePackaging.Checked = false;

        if (ddlCategory.Items.Count > 0)
        {
            ddlCategory.ClearSelection();
            ddlCategory.SelectedIndex = 0;
        }

        if (ddlUnit.Items.Count > 0)
        {
            ddlUnit.ClearSelection();
            ddlUnit.SelectedIndex = 0;
        }

        if (ddlIsActive.Items.Count > 0)
        {
            ddlIsActive.ClearSelection();
            ListItem activeItem = ddlIsActive.Items.FindByValue("1");
            if (activeItem != null) activeItem.Selected = true;
        }

        // ✅ ریست گالری تصاویر
        if (pnlExistingImages != null) pnlExistingImages.Visible = false;
        if (rptProductImages != null)
        {
            rptProductImages.DataSource = null;
            rptProductImages.DataBind();
        }
        if (litImageCount != null) litImageCount.Text = "0";
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