﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MindHorizon.Data.Mapping;
using MindHorizon.Entities;
using MindHorizon.Entities.Identity;

namespace MindHorizon.Data
{
    public class MindHorizonDbContext: IdentityDbContext<User, Role, string, UserClaim, UserRole, IdentityUserLogin<string>, RoleClaim, IdentityUserToken<string>>
    {
        public MindHorizonDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.AddCustomIdentityMappings();
            builder.AddCustomMindHorizonMappings();
            builder.Entity<Post>().Property(b => b.PublishDateTime).HasDefaultValueSql("CONVERT(datetime,GetDate())");
        }

        public virtual DbSet<Category> Categories { set; get; }
        public virtual DbSet<Post> Post { set; get; }
        public virtual DbSet<Bookmark> Bookmarks { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<Like> Likes { get; set; }
        public virtual DbSet<PostCategory> PostCategories { get; set; }
        public virtual DbSet<PostImage> PostImages { get; set; }
        public virtual DbSet<PostLetter> PostLetters { get; set; }
        public virtual DbSet<PostTag> PostTags { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<Visit> Visits { get; set; }
    }
}