using API.Domain.DTOs;
using API.Domain.Models;
using API.Infrastructure.Data;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class LeadListService : ILeadListService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LeadListService> _logger;
    private readonly IQueuePublisher _queuePublisher;
    private readonly IKubernetesJobService _kubernetesJobService;

    public LeadListService(AppDbContext context, ILogger<LeadListService> logger, IQueuePublisher queuePublisher, IKubernetesJobService kubernetesJobService)
    {
        _context = context;
        _logger = logger;
        _queuePublisher = queuePublisher;
        _kubernetesJobService = kubernetesJobService;
    }


    public async Task<PagedResult<LeadListResponse>> GetAll(int page, int pageSize, string? status, string? q)
    {
        var query = _context.LeadLists.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeadListStatus>(status, true, out var statusEnum))
        {
            query = query.Where(l => l.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(l => l.Name.Contains(q));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => MapToResponse(l))
            .ToListAsync();

        return new PagedResult<LeadListResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LeadListResponse?> GetById(Guid id)
    {
        var leadList = await _context.LeadLists.FindAsync(id);
        if (leadList != null)
        {
            return MapToResponse(leadList);
        }

        _logger.LogError("LeadList with id {id} was not found", id);
        return null;
    }

    //TODO: Implement the rabbitMq publisher
    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Create(LeadListCreateRequest request)
    {
        
        var leadList = new LeadList
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SourceUrl = request.SourceUrl.Trim(),
            Status = LeadListStatus.Pending,
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lead list {LeadListId} created with CorrelationId {CorrelationId}", leadList.Id,
            leadList.CorrelationId);

        var msg = new LeadListCreatedMsg
        {
            LeadListId = leadList.Id,
            CorrelationId = leadList.CorrelationId,
            SourceUrl = leadList.SourceUrl,
            CreatedAt = leadList.CreatedAt
        };
        
        await _queuePublisher.PublishLeadListCreated(msg);
        await _kubernetesJobService.CreateWorkerJobAsync(leadList.Id, leadList.CorrelationId);

        return (MapToResponse(leadList), null);
    }

    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Update(Guid id, LeadListCreateRequest request)
    {
        var leadList = await _context.LeadLists.FindAsync(id);

        if (leadList == null)
        {
            _logger.LogError("Lead with id {id} was not found", id);
            return (null, "Lead list not found");
        }

        if (leadList.Status != LeadListStatus.Pending && leadList.Status != LeadListStatus.Failed)
        {
            return (null,
                $"Cannot update lead list with the status {leadList.Status}. Only Pending or Failed can be updated");
        }

       
        leadList.Name = request.Name.Trim();
        leadList.SourceUrl = request.SourceUrl.Trim();
        leadList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead list {LeadListId} updated", leadList.Id);

        return (MapToResponse(leadList), null);
    }

    public async Task<(bool Success, string? ErrorMessage)> Delete(Guid id)
    {
        var leadList = _context.LeadLists.Find(id);
        if (leadList == null)
        {
            _logger.LogError("Lead list with id {id} was not found", id);
            return (false, "Lead not found");
        }

        if (leadList.Status != LeadListStatus.Pending && leadList.Status != LeadListStatus.Failed)
        {
            return (false, $"Cannot delete lead list with status {leadList.Status}. Only Pending or Failed are allowed.");
        }
        
        _context.LeadLists.Remove(leadList);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lead list {LeadListId} deleted",  leadList.Id);
        
        return (true, null);
    }

    
    //TODO: Implement the rabbitMq publisher
    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Reprocess(Guid id)
    {
        var leadList = await _context.LeadLists.FindAsync(id);
        if (leadList == null)
        {
            _logger.LogError("Lead list with id {id} was not found", id);
            return (null, "Lead list not found");
        }

        if (leadList.Status != LeadListStatus.Pending)
        {
            return (null,
                $"Cannot reprocess leadlist with status {leadList.Status}. Only Failed list can be reprocessed. ");
        }

        leadList.Status = LeadListStatus.Pending;
        leadList.ProcessedCount = 0;
        leadList.ErrorMessage = null;
        leadList.CorrelationId = Guid.NewGuid();
        leadList.UpdatedAt = DateTime.UtcNow;
        
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lead list {LeadListId} marked for reprocessing", leadList.Id);


        return (MapToResponse(leadList), null);
    }

    private static LeadListResponse MapToResponse(LeadList leadList)
    {
        return new LeadListResponse
        {
            Id = leadList.Id,
            Name = leadList.Name,
            SourceUrl = leadList.SourceUrl,
            Status = leadList.Status.ToString(),
            ProcessedCount = leadList.ProcessedCount,
            ErrorMessage = leadList.ErrorMessage,
            CreatedAt = leadList.CreatedAt,
            UpdatedAt = leadList.UpdatedAt,
            CorrelationId = leadList.CorrelationId
        };
    }
}



