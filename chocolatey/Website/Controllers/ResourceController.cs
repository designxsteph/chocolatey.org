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
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Markdig;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery.Controllers
{
    public class ResourceController : Controller
    {
        private readonly IFileSystemService _fileSystem;
        public IConfiguration Configuration { get; set; }
        public MarkdownPipeline MarkdownPipeline { get; set; }

        public ResourceController(IFileSystemService fileSystem, IConfiguration configuration)
        {
            _fileSystem = fileSystem;
            Configuration = configuration;

            MarkdownPipeline = new MarkdownPipelineBuilder()
                 .UseSoftlineBreakAsHardlineBreak()
                 .UseAdvancedExtensions()
                 .Build();
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult Resources(string resourceType, string q, int page = 1)
        {
            q = (q ?? string.Empty).Trim();
            if (page < 1) page = 1;
            resourceType = resourceType.Replace("-", "");
            var filePath = Server.MapPath("~/Views/Resources/{0}.cshtml".format_with(resourceType));
            var posts = GetPostsByMostRecentFirst();
            ViewBag.SearchTerm = q;
            var searchFound = false;

            // Find 6 most recent post and tag
            var recentPost = posts.Where(p => !p.Type.Equals("Testimonial")).Take(6);
            foreach (var post in recentPost)
            {
                String[] postTags = post.Tags;
                Array.Resize(ref postTags, postTags.Length + 1);
                postTags[postTags.Length - 1] = "recent";
                post.Tags = postTags;
            }
            recentPost = posts.Where(p => p.Tags.Contains("recent"));

            foreach (var post in posts)
            {
                // Get queries needed for search
                var titleQuery = Request.QueryString["q"] ?? string.Empty;
                var tagQuery = titleQuery.ToLower().Contains("tag");

                // Search posts
                var searchTitle = !tagQuery && post.Title.ToLower().Contains(ViewBag.SearchTerm.ToLower());
                var searchTag = tagQuery && post.Tags.Contains(titleQuery.ToString().Replace("+", string.Empty).Substring(4));
                var searchAll = searchTitle || post.Tags.Contains(titleQuery);

                // Find post search results
                var titleResults = posts.Where(p => p.Title.ToLower().Contains(ViewBag.SearchTerm.ToLower()));
                var tagResults = posts.Where(p => p.Tags.Contains(titleQuery));

                if (!string.IsNullOrEmpty(ViewBag.SearchTerm) && searchAll || searchTag)
                {
                    searchFound = true;

                    if (searchAll && searchFound) // no tag: specified- search everything
                    {
                        posts = titleResults.Union(tagResults);
                    }
                    else if (searchTag && searchFound) // tag: specified- search tags only
                    {
                        tagResults = posts.Where(p => p.Tags.Contains(titleQuery.ToString().Replace("+", string.Empty).Substring(4)));
                        posts = tagResults;
                    }
                }
            }

            // Has a query string, but no searches found- return 0
            if (!string.IsNullOrEmpty(ViewBag.SearchTerm) && !searchFound)
            {
                posts = Enumerable.Empty<ResourceViewModel>();
            }

            // If any search
            if (!string.IsNullOrEmpty(ViewBag.SearchTerm))
            {
                ViewBag.Title = "Resources matching " + ViewBag.SearchTerm;
            }

            // Success Stories Page
            if (resourceType == "successstories")
            {
                ViewBag.Title = "Customer Success Stories";
                posts = posts.Where(p => p.Type.Equals("Customer Success Story"));
            }

            // Case Studies Page
            if (resourceType == "casestudies")
            {
                ViewBag.Title = "Case Studies";
                posts = posts.Where(p => p.Type.Equals("Case Study"));
            }

            // Videos Page (no search)
            if (resourceType == "videos" && string.IsNullOrEmpty(ViewBag.SearchTerm))
            {
                ViewBag.Title = "Videos";
                posts = posts.Where(p => !p.Type.Equals("Testimonial"));
            }

            // Testimonials Page
            if (resourceType == "testimonials")
            {
                ViewBag.Title = "Testimonials";
                posts = posts.Where(p => p.Type.Equals("Testimonial"));
            }

            // Home Page
            if (resourceType == "home")
            {
                // Randomize Posts
                posts = posts.OrderBy(a => Guid.NewGuid()).ToList();

                var featuredPost = posts.Where(p => p.Tags.Contains("featured"));
                featuredPost = featuredPost.Count() > 3 ? featuredPost.Skip(featuredPost.Count() - 2) : featuredPost;

                var successStoryPost = posts.Where(p => p.Type.Equals("Customer Success Story")).Where(p => !p.Tags.Contains("featured"));
                successStoryPost = successStoryPost.Count() > 3 ? successStoryPost.Skip(successStoryPost.Count() - 2) : successStoryPost;

                var caseStudyPost = posts.Where(p => p.Type.Equals("Case Study")).Where(p => !p.Tags.Contains("featured"));
                caseStudyPost = caseStudyPost.Count() > 3 ? caseStudyPost.Skip(caseStudyPost.Count() - 2) : caseStudyPost;

                var testimonialPost = posts.Where(p => p.Type.Equals("Testimonial")).Where(p => !p.Tags.Contains("featured"));
                testimonialPost = testimonialPost.Count() > 3 ? testimonialPost.Skip(testimonialPost.Count() - 3) : testimonialPost;

                posts = successStoryPost.Union(caseStudyPost).Union(testimonialPost).Union(featuredPost);
                posts = posts.Union(recentPost).OrderByDescending(p => p.Published).ToList();
            }

            // Paging
            const int pageSize = 30; // Resources to be shown on each page
            ViewBag.TotalCount = posts.Count(); // Gets total count 
            posts = posts.Skip((page - 1) * pageSize).Take(pageSize).ToList(); // Shows number of resources on page based on pageSize
            ViewBag.LastIndex = page * posts.Count();
            ViewBag.FirstIndex = ViewBag.LastIndex - pageSize + 1;
            ViewBag.NextPage = page + 1;
            ViewBag.PreviousPage = page - 1;
            if (posts.Count() < pageSize)
            {
                ViewBag.LastIndex = ViewBag.TotalCount;
                ViewBag.FirstIndex = ViewBag.LastIndex - posts.Count() + 1;
            }
            if (ViewBag.LastIndex != ViewBag.TotalCount)
            {
                ViewBag.hasNextPage = true;
            }
            if (ViewBag.FirstIndex > posts.Count())
            {
                ViewBag.hasPreviousPage = true;
            }

            // Return Views
            if (resourceType == "home")
            {
                return View("~/Views/Resources/{0}.cshtml".format_with(resourceType), posts);
            }

            return View("~/Views/Resources/Type.cshtml", "~/Views/Resources/_Layout.cshtml", posts);
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult ResourceName(string resourceType, string resourceName, string q)
        {
            q = (q ?? string.Empty).Trim();
            var resourceNameNoHyphens = resourceName.Replace("-", "");
            var filePath = Server.MapPath("~/Views/Resources/Files/{0}.md".format_with(resourceNameNoHyphens));
            ViewBag.SearchTerm = q;

            if (_fileSystem.FileExists(filePath))
            {
                return View("~/Views/Resources/Post.cshtml", "~/Views/Resources/_Layout.cshtml", GetPost(filePath, resourceName));
            }
            else
            {
                // check by urls
                var post = GetPostsByMostRecentFirst().FirstOrDefault(p => p.UrlPath.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
                if (post != null) return View("~/Views/Resources/Post.cshtml", "~/Views/Resources/_Layout.cshtml", post);
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }

        private IEnumerable<ResourceViewModel> GetPostsByMostRecentFirst()
        {
            IList<ResourceViewModel> posts = new List<ResourceViewModel>();

            var postsDirectory = Server.MapPath("~/Views/Resources/Files/");
            var postFiles = Directory.GetFiles(postsDirectory, "*.md", SearchOption.TopDirectoryOnly);
            foreach (var postFile in postFiles)
            {
                posts.Add(GetPost(postFile));
            }

            return posts.OrderByDescending(p => p.Published).ToList();
        }

        private ResourceViewModel GetPost(string filePath, string resourceName = null)
        {
            var model = new ResourceViewModel();
            if (_fileSystem.FileExists(filePath))
            {
                var contents = string.Empty;
                using (var fileStream = System.IO.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    contents = streamReader.ReadToEnd();
                }

                model.UrlPath = GetPostMetadataValue("Url", contents);
                if (string.IsNullOrEmpty(model.UrlPath)) model.UrlPath = GetUrl(filePath, resourceName);
                model.Published = DateTime.ParseExact(GetPostMetadataValue("Published", contents), "yyyyMMdd", CultureInfo.InvariantCulture);
                model.Title = GetPostMetadataValue("Title", contents);
                model.Type = GetPostMetadataValue("Type", contents);
                model.Name = GetPostMetadataValue("Name", contents);
                model.Company = GetPostMetadataValue("Company", contents);
                model.Position = GetPostMetadataValue("Position", contents);
                model.Video = GetPostMetadataValue("Video", contents);
                model.Tags = GetPostMetadataValue("Tags", contents).Split(' ');
                model.Image = Markdown.ToHtml(GetPostMetadataValue("Image", contents), MarkdownPipeline);
                model.Quote = GetPostMetadataValue("Quote", contents);
                model.Summary = GetPostMetadataValue("Summary", contents);
                model.Post = Markdown.ToHtml(contents.Remove(0, contents.IndexOf("---") + 3), MarkdownPipeline);
            }
            
            return model;
        }

        private string GetUrl(string filePath, string resourceName = null)
        {
            if (!string.IsNullOrWhiteSpace(resourceName)) return resourceName;
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var hyphenatedValue = new StringBuilder();

            Char previousChar = '^';
            foreach (var valueChar in fileName)
            {
                if (Char.IsUpper(valueChar) && hyphenatedValue.Length != 0)
                {
                    hyphenatedValue.Append("-");
                }

                if (Char.IsDigit(valueChar) && !Char.IsDigit(previousChar) && hyphenatedValue.Length != 0)
                {
                    hyphenatedValue.Append("-");
                }

                previousChar = valueChar;
                hyphenatedValue.Append(valueChar.to_string());
            }

            return hyphenatedValue.to_string().to_lower();
        }

        private string GetPostMetadataValue(string name, string contents)
        {
            var regex = new Regex(@"(?:^{0}\s*:\s*)([^\r\n]*)(?>\s*\r?$)".format_with(name), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var match = regex.Match(contents);
            return match.Groups[1].Value;
        }

    }
}