using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XF.IncrementalListView
{
    public class IncrementalListView : ListView
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public int PageIndex { get; private set; } = 0;

        public int PageSize { get; set; } = 10;

        public bool IsLoadingIncrementally
        {
            get => (bool)GetValue(IsLoadingIncrementallyProperty);
            set => SetValue(IsLoadingIncrementallyProperty, value);
        }

        public static readonly BindableProperty IsLoadingIncrementallyProperty =
            BindableProperty.Create("IsLoadingIncrementally", typeof(bool), typeof(IncrementalListView));

        public IIncrementalSource IncrementalSource
        {
            get => (IIncrementalSource)GetValue(IncrementalSourceProperty);
            set => SetValue(IncrementalSourceProperty, value);
        }

        public static readonly BindableProperty IncrementalSourceProperty =
            BindableProperty.Create("IncrementalSource", typeof(IIncrementalSource), typeof(IncrementalListView));

        private bool HasMore => (itemsSource == null && PageIndex == 0) || itemsSource.Count == PageIndex * PageSize;

        private readonly int thresholdPercentage = 80;

        private int lastPosition;
        private IList itemsSource;

        public IncrementalListView()
            : this(ListViewCachingStrategy.RecycleElementAndDataTemplate)
        {
        }

        public IncrementalListView(ListViewCachingStrategy cachingStrategy)
            : base(cachingStrategy)
        {
            ItemAppearing += OnItemAppearing;
        }

        protected override async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == ItemsSourceProperty.PropertyName)
            {
                if (ItemsSource != null && !(ItemsSource is IList))
                    throw new Exception($"{nameof(IncrementalListView)} requires that {nameof(ItemsSource)} be of type IList");

                PageIndex = 0;
                itemsSource = ItemsSource as IList;

                if (itemsSource != null && IncrementalSource != null)
                    await LoadMoreItemsAsync();
            }

            else if (propertyName == IncrementalSourceProperty.PropertyName)
            {
                if (itemsSource != null && IncrementalSource != null)
                    await LoadMoreItemsAsync();
            }
        }

        private async void OnItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            int position = itemsSource?.IndexOf(e.Item) ?? 0;

            if (itemsSource != null)
            {
                int scrolledPercentage = (int)Math.Ceiling(((double)position * 100) / itemsSource.Count);

                if (scrolledPercentage >= thresholdPercentage || position > lastPosition || (position == itemsSource.Count - 1))
                {
                    lastPosition = position;
                    if (!IsLoadingIncrementally && !IsRefreshing && HasMore)
                    {
                        await LoadMoreItemsAsync();
                    }
                }
            }
        }

        private async Task LoadMoreItemsAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                IsLoadingIncrementally = true;

                if (IncrementalSource != null && itemsSource != null)
                {
                    await IncrementalSource.GetPagedItemsAsync(PageIndex, PageSize);
                    ++PageIndex;
                }
                IsLoadingIncrementally = false;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}

