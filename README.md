# RaceDay API

**Module:** PROGRAMMING 2B (PROG6212)  
**Assessment:** Portfolio of Evidence – Part 2 (RESTful API)

RaceDay is a full-stack event management system designed for the South African road running, walking and cycling community.  
This repository contains the **RESTful API** (Part 2) built with ASP.NET Core, Entity Framework Core and session-based authentication.

---

## Features

- User registration and login (Organiser / Participant roles)
- Session-based authentication and role-based authorization
- Event management (Create, Read, Update, Delete) – Organiser only
- Category management per event – Organiser only
- Event enrolment – Participant only
- Results capturing and viewing
- Swagger UI for interactive testing
- Unit / Integration tests
- GitHub Actions CI/CD pipeline

---

## User Roles

| Role          | Permissions                                                                 |
|---------------|-----------------------------------------------------------------------------|
| **Organiser** | Create, update and delete events, manage categories, view enrolments, capture results |
| **Participant** | Browse events, enrol in events (select category), view own enrolments and results |

---

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core (Code-First)
- SQL Server / LocalDB
- Session-based Authentication
- xUnit + WebApplicationFactory (tests)
- Swagger / OpenAPI
- GitHub Actions

---

## Getting Started

### Prerequisites

- Visual Studio 2022 or later
- .NET 8 SDK (or the version you used)
- SQL Server LocalDB or full SQL Server

### Youtube Video
https://youtu.be/FNznL60Au4Y

### 1. Clone the repository

```bash
git clone https://github.com/Mcebo01/WebApplication_prompt_users.git
cd RaceDay_API
