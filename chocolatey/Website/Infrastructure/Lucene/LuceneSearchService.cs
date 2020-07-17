using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace NuGetGallery
{
    public class LuceneSearchService : ISearchService
    {
        private readonly bool _containsAllVersions;
        private static readonly string[] FieldAliases = new[] { "Id", "Title", "Tag", "Tags", "Description", "Author", "Authors", "Owner", "Owners" };
        private static readonly string[] Fields = new[] { "Id", "Title", "Tags", "Description", "Authors", "Owners" };
        private Lucene.Net.Store.Directory _directory;

        public LuceneSearchService()
            : this(containsAllVersions: false)
        {
        }

        public LuceneSearchService(bool containsAllVersions)
        {
            _containsAllVersions = containsAllVersions;
            _directory = new LuceneFileSystem(LuceneCommon.IndexDirectory);
        }

        public bool ContainsAllVersions { get { return _containsAllVersions; } }

        public SearchResults Search(SearchFilter searchFilter)
        {
            if (searchFilter == null)
            {
                throw new ArgumentNullException("searchFilter");
            }

            if (searchFilter.Skip < 0)
            {
                throw new ArgumentOutOfRangeException("searchFilter");
            }

            if (searchFilter.Take < 0)
            {
                throw new ArgumentOutOfRangeException("searchFilter");
            }

            return SearchCore(searchFilter);
        }

        private SearchResults SearchCore(SearchFilter searchFilter)
        {
            // Get index timestamp
            DateTime timestamp = File.GetLastWriteTimeUtc(LuceneCommon.IndexMetadataPath);

            int numRecords = searchFilter.Skip + searchFilter.Take;

            // we want all results the first time through
            if (searchFilter.TakeAllResults)
            {
                numRecords = Int32.MaxValue;
            }

            var searcher = new IndexSearcher(_directory, readOnly: true);
            var query = ParseQuery(searchFilter);

            //// If searching by relevance, boost scores by download count.
            //if (searchFilter.SortProperty == SortProperty.Relevance)
            //{
            //    var downloadCountBooster = new FieldScoreQuery("DownloadCount", FieldScoreQuery.Type.INT);
            //    query = new CustomScoreQuery(query, downloadCountBooster);
            //}

            var filterTerm = searchFilter.IncludePrerelease ? "IsLatest" : "IsLatestStable";
            Query filterQuery = new TermQuery(new Term(filterTerm, Boolean.TrueString));

            Filter filter = new QueryWrapperFilter(filterQuery);

            if (searchFilter.IncludeAllVersions)
            {
                filter = searchFilter.IncludePrerelease ?
                    new QueryWrapperFilter(new TermQuery(new Term("InIndex", Boolean.TrueString)))
                    : new QueryWrapperFilter(new TermQuery(new Term("IsPrerelease", Boolean.FalseString)));
            }

            var results = searcher.Search(query, filter: filter, n: numRecords, sort: new Sort(GetSortField(searchFilter)));

            if (results.TotalHits == 0 || searchFilter.CountOnly)
            {
                return new SearchResults(results.TotalHits, timestamp);
            }

            var packages = results.ScoreDocs
                                  .Skip(searchFilter.Skip)
                                  .Select(sd => PackageFromDoc(searcher.Doc(sd.Doc)))
                                  .ToList();

            return new SearchResults(
                results.TotalHits,
                timestamp,
                packages.AsQueryable());
        }

        private static Package PackageFromDoc(Document doc)
        {
            int downloadCount = Int32.Parse(doc.Get("DownloadCount"), CultureInfo.InvariantCulture);
            int versionDownloadCount = Int32.Parse(doc.Get("VersionDownloadCount"), CultureInfo.InvariantCulture);
            int key = Int32.Parse(doc.Get("Key"), CultureInfo.InvariantCulture);
            int packageRegistrationKey = Int32.Parse(doc.Get("PackageRegistrationKey"), CultureInfo.InvariantCulture);
            int packageSize = Int32.Parse(doc.Get("PackageFileSize"), CultureInfo.InvariantCulture);
            bool isLatest = Boolean.Parse(doc.Get("IsLatest"));
            bool isLatestStable = Boolean.Parse(doc.Get("IsLatestStable"));
            bool isPrerelease = Boolean.Parse(doc.Get("IsPrerelease"));
            bool isListed = Boolean.Parse(doc.Get("Listed"));
            bool requiresLicenseAcceptance = Boolean.Parse(doc.Get("RequiresLicenseAcceptance"));
            DateTime created = DateTime.Parse(doc.Get("Created"), CultureInfo.InvariantCulture);
            DateTime published = DateTime.Parse(doc.Get("Published"), CultureInfo.InvariantCulture);
            DateTime lastUpdated = DateTime.Parse(doc.Get("LastUpdated"), CultureInfo.InvariantCulture);

            DateTime? packageTestResultDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageTestResultStatusDate"))) packageTestResultDate = DateTime.Parse(doc.Get("PackageTestResultStatusDate"), CultureInfo.InvariantCulture);
            DateTime? packageValidationResultDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageValidationResultDate"))) packageValidationResultDate = DateTime.Parse(doc.Get("PackageValidationResultDate"), CultureInfo.InvariantCulture);
            DateTime? packageCleanupResultDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageCleanupResultDate"))) packageCleanupResultDate = DateTime.Parse(doc.Get("PackageCleanupResultDate"), CultureInfo.InvariantCulture);
            DateTime? reviewedDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageReviewedDate"))) reviewedDate = DateTime.Parse(doc.Get("PackageReviewedDate"), CultureInfo.InvariantCulture);
            DateTime? approvedDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageApprovedDate"))) approvedDate = DateTime.Parse(doc.Get("PackageApprovedDate"), CultureInfo.InvariantCulture);
            DateTime? downloadCacheDate = null;
            if (!string.IsNullOrEmpty(doc.Get("DownloadCacheDate"))) downloadCacheDate = DateTime.Parse(doc.Get("DownloadCacheDate"), CultureInfo.InvariantCulture);
            DateTime? packageScanResultDate = null;
            if (!string.IsNullOrEmpty(doc.Get("PackageScanResultDate"))) packageScanResultDate = DateTime.Parse(doc.Get("PackageScanResultDate"), CultureInfo.InvariantCulture);

            var owners = doc.Get("FlattenedOwners")
                            .split_safe(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => new User { Username = o })
                            .ToArray();
            var frameworks =
                doc.Get("JoinedSupportedFrameworks")
                   .split_safe(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => new PackageFramework { TargetFramework = s })
                   .ToArray();
            var dependencies =
                doc.Get("FlattenedDependencies")
                   .split_safe(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(CreateDependency)
                   .ToArray();

            return new Package
            {
                Key = key,
                PackageRegistrationKey = packageRegistrationKey,
                PackageRegistration = new PackageRegistration
                {
                    Id = doc.Get("Id-Original"),
                    DownloadCount = downloadCount,
                    Key = packageRegistrationKey,
                    Owners = owners
                },
                Version = doc.Get("Version"),
                Title = doc.Get("Title"),
                Summary = doc.Get("Summary"),
                Description = doc.Get("Description"),
                Tags = doc.Get("Tags"),
                FlattenedAuthors = doc.Get("Authors"),
                Copyright = doc.Get("Copyright"),
                Created = created,
                FlattenedDependencies = doc.Get("FlattenedDependencies"),
                Dependencies = dependencies,
                DownloadCount = versionDownloadCount,
                IconUrl = doc.Get("IconUrl"),
                IsLatest = isLatest,
                IsLatestStable = isLatestStable,
                IsPrerelease = isPrerelease,
                Listed = isListed,
                Language = doc.Get("Language"),
                LastUpdated = lastUpdated,
                Published = published,
                LicenseUrl = doc.Get("LicenseUrl"),
                RequiresLicenseAcceptance = requiresLicenseAcceptance,
                Hash = doc.Get("Hash"),
                HashAlgorithm = doc.Get("HashAlgorithm"),
                PackageFileSize = packageSize,
                ProjectUrl = doc.Get("ProjectUrl"),
                ReleaseNotes = doc.Get("ReleaseNotes"),

                ProjectSourceUrl = doc.Get("ProjectSourceUrl"),
                PackageSourceUrl = doc.Get("PackageSourceUrl"),
                DocsUrl = doc.Get("DocsUrl"),
                MailingListUrl = doc.Get("MailingListUrl"),
                BugTrackerUrl = doc.Get("BugTrackerUrl"),
                StatusForDatabase = doc.Get("PackageStatus"),
                SubmittedStatusForDatabase = doc.Get("PackageSubmittedStatus"),
                PackageTestResultUrl = doc.Get("PackageTestResultUrl"),
                PackageTestResultStatusForDatabase = doc.Get("PackageTestResultStatus"),
                PackageTestResultDate = packageTestResultDate,
                PackageValidationResultStatusForDatabase = doc.Get("PackageValidationResultStatus"),
                PackageValidationResultDate = packageValidationResultDate,
                PackageCleanupResultDate = packageCleanupResultDate,
                ReviewedDate = reviewedDate,
                ApprovedDate = approvedDate,
                ReviewedBy = new User { Username = doc.Get("PackageReviewer") },
                DownloadCacheStatusForDatabase = doc.Get("DownloadCacheStatus"),
                DownloadCacheDate = downloadCacheDate,
                DownloadCache = doc.Get("DownloadCache"),
                PackageScanStatusForDatabase = doc.Get("PackageScanStatus"),
                PackageScanResultDate = packageScanResultDate,
                PackageScanFlagResultForDatabase = doc.Get("PackageScanFlagResult"),

                SupportedFrameworks = frameworks,
            };
        }

        private static PackageDependency CreateDependency(string s)
        {
            string[] parts = s.split_safe(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            return new PackageDependency
            {
                Id = parts.Length > 0 ? parts[0] : null,
                VersionSpec = parts.Length > 1 ? parts[1] : null,
                TargetFramework = parts.Length > 2 ? parts[2] : null,
            };
        }

        private static Query ParseQuery(SearchFilter searchFilter)
        {
            if (String.IsNullOrWhiteSpace(searchFilter.SearchTerm))
            {
                return new MatchAllDocsQuery();
            }

            var fields = new[] { "Id", "Title", "Tags", "Description", "Author" };
            //var analyzer = new StandardAnalyzer(LuceneCommon.LuceneVersion);
            var analyzer = new PerFieldAnalyzer();
            var queryParser = new MultiFieldQueryParser(LuceneCommon.LuceneVersion, fields, analyzer);

            // All terms in the multi-term query appear in at least one of the fields.
            var conjuctionQuery = new BooleanQuery();
            conjuctionQuery.Boost = 2.0f;

            // Some terms in the multi-term query appear in at least one of the fields.
            var disjunctionQuery = new BooleanQuery();
            disjunctionQuery.Boost = 0.1f;

            // Escape the entire term we use for exact searches.
            var escapedSearchTerm = Escape(searchFilter.SearchTerm).Replace("id\\:", string.Empty).Replace("author\\:", string.Empty).Replace("tag\\:", string.Empty);

            // Do not escape id when used against Id-Exact. The results will return incorrectly
            var idExactSearchTerm = searchFilter.SearchTerm.Replace("id:", string.Empty).Replace("author:", string.Empty).Replace("tag:", string.Empty);
            var exactIdQuery = new TermQuery(new Term("Id-Exact", idExactSearchTerm));
            exactIdQuery.Boost = 7.0f;
            var relatedIdQuery = new WildcardQuery(new Term("Id-Exact", idExactSearchTerm + ".*"));
            relatedIdQuery.Boost = 6.5f;
            var startsIdQuery = new WildcardQuery(new Term("Id-Exact", idExactSearchTerm + "*"));
            startsIdQuery.Boost = 6.0f;
            var wildCardIdQuery = new WildcardQuery(new Term("Id-Exact", "*" + idExactSearchTerm + "*"));
            wildCardIdQuery.Boost = 3.0f;

            var exactTitleQuery = new TermQuery(new Term("Title-Exact", escapedSearchTerm));
            exactTitleQuery.Boost = 6.5f;
            var startsTitleQuery = new WildcardQuery(new Term("Title-Exact", escapedSearchTerm + "*"));
            startsTitleQuery.Boost = 5.5f;
            var wildCardTitleQuery = new WildcardQuery(new Term("Title-Exact", "*" + escapedSearchTerm + "*"));
            wildCardTitleQuery.Boost = 2.5f;

            // Suffix wildcard search e.g. jquer*
            var wildCardQuery = new BooleanQuery();
            wildCardQuery.Boost = 0.5f;

            // GetSearchTerms() escapes the search terms, so do not escape again
            var terms = GetSearchTerms(searchFilter.SearchTerm).ToList();
            bool onlySearchById = searchFilter.ByIdOnly || searchFilter.ExactIdOnly || terms.AnySafe(t => t.StartsWith("id\\:"));
            bool onlySearchByExactId = searchFilter.ExactIdOnly;
            bool onlySearchByAuthor = terms.AnySafe(t => t.StartsWith("author\\:"));
            bool onlySearchByTag = terms.AnySafe(t => t.StartsWith("tag\\:"));
            bool searchLimiter = onlySearchById || onlySearchByAuthor || onlySearchByTag;

            foreach (var term in terms)
            {
                var localTerm = term.Replace("id\\:", string.Empty).Replace("author\\:", string.Empty).Replace("tag\\:", string.Empty);
                var termQuery = queryParser.Parse(localTerm);
                conjuctionQuery.Add(termQuery, Occur.MUST);
                disjunctionQuery.Add(termQuery, Occur.SHOULD);

                foreach (var field in fields)
                {
                    if (onlySearchById && field != "Id") continue;
                    if (onlySearchByAuthor && field != "Author") continue;
                    if (onlySearchByTag && field != "Tags") continue;

                    var wildCardTermQuery = new WildcardQuery(new Term(field, localTerm + "*"));
                    wildCardTermQuery.Boost = searchLimiter ? 7.0f : 0.7f;
                    wildCardQuery.Add(wildCardTermQuery, Occur.MUST);
                }
            }

            // Create an OR of all the queries that we have
            var combinedQuery = conjuctionQuery.Combine(new Query[] { exactIdQuery, relatedIdQuery, exactTitleQuery, startsIdQuery, startsTitleQuery, wildCardIdQuery, wildCardTitleQuery, conjuctionQuery, wildCardQuery });

            if (onlySearchByExactId)
            {
                combinedQuery = conjuctionQuery.Combine(new Query[] { exactIdQuery });
            }
            else if (onlySearchById)
            {
                combinedQuery = conjuctionQuery.Combine(new Query[] { exactIdQuery, relatedIdQuery, startsIdQuery, wildCardIdQuery, wildCardQuery });
            }
            else if (onlySearchByAuthor || onlySearchByTag)
            {
                combinedQuery = conjuctionQuery.Combine(new Query[] { wildCardQuery });
            }

            //if (searchFilter.SortProperty == SortProperty.Relevance)
            //{
            //    // If searching by relevance, boost scores by download count.
            //    var downloadCountBooster = new FieldScoreQuery("DownloadCount", FieldScoreQuery.Type.INT);
            //    return new CustomScoreQuery(combinedQuery, downloadCountBooster);
            //}

            return combinedQuery;
        }

        private static IEnumerable<string> GetSearchTerms(string searchTerm)
        {
            return searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Concat(new[] { searchTerm })
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .Select(Escape);
        }

        private static SortField GetSortField(SearchFilter searchFilter)
        {
            switch (searchFilter.SortProperty)
            {
                case SortProperty.Relevance:
                    return SortField.FIELD_SCORE;
                case SortProperty.DisplayName:
                    return new SortField("DisplayName", SortField.STRING, reverse: searchFilter.SortDirection == SortDirection.Descending);
                case SortProperty.DownloadCount:
                    return new SortField("DownloadCount", SortField.INT, reverse: true);
                case SortProperty.Recent:
                    return new SortField("PublishedDate", SortField.LONG, reverse: true);
                case SortProperty.Version:
                    return new SortField("Version", SortField.LONG, reverse: true);
            }

            return SortField.FIELD_SCORE;
        }

        private static string Escape(string term)
        {
            return QueryParser.Escape(term).to_lower_invariant();
        }

        private static int ParseKey(string value)
        {
            int key;
            return Int32.TryParse(value, out key) ? key : 0;
        }
    }
}