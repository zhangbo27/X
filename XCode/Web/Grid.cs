﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NewLife.Reflection;
using NewLife.Web;

namespace XCode.Web
{
    /// <summary>用于显示数据的网格</summary>
    public class Grid
    {
        #region 属性
        private IEntityList _DataSource;
        /// <summary>数据源。如果为空，将会自动使用<see cref="Where"/>查询</summary>
        public IEntityList DataSource
        {
            get
            {
                if (_DataSource == null) Select();
                return _DataSource;
            }
            set { _DataSource = value; }
        }
        #endregion

        #region 构造
        /// <summary>无参数构造</summary>
        public Grid() { Init(); }

        /// <summary>使用实体工厂构造</summary>
        /// <param name="factory"></param>
        public Grid(IEntityOperate factory) { Factory = factory; Init(); }

        /// <summary>获取参数</summary>
        public void Init()
        {
            PageIndex = WebHelper.RequestInt("PageIndex");
            PageSize = WebHelper.RequestInt("PageSize");
            Sort = WebHelper.Request["Sort"];
            SortDesc = WebHelper.Request["Desc"].ToInt() != 0;
        }
        #endregion

        #region 方法
        /// <summary>生成头部。</summary>
        /// <param name="includeHeader">是否包含thead和tr</param>
        /// <param name="names">冒号分割，前面是排序字段名，后面是标题名，没有冒号表示不排序</param>
        /// <returns></returns>
        public virtual String RenderHeader(Boolean includeHeader, params String[] names)
        {
            var sb = new StringBuilder();
            if (includeHeader) sb.Append("<thead><tr>");
            foreach (var item in names)
            {
                var ss = item.Split(":");
                if (ss.Length == 2)
                    sb.AppendFormat("<th><a href=\"{0}\">{1}</a></th> ", GetSortUrl(ss[0]), ss[1]);
                else
                    sb.AppendFormat("<th>{0}</th>", item);
            }
            if (includeHeader) sb.Append("</tr></thead>");
            return sb.ToString();
        }

        /// <summary>获取基础Url，用于附加参数</summary>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        protected virtual String GetBaseUrl(Boolean where, Boolean order, Boolean page)
        {
            var sb = new StringBuilder();
            sb.Append("?");
            var nvs = WebHelper.Request.QueryString;
            foreach (var item in nvs.AllKeys)
            {
                if (item.IsNullOrWhiteSpace()) continue;
                // 过滤
                if (item.EqualIgnoreCase("Sort", "Desc"))
                {
                    if (!order) continue;
                }
                else if (item.EqualIgnoreCase("PageIndex", "PageSize"))
                {
                    if (!page) continue;
                }
                else
                {
                    if (!where) continue;
                }

                if (sb.Length > 1) sb.Append("&");
                sb.AppendFormat("{0}={1}", item, nvs[item]);
            }
            return sb.ToString();
        }
        #endregion

        #region 排序
        private String _Sort;
        /// <summary>排序字段</summary>
        public String Sort { get { return _Sort; } set { _Sort = value; } }

        private Boolean _SortDesc;
        /// <summary>是否降序</summary>
        public Boolean SortDesc { get { return _SortDesc; } set { _SortDesc = value; } }

        /// <summary>获取排序的Url</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetSortUrl(String name)
        {
            // 首次访问该排序项，默认升序，重复访问取反
            var desc = false;
            if (Sort.EqualIgnoreCase(name)) desc = !SortDesc;

            var url = GetBaseUrl(true, false, true);
            if (url.Length > 1) url += "&";
            if (desc)
                return url + "Sort={0}&Desc=1".F(name);
            else
                return url + "Sort={0}".F(name);
        }

        /// <summary>排序字句</summary>
        public virtual String OrderBy
        {
            get
            {
                var sort = Sort;
                if (sort.IsNullOrWhiteSpace()) return null;
                if (SortDesc) sort += " Desc";
                return sort;
            }
        }
        #endregion

        #region 分页
        private Int32 _DefaultPageSize = 20;
        /// <summary>默认页大小</summary>
        public Int32 DefaultPageSize { get { return _DefaultPageSize; } set { if (value <= 0)value = 20; _DefaultPageSize = value; } }

        private Int32 _PageSize = 0;
        /// <summary>页大小。设置有值时采用已有值，否则采用默认也大小</summary>
        public Int32 PageSize { get { return _PageSize > 0 ? _PageSize : DefaultPageSize; } set { _PageSize = value; } }

