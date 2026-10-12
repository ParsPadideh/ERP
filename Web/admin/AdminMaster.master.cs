using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_AdminMaster : System.Web.UI.MasterPage
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litYear.Text = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        }

        // چک احراز هویت
        if (Session == null || Session["UserId"] == null)
        {
            string path = Request.Url.AbsolutePath.ToLower();
            if (path.IndexOf("login.aspx", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Response.Redirect(ResolveUrl("~/Login.aspx") + "?returnUrl=" +
                    HttpUtility.UrlEncode(Request.RawUrl), false);
                return;
            }
        }

        LoadUserInfo();
        ApplyRolePermissions();
        LoadBadges();

        litBreadcrumb.Text = BuildBreadcrumb();
        LoadNotifications();
    }

    // ─────────────────────────────────────────────────────
    // اطلاعات کاربر
    // ─────────────────────────────────────────────────────
    private void LoadUserInfo()
    {
        string fullName = "";
        string username = "";
        string roleName = "کاربر";

        try
        {
            if (Session["FullName"] != null)
                fullName = Convert.ToString(Session["FullName"]).Trim();
            if (Session["Username"] != null)
                username = Convert.ToString(Session["Username"]).Trim();
            if (Session["PrimaryRole"] != null)
                roleName = Convert.ToString(Session["PrimaryRole"]).Trim();

            if (fullName.Length == 0 && username.Length == 0 && Session["UserId"] != null)
            {
                long uid = Convert.ToInt64(Session["UserId"]);
                LoadUserFromDb(uid);
                return;
            }
        }
        catch { }

        ApplyUserToUi(fullName, username, roleName);
    }

    private void ApplyUserToUi(string fullName, string username, string roleName)
    {
        string initial = "U";
        if (fullName.Length > 0)
            initial = fullName.Substring(0, 1);
        else if (username.Length > 0)
            initial = username.Substring(0, 1).ToUpper();

        litUserInitial.Text = HttpUtility.HtmlEncode(initial);
        litUserInitialTop.Text = HttpUtility.HtmlEncode(initial);
        litUserInitialMenu.Text = HttpUtility.HtmlEncode(initial);

        string displayName = fullName.Length > 0 ? fullName : username;
        if (displayName.Length == 0) displayName = "کاربر";

        litUserFullName.Text = HttpUtility.HtmlEncode(displayName);
        litUserNameTop.Text = HttpUtility.HtmlEncode(displayName);
        litUserFullNameMenu.Text = HttpUtility.HtmlEncode(displayName);

        litUsernameMenu.Text = HttpUtility.HtmlEncode(username.Length > 0 ? username : "user");

        litUserRole.Text = HttpUtility.HtmlEncode(roleName);
        litUserRoleTop.Text = HttpUtility.HtmlEncode(roleName);
    }

    private void LoadUserFromDb(long userId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT u.Username, u.FullName, r.RoleName " +
                    "FROM Users u " +
                    "INNER JOIN Roles r ON r.RoleId = u.RoleId " +
                    "WHERE u.UserId = @Id AND u.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string username = Convert.ToString(dr["Username"]);
                            string fullName = dr["FullName"] == DBNull.Value
                                ? "" : Convert.ToString(dr["FullName"]).Trim();
                            string roleName = dr["RoleName"] == DBNull.Value
                                ? "کاربر" : Convert.ToString(dr["RoleName"]);

                            Session["Username"] = username;
                            Session["FullName"] = fullName;
                            Session["PrimaryRole"] = roleName;

                            ApplyUserToUi(fullName, username, roleName);
                        }
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // دسترسی بر اساس نقش
    // ─────────────────────────────────────────────────────
    private void ApplyRolePermissions()
    {
        string role = "کاربر";
        try
        {
            if (Session["PrimaryRole"] != null)
                role = Convert.ToString(Session["PrimaryRole"]);
        }
        catch { }

        bool isAdmin = string.Equals(role, "مدیر سیستم", StringComparison.OrdinalIgnoreCase);
        bool isManager = isAdmin ||
                         string.Equals(role, "مدیر بهره‌برداری", StringComparison.OrdinalIgnoreCase);
        bool isOwner = string.Equals(role, "مالک تجهیزات", StringComparison.OrdinalIgnoreCase);
        bool isMarketer = string.Equals(role, "بازاریاب", StringComparison.OrdinalIgnoreCase);
        bool isKeeper = string.Equals(role, "انباردار", StringComparison.OrdinalIgnoreCase);
        bool isProducer = string.Equals(role, "مسئول تولید", StringComparison.OrdinalIgnoreCase);
        bool isAccountant = string.Equals(role, "حسابدار", StringComparison.OrdinalIgnoreCase);

        // داده‌های پایه
        pnlSectionBase.Visible = isAdmin || isManager || isKeeper || isProducer;

        // طرف‌های تجاری
        pnlSectionParties.Visible = isAdmin || isManager || isMarketer || isAccountant;

        // انبار
        pnlSectionStock.Visible = isAdmin || isManager || isKeeper;

        // خرید
        pnlSectionPurchase.Visible = isAdmin || isManager || isKeeper;

        // تولید
        pnlSectionProduction.Visible = isAdmin || isManager || isProducer;

        // خدمات (جدید) — اپراتورها، انباردار، مسئول تولید و مدیر
        pnlSectionServices.Visible = isAdmin || isManager || isKeeper || isProducer || isAccountant;

        // فروش
        pnlSectionSales.Visible = isAdmin || isManager || isMarketer || isAccountant;

        // مالی
        pnlSectionFinance.Visible = isAdmin || isManager || isAccountant;

        // شرکا و سود
        pnlSectionPartners.Visible = isAdmin || isManager || isAccountant || isOwner;

        // سیستم
        pnlSectionSystem.Visible = isAdmin;

        // مالک تجهیزات — فقط شرکا و سود
        if (isOwner)
        {
            pnlSectionBase.Visible = false;
            pnlSectionParties.Visible = false;
            pnlSectionStock.Visible = false;
            pnlSectionPurchase.Visible = false;
            pnlSectionProduction.Visible = false;
            pnlSectionServices.Visible = false;
            pnlSectionSales.Visible = false;
            pnlSectionFinance.Visible = false;
            pnlSectionSystem.Visible = false;
            // pnlSectionPartners دست‌نخورده — مالک (شریک) باید ببیند
        }
    }
    // ─────────────────────────────────────────────────────
    // Active link
    // ─────────────────────────────────────────────────────
    protected bool IsActive(string pageName)
    {
        if (string.IsNullOrEmpty(pageName)) return false;
        string current = Request.AppRelativeCurrentExecutionFilePath;
        if (string.IsNullOrEmpty(current)) return false;
        return current.EndsWith(pageName, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────
    // Breadcrumb
    // ─────────────────────────────────────────────────────
    private string BuildBreadcrumb()
    {
        string current = Request.AppRelativeCurrentExecutionFilePath;
        if (string.IsNullOrEmpty(current)) return "";

        string pageName = System.IO.Path.GetFileName(current).ToLowerInvariant();

        var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        map["default.aspx"] = "داشبورد";
        map["products.aspx"] = "محصولات";
        map["product-categories.aspx"] = "دسته‌بندی محصولات";
        map["units.aspx"] = "واحدهای اندازه‌گیری";
        map["warehouses.aspx"] = "انبارها";
        map["customers.aspx"] = "مشتریان";
        map["suppliers.aspx"] = "تأمین‌کنندگان";
        map["price-lists.aspx"] = "لیست‌های قیمت";
        map["stock-onhand.aspx"] = "موجودی لحظه‌ای";
        map["stock-movements.aspx"] = "گردش انبار";
        map["purchase-orders.aspx"] = "سفارش‌های خرید";
        map["boms.aspx"] = "فرمول ساخت";
        map["production-orders.aspx"] = "دستورهای تولید";
        map["sales-orders.aspx"] = "سفارش‌های فروش";
        map["proforma-invoices.aspx"] = "پیش‌فاکتورها";
        map["final-invoices.aspx"] = "فاکتورهای نهایی";
        map["expenses.aspx"] = "هزینه‌ها";
        map["expense-categories.aspx"] = "دسته‌بندی هزینه‌ها";
        map["users.aspx"] = "کاربران";
        map["roles.aspx"] = "نقش‌ها";
        map["audit-logs.aspx"] = "لاگ فعالیت‌ها";
        map["change-password.aspx"] = "تغییر رمز عبور";
        map["partners.aspx"] = "شرکا و سهام";
        map["profit-loss.aspx"] = "گزارش سود و زیان";
        map["profit-periods.aspx"] = "دوره‌های تقسیم سود";
        map["profit-settings.aspx"] = "تنظیمات تقسیم سود";
        // ═══ صفحات جدید (خدمات) ═══
        map["service-types.aspx"] = "کاتالوگ خدمات";
        map["service-orders.aspx"] = "سفارش‌های خدمات";
        map["service-receipt.aspx"] = "رسید خدمات";

        map["reports-hub.aspx"] = "مرکز گزارش‌ها";
        string title;
        if (map.TryGetValue(pageName, out title))
        {
            if (pageName.Equals("default.aspx", StringComparison.OrdinalIgnoreCase)) return "";
            return "<span class=\"bc-sep\">/</span><span class=\"text-ink-800\">" +
                   HttpUtility.HtmlEncode(title) + "</span>";
        }
        return "";
    }

    // ─────────────────────────────────────────────────────
    // Badge ها
    // ─────────────────────────────────────────────────────
    private void LoadBadges()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── موجودی زیر حداقل ──
                int lowStockCount = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Products p " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                    "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock", conn))
                {
                    object o = cmd.ExecuteScalar();
                    lowStockCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litLowStockBadge.Text = lowStockCount > 0
                    ? "<span class=\"nav-badge nav-badge--warn\">" + lowStockCount.ToString(CultureInfo.InvariantCulture) + "</span>"
                    : "";

                // ── دستور تولید در حال اجرا ──
                int runningProd = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductionOrders WHERE Status = 2", conn))
                {
                    object o = cmd.ExecuteScalar();
                    runningProd = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litProdBadge.Text = runningProd > 0
                    ? "<span class=\"nav-badge nav-badge--info\">" + runningProd.ToString(CultureInfo.InvariantCulture) + "</span>"
                    : "";

                // ── پیش‌فاکتورهای در انتظار پرداخت ──
                int openProforma = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProformaInvoices " +
                    "WHERE Status IN (2, 3) AND ValidUntil >= GETDATE()", conn))
                {
                    object o = cmd.ExecuteScalar();
                    openProforma = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litProformaBadge.Text = openProforma > 0
                    ? "<span class=\"nav-badge\">" + openProforma.ToString(CultureInfo.InvariantCulture) + "</span>"
                    : "";

                // ── سفارش‌های خدمات در انتظار تسویه (جدید) ──
                int pendingServices = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders " +
                    "WHERE Status = 1 AND IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    pendingServices = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litServiceBadge.Text = pendingServices > 0
                    ? "<span class=\"nav-badge nav-badge--info\">" + pendingServices.ToString(CultureInfo.InvariantCulture) + "</span>"
                    : "";

                // ── دوره‌های بسته‌شده منتظر تقسیم (جدید) ──
                int pendingPeriods = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProfitPeriods WHERE Status = 2", conn))
                {
                    object o = cmd.ExecuteScalar();
                    pendingPeriods = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
                litPeriodsBadge.Text = pendingPeriods > 0
                    ? "<span class=\"nav-badge nav-badge--warn\">" + pendingPeriods.ToString(CultureInfo.InvariantCulture) + "</span>"
                    : "";
            }
        }
        catch
        {
            litLowStockBadge.Text = "";
            litProdBadge.Text = "";
            litProformaBadge.Text = "";
            litServiceBadge.Text = "";
            litPeriodsBadge.Text = "";
        }
    }

    // ─────────────────────────────────────────────────────
    // Notifications (Topbar)
    // ─────────────────────────────────────────────────────
    private void LoadNotifications()
    {
        try
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Title", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Link", typeof(string));
            dt.Columns.Add("TimeText", typeof(string));

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ۱) کالاهای زیر حداقل موجودی
                string sqlLow =
                    "SELECT TOP 3 p.ProductId, p.ProductCode, p.ProductName, p.MinStock, " +
                    "  ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "          WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) AS OnHand " +
                    "FROM Products p " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.MinStock > 0 " +
                    "  AND ISNULL((SELECT SUM(sl.QuantityRemaining) FROM StockLayers sl " +
                    "              WHERE sl.ProductId = p.ProductId AND sl.IsClosed = 0), 0) < p.MinStock " +
                    "ORDER BY p.MinStock DESC";

                using (SqlCommand cmd = new SqlCommand(sqlLow, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string name = Convert.ToString(dr["ProductName"]);
                        decimal onHand = dr["OnHand"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["OnHand"]);
                        decimal minStock = dr["MinStock"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["MinStock"]);

                        DataRow row = dt.NewRow();
                        row["Type"] = "low_stock";
                        row["Title"] = "موجودی کم";
                        row["Description"] = name + " — موجودی " + onHand.ToString("N0") + " از " + minStock.ToString("N0");
                        row["Link"] = "stock-onhand.aspx";
                        row["TimeText"] = "نیاز به سفارش";
                        dt.Rows.Add(row);
                    }
                }

                // ۲) پیش‌فاکتورهای نزدیک به انقضا
                string sqlPf =
                    "SELECT TOP 3 ProformaInvoiceId, ProformaNumber, ValidUntil " +
                    "FROM ProformaInvoices " +
                    "WHERE Status IN (2,3) AND ValidUntil >= GETDATE() " +
                    "  AND ValidUntil <= DATEADD(DAY, 3, GETDATE()) " +
                    "ORDER BY ValidUntil";

                using (SqlCommand cmd = new SqlCommand(sqlPf, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string number = Convert.ToString(dr["ProformaNumber"]);
                        DateTime validUntil = Convert.ToDateTime(dr["ValidUntil"]);

                        DataRow row = dt.NewRow();
                        row["Type"] = "proforma_expiring";
                        row["Title"] = "پیش‌فاکتور در آستانه انقضا";
                        row["Description"] = number + " — انقضا: " + FormatRelative(validUntil);
                        row["Link"] = "proforma-invoices.aspx";
                        row["TimeText"] = FormatRelative(validUntil);
                        dt.Rows.Add(row);
                    }
                }

                // ۳) دستور تولید در حال اجرا
                string sqlProd =
                    "SELECT TOP 3 po.ProductionOrderId, po.OrderNumber, p.ProductName " +
                    "FROM ProductionOrders po " +
                    "INNER JOIN Products p ON p.ProductId = po.ProductId " +
                    "WHERE po.Status = 2 ORDER BY po.ProductionOrderId DESC";

                using (SqlCommand cmd = new SqlCommand(sqlProd, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string number = Convert.ToString(dr["OrderNumber"]);
                        string pname = Convert.ToString(dr["ProductName"]);

                        DataRow row = dt.NewRow();
                        row["Type"] = "prod_running";
                        row["Title"] = "دستور تولید در حال اجرا";
                        row["Description"] = number + " — " + pname;
                        row["Link"] = "production-orders.aspx";
                        row["TimeText"] = "در حال اجرا";
                        dt.Rows.Add(row);
                    }
                }
            }

            int total = dt.Rows.Count;

            if (total == 0)
            {
                litNotifDot.Text = "";
                litNotifCount.Text = "<span class=\"badge bg-surface-muted text-ink-500\">۰ مورد</span>";

                DataRow row = dt.NewRow();
                row["Type"] = "info";
                row["Title"] = "اعلان جدیدی وجود ندارد";
                row["Description"] = "همه چیز مرتب است ✓";
                row["Link"] = "Default.aspx";
                row["TimeText"] = "الان";
                dt.Rows.Add(row);
            }
            else
            {
                litNotifDot.Text = "<span class=\"notif-dot\"></span>";
                litNotifCount.Text = "<span class=\"badge bg-danger-50 text-danger-500\">" +
                    total.ToString(CultureInfo.InvariantCulture) + " مورد</span>";
            }

            rptNotifications.DataSource = dt;
            rptNotifications.DataBind();
        }
        catch
        {
            litNotifDot.Text = "";
            litNotifCount.Text = "";
        }
    }

    private string FormatRelative(DateTime dt)
    {
        TimeSpan span = dt - DateTime.Now;

        if (span.TotalSeconds < 0)
        {
            TimeSpan past = DateTime.Now - dt;
            if (past.TotalMinutes < 60) return ((int)past.TotalMinutes).ToString(CultureInfo.InvariantCulture) + " دقیقه پیش";
            if (past.TotalHours < 24) return ((int)past.TotalHours).ToString(CultureInfo.InvariantCulture) + " ساعت پیش";
            return ((int)past.TotalDays).ToString(CultureInfo.InvariantCulture) + " روز پیش";
        }

        if (span.TotalMinutes < 60) return "تا " + ((int)span.TotalMinutes).ToString(CultureInfo.InvariantCulture) + " دقیقه دیگر";
        if (span.TotalHours < 24) return "تا " + ((int)span.TotalHours).ToString(CultureInfo.InvariantCulture) + " ساعت دیگر";
        return "تا " + ((int)span.TotalDays).ToString(CultureInfo.InvariantCulture) + " روز دیگر";
    }

    // ─────────────────────────────────────────────────────
    // Helpers اعلان
    // ─────────────────────────────────────────────────────
    protected string GetNotifIconCss(object typeObj)
    {
        string type = (typeObj == null || typeObj == DBNull.Value) ? "" : typeObj.ToString();
        string baseC = "grid h-9 w-9 shrink-0 place-items-center rounded-lg ";

        if (type == "low_stock") return baseC + "bg-danger-50 text-danger-500";
        if (type == "proforma_expiring") return baseC + "bg-warning-50 text-warning-600";
        if (type == "prod_running") return baseC + "bg-brand-50 text-brand-600";
        return baseC + "bg-surface-muted text-ink-500";
    }

    protected string GetNotifIcon(object typeObj)
    {
        string type = (typeObj == null || typeObj == DBNull.Value) ? "" : typeObj.ToString();
        if (type == "low_stock") return "package-x";
        if (type == "proforma_expiring") return "clock";
        if (type == "prod_running") return "hammer";
        return "info";
    }

    // ─────────────────────────────────────────────────────
    // Logout
    // ─────────────────────────────────────────────────────
    protected void btnLogout_Click(object sender, EventArgs e)
    {
        try { WriteLogoutAudit(); }
        catch { }

        Session.Clear();
        Session.Abandon();

        try { System.Web.Security.FormsAuthentication.SignOut(); }
        catch { }

        Response.Redirect(ResolveUrl("~/Login.aspx") + "?logout=1", false);
    }

    private void WriteLogoutAudit()
    {
        try
        {
            if (Session["UserId"] == null) return;

            long userId = Convert.ToInt64(Session["UserId"]);
            string username = Session["Username"] == null ? "" : Convert.ToString(Session["Username"]);
            string ip = GetClientIP();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                    "VALUES (@UserId, 'Logout', 'Users', @UserId, @NewValues, @IPAddress, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@NewValues",
                        "{\"message\":\"خروج کاربر " + username.Replace("\"", "'") + "\"}");
                    cmd.Parameters.AddWithValue("@IPAddress",
                        ip.Length > 0 ? (object)ip : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
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
}