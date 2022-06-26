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
    DirectoryInfo info = new DirectoryInfo(DATA_DIR);
    if (!info.Exists) return;
    foreach (DirectoryInfo dir in info.EnumerateDirectories())
    {
      dir.Delete(true);
    }
  }

  private static readonly Dictionary<string, Gravity> gravities = new Dictionary<string, Gravity>() {
    {"top", Gravity.South},
    {"left", Gravity.East},
    {"bottom", Gravity.North},
    {"right", Gravity.West}
  };

  private Gravity GetGravity(string side)
  {
    side = side.ToLower();
    return gravities.GetValueOrDefault(side, Gravity.South);
  }

  private bool IsSide(Gravity g)
  {
    return g == Gravity.East || g == Gravity.West;
  }

  [HttpPost]
  public IActionResult Upload([FromForm] List<IFormFile> files, [FromForm] string side, [FromForm] int pixels)
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
      Gravity g = GetGravity(side);
      if (IsSide(g)) {
        i.Crop(i.Width - pixels, i.Height, g);
      } else {
        i.Crop(i.Width, i.Height - pixels, g);
      }
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
    MakeStorage();
    FileInfo? file = Retrieve(Guid.Parse(guid));
    if (file is null)
    {
      return BadRequest("File not found.");
    }
    Stream s = file.OpenRead(); // disposed by return value
    return File(s, "application/octet-stream", file.Name);
  }
}