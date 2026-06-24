using System.Data;
using ApbdCw6AdonetS26655.DTOs;
using Microsoft.Data.SqlClient;
using ApbdCw6AdonetS26655.Exceptions;

namespace ApbdCw6AdonetS26655.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;
    private static readonly HashSet<string> AllowedAppointmentStatuses = new(StringComparer.Ordinal)
{
    "Scheduled",
    "Completed",
    "Cancelled"
};

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
    }

    public async Task<IReadOnlyList<AppointmentListDto>> GetAppointmentsAsync(
        string? status,
        string? patientLastName,
        int? idDoctor,
        CancellationToken cancellationToken
    )
    {
        var appointments = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + N' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
              AND (@IdDoctor IS NULL OR a.IdDoctor = @IdDoctor)
            ORDER BY a.AppointmentDate;
            """, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
            string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim();

        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value =
            string.IsNullOrWhiteSpace(patientLastName) ? DBNull.Value : patientLastName.Trim();

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value =
            idDoctor.HasValue ? idDoctor.Value : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var idAppointmentOrdinal = reader.GetOrdinal("IdAppointment");
        var appointmentDateOrdinal = reader.GetOrdinal("AppointmentDate");
        var statusOrdinal = reader.GetOrdinal("Status");
        var reasonOrdinal = reader.GetOrdinal("Reason");
        var patientFullNameOrdinal = reader.GetOrdinal("PatientFullName");
        var patientEmailOrdinal = reader.GetOrdinal("PatientEmail");

        while (await reader.ReadAsync(cancellationToken))
        {
            appointments.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(idAppointmentOrdinal),
                AppointmentDate = reader.GetDateTime(appointmentDateOrdinal),
                Status = reader.GetString(statusOrdinal),
                Reason = reader.GetString(reasonOrdinal),
                PatientFullName = reader.GetString(patientFullNameOrdinal),
                PatientEmail = reader.GetString(patientEmailOrdinal)
            });
        }

        return appointments;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(
        int idAppointment,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
        SELECT
            a.IdAppointment,
            a.AppointmentDate,
            a.Status,
            a.Reason,
            a.InternalNotes,
            a.CreatedAt,
            p.IdPatient,
            p.FirstName AS PatientFirstName,
            p.LastName AS PatientLastName,
            p.Email AS PatientEmail,
            p.PhoneNumber AS PatientPhoneNumber,
            d.IdDoctor,
            d.FirstName AS DoctorFirstName,
            d.LastName AS DoctorLastName,
            d.LicenseNumber AS DoctorLicenseNumber,
            s.IdSpecialization,
            s.Name AS SpecializationName
        FROM dbo.Appointments a
        JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
        JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
        JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
        WHERE a.IdAppointment = @IdAppointment;
        """, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var internalNotesOrdinal = reader.GetOrdinal("InternalNotes");

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
            AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Reason = reader.GetString(reader.GetOrdinal("Reason")),
            InternalNotes = reader.IsDBNull(internalNotesOrdinal)
                ? null
                : reader.GetString(internalNotesOrdinal),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),

            IdPatient = reader.GetInt32(reader.GetOrdinal("IdPatient")),
            PatientFirstName = reader.GetString(reader.GetOrdinal("PatientFirstName")),
            PatientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
            PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
            PatientPhoneNumber = reader.GetString(reader.GetOrdinal("PatientPhoneNumber")),

            IdDoctor = reader.GetInt32(reader.GetOrdinal("IdDoctor")),
            DoctorFirstName = reader.GetString(reader.GetOrdinal("DoctorFirstName")),
            DoctorLastName = reader.GetString(reader.GetOrdinal("DoctorLastName")),
            DoctorLicenseNumber = reader.GetString(reader.GetOrdinal("DoctorLicenseNumber")),

            IdSpecialization = reader.GetInt32(reader.GetOrdinal("IdSpecialization")),
            SpecializationName = reader.GetString(reader.GetOrdinal("SpecializationName"))
        };
    }

    public async Task<int> CreateAppointmentAsync(
        CreateAppointmentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        ValidateCreateAppointmentRequest(request);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await ActivePatientExistsAsync(connection, request.IdPatient, cancellationToken))
        {
            throw new InvalidAppointmentRequestException("Patient does not exist or is inactive.");
        }

        if (!await ActiveDoctorExistsAsync(connection, request.IdDoctor, cancellationToken))
        {
            throw new InvalidAppointmentRequestException("Doctor does not exist or is inactive.");
        }

        if (await DoctorHasScheduledAppointmentAtAsync(
                connection,
                request.IdDoctor,
                request.AppointmentDate,
                cancellationToken
            ))
        {
            throw new AppointmentConflictException(
                "Doctor already has a scheduled appointment at the selected time."
            );
        }

        await using var command = new SqlCommand("""
        INSERT INTO dbo.Appointments
            (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
        OUTPUT INSERTED.IdAppointment
        VALUES
            (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason);
        """, connection);

        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason.Trim();

        var insertedId = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(insertedId);
    }

    public async Task<bool> UpdateAppointmentAsync(
        int idAppointment,
        UpdateAppointmentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        ValidateUpdateAppointmentRequest(request);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var existingAppointment = await GetAppointmentStateAsync(
            connection,
            idAppointment,
            cancellationToken
        );

        if (existingAppointment is null)
        {
            return false;
        }

        if (!await ActivePatientExistsAsync(connection, request.IdPatient, cancellationToken))
        {
            throw new InvalidAppointmentRequestException("Patient does not exist or is inactive.");
        }

        if (!await ActiveDoctorExistsAsync(connection, request.IdDoctor, cancellationToken))
        {
            throw new InvalidAppointmentRequestException("Doctor does not exist or is inactive.");
        }

        if (existingAppointment.Status == "Completed"
            && existingAppointment.AppointmentDate != request.AppointmentDate)
        {
            throw new AppointmentConflictException("Completed appointment date cannot be changed.");
        }

        var shouldCheckDoctorConflict =
            request.Status == "Scheduled"
            && (existingAppointment.IdDoctor != request.IdDoctor
                || existingAppointment.AppointmentDate != request.AppointmentDate);

        if (shouldCheckDoctorConflict
            && await DoctorHasScheduledAppointmentAtAsync(
                connection,
                request.IdDoctor,
                request.AppointmentDate,
                cancellationToken,
                idAppointment
            ))
        {
            throw new AppointmentConflictException(
                "Doctor already has a scheduled appointment at the selected time."
            );
        }

        await using var command = new SqlCommand("""
        UPDATE dbo.Appointments
        SET
            IdPatient = @IdPatient,
            IdDoctor = @IdDoctor,
            AppointmentDate = @AppointmentDate,
            Status = @Status,
            Reason = @Reason,
            InternalNotes = @InternalNotes
        WHERE IdAppointment = @IdAppointment;
        """, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = request.Status.Trim();
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason.Trim();
        command.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(request.InternalNotes)
                ? DBNull.Value
                : request.InternalNotes.Trim();

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0;
    }

    public Task<bool> DeleteAppointmentAsync(
        int idAppointment,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private static void ValidateCreateAppointmentRequest(CreateAppointmentRequestDto request)
    {
        if (request.IdPatient <= 0)
        {
            throw new InvalidAppointmentRequestException("Patient id must be greater than 0.");
        }

        if (request.IdDoctor <= 0)
        {
            throw new InvalidAppointmentRequestException("Doctor id must be greater than 0.");
        }

        if (request.AppointmentDate <= DateTime.UtcNow)
        {
            throw new InvalidAppointmentRequestException("Appointment date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidAppointmentRequestException("Reason cannot be empty.");
        }

        if (request.Reason.Trim().Length > 250)
        {
            throw new InvalidAppointmentRequestException("Reason cannot be longer than 250 characters.");
        }
    }

    private static async Task<bool> ActivePatientExistsAsync(
        SqlConnection connection,
        int idPatient,
        CancellationToken cancellationToken
    )
    {
        await using var command = new SqlCommand("""
        SELECT COUNT(1)
        FROM dbo.Patients
        WHERE IdPatient = @IdPatient
          AND IsActive = 1;
        """, connection);

        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ActiveDoctorExistsAsync(
        SqlConnection connection,
        int idDoctor,
        CancellationToken cancellationToken
    )
    {
        await using var command = new SqlCommand("""
        SELECT COUNT(1)
        FROM dbo.Doctors
        WHERE IdDoctor = @IdDoctor
          AND IsActive = 1;
        """, connection);

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> DoctorHasScheduledAppointmentAtAsync(
        SqlConnection connection,
        int idDoctor,
        DateTime appointmentDate,
        CancellationToken cancellationToken,
        int? excludedAppointmentId = null
    )
    {
        await using var command = new SqlCommand("""
        SELECT COUNT(1)
        FROM dbo.Appointments
        WHERE IdDoctor = @IdDoctor
          AND AppointmentDate = @AppointmentDate
          AND Status = N'Scheduled'
          AND (@ExcludedAppointmentId IS NULL OR IdAppointment <> @ExcludedAppointmentId);
        """, connection);

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;
        command.Parameters.Add("@ExcludedAppointmentId", SqlDbType.Int).Value =
            excludedAppointmentId.HasValue ? excludedAppointmentId.Value : DBNull.Value;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) > 0;
    }

    private static void ValidateUpdateAppointmentRequest(UpdateAppointmentRequestDto request)
    {
        if (request.IdPatient <= 0)
        {
            throw new InvalidAppointmentRequestException("Patient id must be greater than 0.");
        }

        if (request.IdDoctor <= 0)
        {
            throw new InvalidAppointmentRequestException("Doctor id must be greater than 0.");
        }

        if (!AllowedAppointmentStatuses.Contains(request.Status.Trim()))
        {
            throw new InvalidAppointmentRequestException(
                "Status must be one of: Scheduled, Completed, Cancelled."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidAppointmentRequestException("Reason cannot be empty.");
        }

        if (request.Reason.Trim().Length > 250)
        {
            throw new InvalidAppointmentRequestException("Reason cannot be longer than 250 characters.");
        }

        if (request.InternalNotes is not null && request.InternalNotes.Trim().Length > 500)
        {
            throw new InvalidAppointmentRequestException(
                "Internal notes cannot be longer than 500 characters."
            );
        }
    }

    private static async Task<AppointmentState?> GetAppointmentStateAsync(
        SqlConnection connection,
        int idAppointment,
        CancellationToken cancellationToken
    )
    {
        await using var command = new SqlCommand("""
        SELECT IdDoctor, AppointmentDate, Status
        FROM dbo.Appointments
        WHERE IdAppointment = @IdAppointment;
        """, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AppointmentState(
            reader.GetInt32(reader.GetOrdinal("IdDoctor")),
            reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
            reader.GetString(reader.GetOrdinal("Status"))
        );
    }

    private sealed record AppointmentState(
    int IdDoctor,
    DateTime AppointmentDate,
    string Status
);

}