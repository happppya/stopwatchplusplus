using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.IO;

using timerthing.ItemModels;
using timerthing.Services;

namespace timerthing
{

    internal class SharedInfo
    {
        public static ItemGroup? SelectedGroup;
        public static ItemInfo? CurrentHovering;
    }

    internal class Util
    {
        public static string FormatSeconds(int totalSeconds)
        {
            int days = totalSeconds / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return $"{days}:{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

    }

    public class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int seconds)
                return Util.FormatSeconds(seconds);
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        public RelayCommand(Action<T> execute) => this.execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ItemGroup> _groups;
        private readonly ObservableCollection<ItemInfo> _items;
        private ICollectionView? _filteredItems;
        private ItemGroup? _selectedGroup;

        public ICommand ToggleActiveCommand { get; }
        public ICommand ToggleExclusive { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string CenterDisplay
        {
            get
            {
                if (SelectedGroup == null) return "";

                if (SharedInfo.CurrentHovering != null)
                {
                    string displayTotal = "Total : " + Util.FormatSeconds(SharedInfo.CurrentHovering.TotalSeconds);
                    string displayAverage = "Avg : " + Util.FormatSeconds(SharedInfo.CurrentHovering.AverageResetTime);
                    return displayTotal + " " + displayAverage;
                }

                int totalGroupSeconds = _items.Where(i => i.Group == SelectedGroup.Id).Sum(i => i.Seconds);
                return "Group Total : " + Util.FormatSeconds(totalGroupSeconds);
            }
        }

        public ICollectionView FilteredItems => _filteredItems!;
        public ItemGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    OnPropertyChanged(nameof(SelectedGroup));
                    OnPropertyChanged(nameof(CenterDisplay));
                    _filteredItems?.Refresh();
                }
            }
        }

        public MainWindowViewModel(ObservableCollection<ItemInfo> items, ObservableCollection<ItemGroup> groups)
        {
            _groups = groups;
            _items = items;

            _filteredItems = CollectionViewSource.GetDefaultView(_items);
            _filteredItems.Filter = FilterByGroup;

            ToggleActiveCommand = new RelayCommand<ButtonInfo>(item =>
            {
                if (item != null)
                    item.IsActive = !item.IsActive;
            });

            ToggleExclusive = new RelayCommand<ItemGroup>(group =>
            {
                if (group != null)
                {
                    foreach (ItemGroup g in _groups)
                        g.IsActive = false;

                    group.IsActive = true;
                    SelectedGroup = group;
                    SharedInfo.SelectedGroup = group;
                }
            });

            SetDefaultGroup();
        }

        public void SetDefaultGroup()
        {
            foreach (ItemGroup group in _groups)
                group.IsActive = false;

            SelectedGroup = _groups.First();
            SelectedGroup.IsActive = true;
            SharedInfo.SelectedGroup = SelectedGroup;

            OnPropertyChanged(nameof(CenterDisplay));
        }

        private bool FilterByGroup(object obj)
        {
            if (obj is ItemInfo item && SelectedGroup != null)
                return item.Group == SelectedGroup.Id;
            return false;
        }
    }

    public partial class MainWindow : Window
    {
        private ItemGroup? _hoveringGroup;
        private ItemInfo? _currentHovering;
        private ItemInfo? _cutItem;

        private int _seconds = 0;

        private const int SAVE_INTERVAL = 30;
        private readonly System.Windows.Threading.DispatcherTimer timer;

        ObservableCollection<ItemInfo> items;
        ObservableCollection<ItemGroup> groups;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;
            this.Closing += MainWindow_Closing;

            this.Focusable = true;
            this.Focus();

            items = new ObservableCollection<ItemInfo>();
            groups = new ObservableCollection<ItemGroup>();

            if (groups.Count == 0)
                groups.Add(new ItemGroup() { Id = "0", DisplayName = "Default" });

            GridList.ItemsSource = items;
            GroupList.ItemsSource = groups;

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            DataService.LoadItems(items, groups);
            SetDefaultGroups();

            DataContext = new MainWindowViewModel(items, groups);
        }

        private void updateHovering()
        {
            SharedInfo.CurrentHovering = _currentHovering;
            ((MainWindowViewModel)DataContext).OnPropertyChanged(nameof(MainWindowViewModel.CenterDisplay));
        }

        private void SetDefaultGroups()
        {
            foreach (ItemInfo item in items)
            {
                bool hasGroup = groups.Any(group => item.Group == group.Id);
                if (!hasGroup)
                    item.Group = "0";
            }
        }

        // ---------------------------
        // Event Handlers
        // ---------------------------
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox)
                return;

            switch (e.Key)
            {
                case Key.N:
                    items.Add(new ItemInfo()
                    {
                        Name = "New Stopwatch",
                        Seconds = 0,
                        TotalSeconds = 0,
                        Group = SharedInfo.SelectedGroup?.Id ?? "0",
                    });
                    break;

                case Key.R:
                    if (_currentHovering != null && _currentHovering.Seconds != 0)
                    {
                        _currentHovering.Seconds = 0;
                        _currentHovering.OnPropertyChanged(nameof(ItemInfo.Seconds));
                        _currentHovering.OnReset();
                        updateHovering();
                    }
                    break;

                case Key.Delete:
                    if (_currentHovering != null)
                    {
                        items.Remove(_currentHovering);
                        _currentHovering = null;
                        updateHovering();
                    }
                    else if (_hoveringGroup != null)
                    {
                        if (_hoveringGroup == ((MainWindowViewModel)DataContext).SelectedGroup)
                            ((MainWindowViewModel)DataContext).SetDefaultGroup();

                        groups.Remove(_hoveringGroup);
                        _hoveringGroup = null;
                    }
                    break;

                case Key.X:
                    if (_currentHovering != null)
                    {
                        _cutItem = _currentHovering;
                        items.Remove(_cutItem);
                        _currentHovering = null;
                        updateHovering();
                    }
                    break;

                case Key.V:
                    if (_cutItem != null)
                    {
                        _cutItem.Group = SharedInfo.SelectedGroup?.Id ?? "0";
                        items.Add(_cutItem);
                        _cutItem = null;
                    }
                    break;

                case Key.G:
                    groups.Add(new ItemGroup() { Id = Guid.NewGuid().ToString(), DisplayName = "New Group" });
                    break;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) => DataService.SaveItems(items, groups);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _seconds++;
            if (_seconds % SAVE_INTERVAL == 0)
                DataService.SaveItems(items, groups);

            foreach (ItemInfo item in items)
            {
                if (!item.IsActive) continue;
                item.Seconds++;
                item.TotalSeconds++;
                item.OnPropertyChanged(nameof(ItemInfo.Seconds));
                ((MainWindowViewModel)DataContext).OnPropertyChanged(nameof(MainWindowViewModel.CenterDisplay));
            }
        }

        private void TextBox_Focus(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Focusable = true;
                tb.IsReadOnly = false;
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.IsReadOnly = true;
                tb.Focusable = false;
                e.Handled = true;
                tb.SelectionLength = 0;
                tb.SelectionStart = 0;
            }
        }

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext != null)
            {
                _currentHovering = (ItemInfo)grid.DataContext;
                updateHovering();
            }
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is ItemInfo item)
            {
                if (_currentHovering == item)
                {
                    _currentHovering = null;
                    updateHovering();
                }
            }
        }

        private void Group_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext != null)
                _hoveringGroup = (ItemGroup)fe.DataContext;
        }

        private void Group_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ItemGroup group)
            {
                if (_hoveringGroup == group)
                    _hoveringGroup = null;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
