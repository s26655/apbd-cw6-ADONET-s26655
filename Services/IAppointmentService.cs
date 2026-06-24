using ApbdCw6AdonetS26655.DTOs;

namespace ApbdCw6AdonetS26655.Services;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentListDto>> GetAppointmentsAsync(
        string? status,
        string? patientLastName,
        int? idDoctor,
        CancellationToken cancellationToken
    );

    Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(
        int idAppointment,
        CancellationToken cancellationToken
    );

    Task<int> CreateAppointmentAsync(
        CreateAppointmentRequestDto request,
        CancellationToken cancellationToken
    );

    Task<bool> UpdateAppointmentAsync(
        int idAppointment,
        UpdateAppointmentRequestDto request,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteAppointmentAsync(
        int idAppointment,
        CancellationToken cancellationToken
    );
}