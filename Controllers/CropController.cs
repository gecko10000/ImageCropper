using Microsoft.AspNetCore.Mvc;
using System.Text;
using ImageMagick;

namespace ImageCropper.Controllers;

[ApiController]
[Route("crop")]
public class FileController : ControllerBase
{

  static FileController()
  {
    DeleteAll();
  }

  private static readonly string DATA_DIR = "data";

  private void MakeStorage()
  {
    new DirectoryInfo(DATA_DIR).Create();
  }

  private bool MakeStorage(Guid guid)
  {
    DirectoryInfo dir = new DirectoryInfo(DATA_DIR + "/" + guid);
    bool alreadyExists = dir.Exists;
    dir.Create();
    return !alreadyExists; // true if created, false if already existed
  }

  private bool Write(Guid guid, MagickImage i, string fileName)
  {
    if (Retrieve(guid) is not null)
    {
      return false;
    }
    i.Write(DATA_DIR + "/" + guid + "/" + fileName);
    return true;
  }

  private FileInfo? Retrieve(Guid guid)
  {
    DirectoryInfo dir = new DirectoryInfo(DATA_DIR + "/" + guid);
    if (!dir.Exists)
    {
      return null;
    }
    FileInfo[] files = dir.GetFiles();
    return files.Length == 0 ? null : files[0];
  }

  private void Delete(Guid guid)
  {
    DirectoryInfo dir = new DirectoryInfo(DATA_DIR + "/" + guid);
    dir.Delete(true);
  }

  public static void DeleteAll()
  {
    foreach (DirectoryInfo dir in new DirectoryInfo(DATA_DIR).EnumerateDirectories())
    {
      dir.Delete(true);
    }
  }

  [HttpPost]
  public IActionResult Upload([FromForm] List<IFormFile> files)
  {
    MakeStorage();
    List<Guid> ids = new List<Guid>();
    foreach (IFormFile file in files)
    {
      MagickImage i;
      try
      {
        using Stream s = file.OpenReadStream();
        i = new MagickImage(s);
      } catch (MagickException){ continue; }
      Guid guid;
      do {
        guid = Guid.NewGuid();
      } while (Retrieve(guid) is not null);
      MakeStorage(guid);
      i.Crop(i.Width, i.Height - 20);
      if (Write(guid, i, file.FileName))
      {
        ids.Add(guid);
        Task.Delay(1000 * 60 * 10).ContinueWith(t => {
          Delete(guid);
        });
      }
    }
    // a whole json library, just for an array?
    StringBuilder json = new StringBuilder("[");
    for (int i = 0; i < ids.Count; i++)
    {
      json.Append("\"" + ids[i] + "\"");
      if (i != ids.Count - 1)
      {
        json.Append(",");
      }
    }
    json.Append("]");
    return Ok(json.ToString());
  }

  [HttpGet("{guid}")]
  public IActionResult Get(string guid)
  {
    FileInfo? file = Retrieve(Guid.Parse(guid));
    if (file is null)
    {
      return BadRequest("File not found.");
    }
    Stream s = file.OpenRead(); // disposed by return value
    return File(s, "application/octet-stream", file.Name);
  }
}