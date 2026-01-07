# AlgoRhythm - E-Learning Platform for Algorithm Learning

AlgoRhythm is a comprehensive e-learning platform designed for learning algorithms and data structures through interactive programming exercises, courses, and real-time code execution.

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
git clone https://github.com/AlgoRhythmProject/Backend.git cd Backend
2. **Set up environment variables** (see below)
3. **Start all services**
docker-compose -f docker-compose.dev.yml build --no-cache
docker-compose -f docker-compose.dev.yml build --no-cache

or in Visual Studio set startup Item to AlgoRhythm 
and in Debugging set Docker Compose Dev (instead of http)

4. **Access the application**
   - Frontend: http://localhost:5173
   - Backend API: https://localhost:7062
   - Swagger UI: https://localhost:7062/swagger
5. **Stop all services**
docker-compose -f docker-compose.dev.yml down -v

---

## ⚙️ Environment Variables

### Required Environment Variables

Create a `.env` file in the root directory or set these in your environment:

#### Backend (AlgoRhythm API)
# Database
SA_PASSWORD=JanMachalski123!!!
CONNECTION_STRING=Server=database;Database=AlgoRhythmDb;User Id=sa;Password=JanMachalski123!!!;Encrypt=False;TrustServerCertificate=True

# JWT Authentication
JWT_KEY= nie wiem czy moge to tu napisac

# SendGrid Email
SENDGRID_API_KEY= tutaj tez nwm

# Code Executor
CODE_EXECUTOR_URL=http://code-executor:5000

# Frontend URL (dla CORS)
FRONTEND_URL=http://localhost:5173


#### Frontend
VITE_API_BASE_URL=http://localhost:7062/api



## 🔧 Troubleshooting
