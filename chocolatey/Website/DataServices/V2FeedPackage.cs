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
using System.Data.Services.Common;

namespace NuGetGallery
{
    [HasStream]
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    public class V2FeedPackage
    {
        public string Id { get; set; }
        public string Version { get; set; }

        public string Title { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string Authors { get; set; }
        public string Copyright { get; set; }
        public DateTime Created { get; set; }
        public string Dependencies { get; set; }
        public int DownloadCount { get; set; }
        public int VersionDownloadCount { get; set; }
        // set up automatically from other fields
        public string GalleryDetailsUrl { get; set; }
        // set up automatically from other fields
        public string ReportAbuseUrl { get; set; }
        public string IconUrl { get; set; }
        public bool IsLatestVersion { get; set; }
        public bool IsAbsoluteLatestVersion { get; set; }
        public bool IsPrerelease { get; set; }
        public string Language { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Published { get; set; }
        public string LicenseUrl { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public string PackageHash { get; set; }
        public string PackageHashAlgorithm { get; set; }
        public long PackageSize { get; set; }
        public string ProjectUrl { get; set; }
        public string ReleaseNotes { get; set; }

        // nuspec enhancements
        public string ProjectSourceUrl { get; set; }
        public string PackageSourceUrl { get; set; }
        public string DocsUrl { get; set; }
        public string MailingListUrl { get; set; }
        public string BugTrackerUrl { get; set; }
        // set up automatically from other fields
        public bool IsApproved { get; set; }
        public string PackageStatus { get; set; }
        public string PackageSubmittedStatus { get; set; }
        public string PackageTestResultUrl { get; set; }
        public string PackageTestResultStatus { get; set; }
        public DateTime? PackageTestResultStatusDate { get; set; }
        public string PackageValidationResultStatus { get; set; }
        public DateTime? PackageValidationResultDate { get; set; }
        public DateTime? PackageCleanupResultDate { get; set; }
        public DateTime? PackageReviewedDate { get; set; }
        public DateTime? PackageApprovedDate { get; set; }
        public string PackageReviewer { get; set; }
        public bool IsDownloadCacheAvailable { get; set; }
        public string DownloadCacheStatus { get; set; }
        public DateTime? DownloadCacheDate { get; set; }
        public string DownloadCache { get; set; }
        public string PackageScanStatus { get; set; }
        public DateTime? PackageScanResultDate { get; set; }
    }
}
