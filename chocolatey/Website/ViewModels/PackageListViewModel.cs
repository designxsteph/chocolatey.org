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
using System.Web.Mvc;
using StackExchange.Profiling;

namespace NuGetGallery
{
    public class PackageListViewModel
    {
        public PackageListViewModel(
            IEnumerable<Package> packages,
            string searchTerm,
            string sortOrder,
            int totalCount,
            int pageIndex,
            int pageSize,
            UrlHelper url,
            bool includePrerelease,
            bool moderatorQueue,
            int updatedCount,
            int submittedCount,
            int waitingCount,
            int respondedCount,
            string moderationStatus,
            bool preferenceGridView,
            bool preferenceModView)
        {
            // TODO: Implement actual sorting
            IEnumerable<ListPackageItemViewModel> items;
            using (MiniProfiler.Current.Step("Querying and mapping packages to list"))
            {
                items = packages
                    .ToList()
                    .Select(pv => new ListPackageItemViewModel(pv, needAuthors: false));
            }
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
            SortOrder = sortOrder;
            ModerationStatus = moderationStatus;
            SearchTerm = searchTerm;
            int pageCount = (TotalCount + PageSize - 1) / PageSize;

            var pager = new PreviousNextPagerViewModel<ListPackageItemViewModel>(
                items,
                PageIndex,
                pageCount,
                page => url.PackageList(page, sortOrder, searchTerm, includePrerelease, moderatorQueue, moderationStatus, preferenceGridView, preferenceModView)
                );

            var pagerSearch = new PreviousNextPagerViewModel<ListPackageItemViewModel>(
                items,
                PageIndex,
                pageCount,
                page => url.SearchResults(page, sortOrder, searchTerm, includePrerelease, moderatorQueue, moderationStatus, preferenceGridView, preferenceModView)
                );

            Items = pager.Items;
            FirstResultIndex = 1 + (PageIndex * PageSize);
            LastResultIndex = FirstResultIndex + Items.Count() - 1;
            Pager = pager;
            PagerSearch = pagerSearch;
            IncludePrerelease = includePrerelease ? "true" : null;
            ModeratorQueue = moderatorQueue ? "true" : null;
            ModerationUpdatedPackageCount = updatedCount;
            ModerationSubmittedPackageCount = submittedCount;
            ModerationWaitingPackageCount = waitingCount;
            ModerationRespondedPackageCount = respondedCount;
            PreferenceGridView = preferenceGridView ? "true" : null;
            PreferenceModView = preferenceModView ? "true" : null;
        }

        public int FirstResultIndex { get; set; }

        public IEnumerable<ListPackageItemViewModel> Items { get; private set; }

        public int LastResultIndex { get; set; }

        public IPreviousNextPager Pager { get; private set; }

        public IPreviousNextPager PagerSearch { get; private set; }

        public int TotalCount { get; private set; }

        public string SearchTerm { get; private set; }

        public string SortOrder { get; private set; }

        public string ModerationStatus { get; private set; }

        public int PageIndex { get; private set; }

        public int PageSize { get; private set; }

        public string IncludePrerelease { get; private set; }

        public string ModeratorQueue { get; private set; }

        public int ModerationUpdatedPackageCount { get; private set; }
        public int ModerationSubmittedPackageCount { get; private set; }
        public int ModerationWaitingPackageCount { get; private set; }
        public int ModerationRespondedPackageCount { get; private set; }

        public string PreferenceGridView { get; private set; }
        public string PreferenceModView { get; private set; }
    }
}
