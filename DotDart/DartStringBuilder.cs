using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotDart
{
    public class DartStringBuilder
    {
        private StringBuilder sb = new StringBuilder();
        private int indent = 0;
        private bool newLine = true;

        private void IndentCheck()
        {
            if (!newLine) return;
            sb.Append(' ', 3 * indent);
            newLine = false;
        }

        public DartStringBuilder Indent()
        {
            if (!newLine)
            {
                sb.AppendLine();
                newLine = true;
            }
            indent++;
            return this;
        }

        public DartStringBuilder Dedent()
        {
            if (!newLine)
            {
                sb.AppendLine();
                newLine = true;
            }
            indent--;
            return this;
        }

        public DartStringBuilder AppendLine()
        {
            IndentCheck();
            sb.AppendLine();
            newLine = true;
            return this;
        }

        public DartStringBuilder AppendLine(string value)
        {
            IndentCheck();
            sb.AppendLine(value);
            newLine = true;
            return this;
        }

        public DartStringBuilder Append(string value)
        {
            IndentCheck();
            sb.Append(value);
            return this;
        }

        public DartStringBuilder AppendHeader(string value)
        {
            IndentCheck();
            sb.Append(value).Append(": ");
            return this;
        }

        public DartStringBuilder AppendJoin(string separator, params object[] values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder AppendJoin<T>(string separator, IEnumerable<T> values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder AppendJoin(string separator, params string[] values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder AppendJoin(char separator, params object[] values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder AppendJoin<T>(char separator, IEnumerable<T> values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder AppendJoin(char separator, params string[] values)
        {
            IndentCheck();
            sb.AppendJoin(separator, values);
            return this;
        }

        public DartStringBuilder Serialize(object value)
        {
            /*
            sb.AppendHeader(nameof(DirectPropertySet))
                .Indent()
                .Append(fileOffset).AppendLine()
                .AppendHeader(nameof(receiver)).Append(receiver).AppendLine()
                .AppendHeader(nameof(target)).Append(target).AppendLine()
                .Append(arguments).AppendLine()
                .Dedent();*/

            if (value == null)
            {
                Append("NULL");
                return this;
            }

            var type = value.GetType();

            // Just emit value if its a value type
            if (type.IsValueType)
            {
                Append(value.ToString());
                return this;
            }

            // Use a custom Serializer if one is available
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                if (method.Name == "Serialize")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(DartStringBuilder))
                    {
                        method.Invoke(value, new object[] {this});
                        return this;
                    }
                }
            }

            // When making lists, we only need to append_lines up until the last item
            bool first = true;
            void AppendLineIfNeeded()
            {
                if (!first)
                {
                    AppendLine();
                }
                else
                {
                    first = false;
                }
            }

            // List out members if List, Array, etc...

            if (value is IEnumerable enumerable)
            {
                Indent();
                var i = enumerable.GetEnumerator();
                while (i.MoveNext())
                {
                    AppendLineIfNeeded();
                    Serialize(i.Current);
                    if (newLine) first = true;
                }

                Dedent();
                return this;
            }

            var fields = type.GetFields()
                .Where(field =>
                    field.Name != "Tag" &&
                    field.Name != "tag" &&
                    !string.IsNullOrWhiteSpace(field.Name))
                .ToArray();
            if (fields.Length == 0)
            {
                Append(type.Name);
                return this;
            }

            AppendHeader(type.Name);

            Indent();
            foreach (var field in fields)
            {
                AppendLineIfNeeded();
                AppendHeader(field.Name);
                Serialize(field.GetValue(value));
                if (newLine) first = true;
            }
            Dedent();

            return this;
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}