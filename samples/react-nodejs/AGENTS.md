# Project Instructions for AI Agent

You are an AI coding assistant working on a full-stack web application built with React (frontend) and Node.js/Express (backend).

## Project Overview

This is a modern full-stack JavaScript/TypeScript application with a React single-page application (SPA) frontend and a Node.js REST API backend. The project uses a monorepo structure.

## Technology Stack

### Frontend
- **Framework:** React 19 with TypeScript
- **Build Tool:** Vite
- **Routing:** React Router v7
- **State Management:** Zustand (global), React Query / TanStack Query (server state)
- **Styling:** Tailwind CSS v4
- **Forms:** React Hook Form + Zod validation
- **Testing:** Vitest, React Testing Library, Playwright (E2E)

### Backend
- **Runtime:** Node.js 22 LTS
- **Framework:** Express.js with TypeScript
- **Database:** PostgreSQL with Prisma ORM
- **Authentication:** JWT (access + refresh tokens)
- **Validation:** Zod
- **Testing:** Vitest, Supertest
- **API Documentation:** OpenAPI / Swagger

## Project Structure

```
packages/
├── client/                  # React frontend (Vite)
│   ├── src/
│   │   ├── components/      # Reusable UI components
│   │   │   ├── ui/          # Base UI primitives (Button, Input, Modal)
│   │   │   └── features/    # Feature-specific components
│   │   ├── pages/           # Page/route components
│   │   ├── hooks/           # Custom React hooks
│   │   ├── stores/          # Zustand stores
│   │   ├── services/        # API client functions
│   │   ├── types/           # TypeScript type definitions
│   │   ├── utils/           # Utility functions
│   │   ├── App.tsx          # Root app component with router
│   │   └── main.tsx         # Entry point
│   ├── public/              # Static assets
│   ├── index.html           # HTML template
│   ├── vite.config.ts
│   ├── tailwind.config.ts
│   └── tsconfig.json
├── server/                  # Express backend
│   ├── src/
│   │   ├── routes/          # Express route handlers
│   │   ├── middleware/       # Express middleware (auth, validation, error)
│   │   ├── services/        # Business logic
│   │   ├── models/          # Prisma-generated types and extensions
│   │   ├── utils/           # Utility functions
│   │   ├── config/          # App configuration
│   │   └── index.ts         # Server entry point
│   ├── prisma/
│   │   ├── schema.prisma    # Database schema
│   │   └── migrations/      # Prisma migrations
│   └── tsconfig.json
├── shared/                  # Shared types and utilities
│   ├── src/
│   │   ├── types/           # Shared TypeScript interfaces
│   │   └── validation/      # Shared Zod schemas
│   └── tsconfig.json
├── package.json             # Root workspace config (npm workspaces)
└── turbo.json               # Turborepo config (optional)
```

## Coding Standards

### General TypeScript
- Enable **strict mode** in all `tsconfig.json`
- Use **explicit return types** on exported functions
- Prefer **interfaces** over type aliases for object shapes
- Use **`const` assertions** and **discriminated unions** where appropriate
- Never use `any` — use `unknown` and narrow the type
- Prefer **named exports** over default exports
- Use **absolute imports** with path aliases (`@/components/...`, `@/services/...`)

### React / Frontend
- Use **functional components** with hooks — no class components
- Use **TypeScript generics** for reusable components (`<T extends ...>`)
- Colocate **component, styles, tests** in the same directory
- Custom hooks must start with `use` prefix
- Keep components small — extract logic into custom hooks
- Use **React.lazy** and **Suspense** for code splitting
- Event handlers: `handleEventName` (e.g., `handleClick`, `handleSubmit`)
- Props interfaces: `ComponentNameProps` (e.g., `ButtonProps`)

### State Management
- **React Query / TanStack Query** for all server state (API data)
- **Zustand** for client-only global state (UI state, user preferences)
- **React state** (`useState`) for local component state
- Never duplicate server data in global state — use query cache

### Node.js / Backend
- Use **ES modules** (`import`/`export`), not CommonJS
- Express routes organized by resource (`/routes/users.ts`, `/routes/products.ts`)
- Middleware chain: `auth → validate → handler → errorHandler`
- All route handlers are `async` with error catching middleware
- Use **Zod schemas** for request validation (body, params, query)
- Environment variables via `dotenv` + typed config module
- Database access only through Prisma client — never raw SQL unless necessary

### Naming Conventions
- **camelCase** for variables, functions, instances
- **PascalCase** for components, classes, types, interfaces
- **SCREAMING_SNAKE_CASE** for constants and env variables
- **kebab-case** for file/folder names (except React components → PascalCase)
- API routes: plural nouns, lowercase (`/api/v1/users`, `/api/v1/products`)

### Testing
- Test files: `ComponentName.test.tsx` or `service.test.ts` (colocated)
- Use `describe` / `it` blocks with descriptive names
- Mock external dependencies (API calls, database)
- Frontend: test behavior, not implementation — use `screen.getByRole`, not CSS selectors
- Backend: use `supertest` for route testing
- E2E: Playwright tests in `e2e/` directory

## Important Notes

- The client dev server proxies `/api` requests to the backend (configured in `vite.config.ts`)
- Environment variables: `.env.local` (client, prefixed `VITE_`), `.env` (server)
- Database URL is in `DATABASE_URL` environment variable
- Run `npx prisma generate` after schema changes
- Run `npx prisma migrate dev` for local development migrations
