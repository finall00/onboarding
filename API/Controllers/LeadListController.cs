using API.DTOs;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace API.Controllers;

[ApiController]
[Route("lead-lists")]
[Produces("application/json")]
public class LeadListController : ControllerBase
{
    private readonly ILogger<LeadListController> _logger;
    private readonly ILeadListService _leadListService;
    
    private readonly IValidator<LeadListCreateRequest> _leadListValidator;


    public LeadListController(
        ILogger<LeadListController> logger,
        ILeadListService leadListService,
        IValidator<LeadListCreateRequest> leadListValidator
    )
    {
        _logger = logger;
        _leadListService = leadListService;
        _leadListValidator = leadListValidator;
    }

    /// <summary>
    /// Retrieves a paginated list of lead lists.
    /// </summary>
    /// <param name="page">The page number to retrieve, starting at 1.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="status">Filters the list by status (e.g., Pending, Completed). Case-insensitive.</param>
    /// <param name="q">A query string to search for in the lead list names.</param>
    /// <returns>A paginated list of lead lists.</returns>
    /// <response code="200">Returns the paginated list of lead lists.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LeadListResponse>), 200)]
    public async Task<ActionResult<LeadListResponse>> GetLeadLists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null
    )
    {
        var result = await _leadListService.GetAll(page, pageSize, status, q);
        return Ok(result);
    }


    /// <summary>
    /// Retrieves a specific lead list by its unique ID.
    /// </summary>
    /// <param name="id">The UUID of the lead list.</param>
    /// <returns>The details of the requested lead list.</returns>
    /// <response code="200">Returns the requested lead list.</response>
    /// <response code="404">If the lead list with the specified ID is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeadListResponse), 200)]
    [ProducesResponseType(typeof(LeadListResponse), 404)]
    public async Task<ActionResult<LeadListResponse>> GetLeadListById(Guid id)
    {
        var leadList = await _leadListService.GetById(id);
        if (leadList == null)
        {
            return NotFound(new { message = "Lead list not found" });
        }

        return Ok(leadList);
    }


    /// <summary>
    /// Creates a new lead list.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="request">The data for the new lead list.</param>
    /// <returns>The newly created lead list.</returns>
    /// <response code="201">Returns the newly created lead list and its location.</response>
    /// <response code="400">If the provided data is invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LeadListResponse), 201)]
    [ProducesResponseType(typeof(LeadListResponse), 400)]
    public async Task<ActionResult<LeadListResponse>> CreateLeadList(
        [FromBody] LeadListCreateRequest request
    )
    {
        
        var validationResult = await _leadListValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        
        var (response, errorMessage) = await _leadListService.Create(request);

        if (response != null)
        {
            return CreatedAtAction(
                nameof(GetLeadListById), 
                new { id = response.Id },
                response);
        }
        
        return BadRequest(errorMessage);
    }

    
    // TODO: pergutar se poder reenserir p valor caso seja vazio a resposta 
    /// <summary>
    /// Updates an existing lead list.
    /// </summary>
    /// <param name="id">The UUID of the lead list to update.</param>
    /// <param name="request">The updated data for the lead list.</param>
    /// <returns>The updated lead list.</returns>
    /// <response code="200">Returns the updated lead list.</response>
    /// <response code="400">If the update is not allowed due to the list's status or if the data is invalid.</response>
    /// <response code="404">If the lead list with the specified ID is not found.</response>
    [ProducesResponseType(typeof(LeadListResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    [HttpPut("{id}")]
    public async Task<ActionResult<LeadListResponse>> UpdateLeadList(
        Guid id,
        [FromQuery] LeadListCreateRequest request
    )
    {
        
        var validationResult = await _leadListValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        
        var (response, errorMessage) = await _leadListService.Update(id, request);

        if (errorMessage == null)
        {
            return Ok(response);
        }

        if (errorMessage.Contains("not found"))
        {
            return NotFound(new { message = errorMessage });
        }

        return BadRequest(new { message = errorMessage });
    }


    /// <summary>
    /// Deletes a lead list.
    /// </summary>
    /// <param name="id">The UUID of the lead list to delete.</param>
    /// <returns>An empty response indicating success.</returns>
    /// <response code="204">The lead list was successfully deleted.</response>
    /// <response code="400">If the deletion is not allowed due to the list's status.</response>
    /// <response code="404">If the lead list with the specified ID is not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> DeleteLeadList(Guid id)
    {
        var (success, errorMessage) = await _leadListService.Delete(id);

        if (success)
        {
            return NoContent();
        }

        if (errorMessage != null && errorMessage.Contains("not found"))
        {
            return NotFound(new { message = errorMessage });
        }

        return BadRequest(new { message = errorMessage });
    }


    /// <summary>
    /// Triggers the reprocessing of a failed lead list.
    /// </summary>
    /// <param name="id">The UUID of the lead list to reprocess.</param>
    /// <returns>The lead list with its updated status and new CorrelationId.</returns>
    /// <response code="200">Returns the lead list that is now queued for reprocessing.</response>
    /// <response code="400">If the lead list's status is not 'Failed'.</response>
    /// <response code="404">If the lead list with the specified ID is not found.</response>
    [HttpPost("{id}/reprocess")]
    [ProducesResponseType(typeof(LeadListResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<LeadListResponse>> ReprocessLeadList(Guid id)
    {
        var (response, errorMessage) = await _leadListService.Reprocess(id);

        if (errorMessage == null)
        {
            return Ok(response);
        }

        if (errorMessage.Contains("not found"))
        {
            return NotFound(new { message = errorMessage });
        }

        return BadRequest(new { message = errorMessage });
    }
}