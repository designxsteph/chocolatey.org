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
using System.IO;
using System.Web.Mvc;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery
{
    public static class UrlExtensions
    {
        // Shorthand for current url
        public static string Current(this UrlHelper url)
        {
            return url.RequestContext.HttpContext.Request.RawUrl;
        }

        public static string Home(this UrlHelper url)
        {
            return url.RouteUrl(RouteName.Home);
        }

        public static string SearchResults(this UrlHelper url)
        {
            return url.RouteUrl(RouteName.SearchResults);
        }

        public static string SearchResults(
            this UrlHelper url, int page, string sortOrder, string searchTerm, bool prerelease, bool moderatorQueue, string moderationStatus, bool preferenceGridView, bool preferenceModView)
        {
            return url.RouteUrl(RouteName.SearchResults, new { searchTerm, sortOrder, page, prerelease, moderatorQueue, moderationStatus, preferenceGridView, preferenceModView });
        }

        public static string PackageList(
            this UrlHelper url, int page, string sortOrder, string searchTerm, bool prerelease, bool moderatorQueue, string moderationStatus, bool preferenceGridView, bool preferenceModView)
        {
            return url.Action(MVC.Packages.ListPackages(searchTerm, sortOrder, page, prerelease, moderatorQueue, moderationStatus, preferenceGridView, preferenceModView));
        }

        public static string PackageList(this UrlHelper url)
        {
            return url.RouteUrl(RouteName.ListPackages);
        }

        public static string Package(this UrlHelper url, string id)
        {
            return url.Package(id, null);
        }

        public static string Package(this UrlHelper url, string id, string version)
        {
            return url.RouteUrl(
                RouteName.DisplayPackage,
                new
                {
                    id,
                    version
                });
        }

        public static string Package(this UrlHelper url, Package package)
        {
            return url.Package(package.PackageRegistration.Id, package.Version);
        }

        public static string Package(this UrlHelper url, IPackageVersionModel package)
        {
            return url.Package(package.Id, package.Version);
        }

        public static string Package(this UrlHelper url, PackageRegistration package)
        {
            return url.Package(package.Id);
        }

        public static string PackageDownload(this UrlHelper url, string id, string version)
        {
            return PackageDownload(url, 1, id, version);
        }

        public static string PackageDownload(this UrlHelper url, int feedVersion, string id, string version)
        {
            string routeName = "v" + feedVersion + RouteName.DownloadPackage;

            string protocol = AppHarbor.IsSecureConnection(url.RequestContext.HttpContext) ? "https" : "http";
            string returnUrl = url.RouteUrl(
                routeName,
                new
                {
                    Id = id,
                    Version = version
                },
                protocol: protocol);
            //hack, replace removing port
            //return Regex.Replace(returnUrl, @"\:\d+\/", "/");

            // Ensure trailing slashes for versionless package URLs, as a fix for package filenames that look like known file extensions
            return version == null ? EnsureTrailingSlash(returnUrl) : returnUrl;
        }

        public static string LogOn(this UrlHelper url)
        {

            return url.Action(MVC.Authentication.LogOn());
            //return url.RouteUrl(
            //    RouteName.Authentication,
            //    new
            //    {
            //        action = "LogOn"
            //    });
        }

        public static string LogOff(this UrlHelper url)
        {
            return url.Action(MVC.Authentication.LogOff(url.Current()));
        }

        public static string Search(this UrlHelper url, string searchTerm)
        {
            return url.RouteUrl(
                RouteName.ListPackages,
                new
                {
                    q = searchTerm
                });
        }

        public static string UploadPackage(this UrlHelper url)
        {
            return url.Action(MVC.Packages.UploadPackage());
        }

        public static string EditPackage(this UrlHelper url, IPackageVersionModel package)
        {
            return url.Action(MVC.Packages.Edit(package.Id, package.Version));
        }

        public static string DeletePackage(this UrlHelper url, IPackageVersionModel package)
        {
            return url.Action(MVC.Packages.Delete(package.Id, package.Version));
        }

        public static string ManagePackageOwners(this UrlHelper url, IPackageVersionModel package)
        {
            return url.Action(MVC.Packages.ManagePackageOwners(package.Id, package.Version));
        }

        public static string ConfirmationUrl(
            this UrlHelper url, ActionResult actionResult, string username, string token, string protocol)
        {
            return url.Action(
                actionResult.AddRouteValue("username", username).AddRouteValue("token", token), protocol: protocol);
        }

        public static string VerifyPackage(this UrlHelper url)
        {
            return url.Action(MVC.Packages.VerifyPackage());
        }

        internal static string EnsureTrailingSlash(string url)
        {
            if (url != null && !url.EndsWith("/", StringComparison.OrdinalIgnoreCase)) return url + '/';

            return url;
        }

        private static readonly IImageFileService _imagesService = DependencyResolver.Current.GetService<IImageFileService>();

        public static string ImageUrl(this UrlHelper url, string packageId, string version, string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(originalUrl)) return null;

            var imagelocation = _imagesService.CacheAndGetImage(originalUrl, packageId, version);

            if (string.IsNullOrWhiteSpace(imagelocation)) return null;

            if (imagelocation.Equals(originalUrl, StringComparison.InvariantCultureIgnoreCase)) return originalUrl;

            return string.Format("~/content/{0}/{1}", Constants.PackageImagesFolderName, Path.GetFileName(imagelocation));
        }
    }
}
