using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_change_password : System.Web.UI.Page
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

        if (!IsPostBack)
        {
            LoadUserInfo();
        }
    }

    // ─────────────────────────────────────────────────────
    // بارگذاری اطلاعات کاربر
    // ─────────────────────────────────────────────────────
    private void LoadUserInfo()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT u.Username, u.FullName, u.LastLoginAt, " +
                    "       r.RoleName " +
                    "FROM Users u " +
                    "INNER JOIN Roles r ON r.RoleId = u.RoleId " +
                    "WHERE u.UserId = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", CurrentUserId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string username = Convert.ToString(dr["Username"]);
                            string fullName = dr["FullName"] == DBNull.Value
                                ? "" : Convert.ToString(dr["FullName"]).Trim();
                            string roleName = dr["RoleName"] == DBNull.Value
                                ? "کاربر" : Convert.ToString(dr["RoleName"]);

                            litUserFullName.Text = HttpUtility.HtmlEncode(
                                fullName.Length > 0 ? fullName : username);
                            litUsername.Text = HttpUtility.HtmlEncode(username);
                            litUserRole.Text = HttpUtility.HtmlEncode(roleName);

                            string initial = "U";
                            if (fullName.Length > 0)
                                initial = fullName.Substring(0, 1);
                            else if (username.Length > 0)
                                initial = username.Substring(0, 1).ToUpper();
                            litUserInitial.Text = HttpUtility.HtmlEncode(initial);

                            if (dr["LastLoginAt"] != DBNull.Value)
                            {
                                litLastLogin.Text = HttpUtility.HtmlEncode(
                                    FormatShamsiDateTime(Convert.ToDateTime(dr["LastLoginAt"])));
                            }
                            else
                            {
                                litLastLogin.Text = "—";
                            }
                        }
                    }
                }

                // آخرین تغییر رمز — از AuditLogs
                litLastPasswordChange.Text = GetLastPasswordChangeText(conn);
            }
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در بارگذاری اطلاعات کاربر: " + ex.Message);
        }
    }

    private string GetLastPasswordChangeText(SqlConnection conn)
    {
        try
        {
            string sql =
                "SELECT TOP 1 CreatedAt FROM AuditLogs " +
                "WHERE UserId = @UserId AND Action = 'ChangePassword' " +
                "ORDER BY AuditLogId DESC";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", CurrentUserId);
                object o = cmd.ExecuteScalar();
                if (o != null && o != DBNull.Value)
                {
                    return FormatShamsiDateTime(Convert.ToDateTime(o));
                }
            }
        }
        catch { }
        return "—";
    }

    private string FormatShamsiDateTime(DateTime dt)
    {
        try
        {
            PersianCalendar pc = new PersianCalendar();
            return string.Format(CultureInfo.InvariantCulture,
                "{0}/{1:D2}/{2:D2} - {3:D2}:{4:D2}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt),
                dt.Hour, dt.Minute);
        }
        catch
        {
            return "—";
        }
    }

    // ─────────────────────────────────────────────────────
    // تغییر رمز عبور
    // ─────────────────────────────────────────────────────
    protected void btnChangePassword_Click(object sender, EventArgs e)
    {
        string currentPwd = txtCurrentPassword.Text;
        string newPwd = txtNewPassword.Text;
        string confirmPwd = txtConfirmPassword.Text;

        // ── اعتبارسنجی ──
        if (currentPwd.Length == 0)
        {
            ShowMessage("رمز عبور فعلی را وارد کنید.");
            return;
        }
        if (newPwd.Length == 0)
        {
            ShowMessage("رمز عبور جدید را وارد کنید.");
            return;
        }
        if (newPwd.Length < 6)
        {
            ShowMessage("رمز عبور جدید باید حداقل ۶ کاراکتر باشد.");
            return;
        }
        if (newPwd.Length > 200)
        {
            ShowMessage("رمز عبور جدید نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            return;
        }
        if (newPwd != confirmPwd)
        {
            ShowMessage("رمز عبور جدید و تکرار آن یکسان نیستند.");
            return;
        }
        if (currentPwd == newPwd)
        {
            ShowMessage("رمز عبور جدید باید با رمز فعلی متفاوت باشد.");
            return;
        }

        // ── بررسی پیچیدگی ──
        if (!IsStrongEnoughPassword(newPwd))
        {
            ShowMessage("رمز جدید باید حداقل شامل یک حرف انگلیسی و یک عدد باشد.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // ── خواندن هش و Salt فعلی ──
                string dbHash = "";
                string dbSalt = "";

                using (SqlCommand get = new SqlCommand(
                    "SELECT PasswordHash, PasswordSalt FROM Users WHERE UserId = @Id", conn))
                {
                    get.Parameters.AddWithValue("@Id", CurrentUserId);
                    using (SqlDataReader dr = get.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            dbHash = dr["PasswordHash"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PasswordHash"]);
                            dbSalt = dr["PasswordSalt"] == DBNull.Value
                                ? "" : Convert.ToString(dr["PasswordSalt"]);
                        }
                        else
                        {
                            ShowMessage("کاربر یافت نشد. لطفاً مجدداً وارد شوید.");
                            return;
                        }
                    }
                }

                // ── بررسی رمز فعلی ──
                // اگر رمز هنوز در حالت پیش‌فرض است (Placeholder)، هیچ‌گاه اینجا نیستیم چون Login خودش آن را هش کرده.
                // اما اگر رمز NULL یا Placeholder بود، اجازه می‌دهیم با رمز پیش‌فرض بررسی شود.
                bool currentOk = false;

                if (string.Equals(dbHash, "CHANGE_ME_ON_FIRST_LOGIN", StringComparison.Ordinal))
                {
                    // فقط در صورتی که کاربر admin باشد و رمز پیش‌فرض را وارد کرده
                    string uname = Convert.ToString(Session["Username"]);
                    if (string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase)
                        && currentPwd == "Admin@123")
                    {
                        currentOk = true;
                    }
                }
                else
                {
                    string computed = HashPassword(currentPwd, dbSalt);
                    if (string.Equals(computed, dbHash, StringComparison.OrdinalIgnoreCase))
                    {
                        currentOk = true;
                    }
                }

                if (!currentOk)
                {
                    WriteAudit(conn, "ChangePasswordFailed", "رمز فعلی اشتباه");
                    ShowMessage("رمز عبور فعلی اشتباه است.");
                    return;
                }

                // ── ساخت هش جدید ──
                string newSalt = GenerateSalt();
                string newHash = HashPassword(newPwd, newSalt);

                // ── به‌روزرسانی ──
                using (SqlCommand upd = new SqlCommand(
                    "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = @Salt, " +
                    "  UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy " +
                    "WHERE UserId = @Id", conn))
                {
                    upd.Parameters.AddWithValue("@Hash", newHash);
                    upd.Parameters.AddWithValue("@Salt", newSalt);
                    upd.Parameters.AddWithValue("@UpdatedBy", CurrentUserId);
                    upd.Parameters.AddWithValue("@Id", CurrentUserId);
                    upd.ExecuteNonQuery();
                }

                // ── Audit ──
                WriteAudit(conn, "ChangePassword", "تغییر رمز عبور توسط کاربر");

                // ── پاک کردن فرم ──
                txtCurrentPassword.Text = "";
                txtNewPassword.Text = "";
                txtConfirmPassword.Text = "";

                // ── به‌روزرسانی نمایش آخرین تغییر رمز ──
                litLastPasswordChange.Text = GetLastPasswordChangeText(conn);
            }

            ShowMessageSuccess("رمز عبور شما با موفقیت تغییر یافت.");
        }
        catch (Exception ex)
        {
            ShowMessage("خطا در تغییر رمز عبور: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────
    // بررسی قدرت رمز
    // ─────────────────────────────────────────────────────
    private bool IsStrongEnoughPassword(string pwd)
    {
        bool hasLetter = false;
        bool hasDigit = false;

        for (int i = 0; i < pwd.Length; i++)
        {
            char c = pwd[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) hasLetter = true;
            else if (c >= '0' && c <= '9') hasDigit = true;
        }

        return hasLetter && hasDigit;
    }

    // ─────────────────────────────────────────────────────
    // Hash
    // ─────────────────────────────────────────────────────
    private string GenerateSalt()
    {
        byte[] saltBytes = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    private string HashPassword(string password, string salt)
    {
        if (salt == null) salt = "";
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        byte[] passBytes = Encoding.UTF8.GetBytes(password);

        byte[] combined = new byte[saltBytes.Length + passBytes.Length];
        System.Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        System.Buffer.BlockCopy(passBytes, 0, combined, saltBytes.Length, passBytes.Length);

        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(combined);
            StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }

    // ─────────────────────────────────────────────────────
    // Audit
    // ─────────────────────────────────────────────────────
    private void WriteAudit(SqlConnection conn, string action, string note)
    {
        try
        {
            string sql =
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, @Action, 'Users', @EntityId, @NewValues, @IPAddress, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", CurrentUserId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityId", CurrentUserId);

                string json = "{\"note\":\"" + (note != null ? note.Replace("\"", "'") : "") + "\"}";
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
    // Helpers پیام
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

    private void ShowMessageSuccess(string message)
    {
        if (message == null) message = "";
        string safe = message
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", " ");

        // نمایش پیام موفقیت + هدایت به داشبورد
        string script =
            "alert('" + safe + "');" +
            "window.location.href = 'Default.aspx';";

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