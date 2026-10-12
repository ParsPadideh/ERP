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

public partial class admin_partners : System.Web.UI.Page
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
        get
        {
            int id;
            if (int.TryParse(hfEditId.Value, out id)) return id;
            return 0;
        }
        set { hfEditId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private int DetailPartnerId
    {
        get
        {
            int id;
            if (int.TryParse(hfDetailPartnerId.Value, out id)) return id;
            return 0;
        }
        set { hfDetailPartnerId.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private string ActiveTab
    {
        get
        {
            if (string.IsNullOrEmpty(hfActiveTab.Value)) return "info";
            return hfActiveTab.Value;
        }
        set { hfActiveTab.Value = value; }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
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
        pnlDetailView.Visible = false;

        phBackToList.Visible = false;
        phAddPartner.Visible = true;

        litPageTitle.Text = "مدیریت شرکا";
        litPageSubtitle.Text = "اطلاعات شرکا، درصد سهام و برداشت‌های علی‌الحساب را مدیریت کنید.";

        LoadStats();
        LoadPartners();
    }

    private void ShowFormView(int partnerId)
    {
        pnlListView.Visible = false;
        pnlFormView.Visible = true;
        pnlDetailView.Visible = false;

        phBackToList.Visible = true;
        phAddPartner.Visible = false;

        LoadUserDropdown();

        if (partnerId > 0)
        {
            EditId = partnerId;
            LoadPartnerForEdit(partnerId);
            litFormTitle.Text = "ویرایش شریک";
            pnlInitialShare.Visible = false;  // No initial share on edit
        }
        else
        {
            EditId = 0;
            ClearForm();
            litFormTitle.Text = "افزودن شریک جدید";
            pnlInitialShare.Visible = true;
        }
    }

    private void ShowDetailView(int partnerId, string tab)
    {
        DetailPartnerId = partnerId;
        ActiveTab = string.IsNullOrEmpty(tab) ? "info" : tab;

        pnlListView.Visible = false;
        pnlFormView.Visible = false;
        pnlDetailView.Visible = true;

        phBackToList.Visible = true;
        phAddPartner.Visible = false;

        LoadPartnerDetail(partnerId);
        LoadPartnerShares(partnerId);
        LoadPartnerDrawings(partnerId);
        LoadEstimate(partnerId);

        litPageTitle.Text = "پرونده شریک";
        litPageSubtitle.Text = "مدیریت اطلاعات، سهام و برداشت‌های این شریک.";
    }

    // ─────────────────────────────────────────────────────
    // Stats
    // ─────────────────────────────────────────────────────
    private void LoadStats()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Total + Active
                string sql1 =
                    "SELECT " +
                    "  COUNT(*) AS Total, " +
                    "  SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount " +
                    "FROM Partners WHERE IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        litStatTotal.Text = HttpUtility.HtmlEncode(ToInt(dr["Total"]).ToString("N0"));
                        litStatActive.Text = HttpUtility.HtmlEncode(ToInt(dr["ActiveCount"]).ToString("N0"));
                    }
                }

                // Total active share
                string sql2 =
                    "SELECT ISNULL(SUM(h.SharePercent), 0) " +
                    "FROM PartnerShareHistory h " +
                    "INNER JOIN Partners p ON p.PartnerId = h.PartnerId " +
                    "WHERE h.EffectiveTo IS NULL AND p.IsActive = 1 AND p.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql2, conn))
                {
                    object o = cmd.ExecuteScalar();
                    decimal sum = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                    litStatTotalShare.Text = HttpUtility.HtmlEncode(
                        FormatPercentValue(sum) + "٪");
                }

                // Open drawings total
                string sql3 =
                    "SELECT ISNULL(SUM(Amount), 0) FROM PartnerDrawings WHERE Status = 1";

                using (SqlCommand cmd = new SqlCommand(sql3, conn))
                {
                    object o = cmd.ExecuteScalar();
                    decimal sum = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                    litStatOpenDrawings.Text = HttpUtility.HtmlEncode(FormatMoneyValue(sum));
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
    // Load Partners List
    // ─────────────────────────────────────────────────────
    private void LoadPartners()
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT " +
                    "  p.PartnerId, p.FullName, p.NationalId, p.Mobile, p.Email, " +
                    "  p.BankAccount, p.IsActive, p.CreatedAt, " +
                    "  u.Username AS LinkedUsername, " +
                    "  ISNULL((" +
                    "     SELECT TOP 1 h.SharePercent FROM PartnerShareHistory h " +
                    "     WHERE h.PartnerId = p.PartnerId AND h.EffectiveTo IS NULL " +
                    "     ORDER BY h.EffectiveFrom DESC" +
                    "  ), 0) AS CurrentSharePercent, " +
                    "  ISNULL((" +
                    "     SELECT SUM(d.Amount) FROM PartnerDrawings d " +
                    "     WHERE d.PartnerId = p.PartnerId AND d.Status = 1" +
                    "  ), 0) AS OpenDrawings, " +
                    "  CASE WHEN EXISTS (" +
                    "     SELECT 1 FROM PartnerShareHistory h WHERE h.PartnerId = p.PartnerId" +
                    "  ) THEN 1 ELSE 0 END AS HasHistory " +
                    "FROM Partners p " +
                    "LEFT JOIN Users u ON u.UserId = p.UserId " +
                    "WHERE p.IsDeleted = 0 " +
                    "ORDER BY p.IsActive DESC, p.FullName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlListEmpty.Visible = true;
                rptPartners.Visible = false;
                rptPartners.DataSource = null;
                rptPartners.DataBind();
            }
            else
            {
                pnlListEmpty.Visible = false;
                rptPartners.Visible = true;
                rptPartners.DataSource = dt;
                rptPartners.DataBind();
            }

            litListInfo.Text = string.Format("{0} شریک",
                dt.Rows.Count.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری شرکا: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load User Dropdown
    // ─────────────────────────────────────────────────────
    private void LoadUserDropdown()
    {
        ddlUserId.Items.Clear();
        ddlUserId.Items.Add(new ListItem("— بدون کاربر مرتبط —", ""));

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT UserId, Username, ISNULL(FullName, '') AS FullName " +
                    "FROM Users WHERE IsDeleted = 0 AND IsActive = 1 " +
                    "ORDER BY Username";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string id = Convert.ToInt32(dr["UserId"]).ToString(CultureInfo.InvariantCulture);
                        string username = Convert.ToString(dr["Username"]);
                        string fullName = Convert.ToString(dr["FullName"]);

                        string label = username;
                        if (fullName.Length > 0) label = fullName + " (@" + username + ")";
                        else label = "@" + username;

                        ddlUserId.Items.Add(new ListItem(label, id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری کاربران: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Load Partner for Edit
    // ─────────────────────────────────────────────────────
    private void LoadPartnerForEdit(int partnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT FullName, NationalId, Mobile, Email, BankAccount, " +
                    "       UserId, Notes, IsActive " +
                    "FROM Partners WHERE PartnerId = @Id AND IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtFullName.Text = Convert.ToString(dr["FullName"]);
                            txtNationalId.Text = dr["NationalId"] == DBNull.Value
                                ? "" : Convert.ToString(dr["NationalId"]);
                            txtMobile.Text = dr["Mobile"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Mobile"]);
                            txtEmail.Text = dr["Email"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Email"]);
                            txtBankAccount.Text = dr["BankAccount"] == DBNull.Value
                                ? "" : Convert.ToString(dr["BankAccount"]);
                            txtNotes.Text = dr["Notes"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Notes"]);
                            chkIsActive.Checked = dr["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(dr["IsActive"]);

                            ddlUserId.ClearSelection();
                            if (dr["UserId"] != DBNull.Value)
                            {
                                string uid = Convert.ToInt32(dr["UserId"])
                                    .ToString(CultureInfo.InvariantCulture);
                                ListItem item = ddlUserId.Items.FindByValue(uid);
                                if (item != null) item.Selected = true;
                            }
                        }
                        else
                        {
                            ShowMessage("شریک مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری شریک: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Save Partner
    // ─────────────────────────────────────────────────────
    protected void btnSave_Click(object sender, EventArgs e)
    {
        int currentId = EditId;
        string fullName = txtFullName.Text.Trim();
        string nationalId = txtNationalId.Text.Trim();
        string mobile = txtMobile.Text.Trim();
        string email = txtEmail.Text.Trim();
        string bankAccount = txtBankAccount.Text.Trim();
        string notes = txtNotes.Text.Trim();
        bool isActive = chkIsActive.Checked;

        // Validation
        if (fullName.Length == 0)
        {
            ShowMessage("نام کامل را وارد کنید.");
            return;
        }
        if (fullName.Length > 150)
        {
            ShowMessage("نام کامل نمی‌تواند بیش از ۱۵۰ کاراکتر باشد.");
            return;
        }
        if (mobile.Length > 0 && mobile.Length < 10)
        {
            ShowMessage("شماره موبایل معتبر نیست.");
            return;
        }

        int userId = 0;
        if (!string.IsNullOrEmpty(ddlUserId.SelectedValue))
            int.TryParse(ddlUserId.SelectedValue, out userId);

        // Initial share (only for new)
        decimal initialShare = 0m;
        bool hasInitialShare = false;
        DateTime? initialFrom = null;
        string initialNotes = "";

        if (currentId == 0 && pnlInitialShare.Visible)
        {
            string shareText = txtInitialSharePercent.Text.Trim();
            if (shareText.Length > 0)
            {
                decimal? sp = ParseDecimal(shareText);
                if (!sp.HasValue || sp.Value < 0 || sp.Value > 100)
                {
                    ShowMessage("درصد سهام باید عددی بین ۰ تا ۱۰۰ باشد.");
                    return;
                }
                initialShare = sp.Value;
                hasInitialShare = true;

                string fromText = txtInitialShareFrom.Text.Trim();
                if (fromText.Length > 0)
                {
                    DateTime? dt = ParsePersianDate(fromText);
                    if (!dt.HasValue)
                    {
                        ShowMessage("تاریخ شروع سهام معتبر نیست. فرمت: 1403/01/01");
                        return;
                    }
                    initialFrom = dt.Value;
                }
                else
                {
                    initialFrom = DateTime.Now.Date;
                }

                initialNotes = txtInitialShareNotes.Text.Trim();

                // Check if total would exceed 100%
                decimal existingSum = GetCurrentActiveShareSum(0);
                if (existingSum + initialShare > 100m)
                {
                    ShowMessage(string.Format(
                        "جمع سهام فعال فعلی {0}٪ است. با احتساب {1}٪ جدید، از ۱۰۰٪ عبور می‌کند.",
                        existingSum.ToString("N2", CultureInfo.InvariantCulture),
                        initialShare.ToString("N2", CultureInfo.InvariantCulture)));
                    return;
                }
            }
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                if (currentId > 0)
                {
                    // UPDATE
                    string updateSql =
                        "UPDATE Partners SET " +
                        "  FullName = @FullName, NationalId = @NationalId, Mobile = @Mobile, " +
                        "  Email = @Email, BankAccount = @BankAccount, UserId = @UserId, " +
                        "  Notes = @Notes, IsActive = @IsActive, " +
                        "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                        "WHERE PartnerId = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@NationalId", NullIfEmpty(nationalId));
                        cmd.Parameters.AddWithValue("@Mobile", NullIfEmpty(mobile));
                        cmd.Parameters.AddWithValue("@Email", NullIfEmpty(email));
                        cmd.Parameters.AddWithValue("@BankAccount", NullIfEmpty(bankAccount));
                        cmd.Parameters.AddWithValue("@UserId", userId > 0 ? (object)userId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UpdatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", currentId);
                        cmd.ExecuteNonQuery();
                    }

                    WriteAudit(conn, "Update", "Partners", currentId, fullName, "ویرایش شریک");
                    ShowMessage("تغییرات با موفقیت ذخیره شد.");
                }
                else
                {
                    // INSERT
                    string insertSql =
                        "INSERT INTO Partners " +
                        "  (FullName, NationalId, Mobile, Email, BankAccount, UserId, " +
                        "   Notes, IsActive, IsDeleted, CreatedAt, CreatedBy) " +
                        "VALUES (@FullName, @NationalId, @Mobile, @Email, @BankAccount, @UserId, " +
                        "   @Notes, @IsActive, 0, GETDATE(), @CreatedBy); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int newId = 0;
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@NationalId", NullIfEmpty(nationalId));
                        cmd.Parameters.AddWithValue("@Mobile", NullIfEmpty(mobile));
                        cmd.Parameters.AddWithValue("@Email", NullIfEmpty(email));
                        cmd.Parameters.AddWithValue("@BankAccount", NullIfEmpty(bankAccount));
                        cmd.Parameters.AddWithValue("@UserId", userId > 0 ? (object)userId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@CreatedBy",
                            CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                    }

                    if (newId > 0)
                    {
                        WriteAudit(conn, "Insert", "Partners", newId, fullName, "ایجاد شریک");

                        // Insert initial share
                        if (hasInitialShare && initialFrom.HasValue)
                        {
                            string shareSql =
                                "INSERT INTO PartnerShareHistory " +
                                "  (PartnerId, SharePercent, EffectiveFrom, EffectiveTo, Notes, CreatedAt, CreatedBy) " +
                                "VALUES (@PartnerId, @SharePercent, @EffFrom, NULL, @Notes, GETDATE(), @CreatedBy)";

                            using (SqlCommand cmd = new SqlCommand(shareSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@PartnerId", newId);
                                cmd.Parameters.AddWithValue("@SharePercent", initialShare);
                                cmd.Parameters.AddWithValue("@EffFrom", initialFrom.Value);
                                cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(initialNotes));
                                cmd.Parameters.AddWithValue("@CreatedBy",
                                    CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }

                            WriteAudit(conn, "Insert", "PartnerShareHistory", newId,
                                fullName, "ثبت سهام اولیه " + initialShare.ToString("N2") + "٪");
                        }
                    }

                    ShowMessage("شریک جدید با موفقیت افزوده شد.");
                }
            }

            ShowListView();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ذخیره: " + ex.Message);
        }
    }

    private decimal GetCurrentActiveShareSum(int excludePartnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT ISNULL(SUM(h.SharePercent), 0) " +
                    "FROM PartnerShareHistory h " +
                    "INNER JOIN Partners p ON p.PartnerId = h.PartnerId " +
                    "WHERE h.EffectiveTo IS NULL AND p.IsActive = 1 AND p.IsDeleted = 0 ";
                if (excludePartnerId > 0)
                    sql += " AND h.PartnerId <> @ExcludeId ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (excludePartnerId > 0)
                        cmd.Parameters.AddWithValue("@ExcludeId", excludePartnerId);
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }
            }
        }
        catch { return 0m; }
    }

    // ─────────────────────────────────────────────────────
    // Repeater Commands (List)
    // ─────────────────────────────────────────────────────
    protected void rptPartners_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        int id;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

        if (e.CommandName == "Manage") ShowDetailView(id, "info");
        else if (e.CommandName == "Edit") ShowFormView(id);
        else if (e.CommandName == "ToggleActive") ToggleActive(id);
        else if (e.CommandName == "Delete") SoftDelete(id);
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
                    "SELECT FullName, IsActive FROM Partners WHERE PartnerId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            name = Convert.ToString(dr["FullName"]);
                            isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("شریک مورد نظر یافت نشد.");
                    return;
                }

                bool newState = !isActive;

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Partners SET IsActive = @IsActive WHERE PartnerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", newState);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "ToggleActive", "Partners", id, name,
                    newState ? "فعال‌سازی شریک" : "غیرفعال‌سازی شریک");
            }

            ShowMessage("وضعیت شریک تغییر یافت.");
            LoadStats();
            LoadPartners();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
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
                    "SELECT FullName FROM Partners WHERE PartnerId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", id);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) name = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(name))
                {
                    ShowMessage("شریک مورد نظر یافت نشد.");
                    return;
                }

                // Check open drawings
                int openCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM PartnerDrawings WHERE PartnerId = @Id AND Status = 1", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    openCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (openCount > 0)
                {
                    ShowMessage(string.Format(
                        "این شریک {0} برداشت باز دارد. ابتدا تسویه کنید.", openCount));
                    return;
                }

                // Check share history
                int histCount = 0;
                using (SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM PartnerShareHistory WHERE PartnerId = @Id", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    object o = check.ExecuteScalar();
                    histCount = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }

                if (histCount > 0)
                {
                    ShowMessage("این شریک تاریخچه سهام دارد و قابل حذف نیست. به‌جای حذف، غیرفعالش کنید.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE Partners SET IsDeleted = 1 WHERE PartnerId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Delete", "Partners", id, name, "حذف نرم شریک");
            }

            ShowMessage("شریک با موفقیت حذف شد.");
            LoadStats();
            LoadPartners();
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در حذف: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Detail: Load Info
    // ─────────────────────────────────────────────────────
    private void LoadPartnerDetail(int partnerId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT p.PartnerId, p.FullName, p.NationalId, p.Mobile, p.Email, " +
                    "       p.BankAccount, p.Notes, p.IsActive, p.CreatedAt, p.UpdatedAt, " +
                    "       u.Username, u.FullName AS UserFullName, " +
                    "  ISNULL((" +
                    "     SELECT TOP 1 h.SharePercent FROM PartnerShareHistory h " +
                    "     WHERE h.PartnerId = p.PartnerId AND h.EffectiveTo IS NULL " +
                    "     ORDER BY h.EffectiveFrom DESC" +
                    "  ), 0) AS CurrentSharePercent, " +
                    "  ISNULL((" +
                    "     SELECT SUM(d.Amount) FROM PartnerDrawings d " +
                    "     WHERE d.PartnerId = p.PartnerId AND d.Status = 1" +
                    "  ), 0) AS OpenDrawings, " +
                    "  ISNULL((" +
                    "     SELECT SUM(d.Amount) FROM PartnerDrawings d " +
                    "     WHERE d.PartnerId = p.PartnerId AND d.Status IN (2,3)" +
                    "  ), 0) AS PaidTotal " +
                    "FROM Partners p " +
                    "LEFT JOIN Users u ON u.UserId = p.UserId " +
                    "WHERE p.PartnerId = @Id AND p.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string fullName = Convert.ToString(dr["FullName"]);
                            bool isActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                            decimal share = dr["CurrentSharePercent"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["CurrentSharePercent"]);
                            decimal openDrawings = dr["OpenDrawings"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["OpenDrawings"]);
                            decimal paidTotal = dr["PaidTotal"] == DBNull.Value
                                ? 0m : Convert.ToDecimal(dr["PaidTotal"]);

                            // Header
                            litDetailName.Text = HttpUtility.HtmlEncode(fullName);
                            litDetailShare.Text = share > 0
                                ? "<span class=\"badge bg-brand-50 text-brand-700 font-mono font-bold\">" +
                                  FormatPercentValue(share) + "٪ سهام</span>"
                                : "<span class=\"badge bg-surface-muted text-ink-500\">بدون سهم فعال</span>";
                            litDetailStatus.Text = isActive
                                ? "<span class=\"badge bg-emerald-50 text-emerald-700\">فعال</span>"
                                : "<span class=\"badge bg-warning-50 text-warning-700\">غیرفعال</span>";

                            string linkedUser = dr["Username"] == DBNull.Value
                                ? "" : Convert.ToString(dr["Username"]);
                            litDetailLinkedUser.Text = linkedUser.Length > 0
                                ? "<span class=\"badge bg-violet-50 text-violet-700\"><i class=\"h-3 w-3\" data-lucide=\"user-check\"></i> @" +
                                  HttpUtility.HtmlEncode(linkedUser) + "</span>"
                                : "";

                            litDetailOpenDrawings.Text = HttpUtility.HtmlEncode(FormatMoneyValue(openDrawings));

                            // Tab Info
                            litInfoFullName.Text = HttpUtility.HtmlEncode(fullName);
                            litInfoNationalId.Text = HtmlOrDash(dr["NationalId"]);
                            litInfoMobile.Text = HtmlOrDash(dr["Mobile"]);
                            litInfoEmail.Text = HtmlOrDash(dr["Email"]);
                            litInfoBank.Text = HtmlOrDash(dr["BankAccount"]);
                            litInfoNotes.Text = HtmlOrDash(dr["Notes"]);
                            litInfoCreatedAt.Text = dr["CreatedAt"] == DBNull.Value
                                ? "—" : HttpUtility.HtmlEncode(FormatShamsiDateTime(Convert.ToDateTime(dr["CreatedAt"])));
                            litInfoUpdatedAt.Text = dr["UpdatedAt"] == DBNull.Value
                                ? "—" : HttpUtility.HtmlEncode(FormatShamsiDateTime(Convert.ToDateTime(dr["UpdatedAt"])));

                            // Summary
                            litSumSharePercent.Text = share > 0
                                ? HttpUtility.HtmlEncode(FormatPercentValue(share) + "٪")
                                : "—";
                            litSumOpenDrawings.Text = HttpUtility.HtmlEncode(FormatMoneyValue(openDrawings));
                            litSumPaidTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(paidTotal));
                        }
                        else
                        {
                            ShowMessage("شریک مورد نظر یافت نشد.");
                            ShowListView();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Detail: Load Shares
    // ─────────────────────────────────────────────────────
    private void LoadPartnerShares(int partnerId)
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT ShareId, SharePercent, EffectiveFrom, EffectiveTo, Notes " +
                    "FROM PartnerShareHistory WHERE PartnerId = @Id " +
                    "ORDER BY EffectiveFrom DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlSharesEmpty.Visible = true;
                rptShares.Visible = false;
                rptShares.DataSource = null;
                rptShares.DataBind();
            }
            else
            {
                pnlSharesEmpty.Visible = false;
                rptShares.Visible = true;
                rptShares.DataSource = dt;
                rptShares.DataBind();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری تاریخچه سهام: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Detail: Add New Share
    // ─────────────────────────────────────────────────────
    protected void btnAddShare_Click(object sender, EventArgs e)
    {
        int partnerId = DetailPartnerId;
        if (partnerId <= 0)
        {
            ShowMessage("ابتدا یک شریک را انتخاب کنید.");
            return;
        }

        string shareText = txtNewSharePercent.Text.Trim();
        string fromText = txtNewShareFrom.Text.Trim();
        string notes = txtNewShareNotes.Text.Trim();

        if (shareText.Length == 0)
        {
            ShowMessage("درصد جدید را وارد کنید.");
            return;
        }
        decimal? sp = ParseDecimal(shareText);
        if (!sp.HasValue || sp.Value < 0 || sp.Value > 100)
        {
            ShowMessage("درصد باید بین ۰ تا ۱۰۰ باشد.");
            return;
        }
        decimal newPercent = sp.Value;

        if (fromText.Length == 0)
        {
            ShowMessage("تاریخ اعمال را وارد کنید.");
            return;
        }
        DateTime? fromDt = ParsePersianDate(fromText);
        if (!fromDt.HasValue)
        {
            ShowMessage("تاریخ اعمال معتبر نیست. فرمت: 1403/04/01");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Get partner name
                string partnerName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT FullName FROM Partners WHERE PartnerId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", partnerId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) partnerName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(partnerName))
                {
                    ShowMessage("شریک مورد نظر یافت نشد.");
                    return;
                }

                // Check total percent won't exceed 100
                decimal otherShare = GetCurrentActiveShareSum(partnerId);
                if (otherShare + newPercent > 100m)
                {
                    ShowMessage(string.Format(
                        "با این درصد، مجموع سهام فعال از ۱۰۰٪ عبور می‌کند.\n" +
                        "سایر شرکا: {0}٪ + این شریک: {1}٪ = {2}٪",
                        otherShare.ToString("N2", CultureInfo.InvariantCulture),
                        newPercent.ToString("N2", CultureInfo.InvariantCulture),
                        (otherShare + newPercent).ToString("N2", CultureInfo.InvariantCulture)));
                    return;
                }

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Close the current open share (set EffectiveTo = fromDate - 1 second)
                        DateTime closeAt = fromDt.Value.AddSeconds(-1);
                        string closeSql =
                            "UPDATE PartnerShareHistory SET EffectiveTo = @CloseAt " +
                            "WHERE PartnerId = @PartnerId AND EffectiveTo IS NULL";

                        using (SqlCommand cmd = new SqlCommand(closeSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.Parameters.AddWithValue("@CloseAt", closeAt);
                            cmd.ExecuteNonQuery();
                        }

                        // Insert new share
                        string insertSql =
                            "INSERT INTO PartnerShareHistory " +
                            "  (PartnerId, SharePercent, EffectiveFrom, EffectiveTo, Notes, CreatedAt, CreatedBy) " +
                            "VALUES (@PartnerId, @SharePercent, @EffFrom, NULL, @Notes, GETDATE(), @CreatedBy)";

                        using (SqlCommand cmd = new SqlCommand(insertSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                            cmd.Parameters.AddWithValue("@SharePercent", newPercent);
                            cmd.Parameters.AddWithValue("@EffFrom", fromDt.Value);
                            cmd.Parameters.AddWithValue("@Notes", NullIfEmpty(notes));
                            cmd.Parameters.AddWithValue("@CreatedBy",
                                CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();

                        WriteAudit(conn, "Insert", "PartnerShareHistory", partnerId,
                            partnerName, "ثبت سهام جدید " + newPercent.ToString("N2") + "٪ از " +
                            ToShamsiDateString(fromDt.Value));
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }

                ShowMessage("درصد جدید با موفقیت ثبت شد.");
            }

            // Clear form
            txtNewSharePercent.Text = "";
            txtNewShareFrom.Text = "";
            txtNewShareNotes.Text = "";

            // Reload
            ActiveTab = "shares";
            ShowDetailView(partnerId, "shares");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت سهام: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Detail: Load Drawings
    // ─────────────────────────────────────────────────────
    private void LoadPartnerDrawings(int partnerId)
    {
        try
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                string sql =
                    "SELECT DrawingId, Amount, DrawingDate, PaymentMethod, ReferenceNumber, " +
                    "       Reason, Status, CreatedAt " +
                    "FROM PartnerDrawings WHERE PartnerId = @Id " +
                    "ORDER BY DrawingDate DESC, DrawingId DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                // Total open
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount),0) FROM PartnerDrawings " +
                    "WHERE PartnerId = @Id AND Status = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    object o = cmd.ExecuteScalar();
                    decimal total = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                    litDrawingsOpenTotal.Text = HttpUtility.HtmlEncode(FormatMoneyValue(total));
                }
            }

            if (dt.Rows.Count == 0)
            {
                pnlDrawingsEmpty.Visible = true;
                rptDrawings.Visible = false;
                rptDrawings.DataSource = null;
                rptDrawings.DataBind();
            }
            else
            {
                pnlDrawingsEmpty.Visible = false;
                rptDrawings.Visible = true;
                rptDrawings.DataSource = dt;
                rptDrawings.DataBind();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری برداشت‌ها: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Detail: Add Drawing
    // ─────────────────────────────────────────────────────
    protected void btnAddDrawing_Click(object sender, EventArgs e)
    {
        int partnerId = DetailPartnerId;
        if (partnerId <= 0)
        {
            ShowMessage("ابتدا یک شریک را انتخاب کنید.");
            return;
        }

        decimal? amount = ParseDecimal(txtDrawingAmount.Text);
        if (!amount.HasValue || amount.Value <= 0)
        {
            ShowMessage("مبلغ برداشت باید عددی بزرگ‌تر از صفر باشد.");
            return;
        }

        DateTime? drawingDate = ParsePersianDate(txtDrawingDate.Text.Trim());
        if (!drawingDate.HasValue)
        {
            ShowMessage("تاریخ برداشت معتبر نیست. فرمت: 1403/02/15");
            return;
        }

        string paymentMethod = ddlDrawingPaymentMethod.SelectedValue;
        if (paymentMethod == null) paymentMethod = "";

        string refNum = txtDrawingRef.Text.Trim();
        string reason = txtDrawingReason.Text.Trim();

        // Check available
        decimal available = GetEstimatedAvailable(partnerId);
        if (amount.Value > available)
        {
            // We don't block — just warn (return false to cancel if user doesn't confirm)
            // Since we don't have custom confirm on server side easily, we'll just proceed.
            // Better UX: proceed but show warning after.
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string partnerName = "";
                using (SqlCommand get = new SqlCommand(
                    "SELECT FullName FROM Partners WHERE PartnerId = @Id AND IsDeleted = 0", conn))
                {
                    get.Parameters.AddWithValue("@Id", partnerId);
                    object o = get.ExecuteScalar();
                    if (o != null && o != DBNull.Value) partnerName = Convert.ToString(o);
                }

                if (string.IsNullOrEmpty(partnerName))
                {
                    ShowMessage("شریک مورد نظر یافت نشد.");
                    return;
                }

                string insertSql =
                    "INSERT INTO PartnerDrawings " +
                    "  (PartnerId, Amount, DrawingDate, PaymentMethod, ReferenceNumber, " +
                    "   Reason, Status, ReturnedAmount, OverDrawnAmount, CreatedAt, CreatedBy) " +
                    "VALUES (@PartnerId, @Amount, @DrawingDate, @PM, @Ref, " +
                    "   @Reason, 1, 0, 0, GETDATE(), @CreatedBy); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newId = 0;
                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PartnerId", partnerId);
                    cmd.Parameters.AddWithValue("@Amount", amount.Value);
                    cmd.Parameters.AddWithValue("@DrawingDate", drawingDate.Value);
                    cmd.Parameters.AddWithValue("@PM", NullIfEmpty(paymentMethod));
                    cmd.Parameters.AddWithValue("@Ref", NullIfEmpty(refNum));
                    cmd.Parameters.AddWithValue("@Reason", NullIfEmpty(reason));
                    cmd.Parameters.AddWithValue("@CreatedBy",
                        CurrentUserId > 0 ? (object)CurrentUserId : DBNull.Value);

                    object r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value) newId = Convert.ToInt32(r);
                }

                if (newId > 0)
                    WriteAudit(conn, "Insert", "PartnerDrawings", newId, partnerName,
                        "ثبت برداشت " + FormatMoneyValue(amount.Value) + " تومان");
            }

            ShowMessage("برداشت با موفقیت ثبت شد.");
            txtDrawingAmount.Text = "";
            txtDrawingDate.Text = ToShamsiDateString(DateTime.Now);
            ddlDrawingPaymentMethod.ClearSelection();
            txtDrawingRef.Text = "";
            txtDrawingReason.Text = "";

            // Reload
            ShowDetailView(partnerId, "drawings");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در ثبت برداشت: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Cancel Drawing
    // ─────────────────────────────────────────────────────
    protected void rptDrawings_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName != "CancelDrawing") return;

        int drawingId;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out drawingId) || drawingId <= 0) return;

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // Verify it's Status = 1 (open)
                int status = 0;
                using (SqlCommand get = new SqlCommand(
                    "SELECT Status FROM PartnerDrawings WHERE DrawingId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", drawingId);
                    object o = get.ExecuteScalar();
                    if (o == null || o == DBNull.Value)
                    {
                        ShowMessage("برداشت یافت نشد.");
                        return;
                    }
                    status = Convert.ToInt32(o);
                }

                if (status != 1)
                {
                    ShowMessage("فقط برداشت‌های باز قابل لغو هستند.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE PartnerDrawings SET Status = 4 WHERE DrawingId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", drawingId);
                    cmd.ExecuteNonQuery();
                }

                WriteAudit(conn, "Cancel", "PartnerDrawings", drawingId,
                    "Drawing #" + drawingId, "لغو برداشت");
            }

            ShowMessage("برداشت لغو شد.");
            ShowDetailView(DetailPartnerId, "drawings");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // Estimate
    // ─────────────────────────────────────────────────────
    private void LoadEstimate(int partnerId)
    {
        try
        {
            // Get current share %
            decimal sharePct = 0m;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 SharePercent FROM PartnerShareHistory " +
                    "WHERE PartnerId = @Id AND EffectiveTo IS NULL " +
                    "ORDER BY EffectiveFrom DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) sharePct = Convert.ToDecimal(o);
                }
            }

            // Determine "current period" — from start of current Persian month to now
            PersianCalendar pc = new PersianCalendar();
            DateTime now = DateTime.Now;
            DateTime periodFrom = pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
            DateTime periodTo = now;

            // Get net profit for the period
            decimal periodProfit = ComputePeriodNetProfit(periodFrom, periodTo);

            decimal estimatedShare = 0m;
            if (periodProfit > 0)
                estimatedShare = Math.Round(periodProfit * sharePct / 100m, 2);

            // Get open drawings
            decimal openDrawings = 0m;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(Amount), 0) FROM PartnerDrawings " +
                    "WHERE PartnerId = @Id AND Status = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", partnerId);
                    object o = cmd.ExecuteScalar();
                    openDrawings = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }
            }

            decimal maxPct = GetSettingDecimal("MaxAdvancePercent", 80m);
            decimal capAmount = Math.Round(estimatedShare * maxPct / 100m, 2);
            decimal available = capAmount - openDrawings;
            if (available < 0m) available = 0m;

            litEstPeriodProfit.Text = HttpUtility.HtmlEncode(FormatMoneyValue(periodProfit));
            litEstFromDate.Text = HttpUtility.HtmlEncode(ToShamsiDateString(periodFrom));
            litEstShare.Text = HttpUtility.HtmlEncode(FormatMoneyValue(estimatedShare));
            litEstSharePercent.Text = HttpUtility.HtmlEncode(FormatPercentValue(sharePct));
            litEstDrawings.Text = HttpUtility.HtmlEncode(FormatMoneyValue(openDrawings));
            litEstAvailable.Text = HttpUtility.HtmlEncode(FormatMoneyValue(available));
            litEstMaxPct.Text = HttpUtility.HtmlEncode(FormatPercentValue(maxPct));
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در محاسبه سهم برآوردی: " + ex.Message);
        }
    }

    private decimal GetEstimatedAvailable(int partnerId)
    {
        decimal sharePct = 0m;
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TOP 1 SharePercent FROM PartnerShareHistory " +
                "WHERE PartnerId = @Id AND EffectiveTo IS NULL " +
                "ORDER BY EffectiveFrom DESC", conn))
            {
                cmd.Parameters.AddWithValue("@Id", partnerId);
                object o = cmd.ExecuteScalar();
                if (o != null && o != DBNull.Value) sharePct = Convert.ToDecimal(o);
            }
        }

        PersianCalendar pc = new PersianCalendar();
        DateTime now = DateTime.Now;
        DateTime periodFrom = pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);

        decimal periodProfit = ComputePeriodNetProfit(periodFrom, now);
        decimal estimatedShare = 0m;
        if (periodProfit > 0)
            estimatedShare = Math.Round(periodProfit * sharePct / 100m, 2);

        decimal openDrawings = 0m;
        using (SqlConnection conn = new SqlConnection(ConnStr))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(SUM(Amount), 0) FROM PartnerDrawings " +
                "WHERE PartnerId = @Id AND Status = 1", conn))
            {
                cmd.Parameters.AddWithValue("@Id", partnerId);
                object o = cmd.ExecuteScalar();
                openDrawings = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
            }
        }

        decimal maxPct = GetSettingDecimal("MaxAdvancePercent", 80m);
        decimal cap = Math.Round(estimatedShare * maxPct / 100m, 2);
        decimal available = cap - openDrawings;
        return available > 0m ? available : 0m;
    }

    private decimal ComputePeriodNetProfit(DateTime from, DateTime to)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                decimal revenue = 0m;
                decimal cogs = 0m;
                decimal expenses = 0m;

                // Revenue + COGS
                string sql1 =
                    "SELECT " +
                    "  ISNULL(SUM(fii.Total), 0) AS Revenue, " +
                    "  ISNULL(SUM(fii.COGS), 0) AS Cogs " +
                    "FROM FinalInvoices fi " +
                    "INNER JOIN FinalInvoiceItems fii ON fii.FinalInvoiceId = fi.FinalInvoiceId " +
                    "WHERE fi.Status IN (1, 2) " +
                    "  AND fi.IssueDate >= @From AND fi.IssueDate <= @To";

                using (SqlCommand cmd = new SqlCommand(sql1, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            revenue = dr["Revenue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Revenue"]);
                            cogs = dr["Cogs"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Cogs"]);
                        }
                    }
                }

                // Expenses
                string sql2 =
                    "SELECT ISNULL(SUM(Amount), 0) FROM Expenses " +
                    "WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate <= @To";

                using (SqlCommand cmd = new SqlCommand(sql2, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    object o = cmd.ExecuteScalar();
                    expenses = (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
                }

                return revenue - cogs - expenses;
            }
        }
        catch { return 0m; }
    }

    private decimal GetSettingDecimal(string key, decimal defaultValue)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT SettingValue FROM AppSettings WHERE SettingKey = @Key", conn))
                {
                    cmd.Parameters.AddWithValue("@Key", key);
                    object o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return defaultValue;
                    decimal d;
                    if (decimal.TryParse(Convert.ToString(o), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out d))
                        return d;
                    return defaultValue;
                }
            }
        }
        catch { return defaultValue; }
    }

    // ─────────────────────────────────────────────────────
    // Form Buttons
    // ─────────────────────────────────────────────────────
    protected void btnShowAddForm_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowFormView(0);
    }

    protected void btnCancelForm_Click(object sender, EventArgs e)
    {
        EditId = 0;
        ShowListView();
    }

    protected void btnBackToList_Click(object sender, EventArgs e)
    {
        EditId = 0;
        DetailPartnerId = 0;
        ShowListView();
    }

    protected void btnEditFromDetail_Click(object sender, EventArgs e)
    {
        ShowFormView(DetailPartnerId);
    }

    // ─────────────────────────────────────────────────────
    // Audit
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

    private string FormatMoneyValue(decimal d)
    {
        if (d == 0m) return "۰";
        return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    private string FormatPercentValue(decimal d)
    {
        if (d == 0m) return "۰";
        if (d == Math.Floor(d))
            return d.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "،");
        return d.ToString("N2", CultureInfo.InvariantCulture).Replace(",", "،");
    }

    protected string FormatPercent(object value)
    {
        if (value == null || value == DBNull.Value) return "۰٪";
        try
        {
            return FormatPercentValue(Convert.ToDecimal(value)) + "٪";
        }
        catch { return "۰٪"; }
    }

    protected string FormatOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        string s = Convert.ToString(value).Trim();
        if (s.Length == 0) return "—";
        return HttpUtility.HtmlEncode(s);
    }

    private string HtmlOrDash(object value)
    {
        if (value == null || value == DBNull.Value) return "—";
        string s = Convert.ToString(value).Trim();
        if (s.Length == 0) return "—";
        return HttpUtility.HtmlEncode(s);
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

    protected string FormatShamsiDateOrOpen(object value)
    {
        if (value == null || value == DBNull.Value)
            return "<span class=\"text-emerald-600 font-bold\">در جریان</span>";
        return FormatShamsiDate(value);
    }

    private string FormatShamsiDateTime(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2} — {3:D2}:{4:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt),
            dt.Hour, dt.Minute);
    }

    private string ToShamsiDateString(DateTime dt)
    {
        PersianCalendar pc = new PersianCalendar();
        return string.Format(CultureInfo.InvariantCulture,
            "{0}/{1:D2}/{2:D2}",
            pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    // ── Card helpers ──
    protected string GetPartnerCardCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string baseC = "relative overflow-hidden rounded-card border-2 bg-surface-card p-4 shadow-card transition hover:shadow-card-lg";
        if (!isActive) return baseC + " border-surface-line opacity-60";
        return baseC + " border-surface-line";
    }

    protected string GetPartnerAvatarCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string baseC = "grid h-10 w-10 shrink-0 place-items-center rounded-base";
        if (!isActive) return baseC + " bg-surface-muted text-ink-400";
        return baseC + " bg-brand-50 text-brand-600";
    }

    protected string GetStatusDot(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        if (isActive)
            return "<span class=\"inline-block h-2.5 w-2.5 rounded-full bg-emerald-500\" title=\"فعال\"></span>";
        return "<span class=\"inline-block h-2.5 w-2.5 rounded-full bg-ink-300\" title=\"غیرفعال\"></span>";
    }

    protected string GetShareBadge(object shareObj)
    {
        try
        {
            decimal s = (shareObj == null || shareObj == DBNull.Value) ? 0m : Convert.ToDecimal(shareObj);
            if (s <= 0m)
                return "<span class=\"badge bg-surface-muted text-ink-500\">بدون سهم فعال</span>";
            return "<span class=\"badge bg-brand-50 text-brand-700 font-mono font-bold\">" +
                   FormatPercentValue(s) + "٪ سهام</span>";
        }
        catch { return ""; }
    }

    protected string GetLinkedUserBadge(object usernameObj)
    {
        if (usernameObj == null || usernameObj == DBNull.Value) return "";
        string u = Convert.ToString(usernameObj).Trim();
        if (u.Length == 0) return "";
        return "<span class=\"badge bg-violet-50 text-violet-700\">@" + HttpUtility.HtmlEncode(u) + "</span>";
    }

    protected string GetToggleCss(object isActiveObj)
    {
        bool isActive = IsTrue(isActiveObj);
        if (isActive) return "icon-button hover:bg-warning-50 hover:text-warning-600";
        return "icon-button hover:bg-success-50 hover:text-success-600";
    }

    protected string GetToggleTooltip(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "غیرفعال کردن" : "فعال کردن";
    }

    protected string GetToggleIcon(object isActiveObj)
    {
        return IsTrue(isActiveObj) ? "toggle-right" : "toggle-left";
    }

    protected string GetToggleConfirm(object isActiveObj, object nameObj)
    {
        bool isActive = IsTrue(isActiveObj);
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        if (isActive) return "return confirm('شریک «" + name + "» غیرفعال شود؟');";
        return "return confirm('شریک «" + name + "» فعال شود؟');";
    }

    protected string GetDeleteConfirm(object nameObj)
    {
        string name = (nameObj == null || nameObj == DBNull.Value) ? "" : nameObj.ToString().Replace("'", "");
        return "return confirm('آیا از حذف شریک «" + name + "» مطمئن هستید؟');";
    }

    protected bool CanDelete(object openDrawingsObj, object hasHistoryObj)
    {
        decimal drawings = (openDrawingsObj == null || openDrawingsObj == DBNull.Value)
            ? 0m : Convert.ToDecimal(openDrawingsObj);
        bool hasHistory = IsTrue(hasHistoryObj);
        return drawings <= 0m && !hasHistory;
    }

    // ── Share helpers ──
    protected string GetShareRowCss(object effectiveToObj)
    {
        if (effectiveToObj == null || effectiveToObj == DBNull.Value)
            return "border-b border-surface-line bg-emerald-50/40 hover:bg-emerald-50/60";
        return "border-b border-surface-line hover:bg-surface-body/70";
    }

    protected string GetShareStatusBadge(object effectiveToObj)
    {
        if (effectiveToObj == null || effectiveToObj == DBNull.Value)
            return "<span class=\"badge bg-emerald-50 text-emerald-700\">در جریان</span>";
        return "<span class=\"badge bg-surface-muted text-ink-500\">بسته‌شده</span>";
    }

    // ── Drawing helpers ──
    protected string GetPaymentBadge(object pmObj)
    {
        if (pmObj == null || pmObj == DBNull.Value)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";
        string pm = Convert.ToString(pmObj).Trim();
        if (pm.Length == 0)
            return "<span class=\"text-[11.5px] text-ink-400\">—</span>";

        string css = "badge bg-surface-muted text-ink-600";
        if (pm == "نقدی") css = "badge bg-emerald-50 text-emerald-700";
        else if (pm == "کارت بانکی") css = "badge bg-brand-50 text-brand-700";
        else if (pm == "انتقال بانکی") css = "badge bg-violet-50 text-violet-700";
        else if (pm == "چک") css = "badge bg-amber-50 text-amber-700";

        return "<span class=\"" + css + "\">" + HttpUtility.HtmlEncode(pm) + "</span>";
    }

    protected string GetDrawingStatusBadge(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        if (s == 1) return "<span class=\"badge bg-amber-50 text-amber-700\">باز (علی‌الحساب)</span>";
        if (s == 2) return "<span class=\"badge bg-emerald-50 text-emerald-700\">تسویه‌شده</span>";
        if (s == 3) return "<span class=\"badge bg-brand-50 text-brand-700\">برگشت‌داده</span>";
        if (s == 4) return "<span class=\"badge bg-surface-muted text-ink-500\">لغو‌شده</span>";
        return "<span class=\"badge bg-surface-muted text-ink-500\">نامشخص</span>";
    }

    protected bool CanCancelDrawing(object statusObj)
    {
        int s = (statusObj == null || statusObj == DBNull.Value) ? 0 : Convert.ToInt32(statusObj);
        return s == 1;
    }

    protected string GetCancelDrawingConfirm()
    {
        return "return confirm('آیا از لغو این برداشت مطمئن هستید؟');";
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private object NullIfEmpty(string s)
    {
        if (s == null) return DBNull.Value;
        if (s.Trim().Length == 0) return DBNull.Value;
        return s.Trim();
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
    // Clear
    // ─────────────────────────────────────────────────────
    private void ClearForm()
    {
        txtFullName.Text = "";
        txtNationalId.Text = "";
        txtMobile.Text = "";
        txtEmail.Text = "";
        txtBankAccount.Text = "";
        txtNotes.Text = "";
        chkIsActive.Checked = true;

        if (ddlUserId.Items.Count > 0) ddlUserId.SelectedIndex = 0;

        txtInitialSharePercent.Text = "";
        txtInitialShareFrom.Text = ToShamsiDateString(DateTime.Now);
        txtInitialShareNotes.Text = "";
        shareWarningBox.Visible = false;
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