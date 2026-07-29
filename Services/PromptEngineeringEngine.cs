namespace MedTriajAI.Services;

/// <summary>
/// Motor de construcción de prompts avanzados con 5 técnicas de Prompt Engineering
/// implementadas para el caso de uso de triaje médico preliminar.
/// </summary>
public class PromptEngineeringEngine
{
    private readonly DocumentLoader _documentLoader;

    // ─── Técnica 1: System Prompt Injection ──────────────────────────────────
    private const string SYSTEM_PROMPT = """
        Eres MedTriaj AI, un asistente especializado en triaje médico preliminar diseñado
        para apoyar al personal de salud en entornos hospitalarios y clínicos.

        Tu base de conocimiento incluye el sistema de triaje ESI (Emergency Severity Index)
        de 5 niveles, protocolos de valoración de signos vitales, y criterios clínicos de
        urgencia y emergencia médica.

        REGLAS ESTRICTAS:
        1. Siempre clasifica al paciente en un nivel ESI del 1 al 5.
        2. Nunca proporciones diagnósticos definitivos; usa terminología como "compatible con",
           "sospecha de" o "sugiere evaluación de".
        3. Siempre indica los signos vitales críticos que deben medirse.
        4. Concluye SIEMPRE con el aviso: "Este análisis es PRELIMINAR. El criterio del
           médico tratante siempre prevalece."
        5. Responde en español formal con lenguaje clínico apropiado.
        6. Usa el razonamiento paso a paso (Chain-of-Thought) en cada análisis.
        """;

    // ─── Técnica 2: Few-Shot Learning ────────────────────────────────────────
    private static readonly List<(string caso, string analisis)> FewShotExamples =
    [
        (
            "Paciente masculino, 58 años. Dolor torácico opresivo de inicio súbito hace 20 minutos, irradiado al brazo izquierdo. Diaforesis intensa. TA: 90/60 mmHg. FC: 115 lpm.",
            """
            **Paso 1 — Síntomas identificados:** Dolor torácico opresivo, irradiación a brazo izquierdo, diaforesis, hipotensión y taquicardia.
            **Paso 2 — Signos vitales críticos:** TA 90/60 (hipotensión), FC 115 (taquicardia), requiere evaluación inmediata de SpO2 y FR.
            **Paso 3 — Clasificación ESI:** Nivel 1 (Resucitación inmediata) — Cuadro compatible con síndrome coronario agudo con inestabilidad hemodinámica.
            **Paso 4 — Acción recomendada:** Activar código infarto, monitor cardíaco inmediato, vía venosa periférica, oxígeno suplementario, EKG de 12 derivaciones urgente.
            ⚠️ Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """
        ),
        (
            "Paciente femenina, 7 años. Fiebre de 38.5°C desde hace 2 días, tos seca, congestión nasal leve. Come y bebe con normalidad. Saturación 98%. FC: 95 lpm.",
            """
            **Paso 1 — Síntomas identificados:** Fiebre moderada (38.5°C), tos seca, congestión nasal, sin dificultad respiratoria, hidratación adecuada.
            **Paso 2 — Signos vitales:** Temperatura 38.5°C (febril), FC 95 (normal para la edad), SpO2 98% (normal). Sin señales de alarma.
            **Paso 3 — Clasificación ESI:** Nivel 4 (Menos urgente) — Cuadro compatible con infección respiratoria alta de origen viral.
            **Paso 4 — Acción recomendada:** Valoración médica en tiempo estándar (1-2 horas), antipirético según peso, hidratación oral, observación de signos de alarma (dificultad respiratoria, SpO2 < 94%).
            ⚠️ Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """
        ),
        (
            "Paciente masculino, 34 años. Laceracion en antebrazo derecho de 4 cm de profundidad por accidente doméstico. Sangrado activo controlado con presión. Sin otros síntomas.",
            """
            **Paso 1 — Síntomas identificados:** Laceración profunda en antebrazo, sangrado activo pero controlable, sin compromiso sistémico.
            **Paso 2 — Signos vitales:** No reportados. Verificar TA y FC para descartar compromiso hemodinámico por pérdida sanguínea.
            **Paso 3 — Clasificación ESI:** Nivel 3 (Urgente) — Herida que requiere evaluación de estructuras profundas (tendones, vasos, nervios) y sutura.
            **Paso 4 — Acción recomendada:** Mantener presión directa, elevar extremidad, valorar necesidad de hemostasia quirúrgica, profilaxis antitetánica según historial.
            ⚠️ Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """
        )
    ];

