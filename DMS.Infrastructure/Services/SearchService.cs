using System.Net;
using System.Text.RegularExpressions;
using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Feature 8, phase 1: one search box over manuals (number, name and the text of the latest version), circulars and
/// alerts, other documents and certificates, and suggestions. Plain SQL LIKE matching, every word must match.
/// Phase 2 (AI assistant over the same sources, through a cloud provider) plugs in behind this same service.
/// </summary>
public sealed partial class SearchService : ISearchService
{
    public static readonly string[] Types = { "Manual", "Circular", "Document", "Suggestion" };
    private const int PerType = 50;
    private readonly SqlDb _db;

    public SearchService(SqlDb db) => _db = db;

    public async Task<List<SearchHit>> SearchAsync(string query, string? types, OfficeUser user)
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => w.Length > 100 ? w[..100] : w).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
        if (words.Count == 0 || words.Sum(w => w.Length) < 2) return new List<SearchHit>();

        var wanted = string.IsNullOrWhiteSpace(types)
            ? Types.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ps = new List<(string, object?)> { ("@me", user.UserId), ("@dept", user.Dept ?? ""), ("@first", words[0]) };
        for (var i = 0; i < words.Count; i++) ps.Add(($"@w{i}", "%" + Escape(words[i]) + "%"));
        string All(string haystack) => string.Join(" AND ", words.Select((_, i) => $"{haystack} LIKE @w{i} ESCAPE '\\'"));

        var hits = new List<SearchHit>();

        if (wanted.Contains("Manual"))
        {
            const string hay = "(m.ManualNo + ' ' + ISNULL(m.ManualName, '') + ' ' + ISNULL(v.TextContents, ''))";
            hits.AddRange(await _db.QueryAsync<SearchHit>($@"
SELECT TOP ({PerType}) 'Manual' AS ItemType, m.DM_ManualID AS ItemID, m.ManualNo AS Number, m.ManualName AS Title,
       SUBSTRING(v.TextContents, CASE WHEN CHARINDEX(@first, v.TextContents) > 150 THEN CHARINDEX(@first, v.TextContents) - 150 ELSE 1 END, 450) AS Snippet,
       m.VesselID, COALESCE(v.ReleasedDate, v.CreatedDate) AS Date
  FROM dbo.DM_Manual m
  OUTER APPLY (SELECT TOP (1) x.TextContents, x.ReleasedDate, x.CreatedDate FROM dbo.DM_ManualVersion x
                WHERE x.DM_ManualID = m.DM_ManualID ORDER BY x.DM_ManualVersionID DESC) v
 WHERE ISNULL(m.IsDeleted, 0) <> 1 AND {All(hay)}
 ORDER BY CASE WHEN m.ManualNo LIKE @w0 ESCAPE '\' OR m.ManualName LIKE @w0 ESCAPE '\' THEN 0 ELSE 1 END, m.ManualNo", ps.ToArray()));
        }

        if (wanted.Contains("Circular"))
        {
            const string hay = "(ISNULL(c.CIR_Number, '') + ' ' + ISNULL(c.Title, '') + ' ' + ISNULL(c.Reference, '') + ' ' + ISNULL(c.Remarks, ''))";
            hits.AddRange(await _db.QueryAsync<SearchHit>($@"
SELECT TOP ({PerType}) CASE WHEN c.CircularType = 'Alert' THEN 'Alert' ELSE 'Circular' END AS ItemType, c.CIR_MasterID AS ItemID,
       c.CIR_Number AS Number, c.Title, LEFT(c.Remarks, 400) AS Snippet, c.VesselID, COALESCE(c.ReleasedDate, c.DateIssued, c.CreatedDate) AS Date
  FROM dbo.CIR_Master c
 WHERE ISNULL(c.IsDeleted, 0) <> 1 AND {All(hay)}
 ORDER BY COALESCE(c.ReleasedDate, c.DateIssued, c.CreatedDate) DESC", ps.ToArray()));
        }

        if (wanted.Contains("Document"))
        {
            const string hay = "(d.DocumentNumber + ' ' + d.DocumentTitle + ' ' + ISNULL(d.Reference, '') + ' ' + ISNULL(d.Comments, '') + ' ' + ISNULL(d.CertificateNo, '') + ' ' + ISNULL(d.IssuedBy, '') + ' ' + ISNULL(t.DocumentType, ''))";
            hits.AddRange(await _db.QueryAsync<SearchHit>($@"
SELECT TOP ({PerType}) CASE WHEN t.IsCertificate = 1 THEN 'Certificate' ELSE 'Document' END AS ItemType, d.OtherDocumentMasterID AS ItemID,
       d.DocumentNumber AS Number, d.DocumentTitle AS Title, LEFT(ISNULL(t.DocumentType + ': ', '') + ISNULL(d.Comments, ''), 400) AS Snippet,
       d.VesselID, COALESCE(d.UploadedOn, d.CreatedDate) AS Date
  FROM dbo.OtherDocumentMaster d
  LEFT JOIN dbo.OtherDocument_Type t ON t.OtherDocument_TypeID = d.OtherDocument_TypeID
 WHERE ISNULL(d.IsDeleted, 0) <> 1 AND {All(hay)}
 ORDER BY COALESCE(d.UploadedOn, d.CreatedDate) DESC", ps.ToArray()));
        }

        if (wanted.Contains("Suggestion"))
        {
            const string hay = "(ISNULL(s.SuggestionNo, '') + ' ' + s.Title + ' ' + ISNULL(s.Description, '') + ' ' + ISNULL(s.QuotedText, ''))";
            hits.AddRange(await _db.QueryAsync<SearchHit>($@"
SELECT TOP ({PerType}) 'Suggestion' AS ItemType, s.SUG_MasterID AS ItemID, s.SuggestionNo AS Number, s.Title,
       LEFT(s.Status + ' - ' + ISNULL(s.Description, ''), 400) AS Snippet, s.VesselID, s.SubmittedOn AS Date
  FROM dbo.SUG_Master s
 WHERE s.IsDeleted = 0 AND {All(hay)}
 ORDER BY s.SubmittedOn DESC", ps.ToArray()));
        }

        foreach (var h in hits) h.Snippet = Clean(h.Snippet);
        return hits;
    }

    /// <summary>LIKE wildcards in the user's words are matched literally.</summary>
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    private static string? Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = Tags().Replace(html, " ");
        text = WebUtility.HtmlDecode(text);
        text = Spaces().Replace(text, " ").Trim();
        // A cut tag or word at either end of the window
        var gt = text.IndexOf('>');
        if (gt >= 0 && gt < 80 && text.IndexOf('<') is < 0 or > 80) text = text[(gt + 1)..].TrimStart();
        var lt = text.LastIndexOf('<');
        if (lt >= 0 && text.Length - lt < 80) text = text[..lt].TrimEnd();
        return text.Length <= 300 ? text : text[..300] + "…";
    }

    [GeneratedRegex("<[^>]*>")] private static partial Regex Tags();
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
}