//
// using System.Text;
// using System.Text.Json;
// using RabbitMQ.Client;
//
// namespace LeadListsApi.Services;
//
// public class RabbitMqPublisher : IRabbitMqPublisher
// {
//     private readonly IConnection _connection;
//     private readonly IModel _channel;
//     private readonly ILogger<RabbitMqPublisher> _logger;
//     private readonly string _exchangeName;
//     private readonly string _routingKey;
//
//     public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
//     {
//         _logger = logger;
//
//         var host = configuration["RabbitMQ:Host"] ?? "localhost";
//         var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
//         var username = configuration["RabbitMQ:Username"] ?? "admin";
//         var password = configuration["RabbitMQ:Password"] ?? "admin123";
//         _exchangeName = configuration["RabbitMQ:Exchange"] ?? "leadlists";
//         _routingKey = configuration["RabbitMQ:RoutingKey"] ?? "leadlist.created";
//
//         try
//         {
//             var factory = new ConnectionFactory
//             {
//                 HostName = host,
//                 Port = port,
//                 UserName = username,
//                 Password = password,
//                 DispatchConsumersAsync = true
//             };
//
//             _connection = factory.CreateConnection();
//             _channel = _connection.CreateModel();
//
//             // Declarar exchange (tipo topic)
//             _channel.ExchangeDeclare(
//                 exchange: _exchangeName,
//                 type: ExchangeType.Topic,
//                 durable: true,
//                 autoDelete: false);
//
//             // Declarar fila para o worker
//             _channel.QueueDeclare(
//                 queue: "leadlists.worker",
//                 durable: true,
//                 exclusive: false,
//                 autoDelete: false);
//
//             // Bind da fila com a exchange
//             _channel.QueueBind(
//                 queue: "leadlists.worker",
//                 exchange: _exchangeName,
//                 routingKey: _routingKey);
//
//             _logger.LogInformation(
//                 "RabbitMQ conectado: {Host}:{Port}, Exchange: {Exchange}",
//                 host, port, _exchangeName);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Erro ao conectar no RabbitMQ");
//             throw;
//         }
//     }
//
//     public Task PublishLeadListCreated(Guid leadListId, Guid correlationId, string sourceUrl)
//     {
//         try
//         {
//             var message = new
//             {
//                 leadListId = leadListId,
//                 correlationId = correlationId,
//                 sourceUrl = sourceUrl,
//                 createdAt = DateTime.UtcNow
//             };
//
//             var json = JsonSerializer.Serialize(message);
//             var body = Encoding.UTF8.GetBytes(json);
//
//             var properties = _channel.CreateBasicProperties();
//             properties.Persistent = true;
//             properties.ContentType = "application/json";
//             properties.CorrelationId = correlationId.ToString();
//
//             _channel.BasicPublish(
//                 exchange: _exchangeName,
//                 routingKey: _routingKey,
//                 basicProperties: properties,
//                 body: body);
//
//             _logger.LogInformation(
//                 "Mensagem publicada: LeadListId={LeadListId}, CorrelationId={CorrelationId}",
//                 leadListId, correlationId);
//
//             return Task.CompletedTask;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(
//                 ex,
//                 "Erro ao publicar mensagem para LeadListId={LeadListId}",
//                 leadListId);
//             throw;
//         }
//     }
//
//     public void Dispose()
//     {
//         _channel?.Close();
//         _connection?.Close();
//         _channel?.Dispose();
//         _connection?.Dispose();
//     }
// }