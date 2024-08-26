using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UploadFileApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly FileService fileService;

        public FilesController(
            FileService fileService)
        {
            this.fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> ListAllBlobs()
        {
            var res = await fileService.ListAsync();
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            await fileService.UploadAsync(file);
            return Ok();
        }

        [HttpGet]
        [Route("fileName")]
        public async Task<IActionResult> Download(string fileName)
        {
            var res = await fileService.DownloadAsync(fileName);
            return File(res.Content, res.ContentType, res.Name);
        }

        [HttpDelete]
        [Route("fileName")]
        public async Task<IActionResult> Delete(string fileName)
        {
            var res = await fileService.DeleteAsync(fileName);
            return Ok(res);
        }
    }
}
