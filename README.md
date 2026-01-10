# SprintTracker - Mphasis Sprint Management System

A robust sprint tracking and management system built with .NET 9 API backend and Next.js frontend, using MongoDB for data storage.

## Features

### For Managers (Project Managers, Scrum Masters)
- **Create and manage projects** with team assignments
- **Create and manage sprints** with goals and capacity planning
- **Sprint board** - Kanban-style task management
- **Reports & Analytics**
  - Burndown charts
  - Velocity tracking
  - Team performance metrics
- **Export reports** for presentations to higher authorities
- **Team management** - View and manage team members

### For Developers
- **View assigned tasks** across all projects
- **Update task status** - Move tasks through workflow
- **Log time** on tasks
- **View sprint progress** and goals

## Tech Stack

### Backend
- **.NET 9** Web API
- **MongoDB** with MongoDB.Driver
- **JWT Authentication** with BCrypt password hashing
- **Swagger/OpenAPI** documentation

### Frontend
- **Next.js 16** with App Router
- **TypeScript** for type safety
- **Tailwind CSS** for styling (Mphasis brand colors)
- **Recharts** for data visualization
- **Zustand** for state management
- **React Hot Toast** for notifications

## Project Structure

```
SprintTracker.Api/
??? Controllers/      # API Controllers
?   ??? AuthController.cs
?   ??? ProjectsController.cs
?   ??? SprintsController.cs
?   ??? TasksController.cs
?   ??? DashboardController.cs
?   ??? UsersController.cs
??? Data/
?   ??? MongoDbContext.cs # MongoDB connection and indexes
??? Models/
?   ??? User.cs
?   ??? Project.cs
?   ??? Sprint.cs
?   ??? SprintTask.cs
?   ??? ActivityLog.cs
?   ??? DTOs/
?       ??? ApiDtos.cs  # Request/Response DTOs
??? Services/
?   ??? AuthService.cs
?   ??? ProjectService.cs
?   ??? SprintService.cs
?   ??? TaskService.cs
?   ??? DashboardService.cs
??? sprinttracker-ui/     # Next.js Frontend
    ??? src/
        ??? app/       # App Router pages
        ??? components/   # Reusable components
        ??? services/     # API service layer
        ??? store/        # Zustand stores
     ??? types/        # TypeScript types
        ??? lib/          # Utilities
```

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- MongoDB (running locally on port 27017)

### Database Setup
1. Ensure MongoDB is running locally
2. Database: `Sprinttracker`
3. Collection prefix: `Sprintallica`

### Backend Setup
```bash
cd SprintTracker.Api
dotnet restore
dotnet run
```
The API will be available at `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

**Weather service**
The Weather proxy now uses the free Open‑Meteo APIs (no API key required). The backend calls Open‑Meteo for geocoding and forecast data, and the frontend calls the backend endpoints `/api/weather/geocode` and `/api/weather/report`.

If Open‑Meteo is temporarily unavailable, the UI will show a friendly error and a retry option.

### Frontend Setup
```bash
cd sprinttracker-ui
npm install
npm run dev
```
The frontend will be available at `http://localhost:3000`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token
- `GET /api/auth/me` - Get current user info

### Projects
- `GET /api/projects` - List projects
- `POST /api/projects` - Create project
- `GET /api/projects/{id}` - Get project details
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Archive project

### Sprints
- `GET /api/sprints/project/{projectId}` - List sprints
- `POST /api/sprints` - Create sprint
- `POST /api/sprints/{id}/start` - Start sprint
- `POST /api/sprints/{id}/complete` - Complete sprint
- `GET /api/sprints/{id}/burndown` - Get burndown data
- `GET /api/sprints/project/{projectId}/velocity` - Get velocity data

### Tasks
- `GET /api/tasks` - List tasks with filters
- `POST /api/tasks` - Create task
- `PUT /api/tasks/{id}` - Update task
- `PATCH /api/tasks/{id}/status` - Update task status
- `GET /api/tasks/my-tasks` - Get user's assigned tasks

### Dashboard
- `GET /api/dashboard` - Get dashboard statistics

## User Roles

| Role | Value | Permissions |
|------|-------|-------------|
| Admin | 0 | Full access |
| Project Manager | 1 | Create/manage projects, sprints, view reports |
| Scrum Master | 2 | Manage sprints, view reports |
| Developer | 3 | Update task status, log time |
| QA | 4 | Update task status, log time |
| Viewer | 5 | Read-only access |

## Mphasis Brand Colors

- Primary: `#0066B3`
- Secondary: `#00A0D2`
- Accent: `#F7941D`
- Dark: `#003366`