    public PromptEngineeringEngine(DocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GetSystemPrompt() => SYSTEM_PROMPT;

    /// <summary>
    /// Técnica 1: System Prompt puro — Consulta directa con personalidad del sistema
    /// </summary>
    public (string prompt, string description) BuildSystemPromptOnly(string userInput)
    {
        var description = "TÉCNICA: System Prompt Injection\n" +
                          "El sistema inyecta un prompt de sistema detallado que define rol, restricciones y comportamiento del modelo antes de cada consulta.";
        return (userInput, description);
    }

    /// <summary>
    /// Técnica 2: Role Prompting — Asignación de rol específico dentro del prompt
    /// </summary>
    public (string prompt, string description) BuildRolePrompt(string userInput)
    {
        var description = "TÉCNICA: Role Prompting\n" +
                          "Se asigna un rol médico específico al modelo dentro del propio prompt para condicionar su perspectiva y vocabulario.";

        var prompt = $"""
            Actúa como un enfermero/a de triaje con 15 años de experiencia en urgencias hospitalarias,
            certificado en el sistema ESI (Emergency Severity Index). Tu objetivo es realizar una
            valoración inicial rápida y precisa del siguiente caso:

            CASO CLÍNICO:
            {userInput}

            Proporciona tu valoración de triaje siguiendo el protocolo ESI, indicando nivel de prioridad
            y acciones inmediatas recomendadas.
            """;

        return (prompt, description);
    }

    /// <summary>
    /// Técnica 3: Few-Shot Learning — Ejemplos de contexto para guiar la respuesta
    /// </summary>
    public (string prompt, string description) BuildFewShotPrompt(string userInput)
    {
        var description = "TÉCNICA: Few-Shot Learning\n" +
                          "Se proporcionan 3 ejemplos resueltos de triaje médico para que el modelo aprenda el patrón y formato de respuesta esperado.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("A continuación hay ejemplos de análisis de triaje médico correctamente realizados:");
        sb.AppendLine();

        for (int i = 0; i < FewShotExamples.Count; i++)
        {
            sb.AppendLine($"### EJEMPLO {i + 1}");
            sb.AppendLine($"**Caso:** {FewShotExamples[i].caso}");
            sb.AppendLine($"**Análisis:**\n{FewShotExamples[i].analisis}");
            sb.AppendLine();
        }

        sb.AppendLine("### CASO A EVALUAR (sigue el mismo formato):");
        sb.AppendLine(userInput);

        return (sb.ToString(), description);
    }

    /// <summary>
    /// Técnica 4: Chain-of-Thought (CoT) — Razonamiento paso a paso explícito
    /// </summary>
    public (string prompt, string description) BuildChainOfThoughtPrompt(string userInput)
    {
        var description = "TÉCNICA: Chain-of-Thought (CoT)\n" +
                          "Se instruye al modelo a razonar explícitamente paso a paso antes de concluir, mejorando la calidad y trazabilidad de la respuesta clínica.";

        var prompt = $"""
            Analiza el siguiente caso de triaje médico siguiendo OBLIGATORIAMENTE este proceso de razonamiento paso a paso:

            CASO:
            {userInput}

            PROCESO DE ANÁLISIS (debes completar CADA paso antes de pasar al siguiente):

            **Paso 1 — Extracción de síntomas:** Lista todos los síntomas y signos referidos. ¿Cuáles son objetivos y cuáles subjetivos?

            **Paso 2 — Evaluación de signos vitales:** ¿Qué signos vitales se reportan? ¿Están dentro de rangos normales? ¿Cuáles faltan y son críticos obtener?

            **Paso 3 — Identificación de banderas rojas:** ¿Existe algún signo o síntoma de alarma que sugiera riesgo vital inmediato?

            **Paso 4 — Clasificación ESI:** Basado en los pasos anteriores, ¿cuál es el nivel ESI correspondiente (1=Emergencia hasta 5=No urgente)? Justifica tu elección.

            **Paso 5 — Plan de acción inmediato:** ¿Qué intervenciones y evaluaciones debe recibir este paciente y en qué orden de prioridad?

            ⚠️ Recuerda: Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """;

        return (prompt, description);
    }

    /// <summary>
    /// Técnica 5: RAG (Retrieval-Augmented Generation) — Inyección de documentos de protocolo
    /// </summary>
    public async Task<(string prompt, string description)> BuildRAGPromptAsync(string userInput)
    {
        var description = "TÉCNICA: RAG — Retrieval-Augmented Generation\n" +
                          "Se recuperan fragmentos relevantes de protocolos clínicos locales y se inyectan en el prompt para fundamentar la respuesta en documentación oficial.";

        var contextDocs = await _documentLoader.LoadProtocolContextAsync(userInput);
        var contextSection = contextDocs.Any()
            ? "CONTEXTO DE PROTOCOLO CLÍNICO (extraído de documentos institucionales):\n" +
              string.Join("\n---\n", contextDocs) + "\n\n"
            : "NOTA: No se encontraron documentos de protocolo relevantes en el directorio local.\n\n";

        var prompt = $"""
            Utilizando el siguiente contexto extraído de protocolos clínicos institucionales, analiza el caso médico presentado:

            {contextSection}CASO CLÍNICO A EVALUAR:
            {userInput}

            Fundamenta tu análisis de triaje en los protocolos proporcionados cuando sea aplicable. Si el protocolo no cubre
            el caso específico, indícalo explícitamente y aplica criterios ESI estándar.
            Proporciona nivel de triaje ESI, justificación clínica y plan de acción inmediato.

            ⚠️ Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """;

        return (prompt, description);
    }

    /// <summary>
    /// Técnica combinada: Máxima potencia — todas las técnicas juntas
    /// </summary>
    public async Task<(string prompt, string description)> BuildCombinedPromptAsync(string userInput)
    {
        var description = "TÉCNICA: Combinada (System Prompt + Role + Few-Shot + CoT + RAG)\n" +
                          "Aplica TODAS las técnicas simultáneamente para máxima precisión y fundamentación clínica.";

        var (ragPrompt, _) = await BuildRAGPromptAsync(userInput);
        var example = FewShotExamples[0];

        var prompt = $"""
            Actúa como un especialista en medicina de urgencias y triaje hospitalario con certificación ESI avanzada.

            EJEMPLO DE REFERENCIA:
            Caso: {example.caso}
            Análisis correcto: {example.analisis}

            DOCUMENTOS DE PROTOCOLO:
            {ragPrompt}

            CASO A ANALIZAR — Sigue el mismo proceso paso a paso del ejemplo, fundamentado en los protocolos:
            {userInput}

            Estructura tu respuesta en los 5 pasos del razonamiento clínico (Síntomas → Signos Vitales → Banderas Rojas → Nivel ESI → Plan de Acción).
            ⚠️ Este análisis es PRELIMINAR. El criterio del médico tratante siempre prevalece.
            """;

        return (prompt, description);
    }
}
