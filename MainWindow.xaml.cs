using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VPLAssistPlus.Data;
using VPLAssistPlus.Models;

namespace VPLAssistPlus
{
    public partial class MainWindow : Window
    {
        // Small helper class for DataGrid display (adds Total column)
        public class ProductRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
            public double Total { get; set; }
        }

        private readonly ProductRepository _repo;
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();

        public MainWindow()
        {
            InitializeComponent();

            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products.json");
            _repo = new ProductRepository(filePath);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbCategory.ItemsSource = new List<string> { "Electronics", "Grocery", "Stationery", "Clothing", "Other" };
            cmbCategory.SelectedIndex = 0;

            await LoadProductsAsync();
        }

        // MULTITHREADING: Load on background thread
        private async Task LoadProductsAsync()
        {
            ToggleUI(false);
            lblStatus.Text = "Loading...";

            try
            {
                var loaded = await Task.Run(() => _repo.Load());
                _products = new ObservableCollection<Product>(loaded);
                RefreshGrid();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load: " + ex.Message);
            }
            finally
            {
                lblStatus.Text = "";
                ToggleUI(true);
            }
        }

        // MULTITHREADING: Save on background thread
        private async Task SaveAsync()
        {
            try
            {
                await Task.Run(() => _repo.Save(_products.ToList()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message);
            }
        }

        private void ToggleUI(bool enabled)
        {
            btnAdd.IsEnabled = enabled;
            btnUpdate.IsEnabled = enabled;
            btnDelete.IsEnabled = enabled;
            btnClear.IsEnabled = enabled;
            btnSearch.IsEnabled = enabled;
            btnReset.IsEnabled = enabled;
        }

        private void RefreshGrid()
        {
            // Display computed Total
            var rows = _products.Select(p => new ProductRow
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                Quantity = p.Quantity,
                Total = p.Price * p.Quantity
            }).ToList();

            dgProducts.ItemsSource = new ObservableCollection<ProductRow>(rows);
        }

        private bool TryGetInputs(out Product p)
        {
            p = new Product();

            if (!int.TryParse(txtId.Text.Trim(), out int id))
            {
                MessageBox.Show("Invalid ID. Enter a number.");
                return false;
            }

            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.");
                return false;
            }

            if (!double.TryParse(txtPrice.Text.Trim(), out double price))
            {
                MessageBox.Show("Invalid price. Enter a numeric value.");
                return false;
            }
            if (price < 0)
            {
                MessageBox.Show("Price cannot be negative.");
                return false;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int qty))
            {
                MessageBox.Show("Invalid quantity. Enter a number.");
                return false;
            }
            if (qty < 0)
            {
                MessageBox.Show("Quantity cannot be negative.");
                return false;
            }

            p.Id = id;
            p.Name = name;
            p.Category = cmbCategory.SelectedItem != null ? cmbCategory.SelectedItem.ToString() : "Other";
            p.Price = price;
            p.Quantity = qty;

            return true;
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInputs(out Product p)) return;

            if (_products.Any(x => x.Id == p.Id))
            {
                MessageBox.Show("This Product ID already exists.");
                return;
            }

            _products.Add(p);
            RefreshGrid();
            UpdateSummary();
            await SaveAsync();
            ClearInputs();
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Selected row is ProductRow, we map back using ID
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Select a product to update.");
                return;
            }

            if (!TryGetInputs(out Product updated)) return;

            var selectedRow = (ProductRow)dgProducts.SelectedItem;
            var existing = _products.FirstOrDefault(x => x.Id == selectedRow.Id);
            if (existing == null)
            {
                MessageBox.Show("Selected product not found.");
                return;
            }

            // If ID changed, ensure uniqueness
            if (updated.Id != existing.Id && _products.Any(x => x.Id == updated.Id))
            {
                MessageBox.Show("New Product ID already exists.");
                return;
            }

            existing.Id = updated.Id;
            existing.Name = updated.Name;
            existing.Category = updated.Category;
            existing.Price = updated.Price;
            existing.Quantity = updated.Quantity;

            RefreshGrid();
            UpdateSummary();
            await SaveAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Select a product to delete.");
                return;
            }

            var row = (ProductRow)dgProducts.SelectedItem;
            var existing = _products.FirstOrDefault(x => x.Id == row.Id);
            if (existing == null) return;

            var result = MessageBox.Show("Delete " + existing.Name + "?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _products.Remove(existing);
            RefreshGrid();
            UpdateSummary();
            await SaveAsync();
            ClearInputs();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            txtId.Text = "";
            txtName.Text = "";
            txtPrice.Text = "";
            txtQty.Text = "";
            cmbCategory.SelectedIndex = 0;
        }

        private void DgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProducts.SelectedItem == null) return;

            var row = (ProductRow)dgProducts.SelectedItem;

            txtId.Text = row.Id.ToString();
            txtName.Text = row.Name;
            txtPrice.Text = row.Price.ToString();
            txtQty.Text = row.Quantity.ToString();
            cmbCategory.SelectedItem = row.Category;
        }

        // MULTITHREADING: Search on background thread
        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string term = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term))
            {
                MessageBox.Show("Enter search text (name or category).");
                return;
            }

            ToggleUI(false);
            lblStatus.Text = "Searching...";

            try
            {
                var results = await Task.Run(() =>
                    _products.Where(p =>
                        p.Name.ToLower().Contains(term) ||
                        p.Category.ToLower().Contains(term)
                    ).ToList()
                );

                var rows = results.Select(p => new ProductRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Total = p.Price * p.Quantity
                }).ToList();

                dgProducts.ItemsSource = new ObservableCollection<ProductRow>(rows);
                lblStatus.Text = "Found: " + rows.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message);
            }
            finally
            {
                ToggleUI(true);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = "";
            RefreshGrid();
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            lblTotalItems.Text = "Items: " + _products.Count;

            int totalQty = _products.Sum(p => p.Quantity);
            lblTotalQty.Text = "Total Qty: " + totalQty;

            double totalValue = _products.Sum(p => p.Price * p.Quantity);
            lblInventoryValue.Text = "Inventory Value: " + totalValue.ToString("F2");
        }
    }
}
