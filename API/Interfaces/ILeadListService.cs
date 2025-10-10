using API.Domain.DTOs;

namespace API.Interfaces;

public interface ILeadListService
{
    Task<PagedResult<LeadListResponse>> GetAll(int page, int pageSize, string? status, string? q);
    Task<LeadListResponse?> GetById(Guid id);
   Task<(LeadListResponse? Response, string? ErrorMessage)>Create(LeadListCreateRequest request);
    Task<(LeadListResponse? Response, string? ErrorMessage)> Update(Guid id, LeadListCreateRequest request);
    Task<(bool Success, string? ErrorMessage)> Delete(Guid id);
    Task<(LeadListResponse? Response, string? ErrorMessage)> Reprocess(Guid id);
    
}