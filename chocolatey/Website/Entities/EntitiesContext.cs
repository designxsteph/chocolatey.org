// Copyright 2011 - Present RealDimensions Software, LLC, the original
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery
// at https://github.com/NuGet/NuGetGallery
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WebBackgrounder;

namespace NuGetGallery
{
    public interface IEntitiesContext
    {
        int SaveChanges();
        DbSet<T> Set<T>() where T : class;

        IDbSet<CuratedFeed> CuratedFeeds { get; set; }
        IDbSet<CuratedPackage> CuratedPackages { get; set; }
        IDbSet<PackageRegistration> PackageRegistrations { get; set; }
        IDbSet<User> Users { get; set; }
    }

    public class EntitiesContext : DbContext, IWorkItemsContext, IEntitiesContext
    {
        private const int DEFAULT_TIMEOUT_SECONDS = 110;

        public EntitiesContext() : this("NuGetGallery", DEFAULT_TIMEOUT_SECONDS)
        {
        }

        public EntitiesContext(int timeoutSeconds) : this("NuGetGallery", timeoutSeconds)
        {
        }

        public EntitiesContext(string nameOrConnectionString) : this(nameOrConnectionString,DEFAULT_TIMEOUT_SECONDS)
        {
        }

        public EntitiesContext(string nameOrConnectionString, int timeoutSeconds)
            : base(SetConnectionString(nameOrConnectionString))
        {
            InitializeCustomOptions(timeoutSeconds);
        }

        internal static string AdjustConnectionString(string nameOrConnectionString, string username, string password)
        {
            var connectionString = SetConnectionString(nameOrConnectionString);
            var returnedConnectionString = new StringBuilder();

            var hasReplacedUser = false;
            var hasReplacedPassword = false;

            var splits = connectionString.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var connString in splits.OrEmptyListIfNull())
            {
                if (connString.StartsWith("User"))
                {
                    returnedConnectionString.AppendFormat("User Id={0};", username);
                    hasReplacedUser = true;
                }
                else if (connString.StartsWith("Password"))
                {
                    returnedConnectionString.AppendFormat("Password={0};", password);
                    hasReplacedPassword = true;
                }
                else
                {
                    returnedConnectionString.AppendFormat("{0};", connString);
                }
            }

            if (!hasReplacedPassword || !hasReplacedUser)
            {
                Trace.WriteLine("Error adjusting connection string - username and password not found to replace.");
            }

            return returnedConnectionString.to_string();
        }

