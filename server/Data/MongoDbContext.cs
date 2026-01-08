using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        InitializeIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Patient> Patients => _database.GetCollection<Patient>("Patients");
    public IMongoCollection<Aidant> Aidants => _database.GetCollection<Aidant>("Aidants");
    public IMongoCollection<Expert> Experts => _database.GetCollection<Expert>("Experts");
    public IMongoCollection<Request> Requests => _database.GetCollection<Request>("Requests");
    public IMongoCollection<Proposal> Proposals => _database.GetCollection<Proposal>("Proposals");
    public IMongoCollection<Message> Messages => _database.GetCollection<Message>("Messages");
    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("Reviews");
    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("Notifications");
    public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("AuditLogs");
    public IMongoCollection<AidantComment> AidantComments => _database.GetCollection<AidantComment>("AidantComments");
    public IMongoCollection<MissionProof> MissionProofs => _database.GetCollection<MissionProof>("MissionProofs");
    public IMongoCollection<MissionCheckIn> MissionCheckIns => _database.GetCollection<MissionCheckIn>("MissionCheckIns");
    public IMongoCollection<SafetyIncident> SafetyIncidents => _database.GetCollection<SafetyIncident>("SafetyIncidents");
    public IMongoCollection<Planning> Plannings => _database.GetCollection<Planning>("Plannings");

    private void InitializeIndexes()
    {
        // User indexes
        Users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));

        // Request indexes - Geospatial index for location queries
        Requests.Indexes.CreateOne(new CreateIndexModel<Request>(
            Builders<Request>.IndexKeys.Geo2DSphere(r => r.Location)));

        Requests.Indexes.CreateOne(new CreateIndexModel<Request>(
            Builders<Request>.IndexKeys.Ascending(r => r.PatientId)));

        Requests.Indexes.CreateOne(new CreateIndexModel<Request>(
            Builders<Request>.IndexKeys.Ascending(r => r.Status)));

        // Proposal indexes
        Proposals.Indexes.CreateOne(new CreateIndexModel<Proposal>(
            Builders<Proposal>.IndexKeys.Ascending(p => p.RequestId)));

        Proposals.Indexes.CreateOne(new CreateIndexModel<Proposal>(
            Builders<Proposal>.IndexKeys.Ascending(p => p.AidantId)));

        // Message indexes
        Messages.Indexes.CreateOne(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys.Ascending(m => m.RequestId)));

        Messages.Indexes.CreateOne(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys.Ascending(m => m.CreatedAt)));

        // Review indexes
        Reviews.Indexes.CreateOne(new CreateIndexModel<Review>(
            Builders<Review>.IndexKeys.Ascending(r => r.AidantId)));

        // Notification indexes
        Notifications.Indexes.CreateOne(new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(n => n.UserId)));

        // AuditLog indexes
        AuditLogs.Indexes.CreateOne(new CreateIndexModel<AuditLog>(
            Builders<AuditLog>.IndexKeys.Ascending(a => a.UserId)));

        AuditLogs.Indexes.CreateOne(new CreateIndexModel<AuditLog>(
            Builders<AuditLog>.IndexKeys.Ascending(a => a.CreatedAt)));

        // Planning indexes
        Plannings.Indexes.CreateOne(new CreateIndexModel<Planning>(
            Builders<Planning>.IndexKeys.Ascending(p => p.AidantId).Ascending(p => p.Date)));
    }
}

