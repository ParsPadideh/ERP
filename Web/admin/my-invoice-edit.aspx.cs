using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_my_invoice_edit : System.Web.UI.Page
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

    private int ProformaId
    {
        get
        {
            int id;
            if (int.TryParse(hfProformaId.Value, out id)) return id;
            return 0;
        }
        set { hfProformaId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // فقط بازاریاب
        string role = Session["PrimaryRole"] == null ? "" : Convert.ToString(Session["PrimaryRole"]);
        if (!string.Equals(role, "بازاریاب", StringComparison.OrdinalIgnoreCase))
        {
            Response.Redirect(ResolveUrl("~/admin/Default.aspx"), false);
            return;
        }

        if (!IsPostBack)
        {
            int id = 0;
            if (!int.TryParse(Request.QueryString["id"], out id) || id <= 0)
            {
                ShowNotFound();
                return;
            }

            ProformaId = id;

            if (!LoadDraft())
            {
                ShowNotFound();
                return;
            }

            LoadProductsForAdding();
        }
    }

    private void ShowNotFound()
    {
        pnlNotFound.Visible = true;
        pnlEdit.Visible = false;
    }

    private bool LoadDraft()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sqlHead =
                    "SELECT pi.ProformaInvoiceId, pi.ProformaNumber, pi.Status, pi.Discount, " +
                    "       pi.MarketerId, pi.Notes, " +
                    "       ISNULL(c.CustomerName, N'—') AS CustomerName, " +
                    "       ISNULL(c.City, N'') AS City, " +
                    "       ISNULL(c.Mobile, N'') AS Mobile " +
                    "FROM ProformaInvoices pi " +
                    "LEFT JOIN Customers c ON c.CustomerId = pi.CustomerId " +
                    "WHERE pi.ProformaInvoiceId = @Id AND pi.IsDeleted = 0";

                int status = 0;
                long ownerId = 0;
                decimal discount = 0m;
                string proformaNumber = "";
                string customerName = "";
                string city = "";
                string mobile = "";
                string notes = "";

                using (SqlCommand cmd = new SqlCommand(sqlHead, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", ProformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return false;

                        status = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
                        ownerId = dr["MarketerId"] == DBNull.Value ? 0 : Convert.ToInt64(dr["MarketerId"]);
                        discount = dr["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Discount"]);
                        proformaNumber = Convert.ToString(dr["ProformaNumber"]);
                        customerName = Convert.ToString(dr["CustomerName"]);
                        city = Convert.ToString(dr["City"]);
                        mobile = Convert.ToString(dr["Mobile"]);
                        notes = dr["Notes"] == DBNull.Value ? "" : Convert.ToString(dr["Notes"]);
                    }
                }

                if (ownerId != CurrentUserId) return false;
                if (status != 2) return false;

                litProformaNumber.Text = HttpUtility.HtmlEncode(proformaNumber);
                litCustomerName.Text = HttpUtility.HtmlEncode(customerName);
                litCustomerInfo.Text = HttpUtility.HtmlEncode(
                    (city.Length > 0 ? city : "") +
                    (mobile.Length > 0 ? (city.Length > 0 ? " · " : "") + mobile : ""));

                txtNotes.Text = notes;

                // اقلام
                string sqlItems =
                    "SELECT pii.ProformaInvoiceItemId, pii.ProductId, pii.Quantity, pii.UnitPrice, " +
                    "       p.ProductName, p.ProductCode, u.UnitName " +
                    "FROM ProformaInvoiceItems pii " +
                    "INNER JOIN Products p ON p.ProductId = pii.ProductId " +
                    "INNER JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE pii.ProformaInvoiceId = @Id";

                DataTable dtItems = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sqlItems, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", ProformaId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtItems);
                    }
                }

                // ساخت JSON
                Dictionary<string, object> meta = new Dictionary<string, object>();
                meta["discount"] = discount;

                List<Dictionary<string, object>> itemsList = new List<Dictionary<string, object>>();
                foreach (DataRow r in dtItems.Rows)
                {
                    Dictionary<string, object> it = new Dictionary<string, object>();
                    it["itemId"] = Convert.ToInt32(r["ProformaInvoiceItemId"]);
                    it["productId"] = Convert.ToInt32(r["ProductId"]);
                    it["name"] = Convert.ToString(r["ProductName"]);
                    it["code"] = Convert.ToString(r["ProductCode"]);
                    it["unit"] = Convert.ToString(r["UnitName"]);
                    it["qty"] = Convert.ToDecimal(r["Quantity"]);
                    it["unitPrice"] = Convert.ToDecimal(r["UnitPrice"]);
                    itemsList.Add(it);
                }

                Dictionary<string, object> root = new Dictionary<string, object>();
                root["meta"] = meta;
                root["items"] = itemsList;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                hfDraftData.Value = serializer.Serialize(root);

                litItemsCount.Text = dtItems.Rows.Count.ToString(CultureInfo.InvariantCulture);

                pnlNotFound.Visible = false;
                pnlEdit.Visible = true;

                return true;
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری پیش‌نویس: " + ex.Message);
            return false;
        }
    }

    private void LoadProductsForAdding()
    {
        try
        {
            ddlProductToAdd.Items.Clear();
            ddlProductToAdd.Items.Add(new ListItem("— محصول را انتخاب کنید —", ""));

            Dictionary<string, Dictionary<string, object>> productsDict =
                new Dictionary<string, Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT p.ProductId, p.ProductCode, p.ProductName, " +
                    "       ISNULL(u.UnitName, N'عدد') AS UnitName, " +
                    "       ISNULL((SELECT TOP 1 pli.UnitPrice FROM PriceListItems pli " +
                    "               INNER JOIN PriceLists pl ON pl.PriceListId = pli.PriceListId " +
                    "               WHERE pli.ProductId = p.ProductId AND pl.PriceListType = 1 " +
                    "                 AND pli.IsActive = 1 AND pl.IsActive = 1 AND pl.IsDeleted = 0 " +
                    "               ORDER BY pli.MinQuantity DESC), 0) AS PriceRetail " +
                    "FROM Products p " +
                    "LEFT JOIN Units u ON u.UnitId = p.UnitId " +
                    "WHERE p.IsActive = 1 AND p.IsDeleted = 0 AND p.ProductType = 2 " +
                    "ORDER BY p.ProductName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int pid = Convert.ToInt32(dr["ProductId"]);
                        string code = Convert.ToString(dr["ProductCode"]);
                        string name = Convert.ToString(dr["ProductName"]);
                        string unit = Convert.ToString(dr["UnitName"]);
                        decimal price = dr["PriceRetail"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["PriceRetail"]);

                        ddlProductToAdd.Items.Add(new ListItem(
                            code + " — " + name, pid.ToString(CultureInfo.InvariantCulture)));

                        Dictionary<string, object> p = new Dictionary<string, object>();
                        p["name"] = name;
                        p["code"] = code;
                        p["unit"] = unit;
                        p["price"] = price;

                        productsDict[pid.ToString(CultureInfo.InvariantCulture)] = p;
                    }
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(productsDict);
            litProductsJson.Text = "<script type=\"application/json\" id=\"" +
                litProductsJson.ClientID + "\">" + json + "</script>";
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری محصولات: " + ex.Message);
        }
    }

    // ═════════════════════════════════════════════════════════
    // ✅ اصلاح‌شده: استفاده از متغیر محلی به‌جای property
    // ═════════════════════════════════════════════════════════
    protected void btnSave_Click(object sender, EventArgs e)
    {
        try
        {
            if (ProformaId <= 0)
            {
                ShowMessage("شناسه پیش‌نویس نامعتبر است.");
                return;
            }

            string json = hfDraftData.Value;
            if (string.IsNullOrEmpty(json))
            {
                ShowMessage("داده‌ای برای ذخیره وجود ندارد.");
                return;
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> payload;

            try
            {
                payload = serializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch
            {
                ShowMessage("داده‌های ارسالی نامعتبر است.");
                return;
            }

            int ownerId = 0;
            int status = 0;
            string proformaNumber = "";

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MarketerId, Status, ProformaNumber FROM ProformaInvoices " +
                    "WHERE ProformaInvoiceId = @Id AND IsDeleted = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", ProformaId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowMessage("پیش‌نویس یافت نشد.");
                            return;
                        }
                        ownerId = dr["MarketerId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MarketerId"]);
                        status = dr["Status"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Status"]);
                        proformaNumber = Convert.ToString(dr["ProformaNumber"]);
                    }
                }

                if (ownerId != (int)CurrentUserId)
                {
                    ShowMessage("شما فقط به پیش‌نویس‌های خودتان دسترسی دارید.");
                    return;
                }

                if (status != 2)
                {
                    ShowMessage("این فاکتور دیگر پیش‌نویس نیست و قابل ویرایش نمی‌باشد.");
                    return;
                }

                List<EditItem> items = new List<EditItem>();

                if (payload.ContainsKey("items") && payload["items"] != null)
                {
                    System.Collections.ArrayList arr = payload["items"] as System.Collections.ArrayList;
                    if (arr != null)
                    {
                        foreach (object o in arr)
                        {
                            Dictionary<string, object> d = o as Dictionary<string, object>;
                            if (d == null) continue;

                            EditItem it = new EditItem();

                            // ✅ اصلاح: متغیر محلی برای int
                            if (d.ContainsKey("productId") && d["productId"] != null)
                            {
                                int localProductId = 0;
                                int.TryParse(d["productId"].ToString(), out localProductId);
                                it.ProductId = localProductId;
                            }

                            // ✅ اصلاح: متغیر محلی برای decimal (qty)
                            if (d.ContainsKey("qty") && d["qty"] != null)
                            {
                                decimal localQty = 0m;
                                decimal.TryParse(d["qty"].ToString(), NumberStyles.Any,
                                    CultureInfo.InvariantCulture, out localQty);
                                it.Quantity = localQty;
                            }

                            // ✅ اصلاح: متغیر محلی برای decimal (unitPrice)
                            if (d.ContainsKey("unitPrice") && d["unitPrice"] != null)
                            {
                                decimal localUnitPrice = 0m;
                                decimal.TryParse(d["unitPrice"].ToString(), NumberStyles.Any,
                                    CultureInfo.InvariantCulture, out localUnitPrice);
                                it.UnitPrice = localUnitPrice;
                            }

                            if (it.ProductId > 0 && it.Quantity > 0)
                                items.Add(it);
                        }
                    }
                }

                if (items.Count == 0)
                {
                    ShowMessage("پیش‌نویس باید حداقل یک قلم داشته باشد.");
                    return;
                }

                decimal discount = 0m;
                if (payload.ContainsKey("meta") && payload["meta"] != null)
                {
                    Dictionary<string, object> meta = payload["meta"] as Dictionary<string, object>;
                    if (meta != null && meta.ContainsKey("discount") && meta["discount"] != null)
                    {
                        decimal.TryParse(meta["discount"].ToString(), NumberStyles.Any,
                            CultureInfo.InvariantCulture, out discount);
                    }
                }

                decimal subtotal = 0m;
                foreach (EditItem it in items)
                {
                    subtotal += it.Quantity * it.UnitPrice;
                }

                if (discount < 0) discount = 0;
                if (discount > subtotal) discount = subtotal;

                decimal total = subtotal - discount;
                if (total < 0) total = 0;

                string notes = txtNotes.Text.Trim();

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "DELETE FROM ProformaInvoiceItems WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", ProformaId);
                            cmd.ExecuteNonQuery();
                        }

                        foreach (EditItem it in items)
                        {
                            decimal itemSubtotal = Math.Round(it.Quantity * it.UnitPrice, 2);

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO ProformaInvoiceItems " +
                                "  (ProformaInvoiceId, ProductId, Quantity, UnitPrice, Discount, Tax, Total) " +
                                "VALUES (@PId, @ProdId, @Qty, @UP, 0, 0, @Total)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@PId", ProformaId);
                                cmd.Parameters.AddWithValue("@ProdId", it.ProductId);
                                cmd.Parameters.AddWithValue("@Qty", it.Quantity);
                                cmd.Parameters.AddWithValue("@UP", it.UnitPrice);
                                cmd.Parameters.AddWithValue("@Total", itemSubtotal);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE ProformaInvoices SET " +
                            "  Subtotal = @Sub, Discount = @Disc, Tax = 0, Total = @Total, " +
                            "  Notes = @Notes, UpdatedAt = GETDATE(), UpdatedBy = @By " +
                            "WHERE ProformaInvoiceId = @Id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Sub", subtotal);
                            cmd.Parameters.AddWithValue("@Disc", discount);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@Notes",
                                notes.Length > 0 ? (object)notes : DBNull.Value);
                            cmd.Parameters.AddWithValue("@By", CurrentUserId);
                            cmd.Parameters.AddWithValue("@Id", ProformaId);
                            cmd.ExecuteNonQuery();
                        }

                        string auditJson = "{\"target\":\"" + proformaNumber.Replace("\"", "'") +
                            "\",\"note\":\"ویرایش پیش‌نویس توسط بازاریاب (" + items.Count + " قلم)\"}";

                        using (SqlCommand cmd = new SqlCommand(
                            "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                            "VALUES (@UserId, 'Update', 'ProformaInvoices', @EntityId, @NewValues, @IPAddress, GETDATE())",
                            conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@UserId", CurrentUserId);
                            cmd.Parameters.AddWithValue("@EntityId", ProformaId);
                            cmd.Parameters.AddWithValue("@NewValues", auditJson);
                            cmd.Parameters.AddWithValue("@IPAddress",
                                GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }

            ShowMessage("پیش‌نویس با موفقیت ذخیره شد.");

            string script = "setTimeout(function(){ window.location.href='my-invoices.aspx'; }, 800);";
            if (ScriptManager.GetCurrent(this) != null)
                ScriptManager.RegisterStartupScript(this, GetType(),
                    "redirect_" + Guid.NewGuid().ToString("N"), script, true);
            else
                ClientScript.RegisterStartupScript(GetType(),
                    "redirect_" + Guid.NewGuid().ToString("N"), script, true);
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره: " + ex.Message);
        }
    }

    private class EditItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
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

    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message.Replace("\\", "\\\\").Replace("'", "\\'")
            .Replace("\r", " ").Replace("\n", " ");
        string script = "alert('" + safe + "');";
        if (ScriptManager.GetCurrent(this) != null)
            ScriptManager.RegisterStartupScript(this, GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
        else
            ClientScript.RegisterStartupScript(GetType(),
                "msg_" + Guid.NewGuid().ToString("N"), script, true);
    }
}