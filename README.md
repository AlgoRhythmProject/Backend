# AlgoRhythm - E-Learning Platform for Algorithm Learning

AlgoRhythm is a comprehensive e-learning platform designed for learning algorithms and data structures through interactive programming exercises, courses, and real-time code execution.

---

## 🏗️ Architecture

The application consists of multiple containerized services working together:

### Services Overview

#### 🔷 Backend (AlgoRhythm API)
- **Technology**: ASP.NET Core 9.0 (C# 13.0)
- **Port**: 7062 (HTTPS), 5062 (HTTP)
- **Description**: Main REST API handling authentication, courses, submissions, achievements, and user management
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer tokens with role-based authorization (Admin, User)

#### 🎨 Frontend
- **Technology**: React + TypeScript + Vite
- **Port**: 5173
- **Description**: Modern SPA providing interactive user interface for courses, exercises, and code submissions

#### 🗄️ Database
- **Technology**: Microsoft SQL Server 2022
- **Port**: 1433
- **Description**: Relational database storing users, courses, tasks, submissions, achievements, and progress tracking
- **Persistent Storage**: Volume mounted for data persistence

#### 🔍 Analyzer
- **Description**: Static code analysis service for evaluating code quality and detecting common mistakes
- **Integration**: Called by Backend for submission analysis

#### ⚙️ Code Executor
- **Port**: 5000
- **Description**: Isolated sandbox environment for safely executing user-submitted C# code against test cases
- **Security**: Containerized execution with resource limits and timeout protection

#### ☁️ Azurite (Azure Storage Emulator)
- **Ports**: 10000 (Blob), 10001 (Queue), 10002 (Table)
- **Description**: Local Azure Storage emulator for file storage (user avatars, lecture materials)
- **Persistent Storage**: Volume mounted at `./azurite-data`

---

## 🚀 Getting Started

### Prerequisites

- **Docker Desktop** (with Docker Compose)
- **.NET 9.0 SDK** (for local development)
- **Node.js 18+** (for frontend local development)
- **Visual Studio 2022** or **VS Code** (recommended)

### Quick Start with Docker Compose

1. **Clone the repository**
```bash
   git clone https://github.com/AlgoRhythmProject/Backend.git
   cd Backend
```

2. **Set up environment variables** (see configuration section below)

3. **Start all services**
   
   Using Docker Compose:
```bash
   docker-compose -f docker-compose.dev.yml build --no-cache
   docker-compose -f docker-compose.dev.yml up
```
   
   Or in Visual Studio:
   - Set startup item to `AlgoRhythm`
   - In Debugging, select `Docker Compose Dev` (instead of http)

4. **Access the application**
   - Frontend: http://localhost:5173
   - Backend API: https://localhost:7062
   - Swagger UI: https://localhost:7062/swagger

5. **Stop all services**
```bash
   docker-compose -f docker-compose.dev.yml down -v
```

---

## ⚙️ Configuration

### Environment Variables

Create a `.env` file in the root directory with the following variables:

#### Backend (AlgoRhythm API)
```env
# Database Configuration
SA_PASSWORD=YourStrongPassword123!
CONNECTION_STRING=Server=database;Database=AlgoRhythmDb;User Id=sa;Password=YourStrongPassword123!;Encrypt=False;TrustServerCertificate=True

# JWT Authentication
JWT_KEY=your-secret-jwt-key-here

# SendGrid Email Service
SENDGRID_API_KEY=your-sendgrid-api-key

# Code Executor Service
CODE_EXECUTOR_URL=http://code-executor:5000

# Frontend URL (for CORS)
FRONTEND_URL=http://localhost:5173
```

#### Frontend
```env
VITE_API_BASE_URL=http://localhost:7062/api
```

> **Note**: Replace placeholder values with your actual configuration. Keep sensitive values secure and never commit them to version control.

---

## 🔧 Troubleshooting

### Common Issues

#### Database Connection Errors
- Ensure SQL Server container is running: `docker ps`
- Verify `SA_PASSWORD` meets SQL Server complexity requirements
- Check connection string in environment variables

#### Frontend Cannot Connect to Backend
- Verify `VITE_API_BASE_URL` is correctly set
- Check CORS configuration in Backend allows `FRONTEND_URL`
- Ensure Backend container is running and healthy

#### Code Executor Timeouts
- Check Code Executor container logs: `docker logs code-executor`
- Verify `CODE_EXECUTOR_URL` is correctly configured
- Ensure sufficient Docker resources are allocated

#### Port Conflicts
- Check if ports 5173, 7062, 1433, or 5000 are already in use
- Modify port mappings in `docker-compose.dev.yml` if needed

#### Container Build Failures
- Clear Docker cache: `docker system prune -a`
- Rebuild without cache: `docker-compose build --no-cache`
- Check Docker daemon is running
