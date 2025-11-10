using EdoSign.Lab_3.Data;
using EdoSign.Lab_3.Models.Entities;
using EdoSign.Lab_3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdoSign.Lab_3.Controllers;

[Authorize]
public class SubroutinesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly CryptoService _crypto;
    private readonly IWebHostEnvironment _env;

    public SubroutinesController(ApplicationDbContext db, CryptoService crypto, IWebHostEnvironment env)
    {
        _db = db;
        _crypto = crypto;
        _env = env;
    }

    // === 1. Завантаження файлу ===
    [HttpGet]
    public IActionResult Upload() => View();

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.Message = "Файл не вибрано!";
            return View();
        }

        var folder = Path.Combine(_env.WebRootPath, "storage");
        Directory.CreateDirectory(folder);

        var id = Guid.NewGuid();
        var path = Path.Combine(folder, id + Path.GetExtension(file.FileName));

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var entity = new FileEntity
        {
            Id = id,
            FileName = file.FileName,
            FilePath = path
        };

        _db.Files.Add(entity);
        await _db.SaveChangesAsync();

        ViewBag.FileId = id;
        return View();
    }

    // === 2. Підпис файлу (автоматично генерує ключ) ===
    [HttpGet]
    public IActionResult SignFile() => View();

    [HttpPost]
    public async Task<IActionResult> SignFile(Guid fileId)
    {
        var file = await _db.Files.FindAsync(fileId);
        if (file == null)
        {
            ViewBag.Message = "Файл не знайдено.";
            return View();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
        var keys = _crypto.GenerateRsaKeyPair();

        var signature = _crypto.SignToBase64(bytes, keys.privatePem);

        file.SignatureBase64 = signature;
        file.PublicKeyPem = keys.publicPem;
        await _db.SaveChangesAsync();

        ViewBag.Message = "✅ Файл успішно підписано.";
        ViewBag.Signature = signature;
        ViewBag.PublicKey = keys.publicPem;

        return View();
    }

    // === 3. Перевірка підпису ===
    [HttpGet]
    public IActionResult VerifyFile() => View();

    [HttpPost]
    public async Task<IActionResult> VerifyFile(Guid fileId)
    {
        var file = await _db.Files.FindAsync(fileId);
        if (file == null)
        {
            ViewBag.Message = "Файл не знайдено.";
            return View();
        }

        if (file.SignatureBase64 == null || file.PublicKeyPem == null)
        {
            ViewBag.Message = "Файл ще не підписано!";
            return View();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
        bool isValid = _crypto.VerifySignature(bytes, file.SignatureBase64, file.PublicKeyPem);

        ViewBag.Message = isValid ? "✅ Підпис дійсний." : "❌ Підпис недійсний.";
        return View();
    }
}
