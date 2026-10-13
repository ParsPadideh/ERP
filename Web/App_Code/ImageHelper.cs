using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;

public static class ImageHelper
{
    public const string ProductImagesFolder = "~/Images/Products/";
    public const string NoImagePath = "~/Images/Products/no-image.png";
    public const int MaxFileSize = 3 * 1024 * 1024; // 3 MB
    public const int ThumbnailSize = 600; // مربع 600×600

    private static readonly string[] AllowedExtensions = new string[]
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    // ═══════════════════════════════════════════════════════
    // Save with auto-resize to 600×600
    // ═══════════════════════════════════════════════════════
    public static string SaveProductImage(FileUpload fileUpload, HttpServerUtility server,
        string productCode, out string error)
    {
        error = null;

        // بررسی وجود فایل
        if (fileUpload == null || !fileUpload.HasFile)
        {
            error = "لطفاً یک فایل تصویر انتخاب کنید.";
            return null;
        }

        // پسوند
        string ext = Path.GetExtension(fileUpload.FileName).ToLowerInvariant();
        bool allowed = false;
        for (int i = 0; i < AllowedExtensions.Length; i++)
        {
            if (AllowedExtensions[i] == ext) { allowed = true; break; }
        }
        if (!allowed)
        {
            error = "پسوند فایل مجاز نیست. فرمت‌های مجاز: JPG, PNG, GIF, WebP";
            return null;
        }

        // حجم
        if (fileUpload.PostedFile.ContentLength > MaxFileSize)
        {
            error = "حجم فایل باید کمتر از ۳ مگابایت باشد.";
            return null;
        }

        // نام یکتا — همه با .jpg ذخیره می‌شوند
        string code = SanitizeCode(productCode);
        string fileName = code + "_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture) + ".jpg";

        string folder = server.MapPath(ProductImagesFolder);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string physicalPath = Path.Combine(folder, fileName);

        try
        {
            // خواندن + تغییر اندازه + ذخیره با کیفیت 85%
            using (System.IO.Stream stream = fileUpload.PostedFile.InputStream)
            using (System.Drawing.Image original = System.Drawing.Image.FromStream(stream))
            {
                ResizeAndSave(original, physicalPath, ThumbnailSize);
            }
        }
        catch (Exception ex)
        {
            error = "خطا در پردازش تصویر: " + ex.Message;
            return null;
        }

        // بررسی نهایی وجود فایل روی سرور
        if (!File.Exists(physicalPath))
        {
            error = "تصویر ذخیره نشد. لطفاً دوباره تلاش کنید.";
            return null;
        }

        return ProductImagesFolder + fileName;
    }

    // ═══════════════════════════════════════════════════════
    // Resize + Save (Cropped to Square)
    // ═══════════════════════════════════════════════════════
    private static void ResizeAndSave(System.Drawing.Image original, string savePath, int size)
    {
        // مربع کن (crop از مرکز)
        int side = Math.Min(original.Width, original.Height);
        int srcX = (original.Width - side) / 2;
        int srcY = (original.Height - side) / 2;

        using (Bitmap output = new Bitmap(size, size))
        using (Graphics g = Graphics.FromImage(output))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            // پس‌زمینه‌ی سفید (برای شفاف PNG ها)
            g.Clear(Color.White);

            Rectangle destRect = new Rectangle(0, 0, size, size);
            Rectangle srcRect = new Rectangle(srcX, srcY, side, side);
            g.DrawImage(original, destRect, srcRect, GraphicsUnit.Pixel);

            // ✅ ذخیره واقعی فایل (داخل same using تا output dispose نشده)
            SaveAsJpeg(output, savePath, 85L);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Save as JPEG with quality
    // ═══════════════════════════════════════════════════════
    private static void SaveAsJpeg(System.Drawing.Image image, string savePath, long quality)
    {
        // حذف فایل قبلی (اگر هست)
        if (File.Exists(savePath)) File.Delete(savePath);

        ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);

        if (jpegEncoder != null)
        {
            using (EncoderParameters encoderParams = new EncoderParameters(1))
            {
                encoderParams.Param[0] =
                    new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                image.Save(savePath, jpegEncoder, encoderParams);
            }
        }
        else
        {
            // fallback اگر encoder پیدا نشد
            image.Save(savePath, ImageFormat.Jpeg);
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        for (int i = 0; i < codecs.Length; i++)
        {
            if (codecs[i].FormatID == format.Guid) return codecs[i];
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════
    // Delete
    // ═══════════════════════════════════════════════════════
    public static void DeleteProductImage(string relativeUrl, HttpServerUtility server)
    {
        if (string.IsNullOrEmpty(relativeUrl)) return;
        if (relativeUrl.EndsWith("no-image.png", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            string physicalPath = server.MapPath(relativeUrl);
            if (File.Exists(physicalPath)) File.Delete(physicalPath);
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════════
    // Get URL (with fallback)
    // ═══════════════════════════════════════════════════════
    public static string GetProductImageUrl(string imageUrl, string appPath)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            string url = imageUrl.Trim();
            if (url.StartsWith("~")) url = appPath + url.Substring(1);
            else if (!url.StartsWith("/")) url = appPath + "/" + url;

            return url.Replace("//", "/");
        }
        string fallback = appPath + "/Images/Products/no-image.png";
        return fallback.Replace("//", "/");
    }

    private static string SanitizeCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "product";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = code;
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');
        result = result.Replace(" ", "_");
        if (result.Length > 40) result = result.Substring(0, 40);
        return result;
    }
}