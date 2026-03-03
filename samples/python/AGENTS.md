# Project Instructions for AI Agent

You are an AI coding assistant working on a Python web API built with FastAPI and modern Python tooling.

## Project Overview

This is a Python web application built with FastAPI following a clean architecture pattern. It uses SQLAlchemy for database access, Pydantic for data validation, and follows Python community best practices.

## Technology Stack

- **Language:** Python 3.12+
- **Framework:** FastAPI
- **ASGI Server:** Uvicorn
- **ORM:** SQLAlchemy 2.0 (async)
- **Database:** PostgreSQL (asyncpg driver)
- **Migrations:** Alembic
- **Validation:** Pydantic v2
- **Authentication:** JWT (python-jose) + OAuth2
- **Task Queue:** Celery with Redis
- **Testing:** pytest, pytest-asyncio, httpx (async test client)
- **Linting:** Ruff (linter + formatter)
- **Type Checking:** mypy (strict mode)
- **Package Management:** uv (or pip + pyproject.toml)

## Project Structure

```
src/
├── app/
│   ├── __init__.py
│   ├── main.py              # FastAPI app factory, middleware, lifespan
│   ├── config.py            # Pydantic Settings (env vars)
│   ├── dependencies.py      # FastAPI dependency injection
│   ├── api/
│   │   ├── __init__.py
│   │   ├── v1/
│   │   │   ├── __init__.py
│   │   │   ├── router.py    # v1 API router aggregator
│   │   │   ├── users.py     # User endpoints
│   │   │   └── products.py  # Product endpoints
│   │   └── deps.py          # Shared API dependencies
│   ├── models/              # SQLAlchemy models
│   │   ├── __init__.py
│   │   ├── base.py          # Declarative base, common mixins
│   │   ├── user.py
│   │   └── product.py
│   ├── schemas/             # Pydantic schemas (request/response)
│   │   ├── __init__.py
│   │   ├── user.py
│   │   └── product.py
│   ├── services/            # Business logic
│   │   ├── __init__.py
│   │   ├── user_service.py
│   │   └── product_service.py
│   ├── repositories/        # Data access layer
│   │   ├── __init__.py
│   │   ├── base.py          # Generic repository base
│   │   └── user_repo.py
│   └── core/                # Cross-cutting concerns
│       ├── __init__.py
│       ├── security.py      # Password hashing, JWT creation
│       ├── exceptions.py    # Custom exception classes
│       └── middleware.py     # Custom middleware
├── alembic/                 # Database migrations
│   ├── versions/
│   ├── env.py
│   └── alembic.ini
├── tests/
│   ├── conftest.py          # Fixtures (test DB, client, factories)
│   ├── test_users.py
│   ├── test_products.py
│   └── factories/           # Test data factories (factory_boy)
├── pyproject.toml           # Project config, dependencies, tool config
├── Dockerfile
└── docker-compose.yml
```

## Coding Standards

### General Python
- Target **Python 3.12+** — use modern syntax (match statements, `type` aliases, etc.)
- Use **type hints** everywhere — functions, variables, return types
- Use **`from __future__ import annotations`** at the top of every file
- Follow **PEP 8** naming conventions (enforced by Ruff)
- Use **dataclasses** or **Pydantic models** — avoid plain dicts for structured data
- Use **`pathlib.Path`** instead of `os.path`
- Use **f-strings** for string formatting
- Maximum line length: **88 characters** (Black/Ruff default)

### Naming Conventions
- **snake_case** for functions, methods, variables, modules, packages
- **PascalCase** for classes (including Pydantic models and SQLAlchemy models)
- **SCREAMING_SNAKE_CASE** for constants
- **_leading_underscore** for private/internal names
- Avoid abbreviations — use descriptive names

### Async / Await
- All FastAPI route handlers are `async def`
- All database operations use async SQLAlchemy (`AsyncSession`)
- Use `asyncio.gather()` for concurrent independent operations
- Never use blocking I/O in async functions — use `run_in_executor` if needed

### FastAPI Patterns
- Use **dependency injection** (`Depends()`) for services, DB sessions, auth
- Use **Pydantic v2 models** for request/response schemas
- Use **`APIRouter`** to organize routes by resource
- Use **`HTTPException`** or custom exception handlers for error responses
- Add **OpenAPI metadata** — summary, description, tags, response models
- Use **`status`** constants (`status.HTTP_201_CREATED`) not magic numbers

### SQLAlchemy 2.0
- Use **Mapped type annotations** (`Mapped[str]`, `Mapped[int | None]`)
- Use **`mapped_column()`** instead of `Column()`
- Async sessions via `async_sessionmaker` and `AsyncSession`
- Use **`select()`** statement style (not legacy `Query` API)
- Always `.scalars()` the result for ORM queries

### Testing
- Test files: `test_*.py` (pytest discovery)
- Use **`pytest-asyncio`** for async tests (`@pytest.mark.anyio`)
- Use **`httpx.AsyncClient`** with `ASGITransport` for API tests
- Use **fixtures** in `conftest.py` for shared setup (DB session, test client)
- Use **`factory_boy`** for test data generation
- Aim for **>90% code coverage** on business logic

### Error Handling
- Define custom exceptions in `core/exceptions.py`
- Register exception handlers in `main.py` with `@app.exception_handler()`
- Return consistent error response format: `{"detail": "message", "code": "ERROR_CODE"}`
- Log errors with `structlog` or standard `logging` with structured output
- Never expose internal errors/stack traces to API consumers

## Important Notes

- Environment config via `.env` file loaded by Pydantic `BaseSettings`
- Database URL: `DATABASE_URL` environment variable (async: `postgresql+asyncpg://...`)
- Alembic migrations: `alembic revision --autogenerate -m "description"`
- Run dev server: `uvicorn app.main:app --reload --host 0.0.0.0 --port 8000`
- Run tests: `pytest -v --cov=app tests/`
- Run linter: `ruff check .` and `ruff format .`
- Run type checker: `mypy src/`
