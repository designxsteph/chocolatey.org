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
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Transactions;
using Elmah;
using NuGet;
using NugetGallery;
using StackExchange.Profiling;

namespace NuGetGallery
{
    public class PackageService : IPackageService
    {
        private readonly ICryptographyService cryptoSvc;
        private readonly IEntityRepository<PackageRegistration> packageRegistrationRepo;
        private readonly IEntityRepository<Package> packageRepo;
        private readonly IEntityRepository<PackageAuthor> packageAuthorRepo;
        private readonly IEntityRepository<PackageFramework> packageFrameworksRepo;
        private readonly IEntityRepository<PackageDependency> packageDependenciesRepo;
        private readonly IEntityRepository<PackageFile> packageFilesRepo;
        private readonly IEntityRepository<ScanResult> scanResultRepo;
        private readonly IPackageFileService packageFileSvc;
        private readonly IEntityRepository<PackageOwnerRequest> packageOwnerRequestRepository;
        private readonly IMessageService messageSvc;
        private readonly IImageFileService imageFileSvc;
        private readonly IIndexingService indexingSvc;
        private readonly IPackageStatisticsService packageStatsSvc;
        private readonly string submittedStatus = PackageStatusType.Submitted.GetDescriptionOrValue();
        private const string TESTING_PASSED_MESSAGE = "has passed automated testing";
        private const string TESTING_FAILED_MESSAGE = "has failed automated testing";

        private const int DEFAULT_CACHE_TIME_MINUTES_PACKAGES = 180;

        public PackageService(
            ICryptographyService cryptoSvc,
            IEntityRepository<PackageRegistration> packageRegistrationRepo,
            IEntityRepository<Package> packageRepo,
            IPackageFileService packageFileSvc,
            IEntityRepository<PackageOwnerRequest> packageOwnerRequestRepository,
            IEntityRepository<PackageAuthor> packageAuthorRepo,
            IEntityRepository<PackageFramework> packageFrameworksRepo,
            IEntityRepository<PackageDependency> packageDependenciesRepo,
            IEntityRepository<PackageFile> packageFilesRepo,
            IEntityRepository<ScanResult> scanResultRepo,
            IMessageService messageSvc,
            IImageFileService imageFileSvc,
            IIndexingService indexingSvc,
            IPackageStatisticsService packageStatsSvc)
        {
            this.cryptoSvc = cryptoSvc;
            this.packageRegistrationRepo = packageRegistrationRepo;
            this.packageRepo = packageRepo;
            this.packageFileSvc = packageFileSvc;
            this.packageOwnerRequestRepository = packageOwnerRequestRepository;
            this.packageAuthorRepo = packageAuthorRepo;
            this.packageFrameworksRepo = packageFrameworksRepo;
            this.packageDependenciesRepo = packageDependenciesRepo;
            this.packageFilesRepo = packageFilesRepo;
            this.scanResultRepo = scanResultRepo;
            this.messageSvc = messageSvc;
            this.imageFileSvc = imageFileSvc;
            this.indexingSvc = indexingSvc;
            this.packageStatsSvc = packageStatsSvc;
        }

        public Package CreatePackage(IPackage nugetPackage, User currentUser)
        {
            ValidateNuGetPackage(nugetPackage);

            var packageRegistration = CreateOrGetPackageRegistration(currentUser, nugetPackage);

            var package = CreatePackageFromNuGetPackage(packageRegistration, nugetPackage, currentUser);
            packageRegistration.Packages.Add(package);

            using (var tx = new TransactionScope())
            {
                using (var stream = nugetPackage.GetStream())
                {
                    UpdateIsLatest(packageRegistration);
                    packageRegistrationRepo.CommitChanges();
                    packageFileSvc.SavePackageFile(package, stream);
                    tx.Complete();
                }
            }

            // Run the following without stopping grabbing the item
            TaskEx.Run(() => UpdateAndNotifyPackageSubmission(package, currentUser));

            //NotifyIndexingService();

            return package;
        }

        public void UpdateAndNotifyPackageSubmission(Package package, User currentUser)
        {
            // Do the following:
            // - changes package status
            // - deletes the image cache and recreates
            // - invalidates caches
            // - notify for moderation

            try
            {
                ChangePackageStatus(package, package.Status, null, string.Format("User '{0}' (maintainer) submitted package.", currentUser.Username), currentUser, package.ReviewedBy, sendMaintainerEmail: false, submittedStatus: package.SubmittedStatus, assignReviewer: false);
                imageFileSvc.DeleteCachedImage(package.PackageRegistration.Id, package.Version);
                imageFileSvc.CacheAndGetImage(package.IconUrl, package.PackageRegistration.Id, package.Version);
            }
            catch (Exception)
            {
                //ignore
            }

            if (package.Status != PackageStatusType.Approved && package.Status != PackageStatusType.Exempted) NotifyForModeration(package, comments: string.Empty);

            InvalidateCache(package.PackageRegistration);
            Cache.InvalidateCacheItem(string.Format("dependentpackages-{0}", package.Key));
            Cache.InvalidateCacheItem(string.Format("packageFiles-{0}", package.Key));
        }

        public void DeletePackage(string id, string version)
        {
            var package = FindPackageByIdAndVersion(id, version, allowPrerelease:true, useCache: false);

            if (package == null) throw new EntityException(Strings.PackageWithIdAndVersionNotFound, id, version);

            using (var tx = new TransactionScope())
            {
                var packageRegistration = package.PackageRegistration;
                packageRepo.DeleteOnCommit(package);
                packageFileSvc.DeletePackageFile(id, version);
                UpdateIsLatest(packageRegistration);
                packageRepo.CommitChanges();
                if (packageRegistration.Packages.Count == 0)
                {
                    packageRegistrationRepo.DeleteOnCommit(packageRegistration);
                    packageRegistrationRepo.CommitChanges();
                }
                tx.Complete();
            }

            InvalidateCache(package.PackageRegistration);
            Cache.InvalidateCacheItem(string.Format("dependentpackages-{0}", package.Key));
            Cache.InvalidateCacheItem(string.Format("packageFiles-{0}", package.Key));
            NotifyIndexingService(package);
        }

