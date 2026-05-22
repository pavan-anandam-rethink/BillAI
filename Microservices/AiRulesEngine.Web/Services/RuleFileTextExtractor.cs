using System.Text;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;

namespace AiRulesEngine.Web.Services;

public sealed class RuleFileTextExtractor : IFileTextExtractor
{
    public async Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        var extension = Path.GetExtension(file.FileName);
        await using var stream = file.OpenReadStream();

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }

            return builder.ToString();
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
