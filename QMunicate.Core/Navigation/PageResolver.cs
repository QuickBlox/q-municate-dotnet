using System;
using System.Collections.Generic;

namespace QMunicate.Core.Navigation
{
    public class PageResolver
    {
        readonly Dictionary<String, Type> pages;

        public PageResolver(Dictionary<string, Type> pagesWrapper)
        {
            if (pagesWrapper == null) throw new ArgumentNullException("pagesWrapper");
            this.pages = pagesWrapper;
        }

        public Type GetPageByKey(String pageKey)
        {
            Type page = null;
            if (this.pages.TryGetValue(pageKey, out page))
            {
                return page;
            }

            throw new KeyNotFoundException(String.Format("{0} not found.", pageKey));
        }
    }
}
