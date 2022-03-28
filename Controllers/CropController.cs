using Microsoft.AspNetCore.Mvc;
using ImageMagick;

namespace ImageCropper.Controllers;

[ApiController]
[Route("crop")]
public class FileController : ControllerBase
{

  private static int index = 0;

  [HttpPost]
  public IActionResult Upload([FromForm] IFormFile files)
  {
    MagickImage i = new MagickImage(files.OpenReadStream());
    i.Crop(i.Width, i.Height - 20);
    Stream stream = new MemoryStream();
    i.Write(stream);
    stream.Seek(0, SeekOrigin.Begin);
    return File(stream, "application/octet-stream", "test.png");
  }
}