﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.Web.Mvc.Html;

namespace AdvancedExternalReviews.DraftContentAreaPreview
{
    /// <summary>
    /// Modified version of ContentAreaRenderer that displays
    /// draft content versions in ExternalReview context
    /// </summary>
    public class DraftContentAreaRenderer : ContentAreaRenderer
    {
        private readonly ContentAreaRenderer _defaultContentAreaRenderer;

        public DraftContentAreaRenderer(ContentAreaRenderer defaultContentAreaRenderer)
        {
            _defaultContentAreaRenderer = defaultContentAreaRenderer;
        }

        public override void Render(HtmlHelper htmlHelper, ContentArea contentArea)
        {
            if (!ExternalReview.IsInExternalReviewContext)
            {
                _defaultContentAreaRenderer.Render(htmlHelper, contentArea);
                return;
            }

            // ************************************************************************************
            // The code modifies original Render method using reflection to make sure
            // proper list of items is used.
            // ************************************************************************************

            if (contentArea == null || contentArea.IsEmpty)
                return;
            ViewContext viewContext = htmlHelper.ViewContext;
            TagBuilder tagBuilder = (TagBuilder) null;
            if (!this.CallProtectedMethod<bool>(nameof(IsInEditMode), htmlHelper) &&
                this.CallProtectedMethod<bool>(nameof(ShouldRenderWrappingElement), htmlHelper))
            {
                tagBuilder =
                    new TagBuilder(this.CallProtectedMethod<string>(nameof(GetContentAreaHtmlTag), htmlHelper,
                        contentArea));
                this.CallProtectedMethod(nameof(AddNonEmptyCssClass), tagBuilder,
                    viewContext.ViewData["cssclass"] as string);
                viewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            }

            var contentAreaItems = contentArea.Items.Select(x => x.CreateWritableClone()).ToList();
            foreach (var contentAreaItem in contentAreaItems)
            {
                if (contentAreaItem.RenderSettings == null)
                {
                    contentAreaItem.RenderSettings = new Dictionary<string, object>();
                }
            }

            this.CallProtectedMethod(nameof(RenderContentAreaItems), htmlHelper, contentAreaItems);
            if (tagBuilder == null)
                return;
            viewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.EndTag));

            // ************************************************************************************
        }

        private T CallProtectedMethod<T>(string methodName, params object[] methodParams)
        {
            var dynMethod = _defaultContentAreaRenderer.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (T) dynMethod.Invoke(_defaultContentAreaRenderer, methodParams);
        }

        private void CallProtectedMethod(string methodName, params object[] methodParams)
        {
            var dynMethod = _defaultContentAreaRenderer.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(_defaultContentAreaRenderer, methodParams);
        }
    }
}
