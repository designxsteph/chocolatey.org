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
using System.Data.Entity;
using System.Linq;
using OData.Linq;
using QueryInterceptor;

namespace NuGetGallery
{
    public static class PackageExtensions
    {
        private static readonly DateTime magicDateThatActuallyMeansUnpublishedBecauseOfLegacyDecisions = new DateTime(1900, 1, 1, 0, 0, 0);

        public static IQueryable<V2FeedPackage> ToV2FeedPackageQuery(this IQueryable<Package> packages, string siteRoot)
        {
            siteRoot = EnsureTrailingSlash(siteRoot);
            var rejectedStatus = PackageStatusType.Rejected.GetDescriptionOrValue();
            var approvedStatus = PackageStatusType.Approved.GetDescriptionOrValue();
            var cacheAvailableStatus = PackageDownloadCacheStatusType.Available.GetDescriptionOrValue();

            return packages
                .Include(p => p.PackageRegistration).WithoutNullPropagation()
                .Where(p => p.StatusForDatabase != rejectedStatus || p.StatusForDatabase == null)
                .Select(
                    p => new V2FeedPackage
                    {
                        Id = p.PackageRegistration.Id,
                        Version = p.Version,
                        Authors = p.FlattenedAuthors,
                        Copyright = p.Copyright,
                        Created = p.Created,
                        Dependencies = p.FlattenedDependencies,
                        Description = p.Description,
                        DownloadCount = p.PackageRegistration.DownloadCount,
                        GalleryDetailsUrl = siteRoot + "packages/" + p.PackageRegistration.Id + "/" + p.Version,
                        IconUrl = p.IconUrl,
                        IsLatestVersion = p.IsLatestStable, // To maintain parity with v1 behavior of the feed, IsLatestVersion would only be used for stable versions.
                        IsAbsoluteLatestVersion = p.IsLatest,
                        IsPrerelease = p.IsPrerelease,
                        LastUpdated = p.LastUpdated,
                        LicenseUrl = p.LicenseUrl,
                        Language = p.Language,
                        PackageHash = p.Hash,
                        PackageHashAlgorithm = p.HashAlgorithm,
                        PackageSize = p.PackageFileSize,
                        ProjectUrl = p.ProjectUrl,
                        ProjectSourceUrl = p.ProjectSourceUrl,
                        PackageSourceUrl = p.PackageSourceUrl,
                        DocsUrl = p.DocsUrl,
                        MailingListUrl = p.MailingListUrl,
                        BugTrackerUrl = p.BugTrackerUrl,
                        ReleaseNotes = p.ReleaseNotes,
                        IsApproved = p.StatusForDatabase != null && p.StatusForDatabase == approvedStatus,
                        PackageStatus = p.StatusForDatabase,
                        PackageSubmittedStatus = p.SubmittedStatusForDatabase,
                        PackageTestResultUrl = p.PackageTestResultUrl,
                        PackageTestResultStatus = p.PackageTestResultStatusForDatabase,
                        PackageTestResultStatusDate = p.PackageTestResultDate,
                        PackageValidationResultStatus = p.PackageValidationResultStatusForDatabase,
                        PackageValidationResultDate = p.PackageValidationResultDate,
                        PackageCleanupResultDate = p.PackageCleanupResultDate,
                        PackageReviewedDate = p.ReviewedDate,
                        PackageApprovedDate = p.ApprovedDate,
                        PackageReviewer = p.ReviewedBy != null ? p.ReviewedBy.Username : null,
                        ReportAbuseUrl = siteRoot + "package/ReportAbuse/" + p.PackageRegistration.Id + "/" + p.Version,
                        RequireLicenseAcceptance = p.RequiresLicenseAcceptance,
                        Published = p.Listed ? p.Published : magicDateThatActuallyMeansUnpublishedBecauseOfLegacyDecisions,
                        Summary = p.Summary,
                        Tags = p.Tags,
                        Title = p.Title,
                        VersionDownloadCount = p.DownloadCount,
                        IsDownloadCacheAvailable = p.DownloadCacheStatusForDatabase != null && p.DownloadCacheStatusForDatabase == cacheAvailableStatus,
                        DownloadCacheStatus = p.DownloadCacheStatusForDatabase,
                        DownloadCacheDate = p.DownloadCacheDate,
                        DownloadCache = p.DownloadCache,
                        PackageScanStatus = p.PackageScanStatusForDatabase,
                        PackageScanResultDate = p.PackageScanResultDate
                        PackageScanFlagResult = p.PackageScanFlagResultForDatabase
                    });
        }

        internal static IQueryable<TVal> WithoutVersionSort<TVal>(this IQueryable<TVal> feedQuery)
        {
            return feedQuery.InterceptWith(new ODataRemoveVersionSorter());
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }
    }
}
