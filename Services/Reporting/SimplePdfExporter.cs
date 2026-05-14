using System.Globalization;
using System.Text;

namespace HRM.Web.Services.Reporting;

public static class SimplePdfExporter
{
    public static byte[] CreatePdf(string title, IEnumerable<string> lines)
    {
        lines ??= Array.Empty<string>();
        var allLines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var safeTitle = EscapePdfText(ToAscii(title));
        var safeLines = allLines
            .Select(ToAscii)
            .Take(40)
            .ToList();

        if (allLines.Count > safeLines.Count)
        {
            safeLines.Add("... more lines are omitted in this starter PDF report");
        }

        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 11 Tf");
        contentBuilder.AppendLine("50 800 Td");
        contentBuilder.AppendLine($"({safeTitle}) Tj");
        contentBuilder.AppendLine("0 -20 Td");

        foreach (var line in safeLines)
        {
            contentBuilder.AppendLine($"({EscapePdfText(line)}) Tj");
            contentBuilder.AppendLine("0 -15 Td");
        }

        contentBuilder.AppendLine("ET");

        var contentStream = contentBuilder.ToString();
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{contentStream}endstream\nendobj\n"
        };

        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long>();
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            writer.Write(obj);
            writer.Flush();
        }

        var xrefStart = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            writer.Write($"{offset:D10} 00000 n \n");
        }

        writer.Write("trailer\n");
        writer.Write($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        writer.Write("startxref\n");
        writer.Write($"{xrefStart}\n");
        writer.Write("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static string ToAscii(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (ch < 32)
            {
                continue;
            }

            builder.Append(ch > 126 ? '?' : ch);
        }

        return builder.ToString().Trim();
    }

    private static string EscapePdfText(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
