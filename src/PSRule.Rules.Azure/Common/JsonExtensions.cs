﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace PSRule.Rules.Azure
{
    internal sealed class TemplateTokenAnnotation : IJsonLineInfo
    {
        private bool _HasLineInfo;

        public TemplateTokenAnnotation()
        {

        }

        public TemplateTokenAnnotation(int lineNumber, int linePosition)
        {
            SetLineInfo(lineNumber, linePosition);
        }

        public int LineNumber { get; private set; }

        public int LinePosition { get; private set; }

        public bool HasLineInfo()
        {
            return _HasLineInfo;
        }
        internal void SetLineInfo(int lineNumber, int linePosition)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
            _HasLineInfo = true;
        }
    }

    internal static class JsonExtensions
    {
        private const string FIELD_DEPENDSON = "dependsOn";

        internal static IJsonLineInfo TryLineInfo(this JToken token)
        {
            if (token == null)
                return null;

            var annotation = token.Annotation<TemplateTokenAnnotation>();
            if (annotation != null)
                return annotation;

            return token;
        }

        internal static JToken CloneTemplateJToken(this JToken token)
        {
            if (token == null)
                return null;

            var annotation = token.Annotation<TemplateTokenAnnotation>();
            if (annotation == null && token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
                annotation = new TemplateTokenAnnotation(lineInfo.LineNumber, lineInfo.LinePosition);

            var clone = token.DeepClone();
            if (annotation != null)
                clone.AddAnnotation(annotation);

            return clone;
        }

        internal static void CopyTemplateAnnotationFrom(this JToken token, JToken source)
        {
            if (token == null || source == null)
                return;

            var annotation = source.Annotation<TemplateTokenAnnotation>();
            if (annotation != null)
                token.AddAnnotation(annotation);
        }

        internal static void UseProperty<TValue>(this JObject o, string propertyName, out TValue value) where TValue : JToken, new()
        {
            if (!o.TryGetValue(propertyName, System.StringComparison.OrdinalIgnoreCase, out JToken v))
            {
                value = new TValue();
                o.Add(propertyName, value);
                return;
            }
            value = (TValue)v;
        }

        internal static bool TryGetDependencies(this JObject resource, out string[] dependencies)
        {
            dependencies = null;
            if (!(resource.ContainsKey(FIELD_DEPENDSON) && resource[FIELD_DEPENDSON] is JArray d && d.Count > 0))
                return false;

            dependencies = d.Values<string>().ToArray();
            return true;
        }
    }
}
