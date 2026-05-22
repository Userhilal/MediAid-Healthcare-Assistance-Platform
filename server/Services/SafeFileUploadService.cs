using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MediAid.Services;

public sealed record SafeFileUploadResult(
    bool IsValid,
    string? ErrorMessage = null,
    string? RelativeUrl = null,
    string? StoredFileName = null,
    string? OriginalFileName = null,
    string? ContentType = null,
    long Size = 0
);

public static class SafeFileUploadService
{
    public const long ChatMaxBytes = 10 * 1024 * 1024;
    public const long ProofMaxBytes = 8 * 1024 * 1024;

    public static readonly string[] ChatAllowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".doc", ".docx"
    };

    public static readonly string[] ProofAllowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    public static async Task<SafeFileUploadResult> SaveAsync(
        IFormFile file,
        string subFolder,
        string[] allowedExtensions,
        long maxBytes)
    {
        if (file == null || file.Length == 0)
        {
            return new SafeFileUploadResult(false, "Aucun fichier fourni.");
        }

        if (file.Length > maxBytes)
        {
            return new SafeFileUploadResult(false, $"Le fichier dépasse la taille maximale autorisée de {maxBytes / 1024 / 1024} MB.");
        }

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        var allowed = new HashSet<string>(allowedExtensions, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(extension) || !allowed.Contains(extension))
        {
            return new SafeFileUploadResult(false, "Type de fichier non autorisé.");
        }

        var safeBaseName = Path.GetFileNameWithoutExtension(originalFileName);
        safeBaseName = Regex.Replace(safeBaseName, @"[^a-zA-Z0-9\-_]+", "_").Trim('_');

        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = "file";
        }

        if (safeBaseName.Length > 80)
        {
            safeBaseName = safeBaseName[..80];
        }

        var storedFileName = $"{Guid.NewGuid():N}_{safeBaseName}{extension}";
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subFolder);

        Directory.CreateDirectory(uploadsFolder);

        var fullPath = Path.Combine(uploadsFolder, storedFileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        var relativeUrl = $"/uploads/{subFolder}/{storedFileName}";

        return new SafeFileUploadResult(
            true,
            RelativeUrl: relativeUrl,
            StoredFileName: storedFileName,
            OriginalFileName: originalFileName,
            ContentType: file.ContentType,
            Size: file.Length
        );
    }
}

