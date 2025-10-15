using FluentAssertions;
using leadListAPI.Domain.DTOs;
using leadListAPI.Domain.Models;
using leadListAPI.Infrastructure.Data;
using leadListAPI.Interfaces;
using leadListAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LeadListAPI_test.Services;

public class LeadListServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<LeadListService>> _loggerMock;
    private readonly Mock<IJobCreator> _jobCreatorMock;
    private readonly LeadListService _sut;

    public LeadListServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _loggerMock = new Mock<ILogger<LeadListService>>();
        _jobCreatorMock = new Mock<IJobCreator>();

        _sut = new LeadListService(_context, _loggerMock.Object, _jobCreatorMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllItems_WhenNoFiltersAreApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "batata" },
            new LeadList { Id = Guid.NewGuid(), Name = "Kiwi" }
        };
        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();

        var result = await _sut.GetAll(page: 1, pageSize: 10, status: null, q: null);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturnFilteredItems_WhenStatusFilterIsApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "Pending 1", Status = LeadListStatus.Pending },
            new LeadList { Id = Guid.NewGuid(), Name = "Completed 1", Status = LeadListStatus.Completed },
            new LeadList { Id = Guid.NewGuid(), Name = "Pending 2", Status = LeadListStatus.Pending }
        };
        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();
        
        var result = await _sut.GetAll(page: 1, pageSize: 10, status: "Pending", q: null);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.Status == "Pending");
    }
    
    [Fact]
    public async Task GetAll_ShouldReturnFilteredItems_WhenQueryFilterIsApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "batata" },
            new LeadList { Id = Guid.NewGuid(), Name = "kiwi" },
            new LeadList { Id = Guid.NewGuid(), Name = "kiwi com batata" }
        };
        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();
        
        var result = await _sut.GetAll(page: 1, pageSize: 10, status: null, q: "batata");

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.Name.Contains("batata"));
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectItem_WhenIdExists()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Target Item" };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();
        
        var result = await _sut.GetById(leadListId);
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(leadListId);
        result.Name.Should().Be("Target Item");
    }

    [Fact]
    public async Task Update_ShouldChangeData_WhenItemExistsAndIsPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Old Name", Status = LeadListStatus.Pending };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();
        
        var request = new LeadListCreateRequest { Name = "New Name", SourceUrl = "https://new.com" };

        var (response, errorMessage) = await _sut.Update(leadListId, request);

        response.Should().NotBeNull();
        errorMessage.Should().BeNull();
        response!.Name.Should().Be("New Name");
    }
    
    [Fact]
    public async Task Delete_ShouldRemoveItem_WhenItemExistsAndIsPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "To Delete", Status = LeadListStatus.Pending };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var (success, errorMessage) = await _sut.Delete(leadListId);

        success.Should().BeTrue();
        errorMessage.Should().BeNull();
        
        var deletedItem = await _context.LeadLists.FindAsync(leadListId);
        deletedItem.Should().BeNull();
    }
    
    [Fact]
    public async Task Delete_ShouldFail_WhenStatusIsNotPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Processing Item", Status = LeadListStatus.Processing };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var (success, errorMessage) = await _sut.Delete(leadListId);

        success.Should().BeFalse();
        errorMessage.Should().Contain($"Cannot delete lead list with status {leadList.Status}. Only Pending or Failed are allowed.");
    }
}