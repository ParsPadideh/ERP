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

public partial class admin_purchase_orders : System.Web.UI.Page
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

    private int EditPOId
    {
        get
        {
            int id;
            if (int.TryParse(hfEditPOId.Value, out id)) return id;
            return 0;
        }
        set { hfEditPOId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int CurrentPOId
    {
        get
        {
            int id;
            if (int.TryParse(hfCurrentPOId.Value, out id)) return id;
            return 0;
        }
        set { hfCurrentPOId.Value = value.ToString(CultureInfo.InvariantCulture); }
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

            LoadSupplierFilter();

            // پارامترهای URL
            string idParam = Request.QueryString["id"];
            string supParam = Request.QueryString["supplierId"];

            if (!string.IsNullOrEmpty(supParam))
            {
                ListItem item = ddlFilterSupplier.Items.FindByValue(supParam);
                if (item != null)
                {
                    ddlFilterSupplier.ClearSelection();
                    item.Selected = true;
                }
            }

            if (!string.IsNullOrEmpty(idParam))
            {
                int poId;
                if (int.TryParse(idParam, out poId) && poId > 0)
                {
                    ShowFormView(poId);
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

        litPageTitle.Text = "سفارش‌های خرید";
        litPageSubtitle.Text = "سفارش خرید مواد اولیه و بسته‌بندی از تأمین‌کنندگان.";

        LoadStats();
        LoadOrders();
    }

    private void ShowFormView(int poId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;

        phBackToList.Visible = true;
        phAddNew.Visible = false;

        LoadSupplierDropdown();

        if (poId > 0)
        {
            EditPOId = poId;
            CurrentPOId = poId;
            LoadOrderForEdit(poId);
        }
        else
        {
            EditPOId = 0;
            CurrentPOId = 0;
            PrepareNewOrder();
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Supplier Filter (list view)
    // ─────────────────────────────────────────────────────
    private void LoadSupplierFilter()
    {
        ddlFilterSupplier.Items.Clear();
        ddlFilterSupplier.Items.Add(new ListItem("همه تأمین‌کنندگان", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT SupplierId, SupplierName FROM Suppliers " +
                    "WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY SupplierName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["SupplierId"]).ToString(CultureInfo.InvariantCulture);
                        ddlFilterSupplier.Items.Add(new ListItem(Convert.ToString(dr["SupplierName"]), id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تأمین‌کنندگان: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Supplier Dropdown (form)
    // ─────────────────────────────────────────────────────
    private void LoadSupplierDropdown()
    {
        ddlSupplier.Items.Clear();
        ddlSupplier.Items.Add(new ListItem("— انتخاب تأمین‌کننده —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT SupplierId, SupplierName, SupplierCode FROM Suppliers " +
                    "WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY SupplierName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["SupplierId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["SupplierName"]);
                        string code = dr["SupplierCode"] == DBNull.Value ? "" : Convert.ToString(dr["SupplierCode"]);

                        ddlSupplier.Items.Add(new ListItem(name + " (" + code + ")", id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تأمین‌کنندگان: " + ex.Message);
        }
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
                    "  COUNT(*) AS TotalOrders, " +
                    "  SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS ApprovedOrders, " +
                    "  SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS ReceivedOrders, " +
                    "  ISNULL(SUM(CASE WHEN Status IN (1,2) THEN Total ELSE 0 END), 0) AS OpenValue " +
                    "FROM PurchaseOrders WHERE IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["TotalOrders"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatApproved.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["ApprovedOrders"]).ToString("N0", CultureInfo.InvariantCulture));
                        litStatReceived.Text = HttpUtility.HtmlEncode(
                            ToInt(dr["ReceivedOrders"]).ToString("N0", CultureInfo.InvariantCulture));
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
            string supFilter = ddlFilterSupplier.SelectedValue;
            if (supFilter == null) supFilter = "";

            int totalCount = GetTotalCount(search, supFilter);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, supFilter, CurrentPage, PageSize);

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

    private string BuildFilterSql(string search, string supFilter, SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(" WHERE po.IsDeleted = 0 ");

        if (StatusFilter.Length > 0)
        {
            int st;
            if (int.TryParse(StatusFilter, out st))
            {
                sb.Append(" AND po.Status = @Status ");
                cmd.Parameters.AddWithValue("@Status", st);
            }
        }

        if (supFilter.Length > 0)
        {
            int s;
            if (int.TryParse(supFilter, out s))
            {
                sb.Append(" AND po.SupplierId = @SupplierId ");
                cmd.Parameters.AddWithValue("@SupplierId", s);
            }
        }

        if (search.Length > 0)
        {
            sb.Append(" AND (po.OrderNumber LIKE @Search OR s.SupplierName LIKE @Search " +
                      " OR po.Notes LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string supFilter)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, supFilter, cmd);
                cmd.CommandText =
                    "SELECT COUNT(*) FROM PurchaseOrders po " +
                    "INNER JOIN Suppliers s ON s.SupplierId = po.SupplierId " + filter;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string supFilter, int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string filter = BuildFilterSql(search, supFilter, cmd);

                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY po.PurchaseOrderId DESC) AS RowNum, " +
                    "    po.PurchaseOrderId, po.OrderNumber, po.SupplierId, " +
                    "    po.OrderDate, po.ExpectedDeliveryDate, po.Status, " +
                    "    po.Subtotal, po.Discount, po.Tax, po.Total, " +
                    "    po.CreatedAt, " +
                    "    s.SupplierName, s.SupplierCode, " +
                    "    ISNULL((SELECT COUNT(*) FROM PurchaseOrderItems poi " +
                    "            WHERE poi.PurchaseOrderId = po.PurchaseOrderId), 0) AS ItemCount " +
                    "  FROM PurchaseOrders po " +
                    "  INNER JOIN Suppliers s ON s.SupplierId = po.SupplierId " +
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

    protected void ddlFilterSupplier_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadOrders();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        ddlFilterSupplier.ClearSelection();
        StatusFilter = "";
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
    // Form: New / Edit
    // ─────────────────────────────────────────────────────
    private void PrepareNewOrder()
    {
        litFormMode.Text = "سفارش خرید جدید";
        litFormNumber.Text = "— (پیش‌نویس)";
        litFormStatusBadge.Text =
            "<span class=\"badge bg-brand-50 text-brand-700\"><i class=\"h-3 w-3\" data-lucide=\"edit\"></i> پیش‌نویس</span>";

        if (ddlSupplier.Items.Count > 0)
        {
            ddlSupplier.ClearSelection();
            ddlSupplier.SelectedIndex = 0;
        }

        txtExpectedDate.Text = "";
        txtPaymentTerms.Text = "";
        txtNotes.Text = "";

        pnlItemForm.Visible = false;
        LoadProductsDropdown();
        LoadItems();
        UpdateTotals();
    }

    private void LoadOrderForEdit(int poId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT po.PurchaseOrderId, po.OrderNumber, po.SupplierId, " +
                    "       po.OrderDate, po.ExpectedDeliveryDate, po.Status, " +
                    "       po.Subtotal, po.Discount, po.Tax, po.Total, " +
                    "       po.PaymentTerms, po.Notes " +
                    "FROM PurchaseOrders po " +
                    "WHERE po.PurchaseOrderId = @Id AND po.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            CurrentPOId = poId;
                            EditPOId = poId;

                            string orderNumber = Convert.ToString(dr["OrderNumber"]);
                            int status = dr["Status"] == DBNull.Value ? 1 : Convert.ToInt32(dr["Status"]);

                            litFormMode.Text = "ویرایش سفارش خرید";
                            litFormNumber.Text = HttpUtility.HtmlEncode(orderNumber);
                            litFormStatusBadge.Text =
                                "<span class=\"" + GetStatusBadgeCss(status) + "\">" +
                                HttpUtility.HtmlEncode(GetStatusLabel(status)) + "</span>";

                            // Supplier
                            if (dr["SupplierId"] != DBNull.Value)
                            {
                                string sid = Convert.ToInt32(dr["SupplierId"]).ToString(CultureInfo.InvariantCulture);
                                ddlSupplier.ClearSelection();
                                ListItem item = ddlSupplier.Items.FindByValue(sid);
                                if (item != null) item.Selected = true;
                            }

                            // Dates
                            if (dr["ExpectedDeliveryDate"] != DBNull.Value)
                            {
                                txtExpectedDate.Text = ToShamsiDateString(Convert.ToDateTime(dr["ExpectedDeliveryDate"]));
                            }
                            else
                            {
                                txtExpectedDate.Text = "";
                            }

                            txtPaymentTerms.Text = dr["PaymentTerms"] == DBNull.Value ? "" : Convert.ToString(dr["PaymentTerms"]);
                            txtNotes.Text = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);

                            // Buttons visibility بر اساس وضعیت
                            bool isDraft = (status == 1);
                            btnSaveDraft.Visible = isDraft;
                            btnSaveAndApprove.Visible = isDraft;
                            btnAddItem.Visible = isDraft;

                            pnlItemForm.Visible = false;

                            LoadProductsDropdown();
                            LoadItems();
                            UpdateTotals();
                        }
                        else
                        {
                            ShowMessage("سفارش مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری سفارش: " + ex.Message);
        }
    }

    private void LoadProductsDropdown()
    {
        ddlItemProduct.Items.Clear();
        ddlItemProduct.Items.Add(new ListItem("— انتخاب محصول —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // محصولات قابل خرید: RM و Packaging
                string sql =
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, p.ProductType, u.UnitName " +
                    "FROM Products p " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 " +
                    "  AND p.ProductType IN (1, 3) " +
                    "ORDER BY p.ProductType, p.ProductName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["ProductId"]).ToString(CultureInfo.InvariantCulture);
                        string code = Convert.ToString(dr["ProductCode"]);
                        string name = Convert.ToString(dr["ProductName"]);
                        string unit = dr["UnitName"] == DBNull.Value ? "" : Convert.ToString(dr["UnitName"]);
                        int type = Convert.ToInt32(dr["ProductType"]);

                        string typeLabel = type == 1 ? "RM" : "بسته";
                        string label = name + " (" + code + ") — " + typeLabel;
                        if (unit.Length > 0) label += " / " + unit;

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
    // Load: Items
    // ─────────────────────────────────────────────────────
    private void LoadItems()
    {
        try
        {
            DataTable dt = new DataTable();

            if (CurrentPOId > 0)
            {
                using (SqlConnection conn = new SqlConnection(ConnStr))
                {
                    conn.Open();

                    string sql =
                        "SELECT poi.PurchaseOrderItemId AS ItemId, poi.ProductId, " +
                        "       poi.Quantity, poi.ReceivedQuantity, poi.UnitPrice, " +
                        "       poi.Discount, poi.Tax, poi.Total, " +
                        "       p.ProductCode, p.ProductName, p.ProductType, " +
                        "       u.UnitName " +
                        "FROM PurchaseOrderItems poi " +
                        "INNER JOIN Products p ON p.ProductId = poi.ProductId " +
                        "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                        "WHERE poi.PurchaseOrderId = @Id " +
                        "ORDER BY poi.PurchaseOrderItemId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentPOId);
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
            decimal subtotal = 0m;
            decimal discountTotal = 0m;
            decimal taxTotal = 0m;
            decimal grand = 0m;

            if (CurrentPOId > 0)
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
                        "FROM PurchaseOrderItems WHERE PurchaseOrderId = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentPOId);
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
    // Form: Add Item
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
        // اعتبارسنجی محصول
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

        // محاسبه Total این قلم
        decimal lineTotal = (qty.Value * unitPrice.Value) - discount + tax;
        if (lineTotal < 0) lineTotal = 0;

        try
        {
            // اگر سفارش جدید است (CurrentPOId=0)، ابتدا یک سفارش پیش‌نویس ایجاد می‌کنیم تا Item اضافه شود
            if (CurrentPOId <= 0)
            {
                int newPOId = CreateDraftOrder();
                if (newPOId <= 0)
                {
                    ShowMessage("خطا در ایجاد سفارش پیش‌نویس.");
                    return;
                }
                CurrentPOId = newPOId;
                EditPOId = newPOId;

                // بعد از ایجاد، فرم را در حالت ویرایش بارگذاری می‌کنیم
                // تا وضعیت دکمه‌ها و شماره سفارش درست شود
                LoadOrderForEdit(newPOId);

                // و دوباره پنل قلم را باز می‌کنیم
                pnlItemForm.Visible = true;
                EditItemId = 0;
                litItemFormTitle.Text = "افزودن قلم جدید";

                // مقادیر را در فیلدها نگه داریم
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
                    // UPDATE
                    string updateSql =
                        "UPDATE PurchaseOrderItems SET " +
                        "  ProductId = @ProductId, Quantity = @Qty, UnitPrice = @UnitPrice, " +
                        "  Discount = @Discount, Tax = @Tax, Total = @Total " +
                        "WHERE PurchaseOrderItemId = @Id";

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
                    // INSERT
                    string insertSql =
                        "INSERT INTO PurchaseOrderItems " +
                        "  (PurchaseOrderId, ProductId, Quantity, ReceivedQuantity, " +
                        "   UnitPrice, Discount, Tax, Total) " +
                        "VALUES (@POId, @ProductId, @Qty, 0, @UnitPrice, @Discount, @Tax, @Total)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@POId", CurrentPOId);
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

                // Recalculate order totals
                RecalculateOrderTotals(conn, CurrentPOId);
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
                int newId = InsertNewOrderHeader(conn, 1);
                return newId;
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
        // شماره یکتا: PO-YYYY-####
        string orderNumber = GenerateOrderNumber(conn);

        // خواندن مقادیر فرم
        int supplierId = 0;
        if (!string.IsNullOrEmpty(ddlSupplier.SelectedValue))
        {
            int.TryParse(ddlSupplier.SelectedValue, out supplierId);
        }

        DateTime? expectedDate = null;
        if (txtExpectedDate.Text.Trim().Length > 0)
        {
            DateTime? d = ParsePersianDate(txtExpectedDate.Text.Trim());
            if (d.HasValue) expectedDate = d.Value;
        }

        string paymentTerms = txtPaymentTerms.Text.Trim();
        string notes = txtNotes.Text.Trim();

        string insertSql =
            "INSERT INTO PurchaseOrders " +
            "  (OrderNumber, SupplierId, OrderDate, ExpectedDeliveryDate, Status, " +
            "   Subtotal, Discount, Tax, Total, PaymentTerms, Notes, " +
            "   CreatedAt, CreatedBy, IsDeleted) " +
            "VALUES (@Number, @SupplierId, GETDATE(), @Expected, @Status, " +
            "   0, 0, 0, 0, @Terms, @Notes, GETDATE(), @CreatedBy, 0); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);";

        int newId = 0;
        using (SqlCommand cmd = new SqlCommand(insertSql, conn))
        {
            cmd.Parameters.AddWithValue("@Number", orderNumber);
            cmd.Parameters.AddWithValue("@SupplierId", supplierId > 0 ? (object)supplierId : DBNull.Value);
            cmd.Parameters.AddWithValue("@Expected",
                expectedDate.HasValue ? (object)expectedDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Terms",
                paymentTerms.Length > 0 ? (object)paymentTerms : DBNull.Value);
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

        string prefix = "PO-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        string sql =
            "SELECT ISNULL(MAX(CAST(SUBSTRING(OrderNumber, LEN(@Prefix) + 1, 20) AS INT)), 0) + 1 " +
            "FROM PurchaseOrders WHERE OrderNumber LIKE @Prefix + '%'";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + next.ToString("D4", CultureInfo.InvariantCulture);
        }
    }

    private void RecalculateOrderTotals(SqlConnection conn, int poId)
    {
        string sql =
            "UPDATE PurchaseOrders SET " +
            "  Subtotal = ISNULL((SELECT SUM(Quantity * UnitPrice) FROM PurchaseOrderItems " +
            "                     WHERE PurchaseOrderId = @Id), 0), " +
            "  Discount = ISNULL((SELECT SUM(Discount) FROM PurchaseOrderItems " +
            "                     WHERE PurchaseOrderId = @Id), 0), " +
            "  Tax = ISNULL((SELECT SUM(Tax) FROM PurchaseOrderItems " +
            "                WHERE PurchaseOrderId = @Id), 0), " +
            "  Total = ISNULL((SELECT SUM(Total) FROM PurchaseOrderItems " +
            "                  WHERE PurchaseOrderId = @Id), 0) " +
            "WHERE PurchaseOrderId = @Id";

        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
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
                    "SELECT PurchaseOrderItemId, ProductId, Quantity, UnitPrice, Discount, Tax " +
                    "FROM PurchaseOrderItems WHERE PurchaseOrderItemId = @Id";

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
                                ListItem item = ddlItemProduct.Items.FindByValue(pid);
                                if (item != null) item.Selected = true;
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
                    "DELETE FROM PurchaseOrderItems WHERE PurchaseOrderItemId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    cmd.ExecuteNonQuery();
                }

                RecalculateOrderTotals(conn, CurrentPOId);
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
        int supplierId = 0;
        if (string.IsNullOrEmpty(ddlSupplier.SelectedValue)
            || !int.TryParse(ddlSupplier.SelectedValue, out supplierId)
            || supplierId <= 0)
        {
            ShowMessage("تأمین‌کننده را انتخاب کنید.");
            return;
        }

        // حداقل یک قلم لازم است
        if (CurrentPOId <= 0)
        {
            ShowMessage("ابتدا حداقل یک قلم به سفارش اضافه کنید.");
            return;
        }

        int itemCount = 0;
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM PurchaseOrderItems WHERE PurchaseOrderId = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", CurrentPOId);
                object o = cmd.ExecuteScalar();
                itemCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }

        if (itemCount == 0)
        {
            ShowMessage("ابتدا حداقل یک قلم به سفارش اضافه کنید.");
            return;
        }

        // ── اعتبارسنجی تاریخ ──
        DateTime? expectedDate = null;
        if (txtExpectedDate.Text.Trim().Length > 0)
        {
            DateTime? d = ParsePersianDate(txtExpectedDate.Text.Trim());
            if (!d.HasValue)
            {
                ShowMessage("تاریخ تحویل معتبر نیست. فرمت صحیح: 1403/05/15");
                return;
            }
            expectedDate = d.Value;
        }

        string paymentTerms = txtPaymentTerms.Text.Trim();
        string notes = txtNotes.Text.Trim();

        if (paymentTerms.Length > 200)
        {
            ShowMessage("شرایط پرداخت بیش از حد طولانی است.");
            return;
        }
        if (notes.Length > 500)
        {
            ShowMessage("یادداشت بیش از حد طولانی است.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (CurrentPOId > 0)
                {
                    // UPDATE
                    string updateSql =
                        "UPDATE PurchaseOrders SET " +
                        "  SupplierId = @SupplierId, ExpectedDeliveryDate = @Expected, " +
                        "  PaymentTerms = @Terms, Notes = @Notes, " +
                        "  Status = @Status, UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                        "WHERE PurchaseOrderId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@SupplierId", supplierId);
                        cmd.Parameters.AddWithValue("@Expected",
                            expectedDate.HasValue ? (object)expectedDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Terms",
                            paymentTerms.Length > 0 ? (object)paymentTerms : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes",
                            notes.Length > 0 ? (object)notes : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", targetStatus);
                        cmd.Parameters.AddWithValue("@UpdatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", CurrentPOId);
                        cmd.ExecuteNonQuery();
                    }

                    RecalculateOrderTotals(conn, CurrentPOId);
                }
                else
                {
                    // INSERT — نباید به اینجا برسیم چون CreateDraftOrder در زمان افزودن قلم ID ساخته
                    int newId = InsertNewOrderHeader(conn, targetStatus);
                    if (newId <= 0)
                    {
                        ShowMessage("خطا در ایجاد سفارش.");
                        return;
                    }
                    CurrentPOId = newId;
                    EditPOId = newId;
                }

                // Audit
                string orderNumber = "";
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT OrderNumber FROM PurchaseOrders WHERE PurchaseOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentPOId);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) orderNumber = Convert.ToString(o);
                }

                WriteAudit(conn, "Update", "PurchaseOrders", CurrentPOId, orderNumber,
                    targetStatus == 2 ? "تأیید سفارش خرید" : "ذخیره پیش‌نویس");
            }

            if (targetStatus == 2)
            {
                ShowMessage("سفارش تأیید شد. حالا می‌توانید کالا را دریافت کنید.");
            }
            else
            {
                ShowMessage("پیش‌نویس سفارش ذخیره شد.");
            }

            ShowListView();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره سفارش: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Order Actions
    // ─────────────────────────────────────────────────────
    protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int poId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out poId) || poId <= 0)
            return;

        if (e.CommandName == "Edit") ShowFormView(poId);
        else if (e.CommandName == "Approve") ApproveOrder(poId);
        else if (e.CommandName == "Receive") ReceiveOrder(poId);
        else if (e.CommandName == "Cancel") CancelOrder(poId);
    }

    private void ApproveOrder(int poId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // چک وضعیت
                int status = GetOrderStatus(conn, poId);
                if (status != 1)
                {
                    ShowMessage("فقط سفارش‌های پیش‌نویس قابل تأیید هستند.");
                    return;
                }

                // چک وجود اقلام
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM PurchaseOrderItems WHERE PurchaseOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    object o = cmd.ExecuteScalar();
                    if ((o == null || o == DBNull.Value) || Convert.ToInt32(o) == 0)
                    {
                        ShowMessage("این سفارش هیچ قلمی ندارد. ابتدا اقلام را اضافه کنید.");
                        return;
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PurchaseOrders SET Status = 2, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE PurchaseOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, poId);
                WriteAudit(conn, "Approve", "PurchaseOrders", poId, num, "تأیید سفارش خرید");
            }

            ShowMessage("سفارش تأیید شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تأیید سفارش: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Receive Order — قلب سیستم FIFO
    // ─────────────────────────────────────────────────────
    private void ReceiveOrder(int poId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, poId);
                if (status != 2)
                {
                    ShowMessage("فقط سفارش‌های تأییدشده قابل دریافت هستند.");
                    return;
                }

                // انتخاب انبار مقصد: اولین انبار فعال با نوع RM
                int warehouseId = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 WarehouseId FROM Warehouses " +
                    "WHERE IsActive = 1 AND WarehouseType = 1 ORDER BY WarehouseId", conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value)
                    {
                        ShowMessage("هیچ انبار فعالی از نوع «مواد اولیه» وجود ندارد. ابتدا یک انبار RM بسازید.");
                        return;
                    }
                    warehouseId = Convert.ToInt32(o);
                }

                // شروع تراکنش
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Load all items
                        DataTable items = new DataTable();
                        string itemSql =
                            "SELECT PurchaseOrderItemId, ProductId, Quantity, UnitPrice " +
                            "FROM PurchaseOrderItems WHERE PurchaseOrderId = @POId";

                        using (SqlCommand cmd = new SqlCommand(itemSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@POId", poId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(items);
                            }
                        }

                        if (items.Rows.Count == 0)
                        {
                            tx.Rollback();
                            ShowMessage("سفارش هیچ قلمی ندارد.");
                            return;
                        }

                        string orderNumber = GetOrderNumberTx(conn, tx, poId);

                        for (int i = 0; i < items.Rows.Count; i++)
                        {
                            DataRow r = items.Rows[i];
                            int itemId = Convert.ToInt32(r["PurchaseOrderItemId"]);
                            int productId = Convert.ToInt32(r["ProductId"]);
                            decimal qty = Convert.ToDecimal(r["Quantity"]);
                            decimal unitPrice = Convert.ToDecimal(r["UnitPrice"]);
                            decimal totalCost = qty * unitPrice;

                            if (qty <= 0) continue;

                            // 1) StockMovement با MovementType = 1 (ورود خرید)
                            int movementId;
                            string mvSql =
                                "INSERT INTO StockMovements " +
                                "  (WarehouseId, ProductId, MovementType, Quantity, UnitCost, TotalCost, " +
                                "   MovementDate, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedBy) " +
                                "VALUES " +
                                "  (@WarehouseId, @ProductId, 1, @Qty, @UnitCost, @TotalCost, " +
                                "   GETDATE(), 'PurchaseOrder', @POId, @Notes, GETDATE(), @CreatedBy); " +
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            using (SqlCommand cmd = new SqlCommand(mvSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@WarehouseId", warehouseId);
                                cmd.Parameters.AddWithValue("@ProductId", productId);
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@UnitCost", unitPrice);
                                cmd.Parameters.AddWithValue("@TotalCost", totalCost);
                                cmd.Parameters.AddWithValue("@POId", poId);
                                cmd.Parameters.AddWithValue("@Notes",
                                    "دریافت از سفارش " + orderNumber);
                                cmd.Parameters.AddWithValue("@CreatedBy",
                                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                                object res = cmd.ExecuteScalar();
                                movementId = res == null || res == DBNull.Value ? 0 : Convert.ToInt32(res);
                            }

                            if (movementId <= 0)
                            {
                                tx.Rollback();
                                ShowMessage("خطا در ایجاد حرکت انبار برای قلم #" + itemId.ToString(CultureInfo.InvariantCulture));
                                return;
                            }

                            // 2) StockLayer جدید
                            string layerSql =
                                "INSERT INTO StockLayers " +
                                "  (WarehouseId, ProductId, StockMovementId, " +
                                "   QuantityReceived, QuantityRemaining, UnitCost, " +
                                "   ReceivedDate, BatchNumber, ExpiryDate, IsClosed) " +
                                "VALUES " +
                                "  (@WarehouseId, @ProductId, @MovementId, " +
                                "   @Qty, @Qty, @UnitCost, GETDATE(), NULL, NULL, 0)";

                            using (SqlCommand cmd = new SqlCommand(layerSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@WarehouseId", warehouseId);
                                cmd.Parameters.AddWithValue("@ProductId", productId);
                                cmd.Parameters.AddWithValue("@MovementId", movementId);
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@UnitCost", unitPrice);
                                cmd.ExecuteNonQuery();
                            }

                            // 3) Update ReceivedQuantity در قلم
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE PurchaseOrderItems SET ReceivedQuantity = @Qty " +
                                "WHERE PurchaseOrderItemId = @Id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@Id", itemId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 4) تغییر وضعیت سفارش به دریافت‌شده (3)
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE PurchaseOrders SET Status = 3, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE PurchaseOrderId = @Id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", poId);
                            cmd.Parameters.AddWithValue("@By",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // 5) Audit
                        WriteAuditTx(conn, tx, "Receive", "PurchaseOrders", poId, orderNumber,
                            "دریافت کالا و ورود به انبار");

                        tx.Commit();
                    }
                    catch (Exception exInner)
                    {
                        try { tx.Rollback(); }
                        catch { }
                        ShowMessage("خطا در ثبت دریافت: " + exInner.Message);
                        return;
                    }
                }
            }

            ShowMessage("کالا با موفقیت دریافت و به موجودی انبار اضافه شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت دریافت: " + ex.Message);
        }
    }

    private int GetOrderStatus(SqlConnection conn, int poId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT Status FROM PurchaseOrders WHERE PurchaseOrderId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
            object o = cmd.ExecuteScalar();
            if (o == null || o == DBNull.Value) return 0;
            return Convert.ToInt32(o);
        }
    }

    private string GetOrderNumber(SqlConnection conn, int poId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT OrderNumber FROM PurchaseOrders WHERE PurchaseOrderId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
            object o = cmd.ExecuteScalar();
            return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o);
        }
    }

    private string GetOrderNumberTx(SqlConnection conn, SqlTransaction tx, int poId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT OrderNumber FROM PurchaseOrders WHERE PurchaseOrderId = @Id", conn, tx))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
            object o = cmd.ExecuteScalar();
            return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o);
        }
    }

    private void CancelOrder(int poId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                int status = GetOrderStatus(conn, poId);
                if (status == 3)
                {
                    ShowMessage("سفارش دریافت‌شده قابل لغو نیست.");
                    return;
                }
                if (status == 4)
                {
                    ShowMessage("این سفارش قبلاً لغو شده است.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PurchaseOrders SET Status = 4, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                    "WHERE PurchaseOrderId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    cmd.Parameters.AddWithValue("@By",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                string num = GetOrderNumber(conn, poId);
                WriteAudit(conn, "Cancel", "PurchaseOrders", poId, num, "لغو سفارش خرید");
            }

            ShowMessage("سفارش لغو شد.");
            LoadStats();
            LoadOrders();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در لغو سفارش: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Navigation Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        ShowFormView(0);
    }

    protected void btnCancelForm_Click(object sender, EventArgs e)
    {
        // اگر پیش‌نویس ناتمام است، به کاربر هشدار می‌دهیم
        // اما اجازه بازگشت می‌دهیم — سفارش پیش‌نویس باقی می‌ماند
        ShowListView();
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
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

    private void WriteAuditTx(SqlConnection conn, SqlTransaction tx, string action, string entityName,
        int entityId, string targetName, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, @EntityName, @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
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

    protected bool CanCancel(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        return s == 1 || s == 2;
    }

    protected bool IsEditableItem()
    {
        // فقط در حالت ویرایش (پیش‌نویس) قابل ویرایش اقلام
        return btnSaveDraft.Visible;
    }

    protected string GetStatusLabel(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "پیش‌نویس";
        if (s == 2) return "تأییدشده";
        if (s == 3) return "دریافت‌شده";
        if (s == 4) return "لغوشده";
        return "—";
    }

    protected string GetStatusIcon(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "edit";
        if (s == 2) return "check-circle-2";
        if (s == 3) return "package-check";
        if (s == 4) return "x-circle";
        return "file";
    }

    protected string GetStatusBadgeCss(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "badge bg-brand-50 text-brand-700";
        if (s == 2) return "badge bg-amber-50 text-amber-700";
        if (s == 3) return "badge bg-emerald-50 text-emerald-700";
        if (s == 4) return "badge bg-rose-50 text-rose-700";
        return "badge bg-surface-muted text-ink-500";
    }

    protected string GetStatusAvatarCss(object statusObj)
    {
        int s = statusObj == null || statusObj == DBNull.Value ? 0 : Convert.ToInt32(statusObj);
        string baseC = "grid h-9 w-9 shrink-0 place-items-center rounded-lg";
        if (s == 1) return baseC + " bg-brand-100 text-brand-700";
        if (s == 2) return baseC + " bg-amber-100 text-amber-700";
        if (s == 3) return baseC + " bg-emerald-100 text-emerald-700";
        if (s == 4) return baseC + " bg-rose-100 text-rose-700";
        return baseC + " bg-surface-muted text-ink-500";
    }

    protected string GetProductTypeIcon(object typeObj)
    {
        int t = typeObj == null || typeObj == DBNull.Value ? 1 : Convert.ToInt32(typeObj);
        if (t == 1) return "pickaxe";
        if (t == 2) return "package-check";
        return "box";
    }

    protected string GetProductTypeAvatarCss(object typeObj)
    {
        int t = typeObj == null || typeObj == DBNull.Value ? 1 : Convert.ToInt32(typeObj);
        string baseC = "grid h-7 w-7 shrink-0 place-items-center rounded-lg";
        if (t == 1) return baseC + " bg-amber-100 text-amber-700";
        if (t == 2) return baseC + " bg-violet-100 text-violet-700";
        return baseC + " bg-emerald-100 text-emerald-700";
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

    protected string FormatShamsiShortOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        return FormatShamsiShort(value);
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

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
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