        public virtual PackageRegistration FindPackageRegistrationById(string id)
        {
            return FindPackageRegistrationById(id, useCache: true);
        }

        public PackageRegistration FindPackageRegistrationById(string id, bool useCache)
        {
            if (useCache)
            {
                return Cache.Get(string.Format("packageregistration-{0}", id.to_lower()),
                 DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                 () => packageRegistrationRepo.GetAll()
                        .Include(pr => pr.Owners)
                        .Include(pr => pr.Packages)
                        .Where(pr => pr.Id == id)
                        .SingleOrDefault());
            }

            return packageRegistrationRepo.GetAll()
                    .Include(pr => pr.Owners)
                    .Include(pr => pr.Packages)
                    .Where(pr => pr.Id == id)
                    .SingleOrDefault();

        }

        public virtual Package FindPackageByIdAndVersion(string id, string version, bool allowPrerelease = true)
        {
            return FindPackageByIdAndVersion(id, version, allowPrerelease, useCache: true);
        }

        public virtual Package FindPackageByIdAndVersion(string id, string version, bool allowPrerelease, bool useCache = true)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            // Optimization: Everytime we look at a package we almost always want to see
            // all the other packages with the same ID via the PackageRegistration property.
            // This resulted in a gnarly query.
            // Instead, we can always query for all packages with the ID.

            IEnumerable<Package> packagesQuery = packageRepo.GetAll()
                                                            .Include(p => p.Authors)
                                                            .Include(p => p.PackageRegistration)
                                                            .Include(p => p.PackageRegistration.Owners)
                                                            //.Include(p => p.Files)
                                                            //.Include(p => p.PackageScanResults)
                                                            .Include(p => p.Dependencies)
                                                            .Include(p => p.SupportedFrameworks)
                                                            .Include(p => p.ReviewedBy)
                                                            .Include(p => p.CreatedBy)
                                                            .Where(p => (p.PackageRegistration.Id == id));

            var packageVersions = useCache
                            ? Cache.Get(
                                string.Format("packageVersions-{0}", id.to_lower()),
                                DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                                () => packagesQuery.ToList())
                            : packagesQuery.ToList();

            return GetPackageFromResults(packageVersions, id, version, allowPrerelease);
        }

        public Package FindPackageForDownloadByIdAndVersion(string id, string version, bool allowPrerelease, bool useCache = true)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            IEnumerable<Package> packagesQuery = packageRepo.GetAll()
                                                            .Include(p => p.PackageRegistration)
                                                            .Where(p => (p.PackageRegistration.Id == id));

            var packageVersions = useCache
                            ? Cache.Get(
                                string.Format("packageDownload-{0}", id.to_lower()),
                                DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                                () => packagesQuery.ToList())
                            : packagesQuery.ToList();

            var package = GetPackageFromResults(packageVersions, id, version, allowPrerelease);

            if (package == null && !string.IsNullOrEmpty(version) && useCache)
            {
                Cache.InvalidateCacheItem("packageDownload-{0}".format_with(id.to_lower()));
                package = GetPackageForDownloadByIdAndVersion(id, version);
            }