        private Int32 _PageIndex = 1;
        /// <summary>页索引</summary>
        public Int32 PageIndex { get { return _PageIndex; } set { if (value <= 0)value = 1; _PageIndex = value; } }

        private Int32 _TotalCount = -1;
        /// <summary>总记录数</summary>
        public Int32 TotalCount
        {
            get
            {
                if (_TotalCount < 0) _TotalCount = Factory.FindCount(Where, null, null, 0, 0);
                return _TotalCount;
            }
            set { _TotalCount = value; }
        }

        /// <summary>页数</summary>
        public Int32 PageCount
        {
            get
            {
                var count = TotalCount / PageSize;
                if ((TotalCount % PageSize) != 0) count++;
                return count;
            }
        }

        private String _PageTemplate = "共<span>{TotalCount}</span>条&nbsp;每页<span>{PageSize}</span>条&nbsp;当前第<span>{PageIndex}</span>页/共<span>{PageCount}</span>页&nbsp;{首页}{上一页}{下一页}{尾页}转到第<input name=\"PageIndex\" type=\"text\" value=\"{PageIndex}\" style=\"width:40px;text-align:right;\" />页<input type=\"button\" name=\"PageJump\" value=\"GO\" />";
        /// <summary>分页模版</summary>
        public String PageTemplate { get { return _PageTemplate; } set { _PageTemplate = value; } }

        private String _PageUrlTemplate = "<a href=\"{链接}\">{名称}</a>&nbsp;";
        /// <summary>分页链接模版</summary>
        public String PageUrlTemplate { get { return _PageUrlTemplate; } set { _PageUrlTemplate = value; } }

        public virtual String RenderPage()
        {
            var txt = PageTemplate;
            txt = txt.Replace("{TotalCount}", TotalCount + "");
            txt = txt.Replace("{PageIndex}", PageIndex + "");
            txt = txt.Replace("{PageSize}", PageSize + "");
            txt = txt.Replace("{PageCount}", PageCount + "");

            if (PageIndex == 1)
            {
                txt = txt.Replace("{首页}", "首页&nbsp;");
                txt = txt.Replace("{上一页}", "上一页&nbsp;");
            }
            if (PageIndex >= PageCount)
            {
                txt = txt.Replace("{尾页}", "尾页&nbsp;");
                txt = txt.Replace("{下一页}", "下一页&nbsp;");
            }

            if (PageIndex > 1)
            {
                txt = txt.Replace("{首页}", GetPageUrl("首页", 1));
                txt = txt.Replace("{上一页}", GetPageUrl("上一页", PageIndex - 1));
            }
            if (PageIndex < PageCount)
            {
                txt = txt.Replace("{尾页}", GetPageUrl("尾页", PageCount));
                txt = txt.Replace("{下一页}", GetPageUrl("下一页", PageIndex + 1));
            }

            return txt;
        }

        String GetPageUrl(String name, Int32 index)
        {
            var url = GetBaseUrl(true, true, false);
            if (PageIndex != index && index > 1)
            {
                if (url.Length > 1) url += "&";
                url += "PageIndex=" + index;
            }
            if (PageSize != DefaultPageSize) url += "&PageSize=" + PageSize;

            var txt = PageUrlTemplate;
            txt = txt.Replace("{链接}", url);
            txt = txt.Replace("{名称}", name);

            return txt;
        }
        #endregion

        #region 查询
        private IEntityOperate _Factory;
        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }

        private String _Where;
        /// <summary>查询条件。由外部根据<see cref="Params"/>构造后赋值，<see cref="Select"/>将会调用该条件查询数据</summary>
        public String Where { get { return _Where; } set { _Where = value; } }

        /// <summary>普通查询参数，不包含分页和排序</summary>
        public virtual IDictionary<String, String> Params
        {
            get
            {
                var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                var sb = new StringBuilder();
                var nvs = WebHelper.Request.QueryString;
                foreach (var item in nvs.AllKeys)
                {
                    if (item.IsNullOrWhiteSpace()) continue;
                    // 过滤
                    if (item.EqualIgnoreCase("Sort", "Desc")) continue;
                    if (item.EqualIgnoreCase("PageIndex", "PageSize")) continue;

                    dic[item] = nvs[item];
                    //var fi = Factory.Table.FindByName(item);
                }

                return dic;
            }
        }

        /// <summary>执行数据查询</summary>
        public virtual void Select()
        {
            DataSource = Factory.FindAll(Where, OrderBy, null, (PageIndex - 1) * PageSize, PageSize);
            TotalCount = Factory.FindCount(Where, null, null, 0, 0);
        }
        #endregion
    }
}