namespace MedTriajAI.Services;

/// <summary>
/// Servicio de carga e ingesta de documentos de protocolo clínico para RAG
/// </summary>
public class DocumentLoader
{
    private readonly string _protocolsDir;

    public DocumentLoader(string protocolsDir)
    {
        _protocolsDir = protocolsDir;
    }

    /// <summary>
    /// Carga fragmentos relevantes de protocolos clínicos basados en el caso ingresado
    /// </summary>
    public async Task<List<string>> LoadProtocolContextAsync(string caseInput)
    {
        var results = new List<string>();
        if (!Directory.Exists(_protocolsDir))
            return results;

        var files = Directory.GetFiles(_protocolsDir, "*.txt")
            .Concat(Directory.GetFiles(_protocolsDir, "*.md"))
            .Concat(Directory.GetFiles(_protocolsDir, "*.json"));

        var keywords = ExtractKeywords(caseInput);

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var relevantChunks = ExtractRelevantChunks(content, keywords, Path.GetFileName(file));
            results.AddRange(relevantChunks);
        }

        // Limitar el contexto para no exceder el límite del modelo
        return results.Take(3).ToList();
    }

    /// <summary>
    /// Lista todos los documentos de protocolo disponibles
    /// </summary>
    public List<string> ListAvailableDocuments()
    {
        if (!Directory.Exists(_protocolsDir))
            return [];

        return Directory.GetFiles(_protocolsDir, "*.*")
            .Where(f => f.EndsWith(".txt") || f.EndsWith(".md") || f.EndsWith(".json"))
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }

    /// <summary>
    /// Carga el contenido completo de un documento de protocolo específico
    /// </summary>
    public async Task<string?> LoadDocumentAsync(string fileName)
    {
        var path = Path.Combine(_protocolsDir, fileName);
        if (!File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path);
    }

    private static List<string> ExtractKeywords(string input)
    {
        var medicalKeywords = new[] {
            "fiebre", "dolor", "torácico", "abdominal", "cabeza", "trauma",
            "fractura", "hemorragia", "sangrado", "disnea", "respiratorio",
            "cardíaco", "neurológico", "convulsión", "pérdida de conciencia",
            "hipotensión", "taquicardia", "bradicardia", "alérgico", "anafilaxia",
            "quemadura", "intoxicación", "embarazo", "obstétrico", "pediátrico",
            "geriatrico", "mental", "psiquiátrico"
        };

        var inputLower = input.ToLowerInvariant();
        return medicalKeywords.Where(k => inputLower.Contains(k)).ToList();
    }

    private static List<string> ExtractRelevantChunks(string content, List<string> keywords, string fileName)
    {
        var chunks = new List<string>();
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var paragraph in paragraphs)
        {
            var paraLower = paragraph.ToLowerInvariant();
            bool isRelevant = keywords.Any(k => paraLower.Contains(k));

            if (isRelevant && paragraph.Trim().Length > 50)
            {
                chunks.Add($"[Fuente: {fileName}]\n{paragraph.Trim()}");
            }
        }

        // Si no hay coincidencias específicas, incluir el inicio del documento como contexto general
        if (chunks.Count == 0 && paragraphs.Length > 0)
        {
            chunks.Add($"[Fuente: {fileName} — Contexto general]\n{paragraphs[0].Trim()}");
        }

        return chunks.Take(2).ToList();
    }
}
