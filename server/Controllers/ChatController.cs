using MediAid.Data;
using MediAid.DTOs;
using MediAid.Models;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IMessageService _messageService;
    private readonly IRequestService _requestService;
    private readonly IUserService _userService;
    private readonly IHubContext<Hubs.ChatHub> _hubContext;
    private readonly MongoDbContext _context;
    private readonly INotificationService _notificationService;

    public ChatController(IMessageService messageService, IRequestService requestService,
        IUserService userService, IHubContext<Hubs.ChatHub> hubContext, MongoDbContext context,
        INotificationService notificationService)
    {
        _messageService = messageService;
        _requestService = requestService;
        _userService = userService;
        _hubContext = hubContext;
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Conversations()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        
        // Get all requests where user is patient or assigned aidant
        var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
        var filterBuilder = Builders<MediAid.Models.Request>.Filter;
        
        // Get requests where user is patient and has assigned aidant
        var patientRequests = await context.Requests
            .Find(r => r.PatientId == userId && r.AssignedAidantId != null)
            .ToListAsync();
        
        // Get requests where user is the assigned aidant
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        var aidantRequests = new List<MediAid.Models.Request>();
        if (aidant != null)
        {
            aidantRequests = await context.Requests
                .Find(r => r.AssignedAidantId == aidant.Id)
                .ToListAsync();
        }
        
        // Combine and deduplicate
        var allRequests = patientRequests.Union(aidantRequests).DistinctBy(r => r.Id).ToList();
        
        var conversationList = new List<object>();
        foreach (var request in allRequests)
        {
            if (request.AssignedAidantId == null) continue;
            
            // Determine the other user
            string otherUserId = "";
            if (request.PatientId == userId)
            {
                // User is patient, other is aidant
                var aidantEntity = await context.Aidants.Find(a => a.Id == request.AssignedAidantId).FirstOrDefaultAsync();
                otherUserId = aidantEntity?.UserId ?? "";
            }
            else
            {
                // User is aidant, other is patient
                otherUserId = request.PatientId;
            }
            
            if (string.IsNullOrEmpty(otherUserId)) continue;
            
            var otherUser = await _userService.GetUserByIdAsync(otherUserId);
            
            // Get last message for this request
            var messages = await _messageService.GetMessagesByRequestIdAsync(request.Id!);
            var lastMessage = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            
            // Get unread count
            var unreadCount = await _messageService.GetUnreadMessageCountAsync(userId, request.Id!);
            
            // Get preview
            var preview = "Aucun message";
            if (lastMessage != null)
            {
                preview = lastMessage.Content;
                if (string.IsNullOrEmpty(preview) && lastMessage.Attachments.Any())
                {
                    preview = $"ðŸ“Ž {lastMessage.Attachments.First().FileName}";
                }
            }
            
            conversationList.Add(new
            {
                requestId = request.Id,
                requestTitle = request.Title,
                otherUserId = otherUserId,
                otherUserName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "Utilisateur",
                lastMessage = preview,
                lastMessageTime = lastMessage?.CreatedAt ?? request.CreatedAt,
                unreadCount = unreadCount,
                isRead = lastMessage == null || lastMessage.IsRead || lastMessage.SenderId == userId
            });
        }
        
        // Sort by last message time
        conversationList = conversationList.OrderByDescending(c => ((DateTime)((dynamic)c).lastMessageTime)).ToList();
        
        ViewBag.Conversations = conversationList;
        ViewBag.CurrentUserId = userId;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);
        
        if (request == null)
        {
            return NotFound();
        }

        // Determine receiver ID
        string receiverId = "";
        if (request.PatientId == userId && request.AssignedAidantId != null)
        {
            var aidant = await _context.Aidants.Find(a => a.Id == request.AssignedAidantId).FirstOrDefaultAsync();
            receiverId = aidant?.UserId ?? "";
        }
        else if (request.AssignedAidantId != null)
        {
            var aidant = await _context.Aidants.Find(a => a.Id == request.AssignedAidantId).FirstOrDefaultAsync();
            if (aidant?.UserId == userId)
            {
                receiverId = request.PatientId;
            }
            else
            {
                return Forbid();
            }
        }
        else
        {
            return BadRequest("Aucun aidant assignÃ© Ã  cette demande.");
        }

        var currentUser = await _userService.GetUserByIdAsync(userId);
        var receiverUser = await _userService.GetUserByIdAsync(receiverId);
        
        // RÃ©cupÃ©rer l'aidant du receiver si c'est un aidant
        Aidant? receiverAidant = null;
        if (receiverUser != null && receiverUser.Role == "Aidant")
        {
            receiverAidant = await _userService.GetAidantByUserIdAsync(receiverId);
        }
        
        ViewBag.ReceiverId = receiverId;
        ViewBag.ReceiverUser = receiverUser;
        ViewBag.ReceiverAidant = receiverAidant;
        ViewBag.CurrentUserId = userId;
        ViewBag.CurrentUserName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "Utilisateur";
        ViewBag.CurrentUser = currentUser;
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        
        var attachments = dto.AttachmentUrls.Select(url => new MessageAttachment
        {
            FileUrl = url,
            FileName = url.Split('/').Last(),
            FileType = GetFileType(url),
            UploadedAt = DateTime.UtcNow
        }).ToList();

        var message = await _messageService.CreateMessageAsync(dto.RequestId, userId, dto.ReceiverId, dto.Content, attachments);
        
        if (message != null)
        {
            var senderName = $"{user?.FirstName} {user?.LastName}";
            var attachmentsData = attachments.Select(a => new
            {
                fileUrl = a.FileUrl,
                fileName = a.FileName,
                fileType = a.FileType
            }).ToList();
            
            // Send to receiver first (for delivery status)
            await _hubContext.Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage",
                userId, senderName, dto.Content, DateTime.UtcNow, message.Id, attachmentsData, "sent");
            
            // Also send to group for real-time update
            await _hubContext.Clients.Group($"request_{dto.RequestId}").SendAsync("ReceiveMessage",
                userId, senderName, dto.Content, DateTime.UtcNow, message.Id, attachmentsData, "sent");
            
            // Mark as delivered when receiver receives it (handled in client)
        }

        return Ok(new { success = true, messageId = message?.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var messages = await _messageService.GetMessagesByRequestIdAsync(requestId);
        
        // Resolve sender names
        var result = new List<object>();
        foreach (var m in messages)
        {
            var sender = await _userService.GetUserByIdAsync(m.SenderId);
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Utilisateur";
            
            result.Add(new
            {
                id = m.Id,
                senderId = m.SenderId,
                senderName = senderName,
                content = m.Content,
                timestamp = m.CreatedAt,
                attachments = m.Attachments.Select(a => new
                {
                    fileUrl = a.FileUrl,
                    fileName = a.FileName,
                    fileType = a.FileType
                }).ToList(),
                isRead = m.IsRead,
                isDelivered = m.IsDelivered,
                messageStatus = m.MessageStatus ?? (m.IsRead ? "read" : m.IsDelivered ? "delivered" : "sent")
            });
        }

        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await _messageService.MarkMessageAsReadAsync(messageId, userId);
        
        if (result)
        {
            var message = await _context.Messages.Find(m => m.Id == messageId).FirstOrDefaultAsync();
            if (message != null)
            {
                // Notify sender that message was read
                await _hubContext.Clients.User(message.SenderId).SendAsync("MessageRead", messageId, userId);
                
                // Mark related notifications as read
                var notifications = await _context.Notifications
                    .Find(n => n.UserId == userId && 
                              n.RelatedEntityId == message.RequestId && 
                              n.Type == "Message" && 
                              !n.IsRead)
                    .ToListAsync();
                
                foreach (var notification in notifications)
                {
                    await _notificationService.MarkAsReadAsync(notification.Id!, userId);
                }
            }
        }
        
        return Ok(new { success = result });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var count = await _messageService.MarkAllMessagesAsReadAsync(requestId, userId);
        
        if (count > 0)
        {
            // Get all messages that were marked as read to notify senders
            var messages = await _context.Messages
                .Find(m => m.RequestId == requestId && m.ReceiverId == userId && m.IsRead == true)
                .ToListAsync();
            
            foreach (var message in messages)
            {
                await _hubContext.Clients.User(message.SenderId).SendAsync("MessageRead", message.Id, userId);
            }
            
            // Mark related notifications as read
            var notifications = await _context.Notifications
                .Find(n => n.UserId == userId && 
                          n.RelatedEntityId == requestId && 
                          n.Type == "Message" && 
                          !n.IsRead)
                .ToListAsync();
            
            foreach (var notification in notifications)
            {
                await _notificationService.MarkAsReadAsync(notification.Id!, userId);
            }
        }
        
        return Ok(new { success = true, count = count });
    }
    
    [HttpPost]
    public async Task<IActionResult> MarkAsDelivered(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await _messageService.MarkMessageAsDeliveredAsync(messageId, userId);
        
        if (result)
        {
            var message = await _context.Messages.Find(m => m.Id == messageId).FirstOrDefaultAsync();
            if (message != null)
            {
                // Notify sender that message was delivered
                await _hubContext.Clients.User(message.SenderId).SendAsync("MessageDelivered", messageId);
            }
        }
        
        return Ok(new { success = result });
    }

    [HttpPost]
    [RequestSizeLimit(SafeFileUploadService.ChatMaxBytes)]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        var upload = await SafeFileUploadService.SaveAsync(
            file,
            "chat",
            SafeFileUploadService.ChatAllowedExtensions,
            SafeFileUploadService.ChatMaxBytes);

        if (!upload.IsValid)
        {
            return BadRequest(upload.ErrorMessage);
        }

        return Json(new
        {
            url = upload.RelativeUrl,
            fileName = upload.OriginalFileName,
            contentType = upload.ContentType,
            size = upload.Size
        });
    }

    private string GetFileType(string url)
    {
        var extension = Path.GetExtension(url).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "image",
            ".pdf" => "document",
            ".doc" or ".docx" => "document",
            _ => "file"
        };
    }
}


