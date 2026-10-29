using Microsoft.EntityFrameworkCore;

namespace Pathway.Infrastructure.Persistence;

/// <summary>
/// Creates the schema at startup. EnsureCreated builds the original tables on an empty database;
/// the idempotent SQL below adds tables introduced later, because EnsureCreated does nothing once
/// any table exists.
/// </summary>
public static class DatabaseSchema
{
    public static async Task EnsureAsync(ProgressDbContext database)
    {
        await database.Database.EnsureCreatedAsync();
        await database.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS workspace_projects (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL, "TemplateId" character varying(100) NOT NULL,
            "Title" character varying(160) NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_workspace_projects_LearnerId_TemplateId" ON workspace_projects ("LearnerId", "TemplateId");
        CREATE TABLE IF NOT EXISTS workspace_files (
            "Id" uuid PRIMARY KEY, "ProjectId" uuid NOT NULL, "Path" character varying(260) NOT NULL,
            "Content" character varying(100000) NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_workspace_files_ProjectId_Path" ON workspace_files ("ProjectId", "Path");
        CREATE TABLE IF NOT EXISTS math_mastery_attempts (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL,
            "LessonSlug" character varying(200) NOT NULL, "QuestionsJson" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL, "SubmittedAt" timestamp with time zone NULL,
            "CorrectAnswers" integer NOT NULL, "Passed" boolean NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_math_mastery_attempts_LearnerId_LessonSlug_CreatedAt" ON math_mastery_attempts ("LearnerId", "LessonSlug", "CreatedAt");
        CREATE TABLE IF NOT EXISTS review_schedules (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL, "LessonSlug" character varying(200) NOT NULL,
            "IntervalDays" integer NOT NULL, "DueAt" timestamp with time zone NOT NULL, "LastReviewedAt" timestamp with time zone NOT NULL);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_review_schedules_LearnerId_LessonSlug" ON review_schedules ("LearnerId", "LessonSlug");
        CREATE TABLE IF NOT EXISTS community_posts (
            "Id" uuid PRIMARY KEY, "AuthorId" character varying(100) NOT NULL, "CourseId" character varying(100) NOT NULL,
            "Title" character varying(180) NOT NULL, "Body" character varying(10000) NOT NULL, "NeedsMentor" boolean NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_community_posts_CourseId_CreatedAt" ON community_posts ("CourseId", "CreatedAt");
        CREATE TABLE IF NOT EXISTS community_replies (
            "Id" uuid PRIMARY KEY, "PostId" uuid NOT NULL, "AuthorId" character varying(100) NOT NULL,
            "Body" character varying(10000) NOT NULL, "IsMentor" boolean NOT NULL, "CreatedAt" timestamp with time zone NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_community_replies_PostId_CreatedAt" ON community_replies ("PostId", "CreatedAt");
        CREATE TABLE IF NOT EXISTS career_outcome_checkins (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL, "CourseId" character varying(100) NOT NULL,
            "InterviewReadiness" integer NOT NULL, "MentorReadiness" integer NOT NULL, "JobSearchStage" character varying(80) NULL,
            "PortfolioUrl" character varying(2000) NULL, "UpdatedAt" timestamp with time zone NOT NULL);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_career_outcome_checkins_LearnerId_CourseId" ON career_outcome_checkins ("LearnerId", "CourseId");
        CREATE TABLE IF NOT EXISTS peer_review_profiles (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL, "CourseId" character varying(100) NOT NULL,
            "Focus" character varying(500) NOT NULL, "AvailableForPeerReview" boolean NOT NULL, "WantsMentorOfficeHours" boolean NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_peer_review_profiles_LearnerId_CourseId" ON peer_review_profiles ("LearnerId", "CourseId");
        CREATE TABLE IF NOT EXISTS activity_events (
            "Id" uuid PRIMARY KEY, "LearnerId" character varying(100) NOT NULL, "SessionId" character varying(100) NOT NULL,
            "EventType" character varying(50) NOT NULL, "CourseId" character varying(100) NULL, "LessonSlug" character varying(200) NULL,
            "Workspace" character varying(50) NULL, "Detail" character varying(100) NULL, "CreatedAt" timestamp with time zone NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_activity_events_CreatedAt" ON activity_events ("CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_activity_events_LearnerId_CreatedAt" ON activity_events ("LearnerId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_activity_events_SessionId_CreatedAt" ON activity_events ("SessionId", "CreatedAt");
        """);
    }
}
