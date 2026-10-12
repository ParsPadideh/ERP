using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;

public partial class tools_PasswordHasher : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void btnGenerate_Click(object sender, EventArgs e)
    {
        string password = txtPassword.Text;
        if (string.IsNullOrEmpty(password))
        {
            Response.Write("<script>alert('رمز را وارد کنید.');</script>");
            return;
        }

        string salt = GenerateSalt();
        string hash = HashPassword(password, salt);

        litSalt.Text = HttpUtility.HtmlEncode(salt);
        litHash.Text = HttpUtility.HtmlEncode(hash);

        string sql =
            "UPDATE Users SET \r\n" +
            "  PasswordHash = N'" + hash.Replace("'", "''") + "', \r\n" +
            "  PasswordSalt = N'" + salt.Replace("'", "''") + "', \r\n" +
            "  UpdatedAt = GETDATE() \r\n" +
            "WHERE Username = N'admin';";

        litSql.Text = HttpUtility.HtmlEncode(sql);
        pnlResult.Visible = true;
    }

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
}