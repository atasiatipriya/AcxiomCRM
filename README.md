# AcxiomCRM

A Customer Relationship Management (CRM) web application developed in ASP.NET Core MVC for managing leads, customers, sales opportunities, and follow-up activities.

## Tech Stack
* C# / .NET 8 (ASP.NET Core MVC)
* Entity Framework Core (Code First)
* Microsoft SQL Server (LocalDB)
* ASP.NET Core Identity (Authentication, Role Management & Lockout)
* Bootstrap 5 & Chart.js

## Key Features
* **Role-Based Access**: Role scoping for Admin, Manager, and Sales Executive with server-side authorization.
* **Lead Management & Conversion**: Complete status workflow (New → Contacted → Qualified → Converted / Lost) and lead-to-customer conversion.
* **Customer Management**: Customer records with duplicate email/phone check, 10-digit mobile validation, and soft-delete.
* **Opportunity & Pipeline Tracking**: Deal tracking across 5 pipeline stages with weighted revenue calculation.
* **Follow-Ups & Activities**: Scheduling calls, meetings, tasks, and follow-ups with validation preventing past dates.
* **Dashboard & Reports**: Real-time KPI summaries, Chart.js visual charts, and CSV report export.
* **Audit Trail**: Append-only audit logging recording user actions and state changes.
* **REST API**: API endpoints for Customers, Leads, and Opportunities with JSON responses and proper status codes.

## Default Logins

The database seeds with three test accounts upon first run:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.local` | `Admin@12345` |
| Manager | `manager@acxiomcrm.local` | `Manager@12345` |
| Sales Executive | `sales@acxiomcrm.local` | `Sales@12345` |

## How to Run

### Prerequisites
* .NET 8.0 SDK
* SQL Server LocalDB

### Steps
1. Navigate to the project directory:
   ```bash
   cd AcxiomCrm
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. Open your browser and go to:
   ```
   http://localhost:5170
   ```
