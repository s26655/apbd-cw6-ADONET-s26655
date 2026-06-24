# Clinic Appointments REST API with ADO.NET

ASP.NET Core Web API project for managing clinic appointments.
The application uses SQL Server and low-level ADO.NET database access through `Microsoft.Data.SqlClient`.

Entity Framework is not used.

## Technologies

* C#
* ASP.NET Core Web API
* SQL Server
* ADO.NET
* Microsoft.Data.SqlClient

## Project structure

```text
Controllers/
  AppointmentsController.cs

DTOs/
  AppointmentListDto.cs
  AppointmentDetailsDto.cs
  CreateAppointmentRequestDto.cs
  UpdateAppointmentRequestDto.cs
  ErrorResponseDto.cs

Exceptions/
  AppointmentConflictException.cs
  InvalidAppointmentRequestException.cs

Services/
  IAppointmentService.cs
  AppointmentService.cs

Sql/
  01_create_and_seed_clinic.sql
  02_drop_clinic_tables.sql
  03_drop_clinic_database.sql

Requests/
  appointments.http
```

## Database setup

The project expects SQL Server to be available with the following connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ClinicAdoNet;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  }
}
```

To prepare the database, run:

```sql
Sql/01_create_and_seed_clinic.sql
```

This script creates the `ClinicAdoNet` database, creates the required tables, and inserts sample data.

Optional cleanup scripts:

```sql
Sql/02_drop_clinic_tables.sql
Sql/03_drop_clinic_database.sql
```

## Running the application

Restore and build the project:

```bash
dotnet build
```

Run the API:

```bash
dotnet run
```

The application uses the port printed by `dotnet run`, for example:

```text
http://localhost:5253
```

## Endpoints

### Get appointments

```http
GET /api/appointments
```

Optional query parameters:

```http
GET /api/appointments?status=Scheduled
GET /api/appointments?patientLastName=Kowalska
GET /api/appointments?idDoctor=1
GET /api/appointments?status=Scheduled&idDoctor=1
```

### Get appointment details

```http
GET /api/appointments/{idAppointment}
```

Returns `404 Not Found` when the appointment does not exist.

### Create appointment

```http
POST /api/appointments
Content-Type: application/json
```

Example body:

```json
{
  "idPatient": 1,
  "idDoctor": 2,
  "appointmentDate": "2026-08-15T10:30:00",
  "reason": "Control visit"
}
```

Business rules:

* patient must exist and be active,
* doctor must exist and be active,
* appointment date cannot be in the past,
* reason cannot be empty,
* reason cannot be longer than 250 characters,
* doctor cannot already have another scheduled appointment at the same time.

Returns:

* `201 Created` after successful creation,
* `400 Bad Request` for invalid input,
* `409 Conflict` for doctor appointment time conflict.

### Update appointment

```http
PUT /api/appointments/{idAppointment}
Content-Type: application/json
```

Example body:

```json
{
  "idPatient": 2,
  "idDoctor": 2,
  "appointmentDate": "2026-08-20T12:00:00",
  "status": "Scheduled",
  "reason": "Rescheduled dermatology consultation",
  "internalNotes": "Patient asked for a later hour."
}
```

Business rules:

* appointment must exist,
* patient must exist and be active,
* doctor must exist and be active,
* status must be one of: `Scheduled`, `Completed`, `Cancelled`,
* completed appointment date cannot be changed,
* scheduled appointment cannot conflict with another scheduled appointment of the same doctor.

Returns:

* `200 OK` after successful update,
* `400 Bad Request` for invalid input,
* `404 Not Found` when the appointment does not exist,
* `409 Conflict` for business conflicts.

### Delete appointment

```http
DELETE /api/appointments/{idAppointment}
```

Business rules:

* appointment must exist,
* completed appointment cannot be deleted.

Returns:

* `204 No Content` after successful deletion,
* `400 Bad Request` for invalid id,
* `404 Not Found` when the appointment does not exist,
* `409 Conflict` when trying to delete a completed appointment.

## Testing

Example requests are available in:

```text
Requests/appointments.http
```

The file contains sample requests for successful and invalid cases, including:

* getting all appointments,
* filtering appointments,
* getting appointment details,
* creating a valid appointment,
* testing appointment conflicts,
* updating appointments,
* deleting appointments,
* testing `400`, `404`, and `409` responses.
