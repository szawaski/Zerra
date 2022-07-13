// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static class CheckBoxListExtension
    {
        public static HtmlString CheckBoxListFor<TModel, TValue>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, IEnumerable<SelectListItem> items, object htmlAttributes = null)
        {
            var modelExpressionProvider = (ModelExpressionProvider)htmlHelper.ViewContext.HttpContext.RequestServices.GetService(typeof(ModelExpressionProvider));

            var listName = ExpressionHelper.GetExpressionText(expression);
            var metaData = modelExpressionProvider.CreateModelExpression(htmlHelper.ViewData, expression);

            items = GetCheckboxListWithDefaultValues(metaData.Model, items);
            return htmlHelper.CheckBoxList(listName, items, htmlAttributes);
        }

        public static HtmlString CheckBoxList(this IHtmlHelper htmlHelper, string listName, IEnumerable<SelectListItem> items, object htmlAttributes = null)
        {
            var container = new TagBuilder("div");
            foreach (var item in items)
            {
                var label = new TagBuilder("label");
                label.MergeAttribute("class", "checkbox"); // default class
                label.MergeAttributes(new RouteValueDictionary(htmlAttributes), true);

                var cb = new TagBuilder("input");
                cb.MergeAttribute("type", "checkbox");
                cb.MergeAttribute("name", listName);
                cb.MergeAttribute("value", item.Value ?? item.Text);
                if (item.Selected)
                    cb.MergeAttribute("checked", "checked");

                cb.TagRenderMode = TagRenderMode.SelfClosing;
                var cbWriter = new System.IO.StringWriter();
                cb.WriteTo(cbWriter, HtmlEncoder.Default);
                label.InnerHtml.SetHtmlContent(cbWriter.ToString() + item.Text);

                var labelWriter = new System.IO.StringWriter();
                label.WriteTo(labelWriter, HtmlEncoder.Default);
                container.InnerHtml.AppendHtml(labelWriter.ToString());
            }

            var writer = new System.IO.StringWriter();
            container.WriteTo(writer, HtmlEncoder.Default);
            return new HtmlString(writer.ToString());
        }

        private static IEnumerable<SelectListItem> GetCheckboxListWithDefaultValues(object defaultValues, IEnumerable<SelectListItem> selectList)
        {
            if (!(defaultValues is IEnumerable defaultValuesList))
                return selectList;

            var values = from object value in defaultValuesList
                                         select Convert.ToString(value, CultureInfo.CurrentCulture);

            var selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
            var newSelectList = new List<SelectListItem>();

            foreach (var item in selectList)
            {
                item.Selected = (item.Value != null) ? selectedValues.Contains(item.Value) : selectedValues.Contains(item.Text);
                newSelectList.Add(item);
            }

            return newSelectList;
        }
    }
}