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
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    [Serializable]
    [DisplayColumn("Title")]
    public class Package : IEntity
    {
        public Package()
        {
            Authors = new HashSet<PackageAuthor>();
            Dependencies = new HashSet<PackageDependency>();
            SupportedFrameworks = new HashSet<PackageFramework>();
            Files = new HashSet<PackageFile>();
           // PackageScanResults = new HashSet<ScanResult>();
            Listed = true;
        }

        public int Key { get; set; }

        public PackageRegistration PackageRegistration { get; set; }
        public int PackageRegistrationKey { get; set; }

        public virtual ICollection<PackageStatistics> DownloadStatistics { get; set; }
        public virtual ICollection<PackageAuthor> Authors { get; set; }
        public virtual ICollection<PackageFile> Files { get; set; }

        [MaxLength(1500)]
        public string Copyright { get; set; }
        public DateTime Created { get; set; }
        public virtual ICollection<PackageDependency> Dependencies { get; set; }

        /// <remarks>
        ///   Has a max length of 4000. Is not indexed but *IS* used for searches.
        /// </remarks>
        [StringLength(4000)]
        public string Description { get; set; }

        /// <remarks>
        ///   Has a max length of 4000. Is not indexed and not used for searches. Db column is nvarchar(max).
        /// </remarks>
        public string ReleaseNotes { get; set; }
        public int DownloadCount { get; set; }

        [StringLength(10)]
        public string HashAlgorithm { get; set; }

        [StringLength(256), Required]
        public string Hash { get; set; }

        [MaxLength(1500)]
        public string IconUrl { get; set; }
        public bool IsLatest { get; set; }
        public bool IsLatestStable { get; set; }
        public DateTime LastUpdated { get; set; }

        [MaxLength(500)]
        public string LicenseUrl { get; set; }

        [StringLength(20)]
        public string Language { get; set; }

        public DateTime Published { get; set; }
        public long PackageFileSize { get; set; }

        [MaxLength(500)]
        public string ProjectUrl { get; set; }

        [MaxLength(500)]
        public string ProjectSourceUrl { get; set; }

        [MaxLength(500)]
        public string PackageSourceUrl { get; set; }

        [MaxLength(500)]
        public string DocsUrl { get; set; }

        [MaxLength(500)]
        public string MailingListUrl { get; set; }

        [MaxLength(500)]
        public string BugTrackerUrl { get; set; }

        public bool RequiresLicenseAcceptance { get; set; }

        [MaxLength(1500)]
        public string Summary { get; set; }

        [MaxLength(1000)]
        public string Tags { get; set; }

        [StringLength(256)]
        public string Title { get; set; }

        [StringLength(64), Required]
        public string Version { get; set; }

        public bool Listed { get; set; }

        public PackageStatusType Status { get; set; }
        [MaxLength(100)]
        [Column("Status")]
        public string StatusForDatabase
        {
            get { return Status.ToString(); }
            set
            {
                if (value == null) Status = PackageStatusType.Unknown;
                else Status = (PackageStatusType)Enum.Parse(typeof(PackageStatusType), value);
            }
        }

        public PackageSubmittedStatusType SubmittedStatus { get; set; }
        [MaxLength(50)]
        [Column("SubmittedStatus")]
        public string SubmittedStatusForDatabase
        {
            get { return SubmittedStatus.ToString(); }
            set
            {
                if (value == null) SubmittedStatus = PackageSubmittedStatusType.Ready;
                else SubmittedStatus = (PackageSubmittedStatusType)Enum.Parse(typeof(PackageSubmittedStatusType), value);
            }
        }

        public virtual User CreatedBy { get; set; }
        public int? CreatedByKey { get; set; }

        public DateTime? ReviewedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public virtual User ReviewedBy { get; set; }
        public int? ReviewedById { get; set; }

        /// <remarks>
        ///   Has a max length of 4000. Is not indexed and not used for searches. Db column is nvarchar(max).
        /// </remarks>
        public string ReviewComments { get; set; }

        public PackageAutomatedReviewResultStatusType PackageTestResultStatus { get; set; }
        [MaxLength(50)]
        [Column("PackageTestResultStatus")]
        public string PackageTestResultStatusForDatabase
        {
            get { return PackageTestResultStatus.ToString(); }
            set
            {
                if (value == null) PackageTestResultStatus = PackageAutomatedReviewResultStatusType.Unknown;
                else PackageTestResultStatus = (PackageAutomatedReviewResultStatusType)Enum.Parse(typeof(PackageAutomatedReviewResultStatusType), value);
            }
        }

        [MaxLength(400)]
        public string PackageTestResultUrl { get; set; }
        public DateTime? PackageTestResultDate { get; set; }

        public PackageAutomatedReviewResultStatusType PackageValidationResultStatus { get; set; }
        [MaxLength(50)]
        [Column("PackageValidationResultStatus")]
        public string PackageValidationResultStatusForDatabase
        {
            get { return PackageValidationResultStatus.ToString(); }
            set
            {
                if (value == null) PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Unknown;
                else PackageValidationResultStatus = (PackageAutomatedReviewResultStatusType)Enum.Parse(typeof(PackageAutomatedReviewResultStatusType), value);
            }
        }
        public DateTime? PackageValidationResultDate { get; set; }

        public virtual User ExemptedFromValidatorBy { get; set; }
        public int? ExemptedFromValidatorById { get; set; }
        [MaxLength(500)]
        public string ExemptedFromValidatorReason { get; set; }

        public virtual User ExemptedFromScannerBy { get; set; }
        public int? ExemptedFromScannerById { get; set; }
        [MaxLength(500)]
        public string ExemptedFromScannerReason { get; set; }

        public DateTime? PackageCleanupResultDate { get; set; }

        public PackageDownloadCacheStatusType DownloadCacheStatus { get; set; }
        [MaxLength(50)]
        [Column("DownloadCacheStatus")]
        public string DownloadCacheStatusForDatabase
        {
            get { return DownloadCacheStatus.ToString(); }
            set
            {
                if (value == null) DownloadCacheStatus = PackageDownloadCacheStatusType.Unknown;
                else DownloadCacheStatus = (PackageDownloadCacheStatusType)Enum.Parse(typeof(PackageDownloadCacheStatusType), value);
            }
        }
        public DateTime? DownloadCacheDate { get; set; }
        /// <remarks>
        ///   Has a max length of 4000. Is not indexed and not used for searches. Db column is nvarchar(max).
        /// </remarks>
        public string DownloadCache { get; set; }

        public PackageScanStatusType PackageScanStatus { get; set; }
        [MaxLength(50)]
        [Column("PackageScanStatus")]
        public string PackageScanStatusForDatabase
        {
            get { return PackageScanStatus.ToString(); }
            set
            {
                if (value == null) PackageScanStatus = PackageScanStatusType.Unknown;
                else PackageScanStatus = (PackageScanStatusType)Enum.Parse(typeof(PackageScanStatusType), value);
            }
        }

        public PackageScanFlagResultType PackageScanFlagResult { get; set; }
        [MaxLength(50)]
        [Column("PackageScanFlagResult")]
        public string PackageScanFlagResultForDatabase
        {
            get { return PackageScanFlagResult.ToString(); }
            set
            {
                if (value == null) PackageScanFlagResult = PackageScanFlagResultType.None;
                else PackageScanFlagResult = (PackageScanFlagResultType)Enum.Parse(typeof(PackageScanFlagResultType), value);
            }
        }

        public DateTime? PackageScanResultDate { get; set; }
        //public virtual ICollection<ScanResult> PackageScanResults { get; set; }

        public bool IsPrerelease { get; set; }
        public virtual ICollection<PackageFramework> SupportedFrameworks { get; set; }

        // TODO: it would be nice if we could change the feed so that we don't need to flatten authors and dependencies
        [MaxLength(1500)]
        public string FlattenedAuthors { get; set; }
        [MaxLength(1500)]
        public string FlattenedDependencies { get; set; }
    }
}
