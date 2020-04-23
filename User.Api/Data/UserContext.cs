using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Api.Models;

namespace User.Api.Data
{
    public class UserContext:DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AppUser>().ToTable("User");

            builder.Entity<UserProperty>().Property(p => p.Key).HasMaxLength(100);
            builder.Entity<UserProperty>().Property(p => p.Value).HasMaxLength(100);
            builder.Entity<UserProperty>().ToTable("UserProperty")
                                           //组合主键
                                        .HasKey(p => new { p.Key, p.AppUserId, p.Value });

            builder.Entity<UserTag>().Property(u => u.Tag).HasMaxLength(100);
            builder.Entity<UserTag>().ToTable("UserTag")
                                      //组合主键
                                      .HasKey(p => new { p.UserId, p.Tag });

            builder.Entity<BPFile>().ToTable("BPFile")
                                    //组合主键
                                    .HasKey(p => new { p.Id});

            base.OnModelCreating(builder);
        }


        public DbSet<AppUser> AppUser { get; set; }
        public DbSet<UserProperty> UserProperty { get; set; }
        public DbSet<UserTag> UserTag { get; set; }
        public DbSet<BPFile> BPFile { get; set; }
    }
}
