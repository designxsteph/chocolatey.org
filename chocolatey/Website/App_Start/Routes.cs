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

using System.Web.Mvc;
using System.Web.Routing;
using MvcHaack.Ajax;
using RouteMagic;

namespace NuGetGallery
{
    public static class Routes
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
            routes.IgnoreRoute("{*Content}", new { imgs = @"(.*/)?Content(/.*)?" });
            routes.IgnoreRoute("{*Scripts}", new { scripts = @"(.*/)?Scripts(/.*)?" });

            routes.MapRoute(RouteName.Home, "", MVC.Pages.Home());

            routes.MapRouteSeo(
                RouteName.InstallerBatchFile, "installChocolatey.cmd", new
                {
                    controller = "Pages",
                    Action = "InstallerBatchFile"
                });

            routes.MapRouteSeo(
               RouteName.Features, "features", new
               {
                   controller = "Pages",
                   Action = "Features"
               });

            routes.MapRouteSeo(
               RouteName.About, "about", new
               {
                   controller = "Pages",
                   Action = "About"
               });

            routes.MapRouteSeo(
                RouteName.Notice, "notice", new
                {
                    controller = "Pages",
                    Action = "Notice"
                });

            var pricingRoute = routes.MapRoute(
                RouteName.Pricing, "pricing", new
                {
                    controller = "Pages",
                    Action = "Pricing"
                });

            routes.MapRoute(
                RouteName.Discount, "discount", new
                {
                    controller = "Pages",
                    Action = "Discount"
                });

            routes.MapRoute(
                RouteName.Evaluation, "evaluation", new
                {
                    controller = "Pages",
                    Action = "Evaluation"
                });

            routes.MapRouteSeo(
               RouteName.Compare, "compare", new
               {
                   controller = "Pages",
                   Action = "Compare"
               });

            routes.MapRouteSeo(
                RouteName.Install, "install", new
                {
                    controller = "Pages",
                    Action = "Install"
                });

            routes.MapRouteSeo(
               RouteName.Sitemap, "sitemap", new
               {
                   controller = "Pages",
                   Action = "Sitemap"
               });

            routes.MapRouteSeo(
               RouteName.Community, "community", new
               {
                   controller = "Pages",
                   Action = "Community"
               });

            var productsRoute = routes.MapRoute(
                RouteName.Products,
                "products/{productName}",
                new { controller = "Products", action = "Products", productName = "home" }
                );

            routes.Redirect(r => r.MapRoute(RouteName.Business, "business")).To(productsRoute, new { productName = "chocolatey-for-business"});
            routes.Redirect(r => r.MapRoute(RouteName.ChocolateyForBusiness, "chocolatey-for-business")).To(productsRoute, new {productName = "chocolatey-for-business"});
            routes.Redirect(r => r.MapRoute(RouteName.C4B, "c4b")).To(productsRoute, new { productName = "chocolatey-for-business" });

            routes.MapRouteSeo(
               RouteName.WhyChocolatey, "why-chocolatey", new
               {
                   controller = "Pages",
                   Action = "WhyChocolatey"
               });

            routes.MapRouteSeo(
               RouteName.UseCases, "use-cases", new
               {
                   controller = "Pages",
                   Action = "UseCases"
               });

            routes.MapRouteSeo(
               RouteName.HowChocolateyWorks, "how-chocolatey-works", new
               {
                   controller = "Pages",
                   Action = "HowChocolateyWorks"
               });

            routes.MapRouteSeo(
               RouteName.Careers, "careers", new
               {
                   controller = "Pages",
                   Action = "Careers"
               });

            routes.MapRouteSeo(
                RouteName.FAQ, "faq", new
                {
                    controller = "Pages",
                    Action = "FAQ"
                });

            routes.MapRouteSeo(
                RouteName.Kickstarter, "kickstarter", new
                {
                    controller = "Pages",
                    Action = "Kickstarter"
                });

            routes.MapRouteSeo(
                RouteName.Terms, "terms", new
                {
                    controller = "Pages",
                    Action = "Terms"
                });

            routes.MapRouteSeo(
                RouteName.Privacy, "privacy", new
                {
                    controller = "Pages",
                    Action = "Privacy"
                });

            routes.MapRouteSeo(
                RouteName.MediaKit, "media-kit", new
                {
                    controller = "Pages",
                    Action = "MediaKit"
                });

