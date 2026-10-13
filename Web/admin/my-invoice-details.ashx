<%@ WebHandler Language="C#" Class="admin_my_invoice_details" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

public class admin_my_invoice_details : IHttpHandler, IReadOnlySessionState
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.Cache.SetNoStore();

        try
        {
            if (context.Session == null || context.Session["UserId"] == null)
            {
                WriteError(context, "not_authenticated");
                return;
            }

            long userId = Convert.ToInt64(context.Session["UserId"]);
            string role = context.Session["PrimaryRole"] == null ? "" : Convert.ToString(context.Session["PrimaryRole"]);

            bool isMarketer = string.Equals(role, "بازاریاب", StringComparison.OrdinalIgnoreCase);
            bool isAdmin = string.Equals(role, "مدیر سیستم", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(role, "مدیر بهره‌برداری", StringComparison.OrdinalIgnoreCase);

            if (!isMarketer && !isAdmin)
            {
                WriteError(context, "not_authorized");
                return;
            }

            int invoiceId = 0;
            if (!int.TryParse(context.Request.QueryString["id"], out invoiceId) || invoiceId <= 0)
            {
                WriteError(context, "invalid_id");
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            Dictionary<string, object> result = new Dictionary<string, object>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sqlHead =
                    "SELECT pi.ProformaInvoiceId, pi.ProformaNumber, pi.IssueDate, pi.ValidUntil, " +
                    "  pi.Status, pi.Subtotal, pi.Discount, pi.Tax, pi.Total, " +
                    "  pi.PaymentTerms, pi.DeliveryTerms, pi.Notes, " +
                    "  ISNULL(pi.MarketerId, 0) AS MarketerId, " +
                    "  ISNULL(c.CustomerName, N'—') AS CustomerName, " +
                    "  ISNULL(c.City, N'') AS City, " +
                    "  ISNULL(c.Mobile, N'') AS Mobile, " +
                    "  ISNULL(c.Address, N'') AS Address, " +
                    "  fi.InvoiceNumber AS FinalInvoiceNumber " +
                    "FROM ProformaInvoices pi " +
                    "LEFT JOIN Customers c ON c.CustomerId = pi.CustomerId " +
                    "LEFT JOIN FinalInvoices fi ON fi.ProformaInvoiceId = pi.ProformaInvoiceId AND fi.IsDeleted = 0 " +
                    "WHERE pi.ProformaInvoiceId = @Id AND pi.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sqlHead, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            WriteError(context, "not_found");
                            return;
                        }

                        long marketerId = dr["MarketerId"] == DBNull.Value ? 0 : Convert.ToInt64(dr["MarketerId"]);

                        if (isMarketer && !isAdmin && marketerId != userId)
                        {
                            WriteError(context, "not_owned");
                            return;
                        }

                        result["id"] = Convert.ToInt32(dr["ProformaInvoiceId"]);
                        result["number"] = Convert.ToString(dr["ProformaNumber"]);
                        result["issueDate"] = FormatShamsi(dr["IssueDate"]);
                        result["validUntil"] = FormatShamsi(dr["ValidUntil"]);
                        result["status"] = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
                        result["statusLabel"] = GetStatusLabel(dr["Status"]);
                        result["statusCss"] = GetStatusCss(dr["Status"]);
                        result["subtotal"] = Convert.ToDecimal(dr["Subtotal"] == DBNull.Value ? 0m : dr["Subtotal"]);
                        result["discount"] = Convert.ToDecimal(dr["Discount"] == DBNull.Value ? 0m : dr["Discount"]);
                        result["tax"] = Convert.ToDecimal(dr["Tax"] == DBNull.Value ? 0m : dr["Tax"]);
                        result["total"] = Convert.ToDecimal(dr["Total"] == DBNull.Value ? 0m : dr["Total"]);
                        result["paymentTerms"] = Convert.ToString(dr["PaymentTerms"]);
                        result["deliveryTerms"] = Convert.ToString(dr["DeliveryTerms"]);
                        result["notes"] = Convert.ToString(dr["Notes"]);
                        result["customerName"] = Convert.ToString(dr["CustomerName"]);
                        result["city"] = Convert.ToString(dr["City"]);
                        result["mobile"] = Convert.ToString(dr["Mobile"]);
                        result["address"] = Convert.ToString(dr["Address"]);
                        result["finalInvoiceNumber"] = dr["FinalInvoiceNumber"] == DBNull.Value
                            ? "" : Convert.ToString(dr["FinalInvoiceNumber"]);
                    }
                }

                List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
                string sqlItems =
                    "SELECT pii.ProductId, pii.Quantity, pii.UnitPrice, pii.Total, " +
                    "  p.ProductName, p.ProductCode, u.UnitName " +
                    "FROM ProformaInvoiceItems pii " +
                    "INNER JOIN Products p ON p.ProductId = pii.ProductId " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE pii.ProformaInvoiceId = @Id " +
                    "ORDER BY pii.ProformaInvoiceItemId";

                using (SqlCommand cmd = new SqlCommand(sqlItems, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Dictionary<string, object> it = new Dictionary<string, object>();
                            it["productId"] = Convert.ToInt32(dr["ProductId"]);
                            it["name"] = Convert.ToString(dr["ProductName"]);
                            it["code"] = Convert.ToString(dr["ProductCode"]);
                            it["unit"] = Convert.ToString(dr["UnitName"]);
                            it["qty"] = Convert.ToDecimal(dr["Quantity"]);
                            it["unitPrice"] = Convert.ToDecimal(dr["UnitPrice"]);
                            it["total"] = Convert.ToDecimal(dr["Total"]);
                            items.Add(it);
                        }
                    }
                }

                result["items"] = items;
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(result));
        }
        catch (Exception ex)
        {
            WriteError(context, "server_error: " + ex.Message);
        }
    }

    private string FormatShamsi(object dateObj)
    {
        if (dateObj == null || dateObj == DBNull.Value) return "—";
        try
        {
            DateTime dt = Convert.ToDateTime(dateObj);
            PersianCalendar pc = new PersianCalendar();
            return string.Format("{0}/{1:D2}/{2:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
        }
        catch { return "—"; }
    }

    private string GetStatusLabel(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        switch (s)
        {
            case 1: return "صادرشده";
            case 2: return "پیش‌نویس";
            case 3: return "تبدیل‌شده";
            case 4: return "پرداخت‌شده";
            case 5: return "لغو شده";
            case 6: return "منقضی‌شده";
            default: return "نامشخص";
        }
    }

    private string GetStatusCss(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        switch (s)
        {
            case 1: return "bg-brand-50 text-brand-700";
            case 2: return "bg-surface-muted text-ink-700";
            case 3: return "bg-violet-50 text-violet-700";
            case 4: return "bg-success-50 text-success-700";
            case 5: return "bg-danger-50 text-danger-600";
            case 6: return "bg-warning-50 text-warning-700";
            default: return "bg-surface-muted text-ink-500";
        }
    }

    private void WriteError(HttpContext context, string message)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        var obj = new Dictionary<string, object>();
        obj["error"] = message;
        context.Response.Write(serializer.Serialize(obj));
    }

    public bool IsReusable { get { return false; } }
}