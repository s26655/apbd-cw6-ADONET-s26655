using ApbdCw6AdonetS26655.DTOs;
using ApbdCw6AdonetS26655.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApbdCw6AdonetS26655.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<AppointmentListDto>>> GetAppointments(
        [FromQuery] string? status,
        [FromQuery] string? patientLastName,
        [FromQuery] int? idDoctor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(
                status,
                patientLastName,
                idDoctor,
                cancellationToken
            );

            return Ok(appointments);
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Message = "A database error occurred while retrieving appointments."
                }
            );
        }
    }
}