            routes.MapRouteSeo(
                RouteName.Company, "company", new
                {
                    controller = "Pages",
                    Action = "Company"
                });

            routes.MapRouteSeo(
                RouteName.ContactTrial, "contact/trial", new
                {
                    controller = "Pages",
                    Action = "ContactTrial"
                });

            routes.MapRouteSeo(
                RouteName.ContactUs, "contact", new
                {
                    controller = "Pages",
                    Action = "ContactUs"
                });

            routes.MapRouteSeo(
                RouteName.ContactGeneral, "contact/general", new
                {
                    controller = "Pages",
                    Action = "ContactGeneral"
                });

            routes.MapRouteSeo(
                RouteName.ContactDiscount, "contact/student-discount", new
                {
                    controller = "Pages",
                    Action = "ContactDiscount"
                });

            routes.MapRouteSeo(
                RouteName.ContactPartner, "contact/partner", new
                {
                    controller = "Pages",
                    Action = "ContactPartner"
                });

            routes.MapRouteSeo(
                RouteName.ContactSales, "contact/sales", new
                {
                    controller = "Pages",
                    Action = "ContactSales"
                });

            routes.MapRouteSeo(
                RouteName.ContactSalesOther, "contact/sales-for-another-organization", new
                {
                    controller = "Pages",
                    Action = "ContactSalesOther"
                });

            routes.MapRouteSeo(
                RouteName.ContactBlocked, "contact/blocked-ip-address", new
                {
                    controller = "Pages",
                    Action = "ContactBlocked"
                });

            routes.MapRouteSeo(
                RouteName.ContactQuickDeployment, "contact/quick-deployment", new
                {
                    controller = "Pages",
                    Action = "ContactQuickDeployment"
                });

            routes.MapRouteSeo(
                RouteName.Support, "support", new
                {
                    controller = "Pages",
                    Action = "Support"
                });

            routes.MapRouteSeo(
                RouteName.ReportIssue, "bugs", new
                {
                    controller = "Pages",
                    Action = "ReportIssue"
                });

            routes.MapRouteSeo(
                RouteName.Press, "press", new
                {
                    controller = "Pages",
                    Action = "Press"
                });

            routes.MapRouteSeo(
                RouteName.Partner, "partner", new
                {
                    controller = "Pages",
                    Action = "Partner"
                });

            routes.MapRouteSeo(
                RouteName.Security, "security", new
                {
                    controller = "Pages",
                    Action = "Security"
                });

            routes.MapRouteSeo(
                RouteName.Covid19, "covid-19", new
                {
                    controller = "Pages",
                    Action = "Covid19"
                });

            routes.MapRouteSeo(
                RouteName.Resources,
                "resources/{resourceType}",
                new { controller = "Resource", action = "Resources", resourceType = "home" }
                );

            var resourceNameRoute = new Route("resources/{resourceType}/{resourceName}", new RouteValueDictionary(new { controller = "Resource", action = "ResourceName" }), new HyphenatedRouteHandler());

            // ResourceName redirects
            routes.Redirect(r => r.MapRoute("ManagingWindowsAtFacebookRedirect", "resources/case-studies/managing-windows-at-facebook")).To(resourceNameRoute, new { resourceType="case-studies", resourceName = "winops-2017-dan-sedlacek-managing-windows-at-facebook" });

            // Add in ResourceName route after redirects have been made
            routes.Add(RouteName.ResourceName, resourceNameRoute);

            routes.MapRouteSeo(
                RouteName.Courses,
                "courses",
                new { controller = "Courses", action = "Courses" }
                );

            routes.MapRouteSeo(
                RouteName.CourseName,
                "courses/{courseName}/{courseModuleName}",
                new { controller = "Courses", action = "CourseName", courseName = "home", courseModuleName = "home" }
                );

            var solutionsRoute = routes.MapRoute(
                RouteName.Solutions,
                "solutions/{solutionName}",
                new { controller = "Solutions", action = "Solutions", solutionName = "home" }
                );

            routes.Redirect(r => r.MapRoute(RouteName.SelfService, "self-service")).To(solutionsRoute);

            var eventsRoute = routes.MapRoute(
                RouteName.Events,
                "events/{eventName}",
                new { controller = "Events", action = "Events", eventName = "home" }
                );

