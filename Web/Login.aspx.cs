using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;

public partial class Login : System.Web.UI.Page
{
    private string ConnStr
    {
        get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; }
    }

    // رمز اولیه مدیر برای تغییر خودکار
    private const string InitialAdminPassword = "Admin@123";
    private const string PlaceholderHash = "CHANGE_ME_ON_FIRST_LOGIN";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litYear.Text = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);

            if (Request.QueryString["logout"] == "1")
            {
                litSuccess.Text = HttpUtility.HtmlEncode("با موفقیت از سامانه خارج شدید.");
                pnlSuccess.Visible = true;
            }

            // اگر کاربر قبلاً لاگین است، مستقیم برود داشبورد
            if (Session["UserId"] != null)
            {
                Response.Redirect(ResolveUrl("~/admin/Default.aspx"), false);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Login
    // ─────────────────────────────────────────────
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        pnlError.Visible = false;
        pnlSuccess.Visible = false;

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (username.Length == 0)
        {
            ShowError("نام کاربری را وارد کنید.");
            return;
        }
        if (password.Length == 0)
        {
            ShowError("رمز عبور را وارد کنید.");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                string sql =
                    "SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, " +
                    "       u.IsActive, u.IsDeleted, u.RoleId, r.RoleName, r.IsActive AS RoleIsActive " +
                    "FROM Users u " +
                    "INNER JOIN Roles r ON r.RoleId = u.RoleId " +
                    "WHERE u.Username = @Username";

                long userId = 0;
                string dbHash = "";
                string dbSalt = "";
                string fullName = "";
                string roleName = "";
                int roleId = 0;
                bool isActive = false;
                bool isDeleted = false;
                bool roleActive = false;

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ShowError("نام کاربری یا رمز عبور نادرست است.");
                            return;
                        }

                        userId = Convert.ToInt64(dr["UserId"]);
                        dbHash = dr["PasswordHash"] == DBNull.Value ? "" : Convert.ToString(dr["PasswordHash"]);
                        dbSalt = dr["PasswordSalt"] == DBNull.Value ? "" : Convert.ToString(dr["PasswordSalt"]);
                        fullName = dr["FullName"] == DBNull.Value ? "" : Convert.ToString(dr["FullName"]);
                        roleName = dr["RoleName"] == DBNull.Value ? "" : Convert.ToString(dr["RoleName"]);
                        roleId = dr["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["RoleId"]);
                        isActive = dr["IsActive"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsActive"]);
                        isDeleted = dr["IsDeleted"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsDeleted"]);
                        roleActive = dr["RoleIsActive"] == DBNull.Value ? false : Convert.ToBoolean(dr["RoleIsActive"]);
                    }
                }

                if (isDeleted)
                {
                    ShowError("این حساب کاربری حذف شده است.");
                    return;
                }
                if (!isActive)
                {
                    ShowError("حساب کاربری شما غیرفعال است. با مدیر سیستم تماس بگیرید.");
                    return;
                }
                if (!roleActive)
                {
                    ShowError("نقش کاربری شما غیرفعال است.");
                    return;
                }

                // ── بررسی رمز ──
                bool passwordOk = false;

                if (string.Equals(dbHash, PlaceholderHash, StringComparison.Ordinal))
                {
                    // حالت اولیه — فقط برای admin با رمز پیش‌فرض
                    if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase)
                        && password == InitialAdminPassword)
                    {
                        passwordOk = true;

                        // رمز را هش و ذخیره کن
                        string newSalt = GenerateSalt();
                        string newHash = HashPassword(password, newSalt);

                        using (SqlCommand upd = new SqlCommand(
                            "UPDATE Users SET PasswordHash = @H, PasswordSalt = @S, UpdatedAt = GETDATE() WHERE UserId = @Id", conn))
                        {
                            upd.Parameters.AddWithValue("@H", newHash);
                            upd.Parameters.AddWithValue("@S", newSalt);
                            upd.Parameters.AddWithValue("@Id", userId);
                            upd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    // بررسی معمول
                    string computed = HashPassword(password, dbSalt);
                    if (string.Equals(computed, dbHash, StringComparison.OrdinalIgnoreCase))
                        passwordOk = true;
                }

                if (!passwordOk)
                {
                    WriteFailedLoginAudit(conn, userId, username);
                    ShowError("نام کاربری یا رمز عبور نادرست است.");
                    return;
                }

                // ── Session ──
                Session["UserId"] = userId;
                Session["Username"] = username;
                Session["FullName"] = fullName;
                Session["RoleId"] = roleId;
                Session["PrimaryRole"] = roleName;
                Session["LoginAt"] = DateTime.Now;

                // ── LastLoginAt ──
                using (SqlCommand upd = new SqlCommand(
                    "UPDATE Users SET LastLoginAt = GETDATE() WHERE UserId = @Id", conn))
                {
                    upd.Parameters.AddWithValue("@Id", userId);
                    upd.ExecuteNonQuery();
                }

                // ── Audit log ──
                try
                {
                    using (SqlCommand log = new SqlCommand(
                        "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                        "VALUES (@UserId, 'Login', 'Users', @UserId, @NewValues, @IPAddress, GETDATE())", conn))
                    {
                        log.Parameters.AddWithValue("@UserId", userId);
                        log.Parameters.AddWithValue("@NewValues",
                            "{\"username\":\"" + username.Replace("\"", "'") + "\"}");
                        log.Parameters.AddWithValue("@IPAddress",
                            GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                        log.ExecuteNonQuery();
                    }
                }
                catch { }

                // ── Redirect ──
                string returnUrl = Request.QueryString["returnUrl"];
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                {
                    Response.Redirect(returnUrl, false);
                }
                else
                {
                    Response.Redirect(ResolveUrl("~/admin/Default.aspx"), false);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("خطای سیستم: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
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

    private void WriteFailedLoginAudit(SqlConnection conn, long userId, string username)
    {
        try
        {
            using (SqlCommand log = new SqlCommand(
                "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, NewValues, IPAddress, CreatedAt) " +
                "VALUES (@UserId, 'LoginFailed', 'Users', @UserId, @NewValues, @IPAddress, GETDATE())", conn))
            {
                log.Parameters.AddWithValue("@UserId", userId > 0 ? (object)userId : DBNull.Value);
                log.Parameters.AddWithValue("@NewValues",
                    "{\"username\":\"" + username.Replace("\"", "'") + "\"}");
                log.Parameters.AddWithValue("@IPAddress",
                    GetClientIP().Length > 0 ? (object)GetClientIP() : DBNull.Value);
                log.ExecuteNonQuery();
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

    private void ShowError(string message)
    {
        litError.Text = HttpUtility.HtmlEncode(message);
        pnlError.Visible = true;
    }
}