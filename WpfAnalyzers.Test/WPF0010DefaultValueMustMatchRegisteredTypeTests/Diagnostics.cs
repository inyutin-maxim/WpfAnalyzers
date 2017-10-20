﻿namespace WpfAnalyzers.Test.WPF0010DefaultValueMustMatchRegisteredTypeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(T),
            typeof(FooControl<T>),
            new PropertyMetadata(↓1));

        public T Bar
        {
            get { return (T)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";

            var expectedMessage = ExpectedMessage.Create("Default value for 'RoslynSandbox.FooControl<T>.BarProperty' must be of type T");
            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(expectedMessage, testCode);
        }

        [TestCase("int", "new PropertyMetadata(↓default(double))")]
        [TestCase("int", "new PropertyMetadata(↓0.0)")]
        [TestCase("double", "new PropertyMetadata(↓1)")]
        [TestCase("double?", "new PropertyMetadata(↓1)")]
        [TestCase("System.Collections.ObjectModel.ObservableCollection<int>", "new PropertyMetadata(↓1)")]
        [TestCase("System.Collections.ObjectModel.ObservableCollection<int>", "new PropertyMetadata(↓new ObservableCollection<double>())")]
        public void DependencyProperty(string typeName, string metadata)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(↓1));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }
    }
}";
            testCode = testCode.AssertReplace("double", typeName)
                               .AssertReplace("new PropertyMetadata(↓1)", metadata);
            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void DependencyPropertyGeneric()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(T),
            typeof(FooControl<T>),
            new PropertyMetadata(↓1));

        public T Bar
        {
            get { return (T)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";
            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void ReadOnlyDependencyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(↓""1.0""));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }
    }
}";
            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void AttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(↓1.0));

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarProperty, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void ReadOnlyAttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(↓default(double)));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarPropertyKey, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            AnalyzerAssert.Diagnostics<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }
    }
}