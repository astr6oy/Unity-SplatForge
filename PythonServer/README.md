# SplatForge Python Server

LLM-based 3D scene composition server for Unity.

## Quick Start

### Prerequisites

- Python 3.11+
- Poetry (dependency management)

### Installation

```bash
cd PythonServer
poetry install
```

### Configuration

Copy the example environment file and configure:

```bash
cp .env.example .env
```

Edit `.env` to set your preferences:

```env
# Use mock provider for testing (no API key needed)
LLM_PROVIDER=mock

# Or use OpenAI
LLM_PROVIDER=openai
OPENAI_API_KEY=sk-...

# Or use Claude
LLM_PROVIDER=claude
ANTHROPIC_API_KEY=sk-ant-...
```

### Running the Server

```bash
# Using poetry script
poetry run serve

# Or directly with uvicorn
poetry run uvicorn splatforge_server.main:app --reload --port 8080
```

The server will start at `http://localhost:8080`.

## API Endpoints

### Health Check

```bash
GET /api/v1/health
```

### Scene Composition (Main Workflow)

```bash
POST /api/v1/compose
Content-Type: application/json

{
  "prompt": "A cozy bedroom with a bed and desk",
  "floorStructure": {
    "boundsMin": {"x": -5, "y": 0, "z": -5},
    "boundsMax": {"x": 5, "y": 0, "z": 5},
    "floorHeight": 0,
    "walls": []
  },
  "options": {
    "maxObjects": 10,
    "includeDecorations": true
  }
}
```

### Layout Suggestions

```bash
POST /api/v1/layout
Content-Type: application/json

{
  "sceneContext": {
    "existingObjects": [],
    "sceneBoundsMin": {"x": -5, "y": 0, "z": -5},
    "sceneBoundsMax": {"x": 5, "y": 3, "z": 5}
  },
  "objectIdsToPlace": ["chair", "desk"],
  "constraints": {
    "avoidOverlap": true,
    "groundObjects": true,
    "minSpacing": 0.5
  }
}
```

### API Documentation

Interactive API docs are available at:
- Swagger UI: `http://localhost:8080/docs`
- ReDoc: `http://localhost:8080/redoc`

## Unity Integration

1. Open Unity project with SplatForge package
2. Go to `Edit > Project Settings > SplatForge`
3. Uncheck **Use Mock Server**
4. Set **Server Endpoint** to `http://localhost:8080`
5. Open `Tools > SplatForge > Control Panel`
6. Click **Connect**

## LLM Providers

### Mock (Default)

Returns predefined layouts based on keywords (bedroom, office, living).
No API key required. Good for UI testing.

### OpenAI

Uses GPT-4 Turbo for layout generation.

```env
LLM_PROVIDER=openai
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-4-turbo-preview
```

### Claude

Uses Claude 3 for layout generation.

```env
LLM_PROVIDER=claude
ANTHROPIC_API_KEY=sk-ant-...
CLAUDE_MODEL=claude-3-opus-20240229
```

## Project Structure

```
PythonServer/
├── src/splatforge_server/
│   ├── main.py           # FastAPI app
│   ├── config.py         # Settings
│   ├── dependencies.py   # DI
│   ├── api/              # API routers
│   ├── models/           # Pydantic schemas
│   ├── services/         # Business logic
│   └── llm/              # LLM providers
├── assets/               # Asset metadata
├── pyproject.toml        # Dependencies
└── .env                  # Configuration
```

## Development

### Running Tests

```bash
poetry run pytest
```

### Code Formatting

```bash
poetry run black src/
poetry run ruff check src/
```
