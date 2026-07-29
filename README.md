# MedTriaj AI — Sistema Inteligente de Triaje Médico Preliminar

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-black?logo=ollama)](https://ollama.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![UFHEC](https://img.shields.io/badge/UFHEC-INF--4350-darkred)](https://ufhec.edu.do)

> **Trabajo Final — INF-4350: Tecnologías de la Inteligencia Artificial**
> Universidad UFHEC | Facultad de Ciencias y Tecnología

---

## 🩺 Descripción

**MedTriaj AI** es un prototipo funcional de un sistema inteligente de triaje médico preliminar desarrollado en C# (.NET 9) que utiliza modelos de lenguaje locales mediante **Ollama** para apoyar al personal de salud en la clasificación inicial de pacientes según el sistema **ESI (Emergency Severity Index)** de 5 niveles.

El sistema aplica 5 técnicas avanzadas de **Prompt Engineering** para mejorar la calidad, precisión y trazabilidad de las respuestas del modelo de IA, fundamentadas en protocolos clínicos institucionales reales.

> ⚠️ **AVISO IMPORTANTE**: MedTriaj AI es un sistema de **APOYO PRELIMINAR** exclusivamente diseñado como prototipo académico. **No reemplaza el criterio clínico del médico profesional certificado.** Cualquier decisión médica real debe ser tomada por un profesional de la salud habilitado.

---

## 🚀 Características Principales

- **🤖 IA 100% Local**: Privacidad total, sin envío de datos a servidores externos mediante Ollama
- **🔧 5 Técnicas de Prompt Engineering**:
  1. **System Prompt Injection** — Contexto institucional y restricciones del sistema
  2. **Role Prompting** — Asignación de rol de enfermero/médico de triaje especializado
  3. **Few-Shot Learning** — 3 casos de ejemplo resueltos para guiar las respuestas
  4. **Chain-of-Thought (CoT)** — Razonamiento clínico paso a paso obligatorio
  5. **RAG (Retrieval-Augmented Generation)** — Recuperación de protocolos clínicos locales
- **📊 Escala ESI Completa** (Niveles 1 al 5) con código de colores
- **📋 Historial de Consultas** en sesión con registro de técnica y nivel ESI
- **📁 Carga de Protocolos** — Ingesta de documentos clínicos `.txt`, `.md`, `.json`
- **💾 Exportación de Reportes** de triaje a archivos de texto
- **🎨 Interfaz Rica** con animaciones, colores y paneles (Spectre.Console)
- **🔄 Modo Demo** inteligente cuando Ollama no está disponible

---

## 📋 Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.com/download) (recomendado para respuestas reales)
- Windows 10/11 (compatible también con Linux/macOS)
- Mínimo 8 GB RAM (16 GB recomendado para modelos grandes)

---

## ⚙️ Instalación y Configuración

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/MedTriajAI.git
cd MedTriajAI
```

### 2. Instalar y configurar Ollama

```bash
# Descargar e instalar Ollama desde https://ollama.com
# Luego, descargar un modelo (recomendados):
ollama pull llama3.2        # Recomendado — equilibrio velocidad/calidad
ollama pull mistral         # Excelente para español
ollama pull phi3            # Más ligero, para equipos con menos RAM
ollama pull gemma2          # Google Gemma, buenas respuestas
ollama pull tinyllama       # Muy ligero, mínimo 4 GB RAM
```

### 3. Iniciar Ollama

```bash
ollama serve
```

### 4. Compilar y ejecutar el prototipo

```bash
cd MedTriajAI
dotnet restore
dotnet build
dotnet run
```

---

## 🎯 Uso del Sistema

Al iniciar, el sistema verifica automáticamente la disponibilidad de Ollama y presenta el menú principal:

```
╔══════════════════════════════════════════════════════════╗
║         MedTriaj AI — Sistema de Triaje Médico           ║
║   Powered by Ollama + C# | INF-4350 UFHEC               ║
╚══════════════════════════════════════════════════════════╝

  1. Nueva Consulta de Triaje (Chain-of-Thought automático)
  2. Consulta con técnica de Prompt Engineering específica
  3. Ver historial de consultas
  4. Gestionar documentos de protocolo clínico
  5. Información sobre técnicas de Prompt Engineering
  6. Exportar última consulta a archivo
  0. Salir
```

### Ejemplo de caso clínico:

```
Descripción del caso:
Paciente masculino, 67 años. Dolor torácico opresivo de inicio súbito hace 
15 minutos, irradiado al brazo izquierdo y mandíbula. Diaforesis intensa. 
FC: 118 lpm. TA: 88/56 mmHg. SpO2: 94%.
```

**Resultado esperado**: ESI Nivel 1 — Emergencia Inmediata (Síndrome Coronario Agudo)

---

## 📁 Estructura del Proyecto

```
MedTriajAI/
├── Program.cs                          # Punto de entrada principal
├── MedTriajAI.csproj                   # Archivo de proyecto .NET 9
├── Services/
│   ├── OllamaClient.cs                 # Cliente HTTP para Ollama API
│   ├── PromptEngineeringEngine.cs      # Motor de las 5 técnicas de PE
│   └── DocumentLoader.cs              # Cargador de protocolos (RAG)
├── UI/
│   └── ConsoleDashboard.cs            # Interfaz interactiva Spectre.Console
└── Protocols/
    ├── Protocolo_Triaje_ESI.txt        # Protocolo ESI completo (5 niveles)
    └── Protocolo_Dolor_Agudo.txt       # Protocolo de manejo del dolor agudo
```

---

## 🧠 Técnicas de Prompt Engineering Implementadas

| # | Técnica | Descripción | Beneficio |
|---|---------|-------------|-----------|
| 1 | **System Prompt** | Define rol, restricciones y comportamiento global | Consistencia y control |
| 2 | **Role Prompting** | Asigna rol de enfermero con 15 años de experiencia | Vocabulario clínico apropiado |
| 3 | **Few-Shot** | 3 ejemplos resueltos de alta calidad | Formato de respuesta consistente |
| 4 | **Chain-of-Thought** | 5 pasos de razonamiento obligatorio | Trazabilidad y precisión |
| 5 | **RAG** | Inyecta fragmentos de protocolos reales | Fundamentación en evidencia |
| 6 | **Combinada** | Todas las técnicas simultáneas | Máxima calidad de respuesta |

---

## 📊 Escala ESI de Triaje

| Nivel | Color | Descripción | Tiempo Máximo |
|-------|-------|-------------|---------------|
| **ESI 1** | 🔴 Rojo | Resucitación / Riesgo vital inmediato | 0 minutos |
| **ESI 2** | 🟠 Naranja | Emergente / Alta severidad | 15 minutos |
| **ESI 3** | 🟡 Amarillo | Urgente / Múltiples recursos | 30 minutos |
| **ESI 4** | 🟢 Verde | Semi-urgente / Un recurso | 1-2 horas |
| **ESI 5** | 🔵 Azul | No urgente / Sin recursos | 2-4 horas |

---

## 🛡️ Consideraciones Éticas

- **Privacidad**: Todo el procesamiento es local. Ningún dato clínico sale del equipo.
- **Transparencia**: El sistema siempre indica la técnica de IA utilizada y si la respuesta es simulada.
- **No sustitución**: Cada respuesta incluye el aviso obligatorio de que es PRELIMINAR.
- **Equidad**: El sistema no hace discriminación por género, edad, etnia ni condición socioeconómica.
- **Supervisión humana**: Diseñado para asistir, no para reemplazar al profesional de la salud.

---

## 📚 Referencias

- Gilboy, N., Tanabe, P., Travers, D. A., & Rosenau, A. M. (2020). *Emergency Severity Index (ESI): A triage tool for emergency department care* (5th ed.). AHRQ.
- Wei, J., et al. (2022). Chain-of-thought prompting elicits reasoning in large language models. *NeurIPS 2022*.
- Brown, T. B., et al. (2020). Language models are few-shot learners. *arXiv:2005.14165*.
- Lewis, P., et al. (2020). Retrieval-augmented generation for knowledge-intensive NLP tasks. *NeurIPS 2020*.
- OMS. (2023). *Ética de la inteligencia artificial en la atención de salud*. Organización Mundial de la Salud.

---

## 👨‍💻 Tecnologías Utilizadas

- **Lenguaje**: C# 13 / .NET 9
- **IA Local**: Ollama (soporte para llama3.2, mistral, phi3, gemma2)
- **UI de Consola**: Spectre.Console 0.49
- **Protocolo**: HTTP REST + JSON streaming
- **Paradigma de Prompt**: System Prompt + Role + Few-Shot + CoT + RAG

---

*Trabajo Final — INF-4350: Tecnologías de la Inteligencia Artificial | UFHEC 2024*
