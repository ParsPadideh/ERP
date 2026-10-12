using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_stock_onhand : System.Web.UI.Page
{
    private const int PageSize = 20;

    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int CurrentPage
    {
        get { return ViewState["CurrentPage"] == null ? 1 : (int)ViewState["CurrentPage"]; }
        set { ViewState["CurrentPage"] = value; }
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
        // مدیریت درخواست تغییر نمای انبار
        HandlePostbackRequests();

        if (!IsPostBack)
        {
            CurrentPage = 1;

            // اگر از URL پارامتر آمده باشد
            string whId = Request.QueryString["warehouseId"];
            string prodId = Request.QueryString["productId"];
            string q = Request.QueryString["q"];

            LoadWarehouseFilter();

            if (!string.IsNullOrEmpty(whId))
            {
                ListItem item = ddlFilterWarehouse.Items.FindByValue(whId);
                if (item != null)
                {
                    ddlFilterWarehouse.ClearSelection();
                    item.Selected = true;
                }
            }

            if (!string.IsNullOrEmpty(q))
            {
                txtSearch.Text = q;
            }

            // اگر productId آمده، در txtSearch قرار نمی‌دهیم چون Id است نه کد
            // از طرف products.aspx فقط productId می‌آید
            if (!string.IsNullOrEmpty(prodId))
            {
                // می‌توان در فیلتر مخفی ذخیره کرد، اما اینجا از txtSearch کد محصول را می‌گیریم
                string code = GetProductCodeById(prodId);
                if (!string.IsNullOrEmpty(code))
                {
                    txtSearch.Text = code;
                }
            }

            LoadStats();
            LoadStock();
        }
    }

    private void HandlePostbackRequests()
    {
        string target = Request.Form["__EVENTTARGET"];
        string arg = Request.Form["__EVENTARGUMENT"];

        if (target == "__PAGE" && !string.IsNullOrEmpty(arg) && arg.StartsWith("setWhView:"))
        {
            string view = arg.Substring("setWhView:".Length);
            hfWarehouseView.Value = view;
            CurrentPage = 1;
            LoadWarehouseFilter();
            LoadStats();
            LoadStock();
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
    // Load: Warehouses Dropdown
    // ─────────────────────────────────────────────────────
    private void LoadWarehouseFilter()
    {
        ddlFilterWarehouse.Items.Clear();
        ddlFilterWarehouse.Items.Add(new ListItem("همه انبارها", ""));

        try
        {
            string whView = hfWarehouseView.Value;
            if (whView == null) whView = "0";

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT WarehouseId, WarehouseName, WarehouseType FROM Warehouses WHERE IsActive = 1 ");

            if (whView == "RM") sql.Append(" AND WarehouseType = 1 ");
            else if (whView == "FG") sql.Append(" AND WarehouseType = 2 ");

            sql.Append(" ORDER BY WarehouseType, WarehouseId");

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql.ToString(), conn))
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
            string whView = hfWarehouseView.Value;
            if (whView == null) whView = "0";

            string whViewFilter = "";
            if (whView == "RM") whViewFilter = " AND w.WarehouseType = 1 ";
            else if (whView == "FG") whViewFilter = " AND w.WarehouseType = 2 ";

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── کل ارزش موجودی + تعداد اقلام ──
                string sql1 =
                    "SELECT " +
                    "  ISNULL(SUM(sl.QuantityRemaining * sl.UnitCost), 0) AS TotalValue, " +
                    "  COUNT(DISTINCT sl.WarehouseId * 1000000 + sl.ProductId) AS ItemCount " +
                    "FROM StockLayers sl " +
                    "INNER JOIN Warehouses w ON w.WarehouseId = sl.WarehouseId " +
                    "WHERE sl.IsClosed = 0 AND sl.QuantityRemaining > 0 " + whViewFilter;

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        decimal totalValue = dr["TotalValue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TotalValue"]);
                        litStatTotalValue.Text = HttpUtility.HtmlEncode(FormatMoney(totalValue));
                        litStatItems.Text = HttpUtility.HtmlEncode(
                            Convert.ToInt32(dr["ItemCount"]).ToString("N0", CultureInfo.InvariantCulture));
                    }
                }

                // ── تعداد رزروهای فعال ──
                string sql2 =
                    "SELECT COUNT(DISTINCT sr.WarehouseId * 1000000 + sr.ProductId) " +
                    "FROM StockReservations sr " +
                    "INNER JOIN Warehouses w ON w.WarehouseId = sr.WarehouseId " +
                    "WHERE sr.Status = 1 " + whViewFilter;

                using (SqlCommand cmd = new SqlCommand(sql2, conn))
                {
                    object o = cmd.ExecuteScalar();
                    int reserved = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatReserved.Text = HttpUtility.HtmlEncode(
                        reserved.ToString("N0", CultureInfo.InvariantCulture));
                }

                // ── کالاهای کمبود ──
                // نکته: MinStock در سطح محصول است، نه انبار — پس Join با Warehouses اشتباه است
                string sql3;

                if (whView == "RM")
                {
                    sql3 =
                        "SELECT COUNT(*) FROM Products p " +
                        "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                        "  AND p.ProductType = 1 " +
                        "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                        "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock";
                }
                else if (whView == "FG")
                {
                    sql3 =
                        "SELECT COUNT(*) FROM Products p " +
                        "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                        "  AND p.ProductType = 2 " +
                        "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                        "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock";
                }
                else
                {
                    sql3 =
                        "SELECT COUNT(*) FROM Products p " +
                        "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                        "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                        "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock";
                }

                using (SqlCommand cmd = new SqlCommand(sql3, conn))
                {
                    object o = cmd.ExecuteScalar();
                    int lowStock = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                    litStatLowStock.Text = HttpUtility.HtmlEncode(
                        lowStock.ToString("N0", CultureInfo.InvariantCulture));
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری آمار: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load: Stock List
    // ─────────────────────────────────────────────────────
    private void LoadStock()
    {
        try
        {
            string search = txtSearch.Text.Trim();
            string whId = ddlFilterWarehouse.SelectedValue;
            string typeFilter = ddlFilterProductType.SelectedValue;
            string stateFilter = ddlFilterStockState.SelectedValue;
            string whView = hfWarehouseView.Value;

            if (whId == null) whId = "";
            if (typeFilter == null) typeFilter = "";
            if (stateFilter == null) stateFilter = "";
            if (whView == null) whView = "0";

            int totalCount = GetTotalCount(search, whId, typeFilter, stateFilter, whView);
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (CurrentPage > totalPages) CurrentPage = totalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DataTable dt = GetPaged(search, whId, typeFilter, stateFilter, whView, CurrentPage, PageSize);

            if (dt.Rows.Count == 0)
            {
                pnlEmpty.Visible = true;
                rptStock.Visible = false;
                rptStock.DataSource = null;
                rptStock.DataBind();
            }
            else
            {
                pnlEmpty.Visible = false;
                rptStock.Visible = true;
                rptStock.DataSource = dt;
                rptStock.DataBind();
            }

            int startIdx = 0;
            int endIdx = 0;
            if (dt.Rows.Count > 0)
            {
                startIdx = (CurrentPage - 1) * PageSize + 1;
                endIdx = startIdx + dt.Rows.Count - 1;
            }

            litInfo.Text = string.Format("نمایش {0} تا {1} از {2} مورد",
                startIdx.ToString(CultureInfo.InvariantCulture),
                endIdx.ToString(CultureInfo.InvariantCulture),
                totalCount.ToString(CultureInfo.InvariantCulture));

            litPageInfo.Text = string.Format("صفحه {0} از {1}",
                CurrentPage.ToString(CultureInfo.InvariantCulture),
                totalPages.ToString(CultureInfo.InvariantCulture));

            litCurrentPage.Text = HttpUtility.HtmlEncode(ToPersianNumber(CurrentPage.ToString(CultureInfo.InvariantCulture)));

            SetPagerEnabled(btnPrev, CurrentPage > 1);
            SetPagerEnabled(btnNext, CurrentPage < totalPages);

            // ── فیلتر Info ──
            litFilterInfo.Text = BuildFilterInfoText(whView, whId, typeFilter, stateFilter, search);

            // ── CSV ──
            BuildCsvData(dt);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری موجودی: " + ex.Message);
        }
    }

    private string BuildFilterInfoText(string whView, string whId, string typeFilter, string stateFilter, string search)
    {
        StringBuilder sb = new StringBuilder();

        if (whView == "RM") sb.Append("📦 انبارهای مواد اولیه");
        else if (whView == "FG") sb.Append("📦 انبارهای محصول نهایی");
        else sb.Append("📦 همه انبارها");

        if (whId.Length > 0) sb.Append(" — انبار انتخابی");
        if (typeFilter.Length > 0) sb.Append(" — نوع محصول فیلتر شده");
        if (stateFilter.Length > 0) sb.Append(" — وضعیت فیلتر شده");
        if (search.Length > 0) sb.Append(" — جستجو: «" + search + "»");

        return HttpUtility.HtmlEncode(sb.ToString());
    }

    private string BuildFilterSql(string search, string whId, string typeFilter, string stateFilter, string whView,
        SqlCommand cmd)
    {
        StringBuilder sb = new StringBuilder();

        // ── نمای انبار ──
        if (whView == "RM")
        {
            sb.Append(" AND w.WarehouseType = 1 ");
        }
        else if (whView == "FG")
        {
            sb.Append(" AND w.WarehouseType = 2 ");
        }

        // ── انبار خاص ──
        if (whId.Length > 0)
        {
            int wid;
            if (int.TryParse(whId, out wid))
            {
                sb.Append(" AND sl.WarehouseId = @WarehouseId ");
                cmd.Parameters.AddWithValue("@WarehouseId", wid);
            }
        }

        // ── نوع محصول ──
        if (typeFilter.Length > 0)
        {
            int t;
            if (int.TryParse(typeFilter, out t))
            {
                sb.Append(" AND p.ProductType = @ProductType ");
                cmd.Parameters.AddWithValue("@ProductType", t);
            }
        }

        // ── جستجو ──
        if (search.Length > 0)
        {
            sb.Append(" AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search) ");
            cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
        }

        // ── فیلتر وضعیت ──
        if (stateFilter == "low")
        {
            // موجودی کمتر از MinStock
            sb.Append(" AND p.MinStock > 0 ");
            sb.Append(" AND (SELECT ISNULL(SUM(sl2.QuantityRemaining), 0) FROM StockLayers sl2 " +
                      "     WHERE sl2.ProductId = sl.ProductId AND sl2.IsClosed = 0) < p.MinStock ");
        }
        else if (stateFilter == "available")
        {
            // موجودی بیشتر از صفر
            sb.Append(" AND sl.QuantityRemaining > 0 ");
        }
        else if (stateFilter == "reserved")
        {
            // دارای رزرو فعال
            sb.Append(" AND EXISTS (SELECT 1 FROM StockReservations sr " +
                      "            WHERE sr.WarehouseId = sl.WarehouseId " +
                      "              AND sr.ProductId = sl.ProductId AND sr.Status = 1) ");
        }

        return sb.ToString();
    }

    private int GetTotalCount(string search, string whId, string typeFilter, string stateFilter, string whView)
    {
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, whId, typeFilter, stateFilter, whView, cmd);

                // شمارش گروه‌های (Warehouse, Product) یکتا
                cmd.CommandText =
                    "SELECT COUNT(*) FROM ( " +
                    "  SELECT sl.WarehouseId, sl.ProductId " +
                    "  FROM StockLayers sl " +
                    "  INNER JOIN Warehouses w ON w.WarehouseId = sl.WarehouseId " +
                    "  INNER JOIN Products p ON p.ProductId = sl.ProductId " +
                    "  WHERE sl.IsClosed = 0 " + filter +
                    "  GROUP BY sl.WarehouseId, sl.ProductId " +
                    ") x";

                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
    }

    private DataTable GetPaged(string search, string whId, string typeFilter, string stateFilter, string whView,
        int page, int pageSize)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string filter = BuildFilterSql(search, whId, typeFilter, stateFilter, whView, cmd);

                // GROUP BY روی (Warehouse, Product) — هر ترکیب یک ردیف
                // محاسبه:
                //   OnHand = جمع QuantityRemaining لایه‌های باز
                //   StockValue = جمع (QuantityRemaining * UnitCost)
                //   Reserved = جمع رزروهای فعال
                string sql =
                    "SELECT * FROM ( " +
                    "  SELECT ROW_NUMBER() OVER (ORDER BY w.WarehouseType, w.WarehouseName, p.ProductName) AS RowNum, " +
                    "    sl.WarehouseId, sl.ProductId, " +
                    "    w.WarehouseName, w.WarehouseType, " +
                    "    p.ProductCode, p.ProductName, p.ProductType, p.MinStock, " +
                    "    u.UnitName, u.Symbol, " +
                    "    SUM(sl.QuantityRemaining) AS OnHand, " +
                    "    SUM(sl.QuantityRemaining * sl.UnitCost) AS StockValue, " +
                    "    ISNULL((SELECT SUM(sr.Quantity) FROM StockReservations sr " +
                    "            WHERE sr.WarehouseId = sl.WarehouseId " +
                    "              AND sr.ProductId = sl.ProductId AND sr.Status = 1), 0) AS Reserved, " +
                    "    SUM(sl.QuantityRemaining) - " +
                    "    ISNULL((SELECT SUM(sr.Quantity) FROM StockReservations sr " +
                    "            WHERE sr.WarehouseId = sl.WarehouseId " +
                    "              AND sr.ProductId = sl.ProductId AND sr.Status = 1), 0) AS Available " +
                    "  FROM StockLayers sl " +
                    "  INNER JOIN Warehouses w ON w.WarehouseId = sl.WarehouseId " +
                    "  INNER JOIN Products p ON p.ProductId = sl.ProductId " +
                    "  INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "  WHERE sl.IsClosed = 0 AND sl.QuantityRemaining > 0 " + filter +
                    "  GROUP BY sl.WarehouseId, sl.ProductId, " +
                    "           w.WarehouseName, w.WarehouseType, " +
                    "           p.ProductCode, p.ProductName, p.ProductType, p.MinStock, " +
                    "           u.UnitName, u.Symbol " +
                    ") x WHERE x.RowNum BETWEEN @Start AND @End ORDER BY x.RowNum";

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
        LoadStock();
    }

    protected void ddlFilterWarehouse_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStock();
    }

    protected void ddlFilterProductType_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStock();
    }

    protected void ddlFilterStockState_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPage = 1;
        LoadStock();
    }

    protected void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Text = "";
        ddlFilterWarehouse.ClearSelection();
        ddlFilterProductType.ClearSelection();
        ddlFilterStockState.ClearSelection();
        hfWarehouseView.Value = "0";
        CurrentPage = 1;
        LoadWarehouseFilter();
        LoadStats();
        LoadStock();
    }

    protected void btnPrev_Click(object sender, EventArgs e)
    {
        if (CurrentPage > 1) { CurrentPage = CurrentPage - 1; LoadStock(); }
    }

    protected void btnNext_Click(object sender, EventArgs e)
    {
        CurrentPage = CurrentPage + 1;
        LoadStock();
    }

    protected void rptStock_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        // در حال حاضر اقدامی نداریم — لینک‌های مستقیم هستند
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
    // CSV Export
    // ─────────────────────────────────────────────────────
    private void BuildCsvData(DataTable dt)
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            litCsvData.Text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("محصول,کد,انبار,نوع انبار,نوع محصول,موجودی فیزیکی,رزروشده,قابل فروش,حداقل,ارزش,واحد");

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            DataRow r = dt.Rows[i];

            sb.Append(CsvEscape(Convert.ToString(r["ProductName"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["ProductCode"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["WarehouseName"]))).Append(",");
            sb.Append(CsvEscape(GetWarehouseTypeLabel(Convert.ToInt32(r["WarehouseType"])))).Append(",");
            sb.Append(CsvEscape(GetTypeLabel(Convert.ToInt32(r["ProductType"])))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["OnHand"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["Reserved"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["Available"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["MinStock"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["StockValue"]))).Append(",");
            sb.Append(CsvEscape(Convert.ToString(r["UnitName"])));
            sb.AppendLine();
        }

        litCsvData.Text = sb.ToString();
    }

    private string GetWarehouseTypeLabel(int type)
    {
        return type == 1 ? "مواد اولیه (RM)" : "محصول نهایی (FG)";
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

    protected string GetWarehouseIcon(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        return type == 1 ? "package" : "package-check";
    }

    protected string GetWarehouseBadgeCss(object typeObj)
    {
        int type = (typeObj == null || typeObj == DBNull.Value) ? 1 : Convert.ToInt32(typeObj);
        if (type == 1) return "badge bg-amber-50 text-amber-700";
        return "badge bg-violet-50 text-violet-700";
    }

    protected string GetRowCss(object onHandObj, object minStockObj, object availableObj)
    {
        decimal onHand = (onHandObj == null || onHandObj == DBNull.Value) ? 0m : Convert.ToDecimal(onHandObj);
        decimal minStock = (minStockObj == null || minStockObj == DBNull.Value) ? 0m : Convert.ToDecimal(minStockObj);

        string baseC = "border-b border-surface-line transition";

        if (minStock > 0 && onHand < minStock)
        {
            return baseC + " bg-rose-50/30 hover:bg-rose-50/60";
        }
        if (onHand == 0)
        {
            return baseC + " hover:bg-surface-body/70";
        }

        return baseC + " hover:bg-surface-body/70";
    }

    protected string RenderReserved(object reservedObj)
    {
        decimal reserved = (reservedObj == null || reservedObj == DBNull.Value) ? 0m : Convert.ToDecimal(reservedObj);

        if (reserved <= 0)
        {
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";
        }

        return "<span class=\"inline-flex items-center gap-1 badge bg-violet-50 text-violet-700 font-mono font-bold\">" +
            "<i class=\"h-3 w-3\" data-lucide=\"lock\"></i>" +
            HttpUtility.HtmlEncode(FormatQuantityValue(reserved)) +
            "</span>";
    }

    protected string RenderAvailable(object availableObj, object onHandObj)
    {
        decimal available = (availableObj == null || availableObj == DBNull.Value) ? 0m : Convert.ToDecimal(availableObj);
        decimal onHand = (onHandObj == null || onHandObj == DBNull.Value) ? 0m : Convert.ToDecimal(onHandObj);

        string label;

        if (available <= 0)
        {
            label = "<span class=\"badge bg-rose-50 text-rose-600 font-mono font-bold\">" +
                HttpUtility.HtmlEncode(FormatQuantityValue(available)) +
                "</span>";
        }
        else if (onHand > 0 && available < onHand)
        {
            // بخشی رزرو شده
            label = "<span class=\"badge bg-amber-50 text-amber-700 font-mono font-bold\">" +
                HttpUtility.HtmlEncode(FormatQuantityValue(available)) +
                "</span>";
        }
        else
        {
            label = "<span class=\"badge bg-emerald-50 text-emerald-700 font-mono font-bold\">" +
                HttpUtility.HtmlEncode(FormatQuantityValue(available)) +
                "</span>";
        }

        return label;
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
            if (d == 0m) return "۰";
            return HttpUtility.HtmlEncode(d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،"));
        }
        catch { return "۰"; }
    }

    private string ToPersianNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        char[] persianDigits = new char[]
        {
            '۰','۱','۲','۳','۴','۵','۶','۷','۸','۹'
        };

        StringBuilder sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c >= '0' && c <= '9')
                sb.Append(persianDigits[c - '0']);
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────
    // ShowMessage
    // ─────────────────────────────────────────────────────
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