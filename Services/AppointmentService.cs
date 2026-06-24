using ApbdCw6AdonetS26655.DTOs;

namespace ApbdCw6AdonetS26655.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
    }

    public Task<IReadOnlyList<AppointmentListDto>> GetAppointmentsAsync(
        string? status,
        string? patientLastName,
        int? idDoctor,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
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