# ?? Build & Run Commands - Sprint Tracker

## Prerequisites

### Required Software
- Node.js 18+ (for Next.js frontend)
- .NET 9 SDK (for API backend)
- MongoDB (local or cloud)
- Git

### Verify Installation
```bash
# Check Node.js version
node --version  # Should show v18.x.x or higher

# Check .NET version
dotnet --version  # Should show 9.x.x

# Check MongoDB
mongod --version  # Or check MongoDB Atlas connection
```

---

## First Time Setup

### 1. Clone Repository
```bash
git clone <your-repo-url>
cd SprintTracker.Api
```

### 2. Setup Backend (.NET API)

#### Configure Database Connection
Edit `appsettings.json`:
```json
{
  "DatabaseSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "SprintTrackerDb"
  }
}
```

Or for MongoDB Atlas:
```json
{
  "DatabaseSettings": {
    "ConnectionString": "mongodb+srv://username:password@cluster.mongodb.net/",
  "DatabaseName": "SprintTrackerDb"
  }
}
```

#### Restore Dependencies
```bash
dotnet restore
```

### 3. Setup Frontend (Next.js)

#### Navigate to Frontend Directory
```bash
cd sprinttracker-ui
```

#### Install Dependencies
```bash
npm install
```

#### Configure API URL
Edit `sprinttracker-ui/.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5001
```

---

## Running the Application

### Option 1: Development Mode (Recommended)

#### Terminal 1 - Start Backend API
```bash
# From SprintTracker.Api directory
dotnet run

# Or with hot reload
dotnet watch run
```

API will run on: `http://localhost:5001`

#### Terminal 2 - Start Frontend
```bash
# From sprinttracker-ui directory
npm run dev
```

Frontend will run on: `http://localhost:3000`

#### Access Application
Open browser: `http://localhost:3000`

### Option 2: Production Build

#### Build Backend
```bash
# From SprintTracker.Api directory
dotnet build --configuration Release
```

#### Build Frontend
```bash
# From sprinttracker-ui directory
npm run build
```

#### Run Production
```bash
# Terminal 1 - Backend
dotnet run --configuration Release

# Terminal 2 - Frontend
npm start
```

---

## Quick Commands Reference

### Backend (.NET API)

```bash
# Navigate to API directory
cd SprintTracker.Api

# Restore packages
dotnet restore

# Build project
dotnet build

# Run API
dotnet run

# Run with hot reload
dotnet watch run

# Run tests (if any)
dotnet test

# Clean build artifacts
dotnet clean

# Create migration (if using EF Core - not needed for MongoDB)
# dotnet ef migrations add <MigrationName>
```

### Frontend (Next.js)

```bash
# Navigate to frontend directory
cd sprinttracker-ui

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Start production server
npm start

# Run linting
npm run lint

# Clean node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

---

## Verify Setup

### 1. Check Backend API

#### Test Health Endpoint
```bash
curl http://localhost:5001/health
```

Should return: `{ "status": "Healthy" }`

#### Test API Endpoints
```bash
# Test auth endpoint
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123","firstName":"Test","lastName":"User"}'
```

### 2. Check Frontend

Open browser: `http://localhost:3000`

You should see the login page.

### 3. Check Database Connection

Check MongoDB:
```bash
# For local MongoDB
mongo
use SprintTrackerDb
show collections

# For MongoDB Atlas
# Check connection in Atlas dashboard
```

---

## Common Issues & Solutions

### Issue: "Port 5001 already in use"

**Solution:**
```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :5001
kill -9 <PID>

# Or change port in appsettings.json
"Kestrel": {
  "EndPoints": {
    "Http": {
      "Url": "http://localhost:5002"
    }
  }
}
```

### Issue: "Port 3000 already in use"

**Solution:**
```bash
# Kill process on port 3000
# Windows
netstat -ano | findstr :3000
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :3000
kill -9 <PID>

# Or run on different port
PORT=3001 npm run dev
```

### Issue: "MongoDB connection failed"

**Solution:**
1. Check if MongoDB is running:
```bash
# For local MongoDB
mongod

# Check status
mongosh --eval "db.runCommand({ ping: 1 })"
```

2. Verify connection string in `appsettings.json`
3. For MongoDB Atlas, check:
   - Network access (IP whitelist)
   - Database user credentials
   - Cluster is running

### Issue: "npm install fails"

**Solution:**
```bash
# Clear npm cache
npm cache clean --force

# Delete node_modules and package-lock.json
rm -rf node_modules package-lock.json

# Reinstall
npm install

# If still fails, try with --legacy-peer-deps
npm install --legacy-peer-deps
```

### Issue: "dotnet restore fails"

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore

# If specific package fails, try:
dotnet restore --force
```

---

## Database Setup

### Local MongoDB Setup

#### Install MongoDB
```bash
# Windows (with Chocolatey)
choco install mongodb

# Mac (with Homebrew)
brew install mongodb-community

# Linux (Ubuntu)
sudo apt-get install mongodb
```

#### Start MongoDB
```bash
# Windows
mongod

