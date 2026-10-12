using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_expenses : System.Web.UI.Page
{
    private const int PageSize = 20;

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

    private int CurrentPage
    {
        get { return ViewState["Page"] == null ? 1 : (int)ViewState["Page"]; }
        set { ViewState["Page"] = value; }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CurrentPage = 1;
            LoadCategoryFilters();
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
        phAddExpense.Visible = true;

        LoadStats();
        LoadExpenses();
    }

    private void ShowFormView(int expenseId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        phBackToList.Visible = true;
        phAddExpense.Visible = false;

        LoadCategoryFormDropdown();

        if (expenseId > 0)
        {
            LoadExpenseForEdit(expenseId);
            litFormTitle.Text = "ویرایش هزینه";
        }
        else
        {
            ClearForm();
            EditId = 0;
            litFormTitle.Text = "ثبت هزینه جدید";
        }
    }

    // ─────────────────────────────────────────────────────
    // Stats — با Soft Delete
    // ─────────────────────────────────────────────────────
    private void LoadStats()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                DateTime monthStart = GetCurrentPersianMonthStart();
                DateTime monthEnd = GetCurrentPersianMonthEnd();

                // 1) Total (non-deleted, all-time)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    decimal total = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                    litStatTotal.Text = HttpUtility.HtmlEncode(FormatMoneyRaw(total));
                }

                // 2) This month total (non-deleted)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    decimal sum = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                    litStatMonth.Text = HttpUtility.HtmlEncode(FormatMoneyRaw(sum));
                }

                // 3) This month count (non-deleted)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    int cnt = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatCount.Text = HttpUtility.HtmlEncode(cnt.ToString("N0"));
                }

                // 4) Deleted count (all-time)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Expenses WHERE IsDeleted = 1", conn))
                {
                    object o = cmd.ExecuteScalar();
                    int cnt = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatDeleted.Text = HttpUtility.HtmlEncode(cnt.ToString("N0"));
                }
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

    private DateTime GetCurrentPersianMonthEnd()
    {
        return new PersianCalendar().AddMonths(GetCurrentPersianMonthStart(), 1);
    }

    // ─────────────────────────────────────────────────────
    // Category Filters — فقط دسته‌های غیرحذف‌شده
    // ─────────────────────────────────────────────────────
    private void LoadCategoryFilters()
    {
        ddlFilterCategory.Items.Clear();
        ddlFilterCategory.Items.Add(new ListItem("همه دسته‌ها", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ExpenseCategoryId, CategoryName FROM ExpenseCategories " +
                    "WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY CategoryName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ExpenseCategoryId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["CategoryName"]);
                        ddlFilterCategory.Items.Add(new ListItem(name, id));
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا در بارگذاری دسته‌ها: " + ex.Message); }
    }

    private void LoadCategoryFormDropdown()
    {
        ddlCategory.Items.Clear();
        ddlCategory.Items.Add(new ListItem("— انتخاب دسته —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ExpenseCategoryId, CategoryName FROM ExpenseCategories " +
                    "WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY CategoryName", conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ExpenseCategoryId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["CategoryName"]);
                        ddlCategory.Items.Add(new ListItem(name, id));
                    }
                }
            }

            if (ddlCategory.Items.Count <= 1)
            {
                ddlCategory.Items.Add(new ListItem(
                    "— هیچ دسته فعالی وجود ندارد — ابتدا از بخش «دسته‌بندی هزینه‌ها» بسازید", ""));
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Load Expenses — با فیلتر Soft Delete
    // ─────────────────────────────────────────────────────
    private void LoadExpenses()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string catFilter = ddlFilterCategory.SelectedValue;
            if (catFilter == null) catFilter = "";
            DateTime? fromDate = ParsePersianDate(txtFilterFrom.Text.Trim());
            DateTime? toDate = ParsePersianDate(txtFilterTo.Text.Trim());

            DateTime? toDateEnd = null;
            if (toDate.HasValue)
                toDateEnd = toDate.Value.AddDays(1).AddSeconds(-1);

            int totalCount = GetTotalCount(search, catFilter, fromDate, toDateEnd);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPagedData(search, catFilter, fromDate, toDateEnd, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptExpenses.Visible = false;
                rptExpenses.DataSource = null;
                rptExpenses.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptExpenses.Visible = true;
                rptExpenses.DataSource = dt;
                rptExpenses.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litListInfo.Text = string.Format("نمایش {0} تا {1} از {2} رکورد",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری هزینه‌ها: " + ex.Message);
        }
    }

    private string BuildFilterSql(string search, string catFilter, DateTime? fromDate, DateTime? toDate, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE 1 = 1 ");

        // ✅ فیلتر Soft Delete
        if (!chkShowDeleted.Checked)
            sb.Append(" AND e.IsDeleted = 0 ");

        if (search.Length > 0)
        {
            sb.Append(" AND (e.Title LIKE @Search OR e.ReferenceNumber LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        int catId;
        if (catFilter.Length > 0 && int.TryParse(catFilter, out catId) && catId > 0)
        {
            sb.Append(" AND e.ExpenseCategoryId = @CatId ");
            cmd.Parameters.AddWithValue("@CatId", catId);
        }

        if (fromDate.HasValue)
        {
            sb.Append(" AND e.ExpenseDate >= @FromDate ");
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sb.Append(" AND e.ExpenseDate <= @ToDate ");
            cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string catFilter, DateTime? fromDate, DateTime? toDate)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, catFilter, fromDate, toDate, cmd);
                cmd.CommandText = "SELECT COUNT(*) FROM Expenses e " + filter;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPagedData(string search, string catFilter, DateTime? fromDate, DateTime? toDate,
        int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, catFilter, fromDate, toDate, cmd);

                // ═══════════════════════════════════════════════════════
                // ✅ ترتیب پویا: وقتی «نمایش حذف‌شده‌ها» تیک است،
                //    حذف‌شده‌ها اول بیایند تا کاربر فوراً ببیند
                // ═══════════════════════════════════════════════════════
                string orderBy;
                if (chkShowDeleted.Checked)
                    orderBy = "e.IsDeleted DESC, e.ExpenseDate DESC, e.ExpenseId DESC";
                else
                    orderBy = "e.ExpenseDate DESC, e.ExpenseId DESC";

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY " + orderBy + ") AS RowNum, " +
                    "    e.ExpenseId, e.Title, e.Amount, e.ExpenseDate, e.PaymentMethod, " +
                    "    e.ReferenceNumber, e.Notes, e.CreatedAt, e.IsDeleted, " +
                    "    ec.CategoryName " +
                    "  FROM Expenses e " +
                    "  INNER JOIN ExpenseCategories ec ON ec.ExpenseCategoryId = e.ExpenseCategoryId " +
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

    // ─────────────────────────────────────────────────────
    // Filter Events
    // ─────────────────────────────────────────────────────
    protected void FilterChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadExpenses();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        if (ddlFilterCategory.Items.Count > 0) ddlFilterCategory.SelectedIndex = 0;
        txtFilterFrom.Text = "";
        txtFilterTo.Text = "";
        chkShowDeleted.Checked = false;
        CurrentPage = 1;
        LoadExpenses();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadExpenses(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadExpenses();
    }

    // ─────────────────────────────────────────────────────
    // Load for Edit
    // ─────────────────────────────────────────────────────
    private void LoadExpenseForEdit(int expenseId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ExpenseCategoryId, Title, Amount, ExpenseDate, " +
                    "       PaymentMethod, ReferenceNumber, Notes " +
                    "FROM Expenses WHERE ExpenseId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", expenseId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            EditId = expenseId;

                            ddlCategory.ClearSelection();
                            if (dr["ExpenseCategoryId"] != DBNull.Value)
                            {
                                string catId = Convert.ToInt32(dr["ExpenseCategoryId"])
                                    .ToString(CultureInfo.InvariantCulture);
                                ListItem item = ddlCategory.Items.FindByValue(catId);
                                if (item != null) item.Selected = true;
                            }

                            txtTitle.Text = Convert.ToString(dr["Title"]);
                            txtAmount.Text = dr["Amount"] == DBNull.Value
                                ? "" : Convert.ToDecimal(dr["Amount"]).ToString(CultureInfo.InvariantCulture);
                            txtExpenseDate.Text = dr["ExpenseDate"] == DBNull.Value
                                ? "" : ToShamsiDateString(Convert.ToDateTime(dr["ExpenseDate"]));

                            string pm = dr["PaymentMethod"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PaymentMethod"]);
                            ddlPaymentMethod.ClearSelection();
                            ListItem pmItem = ddlPaymentMethod.Items.FindByValue(pm);
                            if (pmItem != null) pmItem.Selected = true;

                            txtReferenceNumber.Text = dr["ReferenceNumber"] == DBNull.Value
                                ? "" : Convert.ToString(dr["ReferenceNumber"]);
                            txtNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);
                        }
                        else
                        {
                            ShowMessage("هزینه مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex) { ShowMessage("خطا: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;

        int categoryId;
        if (string.IsNullOrEmpty(ddlCategory.SelectedValue)
            || !int.TryParse(ddlCategory.SelectedValue, out categoryId)
            || categoryId <= 0)
        {
            ShowMessage("دسته هزینه را انتخاب کنید.");
            return;
        }

        string title = txtTitle.Text.Trim();
        if (title.Length == 0) { ShowMessage("عنوان هزینه را وارد کنید."); return; }

        decimal? amount = ParseDecimal(txtAmount.Text);
        if (!amount.HasValue || amount.Value <= 0)
        {
            ShowMessage("مبلغ باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        DateTime? expenseDate = ParsePersianDate(txtExpenseDate.Text.Trim());
        if (!expenseDate.HasValue)
        {
            ShowMessage("تاریخ هزینه معتبر نیست. فرمت صحیح: 1403/01/15");
            return;
        }

        string paymentMethod = ddlPaymentMethod.SelectedValue;
        if (paymentMethod == null) paymentMethod = "";

        string refNum = txtReferenceNumber.Text.Trim();
        string notes = txtNotes.Text.Trim();

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (currentId > 0)
                {
                    string updateSql =
                        "UPDATE Expenses SET " +
                        "  ExpenseCategoryId = @CatId, Title = @Title, Amount = @Amount, " +
                        "  ExpenseDate = @ExpDate, PaymentMethod = @PM, " +
                        "  ReferenceNumber = @Ref, Notes = @Notes " +
                        "WHERE ExpenseId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CatId", categoryId);
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Amount", amount.Value);
                        cmd.Parameters.AddWithValue("@ExpDate", expenseDate.Value);
                        cmd.Parameters.AddWithValue("@PM", paymentMethod.Length > 0 ? (object)paymentMethod : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ref", refNum.Length > 0 ? (object)refNum : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "Expenses", currentId, title, "ویرایش هزینه");
                    ShowMessage("تغییرات ذخیره شد.");
                }
                else
                {
                    string insertSql =
                        "INSERT INTO Expenses " +
                        "  (ExpenseCategoryId, Title, Amount, ExpenseDate, PaymentMethod, " +
                        "   ReferenceNumber, Notes, IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@CatId, @Title, @Amount, @ExpDate, @PM, " +
                        "   @Ref, @Notes, 0, GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CatId", categoryId);
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Amount", amount.Value);
                        cmd.Parameters.AddWithValue("@ExpDate", expenseDate.Value);
                        cmd.Parameters.AddWithValue("@PM", paymentMethod.Length > 0 ? (object)paymentMethod : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ref", refNum.Length > 0 ? (object)refNum : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                        WriteAudit(conn, "Insert", "Expenses", newId, title, "ثبت هزینه جدید");

                    ShowMessage("هزینه ثبت شد.");
                }
            }

            CurrentPage = 1;
            ShowListView();
        }
        catch (Exception ex) { ShowMessage("خطا در ذخیره: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Repeater commands
    // ─────────────────────────────────────────────────────
    protected void rptExpenses_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "SoftDelete") SoftDelete(id);
        else if (e.CommandName == "Restore") RestoreExpense(id);
    }

    // ═══════════════════════════════════════════════════════
    // Soft Delete
    // ═══════════════════════════════════════════════════════
    private void SoftDelete(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string title = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT Title FROM Expenses WHERE ExpenseId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) title = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(title)) { ShowMessage("هزینه یافت نشد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Expenses SET IsDeleted = 1 WHERE ExpenseId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "SoftDelete", "Expenses", id, title, "حذف نرم هزینه");
            }

            ShowMessage("هزینه حذف شد (قابل بازیابی).");
            LoadStats();
            LoadExpenses();
        }
        catch (Exception ex) { ShowMessage("خطا در حذف: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════
    // Restore
    // ═══════════════════════════════════════════════════════
    private void RestoreExpense(int id)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string title = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT Title FROM Expenses WHERE ExpenseId = @Id AND IsDeleted = 1", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) title = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(title)) { ShowMessage("هزینه حذف‌شده یافت نشد."); return; }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Expenses SET IsDeleted = 0 WHERE ExpenseId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Restore", "Expenses", id, title, "بازیابی هزینه");
            }

            ShowMessage("هزینه بازیابی شد.");
            LoadStats();
            LoadExpenses();
        }
        catch (Exception ex) { ShowMessage("خطا در بازیابی: " + ex.Message); }
    }

    // ─────────────────────────────────────────────────────
    // Form Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e) { EditId = 0; ShowFormView(0); }
    protected void btnCancelForm_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }
    protected void btnBackToList_Click(object sender, EventArgs e) { EditId = 0; ShowListView(); }

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

    private string FormatMoneyRaw(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    protected string FormatRef(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        string s = Convert.ToString(value).Trim();
        if (s.Length == 0) return "—";
        return HttpUtility.HtmlEncode(s);
    }

    protected string GetNotesHint(object notesObj)
    {
        if (notesObj == null || notesObj == DBNull.Value) return "";
        string notes = Convert.ToString(notesObj).Trim();
        if (notes.Length == 0) return "";
        string preview = notes.Length > 60 ? notes.Substring(0, 57) + "..." : notes;
        return "<p class=\"mt-0.5 text-[11.5px] text-ink-400\">" + HttpUtility.HtmlEncode(preview) + "</p>";
    }

    protected string GetPaymentBadge(object pmObj)
    {
        if (pmObj == null || pmObj == DBNull.Value) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";
        string pm = Convert.ToString(pmObj).Trim();
        if (pm.Length == 0) return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        string css = "badge bg-surface-muted text-ink-600";
        if (pm == "نقدی") css = "badge bg-emerald-50 text-emerald-700";
        else if (pm == "کارت بانکی") css = "badge bg-brand-50 text-brand-700";
        else if (pm == "انتقال بانکی") css = "badge bg-violet-50 text-violet-700";
        else if (pm == "چک") css = "badge bg-amber-50 text-amber-700";

        return "<span class=\"" + css + "\">" + HttpUtility.HtmlEncode(pm) + "</span>";
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

    protected string GetRowCss(object isDeletedObj)
    {
        return IsTrue(isDeletedObj)
            ? "border-b border-surface-line bg-danger-50/20 opacity-60 hover:opacity-80 transition"
            : "border-b border-surface-line transition hover:bg-surface-body/70";
    }

    protected string GetDeletedBadge(object isDeletedObj)
    {
        if (!IsTrue(isDeletedObj)) return "";
        return " <span class=\"badge bg-danger-50 text-danger-600 text-[10px]\">حذف‌شده</span>";
    }

    protected bool CanEdit(object isDeletedObj) { return !IsTrue(isDeletedObj); }

    protected string GetDeleteConfirm(object titleObj)
    {
        string title = (titleObj == null || titleObj == DBNull.Value)
            ? "" : titleObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف هزینه «" + title + "» مطمئن هستید؟\\n\\nتوجه: قابل بازیابی است.');";
    }

    protected string GetRestoreConfirm(object titleObj)
    {
        string title = (titleObj == null || titleObj == DBNull.Value)
            ? "" : titleObj.ToString().Replace("'", "");
        return "return confirm('هزینه «" + title + "» بازیابی شود؟');";
    }

    // ─────────────────────────────────────────────────────
    // Date Helpers
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
        input = input.Replace(",", "").Replace("،", "").Replace("٪", "").Replace(" ", "");
        decimal d;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    // ─────────────────────────────────────────────────────
    // Misc
    // ─────────────────────────────────────────────────────
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

    private void ClearForm()
    {
        if (ddlCategory.Items.Count > 0) ddlCategory.SelectedIndex = 0;
        txtTitle.Text = "";
        txtAmount.Text = "";
        txtExpenseDate.Text = ToShamsiDateString(DateTime.Now);
        if (ddlPaymentMethod.Items.Count > 0) ddlPaymentMethod.SelectedIndex = 0;
        txtReferenceNumber.Text = "";
        txtNotes.Text = "";
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