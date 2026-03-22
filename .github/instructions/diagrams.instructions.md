---
applyTo: "**/*.eraserdiagram"
---

# Architecture Diagrams Instructions

Follow these guidelines for creating architecture diagrams in this repository.

## Diagram Tool

- All architecture diagrams must be created using Eraser.io's diagram-as-code syntax.
- Store diagram source files in `docs/diagrams/` with the `.eraserdiagram` extension.

## Diagram Types

The first line of each `.eraserdiagram` file must declare the diagram type:

- `cloud-architecture-diagram` - For system architecture and deployment diagrams
- `entity-relationship-diagram` - For database schemas and data models
- `sequence-diagram` - For data flows, API flows, and process sequences
- `flow-chart` - For decision trees and process logic

## Export and Reference

- Export diagrams to `.png` format in the same directory for embedding in markdown.
- Reference the PNG in documentation with a link to the source file:

```markdown
![Diagram Name](docs/diagrams/diagram-name.png)

> Source: [diagram-name.eraserdiagram](docs/diagrams/diagram-name.eraserdiagram)
```

## Generating PNG from Eraser Diagrams

### Using Eraser MCP Server (Recommended)

GitHub Copilot agents can use the Eraser MCP server to generate PNG files from `.eraserdiagram` files.

#### Available MCP Tools

The Eraser MCP server provides specialized tools for each diagram type:

| Tool | Diagram Type | Use For |
|------|-------------|---------|
| `renderSequenceDiagram` | `sequence-diagram` | API flows, process sequences, data flows |
| `renderEntityRelationshipDiagram` | `entity-relationship-diagram` | Database schemas, data models |
| `renderCloudArchitectureDiagram` | `cloud-architecture-diagram` | System architecture, deployment diagrams |
| `renderFlowchart` | `flow-chart` | Decision trees, process logic |
| `renderBpmnDiagram` | `bpmn-diagram` | Business process diagrams |
| `renderPrompt` | Any | Generate diagrams from natural language using AI |

#### Usage Examples

**For existing diagrams** - Generate PNG from `.eraserdiagram` source:

```markdown
Use the Eraser MCP server to generate a PNG from docs/diagrams/database-schema.eraserdiagram
```

**For new diagrams** - Create both diagram and PNG using AI:

```markdown
Use renderPrompt to create a sequence diagram showing the OAuth authentication flow for Steam integration
```

**Using specific tools** - Render with diagram-specific tool:

```markdown
Use renderEntityRelationshipDiagram to generate a PNG from docs/diagrams/database-schema.eraserdiagram
```

The Eraser MCP server will:
- Read the existing `.eraserdiagram` source file
- Generate (export) a `.png` image from the source diagram
- Save the `.png` file in the same directory as the `.eraserdiagram` file

**Diagram Styling Standards:**

Always use these parameters when generating diagrams for consistency:
- `theme: "dark"` - Light text and lines on dark background
- `background: true` - Solid background instead of transparent

This provides better contrast in both light and dark GitHub themes and ensures all diagrams have a consistent professional appearance.

**For manual diagram creation:**
- Create or edit `.eraserdiagram` files using Eraser.io's diagram-as-code syntax
- Use GitHub Copilot to generate the PNG using the Eraser MCP server with dark theme and background
- Alternatively, use the Eraser.io web interface to create diagrams and export both formats

#### MCP Server Documentation

For more details on the Eraser MCP server:
- [Eraser Agent Integration Documentation](https://docs.eraser.io/docs/using-ai-agent-integrations)
- [Eraser MCP GitHub Repository](https://github.com/eraserlabs/eraser-io/tree/main/packages/eraser-mcp)

## Folder Structure

```
docs/
└── diagrams/
    ├── system-architecture.eraserdiagram
    ├── system-architecture.png
    ├── database-schema.eraserdiagram
    ├── database-schema.png
    └── ...
```
