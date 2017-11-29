﻿namespace WpfAnalyzers.Test.WPF0072ValueConversionMustUseCorrectTypesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        [Test]
        public void DirectCastWrongSourceType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(↓typeof(string), typeof(int))]
    public class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(typeof(ICollection), typeof(int))]
    public class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";
            AnalyzerAssert.CodeFix<WPF0072ValueConversionMustUseCorrectTypes, ValueConversionAttributeArgumentFix>(testCode, fixedCode);
        }

        [Test]
        public void DirectCastWrongSourceTypeFullyQualified()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;

    [System.Windows.Data.ValueConversion(↓typeof(string), typeof(int))]
    public class CountConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object System.Windows.Data.IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;

    [System.Windows.Data.ValueConversion(typeof(ICollection), typeof(int))]
    public class CountConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object System.Windows.Data.IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";
            AnalyzerAssert.CodeFix<WPF0072ValueConversionMustUseCorrectTypes, ValueConversionAttributeArgumentFix>(testCode, fixedCode);
        }

        [Test]
        public void DirectCastWrongSourceTypeFullyQualifiedIncludeAttribute()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;

    [System.Windows.Data.ValueConversionAttribute(↓typeof(string), typeof(int))]
    public class CountConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object System.Windows.Data.IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;

    [System.Windows.Data.ValueConversionAttribute(typeof(ICollection), typeof(int))]
    public class CountConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object System.Windows.Data.IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";
            AnalyzerAssert.CodeFix<WPF0072ValueConversionMustUseCorrectTypes, ValueConversionAttributeArgumentFix>(testCode, fixedCode);
        }

        [Test]
        public void DirectCastWrongTargetType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(typeof(ICollection), ↓typeof(string))]
    public class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(typeof(ICollection), typeof(int))]
    public class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ICollection)value).Count;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}";
            AnalyzerAssert.CodeFix<WPF0072ValueConversionMustUseCorrectTypes, ValueConversionAttributeArgumentFix>(testCode, fixedCode);
        }
    }
}