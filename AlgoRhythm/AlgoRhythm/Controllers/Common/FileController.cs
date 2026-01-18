using AlgoRhythm.Shared.Models.Courses;
using Microsoft.AspNetCore.Mvc;
using AlgoRhythm.Attributes;
using Microsoft.AspNetCore.Components.Web;
using Azure;
using System.Text.Encodings.Web;
using System.Web;
using AlgoRhythm.Services.Blob.Interfaces;
using Microsoft.AspNetCore.Authorization;
using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _storageService;
        public FileController(IFileStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Uploads a file to blob storage. Admin only.
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>URL of the uploaded file</returns>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {

            if (file == null || file.Length == 0)
            {
                return BadRequest("Empty file!");
            }

            using var stream = file.OpenReadStream();

            try
            {
                string blobUrl = await _storageService.UploadFileAsync(
                    file.FileName,
                    stream,
                    file.ContentType);

                return Ok(new { Url = blobUrl, Message = "File saved!" });
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return Conflict(new { error = $"File {file.FileName} already exists!" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Unexpected error!" });
            }
        }

        /// <summary>
        /// Retrieves a paginated list of files from blob storage. Admin only.
        /// Returns only metadata - use get_file endpoint to download actual files.
        /// </summary>
        /// <param name="pageSize">Number of files per page (default: 50, max: 100)</param>
        /// <param name="continuationToken">Token for next page (optional)</param>
        /// <returns>List of file metadata with continuation token</returns>
        [HttpGet("list")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListFiles([FromQuery] int pageSize = 50, [FromQuery] string? continuationToken = null)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var (files, nextToken) = await _storageService.ListFilesAsync(pageSize, continuationToken);

            return Ok(new
            {
                Files = files,
                ContinuationToken = nextToken,
                HasMore = nextToken != null
            });
        }

        /// <summary>
        /// Video preview for debugging, not visible in swagger (it does not support streaming).
        /// URL should be opened directly in browser to preview video.
        /// </summary>
        /// <param name="path">Path to the video file</param>
        /// <returns>HTML page with video player</returns>
        [HttpGet("preview_video")]
        [DevelopmentOnly]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult PreviewVideo([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Path is required");
            }

            var videoUrl = $"/api/File/get_file?fileName={Uri.EscapeDataString(path)}";

            return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Video Preview</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            margin: 20px;
                            background: #f0f0f0;
                        }}
                        .container {{
                            max-width: 1200px;
                            margin: 0 auto;
                            background: white;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                        }}
                        video {{
                            width: 100%;
                            max-width: 800px;
                            display: block;
                            margin: 20px auto;
                        }}
                        .info {{
                            color: #666;
                            font-size: 14px;
                            margin-top: 10px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Video Preview</h1>
                        <div class='info'>File: {HttpUtility.HtmlEncode(path)}</div>
                        <video controls preload='metadata'>
                            <source src='{HttpUtility.HtmlEncode(videoUrl)}' type='video/mp4'>
                            Your browser does not support the video tag.
                        </video>
                    </div>
                </body>
                </html>
            ", "text/html");
        }

        /// <summary>
        /// Retrieves a file stream from blob storage with range support for streaming.
        /// Supports partial content requests for efficient video/audio streaming.
        /// </summary>
        /// <param name="fileName">Name of the file in blob storage (with GUID prefix)</param>
        /// <returns>File stream with range processing enabled</returns>
        [HttpGet("get_file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFile([FromQuery] string fileName)
        {
            fileName = fileName.Trim('"');

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("fileName is required");
            }

            try
            {
                var (stream, contentType) = await _storageService.GetFileAsync(fileName);

                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch(FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves video metadata needed for streaming.
        /// </summary>
        /// <param name="fileName">Path to the video file</param>
        /// <returns>Video metadata</returns>
        [HttpGet("video_info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVideoInfo([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("Path is required");
            }

            var videoInfo = await _storageService.GetVideoInfoAsync(fileName);

            if (videoInfo == null)
            {
                return NotFound("File not found");
            }

            return Ok(videoInfo);
        }

        /// <summary>
        /// Deletes a file from blob storage. Admin only.
        /// </summary>
        /// <param name="fileName">Name of the file to delete</param>
        /// <returns>True if the file was successfully deleted, false otherwise</returns>
        [HttpDelete("{fileName}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest();
            }
            
            return await _storageService.DeleteFileAsync(fileName);
        }
    }
}