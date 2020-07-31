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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Elmah;
using NuGet;
using NuGetGallery.MvcOverrides;
using NugetGallery;
using NuGetGallery.Infrastructure;
using System.Text.RegularExpressions;

namespace NuGetGallery
{
    public partial class PackagesController : Controller
    {
        // TODO: add support for URL-based package submission
        // TODO: add support for uploading logos and screenshots
        // TODO: improve validation summary emphasis

        private readonly IPackageService packageSvc;
        private readonly IUserService userSvc;
        private readonly IMessageService messageService;
        private readonly IAutomaticallyCuratePackageCommand autoCuratedPackageCmd;
        private readonly ISearchService searchSvc;

        public IConfiguration Configuration { get; set; }

        protected virtual bool UseHttps()
        {
            return AppHarbor.IsSecureConnection(HttpContext);
        }

        public PackagesController(
            IPackageService packageSvc, IUserService userSvc, IMessageService messageService, IAutomaticallyCuratePackageCommand autoCuratedPackageCmd,
            IConfiguration configuration, ISearchService searchService)
        {
            this.packageSvc = packageSvc;
            this.userSvc = userSvc;
            this.messageService = messageService;
            this.autoCuratedPackageCmd = autoCuratedPackageCmd;
            Configuration = configuration;
            this.searchSvc = searchService;
        }

        [Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult UploadPackage()
        {
            return View("~/Views/Packages/UploadPackage.cshtml");
        }

        [HttpGet]
        public virtual ActionResult DisplayPackage(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version);

            if (package == null) return PackageNotFound(id, version);

            var scanResults = packageSvc.GetPackageScanResults(id, package.Version);
            package.Files = packageSvc.GetPackageFiles(package).ToList();
            package.Description = FindAndCorrectInvalidMarkdownHeaders(package.Description);
            package.ReleaseNotes = FindAndCorrectInvalidMarkdownHeaders(package.ReleaseNotes);
            var model = new DisplayPackageViewModel(package, scanResults);
            return View("~/Views/Packages/DisplayPackage.cshtml", model);
        }

