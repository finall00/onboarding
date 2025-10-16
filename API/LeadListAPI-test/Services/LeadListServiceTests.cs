using FluentAssertions;
using leadListAPI.Domain.DTOs;
using leadListAPI.Domain.Models;
using leadListAPI.Infrastructure.Data;
using leadListAPI.Interfaces;
using leadListAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LeadListAPI_test.Services;

public class LeadListServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<LeadListService>> _loggerMock;
    private readonly Mock<IJobCreator> _jobCreatorMock;
    private readonly LeadListService _service;

    public LeadListServiceTests()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<LeadListService>>();
        _jobCreatorMock = new Mock<IJobCreator>();

        _service = new LeadListService(_context, _loggerMock.Object, _jobCreatorMock.Object);
    }


    //GetAll Tests
    [Fact]
    public async Task GetAll_ShouldReturnAllLead_WhenNoFiltersAreApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "batata" },
            new LeadList { Id = Guid.NewGuid(), Name = "Kiwi" }
        };

        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();

        var result = await _service.GetAll(page: 1, pageSize: 10, status: null, q: null);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturnFilteredLead_WhenStatusFilterIsApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "batata Pending 1", Status = LeadListStatus.Pending },
            new LeadList { Id = Guid.NewGuid(), Name = "batata Completed 1", Status = LeadListStatus.Completed },
            new LeadList { Id = Guid.NewGuid(), Name = "batata Pending 2", Status = LeadListStatus.Pending }
        };

        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();

        var result = await _service.GetAll(page: 1, pageSize: 10, status: "Pending", q: null);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.Status == "Pending");
    }

    [Fact]
    public async Task GetAll_ShouldReturnFilteredLead_WhenQueryFilterIsApplied()
    {
        var leadLists = new[]
        {
            new LeadList { Id = Guid.NewGuid(), Name = "batata" },
            new LeadList { Id = Guid.NewGuid(), Name = "kiwi" },
            new LeadList { Id = Guid.NewGuid(), Name = "kiwi com batata" }
        };
        _context.LeadLists.AddRange(leadLists);
        await _context.SaveChangesAsync();

        var result = await _service.GetAll(page: 1, pageSize: 10, status: null, q: "batata");

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.Name.Contains("batata"));
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectLead_WhenIdExists()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Target Item" };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var result = await _service.GetById(leadListId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(leadListId);
        result.Name.Should().Be("Target Item");
    }

    // Create Test
    [Fact]
    public async Task Create_ShouldCreateSuccessfully()
    {
        var leadList = new LeadListCreateRequest
        {
            Name = "Batata",
            SourceUrl = "http://batatas.com"
        };

        var (response, errorMessage) = await _service.Create(leadList);

        response.Should().NotBeNull();
        errorMessage.Should().BeNull();
        response.Name.Should().Be("Batata");
        response.SourceUrl.Should().Be("http://batatas.com");
        
        var saved = await _context.LeadLists.FindAsync(response.Id);
        saved.Should().NotBeNull();
    }
    
    // Update Test
    [Fact]
    public async Task Update_ShouldChangeData_WhenLeadExistsAndIsPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "batata podre", Status = LeadListStatus.Pending };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();


        var request = new LeadListCreateRequest { Name = "batata doce", SourceUrl = "https://bata.com" };
        var (response, errorMessage) = await _service.Update(leadListId, request);

        response.Should().NotBeNull();
        errorMessage.Should().BeNull();
        response.Name.Should().Be("batata doce");
    }

    [Fact]
    public async Task Update_ShouldFailed_WhenLeadExistsButStatusIsComplete()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "batata podre", Status = LeadListStatus.Completed };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var request = new LeadListCreateRequest { Name = "batata doce", SourceUrl = "https://new.com" };
        var (response, errorMessage) = await _service.Update(leadListId, request);

        response.Should().BeNull();
        errorMessage.Should()
            .Be($"Cannot update lead list with the status {leadList.Status}. Only Pending or Failed can be updated");
    }

    [Fact]
    public async Task Update_ShouldFailed_WhenLeadIdIsNotFound()
    {
        var request = new LeadListCreateRequest { Name = "batata doce", SourceUrl = "https://new.com" };
        var (response, errorMessage) = await _service.Update(Guid.NewGuid(), request);

        response.Should().BeNull();
        errorMessage.Should().Be("Lead list not found");
    }

    // Delete Tests
    [Fact]
    public async Task Delete_ShouldRemoveItem_WhenItemExistsAndIsPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Batata", Status = LeadListStatus.Pending };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var (success, errorMessage) = await _service.Delete(leadListId);

        success.Should().BeTrue();
        errorMessage.Should().BeNull();

        var deletedItem = await _context.LeadLists.FindAsync(leadListId);
        deletedItem.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldRemoveItem_WhenItemExistsAndIsFailed()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Batata", Status = LeadListStatus.Failed };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var (success, errorMessage) = await _service.Delete(leadListId);

        success.Should().BeTrue();
        errorMessage.Should().BeNull();

        var deletedItem = await _context.LeadLists.FindAsync(leadListId);
        deletedItem.Should().BeNull();
    }


    [Fact]
    public async Task Delete_ShouldFailed_WhenStatusIsNotPending()
    {
        var leadListId = Guid.NewGuid();
        var leadList = new LeadList { Id = leadListId, Name = "Processing Item", Status = LeadListStatus.Processing };
        _context.LeadLists.Add(leadList);
        await _context.SaveChangesAsync();

        var (success, errorMessage) = await _service.Delete(leadListId);

        success.Should().BeFalse();
        errorMessage.Should()
            .Contain($"Cannot delete lead list with status {leadList.Status}. Only Pending or Failed are allowed.");
    }
}