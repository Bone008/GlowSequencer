using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GlowSequencer.Util
{
    public static class TextFieldBehavior
    {
        public static readonly DependencyProperty BindOnEnterProperty =
            DependencyProperty.RegisterAttached("BindOnEnter", typeof(bool), typeof(TextFieldBehavior), new UIPropertyMetadata(false, OnBindOnEnterChanged));

        public static readonly DependencyProperty SelectOnFocusProperty =
            DependencyProperty.RegisterAttached("SelectOnFocus", typeof(bool), typeof(TextFieldBehavior), new UIPropertyMetadata(false, OnSelectOnFocusChanged));


        public static bool GetBindOnEnter(TextBox textBox)
        {
            return (bool)textBox.GetValue(BindOnEnterProperty);
        }
        public static void SetBindOnEnter(TextBox textBox, bool value)
        {
            textBox.SetValue(BindOnEnterProperty, value);
        }

        public static bool GetSelectOnFocus(TextBox textBox)
        {
            return (bool)textBox.GetValue(SelectOnFocusProperty);
        }
        public static void SetSelectOnFocus(TextBox textBox, bool value)
        {
            textBox.SetValue(SelectOnFocusProperty, value);
        }

        private static void OnBindOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = d as TextBox;
            if (tb == null)
                return;

            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
                tb.PreviewKeyUp += TextBox_PreviewKeyUp;
            else
                tb.PreviewKeyUp -= TextBox_PreviewKeyUp;
        }

        private static void OnSelectOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = d as TextBox;
            if (tb == null)
                return;

            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
                tb.GotFocus += TextBox_GotFocus;
            else
                tb.GotFocus -= TextBox_GotFocus;
        }

        private static void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                BindingExpression be = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                if(be != null)
                    be.UpdateSource();
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // checking for the Tab key specifically is kind of evil, but it works pretty well
            if(Keyboard.IsKeyDown(Key.Tab))
                ((TextBox)sender).SelectAll();
        }
    }
}