        // not a fan of ValidateInput=false. Look to change this in an upgrade to true, Exclude
        [Authorize, HttpPost, ValidateInput(false)]
        public virtual ActionResult DisplayPackage(string id, string version, FormCollection form)
        {
            if (!ModelState.IsValid) return DisplayPackage(id, version);
            var currentUser = userSvc.FindByUsername(GetIdentity().Name);

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return PackageNotFound(id, version);

            var scanResults = packageSvc.GetPackageScanResults(id, version, useCache:false);
            package.Files = packageSvc.GetPackageFiles(package, useCache: false).ToList();
            package.Description = FindAndCorrectInvalidMarkdownHeaders(package.Description);
            package.ReleaseNotes = FindAndCorrectInvalidMarkdownHeaders(package.ReleaseNotes);
            var model = new DisplayPackageViewModel(package, scanResults);

            if (currentUser.IsBanned)
            {
                TempData["Message"] = "Changes to package status have been saved.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            var packageRegistration = package.PackageRegistration;
            var isMaintainer = packageRegistration.Owners.AnySafe(x => x.Key == currentUser.Key);
            var isModerationRole = User.IsInAnyModerationRole() && !isMaintainer;
            var isModerator = User.IsModerator() && !isMaintainer;

            if (packageRegistration != null && !isMaintainer && !isModerationRole)
            {
                ModelState.AddModelError(String.Empty, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "maintain"));

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (!ModelState.IsValid) return View("~/Views/Packages/DisplayPackage.cshtml", model);

            var status = PackageStatusType.Unknown;
            if (isMaintainer)
            {
                status = package.Status;
            }
            else
            {
                try
                {
                    status = (PackageStatusType)Enum.Parse(typeof(PackageStatusType), form["Status"].clean_html());
                }
                catch (Exception ex)
                {
                    // Log but swallow the exception
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }
            }

            // maintainers and reviewers cannot change the current status
            if (User.IsReviewer() || isMaintainer) status = package.Status;

            if (status == PackageStatusType.Approved && !(
                (package.PackageValidationResultStatus == PackageAutomatedReviewResultStatusType.Passing || package.PackageValidationResultStatus == PackageAutomatedReviewResultStatusType.Exempted)
                    && (package.PackageTestResultStatus == PackageAutomatedReviewResultStatusType.Passing || package.PackageTestResultStatus == PackageAutomatedReviewResultStatusType.Exempted)
                    && (package.PackageScanStatus == PackageScanStatusType.NotFlagged || package.PackageScanStatus == PackageScanStatusType.Exempted)
                ))
            {
                ModelState.AddModelError(String.Empty, "A package cannot be approved unless all required checks have passed, or have been exempted by a moderator.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (package.Status != PackageStatusType.Unknown && status == PackageStatusType.Unknown)
            {
                ModelState.AddModelError(String.Empty, "A package cannot be moved into unknown status.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (package.Status == PackageStatusType.Unknown && status == PackageStatusType.Submitted)
            {
                ModelState.AddModelError(String.Empty, "A package cannot be moved from unknown to submitted status.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (User.IsReviewer() && status != PackageStatusType.Submitted)
            {
                ModelState.AddModelError(String.Empty, "A reviewer can only comment/submit in submitted status.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            var reviewedPlusOneHour = package.ReviewedDate.GetValueOrDefault().AddHours(1);
            if (!User.IsAdmin()
                && package.Status != status
                && reviewedPlusOneHour < DateTime.UtcNow
                && (package.Status == PackageStatusType.Approved
                                      || package.Status == PackageStatusType.Exempted
                                      || package.Status == PackageStatusType.Rejected
                   )
                )
            {
                ModelState.AddModelError(String.Empty, "Only an admin can move a package from approved/exempt/rejected after one hour of status change. Please reach out on Gitter or use contact site admins link in the left side bar.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            var newComments = form["NewReviewComments"].clean_html();
            bool sendMaintainerEmail = form["SendEmail"].clean_html() == "true";
            bool trustedPackage = form["IsTrusted"].clean_html() == "true,false";
            bool maintainerReject = form["MaintainerReject"].clean_html() == "true";
            bool changeSubmittedStatus = form["ChangeSubmittedStatus"].clean_html() == "true";

            //if (comments != package.ReviewComments)
            //{
            //    ModelState.AddModelError(String.Empty, "New comments have been added, please reload.");
            //    return View("~/Views/Packages/DisplayPackage.cshtml", model);
            //}

            if (maintainerReject && string.IsNullOrWhiteSpace(newComments))
            {
                ModelState.AddModelError(String.Empty, "In order to reject a package version, you must provide comments indicating why it is being rejected.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (isMaintainer && string.IsNullOrWhiteSpace(newComments))
            {
                ModelState.AddModelError(String.Empty, "You need to provide comments.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (isMaintainer && maintainerReject)
            {
                status = PackageStatusType.Rejected;
            }

            bool exemptVerfication = form["IsExemptedFromVerification"].clean_html() == "true,false";
            var exemptVerficationReason = form["ExemptedFromVerificationReason"].clean_html();
            if (exemptVerfication && string.IsNullOrWhiteSpace(exemptVerficationReason))
            {
                ModelState.AddModelError(String.Empty, "In order to exempt a package from automated testing, a reason should be specified.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (isModerationRole)
            {
                packageSvc.ExemptPackageFromTesting(package, exemptVerfication, exemptVerficationReason, currentUser, newComments);
            }

            bool exemptValidation = form["ExemptPackageFromValidation"].clean_html() == "true";
            var exemptValidationReason = form["ExemptedFromValidatorReason"].clean_html();
            if (exemptValidation && string.IsNullOrWhiteSpace(exemptValidationReason))
            {
                ModelState.AddModelError(String.Empty, "In order to exempt a package from automated validation, a reason should be specified.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (isModerationRole && exemptValidation && package.PackageValidationResultStatus != PackageAutomatedReviewResultStatusType.Exempted)
            {
                packageSvc.ExemptPackageFromValidation(package, exemptValidationReason, currentUser);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Auto Validation Change - Validation tests have been exempted.";
            }

            bool exemptScanner = form["ExemptPackageFromScanner"].clean_html() == "true";
            var exemptScannerReason = form["ExemptedFromScannerReason"].clean_html();
            if (exemptScanner && string.IsNullOrWhiteSpace(exemptScannerReason))
            {
                ModelState.AddModelError(String.Empty, "In order to exempt a package from automated scanning, a reason should be specified.");

                TempData["ErrorMessage"] = "There was a problem completing the operation.  Check validation summary for more information.";

                return View("~/Views/Packages/DisplayPackage.cshtml", model);
            }

            if (isModerationRole && exemptScanner && package.PackageScanStatus != PackageScanStatusType.Exempted)
            {
                packageSvc.ExemptPackageFromScanner(package, exemptScannerReason, currentUser);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Auto Scanner Change - Scanner tests have been exempted.";
            }

            bool rerunTests = form["RerunTests"].clean_html() == "true";
            if (rerunTests)
            {
                packageSvc.ResetPackageTestStatus(package);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Auto Verification Change - Verification tests have been set to rerun.";
            }

            bool rerunValidation = form["RerunValidation"].clean_html() == "true";
            if (rerunValidation)
            {
                packageSvc.ResetPackageValidationStatus(package);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Auto Validation Change - Validation tests have been set to rerun.";
            }

            bool rerunVirusScanner = form["RerunVirusScanner"].clean_html() == "true";
            if (rerunVirusScanner)
            {
                package.PackageScanStatus = PackageScanStatusType.Unknown;
                packageSvc.SaveMinorPackageChanges(package);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Auto Scanner Change - Virus Scanner has ben set to rerun";
            }

            bool rerunPackageCacher = form["RerunPackageCacher"].clean_html() == "true";
            if (rerunPackageCacher)
            {
                package.DownloadCacheStatus = PackageDownloadCacheStatusType.Unknown;
                packageSvc.SaveMinorPackageChanges(package);
                if (!string.IsNullOrWhiteSpace(newComments)) newComments += "{0}".format_with(Environment.NewLine);
                newComments += "Package Cacher (CDN Download Cache) has ben set to rerun";
            }

            // could be null if no moderation has happened yet
            var moderator = isModerationRole ? currentUser : package.ReviewedBy;

            packageSvc.ChangePackageStatus(package, status, package.ReviewComments, newComments, currentUser,
                moderator, sendMaintainerEmail,
                isModerationRole ?
                    changeSubmittedStatus ?
                        PackageSubmittedStatusType.Waiting
                        : package.SubmittedStatus
                    : PackageSubmittedStatusType.Responded,
                    assignReviewer: true
            );

            if (isModerator)
            {
                packageSvc.ChangeTrustedStatus(package, trustedPackage, moderator);
            }

            //grab updated package
            package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            scanResults = packageSvc.GetPackageScanResults(id, version, useCache: false);
            package.Files = packageSvc.GetPackageFiles(package, useCache: false).ToList();
            model = new DisplayPackageViewModel(package, scanResults);

            TempData["Message"] = "Changes to package status have been saved.";

            return View("~/Views/Packages/DisplayPackage.cshtml", model);
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 30)]
        public virtual ActionResult ListPackages(string q, string sortOrder = null, int page = 1, bool prerelease = false, bool moderatorQueue = false, string moderationStatus = null)
        {
            if (page < 1) page = 1;
            q = (q ?? string.Empty).Trim();

            IQueryable<Package> packageVersions = packageSvc.GetPackagesForListing(prerelease);
            IEnumerable<Package> packagesToShow = new List<Package>();

            if (Request.Cookies["preferenceModView"] != null && string.IsNullOrWhiteSpace(q) && String.IsNullOrEmpty(sortOrder))
            {
                moderatorQueue = true;
            }

            if (moderatorQueue)
            {
                var unknownStatus = PackageStatusType.Unknown.GetDescriptionOrValue();

                //This is going to be fun. Unknown status ones would be listed, but sometimes a few might slip through the cracks if a maintainer unlists a package.
                // A user can just email us to catch those though.
                packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase == unknownStatus || p.StatusForDatabase == null);
            }

            q = (q ?? "").Trim();

            if (String.IsNullOrEmpty(sortOrder))
            {
                // Determine the default sort order. If no query string is specified, then the sortOrder is DownloadCount
                // If we are searching for something, sort by relevance.
                sortOrder = q.IsEmpty() ? Constants.PopularitySortOrder : Constants.RelevanceSortOrder;
            }

            if (String.IsNullOrEmpty(moderationStatus))
            {
                // If no moderation status is specified, default to All Statuses
                moderationStatus = Constants.AllModerationStatuses;
            }

            int totalHits = 0;
            int updatedPackagesCount = 0;
            int respondedPackagesCount = 0;
            int unreviewedPackagesCount = 0;
            int waitingPackagesCount = 0;
            int pendingAutoReviewPackagesCount = 0;
            int unknownPackagesCount = 0;
            var searchFilter = GetSearchFilter(q, sortOrder, page, prerelease, moderationStatus);

            if (moderatorQueue)
            {
                var submittedPackages = packageSvc.GetSubmittedPackages(useCache: !Request.IsAuthenticated).ToList();

                var updatedStatus = PackageSubmittedStatusType.Updated.ToString();
                var respondedStatus = PackageSubmittedStatusType.Responded.ToString();
                var readyStatus = PackageSubmittedStatusType.Ready.ToString();
                var pendingStatus = PackageSubmittedStatusType.Pending.ToString();
                var waitingStatus = PackageSubmittedStatusType.Waiting.ToString();
                var unknownStatus = PackageStatusType.Unknown.GetDescriptionOrValue();

                //var resubmittedPackages = submittedPackages.Where(p => p.ReviewedDate.HasValue && p.Published > p.ReviewedDate).OrderBy(p => p.Published).ToList();
                var resubmittedPackages = submittedPackages.Where(p => p.SubmittedStatusForDatabase == updatedStatus).OrderBy(p => p.Published).ToList();
                updatedPackagesCount = resubmittedPackages.Count;

                var respondedPackages = submittedPackages.Where(p => p.SubmittedStatusForDatabase == respondedStatus).OrderBy(p => p.LastUpdated).ToList();
                respondedPackagesCount = respondedPackages.Count;

                var unreviewedPackages = submittedPackages.Where(p => p.SubmittedStatusForDatabase == readyStatus).OrderBy(p => p.Published).ToList();
                unreviewedPackagesCount = unreviewedPackages.Count;

                var pendingAutoReviewPackages = submittedPackages.Where(p => p.SubmittedStatusForDatabase == pendingStatus || p.SubmittedStatusForDatabase == null).OrderBy(p => p.Published).ToList();
                unreviewedPackagesCount += pendingAutoReviewPackages.Count;
                pendingAutoReviewPackagesCount = pendingAutoReviewPackages.Count;

                //var waitingForMaintainerPackages = submittedPackages.Where(p => p.ReviewedDate >= p.Published).OrderByDescending(p => p.ReviewedDate).ToList();
                var waitingForMaintainerPackages = submittedPackages.Where(p => p.SubmittedStatusForDatabase == waitingStatus).OrderByDescending(p => p.ReviewedDate).ToList();
                waitingPackagesCount = waitingForMaintainerPackages.Count;

                var unknownPackages = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase == unknownStatus || p.StatusForDatabase == null).ToList();
                unknownPackagesCount = unknownPackages.Count;

                packagesToShow = resubmittedPackages.Union(respondedPackages).Union(unreviewedPackages).Union(pendingAutoReviewPackages).Union(waitingForMaintainerPackages);

                switch (searchFilter.SortProperty)
                {
                    case SortProperty.DisplayName:
                        packagesToShow = packagesToShow.OrderBy(p => p.Title);
                        break;
                    case SortProperty.Recent:
                        packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                        break;
                    default:
                        //do not change the search order
                        break;
                }
                switch (searchFilter.SortModeration)
                {
                    case SortModeration.AllStatuses:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = resubmittedPackages.Union(respondedPackages).Union(unreviewedPackages).Union(pendingAutoReviewPackages).Union(waitingForMaintainerPackages);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.SubmittedStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == readyStatus || p.SubmittedStatusForDatabase == pendingStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.UnknownStatus:
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase != readyStatus && p.SubmittedStatusForDatabase != pendingStatus && p.SubmittedStatusForDatabase != waitingStatus && p.SubmittedStatusForDatabase != respondedStatus && p.SubmittedStatusForDatabase != updatedStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packageVersions.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packageVersions.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packageVersions.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.PendingStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == pendingStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.WaitingStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == waitingStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.RespondedStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == respondedStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.ReadyStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == readyStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    case SortModeration.UpdatedStatus:
                        packageVersions = packageVersions.Where(p => !p.IsPrerelease).Where(p => p.StatusForDatabase != unknownStatus && p.StatusForDatabase != null);
                        packagesToShow = submittedPackages.Where(p => p.SubmittedStatusForDatabase == updatedStatus);
                        switch (searchFilter.SortProperty)
                        {
                            case SortProperty.DisplayName:
                                packagesToShow = packagesToShow.OrderBy(p => p.Title);
                                break;
                            case SortProperty.Recent:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.Published);
                                break;
                            case SortProperty.DownloadCount:
                                packagesToShow = packagesToShow.OrderByDescending(p => p.DownloadCount);
                                break;
                            default:
                                //do not change the search order
                                break;
                        }
                        break;
                    default:
                        //do not change the search order
                        break;
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    packagesToShow = packagesToShow.AsQueryable().Search(q).ToList();
                }

                totalHits = packagesToShow.Count() + packageVersions.Count();

                if ((searchFilter.Skip + searchFilter.Take) >= packagesToShow.Count() & string.IsNullOrWhiteSpace(q)) packagesToShow = packagesToShow.Union(packageVersions.OrderByDescending(pv => pv.PackageRegistration.DownloadCount).ToList());

                packagesToShow = packagesToShow.Skip(searchFilter.Skip).Take(searchFilter.Take);
            }
            else
            {
                var results = searchSvc.Search(searchFilter);

                var cacheTime = DateTime.UtcNow.AddSeconds(30);
                // fetch most common query from cache to relieve load on the search service
                if (string.IsNullOrEmpty(q) && page == 1)
                {
                    cacheTime = DateTime.UtcNow.AddMinutes(10);
                }

                totalHits = 0;
                int.TryParse(Cache.Get(
                   string.Format(
                       "searchResultsHits-{0}-{1}-{2}-{3}-{4}-{5}",
                       searchFilter.SearchTerm.to_lower(),
                       searchFilter.IncludePrerelease,
                       searchFilter.Skip,
                       searchFilter.SortProperty.to_string(),
                       searchFilter.SortDirection.to_string(),
                       searchFilter.SortModeration.to_string()),
                   cacheTime,
                   () => results.Hits.to_string()), out totalHits);

                packagesToShow = Cache.Get(
                   string.Format(
                       "searchResults-{0}-{1}-{2}-{3}-{4}-{5}",
                       searchFilter.SearchTerm.to_lower(),
                       searchFilter.IncludePrerelease,
                       searchFilter.Skip,
                       searchFilter.SortProperty.to_string(),
                       searchFilter.SortDirection.to_string(),
                       searchFilter.SortModeration.to_string()),
                   cacheTime,
                   () => results.Data.ToList());
            }

            if (page == 1 && !packagesToShow.Any())
            {
                // In the event the index wasn't updated, we may get an incorrect count.
                totalHits = 0;
            }

            var viewModel = new PackageListViewModel(
                packagesToShow, q, sortOrder, totalHits, page - 1, Constants.DefaultPackageListPageSize, Url, prerelease, moderatorQueue, updatedPackagesCount, unreviewedPackagesCount, waitingPackagesCount, respondedPackagesCount, pendingAutoReviewPackagesCount, unknownPackagesCount, moderationStatus);

            ViewBag.SearchTerm = q;

            return View("~/Views/Packages/ListPackages.cshtml", viewModel);
        }

        // NOTE: Intentionally NOT requiring authentication
        public virtual ActionResult ReportAbuse(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version);

            if (package == null) return PackageNotFound(id, version);

            var model = new ReportAbuseViewModel
            {
                PackageId = id,
                PackageVersion = package.Version,
            };

            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                if (user.Confirmed) model.ConfirmedUser = true;
            }

            return View("~/Views/Packages/ReportAbuse.cshtml", model);
        }

        [HttpPost, ValidateAntiForgeryToken, ValidateFormResponse]
        public virtual ActionResult ReportAbuse(string id, string version, ReportAbuseViewModel reportForm)
        {
            if (!ModelState.IsValid) return ReportAbuse(id, version);

            var package = packageSvc.FindPackageByIdAndVersion(id, version);
            if (package == null) return PackageNotFound(id, version);

            MailAddress from = null;
            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                from = user.ToMailAddress();
            }
            else from = new MailAddress(reportForm.Email);

            var packageUrl = EnsureTrailingSlash(Configuration.GetSiteRoot(UseHttps())) + RemoveStartingSlash(Url.Package(package));

            messageService.ReportAbuse(from, package, reportForm.Message.clean_html(), packageUrl, reportForm.CopySender);

            TempData["Message"] = "Your abuse report has been sent to the site admins.";
            return RedirectToAction(MVC.Packages.DisplayPackage(id, version));
        }

        // NOTE: Intentionally NOT requiring authentication
        public virtual ActionResult ContactAdmins(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version);

            if (package == null) return PackageNotFound(id, version);

            var model = new ContactAdminsViewModel
            {
                PackageId = id,
                PackageVersion = package.Version,
            };

            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                if (user.Confirmed) model.ConfirmedUser = true;
            }

            return View("~/Views/Packages/ContactAdmins.cshtml", model);
        }

        [HttpPost, ValidateAntiForgeryToken, ValidateFormResponse]
        public virtual ActionResult ContactAdmins(string id, string version, ContactAdminsViewModel reportForm)
        {
            if (!ModelState.IsValid) return ContactAdmins(id, version);

            var package = packageSvc.FindPackageByIdAndVersion(id, version);
            if (package == null) return PackageNotFound(id, version);

            MailAddress from = null;
            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                from = user.ToMailAddress();
            }
            else from = new MailAddress(reportForm.Email);

            var packageUrl = EnsureTrailingSlash(Configuration.GetSiteRoot(UseHttps())) + RemoveStartingSlash(Url.Package(package));

            messageService.ContactSiteAdmins(from, package, reportForm.Message.clean_html(), packageUrl, reportForm.CopySender);

            TempData["Message"] = "Your message has been sent to the site admins.";
            return RedirectToAction(MVC.Packages.DisplayPackage(id, version));
        }

        // NOTE: Intentionally NOT requiring authentication
        public virtual ActionResult ContactOwners(string id)
        {
            var package = packageSvc.FindPackageRegistrationById(id);

            if (package == null) return PackageNotFound(id);

            var model = new ContactOwnersViewModel
            {
                PackageId = package.Id,
                Owners = package.Owners.Where(u => u.EmailAllowed)
            };

            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                if (user.Confirmed) model.ConfirmedUser = true;
            }

            return View("~/Views/Packages/ContactOwners.cshtml", model);
        }

        [HttpPost, ValidateAntiForgeryToken, ValidateFormResponse]
        public virtual ActionResult ContactOwners(string id, ContactOwnersViewModel contactForm)
        {
            if (!ModelState.IsValid) return ContactOwners(id);

            var package = packageSvc.FindPackageRegistrationById(id);
            if (package == null) return PackageNotFound(id);

            MailAddress from = null;
            if (Request.IsAuthenticated)
            {
                var user = userSvc.FindByUsername(HttpContext.User.Identity.Name);
                from = user.ToMailAddress();
            }
            else from = new MailAddress(contactForm.Email);

            var packageUrl = EnsureTrailingSlash(Configuration.GetSiteRoot(UseHttps())) + RemoveStartingSlash(Url.Package(package));

            messageService.SendContactOwnersMessage(from, package, contactForm.Message.clean_html(), Url.Action(MVC.Users.Edit(), protocol: Request.Url.Scheme), packageUrl, contactForm.CopySender);

            string message = String.Format(CultureInfo.CurrentCulture, "Your message has been sent to the maintainers of {0}.", id);
            TempData["Message"] = message;
            return RedirectToAction(MVC.Packages.DisplayPackage(id, null));
        }

        // This is the page that explains why there's no download link.
        public virtual ActionResult Download()
        {
            return View("~/Views/Packages/Download.cshtml");
        }

        private bool UserHasPackageChangePermissions(IPrincipal user, Package package)
        {
            if (user != null && (package.IsOwner(user) || user.IsModerator())) return true;

            return false;
        }

        [Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ManagePackageOwners(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version);
            if (package == null) return PackageNotFound(id, version);

            if (!UserHasPackageChangePermissions(HttpContext.User, package)) return new HttpStatusCodeResult(401, "Unauthorized");

            var model = new ManagePackageOwnersViewModel(package, HttpContext.User);

            return View("~/Views/Packages/ManagePackageOwners.cshtml", model);
        }

        [Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Delete(string id, string version)
        {
            return GetPackageOwnerActionFormResult(id, version);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Delete(string id, string version, bool? listed)
        {
            return Delete(id, version, listed, Url.Package);
        }

        internal virtual ActionResult Delete(string id, string version, bool? listed, Func<Package, string> urlFactory)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return PackageNotFound(id, version);
            if (!UserHasPackageChangePermissions(HttpContext.User, package)) return new HttpStatusCodeResult(401, "Unauthorized");

            if (!(listed ?? false)) packageSvc.MarkPackageUnlisted(package);
            else packageSvc.MarkPackageListed(package);

            return Redirect(urlFactory(package));
        }

        [Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Edit(string id, string version)
        {
            return GetPackageOwnerActionFormResult(id, version);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Edit(string id, string version, bool? listed)
        {
            return Edit(id, version, listed, Url.Package);
        }

        [Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ConfirmOwner(string id, string username, string token)
        {
            if (String.IsNullOrEmpty(token)) return HttpNotFound();

            var package = packageSvc.FindPackageRegistrationById(id, useCache: false);
            if (package == null) return HttpNotFound();

            var user = userSvc.FindByUsername(username);
            if (user == null) return HttpNotFound();

            if (!String.Equals(user.Username, User.Identity.Name, StringComparison.OrdinalIgnoreCase)) return new HttpStatusCodeResult(403);

            var model = new PackageOwnerConfirmationModel
            {
                Success = packageSvc.ConfirmPackageOwner(package, user, token),
                PackageId = id
            };

            return View("~/Views/Packages/ConfirmOwner.cshtml", model);
        }

        internal virtual ActionResult Edit(string id, string version, bool? listed, Func<Package, string> urlFactory)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return PackageNotFound(id, version);

            if (!UserHasPackageChangePermissions(HttpContext.User, package)) return new HttpStatusCodeResult(401, "Unauthorized");

            if (!(listed ?? false)) packageSvc.MarkPackageUnlisted(package);
            else packageSvc.MarkPackageListed(package);
            return Redirect(urlFactory(package));
        }

        private ActionResult GetPackageOwnerActionFormResult(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version);
            if (package == null) return PackageNotFound(id, version);
            if (!UserHasPackageChangePermissions(HttpContext.User, package)) return new HttpStatusCodeResult(401, "Unauthorized");

            var model = new DisplayPackageViewModel(package, null);
            return View(model);
        }

        // We may want to have a specific behavior for package not found
        private ActionResult PackageNotFound(string id)
        {
            return PackageNotFound(id, null);
        }

        private ActionResult PackageNotFound(string id, string version)
        {
            return HttpNotFound();
        }

        [HttpPost]
        public virtual ActionResult NotifyMaintainersOfAddedComment(string packageId, CommentViewModel commentViewModel)
        {
            var package = packageSvc.FindPackageRegistrationById(packageId);
            if (package == null) return PackageNotFound(packageId);

            var packageUrl = EnsureTrailingSlash(Configuration.GetSiteRoot(UseHttps())) + RemoveStartingSlash(Url.Package(package));

            messageService.SendCommentNotificationToMaintainers(package, commentViewModel, packageUrl);

            return new HttpStatusCodeResult(200);
        }

        // this methods exist to make unit testing easier
        protected internal virtual IIdentity GetIdentity()
        {
            return User.Identity;
        }

        // this methods exist to make unit testing easier
        protected internal virtual IPackage ReadNuGetPackage(Stream stream)
        {
            return new ZipPackage(stream);
        }

        private SearchFilter GetSearchFilter(string q, string sortOrder, int page, bool includePrerelease, string moderationStatus)
        {
            var searchFilter = new SearchFilter
            {
                SearchTerm = q,
                Skip = (page - 1) * Constants.DefaultPackageListPageSize, // pages are 1-based.
                Take = Constants.DefaultPackageListPageSize,
                IncludePrerelease = includePrerelease
            };

            switch (sortOrder)
            {
                case Constants.AlphabeticSortOrder:
                    searchFilter.SortProperty = SortProperty.DisplayName;
                    searchFilter.SortDirection = SortDirection.Ascending;
                    break;
                case Constants.RecentSortOrder:
                    searchFilter.SortProperty = SortProperty.Recent;
                    break;
                case Constants.PopularitySortOrder:
                    searchFilter.SortProperty = SortProperty.DownloadCount;
                    break;
                default:
                    searchFilter.SortProperty = SortProperty.Relevance;
                    break;
            }
            switch (moderationStatus)
            {
                case Constants.AllModerationStatuses:
                    searchFilter.SortModeration = SortModeration.AllStatuses;
                    break;
                case Constants.SubmittedModerationStatus:
                    searchFilter.SortModeration = SortModeration.SubmittedStatus;
                    break;
                case Constants.PendingModerationStatus:
                    searchFilter.SortModeration = SortModeration.PendingStatus;
                    break;
                case Constants.WaitingModerationStatus:
                    searchFilter.SortModeration = SortModeration.WaitingStatus;
                    break;
                case Constants.RespondedModerationStatus:
                    searchFilter.SortModeration = SortModeration.RespondedStatus;
                    break;
                case Constants.ReadyModerationStatus:
                    searchFilter.SortModeration = SortModeration.ReadyStatus;
                    break;
                case Constants.UpdatedModerationStatus:
                    searchFilter.SortModeration = SortModeration.UpdatedStatus;
                    break;
                case Constants.UnknownModerationStatus:
                    searchFilter.SortModeration = SortModeration.UnknownStatus;
                    break;
            }
            return searchFilter;
        }

        private static string GetSortExpression(string sortOrder)
        {
            switch (sortOrder)
            {
                case Constants.AlphabeticSortOrder:
                    return "PackageRegistration.Id";
                case Constants.RecentSortOrder:
                    return "Published desc";
                case Constants.PopularitySortOrder:
                default:
                    return "PackageRegistration.DownloadCount desc";
            }
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }

        private static string RemoveStartingSlash(string urlPath)
        {
            if (urlPath.StartsWith("/")) return urlPath.Substring(1);

            return urlPath;
        }

        private string FindAndCorrectInvalidMarkdownHeaders(string markdownText)
        {
            var headerHashPattern = @"^(#+)([^\s#].*)$";

            if (markdownText == null) return null;

            return Regex.Replace(
                markdownText,
                headerHashPattern,
                (match => match.Groups[1].Value + " " + match.Groups[2].Value),
                RegexOptions.Multiline);
        }
    }
}
