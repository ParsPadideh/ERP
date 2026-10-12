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

public partial class admin_product_categories : System.Web.UI.Page
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

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack) ShowListView();
    }

    private void ShowListView()
    {
        pnlListView.Visible = true;
        pnlFormView.Visible = false;
        phBackToList.Visible = false;
        phAddNew.Visible = true;

        LoadStats();
        LoadCategories();
    }

    private void ShowFormView(int categoryId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddNew.Visible = false;

        LoadParentDropdown(categoryId);

        if (categoryId > 0)
        {
            EditId = categoryId;
            LoadCategoryForEdit(categoryId);
            litFormTitle.Text = "ویرایش دسته";
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن دسته جدید";
        }
    }

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
                    "  SUM(CASE WHEN IsDeleted = 0 AND ParentCategoryId IS NULL THEN 1 ELSE 0 END) AS RootCount, " +
                    "  SUM(CASE WHEN IsDeleted = 0 AND ParentCategoryId IS NOT NULL THEN 1 ELSE 0 END) AS ChildCount, " +
                    "  SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCount " +
                    "FROM ProductCategories";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatRoot.Text = HttpUtility.HtmlEncode(ToInt(dr["RootCount"]).ToString("N0"));
                        litStatChild.Text = HttpUtility.HtmlEncode(ToInt(dr["ChildCount"]).ToString("N0"));
                        litStatDeleted.Text = HttpUtility.HtmlEncode(ToInt(dr["DeletedCount"]).ToString("N0"));
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
    // Load Categories — به‌صورت بازگشتی (درختی)
    // ─────────────────────────────────────────────────────
    private void LoadCategories()
    {
        try
        {
            // ۱) خواندن همه‌ی دسته‌ها
            DataTable allCategories = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT c.CategoryId, c.CategoryName, c.ParentCategoryId, ");
                sb.Append("       c.Description, c.IsActive, c.IsDeleted, ");
                sb.Append("  ISNULL((SELECT COUNT(*) FROM Products p ");
                sb.Append("          WHERE p.CategoryId = c.CategoryId AND p.IsDeleted = 0), 0) AS ProductCount ");
                sb.Append("FROM ProductCategories c WHERE 1 = 1 ");

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (!chkShowDeleted.Checked)
                        sb.Append(" AND c.IsDeleted = 0 ");

                    string activeFilter = ddlFilterActive.SelectedValue;
                    if (!string.IsNullOrEmpty(activeFilter))
                    {
                        sb.Append(" AND c.IsActive = @IsActive ");
                        cmd.Parameters.AddWithValue("@IsActive", activeFilter == "1" ? 1 : 0);
                    }

                    cmd.CommandText = sb.ToString();

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(allCategories);
                    }
                }
            }

            // ۲) اگر جستجو داشتیم، فیلتر هوشمند
            DataTable filtered = allCategories;
            string searchText = txtSearch.Text.Trim();
            if (searchText.Length > 0)
            {
                filtered = FilterCategoriesWithAncestors(allCategories, searchText);
            }

            // ۳) ساخت درخت
            List<CategoryRow> treeList = BuildTree(filtered);

            // ۴) ساخت DataTable برای Repeater
            DataTable displayTable = new DataTable();
            displayTable.Columns.Add("CategoryId", typeof(int));
            displayTable.Columns.Add("CategoryName", typeof(string));
            displayTable.Columns.Add("Description", typeof(string));
            displayTable.Columns.Add("ParentCategoryId", typeof(int));
            displayTable.Columns.Add("IsActive", typeof(bool));
            displayTable.Columns.Add("IsDeleted", typeof(bool));
            displayTable.Columns.Add("ProductCount", typeof(int));
            displayTable.Columns.Add("ChildCount", typeof(int));
            displayTable.Columns.Add("Depth", typeof(int));

            foreach (CategoryRow cat in treeList)
            {
                DataRow row = displayTable.NewRow();
                row["CategoryId"] = cat.CategoryId;
                row["CategoryName"] = cat.CategoryName;
                row["Description"] = cat.Description != null
                    ? (object)cat.Description : DBNull.Value;
                row["ParentCategoryId"] = cat.ParentCategoryId.HasValue
                    ? (object)cat.ParentCategoryId.Value : DBNull.Value;
                row["IsActive"] = cat.IsActive;
                row["IsDeleted"] = cat.IsDeleted;
                row["ProductCount"] = cat.ProductCount;
                row["ChildCount"] = cat.ChildCount;
                row["Depth"] = cat.Depth;
                displayTable.Rows.Add(row);
            }

            // ۵) Bind
            if (displayTable.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptCategories.Visible = false;
                rptCategories.DataSource = null;
                rptCategories.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptCategories.Visible = true;
                rptCategories.DataSource = displayTable;
                rptCategories.DataBind();
            }

            litListInfo.Text = string.Format("{0} دسته",
                displayTable.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // فیلتر هوشمند: نمایش دسته‌های منطبق + همه والدهایشان
    // ─────────────────────────────────────────────────────
    private DataTable FilterCategoriesWithAncestors(DataTable all, string searchText)
    {
        Dictionary<int, DataRow> byId = new Dictionary<int, DataRow>();
        foreach (DataRow r in all.Rows)
        {
            int cid = Convert.ToInt32(r["CategoryId"]);
            byId[cid] = r;
        }

        HashSet<int> keep = new HashSet<int>();

        // ۱) دسته‌های منطبق با جستجو
        foreach (DataRow r in all.Rows)
        {
            string name = Convert.ToString(r["CategoryName"]);
            if (name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int cid = Convert.ToInt32(r["CategoryId"]);
                keep.Add(cid);
                // والدها
                AddAncestors(r, byId, keep);
            }
        }

        // ۲) ساخت DataTable فیلترشده
        DataTable result = all.Clone();
        foreach (DataRow r in all.Rows)
        {
            int cid = Convert.ToInt32(r["CategoryId"]);
            if (keep.Contains(cid))
                result.ImportRow(r);
        }
        return result;
    }

    private void AddAncestors(DataRow row, Dictionary<int, DataRow> byId, HashSet<int> keep)
    {
        object parentObj = row["ParentCategoryId"];
        if (parentObj == null || parentObj == DBNull.Value) return;

        int parentId = Convert.ToInt32(parentObj);
        if (keep.Contains(parentId)) return;

        if (byId.ContainsKey(parentId))
        {
            keep.Add(parentId);
            AddAncestors(byId[parentId], byId, keep);
        }
    }

    // ─────────────────────────────────────────────────────
    // ساخت ساختار درختی
    // ─────────────────────────────────────────────────────
    private class CategoryRow
    {
        public int CategoryId;
        public string CategoryName;
        public string Description;
        public int? ParentCategoryId;
        public bool IsActive;
        public bool IsDeleted;
        public int ProductCount;
        public int ChildCount;
        public int Depth;
    }

    private List<CategoryRow> BuildTree(DataTable all)
    {
        List<CategoryRow> list = new List<CategoryRow>();
        Dictionary<int, CategoryRow> byId = new Dictionary<int, CategoryRow>();
        Dictionary<int, List<CategoryRow>> byParent = new Dictionary<int, List<CategoryRow>>();
        List<CategoryRow> roots = new List<CategoryRow>();

        // ۱) تبدیل هر DataRow به CategoryRow
        foreach (DataRow r in all.Rows)
        {
            CategoryRow cat = new CategoryRow();
            cat.CategoryId = Convert.ToInt32(r["CategoryId"]);
            cat.CategoryName = Convert.ToString(r["CategoryName"]);
            cat.Description = (r["Description"] == DBNull.Value)
                ? null : Convert.ToString(r["Description"]);
            cat.ParentCategoryId = (r["ParentCategoryId"] == DBNull.Value)
                ? (int?)null : Convert.ToInt32(r["ParentCategoryId"]);
            cat.IsActive = Convert.ToBoolean(r["IsActive"]);
            cat.IsDeleted = Convert.ToBoolean(r["IsDeleted"]);
            cat.ProductCount = Convert.ToInt32(r["ProductCount"]);
            cat.ChildCount = 0;
            cat.Depth = 0;

            list.Add(cat);
            byId[cat.CategoryId] = cat;

            // ✅ ریشه‌ها جدا، بقیه در byParent
            if (cat.ParentCategoryId.HasValue)
            {
                int pid = cat.ParentCategoryId.Value;
                if (!byParent.ContainsKey(pid))
                    byParent[pid] = new List<CategoryRow>();
                byParent[pid].Add(cat);
            }
            else
            {
                roots.Add(cat);
            }
        }

        // ۲) محاسبه ChildCount
        foreach (CategoryRow cat in list)
        {
            if (byParent.ContainsKey(cat.CategoryId))
                cat.ChildCount = byParent[cat.CategoryId].Count;
            else
                cat.ChildCount = 0;
        }

        // ۳) مرتب‌سازی ریشه‌ها بر اساس نام
        roots.Sort(delegate(CategoryRow a, CategoryRow b)
        {
            return string.Compare(a.CategoryName, b.CategoryName, StringComparison.OrdinalIgnoreCase);
        });

        // ۴) مرتب‌سازی فرزندها بر اساس نام
        foreach (KeyValuePair<int, List<CategoryRow>> kv in byParent)
        {
            kv.Value.Sort(delegate(CategoryRow a, CategoryRow b)
            {
                return string.Compare(a.CategoryName, b.CategoryName, StringComparison.OrdinalIgnoreCase);
            });
        }

        // ۵) پیمایش درختی از ریشه‌ها
        List<CategoryRow> result = new List<CategoryRow>();
        foreach (CategoryRow root in roots)
        {
            WalkTree(root, byParent, result, 0);
        }

        // ۶) اگر دسته‌ی یتیمی ماند (والدش فیلتر شده بود)، در آخر اضافه کن
        HashSet<int> added = new HashSet<int>();
        foreach (CategoryRow c in result)
            added.Add(c.CategoryId);

        foreach (CategoryRow c in list)
        {
            if (!added.Contains(c.CategoryId))
            {
                c.Depth = 0;
                result.Add(c);
            }
        }

        return result;
    }

    private void WalkTree(CategoryRow cat, Dictionary<int, List<CategoryRow>> byParent,
        List<CategoryRow> result, int depth)
    {
        cat.Depth = depth;
        result.Add(cat);

        if (byParent.ContainsKey(cat.CategoryId))
        {
            List<CategoryRow> children = byParent[cat.CategoryId];
            for (int i = 0; i < children.Count; i++)
            {
                WalkTree(children[i], byParent, result, depth + 1);
            }
        }
    }

    protected void FilterChanged(object sender, EventArgs e) { LoadCategories(); }

    // ─────────────────────────────────────────────────────
    // Load Parent Dropdown
    // ─────────────────────────────────────────────────────
    private void LoadParentDropdown(int excludeId)
    {
        ddlParentCategory.Items.Clear();
        ddlParentCategory.Items.Add(new ListItem("— بدون والد (دسته اصلی) —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql = "SELECT CategoryId, CategoryName, ParentCategoryId " +
                    "FROM ProductCategories WHERE IsDeleted = 0 ";

                if (excludeId > 0)
                    sql += " AND CategoryId <> @ExcludeId ";

                sql += " ORDER BY CategoryName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (excludeId > 0)
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int id = Convert.ToInt32(dr["CategoryId"]);
                            if (excludeId > 0 && IsDescendantOf(id, excludeId)) continue;

                            string name = Convert.ToString(dr["CategoryName"]);
                            ddlParentCategory.Items.Add(new ListItem(
                                name, id.ToString(CultureInfo.InvariantCulture)));
                        }
                    }
                }
            }
        }
        catch { }
    }

    private bool IsDescendantOf(int childId, int potentialAncestorId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                int current = childId;
                int safety = 0;
                while (current > 0 && safety++ < 50)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT ISNULL(ParentCategoryId, 0) FROM ProductCategories " +
                        "WHERE CategoryId = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", current);
                        object o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) break;
                        int parent = Convert.ToInt32(o);
                        if (parent == potentialAncestorId) return true;
                        if (parent == 0) break;
                        current = parent;
                    }
                }
            }
        }
        catch { }
        return false;
    }

    private void LoadCategoryForEdit(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CategoryName, ParentCategoryId, Description, IsActive " +
                    "FROM ProductCategories WHERE CategoryId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtCategoryName.Text = Convert.ToString(dr["CategoryName"]);
                            txtDescription.Text = dr["Description"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Description"]);

                            ddlParentCategory.ClearSelection();
                            if (dr["ParentCategoryId"] != DBNull.Value)
                            {
                                string pid = Convert.ToInt32(dr["ParentCategoryId"])
                                    .ToString(CultureInfo.InvariantCulture);
                                ListItem pi = ddlParentCategory.Items.FindByValue(pid);
                                if (pi != null) pi.Selected = true;
                            }

                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);
                        }
                        else { ShowMessage("دسته یافت نشد."); ShowListView(); }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;
        string name = txtCategoryName.Text.Trim();
        string description = txtDescription.Text.Trim();
        bool isActive = chkIsActive.Checked;

        int parentId = 0;
        if (!string.IsNullOrEmpty(ddlParentCategory.SelectedValue))
            int.TryParse(ddlParentCategory.SelectedValue, out parentId);

        if (name.Length == 0) { ShowMessage("نام دسته را وارد کنید."); return; }
        if (name.Length > 100) { ShowMessage("نام حداکثر ۱۰۰ کاراکتر."); return; }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک تکراری در بین هم‌والدها
                string dupSql =
                    "SELECT COUNT(*) FROM ProductCategories " +
                    "WHERE CategoryName = @Name AND IsDeleted = 0 " +
                    "  AND ((ParentCategoryId IS NULL AND @ParentId = 0) OR (ParentCategoryId = @ParentId))";
                if (currentId > 0) dupSql += " AND CategoryId <> @Id";

                using (SqlCommand dup = new SqlCommand(dupSql, conn))
                {
                    dup.Parameters.AddWithValue("@Name", name);
                    dup.Parameters.AddWithValue("@ParentId", parentId);
                    if (currentId > 0) dup.Parameters.AddWithValue("@Id", currentId);
                    if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    {
                        ShowMessage("دسته‌ای با این نام در همان سطح قبلاً ثبت شده است.");
                        return;
                    }
                }

                if (currentId > 0)
                {
                    string sql =
                        "UPDATE ProductCategories SET " +
                        "  CategoryName = @Name, ParentCategoryId = @ParentId, " +
                        "  Description = @Desc, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @By " +
                        "WHERE CategoryId = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@ParentId", parentId > 0 ? (object)parentId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "ProductCategories", currentId, name, "ویرایش دسته");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string sql =
                        "INSERT INTO ProductCategories " +
                        "  (CategoryName, ParentCategoryId, Description, IsActive, IsDeleted, CreatedAt) " +
                        "VALUES (@Name, @ParentId, @Desc, @IsActive, 0, GETDATE()); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@ParentId", parentId > 0 ? (object)parentId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Desc", NullIfEmpty(description));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0) WriteAudit(conn, "Insert", "ProductCategories", newId, name, "ایجاد دسته");
                    ShowMessage("دسته جدید افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void rptCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreCategory(id);
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
                    "SELECT CategoryName, IsActive FROM ProductCategories " +
                    "WHERE CategoryId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["CategoryName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("دسته یافت نشد."); return; }

                bool newState = !isActive;
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductCategories SET IsActive = @IsActive, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE CategoryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "ProductCategories", id, name,
                    newState ? "فعال‌سازی دسته" : "غیرفعال‌سازی دسته");
            }

            ShowMessage("وضعیت تغییر یافت.");
            LoadStats();
            LoadCategories();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
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
                    "SELECT CategoryName FROM ProductCategories WHERE CategoryId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("دسته یافت نشد."); return; }

                // چک زیردسته فعال
                int childCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductCategories " +
                    "WHERE ParentCategoryId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    childCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (childCount > 0)
                {
                    ShowMessage(string.Format("این دسته {0} زیردسته فعال دارد و قابل حذف نیست.", childCount));
                    return;
                }

                // چک محصولات فعال
                int prodCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Products WHERE CategoryId = @Id AND IsDeleted = 0", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    prodCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (prodCount > 0)
                {
                    ShowMessage(string.Format("این دسته {0} محصول دارد و قابل حذف نیست.", prodCount));
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductCategories SET IsDeleted = 1, IsActive = 0, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE CategoryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "ProductCategories", id, name, "حذف نرم دسته");
            }

            ShowMessage("دسته حذف شد (قابل بازیابی).");
            LoadStats();
            LoadCategories();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    private void RestoreCategory(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string name = "";
                int parentId = 0;
                using (SqlCommand get = new SqlCommand(
                    "SELECT CategoryName, ISNULL(ParentCategoryId, 0) AS ParentId " +
                    "FROM ProductCategories WHERE CategoryId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["CategoryName"]);
                            parentId = Convert.ToInt32(dr["ParentId"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name)) { ShowMessage("دسته حذف‌شده یافت نشد."); return; }

                // چک والد در دسترس
                if (parentId > 0)
                {
                    int parentDeleted = 0;
                    using (SqlCommand check = new SqlCommand(
                        "SELECT IsDeleted FROM ProductCategories WHERE CategoryId = @PId", conn))
                    {
                        check.Parameters.AddWithValue("@PId", parentId);
                        object o = check.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            parentDeleted = Convert.ToBoolean(o) ? 1 : 0;
                    }

                    if (parentDeleted == 1)
                    {
                        ShowMessage("دسته والد این مورد حذف شده است. ابتدا والد را بازیابی کنید.");
                        return;
                    }
                }

                // چک تکراری در همان سطح
                int dup = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductCategories " +
                    "WHERE CategoryName = @Name AND IsDeleted = 0 AND CategoryId <> @Id " +
                    "  AND ((ParentCategoryId IS NULL AND @PId = 0) OR (ParentCategoryId = @PId))", conn))
                {
                    check.Parameters.AddWithValue("@Name", name);
                    check.Parameters.AddWithValue("@PId", parentId);
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    dup = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (dup > 0) { ShowMessage("دسته دیگری با همین نام در همان سطح وجود دارد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ProductCategories SET IsDeleted = 0, IsActive = 1, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @By WHERE CategoryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@By", CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "ProductCategories", id, name, "بازیابی دسته");
            }

            ShowMessage("دسته بازیابی شد.");
            LoadStats();
            LoadCategories();
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }

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

    // ─── Markup Helpers ───
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

    protected string GetRowCss(object isDeletedObj)
    {
        return IsTrue(isDeletedObj)
            ? "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition"
            : "border-b border-surface-line hover:bg-surface-body/70 transition";
    }

    protected string GetCategoryIcon(object depthObj, object childCountObj)
    {
        int depth = (depthObj == null || depthObj == DBNull.Value) ? 0 : Convert.ToInt32(depthObj);
        int childCount = (childCountObj == null || childCountObj == DBNull.Value) ? 0 : Convert.ToInt32(childCountObj);
        if (childCount > 0) return "folder-tree";
        return depth == 0 ? "folder" : "folder-open";
    }

    protected string GetCategoryAvatarCss(object isActiveObj, object isDeletedObj, object depthObj)
    {
        bool isDeleted = IsTrue(isDeletedObj);
        bool isActive = IsTrue(isActiveObj);
        int depth = (depthObj == null || depthObj == DBNull.Value) ? 0 : Convert.ToInt32(depthObj);

        string baseC = "grid h-7 w-7 shrink-0 place-items-center rounded-lg";
        if (isDeleted) return baseC + " bg-danger-50 text-danger-500";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        if (depth == 0) return baseC + " bg-brand-100 text-brand-700";
        if (depth == 1) return baseC + " bg-brand-50 text-brand-600";
        return baseC + " bg-violet-50 text-violet-600";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected string GetStatusBadge(object isActiveObj, object isDeletedObj)
    {
        if (IsTrue(isDeletedObj)) return "<span class=\"badge bg-danger-50 text-danger-600\">حذف‌شده</span>";
        if (IsTrue(isActiveObj)) return "<span class=\"badge bg-success-50 text-success-600\">فعال</span>";
        return "<span class=\"badge bg-warning-50 text-warning-600\">غیرفعال</span>";
    }

    protected string GetDescriptionHint(object descObj)
    {
        if (descObj == null || descObj == DBNull.Value) return "";
        string desc = Convert.ToString(descObj).Trim();
        if (desc.Length == 0) return "";
        string preview = desc.Length > 60 ? desc.Substring(0, 57) + "..." : desc;
        return "<p class=\"mt-0.5 text-[10.5px] text-ink-400\">" + HttpUtility.HtmlEncode(preview) + "</p>";
    }

    protected bool CanEdit(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected bool CanDelete(object childCountObj, object productCountObj)
    {
        int cc = (childCountObj == null || childCountObj == DBNull.Value) ? 0 : Convert.ToInt32(childCountObj);
        int pc = (productCountObj == null || productCountObj == DBNull.Value) ? 0 : Convert.ToInt32(productCountObj);
        return cc == 0 && pc == 0;
    }

    protected string GetBlockReason(object childCountObj, object productCountObj)
    {
        int cc = (childCountObj == null || childCountObj == DBNull.Value) ? 0 : Convert.ToInt32(childCountObj);
        int pc = (productCountObj == null || productCountObj == DBNull.Value) ? 0 : Convert.ToInt32(productCountObj);
        if (cc > 0) return "این دسته زیردسته دارد";
        if (pc > 0) return "این دسته محصول دارد";
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

    protected string GetToggleConfirm(object isActiveObj, object nameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('دسته «" + name + "» غیرفعال شود؟');";
        return "return confirm('دسته «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف دسته «" + name + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('دسته «" + name + "» بازیابی شود؟');";
    }

    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
    }

    private void ClearForm()
    {
        txtCategoryName.Text = "";
        txtDescription.Text = "";
        chkIsActive.Checked = true;
        if (ddlParentCategory.Items.Count > 0)
            ddlParentCategory.SelectedIndex = 0;
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