using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;

public partial class admin_reports_hub : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litTodayShamsi.Text = HttpUtility.HtmlEncode(ToShamsiDateString(DateTime.Now));
            LoadStats();
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Live Stats
    // ─────────────────────────────────────────────────────
    private void LoadStats()
    {
        try
        {
            DateTime monthStart = GetCurrentPersianMonthStart();
            DateTime monthEnd = new PersianCalendar().AddMonths(monthStart, 1);

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ═══ 1) درآمد کل این ماه (فروش + خدمات) ═══
                decimal productRev = 0m;
                decimal serviceRev = 0m;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(fii.Total), 0) " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) AND fi.IsDeleted = 0 " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    productRev = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Total), 0) FROM ServiceOrders " +
                    "WHERE Status = 3 AND IsDeleted = 0 " +
                    "  AND ServiceDate >= @From AND ServiceDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    serviceRev = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                decimal totalRevenue = productRev + serviceRev;
                litStatRevenue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(totalRevenue));

                // ═══ 2) سود خالص این ماه ═══
                decimal cogs = 0m;
                decimal expenses = 0m;

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(fii.COGS), 0) " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) AND fi.IsDeleted = 0 " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    cogs = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", monthStart);
                    cmd.Parameters.AddWithValue("@To", monthEnd);
                    object o = cmd.ExecuteScalar();
                    expenses = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                decimal netProfit = totalRevenue - cogs - expenses;
                litStatProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(netProfit));

                // رنگ سود
                if (netProfit > 0m)
                {
                    pStatProfit.Attributes["class"] = "mt-0.5 font-mono text-[16px] font-bold text-emerald-700";
                }
                else if (netProfit < 0m)
                {
                    pStatProfit.Attributes["class"] = "mt-0.5 font-mono text-[16px] font-bold text-danger-600";
                }
                else
                {
                    pStatProfit.Attributes["class"] = "mt-0.5 font-mono text-[16px] font-bold text-ink-500";
                }

                // ═══ 3) ارزش موجودی انبار ═══
                decimal stockValue = 0m;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(sl.QuantityRemaining * sl.UnitCost), 0) " +
                    "FROM StockLayers sl " +
                    "INNER JOIN Products p ON p.ProductId = sl.ProductId " +
                    "INNER JOIN Warehouses w ON w.WarehouseId = sl.WarehouseId " +
                    "WHERE sl.IsClosed = 0 AND sl.QuantityRemaining > 0 " +
                    "  AND p.IsDeleted = 0 AND w.IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    stockValue = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                litStatStockValue.Text = HttpUtility.HtmlEncode(FormatMoneyValue(stockValue));

                // ═══ 4) خدمات در جریان ═══
                int pendingServices = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ServiceOrders " +
                    "WHERE Status = 1 AND IsDeleted = 0", conn))
                {
                    object o = cmd.ExecuteScalar();
                    pendingServices = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                litStatPendingServices.Text = HttpUtility.HtmlEncode(
                    pendingServices.ToString("N0", CultureInfo.InvariantCulture));
            }
        }
        catch (Exception ex)
        {
            // اگر خطایی بود، فقط آمار را صفر بگذار — صفحه باید لود شود
            litStatRevenue.Text = "۰";
            litStatProfit.Text = "۰";
            litStatStockValue.Text = "۰";
            litStatPendingServices.Text = "۰";
        }
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private DateTime GetCurrentPersianMonthStart()
    {
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        return pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        bool neg = d < 0m;
        decimal abs = Math.Abs(d);
        string s = abs.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return neg ? "(" + s + ")" : s;
    }
}