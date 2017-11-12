﻿using GlowSequencer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GlowSequencer
{
    public static class Mastermind
    {
        private static MusicSegmentsWindow winMusicSegments = null;
        private static AboutWindow winAbout = null;
        private static TransferWindow winTransfer = null;

        private static void OpenWindow<T>(ref T win, Action closeHandler) where T : Window, new()
        {
            OpenWindow(ref win, () => new T(), closeHandler);
        }

        private static void OpenWindow<T>(ref T win, Func<T> windowConstructor, Action closeHandler) where T : Window
        {
            if (win == null)
            {
                win = windowConstructor();
                win.Owner = Application.Current.MainWindow;
                win.Show();

                win.Closed += (sender, e) => closeHandler();
            }
            else
            {
                win.Activate();
            }
        }


        public static void OpenMusicSegmentsWindow()
        {
            OpenWindow(ref winMusicSegments, () => winMusicSegments = null);
        }

        public static void OpenAboutWindow()
        {
            OpenWindow(ref winAbout, () => winAbout = null);
        }

        public static void OpenTransferWindow(ViewModel.MainViewModel main)
        {
            OpenWindow(ref winTransfer, () => new TransferWindow(main), () => winTransfer = null);
        }


        public static PromptResult<string> ShowPromptString(Window owner, string title, string initialInput = null, Func<string, bool> validPredicate = null)
        {
            return ShowPrompt(owner, title, initialInput, (str => str), validPredicate);
        }

        public static PromptResult<TimeSpan> ShowPromptTimeSpan(Window owner, string title, TimeSpan initialValue)
        {
            var converter = new Util.TimeSpanToStringConverter();

            string initialInput = (string)converter.Convert(initialValue, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
            var result = Mastermind.ShowPrompt(owner, title, initialInput,
                            input => (TimeSpan?)converter.ConvertBack(input, typeof(TimeSpan), null, System.Globalization.CultureInfo.InvariantCulture),
                            value => value != null);

            return result.MapValue(v => v.Value);
        }

        // valueConverter: should return a value not passed by validPredicate on failure, or alternatively throw an exception
        public static PromptResult<T> ShowPrompt<T>(Window owner, string title, string initialInput, Func<string, T> valueConverter, Func<T, bool> validPredicate = null)
        {
            T inputResult;
            bool potentialSuccess = false;
            do
            {
                var prompt = new PromptWindow(title);
                prompt.Owner = owner;
                prompt.PromptText = initialInput;

                if (prompt.ShowDialog() != true)
                    return ResultFailed<T>();

                try
                {
                    inputResult = valueConverter(prompt.PromptText);
                    potentialSuccess = true;
                }
                catch (Exception)
                {
                    inputResult = default(T);
                    potentialSuccess = false;
                }
            } while (!potentialSuccess || (validPredicate != null && !validPredicate(inputResult)));

            return ResultFromValue(inputResult);
        }


        private static PromptResult<T> ResultFailed<T>() => new PromptResult<T> { Success = false };
        private static PromptResult<T> ResultFromValue<T>(T value) => new PromptResult<T> { Success = true, Value = value };

        public class PromptResult<T>
        {
            public bool Success { get; internal set; }
            public T Value { get; internal set; }

            public PromptResult<TNew> MapValue<TNew>(Func<T, TNew> mapper)
            {
                if (Success) return ResultFromValue(mapper(Value));
                else return ResultFailed<TNew>();
            }
        }
    }
}
