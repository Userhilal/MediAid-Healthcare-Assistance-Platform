using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class AidantCommentService : IAidantCommentService
{
    private readonly MongoDbContext _context;

    public AidantCommentService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<AidantComment>> GetCommentsByAidantIdAsync(string aidantId)
    {
        return await _context.AidantComments
            .Find(c => c.TargetAidantId == aidantId && c.IsPublic)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<AidantComment?> GetCommentByIdAsync(string commentId)
    {
        return await _context.AidantComments
            .Find(c => c.Id == commentId)
            .FirstOrDefaultAsync();
    }

    public async Task<AidantComment?> GetCommentByAuthorAndTargetAsync(string authorAidantId, string targetAidantId)
    {
        return await _context.AidantComments
            .Find(c => c.AuthorAidantId == authorAidantId && c.TargetAidantId == targetAidantId)
            .FirstOrDefaultAsync();
    }

    public async Task<AidantComment> CreateCommentAsync(AidantComment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.AidantComments.InsertOneAsync(comment);
        return comment;
    }

    public async Task<bool> UpdateCommentAsync(AidantComment comment)
    {
        comment.UpdatedAt = DateTime.UtcNow;
        var result = await _context.AidantComments.ReplaceOneAsync(c => c.Id == comment.Id, comment);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteCommentAsync(string commentId)
    {
        var result = await _context.AidantComments.DeleteOneAsync(c => c.Id == commentId);
        return result.DeletedCount > 0;
    }
}





