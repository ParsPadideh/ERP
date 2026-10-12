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

public partial class admin_partner_statement : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    private int PartnerId
    {
        get
        {
            int id;
            if (int.TryParse(hfPartnerId.Value, out id)) return id;
            return 0;
        }
        set { hfPartnerId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Check query string
            string pid = Request.QueryString["id"];
            int queryId;
            if (!string.IsNullOrEmpty(pid) && int.TryParse(pid, out queryId) && queryId > 0)
            {
                PartnerId = queryId;
            }

            // Default date range: current Persian year start
            DateTime yearStart = GetCurrentPersianYearStart();
            txtFrom.Text = ToShamsiDateString(yearStart);
            txtTo.Text = ToShamsiDateString(DateTime.Now);

            LoadPartnersDropdown();
            LoadCompanyInfo();

            if (PartnerId > 0)
            {
                SelectPartnerInDropdown(PartnerId);
                LoadStatement();
            }
            else
            {
                ShowBannerHidden();
            }
        }
    }

    private DateTime GetCurrentPersianYearStart()
    {
        PersianCalendar pc = new PersianCalendar();
        return pc.ToDateTime(pc.GetYear(DateTime.Now), 1, 1, 0, 0, 0, 0);
    }

    // ─────────────────────────────────────────────────────
    // Dropdown
    // ─────────────────────────────────────────────────────
    private void LoadPartnersDropdown()
    {
        ddlPartner.Items.Clear();
        ddlPartner.Items.Add(new ListItem("— انتخاب شریک —", "0"));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT PartnerId, FullName, IsActive FROM Partners " +
                    "WHERE IsDeleted = 0 ORDER BY IsActive DESC, FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["PartnerId"]).ToString(CultureInfo.InvariantCulture);
                        string name = Convert.ToString(dr["FullName"]);
                        bool active = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        string label = name + (active ? "" : " (غیرفعال)");
                        ddlPartner.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch { }
    }

    private void SelectPartnerInDropdown(int partnerId)
    {
        ddlPartner.ClearSelection();
        ListItem item = ddlPartner.Items.FindByValue(partnerId.ToString(CultureInfo.InvariantCulture));
        if (item != null) item.Selected = true;
    }

    protected void ddlPartner_SelectedIndexChanged(object sender, EventArgs e)
    {
        int pid;
        if (!string.IsNullOrEmpty(ddlPartner.SelectedValue)
          && int.TryParse(ddlPartner.SelectedValue, out pid) && pid > 0)
        {
            PartnerId = pid;
            LoadStatement();
        }
        else
        {
            PartnerId = 0;
            ShowBannerHidden();
        }
    }

    // ─────────────────────────────────────────────────────
    // Company Info
    // ─────────────────────────────────────────────────────
    private void LoadCompanyInfo()
    {
        string companyName = ConfigurationManager.AppSettings["CompanyName"];
        if (string.IsNullOrEmpty(companyName)) companyName = "کارخانه تولید و بسته‌بندی مواد غذایی";
        litCompanyName.Text = HttpUtility.HtmlEncode(companyName);

        StringBuilder meta = new StringBuilder();
        string tagline = ConfigurationManager.AppSettings["CompanyTagline"];
        string address = ConfigurationManager.AppSettings["CompanyAddress"];
        string phone = ConfigurationManager.AppSettings["CompanyPhone"];

        if (!string.IsNullOrEmpty(tagline)) meta.Append(HttpUtility.HtmlEncode(tagline));
        if (!string.IsNullOrEmpty(address)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("آدرس: " + HttpUtility.HtmlEncode(address)); }
        if (!string.IsNullOrEmpty(phone)) { if (meta.Length > 0) meta.Append(" — "); meta.Append("تلفن: " + HttpUtility.HtmlEncode(phone)); }
        litCompanyMeta.Text = meta.ToString();

        string logoUrl = ConfigurationManager.AppSettings["CompanyLogoUrl"];
        if (!string.IsNullOrEmpty(logoUrl))
        {
            try
            {
                string resolved = ResolveUrl(logoUrl);
                if (System.IO.File.Exists(Server.MapPath(logoUrl)))
                {
                    imgPrintLogo.ImageUrl = resolved;
                    imgPrintLogo.Visible = true;
                }
            }
            catch { }
        }

        litPrintDate.Text = HttpUtility.HtmlEncode(
            ToShamsiDateString(DateTime.Now) + " — " +
            DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    private void ShowBannerHidden()
    {
        pnlPartnerBanner.Visible = false;
        litPrintPeriod.Text = "برای مشاهده، ابتدا شریک را انتخاب کنید";
        rptLedger.DataSource = null;
        rptLedger.DataBind();
        pnlLedgerEmpty.Visible = true;
        litSumEarned.Text = "۰";
        litSumDrawings.Text = "۰";
        litSumPaid.Text = "۰";
        litBalance.Text = "۰";
    }

    // ─────────────────────────────────────────────────────
    // Main Load
    // ─────────────────────────────────────────────────────
    private void LoadStatement()
    {
        if (PartnerId <= 0)
        {
            ShowBannerHidden();
            return;
        }

        DateTime? from = ParsePersianDate(txtFrom.Text.Trim());
        DateTime? to = ParsePersianDate(txtTo.Text.Trim());
        if (!from.HasValue) from = GetCurrentPersianYearStart();
        if (!to.HasValue) to = DateTime.Now;

        DateTime fromDt = from.Value.Date;
        DateTime toDt = to.Value.Date.AddDays(1).AddSeconds(-1);

        try
        {
            // 1. Load partner info
            LoadPartnerBanner(PartnerId);

            // 2. Load summary (all-time)
            LoadSummary(PartnerId);

            // 3. Load ledger
            LoadLedger(PartnerId, fromDt, toDt);

            // Print period text
            litPrintPeriod.Text = "شریک: " +
                HttpUtility.HtmlEncode(ddlPartner.SelectedItem.Text) +
                " — از " + HttpUtility.HtmlEncode(ToShamsiDateString(fromDt)) +
                " تا " + HttpUtility.HtmlEncode(ToShamsiDateString(toDt));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری صورت‌حساب: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Partner Banner
    // ─────────────────────────────────────────────────────
    private void LoadPartnerBanner(int partnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT p.PartnerId, p.FullName, p.Mobile, p.NationalId, p.IsActive, " +
                    "  ISNULL((SELECT TOP 1 h.SharePercent FROM PartnerShareHistory h " +
                    "          WHERE h.PartnerId = p.PartnerId AND h.EffectiveTo IS NULL " +
                    "          ORDER BY h.EffectiveFrom DESC), 0) AS CurrentShare " +
                    "FROM Partners p " +
                    "WHERE p.PartnerId = @Id AND p.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            pnlPartnerBanner.Visible = true;

                            string name = Convert.ToString(dr["FullName"]);
                            bool active = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            decimal share = dr["CurrentShare"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CurrentShare"]);

                            litPartnerName.Text = HttpUtility.HtmlEncode(name);

                            litPartnerShare.Text = share > 0m
                                ? "<span class=\"badge bg-brand-100 text-brand-700 font-mono font-bold\">" +
                                  FormatPercentValue(share) + "٪ سهام</span>"
                                : "<span class=\"badge bg-surface-muted text-ink-500\">بدون سهم فعال</span>";

                            litPartnerStatus.Text = active
                                ? "<span class=\"badge bg-emerald-50 text-emerald-700\">فعال</span>"
                                : "<span class=\"badge bg-warning-50 text-warning-700\">غیرفعال</span>";

                            string mobile = dr["Mobile"] == DBNull.Value ? "" : Convert.ToString(dr["Mobile"]).Trim();
                            litPartnerContact.Text = mobile.Length > 0
                                ? "<span class=\"badge bg-surface-muted text-ink-600\" dir=\"ltr\" style=\"font-family:monospace;\">" +
                                  HttpUtility.HtmlEncode(mobile) + "</span>"
                                : "";

                            litCurrentSharePercent.Text = share > 0m
                                ? HttpUtility.HtmlEncode(FormatPercentValue(share) + "٪")
                                : "—";
                        }
                        else
                        {
                            pnlPartnerBanner.Visible = false;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────
    // Summary (all-time)
    // ─────────────────────────────────────────────────────
    private void LoadSummary(int partnerId)
    {
        try
        {
            decimal sumEarned = 0m;
            decimal sumDrawings = 0m;   // status 1, 2, 3
            decimal sumPaid = 0m;        // real cash payments
            int earnCount = 0;
            int drawCount = 0;
            int payCount = 0;

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // 1) Total earned from distributions
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*), ISNULL(SUM(ShareAmount), 0) " +
                    "FROM ProfitDistributionItems WHERE PartnerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            earnCount = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]);
                            sumEarned = dr[1] == DBNull.Value ? 0m : Convert.ToDecimal(dr[1]);
                        }
                    }
                }

                // 2) Drawings (all non-cancelled)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*), ISNULL(SUM(Amount), 0) " +
                    "FROM PartnerDrawings WHERE PartnerId = @Id AND Status IN (1, 2, 3)", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            drawCount = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]);
                            sumDrawings = dr[1] == DBNull.Value ? 0m : Convert.ToDecimal(dr[1]);
                        }
                    }
                }

                // 3) Real cash payments (PaidAmount - drawings applied)
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(paid), 0), COUNT(*) FROM ( " +
                    "  SELECT (i.PaidAmount - ISNULL(( " +
                    "     SELECT SUM(dr.Amount) FROM PartnerDrawings dr " +
                    "     WHERE dr.AppliedItemId = i.ItemId AND dr.Status = 2), 0) " +
                    "  ) AS paid " +
                    "  FROM ProfitDistributionItems i WHERE i.PartnerId = @Id " +
                    ") x WHERE paid > 0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            payCount = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]);
                            sumPaid = dr[0] == DBNull.Value ? 0m : Convert.ToDecimal(dr[0]);
                        }
                    }
                }

                // Balance = earned - (drawings + real payments)
                decimal balance = sumEarned - (sumDrawings + sumPaid);

                litSumEarned.Text = HttpUtility.HtmlEncode(FormatMoneyValue(sumEarned));
                litSumDrawings.Text = HttpUtility.HtmlEncode(FormatMoneyValue(sumDrawings));
                litSumPaid.Text = HttpUtility.HtmlEncode(FormatMoneyValue(sumPaid));
                litBalance.Text = HttpUtility.HtmlEncode(FormatMoneyValue(balance));

                litSumEarnedHint.Text = earnCount > 0
                    ? "از " + earnCount.ToString("N0") + " تقسیم"
                    : "بدون سابقه";
                litSumDrawingsHint.Text = drawCount > 0
                    ? "در " + drawCount.ToString("N0") + " نوبت"
                    : "بدون برداشت";
                litSumPaidHint.Text = payCount > 0
                    ? "برای " + payCount.ToString("N0") + " قلم"
                    : "بدون پرداخت";

                // Color the balance card
                if (balance > 0)
                {
                    cardBalance.Attributes["class"] =
                        "rounded-card border-2 border-emerald-300 bg-emerald-50/40 p-5 shadow-card print-card";
                    pBalance.Attributes["class"] = "font-mono text-[18px] font-black text-emerald-700";
                    litBalanceHint.Text = "شرکت بدهکار به شریک";
                }
                else if (balance < 0)
                {
                    cardBalance.Attributes["class"] =
                        "rounded-card border-2 border-danger-300 bg-danger-50/40 p-5 shadow-card print-card";
                    pBalance.Attributes["class"] = "font-mono text-[18px] font-black text-danger-600";
                    litBalanceHint.Text = "شریک بدهکار به شرکت";
                }
                else
                {
                    cardBalance.Attributes["class"] =
                        "rounded-card border-2 border-surface-line bg-surface-card p-5 shadow-card print-card";
                    pBalance.Attributes["class"] = "font-mono text-[18px] font-black text-emerald-700";
                    litBalanceHint.Text = "کاملاً تسویه";
                }

                litClosingBalance.InnerHtml = HttpUtility.HtmlEncode(FormatMoneyValue(balance));
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری خلاصه: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Ledger
    // ─────────────────────────────────────────────────────
    private void LoadLedger(int partnerId, DateTime fromDt, DateTime toDt)
    {
        // ═══ متغیرهای بیرونی (قبل از using تعریف می‌شوند) ═══
        decimal openingEarned = 0m;
        decimal openingDrawings = 0m;
        decimal openingPaid = 0m;
        decimal openingBalance = 0m;

        DataTable ledger = new DataTable();
        ledger.Columns.Add("EventDate", typeof(string));
        ledger.Columns.Add("EventDateRaw", typeof(DateTime));
        ledger.Columns.Add("SortKey", typeof(int));
        ledger.Columns.Add("Description", typeof(string));
        ledger.Columns.Add("Reference", typeof(string));
        ledger.Columns.Add("Debit", typeof(decimal));
        ledger.Columns.Add("Credit", typeof(decimal));
        ledger.Columns.Add("RowCss", typeof(string));
        ledger.Columns.Add("IconHtml", typeof(string));
        ledger.Columns.Add("SubHintHtml", typeof(string));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ═══ 1) Opening balance (before fromDt) ═══
                // Earned before
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(i.ShareAmount), 0) " +
                    "FROM ProfitDistributionItems i " +
                    "INNER JOIN ProfitDistributions d ON d.DistributionId = i.DistributionId " +
                    "WHERE i.PartnerId = @Id AND d.PeriodTo < @From", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    cmd.Parameters.AddWithValue("@From", fromDt);
                    object o = cmd.ExecuteScalar();
                    openingEarned = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Drawings before
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) " +
                    "FROM PartnerDrawings " +
                    "WHERE PartnerId = @Id AND Status IN (1, 2, 3) AND DrawingDate < @From", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    cmd.Parameters.AddWithValue("@From", fromDt);
                    object o = cmd.ExecuteScalar();
                    openingDrawings = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                // Real payments before
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(paid), 0) FROM ( " +
                    "  SELECT (i.PaidAmount - ISNULL(( " +
                    "     SELECT SUM(dr.Amount) FROM PartnerDrawings dr " +
                    "     WHERE dr.AppliedItemId = i.ItemId AND dr.Status = 2), 0) " +
                    "  ) AS paid, i.PaidAt " +
                    "  FROM ProfitDistributionItems i " +
                    "  WHERE i.PartnerId = @Id " +
                    ") x WHERE paid > 0 AND PaidAt < @From", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    cmd.Parameters.AddWithValue("@From", fromDt);
                    object o = cmd.ExecuteScalar();
                    openingPaid = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                openingBalance = openingEarned - (openingDrawings + openingPaid);

                // ═══ 2) Drawings in range ═══
                DataTable dtDrawings = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT DrawingId, DrawingDate, Amount, Status, Reason, " +
                    "       PaymentMethod, ReferenceNumber, ReturnedAt " +
                    "FROM PartnerDrawings " +
                    "WHERE PartnerId = @Id AND Status IN (1, 2, 3) " +
                    "  AND DrawingDate >= @From AND DrawingDate <= @To", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    cmd.Parameters.AddWithValue("@From", fromDt);
                    cmd.Parameters.AddWithValue("@To", toDt);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dtDrawings);
                }

                // ═══ 3) Distributions in range ═══
                DataTable dtDists = new DataTable();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT i.ItemId, i.ShareAmount, i.PaidAmount, i.PaidAt, i.PaymentMethod, " +
                    "       i.ReferenceNumber, d.DistributionNumber, d.PeriodTo, d.FinalizedAt, " +
                    "       ISNULL((SELECT SUM(dr.Amount) FROM PartnerDrawings dr " +
                    "               WHERE dr.AppliedItemId = i.ItemId AND dr.Status = 2), 0) AS DrawingsApplied " +
                    "FROM ProfitDistributionItems i " +
                    "INNER JOIN ProfitDistributions d ON d.DistributionId = i.DistributionId " +
                    "WHERE i.PartnerId = @Id " +
                    "  AND d.PeriodTo >= @From AND d.PeriodTo <= @To", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    cmd.Parameters.AddWithValue("@From", fromDt);
                    cmd.Parameters.AddWithValue("@To", toDt);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dtDists);
                }

                // ═══ 4) Build ledger events ═══
                // Drawings (debit at DrawingDate)
                foreach (DataRow r in dtDrawings.Rows)
                {
                    int status = Convert.ToInt32(r["Status"]);

                    DateTime drawingDate = Convert.ToDateTime(r["DrawingDate"]);
                    decimal amount = Convert.ToDecimal(r["Amount"]);
                    string reason = r["Reason"] == DBNull.Value ? "" : Convert.ToString(r["Reason"]).Trim();
                    string method = r["PaymentMethod"] == DBNull.Value ? "" : Convert.ToString(r["PaymentMethod"]).Trim();
                    string reference = "DRW-" + Convert.ToInt32(r["DrawingId"]).ToString("D4");

                    DataRow eventRow = ledger.NewRow();
                    eventRow["EventDateRaw"] = drawingDate;
                    eventRow["EventDate"] = ToShamsiDateString(drawingDate);
                    eventRow["SortKey"] = 10;
                    eventRow["Description"] = "برداشت علی‌الحساب" +
                        (reason.Length > 0 ? " — " + reason : "");
                    eventRow["Reference"] = reference;
                    eventRow["Debit"] = amount;
                    eventRow["Credit"] = 0m;
                    eventRow["RowCss"] = "";
                    eventRow["IconHtml"] =
                        "<span class=\"grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-amber-50 text-amber-600\">" +
                        "<i class=\"h-4 w-4\" data-lucide=\"hand-coins\"></i></span>";

                    StringBuilder sub = new StringBuilder();
                    if (method.Length > 0)
                        sub.Append("<p class=\"mt-0.5 text-[10.5px] text-ink-400\">روش: " +
                            HttpUtility.HtmlEncode(method) + "</p>");
                    eventRow["SubHintHtml"] = sub.ToString();

                    ledger.Rows.Add(eventRow);

                    // If returned, add credit
                    if (status == 3 && r["ReturnedAt"] != DBNull.Value)
                    {
                        DateTime retAt = Convert.ToDateTime(r["ReturnedAt"]);
                        DataRow retRow = ledger.NewRow();
                        retRow["EventDateRaw"] = retAt;
                        retRow["EventDate"] = ToShamsiDateString(retAt);
                        retRow["SortKey"] = 11;
                        retRow["Description"] = "بازگشت برداشت توسط شریک — " + reference;
                        retRow["Reference"] = reference;
                        retRow["Debit"] = 0m;
                        retRow["Credit"] = amount;
                        retRow["RowCss"] = "";
                        retRow["IconHtml"] =
                            "<span class=\"grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-emerald-50 text-emerald-600\">" +
                            "<i class=\"h-4 w-4\" data-lucide=\"corner-down-right\"></i></span>";
                        retRow["SubHintHtml"] = "";
                        ledger.Rows.Add(retRow);
                    }
                }

                // Distributions (credit at PeriodTo, real payment debit at PaidAt)
                foreach (DataRow r in dtDists.Rows)
                {
                    DateTime periodTo = Convert.ToDateTime(r["PeriodTo"]);
                    string distNumber = Convert.ToString(r["DistributionNumber"]);
                    decimal shareAmount = Convert.ToDecimal(r["ShareAmount"]);
                    decimal paidAmount = Convert.ToDecimal(r["PaidAmount"]);
                    decimal drawingsApplied = Convert.ToDecimal(r["DrawingsApplied"]);
                    decimal realPaid = paidAmount - drawingsApplied;
                    if (realPaid < 0m) realPaid = 0m;

                    // ═══ Credit: share earned ═══
                    DataRow creditRow = ledger.NewRow();
                    creditRow["EventDateRaw"] = periodTo;
                    creditRow["EventDate"] = ToShamsiDateString(periodTo);
                    creditRow["SortKey"] = 20;
                    creditRow["Description"] = "سهم از تقسیم سود — " + distNumber;
                    creditRow["Reference"] = distNumber;
                    creditRow["Debit"] = 0m;
                    creditRow["Credit"] = shareAmount;
                    creditRow["RowCss"] = "";
                    creditRow["IconHtml"] =
                        "<span class=\"grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-emerald-50 text-emerald-600\">" +
                        "<i class=\"h-4 w-4\" data-lucide=\"trending-up\"></i></span>";

                    string hint = "";
                    if (drawingsApplied > 0m)
                        hint = "<p class=\"mt-0.5 text-[10.5px] text-amber-600\">" +
                            "برداشت اعمال‌شده: <span class=\"font-mono\">" +
                            HttpUtility.HtmlEncode(FormatMoneyValue(drawingsApplied)) +
                            "</span> تومان</p>";
                    creditRow["SubHintHtml"] = hint;
                    ledger.Rows.Add(creditRow);

                    // ═══ Debit: real cash payment (if any) ═══
                    if (realPaid > 0m && r["PaidAt"] != DBNull.Value)
                    {
                        DateTime paidAt = Convert.ToDateTime(r["PaidAt"]);
                        string method = r["PaymentMethod"] == DBNull.Value
                            ? "" : Convert.ToString(r["PaymentMethod"]).Trim();
                        string refNum = r["ReferenceNumber"] == DBNull.Value
                            ? "" : Convert.ToString(r["ReferenceNumber"]).Trim();

                        DataRow payRow = ledger.NewRow();
                        payRow["EventDateRaw"] = paidAt;
                        payRow["EventDate"] = ToShamsiDateString(paidAt);
                        payRow["SortKey"] = 30;
                        payRow["Description"] = "پرداخت نقدی از تقسیم " + distNumber;
                        payRow["Reference"] = refNum.Length > 0 ? refNum : distNumber;
                        payRow["Debit"] = realPaid;
                        payRow["Credit"] = 0m;
                        payRow["RowCss"] = "";
                        payRow["IconHtml"] =
                            "<span class=\"grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand-600\">" +
                            "<i class=\"h-4 w-4\" data-lucide=\"banknote\"></i></span>";

                        string sub = "";
                        if (method.Length > 0)
                            sub = "<p class=\"mt-0.5 text-[10.5px] text-ink-400\">روش: " +
                                HttpUtility.HtmlEncode(method) + "</p>";
                        payRow["SubHintHtml"] = sub;
                        ledger.Rows.Add(payRow);
                    }
                }
            } // ← اینجا using بسته می‌شود ولی openingBalance همچنان معتبر است

            // ═══ Sort and compute running balance ═══
            DataView dv = ledger.DefaultView;
            dv.Sort = "EventDateRaw ASC, SortKey ASC";
            DataTable sorted = dv.ToTable();

            // Opening row
            DataTable finalLedger = new DataTable();
            finalLedger.Columns.Add("EventDate", typeof(string));
            finalLedger.Columns.Add("Description", typeof(string));
            finalLedger.Columns.Add("Reference", typeof(string));
            finalLedger.Columns.Add("DebitHtml", typeof(string));
            finalLedger.Columns.Add("CreditHtml", typeof(string));
            finalLedger.Columns.Add("BalanceHtml", typeof(string));
            finalLedger.Columns.Add("BalanceCss", typeof(string));
            finalLedger.Columns.Add("RowCss", typeof(string));
            finalLedger.Columns.Add("IconHtml", typeof(string));
            finalLedger.Columns.Add("SubHintHtml", typeof(string));

            bool hasAnyEvent = sorted.Rows.Count > 0;

            // Add opening row if there's an opening balance
            if (Math.Abs(openingBalance) > 0.01m)
            {
                DataRow openRow = finalLedger.NewRow();
                openRow["EventDate"] = ToShamsiDateString(fromDt.AddDays(-1));
                openRow["Description"] = "مانده از قبل از این بازه";
                openRow["Reference"] = "—";
                openRow["DebitHtml"] = "—";
                openRow["CreditHtml"] = "—";
                openRow["BalanceHtml"] = HttpUtility.HtmlEncode(FormatMoneyValue(openingBalance)) + " تومان";
                openRow["BalanceCss"] = openingBalance > 0 ? "balance-pos" :
                                       (openingBalance < 0 ? "balance-neg" : "balance-zero");
                openRow["RowCss"] = "opening-row";
                openRow["IconHtml"] = "";
                openRow["SubHintHtml"] = "";
                finalLedger.Rows.Add(openRow);
            }

            decimal running = openingBalance;

            foreach (DataRow r in sorted.Rows)
            {
                decimal debit = Convert.ToDecimal(r["Debit"]);
                decimal credit = Convert.ToDecimal(r["Credit"]);
                running += credit - debit;

                DataRow lr = finalLedger.NewRow();
                lr["EventDate"] = r["EventDate"];
                lr["Description"] = r["Description"];
                lr["Reference"] = r["Reference"];
                lr["DebitHtml"] = debit > 0m
                    ? HttpUtility.HtmlEncode(FormatMoneyValue(debit))
                    : "—";
                lr["CreditHtml"] = credit > 0m
                    ? HttpUtility.HtmlEncode(FormatMoneyValue(credit))
                    : "—";
                lr["BalanceHtml"] = HttpUtility.HtmlEncode(FormatMoneyValue(running)) + " تومان";
                lr["BalanceCss"] = running > 0m ? "balance-pos" :
                                  (running < 0m ? "balance-neg" : "balance-zero");
                lr["RowCss"] = Convert.ToString(r["RowCss"]);
                lr["IconHtml"] = Convert.ToString(r["IconHtml"]);
                lr["SubHintHtml"] = Convert.ToString(r["SubHintHtml"]);
                finalLedger.Rows.Add(lr);
            }

            // Add closing row
            if (hasAnyEvent)
            {
                DataRow closeRow = finalLedger.NewRow();
                closeRow["EventDate"] = "—";
                closeRow["Description"] = "مانده نهایی در پایان بازه";
                closeRow["Reference"] = "—";
                closeRow["DebitHtml"] = "—";
                closeRow["CreditHtml"] = "—";
                closeRow["BalanceHtml"] = HttpUtility.HtmlEncode(FormatMoneyValue(running)) + " تومان";
                closeRow["BalanceCss"] = running > 0m ? "balance-pos" :
                                        (running < 0m ? "balance-neg" : "balance-zero");
                closeRow["RowCss"] = "closing-row";
                closeRow["IconHtml"] = "";
                closeRow["SubHintHtml"] = "";
                finalLedger.Rows.Add(closeRow);
            }

            if (finalLedger.Rows.Count == 0)
            {
                pnlLedgerEmpty.Visible = true;
                rptLedger.Visible = false;
                rptLedger.DataSource = null;
                rptLedger.DataBind();
            }
            else
            {
                pnlLedgerEmpty.Visible = false;
                rptLedger.Visible = true;
                rptLedger.DataSource = finalLedger;
                rptLedger.DataBind();
            }

            litLedgerCount.Text = HttpUtility.HtmlEncode(
                string.Format("{0} ردیف در دفتر", sorted.Rows.Count.ToString("N0")));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ساخت دفتر: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Filters
    // ─────────────────────────────────────────────────────
    protected void btnApply_Click(object sender, EventArgs e)
    {
        LoadStatement();
    }

    protected void QuickRange_Click(object sender, EventArgs e)
    {
        LinkButton btn = sender as LinkButton;
        if (btn == null) return;

        string arg = btn.CommandArgument;
        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        DateTime from;
        DateTime to = now;

        if (arg == "year")
        {
            from = pc.ToDateTime(pc.GetYear(now), 1, 1, 0, 0, 0, 0);
        }
        else if (arg == "6m")
        {
            int m = pc.GetMonth(now);
            int startMonth = m - 5;
            int year = pc.GetYear(now);
            while (startMonth < 1) { startMonth += 12; year--; }
            from = pc.ToDateTime(year, startMonth, 1, 0, 0, 0, 0);
        }
        else if (arg == "30d")
        {
            from = now.Date.AddDays(-29);
        }
        else if (arg == "all")
        {
            from = pc.ToDateTime(1300, 1, 1, 0, 0, 0, 0);
        }
        else return;

        txtFrom.Text = ToShamsiDateString(from);
        txtTo.Text = ToShamsiDateString(to);
        LoadStatement();
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        bool neg = d < 0m;
        decimal abs = Math.Abs(d);
        string s = abs.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return neg ? "(" + s + ")" : s;
    }

    private string FormatPercentValue(decimal d)
    {
        if (d == 0m) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture);
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
    }

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
    // Message
    // ─────────────────────────────────────────────────────
    private void ShowMessage(string message)
    {
        if (message == null) message = "";
        string safe = message
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", "\\n");

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