# Mac/Linux
mongod --dbpath /path/to/data/db

# Or as service
sudo systemctl start mongod
```

### MongoDB Atlas Setup (Cloud)

1. Go to https://www.mongodb.com/cloud/atlas
2. Create free cluster
3. Create database user
4. Whitelist IP address (0.0.0.0/0 for development)
5. Get connection string
6. Update `appsettings.json` with connection string

---

## Environment Variables

### Backend (appsettings.json)
```json
{
  "DatabaseSettings": {
  "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "SprintTrackerDb"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars-long",
    "Issuer": "SprintTrackerApi",
    "Audience": "SprintTrackerClient",
    "ExpiryInMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
"Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend (.env.local)
```env
NEXT_PUBLIC_API_URL=http://localhost:5001
NODE_ENV=development
```

---

## Build for Production

### 1. Build Backend
```bash
cd SprintTracker.Api
dotnet publish -c Release -o ./publish
```

Output directory: `SprintTracker.Api/publish`

### 2. Build Frontend
```bash
cd sprinttracker-ui
npm run build
```

Output directory: `sprinttracker-ui/.next`

### 3. Deploy

#### Backend Deployment Options:
- Azure App Service
- AWS Elastic Beanstalk
- Docker container
- IIS (Windows Server)

#### Frontend Deployment Options:
- Vercel (Recommended for Next.js)
- Netlify
- Azure Static Web Apps
- AWS Amplify

---

## Docker Setup (Optional)

### Backend Dockerfile
Create `Dockerfile` in `SprintTracker.Api`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SprintTracker.Api.csproj", "./"]
RUN dotnet restore "SprintTracker.Api.csproj"
COPY . .
RUN dotnet build "SprintTracker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SprintTracker.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SprintTracker.Api.dll"]
```

### Build & Run Docker
```bash
# Build image
docker build -t sprinttracker-api .

# Run container
docker run -p 5001:5001 sprinttracker-api
```

### Docker Compose (Full Stack)
Create `docker-compose.yml`:
```yaml
version: '3.8'
services:
  mongodb:
    image: mongo:latest
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  api:
    build: ./SprintTracker.Api
    ports:
      - "5001:5001"
    environment:
      - DatabaseSettings__ConnectionString=mongodb://mongodb:27017
      - DatabaseSettings__DatabaseName=SprintTrackerDb
    depends_on:
      - mongodb

  frontend:
    build: ./sprinttracker-ui
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://api:5001
    depends_on:
      - api

volumes:
  mongodb_data:
```

Run with:
```bash
docker-compose up
```

---

## Testing

### Manual Testing
1. Start backend and frontend
2. Open `http://localhost:3000`
3. Register new user
4. Login
5. Create project
6. Create sprint
7. Start sprint
8. Submit work (as developer)
9. View report (as manager)
10. Complete sprint

### API Testing with Postman

#### Import Postman Collection
Create collection with these endpoints:

**Auth:**
- POST `/api/auth/register`
- POST `/api/auth/login`

**Projects:**
- GET `/api/projects`
- POST `/api/projects`
- GET `/api/projects/{id}`

**Sprints:**
- GET `/api/sprints/project/{projectId}`
- POST `/api/sprints`
- PUT `/api/sprints/{id}/start`
- PUT `/api/sprints/{id}/complete`

**Submissions:**
- POST `/api/sprintsubmissions/{sprintId}`
- GET `/api/sprintsubmissions/sprint/{sprintId}/report`

---

## Monitoring & Logs

### Backend Logs
Logs are written to console and can be configured in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

View logs:
```bash
# Run with verbose logging
dotnet run --verbosity detailed
```

### Frontend Logs
Check browser console (F12) for frontend errors.

---

## Useful Scripts

### Create Database Backup
```bash
# MongoDB backup
mongodump --db SprintTrackerDb --out ./backup

# Restore
mongorestore --db SprintTrackerDb ./backup/SprintTrackerDb
```

### Reset Database
```bash
# Drop database (WARNING: Deletes all data!)
mongosh
use SprintTrackerDb
db.dropDatabase()
```

### Check API Health
```bash
# Simple health check script
curl -s http://localhost:5001/health | jq .
```

---

## Development Workflow

### Daily Development
```bash
# Terminal 1 - Backend with hot reload
cd SprintTracker.Api
dotnet watch run

# Terminal 2 - Frontend with hot reload
cd sprinttracker-ui
npm run dev

# Terminal 3 - MongoDB (if local)
mongod
```

### Before Committing
```bash
# Backend
cd SprintTracker.Api
dotnet build
dotnet test

# Frontend
cd sprinttracker-ui
npm run lint
npm run build
```

---

## ?? You're All Set!

**Start developing:**
1. ? Backend running on `http://localhost:5001`
2. ? Frontend running on `http://localhost:3000`
3. ? MongoDB connected
4. ? Hot reload enabled

**Open browser:** `http://localhost:3000`

**Happy coding!** ??

---

*For detailed feature documentation, see `FINAL_SUMMARY.md`*
*For quick start guide, see `QUICK_START_GUIDE.md`*
