using AlgoRhythm.Shared.Models.Courses;
using Microsoft.AspNetCore.Mvc;
using AlgoRhythm.Attributes;
using Microsoft.AspNetCore.Components.Web;
using Azure;
using AlgoRhythm.Services.Interfaces;
using System.Text.Encodings.Web;
using System.Web;

namespace AlgoRhythm.Controllers
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
        /// Uploads a file to blob (or azurite in dev)
        /// </summary>
        [HttpPost]
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
        /// Video preview for debugging, not visible in swagger (it does not support streaming)
        /// url should be opened directly in browser to preview video
        /// </summary>
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
            ");
        }

        /// <summary>
        /// Endpoint for retrieving a file stream from blob
        /// </summary>
        /// <param name="fileName">Name of a file inside blob</param>
        [HttpGet("get_file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFile([FromQuery] string fileName)
        {
            fileName = fileName.Trim('"');

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("Path is required");
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
        /// Endpoint for retrieving video metadata needed for streaming
        /// </summary>
        /// <param name="fileName">Path to a file</param>
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
        /// Deletes a file from blob
        /// </summary>
        /// <param name="fileName">blob fileName</param>
        /// <returns>true if the blob was succesfully deleted, false otherwise</returns>
        [HttpDelete("{fileName}")]
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