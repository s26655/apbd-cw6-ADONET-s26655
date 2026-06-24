using ApbdCw6AdonetS26655.DTOs;
using ApbdCw6AdonetS26655.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ApbdCw6AdonetS26655.Exceptions;

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

    [HttpGet("{idAppointment:int}")]
    [ProducesResponseType(typeof(AppointmentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppointmentDetailsDto>> GetAppointmentById(
        [FromRoute] int idAppointment,
        CancellationToken cancellationToken
    )
    {
        if (idAppointment <= 0)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Appointment id must be greater than 0."
            });
        }

        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(
                idAppointment,
                cancellationToken
            );

            if (appointment is null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = $"Appointment with id {idAppointment} was not found."
                });
            }

            return Ok(appointment);
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Message = "A database error occurred while retrieving appointment details."
                }
            );
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppointmentDetailsDto>> CreateAppointment(
    [FromBody] CreateAppointmentRequestDto request,
    CancellationToken cancellationToken
)
    {
        try
        {
            var idAppointment = await _appointmentService.CreateAppointmentAsync(
                request,
                cancellationToken
            );

            var createdAppointment = await _appointmentService.GetAppointmentByIdAsync(
                idAppointment,
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetAppointmentById),
                new { idAppointment },
                createdAppointment
            );
        }
        catch (InvalidAppointmentRequestException ex)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Message = "A database error occurred while creating the appointment."
                }
            );
        }
    }

    [HttpPut("{idAppointment:int}")]
    [ProducesResponseType(typeof(AppointmentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppointmentDetailsDto>> UpdateAppointment(
    [FromRoute] int idAppointment,
    [FromBody] UpdateAppointmentRequestDto request,
    CancellationToken cancellationToken
)
    {
        if (idAppointment <= 0)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Appointment id must be greater than 0."
            });
        }

        try
        {
            var wasUpdated = await _appointmentService.UpdateAppointmentAsync(
                idAppointment,
                request,
                cancellationToken
            );

            if (!wasUpdated)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = $"Appointment with id {idAppointment} was not found."
                });
            }

            var updatedAppointment = await _appointmentService.GetAppointmentByIdAsync(
                idAppointment,
                cancellationToken
            );

            return Ok(updatedAppointment);
        }
        catch (InvalidAppointmentRequestException ex)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Message = "A database error occurred while updating the appointment."
                }
            );
        }
    }

    [HttpDelete("{idAppointment:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAppointment(
    [FromRoute] int idAppointment,
    CancellationToken cancellationToken
)
    {
        if (idAppointment <= 0)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Appointment id must be greater than 0."
            });
        }

        try
        {
            var wasDeleted = await _appointmentService.DeleteAppointmentAsync(
                idAppointment,
                cancellationToken
            );

            if (!wasDeleted)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = $"Appointment with id {idAppointment} was not found."
                });
            }

            return NoContent();
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Message = "A database error occurred while deleting the appointment."
                }
            );
        }
    }

}