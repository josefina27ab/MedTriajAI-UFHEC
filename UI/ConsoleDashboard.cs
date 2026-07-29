using Spectre.Console;
using MedTriajAI.Services;

namespace MedTriajAI.UI;

/// <summary>
/// Interfaz de consola interactiva y enriquecida para MedTriaj AI
/// </summary>
public class ConsoleDashboard
{
    private readonly OllamaClient _ollama;
    private readonly PromptEngineeringEngine _promptEngine;
    private readonly DocumentLoader _docLoader;
    private readonly List<ConsultaRecord> _historial = [];

    public ConsoleDashboard(OllamaClient ollama, PromptEngineeringEngine promptEngine, DocumentLoader docLoader)
    {
        _ollama = ollama;
        _promptEngine = promptEngine;
        _docLoader = docLoader;
    }

    public async Task RunAsync()
    {
        ShowBanner();
        await CheckOllamaStatusAsync();

        bool running = true;
        while (running)
        {
            ShowMainMenu();
            var choice = Console.ReadLine()?.Trim() ?? "";

            switch (choice)
            {
                case "1": await RunTriageConsultaAsync(); break;
                case "2": await SeleccionarTecnicaYConsultarAsync(); break;
                case "3": ShowHistorial(); break;
                case "4": await ShowDocumentosProtocoloAsync(); break;
                case "5": ShowAyudaPromptEngineering(); break;
                case "6": await ExportarConsultaAsync(); break;
                case "0": running = false; break;
                default:
                    AnsiConsole.MarkupLine("[red]Opción no válida. Intente nuevamente.[/]");
                    break;
            }
        }

        AnsiConsole.MarkupLine("\n[bold green]¡Gracias por usar MedTriaj AI! Recuerde: siempre consulte con el médico tratante.[/]\n");
    }

