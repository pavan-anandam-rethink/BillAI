using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace AiRulesEngine.Web.Services
{
    public class FileTextExtractor : IFileTextExtractor
    {
        public async Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            if (extension == ".pdf" || file.ContentType == "application/pdf")
            {
                return ExtractPdfText(stream);
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private static string ExtractPdfText(Stream stream)
        {
            var builder = new StringBuilder();
            using var document = PdfDocument.Open(stream);

            foreach (var page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }

            return builder.ToString();
        }
    }
}