        private static bool TreatAsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.IndexOf('=') >= 0;
        }

        private static string SetConnectionString(string nameOrConnectionString)
        {
            var connectionString = new StringBuilder();

            if (!TreatAsConnectionString(nameOrConnectionString))
            {
                connectionString.Append(System.Configuration.ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString);
            }
            else
            {
                connectionString.Append(nameOrConnectionString);
            }

            if (!connectionString.to_string().EndsWith(";"))
            {
                connectionString.Append(";");
            }

            connectionString.Append("Max Pool Size=300");

            return connectionString.to_string();
        }

        //todo: Look into ensuring background jobs always have resources
        // https://msdn.microsoft.com/en-us/library/bb933944.aspx

        /// <summary>
        ///   Initializes the custom options.
        /// </summary>
        protected void InitializeCustomOptions(int timeoutSeconds)
        {
            // defaults for quick reference
            //Configuration.LazyLoadingEnabled = true;
            //Configuration.ProxyCreationEnabled = true;
            //Configuration.AutoDetectChangesEnabled = true;
            //Configuration.ValidateOnSaveEnabled = true;

            Configuration.LazyLoadingEnabled = false;
            //Configuration.ValidateOnSaveEnabled = false;
            var adapter = this as IObjectContextAdapter;
            if (adapter != null)
            {
                var objectContext = adapter.ObjectContext;
                objectContext.CommandTimeout = timeoutSeconds; // value in seconds
            }
       }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.Key);

            modelBuilder.Entity<User>()
                        .HasMany<EmailMessage>(u => u.Messages)
                        .WithRequired(em => em.ToUser)
                        .HasForeignKey(em => em.ToUserKey);

            modelBuilder.Entity<User>()
                        .HasMany<Role>(u => u.Roles)
                        .WithMany(r => r.Users)
                        .Map(c => c.ToTable("UserRoles").MapLeftKey("UserKey").MapRightKey("RoleKey"));

            modelBuilder.Entity<Role>().HasKey(u => u.Key);

            modelBuilder.Entity<EmailMessage>().HasKey(em => em.Key);

            modelBuilder.Entity<EmailMessage>()
                        .HasOptional<User>(em => em.FromUser)
                        .WithMany()
                        .HasForeignKey(em => em.FromUserKey);

            modelBuilder.Entity<PackageRegistration>().HasKey(pr => pr.Key);

            modelBuilder.Entity<PackageRegistration>()
                        .HasMany<User>(pr => pr.Owners)
                        .WithMany()
                        .Map(
                            c =>
                            c.ToTable("PackageRegistrationOwners").MapLeftKey("PackageRegistrationKey").MapRightKey("UserKey"));

            modelBuilder.Entity<PackageRegistration>()
                        .HasMany<Package>(pr => pr.Packages)
                        .WithRequired(p => p.PackageRegistration)
                        .HasForeignKey(p => p.PackageRegistrationKey);

            modelBuilder.Entity<Package>().HasKey(p => p.Key);

            modelBuilder.Entity<Package>()
                        .HasMany<PackageAuthor>(p => p.Authors)
                        .WithRequired(pa => pa.Package)
                        .HasForeignKey(pa => pa.PackageKey);

            modelBuilder.Entity<Package>()
                        .HasMany<PackageStatistics>(p => p.DownloadStatistics)
                        .WithRequired(ps => ps.Package)
                        .HasForeignKey(ps => ps.PackageKey);

            modelBuilder.Entity<PackageStatistics>().HasKey(ps => ps.Key);

            modelBuilder.Entity<Package>()
                        .HasMany<PackageDependency>(p => p.Dependencies)
                        .WithRequired(pd => pd.Package)
                        .HasForeignKey(pd => pd.PackageKey);

            modelBuilder.Entity<PackageDependency>().HasKey(pd => pd.Key);

            modelBuilder.Entity<PackageAuthor>().HasKey(pa => pa.Key);

            modelBuilder.Entity<GallerySetting>().HasKey(gs => gs.Key);

            modelBuilder.Entity<WorkItem>().HasKey(wi => wi.Id);

            modelBuilder.Entity<PackageOwnerRequest>().HasKey(por => por.Key);

            modelBuilder.Entity<PackageFramework>().HasKey(pf => pf.Key);

            modelBuilder.Entity<CuratedFeed>().HasKey(cf => cf.Key);

            modelBuilder.Entity<CuratedFeed>()
                        .HasMany<CuratedPackage>(cf => cf.Packages)
                        .WithRequired(cp => cp.CuratedFeed)
                        .HasForeignKey(cp => cp.CuratedFeedKey);

            modelBuilder.Entity<CuratedFeed>()
                        .HasMany<User>(cf => cf.Managers)
                        .WithMany()
                        .Map(c => c.ToTable("CuratedFeedManagers").MapLeftKey("CuratedFeedKey").MapRightKey("UserKey"));

            modelBuilder.Entity<CuratedPackage>().HasKey(cp => cp.Key);

            modelBuilder.Entity<CuratedPackage>().HasRequired(cp => cp.PackageRegistration);

            OnChocolateyModelCreating(modelBuilder);
        }

        public IDbSet<CuratedFeed> CuratedFeeds { get; set; }
        public IDbSet<CuratedPackage> CuratedPackages { get; set; }
        public IDbSet<PackageRegistration> PackageRegistrations { get; set; }
        public IDbSet<User> Users { get; set; }
        public IDbSet<WorkItem> WorkItems { get; set; }

        protected void OnChocolateyModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSiteProfile>().HasKey(e => e.Key);

            modelBuilder.Entity<PackageRegistration>()
                        .HasOptional<User>(e => e.TrustedBy)
                        .WithMany()
                        .HasForeignKey(e => e.TrustedById)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<PackageRegistration>()
                        .HasOptional<User>(e => e.ExemptedFromVerificationBy)
                        .WithMany()
                        .HasForeignKey(e => e.ExemptedFromVerificationById)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<Package>()
                        .HasOptional<User>(e => e.ReviewedBy)
                        .WithMany()
                        .HasForeignKey(e => e.ReviewedById)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<Package>()
                        .HasOptional<User>(e => e.ExemptedFromScannerBy)
                        .WithMany()
                        .HasForeignKey(e => e.ExemptedFromScannerById)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<Package>()
                        .HasOptional<User>(e => e.ExemptedFromValidatorBy)
                        .WithMany()
                        .HasForeignKey(e => e.ExemptedFromValidatorById)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<Package>()
                        .HasOptional<User>(e => e.CreatedBy)
                        .WithMany()
                        .HasForeignKey(e => e.CreatedByKey)
                        .WillCascadeOnDelete(false);

            modelBuilder.Entity<Package>()
                        .HasMany<PackageFile>(p => p.Files)
                        .WithRequired(pf => pf.Package)
                        .HasForeignKey(pf => pf.PackageKey);

            modelBuilder.Entity<PackageFile>().HasKey(pa => pa.Key);

            modelBuilder.Entity<ScanResult>().HasKey(sr => sr.Key);

            //.WithMany(p => p.PackageScanResults)
            modelBuilder.Entity<ScanResult>()
                     .HasMany<Package>(s => s.Packages)
                     .WithMany()
                     .Map(
                         c =>
                         c.ToTable("PackageScanResults").MapLeftKey("ScanResultKey").MapRightKey("PackageKey"));

            // take these out for now, will approach later
            modelBuilder.Entity<Course>().HasKey(e => e.Key);
            modelBuilder.Entity<Course>()
                .HasMany<CourseModule>(c => c.CourseModules)
                .WithRequired(cm => cm.Course)
                .HasForeignKey(cm => cm.CourseKey);

            modelBuilder.Entity<CourseModule>().HasKey(e => e.Key);

            modelBuilder.Entity<UserCourseAchievement>().HasKey(e => e.Key);

            modelBuilder.Entity<UserCourseAchievement>()
                .HasRequired<User>(ca => ca.User)
                .WithMany()
                .HasForeignKey(ca => ca.UserKey)
                .WillCascadeOnDelete(false);

            //modelBuilder.Entity<UserCourseAchievement>()
            //    .HasRequired<Course>(ca => ca.Course)
            //    .WithMany()
            //    .HasForeignKey(ca => ca.CourseKey)
            //    .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserCourseAchievement>()
                .HasMany<UserCourseModuleAchievement>(ca => ca.CourseModuleAchievements)
                .WithRequired(ma => ma.UserCourseAchievement)
                .HasForeignKey(ma => ma.UserCourseAchievementKey);

            modelBuilder.Entity<UserCourseModuleAchievement>().HasKey(e => e.Key);
        }
    }
}
