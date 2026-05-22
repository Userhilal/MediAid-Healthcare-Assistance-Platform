using MediAid.Models;

namespace MediAid.Services;

public interface IAidantCommentService
{
    Task<List<AidantComment>> GetCommentsByAidantIdAsync(string aidantId);
    Task<AidantComment?> GetCommentByIdAsync(string commentId);
    Task<AidantComment?> GetCommentByAuthorAndTargetAsync(string authorAidantId, string targetAidantId);
    Task<AidantComment> CreateCommentAsync(AidantComment comment);
    Task<bool> UpdateCommentAsync(AidantComment comment);
    Task<bool> DeleteCommentAsync(string commentId);
}






