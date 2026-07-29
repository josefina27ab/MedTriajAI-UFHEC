using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedTriajAI.Services;

/// <summary>
/// Cliente HTTP para comunicarse con la API local de Ollama
/// </summary>
public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private bool _ollamaAvailable = false;
    private string _currentModel = "llama3.2";

    public string CurrentModel => _currentModel;
    public bool IsAvailable => _ollamaAvailable;

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    /// <summary>
    /// Verifica si el servidor Ollama está disponible y lista los modelos
    /// </summary>
    public async Task<(bool available, List<string> models)> CheckHealthAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/api/tags");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OllamaTagsResponse>(json, JsonOptions);
                var models = data?.Models?.Select(m => m.Name).ToList() ?? [];
                _ollamaAvailable = true;
                if (models.Count > 0) _currentModel = models[0];
                return (true, models);
            }
        }
        catch { /* Ollama no disponible */ }
        _ollamaAvailable = false;
        return (false, []);
    }

    public void SetModel(string model) => _currentModel = model;

    /// <summary>
    /// Genera una respuesta usando el modelo local de Ollama
    /// </summary>
    public async Task<OllamaResult> GenerateAsync(string prompt, string? systemPrompt = null)
    {
        if (!_ollamaAvailable)
            return SimulatedResponse(prompt);

        try
        {
            var request = new OllamaChatRequest
            {
                Model = _currentModel,
                Messages = BuildMessages(systemPrompt, prompt),
                Stream = false
            };

            var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/chat", request, JsonOptions);
            if (!response.IsSuccessStatusCode)
                return SimulatedResponse(prompt);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaChatResponse>(json, JsonOptions);
            return new OllamaResult
            {
                Content = result?.Message?.Content ?? "(Sin respuesta del modelo)",
                IsSimulated = false,
                Model = _currentModel
            };
        }
        catch (Exception ex)
        {
            return new OllamaResult
            {
                Content = $"[Error de conexión con Ollama: {ex.Message}]\n\n" + SimulatedResponse(prompt).Content,
                IsSimulated = true,
                Model = "SIMULADO"
            };
        }
    }

    /// <summary>
    /// Genera una respuesta en modo streaming (con callback por token)
    /// </summary>
    public async Task<OllamaResult> GenerateStreamAsync(string prompt, string? systemPrompt = null, Action<string>? onToken = null)
    {
        if (!_ollamaAvailable)
        {
            var sim = SimulatedResponse(prompt);
            onToken?.Invoke(sim.Content);
            return sim;
        }

        var fullContent = new System.Text.StringBuilder();
        try
        {
            var request = new OllamaChatRequest
            {
                Model = _currentModel,
                Messages = BuildMessages(systemPrompt, prompt),
                Stream = true
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
                if (chunk?.Message?.Content is string token && !string.IsNullOrEmpty(token))
                {
                    fullContent.Append(token);
                    onToken?.Invoke(token);
                }
                if (chunk?.Done == true) break;
            }
        }
        catch (Exception ex)
        {
            var sim = SimulatedResponse(prompt);
            fullContent.Append($"\n[Streaming error: {ex.Message}]\n{sim.Content}");
            onToken?.Invoke(fullContent.ToString());
            return new OllamaResult { Content = fullContent.ToString(), IsSimulated = true, Model = "SIMULADO" };
        }

        return new OllamaResult { Content = fullContent.ToString(), IsSimulated = false, Model = _currentModel };
    }

    private static List<OllamaMessage> BuildMessages(string? systemPrompt, string userPrompt)
    {
        var messages = new List<OllamaMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new OllamaMessage { Role = "system", Content = systemPrompt });
        messages.Add(new OllamaMessage { Role = "user", Content = userPrompt });
        return messages;
    }

    /// <summary>
    /// Respuesta simulada inteligente cuando Ollama no está disponible
    /// </summary>
    private static OllamaResult SimulatedResponse(string prompt)
    {
        var lowerPrompt = prompt.ToLowerInvariant();
        string content;

        if (lowerPrompt.Contains("fiebre") || lowerPrompt.Contains("dolor") || lowerPrompt.Contains("paciente"))
        {
            content = """
            [MODO DEMO — Ollama no disponible. Instale Ollama y ejecute: ollama pull llama3.2]

            ## Análisis de Triaje Médico Preliminar

            **Paso 1 — Extracción de Síntomas Reportados:**
            Identifico los síntomas mencionados en la descripción del caso. Se detectan síntomas compatibles con un cuadro clínico agudo que requiere evaluación.

            **Paso 2 — Evaluación de Signos Vitales:**
            Sin signos vitales objetivos disponibles en este momento. Se recomienda medición inmediata de: frecuencia cardíaca, presión arterial, temperatura, saturación de oxígeno y frecuencia respiratoria.

            **Paso 3 — Clasificación de Severidad (Escala ESI):**
            Basado en los síntomas descritos, clasificación preliminar: **ESI Nivel 3 (Urgente)** — Requiere atención médica en los próximos 30 minutos.

            **Paso 4 — Recomendación Clínica Preliminar:**
            • Derivar al área de urgencias para evaluación médica completa.
            • Monitoreo continuo de signos vitales cada 15 minutos.
            • Aplicar protocolo de hidratación si hay signos de deshidratación.
            • ⚠️ Este análisis es PRELIMINAR y NO reemplaza el criterio clínico del médico tratante.
            """;
        }
        else
        {
            content = """
            [MODO DEMO — Ollama no disponible. Instale Ollama y ejecute: ollama pull llama3.2]

            ## Respuesta del Sistema MedTriaj AI

            Soy MedTriaj AI, un asistente de triaje médico preliminar basado en inteligencia artificial local.
            Mi función es apoyar al personal de salud en la clasificación inicial de pacientes, aplicando el sistema
            de triaje ESI (Emergency Severity Index) como guía estructural.

            Para obtener un análisis real, asegúrese de:
            1. Instalar Ollama: https://ollama.com
            2. Descargar un modelo: `ollama pull llama3.2`
            3. Iniciar el servicio: `ollama serve`

            ⚠️ Este sistema es una herramienta de APOYO PRELIMINAR. El criterio clínico del médico profesional siempre prevalece.
            """;
        }

        return new OllamaResult { Content = content, IsSimulated = true, Model = "SIMULADO (Demo)" };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

// ─── Modelos de datos ────────────────────────────────────────────────────────

public record OllamaResult
{
    public string Content { get; init; } = "";
    public bool IsSimulated { get; init; }
    public string Model { get; init; } = "";
}

public class OllamaChatRequest
{
    public string Model { get; set; } = "";
    public List<OllamaMessage> Messages { get; set; } = [];
    public bool Stream { get; set; } = false;
}

public class OllamaMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class OllamaChatResponse
{
    public OllamaMessage? Message { get; set; }
    public bool Done { get; set; }
}

public class OllamaTagsResponse
{
    public List<OllamaModelInfo>? Models { get; set; }
}

public class OllamaModelInfo
{
    public string Name { get; set; } = "";
}