            return package;
        }

        public Package GetPackageForDownloadByIdAndVersion(string id, string version)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            return packageRepo.GetAll()
                .Include(p => p.PackageRegistration)
                .SingleOrDefault(p =>
                    p.PackageRegistration.Id.Equals(id, StringComparison.OrdinalIgnoreCase)
                    && p.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
        }

        public Package GetPackageFromResults(IList<Package> packageVersions, string id, string version, bool allowPrerelease)
        {
            if (String.IsNullOrEmpty(version) && !allowPrerelease)
            {
                // If there's a specific version given, don't bother filtering by prerelease. You could be asking for a prerelease package.
                packageVersions = packageVersions.Where(p => !p.IsPrerelease).ToList();
            }

            Package package = null;
            if (version == null)
            {
                package = allowPrerelease ?
                      packageVersions.FirstOrDefault(p => p.IsLatest)
                    : packageVersions.FirstOrDefault(p => p.IsLatestStable);

                // If we couldn't find a package marked as latest, then
                // return the most recent one.
                //bug: this looks like it would return the wrong version if 1.2.0 versus 1.10.0
                if (package == null) package = packageVersions.OrderByDescending(p => p.Version).FirstOrDefault();
            }
            else
            {
                package = packageVersions
                    .Where(
                        p =>
                        p.PackageRegistration.Id.Equals(id, StringComparison.OrdinalIgnoreCase)
                        && p.Version.Equals(version, StringComparison.OrdinalIgnoreCase))
                    .SingleOrDefault();
            }

            return package;
        }

        public IEnumerable<ScanResult> GetPackageScanResults(string id, string version, bool useCache = true)
        {
            var packageScanResults = scanResultRepo.GetAll()
                .Include(s => s.Packages)
                .Where(s => s.Packages.Any(p => p.PackageRegistration.Id == id && p.Version == version));

            var scanResults = useCache
                           ? Cache.Get(
                               string.Format("packageScans-{0}-{1}", id.to_lower(), version.to_lower()),
                               DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                               () => packageScanResults.ToList())
                           : packageScanResults.ToList();

            var nupkgs = scanResults.Where(s => s.FileName.EndsWith(".nupkg")).OrderByDescending(s => s.ScanDate).Skip(1).OrEmptyListIfNull().ToList();
            if (nupkgs.Count != 0)
            {
                scanResults.RemoveAll(s => nupkgs.Contains(s));
            }

            return scanResults;
        }

        public IEnumerable<PackageFile> GetPackageFiles(Package package, bool useCache = true)
        {
            IEnumerable<PackageFile> packageFilesQuery = packageFilesRepo.GetAll()
                        .Where(p => (p.PackageKey == package.Key)
                        );

            var packageFiles = useCache
                          ? Cache.Get(
                              string.Format("packageFiles-{0}", package.Key),
                              DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                              () => packageFilesQuery.ToList())
                          : packageFilesQuery.ToList();

            return packageFiles;
        }

        public IQueryable<Package> GetPackagesForListing(bool includePrerelease)
        {
            IQueryable<Package> packages = null;

            // this is based on what is necessary for search. See Extensions.Search and the searchCriteria
            packages = packageRepo.GetAll()
                                  .Include(p => p.Authors)
                                  .Include(p => p.PackageRegistration)
                                  .Include(p => p.PackageRegistration.Owners)
                                  .Where(p => p.Listed);

            return includePrerelease
                       ? packages.Where(p => p.IsLatest)
                       : packages.Where(p => p.IsLatestStable);
        }

        public IEnumerable<Package> GetSubmittedPackages(bool useCache)
        {
            var packagesQuery = packageRepo.GetAll()
                                    .Include(p => p.Authors)
                                    .Include(x => x.PackageRegistration)
                                    .Include(x => x.PackageRegistration.Owners)
                                    .Where(p => !p.IsPrerelease)
                                    .Where(p => p.StatusForDatabase == submittedStatus);

            return useCache ? Cache.Get(
                        "submittedPackages",
                        DateTime.UtcNow.AddMinutes(5),
                        () => packagesQuery.ToList())
                    : packagesQuery.ToList();


        }

        public IEnumerable<Package> FindPackagesByOwner(User user)
        {
            return Cache.Get(string.Format("maintainerpackages-{0}", user.Username),
                    DateTime.UtcNow.AddMinutes(30),
                    () => (from pr in packageRegistrationRepo.GetAll()
                           from u in pr.Owners
                           where u.Username == user.Username
                           from p in pr.Packages
                           select p).Include(p => p.PackageRegistration).ToList());

            //return (from pr in packageRegistrationRepo.GetAll()
            //        from u in pr.Owners
            //        where u.Username == user.Username
            //        from p in pr.Packages
            //        select p).Include(p => p.PackageRegistration).ToList();
        }

        public IEnumerable<Package> FindDependentPackages(Package package)
        {
            // Grab all candidates
            var candidateDependents = Cache.Get(string.Format("dependentpackages-{0}", package.Key),
                   DateTime.UtcNow.AddMinutes(DEFAULT_CACHE_TIME_MINUTES_PACKAGES),
                   () => (from p in packageRepo.GetAll()
                          from d in p.Dependencies
                          where d.Id == package.PackageRegistration.Id
                          select d).Include(pk => pk.Package.PackageRegistration).ToList());

            //var candidateDependents = (from p in packageRepo.GetAll()
            //                           from d in p.Dependencies
            //                           where d.Id == package.PackageRegistration.Id
            //                           select d).Include(pk => pk.Package.PackageRegistration).ToList();


            // Now filter by version range.
            var packageVersion = new SemanticVersion(package.Version);
            var dependents = from d in candidateDependents
                             where VersionUtility.ParseVersionSpec(d.VersionSpec).Satisfies(packageVersion)
                             select d;

            return dependents.Select(d => d.Package);
        }

        public void PublishPackage(string id, string version)
        {
            var package = FindPackageByIdAndVersion(id, version, allowPrerelease:true, useCache:false);

            if (package == null) throw new EntityException(Strings.PackageWithIdAndVersionNotFound, id, version);

            MarkPackageListed(package);
        }

        public void AddDownloadStatistics(Package package, string userHostAddress, string userAgent)
        {
            using (MiniProfiler.Current.Step("Updating package stats"))
            {
                try
                {
                    packageStatsSvc.RecordPackageDownloadStatistics(package.Key, userHostAddress, userAgent);
                }
                catch (Exception ex)
                {
                    // Log but swallow the exception
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }
            }
        }

        private PackageRegistration CreateOrGetPackageRegistration(User currentUser, IPackage nugetPackage)
        {
            var packageRegistration = FindPackageRegistrationById(nugetPackage.Id, useCache:false);

            if (packageRegistration != null && !packageRegistration.Owners.Contains(currentUser)) throw new EntityException(Strings.PackageIdNotAvailable, nugetPackage.Id);

            if (packageRegistration == null)
            {
                packageRegistration = new PackageRegistration
                {
                    Id = nugetPackage.Id
                };

                packageRegistration.Owners.Add(currentUser);

                packageRegistrationRepo.InsertOnCommit(packageRegistration);
            }

            return packageRegistration;
        }

        private Package CreatePackageFromNuGetPackage(PackageRegistration packageRegistration, IPackage nugetPackage, User currentUser)
        {
            var package = FindPackageByIdAndVersion(packageRegistration.Id, nugetPackage.Version.ToString(), allowPrerelease:true, useCache:false);

            if (package != null)
            {
                switch (package.Status)
                {
                    case PackageStatusType.Rejected :
                        throw new EntityException(
                            string.Format(
                                "This package has been {0} and can no longer be submitted.",
                                package.Status.GetDescriptionOrValue().ToLower()));
                    case PackageStatusType.Submitted :
                        //continue on
                        break;
                    default :
                        throw new EntityException(
                            "A package with identifier '{0}' and version '{1}' already exists.",
                            packageRegistration.Id,
                            package.Version);
                }
            }

            var now = DateTime.UtcNow;
            var packageFileStream = nugetPackage.GetStream();

            //if new package versus updating an existing package.
            if (package == null) package = new Package();

            package.Version = nugetPackage.Version.ToString();
            package.Description = nugetPackage.Description;
            package.ReleaseNotes = nugetPackage.ReleaseNotes;
            package.RequiresLicenseAcceptance = nugetPackage.RequireLicenseAcceptance;
            package.HashAlgorithm = Constants.Sha512HashAlgorithmId;
            package.Hash = cryptoSvc.GenerateHash(packageFileStream.ReadAllBytes());
            package.PackageFileSize = packageFileStream.Length;
            package.Created = now;
            package.Language = nugetPackage.Language;
            package.LastUpdated = now;
            package.Published = now;
            package.Copyright = nugetPackage.Copyright;
            package.IsPrerelease = !nugetPackage.IsReleaseVersion();
            package.Listed = false;
            package.Status = PackageStatusType.Submitted;
            package.SubmittedStatus = PackageSubmittedStatusType.Pending;

            package.DownloadCacheStatus = PackageDownloadCacheStatusType.Unknown;
            package.PackageScanStatus = PackageScanStatusType.Unknown;

            package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Pending;
            package.PackageValidationResultDate = null;
            package.PackageCleanupResultDate = null;

            package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Pending;
            package.PackageTestResultDate = null;
            if (packageRegistration.ExemptedFromVerification)
            {
                package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Exempted;
            }

            package.PackageTestResultUrl = string.Empty;
            package.ApprovedDate = null;

            if (package.ReviewedDate.HasValue) package.SubmittedStatus = PackageSubmittedStatusType.Updated;

            package.IconUrl = nugetPackage.IconUrl == null ? string.Empty : nugetPackage.IconUrl.ToString();
            package.LicenseUrl = nugetPackage.LicenseUrl == null ? string.Empty : nugetPackage.LicenseUrl.ToString();
            package.ProjectUrl = nugetPackage.ProjectUrl == null ? string.Empty : nugetPackage.ProjectUrl.ToString();

            package.ProjectSourceUrl = nugetPackage.ProjectSourceUrl == null
                                           ? string.Empty
                                           : nugetPackage.ProjectSourceUrl.ToString();
            package.PackageSourceUrl = nugetPackage.PackageSourceUrl == null
                                           ? string.Empty
                                           : nugetPackage.PackageSourceUrl.ToString();
            package.DocsUrl = nugetPackage.DocsUrl == null ? string.Empty : nugetPackage.DocsUrl.ToString();
            package.MailingListUrl = nugetPackage.MailingListUrl == null
                                         ? string.Empty
                                         : nugetPackage.MailingListUrl.ToString();
            package.BugTrackerUrl = nugetPackage.BugTrackerUrl == null ? string.Empty : nugetPackage.BugTrackerUrl.ToString();
            package.Summary = nugetPackage.Summary ?? string.Empty;
            package.Tags = nugetPackage.Tags ?? string.Empty;
            package.Title = nugetPackage.Title ?? string.Empty;
            package.CreatedByKey = currentUser.Key;

            foreach (var item in package.Authors.OrEmptyListIfNull().ToList())
            {
                packageAuthorRepo.DeleteOnCommit(item);
            }
            packageAuthorRepo.CommitChanges();
            foreach (var author in nugetPackage.Authors)
            {
                package.Authors.Add(
                    new PackageAuthor
                    {
                        Name = author
                    });
            }

            foreach (var item in package.SupportedFrameworks.OrEmptyListIfNull().ToList())
            {
                packageFrameworksRepo.DeleteOnCommit(item);
            }
            packageFrameworksRepo.CommitChanges();
            var supportedFrameworks = GetSupportedFrameworks(nugetPackage).Select(fn => fn.ToShortNameOrNull()).ToArray();
            if (!supportedFrameworks.AnySafe(sf => sf == null))
            {
                foreach (var supportedFramework in supportedFrameworks)
                {
                    package.SupportedFrameworks.Add(
                        new PackageFramework
                        {
                            TargetFramework = supportedFramework
                        });
                }
            }

            foreach (var item in package.Dependencies.OrEmptyListIfNull().ToList())
            {
                packageDependenciesRepo.DeleteOnCommit(item);
            }
            packageDependenciesRepo.CommitChanges();
            foreach (var dependencySet in nugetPackage.DependencySets)
            {
                if (dependencySet.Dependencies.Count == 0)
                {
                    package.Dependencies.Add(
                        new PackageDependency
                        {
                            Id = null,
                            VersionSpec = null,
                            TargetFramework = dependencySet.TargetFramework.ToShortNameOrNull()
                        });
                } else
                {
                    foreach (var dependency in dependencySet.Dependencies.Select(
                        d => new
                        {
                            d.Id,
                            d.VersionSpec,
                            dependencySet.TargetFramework
                        }))
                    {
                        package.Dependencies.Add(
                            new PackageDependency
                            {
                                Id = dependency.Id,
                                VersionSpec = dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString(),
                                TargetFramework = dependency.TargetFramework.ToShortNameOrNull()
                            });
                    }
                }
            }

            package.Files = GetPackageFiles(package, useCache: false).ToList();
            foreach (var item in package.Files.OrEmptyListIfNull().ToList())
            {
                packageFilesRepo.DeleteOnCommit(item);
            }
            packageFilesRepo.CommitChanges();
            foreach (var packageFile in nugetPackage.GetFiles().OrEmptyListIfNull())
            {
                var filePath = packageFile.Path;
                var fileContent = " ";

                IList<string> extensions = new List<string>();
                var approvedExtensions = Configuration.ReadAppSettings("PackageFileTextExtensions");
                if (!string.IsNullOrWhiteSpace(approvedExtensions))
                {
                    foreach (var extension in approvedExtensions.Split(',', ';'))
                    {
                        extensions.Add("." + extension);
                    }
                }
                IList<string> checksumExtensions = new List<string>();
                var checksumApprovedExtensions = Configuration.ReadAppSettings("PackageFileChecksumExtensions");
                if (!string.IsNullOrWhiteSpace(checksumApprovedExtensions))
                {
                    foreach (var extension in checksumApprovedExtensions.Split(',', ';'))
                    {
                        checksumExtensions.Add("." + extension);
                    }
                }

                try
                {
                    var extension = Path.GetExtension(filePath);
                    if (extension != null)
                    {
                        if (extensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase)) fileContent = packageFile.GetStream().ReadToEnd();
                        else if (checksumExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
                        {
                            var bytes = packageFile.GetStream().ReadAllBytes();
                            var md5Hash =
                                BitConverter.ToString(Convert.FromBase64String(cryptoSvc.GenerateHash(bytes, "MD5")))
                                            .Replace("-", string.Empty);
                            var sha1Hash =
                                BitConverter.ToString(Convert.FromBase64String(cryptoSvc.GenerateHash(bytes, "SHA1")))
                                            .Replace("-", string.Empty);

                            var sha256Hash =
                               BitConverter.ToString(Convert.FromBase64String(cryptoSvc.GenerateHash(bytes, "SHA256")))
                                           .Replace("-", string.Empty);

                            var sha512Hash =
                               BitConverter.ToString(Convert.FromBase64String(cryptoSvc.GenerateHash(bytes, "SHA512")))
                                           .Replace("-", string.Empty);

                            fileContent = string.Format("md5: {0} | sha1: {1} | sha256: {2} | sha512: {3}", md5Hash, sha1Hash, sha256Hash, sha512Hash);
                        }
                    }
                } catch (Exception ex)
                {
                    // Log but swallow the exception
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }

                package.Files.Add(
                    new PackageFile
                    {
                        FilePath = filePath,
                        FileContent = fileContent,
                    });
            }

            package.FlattenedAuthors = package.Authors.Flatten();
            package.FlattenedDependencies = package.Dependencies.Flatten();

            return package;
        }

        public virtual IEnumerable<FrameworkName> GetSupportedFrameworks(IPackage package)
        {
            return package.GetSupportedFrameworks();
        }

        private static void ValidateNuGetPackage(IPackage nugetPackage)
        {
            // TODO: Change this to use DataAnnotations
            if (nugetPackage.Id.Length > 128) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Id", "128");
            if (nugetPackage.Authors != null && String.Join(",", nugetPackage.Authors.ToArray()).Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Authors", "4000");
            if (nugetPackage.Copyright != null && nugetPackage.Copyright.Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Copyright", "4000");
            if (nugetPackage.Description != null && nugetPackage.Description.Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Description", "4000");
            if (nugetPackage.IconUrl != null && nugetPackage.IconUrl.ToString().Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "IconUrl", "4000");
            if (nugetPackage.LicenseUrl != null && nugetPackage.LicenseUrl.ToString().Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "LicenseUrl", "4000");
            if (nugetPackage.ProjectUrl != null && nugetPackage.ProjectUrl.ToString().Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "ProjectUrl", "4000");
            if (nugetPackage.Summary != null && nugetPackage.Summary.Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Summary", "4000");
            if (nugetPackage.Tags != null && nugetPackage.Tags.Length > 4000) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Tags", "4000");
            if (nugetPackage.Title != null && nugetPackage.Title.Length > 256) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Title", "256");

            if (nugetPackage.Version != null && nugetPackage.Version.ToString().Length > 64) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Version", "64");

            if (nugetPackage.Language != null && nugetPackage.Language.Length > 20) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Language", "20");

            foreach (var dependency in nugetPackage.DependencySets.SelectMany(s => s.Dependencies))
            {
                if (dependency.Id != null && dependency.Id.Length > 128) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Dependency.Id", "128");

                if (dependency.VersionSpec != null && dependency.VersionSpec.ToString().Length > 256) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Dependency.VersionSpec", "256");
            }

            if (nugetPackage.DependencySets != null && nugetPackage.DependencySets.Flatten().Length > Int16.MaxValue) throw new EntityException(Strings.NuGetPackagePropertyTooLong, "Dependencies", Int16.MaxValue);
        }

        private static void UpdateIsLatest(PackageRegistration packageRegistration)
        {
            if (!packageRegistration.Packages.Any()) return;

            // TODO: improve setting the latest bit; this is horrible. Trigger maybe?
            foreach (var pv in packageRegistration.Packages.Where(p => p.IsLatest || p.IsLatestStable))
            {
                pv.IsLatest = false;
                pv.IsLatestStable = false;
                pv.LastUpdated = DateTime.UtcNow;
            }

            // If the last listed package was just unlisted, then we won't find another one
            var latestPackage = FindPackage(packageRegistration.Packages, p => p.Listed);

            if (latestPackage != null)
            {
                latestPackage.IsLatest = true;
                latestPackage.LastUpdated = DateTime.UtcNow;

                if (latestPackage.IsPrerelease)
                {
                    // If the newest uploaded package is a prerelease package, we need to find an older package that is
                    // a release version and set it to IsLatest.
                    var latestReleasePackage =
                        FindPackage(packageRegistration.Packages.Where(p => !p.IsPrerelease && p.Listed));
                    if (latestReleasePackage != null)
                    {
                        // We could have no release packages
                        latestReleasePackage.IsLatestStable = true;
                        latestReleasePackage.LastUpdated = DateTime.UtcNow;
                    }
                } else
                {
                    // Only release versions are marked as IsLatestStable.
                    latestPackage.IsLatestStable = true;
                }
            }
        }

        public void AddPackageOwner(PackageRegistration package, User user)
        {
            package.Owners.Add(user);
            packageRepo.CommitChanges();


            var request = FindExistingPackageOwnerRequest(package, user);
            if (request != null)
            {
                packageOwnerRequestRepository.DeleteOnCommit(request);
                packageOwnerRequestRepository.CommitChanges();
            }
            Cache.InvalidateCacheItem(string.Format("maintainerpackages-{0}", user.Username));
            InvalidateCache(package);
            //NotifyIndexingService(package.Packages.FirstOrDefault());
        }

        public void RemovePackageOwner(PackageRegistration package, User user)
        {
            var pendingOwner = FindExistingPackageOwnerRequest(package, user);
            if (pendingOwner != null)
            {
                packageOwnerRequestRepository.DeleteOnCommit(pendingOwner);
                packageOwnerRequestRepository.CommitChanges();

                Cache.InvalidateCacheItem(string.Format("maintainerpackages-{0}", user.Username));
                InvalidateCache(package);
                // NotifyIndexingService(package.Packages.FirstOrDefault());

                return;
            }

            package.Owners.Remove(user);
            packageRepo.CommitChanges();
            Cache.InvalidateCacheItem(string.Format("maintainerpackages-{0}", user.Username));
            InvalidateCache(package);
            // NotifyIndexingService(package.Packages.FirstOrDefault());
        }

        // TODO: Should probably be run in a transaction
        public void MarkPackageListed(Package package)
        {
            if (package == null) throw new ArgumentNullException("package");
            if (package.Listed) return;

            if (!package.Listed && (package.IsLatestStable || package.IsLatest)) throw new InvalidOperationException("An unlisted package should never be latest or latest stable!");

            if (package.Status == PackageStatusType.Approved || package.Status == PackageStatusType.Exempted)
            {
                package.Listed = true;
                package.LastUpdated = DateTime.UtcNow;
            }

            UpdateIsLatest(package.PackageRegistration);

            packageRepo.CommitChanges();
            InvalidateCache(package.PackageRegistration);
            NotifyIndexingService(package);
        }

        public void ChangePackageStatus(Package package, PackageStatusType status, string comments, string newReviewComments, User user, User reviewer, bool sendMaintainerEmail, PackageSubmittedStatusType submittedStatus, bool assignReviewer)
        {
            // no changes
            if (package.Status == status
                && package.ReviewComments == comments
                && string.IsNullOrWhiteSpace(newReviewComments)
                && package.SubmittedStatus == submittedStatus) return;

            var now = DateTime.UtcNow;

            var statusChanged = false;
            var submittedStatusChanged = false;
            var newReviewCommentsOriginallyEmpty = string.IsNullOrWhiteSpace(newReviewComments);

            if (package.Status == PackageStatusType.Rejected && status == PackageStatusType.Submitted)
            {
                // unrejecting, provide another 15 days
                if (package.PackageCleanupResultDate.HasValue) package.PackageCleanupResultDate = DateTime.UtcNow;
                //todo: Set back to ready status?
                //if (package.SubmittedStatus == PackageSubmittedStatusType.Waiting && submittedStatus == package.SubmittedStatus)
            }

            if (package.Status == PackageStatusType.Submitted && package.SubmittedStatus != submittedStatus)
            {
                submittedStatusChanged = true;
                package.SubmittedStatus = submittedStatus;
            }

            if (package.Status != status && status != PackageStatusType.Unknown)
            {
                statusChanged = true;
                if (!string.IsNullOrWhiteSpace(newReviewComments)) newReviewComments += string.Format("{0}",Environment.NewLine);
                newReviewComments += string.Format("Status Change - Changed status of package from '{0}' to '{1}'.", package.Status.GetDescriptionOrValue().to_lower(), status.GetDescriptionOrValue().to_lower());
                package.Status = status;
                package.ApprovedDate = null;
                package.LastUpdated = now;

                switch (package.Status)
                {
                    case PackageStatusType.Submitted :
                    case PackageStatusType.Rejected :
                        package.Listed = false;
                        if (package.PackageTestResultStatus == PackageAutomatedReviewResultStatusType.Pending) package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Exempted;
                        if (package.PackageValidationResultStatus == PackageAutomatedReviewResultStatusType.Pending) package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Exempted;
                        break;
                    case PackageStatusType.Approved :
                        package.ApprovedDate = now;
                        package.Listed = true;
                        package.PackageCleanupResultDate = null;
                        break;
                    case PackageStatusType.Exempted :
                        package.Listed = true;
                        break;
                }

                UpdateIsLatest(package.PackageRegistration);
            }

            // reviewer could be null / if user is requesting the package rejected, update
            // assign the reviewer if the user is a reviewer and the status is staying
            // submitted, or the status is changing
            if (assignReviewer && ((user == reviewer && status == PackageStatusType.Submitted) || (statusChanged && status != PackageStatusType.Submitted)))
            {
                package.ReviewedDate = now;
                package.ReviewedById = user.Key;
            }

            if (package.ReviewComments != comments && !string.IsNullOrWhiteSpace(comments))
            {
                package.ReviewComments = comments;
            }

            if (!string.IsNullOrWhiteSpace(newReviewComments))
            {
                package.LastUpdated = now;
                var commenter = user == reviewer ? "reviewer" : "maintainer";

                if (!string.IsNullOrWhiteSpace(package.ReviewComments)) package.ReviewComments += string.Format("{0}{0}", Environment.NewLine);

                package.ReviewComments += string.Format("#### {0} ({1}) on {2} +00:00:{3}{4}", user.Username, commenter, now.ToString("dd MMM yyyy HH:mm:ss"), Environment.NewLine, newReviewComments);
            }

            packageRepo.CommitChanges();

            SendMaintainerEmail(package, newReviewComments, user, sendMaintainerEmail, statusChanged, submittedStatusChanged, newReviewCommentsOriginallyEmpty);
            SendReviewerEmail(package, newReviewComments, user, reviewer);

            InvalidateCache(package.PackageRegistration);
            //if (package.Status == PackageStatusType.Approved || package.Status == PackageStatusType.Exempted)
            //{
            //    NotifyIndexingService(package);
            //}
        }

        private void SendMaintainerEmail(Package package, string reviewComments, User user, bool sendMaintainerEmail, bool statusChanged, bool submittedStatusChanged, bool reviewCommentsIsOnlyStatusChange)
        {
            if (!sendMaintainerEmail || string.IsNullOrWhiteSpace(reviewComments)) return;

            var subject = string.Empty;

            if (reviewComments.Contains(TESTING_PASSED_MESSAGE))
            {
                subject = Constants.MODERATION_VERIFICATION_PASS;
            }
            else if (reviewComments.Contains(TESTING_FAILED_MESSAGE))
            {
                subject = "Action Required - Failed Verification Testing";
            }
            else if (statusChanged)
            {
                subject = "{0}".format_with(package.Status.GetDescriptionOrValue());
            }
            else if (package.SubmittedStatus == PackageSubmittedStatusType.Waiting)
            {
                subject = "Action Required - Review Comments";
            }

            if (package.Status == PackageStatusType.Approved && statusChanged && !reviewCommentsIsOnlyStatusChange)
            {
                subject += " With Review Comments";
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "Review Comments";
            }

            messageSvc.SendPackageModerationEmail(package, reviewComments, subject, user);
        }

        private void SendReviewerEmail(Package package, string reviewComments, User user, User reviewer)
        {
            if (user != reviewer && reviewer != null)
            {
                messageSvc.SendPackageModerationReviewerEmail(package, reviewComments, user);
            }
        }

        public void ChangeTrustedStatus(Package package, bool trustedPackage, User user)
        {
            if (package.PackageRegistration.IsTrusted == trustedPackage) return;

            package.PackageRegistration.IsTrusted = trustedPackage;
            package.PackageRegistration.TrustedById = user.Key;
            package.PackageRegistration.TrustedDate = DateTime.UtcNow;

            if (trustedPackage)
            {
                var packagesToUpdate =
                    package.PackageRegistration.Packages.Where(
                        p => p.Status == PackageStatusType.Unknown || p.Status == PackageStatusType.Submitted).ToList();

                if (packagesToUpdate.Count != 0)
                {
                    var now = DateTime.UtcNow;
                    foreach (var trustedPkg in packagesToUpdate.OrEmptyListIfNull())
                    {
                        if (trustedPkg.Status == PackageStatusType.Submitted) trustedPkg.Listed = true;

                        trustedPkg.Status = PackageStatusType.Approved;
                        trustedPkg.LastUpdated = now;
                        trustedPkg.ReviewedDate = now;
                        trustedPkg.ApprovedDate = now;
                    }

                    packageRegistrationRepo.CommitChanges();
                }
            }

            UpdateIsLatest(package.PackageRegistration);

            packageRepo.CommitChanges();
            InvalidateCache(package.PackageRegistration);
        }

        public void UpdateSubmittedStatusAfterAutomatedReviews(Package package)
        {
            var now = DateTime.UtcNow;

            var passingVerification = (package.PackageTestResultStatus == PackageAutomatedReviewResultStatusType.Passing
                                       || package.PackageTestResultStatus == PackageAutomatedReviewResultStatusType.Exempted);
            var passingValidation = (package.PackageValidationResultStatus == PackageAutomatedReviewResultStatusType.Passing
                                     || package.PackageValidationResultStatus == PackageAutomatedReviewResultStatusType.Exempted);

            if (passingValidation && passingVerification && package.SubmittedStatus == PackageSubmittedStatusType.Pending)
            {
                package.SubmittedStatus = PackageSubmittedStatusType.Ready;
            }

            var skipsVerification = package.PackageRegistration.ExemptedFromVerification;
            if ((package.PackageTestResultDate.HasValue || skipsVerification) && package.PackageValidationResultDate.HasValue)
            {
                // we don't do human moderation for prereleases
                if (package.IsPrerelease)
                {
                    package.Listed = true;
                    package.Status = PackageStatusType.Exempted;
                }

                var trustedPackagePassingAutomatedReview = PackageIsTrusted(package) && passingVerification && passingValidation;
                if (trustedPackagePassingAutomatedReview)
                {
                    package.Listed = true;
                    package.Status = PackageStatusType.Approved;
                    package.ReviewedDate = now;
                    package.ApprovedDate = now;
                }

                UpdateIsLatest(package.PackageRegistration);

                if (package.IsPrerelease || trustedPackagePassingAutomatedReview)
                {
                    messageSvc.SendPackageModerationEmail(package, null, Constants.MODERATION_FINISHED, null);
                }
            }
        }

        private bool PackageIsTrusted(Package package)
        {
            return package.PackageRegistration.IsTrusted || (package.CreatedBy != null && package.CreatedBy.IsTrusted && package.PackageRegistration.Packages.Count(p => p.Listed && !p.IsPrerelease) >= 1);
        }

        public void ChangePackageTestStatus(Package package, bool success, string resultDetailsUrl, User testReporter)
        {
            package.PackageTestResultUrl = resultDetailsUrl;
            package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Failing;
            if (success) package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Passing;
            // the date doesn't need to be exact
            package.PackageTestResultDate = DateTime.UtcNow;

            packageRepo.CommitChanges();

            var testComments = string.Format("{0} {1}.{2} This is not the only check that is performed so check the package page to ensure a 'Ready' status.{2} Please visit {3} for details.",
                package.PackageRegistration.Id,
                success ? TESTING_PASSED_MESSAGE : TESTING_FAILED_MESSAGE,
                Environment.NewLine,
                resultDetailsUrl
                );

            var bypassHolding = package.IsPrerelease;
            var testCommentsAdditional = bypassHolding ? "This package is a prerelease, which means it automatically is approved (even if it fails)." : string.Empty;

            if (package.Status == PackageStatusType.Submitted)
            {
              testComments += success || bypassHolding
                                    ? string.Format("{0} This is an FYI only. There is no action you need to take. {1}", Environment.NewLine, testCommentsAdditional)
                                    : string.Format(@"{0} The package status will be changed and will be waiting on your next actions.

* **NEW!** We have a [test environment](https://github.com/chocolatey/chocolatey-test-environment) for you to replicate the testing we do. This can be used at any time to test packages! See https://github.com/chocolatey/chocolatey-test-environment
* Please log in and leave a review comment if you have questions and/or comments.
* If you see the verifier needs to rerun testing against the package without resubmitting (a issue in the test results), you can do that on the package page in the review section.
* If the verifier is [incompatible with the package](https://github.com/chocolatey/package-verifier/wiki), please log in and leave a review comment if the package needs to bypass testing (e.g. package installs specific drivers).
* Automated testing can also fail when a package is not completely silent or has pop ups ([AutoHotKey](https://chocolatey.org/packages/autohotkey.portable) can assist - a great example is the [VeraCrypt package](https://chocolatey.org/packages/veracrypt/1.16#files)). 
* A package that cannot be made completely unattended should have the notSilent tag. Note that this must be approved by moderators.", Environment.NewLine);

              ChangePackageStatus(package, package.Status, package.ReviewComments, testComments, testReporter, testReporter, true, success || bypassHolding ? package.SubmittedStatus : PackageSubmittedStatusType.Waiting, assignReviewer: false);
            }
            else if (!success && package.Status != PackageStatusType.Submitted)
            {
                messageSvc.SendPackageTestFailureMessage(package, resultDetailsUrl);
            }

            UpdateSubmittedStatusAfterAutomatedReviews(package);

            packageRepo.CommitChanges();
            InvalidateCache(package.PackageRegistration);
        }

        public void ResetPackageTestStatus(Package package)
        {
            package.PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Pending;
            if (package.Status == PackageStatusType.Submitted) package.SubmittedStatus = PackageSubmittedStatusType.Pending;
            packageRepo.CommitChanges();
        }

        public void ResetPackageValidationStatus(Package package)
        {
            package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Pending;
            if (package.Status == PackageStatusType.Submitted) package.SubmittedStatus = PackageSubmittedStatusType.Pending;
            packageRepo.CommitChanges();
        }

        public void SaveMinorPackageChanges(Package package)
        {
            packageRepo.CommitChanges();
            InvalidateCache(package.PackageRegistration);
        }

        public void ExemptPackageFromTesting(Package package, bool exemptPackage, string reason, User reviewer)
        {
            if (package.PackageRegistration.ExemptedFromVerification == exemptPackage) return;

            var packagesToUpdate = package.PackageRegistration.Packages.Where(p => p.PackageTestResultStatus != PackageAutomatedReviewResultStatusType.Passing && p.PackageTestResultStatus != PackageAutomatedReviewResultStatusType.Unknown).ToList();

            foreach (var packageVersion in packagesToUpdate.OrEmptyListIfNull())
            {
                // We go unknown because we don't know if the verifier is going to pick this up or not. Better not to leave it in a pending state.
                packageVersion.PackageTestResultStatus = exemptPackage ? PackageAutomatedReviewResultStatusType.Exempted : PackageAutomatedReviewResultStatusType.Unknown;
            }

            // this may not be a package in submitted status.
            package.PackageTestResultStatus = exemptPackage ? PackageAutomatedReviewResultStatusType.Exempted : PackageAutomatedReviewResultStatusType.Unknown;
            var packageRegistration = package.PackageRegistration;
            packageRegistration.ExemptedFromVerification = exemptPackage;
            packageRegistration.ExemptedFromVerificationById = reviewer.Key;
            packageRegistration.ExemptedFromVerificationDate = DateTime.UtcNow;
            packageRegistration.ExemptedFromVerificationReason = reason;

            packageRepo.CommitChanges();
            packageRegistrationRepo.CommitChanges();
        }

        public void ExemptPackageFromValidation(Package package)
        {
            package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Exempted;
            package.PackageValidationResultDate = DateTime.UtcNow;

            packageRepo.CommitChanges();
        }

        // TODO: Should probably be run in a transaction
        public void MarkPackageUnlisted(Package package)
        {
            if (package == null) throw new ArgumentNullException("package");
            if (!package.Listed) return;

            package.Listed = false;
            package.LastUpdated = DateTime.UtcNow;

            if (package.IsLatest || package.IsLatestStable) UpdateIsLatest(package.PackageRegistration);
            packageRepo.CommitChanges();
            InvalidateCache(package.PackageRegistration);
            NotifyIndexingService(package);
        }

        private static Package FindPackage(IEnumerable<Package> packages, Func<Package, bool> predicate = null)
        {
            if (predicate != null) packages = packages.Where(predicate);
            SemanticVersion version = packages.Max(p => new SemanticVersion(p.Version));

            if (version == null) return null;
            return packages.First(pv => pv.Version.Equals(version.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        public PackageOwnerRequest CreatePackageOwnerRequest(PackageRegistration package, User currentOwner, User newOwner)
        {
            var existingRequest = FindExistingPackageOwnerRequest(package, newOwner);
            if (existingRequest != null) return existingRequest;

            var newRequest = new PackageOwnerRequest
            {
                PackageRegistrationKey = package.Key,
                RequestingOwnerKey = currentOwner.Key,
                NewOwnerKey = newOwner.Key,
                ConfirmationCode = cryptoSvc.GenerateToken(),
                RequestDate = DateTime.UtcNow
            };

            packageOwnerRequestRepository.InsertOnCommit(newRequest);
            packageOwnerRequestRepository.CommitChanges();
            InvalidateCache(package);
            Cache.InvalidateCacheItem(string.Format("maintainerpackages-{0}", newOwner.Username));

            return newRequest;
        }

        public bool ConfirmPackageOwner(PackageRegistration package, User pendingOwner, string token)
        {
            if (package == null) throw new ArgumentNullException("package");

            if (pendingOwner == null) throw new ArgumentNullException("pendingOwner");

            if (String.IsNullOrEmpty(token)) throw new ArgumentNullException("token");

            if (package.IsOwner(pendingOwner)) return true;

            var request = FindExistingPackageOwnerRequest(package, pendingOwner);
            if (request != null && request.ConfirmationCode == token)
            {
                AddPackageOwner(package, pendingOwner);
                return true;
            }

            return false;
        }

        private PackageOwnerRequest FindExistingPackageOwnerRequest(PackageRegistration package, User pendingOwner)
        {
            return (from request in packageOwnerRequestRepository.GetAll()
                    where request.PackageRegistrationKey == package.Key && request.NewOwnerKey == pendingOwner.Key
                    select request).FirstOrDefault();
        }

        //private void NotifyIndexingService()
        //{
        //    // run on background thread
        //    TaskEx.Run(() => indexingSvc.UpdateIndex(forceRefresh:false));
        //}

        private void NotifyIndexingService(Package package)
        {
            // run on background thread
            TaskEx.Run(() => indexingSvc.UpdatePackage(package));
        }

        private void InvalidateCache(PackageRegistration packageRegistration)
        {
            TaskEx.Run(
                () =>
                {
                    Cache.InvalidateCacheItem(string.Format("packageregistration-{0}", packageRegistration.Id.to_lower()));
                    Cache.InvalidateCacheItem(string.Format("V2Feed-FindPackagesById-{0}", packageRegistration.Id.to_lower()));
                    Cache.InvalidateCacheItem(string.Format("V2Feed-Search-{0}", packageRegistration.Id.to_lower()));
                    Cache.InvalidateCacheItem(string.Format("packageVersions-{0}", packageRegistration.Id.to_lower()));
                    Cache.InvalidateCacheItem(string.Format("packageDownload-{0}", packageRegistration.Id.to_lower()));
                    Cache.InvalidateCacheItem(string.Format("item-{0}-{1}", typeof(Package).Name, packageRegistration.Key));
                    // these are package key specific
                    //Cache.InvalidateCacheItem(string.Format("dependentpackages-{0}", packageRegistration.Key));
                    //Cache.InvalidateCacheItem(string.Format("packageFiles-{0}", packageRegistration.Key));
                });
        }

        private void NotifyForModeration(Package package, string comments)
        {
            TaskEx.Run(() => messageSvc.SendPackageModerationEmail(package, comments, Constants.MODERATION_SUBMITTED, null));
        }
    }
}
