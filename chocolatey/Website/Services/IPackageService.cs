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

using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace NuGetGallery
{
    public interface IPackageService
    {
        Package CreatePackage(IPackage nugetPackage, User currentUser);

        void DeletePackage(string id, string version);

        PackageRegistration FindPackageRegistrationById(string id);

        PackageRegistration FindPackageRegistrationById(string id, bool useCache);

        Package FindPackageByIdAndVersion(string id, string version, bool allowPrerelease = true);

        Package FindPackageByIdAndVersion(string id, string version, bool allowPrerelease, bool useCache = true);

        Package FindPackageForDownloadByIdAndVersion(string id, string version, bool allowPrerelease, bool useCache = true);

        IQueryable<Package> GetPackagesForListing(bool includePrerelease);

        IEnumerable<Package> GetSubmittedPackages(bool useCache);

        void PublishPackage(string id, string version);

        IEnumerable<Package> FindPackagesByOwner(User user);

        IEnumerable<Package> FindDependentPackages(Package package);

        PackageOwnerRequest CreatePackageOwnerRequest(PackageRegistration package, User currentOwner, User newOwner);

        bool ConfirmPackageOwner(PackageRegistration package, User user, string token);

        void AddPackageOwner(PackageRegistration package, User user);

        void RemovePackageOwner(PackageRegistration package, User user);

        void AddDownloadStatistics(Package package, string userHostAddress, string userAgent);

        void MarkPackageUnlisted(Package package);

        void MarkPackageListed(Package package);

        void ChangePackageStatus(Package package, PackageStatusType status, string comments, string newReviewComments, User user, User reviewer, bool sendMaintainerEmail, PackageSubmittedStatusType submittedStatus, bool assignReviewer);

        void ChangeTrustedStatus(Package package, bool trustedPackage, User user);

        void UpdateSubmittedStatusAfterAutomatedReviews(Package package);

        void ChangePackageTestStatus(Package package, bool success, string resultDetailsUrl, User testReporter);

        void ResetPackageTestStatus(Package package);

        void ResetPackageValidationStatus(Package package);

        /// <summary>
        /// Saves minor package changes. Do not call this is you are listing or unlisting a package.
        /// </summary>
        /// <param name="package">The package.</param>
        void SaveMinorPackageChanges(Package package);

        void ExemptPackageFromTesting(Package package, bool exemptPackage, string reason, User reviewer, string newComments);

        void ExemptPackageFromValidation(Package package, string reason, User reviewer);

        void ExemptPackageFromScanner(Package package, string reason, User reviewer);

        IEnumerable<ScanResult> GetPackageScanResults(string id, string version, bool useCache = true);
        IEnumerable<PackageFile> GetPackageFiles(Package package, bool useCache = true);
    }
}
