using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderingAssistSystem_StaffApp.Models
{
    public class PageCache
    {
        private static PageCache _instance;
        public static PageCache Instance => _instance ??= new PageCache();

        private readonly Dictionary<string, Page> _pages = new();

        private PageCache() { } // Private constructor to enforce singleton

        public Page GetOrCreatePage(string key, Func<Page> createPage)
        {
            if (!_pages.ContainsKey(key))
            {
                _pages[key] = createPage(); // Create and store the page if not already cached
            }
            return _pages[key];
        }

        public Page GetPage(string key)
        {
            return _pages.ContainsKey(key) ? _pages[key] : null;
        }

        public void ClearCache()
        {
            _pages.Clear();
        }
    }

}
