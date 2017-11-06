using GuiLabs.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer
{
    public static class ActionManagerExtensions
    {
        // null-safety
        private static void DoRec(this ActionManager am, IAction action)
        {
            if (am == null)
                action.Execute();
            else
                am.RecordAction(action);
        }

        // generic
        public static void RecordAction(this ActionManager am, Action executeFn, Action undoFn)
        {
            am.DoRec(new BasicUndoAction(executeFn, undoFn));
        }


        // collection based
        public static void RecordAdd<T>(this ActionManager am, ICollection<T> collection, T item)
        {
            am.DoRec(new BasicUndoAction(() => collection.Add(item), () => collection.Remove(item)));
        }

        public static void RecordInsert<T>(this ActionManager am, IList<T> collection, int index, T item)
        {
            am.DoRec(new BasicUndoAction(() => collection.Insert(index, item), () => collection.Remove(item)));
        }

        /// <summary>Replaces the first occurence of <paramref name="oldItem"/> in a collection.</summary>
        public static void RecordReplace<T>(this ActionManager am, IList<T> collection, T oldItem, T newItem)
        {
            int index = collection.IndexOf(oldItem);
            if (index == -1)
                throw new ArgumentException("oldItem has to be contained in collection");

            RecordReplace(am, collection, index, newItem);
        }
        /// <summary>Replaces the item at the given index with another item.</summary>
        public static void RecordReplace<T>(this ActionManager am, IList<T> collection, int index, T newItem)
        {
            am.DoRec(new StatefulUndoAction<T>(() =>
            {
                T oldItem = collection[index];
                collection[index] = newItem;
                return oldItem;
            }, oldItem => collection[index] = oldItem));
        }

        public static void RecordRemove<T>(this ActionManager am, IList<T> collection, T item)
        {
            am.DoRec(new StatefulUndoAction<int>(() =>
            {
                int index = collection.IndexOf(item);
                collection.RemoveAt(index);
                return index;
            },
            index => collection.Insert(index, item)));
        }


        // property based
        public static void RecordSetProperty<TObject, TValue>(this ActionManager am, TObject obj, Expression<Func<TObject, TValue>> propertyExpr, TValue value)
        {
            PropertyInfo prop = (PropertyInfo)((MemberExpression)propertyExpr.Body).Member; // ^= propertyExpr.ExtractProperty()

            TValue oldValue = (TValue)prop.GetValue(obj);
            if (!EqualityComparer<TValue>.Default.Equals(value, oldValue))
                am.DoRec(new SetPropertyAction(obj, prop.Name, value));

            //am.RecordAction(new StatefulUndoAction<TValue>(() =>
            //{
            //    TValue oldValue = (TValue)prop.GetValue(obj);
            //    prop.SetValue(obj, value);
            //    return oldValue;
            //},
            //oldValue => prop.SetValue(obj, oldValue)));
        }
    }

    public class BasicUndoAction : AbstractAction
    {
        private Action _executeFn;
        private Action _undoFn;

        public BasicUndoAction(Action executeFn, Action undoFn)
        {
            _executeFn = executeFn;
            _undoFn = undoFn;
        }

        protected override void ExecuteCore()
        {
            _executeFn();
        }

        protected override void UnExecuteCore()
        {
            _undoFn();
        }
    }

    public class StatefulUndoAction<T> : AbstractAction
    {
        private Func<T> _executeFn;
        private Action<T> _undoFn;

        private T state = default(T);

        public StatefulUndoAction(Func<T> executeFn, Action<T> undoFn)
        {
            _executeFn = executeFn;
            _undoFn = undoFn;
        }

        protected override void ExecuteCore()
        {
            state = _executeFn();
        }

        protected override void UnExecuteCore()
        {
            _undoFn(state);
        }
    }
}
