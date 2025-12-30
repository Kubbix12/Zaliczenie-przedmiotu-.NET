using CrudWpfApp.Models;
using CrudWpfApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CrudWpfApp.ViewModels
{
    public sealed class MainViewModel : BaseViewModel, IDataErrorInfo
    {
        private readonly JsonFileStore _store = new();
        private readonly string _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrudWpfApp",
            "products.json"
        );

        public ObservableCollection<Product> Items { get; } = new();
        public ICollectionView ItemsView { get; }

        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand SwitchToAddModeCommand { get; }

        public bool IsEditMode => Selected is not null;
        public bool IsAddMode => Selected is null;

        private Product? _selected;
        public Product? Selected
        {
            get => _selected;
            set
            {
                if (!Set(ref _selected, value)) return;

                if (value is null)
                {
                    OnPropertyChanged(nameof(IsEditMode));
                    OnPropertyChanged(nameof(IsAddMode));
                    RaiseAllCanExecute();
                    return;
                }

                Name = value.Name;
                Category = value.Category;
                PriceText = value.Price.ToString("0.##");
                StockText = value.Stock.ToString();

                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsAddMode));
                RaiseAllCanExecute();
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!Set(ref _searchText, value)) return;
                ItemsView.Refresh();
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (!Set(ref _name, value)) return;
                RaiseAllCanExecute();
            }
        }

        private string _category = "";
        public string Category
        {
            get => _category;
            set
            {
                if (!Set(ref _category, value)) return;
                RaiseAllCanExecute();
            }
        }

        private string _priceText = "";
        public string PriceText
        {
            get => _priceText;
            set
            {
                if (!Set(ref _priceText, value)) return;
                RaiseAllCanExecute();
            }
        }

        private string _stockText = "";
        public string StockText
        {
            get => _stockText;
            set
            {
                if (!Set(ref _stockText, value)) return;
                RaiseAllCanExecute();
            }
        }

        public MainViewModel()
        {
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItems;

            AddCommand = new RelayCommand(Add, CanAdd);
            UpdateCommand = new RelayCommand(Update, CanUpdate);
            DeleteCommand = new RelayCommand(Delete, CanDelete);
            ClearCommand = new RelayCommand(Clear);
            RestoreCommand = new RelayCommand(Restore, CanRestore);
            SwitchToAddModeCommand = new RelayCommand(SwitchToAddMode, CanSwitchToAddMode);

            _ = LoadAsync();
        }

        private bool FilterItems(object obj)
        {
            if (obj is not Product p) return false;
            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (q.Length == 0) return true;

            return p.Name.ToLowerInvariant().Contains(q)
                || p.Category.ToLowerInvariant().Contains(q)
                || p.Price.ToString().ToLowerInvariant().Contains(q)
                || p.Stock.ToString().ToLowerInvariant().Contains(q);
        }

        private bool DuplicateExists(string name, string category, Guid? ignoreId = null)
        {
            var n = (name ?? "").Trim();
            var c = (category ?? "").Trim();

            return Items.Any(p =>
                (ignoreId is null || p.Id != ignoreId.Value) &&
                string.Equals(p.Name?.Trim(), n, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.Category?.Trim(), c, StringComparison.OrdinalIgnoreCase)
            );
        }

        private void Add()
        {
            if (!ValidateForm(showMessage: true)) return;

            if (DuplicateExists(Name, Category))
            {
                MessageBox.Show(
                    "Taki produkt już istnieje w tej kategorii (ta sama nazwa + kategoria).",
                    "Duplikat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            var product = new Product
            {
                Name = Name.Trim(),
                Category = Category.Trim(),
                Price = ParsePrice(),
                Stock = ParseStock(),
            };

            Items.Add(product);
            Clear();
            _ = SaveAsync();
        }

        private void Update()
        {
            if (Selected is null) return;
            if (!ValidateForm(showMessage: true)) return;

            if (DuplicateExists(Name, Category, ignoreId: Selected.Id))
            {
                MessageBox.Show(
                    "Taki produkt już istnieje w tej kategorii (ta sama nazwa + kategoria).",
                    "Duplikat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            Selected.Name = Name.Trim();
            Selected.Category = Category.Trim();
            Selected.Price = ParsePrice();
            Selected.Stock = ParseStock();

            ItemsView.Refresh();
            _ = SaveAsync();
        }

        private void Delete()
        {
            if (Selected is null) return;

            var res = MessageBox.Show(
                $"Usunąć: {Selected.Name}?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (res != MessageBoxResult.Yes) return;

            Items.Remove(Selected);
            Clear();
            _ = SaveAsync();
        }

        private void Clear()
        {
            Selected = null;
            Name = "";
            Category = "";
            PriceText = "";
            StockText = "";
            RaiseAllCanExecute();
        }

        private void Restore()
        {
            if (Selected is null) return;

            Name = Selected.Name;
            Category = Selected.Category;
            PriceText = Selected.Price.ToString("0.##");
            StockText = Selected.Stock.ToString();
            RaiseAllCanExecute();
        }

        private void SwitchToAddMode()
        {
            Clear();
        }

        private bool CanAdd() => IsAddMode && ValidateForm(showMessage: false);
        private bool CanUpdate() => IsEditMode && ValidateForm(showMessage: false);
        private bool CanDelete() => IsEditMode;
        private bool CanRestore() => IsEditMode;
        private bool CanSwitchToAddMode() => IsEditMode;

        private void RaiseAllCanExecute()
        {
            AddCommand.RaiseCanExecuteChanged();
            UpdateCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            RestoreCommand.RaiseCanExecuteChanged();
            SwitchToAddModeCommand.RaiseCanExecuteChanged();
        }

        private decimal ParsePrice()
        {
            var raw = (PriceText ?? "").Trim().Replace(',', '.');
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0m;
        }

        private int ParseStock()
            => int.TryParse((StockText ?? "").Trim(), out var s) ? s : 0;

        private bool ValidateForm(bool showMessage)
        {
            var errors = new[]
            {
                this[nameof(Name)],
                this[nameof(Category)],
                this[nameof(PriceText)],
                this[nameof(StockText)],
            }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            if (errors.Count == 0) return true;

            if (showMessage)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, errors),
                    "Błędy walidacji",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            return false;
        }

        public string Error => "";

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Name):
                        if (string.IsNullOrWhiteSpace(Name)) return "Nazwa jest wymagana.";
                        if (Name.Trim().Length < 2) return "Nazwa musi mieć min. 2 znaki.";
                        if (Name.Trim().Length > 60) return "Nazwa może mieć max. 60 znaków.";
                        return "";

                    case nameof(Category):
                        if (string.IsNullOrWhiteSpace(Category)) return "Kategoria jest wymagana.";
                        if (Category.Trim().Length < 2) return "Kategoria musi mieć min. 2 znaki.";
                        if (Category.Trim().Length > 40) return "Kategoria może mieć max. 40 znaków.";
                        return "";

                    case nameof(PriceText):
                        if (string.IsNullOrWhiteSpace(PriceText)) return "Cena jest wymagana.";
                        var price = ParsePrice();
                        if (price <= 0) return "Cena musi być większa od 0.";
                        if (price > 1_000_000) return "Cena jest za duża (limit: 1 000 000).";
                        return "";

                    case nameof(StockText):
                        if (string.IsNullOrWhiteSpace(StockText)) return "Stan magazynowy jest wymagany.";
                        if (!int.TryParse((StockText ?? "").Trim(), out var s)) return "Stan magazynowy musi być liczbą całkowitą.";
                        if (s < 0) return "Stan magazynowy nie może być ujemny.";
                        if (s > 1_000_000) return "Stan magazynowy jest za duży (limit: 1 000 000).";
                        return "";
                }

                return "";
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                var data = await _store.LoadAsync<ObservableCollection<Product>>(_dbPath);
                if (data is null) return;

                Items.Clear();
                foreach (var x in data.OrderByDescending(x => x.CreatedAt))
                    Items.Add(x);

                ItemsView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się wczytać danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                var snapshot = new ObservableCollection<Product>(Items);
                await _store.SaveAsync(_dbPath, snapshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się zapisać danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
