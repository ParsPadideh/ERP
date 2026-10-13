using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_sales_presentation : System.Web.UI.Page
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

    private string CurrentUserName
    {
        get
        {
            if (Session["FullName"] != null)
            {
                string s = Convert.ToString(Session["FullName"]).Trim();
                if (s.Length > 0) return s;
            }
            if (Session["Username"] != null)
                return Convert.ToString(Session["Username"]).Trim();
            return "بازاریاب";
        }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        LoadCustomersJson();
        LoadProductImagesJson();
        LoadProducts();

        if (!IsPostBack)
        {
            litMarketerName.Text = HttpUtility.HtmlEncode(CurrentUserName);
            litTodayShamsi.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
        }
        else
        {
            string eventTarget = Request["__EVENTTARGET"];
            if (eventTarget == "submitCartAction")
            {
                ProcessCartSubmission();
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    // IMAGE HELPERS (for ASPX)
    // ═══════════════════════════════════════════════════════
    protected bool HasProductImage(object imageUrlObj)
    {
        if (imageUrlObj == null || imageUrlObj == DBNull.Value) return false;
        string url = Convert.ToString(imageUrlObj).Trim();
        return url.Length > 0;
    }

    protected string GetProductImageWrapperCss(object imageUrlObj, object productIdObj, object categoryIdObj)
    {
        if (HasProductImage(imageUrlObj))
            return "product-img";

        int pid = (productIdObj == null || productIdObj == DBNull.Value)
            ? 0 : Convert.ToInt32(productIdObj);
        int idx = (pid % 6) + 1;
        return "product-img product-img-gradient-" + idx.ToString(CultureInfo.InvariantCulture);
    }

    protected string GetProductEmoji(object categoryNameObj)
    {
        string cat = (categoryNameObj == null || categoryNameObj == DBNull.Value)
            ? "" : Convert.ToString(categoryNameObj);

        if (cat.Contains("خشکبار") || cat.Contains("پسته") || cat.Contains("بادام")) return "🥜";
        if (cat.Contains("ادویه") || cat.Contains("زعفران")) return "🌿";
        if (cat.Contains("حبوبات") || cat.Contains("لوبیا") || cat.Contains("نخود")) return "🫘";
        if (cat.Contains("دمنوش") || cat.Contains("چای")) return "🍵";
        if (cat.Contains("خرما") || cat.Contains("میوه")) return "🍇";
        if (cat.Contains("برنج") || cat.Contains("غلات")) return "🌾";
        if (cat.Contains("روغن") || cat.Contains("مایع")) return "🫗";
        return "🌰";
    }

    protected string GetStockBadgeCss(object stockObj)
    {
        decimal stock = (stockObj == null || stockObj == DBNull.Value)
            ? 0m : Convert.ToDecimal(stockObj);
        if (stock <= 0m) return "product-stock-badge out";
        if (stock < 20m) return "product-stock-badge low";
        return "product-stock-badge";
    }

    protected string GetStockBadgeText(object stockObj, object unitObj)
    {
        decimal stock = (stockObj == null || stockObj == DBNull.Value)
            ? 0m : Convert.ToDecimal(stockObj);
        if (stock <= 0m) return "ناموجود";
        if (stock < 20m) return "کم موجود";
        string unit = (unitObj == null || unitObj == DBNull.Value) ? "" : Convert.ToString(unitObj);
        string s = FormatQtyValue(stock);
        if (unit.Length > 0) s += " " + unit;
        return s;
    }

    // ═══════════════════════════════════════════════════════
    // Load Products
    // ═══════════════════════════════════════════════════════
    private void LoadProducts()
    {
        try
        {
            DataTable dt = new DataTable();

            string sql =
                "SELECT " +
                "    p.ProductId, " +
                "    p.ProductCode, " +
                "    p.ProductName, " +
                "    p.CategoryId, " +
                "    p.ImageUrl, " +
                "    ISNULL(pc.CategoryName, N'سایر') AS CategoryName, " +
                "    u.UnitName, " +
                "    ISNULL((" +
                "        SELECT SUM(sl.QuantityRemaining) " +
                "        FROM StockLayers sl " +
                "        WHERE sl.ProductId = p.ProductId " +
                "          AND sl.IsClosed = 0 " +
                "          AND sl.QuantityRemaining > 0" +
                "    ), 0) AS AvailableStock, " +
                "    ISNULL((" +
                "        SELECT TOP 1 pli.UnitPrice FROM PriceListItems pli " +
                "        INNER JOIN PriceLists pl ON pl.PriceListId = pli.PriceListId " +
                "        WHERE pli.ProductId = p.ProductId AND pl.PriceListType = 1 " +
                "          AND pli.IsActive = 1 AND pl.IsActive = 1 AND pl.IsDeleted = 0 " +
                "          AND pli.EffectiveFrom <= GETDATE() " +
                "          AND (pli.EffectiveTo IS NULL OR pli.EffectiveTo >= GETDATE()) " +
                "        ORDER BY pli.MinQuantity DESC, pli.PriceListItemId DESC" +
                "    ), 0) AS PriceRetail, " +
                "    ISNULL((" +
                "        SELECT TOP 1 pli.UnitPrice FROM PriceListItems pli " +
                "        INNER JOIN PriceLists pl ON pl.PriceListId = pli.PriceListId " +
                "        WHERE pli.ProductId = p.ProductId AND pl.PriceListType = 2 " +
                "          AND pli.IsActive = 1 AND pl.IsActive = 1 AND pl.IsDeleted = 0 " +
                "          AND pli.EffectiveFrom <= GETDATE() " +
                "          AND (pli.EffectiveTo IS NULL OR pli.EffectiveTo >= GETDATE()) " +
                "        ORDER BY pli.MinQuantity DESC, pli.PriceListItemId DESC" +
                "    ), 0) AS PriceWholesale, " +
                "    ISNULL((" +
                "        SELECT TOP 1 pli.UnitPrice FROM PriceListItems pli " +
                "        INNER JOIN PriceLists pl ON pl.PriceListId = pli.PriceListId " +
                "        WHERE pli.ProductId = p.ProductId AND pl.PriceListType = 3 " +
                "          AND pli.IsActive = 1 AND pl.IsActive = 1 AND pl.IsDeleted = 0 " +
                "          AND pli.EffectiveFrom <= GETDATE() " +
                "          AND (pli.EffectiveTo IS NULL OR pli.EffectiveTo >= GETDATE()) " +
                "        ORDER BY pli.MinQuantity DESC, pli.PriceListItemId DESC" +
                "    ), 0) AS PriceAgent, " +
                "    ISNULL((" +
                "        SELECT TOP 1 pli.UnitPrice FROM PriceListItems pli " +
                "        INNER JOIN PriceLists pl ON pl.PriceListId = pli.PriceListId " +
                "        WHERE pli.ProductId = p.ProductId AND pl.PriceListType = 1 " +
                "          AND pli.IsActive = 1 AND pl.IsActive = 1 AND pl.IsDeleted = 0 " +
                "          AND pli.EffectiveFrom <= GETDATE() " +
                "          AND (pli.EffectiveTo IS NULL OR pli.EffectiveTo >= GETDATE()) " +
                "        ORDER BY pli.MinQuantity DESC, pli.PriceListItemId DESC" +
                "    ), 0) AS PriceFarmer " +
                "FROM Products p " +
                "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                "LEFT JOIN ProductCategories pc ON pc.CategoryId = p.CategoryId " +
                "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND u.IsDeleted = 0 " +
                "  AND p.ProductType = 2 " +
                "ORDER BY pc.CategoryName, p.ProductName";

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            rptProducts.DataSource = dt;
            rptProducts.DataBind();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری محصولات: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Load Customers JSON
    // ═══════════════════════════════════════════════════════
    private void LoadCustomersJson()
    {
        try
        {
            List<Dictionary<string, object>> customers = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string role = Session["PrimaryRole"] == null ? "" : Convert.ToString(Session["PrimaryRole"]);
                bool isAdmin = (role == "مدیر سیستم" || role == "مدیر بهره‌برداری");

                string sql =
                    "SELECT CustomerId, CustomerCode, CustomerName, CustomerType, " +
                    "       ISNULL(City, '') AS City, ISNULL(Mobile, '') AS Mobile " +
                    "FROM Customers WHERE IsDeleted = 0 AND IsActive = 1 ";

                if (!isAdmin && CurrentUserId > 0)
                    sql += " AND MarketerId = @MarketerId ";

                sql += " ORDER BY CustomerName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (!isAdmin && CurrentUserId > 0)
                        cmd.Parameters.AddWithValue("@MarketerId", CurrentUserId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Dictionary<string, object> c = new Dictionary<string, object>();
                            c["id"] = Convert.ToInt32(dr["CustomerId"]);
                            c["code"] = Convert.ToString(dr["CustomerCode"]);
                            c["name"] = Convert.ToString(dr["CustomerName"]);
                            c["type"] = dr["CustomerType"] == DBNull.Value
                                ? 2 : Convert.ToInt32(dr["CustomerType"]);
                            c["city"] = Convert.ToString(dr["City"]);
                            c["mobile"] = Convert.ToString(dr["Mobile"]);
                            customers.Add(c);
                        }
                    }
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(customers);

            litCustomersJson.Text = "<script type=\"application/json\" id=\"" +
                litCustomersJson.ClientID + "\">" + json + "</script>";
        }
        catch
        {
            litCustomersJson.Text = "<script type=\"application/json\" id=\"" +
                litCustomersJson.ClientID + "\">[]</script>";
        }
    }

    // ═══════════════════════════════════════════════════════
    // Load Product Images JSON (for gallery)
    // ═══════════════════════════════════════════════════════
    private void LoadProductImagesJson()
    {
        try
        {
            Dictionary<string, List<Dictionary<string, object>>> result =
                new Dictionary<string, List<Dictionary<string, object>>>();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ۱) خواندن عکس‌های جدول ProductImages
                string sqlImages =
                    "SELECT pi.ProductId, pi.ImageId, pi.ImageUrl, pi.IsPrimary, pi.SortOrder " +
                    "FROM ProductImages pi " +
                    "INNER JOIN Products p ON p.ProductId = pi.ProductId " +
                    "WHERE pi.IsDeleted = 0 " +
                    "  AND p.IsActive = 1 AND p.IsDeleted = 0 AND p.ProductType = 2 " +
                    "ORDER BY pi.ProductId, pi.IsPrimary DESC, pi.SortOrder ASC, pi.ImageId ASC";

                using (SqlCommand cmd = new SqlCommand(sqlImages, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int productId = Convert.ToInt32(dr["ProductId"]);
                        string key = productId.ToString(CultureInfo.InvariantCulture);

                        if (!result.ContainsKey(key))
                            result[key] = new List<Dictionary<string, object>>();

                        // ✅ اینجا ResolveUrl صدا زده می‌شود تا ~ به مسیر واقعی تبدیل شود
                        string rawUrl = Convert.ToString(dr["ImageUrl"]);
                        string resolvedUrl = ResolveUrl(rawUrl);

                        Dictionary<string, object> item = new Dictionary<string, object>();
                        item["imageId"] = Convert.ToInt32(dr["ImageId"]);
                        item["url"] = resolvedUrl;
                        item["isPrimary"] = dr["IsPrimary"] != DBNull.Value
                            && Convert.ToBoolean(dr["IsPrimary"]);

                        result[key].Add(item);
                    }
                }

                // ۲) برای محصولاتی که در جدول ProductImages عکس ندارند ولی ImageUrl قدیمی دارند
                string sqlFallback =
                    "SELECT p.ProductId, p.ImageUrl " +
                    "FROM Products p " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.ProductType = 2 " +
                    "  AND p.ImageUrl IS NOT NULL AND LEN(LTRIM(RTRIM(p.ImageUrl))) > 0 " +
                    "  AND NOT EXISTS (" +
                    "      SELECT 1 FROM ProductImages pi " +
                    "      WHERE pi.ProductId = p.ProductId AND pi.IsDeleted = 0" +
                    "  )";

                using (SqlCommand cmd = new SqlCommand(sqlFallback, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int productId = Convert.ToInt32(dr["ProductId"]);
                        string key = productId.ToString(CultureInfo.InvariantCulture);
                        string rawUrl = Convert.ToString(dr["ImageUrl"]).Trim();

                        if (!result.ContainsKey(key))
                        {
                            List<Dictionary<string, object>> list =
                                new List<Dictionary<string, object>>();

                            // ✅ اینجا هم ResolveUrl صدا زده می‌شود
                            Dictionary<string, object> item = new Dictionary<string, object>();
                            item["imageId"] = 0;
                            item["url"] = ResolveUrl(rawUrl);
                            item["isPrimary"] = true;

                            list.Add(item);
                            result[key] = list;
                        }
                    }
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(result);

            litProductImagesJson.Text = "<script type=\"application/json\" id=\"" +
                litProductImagesJson.ClientID + "\">" + json + "</script>";
        }
        catch
        {
            litProductImagesJson.Text = "<script type=\"application/json\" id=\"" +
                litProductImagesJson.ClientID + "\">{}</script>";
        }
    }

    // ═══════════════════════════════════════════════════════
    // SAVE NEW CUSTOMER
    // ═══════════════════════════════════════════════════════
    protected void btnSaveNewCustomer_Click(object sender, EventArgs e)
    {
        try
        {
            string name = txtNewCustomerName.Text.Trim();
            string mobile = txtNewCustomerMobile.Text.Trim();
            string city = txtNewCustomerCity.Text.Trim();
            string code = txtNewCustomerCode.Text.Trim();
            string econCode = txtNewCustomerEconomicCode.Text.Trim();
            string address = txtNewCustomerAddress.Text.Trim();

            int customerType = 2;
            int.TryParse(hfNewCustomerType.Value, out customerType);
            if (customerType < 1 || customerType > 4) customerType = 2;

            if (name.Length == 0)
            {
                ShowMessage("نام مشتری را وارد کنید.");
                return;
            }
            if (mobile.Length < 10)
            {
                ShowMessage("شماره موبایل معتبر وارد کنید (حداقل ۱۰ رقم).");
                return;
            }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (code.Length == 0)
                {
                    code = GenerateCustomerCode(conn);
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Customers WHERE CustomerCode = @Code AND IsDeleted = 0", conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", code);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        {
                            ShowMessage("کدی با این مقدار قبلاً ثبت شده است. کد دیگری وارد کنید یا خالی بگذارید.");
                            return;
                        }
                    }
                }

                int newCustomerId = 0;
                string insertSql =
                    "INSERT INTO Customers " +
                    "  (CustomerCode, CustomerName, CustomerType, Mobile, City, Address, " +
                    "   EconomicCode, CreditLimit, MarketerId, IsActive, IsDeleted, " +
                    "   CreatedAt, CreatedBy) " +
                    "VALUES (@Code, @Name, @Type, @Mobile, @City, @Address, " +
                    "   @EconCode, 0, @MarketerId, 1, 0, " +
                    "   GETDATE(), @CreatedBy); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Type", customerType);
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@City", city.Length > 0 ? (object)city : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", address.Length > 0 ? (object)address : DBNull.Value);
                    cmd.Parameters.AddWithValue("@EconCode", econCode.Length > 0 ? (object)econCode : DBNull.Value);
                    cmd.Parameters.AddWithValue("@MarketerId",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                    object r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value)
                        newCustomerId = Convert.ToInt32(r);
                }

                if (newCustomerId <= 0)
                {
                    ShowMessage("خطا در ثبت مشتری.");
                    return;
                }

                WriteAudit(conn, "Insert", "Customers", newCustomerId, name,
                    "ثبت مشتری جدید توسط بازاریاب (کاتالوگ فروش)");

                LoadCustomersJson();

                Dictionary<string, object> newCust = new Dictionary<string, object>();
                newCust["id"] = newCustomerId;
                newCust["code"] = code;
                newCust["name"] = name;
                newCust["type"] = customerType;
                newCust["city"] = city;
                newCust["mobile"] = mobile;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                hfNewCustomerData.Value = serializer.Serialize(newCust);

                txtNewCustomerName.Text = "";
                txtNewCustomerMobile.Text = "";
                txtNewCustomerCity.Text = "";
                txtNewCustomerCode.Text = "";
                txtNewCustomerEconomicCode.Text = "";
                txtNewCustomerAddress.Text = "";

                string script = "setTimeout(function(){ if(typeof handleNewCustomerSaved === 'function') handleNewCustomerSaved(); }, 100);";
                if (ScriptManager.GetCurrent(this) != null)
                    ScriptManager.RegisterStartupScript(this, GetType(),
                        "newCust_" + Guid.NewGuid().ToString("N"), script, true);
                else
                    ClientScript.RegisterStartupScript(GetType(),
                        "newCust_" + Guid.NewGuid().ToString("N"), script, true);
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    private string GenerateCustomerCode(SqlConnection conn)
    {
        string prefix = "C-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-";

        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(CustomerCode, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
            "FROM Customers WHERE CustomerCode LIKE @Prefix + '%'", conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + seq.ToString("D3");
        }
    }

    // ═══════════════════════════════════════════════════════
    // SUBMIT CART
    // ═══════════════════════════════════════════════════════
    private void ProcessCartSubmission()
    {
        try
        {
            string json = hfCartJson.Value;
            if (string.IsNullOrEmpty(json)) { ShowMessage("سبد خالی است."); return; }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> payload;

            try
            {
                payload = serializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch
            {
                ShowMessage("اطلاعات سبد نامعتبر است.");
                return;
            }

            if (payload == null) { ShowMessage("اطلاعات سبد نامعتبر است."); return; }

            int customerId = 0;
            if (payload.ContainsKey("customerId") && payload["customerId"] != null)
            {
                int.TryParse(payload["customerId"].ToString(), out customerId);
            }
            if (customerId <= 0) { ShowMessage("مشتری انتخاب نشده است."); return; }

            int orderType = 2;
            if (payload.ContainsKey("customerType") && payload["customerType"] != null)
            {
                int.TryParse(payload["customerType"].ToString(), out orderType);
            }

            decimal globalDiscount = 0m;
            if (payload.ContainsKey("discount") && payload["discount"] != null)
            {
                decimal.TryParse(payload["discount"].ToString(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out globalDiscount);
            }

            if (!payload.ContainsKey("items") || payload["items"] == null)
            {
                ShowMessage("سبد خالی است.");
                return;
            }

            System.Collections.ArrayList itemsRaw = payload["items"] as System.Collections.ArrayList;
            if (itemsRaw == null || itemsRaw.Count == 0)
            {
                ShowMessage("سبد خالی است.");
                return;
            }

            List<CartItem> items = new List<CartItem>();
            foreach (object o in itemsRaw)
            {
                Dictionary<string, object> d = o as Dictionary<string, object>;
                if (d == null) continue;

                CartItem ci = new CartItem();

                if (d.ContainsKey("productId") && d["productId"] != null)
                {
                    int productId;
                    if (int.TryParse(d["productId"].ToString(), out productId))
                        ci.ProductId = productId;
                }
                if (d.ContainsKey("qty") && d["qty"] != null)
                {
                    decimal q;
                    if (decimal.TryParse(d["qty"].ToString(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out q))
                        ci.Quantity = q;
                }
                if (d.ContainsKey("price") && d["price"] != null)
                {
                    decimal p;
                    if (decimal.TryParse(d["price"].ToString(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out p))
                        ci.UnitPrice = p;
                }

                if (ci.ProductId > 0 && ci.Quantity > 0)
                    items.Add(ci);
            }

            if (items.Count == 0) { ShowMessage("سبد خالی است."); return; }

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string proformaNumber = GenerateProformaNumber(conn);
                int marketerId = GetCustomerMarketerId(conn, customerId);

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int proformaId = 0;

                        using (SqlCommand cmd = new SqlCommand(
                            "INSERT INTO ProformaInvoices " +
                            "  (ProformaNumber, CustomerId, OrderType, Channel, MarketerId, " +
                            "   IssueDate, ValidUntil, Status, Subtotal, Discount, Tax, Total, " +
                            "   IsDeleted, CreatedAt, CreatedBy) " +
                            "VALUES (@Number, @CId, @OT, 1, @Mk, " +
                            "   GETDATE(), DATEADD(DAY, 3, GETDATE()), 1, 0, 0, 0, 0, " +
                            "   0, GETDATE(), @By); " +
                            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Number", proformaNumber);
                            cmd.Parameters.AddWithValue("@CId", customerId);
                            cmd.Parameters.AddWithValue("@OT", orderType);
                            cmd.Parameters.AddWithValue("@Mk",
                                marketerId > 0 ? (object)marketerId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@By",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                            object r = cmd.ExecuteScalar();
                            if (r != null && r != DBNull.Value)
                                proformaId = Convert.ToInt32(r);
                        }

                        if (proformaId <= 0) throw new Exception("خطا در ایجاد پیش‌فاکتور.");

                        decimal subtotal = 0m;
                        decimal totalTax = 0m;

                        foreach (CartItem ci in items)
                        {
                            decimal itemSubtotal = Math.Round(ci.Quantity * ci.UnitPrice, 2);
                            decimal itemDiscount = 0m;
                            decimal itemTax = 0m;
                            decimal itemTotal = itemSubtotal - itemDiscount + itemTax;

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO ProformaInvoiceItems " +
                                "  (ProformaInvoiceId, ProductId, Quantity, UnitPrice, Discount, Tax, Total) " +
                                "VALUES (@PId, @ProdId, @Qty, @UP, @Disc, @Tax, @Total)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@PId", proformaId);
                                cmd.Parameters.AddWithValue("@ProdId", ci.ProductId);
                                cmd.Parameters.AddWithValue("@Qty", ci.Quantity);
                                cmd.Parameters.AddWithValue("@UP", ci.UnitPrice);
                                cmd.Parameters.AddWithValue("@Disc", itemDiscount);
                                cmd.Parameters.AddWithValue("@Tax", itemTax);
                                cmd.Parameters.AddWithValue("@Total", itemTotal);
                                cmd.ExecuteNonQuery();
                            }

                            subtotal += itemSubtotal;
                            totalTax += itemTax;
                        }

                        decimal totalDiscount = globalDiscount;
                        if (totalDiscount < 0m) totalDiscount = 0m;
                        if (totalDiscount > subtotal) totalDiscount = subtotal;

                        decimal total = subtotal - totalDiscount + totalTax;
                        if (total < 0m) total = 0m;

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProformaInvoices SET " +
                            "  Subtotal = @Sub, Discount = @Disc, Tax = @Tax, Total = @Total " +
                            "WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Sub", subtotal);
                            cmd.Parameters.AddWithValue("@Disc", totalDiscount);
                            cmd.Parameters.AddWithValue("@Tax", totalTax);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@Id", proformaId);
                            cmd.ExecuteNonQuery();
                        }

                        WriteAuditTran(conn, tran, "Insert", "ProformaInvoices", proformaId,
                            proformaNumber, "ایجاد پیش‌فاکتور از کاتالوگ فروش");

                        tran.Commit();

                        hfCartJson.Value = "[]";

                        string script =
                            "if (typeof showSuccessModal === 'function') {" +
                            "  showSuccessModal(" + proformaId.ToString(CultureInfo.InvariantCulture) +
                            ", '" + proformaNumber.Replace("'", "") + "');" +
                            "}";

                        if (ScriptManager.GetCurrent(this) != null)
                            ScriptManager.RegisterStartupScript(this, GetType(),
                                "success_" + Guid.NewGuid().ToString("N"), script, true);
                        else
                            ClientScript.RegisterStartupScript(GetType(),
                                "success_" + Guid.NewGuid().ToString("N"), script, true);
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت پیش‌فاکتور: " + ex.Message);
        }
    }

    private class CartItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private string GenerateProformaNumber(SqlConnection conn)
    {
        PersianCalendar pc = new PersianCalendar();
        int year = pc.GetYear(DateTime.Now);
        string prefix = "PF-" + year.ToString(CultureInfo.InvariantCulture) + "-";

        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(ProformaNumber, LEN(@Prefix) + 1, 10) AS INT)), 0) + 1 " +
            "FROM ProformaInvoices WHERE ProformaNumber LIKE @Prefix + '%'", conn))
        {
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            object o = cmd.ExecuteScalar();
            int seq = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
            return prefix + seq.ToString("D4");
        }
    }

    private int GetCustomerMarketerId(SqlConnection conn, int customerId)
    {
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(MarketerId, 0) FROM Customers WHERE CustomerId = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
        }
        catch { return 0; }
    }

    private string FormatQtyValue(decimal d)
    {
        if (d == 0m) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
    }

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

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message.Replace("\\", "\\\\").Replace("'", "\\'")
            .Replace("\r", " ").Replace("\n", "\\n");
        string script = "alert('" + safe + "');";
        if (ScriptManager.GetCurrent(this) != null)
            ScriptManager.RegisterStartupScript(this, GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
        else
            ClientScript.RegisterStartupScript(GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
    }
}