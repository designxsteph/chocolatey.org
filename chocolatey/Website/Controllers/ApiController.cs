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
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using NuGet;

namespace NuGetGallery
{
    public partial class ApiController : AppController
    {
        private readonly IPackageService packageSvc;
        private readonly IScanService scanSvc;
        private readonly IUserService userSvc;
        private readonly IPackageFileService packageFileSvc;
        private readonly INuGetExeDownloaderService nugetExeDownloaderSvc;
        private readonly IConfiguration settings;
        //private const int MAX_ALLOWED_CONTENT_LENGTH = 209715200; // 200 MB
        //private const int MAX_ALLOWED_CONTENT_LENGTH = 104857600; // 100 MB
        //private const int MAX_ALLOWED_CONTENT_LENGTH = 157286400; // 150 MB
        private const int MAX_ALLOWED_CONTENT_LENGTH = 367001600; // 350 MB
        private const int ONE_MB = 1048576;
        private IList<string> _forbiddenPackageNames = new List<string>();

        public ApiController(IPackageService packageSvc, IScanService scanSvc, IPackageFileService packageFileSvc, IUserService userSvc, INuGetExeDownloaderService nugetExeDownloaderSvc, IConfiguration settings)
        {
            this.packageSvc = packageSvc;
            this.scanSvc = scanSvc;
            this.packageFileSvc = packageFileSvc;
            this.userSvc = userSvc;
            this.nugetExeDownloaderSvc = nugetExeDownloaderSvc;
            this.settings = settings;

            var forbiddenPackageNames = Configuration.ReadAppSettings("ForbiddenPackageNames");
            if (!string.IsNullOrWhiteSpace(forbiddenPackageNames))
            {
                foreach (var forbiddenPackageName in forbiddenPackageNames.Split(new []{',', ';'},StringSplitOptions.RemoveEmptyEntries))
                {
                    _forbiddenPackageNames.Add(forbiddenPackageName.to_string().Trim());
                }
            }
        }

        [ActionName("GetPackageApi"), HttpGet]
        public virtual ActionResult GetPackage(string id, string version)
        {
            // if the version is null, the user is asking for the latest version. Presumably they don't want includePrerelease release versions.
            // The allow prerelease flag is ignored if both partialId and version are specified.
            var package = packageSvc.FindPackageForDownloadByIdAndVersion(id, version, allowPrerelease: false);

            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            // CloudFlare IP passed variable first
            var ipAddress = Request.Headers["CF-CONNECTING-IP"].to_string();
            if (string.IsNullOrWhiteSpace(ipAddress)) ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"].to_string();
            if (string.IsNullOrWhiteSpace(ipAddress)) ipAddress = Request.UserHostAddress;

            packageSvc.AddDownloadStatistics(package, ipAddress, Request.UserAgent);

            return packageFileSvc.CreateDownloadPackageActionResult(package);
        }

        [ActionName("GetNuGetExeApi"), HttpGet, OutputCache(VaryByParam = "none", Location = OutputCacheLocation.ServerAndClient, Duration = 600)]
        public virtual ActionResult GetNuGetExe()
        {
            return nugetExeDownloaderSvc.CreateNuGetExeDownloadActionResult();
        }

        [ActionName("VerifyPackageKeyApi"), HttpGet]
        public virtual ActionResult VerifyPackageKey(string apiKey, string id, string version)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var user = userSvc.FindByApiKey(parsedApiKey);
            if (user == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "push"));

