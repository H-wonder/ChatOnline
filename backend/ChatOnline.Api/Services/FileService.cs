using Microsoft.AspNetCore.Http;

namespace ChatOnline.Api.Services;

/// <summary>
/// 文件服务：负责文件存储、类型校验、大小限制
/// 文件保存在 ./uploads/ 目录下
/// </summary>
public class FileService
{
    // 上传目录路径
    private readonly string _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    // 允许的文件类型
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private static readonly HashSet<string> AllowedFileTypes = new()
    {
        ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar", ".mp3", ".mp4"
    };

    // 最大文件大小：10MB
    private const long MaxFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// 保存上传的文件到 uploads/ 目录
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <returns>文件的 URL 相对路径，供前端读取</returns>
    public async Task<(bool Success, string Message, string? FileUrl)> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "文件为空", null);

        if (file.Length > MaxFileSize)
            return (false, $"文件最大 {MaxFileSize / 1024 / 1024}MB", null);

        // 获取文件扩展名并校验类型
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        bool isImage = AllowedImageTypes.Contains(extension);
        bool isFile = AllowedFileTypes.Contains(extension);

        if (!isImage && !isFile)
            return (false, "不支持的文件类型", null);

        // 生成唯一文件名，避免重名覆盖
        string uniqueName = $"{Guid.NewGuid()}{extension}";

        // 按日期分子目录
        string dateDir = DateTime.UtcNow.ToString("yyyyMM");
        string dir = Path.Combine(_uploadDir, dateDir);

        // 确保目录存在
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 写入磁盘
        string filePath = Path.Combine(dir, uniqueName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 返回访问路径
        string fileUrl = $"/uploads/{dateDir}/{uniqueName}";
        return (true, "上传成功", fileUrl);
    }
}