            routes.Redirect(r => r.MapRoute(RouteName.CurrentWebinar, "webinar")).To(eventsRoute, new { eventName = "chocolatey-deployments" });

            routes.MapRoute(RouteName.Stats, "stats", MVC.Pages.Stats());

            routes.MapRoute(
                "rss feed", "feed.rss", new
                {
                    controller = "RSS",
                    Action = "feed.rss"
                });

            routes.Add(new JsonRoute("json/{controller}"));

            routes.MapRouteSeo(
               RouteName.Policies, "policies", new
               {
                   controller = "Pages",
                   Action = "Terms"
               });

            var packageListRoute = routes.MapRoute(RouteName.ListPackages, "packages", MVC.Packages.ListPackages());

            routes.MapRoute(
                RouteName.NotifyComment, "packages/{packageId}/notify-comment", new
                {
                    controller = MVC.Packages.Name,
                    action = "NotifyMaintainersOfAddedComment"
                });

            var uploadPackageRoute = routes.MapRoute(RouteName.UploadPackage, "packages/upload", MVC.Packages.UploadPackage());

            routes.MapRoute(
                RouteName.PackageOwnerConfirmation, "packages/{id}/owners/{username}/confirm/{token}", new
                {
                    controller = MVC.Packages.Name,
                    action = "ConfirmOwner"
                });

            // We need the following two routes (rather than just one) due to Routing's
            // Consecutive Optional Parameter bug. :(
            var packageDisplayRoute = routes.MapRoute(
                RouteName.DisplayPackage, "packages/{id}/{version}", MVC.Packages.DisplayPackage().AddRouteValue("version", UrlParameter.Optional), null /*defaults*/, new
                {
                    version = new VersionRouteConstraint()
                });

            var packageVersionActionRoute = routes.MapRoute(
                RouteName.PackageVersionAction, "packages/{id}/{version}/{action}", new
                {
                    controller = MVC.Packages.Name
                }, new
                {
                    version = new VersionRouteConstraint()
                });

            var packageActionRoute = routes.MapRoute(
                RouteName.PackageAction, "packages/{id}/{action}", new
                {
                    controller = MVC.Packages.Name
                });

            var resendRoute = routes.MapRoute("ResendConfirmation", "account/ResendConfirmation", MVC.Users.ResendConfirmation());

            //Redirecting v1 Confirmation Route
            routes.Redirect(r => r.MapRoute("v1Confirmation", "Users/Account/ChallengeEmail")).To(resendRoute);

            routes.MapRoute(
                RouteName.Authentication, "users/account/{action}", new
                {
                    controller = MVC.Authentication.Name
                });

            routes.MapRoute(RouteName.Profile, "profiles/{username}", MVC.Users.Profiles());

            routes.MapRoute(RouteName.PasswordReset, "account/{action}/{username}/{token}", MVC.Users.ResetPassword());

            routes.MapRoute(RouteName.Account, "account/{action}", MVC.Users.Account());

            routes.MapRoute(
                "site" + RouteName.DownloadPackage, "packages/{id}/{version}/DownloadPackage", MVC.Api.GetPackage(), defaults: new
                {
                    version = UrlParameter.Optional
                }, constraints: new
                {
                    httpMethod = new HttpMethodConstraint("GET")
                });

            // V1 Routes
            routes.MapRoute("v1Legacy" + RouteName.PushPackageApi, "PackageFiles/{apiKey}/nupkg", MVC.Api.CreatePackagePost());
            routes.MapRoute("v1Legacy" + RouteName.PublishPackageApi, "PublishedPackages/Publish", MVC.Api.PublishPackage());

            // V2 routes
            routes.MapRoute(
                "v2" + RouteName.VerifyPackageKey, "api/v2/verifykey/{id}/{version}", MVC.Api.VerifyPackageKey(), defaults: new
                {
                    id = UrlParameter.Optional,
                    version = UrlParameter.Optional
                });

            routes.MapRoute(
                "v2" + RouteName.DownloadPackage, "api/v2/package/{id}/{version}", MVC.Api.GetPackage(), defaults: new
                {
                    version = UrlParameter.Optional
                }, constraints: new
                {
                    httpMethod = new HttpMethodConstraint("GET")
                });

            routes.MapRoute(
                "v2" + RouteName.PushPackageApi, "api/v2/package", MVC.Api.CreatePackagePut(), defaults: null, constraints: new
                {
                    httpMethod = new HttpMethodConstraint("PUT")
                });

