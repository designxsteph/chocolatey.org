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
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    public class PackageViewModel : IPackageVersionModel
    {
        private readonly Package package;

        public PackageViewModel(Package package)
        {
            this.package = package;
            Version = package.Version;
            Summary = package.Summary;
            Description = package.Description;
            ReleaseNotes = package.ReleaseNotes;
            IconUrl = package.IconUrl;
            ProjectUrl = package.ProjectUrl;
            ProjectSourceUrl = package.ProjectSourceUrl;
            PackageSourceUrl = package.PackageSourceUrl;
            DocsUrl = package.DocsUrl;
            MailingListUrl = package.MailingListUrl;
            BugTrackerUrl = package.BugTrackerUrl;
            LicenseUrl = package.LicenseUrl;
            LatestVersion = package.IsLatest;
            LatestStableVersion = package.IsLatestStable;
            LastUpdated = package.Status == PackageStatusType.Submitted ? package.LastUpdated : package.Published;
            PublishedDate = package.Published;
            Listed = package.Listed;
            DownloadCount = package.DownloadCount;
            Prerelease = package.IsPrerelease;
            Status = package.Status;
            SubmittedStatus = package.SubmittedStatus;
            ApprovedDate = package.ApprovedDate;
            IsExemptedFromVerification = package.PackageRegistration.ExemptedFromVerification;
            ExemptedFromVerificationReason = package.PackageRegistration.ExemptedFromVerificationReason;
            ReviewerUserName = package.ReviewedBy != null ? package.ReviewedBy.Username : string.Empty;
            ReviewerEmailAddress = package.ReviewedBy != null ? package.ReviewedBy.EmailAddress : string.Empty;
            ReviewedDate = package.ReviewedDate;
            ReviewComments = package.ReviewComments;
            PackageTestResultsStatus = package.PackageTestResultStatus;
            PackageTestResultsUrl = package.PackageTestResultUrl ?? string.Empty;
            PackageValidationResultStatus = package.PackageValidationResultStatus;
            IsDownloadCacheAvailable = package.DownloadCacheStatus == PackageDownloadCacheStatusType.Available;
            PackageScanResultStatus = package.PackageScanStatus;
            PackageScanFlagResultType = package.PackageScanFlagResult;
            ExemptedFromScannerReason = package.ExemptedFromScannerReason;
            ExemptedFromValidatorReason = package.ExemptedFromValidatorReason;
        }

        public string Id { get { return package.PackageRegistration.Id; } }
        public string Version { get; set; }
        public string Title { get { return String.IsNullOrEmpty(package.Title) ? package.PackageRegistration.Id : package.Title; } }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string ReleaseNotes { get; set; }
        public string IconUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string ProjectSourceUrl { get; set; }
        public string PackageSourceUrl { get; set; }
        public string DocsUrl { get; set; }
        public string MailingListUrl { get; set; }
        public string BugTrackerUrl { get; set; }
        public string LicenseUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool LatestVersion { get; set; }
        public bool LatestStableVersion { get; set; }
        public bool Prerelease { get; set; }
        public int DownloadCount { get; set; }
        public bool Listed { get; set; }
        [Display(Name = "Package Status")]
        public PackageStatusType Status { get; set; }
        [Display(Name = "Submitted Status")]
        public PackageSubmittedStatusType SubmittedStatus { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string ReviewerUserName { get; set; }
        public string ReviewerEmailAddress { get; set; }
        [Display(Name = "Review Comments")]
        public string ReviewComments { get; set; }
        public PackageAutomatedReviewResultStatusType PackageTestResultsStatus { get; set; }
        public string PackageTestResultsUrl { get; set; }
        public PackageAutomatedReviewResultStatusType PackageValidationResultStatus { get; set; }
        public PackageScanStatusType PackageScanResultStatus { get; set; }
        public PackageScanFlagResultType PackageScanFlagResultType { get; set; }
        public bool IsDownloadCacheAvailable { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public bool IsExemptedFromVerification { get; set; }
        [Display(Name = "Exempted Reason")]
        [StringLength(500)]
        public string ExemptedFromVerificationReason { get; set; }

        [Display(Name = "Scanner Exempted Reason")]
        [StringLength(500)]
        public string ExemptedFromScannerReason { get; set; }

        [Display(Name = "Validator Exempted Reason")]
        [StringLength(500)]
        public string ExemptedFromValidatorReason { get; set; }

        public int TotalDownloadCount { get { return package.PackageRegistration.DownloadCount; } }

        public bool IsCurrent(IPackageVersionModel current)
        {
            return current.Version == Version && current.Id == Id;
        }
    }
}
