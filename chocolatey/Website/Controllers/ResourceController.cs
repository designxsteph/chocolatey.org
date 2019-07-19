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
        public ActionResult Resources(string resourceType)
        {
            resourceType = resourceType.Replace("-", "");
            var filePath = Server.MapPath("~/Views/Resources/{0}.cshtml".format_with(resourceType));
            var posts = GetPostsByMostRecentFirst();

            // Find 6 most recent post and tag
            var recentPost = posts.Take(6);
            foreach (var post in recentPost)
            {
                String[] postTags = post.Tags;
                Array.Resize(ref postTags, postTags.Length + 1);
                postTags[postTags.Length - 1] = "recent";
                post.Tags = postTags;
            }
            recentPost = posts.Where(p => p.Tags.Contains("recent"));

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

            // Videos Page
            if (resourceType == "videos")
            {
                ViewBag.Title = "Videos";
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

                posts = successStoryPost.Union(caseStudyPost).Union(featuredPost);
                posts = posts.Union(recentPost).OrderByDescending(p => p.Published).ToList();
            }

            // Return Views
            if (resourceType == "home" || resourceType == "testimonials")
            {
                return View("~/Views/Resources/{0}.cshtml".format_with(resourceType), posts);
            }

            return View("~/Views/Resources/Type.cshtml", "~/Views/Resources/_Layout.cshtml", posts);
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult ResourceName(string resourceType, string resourceName)
        {
            var resourceNameNoHyphens = resourceName.Replace("-", "");
            var filePath = Server.MapPath("~/Views/Resources/Files/{0}.md".format_with(resourceNameNoHyphens));

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