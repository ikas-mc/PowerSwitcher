﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Petrroll.Helpers
{

    public static class ObservableLINQShim
    {
        public static ObservableCollectionWhereShim<C, T> WhereObservable<C, T>(this C collection, Func<T, bool> predicate) where C : INotifyCollectionChanged, ICollection<T>
        {
            return new ObservableCollectionWhereShim<C, T>(collection, predicate);
        }
    }

    public class ObservableCollectionWhereShim<C, T> : INotifyCollectionChanged, ICollection<T> where C : ICollection<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Func<T, bool> Predicate { get; private set; }
        public C BaseCollection { get; private set; }

        public ObservableCollectionWhereShim(C baseCollection, Func<T, bool> predicate)
        {
            Predicate = predicate;
            BaseCollection = baseCollection;

            baseCollection.CollectionChanged += Obs_CollectionChanged;
        }

        private void Obs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    handleAdd(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    handleRemove(e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    handleReplace(e);
                    break;
                case NotifyCollectionChangedAction.Move:
                    handleMove(e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    handleReset();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void handleMove(NotifyCollectionChangedEventArgs e)
        {
            var newItems = getNewItems(e);
            var delItems = getOldItems(e);

            if (newItems.Count < 1 && delItems.Count < 1) { return; }

            var newItemsIndex = getNewItemsIndex(e);
            var oldItemsIndex = getOldItemsIndex(e);

            if (newItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, oldItemsIndex)); }
            else if (delItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, newItemsIndex)); }
            else { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItems, newItemsIndex, oldItemsIndex)); }

        }

        private void handleReplace(NotifyCollectionChangedEventArgs e)
        {
            var newItems = getNewItems(e);
            var delItems = getOldItems(e);

            if (newItems.Count < 1 && delItems.Count < 1) { return; }

            var changeItemIndex = getNewItemsIndex(e);
            if (newItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, changeItemIndex)); }
            else if (delItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, changeItemIndex)); }
            else { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, delItems, changeItemIndex)); }
        }

        private void handleAdd(NotifyCollectionChangedEventArgs e)
        {
            IList newItems = getNewItems(e);
            if (newItems.Count < 1) { return; }

            var newItemsIndex = getNewItemsIndex(e);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, newItemsIndex));
        }

        private void handleRemove(NotifyCollectionChangedEventArgs e)
        {
            var delItems = getOldItems(e);
            if (delItems.Count < 1) { return; }

            var delItemsIndex = getOldItemsIndex(e);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, delItemsIndex));
        }

        private void handleReset()
        {
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }


        private IList getOldItems(NotifyCollectionChangedEventArgs e)
        {
            return e.OldItems.Cast<T>().Where(Predicate).ToList();
        }

        private int getOldItemsIndex(NotifyCollectionChangedEventArgs e)
        {
            return (e.OldStartingIndex < 0) ? e.OldStartingIndex : BaseCollection.Take(e.OldStartingIndex).Count(Predicate);
        }

        private IList getNewItems(NotifyCollectionChangedEventArgs e)
        {
            return e.NewItems.Cast<T>().Where(Predicate).ToList();
        }

        private int getNewItemsIndex(NotifyCollectionChangedEventArgs e)
        {
            return (e.NewStartingIndex < 0) ? e.NewStartingIndex : BaseCollection.Take(e.NewStartingIndex).Count(Predicate);
        }

        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        public int Count => BaseCollection.Count;
        public bool IsReadOnly => ((ICollection<T>)BaseCollection).IsReadOnly;

        public void Add(T item) => BaseCollection.Add(item);

        public void Clear() => BaseCollection.Clear();

        public bool Contains(T item) => BaseCollection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => BaseCollection.CopyTo(array, arrayIndex);

        public bool Remove(T item) => BaseCollection.Remove(item);

        public IEnumerator<T> GetEnumerator() => BaseCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => BaseCollection.GetEnumerator();
    }
}