            if (!String.IsNullOrEmpty(id))
            {
                // If the partialId is present, then verify that the user has permission to push for the specific Id \ version combination.
                var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
                if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

                if (!package.IsOwner(user)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "push"));
            }

            return new EmptyResult();
        }

        [ActionName("PushPackageApi"), HttpPut]
        public virtual ActionResult CreatePackagePut(string apiKey)
        {
            return CreatePackageInternal(apiKey);
        }

        [ActionName("PushPackageApi"), HttpPost]
        public virtual ActionResult CreatePackagePost(string apiKey)
        {
            return CreatePackageInternal(apiKey);
        }

        private ActionResult CreatePackageInternal(string apiKey)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var user = userSvc.FindByApiKey(parsedApiKey);
            if (user == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "push"));

            if (user.IsBanned) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Created, "Package has been pushed and will show up once moderated and approved.");

            if (Request.ContentLength > MAX_ALLOWED_CONTENT_LENGTH)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.RequestEntityTooLarge,String.Format(CultureInfo.CurrentCulture, Strings.PackageTooLarge, MAX_ALLOWED_CONTENT_LENGTH / ONE_MB));
            }

            // Tempfile to store the package from stream.
            // Based on https://github.com/NuGet/NuGetGallery/issues/3042
            var temporaryFile = Path.GetTempFileName();

            var packageToPush = ReadPackageFromRequest(temporaryFile);

            // don't allow forbidden package names to be pushed
            if (_forbiddenPackageNames.Contains(packageToPush.Id, StringComparer.InvariantCultureIgnoreCase))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "push"));
            }

            // Ensure that the user can push packages for this partialId.
            var packageRegistration = packageSvc.FindPackageRegistrationById(packageToPush.Id, useCache: false);
            if (packageRegistration != null)
            {
                if (!packageRegistration.IsOwner(user)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "push"));

                var existingPackage = packageRegistration.Packages.FirstOrDefault(p => p.Version.Equals(packageToPush.Version.ToString(), StringComparison.OrdinalIgnoreCase));

                if (existingPackage != null)
                {
                    switch (existingPackage.Status)
                    {
                        case PackageStatusType.Rejected:
                            var testReporterUser = userSvc.FindByUserId(settings.PackageOperationsUserKey);

                            if (existingPackage.PackageCleanupResultDate.HasValue &&
                                testReporterUser != null &&
                                existingPackage.ReviewedById == testReporterUser.Key
                                )
                            {
                                //allow rejected by cleanup to return a value
                                break;
                            }

                            return new HttpStatusCodeWithBodyResult(
                                HttpStatusCode.Conflict, string.Format("This package has been {0} and can no longer be submitted.", existingPackage.Status.GetDescriptionOrValue().ToLower()));
                        case PackageStatusType.Submitted:
                            //continue on
                            break;
                        default:
                            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Conflict, String.Format(CultureInfo.CurrentCulture, Strings.PackageExistsAndCannotBeModified, packageToPush.Id, packageToPush.Version));
                    }
                }
                else if(!packageRegistration.Packages.Any(p => !p.IsPrerelease && p.Status == PackageStatusType.Approved)
                      && packageRegistration.Packages.Any(p => p.Status == PackageStatusType.Submitted))
                {
                    return new HttpStatusCodeWithBodyResult(
                        HttpStatusCode.Forbidden,
                        string.Format("The package {0} has a previous version in a submitted state, and no approved stable releases. Please work to have the existing package version approved or rejected first.",
                            packageToPush.Id),
                        string.Format(@"
Please wait until a minimum of 1 version of the {0} package have been approved,
before pushing a new version.

If the package is currently failing, please see any failure emails sent
out on why it could be failing, as well as instructions on how to fix
any moderation related failures.",
                            packageToPush.Id));
                }

                var allowedNumberOfPackageVersionsInSubmittedStatus = 10;
                if (packageRegistration.Packages.Count(p => p.Status == PackageStatusType.Submitted) >= allowedNumberOfPackageVersionsInSubmittedStatus)
                {
                    return new HttpStatusCodeWithBodyResult(
                        HttpStatusCode.Forbidden,
                        string.Format("The package {0} has {1} versions currently in a submitted state. For moderation purposes we limit to {1} versions in moderation at a time. Please wait to have the existing package version(s) approved or rejected first.",
                            packageToPush.Id, allowedNumberOfPackageVersionsInSubmittedStatus),
                        string.Format(@"
Please wait to submit more versions of {0} until the current versions are approved or rejected.

If the package is currently failing, please see any failure emails sent
out on why it could be failing, as well as instructions on how to fix
any moderation related failures.",
                            packageToPush.Id));
                }
            }

            try
            {
                packageSvc.CreatePackage(packageToPush, user);

                packageToPush = null;

                try
                {
                    System.IO.File.Delete(temporaryFile);
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeWithBodyResult(HttpStatusCode.InternalServerError, "Could not remove temporary upload file: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.Conflict, string.Format("This package had an issue pushing: {0}", ex.Message));
            }
            finally
            {
                OptimizedZipPackage.PurgeCache();
            }

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Created, "Package has been pushed and will show up once moderated and approved.");
        }

        [ActionName("DeletePackageApi"), HttpDelete]
        public virtual ActionResult DeletePackage(string apiKey, string id, string version)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var user = userSvc.FindByApiKey(parsedApiKey);
            if (user == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "delete"));

            if (user.IsBanned) return new EmptyResult();

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            if (!package.IsOwner(user)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "delete"));

            packageSvc.MarkPackageUnlisted(package);
            return new EmptyResult();
        }

        [ActionName("PublishPackageApi"), HttpPost]
        public virtual ActionResult PublishPackage(string apiKey, string id, string version)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var user = userSvc.FindByApiKey(parsedApiKey);
            if (user == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "publish"));

            if (user.IsBanned) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package has been accepted and will show up once moderated and approved.");

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            if (!package.IsOwner(user)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, string.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "publish"));

            packageSvc.MarkPackageListed(package);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package has been accepted and will show up once moderated and approved.");
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            filterContext.ExceptionHandled = true;
            var exception = filterContext.Exception;
            var request = filterContext.HttpContext.Request;
            filterContext.Result = new HttpStatusCodeWithBodyResult(HttpStatusCode.InternalServerError, exception.Message, request.IsLocal ? exception.StackTrace : exception.Message);
        }

        protected internal virtual IPackage ReadPackageFromRequest(string temporaryFile)
        {
            Stream stream;
            if (Request.Files.Count > 0)
            {
                // If we're using the newer API, the package stream is sent as a file.
                stream = Request.Files[0].InputStream;
            }
            else stream = Request.InputStream;

            using (var temporaryFileStream = System.IO.File.Open(temporaryFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                stream.CopyTo(temporaryFileStream);
            }

            return new OptimizedZipPackage(temporaryFile);
        }

        [ActionName("PackageIDs"), HttpGet]
        public virtual ActionResult GetPackageIds(string partialId, bool? includePrerelease)
        {
            var qry = GetService<IPackageIdsQuery>();
            return new JsonNetResult(qry.Execute(partialId, includePrerelease).ToArray());
        }

        [ActionName("PackageVersions"), HttpGet]
        public virtual ActionResult GetPackageVersions(string id, bool? includePrerelease)
        {
            var qry = GetService<IPackageVersionsQuery>();
            return new JsonNetResult(qry.Execute(id, includePrerelease).ToArray());
        }

        [ActionName("TestPackageApi"), HttpPost]
        public virtual ActionResult SubmitPackageTestResults(string apiKey, string id, string version, bool success, string resultDetailsUrl)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var testReporterUser = userSvc.FindByApiKey(parsedApiKey);
            if (testReporterUser == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submittestresults"));
            // Only the package operations user can submit test results
            if (testReporterUser.Key != settings.PackageOperationsUserKey) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submittestresults"));

            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            if (string.IsNullOrWhiteSpace(resultDetailsUrl))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Submitting test results requires 'resultDetailsUrl' and 'success'.");
            }

            try
            {
                var tempUri = new Uri(resultDetailsUrl);
            }
            catch (Exception)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Submitting test results requires 'resultDetailsUrl' to be a Url.");
            }

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            packageSvc.ChangePackageTestStatus(package, success, resultDetailsUrl, testReporterUser);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package test results have been updated.");
        }

        [ActionName("ValidatePackageApi"), HttpPost]
        public virtual ActionResult SubmitPackageValidationResults(string apiKey, string id, string version, bool success, string validationComments)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var testReporterUser = userSvc.FindByApiKey(parsedApiKey);
            if (testReporterUser == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitvalidationresults"));
            // Only the package operations user can submit test results
            if (testReporterUser.Key != settings.PackageOperationsUserKey) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitvalidationresults"));

            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            if (string.IsNullOrWhiteSpace(validationComments))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Submitting validation results requires 'validationComments' and 'success'.");
            }

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            package.PackageValidationResultDate = DateTime.UtcNow;
            package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Failing;

            var message = "{0} has failed automated validation.".format_with(package.PackageRegistration.Id);
            if (success)
            {
                package.PackageValidationResultStatus = PackageAutomatedReviewResultStatusType.Passing;
                message = "{0} has passed automated validation.".format_with(package.PackageRegistration.Id);
            }

            message += "{0}{1}".format_with(Environment.NewLine, validationComments);

            packageSvc.UpdateSubmittedStatusAfterAutomatedReviews(package);

            packageSvc.ChangePackageStatus(package, package.Status, package.ReviewComments, message, testReporterUser, testReporterUser, sendMaintainerEmail: true, submittedStatus: success ? package.SubmittedStatus : PackageSubmittedStatusType.Waiting, assignReviewer: false);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package validation results have been updated.");
        }

        [ActionName("CleanupPackageApi"), HttpPost]
        public virtual ActionResult SubmitPackageCleanupResults(string apiKey, string id, string version, bool reject, string cleanupComments)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var testReporterUser = userSvc.FindByApiKey(parsedApiKey);
            if (testReporterUser == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitcleanupresults"));
            // Only the package operations user can submit test results
            if (testReporterUser.Key != settings.PackageOperationsUserKey) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitcleanupresults"));

            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            if (string.IsNullOrWhiteSpace(cleanupComments))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Submitting cleanup results requires 'cleanupComments' and 'reject'.");
            }

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            if (!package.PackageCleanupResultDate.HasValue) package.PackageCleanupResultDate = DateTime.UtcNow;

            packageSvc.ChangePackageStatus(package, reject ? PackageStatusType.Rejected : package.Status, package.ReviewComments, cleanupComments, testReporterUser, testReporterUser, sendMaintainerEmail: true, submittedStatus: package.SubmittedStatus, assignReviewer: reject);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package validation results have been updated.");
        }

        [ActionName("DownloadCachePackageApi"), HttpPost]
        public virtual ActionResult SubmitPackageDownloadCacheResults(string apiKey, string id, string version, string cacheStatus, string cacheData)
        {
            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var testReporterUser = userSvc.FindByApiKey(parsedApiKey);
            if (testReporterUser == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitcacheresults"));
            // Only the package operations user can submit test results
            if (testReporterUser.Key != settings.PackageOperationsUserKey) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitcacheresults"));

            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            PackageDownloadCacheStatusType downloadCacheStatus;
            try
            {
                Enum.TryParse(cacheStatus, true, out downloadCacheStatus);
            }
            catch (Exception)
            {
                downloadCacheStatus = PackageDownloadCacheStatusType.Unknown;
            }

            if (downloadCacheStatus == PackageDownloadCacheStatusType.Unknown)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "'cacheStatus' must be passed as 'Available', 'Checked', or 'Investigate'.");
            }

            var cached = downloadCacheStatus == PackageDownloadCacheStatusType.Available;
            if (cached && string.IsNullOrWhiteSpace(cacheData))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Submitting cache with 'cacheStatus'='Available' requires 'cacheData'.");
            }

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            package.DownloadCacheDate = DateTime.UtcNow;
            package.DownloadCacheStatus = downloadCacheStatus;
            if (!string.IsNullOrWhiteSpace(cacheData)) package.DownloadCache = cacheData;

            packageSvc.SaveMinorPackageChanges(package);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package validation results have been updated.");
        }

        [ActionName("ScanPackageApi"), HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 20)]
        public virtual ActionResult GetScanResults(string apiKey, string id, string version, string sha256Checksum)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);
            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            // if (string.IsNullOrWhiteSpace(sha256Checksum)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "Sha256Checksum is required.");

            if (settings.ScanResultsKey.to_lower() != apiKey.to_lower())
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, "The specified key does not provide the authority to get scan results for packages");
            }

            var scanResults = new List<PackageScanResult>();

            var results = scanSvc.GetResults(id, version, sha256Checksum);

            foreach (var result in results.OrEmptyListIfNull())
            {
                scanResults.Add(new PackageScanResult
                {
                    FileName = result.FileName.to_string(),
                    Sha256Checksum = result.Sha256Checksum.to_string(),
                    Positives = result.Positives.to_string(),
                    TotalScans = result.TotalScans.to_string(),
                    ScanDetailsUrl = result.ScanDetailsUrl.to_string(),
                    ScanData = result.ScanData.to_string(),
                    ScanDate = result.ScanDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                });
            }

            return new JsonNetResult(scanResults.ToArray());
        }

        [ActionName("ScanPackageApi"), HttpPost]
        public virtual ActionResult SubmitPackageScanResults(string apiKey, string id, string version, string scanStatus, string scanFlagResult, ICollection<PackageScanResult> scanResults)
        {
            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(version))
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));
            }

            Guid parsedApiKey;
            if (!Guid.TryParse(apiKey, out parsedApiKey)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, Strings.InvalidApiKey);

            var testReporterUser = userSvc.FindByApiKey(parsedApiKey);
            if (testReporterUser == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitscanresults"));
            // Only the package operations user can submit results
            if (testReporterUser.Key != settings.PackageOperationsUserKey) return new HttpStatusCodeWithBodyResult(HttpStatusCode.Forbidden, String.Format(CultureInfo.CurrentCulture, Strings.ApiKeyNotAuthorized, "submitscanresults"));

            if (string.IsNullOrWhiteSpace(scanStatus)) return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "scanStatus is required.");

            PackageScanStatusType packageScanStatus;
            try
            {
                Enum.TryParse(scanStatus.to_string(), true, out packageScanStatus);
            }
            catch (Exception)
            {
                packageScanStatus = PackageScanStatusType.Unknown;
            }

            if (packageScanStatus == PackageScanStatusType.Unknown)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "'scanStatus' must be passed as 'NotFlagged', 'Flagged', 'Exempted', or 'Investigate'.");
            }

            PackageScanFlagResultType packageScanFlagResult;
            try
            {
                Enum.TryParse(scanFlagResult.to_string(), true, out packageScanFlagResult);
            }
            catch (Exception)
            {
                packageScanFlagResult = PackageScanFlagResultType.Unknown;
            }

            if (packageScanFlagResult == PackageScanFlagResultType.Unknown)
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "'scanFlagResult' must be passed as 'None', 'Note', 'Warning', or 'Error'.");
            }

            if (packageScanStatus != PackageScanStatusType.Investigate && !scanResults.Any())
            {
                return new HttpStatusCodeWithBodyResult(HttpStatusCode.BadRequest, "You must submit data with results.");
            }

            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
            if (package == null) return new HttpStatusCodeWithBodyResult(HttpStatusCode.NotFound, string.Format(CultureInfo.CurrentCulture, Strings.PackageWithIdAndVersionNotFound, id, version));

            foreach (var result in scanResults.OrEmptyListIfNull())
            {
                scanSvc.SaveOrUpdateResults(result, package);
            }

            package.PackageScanResultDate = DateTime.UtcNow;
            package.PackageScanStatus = packageScanStatus;
            package.PackageScanFlagResult = packageScanFlagResult;

            var message = "{0} has failed automated scanning.".format_with(package.PackageRegistration.Id);
            if (packageScanStatus == PackageScanStatusType.NotFlagged)
            {
                message = "{0} has passed automated scanning.".format_with(package.PackageRegistration.Id);
            }

            packageSvc.UpdateSubmittedStatusAfterAutomatedReviews(package);

            packageSvc.ChangePackageStatus(package, package.Status, package.ReviewComments, message, testReporterUser, testReporterUser, sendMaintainerEmail: true, submittedStatus: package.SubmittedStatus, assignReviewer: false);

            return new HttpStatusCodeWithBodyResult(HttpStatusCode.Accepted, "Package scan results have been updated.");
        }
    }
}
