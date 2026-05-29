using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatOnline.Api.Services;

namespace ChatOnline.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;

    public FileController(FileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// POST /api/files/upload
    /// 上传文件（IFormFile：ASP.NET Core 对上传文件的标准封装）
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var (success, message, fileUrl) = await _fileService.UploadFileAsync(file);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { success = true, message, fileUrl });
    }
}
