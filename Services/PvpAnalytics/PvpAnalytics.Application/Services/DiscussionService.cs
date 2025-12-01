using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IDiscussionService
{
    Task<List<DiscussionThreadDto>> GetThreadsForMatchAsync(long matchId, CancellationToken ct = default);
    Task<DiscussionThreadDto?> GetThreadAsync(long threadId, CancellationToken ct = default);
    Task<DiscussionThreadDto> CreateThreadAsync(CreateThreadDto dto, Guid userId, CancellationToken ct = default);
    Task<DiscussionPostDto> CreatePostAsync(CreatePostDto dto, Guid userId, CancellationToken ct = default);
}

public class DiscussionService(
    IRepository<MatchDiscussionThread> threadRepo,
    IRepository<MatchDiscussionPost> postRepo,
    PvpAnalyticsDbContext dbContext) : IDiscussionService
{
    public async Task<List<DiscussionThreadDto>> GetThreadsForMatchAsync(long matchId, CancellationToken ct = default)
    {
        var threads = await dbContext.MatchDiscussionThreads
            .Where(dt => dt.MatchId == matchId && !dt.IsLocked)
            .Select(thread => new
            {
                Thread = thread,
                PostCount = dbContext.MatchDiscussionPosts
                    .Count(p => p.ThreadId == thread.Id && !p.IsDeleted)
            })
            .OrderByDescending(x => x.Thread.IsPinned)
            .ThenByDescending(x => x.Thread.CreatedAt)
            .Select(x => new DiscussionThreadDto
            {
                Id = x.Thread.Id,
                MatchId = x.Thread.MatchId,
                CreatedByUserId = x.Thread.CreatedByUserId,
                CreatedAt = x.Thread.CreatedAt,
                Title = x.Thread.Title,
                PostCount = x.PostCount
            })
            .ToListAsync(ct);

        return threads;
    }

    public async Task<DiscussionThreadDto?> GetThreadAsync(long threadId, CancellationToken ct = default)
    {
        var thread = await dbContext.MatchDiscussionThreads
            .Include(dt => dt.Posts.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(dt => dt.Id == threadId, ct);

        if (thread == null)
            return null;

        var posts = thread.Posts
            .OrderBy(p => p.CreatedAt)
            .Select(p => new DiscussionPostDto
            {
                Id = p.Id,
                ThreadId = p.ThreadId,
                AuthorUserId = p.AuthorUserId,
                Body = p.Body,
                CreatedAt = p.CreatedAt,
                ParentPostId = p.ParentPostId,
                Upvotes = p.Upvotes
            }).ToList();

        return new DiscussionThreadDto
        {
            Id = thread.Id,
            MatchId = thread.MatchId,
            CreatedByUserId = thread.CreatedByUserId,
            CreatedAt = thread.CreatedAt,
            Title = thread.Title,
            PostCount = posts.Count,
            Posts = posts
        };
    }

    public async Task<DiscussionThreadDto> CreateThreadAsync(CreateThreadDto dto, Guid userId, CancellationToken ct = default)
    {
        var thread = new MatchDiscussionThread
        {
            MatchId = dto.MatchId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            Title = dto.Title,
            IsLocked = false,
            IsPinned = false
        };

        await threadRepo.AddAsync(thread, true, ct);

        return new DiscussionThreadDto
        {
            Id = thread.Id,
            MatchId = thread.MatchId,
            CreatedByUserId = userId,
            CreatedAt = thread.CreatedAt,
            Title = thread.Title,
            PostCount = 0,
            Posts = new List<DiscussionPostDto>()
        };
    }

    public async Task<DiscussionPostDto> CreatePostAsync(CreatePostDto dto, Guid userId, CancellationToken ct = default)
    {
        var thread = await dbContext.MatchDiscussionThreads.FindAsync(([dto.ThreadId]), ct);
        if (thread == null)
        {
            throw new InvalidOperationException($" Thread with ID {dto.ThreadId} not found");
        }

        if (thread.IsLocked)
        {
            throw new InvalidOperationException($" Thread with ID {dto.ThreadId} is locked");
        }
        var post = new MatchDiscussionPost
        {
            ThreadId = dto.ThreadId,
            AuthorUserId = userId,
            Body = dto.Body,
            CreatedAt = DateTime.UtcNow,
            ParentPostId = dto.ParentPostId,
            Upvotes = 0,
            IsDeleted = false
        };

        await postRepo.AddAsync(post, true, ct);

        return new DiscussionPostDto
        {
            Id = post.Id,
            ThreadId = post.ThreadId,
            AuthorUserId = userId,
            Body = post.Body,
            CreatedAt = post.CreatedAt,
            ParentPostId = post.ParentPostId,
            Upvotes = 0
        };
    }
}

