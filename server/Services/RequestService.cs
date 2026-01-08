using MediAid.Data;
using MediAid.DTOs;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class RequestService : IRequestService
{
    private readonly MongoDbContext _context;

    public RequestService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Request?> CreateRequestAsync(string patientId, CreateRequestDto dto)
    {
        var request = new Request
        {
            PatientId = patientId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Urgency = dto.Urgency,
            RequestedDate = dto.RequestedDate,
            Address = dto.Address,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Documents = dto.Documents,
            RequiresExpertValidation = dto.RequiresExpertValidation,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            request.Location = new Location
            {
                Type = "Point",
                Coordinates = new double[] { dto.Longitude.Value, dto.Latitude.Value }
            };
        }

        await _context.Requests.InsertOneAsync(request);
        return request;
    }

    public async Task<Request?> CreateRequestFromWizardAsync(string patientId, CreateRequestWizardDto dto)
    {
        var request = new Request
        {
            PatientId = patientId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Urgency = dto.Urgency,
            RequestedDate = dto.RequestedDate,
            Address = dto.Address,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Documents = dto.Documents,
            RequiresExpertValidation = dto.RequiresExpertValidation,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            request.Location = new Location
            {
                Type = "Point",
                Coordinates = new double[] { dto.Longitude.Value, dto.Latitude.Value }
            };
        }

        await _context.Requests.InsertOneAsync(request);
        return request;
    }

    public async Task<Request?> GetRequestByIdAsync(string requestId)
    {
        return await _context.Requests.Find(r => r.Id == requestId).FirstOrDefaultAsync();
    }

    public async Task<List<Request>> GetRequestsByPatientIdAsync(string patientId)
    {
        return await _context.Requests.Find(r => r.PatientId == patientId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Request>> GetAvailableRequestsAsync(double? latitude, double? longitude, double? radiusKm, string? category, string? urgency)
    {
        var filterBuilder = Builders<Request>.Filter;
        var filters = new List<FilterDefinition<Request>>
        {
            filterBuilder.Eq(r => r.Status, "Open")
        };

        if (!string.IsNullOrEmpty(category))
        {
            filters.Add(filterBuilder.Eq(r => r.Category, category));
        }

        if (!string.IsNullOrEmpty(urgency))
        {
            filters.Add(filterBuilder.Eq(r => r.Urgency, urgency));
        }

        var filter = filterBuilder.And(filters);

        var requests = await _context.Requests.Find(filter).ToListAsync();

        // Filter by distance if coordinates provided
        // IMPORTANT: Les demandes sans localisation sont incluses pour permettre aux aidants de les voir
        if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
        {
            var filteredRequests = new List<Request>();
            
            foreach (var request in requests)
            {
                // Si la demande n'a pas de localisation, on l'inclut quand même
                if (request.Location?.Coordinates == null || request.Location.Coordinates.Length < 2)
                {
                    filteredRequests.Add(request);
                    continue;
                }

                // Sinon, on vérifie la distance
                var distance = CalculateDistance(latitude.Value, longitude.Value,
                    request.Location.Coordinates[1], request.Location.Coordinates[0]);
                
                if (distance <= radiusKm.Value)
                {
                    filteredRequests.Add(request);
                }
            }
            
            requests = filteredRequests;
        }

        return requests.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<List<Request>> GetAllRequestsWithLocationAsync()
    {
        var filter = Builders<Request>.Filter.And(
            Builders<Request>.Filter.Ne(r => r.Location, null),
            Builders<Request>.Filter.In(r => r.Status, new[] { "Open", "Assigned" })
        );
        return await _context.Requests.Find(filter).ToListAsync();
    }

    public async Task<bool> UpdateRequestAsync(Request request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        var result = await _context.Requests.ReplaceOneAsync(r => r.Id == request.Id, request);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> CancelRequestAsync(string requestId, string patientId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null || request.PatientId != patientId)
        {
            return false;
        }

        if (request.Status == "Completed" || request.Status == "Cancelled")
        {
            return false;
        }

        request.Status = "Cancelled";
        request.UpdatedAt = DateTime.UtcNow;
        return await UpdateRequestAsync(request);
    }

    public async Task<bool> AssignAidantAsync(string requestId, string aidantId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return false;
        }

        request.AssignedAidantId = aidantId;
        request.Status = "Assigned";
        request.UpdatedAt = DateTime.UtcNow;
        return await UpdateRequestAsync(request);
    }

    public async Task<bool> DeleteRequestAsync(string requestId, string patientId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null || request.PatientId != patientId)
        {
            return false;
        }

        // Contraintes : on ne peut supprimer que les demandes annulées ou complétées
        // Pas les demandes en cours (Open, Assigned, InProgress)
        if (request.Status != "Cancelled" && request.Status != "Completed")
        {
            return false;
        }

        var result = await _context.Requests.DeleteOneAsync(r => r.Id == requestId);
        return result.DeletedCount > 0;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in kilometers
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}