    private void ShowBanner()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("MedTriaj AI").Color(Color.CornflowerBlue));
        AnsiConsole.Write(
            new Panel(
                "[bold white]Sistema Inteligente de Triaje Médico Preliminar[/]\n" +
                "[dim]Powered by Ollama + C# | INF-4350 UFHEC | Tecnologías de IA[/]\n" +
                "[yellow]⚠️  ADVERTENCIA: Este sistema es de APOYO PRELIMINAR exclusivamente.[/]\n" +
                "[yellow]   El criterio clínico del médico profesional SIEMPRE prevalece.[/]"
            )
            .Border(BoxBorder.Double)
            .BorderColor(Color.CornflowerBlue)
            .Header("[bold blue] MedTriaj AI v1.0 [/]")
        );
        Console.WriteLine();
    }

    private async Task CheckOllamaStatusAsync()
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Conectando con Ollama...", async ctx =>
            {
                var (available, models) = await _ollama.CheckHealthAsync();
                if (available)
                {
                    ctx.Status("¡Conexión establecida!");
                    AnsiConsole.MarkupLine($"[green]✓ Ollama disponible en localhost:11434[/]");
                    AnsiConsole.MarkupLine($"[green]✓ Modelos disponibles: {string.Join(", ", models)}[/]");
                    AnsiConsole.MarkupLine($"[green]✓ Modelo activo: {_ollama.CurrentModel}[/]");

                    if (models.Count > 1)
                    {
                        var selected = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[cyan]Seleccione el modelo a utilizar:[/]")
                                .AddChoices(models));
                        _ollama.SetModel(selected);
                        AnsiConsole.MarkupLine($"[bold green]Modelo seleccionado: {selected}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠ Ollama no está disponible en localhost:11434[/]");
                    AnsiConsole.MarkupLine("[dim]El sistema funcionará en MODO DEMO con respuestas simuladas.[/]");
                    AnsiConsole.MarkupLine("[dim]Para activar el modelo real: instale Ollama y ejecute 'ollama pull llama3.2'[/]");
                }
            });
        Console.WriteLine();
    }

    private void ShowMainMenu()
    {
        var statusIcon = _ollama.IsAvailable ? "[green]● ONLINE[/]" : "[yellow]● DEMO[/]";
        var modelInfo = _ollama.IsAvailable ? $"Modelo: [cyan]{_ollama.CurrentModel}[/]" : "[dim]Modo Simulación[/]";

        AnsiConsole.Write(new Rule($"[bold blue]MENÚ PRINCIPAL[/]  {statusIcon}  {modelInfo}").RuleStyle("blue dim"));
        Console.WriteLine();
        AnsiConsole.MarkupLine("  [cyan bold]1.[/] Nueva Consulta de Triaje (técnica automática)");
        AnsiConsole.MarkupLine("  [cyan bold]2.[/] Consulta con técnica de Prompt Engineering específica");
        AnsiConsole.MarkupLine("  [cyan bold]3.[/] Ver historial de consultas");
        AnsiConsole.MarkupLine("  [cyan bold]4.[/] Gestionar documentos de protocolo clínico");
        AnsiConsole.MarkupLine("  [cyan bold]5.[/] Información sobre técnicas de Prompt Engineering");
        AnsiConsole.MarkupLine("  [cyan bold]6.[/] Exportar última consulta a archivo");
        AnsiConsole.MarkupLine("  [red bold]0.[/] Salir");
        Console.WriteLine();
        AnsiConsole.Markup("[bold]Seleccione una opción: [/]");
    }

    private async Task RunTriageConsultaAsync()
    {
        AnsiConsole.Write(new Rule("[bold green]NUEVA CONSULTA DE TRIAJE[/]").RuleStyle("green"));
        Console.WriteLine();
        AnsiConsole.MarkupLine("[dim]Ingrese la descripción clínica del paciente (síntomas, edad, sexo, signos vitales si los tiene).[/]");
        AnsiConsole.MarkupLine("[dim]Ejemplo: 'Paciente femenina, 45 años, dolor abdominal agudo en fosa iliaca derecha, fiebre 38.9°C, náuseas'[/]");
        Console.WriteLine();
        AnsiConsole.Markup("[bold white]Descripción del caso: [/]");
        var caseInput = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(caseInput))
        {
            AnsiConsole.MarkupLine("[red]Por favor ingrese una descripción del caso.[/]");
            return;
        }

        // Usar CoT como técnica por defecto en la consulta rápida
        var (prompt, techDesc) = _promptEngine.BuildChainOfThoughtPrompt(caseInput);
        await ExecuteAndShowResultAsync(caseInput, prompt, techDesc, "Chain-of-Thought");
    }

    private async Task SeleccionarTecnicaYConsultarAsync()
    {
        AnsiConsole.Write(new Rule("[bold cyan]SELECCIÓN DE TÉCNICA DE PROMPT ENGINEERING[/]").RuleStyle("cyan"));
        Console.WriteLine();

        var technique = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Seleccione la técnica de Prompt Engineering:[/]")
                .AddChoices([
                    "1. System Prompt Injection — Contexto institucional del sistema",
                    "2. Role Prompting — Enfermero de triaje especializado",
                    "3. Few-Shot Learning — 3 ejemplos de referencia",
                    "4. Chain-of-Thought — Razonamiento paso a paso",
                    "5. RAG — Recuperación de protocolos clínicos locales",
                    "6. COMBINADA — Todas las técnicas a máxima potencia"
                ])
        );

        Console.WriteLine();
        AnsiConsole.MarkupLine("[dim]Ingrese la descripción clínica del paciente:[/]");
        AnsiConsole.Markup("[bold white]Descripción del caso: [/]");
        var caseInput = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(caseInput))
        {
            AnsiConsole.MarkupLine("[red]Por favor ingrese una descripción del caso.[/]");
            return;
        }

        string prompt = "";
        string techDesc = "";
        string techName = "";

        if (technique.StartsWith("1"))
        {
            var r = _promptEngine.BuildSystemPromptOnly(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "System Prompt");
        }
        else if (technique.StartsWith("2"))
        {
            var r = _promptEngine.BuildRolePrompt(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "Role Prompting");
        }
        else if (technique.StartsWith("3"))
        {
            var r = _promptEngine.BuildFewShotPrompt(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "Few-Shot Learning");
        }
        else if (technique.StartsWith("4"))
        {
            var r = _promptEngine.BuildChainOfThoughtPrompt(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "Chain-of-Thought");
        }
        else if (technique.StartsWith("5"))
        {
            var r = await _promptEngine.BuildRAGPromptAsync(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "RAG");
        }
        else
        {
            var r = await _promptEngine.BuildCombinedPromptAsync(caseInput);
            (prompt, techDesc, techName) = (r.prompt, r.description, "COMBINADA");
        }

        await ExecuteAndShowResultAsync(caseInput, prompt, techDesc, techName);
    }

    private async Task ExecuteAndShowResultAsync(string caseInput, string prompt, string techDesc, string techName)
    {
        Console.WriteLine();

        // Mostrar información de la técnica usada
        var techPanel = new Panel($"[bold yellow]{techDesc}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Header("[yellow] Técnica Aplicada [/]");
        AnsiConsole.Write(techPanel);

        // Mostrar el prompt generado (colapsable)
        if (AnsiConsole.Confirm("[dim]¿Desea ver el prompt generado antes de enviarlo?[/]", defaultValue: false))
        {
            AnsiConsole.Write(
                new Panel(Markup.Escape(prompt))
                    .Border(BoxBorder.Ascii)
                    .BorderColor(Color.Grey)
                    .Header("[grey] Prompt Generado [/]")
            );
        }

        Console.WriteLine();
        OllamaResult result = null!;

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Procesando con IA local...[/]", maxValue: 100);
                task.IsIndeterminate = true;

                string systemPrompt = _promptEngine.GetSystemPrompt();
                result = await _ollama.GenerateAsync(prompt, systemPrompt);
                task.Value = 100;
            });

        // Determinar el nivel ESI de la respuesta
        var esiLevel = ExtractESILevel(result.Content);
        var (esiColor, esiLabel) = GetESIDisplay(esiLevel);

        // Mostrar resultado
        Console.WriteLine();
        AnsiConsole.Write(new Rule($"[bold {esiColor}]RESULTADO DE TRIAJE — {esiLabel}[/]").RuleStyle(esiColor));
        Console.WriteLine();

        if (result.IsSimulated)
            AnsiConsole.MarkupLine("[dim yellow]⚠ Respuesta generada en modo DEMO (Ollama no disponible)[/]");
        else
            AnsiConsole.MarkupLine($"[dim green]✓ Respuesta generada por: {result.Model}[/]");

        Console.WriteLine();
        AnsiConsole.MarkupLine(Markup.Escape(result.Content));

        // Guardar en historial
        var record = new ConsultaRecord
        {
            Timestamp = DateTime.Now,
            CasoDescripcion = caseInput,
            TecnicaUsada = techName,
            Respuesta = result.Content,
            ESILevel = esiLevel,
            FueSimulado = result.IsSimulated
        };
        _historial.Add(record);

        Console.WriteLine();
        AnsiConsole.MarkupLine($"[dim]✓ Consulta guardada en historial. Total de consultas: {_historial.Count}[/]");

        Console.WriteLine();
        AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private void ShowHistorial()
    {
        AnsiConsole.Write(new Rule("[bold blue]HISTORIAL DE CONSULTAS[/]").RuleStyle("blue"));

        if (_historial.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No hay consultas en el historial aún.[/]");
            Console.WriteLine();
            AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
            Console.ReadLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold]#[/]").Width(4))
            .AddColumn(new TableColumn("[bold]Hora[/]").Width(10))
            .AddColumn(new TableColumn("[bold]Técnica[/]").Width(20))
            .AddColumn(new TableColumn("[bold]Nivel ESI[/]").Width(12))
            .AddColumn(new TableColumn("[bold]Descripción del Caso[/]").Width(50));

        for (int i = 0; i < _historial.Count; i++)
        {
            var r = _historial[i];
            var (color, label) = GetESIDisplay(r.ESILevel);
            table.AddRow(
                $"[dim]{i + 1}[/]",
                $"[dim]{r.Timestamp:HH:mm:ss}[/]",
                $"[cyan]{r.TecnicaUsada}[/]",
                $"[{color}]{label}[/]",
                Markup.Escape(r.CasoDescripcion.Length > 60
                    ? r.CasoDescripcion[..60] + "..."
                    : r.CasoDescripcion)
            );
        }

        AnsiConsole.Write(table);
        Console.WriteLine();
        AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private async Task ShowDocumentosProtocoloAsync()
    {
        AnsiConsole.Write(new Rule("[bold magenta]DOCUMENTOS DE PROTOCOLO CLÍNICO[/]").RuleStyle("magenta"));

        var docs = _docLoader.ListAvailableDocuments();
        if (docs.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No se encontraron documentos en el directorio de protocolos.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓ {docs.Count} documento(s) disponible(s):[/]");
            foreach (var doc in docs)
                AnsiConsole.MarkupLine($"  [cyan]•[/] {doc}");

            Console.WriteLine();
            if (AnsiConsole.Confirm("¿Desea ver el contenido de algún documento?", defaultValue: false))
            {
                docs.Add("← Cancelar");
                var selected = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Seleccione el documento:")
                        .AddChoices(docs));

                if (selected != "← Cancelar")
                {
                    var content = await _docLoader.LoadDocumentAsync(selected);
                    if (content != null)
                    {
                        AnsiConsole.Write(
                            new Panel(Markup.Escape(content))
                                .Border(BoxBorder.Rounded)
                                .BorderColor(Color.Magenta1)
                                .Header($"[magenta] {selected} [/]")
                        );
                    }
                }
            }
        }

        Console.WriteLine();
        AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static void ShowAyudaPromptEngineering()
    {
        AnsiConsole.Write(new Rule("[bold yellow]TÉCNICAS DE PROMPT ENGINEERING[/]").RuleStyle("yellow"));
        Console.WriteLine();

        var techniques = new[]
        {
            ("[bold]1. System Prompt Injection[/]",
             "Inyecta instrucciones globales de comportamiento al modelo antes de cada consulta.\n   Define el rol, las restricciones y la personalidad del sistema de IA."),
            ("[bold]2. Role Prompting[/]",
             "Asigna un rol profesional específico al modelo dentro del propio prompt.\n   El modelo adopta la perspectiva y el vocabulario del rol asignado."),
            ("[bold]3. Few-Shot Learning[/]",
             "Proporciona ejemplos resueltos de alta calidad para guiar al modelo.\n   El modelo aprende el patrón de respuesta esperado a partir de los ejemplos."),
            ("[bold]4. Chain-of-Thought (CoT)[/]",
             "Instruye al modelo a razonar paso a paso antes de concluir.\n   Mejora la precisión y la trazabilidad del razonamiento clínico."),
            ("[bold]5. RAG (Retrieval-Augmented Generation)[/]",
             "Recupera fragmentos de documentos reales e inyecta el contexto en el prompt.\n   Fundamenta las respuestas en protocolos institucionales verificados.")
        };

        foreach (var (title, desc) in techniques)
        {
            AnsiConsole.Write(
                new Panel($"{title}\n[dim]{desc}[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Padding(1, 0)
            );
            Console.WriteLine();
        }

        AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private async Task ExportarConsultaAsync()
    {
        if (_historial.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No hay consultas para exportar.[/]");
            AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
            Console.ReadLine();
            return;
        }

        var lastConsulta = _historial.Last();
        var fileName = $"consulta_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);

        var content = $"""
            ================================================================================
            MEDTRIAJ AI — REPORTE DE CONSULTA DE TRIAJE
            Universidad UFHEC | INF-4350: Tecnologías de la Inteligencia Artificial
            ================================================================================

            Fecha y Hora: {lastConsulta.Timestamp:dd/MM/yyyy HH:mm:ss}
            Técnica de Prompt Engineering Aplicada: {lastConsulta.TecnicaUsada}
            Nivel ESI Identificado: {lastConsulta.ESILevel} ({GetESIDescription(lastConsulta.ESILevel)})
            Modo: {(lastConsulta.FueSimulado ? "SIMULACIÓN (Ollama no disponible)" : "IA LOCAL (Ollama)")}

            ────────────────────────────────────────────────────────────────────────────────
            DESCRIPCIÓN DEL CASO:
            {lastConsulta.CasoDescripcion}

            ────────────────────────────────────────────────────────────────────────────────
            ANÁLISIS DE TRIAJE PRELIMINAR:
            {lastConsulta.Respuesta}

            ================================================================================
            AVISO LEGAL: Este análisis es PRELIMINAR y generado por un sistema de IA.
            NO reemplaza la evaluación clínica del médico profesional certificado.
            ================================================================================
            """;

        await File.WriteAllTextAsync(exportPath, content);
        AnsiConsole.MarkupLine($"[green]✓ Consulta exportada exitosamente a:[/]");
        AnsiConsole.MarkupLine($"  [cyan]{exportPath}[/]");
        Console.WriteLine();
        AnsiConsole.Markup("[dim]Presione ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static int ExtractESILevel(string response)
    {
        var lower = response.ToLowerInvariant();
        if (lower.Contains("nivel 1") || lower.Contains("esi 1") || lower.Contains("nivel: 1")) return 1;
        if (lower.Contains("nivel 2") || lower.Contains("esi 2") || lower.Contains("nivel: 2")) return 2;
        if (lower.Contains("nivel 3") || lower.Contains("esi 3") || lower.Contains("nivel: 3")) return 3;
        if (lower.Contains("nivel 4") || lower.Contains("esi 4") || lower.Contains("nivel: 4")) return 4;
        if (lower.Contains("nivel 5") || lower.Contains("esi 5") || lower.Contains("nivel: 5")) return 5;
        return 0;
    }

    private static (string color, string label) GetESIDisplay(int level) => level switch
    {
        1 => ("red", "ESI 1 — EMERGENCIA INMEDIATA"),
        2 => ("darkorange3", "ESI 2 — MUY URGENTE"),
        3 => ("yellow", "ESI 3 — URGENTE"),
        4 => ("green", "ESI 4 — MENOS URGENTE"),
        5 => ("blue", "ESI 5 — NO URGENTE"),
        _ => ("white", "ESI — NO DETERMINADO")
    };

    private static string GetESIDescription(int level) => level switch
    {
        1 => "Resucitación / Riesgo vital inmediato",
        2 => "Emergente / Alta severidad",
        3 => "Urgente / Múltiples recursos necesarios",
        4 => "Semi-urgente / Un recurso",
        5 => "No urgente / Sin recursos",
        _ => "No determinado"
    };
}

public record ConsultaRecord
{
    public DateTime Timestamp { get; init; }
    public string CasoDescripcion { get; init; } = "";
    public string TecnicaUsada { get; init; } = "";
    public string Respuesta { get; init; } = "";
    public int ESILevel { get; init; }
    public bool FueSimulado { get; init; }
}
