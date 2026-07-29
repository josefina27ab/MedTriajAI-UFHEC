using MedTriajAI.Services;
using MedTriajAI.UI;

// ─── Configuración de la aplicación ──────────────────────────────────────────
var protocolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Protocols");

var ollamaClient = new OllamaClient("http://localhost:11434");
var docLoader = new DocumentLoader(protocolsDir);
var promptEngine = new PromptEngineeringEngine(docLoader);
var dashboard = new ConsoleDashboard(ollamaClient, promptEngine, docLoader);

// ─── Arranque ─────────────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Title = "MedTriaj AI — Sistema de Triaje Médico | INF-4350 UFHEC";

await dashboard.RunAsync();
