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

    public Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(
        int idAppointment,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
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