using System.Data;
using ApbdCw6AdonetS26655.DTOs;
using Microsoft.Data.SqlClient;

namespace ApbdCw6AdonetS26655.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

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

    public Task<int> CreateAppointmentAsync(
        CreateAppointmentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAppointmentAsync(
        int idAppointment,
        UpdateAppointmentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAppointmentAsync(
        int idAppointment,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}