            routes.MapRoute(
                "v2" + RouteName.DeletePackageApi, "api/v2/package/{id}/{version}", MVC.Api.DeletePackage(), defaults: null, constraints: new
                {
                    httpMethod = new HttpMethodConstraint("DELETE")
                });

            routes.MapRoute(
                "v2" + RouteName.PublishPackageApi, "api/v2/package/{id}/{version}", MVC.Api.PublishPackage(), defaults: null, constraints: new
                {
                    httpMethod = new HttpMethodConstraint("POST")
                });

            routes.MapServiceRoute(RouteName.V2ApiSubmittedFeed, "api/v2/submitted", typeof(V2SubmittedFeed));

            routes.MapRoute(
                 "v2" + RouteName.TestPackageApi,
                 "api/v2/test/{id}/{version}",
                 MVC.Api.TestPackage(),
                 defaults: null,
                 constraints: new
                 {
                    httpMethod = new HttpMethodConstraint("POST")
                 }
            );

            routes.MapRoute(
                 "v2" + RouteName.ValidatePackageApi,
                 "api/v2/validate/{id}/{version}",
                 MVC.Api.ValidatePackage(),
                 defaults: null,
                 constraints: new
                 {
                    httpMethod = new HttpMethodConstraint("POST")
                 }
            );

            routes.MapRoute(
                 "v2" + RouteName.CleanupPackageApi,
                 "api/v2/cleanup/{id}/{version}",
                 MVC.Api.CleanupPackage(),
                 defaults: null,
                 constraints: new
                 {
                    httpMethod = new HttpMethodConstraint("POST")
                 }
            );

            routes.MapRoute(
                 "v2" + RouteName.DownloadCachePackageApi,
                 "api/v2/cache/{id}/{version}",
                 MVC.Api.DownloadCachePackage(),
                 defaults: null,
                 constraints: new
                 {
                    httpMethod = new HttpMethodConstraint("POST")
                 }
            );

            routes.MapRoute(
                 "v2" + RouteName.ScanPackageApi,
                 "api/v2/scan/{id}/{version}",
                 MVC.Api.ScanPackage(),
                 defaults: null
            );

            routes.MapRouteSeo(
                "Search", "search", new
                {
                    controller = "Packages",
                    Action = "ListPackages"
                });

            routes.MapRoute("v2PackageIds", "api/v2/package-ids", MVC.Api.GetPackageIds());

            routes.MapRoute("v2PackageVersions", "api/v2/package-versions/{id}", MVC.Api.GetPackageVersions());

            routes.MapServiceRoute(RouteName.V2ApiFeed, "api/v2/", typeof(V2Feed));

            // Redirected Legacy Routes

            routes.Redirect(r => r.MapRoute("ReportAbuse", "Package/ReportAbuse/{id}/{version}", MVC.Packages.ReportAbuse()), permanent: true).To(packageVersionActionRoute);

            routes.Redirect(
                r => r.MapRoute(
                    "PackageActions", "Package/{action}/{id}", MVC.Packages.ContactOwners(), null /*defaults*/, // This next bit looks bad, but it's not. It will never change because
                    // it's mapping the legacy routes to the new better routes.
                    new
                    {
                        action = "ContactOwners|ManagePackageOwners"
                    }), permanent: true).To(packageActionRoute);

            // TODO: this route looks broken as there is no EditPackage action
            //routes.Redirect(
            //    r => r.MapRoute(
            //        "EditPackage",
            //        "Package/Edit/{id}/{version}",
            //        new { controller = PackagesController.ControllerName, action = "EditPackage" }),
            //    permanent: true).To(packageVersionActionRoute);

            routes.Redirect(r => r.MapRoute(RouteName.ListPackages, "List/Packages", MVC.Packages.ListPackages()), permanent: true).To(packageListRoute);

            routes.Redirect(r => r.MapRoute(RouteName.DisplayPackage, "List/Packages/{id}/{version}", MVC.Packages.DisplayPackage().AddRouteValue("version", UrlParameter.Optional)), permanent: true)
                  .To(packageDisplayRoute);

            routes.Redirect(r => r.MapRoute(RouteName.NewSubmission, "Contribute/NewSubmission", MVC.Packages.UploadPackage()), permanent: true).To(uploadPackageRoute);
        }
    }
}
